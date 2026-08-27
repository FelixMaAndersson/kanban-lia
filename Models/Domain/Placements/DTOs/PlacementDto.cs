namespace kanban_lia.Models.Domain.Placements.DTOs
{
    public record PlacementDto(
        Guid EntityId,
        Guid ColumnId,
        string Position,
        DateTime TimeStamp
    );
}
