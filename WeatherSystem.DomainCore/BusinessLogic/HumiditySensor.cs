using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherSystem.DomainCore.BusinessLogic
{
    public class HumiditySensor : Sensor
    {
        private Random _rnd = new Random();

        public HumiditySensor()
        {
            Name = "Humidity";
            Unit = "%";
        }

        public override double Measure()
        {
            return _rnd.Next(30, 90);
        }
    }
}
