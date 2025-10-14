using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherSystem.DomainCore.BusinessLogic
{
   public abstract class Sensor
    {
    public string Name { get; protected set; }
    public string Unit { get; protected set; }

        public abstract double Measure();
       

    }
}
