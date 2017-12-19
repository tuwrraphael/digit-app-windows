using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace digit_app
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private string deviceId;

        public MainPage()
        {
            this.InitializeComponent();
            StartBleDeviceWatcher();
        }

        

        private void StartBleDeviceWatcher()
        {
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };
            string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";
            var deviceWatcher = DeviceInformation.CreateWatcher(aqsAllBluetoothLEDevices,
                        requestedProperties,
                        DeviceInformationKind.AssociationEndpoint);
            deviceWatcher.Added += async (sender, args) =>
            {
                if (args.Name == "Digit")
                {
                    deviceWatcher.Stop();
                    try
                    {
                        deviceId = args.Id;
                        var bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(args.Id);
                        var svcs = (await bluetoothLeDevice.GetGattServicesAsync()).Services
                        .Where(v => v.Uuid.Equals(new Guid("00001523-1212-efde-1523-785fef13d123"))).FirstOrDefault();
                        if (null != svcs)
                        {
                            byte[] data = new BLE.CTSConverter().Convert(DateTime.Now);
                            await svcs.GetAllCharacteristics().First().WriteValueAsync(data.AsBuffer());
                        }
                        //sendTime();
                    }
                    catch (Exception ex) when ((uint)ex.HResult == 0x800710df)
                    {
                        // ERROR_DEVICE_NOT_AVAILABLE because the Bluetooth radio is not on.
                        //Log("Radio not turned on");
                    }
                }
            };
            //deviceWatcher.Updated += DeviceWatcher_Updated;
            //deviceWatcher.Removed += DeviceWatcher_Removed;
            //deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            //deviceWatcher.Stopped += DeviceWatcher_Stopped;
            deviceWatcher.Start();
        }
    }
}
