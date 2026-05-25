using AutoMapper;
using Backend.Demo.Contracts.MasterData;
using Backend.Demo.Domain;
using FlowEngine.Data;
using FlowApiController = FlowEngine.Server.WebApi.ApiController<int, Backend.Demo.Domain.Pallet, Backend.Demo.Contracts.MasterData.PalletModel, Backend.Demo.Contracts.MasterData.PalletModel>;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Demo.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PalletsController : FlowApiController {
    public PalletsController(ILogger<PalletsController> logger, IManager<int, Pallet> manager, IMapper mapper)
        : base(logger, manager, mapper) {
    }

    public sealed class PalletModelProfile : Profile {
        public PalletModelProfile() {
            CreateMap<PalletModel, Pallet>();
            CreateMap<Pallet, PalletModel>();
        }
    }
}
