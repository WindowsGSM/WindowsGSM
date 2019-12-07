using System.Threading.Tasks;
using System.Diagnostics;

namespace WindowsGSM.GameServer.Check
{
    class CrashDetection
    {
        public async Task<bool> IsServerCrashed(Process process, string serverGame)
        {
            int exitCode;
            switch (serverGame)
            {
                case (GameServer.CSGO.FullName):
                case (GameServer.GMOD.FullName):
                case (GameServer.TF2.FullName):
                case (GameServer.MCPE.FullName):
                case (GameServer.CS.FullName):
                case (GameServer.CSCZ.FullName):
                case (GameServer.HL2DM.FullName):
                case (GameServer.L4D2.FullName):
                    exitCode = 0; break;
                case (GameServer.RUST.FullName):
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
