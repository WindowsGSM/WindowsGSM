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
                        ""value"": """ + servergame + @"""
                    },
                    {
                        ""name"": ""Server IP:Port"",
                        ""value"": """ + serverip + ":"+ serverport + @"""
                    }],
                    ""author"": {
                        ""name"": """ + servername + @""",
                        ""icon_url"": """ + gameicon + @"""
                    },
                    ""footer"": {
                        ""text"": ""WindowsGSM - Alert"",
                        ""icon_url"": """ + avatarUrl + @"""
                    },
                    ""timestamp"": """ + time + @"""
                }],
                ""thumbnail"": {
                    ""url"": ""https://upload.wikimedia.org/wikipedia/commons/3/38/4-Nature-Wallpapers-2014-1_ukaavUI.jpg""
                }
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

        private string GetColor(string serverStatus)
        {
            switch (serverStatus)
            {
                case "Started": return "65280"; //Green
                case "Stopped": return "16755200"; //Orange
                case "Restarted": return "65535"; //Cyan
                case "Crashed": return "16711680"; //Red
                default:
                    return "16777215";
            }
        }

        private string GetStatusWithEmoji(string serverStatus)
        {
            switch (serverStatus)
            {
                case "Started": return $"{serverStatus} :ok:";
                case "Stopped": return $"{serverStatus} :octagonal_sign:";
                case "Restarted": return $"{serverStatus} :arrows_counterclockwise:";
                case "Crashed": return $"{serverStatus} :warning:";
                default:
                    return serverStatus;
            }
        }

        private string GetServerGameIcon(string serverGame)
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
            string avatarUrl = "https://github.com/BattlefieldDuck/WindowsGSM/raw/master/WindowsGSM/Images/WindowsGSM";
            switch (_donorType)
            {
                case "BRONZE":
                case "GOLD":
                case "EMERALD":
                    avatarUrl += $"-{_donorType}";
                    break;
            }

            return $"{avatarUrl}.png";
        }
    }
}
