using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Exceptions;

namespace kanban_lia.Models.Domain.Placements
{
    public readonly record struct EntityId(Guid Id);
    public class Placement
    {
        public EntityId EntityId { get; }

        public ColumnId ColumnId { get; }

        public DateTime Timestamp { get; }

        public string SortKey { get; }

        private Placement(EntityId entityId, ColumnId columnId, DateTime timestamp, string sortKey)
        {
            EntityId = entityId;
            ColumnId = columnId;
            Timestamp = timestamp;
            SortKey = sortKey;

        }

        public static Placement Create(EntityId entityId, ColumnId columnId, string sortKey)
        {

            if (string.IsNullOrWhiteSpace(sortKey))
            {
                throw new InvalidDomainException($"Invalid placement sort key: '{sortKey}'.");
            }

            return new Placement(entityId, columnId, DateTime.UtcNow, sortKey);
        }
    }
}