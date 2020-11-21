using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
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
            List<string> adminIds = Configs.GetBotAdminIds();
            if (!adminIds.Contains(message.Author.Id.ToString())) { return; }

            // Return if the message is not WindowsGSM prefix
            var prefix = Configs.GetBotPrefix();
            var commandLen = prefix.Length + 4;
            if (message.Content.Length < commandLen) { return; }
            if (message.Content.Length == commandLen && message.Content == $"{prefix}wgsm")
            {
                await SendHelpEmbed(message);
                return;
            }

            if (message.Content.Length >= commandLen + 1 && message.Content.Substring(0, commandLen + 1) == $"{prefix}wgsm ")
            {
                // Remote Actions
                string[] args = message.Content.Split(new[] { ' ' }, 2);
                string[] splits = args[1].Split(' ');

                switch (splits[0])
                {
                    case "start":
                    case "stop":
                    case "restart":
                    case "send":
                    case "list":
                    case "check":
                    case "backup":
                    case "update":
                        List<string> serverIds = Configs.GetServerIdsByAdminId(message.Author.Id.ToString());
                        if (splits[0] == "check")
                        {
                            await message.Channel.SendMessageAsync(
                                serverIds.Contains("0") ?
                                "You have full permission.\nCommands: `check`, `list`, `start`, `stop`, `restart`, `send`, `backup`, `update`" :
                                $"You have permission on servers (`{string.Join(",", serverIds.ToArray())}`)\nCommands: `check`, `start`, `stop`, `restart`, `send`, `backup`, `update`");
                            break;
                        }

                        if (splits[0] == "list" && serverIds.Contains("0"))
                        {
                            await Action_List(message);
                        }
                        else if (splits[0] != "list" && (serverIds.Contains("0") || serverIds.Contains(splits[1])))
                        {
                            switch (splits[0])
                            {
                                case "start": await Action_Start(message, args[1]); break;
                                case "stop": await Action_Stop(message, args[1]); break;
                                case "restart": await Action_Restart(message, args[1]); break;
                                case "send": await Action_SendCommand(message, args[1]); break;
                                case "backup": await Action_Backup(message, args[1]); break;
                                case "update": await Action_Update(message, args[1]); break;
                            }
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync("You don't have permission to access.");
                        }
                        break;
                    default: await SendHelpEmbed(message); break;
                }
            }
        }

        private async Task Action_List(SocketMessage message)
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                MainWindow WindowsGSM = (MainWindow)Application.Current.MainWindow;

                var list = WindowsGSM.GetServerList();

                string ids = string.Empty;
                string status = string.Empty;
                string servers = string.Empty;

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

        private async Task Action_Backup(SocketMessage message, string command)
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
                        if (serverStatus == MainWindow.ServerStatus.Stopped)
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) Backup started - this may take some time.");
                            bool backuped = await WindowsGSM.BackupServerById(args[1], message.Author.Id.ToString(), message.Author.Username);
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) {(backuped ? "Backup Complete" : "Fail to Backup")}.");
                        }
                        else if (serverStatus == MainWindow.ServerStatus.Backuping)
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) already Backuping.");
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) currently in {serverStatus.ToString()} state, not able to backup.");
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
                await message.Channel.SendMessageAsync($"Usage: {Configs.GetBotPrefix()}wgsm backup `<SERVERID>`");
            }
        }

        private async Task Action_Update(SocketMessage message, string command)
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
                        if (serverStatus == MainWindow.ServerStatus.Stopped)
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) Update started - this may take some time.");
                            bool updated = await WindowsGSM.UpdateServerById(args[1], message.Author.Id.ToString(), message.Author.Username);
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) {(updated ? "Updated" : "Fail to Update")}.");
                        }
                        else if (serverStatus == MainWindow.ServerStatus.Updating)
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) already Updating.");
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) currently in {serverStatus.ToString()} state, not able to update.");
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
                await message.Channel.SendMessageAsync($"Usage: {Configs.GetBotPrefix()}wgsm update `<SERVERID>`");
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
            embed.AddField("Command", $"{prefix}wgsm check\n{prefix}wgsm list\n{prefix}wgsm start <SERVERID>\n{prefix}wgsm stop <SERVERID>\n{prefix}wgsm restart <SERVERID>\n{prefix}wgsm send <SERVERID> <COMMAND>\n{prefix}wgsm backup <SERVERID>\n{prefix}wgsm update <SERVERID>", inline: true);
            embed.AddField("Usage", "Check permission\nPrint server list with id, status and name\nStart a server remotely by serverId\nStop a server remotely by serverId\nRestart a server remotely by serverId\nSend a command to server console\nBackup a server remotely by serverId\nUpdate a server remotely by serverId", inline: true);

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }
    }
}
