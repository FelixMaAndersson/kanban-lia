namespace kanban_lia.Domain
{
    public class Board
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;

        public String TrackedRoots { get; set; } = string.Empty;
    
    }
}
