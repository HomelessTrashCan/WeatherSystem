using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using WeatherSystem.Grpc.Server.Protos;

namespace WeatherSystem.Simulator
{
    public class GrpcWeatherPublisher
    {
        private readonly WeatherPublisherOptions _options;
        private readonly Action<string> _logAction;

        public GrpcWeatherPublisher(IOptions<WeatherPublisherOptions> options, Action<string>? logAction = null)
        {
            _options = options.Value;
            _logAction = logAction ?? Console.WriteLine;
        }

        public async Task PublishMeasurementAsync(
            DateTimeOffset timestamp,
            string dayPhase,
            double temperature, 
            double humidity, 
            double pressure, 
            bool isRaining,
            CancellationToken cancellationToken)
        {
            try
            {
                var simulatorId = !string.IsNullOrEmpty(_options.SimulatorId) 
                    ? _options.SimulatorId 
                    : Environment.MachineName + "-" + Environment.ProcessId;

                using var channel = GrpcChannel.ForAddress(_options.GrpcServiceUrl);
                var client = new WeatherBroadcast.WeatherBroadcastClient(channel);

                // Add custom headers to identify the simulator
                var headers = new Metadata
                {
                    { "simulator-id", simulatorId }
                };

                using var call = client.StreamWeatherData(headers: headers, cancellationToken: cancellationToken);
                
                var measurement = new WeatherMeasurement
                {
                    Timestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    DayPhase = dayPhase,
                    Temperature = temperature,
                    Humidity = humidity,
                    Pressure = pressure,
                    IsRaining = isRaining
                };

                await call.RequestStream.WriteAsync(measurement);
                _logAction($"[{simulatorId}] Messung an gRPC-Server gesendet: {measurement.Timestamp} - Temp: {measurement.Temperature}°C");

                await call.RequestStream.CompleteAsync();
                var response = await call;
                
                if (response.Success)
                {
                    _logAction($"[{simulatorId}] Server bestätigt: {response.Message}");
                }
                else
                {
                    _logAction($"[{simulatorId}] Fehler vom Server: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                _logAction($"Fehler bei der gRPC-Übertragung: {ex.Message}");
            }
        }
    }
}