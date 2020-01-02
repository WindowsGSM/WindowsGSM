using System.Threading.Tasks;
using System.Diagnostics;

namespace WindowsGSM.GameServer.Action
{
    class Restart
    {
        private readonly Functions.ServerTable server;
        private readonly string gslt = "";
        private readonly string additionalParam = "";
        public string Error = "";
        public string Notice = "";

        public Restart(Functions.ServerTable server, string gslt, string additionalParam)
        {
            this.server = server;
            this.gslt = gslt;
            this.additionalParam = additionalParam;
        }

        public async Task<Process> Run(Process process)
        {
            Stop actionStop = new Stop(server);
            await actionStop.Run(process);

            Start actionStart = new Start(server, gslt, additionalParam);
            return await actionStart.Run();
        }
    }
}
