using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace WindowsGSM.Discord
{
    class Webhook 
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _webhookUrl;
        private readonly string _donorType;

        public Webhook(string webhookurl, string donorType = "")
        {
            _webhookUrl = webhookurl;
            _donorType = donorType;
        }

        public async Task<bool> Send(string serverid, string servergame, string serverstatus, string servername, string serverip, string serverport)
        {
            if (string.IsNullOrWhiteSpace(_webhookUrl))
            {
                return false;
            }

            string color = GetColor(serverstatus);
            string status = GetStatusWithEmoji(serverstatus);
            string gameicon = GetServerGameIcon(servergame);
            string avatarUrl = GetAvatarUrl();
            string time = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.mssZ");
            string json = @"
            {
                ""username"": ""WindowsGSM"",
                ""avatar_url"": """ + avatarUrl  + @""",
                ""embeds"": [
                {
                    ""title"": ""Status (ID: " + serverid + @")"",
                    ""type"": ""rich"",
                    ""description"": """ + status + @""",
                    ""color"": " + color + @",
                    ""fields"": [
                    {
                        ""name"": ""Game Server"",
                        ""value"": """ + servergame + @""",
                        ""inline"": true
                    },
                    {
                        ""name"": ""Server IP:Port"",
                        ""value"": """ + serverip + ":"+ serverport + @""",
                        ""inline"": true
                    }],
                    ""author"": {
                        ""name"": """ + servername + @""",
                        ""icon_url"": """ + gameicon + @"""
                    },
                    ""footer"": {
                        ""text"": ""WindowsGSM - Alert"",
                        ""icon_url"": """ + avatarUrl + @"""
                    },
                    ""timestamp"": """ + time + @""",
                    ""thumbnail"": {
                        ""url"": ""https://windowsgsm.com/assets/images/warning.png""
                    }
                }]
            }";

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(_webhookUrl, content);
                if (response.Content != null)
                {
                    return true;
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"Fail to send webhook ({_webhookUrl})");
            }

            return false;
        }

        private static string GetColor(string serverStatus)
        {
            if (serverStatus.Contains("Started"))
            {
                return "65280"; //Green
            }
            else if (serverStatus.Contains("Restarted"))
            {
                return "65535"; //Cyan
            }
            else if(serverStatus.Contains("Crashed"))
            {
                return "16711680"; //Red
            }
            else if(serverStatus.Contains("Updated"))
            {
                return "16564292"; //Gold
            }

            return "16777215";
        }

        private static string GetStatusWithEmoji(string serverStatus)
        {
            if (serverStatus.Contains("Started"))
            {
                return ":green_circle: " + serverStatus;
            }
            else if (serverStatus.Contains("Restarted"))
            {
                return ":blue_circle: " + serverStatus;
            }
            else if (serverStatus.Contains("Crashed"))
            {
                return ":red_circle: " + serverStatus;
            }
            else if (serverStatus.Contains("Updated"))
            {
                return ":orange_circle: " + serverStatus;
            }

            return serverStatus;
        }

        private static string GetServerGameIcon(string serverGame)
        {
            try
            {
                return @"https://github.com/BattlefieldDuck/WindowsGSM/raw/master/WindowsGSM/" + GameServer.Data.Icon.ResourceManager.GetString(serverGame);
            }
            catch
            {
                return @"https://github.com/BattlefieldDuck/WindowsGSM/raw/master/WindowsGSM/Images/WindowsGSM.png";
            }
        }

        private string GetAvatarUrl()
        {
            return "https://github.com/BattlefieldDuck/WindowsGSM/raw/master/WindowsGSM/Images/WindowsGSM" + (string.IsNullOrWhiteSpace(_donorType) ? "" : $"-{_donorType}") + ".png";
        }
    }
}
