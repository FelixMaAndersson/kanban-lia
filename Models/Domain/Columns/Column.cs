using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Exceptions;

namespace kanban_lia.Models.Domain.Columns
{
    public readonly record struct ColumnId(Guid Id);
    public class Column
    {
        public ColumnId Id { get; }
        public BoardId BoardId { get; }
        public string Title { get; private set; }
        public int Position { get; }
        public bool RequestWritable { get; private set; }

        private Column(
            ColumnId id,
            BoardId boardId,
            string title,
            int position,
            bool requestWritable)
        {
            Id = id;
            BoardId = boardId;
            Title = title;
            Position = position;
            RequestWritable = requestWritable;
        }

        public static Column Create(
            ColumnId? id,
            BoardId boardId,
            string title,
            int position,
            bool requestWritable = true)
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

            return new Column(id ?? new ColumnId(Guid.NewGuid()), boardId, title, position, requestWritable);
        }

        public static Column Restore(
            ColumnId id,
            BoardId boardId,
            string title,
            int position,
            bool requestWritable)
        {
            return new Column(id, boardId, title, position, requestWritable);
        }

        public void SetRequestWritable(bool writable)
        {
            RequestWritable = writable;
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