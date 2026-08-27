using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Placements.Exceptions
{
    public class ColumnNotFoundException : Exception
    {
        public ColumnNotFoundException(ColumnId id)
            : base($"Column with id '{id}' was not found.")
        {
        }
    }
}
