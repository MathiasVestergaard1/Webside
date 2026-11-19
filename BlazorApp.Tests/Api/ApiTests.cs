using System.Globalization;
using System.Net;
using System.Reflection;
using BlazorApp.Components.API;
using BlazorApp.Tests.Helpers;

namespace BlazorApp.Tests.Api;

public class ApiTests
{
    private const string StationNamesJson = """
        {
          "features": [
            { "properties": { "stationId": "06180", "name": "Københavns Lufthavn" } },
            { "properties": { "stationId": "06030", "name": "Flyvestation Aalborg" } }
          ]
        }
        """;

    private const string TemperaturesJson = """
        {
          "features": [
            { "geometry": { "coordinates": [12.65, 55.61] },
              "properties": { "stationId": "06180", "value": 4.2, "from": "2026-11-19T10:00:00Z" } },
            { "geometry": { "coordinates": [12.65, 55.61] },
              "properties": { "stationId": "06180", "value": 5.1, "from": "2026-11-19T11:00:00Z" } },
            { "geometry": { "coordinates": [9.85, 57.09] },
              "properties": { "stationId": "06030", "value": 2.0, "from": "2026-11-19T11:00:00Z" } },
            { "geometry": { "coordinates": [10.0, 56.0] },
              "properties": { "stationId": "06999", "value": -1.5, "from": "2026-11-19T11:00:00Z" } }
          ]
        }
        """;

    public ApiTests()
    {
        // Api caches station names in a static field; clear it so tests don't depend on each other.
        typeof(Components.API.Api)
            .GetField("_stationNames", BindingFlags.NonPublic | BindingFlags.Static)!
            .SetValue(null, null);
    }

    private static (Components.API.Api Api, FakeHttpMessageHandler Handler) CreateApi(
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new FakeHttpMessageHandler(respond);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://opendataapi.dmi.dk") };
        return (new Components.API.Api(client), handler);
    }

    private static HttpResponseMessage RouteDmi(HttpRequestMessage request, HttpStatusCode stationStatus = HttpStatusCode.OK)
    {
        var path = request.RequestUri!.AbsolutePath;
        if (path.EndsWith("/station/items"))
            return FakeHttpMessageHandler.Json(stationStatus == HttpStatusCode.OK ? StationNamesJson : "error", stationStatus);
        if (path.EndsWith("/stationValue/items"))
            return FakeHttpMessageHandler.Json(TemperaturesJson);
        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAsync_ReturnsBody_OnSuccess()
    {
        var (api, _) = CreateApi(_ => FakeHttpMessageHandler.Json("""{"ok":true}"""));

        var body = await api.GetAsync("anything");

        Assert.Equal("""{"ok":true}""", body);
    }

    [Fact]
    public async Task GetAsync_Throws_WithStatusAndBody_OnError()
    {
        var (api, _) = CreateApi(_ => FakeHttpMessageHandler.Json("boom", HttpStatusCode.InternalServerError));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => api.GetAsync("anything"));

        Assert.Contains("500", ex.Message);
        Assert.Contains("boom", ex.Message);
    }

    [Fact]
    public async Task GetDenmarkTemperaturesAsync_ReturnsNewestValuePerStation_WithNames()
    {
        var (api, _) = CreateApi(r => RouteDmi(r));

        var result = await api.GetDenmarkTemperaturesAsync(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(["06030", "06180", "06999"], result.Select(s => s.StationId));

        var cph = result.Single(s => s.StationId == "06180");
        Assert.Equal("Københavns Lufthavn", cph.Name);
        Assert.Equal(5.1, cph.TemperatureCelsius);
        Assert.Equal(55.61, cph.Latitude);
        Assert.Equal(12.65, cph.Longitude);

        // Unknown stations fall back to their id as the name.
        Assert.Equal("06999", result.Single(s => s.StationId == "06999").Name);
    }

    [Fact]
    public async Task GetDenmarkTemperaturesAsync_BuildsExpectedQuery()
    {
        var (api, handler) = CreateApi(r => RouteDmi(r));

        await api.GetDenmarkTemperaturesAsync(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero));

        var query = Uri.UnescapeDataString(handler.Requests
            .Single(r => r.RequestUri!.AbsolutePath.EndsWith("/stationValue/items"))
            .RequestUri!.Query);
        Assert.Contains("parameterId=mean_temp", query);
        Assert.Contains("timeResolution=hour", query);
        Assert.Contains("bbox=8,54.5,15.5,58", query);
        Assert.Contains("datetime=2026-11-19T09:00:00Z/2026-11-19T12:00:00Z", query);
    }

