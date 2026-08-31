using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Placements.Exceptions
{
    public class ColumnNotFoundException(ColumnId ColumnId) : Exception($"Column with id '{ColumnId.Id}' was not found.");
}
