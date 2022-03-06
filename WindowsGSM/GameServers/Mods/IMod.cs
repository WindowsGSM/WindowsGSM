namespace WindowsGSM.GameServers.Mods
{
    public interface IMod
    {
        public Task<(List<string>, string)> GetVersions() => throw new NotImplementedException();

        public Task Create(IGameServer gameServer, string version) => throw new NotImplementedException();

        public Task Update(IGameServer gameServer, string version) => throw new NotImplementedException();

        public Task Delete(IGameServer gameServer) => throw new NotImplementedException();

        public bool Exists(IGameServer gameServer) => throw new NotImplementedException();
    }
}
