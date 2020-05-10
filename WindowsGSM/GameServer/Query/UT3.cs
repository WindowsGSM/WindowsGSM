using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WindowsGSM.Functions;

namespace WindowsGSM.GameServer.Query
{
    class UT3
    {
        private static readonly byte[] UT3_MAGIC = { 0xFE, 0xFD };
        private static readonly byte[] UT3_HANDSHAKE = { 0x09 };
        private static readonly byte[] UT3_INFO = { 0x00 };
        private static readonly byte[] UT3_SESSIONID = { 0x10, 0x20, 0x30, 0x40 };

        private IPEndPoint _IPEndPoint;
        private int _timeout;

        public UT3() { }

        public UT3(string address, int port, int timeout = 5)
        {
            SetAddressPort(address, port, timeout);
        }

        public void SetAddressPort(string address, int port, int timeout = 5)
        {
            _IPEndPoint = new IPEndPoint(IPAddress.Parse(address), port);
            _timeout = timeout * 1000;
        }

        public async Task<Dictionary<string, string>> GetInfo()
        {
            return await Task.Run(() =>
            {
                try
                {
                    byte[] requestData;
                    byte[] responseData;
                    using (UdpClientHandler udpHandler = new UdpClientHandler(_IPEndPoint))
                    {
                        // Send UT3_HANDSHAKE request
                        requestData = UT3_MAGIC
                            .Concat(UT3_HANDSHAKE)
                            .Concat(UT3_SESSIONID)
                            .ToArray();

                        // Receive response
                        byte[] token = GetToken(udpHandler.GetResponse(requestData, requestData.Length, _timeout, _timeout).ToArray());

                        // Send UT3_INFO request
                        requestData = UT3_MAGIC
                            .Concat(UT3_INFO)
                            .Concat(UT3_SESSIONID)
                            .Concat(token)
                            .ToArray();

                        // Receive response
                        responseData = udpHandler.GetResponse(requestData, requestData.Length, _timeout, _timeout)
                            .Skip(5)
                            .ToArray();
                    }

                    var keys = new Dictionary<string, string>();
                    using (var br = new BinaryReader(new MemoryStream(responseData), Encoding.UTF8))
                    {
                        keys["MOTD"] = ReadString(br);
                        keys["GameType"] = ReadString(br);
                        keys["Map"] = ReadString(br);
                        keys["Players"] = ReadString(br);
                        keys["MaxPlayers"] = ReadString(br);
                        keys["Port"] = br.ReadInt16().ToString();
                        keys["IP"] = ReadString(br);
                    }

                    return keys.Count <= 0 ? null : keys;
                }
                catch
                {
                    return null;
                }
            });
        }

        private byte[] GetToken(byte[] response)
        {
            int challenge = int.Parse(Encoding.ASCII.GetString(response.Skip(5).ToArray()));
            return new[] { (byte)(challenge >> 24 & 0xFF), (byte)(challenge >> 16 & 0xFF), (byte)(challenge >> 8 & 0xFF), (byte)(challenge >> 0 & 0xFF) };
        }

        private string ReadString(BinaryReader br)
        {
            byte[] bytes = new byte[0];

            // Get all bytes until 0x00
            do
            {
                bytes = bytes.Concat(new[] { br.ReadByte() }).ToArray();
            }
            while (bytes[bytes.Length - 1] != 0x00);

            // Return bytes in UTF8 except the last byte because it is 0x00
            return Encoding.UTF8.GetString(bytes.Take(bytes.Length - 1).ToArray());
        }

        public async Task<string> GetPlayersAndMaxPlayers()
        {
            try
            {
                Dictionary<string, string> kv = await GetInfo();
                return kv["Players"] + '/' + kv["MaxPlayers"];
            }
            catch
            {
                return null;
            }
        }
    }
}
