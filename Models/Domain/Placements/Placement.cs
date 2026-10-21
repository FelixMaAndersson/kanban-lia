using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Exceptions;

namespace kanban_lia.Models.Domain.Placements
{
    /// <summary>
    /// Strongly typed identifier of an entity that can be placed on a board, such as a card.
    /// </summary>
    /// <param name="Id">The GUID that uniquely identifies the entity.</param>
    public readonly record struct EntityId(Guid Id);
    /// <summary>
    /// Immutable record of one entity being in a particular column of a board at a particular time.
    /// </summary>
    /// <remarks>Placements are append-only history: moving an entity writes a new row instead of updating or
    /// deleting the old one, and the repository picks the row with the newest <see cref="Timestamp"/> as the
    /// entity's current placement. <see cref="SortKey"/> is an opaque string whose binary ordering defines the
    /// entity's position within its column; it is assigned by the service layer, not chosen by callers. Instances
    /// are only created through <see cref="Create"/>.</remarks>
    public class Placement
    {
        public EntityId EntityId { get; }
        public BoardId BoardId { get; }
        public ColumnId ColumnId { get; }
        public DateTime Timestamp { get; }
        public string SortKey { get; }

        private Placement(
            Guid entityId,
            Guid boardId,
            Guid columnId,
            string sortKey,
            DateTime timestamp)
        {
            EntityId = new EntityId(entityId);
            BoardId = new BoardId(boardId);
            ColumnId = new ColumnId(columnId);
            SortKey = sortKey;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Creates a placement for an entity in the given column, stamped with the current UTC time.
        /// </summary>
        /// <remarks>Supersedes any earlier placement of the same entity only in the sense that it produces a newer
        /// timestamp; existing rows are not modified. The timestamp is read from <see cref="DateTime.UtcNow"/> at
        /// creation, so concurrent moves of one entity can share a timestamp and tie.</remarks>
        /// <param name="entityId">Entity being placed.</param>
        /// <param name="boardId">Board the placement belongs to.</param>
        /// <param name="columnId">Column the entity is placed in.</param>
        /// <param name="sortKey">Ordering value within the column; must contain at least one non-whitespace
        /// character.</param>
        /// <returns>The new placement.</returns>
        /// <exception cref="InvalidDomainException">
        /// <paramref name="sortKey"/> is null, empty or whitespace.
        /// </exception>
        public static Placement Create(
            EntityId entityId,
            BoardId boardId,
            ColumnId columnId,
            string sortKey)
        {
            if (string.IsNullOrWhiteSpace(sortKey))
            {
                throw new InvalidDomainException(
                    $"Invalid placement sort key: '{sortKey}'.");
            }

            return new Placement(
                entityId.Id,
                boardId.Id,
                columnId.Id,
                sortKey,
                DateTime.UtcNow);
        }
    }
}