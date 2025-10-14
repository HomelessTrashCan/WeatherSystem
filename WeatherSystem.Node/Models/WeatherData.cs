using System.ComponentModel.DataAnnotations;

namespace WeatherSystem.Node.Models
{
    public class WeatherData
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Timestamp { get; set; } = string.Empty;
        
        [Required]
        public string DayPhase { get; set; } = string.Empty;
        
        public double Temperature { get; set; }
        
        public double Humidity { get; set; }
        
        public double Pressure { get; set; }
        
        public bool IsRaining { get; set; }
        
        [Required]
        public string SimulatorId { get; set; } = string.Empty;
        
        [Required]
        public string NodeName { get; set; } = string.Empty;
        
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        
        public string? ClientPeer { get; set; }
    }
}