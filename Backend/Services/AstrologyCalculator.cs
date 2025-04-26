using SwissEphNet;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using AstrologyApp.Models; // Adjust the namespace according to your project structure

// USE CACHING FOR GEOCODING RESULTS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

public class AstrologyCalculator
{
    private readonly string? _ephemerisPath;
    private readonly string? _apiKey;
    private readonly IMemoryCache _cache;
    private readonly HashSet<string> _cacheKeys = new(); // Tracks keys

    public AstrologyCalculator(IConfiguration configuration, IMemoryCache cache)
    {
        _ephemerisPath = configuration["EphemerisPath"];
        _apiKey = configuration["GoogleGeocodingApiKey"];
        _cache = cache;
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

    public async Task<string> GetRisingSignAsync(string birthLocation, DateTime birthday, TimeSpan birthTime)
    {
        using var se = new SwissEph(); // Initialize Swiss Ephemeris
        
        se.swe_set_ephe_path(_ephemerisPath); // Set the ephemeris path

        double latitude = await GetLatitudeAsync(birthLocation); // Fetch latitude
        double longitude = await GetLongitudeAsync(birthLocation); // Fetch longitude
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

    // Method to fetch latitude
    private async Task<double> GetLatitudeAsync(string location)
    {
        var geometryLocation = await GetLocationAsync(location);
        return geometryLocation.Latitude;
    }

    // Method to fetch latitude
    private async Task<double> GetLongitudeAsync(string location)
    {
        var geometryLocation = await GetLocationAsync(location);
        return geometryLocation.Longitude;
    }

    private async Task<Location> GetLocationAsync(string location)
    {
        if (!_cache.TryGetValue(location, out Location? cachedLocation) || cachedLocation == null)
        {
            // Fetch location from API and cache it
            var fetchedLocation = await FetchLocationFromApiAsync(location);
            _cache.Set(location, fetchedLocation, TimeSpan.FromHours(6)); // Cache for 6 hours
            return fetchedLocation;
        }

        return cachedLocation; // Use cached result
    }
   
    private async Task<Location> FetchLocationFromApiAsync(string location)
    {
        string escapedLocation = CustomEscapeDataString(location);
        var apiUrl = $"https://maps.googleapis.com/maps/api/geocode/json?address={escapedLocation}&key={_apiKey}";

        using var client = new HttpClient();
        // Console.WriteLine($"API URL: {apiUrl}");

        var response = await client.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();

        var rawJson = await response.Content.ReadAsStringAsync();
        // Console.WriteLine($"Raw JSON Response: {rawJson}");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var result = JsonSerializer.Deserialize<GeocodingResponse>(rawJson, options);

        if (result?.Status != "OK" || result?.Results == null || !result.Results.Any())
        {
            throw new Exception($"Geocoding API error: {result?.Status}");
        }

        var geometry = result.Results.First().Geometry;
        string geometryJson = JsonSerializer.Serialize(geometry, new JsonSerializerOptions { WriteIndented = true });
        // Console.WriteLine($"Geometry JSON: {geometryJson}");

        // Console.WriteLine($"Location: {geometry.Location.Latitude}, {geometry.Location.Longitude}");

        return geometry.Location; // Return the location only
    }



    private string CustomEscapeDataString(string location)
    {
        // Escape the location
        var escapedLocation = Uri.EscapeDataString(location);

        // Replace "%2C" back to ","
        return escapedLocation.Replace("%2C%20", ",");
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

    public int CalculateCompatibilityScoreWithCaching(
    string yourEmail, string otherEmail,
    BirthdayRecord yourRecord, BirthdayRecord otherRecord)
    {
        var cacheKey = string.Compare(yourEmail, otherEmail, StringComparison.Ordinal) < 0 
            ? $"{yourEmail}:{otherEmail}" 
            : $"{otherEmail}:{yourEmail}";

        if (_cache.TryGetValue(cacheKey, out int cachedScore))
        {
            Console.WriteLine(_cacheKeys.Count);
            return cachedScore;
        }

        var compatibilityScore = CalculateCompatibilityScore(
            yourRecord.SunSign, yourRecord.MoonSign, yourRecord.RisingSign,
            otherRecord.SunSign, otherRecord.MoonSign, otherRecord.RisingSign
        );

        _cache.Set(cacheKey, compatibilityScore, TimeSpan.FromSeconds(6));
        _cacheKeys.Add(cacheKey); // Track the cache key

        return compatibilityScore;
    }


    public void ClearCompatibilityCache(string email)
    {
        // Normalize email to avoid case-sensitivity issues
        var normalizedEmail = email.ToLowerInvariant();

        // Find keys related to the given email
        var keysToRemove = _cacheKeys
            .Where(key => key.StartsWith($"{normalizedEmail}:") || key.EndsWith($":{normalizedEmail}"))
            .ToList();

        Console.WriteLine($"[Cache] Clearing {keysToRemove.Count} cache entries for email: {email}");

        // Synchronize access to shared resources (_cacheKeys) for thread safety
        lock (_cacheKeys)
        {
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key); // Remove from cache
                _cacheKeys.Remove(key); // Remove from tracker
                Console.WriteLine($"[Cache] Removed cache key: {key}");
            }
        }

        // Log completion
        Console.WriteLine($"[Cache] Finished clearing cache for email: {email}");
    }

}
