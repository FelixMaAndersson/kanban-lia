using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Services.Placements.Exceptions
{
    public class PlacementNotFoundException(EntityId entityId) 
        : Exception($"No current placement found for entity '{entityId.Id}'.");
}
