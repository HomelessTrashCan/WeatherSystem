using System;
using System.Threading;
using System.Threading.Tasks;
using WeatherSystem.DomainCore.BusinessLogic;
using WeatherSystem.DomainCore.Infrastructure;

namespace WeatherSystem.Simulator
{
    /// <summary>
    /// Service responsible for collecting and reporting weather measurements
    /// </summary>
    public class WeatherMeasurementService
    {
        private readonly MeasurementRules _rules;
        private readonly LidarSensor _lidar;
        private readonly HumiditySensor _humidity;
        private readonly TemperatureSensor _temperature;
        private readonly Random _rnd;
        private readonly TimeProvider _timeProvider;
        private readonly int _measurementIntervalMs;
        private readonly Action<string> _logAction;
      

        // Einstellungen für die 24-Stunden-Simulation
        private readonly int _simulationSpeedFactor = 10; // 1 Simulationsminute = 1 Sekunde Echtzeit
        private readonly TimeSpan _simulationDuration = TimeSpan.FromHours(24);
        private readonly TimeSpan _measurementInterval = TimeSpan.FromMinutes(15);

        public WeatherMeasurementService(
            TimeProvider timeProvider,
            int measurementIntervalMinutes = 15,
            Action<string>? logAction = null
           )
        {
            _timeProvider = timeProvider;
            _measurementIntervalMs = measurementIntervalMinutes * 60 * 1000;
            _logAction = logAction ?? Console.WriteLine;
           
            
            // Initialize sensors
            _rules = new MeasurementRules();
            _lidar = new LidarSensor();
            _humidity = new HumiditySensor();
            _temperature = new TemperatureSensor();
            _rnd = new Random();
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (_timeProvider is WeatherStationTimeProvider simulationTimeProvider)
            {
                await Run24HourSimulationAsync(simulationTimeProvider, cancellationToken);
            }
            else
            {
                _logAction("Error: WeatherStationTimeProvider required for simulation");
            }
        }

