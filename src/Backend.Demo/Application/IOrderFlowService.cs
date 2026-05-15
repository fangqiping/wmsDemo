using Backend.Demo.Domain;

namespace Backend.Demo.Application;

public interface IOrderFlowService {
    Task<InboundOrder> StartInboundFlowAsync(int orderId);
    Task<OutboundOrder> StartOutboundFlowAsync(int orderId);
}
