using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebAPI.Data;
using WebAPI.Models;
using WebAPI.Services;

var builder = WebApplication.CreateBuilder(args);


// 1. DB Context

builder.Services.AddDbContext<SchedulerContext>(options =>
    options.UseSqlServer(
        "Server=(localdb)\\mssqllocaldb;Database=SchedulerDB;Trusted_Connection=True;MultipleActiveResultSets=true"));


// 2. Identity + Auth

builder.Services
    .AddIdentity<AplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<SchedulerContext>()
    .AddDefaultTokenProviders();

// Cookie settings 
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "LexiconRadio.Auth";
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Authentication & Authorization
builder.Services.AddAuthentication();

var authBuilder = builder.Services.AddAuthorizationBuilder();
authBuilder.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

// 3. Services
builder.Services.AddScoped<IPaymentService, PaymentService>();

// 4. Swagger + CORS

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:5176",
                "http://localhost:5181"   
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});



var app = builder.Build();


// 5. Seed data (Admin user)

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var db = services.GetRequiredService<SchedulerContext>();
    await db.Database.MigrateAsync();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<AplicationUser>>();

    const string adminRole = "Admin";
    if (!await roleManager.RoleExistsAsync(adminRole))
    {
        await roleManager.CreateAsync(new IdentityRole(adminRole));
    }

    const string adminEmail = "admin@radio.local";
    const string adminPassword = "Admin123!";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new AplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, adminRole);
        }
    }
}


// 6. Middleware

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();



// Register new contributor (creates Identity user + Contributor)
app.MapPost("/api/auth/register-contributor", async (
    RegisterContributorRequest request,
    UserManager<AplicationUser> userManager,
    SchedulerContext db) =>
{
    var existing = await userManager.FindByEmailAsync(request.Email);
    if (existing != null)
    {
        return Results.BadRequest(new { message = "Email already registered." });
    }

    var user = new AplicationUser
    {
        UserName = request.Email,
        Email = request.Email
    };

    var createResult = await userManager.CreateAsync(user, request.Password);
    if (!createResult.Succeeded)
    {
        var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
        return Results.BadRequest(new { message = errors });
    }

    var contributor = new Contributor
    {
        FullName = request.FullName,
        StageName = request.StageName,
        Address = request.Address,
        PhoneNumber = request.PhoneNumber,
        Email = request.Email,
        Bio = request.Bio,
        PhotoUrl = request.PhotoUrl,
        HourlyRate = request.HourlyRate,
        UserId = user.Id
    };

    db.Contributors.Add(contributor);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        contributor.Id,
        contributor.FullName,
        contributor.StageName,
        contributor.Email
    });
});

// Login 
app.MapPost("/api/auth/login", async (
    LoginRequest request,
    SignInManager<AplicationUser> signInManager,
    UserManager<AplicationUser> userManager,
    SchedulerContext db) =>
{
    var user = await userManager.FindByEmailAsync(request.Email);
    if (user == null)
        return Results.BadRequest(new { message = "Invalid email or password." });

    var signInResult = await signInManager.PasswordSignInAsync(
        user,
        request.Password,
        isPersistent: false,
        lockoutOnFailure: false);

    if (!signInResult.Succeeded)
        return Results.BadRequest(new { message = "Invalid email or password." });

    var contributor = await db.Contributors
        .Include(c => c.Payments)
        .FirstOrDefaultAsync(c => c.UserId == user.Id);

    if (contributor == null)
        return Results.NotFound(new { message = "No contributor profile linked to this user." });

    return Results.Ok(new
    {
        contributor.Id,
        contributor.FullName,
        contributor.StageName,
        contributor.Email
    });
});

// Logout
app.MapPost("/api/auth/logout", async (SignInManager<AplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
});


//   API ENDPOINTS   


// Get today's schedule
app.MapGet("/schedule/today", async (SchedulerContext db) =>
{
    var today = DateTime.Today;
    var events = await db.Events
        .Where(e => e.StartTime.Date == today)
        .OrderBy(e => e.StartTime)
        .ToListAsync();

    return Results.Ok(events);
});

// Get schedule for next 7 days
app.MapGet("/schedule/week", async (SchedulerContext db) =>
{
    var start = DateTime.Today;
    var end = start.AddDays(7);

    var events = await db.Events
        .Where(e => e.StartTime >= start && e.StartTime < end)
        .OrderBy(e => e.StartTime)
        .ToListAsync();

    return Results.Ok(events);
});

// Get single event details
app.MapGet("/event/{id}", async (int id, SchedulerContext db) =>
{
    var ev = await db.Events.FindAsync(id);
    return ev is not null ? Results.Ok(ev) : Results.NotFound();
});

