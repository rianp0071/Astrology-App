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
builder.Services.AddScoped<AstrologyCalculator>();

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

        if (request.BirthTime == default)
        {
            return Results.BadRequest("Invalid birth time.");
        }

        if (string.IsNullOrWhiteSpace(request.BirthLocation))
        {
            return Results.BadRequest("Birth location cannot be empty.");
        }

        // Save the birthday and additional information
        birthdayService.SaveBirthday(request.Email, request.Birthday, request.BirthTime, request.BirthLocation);

        return Results.Ok("Birthday data saved successfully!");
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
        Birthday = user.Record.Birthday,
        BirthTime = user.Record.BirthTime,
        BirthLocation = user.Record.BirthLocation,
        SunSign = user.Record.SunSign,
        MoonSign = user.Record.MoonSign,
        RisingSign = user.Record.RisingSign
    });


    return Results.Ok(response);
});

app.MapGet("/getBirthday/{email}", (BirthdayService birthdayService, string email) =>
{
    var birthdayRecord = birthdayService.GetBirthdayRecord(email);

    if (birthdayRecord == null)
    {
        return Results.NotFound($"No birthday record found for email: {email}");
    }

    if (birthdayRecord.Birthday == default)
    {
        return Results.NotFound($"No birthday found for email: {email}");
    }

    return Results.Ok(new
    {
        Email = email,
        Birthday = birthdayRecord.Birthday,
        BirthTime = birthdayRecord.BirthTime,   
        BirthLocation = birthdayRecord.BirthLocation,
        SunSign = birthdayRecord.SunSign,
        MoonSign = birthdayRecord.MoonSign,
        RisingSign = birthdayRecord.RisingSign
    });
});


app.Run();

public class BirthdayRequest
{
    public required string Email { get; set; }
    public DateTime Birthday { get; set; }
    public TimeSpan BirthTime { get; set; }
    public string BirthLocation { get; set; } = string.Empty;
    public string SunSign { get; set; } = string.Empty; // Added Sun sign
    public string MoonSign { get; set; } = string.Empty; // Added Moon sign
    public string RisingSign { get; set; } = string.Empty; // Added Rising sign
}
