namespace kanban_lia.Domain
{
    public class CardPlacement
    {
        //Kombinerad primary key på EntityId och ColumnId
        public int EntityId { get; set; }

        public int ColumnId { get; set; }

        public int Position { get; set; }

        // Timestamp för när kortet placerades i kolumnen
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    }
}
