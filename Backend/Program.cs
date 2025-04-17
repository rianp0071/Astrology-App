using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Define the custom ApplicationDbContext for Identity
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseInMemoryDatabase("AppDb")); // Use an in-memory database for simplicity

// Add Authorization services
builder.Services.AddAuthorization();

// Add Identity services
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<IdentityDbContext>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5075")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<BirthdayService>();

var app = builder.Build();

app.UseCors(); 

// Automatically map Identity API endpoints (register, login, etc.)
app.MapIdentityApi<IdentityUser>();

app.MapGet("/users", async (UserManager<IdentityUser> userManager) =>
{
    var emails = await userManager.Users
        .Select(user => user.Email)
        .ToListAsync();

    return Results.Ok(emails);
});

app.MapGet("/unprotected", () => "This is an unprotected route!");

app.MapGet("/", () => "This is a protected route!")
    .RequireAuthorization();

app.MapPost("/saveBirthday", (BirthdayService birthdayService, BirthdayRequest request) =>
{
    try
    {
        // Validate the input
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest("Email cannot be empty.");
        }

        if (request.Birthday == default)
        {
            return Results.BadRequest("Invalid birthday.");
        }

        // Save the birthday
        birthdayService.SaveBirthday(request.Email, request.Birthday);

        return Results.Ok("Birthday saved successfully!");
    }
    catch (Exception ex)
    {
        // Log the exception (optional)
        Console.WriteLine($"An error occurred: {ex.Message}");

        // Return a generic error response
        return Results.Problem("An error occurred while saving the birthday. Please try again later.");
    }
});

app.MapGet("/getAllUsersWithBirthdays", (BirthdayService birthdayService) =>
{
    var usersWithBirthdays = birthdayService.GetAllBirthdays();

    if (!usersWithBirthdays.Any())
    {
        return Results.NotFound("No users with birthdays found.");
    }

    var response = usersWithBirthdays.Select(user => new
    {
        Email = user.Email,
        Birthday = user.Birthday
    });

    return Results.Ok(response);
});

app.MapGet("/getBirthday/{email}", (BirthdayService birthdayService, string email) =>
{
    var birthday = birthdayService.GetBirthday(email);

    if (birthday == null)
    {
        return Results.NotFound($"No birthday found for email: {email}");
    }

    return Results.Ok(new
    {
        Email = email,
        Birthday = birthday
    });
});


app.Run();

public class BirthdayRequest
{
    public required string Email { get; set; }
    public DateTime Birthday { get; set; }
}
