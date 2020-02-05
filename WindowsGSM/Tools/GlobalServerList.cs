using System.IO;
using System.Net;

namespace WindowsGSM.Tools
{
    static class GlobalServerList
    {
        public static bool IsServerOnSteamServerList(string publicIP, string port)
        {
            if (WebRequest.Create("http://api.steampowered.com/ISteamApps/GetServersAtAddress/v0001?addr=" + publicIP + "&format=json") is HttpWebRequest webRequest)
            {
                webRequest.Method = "GET";
                webRequest.UserAgent = "Anything";
                webRequest.ServicePoint.Expect100Continue = false;

                try
                {
                    using (var responseReader = new StreamReader(webRequest.GetResponse().GetResponseStream()))
                    {
                        string json = responseReader.ReadToEnd();
                        string matchString = "\"addr\":\"" + publicIP + ":" + port + "\"";

                        if (json.Contains(matchString))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    //ignore
                }
            }

            return false;
        }
    }
}
