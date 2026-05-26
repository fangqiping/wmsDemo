using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using Backend.Demo.Seeding;
using FlowEngine.Data;
using FlowEngine.Data.EntityFramework.Storage;
using FlowEngine.Execution.Design;
using Microsoft.EntityFrameworkCore;

namespace Backend.Demo.DependencyInjection;

public sealed class BackendDemoInitializer : IBackendDemoInitializer {
    private readonly ILogger<BackendDemoInitializer> _logger;
    private readonly DbContext _dbContext;
    private readonly IManager _manager;
    private readonly IFlowDraftService _flowDraftService;
    private readonly IFlowPublisher _flowPublisher;
    private readonly IPublishedFlowProvider _publishedFlowProvider;

    public BackendDemoInitializer(
        ILogger<BackendDemoInitializer> logger,
        DbContext dbContext,
        IManager manager,
        IFlowDraftService flowDraftService,
        IFlowPublisher flowPublisher,
        IPublishedFlowProvider publishedFlowProvider) {
        _logger = logger;
        _dbContext = dbContext;
        _manager = manager;
        _flowDraftService = flowDraftService;
        _flowPublisher = flowPublisher;
        _publishedFlowProvider = publishedFlowProvider;
    }

    public async Task InitializeAsync() {
        await ResetLegacySqliteDatabaseAsync();
        await _dbContext.Database.MigrateAsync();

        await EnsureMasterDataAsync();
        await EnsureFlowBindingsAsync();
        await EnsureFlowPublishedAsync(BackendDemoFlowSeeds.CreateInboundDraft);
        await EnsureFlowPublishedAsync(BackendDemoFlowSeeds.CreateOutboundDraft);
        await _publishedFlowProvider.RefreshAsync();
    }

    private async Task ResetLegacySqliteDatabaseAsync() {
        if (!string.Equals(_dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal)) {
            return;
        }

        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose) {
            await connection.OpenAsync();
        }

