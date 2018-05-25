using DigitService.Client;
using DigitService.Models;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace DigitAppCore
{
    public class BatteryService
    {
        private readonly DigitBLEClient digitBLEClient;
        private readonly IDigitServiceClient digitServiceClient;
        private const int EveryNthTime = 3;
        private const string BatteryMeasurementTimerKey = "SendBattery";

        public BatteryService(DigitBLEClient digitBLEClient, IDigitServiceClient digitServiceClient)
        {
            this.digitBLEClient = digitBLEClient;
            this.digitServiceClient = digitServiceClient;
        }

        public async Task<bool> TimeTriggeredMeasurement()
        {
            var stored = (ApplicationData.Current.LocalSettings.Values[BatteryMeasurementTimerKey] as int?);
            var times = stored.HasValue ? (stored.Value - 1) : EveryNthTime;
            bool measurementSuccessful = false;
            if (times == 0)
            {
                if (measurementSuccessful = await AddBatteryMeasurement())
                {
                    times = EveryNthTime;
                }
                else
                {
                    times = 1;
                }

            }
            ApplicationData.Current.LocalSettings.Values[BatteryMeasurementTimerKey] = times;
            return measurementSuccessful;
        }

        public async Task<bool> AddBatteryMeasurement()
        {
            try
            {
                var batt = await digitBLEClient.ReadBatteryAsync();
                if (!batt.HasValue)
                {
                    await digitServiceClient.LogAsync($"Device was not reachable to read battery");
                }
                else
                {
                    var value = (batt.Value / 100.0) * 1023;
                    var measurement = new BatteryMeasurement()
                    {
                        MeasurementTime = DateTime.Now,
                        RawValue = (uint)value
                    };
                    try
                    {
                        await digitServiceClient.Device["12345"].Battery.AddMeasurementAsync(measurement);
                        ApplicationData.Current.LocalSettings.Values[BatteryMeasurementTimerKey] = EveryNthTime;
                        return true;
                    }
                    catch (DigitServiceException e)
                    {
                        await digitServiceClient.LogAsync($"Could not post battery value {batt.Value}: {e.Message}", 3);
                    }
                }
            }
            catch (DigitBLEExpcetion ex)
            {
                await digitServiceClient.LogAsync($"Could not read battery: {ex.Message}", 3);
            }
            return false;
        }
    }
}
