using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Windows;

namespace WindowsGSM.DiscordBot
{
    class Commands
    {
        private readonly DiscordSocketClient _client;

        public Commands(DiscordSocketClient client)
        {
            _client = client;
            _client.MessageReceived += CommandReceivedAsync;
        }

        private async Task CommandReceivedAsync(SocketMessage message)
        {
            // The bot should never respond to itself.
            if (message.Author.Id == _client.CurrentUser.Id) { return; }

            // Return if the author is not admin
            var adminIDs = Configs.GetBotAdmins();
            if (!adminIDs.Contains(message.Author.Id.ToString())) { return; }

            // Return if the message is not WindowsGSM prefix
            var prefix = Configs.GetBotPrefix();
            var commandLen = prefix.Length + 4;
            if (message.Content.Length < commandLen) { return; }
            if (message.Content.Length == commandLen && message.Content == $"{prefix}wgsm")
            {
                await SendHelpEmbed(message);
                return;
            }
            if (message.Content.Length >= (commandLen + 1) && message.Content.Substring(0, commandLen + 1) != $"{prefix}wgsm ") { return; }

            // Remote Actions
            string[] args = message.Content.Split(new char[] { ' ' }, 2);
            switch (args[1].Split(' ')[0])
            {
                case "list": await Action_List(message); break;
                case "start": await Action_Start(message, args[1]); break;
                case "stop": await Action_Stop(message, args[1]); break;
                case "restart": await Action_Restart(message, args[1]); break;
                case "send": await Action_SendCommand(message, args[1]); break;
                default: await SendHelpEmbed(message); break;
            }
        }

        private async Task Action_List(SocketMessage message)
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                MainWindow WindowsGSM = (MainWindow)Application.Current.MainWindow;

                var list = WindowsGSM.GetServerList();
                string ids = "", status = "", servers = "";
                foreach ((string id, string state, string server) in list)
                {
                    ids += $"`{id}`\n";
                    status += $"`{state}`\n";
                    servers += $"`{server}`\n";
                }

                var embed = new EmbedBuilder { Color = Color.Teal };
                embed.AddField("ID", ids, inline: true);
                embed.AddField("Status", status, inline: true);
                embed.AddField("Server Name", servers, inline: true);

