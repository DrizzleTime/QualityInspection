using Microsoft.EntityFrameworkCore;

namespace QualityInspection;

public static class ExtendedServices
{
    public static void AddDbService(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MySqlConnection");
        services.AddPooledDbContextFactory<MyDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
    }
}