using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using Backend.Demo.Seeding;
using FlowEngine.Data;
using FlowEngine.Data.EntityFramework.Storage;
using FlowEngine.Execution.Design;
using Microsoft.EntityFrameworkCore;

namespace Backend.Demo.DependencyInjection;

public sealed class BackendDemoInitializer : IBackendDemoInitializer {
    private readonly DbContext _dbContext;
    private readonly IManager _manager;
    private readonly IFlowDraftService _flowDraftService;
    private readonly IFlowPublisher _flowPublisher;
    private readonly IPublishedFlowProvider _publishedFlowProvider;

    public BackendDemoInitializer(
        DbContext dbContext,
        IManager manager,
        IFlowDraftService flowDraftService,
        IFlowPublisher flowPublisher,
        IPublishedFlowProvider publishedFlowProvider) {
        _dbContext = dbContext;
        _manager = manager;
        _flowDraftService = flowDraftService;
        _flowPublisher = flowPublisher;
        _publishedFlowProvider = publishedFlowProvider;
    }

    public async Task InitializeAsync() {
        await _dbContext.Database.EnsureCreatedAsync();

        await EnsureMasterDataAsync();
        await EnsureFlowBindingsAsync();
        await EnsureFlowPublishedAsync(BackendDemoFlowSeeds.CreateInboundDraft);
        await EnsureFlowPublishedAsync(BackendDemoFlowSeeds.CreateOutboundDraft);
        await _publishedFlowProvider.RefreshAsync();
    }

    private async Task EnsureMasterDataAsync() {
        if (await _manager.AnyAsync<int, Warehouse>()) {
            return;
        }

        var warehouse = await _manager.AddAsync<int, Warehouse>(new Warehouse {
            Code = "WH-01",
            Name = "Demo Warehouse"
        });

        var transaction = _manager.CreateTransaction();
        await transaction.AddAsync<int, Location>(new Location {
            Code = "IN-01",
            Name = "Inbound Station 01",
            LocationType = LocationType.InboundStation,
            WarehouseId = warehouse.Id
        });
        await transaction.AddAsync<int, Location>(new Location {
            Code = "RACK-A1",
            Name = "Rack A1",
            LocationType = LocationType.Rack,
            WarehouseId = warehouse.Id
        });
        await transaction.AddAsync<int, Location>(new Location {
            Code = "OUT-01",
            Name = "Outbound Station 01",
            LocationType = LocationType.OutboundStation,
            WarehouseId = warehouse.Id
        });

        await transaction.AddAsync<int, Sku>(new Sku {
            Code = "SKU-001",
            Name = "Demo Tote",
            Spec = "Blue / 600x400"
        });
        await transaction.AddAsync<int, Sku>(new Sku {
            Code = "SKU-002",
            Name = "Demo Carton",
            Spec = "Brown / 300x200"
        });

        await transaction.CommitAsync();
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
        var definition = await _manager.GetByIdAsync<string, FlowDefinition>(draftRequest.Code);
        if (definition is { ActiveVersionId: not null }) {
            return;
        }

        var existingDraft = await _manager.GetByIdAsync<string, FlowDraft>(draftRequest.Code);
        draftRequest.Revision = existingDraft?.Revision ?? 0;

        var draft = await _flowDraftService.SaveDraftAsync(draftRequest);
        await _flowPublisher.PublishAsync(new PublishFlowRequest {
            Code = draft.Code,
            ExpectedRevision = draft.Revision,
            PublishedBy = "backend-demo"
        });
    }
}
