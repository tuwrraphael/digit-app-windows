using DigitAppCore;
using DigitService.Models;
using Newtonsoft.Json;
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


                var content = JsonConvert.DeserializeObject<PushPayload>(notification.Content);
                switch (content.Action)
                {
                    case PushActions.MeasureBattery:
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
                    case PushActions.SendTime:
                        if (opts.IsConfigured)
                        {
                            var bleClient = new DigitBLEClient(opts);
                            try
                            {
                                await bleClient.SetTime(DateTime.Now);
                            }
                            catch (DigitBLEExpcetion e)
                            {
                                await client.LogAsync($"Could not send time: {e.Message}", 3);
                            }
                        }
                        else
                        {
                            await client.LogAsync($"Push error: no device configured.", 3);
                        }
                        break;
                    case PushActions.SendLocation:
                        var locationService = new LocationService(client);
                        await locationService.SendCurrentLocation();
                        break;
                    default: break;
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