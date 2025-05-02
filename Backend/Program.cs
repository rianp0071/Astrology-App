using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AstrologyApp.Models;
using System.Security.Claims;


// Implement cCACHING LATER ON FOR BETTER PERFORMANCE!!!!!!!!!!!!!

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
builder.Services.AddMemoryCache(); // Registers IMemoryCache
builder.Services.AddScoped<AstrologyCalculator>(); // Registers AstrologyCalculator as a scoped service

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

app.MapGet("/users/{email}", async (UserManager<IdentityUser> userManager, string email) =>
{
    var userExists = await userManager.FindByEmailAsync(email) != null;
    return userExists ? Results.Ok() : Results.NotFound();
});

app.MapGet("/unprotected", () => "This is an unprotected route!");

app.MapGet("/", () => "This is a protected route!")
    .RequireAuthorization();

app.MapPost("/saveBirthday", async (BirthdayService birthdayService, BirthdayRequest request, AstrologyCalculator calculator) =>
{
    try
    {
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

        await birthdayService.SaveBirthdayAsync(request.Email, request.Birthday, request.BirthTime, request.BirthLocation);
        calculator.ClearCompatibilityCache(request.Email); // Clear cache for the user after saving birthday

        return Results.Ok("Birthday data saved successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
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
        user.Email,
        user.Record.Birthday,
        user.Record.BirthTime,
        user.Record.BirthLocation,
        user.Record.SunSign,
        user.Record.MoonSign,
        user.Record.RisingSign
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
        birthdayRecord.Birthday,
        birthdayRecord.BirthTime,
        birthdayRecord.BirthLocation,
        birthdayRecord.SunSign,
        birthdayRecord.MoonSign,
        birthdayRecord.RisingSign
    });
});

// WHEN SWITCHING TO A DATABASE, USE INDEXING FOR BETTER PERFORMANCE!!!!!!!!!!!!
app.MapGet("/getTop10CompatibilityWithOtherUsers/{email}", (BirthdayService birthdayService, string email, IServiceProvider serviceProvider) =>
{
    var currentUser = birthdayService.GetBirthdayRecord(email);

    if (currentUser == null)
    {
        return Results.NotFound($"No birthday record found for email: {email}");
    }

    var allUsers = birthdayService.GetAllBirthdays()
        .Where(user => user.Email != email);

    if (!allUsers.Any())
    {
        return Results.NotFound("No other users found.");
    }

    var priorityQueue = new PriorityQueue<(string Email, BirthdayRecord Record, int CompatibilityScore), int>();
    var lockObject = new object();

    Parallel.ForEach(allUsers, user =>
    {
        var otherRecord = user.Record;

        using var scope = serviceProvider.CreateScope();
        var calculator = scope.ServiceProvider.GetRequiredService<AstrologyCalculator>();

        var compatibilityScore = calculator.CalculateCompatibilityScoreWithCaching(
            email, user.Email, currentUser, otherRecord);

        lock (lockObject)
        {
            if (priorityQueue.Count < 10)
            {
                priorityQueue.Enqueue((user.Email, otherRecord, compatibilityScore), compatibilityScore);
            }
            else if (compatibilityScore > priorityQueue.Peek().CompatibilityScore)
            {
                priorityQueue.Dequeue();
                priorityQueue.Enqueue((user.Email, otherRecord, compatibilityScore), compatibilityScore);
            }
        }
    });

    var top10Users = new List<(string Email, BirthdayRecord Record, int CompatibilityScore)>();
    while (priorityQueue.Count > 0)
    {
        top10Users.Add(priorityQueue.Dequeue());
    }

    top10Users = top10Users.OrderByDescending(u => u.CompatibilityScore).ToList();

    var response = top10Users.Select(user => new
    {
        user.Email,
        user.Record.Birthday,
        user.Record.BirthTime,
        user.Record.BirthLocation,
        user.Record.SunSign,
        user.Record.MoonSign,
        user.Record.RisingSign,
        user.CompatibilityScore
    });

    return Results.Ok(response);
});


