using Backend.Demo.DependencyInjection;
using FlowEngine.Data.EntityFramework.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Demo;

public sealed class BackendDemoDesignTimeDbContextFactory : IDesignTimeDbContextFactory<DataDbContext> {
    public DataDbContext CreateDbContext(string[] args) {
        var connectionString = args.FirstOrDefault(arg => arg.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            ?? "Data Source=backend-demo.db";

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackendDemoApplication(connectionString);

        var provider = services.BuildServiceProvider();
        return (DataDbContext)provider.GetRequiredService<DbContext>();
    }
}
