using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Services.Placements.Exceptions
{
    public class PlacementNotFoundException : Exception
    {
        public PlacementNotFoundException(EntityId entityId)
    : base($"No current placement found for entity '{entityId.Id}'.")
        {
        }
    }
}
