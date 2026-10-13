using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Exceptions;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Tests
{
    public class PlacementTests
    {

        [Fact]
        public void Create_WithValidParameters_ShouldCreatePlacement()
        {
            // Arrange
            var entityId = new EntityId(Guid.NewGuid());
            var boardId = new BoardId(Guid.NewGuid());
            var columnId = new ColumnId(Guid.NewGuid());

            var sortKey = "a";

            // Act
            var placement = Placement.Create(entityId, boardId, columnId, sortKey);

            // Assert
            Assert.Equal(entityId, placement.EntityId);
            Assert.Equal(boardId, placement.BoardId);
            Assert.Equal(columnId, placement.ColumnId);
            Assert.Equal(sortKey, placement.SortKey);
        }

        [Fact]
        public void Create_WithInvalidSortKey_ShouldThrowInvalidDomainException()
        {
            // Arrange
            var entityId = new EntityId(Guid.NewGuid());
            var boardId = new BoardId(Guid.NewGuid());
            var columnId = new ColumnId(Guid.NewGuid());
            var invalidSortKey = "   "; // Invalid sort key (whitespace)

            // Act & Assert
            Assert.Throws<InvalidDomainException>(() => Placement.Create(entityId, boardId, columnId, invalidSortKey));
        }

        [Fact]
        public void Create_WithNullSortKey_ShouldThrowInvalidDomainException()
        {
            // Arrange
            var entityId = new EntityId(Guid.NewGuid());
            var boardId = new BoardId(Guid.NewGuid());
            var columnId = new ColumnId(Guid.NewGuid());
            string? nullSortKey = null; // Null sort key

            // Act & Assert
            Assert.Throws<InvalidDomainException>(() => Placement.Create(entityId, boardId, columnId, nullSortKey!));
        }

        [Fact]
        public void Create_WithEmptySortKey_ShouldThrowInvalidDomainException()
        {
            // Arrange
            var entityId = new EntityId(Guid.NewGuid());
            var boardId = new BoardId(Guid.NewGuid());
            var columnId = new ColumnId(Guid.NewGuid());
            var emptySortKey = ""; // Empty sort key

            // Act & Assert
            Assert.Throws<InvalidDomainException>(() => Placement.Create(entityId, boardId, columnId, emptySortKey));
        }

        [Fact]
        public void Create_ShouldSetTimestampToCurrentUtcTime()
        {
            // Arrange
            var entityId = new EntityId(Guid.NewGuid());
            var boardId = new BoardId(Guid.NewGuid());
            var columnId = new ColumnId(Guid.NewGuid());
            var sortKey = "a";

            // Act
            var placement = Placement.Create(entityId, boardId, columnId, sortKey);

            // Assert
            var timeDifference = DateTime.UtcNow - placement.Timestamp;
            Assert.True(timeDifference.TotalSeconds < 1, "Timestamp should be set to current UTC time.");
        }
    }
}