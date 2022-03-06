using WindowsGSM.GameServers;

namespace WindowsGSM.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGameServerServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<MCBE>();
        }
    }
}
