namespace EventManagement
{
    public class WeatherForecast
    {
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (TemperatureC * 9/5);

        public string? Summary { get; set; }
    }
}
