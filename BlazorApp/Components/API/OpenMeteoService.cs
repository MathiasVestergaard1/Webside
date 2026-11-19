using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlazorApp.Components.API
{
    // Looks up a place (Danish postal code or city name) and fetches its five-day forecast from Open-Meteo.
    public class OpenMeteoService(HttpClient client)
    {
        private readonly HttpClient _client = client;

        public const string GeocodingUrl = "https://geocoding-api.open-meteo.com/v1/search";
        public const string ForecastUrl = "https://api.open-meteo.com/v1/forecast";

        // Never throws: failures are returned as a Danish error message for the page to show.
        public async Task<WeatherSearchResult> SearchAsync(string query, IReadOnlyList<PostalCodeEntry> postalCodes)
        {
            query = query.Trim();

            try
            {
                GeocodingResult? place;

                // Four-digit numeric searches use the local postal-code file.
                if (IsPostalCode(query))
                {
                    place = FindPostalCode(query, postalCodes);
                    if (place is null)
                    {
                        return WeatherSearchResult.Error($"Postnummeret {query} blev ikke fundet.");
                    }
                }
                else
                {
                    place = await FindCityAsync(query);
                    if (place is null)
                    {
                        return WeatherSearchResult.Error($"Kunne ikke finde et sted der hedder \"{query}\".");
                    }
                }

                var forecast = await GetForecastAsync(place.Latitude, place.Longitude);
                var days = ToDayForecasts(forecast?.Daily);
                if (days is null)
                {
                    return WeatherSearchResult.Error("Vejrdata kunne ikke hentes.");
                }

                return new WeatherSearchResult(place, forecast?.Current, days, null);
            }
            catch (HttpRequestException)
            {
                return WeatherSearchResult.Error("Der var et problem med at kontakte vejrtjenesten. Prøv igen senere.");
            }
            catch (JsonException)
            {
                return WeatherSearchResult.Error("Vejrtjenesten returnerede ugyldige data.");
            }
            catch (Exception)
            {
                return WeatherSearchResult.Error("Der opstod en uventet fejl under søgningen.");
            }
        }

        public static bool IsPostalCode(string query) =>
            query.Length == 4 && query.All(char.IsAsciiDigit);

        public static GeocodingResult? FindPostalCode(string postalCode, IReadOnlyList<PostalCodeEntry> postalCodes)
        {
            var entry = postalCodes.FirstOrDefault(p => p.PostalCode == postalCode);

            return entry is null
                ? null
                : new GeocodingResult
                {
                    Name = entry.Name,
                    Latitude = entry.Latitude,
                    Longitude = entry.Longitude,
                    Region = entry.Region,
                    Country = entry.Country
                };
        }

        public async Task<GeocodingResult?> FindCityAsync(string name)
        {
            var url = $"{GeocodingUrl}?name={Uri.EscapeDataString(name)}&count=1&language=da";
            var geo = await _client.GetFromJsonAsync<GeocodingResponse>(url);
            return geo?.Results?.FirstOrDefault();
        }

        public Task<ForecastResponse?> GetForecastAsync(double latitude, double longitude)
        {
            var url = ForecastUrl +
                $"?latitude={latitude.ToString(CultureInfo.InvariantCulture)}" +
                $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
                "&current=temperature_2m,weather_code" +
                "&daily=weather_code,temperature_2m_max,temperature_2m_min,temperature_2m_mean,wind_speed_10m_max" +
                "&wind_speed_unit=ms&timezone=auto&forecast_days=5";

            return _client.GetFromJsonAsync<ForecastResponse>(url);
        }

        // Turns Open-Meteo's parallel arrays into one row per day.
        // Returns null if there are no days or any array is shorter than the list of dates.
        public static List<DayForecast>? ToDayForecasts(DailyData? daily)
        {
            if (daily is null ||
                daily.Time.Count == 0 ||
                daily.TemperatureMean.Count < daily.Time.Count ||
                daily.TemperatureMax.Count < daily.Time.Count ||
                daily.TemperatureMin.Count < daily.Time.Count ||
                daily.WindSpeedMax.Count < daily.Time.Count ||
                daily.WeatherCode.Count < daily.Time.Count)
            {
                return null;
            }

            return Enumerable.Range(0, daily.Time.Count)
                .Select(i => new DayForecast
                {
                    Date = DateOnly.Parse(daily.Time[i], CultureInfo.InvariantCulture),
                    TempMean = daily.TemperatureMean[i],
                    TempMax = daily.TemperatureMax[i],
                    TempMin = daily.TemperatureMin[i],
                    WindSpeed = daily.WindSpeedMax[i],
                    WeatherCode = daily.WeatherCode[i]
                })
                .ToList();
        }
    }

    public record WeatherSearchResult(
        GeocodingResult? Location,
        CurrentWeatherData? Current,
        List<DayForecast>? Forecasts,
        string? ErrorMessage)
    {
        public static WeatherSearchResult Error(string message) => new(null, null, null, message);
    }
}
