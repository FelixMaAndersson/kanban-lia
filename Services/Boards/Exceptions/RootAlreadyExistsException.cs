using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Services.Boards.Exceptions
{
    /// <summary>
    /// The exception thrown when a root entity is added to a board that already has that root.
    /// </summary>
    /// <param name="id">Identifier of the root entity that is already registered on the board.</param>
    public class RootAlreadyExistsException(EntityId id) : Exception($"Root with id '{id}' already exists on this board.");
}
