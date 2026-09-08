namespace kanban_lia.Models.Events
{
    public record PlacementCreatedEvent(
    Guid EntityId,
    Guid? SourceColumnId,
    Guid TargetColumnId
);
}
