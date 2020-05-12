using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace WindowsGSM.DiscordBot
{
	class Bot
	{
		private DiscordSocketClient _client;
		private string _donorType;
		private SocketTextChannel _dashboardTextChannel;
		private RestUserMessage _dashboardMessage;
		private long _startToken;

		public Bot()
		{
			Configs.CreateConfigs();
		}

		public async Task<bool> Start()
		{
			_client = new DiscordSocketClient();
			_client.Ready += On_Bot_Ready;
			_startToken = DateTime.UtcNow.Ticks;

			try
			{
				await _client.LoginAsync(TokenType.Bot, Configs.GetBotToken());
				await _client.StartAsync();
			}
			catch
			{
				return false;
			}

			// Listen Commands
			new Commands(_client);

			return true;
		}

		private async Task On_Bot_Ready()
		{
			try
			{
				Stream stream = Application.GetResourceStream(new Uri($"pack://application:,,,/Images/WindowsGSM{(string.IsNullOrWhiteSpace(_donorType) ? string.Empty : $"-{_donorType}")}.png")).Stream;
				await _client.CurrentUser.ModifyAsync(x =>
				{
					x.Username = "WindowsGSM";
					x.Avatar = new Image(stream);
				});
			}
			catch
			{
				// ignore
			}

			StartDiscordPresenceUpdate();
			StartDashBoardRefresh();
		}

		private async void StartDiscordPresenceUpdate()
		{
			while (_client != null && _client.CurrentUser != null)
			{
				if (Application.Current != null)
				{
					await Application.Current.Dispatcher.Invoke(async () =>
					{
						MainWindow WindowsGSM = (MainWindow)Application.Current.MainWindow;
						int serverCount = WindowsGSM.ServerGrid.Items.Count;
						await _client.SetGameAsync($"{serverCount} game server{(serverCount > 1 ? "s" : string.Empty)}");
					});
				}

				await Task.Delay(900000);
			}
		}

		private async void StartDashBoardRefresh()
		{
			if (!ulong.TryParse(Configs.GetDashboardChannel(), out ulong channelId))
			{
				return;
			}

			long startToken = _startToken;

			var system = new Functions.SystemMetrics();
			await Task.Run(() => system.GetCPUStaticInfo());
			await Task.Run(() => system.GetRAMStaticInfo());
			await Task.Run(() => system.GetDiskStaticInfo());

			int refreshRate = Configs.GetDashboardRefreshRate();

			List<ulong> guildIds = new List<ulong>();
			var guilds = _client.Guilds.GetEnumerator();
			while (guilds.MoveNext()) { guildIds.Add(guilds.Current.Id); }

			if (ShouldReturn(startToken)) { return; }

			foreach (ulong Id in guildIds)
			{
				_dashboardTextChannel = _client.GetGuild(Id).GetTextChannel(channelId);
				if (_dashboardTextChannel != null)
				{
					// Delete WindowsGSM bot old messages
					IEnumerable<IMessage> messages = await _dashboardTextChannel.GetMessagesAsync().FlattenAsync();
					await _dashboardTextChannel.DeleteMessagesAsync(messages.Where(s => s.Author.Id == _client.CurrentUser.Id));
					break;
				}
			}

			if (ShouldReturn(startToken)) { return; }

			while (_client != null && _client.CurrentUser != null)
			{
				var embed = new EmbedBuilder
				{
					Title = ":small_orange_diamond: Dashboard",
					Description = $"Server name: {Environment.MachineName}",
					Color = Color.Blue
				};
				
				embed.AddField("CPU", GetProgressBar(await Task.Run(() => system.GetCPUUsage())), true);
				double ramUsage = await Task.Run(() => system.GetRAMUsage());
				embed.AddField("Memory: " + Functions.SystemMetrics.GetMemoryRatioString(ramUsage, system.RAMTotalSize), GetProgressBar(ramUsage), true);
				double diskUsage = await Task.Run(() => system.GetDiskUsage());
				embed.AddField("Disk: " + Functions.SystemMetrics.GetDiskRatioString(diskUsage, system.DiskTotalSize), GetProgressBar(diskUsage), true);

				(int serverCount, int startedCount, int activePlayers) = await GetGameServerDashBoardDetails();
				embed.AddField($"Servers: {serverCount}/{MainWindow.MAX_SERVER}", GetProgressBar(serverCount * 100 / MainWindow.MAX_SERVER), true);
				embed.AddField($"Online: {startedCount}/{serverCount}", GetProgressBar((serverCount == 0) ? 0 : startedCount * 100 / serverCount), true);
				embed.AddField("Active Players", GetActivePlayersString(activePlayers), true);

				embed.WithFooter(new EmbedFooterBuilder().WithIconUrl("https://github.com/WindowsGSM/WindowsGSM/raw/master/WindowsGSM/Images/WindowsGSM.png").WithText($"WindowsGSM {MainWindow.WGSM_VERSION} | Live Dashboard"));
				embed.WithCurrentTimestamp();

				if (ShouldReturn(startToken)) { break; }

				if (_dashboardTextChannel != null)
				{
					if (_dashboardMessage == null)
					{
						try
						{
							_dashboardMessage = await _dashboardTextChannel.SendMessageAsync(embed: embed.Build());
						}
						catch
						{
							await Task.Delay(60000);
						}
					}
					else
					{
						try
						{
							await _dashboardMessage.ModifyAsync(m => m.Embed = embed.Build());
						}
						catch
						{
							await Task.Delay(60000);
						}
					}
				}

				await Task.Delay(refreshRate * 1000);
			}

			// Delete the message after the bot stop
			try
			{
				await _dashboardTextChannel.DeleteMessageAsync(_dashboardMessage);
			}
			catch
			{
				// ignore
			}
		}

		public void SetDonorType(string donorType)
		{
			_donorType = donorType;
		}

		public async Task Stop()
		{
			if (_client != null)
			{
				_startToken = 0;
				await _client.StopAsync();

				// Delete the message after the bot stop
				try
				{
					await _dashboardTextChannel.DeleteMessageAsync(_dashboardMessage);
				}
				catch
				{
					// ignore
				}
			}
		}

		public string GetInviteLink()
		{
			return (_client == null || _client.CurrentUser == null) ? string.Empty : $"https://discordapp.com/api/oauth2/authorize?client_id={_client.CurrentUser.Id}&permissions=67497024&scope=bot";
		}

		private string GetProgressBar(double progress)
		{ 
			// ▌ // ▋ // █ // Which one is the best?
			const int MAX_BLOCK = 23;
			string display = $" {(int)progress}% ";

			int startIndex = MAX_BLOCK / 2 - display.Length / 2;
			string progressBar = string.Concat(Enumerable.Repeat("█", (int)(progress / 100 * MAX_BLOCK))).PadRight(MAX_BLOCK).Remove(startIndex, display.Length).Insert(startIndex, display);

			return $"**`{progressBar}`**";
		}

		private string GetActivePlayersString(int activePlayers)
		{
			const int MAX_BLOCK = 23;
			string display = $" {activePlayers} ";

			int startIndex = MAX_BLOCK / 2 - display.Length / 2;
			string activePlayersString = string.Concat(Enumerable.Repeat(" ", MAX_BLOCK)).Remove(startIndex, display.Length).Insert(startIndex, display);

			return $"**`{activePlayersString}`**";
		}

		private async Task<(int, int, int)> GetGameServerDashBoardDetails()
		{
			if (Application.Current != null)
			{
				return await Application.Current.Dispatcher.Invoke(async () =>
				{
					MainWindow WindowsGSM = (MainWindow)Application.Current.MainWindow;
					return (WindowsGSM.GetServerCount(), WindowsGSM.GetStartedServerCount(), WindowsGSM.GetActivePlayers());
				});
			}

			return (0, 0, 0);
		}

		private bool ShouldReturn(long startToken)
		{
			return startToken != _startToken;
		}
	}
}
