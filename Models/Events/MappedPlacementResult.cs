using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Models.Events
{
    public record MappedPlacementResult(
        List<Column> ConnectedTargetColumns,
        List<Column> SourceColumns);
}
