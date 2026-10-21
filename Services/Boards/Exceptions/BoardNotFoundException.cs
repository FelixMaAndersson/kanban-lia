using kanban_lia.Models.Domain.Boards;

namespace kanban_lia.Services.Boards.Exceptions
{
    /// <summary>
    /// Thrown when an operation targets a board that does not exist.
    /// </summary>
    /// <remarks>Raised by the board, column and placement services after a board lookup comes up empty, and mapped
    /// to an HTTP 404 problem response by the application's exception handler.</remarks>
    /// <param name="id">Identifier of the board that could not be found; it is included in the exception message.</param>
    public class BoardNotFoundException(BoardId id) : Exception($"Board with id '{id}' was not found.");
}
