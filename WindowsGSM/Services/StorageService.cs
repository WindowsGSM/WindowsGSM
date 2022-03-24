using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace WindowsGSM.Services
{
    public static class StorageService
    {
        public static readonly string StoragePath = Path.Combine(GameServerService.BasePath, "storage");

        public static bool TryGetItem<T>(string key, [NotNullWhen(true)] out T? data)
        {
            string path = Path.Combine(StoragePath, $"{key}.json");

            data = File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path)) : default;

            return data != null;
        }

        public static void SetItem(string key, object data)
        {
            string path = Path.Combine(StoragePath, $"{key}.json");
            string contents = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

            Directory.CreateDirectory(StoragePath);
            File.WriteAllText(path, contents);
        }
    }
}
