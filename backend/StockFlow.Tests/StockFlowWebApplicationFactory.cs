using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockFlow.Api.Data;

namespace StockFlow.Tests;

public class StockFlowWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public StockFlowWebApplicationFactory()
    {
        _connectionString = Environment.GetEnvironmentVariable("STOCKFLOW_TEST_DB_CONNECTION")
            ?? throw new InvalidOperationException("STOCKFLOW_TEST_DB_CONNECTION is required for integration tests.");

        if (!_connectionString.Contains("stockflow_test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Test connection string must target a database containing 'stockflow_test'.");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        System.IO.File.AppendAllText("/tmp/cfg.log", "CONFIGURE WEB HOST CALLED\n");
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            Console.WriteLine("CONFIGURE SERVICES CALLED");
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<StockFlowDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<StockFlowDbContext>(opt =>
                opt.UseNpgsql(_connectionString, npg => npg.MigrationsAssembly("StockFlow.Api")));

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StockFlowDbContext>();
            Console.WriteLine("BEFORE MIGRATE");
            db.Database.Migrate();
            Console.WriteLine("AFTER MIGRATE");
        });
    }

    public StockFlowDbContext CreateDbContext()
    {
        var opts = new DbContextOptionsBuilder<StockFlowDbContext>()
            .UseNpgsql(_connectionString, npg => npg.MigrationsAssembly("StockFlow.Api")).Options;
        return new StockFlowDbContext(opts);
    }
}
