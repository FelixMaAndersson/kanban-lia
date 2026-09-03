namespace kanban_lia.Models.Domain.Placements.DTOs
{
    public record PlacementDto(
        Guid EntityId,
        Guid BoardId,
        Guid ColumnId,
        string Position,
        DateTime TimeStamp
    );
}