        private async Task Run24HourSimulationAsync(WeatherStationTimeProvider timeProvider, CancellationToken cancellationToken)
        {
            _logAction("Starting 24-hour weather simulation with measurements every 15 minutes...");
            _logAction($"Simulation speed: {_simulationSpeedFactor}x (1 simulated minute = {1.0/_simulationSpeedFactor:F1} seconds real time)");

            // Setze Start auf 00:00 Uhr lokale Zeit für einen vollen Tag
            var simulationStart = new DateTimeOffset(
                DateTime.Today, // Lokale Zeit 00:00
                timeProvider.LocalTimeZone.GetUtcOffset(DateTime.Today)); // Offset der konfigurierten Zeitzone
    
            // Ende der Simulation nach 24 Stunden
            var simulationEnd = simulationStart.Add(_simulationDuration);
            
            // Aktuelle simulierte Zeit
            var currentTime = simulationStart;
            
            while (!cancellationToken.IsCancellationRequested)
            {
                // Aktualisiere die simulierte Zeit im TimeProvider
                timeProvider.SetCurrentTime(currentTime);
                
                // Nimm eine Messung vor
                await TakeMeasurementWithDayNightCycleAsync(currentTime, cancellationToken);
                
                // Zeit für die nächste Messung
                currentTime = currentTime.Add(_measurementInterval);
                
                // Warte in der Echtzeit (skaliert mit Simulationsgeschwindigkeit)
                var realTimeDelayMs = (int)(_measurementInterval.TotalMinutes * (1000 / _simulationSpeedFactor));
                try
                {
                    await Task.Delay(realTimeDelayMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logAction("Simulation complete - 24 hours of weather data collected.");
        }

        private async Task TakeMeasurementWithDayNightCycleAsync(DateTimeOffset simulatedTime, CancellationToken cancellationToken)
        {
            await Task.Delay(1000); // Simuliere asynchrone Arbeit

            var now = simulatedTime.LocalDateTime;
            var isDaytime = IsDay(now.Hour);

            try
            {
                // --- LIDAR (Regen ja/nein) - Nachts etwas höhere Regenwahrscheinlichkeit
                var lidarVal = IsDaytimeAdjustedMeasurement(_lidar, isDaytime);
                bool hasRain = lidarVal == 1;

                // --- Druck (simuliert) und Frequenzregel für Temperatur
                var pressure = _rnd.Next(930, 1021); // hPa
                var tempFreqFactor = _rules.AdjustTemperatureFrequency(pressure); // 1 oder 2

                // --- Temperaturmessung - Tag/Nacht berücksichtigen
                var temp1 = IsDaytimeAdjustedMeasurement(_temperature, isDaytime);

                // --- Feuchtigkeit nur messen, wenn kein Regen laut Regel
                double? humidityVal = null;
                if (_rules.ShouldMeasureHumidity(lidarVal))
                {
                    humidityVal = IsDaytimeAdjustedMeasurement(_humidity, isDaytime);
                }

                // Ausgabe mit Tag/Nacht Hinweis
                var timeOfDay = isDaytime ? "DAY" : "NIGHT";
                _logAction(
                    $"[{now:HH:mm:ss}] [{timeOfDay}] rain={(hasRain ? "yes" : "no")}, " +
                    $"pressure={pressure} hPa, temp={temp1:0.0}°C" +
                    (humidityVal.HasValue ? $", humidity={humidityVal:0}%" : ", humidity=--"));
                
               

                // Bei doppelter Temp-Frequenz eine zweite Messung im gleichen Intervall
                if (tempFreqFactor == 2)
                {
                    // Simuliere halbes Intervall später
                    var extraTime = simulatedTime.Add(_measurementInterval / 2);
                    isDaytime = IsDay(extraTime.Hour);
                    
                    var temp2 = IsDaytimeAdjustedMeasurement(_temperature, isDaytime);
                    timeOfDay = isDaytime ? "DAY" : "NIGHT";
                    _logAction($"[{extraTime.LocalDateTime:HH:mm:ss}] [{timeOfDay}] extra temp due to low pressure => {temp2:0.0}°C");
                    
                   
                    
                }
            }
            catch (Exception error)
            {
                _logAction($"Error during measurement: {error.Message}");
            }
        }

        // Bestimmt, ob es Tag (6-18 Uhr) oder Nacht (18-6 Uhr) ist
        private bool IsDay(int hour)
        {
            return hour >= 6 && hour < 18;
        }

        // Passt Messwerte je nach Tageszeit an
        private int IsDaytimeAdjustedMeasurement(LidarSensor sensor, bool isDay)
        {
            var baseValue = (int)sensor.Measure();
            
            // Nachts leicht erhöhte Regenwahrscheinlichkeit
            if (!isDay && baseValue == 0)
            {
                // 20% Chance auf Regen nachts, wenn eigentlich kein Regen
                return _rnd.Next(5) == 0 ? 1 : 0;
            }
            
            return baseValue;
        }

        // Passt Temperaturwerte je nach Tageszeit an
        private double IsDaytimeAdjustedMeasurement(TemperatureSensor sensor, bool isDay)
        {
            var baseTemp = sensor.Measure();
            
            if (isDay)
            {
                // Tagsüber wärmer: +0 bis +5 Grad
                return baseTemp + _rnd.NextDouble() * 5;
            }
            else
            {
                // Nachts kühler: -2 bis -8 Grad
                return baseTemp - (2 + _rnd.NextDouble() * 6);
            }
        }

        // Passt Feuchtigkeitswerte je nach Tageszeit an
        private double IsDaytimeAdjustedMeasurement(HumiditySensor sensor, bool isDay)
        {
            var baseHumidity = sensor.Measure();
            
            if (isDay)
            {
                // Tagsüber weniger feucht: -5 bis 0 %
                return Math.Max(0, baseHumidity - _rnd.NextDouble() * 5);
            }
            else
            {
                // Nachts feuchter: +5 bis +15 %
                return Math.Min(100, baseHumidity + 5 + _rnd.NextDouble() * 10);
            }
        }
    }
}