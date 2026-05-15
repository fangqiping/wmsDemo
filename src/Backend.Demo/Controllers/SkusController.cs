using AutoMapper;
using Backend.Demo.Contracts.MasterData;
using Backend.Demo.Domain;
using FlowEngine.Data;
using FlowApiController = FlowEngine.Server.WebApi.ApiController<int, Backend.Demo.Domain.Sku, Backend.Demo.Contracts.MasterData.SkuModel, Backend.Demo.Contracts.MasterData.SkuModel>;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Demo.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SkusController : FlowApiController {
    public SkusController(ILogger<SkusController> logger, IManager<int, Sku> manager, IMapper mapper)
        : base(logger, manager, mapper) {
    }

    public sealed class SkuModelProfile : Profile {
        public SkuModelProfile() {
            CreateMap<SkuModel, Sku>();
            CreateMap<Sku, SkuModel>();
        }
    }
}
