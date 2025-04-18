using SwissEphNet;

public class AstrologyCalculator
{
    private readonly string? _ephemerisPath;

    public AstrologyCalculator(IConfiguration configuration)
    {
        // Read the EphemerisPath from appsettings.json
        _ephemerisPath = configuration["EphemerisPath"];
    }

    public string GetSunSign(DateTime birthday)
    {
        int month = birthday.Month;
        int day = birthday.Day;

        return month switch
        {
            1 when day <= 19 => "Capricorn",
            1 => "Aquarius",
            2 when day <= 18 => "Aquarius",
            2 => "Pisces",
            3 when day <= 20 => "Pisces",
            3 => "Aries",
            4 when day <= 19 => "Aries",
            4 => "Taurus",
            5 when day <= 20 => "Taurus",
            5 => "Gemini",
            6 when day <= 20 => "Gemini",
            6 => "Cancer",
            7 when day <= 22 => "Cancer",
            7 => "Leo",
            8 when day <= 22 => "Leo",
            8 => "Virgo",
            9 when day <= 22 => "Virgo",
            9 => "Libra",
            10 when day <= 22 => "Libra",
            10 => "Scorpio",
            11 when day <= 21 => "Scorpio",
            11 => "Sagittarius",
            12 when day <= 21 => "Sagittarius",
            12 => "Capricorn",
            _ => "Unknown"
        };
    }

    public string GetMoonSign(string birthLocation, DateTime birthday, TimeSpan birthTime)
    {
        using var se = new SwissEph(); // No arguments for constructor
        
        // Check or set the ephemeris path explicitly
        se.swe_set_ephe_path(_ephemerisPath); 

        double julianDay = se.swe_julday(birthday.Year, birthday.Month, birthday.Day, birthTime.TotalHours, SwissEph.SE_GREG_CAL);
        var moonPosition = new double[6];
        string error = string.Empty;

        // Get the Moon's position
        se.swe_calc(julianDay, SwissEph.SE_MOON, SwissEph.SEFLG_SWIEPH, moonPosition, ref error);

        return GetZodiacSign(moonPosition[0]); // [0] is the longitude
    }

    public string GetRisingSign(string birthLocation, DateTime birthday, TimeSpan birthTime)
    {
        using var se = new SwissEph(); // Initialize Swiss Ephemeris
        
        se.swe_set_ephe_path(_ephemerisPath); // Set the ephemeris path

        double latitude = GetLatitude(birthLocation); // Fetch latitude
        double longitude = GetLongitude(birthLocation); // Fetch longitude
        double julianDay = se.swe_julday(birthday.Year, birthday.Month, birthday.Day, birthTime.TotalHours, SwissEph.SE_GREG_CAL);

        double[] houses = new double[13]; // Array for house cusps (0-indexed)
        double[] ascmc = new double[10]; // Array for Ascendant and Midheaven
        string error = string.Empty;

        // Calculate houses and Ascendant
        se.swe_houses(julianDay, latitude, longitude, 'P', houses, ascmc);

        // Validate Ascendant
        if (ascmc[0] == 0)
        {
            throw new InvalidOperationException("Ascendant calculation failed or returned zero.");
        }

        return GetZodiacSign(ascmc[0]); // Ascendant longitude is at index 0
    }


    private string GetZodiacSign(double position)
    {
        int sign = (int)(position / 30.0); // Divide longitude into 30° segments
        return sign switch
        {
            0 => "Aries",
            1 => "Taurus",
            2 => "Gemini",
            3 => "Cancer",
            4 => "Leo",
            5 => "Virgo",
            6 => "Libra",
            7 => "Scorpio",
            8 => "Sagittarius",
            9 => "Capricorn",
            10 => "Aquarius",
            11 => "Pisces",
            _ => "Unknown"
        };
    }

    private double GetLatitude(string location)
    {
        // Replace with geocoding API logic
        return 37.7749; // Example: San Francisco
    }

    private double GetLongitude(string location)
    {
        // Replace with geocoding API logic
        return -122.4194; // Example: San Francisco
    }

    private bool IsCompatible(string yourSunSign, string otherSunSign)
    {
        var compatibilityMap = new Dictionary<string, List<string>>
        {
            { "Aries", new List<string> { "Aries", "Leo", "Sagittarius", "Aquarius", "Gemini" } },
            { "Taurus", new List<string> { "Taurus", "Virgo", "Capricorn", "Pisces", "Cancer" } },
            { "Gemini", new List<string> { "Gemini", "Libra", "Aquarius", "Aries", "Leo" } },
            { "Cancer", new List<string> { "Cancer", "Pisces", "Scorpio", "Taurus", "Virgo" } },
            { "Leo", new List<string> { "Leo", "Aries", "Sagittarius", "Gemini", "Libra" } },
            { "Virgo", new List<string> { "Virgo", "Taurus", "Capricorn", "Cancer", "Scorpio" } },
            { "Libra", new List<string> { "Libra", "Gemini", "Aquarius", "Leo", "Sagittarius" } },
            { "Scorpio", new List<string> { "Scorpio", "Cancer", "Pisces", "Virgo", "Capricorn" } },
            { "Sagittarius", new List<string> { "Sagittarius", "Aries", "Leo", "Libra", "Aquarius" } },
            { "Capricorn", new List<string> { "Capricorn", "Taurus", "Virgo", "Scorpio", "Pisces" } },
            { "Aquarius", new List<string> { "Aquarius", "Gemini", "Libra", "Aries", "Sagittarius" } },
            { "Pisces", new List<string> { "Pisces", "Cancer", "Scorpio", "Taurus", "Capricorn" } }
        };

        return compatibilityMap.TryGetValue(yourSunSign, out var compatibleSigns) &&
            compatibleSigns.Contains(otherSunSign);
    }

    public int CalculateCompatibilityScore(
    string yourSunSign, string yourMoonSign, string yourRisingSign,
    string otherSunSign, string otherMoonSign, string otherRisingSign)
    {
        // Define weights
        const int sunWeight = 45;
        const int moonWeight = 35;
        const int risingWeight = 20;

        // Calculate individual compatibility scores (1 if compatible, 0 if not)
        int sunScore = IsCompatible(yourSunSign, otherSunSign) ? sunWeight : 0;
        int moonScore = IsCompatible(yourMoonSign, otherMoonSign) ? moonWeight : 0;
        int risingScore = IsCompatible(yourRisingSign, otherRisingSign) ? risingWeight : 0;

        // Combine the scores for a total out of 100
        int totalScore = sunScore + moonScore + risingScore;

        // Console.WriteLine($"Sun Compatibility: {sunScore}/{sunWeight}");
        // Console.WriteLine($"Moon Compatibility: {moonScore}/{moonWeight}");
        // Console.WriteLine($"Rising Compatibility: {risingScore}/{risingWeight}");
        // Console.WriteLine($"Total Compatibility Score: {totalScore}/100");

        return totalScore; // Return the final score
    }

}
