using WeatherSystem.DomainCore.BusinessLogic;
using WeatherSystem.DomainCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WeatherSystem.Simulator;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// ServiceDefaults aktivieren

// Logging vollständig deaktivieren - keine Provider registrieren
builder.Logging.ClearProviders();

// Publisher konfigurieren
builder.Services.Configure<WeatherPublisherOptions>(
    builder.Configuration.GetSection("WeatherPublisher"));

// Simulation konfigurieren
builder.Services.Configure<SimulationOptions>(
    builder.Configuration.GetSection("Simulation"));

// GrpcWeatherPublisher als Service registrieren
builder.Services.AddSingleton<GrpcWeatherPublisher>();

// Dienst registrieren
builder.Services.AddHostedService<WeatherSimulatorHostedService>();

var app = builder.Build();
await app.RunAsync();

// Konfigurationsklasse für den Publisher
public class WeatherPublisherOptions
{
    public string GrpcServiceUrl { get; set; } = string.Empty;
    public string SimulatorId { get; set; } = string.Empty;
}

// Konfigurationsklasse für die Simulation
public class SimulationOptions
{
    public int MeasurementIntervalMinutes { get; set; } = 15;
    public int SimulationSpeedFactor { get; set; } = 10;
}

// Implementierung des HostedService
public class WeatherSimulatorHostedService : BackgroundService
{
    private readonly MeasurementDataWriter _measurementWriter;
    private readonly GrpcWeatherPublisher _publisher;
    private readonly SimulationOptions _simulationOptions;

    public WeatherSimulatorHostedService(
        GrpcWeatherPublisher publisher,
        IOptions<SimulationOptions> simulationOptions)
    {
        _measurementWriter = new MeasurementDataWriter();
        _publisher = publisher;
        _simulationOptions = simulationOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // TimeProvider für die Wetterstation instanziieren
        var timeProvider = new WeatherStationTimeProvider();
        
        // Wetterstation-Service mit dem TimeProvider und konfigurierten Intervall initialisieren
        var weatherService = new WeatherMeasurementService(
            timeProvider,
            measurementIntervalMinutes: _simulationOptions.MeasurementIntervalMinutes,
            logAction: msg => ProcessMeasurement(msg, timeProvider, stoppingToken)
        );

        // Service ausführen
        await weatherService.RunAsync(stoppingToken);
    }

    private void ProcessMeasurement(string message, WeatherStationTimeProvider timeProvider, CancellationToken token)
    {
        // Messung in der Konsole ausgeben
        _measurementWriter.WriteMeasurement(message);
        
        // Wenn es eine gültige Messung ist, an gRPC-Server senden
        if (IsMeasurementMessage(message))
        {
            try
            {
                // Daten aus der Nachricht extrahieren
                var timestamp = timeProvider.GetUtcNow();
                var dayPhase = message.Contains("DAY") ? "DAY" : "NIGHT";
                
                // Werte extrahieren mit RegEx
                double temp = ExtractValue(message, "temp=", "°C");
                double humidity = ExtractValue(message, "humidity=", "%");
                if (double.IsNaN(humidity)) humidity = 0; // Standardwert falls keine Feuchtigkeit
                
                double pressure = ExtractValue(message, "pressure=", " hPa");
                bool isRaining = message.Contains("rain=yes") || message.Contains("rain=true");

                // Asynchron an gRPC-Server senden (nicht auf Ergebnis warten)
                _ = _publisher.PublishMeasurementAsync(
                    timestamp, dayPhase, temp, humidity, pressure, isRaining, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Verarbeiten der Messung: {ex.Message}");
            }
        }
    }
    
    private bool IsMeasurementMessage(string message)
    {
        return message.Contains("[") && 
               message.Contains(":") && 
               (message.Contains("DAY") || message.Contains("NIGHT")) &&
               !message.Contains("extra temp") && // Haupt-Messwerte, keine Extra-Temp
               message.Contains("rain=") &&
               message.Contains("pressure=") &&
               message.Contains("temp=");
    }
    
    private double ExtractValue(string message, string prefix, string suffix)
    {
        try
        {
            var pattern = $"{Regex.Escape(prefix)}([0-9.]+)";
            var match = Regex.Match(message, pattern);
            if (match.Success && match.Groups.Count > 1)
            {
                if (double.TryParse(match.Groups[1].Value, out double value))
                {
                    return value;
                }
            }
            return double.NaN;
        }
        catch
        {
            return double.NaN;
        }
    }
}

// Hilfsklasse zur strengen Filterung - nur Messwerte anzeigen
public class MeasurementDataWriter
{
    public void WriteMeasurement(string message)
    {
        // Nur tatsächliche Messwerte anzeigen, keine Startmeldungen etc.
        // Wir suchen nach dem typischen Format einer Messwertausgabe
        if (message.Contains("[") && 
            message.Contains(":") && 
            (message.Contains("DAY") || message.Contains("NIGHT")) &&
            message.Contains("rain=") &&
            message.Contains("pressure=") &&
            message.Contains("temp="))
        {
            Console.WriteLine(message);
        }
        // Spezieller Fall für die Extra-Temperaturmessungen
        else if (message.Contains("[") && 
                 message.Contains(":") &&
                 (message.Contains("DAY") || message.Contains("NIGHT")) &&
                 message.Contains("extra temp") && 
                 message.Contains("°C"))
        {
            Console.WriteLine(message);
        }
    }
}