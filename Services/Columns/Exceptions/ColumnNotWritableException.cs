using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Columns.Exceptions
{
    /// <summary>
    /// Exception thrown when an operation tries to place an entity into a column that does not accept new placements.
    /// </summary>
    /// <remarks>Raised by the placement service when a column flagged as not writable is targeted by a placement
    /// requested from that column's own board (or with no requesting board given); placements arriving from another
    /// board are still accepted. The exception is translated to HTTP 403 by the global exception handler, and the
    /// message names the offending column.</remarks>
    /// <param name="id">Identifier of the column that rejected the write.</param>
    public class ColumnNotWritableException(ColumnId id)
        : Exception($"Column with ID '{id}' does not accept new placements.");
}