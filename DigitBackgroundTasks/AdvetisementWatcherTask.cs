using DigitAppCore;
using System;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth.Background;

namespace DigitBackgroundTasks
{
    public sealed class AdvertimesmentWatcherTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            var details = (BluetoothLEAdvertisementWatcherTriggerDetails)taskInstance.TriggerDetails;
            var client = new DigitServiceClient();
            await client.LogAsync($"Advertisement watcher trigger");
            var opts = new DigitBLEOptions();
            if (opts.IsConfigured)
            {
                var bleClient = new DigitBLEClient(opts);
                try
                {
                    await bleClient.SetTime(DateTime.Now);
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
            if (new AdvertisementWatcherManager().RegisterAdvertisementWatcherTask())
            {
                await client.LogAsync($"Re-registered advertisement watcher task");
            }
            _deferral.Complete();
        }
    }
}