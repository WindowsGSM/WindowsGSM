using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace WindowsGSM.Functions.Discord
{
    class Webhook 
    {
        private string WebhookUrl;

        public Webhook(string webhookurl)
        {
            WebhookUrl = webhookurl;
        }

        public async Task<bool> Send(string serverid, string servergame, string status, string servername, string serverip, string serverport)
        {
            if (string.IsNullOrWhiteSpace(WebhookUrl))
            {
                return false;
            }

            string color;

            switch (status)
            {
                case "Started": color = "65280"; break; //Green
                case "Stopped": color = "16755200"; break; //Orange
                case "Restarted": color = "65535"; break; //Cyan
                case "Crashed": color = "16711680"; break; //Red
                default: color = "16777215"; break;
            }

            switch (status)
            {
                case "Started": status += " :ok:"; break;
                case "Stopped": status += " :octagonal_sign:"; break;
                case "Restarted": status += " :arrows_counterclockwise:"; break;
                case "Crashed": status += " :warning:"; break;
                default: break;
            }

            string gameicon;

            switch (servergame)
            {
                case "Team Fortress 2 Dedicated Server": gameicon = @"https://windowsgsm.battlefieldduck.com/images/tf2.png"; break;
                default: gameicon = @"https://www.battlefieldduck.com/assets/images/windowsgsm.png"; break;
            }

            string time = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.mssZ");

            string json = @"
            {
                'username': 'WindowsGSM',
                'avatar_url': 'https://www.battlefieldduck.com/assets/images/windowsgsm.png',
                'embeds': [
                {
                    'title': 'Status (ID: " + serverid + @")',
                    'type': 'rich',
                    'description': '" + status + @"',
                    'color': " + color + @",
                    'fields': [
                    {
                        'name': 'Game Server',
                        'value': '" + servergame + @"'
                    },
                    {
                        'name': 'Server IP:Port',
                        'value': '" + serverip + ":"+ serverport + @"'
                    }],
                    'author': {
                        'name': '" + servername + @"',
                        'icon_url': '" + gameicon + @"'
                    },
                    'footer': {
                        'text': 'WindowsGSM - Alert',
                        'icon_url': 'https://www.battlefieldduck.com/assets/images/windowsgsm.png'
                    },
                    'timestamp': '" + time + @"'
                }]
            }";

            var content = new StringContent(json.Replace("'", "\""), Encoding.UTF8, "application/json");

            using (var httpClient = new HttpClient())
            {
                var httpResponse = await httpClient.PostAsync(WebhookUrl, content);

                if (httpResponse.Content != null)
                {
                    //var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    return true;
                }

                return false;
            }
        }
    }
}
