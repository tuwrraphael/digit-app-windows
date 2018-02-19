using DigitAppCore;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace DigitBackgroundTasks
{
    public sealed class TimeTriggerBackgroundTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            var client = new DigitServiceClient();
            var stored = (ApplicationData.Current.LocalSettings.Values["TimeTrigger"] as int?);
            var times = stored.HasValue ? (stored.Value + 1) : 1;
            ApplicationData.Current.LocalSettings.Values["TimeTrigger"] = times;
            await client.LogAsync($"Time trigged background task ran (times: {times}).");
            _deferral.Complete();
        }
    }
}