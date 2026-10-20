using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Services.Placements.DTOs
{
    public sealed record PlacementFailure(EntityId EntityId, string Error);

    public sealed record PlacementCreateResult(
        IReadOnlyList<EntityId> SucceededEntityIds,
        IReadOnlyList<PlacementFailure> Failures);
}
