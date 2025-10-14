using WeatherSystem.WebApp.Models;

namespace WeatherSystem.WebApp.Services
{
    public interface IWeatherDataCollectionService
    {
        Task StartAsync();
        Task StopAsync();
        IEnumerable<WeatherNodeData> GetAllWeatherData();
        WeatherNodeData? GetWeatherDataByNode(string nodeName);
        event Action? OnDataUpdated;
    }
}