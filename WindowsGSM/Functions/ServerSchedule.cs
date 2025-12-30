        public static async Task<bool> ScheduleRestartAsync(string serverId, string cronExpression)
        {
            try
            {
                await Task.Run(() =>
                {
                    var serverConfig = new ServerConfig(serverId);
                    serverConfig.RestartCrontab = true;
                    serverConfig.CrontabFormat = cronExpression;
                    ServerConfig.SetSetting(serverId, ServerConfig.SettingName.RestartCrontab, "1");
                    ServerConfig.SetSetting(serverId, ServerConfig.SettingName.CrontabFormat, cronExpression);
                });
                return true;
            }
            catch
            {
                return false;
            }
        }