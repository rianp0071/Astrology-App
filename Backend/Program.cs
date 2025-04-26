using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AstrologyApp.Models;


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

app.MapGet("/users/{email}", async (UserManager<IdentityUser> userManager, string email) =>
{
    var userExists = await userManager.FindByEmailAsync(email) != null;
    return userExists ? Results.Ok() : Results.NotFound();
});

app.MapGet("/unprotected", () => "This is an unprotected route!");

app.MapGet("/", () => "This is a protected route!")
    .RequireAuthorization();

app.MapPost("/saveBirthday", async (BirthdayService birthdayService, BirthdayRequest request) =>
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
app.MapGet("/getTop10CompatibilityWithOtherUsers/{email}", (BirthdayService birthdayService, string email) =>
{
    // Get the current user's birthday record
    var currentUser = birthdayService.GetBirthdayRecord(email);

    if (currentUser == null)
    {
        return Results.NotFound($"No birthday record found for email: {email}");
    }

    // Get all other users' records
    var allUsers = birthdayService.GetAllBirthdays()
        .Where(user => user.Email != email); // Exclude the current user

    if (!allUsers.Any())
    {
        return Results.NotFound("No other users found.");
    }

    // PriorityQueue to maintain top 10 compatible users
    var priorityQueue = new PriorityQueue<(string Email, BirthdayRecord Record, int CompatibilityScore), int>();
    var lockObject = new object(); // Lock object for thread safety

    // Parallel Processing to calculate compatibility scores
    Parallel.ForEach(allUsers, user =>
    {
        var otherRecord = user.Record;

        // Calculate compatibility score
        var compatibilityScore = new AstrologyCalculator(new ConfigurationBuilder().Build())
            .CalculateCompatibilityScore(
                currentUser.SunSign, currentUser.MoonSign, currentUser.RisingSign,
                otherRecord.SunSign, otherRecord.MoonSign, otherRecord.RisingSign
            );

        // Thread-safe operations with PriorityQueue
        lock (lockObject)
        {
            if (priorityQueue.Count < 10)
            {
                priorityQueue.Enqueue((user.Email, otherRecord, compatibilityScore), compatibilityScore);
            }
            else if (compatibilityScore > priorityQueue.Peek().CompatibilityScore)
            {
                priorityQueue.Dequeue(); // Remove the smallest
                priorityQueue.Enqueue((user.Email, otherRecord, compatibilityScore), compatibilityScore);
            }
        }
    });

    // Extract top 10 users from the priority queue
    var top10Users = new List<(string Email, BirthdayRecord Record, int CompatibilityScore)>();
    while (priorityQueue.Count > 0)
    {
        top10Users.Add(priorityQueue.Dequeue());
    }

    // Sort the results in descending order by compatibility score
    top10Users = top10Users.OrderByDescending(u => u.CompatibilityScore).ToList();

    // Format the response
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

app.Run();
