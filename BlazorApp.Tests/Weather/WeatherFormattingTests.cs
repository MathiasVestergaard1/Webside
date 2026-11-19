using static BlazorApp.Components.WeatherFormatting;

namespace BlazorApp.Tests.Weather;

public class WeatherFormattingTests
{
    [Theory]
    [InlineData(0, "rgb(0, 60, 255)")]
    [InlineData(25, "rgb(255, 60, 0)")]
    [InlineData(12.5, "rgb(127, 60, 127)")]
    [InlineData(-15, "rgb(0, 60, 255)")]
    [InlineData(40, "rgb(255, 60, 0)")]
    public void TemperatureColor_GoesFromBlueToRed_AndClamps(double celsius, string expected)
    {
        Assert.Equal(expected, TemperatureColor(celsius));
    }

    [Theory]
    [InlineData(4.6, 5)]
    [InlineData(4.4, 4)]
    [InlineData(-3.7, -4)]
    [InlineData(-0.2, 0)]
    public void RoundTemperature_RoundsToNearestDegree(double celsius, int expected)
    {
        Assert.Equal(expected, RoundTemperature(celsius));
    }

    [Fact]
    public void ToDanishTime_UsesWinterAndSummerTime()
    {
        Assert.Equal(new DateTime(2026, 1, 15, 13, 0, 0), ToDanishTime(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero)));
        Assert.Equal(new DateTime(2026, 7, 15, 14, 0, 0), ToDanishTime(new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void DanishHour_TruncatesToWholeHour()
    {
        Assert.Equal(new DateTime(2026, 11, 19, 13, 0, 0), DanishHour(new DateTimeOffset(2026, 11, 19, 12, 59, 59, TimeSpan.Zero)));
    }

    [Fact]
    public void FormatTime_UsesTwentyFourHourClock()
    {
        Assert.Equal("07:05", FormatTime(new DateTime(2026, 11, 19, 7, 5, 0)));
        Assert.Equal("19:00", FormatTime(new DateTime(2026, 11, 19, 19, 0, 0)));
    }

    [Fact]
    public void ResolveDanishTime_NormalWinterHour()
    {
        var (at, local) = ResolveDanishTime(new DateTime(2026, 11, 19, 12, 0, 0));

        Assert.Equal(new DateTimeOffset(2026, 11, 19, 11, 0, 0, TimeSpan.Zero), at);
        Assert.Equal(new DateTime(2026, 11, 19, 12, 0, 0), local);
    }

    [Fact]
    public void ResolveDanishTime_SpringGap_MovesToNextValidHour()
    {
        // 29 March 2026: clocks jump from 02:00 to 03:00, so 02:00 doesn't exist.
        var (at, local) = ResolveDanishTime(new DateTime(2026, 3, 29, 2, 0, 0));

        Assert.Equal(new DateTime(2026, 3, 29, 3, 0, 0), local);
        Assert.Equal(new DateTimeOffset(2026, 3, 29, 1, 0, 0, TimeSpan.Zero), at);
    }

    [Fact]
    public void ResolveDanishTime_AutumnOverlap_UsesEarlierOccurrence()
    {
        // 25 October 2026: 02:00-03:00 happens twice; the first time is still summer time (UTC+2).
        var (at, _) = ResolveDanishTime(new DateTime(2026, 10, 25, 2, 0, 0));

        Assert.Equal(new DateTimeOffset(2026, 10, 25, 0, 0, 0, TimeSpan.Zero), at);
    }
}
