using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace DigitAppCore
{
    public class DigitBLEOptions
    {
        public string DeviceId { get; private set; }

        public bool IsConfigured  => null != DeviceId;

        public void StoreDeviceId(string id)
        {
            DeviceId = id;
            ApplicationData.Current.LocalSettings.Values["DigitId"] = id;
        }

        public DigitBLEOptions()
        {
            DeviceId = ApplicationData.Current.LocalSettings.Values["DigitId"] as string;
        }
    }
}
