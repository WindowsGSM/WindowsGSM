        public static async Task<bool> ApplyCrontabAsync(string serverId, string cronExpression)
        {
            try
            {
                await Task.Run(() =>
                {
                    var serverConfig = new ServerConfig(serverId);
                    serverConfig.CrontabFormat = cronExpression;
                    ServerConfig.SetSetting(serverId, ServerConfig.SettingName.CrontabFormat, cronExpression);
                });
                return true;
            }
            catch
            {
                return false;
            }
        }