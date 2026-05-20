using Backend.Demo.Domain;
using FlowEngine.Data.DependencyInjection;
using FlowEngine.Data.EntityFramework.ReaderWriter;
using FlowEngine.Data.EntityFramework.Storage;
using FlowEngine.Data.EntityFramework.Storage.DependencyInjection;

namespace Backend.Demo.DependencyInjection;

public static class BackendDemoDataServiceCollectionExtensions {
    public static IServiceCollection AddBackendDemoData(this IServiceCollection services, string connectionString) {
        services.AddDataDbContext(
            Providers.SQLITE,
            connectionString,
            migrationAssembly: typeof(BackendDemoDataServiceCollectionExtensions).Assembly.GetName().Name,
            discoveryAssembly: typeof(BackendDemoDataServiceCollectionExtensions).Assembly);

        services.AddDataCore(new[] {
            typeof(Warehouse),
            typeof(Location),
            typeof(Sku),
            typeof(InboundOrder),
            typeof(InboundOrderLine),
            typeof(OutboundOrder),
            typeof(OutboundOrderLine),
            typeof(FlowBinding),
        }).UseDbReaderWriter();

        return services;
    }
}
