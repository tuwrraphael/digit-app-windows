using DigitAppCore;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation;

namespace DigitBackgroundTasks
{
    public sealed class VisitsBackgroundTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            GeovisitTriggerDetails visit = (GeovisitTriggerDetails)taskInstance.TriggerDetails;
            var client = DigitServiceBuilder.Get();
            var reports = visit.ReadReports();
            var msg = string.Join("\n", reports.Select(r => $"{r.Timestamp} {r.StateChange} {r.Position.Coordinate.Point}").ToArray());
            await client.LogAsync($"Geovisit:\n{msg}");
            _deferral.Complete();
        }
    }
}