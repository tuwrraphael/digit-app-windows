using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitAppCore.BLE
{
    public class CTSConverter
    {
        public byte[] Convert(DateTime dateTime)
        {
            return new byte[] {
                (byte)(dateTime.Year & 0xFF),
                (byte)((dateTime.Year >> 8) & 0xFF),
                (byte)(dateTime.Month & 0xFF),
                (byte)(dateTime.Day & 0xFF),
                (byte)(dateTime.Hour & 0xFF),
                (byte)(dateTime.Minute & 0xFF),
                (byte)(dateTime.Second & 0xFF),
                Convert(dateTime.DayOfWeek),
                (byte)(((float)dateTime.Millisecond / 1000.0) * 256)
            };
        }

        private byte Convert(DayOfWeek dayOfWeek)
        {
            switch(dayOfWeek)
            {
                case DayOfWeek.Monday:
                    return 1;
                case DayOfWeek.Tuesday:
                    return 2;
                case DayOfWeek.Wednesday:
                    return 3;
                case DayOfWeek.Thursday:
                    return 4;
                case DayOfWeek.Friday:
                    return 5;
                case DayOfWeek.Saturday:
                    return 6;
                case DayOfWeek.Sunday:
                    return 7;
                default:
                    return 0;
            }
        }
    }
}
