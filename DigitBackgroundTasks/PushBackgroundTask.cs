using DigitAppCore;
using System;
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
            var client = DigitServiceBuilder.Get();
            try
            {
                RawNotification notification = (RawNotification)taskInstance.TriggerDetails;
                await client.LogAsync($"Received push {notification.Content}");
                var opts = new DigitBLEOptions();
                if (opts.IsConfigured)
                {
                    var bleClient = new DigitBLEClient(opts);
                    switch (notification.Content)
                    {
                        case "measure_battery":
                            var batteryService = new BatteryService(bleClient, client);
                            await batteryService.AddBatteryMeasurement();
                            break;
                        case "send_time":
                            try
                            {
                                await bleClient.SetTime(DateTime.Now);
                            }
                            catch (DigitBLEExpcetion e)
                            {
                                await client.LogAsync($"Could not send time: {e.Message}", 3);
                            }
                            break;
                        default: break;
                    }
                }
                else
                {
                    await client.LogAsync($"Push error: no device configured.", 3);
                }
            }
            catch (Exception e)
            {
                await client.LogAsync($"Unhandled background exception: {e.Message}", 3);
            }
            _deferral.Complete();
        }
    }
}