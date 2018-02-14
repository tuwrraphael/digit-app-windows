namespace DigitAppCore
{
    public interface IDigitServiceClientConfig
    {
        OpenIdApiClientConfig OpenIdConfig { get; }
        string DigitServiceClientUrl { get; }
    }
}
