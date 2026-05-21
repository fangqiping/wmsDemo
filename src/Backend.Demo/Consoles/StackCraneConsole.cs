using FlowEngine.Data;
using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Execution;
using FlowEngine.Execution.Consoles;
using FlowEngine.Execution.Events;
using FlowEngine.Execution.Executors;
using FlowEngine.Notifications;

namespace Backend.Demo;

public sealed class StackCraneConsole : ConsoleBase {
    public const string NAME = "StackCraneConsole";
    private readonly IManagerFactory<int, Location> _locationManagerFactory;

    public StackCraneConsole(
        ILogger<StackCraneConsole> logger,
        IManagerFactory<int, Location> locationManagerFactory,
        IManagerFactory<string, ConsoleInfo> managerFactory,
        IOperationTaskStore taskStore,
        IExecutableEventProducer executableEventProducer,
        INotifier notifier)
        : base(
            logger,
            NAME,
            disableOnTaskFailed: false,
            disableOnTaskCanceled: false,
            Executor.Queue<IOperationTask>(1),
            managerFactory,
            taskStore,
            executableEventProducer,
            notifier) {
        _locationManagerFactory = locationManagerFactory;
    }

    public async Task UpdateLocationStatusAsync(int locationId, LocationStatus status) {
        using var scopedManager = _locationManagerFactory.Create();
        await scopedManager.Service.UpdateAsync(locationId, location => location.Status = status);
    }
}
