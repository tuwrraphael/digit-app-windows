using DigitAppCore;
using System;
using Windows.ApplicationModel.Background;

namespace DigitBackgroundTasks
{
    public sealed class TimeTriggerBackgroundTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            var client = DigitServiceBuilder.Get();
            try
            {
                var opts = new DigitBLEOptions();
                if (opts.IsConfigured)
                {
                    var bleClient = new DigitBLEClient(opts);
                    var batteryService = new BatteryService(bleClient, client);
                    await batteryService.TimeTriggeredMeasurement();
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