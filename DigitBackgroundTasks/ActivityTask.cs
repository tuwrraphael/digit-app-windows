using DigitAppCore;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.Devices.Sensors;
using Windows.Storage;

namespace DigitBackgroundTasks
{
    public sealed class ActivityTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            var data = (ActivitySensorTriggerDetails)taskInstance.TriggerDetails;
            var last = data.ReadReports().OrderByDescending(p => p.Reading.Timestamp).FirstOrDefault();
            var stored = (ApplicationData.Current.LocalSettings.Values["Activity"] as ActivityType?);
            if (null == last)
            {
                ActivityType activity = ActivityType.Idle;
                bool known = true;
                switch (last.Reading.Activity)
                {
                    case ActivityType.Fidgeting:
                    case ActivityType.Idle:
                    case ActivityType.Stationary:
                        activity = ActivityType.Idle;
                        break;
                    case ActivityType.Biking:
                    case ActivityType.InVehicle:
                        activity = ActivityType.InVehicle;
                        break;
                    case ActivityType.Walking:
                    case ActivityType.Running:
                        activity = ActivityType.Walking;
                        break;
                    default:
                        known = false;
                        break;
                }
                if (known && (!stored.HasValue || stored.Value != activity))
                {
                    var client = new DigitServiceClient();
                    await client.LogAsync($"{last.Reading.Activity} Confidence {last.Reading.Confidence} at {last.Reading.Timestamp}");
                    ApplicationData.Current.LocalSettings.Values["Activity"] = activity;
                }
            }
            _deferral.Complete();
        }
    }
}