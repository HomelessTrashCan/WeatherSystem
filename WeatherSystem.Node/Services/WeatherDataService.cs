using Microsoft.EntityFrameworkCore;
using WeatherSystem.Grpc.Server.Protos;
using WeatherSystem.Node.Data;
using WeatherSystem.Node.Models;

namespace WeatherSystem.Node.Services
{
    public class WeatherDataService : IWeatherDataService
    {
        private readonly WeatherDbContext _context;
        private readonly ILogger<WeatherDataService> _logger;

        public WeatherDataService(WeatherDbContext context, ILogger<WeatherDataService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SaveWeatherDataAsync(WeatherMeasurement measurement, string simulatorId, string nodeName, string? clientPeer = null)
        {
            try
            {
                var weatherData = new WeatherData
                {
                    Timestamp = measurement.Timestamp,
                    DayPhase = measurement.DayPhase,
                    Temperature = measurement.Temperature,
                    Humidity = measurement.Humidity,
                    Pressure = measurement.Pressure,
                    IsRaining = measurement.IsRaining,
                    SimulatorId = simulatorId,
                    NodeName = nodeName,
                    ClientPeer = clientPeer,
                    ReceivedAt = DateTime.UtcNow
                };

                _context.WeatherMeasurements.Add(weatherData);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Wetterdaten gespeichert: SimulatorId={SimulatorId}, NodeName={NodeName}, Timestamp={Timestamp}", 
                    simulatorId, nodeName, measurement.Timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Speichern der Wetterdaten: SimulatorId={SimulatorId}, NodeName={NodeName}", 
                    simulatorId, nodeName);
                throw;
            }
        }

        public async Task<IEnumerable<WeatherData>> GetWeatherDataAsync(string? simulatorId = null, DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var query = _context.WeatherMeasurements.AsQueryable();

                if (!string.IsNullOrEmpty(simulatorId))
                {
                    query = query.Where(w => w.SimulatorId == simulatorId);
                }

                if (from.HasValue)
                {
                    query = query.Where(w => w.ReceivedAt >= from.Value);
                }

                if (to.HasValue)
                {
                    query = query.Where(w => w.ReceivedAt <= to.Value);
                }

                return await query.OrderBy(w => w.ReceivedAt).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Abrufen der Wetterdaten: SimulatorId={SimulatorId}", simulatorId);
                throw;
            }
        }

        public async Task<WeatherData?> GetLatestWeatherDataAsync(string? simulatorId = null)
        {
            try
            {
                var query = _context.WeatherMeasurements.AsQueryable();

                if (!string.IsNullOrEmpty(simulatorId))
                {
                    query = query.Where(w => w.SimulatorId == simulatorId);
                }

                return await query.OrderByDescending(w => w.ReceivedAt).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Abrufen der neuesten Wetterdaten: SimulatorId={SimulatorId}", simulatorId);
                throw;
            }
        }

        public async Task<int> GetMeasurementCountAsync(string? simulatorId = null)
        {
            try
            {
                var query = _context.WeatherMeasurements.AsQueryable();

                if (!string.IsNullOrEmpty(simulatorId))
                {
                    query = query.Where(w => w.SimulatorId == simulatorId);
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Zählen der Wetterdaten: SimulatorId={SimulatorId}", simulatorId);
                throw;
            }
        }
    }
}