using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using WindowsGSM.Services;
using WindowsGSM.Utilities;

namespace WindowsGSM.GameServers.Configs
{
    public interface IConfig
    {
        public string LocalVersion { get; set; }

        public string ClassName { get; }
        
        public Guid Guid { get; set; }

        public BasicConfig Basic { get; set; }

        public AdvancedConfig Advanced { get; set; }

        public BackupConfig Backup { get; set; }

        public bool Exists()
        {
            string path = Path.Combine(GameServerService.ConfigsPath, $"{Guid}.json");
           
            return File.Exists(path);
        }

        public Task Update()
        {
            string path = Path.Combine(GameServerService.ConfigsPath, $"{Guid}.json");
            string contents = JsonSerializer.Serialize<dynamic>(this, new JsonSerializerOptions { WriteIndented = true });

            return File.WriteAllTextAsync(path, contents);
        }

        public Task Delete()
        {
            string path = Path.Combine(GameServerService.ConfigsPath, $"{Guid}.json");

            return FileEx.DeleteAsync(path);
        }

        public async Task<IConfig> Clone()
        {
            string path = Path.Combine(GameServerService.ConfigsPath, $"{Guid}.json");

            return (IConfig)JsonSerializer.Deserialize(await File.ReadAllTextAsync(path), GetType())!;
        }

        public bool TryGetPropertyInfo(string memberName, [NotNullWhen(true)] out PropertyInfo? tab)
        {
            tab = GetType().GetProperties().FirstOrDefault(x => x.Name.Equals(memberName));
            
            return tab != null;
        }
    }
}
