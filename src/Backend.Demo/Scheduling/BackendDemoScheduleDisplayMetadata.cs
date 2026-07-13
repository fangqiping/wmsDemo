namespace Backend.Demo.Scheduling;

public sealed record BackendDemoScheduleDisplayMetadata(
    string OrderType,
    int? OrderId,
    string? OrderCode,
    string? Sku,
    string? Pallet,
    string? SourceLocation,
    string? RequestedSourceLocation,
    string? TargetLocation,
    string? RequestedTargetLocation);
