using AutoMapper;
using Backend.Demo.Application;
using Backend.Demo.Contracts.Orders;
using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowApiController = FlowEngine.Server.WebApi.ApiController<int, Backend.Demo.Domain.OutboundOrder, Backend.Demo.Contracts.Orders.OutboundOrderModel, Backend.Demo.Contracts.Orders.OutboundOrderModel>;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Demo.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OutboundOrdersController : FlowApiController {
    private static readonly Func<IQueryable<OutboundOrder>, IQueryable<OutboundOrder>> INCLUDE =
        orders => orders.Include(entity => entity.Lines);

    private readonly IOrderFlowService _orderFlowService;

    public OutboundOrdersController(
        ILogger<OutboundOrdersController> logger,
        IManager<int, OutboundOrder> manager,
        IMapper mapper,
        IOrderFlowService orderFlowService)
        : base(logger, manager, mapper, INCLUDE) {
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
