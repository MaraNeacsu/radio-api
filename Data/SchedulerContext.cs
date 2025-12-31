using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models;

namespace WebAPI.Data
{
    public class SchedulerContext(DbContextOptions<SchedulerContext> options) : IdentityDbContext<AplicationUser>(options)
    {
        public DbSet<ScheduleEvent> Events => Set<ScheduleEvent>();
        public DbSet<Contributor> Contributors => Set<Contributor>();
        public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

          
            builder.Entity<AplicationUser>()
                .HasOne(u => u.Contributor)
                .WithOne(c => c.User)
                .HasForeignKey<Contributor>(c => c.UserId);

            
            builder.Entity<PaymentRecord>()
                .HasOne(p => p.Contributor)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.ContributorId);

            
            builder.Entity<ScheduleEvent>()
                .HasOne(e => e.HostContributor)
                .WithMany(c => c.HostedEvents)
                .HasForeignKey(e => e.HostContributorId)
                .OnDelete(DeleteBehavior.Restrict);

          
            builder.Entity<ScheduleEvent>()
                .HasOne(e => e.GuestContributor)
                .WithMany(c => c.GuestEvents)
                .HasForeignKey(e => e.GuestContributorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
