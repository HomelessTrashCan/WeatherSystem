using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherSystem.DomainCore.BusinessLogic
{
    public class MeasurementRules
    {
        public bool ShouldMeasureHumidity(int lidarValue)
        {
            // Regel: Wenn es regnet (lidarValue == 1), dann keine Feuchtigkeitsmessung
            return lidarValue == 0;
        }

        public int AdjustTemperatureFrequency(double pressureValue)
        {
            // Regel: Wenn Druck < 950 HPa -> doppelte Frequenz
            return pressureValue < 950 ? 2 : 1;
        }
    }
}
