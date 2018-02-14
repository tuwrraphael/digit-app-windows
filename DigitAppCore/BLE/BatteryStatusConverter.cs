using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Background;
using Windows.Storage.Streams;

namespace DigitAppCore.BLE
{
    public class BatteryStatusConverter
    {
        public byte GetValueFromBuffer(IBuffer buffer)
        {
            var dataReader = DataReader.FromBuffer(buffer);
            return dataReader.ReadByte();
        }
    }
}