                await message.Channel.SendMessageAsync(embed: embed.Build());
            });
        }

        private async Task Action_Start(SocketMessage message, string command)
        {
            string[] args = command.Split(' ');
            if (args.Length == 2 && int.TryParse(args[1], out int i))
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    MainWindow WindowsGSM = (MainWindow)Application.Current.MainWindow;
                    if (WindowsGSM.IsServerExist(args[1]))
                    {
                        MainWindow.ServerStatus serverStatus = WindowsGSM.GetServerStatus(args[1]);
                        if (serverStatus == MainWindow.ServerStatus.Stopped)
                        {
                            bool started = await WindowsGSM.StartServerById(args[1], message.Author.Id.ToString(), message.Author.Username);
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) {(started ? "Started" : "Fail to Start")}.");
                        }
                        else if (serverStatus == MainWindow.ServerStatus.Started)
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) already Started.");
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) currently in {serverStatus.ToString()} state, not able to start.");
                        }

                        await SendServerEmbed(message, Color.Green, args[1], WindowsGSM.GetServerStatus(args[1]).ToString(), WindowsGSM.GetServerName(args[1]));
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) does not exists.");
                    }
                });
            }
            else
            {
                await message.Channel.SendMessageAsync($"Usage: {Configs.GetBotPrefix()}wgsm start `<SERVERID>`");
            }
        }

        private async Task Action_Stop(SocketMessage message, string command)
        {
            string[] args = command.Split(' ');
            if (args.Length == 2 && int.TryParse(args[1], out int i))
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    MainWindow WindowsGSM = (MainWindow)Application.Current.MainWindow;
                    if (WindowsGSM.IsServerExist(args[1]))
                    {
                        MainWindow.ServerStatus serverStatus = WindowsGSM.GetServerStatus(args[1]);
                        if (serverStatus == MainWindow.ServerStatus.Started)
                        {
                            bool started = await WindowsGSM.StopServerById(args[1], message.Author.Id.ToString(), message.Author.Username);
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) {(started ? "Stopped" : "Fail to Stop")}.");
                        }
                        else if (serverStatus == MainWindow.ServerStatus.Stopped)
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) already Stopped.");
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) currently in {serverStatus.ToString()} state, not able to stop.");
                        }

                        await SendServerEmbed(message, Color.Orange, args[1], WindowsGSM.GetServerStatus(args[1]).ToString(), WindowsGSM.GetServerName(args[1]));
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) does not exists.");
                    }
                });
            }
            else
            {
                await message.Channel.SendMessageAsync($"Usage: {Configs.GetBotPrefix()}wgsm stop `<SERVERID>`");
            }
        }

        private async Task Action_Restart(SocketMessage message, string command)
        {
            string[] args = command.Split(' ');
            if (args.Length == 2 && int.TryParse(args[1], out int i))
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    MainWindow WindowsGSM = (MainWindow)Application.Current.MainWindow;
                    if (WindowsGSM.IsServerExist(args[1]))
                    {
                        MainWindow.ServerStatus serverStatus = WindowsGSM.GetServerStatus(args[1]);
                        if (serverStatus == MainWindow.ServerStatus.Started)
                        {
                            bool started = await WindowsGSM.RestartServerById(args[1], message.Author.Id.ToString(), message.Author.Username);
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) {(started ? "Restarted" : "Fail to Restart")}.");
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) currently in {serverStatus.ToString()} state, not able to restart.");
                        }

                        await SendServerEmbed(message, Color.Blue, args[1], WindowsGSM.GetServerStatus(args[1]).ToString(), WindowsGSM.GetServerName(args[1]));
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) does not exists.");
                    }
                });
            }
            else
            {
                await message.Channel.SendMessageAsync($"Usage: {Configs.GetBotPrefix()}wgsm restart `<SERVERID>`");
            }
        }

        private async Task Action_SendCommand(SocketMessage message, string command)
        {
            string[] args = command.Split(' ');
            if (args.Length >= 2 && int.TryParse(args[1], out int i))
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    MainWindow WindowsGSM = (MainWindow)Application.Current.MainWindow;
                    if (WindowsGSM.IsServerExist(args[1]))
                    {
                        MainWindow.ServerStatus serverStatus = WindowsGSM.GetServerStatus(args[1]);
                        if (serverStatus == MainWindow.ServerStatus.Started)
                        {
                            string sendCommand = command.Substring(args[1].Length + 6);
                            bool sent = await WindowsGSM.SendCommandById(args[1], sendCommand, message.Author.Id.ToString(), message.Author.Username);
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) {(sent ? "Command sent" : "Fail to send command")}. | `{sendCommand}`");
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) currently in {serverStatus.ToString()} state, not able to send command.");
                        }
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) does not exists.");
                    }
                });
            }
            else
            {
                await message.Channel.SendMessageAsync($"Usage: {Configs.GetBotPrefix()}wgsm send `<SERVERID>` `<COMMAND>`");
            }
        }

        private async Task SendServerEmbed(SocketMessage message, Color color, string serverId, string serverStatus, string serverName)
        {
            var embed = new EmbedBuilder { Color = color };
            embed.AddField("ID", serverId, inline: true);
            embed.AddField("Status", serverStatus, inline: true);
            embed.AddField("Server Name", serverName, inline: true);

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task SendHelpEmbed(SocketMessage message)
        {
            var embed = new EmbedBuilder
            {
                Title = "Available Commands:",
                Color = Color.Teal
            };

            string prefix = Configs.GetBotPrefix();
            embed.AddField("Command", $"{prefix}wgsm list\n{prefix}wgsm start <SERVERID>\n{prefix}wgsm stop <SERVERID>\n{prefix}wgsm restart <SERVERID>\n{prefix}wgsm send <SERVERID> <COMMAND>", inline: true);
            embed.AddField("Usage", "Print server list with id, status and name\nStart a server remotely by serverId\nStop a server remotely by serverId\nRestart a server remotely by serverId\nSend a command to server console", inline: true);

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }
    }
}
