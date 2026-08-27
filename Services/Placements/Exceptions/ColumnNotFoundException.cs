using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Placements.Exceptions
{
    public class ColumnNotFoundException(ColumnId id) : Exception($"Column with id '{id}' was not found.");
}
