using System.Globalization;

namespace BlazorApp.Components;

// Formatting and Danish time-zone helpers used by the weather page.
public static class WeatherFormatting
{
    public static readonly TimeZoneInfo DanishTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    // Blue at 0°C, red at 25°C.
    public static string TemperatureColor(double celsius)
    {
        var t = Math.Clamp(celsius / 25.0, 0.0, 1.0);
        var red = (int)(t * 255);
        var blue = (int)((1 - t) * 255);

        return $"rgb({red}, 60, {blue})";
    }

    public static DateTime CurrentDanishHour() => DanishHour(DateTimeOffset.UtcNow);

    // The Danish local time of `at`, truncated to the whole hour.
    public static DateTime DanishHour(DateTimeOffset at)
    {
        var local = ToDanishTime(at);
        return new DateTime(local.Year, local.Month, local.Day, local.Hour, 0, 0);
    }

    public static DateTime ToDanishTime(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, DanishTimeZone).DateTime;

    // Converts a Danish local time to an exact instant, handling daylight-saving transitions.
    // Returns the (possibly adjusted) local time as well, so the UI can show what was used.
    public static (DateTimeOffset At, DateTime Local) ResolveDanishTime(DateTime localTime)
    {
        var local = DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified);

        // In the spring the clock skips 02:00-03:00, so move forward past the gap.
        if (DanishTimeZone.IsInvalidTime(local))
        {
            local = local.AddHours(1);
        }

        // During the autumn clock change, use the earlier occurrence of the selected local time.
        if (DanishTimeZone.IsAmbiguousTime(local))
        {
            var offset = DanishTimeZone.GetAmbiguousTimeOffsets(local).Max();
            return (new DateTimeOffset(local, offset), local);
        }

        return (new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, DanishTimeZone)), local);
    }

    public static int RoundTemperature(double celsius) => (int)Math.Round(celsius);

    public static string FormatTime(DateTime value) =>
        value.ToString("HH:mm", CultureInfo.InvariantCulture);
}
