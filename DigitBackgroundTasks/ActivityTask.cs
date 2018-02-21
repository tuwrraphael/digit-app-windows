using DigitAppCore;
using Windows.ApplicationModel.Background;
using Windows.Devices.Sensors;

namespace DigitBackgroundTasks
{
    public sealed class ActivityTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            var data = (ActivitySensorTriggerDetails)taskInstance.TriggerDetails;
            var reports = data.ReadReports();
            var client = new DigitServiceClient();
            foreach (var report in reports)
            {
                await client.LogAsync($"{report.Reading.Activity} Confidence {report.Reading.Confidence} at {report.Reading.Timestamp}");
            }
            _deferral.Complete();
        }
    }
}