using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WindowsGSM.DiscordBot
{
    static class Configs
    {
		private static readonly string _botPath = Functions.ServerPath.Get("DiscordBot");

		public static void CreateConfigs()
		{
			Directory.CreateDirectory(_botPath);
			File.AppendText(Path.Combine(_botPath, "token.txt"));
			File.AppendText(Path.Combine(_botPath, "adminIDs.txt"));
			File.AppendText(Path.Combine(_botPath, "prefix.txt"));
		}

		public static string GetCommandsList()
		{
			string prefix = GetBotPrefix();
			return $"{prefix}wgsm list\n{prefix}wgsm start <SERVERID>\n{prefix}wgsm stop <SERVERID>\n{prefix}wgsm restart <SERVERID>\n{prefix}wgsm send <SERVERID> <COMMAND>";
		}

		public static string GetBotPrefix()
		{
			try
			{
				return File.ReadAllText(Path.Combine(_botPath, "prefix.txt")).Trim();
			}
			catch
			{
				return "";
			}
		}

		public static void SetBotPrefix(string prefix)
		{
			Directory.CreateDirectory(_botPath);
			File.WriteAllText(Path.Combine(_botPath, "prefix.txt"), prefix);
		}

		public static string GetBotToken()
		{
			try
			{
				return File.ReadAllText(Path.Combine(_botPath, "token.txt")).Trim();
			}
			catch
			{
				return "";
			}
		}

		public static void SetBotToken(string token)
		{
			Directory.CreateDirectory(_botPath);
			File.WriteAllText(Path.Combine(_botPath, "token.txt"), token.Trim());
		}

		public static List<string> GetBotAdmins()
		{
			try
			{
				return File.ReadAllLines(Path.Combine(_botPath, "adminIDs.txt")).ToList();
			}
			catch
			{
				return new List<string>();
			}
		}

		public static void SetBotAdmins(List<string> adminIDs)
		{
			Directory.CreateDirectory(_botPath);
			File.WriteAllText(Path.Combine(_botPath, "adminIDs.txt"), string.Join("\n", adminIDs.ToArray()));
		}
	}
}
