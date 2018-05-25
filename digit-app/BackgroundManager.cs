using DigitAppCore;
using DigitBackgroundTasks;
using DigitService.Client;
using DigitService.Models;
using Microsoft.WindowsAzure.Messaging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Enumeration;
using Windows.Devices.Geolocation;
using Windows.Networking.PushNotifications;
using Windows.Storage;

namespace digit_app
{
    public class BackgroundManager
    {
        private readonly ApplicationDataContainer localSettings;
        private readonly string notificationHubName;
        private readonly string notificationHubEndpoint;
        private readonly IDigitServiceClient client;

        public BackgroundManager()
        {
            localSettings = ApplicationData.Current.LocalSettings;
            var configResources = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView("config");
            notificationHubName = configResources.GetString("NotificationHubName");
            notificationHubEndpoint = configResources.GetString("NotificationHubEndpoint");
            client = DigitServiceBuilder.Get();
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
                    var pushChannelRegistration = new PushChannelRegistration()
                    {
                        Uri = result.RegistrationId
                    };
                    try //retry here because it's one of the calls where the user is active
                    {
                        await client.SetupPushChannelAsync(pushChannelRegistration);
                    }
                    catch (UnauthorizedException)
                    {
                        var authenticationProvider = DigitServiceBuilder.AuthenticationProvider();
                        await authenticationProvider.AuthenticateUser();
                        try
                        {
                            await client.SetupPushChannelAsync(pushChannelRegistration);
                        }
                        catch (UnauthorizedException e)
                        {
                            await client.LogAsync($"Authorization error while push channel registration: {e.Message}", 3);
                            return;
                        }
                    }
                    catch (DigitServiceException s)
                    {
                        await client.LogAsync($"Failed to register notification channel: {s.Message}", 3);
                        return;
                    }
                    localSettings.Values["PushChannelUri"] = channel.Uri;
                    await client.LogAsync($"Successfully registered notification channel {result.RegistrationId}", 1);
                }
                else
                {
                    await client.LogAsync($"Failed to register channel at hub", -1);
                }
            }
        }


        public async void RegisterPushBackgroundTask()
        {
            if (BackgroundTaskRegistration.AllTasks.Any(p => p.Value.Name == nameof(PushBackgroundTask)))
                return;
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

        public async void RegisterGeolocationTasks()
        {
            var access = await Geolocator.RequestAccessAsync();
            if (access != GeolocationAccessStatus.Allowed)
            {
                await client.LogAsync($"Geolocation Access Denied.", 3);
            }
            //if (!BackgroundTaskRegistration.AllTasks.Any(p => p.Value.Name == nameof(VisitsBackgroundTask)))
            //{
            //    var builder = new BackgroundTaskBuilder
            //    {
            //        Name = nameof(VisitsBackgroundTask),
            //        TaskEntryPoint = typeof(VisitsBackgroundTask).FullName
            //    };
            //    builder.SetTrigger(new GeovisitTrigger());
            //    BackgroundTaskRegistration t = builder.Register();
            //    await client.LogAsync($"Successfully registered geovisit trigger task.", 1);
            //}
        }

        public async void RegisterTimeTriggerTask()
        {
            if (!BackgroundTaskRegistration.AllTasks.Any(p => p.Value.Name == nameof(TimeTriggerBackgroundTask)))
            {
                var builder = new BackgroundTaskBuilder
                {
                    Name = nameof(TimeTriggerBackgroundTask),
                    TaskEntryPoint = typeof(TimeTriggerBackgroundTask).FullName
                };
                builder.SetTrigger(new TimeTrigger(120, false));
                BackgroundTaskRegistration t = builder.Register();
                await client.LogAsync($"Successfully registered time trigger task.", 1);
            }
        }

        public async void RegisterActivityTriggerTask()
        {
            var deviceAccessInfo = DeviceAccessInformation.CreateFromDeviceClassId(new Guid("9D9E0118-1807-4F2E-96E4-2CE57142E196"));
            if (deviceAccessInfo.CurrentStatus == DeviceAccessStatus.Allowed)
            {
                if (!BackgroundTaskRegistration.AllTasks.Any(p => p.Value.Name == nameof(ActivityTask)))
                {
                    var builder = new BackgroundTaskBuilder
                    {
                        Name = nameof(ActivityTask),
                        TaskEntryPoint = typeof(ActivityTask).FullName
                    };
                    var trigger = new ActivitySensorTrigger((uint)new TimeSpan(0, 5, 0).TotalMilliseconds);
                    foreach (var act in trigger.SupportedActivities)
                    {
                        trigger.SubscribedActivities.Add(act);
                    }
                    builder.SetTrigger(trigger);
                    BackgroundTaskRegistration t = builder.Register();
                    await client.LogAsync($"Successfully registered activity trigger task.", 1);
                }
            }
            else
            {
                await client.LogAsync($"Activity Sensor not allowed", 3);
            }
        }

        public async Task<bool> CheckAccess()
        {
            var status = await BackgroundExecutionManager.RequestAccessAsync();
            return status == BackgroundAccessStatus.AlwaysAllowed || status == BackgroundAccessStatus.AllowedSubjectToSystemPolicy;
        }
    }
}
