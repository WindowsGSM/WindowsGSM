using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using WindowsGSM.Services;

namespace WindowsGSM.Games
{
    public interface IConfig
    {
        public string LocalVersion { get; set; }

        public string ClassName { get; }
        
        public Guid Guid { get; set; }

        public BasicConfig Basic { get; set; }

        public void Update()
        {
            string path = Path.Combine(GameServerService.ConfigsPath, $"{Guid}.json");
            string contents = JsonSerializer.Serialize<dynamic>(this, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(path, contents);
        }

        public void Delete()
        {
            string path = Path.Combine(GameServerService.ConfigsPath, $"{Guid}.json");

            File.Delete(path);
        }

        public bool TryGetPropertyInfo(string memberName, [NotNullWhen(true)] out PropertyInfo? tab)
        {
            tab = GetType().GetProperties().FirstOrDefault(x => x.Name.Equals(memberName));
            
            return tab != null;
        }
    }
}
