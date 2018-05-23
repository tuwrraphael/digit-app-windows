using DigitService.Client;
using Microsoft.Extensions.Options;
using System;

namespace DigitAppCore
{
    public class DigitServiceBuilder
    {
        public static BearerTokenAuthenticationProvider AuthenticationProvider()
        {
            return new BearerTokenAuthenticationProvider(DigitServiceClientConfig.Default.AuthenticationOptions);
        }

        public static IDigitServiceClient Get()
        {
            var optionsAccessor = new OptionsWrapper<DigitServiceOptions>(new DigitServiceOptions()
            {
                DigitServiceBaseUri = new Uri(DigitServiceClientConfig.Default.DigitServiceClientUrl),
                LogAuthor = "digitApp"
            });
            var client = new DigitServiceClient(AuthenticationProvider(), optionsAccessor);
            return client;
        }
    }
}
