using System;

namespace DigitAppCore
{
    public class StoredTokens
    {
        public DateTime? AccessTokenExpires { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        public StoredTokens()
        {
            AccessTokenExpires = null;
            AccessToken = null;
            RefreshToken = null;
        }

        public bool Expired => null == AccessToken || !AccessTokenExpires.HasValue || AccessTokenExpires.Value < DateTime.Now;
        public bool HasRefreshToken => null != RefreshToken;
    }
}
