namespace kanban_lia.Domain
{
    public readonly record struct BoardId(Guid value);
    public class Board
    {
        private readonly HashSet<Guid> _roots = [];
        public BoardId Id { get; }
        public string Title { get; private set; } = string.Empty;

   
        public IReadOnlyCollection<Guid> Roots => _roots;

        private Board(BoardId id, string title)
        {
            Id = id;
            Title = title;
        }


        public static Board Create(string title)
        {
            return new Board(new BoardId(Guid.NewGuid()), title);
        }

        public void AdddRoot(Guid entityId)
        {
            if (!_roots.Contains(entityId))
            {
                _roots.Add(entityId);
            }
        }

        public void RemoveRoot(Guid entityId)
        {
            _roots.Remove(entityId);
        }

        public void Rename(string title)
        {
            Title = title;
        }
    }
}
