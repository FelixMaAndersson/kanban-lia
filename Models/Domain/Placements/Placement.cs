using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements.Exceptions;

namespace kanban_lia.Models.Domain.Placements
{
    public readonly record struct EntityId(Guid Value);
    public class Placement
    {
        public EntityId EntityId { get; }

        public ColumnId ColumnId { get; }

        public string Position { get; }

        // Timestamp för när kortet placerades i kolumnen
        public DateTime Timestamp { get; }

        private Placement(EntityId entityId, ColumnId columnId, string position, DateTime timestamp)
        {
            EntityId = entityId;
            ColumnId = columnId;
            Position = position;
            Timestamp = timestamp;
        }

        public static Placement Create(EntityId entityId, ColumnId columnId, string position)
        {
            if (string.IsNullOrWhiteSpace(position))
            {
                throw new InvalidPlacementPositionException(position);
            }

            return new Placement(entityId, columnId, position, DateTime.UtcNow);
        }
    }
}
