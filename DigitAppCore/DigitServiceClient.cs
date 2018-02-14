using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace DigitAppCore
{
    public class DigitServiceClient
    {
        private const string DeviceId = "12345";
        private string digitServiceClientUrl;
        public DigitServiceClient()
        {
            var configResources = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView("coreconfig");
            digitServiceClientUrl = configResources.GetString("DigitServiceClientUrl");
        }

        private HttpClient GetClient()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new HttpMediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Accept.Add(new HttpMediaTypeWithQualityHeaderValue("text/javascript"));
            return client;
        }

        public async Task<bool> LogAsync(string message, int code = 0)
        {
            var json = JsonConvert.SerializeObject(new LogEntry()
            {
                Code = code,
                Message = message,
                OccurenceTime = DateTime.Now
            });
            var client = GetClient();
            HttpStringContent content = new HttpStringContent(json, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
            HttpResponseMessage res = await client.PostAsync(new Uri($"{digitServiceClientUrl}/api/device/{DeviceId}/log"), content);
            return res.IsSuccessStatusCode;
        }
    }
}
