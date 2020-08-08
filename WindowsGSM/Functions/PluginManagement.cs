using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WindowsGSM.Functions
{
    public class PluginMetadata
    {
        public bool IsLoaded;
        public string GameImage, AuthorImage, FullName, FileName, Error;
        public Plugin Plugin;
        public Type Type;
    }

    class PluginManagement
    {
        public const string DefaultUserImage = "pack://application:,,,/Images/Plugins/User.png";
        public const string DefaultPluginImage = "pack://application:,,,/Images/WindowsGSM.png";

        public PluginManagement()
        {
            Directory.CreateDirectory(ServerPath.GetPlugins());
        }

        public async Task<List<PluginMetadata>> LoadPlugins()
        {
            var plugins = new List<PluginMetadata>();
            foreach (var pluginFolder in Directory.GetDirectories(ServerPath.GetPlugins(), "*.cs", SearchOption.TopDirectoryOnly).ToList())
            {
                var pluginFile = Path.Combine(pluginFolder, Path.GetFileName(pluginFolder));
                if (File.Exists(pluginFile))
                {
                    var plugin = await LoadPlugin(pluginFile);
                    if (plugin != null)
                    {
                        plugins.Add(plugin);
                    }
                }
            }
            
            return plugins;
        }

        public async Task<PluginMetadata> LoadPlugin(string path)
        {
            var pluginMetadata = new PluginMetadata
            {
                FileName = Path.GetFileName(path)
            };

            var options = new CompilerParameters();
            options.ReferencedAssemblies.Add(Assembly.GetEntryAssembly().Location);
            options.ReferencedAssemblies.Add("System.dll");
            options.GenerateInMemory = true;

            var c = new Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider();
            var cr = c.CompileAssemblyFromSource(options, File.ReadAllText(path));
            if (cr.Errors.HasErrors)
            {
                var sb = new StringBuilder();
                foreach (CompilerError err in cr.Errors)
                {
                    sb.Append($"{err.ErrorText}\nLine: {err.Line} - Column: {err.Column}\n\n");
                }
                pluginMetadata.Error = sb.ToString();
                Console.WriteLine(pluginMetadata.Error);
                return pluginMetadata;
            }

            try
            {
                pluginMetadata.Type = cr.CompiledAssembly.GetType($"WindowsGSM.Plugins.{Path.GetFileNameWithoutExtension(path)}");
                var plugin = GetPluginClass(pluginMetadata);
                pluginMetadata.FullName = $"{plugin.FullName} [{pluginMetadata.FileName}]";
                pluginMetadata.Plugin = plugin.Plugin;
                try
                {
                    string gameImage = ServerPath.GetPlugins(pluginMetadata.FileName, $"{Path.GetFileNameWithoutExtension(pluginMetadata.FileName)}.png");
                    ImageSource image = new BitmapImage(new Uri(gameImage));
                    pluginMetadata.GameImage = gameImage;
                }
                catch
                {
                    pluginMetadata.GameImage = DefaultPluginImage;
                }
                try
                {
                    string authorImage = ServerPath.GetPlugins(pluginMetadata.FileName, "author.png");
                    ImageSource image = new BitmapImage(new Uri(authorImage));
                    pluginMetadata.AuthorImage = authorImage;
                }
                catch
                {
                    pluginMetadata.AuthorImage = DefaultUserImage;
                }
                pluginMetadata.IsLoaded = true;
            }
            catch (Exception e)
            {
                pluginMetadata.Error = e.Message;
                Console.WriteLine(pluginMetadata.Error);
                pluginMetadata.IsLoaded = false;
            }

            return pluginMetadata;
        }

        public static BitmapSource GetDefaultUserBitmapSource()
        {
            using (var stream = System.Windows.Application.GetResourceStream(new Uri(DefaultUserImage)).Stream)
            {
                return BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
        }

        public static BitmapSource GetDefaultPluginBitmapSource()
        {
            using (var stream = System.Windows.Application.GetResourceStream(new Uri(DefaultPluginImage)).Stream)
            {
                return BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
        }

        public static dynamic GetPluginClass(PluginMetadata plugin, ServerConfig serverConfig = null) => Activator.CreateInstance(plugin.Type, serverConfig);
    }
}
