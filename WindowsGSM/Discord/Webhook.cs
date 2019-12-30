using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace WindowsGSM.Discord
{
    class Webhook 
    {
        private readonly string WebhookUrl;
        private readonly string DonorType;

        public Webhook(string webhookurl, string donorType = "")
        {
            WebhookUrl = webhookurl;
            DonorType = donorType;
        }

        public async Task<bool> Send(string serverid, string servergame, string serverstatus, string servername, string serverip, string serverport)
        {
            if (string.IsNullOrWhiteSpace(WebhookUrl))
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
                }]
            }";

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var httpResponse = await httpClient.PostAsync(WebhookUrl, content);

                    if (httpResponse.Content != null)
                    {
                        return true;
                    }
                }
                catch
                {
                    return false;
                }

                return false;
            }
        }

        private string GetColor(string serverstatus)
        {
            string color;
            switch (serverstatus)
            {
                case "Started":
                    color = "65280"; break; //Green
                case "Stopped":
                    color = "16755200"; break; //Orange
                case "Restarted":
                    color = "65535"; break; //Cyan
                case "Crashed":
                    color = "16711680"; break; //Red
                default:
                    color = "16777215"; break;
            }

            return color;
        }

        private string GetStatusWithEmoji(string serverstatus)
        {
            string status = serverstatus;
            switch (serverstatus)
            {
                case "Started":
                    status += " :ok:"; break;
                case "Stopped":
                    status += " :octagonal_sign:"; break;
                case "Restarted":
                    status += " :arrows_counterclockwise:"; break;
                case "Crashed":
                    status += " :warning:"; break;
            }

            return status;
        }

        private string GetServerGameIcon(string servergame)
        {
            try
            {
                string gameicon = @"https://github.com/BattlefieldDuck/WindowsGSM/raw/master/WindowsGSM/" + GameServer.Data.Icon.ResourceManager.GetString(servergame);
                return gameicon;
            }
            catch
            {
                return @"https://github.com/BattlefieldDuck/WindowsGSM/raw/master/WindowsGSM/Images/WindowsGSM.png";
            }
        }

        private string GetAvatarUrl()
        {
            string avatarUrl = "https://github.com/BattlefieldDuck/WindowsGSM/raw/master/WindowsGSM/Images/WindowsGSM";
            switch (DonorType)
            {
                case "BRONZE":
                case "GOLD":
                case "EMERALD":
                    avatarUrl += $"-{DonorType}";
                    break;
            }
            avatarUrl += ".png";

            return avatarUrl;
        }
    }
}
