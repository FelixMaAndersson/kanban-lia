using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Models.Events
{
    public record ColumnPairSelection(
    List<(ColumnId Target, List<ColumnId> Sources)> ColumnPairs,
    List<Guid> AffectedBoardIds);
}
