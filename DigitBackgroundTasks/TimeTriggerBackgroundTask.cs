using DigitAppCore;
using Windows.ApplicationModel.Background;

namespace DigitBackgroundTasks
{
    public sealed class TimeTriggerBackgroundTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            var client = new DigitServiceClient();
            var opts = new DigitBLEOptions();
            if (opts.IsConfigured)
            {
                var bleClient = new DigitBLEClient(opts);
                var batteryService = new BatteryService(bleClient, client);
                await batteryService.AddBatteryMeasurement();
            }
            _deferral.Complete();
        }
    }
}