// Create event
app.MapPost("/event", async (ScheduleEvent newEvent, SchedulerContext db) =>
{
    db.Events.Add(newEvent);
    await db.SaveChangesAsync();
    return Results.Created($"/event/{newEvent.Id}", newEvent);
});

// Reschedule event
app.MapPut("/event/{id}/reschedule", async (int id, DateTime newStart, DateTime newEnd, SchedulerContext db) =>
{
    var ev = await db.Events.FindAsync(id);
    if (ev is null) return Results.NotFound();

    ev.StartTime = newStart;
    ev.EndTime = newEnd;
    await db.SaveChangesAsync();
    return Results.Ok(ev);
});

// Assign host by contributorId
app.MapPut("/event/{id}/assign-host", async (int id, int contributorId, SchedulerContext db) =>
{
    var ev = await db.Events.FindAsync(id);
    var contributor = await db.Contributors.FindAsync(contributorId);

    if (ev is null || contributor is null)
        return Results.NotFound();

    ev.HostContributorId = contributorId;
    ev.Host = contributor.StageName;
    await db.SaveChangesAsync();

    return Results.Ok(ev);
});

// Assign guest by contributorId
app.MapPut("/event/{id}/assign-guest", async (int id, int contributorId, SchedulerContext db) =>
{
    var ev = await db.Events.FindAsync(id);
    var contributor = await db.Contributors.FindAsync(contributorId);

    if (ev is null || contributor is null)
        return Results.NotFound();

    ev.GuestContributorId = contributorId;
    ev.Guest = contributor.StageName;
    await db.SaveChangesAsync();

    return Results.Ok(ev);
});

// Delete event
app.MapDelete("/event/{id}", async (int id, SchedulerContext db) =>
{
    var ev = await db.Events.FindAsync(id);
    if (ev is null) return Results.NotFound();

    db.Events.Remove(ev);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Contributors (ADMIN) 


app.MapGet("/api/admin/contributors", async (SchedulerContext db) =>
{
    var contributors = await db.Contributors
        .Include(c => c.Payments)
        .ToListAsync();

    return Results.Ok(contributors);
}).RequireAuthorization("AdminOnly");

// GET: contributor by id (ADMIN)
app.MapGet("/api/admin/contributors/{id}", async (int id, SchedulerContext db) =>
{
    var contributor = await db.Contributors
        .Include(c => c.Payments)
        .FirstOrDefaultAsync(c => c.Id == id);

    return contributor is not null ? Results.Ok(contributor) : Results.NotFound();
}).RequireAuthorization("AdminOnly");


app.MapPost("/api/admin/contributors", async (Contributor newContributor, SchedulerContext db) =>
{
    db.Contributors.Add(newContributor);
    await db.SaveChangesAsync();
    return Results.Created($"/api/admin/contributors/{newContributor.Id}", newContributor);
}).RequireAuthorization("AdminOnly");


app.MapPut("/api/admin/contributors/{id}", async (int id, Contributor updated, SchedulerContext db) =>
{
    var contributor = await db.Contributors.FindAsync(id);
    if (contributor is null) return Results.NotFound();

    contributor.FullName = updated.FullName;
    contributor.Address = updated.Address;
    contributor.PhoneNumber = updated.PhoneNumber;
    contributor.Email = updated.Email;
    contributor.StageName = updated.StageName;
    contributor.Bio = updated.Bio;
    contributor.PhotoUrl = updated.PhotoUrl;
    contributor.HourlyRate = updated.HourlyRate;

    await db.SaveChangesAsync();
    return Results.Ok(contributor);
}).RequireAuthorization("AdminOnly");


app.MapPost("/api/admin/contributors/{id}/payments/generate",
    async (int id, int year, int month,
           SchedulerContext db,
           IPaymentService paymentService) =>
    {
        try
        {
            var record = await paymentService.GenerateMonthlyPaymentAsync(id, year, month, db);
            return Results.Ok(record);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }).RequireAuthorization("AdminOnly");



app.MapGet("/api/me", async (SchedulerContext db, HttpContext http) =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var contributor = await db.Contributors
        .Include(c => c.Payments)
        .FirstOrDefaultAsync(c => c.UserId == userId);

    if (contributor is null)
        return Results.NotFound("Contributor not found for logged-in user");

    return Results.Ok(new
    {
        contributor.Id,
        contributor.FullName,
        contributor.StageName,
        contributor.Address,
        contributor.PhoneNumber,
        contributor.Email,
        contributor.HourlyRate,
        contributor.Bio,
        contributor.PhotoUrl,
        Payments = contributor.Payments.Select(p => new
        {
            p.Id,
            p.Amount,
            p.PaymentDate,
            p.Description
        })
    });
}).RequireAuthorization();


app.Run();