    [Fact]
    public async Task GetDenmarkTemperaturesAsync_StillWorks_WhenStationNamesFail()
    {
        var (api, _) = CreateApi(r => RouteDmi(r, HttpStatusCode.ServiceUnavailable));

        var result = await api.GetDenmarkTemperaturesAsync(DateTimeOffset.UtcNow);

        Assert.Equal(3, result.Count);
        Assert.All(result, s => Assert.Equal(s.StationId, s.Name));
    }

    [Fact]
    public async Task GetDenmarkTemperaturesAsync_StillWorks_WhenStationNamesAreInvalidJson()
    {
        var (api, _) = CreateApi(r => r.RequestUri!.AbsolutePath.EndsWith("/station/items")
            ? FakeHttpMessageHandler.Json("<html>Maintenance</html>")
            : RouteDmi(r));

        var result = await api.GetDenmarkTemperaturesAsync(DateTimeOffset.UtcNow);

        Assert.Equal(3, result.Count);
        Assert.All(result, s => Assert.Equal(s.StationId, s.Name));
    }

    [Fact]
    public async Task GetDenmarkTemperaturesAsync_UsesInvariantNumbers_UnderDanishCulture()
    {
        var (api, handler) = CreateApi(r => RouteDmi(r));
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("da-DK");
        try
        {
            await api.GetDenmarkTemperaturesAsync(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        // Danish formatting would turn 54.5 into "54,5" and break the bbox.
        Assert.All(handler.Requests, r => Assert.Contains("bbox=8,54.5,15.5,58", Uri.UnescapeDataString(r.RequestUri!.Query)));
    }

    [Fact]
    public async Task GetDenmarkTemperaturesAsync_RetriesStationNames_AfterFailure()
    {
        var namesAvailable = false;
        var (api, handler) = CreateApi(r => RouteDmi(r, namesAvailable ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable));

        await api.GetDenmarkTemperaturesAsync(DateTimeOffset.UtcNow);
        namesAvailable = true;
        var result = await api.GetDenmarkTemperaturesAsync(DateTimeOffset.UtcNow);

        Assert.Equal(2, handler.Requests.Count(r => r.RequestUri!.AbsolutePath.EndsWith("/station/items")));
        Assert.Equal("Københavns Lufthavn", result.Single(s => s.StationId == "06180").Name);
    }

    [Fact]
    public async Task GetDenmarkTemperaturesAsync_CachesStationNames()
    {
        var (api, handler) = CreateApi(r => RouteDmi(r));

        await api.GetDenmarkTemperaturesAsync(DateTimeOffset.UtcNow);
        await api.GetDenmarkTemperaturesAsync(DateTimeOffset.UtcNow);

        Assert.Single(handler.Requests, r => r.RequestUri!.AbsolutePath.EndsWith("/station/items"));
    }

    [Fact]
    public async Task GetDenmarkTemperaturesAsync_Throws_WhenTemperatureCallFails()
    {
        var (api, _) = CreateApi(_ => FakeHttpMessageHandler.Json("down", HttpStatusCode.BadGateway));

        await Assert.ThrowsAsync<HttpRequestException>(() => api.GetDenmarkTemperaturesAsync(DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task GetStationNamesAsync_SkipsBlankNames()
    {
        var (api, _) = CreateApi(_ => FakeHttpMessageHandler.Json("""
            { "features": [
                { "properties": { "stationId": "1", "name": "A" } },
                { "properties": { "stationId": "2", "name": " " } }
            ] }
            """));

        var names = await api.GetStationNamesAsync();

        Assert.Equal(new Dictionary<string, string> { ["1"] = "A" }, names);
    }
}