        try {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                select count(*)
                from sqlite_master
                where type = 'table'
                  and name not like 'sqlite_%'
                  and name != '__EFMigrationsHistory';
                """;
            var userTableCount = Convert.ToInt64(await command.ExecuteScalarAsync() ?? 0L);

            await using var historyCommand = connection.CreateCommand();
            historyCommand.CommandText = """
                select count(*)
                from sqlite_master
                where type = 'table'
                  and name = '__EFMigrationsHistory';
                """;
            var migrationHistoryCount = Convert.ToInt64(await historyCommand.ExecuteScalarAsync() ?? 0L);

            if (userTableCount > 0 && migrationHistoryCount == 0) {
                _logger.LogWarning("Detected legacy SQLite database created without EF migrations. Recreating demo database.");
                await _dbContext.Database.EnsureDeletedAsync();
            }
        } finally {
            if (shouldClose) {
                await connection.CloseAsync();
            }
        }
    }

    private async Task EnsureMasterDataAsync() {
        if (await _manager.AnyAsync<int, Warehouse>()) {
            return;
        }

        var warehouse = await _manager.AddAsync<int, Warehouse>(new Warehouse {
            Code = "WH-01",
            Name = "Demo Warehouse"
        });

        await _manager.AddAsync<int, Location>(new Location {
            Code = "IN-01",
            Name = "Inbound Station 01",
            Enabled = true,
            Acquired = false,
            LocationType = LocationType.InboundStation,
            Status = LocationStatus.Available,
            WarehouseId = warehouse.Id
        });
        await _manager.AddAsync<int, Port>(new Port {
            Code = "IN-PORT-01",
            Name = "Inbound Port 01",
            Enabled = true,
            Acquired = false,
            PortType = PortType.Inbound,
            Status = PortStatus.Idle,
            WarehouseId = warehouse.Id
        });
        await _manager.AddAsync<int, Location>(new Location {
            Code = "RACK-A1",
            Name = "Rack A1",
            Enabled = true,
            Acquired = false,
            LocationType = LocationType.Rack,
            Status = LocationStatus.Empty,
            WarehouseId = warehouse.Id
        });
        var sku001 = await _manager.AddAsync<int, Sku>(new Sku {
            Code = "SKU-001",
            Name = "Demo Tote",
            Spec = "Blue / 600x400"
        });
        await _manager.AddAsync<int, Sku>(new Sku {
            Code = "SKU-002",
            Name = "Demo Carton",
            Spec = "Brown / 300x200"
        });
        var palletA2 = await _manager.AddAsync<int, Pallet>(new Pallet {
            Code = "PLT-SEED-RACK-A2",
            Enabled = true,
            Acquired = false,
            SkuId = sku001.Id,
            Quantity = 1
        });
        await _manager.AddAsync<int, Location>(new Location {
            Code = "RACK-A2",
            Name = "Rack A2",
            Enabled = true,
            Acquired = false,
            LocationType = LocationType.Rack,
            Status = LocationStatus.Occupied,
            CurrentPalletId = palletA2.Id,
            WarehouseId = warehouse.Id
        });
        await _manager.AddAsync<int, Location>(new Location {
            Code = "OUT-01",
            Name = "Outbound Station 01",
            Enabled = true,
            Acquired = false,
            LocationType = LocationType.OutboundStation,
            Status = LocationStatus.Available,
            WarehouseId = warehouse.Id
        });
        await _manager.AddAsync<int, Port>(new Port {
            Code = "OUT-PORT-01",
            Name = "Outbound Port 01",
            Enabled = true,
            Acquired = false,
            PortType = PortType.Outbound,
            Status = PortStatus.Idle,
            WarehouseId = warehouse.Id
        });
    }

    private async Task EnsureFlowBindingsAsync() {
        if (await _manager.AnyAsync<int, FlowBinding>()) {
            return;
        }

        var transaction = _manager.CreateTransaction();
        await transaction.AddAsync<int, FlowBinding>(new FlowBinding {
            BusinessType = BusinessFlowType.InboundOrder,
            FlowDefinitionCode = BackendDemoFlowSeeds.InboundFlowCode,
            Enabled = true
        });
        await transaction.AddAsync<int, FlowBinding>(new FlowBinding {
            BusinessType = BusinessFlowType.OutboundOrder,
            FlowDefinitionCode = BackendDemoFlowSeeds.OutboundFlowCode,
            Enabled = true
        });
        await transaction.CommitAsync();
    }

    private async Task EnsureFlowPublishedAsync(Func<int, SaveFlowDraftRequest> draftFactory) {
        var draftRequest = draftFactory(0);
        var existingDraft = await _manager.GetByIdAsync<string, FlowDraft>(draftRequest.Code);
        var definition = await _manager.GetByIdAsync<string, FlowDefinition>(draftRequest.Code);
        FlowDraftDetail? currentDraft = existingDraft == null
            ? null
            : new FlowDraftDetail {
                Code = draftRequest.Code,
                Name = draftRequest.Name,
                Description = draftRequest.Description,
                Revision = existingDraft.Revision,
                DraftDocumentJson = existingDraft.DraftDocumentJson,
                UpdatedAt = existingDraft.UpdatedAt,
                UpdatedBy = existingDraft.UpdatedBy
            };

        var requiresDraftUpdate = existingDraft == null
            || existingDraft.DraftDocumentJson != draftRequest.DraftDocumentJson
            || definition?.Name != draftRequest.Name
            || definition.Description != draftRequest.Description;

        if (requiresDraftUpdate) {
            draftRequest.Revision = existingDraft?.Revision ?? 0;
            currentDraft = await _flowDraftService.SaveDraftAsync(draftRequest);
        }

        if (currentDraft == null) {
            throw new InvalidOperationException($"FlowDraft-{draftRequest.Code} was not created.");
        }

        if (definition?.ActiveVersionId is not long activeVersionId) {
            await PublishDraftAsync(currentDraft);
            return;
        }

        var activeVersion = await _manager.GetByIdAsync<long, FlowVersion>(activeVersionId);
        if (activeVersion == null || activeVersion.SourceDraftRevision < currentDraft.Revision) {
            await PublishDraftAsync(currentDraft);
        }
    }

    private Task PublishDraftAsync(FlowDraftDetail draft) {
        return _flowPublisher.PublishAsync(new PublishFlowRequest {
            Code = draft.Code,
            ExpectedRevision = draft.Revision,
            PublishedBy = "backend-demo"
        });
    }
}
