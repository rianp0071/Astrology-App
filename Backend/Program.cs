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


var app = builder.Build();

app.UseCors(); 

// Automatically map Identity API endpoints (register, login, etc.)
app.MapIdentityApi<IdentityUser>();

app.MapGet("/unprotected", () => "This is an unprotected route!");

app.MapGet("/", () => "This is a protected route!")
    .RequireAuthorization();

app.Run();
