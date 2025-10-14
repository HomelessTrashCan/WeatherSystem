using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using WeatherSystem.Grpc.Server.Protos;

namespace WeatherSystem.Grpc.Server.Services
{
    public class WeatherBroadcastService : Protos.WeatherBroadcast.WeatherBroadcastBase
    {
        private readonly ILogger<WeatherBroadcastService> _logger;
        // ConcurrentDictionary zum Thread-sicheren Speichern der Abonnenten
        private static readonly ConcurrentDictionary<string, IServerStreamWriter<WeatherMeasurement>> _subscribers = new();
        // Letzte empfangene Messung f�r neue Abonnenten
        private static WeatherMeasurement _lastMeasurement;

        public WeatherBroadcastService(ILogger<WeatherBroadcastService> logger)
        {
            _logger = logger;
        }

        // Empfangen von Wetterdaten vom Simulator
        public override async Task<WeatherAcknowledgement> StreamWeatherData(
            IAsyncStreamReader<WeatherMeasurement> requestStream,
            ServerCallContext context)
        {
            // Vorhandener Code...
        }

        // Client f�r Wetterdaten registrieren
        public override async Task SubscribeToWeatherUpdates(
            SubscriptionRequest request,
            IServerStreamWriter<WeatherMeasurement> responseStream,
            ServerCallContext context)
        {
            // Vorhandener Code...
        }

        // Neu: Health-Check-Endpunkt f�r Client-Verbindungspr�fungen
        public override Task<HealthCheckResponse> CheckHealth(HealthCheckRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Health-Check-Anfrage empfangen von Client: {ClientIp}", context.Peer);
            
            // Hier k�nnten Sie zus�tzliche Dienst-Gesundheitspr�fungen durchf�hren
            var isHealthy = true; 
            var message = isHealthy ? "Der Wetter-Broadcast-Service ist aktiv und betriebsbereit." : "Der Service hat Probleme.";

            return Task.FromResult(new HealthCheckResponse
            {
                Status = isHealthy,
                Message = message
            });
        }
    }
}