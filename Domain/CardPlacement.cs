namespace kanban_lia.Domain
{
    public class CardPlacement
    {
        int EntityId { get; set; }

        int ColumnId { get; set; }

        int Position { get; set; }

        int Timestamp { get; set; } = 0;


    }
}
