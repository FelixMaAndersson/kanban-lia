using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Columns.Exceptions
{
    public class ColumnNotFoundException(ColumnId id) : Exception($"Column with ID '{id}' was not found.");
}
