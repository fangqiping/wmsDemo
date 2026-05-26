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
    private readonly IManagerFactory _managerFactory;

    public StackCraneConsole(
        ILogger<StackCraneConsole> logger,
        IManagerFactory managerFactory,
        IManagerFactory<string, ConsoleInfo> consoleInfoManagerFactory,
        IOperationTaskStore taskStore,
        IExecutableEventProducer executableEventProducer,
        INotifier notifier)
        : base(
            logger,
            NAME,
            disableOnTaskFailed: false,
            disableOnTaskCanceled: false,
            Executor.Queue<IOperationTask>(1),
            consoleInfoManagerFactory,
            taskStore,
            executableEventProducer,
            notifier) {
        _managerFactory = managerFactory;
    }

    public async Task UpdateLocationStatusAsync(int locationId, LocationStatus status) {
        using var scopedManager = _managerFactory.Create();
        await scopedManager.Service.UpdateAsync<int, Location>(locationId, location => location.Status = status);
    }

    public async Task MoveToRackAsync(int locationId) {
        using var scopedManager = _managerFactory.Create();
        var location = await scopedManager.Service.GetByIdAsync<int, Location>(locationId)
            ?? throw new InvalidOperationException($"Location-{locationId} not found.");
        if (!location.Enabled) {
            throw new InvalidOperationException($"Location-{locationId} is disabled.");
        }
    }

    public async Task RetrieveAsync(int locationId, int palletId) {
        using var scopedManager = _managerFactory.Create();
        var transaction = scopedManager.Service.CreateTransaction();
        await transaction.UpdateAsync<int, Location>(locationId, location => {
            location.Status = LocationStatus.Empty;
            location.CurrentPalletId = null;
        });
        await transaction.UpdateAsync<int, Pallet>(palletId, pallet => {
            pallet.Enabled = false;
        });
        await transaction.CommitAsync();
    }
}
