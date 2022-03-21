using System.Net;
using System.Net.Sockets;
using WindowsGSM.Attributes;

namespace WindowsGSM.GameServers.Configs
{
    public class ProtocolConfig
    {
        [TextField(Label = "IP Address", Required = true)]
        public string IPAddress { get; set; } = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "127.0.0.1";

        [NumericField(Label = "Query Port", Required = true, Min = 0, Max = 65535)]
        public int QueryPort { get; set; } = 27015;
    }
}
