using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Query;
using Newtonsoft.Json.Linq;

namespace WindowsGSM.Plugins
{
    public class Skeleton
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
        public Skeleton(ServerConfig serverData) => _serverData = serverData;
        private readonly ServerConfig _serverData;
        public string Error, Notice;


        // - Game server Fixed variables
        public string StartPath = "";
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


        // - Install server function
        public async Task<Process> Install()
        {
            return null;
        }


        // - Update server function
        public async Task<Process> Update()
        {
            return null;
        }


        // - Check if the installation is successful
        public bool IsInstallValid()
        {
            return false;
        }


        // - Check if the directory contains paper.jar for import
        public bool IsImportValid(string path)
        {
            return false;
        }


        // - Get Local server version
        public string GetLocalBuild()
        {
            return "";
        }


        // - Get Latest server version
        public async Task<string> GetRemoteBuild()
        {
            return "";
        }
    }
}