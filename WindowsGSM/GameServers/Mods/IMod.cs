namespace WindowsGSM.GameServers.Mods
{
    public interface IMod
    {
        public string Name { get; }

        public string Description { get; }

        public Type ConfigType { get; }

        public string GetLocalVersion(IGameServer gameServer);

        public Task<List<string>> GetVersions();

        public Task Install(IGameServer gameServer, string version);

        public Task Update(IGameServer gameServer, string version);

        public Task Delete(IGameServer gameServer);
    }
}
