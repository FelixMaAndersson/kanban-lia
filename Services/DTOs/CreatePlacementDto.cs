namespace kanban_lia.Services.DTOs
{
    public record CreatePlacementDto(Guid EntityId, Guid ColumnId, string Position);
}
