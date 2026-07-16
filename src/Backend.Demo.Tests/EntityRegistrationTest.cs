using Backend.Demo.DependencyInjection;
using Backend.Demo.Domain;
using Backend.Demo.Scheduling;
using FlowEngine.Data;
using FlowEngine.Execution.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Demo.Tests;

public sealed class EntityRegistrationTest {
    [Fact]
    public void DomainEntities_AreDiscoverableByFlowEngineDataLayer() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackendDemoApplication("Data Source=:memory:");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IManager<int, Warehouse>>();
        scope.ServiceProvider.GetRequiredService<IManager<int, Location>>();
        scope.ServiceProvider.GetRequiredService<IManager<int, Port>>();
        scope.ServiceProvider.GetRequiredService<IManager<int, Pallet>>();
        scope.ServiceProvider.GetRequiredService<IManager<int, Sku>>();
        scope.ServiceProvider.GetRequiredService<IManager<int, InboundOrder>>();
        scope.ServiceProvider.GetRequiredService<IManager<int, OutboundOrder>>();
        scope.ServiceProvider.GetRequiredService<IManager<int, FlowBinding>>();
    }

    [Fact]
    public void SchedulingServices_AreRegisteredByBackendDemoApplication() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackendDemoApplication("Data Source=:memory:");

        using var provider = services.BuildServiceProvider(true);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ISchedulePlanStore>();
        provider.GetRequiredService<IGlobalScheduleCoordinator>();
        provider.GetRequiredService<IScheduleRuntimeFeedback>();
        Assert.Contains(
            scope.ServiceProvider.GetServices<IScheduleCandidateProvider>(),
            candidateProvider => candidateProvider is BackendDemoScheduleCandidateProvider);
    }
}
