using FlowEngine.Execution.Consoles;
using FlowEngine.Execution.DependencyInjection;
using FlowEngine.Execution.Design;
using FlowEngine.Execution.FlowEngine;
using FlowEngine.Execution.Resource;
using FlowEngine.Server.DependencyInjection;
using FlowEngine.Server.Execution;
using Backend.Demo.Application;
using Backend.Demo.Resource;
using Backend.Demo.Scheduling;
using Backend.Demo.Watchers;
using FlowEngine.Data.DependencyInjection;
using FlowEngine.Data.EntityFramework.Storage.DependencyInjection;
using FlowEngine.Execution.Scheduling;
using FlowEngine.Utils.DependencyInjection;

namespace Backend.Demo.DependencyInjection;

public static class BackendDemoApplicationServiceCollectionExtensions {
    public static IServiceCollection AddBackendDemoApplication(this IServiceCollection services, string connectionString) {
        services.AddBackendDemoData(connectionString);
        services.AddExecution(typeof(BackendDemoApplicationServiceCollectionExtensions).Assembly);
        services.AddWatchers(new[] {
            typeof(FlowTaskOrderStatusWatcher),
            typeof(InboundOrderFlowTaskLinkWatcher),
            typeof(OutboundOrderFlowTaskLinkWatcher),
        });
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
        services.AddBackendDemoOperationTemplates();
        services.AddApiControllers(typeof(BackendDemoApplicationServiceCollectionExtensions).Assembly);
        services.AddExecutionControllers();
        services.AddSingleton<IScheduleCandidateProvider, BackendDemoScheduleCandidateProvider>();
        services.AddSingletonHostedService<OrderStatusReconciliationService>();
        services.AddScoped<IOrderFlowService, OrderFlowService>();
        services.AddScoped<IBackendDemoInitializer, BackendDemoInitializer>();
        return services;
    }

    private static IServiceCollection AddBackendDemoOperationTemplates(this IServiceCollection services) {
        services.AddOperationTemplate<AcquireInboundPortTask>("resource-acquire-inbound-port", options => {
            options.Name = "Acquire inbound port";
            options.Description = "Acquires an idle inbound port for the selected warehouse.";
            options.Category = FunctionConsole.NAME;
        });
        services.AddOperationTemplate<AcquireOutboundPortTask>("resource-acquire-outbound-port", options => {
            options.Name = "Acquire outbound port";
            options.Description = "Acquires an idle outbound port for the selected warehouse.";
            options.Category = FunctionConsole.NAME;
        });
        services.AddOperationTemplate<AcquireEmptyRackLocationTask>("resource-acquire-empty-rack", options => {
            options.Name = "Acquire empty rack";
            options.Description = "Acquires an empty rack location, optionally honoring a preferred location.";
            options.Category = FunctionConsole.NAME;
        });
        services.AddOperationTemplate<AcquireOccupiedRackLocationTask>("resource-acquire-occupied-rack", options => {
            options.Name = "Acquire occupied rack";
            options.Description = "Acquires an occupied rack location for the requested SKU.";
            options.Category = FunctionConsole.NAME;
        });
        services.AddOperationTemplate<LoadLocationSnapshotOperationTask>("resource-load-location-snapshot", options => {
            options.Name = "Load location snapshot";
            options.Description = "Loads location and current pallet details.";
            options.Category = FunctionConsole.NAME;
        });
        services.AddOperationTemplate<BindInboundLocationTask>("resource-bind-inbound-location", options => {
            options.Name = "Bind inbound location";
            options.Description = "Creates an inbound pallet and binds it to the target rack.";
            options.Category = FunctionConsole.NAME;
        });
        services.AddOperationTemplate<BindOutboundPortTask>("resource-bind-outbound-port", options => {
            options.Name = "Bind outbound port";
            options.Description = "Moves a source pallet from rack state to outbound port state.";
            options.Category = FunctionConsole.NAME;
        });
        services.AddOperationTemplate<OccupyInboundPortTask>("resource-occupy-inbound-port", options => {
            options.Name = "Occupy inbound port";
            options.Description = "Marks an inbound port as occupied.";
            options.Category = FunctionConsole.NAME;
        });
        services.AddOperationTemplate<ReleaseOutboundPortTask>("resource-release-outbound-port", options => {
            options.Name = "Release outbound port";
            options.Description = "Releases an outbound port and disables the source pallet.";
            options.Category = FunctionConsole.NAME;
        });
        return services;
    }
}
