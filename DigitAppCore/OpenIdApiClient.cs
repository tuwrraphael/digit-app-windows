using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
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

        }

        private class StoredTokens
        {
            public DateTime? AccessTokenExpires { get; set; }
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }

            public bool Expired => null == AccessToken || !AccessTokenExpires.HasValue || AccessTokenExpires.Value < DateTime.Now;
            public bool HasRefreshToken => null != RefreshToken;
        }

        private static SemaphoreSlim accessTokenSem = new SemaphoreSlim(1);

        private StoredTokens Tokens
        {
            get
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                var tokens = new StoredTokens()
                {
                    AccessToken = (localSettings.Values[AccessTokenKey] as string),
                    RefreshToken = (localSettings.Values[RefreshTokenKey] as string)
                };
                if (localSettings.Values[AccessTokenExpiresKey] is string expiration
                    && DateTime.TryParse(expiration, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime res))
                {
                    tokens.AccessTokenExpires = res;
                }
                return tokens;
            }
            set
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[AccessTokenKey] = value.AccessToken;
                if (value.AccessTokenExpires.HasValue)
                {
                    localSettings.Values[AccessTokenExpiresKey] = value.AccessTokenExpires.Value.ToString(CultureInfo.InvariantCulture);
                }
                localSettings.Values[RefreshTokenKey] = value.RefreshToken;
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
            await accessTokenSem.WaitAsync();
            var tokens = Tokens;
            if (!tokens.Expired)
            {
                client.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue("Bearer", tokens.AccessToken);
                accessTokenSem.Release();
                return client;
            }
            else if (tokens.HasRefreshToken)
            {
                try
                {
                    var dict = new Dictionary<string, string>
                {
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "grant_type", "refresh_token" },
                    { "refresh_token", tokens.RefreshToken }
                };
                    HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(dict);
                    HttpResponseMessage res = await client.PostAsync(new Uri($"{authorizationUrl}/connect/token"), content);
                    if (res.IsSuccessStatusCode)
                    {
                        var str = await res.Content.ReadAsStringAsync();
                        var token = JsonConvert.DeserializeObject<TokenResponse>(str);
                        tokens = new StoredTokens()
                        {
                            AccessToken = token.access_token,
                            RefreshToken = token.refresh_token,
                            AccessTokenExpires = DateTime.Now.AddSeconds(token.expires_in)
                        };
                        Tokens = tokens;
                        client.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue("Bearer", tokens.AccessToken);
                        return client;
                    }
                    else
                    {
                        throw new UnauthorizedException("Could not redeem refresh token.");
                    }
                }
                finally
                {
                    accessTokenSem.Release();
                }
            }
            else
            {
                accessTokenSem.Release();
                throw new UnauthorizedException("No valid access or refresh token configured.");
            }
        }

        public async Task AuthenticateClient()
        {
            var state = Guid.NewGuid().ToString();
            var nonce = Guid.NewGuid().ToString();
            await accessTokenSem.WaitAsync();
            try
            {
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
                        var tokens = new StoredTokens()
                        {
                            AccessToken = token.access_token,
                            RefreshToken = token.refresh_token,
                            AccessTokenExpires = DateTime.Now.AddSeconds(token.expires_in)
                        };
                        Tokens = tokens;
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
            finally
            {
                accessTokenSem.Release();
            }
        }
    }
}
