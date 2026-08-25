namespace kanban_lia.Domain
{
    public class Column
    {
        // kanske ska vara en int istället för en guid. p.g.a. ett lågt maxantal
        public Guid Id { get; set;  }
        public string Title { get; set; } = string.Empty;

        public int Position { get; set; }
        public int BoardId { get; set; }
    }
}
