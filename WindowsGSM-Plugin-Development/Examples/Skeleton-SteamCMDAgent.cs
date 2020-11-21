using System;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Engine;
using WindowsGSM.GameServer.Query;

namespace WindowsGSM.Plugins
{
    public class Skeleton_SteamCMDAgent : SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "",
            author = "",
            description = "",
            version = "",
            url = "",
            color = "#ffffff"
        };


        // - Standard Constructor and properties
        public Skeleton_SteamCMDAgent(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;


        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => false;
        public override string AppId => "";


        // - Game server Fixed variables
        public override string StartPath => "";
        public string FullName = "";
        public bool AllowsEmbedConsole = false;
        public int PortIncrements = 1;
        public object QueryMethod = null;


        // - Game server default values
        public string Port = "";
        public string QueryPort = "";
        public string Defaultmap = "";
        public string Maxplayers = "";
        public string Additional = "";


        // - Create a default cfg for the game server after installation
        public async void CreateServerCFG()
        {

        }


        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {
            return null;
        }


        // - Stop server function
        public async Task Stop(Process p)
        {

        }
    }
}