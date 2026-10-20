using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Columns.Exceptions
{
    public class ColumnNotWritableException(ColumnId id)
        : Exception($"Column with ID '{id}' does not accept new placements.");
}