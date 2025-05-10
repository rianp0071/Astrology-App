using Microsoft.EntityFrameworkCore;
using AstrologyApp.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ChatMessage> Messages { get; set; } // Table for storing chat messages
}
