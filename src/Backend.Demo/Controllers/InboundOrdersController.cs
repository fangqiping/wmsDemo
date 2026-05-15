using AutoMapper;
using Backend.Demo.Application;
using Backend.Demo.Contracts.Orders;
using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowApiController = FlowEngine.Server.WebApi.ApiController<int, Backend.Demo.Domain.InboundOrder, Backend.Demo.Contracts.Orders.InboundOrderModel, Backend.Demo.Contracts.Orders.InboundOrderModel>;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Demo.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InboundOrdersController : FlowApiController {
    private static readonly Func<IQueryable<InboundOrder>, IQueryable<InboundOrder>> INCLUDE =
        orders => orders.Include(entity => entity.Lines);

    private readonly IOrderFlowService _orderFlowService;

    public InboundOrdersController(
        ILogger<InboundOrdersController> logger,
        IManager<int, InboundOrder> manager,
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
        var order = await _orderFlowService.StartInboundFlowAsync(id);
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

    protected override async Task<InboundOrder> FromInputAsync(InboundOrderModel input, Func<Task<InboundOrder>>? getter) {
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

    public sealed class InboundOrderModelProfile : Profile {
        public InboundOrderModelProfile() {
            CreateMap<InboundOrderLineModel, InboundOrderLine>();
            CreateMap<InboundOrderLine, InboundOrderLineModel>();
            CreateMap<InboundOrderModel, InboundOrder>()
                .ForMember(entity => entity.Status, opt => opt.MapFrom(model => (OrderStatus)model.Status));
            CreateMap<InboundOrder, InboundOrderModel>()
                .ForMember(model => model.Status, opt => opt.MapFrom(entity => (int)entity.Status));
        }
    }
}
