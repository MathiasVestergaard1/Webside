
namespace BlazorApp.Models;

public class CityWeatherCard
{
    public string CityName { get; set; } = "";
    public double TemperatureCelsius { get; set; }
    public string WeatherType { get; set; } = "";
    public string TimeOfDay { get; set; } = "";
    public string Theme { get; set; } = "default-city";
}
