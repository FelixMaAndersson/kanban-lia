namespace kanban_lia.Services.Placements.DTOs
{
    public record CreatePlacementDto(Guid EntityId, Guid ColumnId, string Position);
}