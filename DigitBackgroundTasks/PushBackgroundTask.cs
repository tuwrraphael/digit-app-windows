using DigitAppCore;
using Windows.ApplicationModel.Background;
using Windows.Networking.PushNotifications;

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
            switch (notification.Content)
            {
                case "measure_battery":
                    var opts = new DigitBLEOptions();
                    if (opts.IsConfigured)
                    {
                        var bleClient = new DigitBLEClient(opts);
                        var batteryService = new BatteryService(bleClient, client);
                        await batteryService.AddBatteryMeasurement();
                    }
                    else
                    {
                        await client.LogAsync($"Push error: no device configured.", 3);
                    }
                    break;
                default: break;
            }
            _deferral.Complete();
        }
    }
}