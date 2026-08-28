using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Exceptions;

namespace kanban_lia.Models.Domain.Placements
{
    public readonly record struct EntityId(Guid Id);
    public class Placement
    {
        public EntityId EntityId { get; }

        public ColumnId ColumnId { get; }
        public string Position { get; }

        public DateTime Timestamp { get; }

        private Placement(Guid entityId, Guid columnId, string position, DateTime timestamp)
        {
            EntityId = new EntityId(entityId);
            ColumnId = new ColumnId(columnId);
            Position = position;
            Timestamp = timestamp;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="columnId"></param>
        /// <param name="position">Provide a lexographical position for the entity in the column</param>
        /// <returns></returns>
        /// <exception cref="InvalidDomainException"></exception>
        public static Placement Create(EntityId entityId, ColumnId columnId, string position)
        {
            if (string.IsNullOrWhiteSpace(position))
            {
                throw new InvalidDomainException($"Invalid placement position: '{position}'.");
            }

            return new Placement(entityId.Id, columnId.Id, position, DateTime.UtcNow);
        }
    }
}
