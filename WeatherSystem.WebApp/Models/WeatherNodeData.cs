namespace WeatherSystem.WebApp.Models
{
    public class WeatherNodeData
    {
        public string NodeName { get; set; } = string.Empty;
        public string SimulatorId { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string DayPhase { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }
        public bool IsRaining { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public bool IsConnected { get; set; }
        public string ConnectionStatus { get; set; } = "Disconnected";
    }
}