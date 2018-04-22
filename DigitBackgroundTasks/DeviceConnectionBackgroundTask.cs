using DigitAppCore;
using System;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth;

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
            }
            else
            {
                await client.LogAsync($"Device Connection Task error: no device configured.", 3);
            }
            _deferral.Complete();
        }
    }
}