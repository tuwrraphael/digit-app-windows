using Windows.ApplicationModel.Background;

namespace DigitAppCore
{
    public class AdvertisementWatcherManager
    {
        private const string TaskName = "AdvertimesmentWatcherTask";
        private const string Namespace = "DigitBackgroundTasks";

        public bool RegisterAdvertisementWatcherTask()
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == TaskName)
                {
                    return false;
                }
            }
            var builder = new BackgroundTaskBuilder
            {
                Name = TaskName,
                TaskEntryPoint = $"{Namespace}.{TaskName}"
            };
            var trigger = new BluetoothLEAdvertisementWatcherTrigger();
            trigger.AdvertisementFilter.Advertisement.ServiceUuids.Add(DigitBLEClient.DigitServiceGuid);
            builder.SetTrigger(trigger);
            BackgroundTaskRegistration t = builder.Register();
            return true;
        }
    }
}
