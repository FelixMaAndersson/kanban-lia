namespace kanban_lia.Domain
{
    public readonly record struct ColumnId(Guid Value);

    public class Column
    {
        public ColumnId Id { get; }
        public string Title { get; private set;} = string.Empty;

        public int Position { get; }
        public int BoardId { get; }

        private Column(string title, int position, int boardId)
        {
            Id = new ColumnId(Guid.NewGuid());
            Title = title;
            Position = position;
            BoardId = boardId;
        }

        public static Column Create(string title, int position, int boardId)
        {
            return new Column(title, position, boardId);
        }

        public void Rename(string title)
        {
            Title = title;
        }
    }
}
