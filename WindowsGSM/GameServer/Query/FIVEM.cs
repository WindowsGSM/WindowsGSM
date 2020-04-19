using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WindowsGSM.GameServer.Query
{
    class FIVEM
    {
        private static readonly byte[] FIVEM_INFO = Encoding.Default.GetBytes("getinfo windowsgsm");

        private UdpClient _udpClient;
        private IPEndPoint _IPEndPoint;
        private int _timeout;

        public FIVEM() { }

        public FIVEM(string address, int port, int timeout = 5)
        {
            SetAddressPort(address, port, timeout);
        }

        public void SetAddressPort(string address, int port, int timeout = 5)
        {
            _IPEndPoint = new IPEndPoint(IPAddress.Parse(address), port);
            _timeout = timeout;
        }

        /// <summary>Get general information of specific game server.</summary>
        public async Task<Dictionary<string, string>> GetInfo()
        {
            return await Task.Run(() =>
            {
                try
                {
                    _udpClient = new UdpClient();
                    _udpClient.Client.SendTimeout = _udpClient.Client.ReceiveTimeout = _timeout * 1000;
                    _udpClient.Connect(_IPEndPoint);

                    // Send FIVEM_INFO request
                    byte[] request = new byte[0].Concat(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }).Concat(FIVEM_INFO).ToArray();
                    _udpClient.Send(request, request.Length);

                    // Receive response (Skip "\FF\FF\FF\FFinfoResponse\n\\")
                    byte[] response = _udpClient.Receive(ref _IPEndPoint).Skip(18).ToArray();
                    string[] splits = Encoding.UTF8.GetString(response).Split('\\');

                    // Store response's data
                    var keys = new Dictionary<string, string>();
                    for (int i = 0; i < splits.Length; i += 2)
                    {
                        keys.Add(splits[i], splits[i + 1]);
                    }

                    return keys.Count <= 0 ? null : keys;
                }
                catch
                {
                    return null;
                }
            });
        }

        public async Task<string> GetPlayersAndMaxPlayers()
        {
            try
            {
                Dictionary<string, string> kv = await GetInfo();
                return kv["clients"] + '/' + kv["sv_maxclients"];
            }
            catch
            {
                return null;
            }
        }
    }
}
