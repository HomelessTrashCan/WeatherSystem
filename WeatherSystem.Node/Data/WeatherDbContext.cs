using Microsoft.EntityFrameworkCore;
using WeatherSystem.Node.Models;

namespace WeatherSystem.Node.Data
{
    public class WeatherDbContext : DbContext
    {
        public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options)
        {
        }

        public DbSet<WeatherData> WeatherMeasurements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WeatherData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.DayPhase).IsRequired();
                entity.Property(e => e.SimulatorId).IsRequired();
                entity.Property(e => e.NodeName).IsRequired();
                entity.Property(e => e.ReceivedAt).IsRequired();
                
                // Index für bessere Performance bei Abfragen
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.SimulatorId);
                entity.HasIndex(e => e.NodeName);
                entity.HasIndex(e => e.ReceivedAt);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}