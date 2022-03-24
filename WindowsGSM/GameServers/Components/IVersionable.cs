namespace WindowsGSM.GameServers.Components
{
    public interface IVersionable
    {
        public Task<List<string>> GetVersions();
    }
}
