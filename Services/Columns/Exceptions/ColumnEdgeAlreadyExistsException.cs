using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Columns.Exceptions
{
    public class ColumnEdgeAlreadyExistsException(ColumnId from, ColumnId to)
        : Exception($"ColumnEdge with ID's 'From: {from}' 'To: {to}' already exists.");
}
