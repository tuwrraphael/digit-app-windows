using DigitAppCore;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace DigitBackgroundTasks
{
    public sealed class TimeTriggerBackgroundTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;
        private const int EveryNthTime = 3;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            var stored = (ApplicationData.Current.LocalSettings.Values["SendBattery"] as int?);
            var times = stored.HasValue ? (stored.Value - 1) : EveryNthTime;
            if (times == 0)
            {
                var client = new DigitServiceClient();
                var opts = new DigitBLEOptions();
                if (opts.IsConfigured)
                {
                    var bleClient = new DigitBLEClient(opts);
                    var batteryService = new BatteryService(bleClient, client);
                    if (await batteryService.AddBatteryMeasurement())
                    {
                        times = EveryNthTime;
                    }
                    else
                    {
                        times = 1;
                    }
                }
            }
            ApplicationData.Current.LocalSettings.Values["SendBattery"] = times;
            _deferral.Complete();
        }
    }
}