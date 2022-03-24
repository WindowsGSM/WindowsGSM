using System.Net;
using System.Net.Sockets;
using WindowsGSM.Attributes;

namespace WindowsGSM.GameServers.Configs
{
    public class ProtocolConfig
    {
        [TextField(Label = "IP Address", Required = true)]
        public string IPAddress { get; set; } = GetLocalIPAddress();

        [NumericField(Label = "Query Port", Required = true, Min = 0, Max = 65535)]
        public int QueryPort { get; set; }

        public static string GetLocalIPAddress()
        {
            using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, 0);

            try
            {
                socket.Connect("8.8.8.8", 65530);
            }
            catch
            {
                // Tested not accurate, may get wrong local address if VMware Network exists
                return Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "127.0.0.1";
            }

            return ((IPEndPoint?)socket.LocalEndPoint)?.Address.ToString() ?? "127.0.0.1";
        }
    }
}
