using DigitAppCore;
using DigitAppCore.BLE;
using System;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Background;
using Windows.Networking.PushNotifications;
using Windows.Storage;

namespace DigitBackgroundTasks
{
    public sealed class BatteryNotificationTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            var details = (GattCharacteristicNotificationTriggerDetails)taskInstance.TriggerDetails;
            var value = new BatteryStatusConverter().GetValueFromBuffer(details.Value);
            var client = new DigitServiceClient();
            await client.LogAsync($"Battery notification. Value: {value}");
            _deferral.Complete();
        }
    }
}