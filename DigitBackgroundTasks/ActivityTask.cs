using DigitAppCore;
using DigitService.Client;
using Microsoft.Extensions.Options;
using System;
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
            var client = DigitServiceBuilder.Get();
            var data = (ActivitySensorTriggerDetails)taskInstance.TriggerDetails;
            var reports = data.ReadReports();
            var last = reports.OrderByDescending(p => p.Reading.Timestamp).FirstOrDefault();
            var storedString = ApplicationData.Current.LocalSettings.Values["Activity"] as string;
            ActivityType? stored = null;
            if (Enum.TryParse(storedString, out ActivityType output))
            {
                stored = output;
            }
            if (null != last)
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
                    await client.LogAsync($"{activity}");
                    ApplicationData.Current.LocalSettings.Values["Activity"] = activity.ToString();
                }
            }
            await client.LogAsync(string.Join("\n", reports.Select(r => $"{r.Reading.Activity} Confidence {r.Reading.Confidence} at {r.Reading.Timestamp}")));
            _deferral.Complete();
        }
    }
}