using System.Globalization;
using System.Net;
using BlazorApp.Components.API;
using BlazorApp.Tests.Helpers;

namespace BlazorApp.Tests.Weather;

public class OpenMeteoServiceTests
{
    private const string ForecastJson = """
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

    private const string AarhusGeocodingJson = """
        { "results": [ { "name": "Aarhus", "latitude": 56.15, "longitude": 10.21,
                         "country": "Danmark", "admin1": "Midtjylland" } ] }
        """;

    private static readonly List<PostalCodeEntry> PostalCodes =
    [
        new() { PostalCode = "8000", Name = "Aarhus C", Latitude = 56.1567, Longitude = 10.2108, Region = "Midtjylland", Country = "Danmark" },
    ];

    private static (OpenMeteoService Service, FakeHttpMessageHandler Handler) CreateService(
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new FakeHttpMessageHandler(respond);
        return (new OpenMeteoService(new HttpClient(handler)), handler);
    }

    private static HttpResponseMessage Route(HttpRequestMessage request, string geocodingJson = AarhusGeocodingJson, string forecastJson = ForecastJson) =>
        request.RequestUri!.Host switch
        {
            "geocoding-api.open-meteo.com" => FakeHttpMessageHandler.Json(geocodingJson),
            "api.open-meteo.com" => FakeHttpMessageHandler.Json(forecastJson),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        };

    [Fact]
    public async Task Search_PostalCode_UsesLocalFile_AndSkipsGeocoding()
    {
        var (service, handler) = CreateService(r => Route(r));

        var result = await service.SearchAsync(" 8000 ", PostalCodes);

        Assert.Null(result.ErrorMessage);
        Assert.Equal("Aarhus C", result.Location!.Name);
        Assert.Equal("api.open-meteo.com", Assert.Single(handler.Requests).RequestUri!.Host);
    }

    [Fact]
    public async Task Search_UnknownPostalCode_ReturnsError_WithoutCallingApi()
    {
        var (service, handler) = CreateService(r => Route(r));

        var result = await service.SearchAsync("9999", PostalCodes);

        Assert.Equal("Postnummeret 9999 blev ikke fundet.", result.ErrorMessage);
        Assert.Null(result.Forecasts);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Search_City_GeocodesThenFetchesForecast()
    {
        var (service, handler) = CreateService(r => Route(r));

        var result = await service.SearchAsync("Aarhus", PostalCodes);

        Assert.Null(result.ErrorMessage);
        Assert.Equal("Aarhus", result.Location!.Name);
        Assert.Equal(6.3, result.Current!.Temperature);
        Assert.Equal(2, result.Forecasts!.Count);
        Assert.Equal(new DateOnly(2026, 11, 20), result.Forecasts[1].Date);
        Assert.Equal(61, result.Forecasts[1].WeatherCode);
        Assert.Equal(31.0, result.Forecasts[1].WindSpeed);

        var forecastQuery = handler.Requests.Single(r => r.RequestUri!.Host == "api.open-meteo.com").RequestUri!.Query;
        Assert.Contains("latitude=56.15", forecastQuery);
        Assert.Contains("longitude=10.21", forecastQuery);
    }

    [Fact]
    public async Task Search_EscapesCityName()
    {
        var (service, handler) = CreateService(r => Route(r));

        await service.SearchAsync("Ærø & Co", PostalCodes);

        var geocoding = handler.Requests.First(r => r.RequestUri!.Host == "geocoding-api.open-meteo.com");
        Assert.Contains("name=%C3%86r%C3%B8%20%26%20Co", geocoding.RequestUri!.Query);
    }

    [Fact]
    public async Task Search_UsesInvariantNumbers_UnderDanishCulture()
    {
        var (service, handler) = CreateService(r => Route(r));
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("da-DK");
        try
        {
            await service.SearchAsync("8000", PostalCodes);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        Assert.Contains("latitude=56.1567&longitude=10.2108", Assert.Single(handler.Requests).RequestUri!.Query);
    }

    [Theory]
    [InlineData("""{ "results": [] }""")]
    [InlineData("""{}""")]
    public async Task Search_CityNotFound_ReturnsError(string geocodingJson)
    {
        var (service, _) = CreateService(r => Route(r, geocodingJson: geocodingJson));

        var result = await service.SearchAsync("Atlantis", PostalCodes);

        Assert.Equal("Kunne ikke finde et sted der hedder \"Atlantis\".", result.ErrorMessage);
    }

    [Fact]
    public async Task Search_IncompleteForecast_ReturnsError()
    {
        // Two dates but only one max temperature.
        const string json = """
            { "daily": { "time": ["2026-11-19", "2026-11-20"], "weather_code": [2, 61],
                         "temperature_2m_max": [8.1], "temperature_2m_min": [3.2, 2.5],
                         "temperature_2m_mean": [5.6, 4.7], "wind_speed_10m_max": [20.5, 31.0] } }
            """;
        var (service, _) = CreateService(r => Route(r, forecastJson: json));

        var result = await service.SearchAsync("8000", PostalCodes);

        Assert.Equal("Vejrdata kunne ikke hentes.", result.ErrorMessage);
        Assert.Null(result.Location);
    }

    [Fact]
    public async Task Search_HttpError_ReturnsFriendlyError()
    {
        var (service, _) = CreateService(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var result = await service.SearchAsync("Aarhus", PostalCodes);

        Assert.Equal("Der var et problem med at kontakte vejrtjenesten. Prøv igen senere.", result.ErrorMessage);
    }

    [Fact]
    public async Task Search_InvalidJson_ReturnsFriendlyError()
    {
        var (service, _) = CreateService(r => Route(r, forecastJson: "not json"));

        var result = await service.SearchAsync("8000", PostalCodes);

        Assert.Equal("Vejrtjenesten returnerede ugyldige data.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("8000", true)]
    [InlineData("800", false)]
    [InlineData("80000", false)]
    [InlineData("80a0", false)]
    [InlineData("Odense", false)]
    public void IsPostalCode_RequiresFourDigits(string query, bool expected)
    {
        Assert.Equal(expected, OpenMeteoService.IsPostalCode(query));
    }

    [Fact]
    public void ToDayForecasts_ReturnsNull_ForEmptyOrMissingData()
    {
        Assert.Null(OpenMeteoService.ToDayForecasts(null));
        Assert.Null(OpenMeteoService.ToDayForecasts(new DailyData()));
    }
}
