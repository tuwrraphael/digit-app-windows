using DigitAppCore;
using System;
using Windows.ApplicationModel.Background;
using Windows.Networking.PushNotifications;
using Windows.Storage;

namespace DigitBackgroundTasks
{
    public sealed class PushBackgroundTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            RawNotification notification = (RawNotification)taskInstance.TriggerDetails;
            var client = new DigitServiceClient();
            await client.LogAsync($"Received push {notification.Content}");
            var opts = new DigitBLEOptions();
            if (opts.IsConfigured)
            {
                var bleClient = new DigitBLEClient(opts);
                try
                {
                    await bleClient.SetTime(DateTime.Now);
                    var batt = await bleClient.ReadBatteryAsync();
                    double calc = 3.6 * (batt / 100.0);
                    await client.LogAsync($"Read battery: {batt} = {calc}V");
                }
                catch (DigitBLEExpcetion e)
                {
                    await client.LogAsync($"BLE error: ${e.Message}", 3);
                }
            }
            else
            {
                await client.LogAsync($"Push error: no device configured.", 3);
            }

            _deferral.Complete();
        }
    }
}