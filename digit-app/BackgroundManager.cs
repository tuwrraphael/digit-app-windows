using DigitAppCore;
using DigitBackgroundTasks;
using Microsoft.WindowsAzure.Messaging;
using System;
using Windows.ApplicationModel.Background;
using Windows.Networking.PushNotifications;
using Windows.Storage;

namespace digit_app
{
    public class BackgroundManager
    {
        private readonly ApplicationDataContainer localSettings;
        private readonly string notificationHubName;
        private readonly string notificationHubEndpoint;
        private readonly DigitServiceClient client;

        public BackgroundManager()
        {
            localSettings = ApplicationData.Current.LocalSettings;
            var configResources = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView("config");
            notificationHubName = configResources.GetString("NotificationHubName");
            notificationHubEndpoint = configResources.GetString("NotificationHubEndpoint");
            client = new DigitServiceClient();
        }

        public async void RegisterPushChannel()
        {
            var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            var oldChannel = (localSettings.Values["PushChannelUri"] as string);
            if (null == oldChannel || oldChannel != channel.Uri)
            {
                var hub = new NotificationHub(notificationHubName, notificationHubEndpoint);
                var result = await hub.RegisterNativeAsync(channel.Uri);
                if (result.RegistrationId != null)
                {
                    localSettings.Values["PushChannelUri"] = channel.Uri;
                    await client.LogAsync($"Successfully registered notification channel {result.RegistrationId}");
                }
                else
                {
                    await client.LogAsync($"Failed to register notification channel", -1);
                }
            }
        }


        public async void RegisterPushBackgroundTask()
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == nameof(PushBackgroundTask))
                {
                    return;
                }
            }
            var builder = new BackgroundTaskBuilder
            {
                Name = nameof(PushBackgroundTask),
                TaskEntryPoint = typeof(PushBackgroundTask).FullName
            };
            builder.SetTrigger(new PushNotificationTrigger());
            BackgroundTaskRegistration t = builder.Register();
            await client.LogAsync($"Successfully registered push background task");
        }

        public async void RegisterDeviceConnectionBackgroundTask(string deviceId)
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == nameof(DeviceConnectionBackgroundTask))
                {
                    task.Value.Unregister(false);
                }
            }
            var builder = new BackgroundTaskBuilder
            {
                Name = nameof(DeviceConnectionBackgroundTask),
                TaskEntryPoint = typeof(DeviceConnectionBackgroundTask).FullName
            };
            var trigger = await DeviceConnectionChangeTrigger.FromIdAsync(deviceId);
            trigger.MaintainConnection = true;
            builder.SetTrigger(trigger);
            BackgroundTaskRegistration t = builder.Register();
            await client.LogAsync($"Successfully registered decive connection changed task");
        }

        public async void RegisterAdvertisementWatcherTask()
        {
            if (new AdvertisementWatcherManager().RegisterAdvertisementWatcherTask())
            {
                await client.LogAsync($"Successfully registered advertisement watcher task");
            }
        }

        public async void RegisterBatteryNotificationTask(GattCharacteristicNotificationTrigger trigger)
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == nameof(BatteryNotificationTask))
                {
                    task.Value.Unregister(false);
                }
            }
            var builder = new BackgroundTaskBuilder
            {
                Name = nameof(BatteryNotificationTask),
                TaskEntryPoint = typeof(BatteryNotificationTask).FullName
            };
            builder.SetTrigger(trigger);
            BackgroundTaskRegistration t = builder.Register();
            await client.LogAsync($"Successfully registered battery notification task");
        }
    }
}
