using AutoMapper;
using Backend.Demo.Contracts.MasterData;
using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowApiController = FlowEngine.Server.WebApi.ApiController<int, Backend.Demo.Domain.Port, Backend.Demo.Contracts.MasterData.PortModel, Backend.Demo.Contracts.MasterData.PortModel>;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Demo.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PortsController : FlowApiController {
    public PortsController(ILogger<PortsController> logger, IManager<int, Port> manager, IMapper mapper)
        : base(logger, manager, mapper) {
    }

    public sealed class PortModelProfile : Profile {
        public PortModelProfile() {
            CreateMap<PortModel, Port>()
                .ForMember(entity => entity.PortType, opt => opt.MapFrom(model => (PortType)model.PortType))
                .ForMember(entity => entity.Status, opt => opt.MapFrom(model => (PortStatus)model.Status));
            CreateMap<Port, PortModel>()
                .ForMember(model => model.PortType, opt => opt.MapFrom(entity => (int)entity.PortType))
                .ForMember(model => model.Status, opt => opt.MapFrom(entity => (int)entity.Status));
        }
    }
}
