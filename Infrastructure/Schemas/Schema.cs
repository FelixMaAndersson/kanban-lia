using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Infrastructure.Schemas
{
    /// <summary>
    /// Provides constants for database table and column names used by the data access layer.
    /// </summary>
    /// <remarks>Contains nested static classes that group table and column name constants (for example:
    /// Boards, Columns, ColumnEdges, Placements, BoardRoots). Most column names are produced with nameof to remain
    /// refactor-safe; some table or key names are literal strings. Use these constants to avoid hard-coded identifiers
    /// in SQL, queries, and migrations.</remarks>
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
            public const string BoardId = nameof(Column.BoardId);
            public const string Title = nameof(Column.Title);
            public const string Position = nameof(Column.Position);
            public const string RequestWritable = nameof(Column.RequestWritable);
        }

        public static class ColumnEdges
        {
            public const string Table = "ColumnEdges";
            public const string FromColumnId = nameof(ColumnEdge.FromColumnId);
            public const string ToColumnId = nameof(ColumnEdge.ToColumnId);
        }

        public static class Placements
        {
            public const string Table = "Placements";
            public const string EntityId = nameof(Placement.EntityId);
            public const string BoardId = nameof(Placement.BoardId);
            public const string ColumnId = nameof(Placement.ColumnId);
            public const string SortKey = nameof(Placement.SortKey);
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
