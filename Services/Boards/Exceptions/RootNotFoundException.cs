using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Services.Boards.Exceptions
{
    public class RootNotFoundException(EntityId id) : Exception($"Root with id '{id}' was not found.")
    {
    }
}
