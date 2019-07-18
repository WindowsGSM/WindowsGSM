using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace WindowsGSM.Functions.Discord
{
    class Webhook 
    {
        private readonly string WebhookUrl;

        public Webhook(string webhookurl)
        {
            WebhookUrl = webhookurl;
        }

        public async Task<bool> Send(string serverid, string servergame, string serverstatus, string servername, string serverip, string serverport)
        {
            if (string.IsNullOrWhiteSpace(WebhookUrl))
            {
                return false;
            }

            string color;
            switch (serverstatus)
            {
                case "Started": color = "65280"; break; //Green
                case "Stopped": color = "16755200"; break; //Orange
                case "Restarted": color = "65535"; break; //Cyan
                case "Crashed": color = "16711680"; break; //Red
                default: color = "16777215"; break;
            }

            string status = serverstatus;
            switch (serverstatus)
            {
                case "Started": status += " :ok:"; break;
                case "Stopped": status += " :octagonal_sign:"; break;
                case "Restarted": status += " :arrows_counterclockwise:"; break;
                case "Crashed": status += " :warning:"; break;
                default: break;
            }

            string gameicon = @"https://github.com/BattlefieldDuck/WindowsGSM/blob/master/WindowsGSM/Images/";
            switch (servergame)
            {
                case "Garry's Mod Dedicated Server": gameicon += @"games/gmod.png?raw=true"; break;
                case "Team Fortress 2 Dedicated Server": gameicon += @"games/tf2.png?raw=true"; break;
                default: gameicon += @"windowsgsm.png?raw=true"; break;
            }

            string time = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.mssZ");

            string wgsmPath = "https://github.com/BattlefieldDuck/WindowsGSM/blob/master/WindowsGSM/Images/windowsgsm.png?raw=true";
            string json = @"
            {
                ""username"": ""WindowsGSM"",
                ""avatar_url"": ""https://github.com/BattlefieldDuck/WindowsGSM/blob/master/WindowsGSM/Images/windowsgsm.png?raw=true"",
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
                        ""icon_url"": ""https://github.com/BattlefieldDuck/WindowsGSM/blob/master/WindowsGSM/Images/windowsgsm.png?raw=true""
                    },
                    ""timestamp"": """ + time + @"""
                }]
            }";

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using (var httpClient = new HttpClient())
            {
                var httpResponse = await httpClient.PostAsync(WebhookUrl, content);

                if (httpResponse.Content != null)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
