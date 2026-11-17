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

    [JsonPropertyName("current")]
    public CurrentWeatherData? Current { get; set; }

    [JsonPropertyName("daily")]
    public DailyData? Daily { get; set; }
}

// ---------- Current Weather API ----------

public class CurrentWeatherData
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = "";

    [JsonPropertyName("temperature_2m")]
    public double Temperature { get; set; }

    [JsonPropertyName("weather_code")]
    public int WeatherCode { get; set; }
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

// ---------- View model brugt af siden ----------

public class DayForecast
{
    public DateOnly Date { get; set; }
    public double TempMean { get; set; }
    public double TempMax { get; set; }
    public double TempMin { get; set; }
    public double WindSpeed { get; set; }
    public int WeatherCode { get; set; }

    public string Condition => WeatherCodes.ToEnglish(WeatherCode);
}

public static class WeatherCodes
{
    // Translate weather codes into English descriptions with emojis.
    public static string ToEnglish(int code) => code switch
    {
        0 => "Clear sky ☀️",
        1 => "Mainly clear 🌤️",
        2 => "Partly cloudy ⛅",
        3 => "Overcast ☁️",
        45 or 48 => "Fog 🌫️",
        51 or 53 or 55 => "Drizzle 🌦️",
        56 or 57 => "Freezing drizzle 🧊",
        61 => "Light rain 🌦️",
        63 => "Rain 🌧️",
        65 => "Heavy rain 🌧️",
        66 or 67 => "Freezing rain 🧊",
        71 => "Light snowfall 🌨️",
        73 => "Snowfall ❄️",
        75 => "Heavy snowfall ❄️",
        77 => "Snow grains ❄️",
        80 or 81 or 82 => "Rain showers 🌦️",
        85 or 86 => "Snow showers 🌨️",
        95 => "Thunderstorm ⛈️",
        96 or 99 => "Thunderstorm with hail ⛈️",
        _ => "Unknown ❓"
    };
}

// Class til postnumrer
public class PostalCodeEntry
{
    public string PostalCode { get; set; } = "";
    public string Name { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }
}
