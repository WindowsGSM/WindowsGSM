using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace WindowsGSM.DiscordBot
{
    class Bot : IDisposable
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
            _client.Ready += OnBotReady;

            try
            {
                await _client.LoginAsync(TokenType.Bot, Configs.GetBotToken());
                await _client.StartAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Bot login error: {ex.Message}");
                return false;
            }

            // Listen Commands
            new Commands(_client);

            return true;
        }

        private async Task OnBotReady()
        {
            try
            {
                string resourceUri = $"pack://application:,,,/Images/WindowsGSM{(string.IsNullOrWhiteSpace(_donorType) ? string.Empty : $"-{_donorType}")}.png";
                Stream stream = Application.GetResourceStream(new Uri(resourceUri)).Stream;
                await _client.CurrentUser.ModifyAsync(x =>
                {
                    x.Username = "WindowsGSM";
                    x.Avatar = new Image(stream);
                });
                stream.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting bot avatar: {ex.Message}");
            }

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await StartDiscordPresenceUpdate();
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Presence update task canceled.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in presence update: {ex.Message}");
            }
        }

        private async Task StartDiscordPresenceUpdate()
        {
            while (_client != null && _client.CurrentUser != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    if (Application.Current != null)
                    {
                        await Application.Current.Dispatcher.Invoke(async () =>
                        {
                            MainWindow windowsGSM = (MainWindow)Application.Current.MainWindow;
                            int serverCount = windowsGSM.ServerGrid.Items.Count;
                            await _client.SetGameAsync($"{serverCount} game server{(serverCount > 1 ? "s" : string.Empty)}");
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating game status: {ex.Message}");
                }

                await Task.Delay(900000, _cancellationTokenSource.Token);
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
                _cancellationTokenSource?.Cancel();

                await _client.StopAsync();

                if (_dashboardTextChannel != null && _dashboardMessage != null)
                {
                    try
                    {
                        await _dashboardTextChannel.DeleteMessageAsync(_dashboardMessage);
                        _dashboardMessage = null;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error deleting dashboard message: {ex.Message}");
                    }
                }
            }
        }

        public string GetInviteLink()
        {
            return _client == null || _client.CurrentUser == null
                ? string.Empty
                : $"https://discordapp.com/api/oauth2/authorize?client_id={_client.CurrentUser.Id}&permissions=67497024&scope=bot";
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            _client?.Dispose();
        }
    }
}
