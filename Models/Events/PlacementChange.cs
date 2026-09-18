namespace kanban_lia.Models.Events
{
    public record PlacementChange(
    Guid? SourceColumnId,
    Guid TargetColumnId
);
}
