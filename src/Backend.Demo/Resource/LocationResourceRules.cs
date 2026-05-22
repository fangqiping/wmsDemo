using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Execution.Resource;

namespace Backend.Demo.Resource;

[Rule(AcquireEmptyRackLocationTask.RULE_NAME)]
public sealed class EmptyRackLocationRule : IRule<int, Location, EmptyRackLocationRequest> {
    public async Task ApplyAsync(
        IResourceContext<int, Location, EmptyRackLocationRequest> context,
        IEnumerable<EmptyRackLocationRequest> requests) {
        var requestList = requests.ToList();
        var reservedIds = new HashSet<int>();

        foreach (var request in requestList) {
            var candidates = (await context.Manager.GetAsync(
                search: locations => locations.Where(location =>
                    location.Enabled
                    && !location.Acquired
                    && location.LocationType == LocationType.Rack
                    && location.Status == LocationStatus.Empty
                    && location.CurrentPalletId == null
                    && location.WarehouseId == request.WarehouseId),
                sort: locations => locations.OrderBy(location => location.Id)))
                .Where(location => !reservedIds.Contains(location.Id))
                .ToArray();

            var selected = request.PreferredLocationId > 0
                ? candidates.FirstOrDefault(location => location.Id == request.PreferredLocationId) ?? candidates.FirstOrDefault()
                : candidates.FirstOrDefault();
            if (selected == null) {
                continue;
            }

            reservedIds.Add(selected.Id);
            context.Succeed(request, selected.Id);
        }
    }
}

[Rule(AcquireOccupiedRackLocationTask.RULE_NAME)]
public sealed class OccupiedRackLocationRule : IRule<int, Location, OccupiedRackLocationRequest> {
    public async Task ApplyAsync(
        IResourceContext<int, Location, OccupiedRackLocationRequest> context,
        IEnumerable<OccupiedRackLocationRequest> requests) {
        var requestList = requests.ToList();
        var reservedIds = new HashSet<int>();

        foreach (var request in requestList) {
            var candidates = (await context.Manager.GetAsync(
                search: locations => locations.Where(location =>
                    location.Enabled
                    && !location.Acquired
                    && location.LocationType == LocationType.Rack
                    && location.Status == LocationStatus.Occupied
                    && location.CurrentPalletId != null
                    && location.WarehouseId == request.WarehouseId
                    && location.CurrentPallet != null
                    && location.CurrentPallet.SkuId == request.SkuId),
                sort: locations => locations.OrderBy(location => location.Id)))
                .Where(location => !reservedIds.Contains(location.Id))
                .ToArray();

            var selected = request.PreferredLocationId > 0
                ? candidates.FirstOrDefault(location => location.Id == request.PreferredLocationId) ?? candidates.FirstOrDefault()
                : candidates.FirstOrDefault();
            if (selected == null) {
                continue;
            }

            reservedIds.Add(selected.Id);
            context.Succeed(request, selected.Id);
        }
    }
}
