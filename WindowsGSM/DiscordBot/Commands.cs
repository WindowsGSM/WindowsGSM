using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WindowsGSM.Functions;

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
                List<string> serverIds = Configs.GetServerIdsByAdminId(message.Author.Id.ToString());

                switch (splits[0])
                {
                    case "check":
                        await Action_Check(message, serverIds);
                        break;

                    case "list":
                        await Action_List(message, serverIds);
                        break;

                    case "stats":
                        await Action_Stats(message);
                        break;
                    case "start":
                    case "stop":
                    case "restart":
                    case "send":
                    case "backup":
                    case "update":
                    case "stats":
                        if (Author_Server_Permission_Check(serverIds, splits[1]))
                        {
                            switch (splits[0])
                            {
                                case "start": await Action_Start(message, args[1]); break;
                                case "stop": await Action_Stop(message, args[1]); break;
                                case "restart": await Action_Restart(message, args[1]); break;
                                case "send": await Action_SendCommand(message, args[1]); break;
                                case "backup": await Action_Backup(message, args[1]); break;
                                case "update": await Action_Update(message, args[1]); break;
                                case "stats": await Action_Stats(message); break;
                            }
                        }
                        else
                        {
                            await Send_No_Permission(message);
                        }
                        break;
                    default: await SendHelpEmbed(message); break;
                }
            }
        }

        // Authorization check for server-level commands
        private bool Author_Server_Permission_Check(List<string> serverIds, string targetId)
        {
            return serverIds.Contains("0") || serverIds.Contains(targetId);
        }

        private async Task Send_No_Permission(SocketMessage message)
        {
            await message.Channel.SendMessageAsync("You don't have permission to access.");
        }

        private async Task Action_Check(SocketMessage message, List<string> serverIds)
        {
            await message.Channel.SendMessageAsync(
                serverIds.Contains("0") ?
                "You have full permission.\nCommands: `check`, `list`, `start`, `stop`, `restart`, `send`, `backup`, `update`, `stats`" :
                $"You have permission on servers (`{string.Join(",", serverIds.ToArray())}`)\nCommands: `check`, `start`, `stop`, `restart`, `send`, `backup`, `update`, `stats`");
        }

        private async Task Action_List(SocketMessage message, List<string> serverIds)
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
                    if (!Author_Server_Permission_Check(serverIds, id))
                    {
                        continue; // skip servers the user cannot access
                    }

                    ids += $"`{id}`\n";
                    status += $"`{state}`\n";
                    servers += $"`{server}`\n";
                }

                if (string.IsNullOrEmpty(ids))
                {
                    await message.Channel.SendMessageAsync("You don't have permission to view any servers.");
                    return;
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
                            await message.Channel.SendMessageAsync($"Server (ID: {args[1]}) currently in {serverStatus} state, not able to update.");
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

        private async Task Action_Stats(SocketMessage message)
        {
            var system = new SystemMetrics();
            await Task.Run(() => system.GetCPUStaticInfo());
            await Task.Run(() => system.GetRAMStaticInfo());
            await Task.Run(() => system.GetDiskStaticInfo());

            await message.Channel.SendMessageAsync(embed: (await GetMessageEmbed(system)).Build());
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
            embed.AddField("Command", $"{prefix}wgsm check\n{prefix}wgsm list\n{prefix}wgsm start <SERVERID>\n{prefix}wgsm stop <SERVERID>\n{prefix}wgsm restart <SERVERID>\n{prefix}wgsm update <SERVERID>\n{prefix}wgsm send <SERVERID> <COMMAND>\n{prefix}wgsm backup <SERVERID>\n{prefix}wgsm stats", inline: true);
            embed.AddField("Usage", "Check permission\nPrint server list with id, status and name\nStart a server remotely by serverId\nStop a server remotely by serverId\nRestart a server remotely by serverId\nSend a command to server console\nBackup a server remotely by serverId\nUpdate a server remotely by serverId", inline: true);

            await message.Channel.SendMessageAsync(embed: embed.Build());
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

        private async Task<EmbedBuilder> GetMessageEmbed(SystemMetrics system)
        {
            var embed = new EmbedBuilder
            {
                Title = ":small_orange_diamond: System Metrics",
                Description = $"Server name: {Environment.MachineName}",
                Color = Color.Blue
            };

            embed.AddField("CPU", GetProgressBar(await Task.Run(() => system.GetCPUUsage())), true);
            double ramUsage = await Task.Run(() => system.GetRAMUsage());
            embed.AddField("Memory: " + SystemMetrics.GetMemoryRatioString(ramUsage, system.RAMTotalSize), GetProgressBar(ramUsage), true);
            double diskUsage = await Task.Run(() => system.GetDiskUsage());
            embed.AddField("Disk: " + SystemMetrics.GetDiskRatioString(diskUsage, system.DiskTotalSize), GetProgressBar(diskUsage), true);

            (int serverCount, int startedCount, int activePlayers) = await GetGameServerDashBoardDetails();
            embed.AddField($"Servers: {serverCount}/{MainWindow.MAX_SERVER}", GetProgressBar(serverCount * 100 / MainWindow.MAX_SERVER), true);
            embed.AddField($"Online: {startedCount}/{serverCount}", GetProgressBar((serverCount == 0) ? 0 : startedCount * 100 / serverCount), true);
            embed.AddField("Active Players", GetActivePlayersString(activePlayers), true);

            embed.WithFooter(new EmbedFooterBuilder().WithIconUrl("https://github.com/WindowsGSM/WindowsGSM/raw/master/WindowsGSM/Images/WindowsGSM.png").WithText($"WindowsGSM {MainWindow.WGSM_VERSION} | System Metrics"));
            embed.WithCurrentTimestamp();

            return embed;
        }
    }
}
