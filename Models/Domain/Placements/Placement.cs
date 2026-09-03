using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Exceptions;

namespace kanban_lia.Models.Domain.Placements
{
    public readonly record struct EntityId(Guid Id);
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