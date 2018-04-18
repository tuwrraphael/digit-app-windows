using DigitAppCore;
using System;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.Background;
using Windows.Storage.Streams;

namespace DigitBackgroundTasks
{
    public sealed class AdvertimesmentWatcherTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;

        private class DigitAdvertisementPacket
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

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            var details = (BluetoothLEAdvertisementWatcherTriggerDetails)taskInstance.TriggerDetails;
            var client = new DigitServiceClient();
            var packets = details.Advertisements.Select(v => new DigitAdvertisementPacket(v));
            var data = string.Join(",", packets.Select(v => v.ToString()));
            await client.LogAsync($"Advertisements: {data}");
            var latest = packets.OrderByDescending(v => v.Timestamp).First();
            if (!latest.TimeKnown.HasValue || !latest.TimeKnown.Value)
            {
                var opts = new DigitBLEOptions();
                if (opts.IsConfigured)
                {
                    var bleClient = new DigitBLEClient(opts);
                    try
                    {
                        var res = await bleClient.SetTime(DateTime.Now);
                        await client.LogAsync($"Sent current time to watch: {res}", res == Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus.Success ? 1 : 3);
                    }
                    catch (DigitBLEExpcetion e)
                    {
                        await client.LogAsync($"BLE error: ${e.Message}", 3);
                    }
                }
                else
                {
                    await client.LogAsync($"Adv Task error: no device configured.", 3);
                }
            }
            if (new AdvertisementWatcherManager().RegisterAdvertisementWatcherTask())
            {
                await client.LogAsync($"Re-registered advertisement watcher task");
            }
            _deferral.Complete();
        }
    }
}