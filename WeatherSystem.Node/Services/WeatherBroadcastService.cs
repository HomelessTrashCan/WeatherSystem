using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using WeatherSystem.Grpc.Server.Protos;
using WeatherSystem.Node.Services;
using System.Text.RegularExpressions;

namespace WeatherSystem.Node.Services
{
    public class WeatherBroadcastService : WeatherBroadcast.WeatherBroadcastBase
    {
        private readonly ILogger<WeatherBroadcastService> _logger;
        private readonly DisplayOptions _displayOptions;
        private readonly NodeSettings _nodeSettings;
        private readonly IWeatherDataService _weatherDataService;
        // ConcurrentDictionary zum Thread-sicheren Speichern der Abonnenten
        private static readonly ConcurrentDictionary<string, IServerStreamWriter<WeatherMeasurement>> _subscribers = new();
        // Letzte empfangene Messung für neue Abonnenten
        private static WeatherMeasurement _lastMeasurement;


        //logger hinzufügen
        public WeatherBroadcastService(
            ILogger<WeatherBroadcastService> logger,
            IOptions<DisplayOptions> displayOptions,
            IOptions<NodeSettings> nodeSettings,
            IWeatherDataService weatherDataService)
        {
            _logger = logger;
            _displayOptions = displayOptions.Value;
            _nodeSettings = nodeSettings.Value;
            _weatherDataService = weatherDataService;
        }

        // Empfangen von Wetterdaten vom Simulator
        public override async Task<WeatherAcknowledgement> StreamWeatherData(
            IAsyncStreamReader<WeatherMeasurement> requestStream,
            ServerCallContext context)
        {
            try
            {
                var clientPeer = context.Peer;
                
                // SimulatorId aus den Headers extrahieren
                var simulatorId = "unknown-simulator";
                if (context.RequestHeaders.Any(h => h.Key == "simulator-id"))
                {
                    simulatorId = context.RequestHeaders.GetValue("simulator-id") ?? simulatorId;
                }
                else
                {
                    // Fallback: aus Peer extrahieren
                    simulatorId = ExtractSimulatorIdFromPeer(clientPeer) ?? simulatorId;
                }
                
                _logger.LogInformation("Neuer Datenstrom vom Wettersimulator empfangen von {Peer}, SimulatorId: {SimulatorId}", 
                    clientPeer, simulatorId);

                // Farbige Ausgabe für Header
                if (_displayOptions.UseColors)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                }
                Console.WriteLine("================================================");
                Console.WriteLine($"    WETTERDATEN VOM SIMULATOR [{simulatorId}]");
                Console.WriteLine($"    NODE: {_nodeSettings.NodeName}");
                Console.WriteLine("================================================");
                if (_displayOptions.UseColors)
                {
                    Console.ResetColor();
                }

                int measurementCount = 0;

                // Wetterdaten vom Simulator empfangen
                await foreach (var measurement in requestStream.ReadAllAsync())
                {
                    measurementCount++;
                    
                    // In SQLite Datenbank speichern
                    await _weatherDataService.SaveWeatherDataAsync(
                        measurement, 
                        simulatorId, 
                        _nodeSettings.NodeName, 
                        clientPeer);

                    // Schöne Konsolen-Ausgabe
                    DisplayWeatherMeasurement(measurement, _displayOptions, simulatorId);

                    // Strukturiertes Logging
                    _logger.LogDebug("Wettermessung #{Count} empfangen und gespeichert: " +
                        "SimulatorId={SimulatorId}, NodeName={NodeName}, " +
                        "Timestamp={Timestamp}, DayPhase={DayPhase}, " +
                        "Temperature={Temperature:F1}°C, Humidity={Humidity:F1}%, " +
                        "Pressure={Pressure:F1} hPa, Rain={IsRaining}",
                        measurementCount,
                        simulatorId,
                        _nodeSettings.NodeName,
                        measurement.Timestamp,
                        measurement.DayPhase,
                        measurement.Temperature,
                        measurement.Humidity,
                        measurement.Pressure,
                        measurement.IsRaining ? "Yes" : "No");

                    // Aktuelle Messung speichern
                    _lastMeasurement = measurement;

                    // An alle Abonnenten verteilen
                    foreach (var subscriber in _subscribers)
                    {
                        try
                        {
                            await subscriber.Value.WriteAsync(measurement);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Fehler beim Senden an Abonnent {SubscriberId}", subscriber.Key);
                            // Fehlerhafte Abonnenten entfernen
                            _subscribers.TryRemove(subscriber.Key, out _);
                        }
                    }
                }

                var successMessage = $"Stream erfolgreich beendet. {measurementCount} Messungen von {simulatorId} empfangen und in Node {_nodeSettings.NodeName} gespeichert.";
                _logger.LogInformation(successMessage);

                return new WeatherAcknowledgement { Success = true, Message = successMessage };
            }
            catch (Exception ex)
            {
                var errorMessage = $"Fehler beim Empfangen des Wetterstroms: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return new WeatherAcknowledgement { Success = false, Message = errorMessage };
            }
        }

