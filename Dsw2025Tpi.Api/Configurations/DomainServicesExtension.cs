using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Data;
using Dsw2025Tpi.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Dsw2025Tpi.Domain.Interfaces;

namespace Dsw2025Tpi.Api.Configurations;

public static class DomainServicesConfigurationExtension
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<Dsw2025TpiContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("Dsw2025TpiDb"));
            
        });

        services.AddScoped<IRepository, EfRepository>();
        services.AddTransient<ProductService>();
        services.AddTransient<OrderService>();

        return services;
    }
}
