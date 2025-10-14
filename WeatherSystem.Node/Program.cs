using Microsoft.EntityFrameworkCore;
using WeatherSystem.Node.Data;
using WeatherSystem.Node.Services;

namespace WeatherSystem.Node
{
    public class NodeSettings
    {
        public string NodeName { get; set; } = "DefaultNode";
        public string DatabasePath { get; set; } = "./Data";
    }

    public class DisplayOptions
    {
        public bool ShowTimestamp { get; set; } = true;
        public bool ShowDayPhase { get; set; } = true;
        public bool UseColors { get; set; } = true;
        public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Service Defaults hinzufügen
            builder.AddServiceDefaults();

            // Konfigurationen registrieren
            builder.Services.Configure<NodeSettings>(
                builder.Configuration.GetSection("NodeSettings"));
            builder.Services.Configure<DisplayOptions>(
                builder.Configuration.GetSection("Display"));

            // NodeSettings für weitere Verwendung
            var nodeSettings = new NodeSettings();
            builder.Configuration.GetSection("NodeSettings").Bind(nodeSettings);

            // Sicherstellen, dass das Data-Verzeichnis existiert
            if (!Directory.Exists(nodeSettings.DatabasePath))
            {
                Directory.CreateDirectory(nodeSettings.DatabasePath);
            }

            // SQLite Datenbank konfigurieren
            var databaseFileName = $"{nodeSettings.NodeName}.db";
            var connectionString = $"Data Source={Path.Combine(nodeSettings.DatabasePath, databaseFileName)}";
            
            builder.Services.AddDbContext<WeatherDbContext>(options =>
                options.UseSqlite(connectionString));

            // Services registrieren
            builder.Services.AddScoped<IWeatherDataService, WeatherDataService>();

            // gRPC Services hinzufügen
            builder.Services.AddGrpc();

            var app = builder.Build();

            // Datenbank erstellen/migrieren
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
                await dbContext.Database.EnsureCreatedAsync();
            }

            // Default Endpoints mappen
            app.MapDefaultEndpoints();

            // gRPC Service registrieren
            app.MapGrpcService<WeatherBroadcastService>();
            
            app.MapGet("/", () => "Weather Node - gRPC Server läuft. " +
                "Warte auf Wetterdaten vom Simulator...");

            // Get the actual URL from the application configuration
            var addresses = app.Urls;
            var serverUrl = addresses.FirstOrDefault() ?? "Unknown";

            Console.WriteLine("=== Weather Node (gRPC Server) gestartet ===");
            Console.WriteLine($"Node Name: {nodeSettings.NodeName}");
            Console.WriteLine($"gRPC Server läuft auf: {serverUrl}");
            Console.WriteLine($"SQLite Datenbank: {Path.Combine(nodeSettings.DatabasePath, databaseFileName)}");
            Console.WriteLine("Warte auf Wetterdaten vom Simulator...");
            Console.WriteLine("Drücken Sie Ctrl+C zum Beenden.");

            // Graceful shutdown handling
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                await app.RunAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nAnwendung wird beendet...");
            }
        }
    }
}