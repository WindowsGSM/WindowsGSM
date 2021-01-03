using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using WindowsGSM.Functions;

namespace WindowsGSM.DiscordBot
{
	class Bot
	{
		private DiscordSocketClient _client;
		private string _donorType;
		private SocketTextChannel _dashboardTextChannel;
		private RestUserMessage _dashboardMessage;
		private CancellationTokenSource _cancellationTokenSource;

		public Bot()
		{
			Configs.CreateConfigs();
		}

		public async Task<bool> Start()
		{
			_client = new DiscordSocketClient();
			_client.Ready += On_Bot_Ready;

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

			List<Task> tasks = new List<Task>
			{
				StartDiscordPresenceUpdate(),
			};

			_cancellationTokenSource = new CancellationTokenSource();

			await Task.Run(() =>
			{
				try
				{
					Task.WaitAny(tasks.ToArray(), _cancellationTokenSource.Token);
				}
				catch (AggregateException e)
				{
					System.Diagnostics.Debug.WriteLine($"{e.Message}");
				}
			});
		}

		private async Task StartDiscordPresenceUpdate()
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


		public void SetDonorType(string donorType)
		{
			_donorType = donorType;
		}

		public async Task Stop()
		{
			if (_client != null)
			{
				try
				{
					_cancellationTokenSource?.Cancel();
				}
				catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine($"{e.Message}");
                }

				await _client.StopAsync();

				// Delete the message after the bot stop
				try
				{
					await _dashboardTextChannel.DeleteMessageAsync(_dashboardMessage);
					_dashboardMessage = null;
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
	}
}
