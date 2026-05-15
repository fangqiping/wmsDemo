using FlowEngine.Data;
using FlowEngine.Execution;
using FlowEngine.Execution.Consoles;
using FlowEngine.Execution.Events;
using FlowEngine.Execution.Executors;
using FlowEngine.Notifications;

namespace Backend.Demo;

public sealed class StackCraneConsole : ConsoleBase {
    public const string NAME = "StackCraneConsole";

    public StackCraneConsole(
        ILogger<StackCraneConsole> logger,
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
