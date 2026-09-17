using System.Globalization;
using System.Text.Json;

namespace BlazorApp.Components.API
{
    public static class DenmarkBoundary
    {
        private static List<string>? _cachedPaths;
        private static readonly Lock CacheLock = new();

        public static List<string> GetSvgPaths(string geoJsonFilePath)
        {
            if (_cachedPaths is not null)
            {
                return _cachedPaths;
            }

            lock (CacheLock)
            {
                _cachedPaths ??= BuildSvgPaths(geoJsonFilePath);
                return _cachedPaths;
            }
        }

        private static List<string> BuildSvgPaths(string geoJsonFilePath)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(geoJsonFilePath));
            var geometry = document.RootElement.GetProperty("features")[0].GetProperty("geometry");

            var paths = new List<string>();
            foreach (var polygon in geometry.GetProperty("coordinates").EnumerateArray())
            {
                foreach (var ring in polygon.EnumerateArray())
                {
                    var points = ring.EnumerateArray().Select(coordinate =>
                    {
                        var lon = coordinate[0].GetDouble();
                        var lat = coordinate[1].GetDouble();
                        var x = (lon - Api.DenmarkMinLongitude) / (Api.DenmarkMaxLongitude - Api.DenmarkMinLongitude) * 100;
                        var y = (Api.DenmarkMaxLatitude - lat) / (Api.DenmarkMaxLatitude - Api.DenmarkMinLatitude) * 100;
                        return string.Create(CultureInfo.InvariantCulture, $"{x:F2} {y:F2}");
                    });

                    paths.Add($"M{string.Join('L', points)}Z");
                }
            }

            return paths;
        }
    }
}
