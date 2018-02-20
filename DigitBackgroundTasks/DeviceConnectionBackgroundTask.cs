using DigitAppCore;
using System;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth;
using Windows.Networking.PushNotifications;
using Windows.Storage;

namespace DigitBackgroundTasks
{
    public sealed class DeviceConnectionBackgroundTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            object trigger = taskInstance.TriggerDetails;
            var client = new DigitServiceClient();
            var opts = new DigitBLEOptions();

            if (opts.IsConfigured)
            {
                var isConnected = (await BluetoothLEDevice.FromIdAsync(opts.DeviceId)).ConnectionStatus == BluetoothConnectionStatus.Connected;
                if (isConnected)
                {
                    var bleClient = new DigitBLEClient(opts);
                    try
                    {
                        await bleClient.SetTime(DateTime.Now);
                    }
                    catch (DigitBLEExpcetion e)
                    {
                        await client.LogAsync($"BLE error: ${e.Message}", 3);
                    }
                    //try
                    //{
                    //    var res = await bleClient.SubscribeToBatteryCharacteristicAsync();
                    //    if (res == Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus.Success)
                    //    {
                    //        await client.LogAsync($"Subscribed to battery characteristic", 1);
                    //    }
                    //    else
                    //    {
                    //        await client.LogAsync($"Error subscribing to battery characterisitc", 3);
                    //    }
                    //}
                    //catch (DigitBLEExpcetion e)
                    //{
                    //    await client.LogAsync($"BLE error: ${e.Message}", 3);
                    //}
                }
            }
            else
            {
                await client.LogAsync($"Device Connection Task error: no device configured.", 3);
            }
            await client.LogAsync($"Device connection changed trigger");
            _deferral.Complete();
        }
    }
}