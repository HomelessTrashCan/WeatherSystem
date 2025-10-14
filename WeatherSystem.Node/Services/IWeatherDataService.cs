using WeatherSystem.Grpc.Server.Protos;
using WeatherSystem.Node.Models;

namespace WeatherSystem.Node.Services
{
    public interface IWeatherDataService
    {
        Task SaveWeatherDataAsync(WeatherMeasurement measurement, string simulatorId, string nodeName, string? clientPeer = null);
        Task<IEnumerable<WeatherData>> GetWeatherDataAsync(string? simulatorId = null, DateTime? from = null, DateTime? to = null);
        Task<WeatherData?> GetLatestWeatherDataAsync(string? simulatorId = null);
        Task<int> GetMeasurementCountAsync(string? simulatorId = null);
    }
}