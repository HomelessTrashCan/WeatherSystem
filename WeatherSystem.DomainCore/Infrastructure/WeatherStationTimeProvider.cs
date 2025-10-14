using System;

namespace WeatherSystem.DomainCore.Infrastructure
{
    /// <summary>
    /// Ein TimeProvider für die Wetterstation, der eine 24-Stunden-Simulation
    /// mit Tag/Nacht-Zyklus ermöglicht und alle 15 Minuten Messungen ausgibt.
    /// </summary>
    public class WeatherStationTimeProvider : TimeProvider
    {
        private readonly TimeZoneInfo _timeZone;
        private DateTimeOffset _currentSimulationTime;

        public WeatherStationTimeProvider(TimeZoneInfo? timeZone = null)
        {
            _timeZone = timeZone ?? TimeZoneInfo.Local;

            // Starte Simulation um 00:00 Uhr in lokaler Zeit
            var todayLocal = DateTime.Today; // Lokale Zeit (00:00)
            var todayUtc = TimeZoneInfo.ConvertTimeToUtc(todayLocal, _timeZone); // Konvertiere zur UTC-Zeit
            _currentSimulationTime = new DateTimeOffset(todayUtc);
        }

        /// <summary>
        /// Gibt die simulierte Zeit zurück
        /// </summary>
        public override DateTimeOffset GetUtcNow() => _currentSimulationTime;

        /// <summary>
        /// Gibt die lokale Zeit in der konfigurierten Zeitzone zurück
        /// </summary>
        public override TimeZoneInfo LocalTimeZone => _timeZone;

        /// <summary>
        /// Setzt die aktuelle simulierte Zeit
        /// </summary>
        public void SetCurrentTime(DateTimeOffset simulatedTime)
        {
            _currentSimulationTime = simulatedTime;
        }
    }
}