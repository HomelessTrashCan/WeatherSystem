using WeatherSystem.WebApp.Services;

namespace WeatherSystem.WebApp.Services
{
    public class WeatherDataHostedService : BackgroundService
    {
        private readonly IWeatherDataCollectionService _weatherDataService;
        private readonly ILogger<WeatherDataHostedService> _logger;

        public WeatherDataHostedService(
            IWeatherDataCollectionService weatherDataService,
            ILogger<WeatherDataHostedService> logger)
        {
            _weatherDataService = weatherDataService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Weather Data Collection Service starting...");

            try
            {
                await _weatherDataService.StartAsync();
                
                // Keep the service running until cancellation is requested
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Weather Data Collection Service stopping...");
            }
            finally
            {
                await _weatherDataService.StopAsync();
            }
        }
    }
}