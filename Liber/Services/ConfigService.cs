using ComCat.Exceptions;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ComCat.Services
{
    public class ConfigService
    {
        [JsonInclude]
        public int MessageCacheSize
        {
            get { return (int)_messageCacheSize; }
            private set { _messageCacheSize = value; }
        }
        private int? _messageCacheSize;
        [JsonInclude]
        public string Prefix { get; private set; }
        [JsonInclude]
        public string Token { get; private set; }

        private void Validate()
        {
            if (_messageCacheSize == null)
                throw new MissingConfigException(nameof(MessageCacheSize));
            if (string.IsNullOrEmpty(Prefix))
                throw new MissingConfigException(nameof(Prefix));
            if (string.IsNullOrEmpty(Token))
                throw new MissingConfigException(nameof(Token));
        }

        public static async Task<ConfigService> GetAsync(string fileName)
        {
            using FileStream file = File.OpenRead(
                $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}{fileName}");
            var instance = await JsonSerializer.DeserializeAsync<ConfigService>(file);
            instance.Validate();
            return instance;
        }
    }
}
