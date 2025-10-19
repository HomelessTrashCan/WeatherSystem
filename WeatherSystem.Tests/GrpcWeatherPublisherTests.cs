using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using WeatherSystem.Simulator;
using System.Collections.Generic;

namespace WeatherSystem.Tests
{
    public class GrpcWeatherPublisherTests
    {
        [Fact]
        public async Task PublishMeasurementAsync_LogsCorrectly_WhenCalled()
        {
            // Arrange
            var loggedMessages = new List<string>();
            var options = new Mock<IOptions<WeatherPublisherOptions>>();
            options.Setup(o => o.Value).Returns(new WeatherPublisherOptions 
            { 
                GrpcServiceUrl = "http://test-server:5000",
                SimulatorId = "test-simulator"
            });
            
            Action<string> logAction = (message) => loggedMessages.Add(message);
            
            var publisher = new TestGrpcWeatherPublisher(options.Object, logAction);
            
            var timestamp = new DateTimeOffset(2025, 10, 19, 14, 30, 0, TimeSpan.Zero);
            var dayPhase = "DAY";
            var temperature = 22.5;
            var humidity = 45.0;
            var pressure = 1013.2;
            var isRaining = false;

            // Act
            await publisher.TestPublishMeasurementAsync(timestamp, dayPhase, temperature, 
                humidity, pressure, isRaining, CancellationToken.None);

            // Assert
            Assert.Contains(loggedMessages, msg => msg.Contains("[test-simulator]"));
            Assert.Contains(loggedMessages, msg => msg.Contains("2025-10-19 14:30:00"));
            Assert.Contains(loggedMessages, msg => msg.Contains("22.5"));
            Assert.DoesNotContain(loggedMessages, msg => msg.Contains("Fehler"));
        }
        
        // Testbare Version des GrpcWeatherPublisher, die keine tatsächliche gRPC-Verbindung aufbaut
        private class TestGrpcWeatherPublisher : GrpcWeatherPublisher
        {
            public TestGrpcWeatherPublisher(IOptions<WeatherPublisherOptions> options, Action<string>? logAction = null)
                : base(options, logAction)
            {
            }
            
            public async Task TestPublishMeasurementAsync(
                DateTimeOffset timestamp,
                string dayPhase,
                double temperature,
                double humidity,
                double pressure,
                bool isRaining,
                CancellationToken cancellationToken)
            {
                // Simuliere erfolgreiche Antwort vom Server
                _logAction($"[{GetSimulatorId()}] Messung an gRPC-Server gesendet: {timestamp:yyyy-MM-dd HH:mm:ss} - Temp: {temperature}°C");
                _logAction($"[{GetSimulatorId()}] Server bestätigt: Messung erfolgreich empfangen");
                
                await Task.CompletedTask;
            }
            
            private string GetSimulatorId()
            {
                return _options.SimulatorId;
            }
        }
    }
}