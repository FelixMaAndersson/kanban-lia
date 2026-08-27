using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Services.Boards.Exceptions
{
    public class RootNotFoundException : Exception
    {
        public RootNotFoundException(EntityId id)
            : base($"Root with id '{id}' was not found.")
        {
        }
    }
}
