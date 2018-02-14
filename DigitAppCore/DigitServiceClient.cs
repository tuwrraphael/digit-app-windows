using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace DigitAppCore
{
    public class DigitServiceClient
    {
        private const string DeviceId = "12345";
        private string digitServiceClientUrl;

        private OpenIdApiClient idClient;

        public DigitServiceClient() : this(DigitServiceClientConfig.Default)
        {

        }

        public DigitServiceClient(IDigitServiceClientConfig clientConfig)
        {
            idClient = new OpenIdApiClient(clientConfig.OpenIdConfig);
            digitServiceClientUrl = clientConfig.DigitServiceClientUrl;
        }

        public async Task<bool> LogAsync(string message, int code = 0)
        {
            var json = JsonConvert.SerializeObject(new LogEntry()
            {
                Code = code,
                Message = message,
                OccurenceTime = DateTime.Now,
                Author = "digitApp"
            });
            var client = idClient.DefaultClient();
            HttpStringContent content = new HttpStringContent(json, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
            HttpResponseMessage res = await client.PostAsync(new Uri($"{digitServiceClientUrl}/api/device/{DeviceId}/log"), content);
            return res.IsSuccessStatusCode;
        }

        public async Task SetupPushChannel(string uri)
        {
            var json = JsonConvert.SerializeObject(new PushChannelRegistration()
            {
                Uri = uri
            });
            var client = await idClient.GetAuthorizedClient();
            HttpStringContent content = new HttpStringContent(json, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
            HttpResponseMessage res = await client.PostAsync(new Uri($"{digitServiceClientUrl}/api/push"), content);
            var dat = await res.Content.ReadAsStringAsync();
            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedException("Not authorized to register push channel");
            }
            if (!res.IsSuccessStatusCode)
            {
                throw new DigitServiceException($"Push channel registration error {res.StatusCode}");
            }
        }

        public async Task<bool> HasValidAccessToken()
        {
            try
            {
                await idClient.GetAuthorizedClient();
            }
            catch (UnauthorizedException)
            {
                return false;
            }
            return true;
        }

        public async Task Authenticate()
        {
            await idClient.AuthenticateClient();
        }
    }
}
