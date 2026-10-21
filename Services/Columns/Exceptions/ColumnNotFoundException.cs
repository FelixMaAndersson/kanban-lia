using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Columns.Exceptions
{
    /// <summary>
    /// Exception thrown when a column with the given identifier cannot be found.
    /// </summary>
    /// <param name="id">Identifier of the column that was sought.</param>
    public class ColumnNotFoundException(ColumnId id) : Exception($"Column with ID '{id}' was not found.");
}
