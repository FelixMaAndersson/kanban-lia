namespace kanban_lia.Models.Events
{
    public record PlacementCreatedEvent(
    Guid[] EntityIds,
    Guid? SourceColumnId,
    Guid TargetColumnId
);
}
