using System.Globalization;
using System.Text.Json;

namespace BlazorApp.Components.API
{
    public class Api(HttpClient client)
    {
        private readonly HttpClient _client = client;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        // Roughly the bounding box around Denmark (incl. islands, Bornholm), excluding Greenland/Faroe Islands stations.
        public const double DenmarkMinLongitude = 8;
        public const double DenmarkMinLatitude = 54.5;
        public const double DenmarkMaxLongitude = 15.5;
        public const double DenmarkMaxLatitude = 58;

        public async Task<string> GetAsync(string requestUri)
        {
            var response = await _client.GetAsync(requestUri);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"{(int)response.StatusCode} {response.ReasonPhrase} calling {response.RequestMessage?.RequestUri}: {body}");
            }

            return body;
        }

        public async Task<List<StationTemperature>> GetDenmarkTemperaturesAsync()
        {
            var to = DateTimeOffset.UtcNow;
            var from = to.AddHours(-3);

            var requestUri = "v2/climateData/collections/stationValue/items" +
                "?parameterId=mean_temp" +
                "&timeResolution=hour" +
                $"&bbox={FormatNumber(DenmarkMinLongitude)},{FormatNumber(DenmarkMinLatitude)},{FormatNumber(DenmarkMaxLongitude)},{FormatNumber(DenmarkMaxLatitude)}" +
                $"&datetime={FormatUtc(from)}/{FormatUtc(to)}" +
                "&sortorder=from,DESC" +
                "&limit=300";

            var json = await GetAsync(requestUri);
            var data = JsonSerializer.Deserialize<ClimateDataResponse>(json, JsonOptions)
                ?? new ClimateDataResponse();

            return data.Features
                .GroupBy(f => f.Properties.StationId)
                .Select(g => g.OrderByDescending(f => f.Properties.From).First())
                .Select(f => new StationTemperature(
                    f.Properties.StationId,
                    f.Geometry.Coordinates.Length > 1 ? f.Geometry.Coordinates[1] : 0,
                    f.Geometry.Coordinates.Length > 0 ? f.Geometry.Coordinates[0] : 0,
                    f.Properties.Value,
                    f.Properties.From))
                .OrderBy(s => s.StationId)
                .ToList();
        }

        private static string FormatUtc(DateTimeOffset value) =>
            value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        private static string FormatNumber(double value) =>
            value.ToString(CultureInfo.InvariantCulture);
    }

    public record StationTemperature(string StationId, double Latitude, double Longitude, double TemperatureCelsius, DateTimeOffset ObservedFrom);

    public class ClimateDataResponse
    {
        public List<ClimateDataFeature> Features { get; set; } = [];
    }

    public class ClimateDataFeature
    {
        public ClimateDataGeometry Geometry { get; set; } = new();
        public ClimateDataProperties Properties { get; set; } = new();
    }

    public class ClimateDataGeometry
    {
        public double[] Coordinates { get; set; } = [];
    }

    public class ClimateDataProperties
    {
        public string StationId { get; set; } = string.Empty;
        public double Value { get; set; }
        public DateTimeOffset From { get; set; }
    }
}
