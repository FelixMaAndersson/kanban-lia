namespace kanban_lia.Models.Events
{
    public record PlacementCreatedEvent(
        Guid[] EntityIds,
        IEnumerable<PlacementChange> Changes
    );
}