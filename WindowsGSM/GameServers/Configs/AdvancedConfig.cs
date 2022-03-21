using System.Diagnostics;
using WindowsGSM.Attributes;
using WindowsGSM.Extensions;
using WindowsGSM.GameServers.Components;

namespace WindowsGSM.GameServers.Configs
{
    public class AdvancedConfig
    {
        [Select(Label = "Process Priority", HelperText = "Sets the priority class for the specified process.", SelectItemsType = typeof(ProcessPrioritySelectItem))]
        public string ProcessPriority { get; set; } = ProcessPriorityClass.Normal.ToStringEx();

        [TextField(Label = "Processor Affinity", HelperText = "Processor Affinity also called CPU pinning, allows the user to assign a process to use only a few cores.", Required = true, ProcessorAffinity = true)]
        public uint ProcessorAffinity { get; set; } = Utilities.ProcessorAffinity.Default;

        [CheckBox(Label = "Auto Start on Boot", HelperText = "Automatically start the server on boot.", IsSwitch = true)]
        public bool AutoStart { get; set; }

        [CheckBox(Label = "RestartOnCrash", HelperText = "Automatically restart the server when the server crashes unexpectedly.", IsSwitch = true)]
        public bool RestartOnCrash { get; set; }

        [CheckBox(Label = "Auto Update", HelperText = "Automatically update the server when update is available.", IsSwitch = true)]
        public bool AutoUpdate { get; set; }
    }
}
