namespace kanban_lia.Models.DTOs.Placement
{
    public record PlacementDto(
        Guid EntityId,
        Guid ColumnId,
        string Position,
        DateTime TimeStamp
    );
}
