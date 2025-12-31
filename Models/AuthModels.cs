namespace WebAPI.Models;

public record RegisterContributorRequest(
    string Email,
    string Password,
    string FullName,
    string? StageName,
    string Address,
    string PhoneNumber,
    decimal HourlyRate,
    string? Bio,
    string? PhotoUrl
);

public record LoginRequest(string Email, string Password);
