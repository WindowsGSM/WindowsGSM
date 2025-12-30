using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using WindowsGSM.Functions;

namespace WindowsGSM.DiscordBot
{
    static class Configs
    {
        private static readonly string _botPath = ServerPath.Get(ServerPath.FolderName.Configs, "discordbot");

        // Cache
        private static string _cachedPrefix;
        private static string _cachedToken;
        private static string _cachedChannel;
        private static int? _cachedRefreshRate;
        private static List<(string AdminId, string ServerIds)> _cachedAdminList;

        public static void CreateConfigs()
        {
            Directory.CreateDirectory(_botPath);
        }

        public static string GetCommandsList()
        {
            string prefix = GetBotPrefix();
            return $"{prefix}wgsm check\n{prefix}wgsm list\n{prefix}wgsm start <SERVERID>\n{prefix}wgsm stop <SERVERID>\n{prefix}wgsm restart <SERVERID>\n{prefix}wgsm update <SERVERID>\n{prefix}wgsm send <SERVERID> <COMMAND>\n{prefix}wgsm backup <SERVERID>\n{prefix}wgsm stats";
        }

        public static string GetBotPrefix()
        {
            if (_cachedPrefix != null) return _cachedPrefix;
            try
            {
                _cachedPrefix = File.ReadAllText(Path.Combine(_botPath, "prefix.txt")).Trim();
            }
            catch
            {
                _cachedPrefix = string.Empty;
            }
            return _cachedPrefix;
        }

        public static void SetBotPrefix(string prefix)
        {
            _cachedPrefix = prefix;
            Directory.CreateDirectory(_botPath);
            File.WriteAllText(Path.Combine(_botPath, "prefix.txt"), prefix);
        }

        public static string GetBotToken()
        {
            if (_cachedToken != null) return _cachedToken;
            try
            {
                string path = Path.Combine(_botPath, "token.txt");
                if (File.Exists(path))
                {
                    try 
                    {
                        byte[] encryptedData = File.ReadAllBytes(path);
                        byte[] decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                        _cachedToken = Encoding.UTF8.GetString(decryptedData);
                    }
                    catch
                    {
                        _cachedToken = File.ReadAllText(path).Trim();
                    }
                }
                else
                {
                    _cachedToken = string.Empty;
                }
            }
            catch
            {
                _cachedToken = string.Empty;
            }
            return _cachedToken;
        }

        public static void SetBotToken(string token)
        {
            _cachedToken = token.Trim();
            Directory.CreateDirectory(_botPath);
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(_cachedToken);
                byte[] encryptedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(Path.Combine(_botPath, "token.txt"), encryptedData);
            }
            catch
            {
                File.WriteAllText(Path.Combine(_botPath, "token.txt"), _cachedToken);
            }
        }

        public static string GetDashboardChannel()
        {
            if (_cachedChannel != null) return _cachedChannel;
            try
            {
                _cachedChannel = File.ReadAllText(Path.Combine(_botPath, "channel.txt")).Trim();
            }
            catch
            {
                _cachedChannel = string.Empty;
            }
            return _cachedChannel;
        }

        public static void SetDashboardChannel(string channel)
        {
            _cachedChannel = channel.Trim();
            Directory.CreateDirectory(_botPath);
            File.WriteAllText(Path.Combine(_botPath, "channel.txt"), _cachedChannel);
        }

        public static int GetDashboardRefreshRate()
        {
            if (_cachedRefreshRate.HasValue) return _cachedRefreshRate.Value;
            try
            {
                _cachedRefreshRate = int.Parse(File.ReadAllText(Path.Combine(_botPath, "refreshrate.txt")).Trim());
            }
            catch
            {
                _cachedRefreshRate = 5;
            }
            return _cachedRefreshRate.Value;
        }

        public static void SetDashboardRefreshRate(int rate)
        {
            _cachedRefreshRate = rate;
            Directory.CreateDirectory(_botPath);
            File.WriteAllText(Path.Combine(_botPath, "refreshrate.txt"), rate.ToString());
        }

        private static void LoadAdminList()
        {
            if (_cachedAdminList != null) return;
            _cachedAdminList = new List<(string, string)>();
            try
            {
                var lines = File.ReadAllLines(Path.Combine(_botPath, "adminIDs.txt"));
                foreach (var line in lines)
                {
                    string[] items = line.Split(new[] { ' ' }, 2);
                    _cachedAdminList.Add((items[0], items.Length > 1 ? items[1].Trim() : string.Empty));
                }
            }
            catch
            {
                // Ignore
            }
        }

        public static List<string> GetBotAdminIds()
        {
            LoadAdminList();
            return _cachedAdminList.Select(x => x.AdminId).ToList();
        }

        public static List<string> GetServerIdsByAdminId(string adminId)
        {
            LoadAdminList();
            var admin = _cachedAdminList.FirstOrDefault(x => x.AdminId == adminId);
            if (admin.AdminId != null && !string.IsNullOrEmpty(admin.ServerIds))
            {
                return admin.ServerIds.Split(',').Select(s => s.Trim()).ToList();
            }
            return new List<string>();
        }

        public static List<(string, string)> GetBotAdminList()
        {
            LoadAdminList();
            return new List<(string, string)>(_cachedAdminList);
        }

        public static void SetBotAdminList(List<(string, string)> adminList)
        {
            _cachedAdminList = new List<(string, string)>(adminList);
            Directory.CreateDirectory(_botPath);

            List<string> lines = new List<string>();
            foreach ((string adminID, string serverIDs) in adminList)
            {
                lines.Add($"{adminID} {serverIDs}");
            }
            File.WriteAllText(Path.Combine(_botPath, "adminIDs.txt"), string.Join(Environment.NewLine, lines.ToArray()));
        }
    }
}
