using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherSystem.DomainCore.BusinessLogic
{
    public class LidarSensor : Sensor
    {
        private Random _rnd = new Random();

        public LidarSensor()
        {
            Name = "Lidar";
            Unit = "Rain"; // true/false Simulation
        }

        public override double Measure()
        {
            return _rnd.Next(0, 2); // 0 = kein Regen, 1 = Regen
        }
    }
}
