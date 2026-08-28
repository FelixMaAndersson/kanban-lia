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

        private Placement(Guid entityId, Guid columnId, string position, DateTime timestamp)
        {
            EntityId = new EntityId(entityId);
            ColumnId = new ColumnId(columnId);
            Position = position;
            Timestamp = timestamp;
            SortKey = sortKey;

        }

        public static Placement Create(EntityId entityId, ColumnId columnId, string sortKey)
        {

            if (string.IsNullOrWhiteSpace(sortKey))
            {
                throw new InvalidDomainException($"Invalid placement sort key: '{sortKey}'.");
            }

            return new Placement(entityId.Id, columnId.Id, position, DateTime.UtcNow);
        }
    }
}