using System.Threading.Tasks;

namespace WindowsGSM.GameServer
{
    class ZPS : Engine.Source
    {
        public const string FullName = "Zombie Panic Source Dedicated Server";
        public override string Defaultmap { get { return "zps_cinema"; } }
        public override string Game { get { return "zps"; } }
        public override string AppId { get { return "17505"; } }

        public ZPS(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }

        public async Task<string> GetServerDetailsAsync()
        {
            return await Task.Run(() =>
            {
                return $"Server Name: {serverData.ServerName}, IP: {serverData.ServerIP}, Port: {serverData.ServerPort}";
            });
        }
    }
}