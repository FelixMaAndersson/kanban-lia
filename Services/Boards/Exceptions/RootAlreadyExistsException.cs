using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Services.Boards.Exceptions
{
    public class RootAlreadyExistsException(EntityId id) : Exception($"Root with id '{id}' already exists on this board.");
}
