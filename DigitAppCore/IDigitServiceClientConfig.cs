namespace DigitAppCore
{
    public interface IDigitServiceClientConfig
    {
        BearerTokenAuthenticationProviderOptions AuthenticationOptions { get; }
        string DigitServiceClientUrl { get; }
    }
}
