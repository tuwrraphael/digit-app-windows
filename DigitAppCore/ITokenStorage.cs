using System.Threading.Tasks;

namespace DigitAppCore
{
    public interface ITokenStorage
    {
        Task Store(StoredTokens tokens);

        Task<StoredTokens> Get();
    }
}