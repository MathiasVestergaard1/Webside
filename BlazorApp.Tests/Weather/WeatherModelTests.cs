using System.Text.Json;

namespace BlazorApp.Tests.Weather;

public class WeatherModelTests
{
    [Theory]
    [InlineData(0, "Klar himmel ☀️")]
    [InlineData(3, "Overskyet ☁️")]
    [InlineData(45, "Tåge 🌫️")]
    [InlineData(48, "Tåge 🌫️")]
    [InlineData(63, "Regn 🌧️")]
    [InlineData(99, "Tordenvejr med hagl ⛈️")]
    [InlineData(-1, "Ukendt ❓")]
    [InlineData(1234, "Ukendt ❓")]
    public void WeatherCodes_ToDanish_MapsCodes(int code, string expected)
    {
        Assert.Equal(expected, WeatherCodes.ToDanish(code));
    }

    [Fact]
    public void DayForecast_Condition_UsesWeatherCode()
    {
        var forecast = new DayForecast { WeatherCode = 73 };

        Assert.Equal("Snefald ❄️", forecast.Condition);
    }

    [Fact]
    public void GeocodingResponse_DeserializesOpenMeteoJson()
    {
        const string json = """
            { "results": [ { "name": "Aarhus", "latitude": 56.15, "longitude": 10.21,
                             "country": "Danmark", "admin1": "Midtjylland" } ] }
            """;

        var result = JsonSerializer.Deserialize<GeocodingResponse>(json)!.Results!.Single();

        Assert.Equal("Aarhus", result.Name);
        Assert.Equal(56.15, result.Latitude);
        Assert.Equal(10.21, result.Longitude);
        Assert.Equal("Midtjylland", result.Region);
    }

    [Fact]
    public void ForecastResponse_DeserializesOpenMeteoJson()
    {
        const string json = """
            {
              "current": { "time": "2026-11-19T12:00", "temperature_2m": 6.3, "weather_code": 2 },
              "daily": {
                "time": ["2026-11-19", "2026-11-20"],
                "weather_code": [2, 61],
                "temperature_2m_max": [8.1, 7.0],
                "temperature_2m_min": [3.2, 2.5],
                "temperature_2m_mean": [5.6, 4.7],
                "wind_speed_10m_max": [20.5, 31.0]
              }
            }
            """;

        var response = JsonSerializer.Deserialize<ForecastResponse>(json)!;

        Assert.Equal(6.3, response.Current!.Temperature);
        Assert.Equal(2, response.Current.WeatherCode);
        Assert.Equal(["2026-11-19", "2026-11-20"], response.Daily!.Time);
        Assert.Equal([8.1, 7.0], response.Daily.TemperatureMax);
        Assert.Equal([20.5, 31.0], response.Daily.WindSpeedMax);
    }

    [Fact]
    public void PostalCodesFile_IsValidAndNotEmpty()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "postnumre.json");
        var entries = JsonSerializer.Deserialize<List<PostalCodeEntry>>(
            File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        Assert.NotEmpty(entries);
        Assert.All(entries, e => Assert.False(string.IsNullOrWhiteSpace(e.PostalCode)));
    }
}
