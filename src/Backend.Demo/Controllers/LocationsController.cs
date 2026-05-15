using AutoMapper;
using Backend.Demo.Contracts.MasterData;
using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowApiController = FlowEngine.Server.WebApi.ApiController<int, Backend.Demo.Domain.Location, Backend.Demo.Contracts.MasterData.LocationModel, Backend.Demo.Contracts.MasterData.LocationModel>;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Demo.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class LocationsController : FlowApiController {
    public LocationsController(ILogger<LocationsController> logger, IManager<int, Location> manager, IMapper mapper)
        : base(logger, manager, mapper) {
    }

    public sealed class LocationModelProfile : Profile {
        public LocationModelProfile() {
            CreateMap<LocationModel, Location>()
                .ForMember(entity => entity.LocationType, opt => opt.MapFrom(model => (LocationType)model.LocationType));
            CreateMap<Location, LocationModel>()
                .ForMember(model => model.LocationType, opt => opt.MapFrom(entity => (int)entity.LocationType));
        }
    }
}
