using System.Text.Json.Serialization;

// ---------- Geocoding API ----------

public class GeocodingResponse
{
    [JsonPropertyName("results")]
    public List<GeocodingResult>? Results { get; set; }
}

public class GeocodingResult
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("admin1")]
    public string? Region { get; set; }
}

// ---------- Forecast API ----------

public class ForecastResponse
{
    [JsonPropertyName("daily")]
    public DailyData? Daily { get; set; }
}

public class DailyData
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = new();

    [JsonPropertyName("weather_code")]
    public List<int> WeatherCode { get; set; } = new();

    [JsonPropertyName("temperature_2m_max")]
    public List<double> TemperatureMax { get; set; } = new();

    [JsonPropertyName("temperature_2m_min")]
    public List<double> TemperatureMin { get; set; } = new();

    [JsonPropertyName("temperature_2m_mean")]
    public List<double> TemperatureMean { get; set; } = new();

    [JsonPropertyName("wind_speed_10m_max")]
    public List<double> WindSpeedMax { get; set; } = new();
}

// ---------- View model used by the page ----------

public class DayForecast
{
    public DateOnly Date { get; set; }
    public double TempMean { get; set; }
    public double TempMax { get; set; }
    public double TempMin { get; set; }
    public double WindSpeed { get; set; }
    public int WeatherCode { get; set; }

    public string Condition => WeatherCodes.ToDanish(WeatherCode);
}

public static class WeatherCodes
{
    // WMO weather interpretation codes -> Danish text
    public static string ToDanish(int code) => code switch
    {
        0 => "Klar himmel",
        1 => "Overvejende klart",
        2 => "Delvist skyet",
        3 => "Overskyet",
        45 or 48 => "Tåge",
        51 or 53 or 55 => "Støvregn",
        56 or 57 => "Isslag (støvregn)",
        61 => "Let regn",
        63 => "Regn",
        65 => "Kraftig regn",
        66 or 67 => "Isslag (regn)",
        71 => "Let snefald",
        73 => "Snefald",
        75 => "Kraftigt snefald",
        77 => "Snekorn",
        80 or 81 or 82 => "Regnbyger",
        85 or 86 => "Snebyger",
        95 => "Tordenvejr",
        96 or 99 => "Tordenvejr med hagl",
        _ => "Ukendt"
    };
}
