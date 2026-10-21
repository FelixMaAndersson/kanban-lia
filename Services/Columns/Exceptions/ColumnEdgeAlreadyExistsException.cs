using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Columns.Exceptions
{
    /// <summary>
    /// The exception thrown when a column edge between the two given columns already exists.
    /// </summary>
    /// <param name="from">Identifier of the originating column of the duplicate edge.</param>
    /// <param name="to">Identifier of the destination column of the duplicate edge.</param>
    public class ColumnEdgeAlreadyExistsException(ColumnId from, ColumnId to)
        : Exception($"ColumnEdge with ID's 'From: {from}' 'To: {to}' already exists.");
}
