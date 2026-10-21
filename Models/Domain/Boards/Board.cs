using kanban_lia.Models.Domain.Exceptions;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Models.Domain.Boards
{
    /// <summary>
    /// Strongly typed identifier of a board, wrapping the underlying GUID.
    /// </summary>
    /// <param name="Id">The GUID that uniquely identifies the board.</param>
    public readonly record struct BoardId(Guid Id);

    /// <summary>
    /// Aggregate that represents a kanban board: its identity, its title and the root entities that belong to it.
    /// </summary>
    /// <remarks>Instances are created through <see cref="Create"/>, which generates a new id and validates the
    /// title, or through <see cref="Restore"/>, which rehydrates a persisted row without validation so that
    /// historical data can always be loaded. Roots are kept in a hash set inside the instance: <see cref="AddRoot"/>
    /// and <see cref="RemoveRoot"/> are idempotent and do not talk to storage themselves - persistence is the
    /// repository's job. The type is not thread-safe, and the enumeration order of <see cref="Roots"/> is not
    /// defined.</remarks>
    public class Board
    {
        private readonly HashSet<EntityId> _roots = [];
        public BoardId Id { get; }
        public string Title { get; private set; }
        public IReadOnlyCollection<EntityId> Roots => _roots;

        private Board(BoardId id, string title)
        {
            Id = id;
            Title = title;
        }

        /// <summary>
        /// Creates a new board with a freshly generated identifier.
        /// </summary>
        /// <param name="title">Initial title; must contain at least one non-whitespace character and at most 255
        /// characters.</param>
        /// <returns>The newly created board, with no roots.</returns>
        /// <exception cref="InvalidDomainException">
        /// The title is null, empty or whitespace, or longer than 255 characters.
        /// </exception>
        public static Board Create(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new InvalidDomainException("Board title cannot be empty.");
            }

            if (title.Length > 255)
            {
                throw new InvalidDomainException("Board title is too long.");
            }

            return new Board(new BoardId(Guid.NewGuid()), title);
        }

        /// <summary>
        /// Rebuilds a board from persisted data without applying title validation.
        /// </summary>
        /// <remarks>Used by the repository layer when loading a row that was already validated on the way in, so
        /// that pre-existing data can still be read if it would no longer pass today's rules.</remarks>
        /// <param name="id">Identifier the board was stored under.</param>
        /// <param name="title">Persisted title.</param>
        /// <returns>The restored board, with no roots; callers add persisted roots via <see cref="AddRoot"/>.</returns>
        public static Board Restore(BoardId id, string title)
        {
            return new Board(id, title);
        }

        /// <summary>
        /// Changes the board's title after validating it with the same rules as <see cref="Create"/>.
        /// </summary>
        /// <param name="title">Replacement title; must contain at least one non-whitespace character and at most
        /// 255 characters.</param>
        /// <exception cref="InvalidDomainException">
        /// The title is null, empty or whitespace, or longer than 255 characters; the board is left unchanged.
        /// </exception>
        public void Rename(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new InvalidDomainException("Board title cannot be empty.");
            }

            if (title.Length > 255)
            {
                throw new InvalidDomainException("Board title is too long.");
            }

            Title = title;
        }

        /// <summary>
        /// Registers an entity as a root of the board.
        /// </summary>
        /// <remarks>Only affects the in-memory instance; adding the same entity twice has no further effect.
        /// Persisting the root is the responsibility of the repository.</remarks>
        /// <param name="entityId">Identifier of the entity to add.</param>
        public void AddRoot(EntityId entityId)
        {
            _roots.Add(entityId);
        }

        /// <summary>
        /// Removes an entity from the board's roots.
        /// </summary>
        /// <remarks>Only affects the in-memory instance; removing an entity that is not a root does nothing.
        /// Persisting the removal is the responsibility of the repository.</remarks>
        /// <param name="entityId">Identifier of the entity to remove.</param>
        public void RemoveRoot(EntityId entityId)
        {
            _roots.Remove(entityId);
        }
    }
}
