using System;
using System.Threading.Tasks;

namespace DigitAppCore
{
    public class BatteryService
    {
        private readonly DigitBLEClient digitBLEClient;
        private readonly DigitServiceClient digitServiceClient;

        public BatteryService(DigitBLEClient digitBLEClient, DigitServiceClient digitServiceClient)
        {
            this.digitBLEClient = digitBLEClient;
            this.digitServiceClient = digitServiceClient;
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
                        await digitServiceClient.PostBatteryMeasurement("12345", measurement);
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
