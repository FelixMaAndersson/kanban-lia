using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Models.Domain.Boards
{
    public readonly record struct BoardId(Guid Value);
    public class Board
    {
        private readonly HashSet<EntityId> _roots = [];
        public BoardId Id { get; }
        public string Title { get; private set; } = string.Empty;


        public IReadOnlyCollection<EntityId> Roots => _roots;

        private Board(BoardId id, string title)
        {
            Id = id;
            Title = title;
        }


        public static Board Create(string title)
        {
            return new Board(new BoardId(Guid.NewGuid()), title);
        }

        public void AddRoot(EntityId entityId)
        {
            _roots.Add(entityId);
        }

        public void RemoveRoot(EntityId entityId)
        {
            _roots.Remove(entityId);
        }

        public void Rename(string title)
        {
            Title = title;
        }
    }
}
