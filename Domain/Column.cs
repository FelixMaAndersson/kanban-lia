namespace kanban_lia.Domain
{
    public class Column
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        public int Position { get; set; }
        public int BoardId { get; set; }
    }
}
