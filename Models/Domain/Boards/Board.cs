using kanban_lia.Models.Domain.Exceptions;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Models.Domain.Boards
{
    public readonly record struct BoardId(Guid Id);
    public class Board
    {
        private readonly HashSet<EntityId> _roots = [];
        public BoardId Id { get; }
        public string Title { get; private set; }
        public IReadOnlyCollection<EntityId> Roots => _roots;

        private Board(Guid id, string title)
        {
            Id = new BoardId(id);
            Title = title;
        }

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

            return new Board(Guid.NewGuid(), title);
        }
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

        public void AddRoot(EntityId entityId)
        {
            _roots.Add(entityId);
        }

        public void RemoveRoot(EntityId entityId)
        {
            _roots.Remove(entityId);
        }
    }
}
