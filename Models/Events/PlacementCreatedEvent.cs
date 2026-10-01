using kanban_lia.Services.IntegrationEvents.Models;

namespace kanban_lia.Models.Events
{
    public record PlacementCreatedEvent(
        Guid[] EntityIds,
        IEnumerable<PlacementChange> Changes,
        Actor Actor
    );
}