app.MapPost("/calculateCompatibility", async (AstrologyCalculator calculator, CompatibilityRequest request) =>
{
    try
    {
        // Validate input
        if (request.Person1 == null || request.Person2 == null)
        {
            return Results.BadRequest("Both individuals' details must be provided.");
        }

        // Dynamically calculate Sun sign, Moon sign, and Rising sign for Person 1
        var person1SunSign = calculator.GetSunSign(request.Person1.Birthday);
        var person1MoonSign = calculator.GetMoonSign(request.Person1.BirthLocation, request.Person1.Birthday, request.Person1.BirthTime);
        var person1RisingSign = await calculator.GetRisingSignAsync(request.Person1.BirthLocation, request.Person1.Birthday, request.Person1.BirthTime);

        // Dynamically calculate Sun sign, Moon sign, and Rising sign for Person 2
        var person2SunSign = calculator.GetSunSign(request.Person2.Birthday);
        var person2MoonSign = calculator.GetMoonSign(request.Person2.BirthLocation, request.Person2.Birthday, request.Person2.BirthTime);
        var person2RisingSign = await calculator.GetRisingSignAsync(request.Person2.BirthLocation, request.Person2.Birthday, request.Person2.BirthTime);

        // Calculate compatibility score
        var compatibilityScore = calculator.CalculateCompatibilityScore(
            person1SunSign, person1MoonSign, person1RisingSign,
            person2SunSign, person2MoonSign, person2RisingSign
        );

        // Generate compatibility message
        var compatibilityMessage = compatibilityScore switch
        {
            >= 80 => $"Soulmates! You scored {compatibilityScore}%. A perfect match full of harmony!",
            >= 50 => $"Not bad! You scored {compatibilityScore}%. With a little effort, this could work!",
            _ => $"Oh no! You scored {compatibilityScore}%. Opposites might attract, but this could be a challenge!"
        };

        // Return the result
        return Results.Ok(compatibilityMessage);
    }
    catch (Exception ex)
    {
        // Log the exception (optional)
        Console.WriteLine($"An error occurred: {ex.Message}");

        // Return a generic error response
        return Results.Problem("An error occurred while calculating compatibility. Please try again later.");
    }
});

app.MapPost("/getSigns", async (AstrologyCalculator calculator, GetSignsRequest request) =>
{
    // Validate the input
    if (request.Birthday == default)
    {
        return Results.BadRequest("Invalid or missing birthday.");
    }

    if (string.IsNullOrWhiteSpace(request.BirthLocation))
    {
        // If BirthLocation is null, calculate and return only the Sun sign
        var sunSign = calculator.GetSunSign(request.Birthday);
        return Results.Ok(new { SunSign = sunSign });
    }
    else
    {
        // If BirthLocation is provided, calculate and return Sun, Moon, and Rising signs
        var sunSign = calculator.GetSunSign(request.Birthday);
        var moonSign = calculator.GetMoonSign(request.BirthLocation, request.Birthday, request.BirthTime);
        var risingSign = await calculator.GetRisingSignAsync(request.BirthLocation, request.Birthday, request.BirthTime);

        return Results.Ok(new
        {
            SunSign = sunSign,
            MoonSign = moonSign,
            RisingSign = risingSign
        });
    }
});

app.MapPost("/createProfile", async (UserManager<IdentityUser> userManager, ProfileRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest("Email cannot be null or empty.");
    }

    var user = await userManager.FindByEmailAsync(request.Email);
    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    // Store profile data using user claims (no extra DB needed)
    var claims = new List<Claim>
    {
        new Claim("Name", request.Name),
        new Claim("Age", request.Age.ToString()),
        new Claim("Pronouns", request.Pronouns),
        new Claim("Description", request.Description)
    };

    try
    {
        await userManager.AddClaimsAsync(user, claims);
        return Results.Ok("Profile saved successfully!");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error saving profile: {ex.Message}");
    }
});

app.MapGet("/getProfile/{email}", async (UserManager<IdentityUser> userManager, string email) =>
{
    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    var claims = await userManager.GetClaimsAsync(user);

    var profile = new
    {
        Email = user.Email,
        Name = claims.FirstOrDefault(c => c.Type == "Name")?.Value ?? "Not set",
        Age = claims.FirstOrDefault(c => c.Type == "Age")?.Value ?? "Not set",
        Pronouns = claims.FirstOrDefault(c => c.Type == "Pronouns")?.Value ?? "Not set",
        Description = claims.FirstOrDefault(c => c.Type == "Description")?.Value ?? "Not set"
    };

    return Results.Ok(profile);
});


app.Run();
