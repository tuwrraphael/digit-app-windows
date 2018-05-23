﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Authentication.Web;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace DigitAppCore
{
    public partial class OpenIdApiClient
    {
        private readonly string authorizationUrl;
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string scopes;
        private readonly string redirectUri;
        private readonly ITokenStorage tokenStorage;

        public OpenIdApiClient(OpenIdApiClientConfig config)
        : this(config.Endpoint, config.ClientId, config.ClientSecret, config.Scopes, config.RedirectUrl, new TokenStorage(config.Name)) { }

        public OpenIdApiClient(string authorizationUrl, string clientId, string clientSecret, string scopes, string redirectUri, ITokenStorage tokenStorage)
        {
            this.authorizationUrl = authorizationUrl;
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.scopes = scopes;
            this.redirectUri = redirectUri;
            this.tokenStorage = tokenStorage;
        }

        private static SemaphoreSlim accessTokenSem = new SemaphoreSlim(1);

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
            var tokens = await tokenStorage.Get();
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
                        await tokenStorage.Store(tokens);
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
                        await tokenStorage.Store(tokens);
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
