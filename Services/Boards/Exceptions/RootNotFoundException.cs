using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Services.Boards.Exceptions
{
    /// <summary>
    /// Exception thrown when a board root with the given entity identifier cannot be found.
    /// </summary>
    /// <param name="id">Identifier of the root that was sought.</param>
    public class RootNotFoundException(EntityId id) : Exception($"Root with id '{id}' was not found.");
}
