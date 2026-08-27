using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Exceptions;

namespace kanban_lia.Models.Domain.Columns
{
    public readonly record struct ColumnId(Guid Value);
    public class Column
    {
        public ColumnId Id { get; }
        public string Title { get; private set; }
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
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new InvalidDomainException("Column title cannot be empty");
            }

            if (title.Length > 255)
            {
                throw new InvalidDomainException("Column title is too long");
            }

            if (position < 0)
            {
                throw new InvalidDomainException("Column position cannot be negative");
            }

            return new Column(title, position, boardId);
        }

        public void Rename(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new InvalidDomainException("Column title cannot be empty");
            }

            if (title.Length > 255)
            {
                throw new InvalidDomainException("Column title is too long");
            }

            Title = title;
        }
    }
}
