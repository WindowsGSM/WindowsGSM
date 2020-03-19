using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WindowsGSM.GameServer.Query
{
    class A2S
    {
        private static readonly byte[] A2S_INFO = Encoding.Default.GetBytes("TSource Engine Query");
        private static readonly byte[] A2S_PLAYER = Encoding.Default.GetBytes("U");
        private static readonly byte[] A2S_RULES = Encoding.Default.GetBytes("V");

        private const byte SOURCE_RESPONSE = 0x49;
        private const byte GOLDSOURCE_RESPONSE = 0x6D;

        private UdpClient _udpClient;
        private IPEndPoint _IPEndPoint;
        private int _timeout;

        public A2S() { }

        public A2S(string address, int port, int timeout = 5)
        {
            SetAddressPort(address, port, timeout);
        }

        public void SetAddressPort(string address, int port, int timeout = 5)
        {
            _IPEndPoint = new IPEndPoint(IPAddress.Parse(address), port);
            _timeout = timeout;
        }

        /// <summary>Retrieves information about the server including, but not limited to: its name, the map currently being played, and the number of players.</summary>
        /// <returns>Returns (key, value)</returns>
        public async Task<Dictionary<string, string>> GetInfo()
        {
            return await Task.Run(() =>
            {
                try
                {
                    _udpClient = new UdpClient();
                    _udpClient.Client.SendTimeout = _udpClient.Client.ReceiveTimeout = _timeout * 1000;
                    _udpClient.Connect(_IPEndPoint);

                    // Send A2S_INFO request
                    byte[] request = new byte[0].Concat(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }).Concat(A2S_INFO).Concat(new byte[] { 0x00 }).ToArray();
                    _udpClient.Send(request, request.Length);

                    // Receive response
                    byte[] response = _udpClient.Receive(ref _IPEndPoint).Skip(4).ToArray();

                    // Store response's data
                    var keys = new Dictionary<string, string>();

                    // Load response's data
                    using (var br = new BinaryReader(new MemoryStream(response), Encoding.UTF8))
                    {
                        byte header = br.ReadByte();

                        if (header == SOURCE_RESPONSE)
                        {
                            keys["Header"] = header.ToString();
                            keys["Protocol"] = br.ReadByte().ToString();
                            keys["Name"] = ReadString(br);
                            keys["Map"] = ReadString(br);
                            keys["Folder"] = ReadString(br);
                            keys["Game"] = ReadString(br);
                            keys["ID"] = br.ReadInt16().ToString();
                            keys["Players"] = br.ReadByte().ToString();
                            keys["MaxPlayers"] = br.ReadByte().ToString();
                            keys["Bots"] = br.ReadByte().ToString();
                            char c = br.ReadChar();
                            keys["ServerType"] = c == 'd' ? "Dedicated" : c == 'l' ? "Listen" : "SourceTV";
                            c = br.ReadChar();
                            keys["Environment"] = c == 'w' ? "Windows" : c == 'l' ? "Linux" : "Mac";
                            keys["Visibility"] = br.ReadBoolean() ? "Private" : "Public";
                            keys["VAC"] = br.ReadBoolean() ? "Secured" : "Unsecured";

                            if (int.Parse(keys["ID"]) == 2400) // The Ship
                            {
                                keys["Mode"] = br.ReadByte().ToString();
                                keys["Witnesses"] = br.ReadByte().ToString();
                                keys["Duration"] = br.ReadByte().ToString();
                            }

                            keys["Version"] = ReadString(br);

                            var edf = br.ReadByte();
                            if ((edf & 0x80) == 1) { keys["Port"] = br.ReadInt16().ToString(); }
                            if ((edf & 0x10) == 1) { keys["SteamID"] = br.ReadUInt64().ToString(); }
                            if ((edf & 0x40) == 1)
                            {
                                keys["SpectatorPort"] = br.ReadInt16().ToString();
                                keys["SpectatorName"] = ReadString(br);
                            }
                            if ((edf & 0x20) == 1) { keys["Keywords"] = ReadString(br); }
                            if ((edf & 0x01) == 1) { keys["GameID"] = br.ReadUInt64().ToString(); }
                        }
                        else if (header == GOLDSOURCE_RESPONSE)
                        {
                            keys["Header"] = header.ToString();
                            keys["Address"] = ReadString(br);
                            keys["Name"] = ReadString(br);
                            keys["Map"] = ReadString(br);
                            keys["Folder"] = ReadString(br);
                            keys["Game"] = ReadString(br);
                            keys["Address"] = ReadString(br);
                            keys["Players"] = br.ReadByte().ToString();
                            keys["MaxPlayers"] = br.ReadByte().ToString();
                            keys["Protocol"] = br.ReadByte().ToString();
                            char c = char.ToLower(br.ReadChar());
                            keys["ServerType"] = c == 'd' ? "Dedicated" : c == 'l' ? "Listen" : "HLTV";
                            c = br.ReadChar();
                            keys["Environment"] = c == 'w' ? "Windows" : c == 'l' ? "Linux" : "Mac";
                            keys["Visibility"] = br.ReadBoolean() ? "Private" : "Public";
                            keys["Mod"] = br.ReadBoolean().ToString();

                            if (bool.Parse(keys["Mod"]))
                            {
                                keys["Link"] = ReadString(br);
                                keys["DownloadLink"] = ReadString(br);
                                br.ReadByte();
                                keys["Version"] = br.ReadInt64().ToString();
                                keys["Size"] = br.ReadInt64().ToString();
                                keys["Type"] = br.ReadByte().ToString();
                                keys["DLL"] = br.ReadByte().ToString();
                            }

                            keys["VAC"] = br.ReadBoolean() ? "Secured" : "Unsecured";
                            keys["Bots"] = br.ReadByte().ToString();
                        }
                    }

                    return keys.Count <= 0 ? null : keys;
                }
                catch
                {
                    return null;
                }
            });
        }

        /// <summary>Retrieves information about the players currently on the server.</summary>
        /// <returns>Returns (id, (name, score, timeConnected))</returns>
        public async Task<Dictionary<int, (string, long, TimeSpan)>> GetPlayer()
        {
            return await Task.Run(() =>
            {
                try
                {
                    _udpClient = new UdpClient();
                    _udpClient.Client.SendTimeout = _udpClient.Client.ReceiveTimeout = _timeout * 1000;
                    _udpClient.Connect(_IPEndPoint);

                    // Send A2S_PLAYER request
                    byte[] request = new byte[0].Concat(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }).Concat(A2S_PLAYER).Concat(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }).ToArray();
                    _udpClient.Send(request, request.Length);

                    // Receive response
                    byte[] response = _udpClient.Receive(ref _IPEndPoint).Skip(5).ToArray();

                    // Send A2S_PLAYER request with challenge
                    request = new byte[0].Concat(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }).Concat(A2S_PLAYER).Concat(response).ToArray();
                    _udpClient.Send(request, request.Length);

                    // Receive response
                    response = _udpClient.Receive(ref _IPEndPoint).Skip(4).ToArray();

                    // Store response's data
                    var keys = new Dictionary<int, (string, long, TimeSpan)>();

                    // Load response's data
                    using (var br = new BinaryReader(new MemoryStream(response), Encoding.UTF8))
                    {
                        br.ReadByte(); // Header
                        int players = br.ReadByte();

                        for (int i = 0; i < players; i++)
                        {
                            br.ReadByte(); // index
                            string name = ReadString(br);
                            int score = br.ReadInt32();
                            TimeSpan timeConnected = TimeSpan.FromSeconds((int)br.ReadSingle());

                            keys[i] = (name, score, timeConnected);
                        }
                    }

                    return keys;
                }
                catch
                {
                    return null;
                }
            });
        }

        private string ReadString(BinaryReader br)
        {
            byte[] bytes = new byte[0];

            // Get all bytes until 0x00
            do
            {
                bytes = bytes.Concat(new byte[] { br.ReadByte() }).ToArray();
            }
            while (bytes[bytes.Length - 1] != 0x00);

            // Return bytes in UTF8 except the last byte because it is 0x00
            return Encoding.UTF8.GetString(bytes.Take(bytes.Length - 1).ToArray());
        }
    }
}
