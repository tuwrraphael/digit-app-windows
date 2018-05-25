using DigitService.Client;
using DigitService.Models;
using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace DigitAppCore
{
    public class LocationService
    {
        private readonly IDigitServiceClient client;

        public LocationService(IDigitServiceClient client)
        {
            this.client = client;
        }

        public async Task SendCurrentLocation()
        {
            try
            {
                var accessStatus = await Geolocator.RequestAccessAsync();
                if (accessStatus != GeolocationAccessStatus.Allowed)
                {
                    await client.Location.NotifyErrorAsync(new LocationConfigurationError()
                    {
                        Denied = true,
                        Disabled = false
                    });
                    return;
                }
                var locator = new Geolocator();
                if (locator.LocationStatus == PositionStatus.Disabled)
                {
                    await client.Location.NotifyErrorAsync(new LocationConfigurationError()
                    {
                        Denied = false,
                        Disabled = true
                    });
                }
                var res = await locator.GetGeopositionAsync(new TimeSpan(0, 10, 0), new TimeSpan(0, 0, 23));
                await client.Location.AddLocationAsync(new Location()
                {
                    Latitude = res.Coordinate.Point.Position.Latitude,
                    Longitude = res.Coordinate.Point.Position.Longitude,
                    Accuracy = res.Coordinate.Accuracy,
                    Timestamp = res.Coordinate.Timestamp.UtcDateTime
                });
            }
            catch (DigitServiceException e)
            {
                await client.LogAsync($"Could not update location information: {e.Message}", 3);
            }
        }
    }
}
