using DigitAppCore;
using DigitService.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
                if (args.Name == "Digit")
                {
                    var dev = Devices.SingleOrDefault(p => args.Id == p.Id);
                    if (null == dev)
                    {
                        Devices.Add(new BluetoothLEDeviceDisplay(args));
                    }
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
            var client = DigitServiceBuilder.Get();
            if (ResultsListView.SelectedItem is BluetoothLEDeviceDisplay selected)
            {
                bool claimed = false;
                try
                {
                    claimed = await client.Device["12345"].ClaimAsync();
                }
                catch (DigitServiceException exc)
                {
                    await client.LogAsync($"Could not claim device: ${exc.Message}");
                }
                if (claimed)
                {
                    var opts = new DigitBLEOptions();
                    opts.StoreDeviceId(selected.Id);
                    DigitId = selected.Id;
                    var bleClient = new DigitBLEClient(opts);
                    bool paired = false;
                    try
                    {
                        paired = await bleClient.Pair();
                    }
                    catch (DigitBLEExpcetion ex)
                    {
                        await client.LogAsync($"Pairing failed: {ex.Message}", 3);
                    }
                    var pairInformation = paired ? "(paired)" : "";
                    await client.LogAsync($"Selected device {selected.Id} as digit{pairInformation}.");
                    var man = new BackgroundManager();
                    man.RegisterDeviceConnectionBackgroundTask(selected.Id);
                }
            }
        }

        private async Task ConfigureTasks()
        {
            bool isInternetConnected = NetworkInterface.GetIsNetworkAvailable();
            if (!isInternetConnected)
            {
                ContentDialog noWifiDialog = new ContentDialog
                {
                    Title = "No Network connection",
                    Content = "Check your connection and try again.",
                    CloseButtonText = "Ok"
                };
                ContentDialogResult result = await noWifiDialog.ShowAsync();
            }
            else
            {
                var authenticationProvider = DigitServiceBuilder.AuthenticationProvider();
                if (!await authenticationProvider.HasValidAccessToken())
                {
                    await authenticationProvider.AuthenticateUser();
                }
                var client = DigitServiceBuilder.Get();
                var man = new BackgroundManager();
                if (await man.CheckAccess())
                {
                    man.RegisterPushChannel();
                    man.RegisterPushBackgroundTask();
                    man.RegisterAdvertisementWatcherTask();
                    man.RegisterGeolocationTasks();
                    man.RegisterTimeTriggerTask();
                    //man.RegisterActivityTriggerTask();
                }
                else
                {
                    await client.LogAsync("Background tasks disabled", 3);
                }
            }

        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await ConfigureTasks();
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            await ConfigureTasks();
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            var opts = new DigitBLEOptions();
            var bleClient = new DigitBLEClient(opts);
            await bleClient.EnterBootloader();
        }
    }
}
