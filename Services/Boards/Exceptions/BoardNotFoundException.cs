using kanban_lia.Models.Domain.Boards;

namespace kanban_lia.Services.Boards.Exceptions
{
    public class BoardNotFoundException : Exception
    {
        public BoardNotFoundException(BoardId id)
            : base($"Board with id '{id}' was not found.")
        {
        }
    }
}