        // Client für Wetterdaten registrieren
        public override async Task SubscribeToWeatherUpdates(
            SubscriptionRequest request,
            IServerStreamWriter<WeatherMeasurement> responseStream,
            ServerCallContext context)
        {
            var clientId = request.ClientId;
            if (string.IsNullOrEmpty(clientId))
            {
                clientId = Guid.NewGuid().ToString();
            }

            _logger.LogInformation("Neuer Client abonniert Wetterdaten: {ClientId} auf Node {NodeName}", 
                clientId, _nodeSettings.NodeName);

            // Client zur Liste der Abonnenten hinzufügen
            _subscribers[clientId] = responseStream;

            // Falls verfügbar, sofort die letzte Messung senden
            if (_lastMeasurement != null)
            {
                try
                {
                    await responseStream.WriteAsync(_lastMeasurement);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Client {ClientId} hat die Verbindung bereits getrennt", clientId);
                    // Client sofort entfernen, da keine Verbindung mehr besteht
                    _subscribers.TryRemove(clientId, out _);
                    return; // Methode beenden, da keine Verbindung mehr besteht
                }
            }

            try
            {
                // Warten bis der Client die Verbindung trennt
                await Task.Delay(TimeSpan.FromDays(1), context.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Normal bei Verbindungsabbruch
            }
            finally
            {
                // Client aus der Liste entfernen
                _subscribers.TryRemove(clientId, out _);
                _logger.LogInformation("Client-Abonnement beendet: {ClientId}", clientId);
            }
        }

        // SimulatorId aus dem Peer-String extrahieren
        private string? ExtractSimulatorIdFromPeer(string? peer)
        {
            if (string.IsNullOrEmpty(peer))
                return null;

            // Versuche verschiedene Muster zu finden
            // Beispiel: "ipv4:127.0.0.1:12345" -> versuche Port zu verwenden
            var match = Regex.Match(peer, @"ipv[46]:[\d\.:]+:(\d+)");
            if (match.Success)
            {
                return $"simulator-port-{match.Groups[1].Value}";
            }

            // Fallback: verwende einen Hash des Peer-Strings
            return $"simulator-{Math.Abs(peer.GetHashCode()) % 10000:D4}";
        }

        // Methode zum Anzeigen der Wetterdaten mit Formatierung
        private static void DisplayWeatherMeasurement(WeatherMeasurement measurement, DisplayOptions displayOptions, string simulatorId)
        {
            // Farben basierend auf Tageszeit setzen
            ConsoleColor timeColor = measurement.DayPhase == "DAY" ? ConsoleColor.Yellow : ConsoleColor.Blue;
            
            // Farbe für Temperatur basierend auf Wert
            ConsoleColor tempColor = ConsoleColor.White;
            if (measurement.Temperature > 25) tempColor = ConsoleColor.Red;
            else if (measurement.Temperature < 10) tempColor = ConsoleColor.Cyan;
            else tempColor = ConsoleColor.Green;

            // Simulator-ID anzeigen
            if (displayOptions.UseColors)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
            }
            Console.Write($"[{simulatorId}] ");

            // Oberer Teil der Anzeige
            if (displayOptions.ShowTimestamp)
            {
                if (displayOptions.UseColors)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                }
                Console.Write($"[{measurement.Timestamp}] ");
            }
            
            if (displayOptions.ShowDayPhase)
            {
                if (displayOptions.UseColors)
                {
                    Console.ForegroundColor = timeColor;
                }
                Console.Write($"[{measurement.DayPhase}]");
                if (displayOptions.UseColors)
                {
                    Console.ResetColor();
                }
            }
            Console.WriteLine();

            // Werte anzeigen
            Console.Write("   Temperatur: ");
            if (displayOptions.UseColors)
            {
                Console.ForegroundColor = tempColor;
            }
            Console.WriteLine($"{measurement.Temperature:F1}°C");
            if (displayOptions.UseColors)
            {
                Console.ResetColor();
            }

            Console.WriteLine($"   Luftfeuchtigkeit: {measurement.Humidity:F1}%");
            Console.WriteLine($"   Luftdruck: {measurement.Pressure:F1} hPa");
            
            // Regenstatus
            Console.Write("   Niederschlag: ");
            if (measurement.IsRaining)
            {
                if (displayOptions.UseColors)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                }
                Console.WriteLine("Ja");
            }
            else
            {
                Console.WriteLine("Nein");
            }
            if (displayOptions.UseColors)
            {
                Console.ResetColor();
            }
            
            Console.WriteLine("------------------------------------------------");
        }
    }
}