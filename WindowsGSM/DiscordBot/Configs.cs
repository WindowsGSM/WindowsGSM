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
		}

		public static string GetCommandsList()
		{
			string prefix = GetBotPrefix();
			return $"{prefix}wgsm check\n{prefix}wgsm list\n{prefix}wgsm start <SERVERID>\n{prefix}wgsm stop <SERVERID>\n{prefix}wgsm restart <SERVERID>\n{prefix}wgsm send <SERVERID> <COMMAND>";
		}

		public static string GetBotPrefix()
		{
			try
			{
				return File.ReadAllText(Path.Combine(_botPath, "prefix.txt")).Trim();
			}
			catch
			{
				return string.Empty;
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
				return string.Empty;
			}
		}

		public static void SetBotToken(string token)
		{
			Directory.CreateDirectory(_botPath);
			File.WriteAllText(Path.Combine(_botPath, "token.txt"), token.Trim());
		}

		public static string GetDashboardChannel()
		{
			try
			{
				return File.ReadAllText(Path.Combine(_botPath, "channel.txt")).Trim();
			}
			catch
			{
				return string.Empty;
			}
		}

		public static void SetDashboardChannel(string channel)
		{
			Directory.CreateDirectory(_botPath);
			File.WriteAllText(Path.Combine(_botPath, "channel.txt"), channel.Trim());
		}

		public static int GetDashboardRefreshRate()
		{
			try
			{
				return int.Parse(File.ReadAllText(Path.Combine(_botPath, "refreshrate.txt")).Trim());
			}
			catch
			{
				return 5;
			}
		}

		public static void SetDashboardRefreshRate(int rate)
		{
			Directory.CreateDirectory(_botPath);
			File.WriteAllText(Path.Combine(_botPath, "refreshrate.txt"), rate.ToString());
		}

		public static List<string> GetBotAdminIds()
		{
			try
			{
				var adminIds = new List<string>();
				var lines = File.ReadAllLines(Path.Combine(_botPath, "adminIDs.txt"));
				foreach (var line in lines)
				{
					string[] items = line.Split(new char[] { ' ' }, 2);
					adminIds.Add(items[0]);
				}
				return adminIds;
			}
			catch
			{
				return new List<string>();
			}
		}

		public static List<string> GetServerIdsByAdminId(string adminId)
		{
			try
			{
				var lines = File.ReadAllLines(Path.Combine(_botPath, "adminIDs.txt"));
				foreach (var line in lines)
				{
					string[] items = line.Split(new[] { ' ' }, 2);
					if (items[0] == adminId)
					{
						return items[1].Trim().Split(',').Select(s => s.Trim()).ToList();
					}
				}

				return new List<string>();
			}
			catch
			{
				return new List<string>();
			}
		}

		public static List<(string, string)> GetBotAdminList()
		{
			try
			{
				var adminList = new List<(string, string)>();
				var lines = File.ReadAllLines(Path.Combine(_botPath, "adminIDs.txt"));
				foreach (var line in lines)
				{
					string[] items = line.Split(new[] { ' ' }, 2);
					adminList.Add((items[0], items.Length == 1 ? string.Empty : items[1]));
				}
				return adminList;
			}
			catch
			{
				return new List<(string, string)>();
			}
		}

		public static void SetBotAdminList(List<(string, string)> adminList)
		{
			Directory.CreateDirectory(_botPath);

			List<string> lines = new List<string>();
			foreach ((string adminID, string serverIDs) in adminList)
			{
				lines.Add($"{adminID} {serverIDs}");
			}
			File.WriteAllText(Path.Combine(_botPath, "adminIDs.txt"), string.Join("\n", lines.ToArray()));
		}
	}
}
