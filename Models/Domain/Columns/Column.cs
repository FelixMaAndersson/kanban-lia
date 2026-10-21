using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Exceptions;

namespace kanban_lia.Models.Domain.Columns
{
    /// <summary>
    /// Strongly typed identifier for a <see cref="Column"/>.
    /// </summary>
    /// <param name="Id">The underlying Guid of the column.</param>
    public readonly record struct ColumnId(Guid Id);

    /// <summary>
    /// Domain entity representing a column on a board, in which entities are placed.
    /// </summary>
    /// <remarks>A column belongs to exactly one board and is positioned within it by a non-negative position value.
    /// New columns are created through <see cref="Create"/>; <see cref="Restore"/> rehydrates a column from stored
    /// data without re-running validation.</remarks>
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

        /// <summary>
        /// Creates a new column, validating the title and position and generating an identifier when none is given.
        /// </summary>
        /// <param name="id">Optional identifier for the column; a new Guid is generated when
        /// <see langword="null"/>.</param>
        /// <param name="boardId">Identifier of the board the column belongs to.</param>
        /// <param name="title">The column's title; must be non-empty and at most 255 characters.</param>
        /// <param name="position">The column's position within its board; must not be negative.</param>
        /// <param name="requestWritable">Whether placement requests may write to the column; defaults to
        /// <see langword="true"/>.</param>
        /// <returns>The newly created column.</returns>
        /// <exception cref="InvalidDomainException">The title is empty, whitespace, or longer than 255 characters, or
        /// the position is negative.</exception>
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

        /// <summary>
        /// Rehydrates an existing column from persisted values without validation.
        /// </summary>
        /// <param name="id">The column's identifier.</param>
        /// <param name="boardId">Identifier of the board the column belongs to.</param>
        /// <param name="title">The stored title of the column.</param>
        /// <param name="position">The stored position of the column within its board.</param>
        /// <param name="requestWritable">The stored write-access flag of the column.</param>
        /// <returns>The restored column.</returns>
        public static Column Restore(
            ColumnId id,
            BoardId boardId,
            string title,
            int position,
            bool requestWritable)
        {
            return new Column(id, boardId, title, position, requestWritable);
        }

        /// <summary>
        /// Sets whether placement requests are allowed to write to this column.
        /// </summary>
        /// <param name="writable">The new write-access flag.</param>
        public void SetRequestWritable(bool writable)
        {
            RequestWritable = writable;
        }

        /// <summary>
        /// Changes the column's title after validating it.
        /// </summary>
        /// <param name="title">The column's new title; must be non-empty and at most 255 characters.</param>
        /// <exception cref="InvalidDomainException">The title is empty, whitespace, or longer than 255 characters.
        /// </exception>
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