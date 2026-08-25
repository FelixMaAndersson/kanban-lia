namespace kanban_lia.Domain
{
    public class CardPlacement
    {
        public Guid Id { get; set; }
        public int EntityId { get; set; }

        public int ColumnId { get; set; }

        public int Position { get; set; }

        // Timestamp för när kortet placerades i kolumnen
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    }
}
