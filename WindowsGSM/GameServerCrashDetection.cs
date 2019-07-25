using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace WindowsGSM
{
    class GameServerCrashDetection
    {
        public async Task<bool> IsServerCrashed(Process process, string serverGame)
        {
            int exitCode;
            switch (serverGame)
            {
                case ("Counter-Strike: Global Offensive Dedicated Server"):
                    exitCode = 0; break;
                case ("Garry's Mod Dedicated Server"):
                    exitCode = 0; break;
                case ("Team Fortress 2 Dedicated Server"):
                    exitCode = 0; break;
                case ("Minecraft Pocket Edition Server | PocketMine-MP"):
                    exitCode = 0; break;
                case ("Rust Dedicated Server"):
                    exitCode = -1; break;
                default: return false;
            }

            while (process != null)
            {
                if (process.HasExited)
                {
                    return (process.ExitCode != exitCode);
                }

                await Task.Delay(1000);
            }

            return false;
        }
    }
}
