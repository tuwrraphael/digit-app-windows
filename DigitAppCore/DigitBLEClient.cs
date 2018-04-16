using DigitAppCore.BLE;
using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace DigitAppCore
{
    public class DigitBLEClient
    {
        public static Guid DigitServiceGuid = new Guid("00001523-1212-efde-1523-785fef13d123");

        private DigitBLEOptions options;

        public DigitBLEClient(DigitBLEOptions options)
        {
            this.options = options;
        }

        public async Task<GattCommunicationStatus> SetTime(DateTime dateTime)
        {
            var bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(options.DeviceId);
            var svcs = (await bluetoothLeDevice.GetGattServicesAsync()).Services
            .Where(v => v.Uuid.Equals(DigitServiceGuid)).FirstOrDefault();
            if (null != svcs)
            {
                byte[] data = new CTSConverter().Convert(dateTime);
                var chars = await svcs.GetCharacteristicsAsync();
                return await chars.Characteristics.First().WriteValueAsync(data.AsBuffer());
            }
            throw new DigitBLEExpcetion("Service not found");
        }

        private async Task<GattCharacteristic> GetBatteryCharacteristicAsync()
        {
            var bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(options.DeviceId);
            var svc = (await bluetoothLeDevice.GetGattServicesForUuidAsync(GattServiceUuids.Battery))
                .Services.FirstOrDefault();
            if (null == svc)
            {
                throw new DigitBLEExpcetion("Battery Service not found");
            }
            var chars = (await svc.GetCharacteristicsForUuidAsync(GattCharacteristicUuids.BatteryLevel))
                .Characteristics.FirstOrDefault();
            if (null == chars)
            {
                throw new DigitBLEExpcetion("Battery Level Characteristic not found");
            }
            return chars;
        }

        public async Task<GattCommunicationStatus> SubscribeToBatteryCharacteristicAsync()
        {
            var chars = await GetBatteryCharacteristicAsync();
            return await chars.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
        }


        public async Task<byte?> ReadBatteryAsync()
        {
            var chars = await GetBatteryCharacteristicAsync();
            var res = await chars.ReadValueAsync();
            if (res.Status == GattCommunicationStatus.Success)
            {
                return new BatteryStatusConverter().GetValueFromBuffer(res.Value);
            }
            else if (res.Status == GattCommunicationStatus.Unreachable)
            {
                return null;
            }
            throw new DigitBLEExpcetion("Could not read battery status");
        }

        public async Task<GattCharacteristicNotificationTrigger> CreateTriggerForBatteryStatusAsync()
        {
            var chars = await GetBatteryCharacteristicAsync();
            return new GattCharacteristicNotificationTrigger(chars);
        }

        public async Task<bool> Pair()
        {
            var bluetoothLeDevice = await DeviceInformation.CreateFromIdAsync(options.DeviceId);
            if (!bluetoothLeDevice.Pairing.IsPaired)
            {
                var pairingStatus = await bluetoothLeDevice.Pairing.PairAsync();
                if (pairingStatus.Status != DevicePairingResultStatus.Paired)
                {
                    throw new DigitBLEExpcetion("Could not pair");
                }
                return true;
            }
            return false;
        }

        public async Task EnterBootloader()
        {
            var bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(options.DeviceId);
            var svc = (await bluetoothLeDevice.GetGattServicesForUuidAsync(new Guid("0000fe59-0000-1000-8000-00805f9b34fb")))
            .Services.Single();
            var chars = (await svc.GetCharacteristicsForUuidAsync(new Guid("8ec90003-f315-4f60-9fb8-838830daea50"))).Characteristics.Single();
            await chars.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate);
            byte[] data = { 0x01 };
            //chars.ValueChanged += Chars_ValueChanged;
            await chars.WriteValueAsync(data.AsBuffer());
            return;
        }
    }
}
