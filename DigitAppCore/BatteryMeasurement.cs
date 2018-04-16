using System;

namespace DigitAppCore
{
    public class BatteryMeasurement
    {
        public DateTime MeasurementTime { get; set; }
        public uint RawValue { get; set; }
    }
}