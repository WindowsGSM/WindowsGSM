        public static async Task<bool> KillProcessAsync(int pid)
        {
            try
            {
                await Task.Run(() =>
                {
                    Process process = Process.GetProcessById(pid);
                    process.Kill();
                });
                return true;
            }
            catch
            {
                return false;
            }
        }