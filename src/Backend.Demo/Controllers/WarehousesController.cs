using AutoMapper;
using Backend.Demo.Contracts.MasterData;
using Backend.Demo.Domain;
using FlowEngine.Data;
using FlowApiController = FlowEngine.Server.WebApi.ApiController<int, Backend.Demo.Domain.Warehouse, Backend.Demo.Contracts.MasterData.WarehouseModel, Backend.Demo.Contracts.MasterData.WarehouseModel>;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Demo.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WarehousesController : FlowApiController {
    public WarehousesController(ILogger<WarehousesController> logger, IManager<int, Warehouse> manager, IMapper mapper)
        : base(logger, manager, mapper) {
    }

    public sealed class WarehouseModelProfile : Profile {
        public WarehouseModelProfile() {
            CreateMap<WarehouseModel, Warehouse>();
            CreateMap<Warehouse, WarehouseModel>();
        }
    }
}
