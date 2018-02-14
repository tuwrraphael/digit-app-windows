using System.ComponentModel;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml.Media.Imaging;

namespace digit_app
{
    public class BluetoothLEDeviceDisplay : INotifyPropertyChanged
    {
        public BluetoothLEDeviceDisplay(DeviceInformation deviceInfoIn)
        {
            DeviceInformation = deviceInfoIn;
        }

        public DeviceInformation DeviceInformation { get; private set; }
        public string Id => DeviceInformation.Id;
        public string Name => DeviceInformation.Name;    
        public bool IsPaired => DeviceInformation.Pairing.IsPaired;
        public bool IsConnected => (bool?)DeviceInformation.Properties["System.Devices.Aep.IsConnected"] == true;
        public bool IsConnectable => (bool?)DeviceInformation.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"] == true;

        public BitmapImage GlyphBitmapImage { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Update(DeviceInformationUpdate deviceInfoUpdate)
        {
            DeviceInformation.Update(deviceInfoUpdate);
            OnPropertyChanged("Id");
            OnPropertyChanged("Name");
            OnPropertyChanged("DeviceInformation");
            OnPropertyChanged("IsPaired");
            OnPropertyChanged("IsConnected");
            OnPropertyChanged("Properties");
            OnPropertyChanged("IsConnectable");
        }
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}