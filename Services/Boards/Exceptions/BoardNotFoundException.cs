using kanban_lia.Models.Domain.Boards;

namespace kanban_lia.Services.Boards.Exceptions
{
    public class BoardNotFoundException(BoardId id) : Exception($"Board with id '{id}' was not found.")
    {
    }
}
