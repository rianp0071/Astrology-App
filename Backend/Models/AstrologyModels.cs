namespace AstrologyApp.Models // Replace with your project's namespace
{
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

    public class CompatibilityRequest
    {
        public PersonDetails Person1 { get; set; } = new PersonDetails();
        public PersonDetails Person2 { get; set; } = new PersonDetails();
    }

    public class PersonDetails
    {
        public string Name { get; set; } = string.Empty;
        public DateTime Birthday { get; set; }
        public TimeSpan BirthTime { get; set; }
        public string BirthLocation { get; set; } = string.Empty;
    }

    public class GetSignsRequest
    {
        public DateTime Birthday { get; set; }
        public TimeSpan BirthTime { get; set; }
        public string? BirthLocation { get; set; } // Optional field for location
    }
}
