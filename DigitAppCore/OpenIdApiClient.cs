using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Authentication.Web;
using Windows.Storage;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace DigitAppCore
{
    public class OpenIdApiClient
    {
        private readonly string authorizationUrl;
        private readonly string name;
        private readonly ApplicationDataContainer localSettings;
        private string accessToken;
        private string refreshToken;
        private DateTime? accessTokenExpiration;
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string scopes;
        private readonly string redirectUri;

        private string AccessTokenKey => $"{name}ApiAccessToken";
        private string RefreshTokenKey => $"{name}ApiRefreshToken";
        private string AccessTokenExpiresKey => $"{name}ApiAccessTokenExpiration";

        public OpenIdApiClient(OpenIdApiClientConfig config)
        : this(config.Endpoint, config.Name, config.ClientId, config.ClientSecret, config.Scopes, config.RedirectUrl) { }

        public OpenIdApiClient(string authorizationUrl, string name, string clientId, string clientSecret, string scopes, string redirectUri)
        {
            this.authorizationUrl = authorizationUrl;
            this.name = name;
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.scopes = scopes;
            this.redirectUri = redirectUri;
            localSettings = ApplicationData.Current.LocalSettings;
            accessToken = (localSettings.Values[AccessTokenKey] as string);
            refreshToken = (localSettings.Values[RefreshTokenKey] as string);
            if (localSettings.Values[AccessTokenExpiresKey] is string expiration
                && DateTime.TryParse(expiration, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime res))
            {
                accessTokenExpiration = res;
            }
        }

        public HttpClient DefaultClient()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new HttpMediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Accept.Add(new HttpMediaTypeWithQualityHeaderValue("text/javascript"));
            return client;
        }

        public async Task<HttpClient> GetAuthorizedClient()
        {
            var client = DefaultClient();
            if (null != accessToken && accessTokenExpiration.HasValue && accessTokenExpiration.Value > DateTime.Now)
            {
                client.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue("Bearer", accessToken);
                return client;
            }
            else if (null != refreshToken)
            {
                var dict = new Dictionary<string, string>
                {
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "grant_type", "refresh_token" },
                    { "refresh_token", refreshToken }
                };
                HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(dict);
                HttpResponseMessage res = await client.PostAsync(new Uri($"{authorizationUrl}/connect/token"), content);
                if (res.IsSuccessStatusCode)
                {
                    var str = await res.Content.ReadAsStringAsync();
                    var token = JsonConvert.DeserializeObject<TokenResponse>(str);
                    localSettings.Values[AccessTokenKey] = token.access_token;
                    var expiration = DateTime.Now.AddSeconds(token.expires_in);
                    localSettings.Values[AccessTokenExpiresKey] = expiration;
                    accessToken = token.access_token;
                    accessTokenExpiration = expiration;
                    client.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue("Bearer", accessToken);
                    return client;
                }
                else
                {
                    throw new UnauthorizedException("Could not redeem refresh token.");
                }
            }
            throw new UnauthorizedException("No valid access or refresh token configured.");
        }

        public async Task AuthenticateClient()
        {
            var state = Guid.NewGuid().ToString();
            var nonce = Guid.NewGuid().ToString();
            var webAuthenticationResult =
              await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None,
              new Uri($"{authorizationUrl}/connect/authorize?client_id={clientId}&scope={scopes} openid offline_access&response_type=code id_token&" +
              $"redirect_uri={redirectUri}&state={state}&nonce={nonce}"),
              new Uri(redirectUri));

            if (webAuthenticationResult.ResponseStatus == WebAuthenticationStatus.Success)
            {
                var data = webAuthenticationResult.ResponseData;
                var parms = new WwwFormUrlDecoder(new Uri(data.Replace("#", "?")).Query);
                if (state != parms.Where(p => p.Name == "state").Single().Value)
                {
                    throw new UnauthorizedException("State differs");
                }
                var code = parms.Where(p => p.Name == "code").Single().Value;
                var client = DefaultClient();
                var dict = new Dictionary<string, string>();
                dict.Add("client_id", clientId);
                dict.Add("client_secret", clientSecret);
                dict.Add("grant_type", "authorization_code");
                dict.Add("code", code);
                dict.Add("redirect_uri", redirectUri);
                HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(dict);
                HttpResponseMessage res = await client.PostAsync(new Uri($"{authorizationUrl}/connect/token"), content);
                if (res.IsSuccessStatusCode)
                {
                    var str = await res.Content.ReadAsStringAsync();
                    var token = JsonConvert.DeserializeObject<TokenResponse>(str);
                    localSettings.Values[AccessTokenKey] = token.access_token;
                    var expiration = DateTime.Now.AddSeconds(token.expires_in);
                    localSettings.Values[AccessTokenExpiresKey] = expiration.ToString(CultureInfo.InvariantCulture);
                    localSettings.Values[RefreshTokenKey] = token.refresh_token;
                    accessToken = token.access_token;
                    refreshToken = token.refresh_token;
                    accessTokenExpiration = expiration;
                }
                else
                {
                    throw new UnauthorizedException("Could not redeem refresh token.");
                }
            }
            else
            {
                throw new UnauthorizedException("Browser authentication was not successful");
            }
        }
    }
}
