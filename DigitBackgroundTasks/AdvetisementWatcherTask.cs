using DigitAppCore;
using System;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth.Background;

namespace DigitBackgroundTasks
{
    public sealed partial class AdvertimesmentWatcherTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            var details = (BluetoothLEAdvertisementWatcherTriggerDetails)taskInstance.TriggerDetails;
            var client = DigitServiceBuilder.Get();
            if (details.Advertisements != null && details.Advertisements.Count > 0)
            {
                var packets = details.Advertisements.Select(v => new DigitAdvertisementPacket(v));
                var latest = packets.OrderByDescending(v => v.Timestamp).First();
                if (!latest.TimeKnown.HasValue || !latest.TimeKnown.Value)
                {
                    var opts = new DigitBLEOptions();
                    if (opts.IsConfigured)
                    {
                        var bleClient = new DigitBLEClient(opts);
                        try
                        {
                            await bleClient.SetTime(DateTime.Now);
                            await client.LogAsync($"Sent current time to watch (advertisement requested)", 1);
                        }
                        catch (DigitBLEExpcetion e)
                        {
                            await client.LogAsync($"Could not send time (advertisement requested): ${e.Message}", 3);
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
            }
            _deferral.Complete();
        }
    }
}