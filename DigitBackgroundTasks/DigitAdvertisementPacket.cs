using System;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace DigitBackgroundTasks
{
    public sealed class DigitAdvertisementPacket
    {
        public DateTimeOffset Timestamp { get; set; }
        public bool? TimeKnown { get; set; }
        public BluetoothLEAdvertisementType Type { get; set; }
        public byte? Flags { get; set; }
        public string LocalName { get; set; }

        public DigitAdvertisementPacket(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            Timestamp = args.Timestamp;
            var data = args.Advertisement.GetManufacturerDataByCompanyId(89);
            if (data.Count > 0)
            {
                var buffer = data[0].Data;
                Flags = DataReader.FromBuffer(buffer).ReadByte();
                TimeKnown = (Flags.Value & 1) > 0;
            }
            Type = args.AdvertisementType;
            LocalName = args.Advertisement.LocalName;
        }

        public override string ToString()
        {
            return $"{Timestamp}{LocalName}: T:{Type};{Flags}";
        }
    }
}