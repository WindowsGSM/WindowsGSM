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

        public bool TryGetPropertyInfo(string memberName, [NotNullWhen(true)] out PropertyInfo? tab)
        {
            tab = GetType().GetProperties().FirstOrDefault(x => x.Name.Equals(memberName));
            
            return tab != null;
        }
    }
}
