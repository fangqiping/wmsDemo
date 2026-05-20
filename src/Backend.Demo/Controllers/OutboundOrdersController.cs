using AutoMapper;
using Backend.Demo.Application;
using Backend.Demo.Contracts.Orders;
using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowEngine.Execution;
using FlowEngine.Execution.FlowEngine;
using FlowApiController = FlowEngine.Server.WebApi.ApiController<int, Backend.Demo.Domain.OutboundOrder, Backend.Demo.Contracts.Orders.OutboundOrderModel, Backend.Demo.Contracts.Orders.OutboundOrderModel>;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Demo.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OutboundOrdersController : FlowApiController {
    private static readonly Func<IQueryable<OutboundOrder>, IQueryable<OutboundOrder>> INCLUDE =
        orders => orders.Include(entity => entity.Lines);

    private readonly IManager _rootManager;
    private readonly IOrderFlowService _orderFlowService;

    public OutboundOrdersController(
        ILogger<OutboundOrdersController> logger,
        IManager<int, OutboundOrder> manager,
        IManager rootManager,
        IMapper mapper,
        IOrderFlowService orderFlowService)
        : base(logger, manager, mapper, INCLUDE) {
        _rootManager = rootManager;
        _orderFlowService = orderFlowService;
    }

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> SubmitAsync(int id) {
        var order = await Manager.UpdateAsync(id, entity => {
            entity.Status = OrderStatus.Submitted;
            entity.UpdatedTime = DateTimeOffset.UtcNow;
        });
        return Ok(ToOutput(order));
    }

    [HttpPost("{id}/start-flow")]
    public async Task<IActionResult> StartFlowAsync(int id) {
        var order = await _orderFlowService.StartOutboundFlowAsync(id);
        return Ok(ToOutput(order));
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteAsync(int id) {
        var order = await Manager.UpdateAsync(id, entity => {
            entity.Status = OrderStatus.Completed;
            entity.UpdatedTime = DateTimeOffset.UtcNow;
            entity.CompletedTime = DateTimeOffset.UtcNow;
        });
        return Ok(ToOutput(order));
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelAsync(int id) {
        var order = await Manager.UpdateAsync(id, entity => {
            entity.Status = OrderStatus.Canceled;
            entity.UpdatedTime = DateTimeOffset.UtcNow;
        });
        return Ok(ToOutput(order));
    }

    protected override async Task<OutboundOrder> FromInputAsync(OutboundOrderModel input, Func<Task<OutboundOrder>>? getter) {
        var entity = await base.FromInputAsync(input, getter);
        var now = DateTimeOffset.UtcNow;
        if (getter == null && entity.CreatedTime == default) {
            entity.CreatedTime = now;
        }
        entity.UpdatedTime = now;
        if (getter == null) {
            entity.Status = input.Status == default ? OrderStatus.Draft : (OrderStatus)input.Status;
        }
        return entity;
    }

    protected override OutboundOrderModel ToOutput(OutboundOrder entity) {
        var model = base.ToOutput(entity);
        if (!entity.FlowTaskId.HasValue || model.Status != (int)OrderStatus.Running) {
            return model;
        }

        var flowTaskDetail = _rootManager.GetByIdAsync<long, FlowTaskDetail>(entity.FlowTaskId.Value)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
        if (flowTaskDetail == null) {
            return model;
        }

        if (flowTaskDetail.Status is ExecutableStatus.Completed or ExecutableStatus.Failed or ExecutableStatus.Canceled) {
            model.Status = (int)MapStatus(flowTaskDetail.Status);
            model.CompletedTime = flowTaskDetail.FinishedTime;
        }

        return model;
    }

    private static OrderStatus MapStatus(ExecutableStatus status) {
        return status switch {
            ExecutableStatus.Completed => OrderStatus.Completed,
            ExecutableStatus.Failed => OrderStatus.Failed,
            ExecutableStatus.Canceled => OrderStatus.Canceled,
            _ => OrderStatus.Running,
        };
    }

    public sealed class OutboundOrderModelProfile : Profile {
        public OutboundOrderModelProfile() {
            CreateMap<OutboundOrderLineModel, OutboundOrderLine>();
            CreateMap<OutboundOrderLine, OutboundOrderLineModel>();
            CreateMap<OutboundOrderModel, OutboundOrder>()
                .ForMember(entity => entity.Status, opt => opt.MapFrom(model => (OrderStatus)model.Status));
            CreateMap<OutboundOrder, OutboundOrderModel>()
                .ForMember(model => model.Status, opt => opt.MapFrom(entity => (int)entity.Status));
        }
    }
}
