using DigitAppCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace digit_app
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Configure : Page
    {
        private ObservableCollection<BluetoothLEDeviceDisplay> Devices = new ObservableCollection<BluetoothLEDeviceDisplay>();

        private string DigitId;

        public Configure()
        {
            this.InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };
            string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";
            var deviceWatcher = DeviceInformation.CreateWatcher(aqsAllBluetoothLEDevices,
                        requestedProperties,
                        DeviceInformationKind.AssociationEndpoint);
            deviceWatcher.Added += async (s, args) =>
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var dev = Devices.SingleOrDefault(p => args.Id == p.Id);
                if (null == dev)
                {
                    Devices.Add(new BluetoothLEDeviceDisplay(args));
                }
            });
            deviceWatcher.Removed += async (s, args) =>
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Devices.Remove(Devices.SingleOrDefault(p => args.Id == p.Id));
            });

            deviceWatcher.Updated += async (s, args) =>
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var dev = Devices.SingleOrDefault(p => args.Id == p.Id);
                if (null != dev)
                {
                    dev.Update(args);
                }
            });
            deviceWatcher.Start();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var client = new DigitServiceClient();
            var selected = ResultsListView.SelectedItem as BluetoothLEDeviceDisplay;
            if (null != selected)
            {
                var opts = new DigitBLEOptions();
                opts.StoreDeviceId(selected.Id);
                DigitId = selected.Id;
                await client.LogAsync($"Selected device {selected.Id} as digit.");
                var man = new BackgroundManager();
                man.RegisterDeviceConnectionBackgroundTask(selected.Id);
                var bleClient = new DigitBLEClient(opts);
                var res = await bleClient.SubscribeToBatteryCharacteristicAsync();
                if (res == Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus.Success)
                {
                    await client.LogAsync($"Subscribed to battery characteristic", 1);
                }
                else
                {
                    await client.LogAsync($"Error subscribing to battery characterisitc", 3);
                }
                try
                {
                    var trigger = await bleClient.CreateTriggerForBatteryStatusAsync();
                    man.RegisterBatteryNotificationTask(trigger);
                }
                catch (DigitBLEExpcetion ex)
                {
                    await client.LogAsync($"Error while registering battery notification task {ex.Message}");
                }
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var client = new DigitServiceClient();
            if (!await client.HasValidAccessToken())
            {
                await client.Authenticate();
            }     
            var man = new BackgroundManager();
            man.RegisterPushChannel();
            man.RegisterPushBackgroundTask();
            man.RegisterAdvertisementWatcherTask();
        }
    }
}
