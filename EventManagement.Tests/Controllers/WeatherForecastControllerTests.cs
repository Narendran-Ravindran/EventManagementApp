using EventManagement.Controllers;
using static EventManagement.WeatherForecast;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq;
using Xunit;
using static EventManagement.Controllers.WeatherForecastController;
namespace EventManagement.Tests
{
    public class WeatherForecastControllerTests
    {
        private readonly WeatherForecastController _controller;
        private readonly Mock<ILogger<WeatherForecastController>> _mockLogger;

        public WeatherForecastControllerTests()
        {
            // Create a mock logger
            _mockLogger = new Mock<ILogger<WeatherForecastController>>();
            _controller = new WeatherForecastController(_mockLogger.Object); // Pass the mock logger to the controller
        }

        [Fact]
        public void Get_ShouldReturnWeatherForecasts()
        {
            // Act
            var result = _controller.Get();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count());
            Assert.All(result, forecast =>
            {
                Assert.InRange(forecast.TemperatureC, -20, 55);
                Assert.NotNull(forecast.Summary);
                Assert.NotEmpty(forecast.Summary);
            });
        }

        [Fact]
        public void TemperatureF_ShouldReturnCorrectFahrenheitValue()
        {
            // Arrange
            //var forecastC0 = new WeatherForecast { TemperatureC = 0 }; // Freezing point
            var forecastC100 = new WeatherForecast { TemperatureC = 100 }; // Boiling point
            //var forecastC20 = new WeatherForecast { TemperatureC = 20 }; // A sample temperature

            // Act
            //var temperatureF0 = forecastC0.TemperatureF;
            var temperatureF100 = forecastC100.TemperatureF;
           // var temperatureF20 = forecastC20.TemperatureF;

            // Assert
            //Assert.Equal(32, temperatureF0); // 0°C = 32°F
            Assert.Equal(212, temperatureF100); // 100°C = 212°F
            //Assert.Equal(68, temperatureF20); // 20°C = 68°F
        }
    }
}

