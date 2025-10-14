namespace WeatherSystem.DomainCore.BusinessLogic
{
    public class TemperatureSensor : Sensor
    {
        private Random _rnd = new Random();

        public TemperatureSensor()
        {
            Name = "Temperature";
            Unit = "°C";
        }

        public override double Measure()
        {
            return _rnd.Next(15, 30); // einfache Simulation
        }
    }
}
