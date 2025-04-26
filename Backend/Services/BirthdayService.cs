using System.Threading.Tasks;

public class BirthdayService
{
    private readonly Dictionary<string, BirthdayRecord> _birthdays = new();
    private readonly IServiceProvider _serviceProvider; // Service provider to resolve scoped services

    public BirthdayService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider; // Injected via DI
    }

    // Save or update the birthday data associated with an email
    public async Task SaveBirthdayAsync(string email, DateTime birthday, TimeSpan birthTime, string birthLocation)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope(); // Create a new DI scope
            var calculator = scope.ServiceProvider.GetRequiredService<AstrologyCalculator>(); // Resolve AstrologyCalculator from DI
            // Resolve scoped service

            if (string.IsNullOrWhiteSpace(birthLocation))
                throw new ArgumentException("Birth location cannot be null or empty.", nameof(birthLocation));

            if (birthday == default)
                throw new ArgumentException("Invalid birthday provided.", nameof(birthday));

            if (birthTime == default)
                throw new ArgumentException("Invalid birth time provided.", nameof(birthTime));

            var sunSign = calculator.GetSunSign(birthday); // Calculate Sun sign
            var moonSign = calculator.GetMoonSign(birthLocation, birthday, birthTime); // Calculate Moon sign
            var risingSign = await calculator.GetRisingSignAsync(birthLocation, birthday, birthTime); // Calculate Rising sign

            _birthdays[email] = new BirthdayRecord
            {
                Birthday = birthday,
                BirthTime = birthTime,
                BirthLocation = birthLocation,
                SunSign = sunSign,
                MoonSign = moonSign,
                RisingSign = risingSign
            };
        }
        catch (Exception ex)
        {
            // Log the exception (ensure a logging mechanism is in place)
            Console.WriteLine($"Error saving birthday: {ex.Message}");
            throw new InvalidOperationException("An error occurred while saving the birthday. Please try again later.", ex);
        }
    }

    // Retrieve a user's birthday record based on email
    public BirthdayRecord? GetBirthdayRecord(string email)
    {
        return _birthdays.TryGetValue(email, out var record) ? record : null;
    }

    // Retrieve all saved email-birthday records
    public IEnumerable<(string Email, BirthdayRecord Record)> GetAllBirthdays()
    {
        return _birthdays.Select(b => (b.Key, b.Value));
    }
}

// Record to hold full birthday details
public class BirthdayRecord
{
    public DateTime Birthday { get; set; }
    public TimeSpan BirthTime { get; set; }
    public string BirthLocation { get; set; } = string.Empty;
    public string SunSign { get; set; } = string.Empty; // Added Sun sign
    public string MoonSign { get; set; } = string.Empty; // Added Moon sign
    public string RisingSign { get; set; } = string.Empty; // Added Rising sign
}
