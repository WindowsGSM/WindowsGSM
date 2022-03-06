using WindowsGSM.Attributes;

namespace WindowsGSM.GameServers.Configs
{
    public class SourceModConfig
    {
        [TextField(Label = "Local Version", HelperText = "SourceMod Version (ReadOnly)", ReadOnly = true)]
        public string LocalVersion { get; set; } = string.Empty;
    }
}
