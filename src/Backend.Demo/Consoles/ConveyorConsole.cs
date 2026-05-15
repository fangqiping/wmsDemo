using FlowEngine.Data;
using FlowEngine.Execution;
using FlowEngine.Execution.Consoles;
using FlowEngine.Execution.Events;
using FlowEngine.Execution.Executors;
using FlowEngine.Notifications;

namespace Backend.Demo;

public sealed class ConveyorConsole : ConsoleBase {
    public const string NAME = "ConveyorConsole";

    public ConveyorConsole(
        ILogger<ConveyorConsole> logger,
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
    }
}
