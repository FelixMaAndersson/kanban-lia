using kanban_lia.Models.Domain.Boards;

namespace kanban_lia.Models.Domain.Columns
{
    public readonly record struct ColumnId(Guid Value);
    public class Column
    {
        public ColumnId Id { get; }
        public string Title { get; private set; } = string.Empty;

        public int Position { get; }
        public BoardId BoardId { get; }

        private Column(string title, int position, BoardId boardId)
        {
            Id = new ColumnId(Guid.NewGuid());
            Title = title;
            Position = position;
            BoardId = boardId;
        }

        public static Column Create(string title, int position, BoardId boardId)
        {
            return new Column(title, position, boardId);
        }

        public void Rename(string title)
        {
            Title = title;
        }
    }
}
