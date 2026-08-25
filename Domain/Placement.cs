namespace kanban_lia.Domain
{
    public class Placement
    {
        public int EntityId { get; }

        public ColumnId ColumnId { get; }

        public int Position { get; }

        // Timestamp för när kortet placerades i kolumnen
        public DateTime Timestamp { get; }

        private Placement(int entityId, ColumnId columnId, int position, DateTime timestamp)
        {
            EntityId = entityId;
            ColumnId = columnId;
            Position = position;
            Timestamp = timestamp;
        }

        public static Placement Create(int entityId, ColumnId columnId, int position)
        {
            return new Placement(entityId, columnId, position, DateTime.UtcNow);
        }

    }
}
