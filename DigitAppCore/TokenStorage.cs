using System.Threading.Tasks;
using Windows.Storage;
using System;
using Newtonsoft.Json;

namespace DigitAppCore
{
    public class TokenStorage : ITokenStorage
    {
        private string name;

        public TokenStorage(string name)
        {
            this.name = name;
        }

        public async Task<StoredTokens> Get()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            try
            {
                StorageFile tokenFile = await localFolder.GetFileAsync($"{name}Tokens.json");
                var str = await FileIO.ReadTextAsync(tokenFile);
                return JsonConvert.DeserializeObject<StoredTokens>(str);
            }
            catch (Exception)
            {
                return new StoredTokens();
            }
        }

        public async Task Store(StoredTokens tokens)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile tokenFile = await localFolder.CreateFileAsync($"{name}Tokens.json",
                CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(tokenFile, JsonConvert.SerializeObject(tokens));
        }
    }
}