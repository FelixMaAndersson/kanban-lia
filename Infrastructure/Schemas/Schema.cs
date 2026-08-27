using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Infrastructure.Schemas
{
    public static class Schema
    {
        public static class Boards
        {
            public const string Table = "Boards";
            public const string Id = nameof(Board.Id);
            public const string Title = nameof(Board.Title);
        }

        public static class Columns
        {
            public const string Table = "Columns";
            public const string Id = nameof(Column.Id);
            public const string Title = nameof(Column.Title);
            public const string Position = nameof(Column.Position);
            public const string BoardId = nameof(Column.BoardId);
        }

        public static class Placements
        {
            public const string Table = "Placements";
            public const string EntityId = nameof(Placement.EntityId);
            public const string ColumnId = nameof(Placement.ColumnId);
            public const string Position = nameof(Placement.Position);
            public const string Timestamp = nameof(Placement.Timestamp);
        }

        public static class BoardRoots
        {
            public const string Table = "BoardRoots";
            public const string BoardId = "BoardId";
            public const string EntityId = "EntityId";
        }
    }
}
