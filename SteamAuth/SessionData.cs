using System.Net;

namespace SteamAuth
{
    public class SessionData
    {
        public string SessionID { get; set; } = string.Empty;

        public string SteamLogin { get; set; } = string.Empty;

        public string SteamLoginSecure { get; set; } = string.Empty;

        public string WebCookie { get; set; } = string.Empty;

        public string OAuthToken { get; set; } = string.Empty;

        public ulong SteamID { get; set; }

        public void AddCookies(CookieContainer cookies)
        {
            cookies.Add(new Cookie("mobileClientVersion", "0 (2.1.3)", "/", ".steamcommunity.com"));
            cookies.Add(new Cookie("mobileClient", "android", "/", ".steamcommunity.com"));

            cookies.Add(new Cookie("steamid", SteamID.ToString(), "/", ".steamcommunity.com"));
            cookies.Add(new Cookie("steamLogin", SteamLogin, "/", ".steamcommunity.com")
            {
                HttpOnly = true
            });

            cookies.Add(new Cookie("steamLoginSecure", SteamLoginSecure, "/", ".steamcommunity.com")
            {
                HttpOnly = true,
                Secure = true
            });
            cookies.Add(new Cookie("Steam_Language", "english", "/", ".steamcommunity.com"));
            cookies.Add(new Cookie("dob", "", "/", ".steamcommunity.com"));
            cookies.Add(new Cookie("sessionid", this.SessionID, "/", ".steamcommunity.com"));
        }
    }
}
