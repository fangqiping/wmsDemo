using FlowEngine.Execution.Consoles;
using FlowEngine.Execution.DependencyInjection;
using FlowEngine.Execution.Design;
using FlowEngine.Execution.FlowEngine;
using FlowEngine.Execution.Resource;
using FlowEngine.Server.DependencyInjection;
using FlowEngine.Server.Execution;
using Backend.Demo.Application;
using Backend.Demo.Watchers;
using FlowEngine.Data.DependencyInjection;
using FlowEngine.Data.EntityFramework.Storage.DependencyInjection;

namespace Backend.Demo.DependencyInjection;

public static class BackendDemoApplicationServiceCollectionExtensions {
    public static IServiceCollection AddBackendDemoApplication(this IServiceCollection services, string connectionString) {
        services.AddBackendDemoData(connectionString);
        services.AddExecution(typeof(BackendDemoApplicationServiceCollectionExtensions).Assembly);
        services.AddWatchers(new[] { typeof(FlowTaskOrderStatusWatcher) });
        services.AddEntities(
            typeof(ConsoleInfo),
            typeof(OperationTaskDetail),
            typeof(FlowTaskDetail),
            typeof(FlowDefinition),
            typeof(FlowDraft),
            typeof(FlowVersion),
            typeof(VariableEntity),
            typeof(ResourceDetail));
        services.AddConsole<ConveyorConsole>();
        services.AddConsole<StackCraneConsole>();
        services.AddApiControllers(typeof(BackendDemoApplicationServiceCollectionExtensions).Assembly);
        services.AddExecutionControllers();
        services.AddScoped<IOrderFlowService, OrderFlowService>();
        services.AddScoped<IBackendDemoInitializer, BackendDemoInitializer>();
        return services;
    }
}
