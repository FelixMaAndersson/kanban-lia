using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Exceptions;

namespace kanban_lia.Tests
{
    public class ColumnEdgeTests
    {
        [Fact]
        public void Create_WithValidColumnIds_CreatesColumnEdge()
        {
            // Arrange
            var fromColumnId = new ColumnId(Guid.NewGuid());
            var toColumnId = new ColumnId(Guid.NewGuid());

            // Act
            var columnEdge = ColumnEdge.Create(fromColumnId, toColumnId);

            // Assert
            Assert.Equal(fromColumnId, columnEdge.FromColumnId);
            Assert.Equal(toColumnId, columnEdge.ToColumnId);
        }
        [Fact]
        public void Create_WithSameIds_ThrowsInvalidDomainException()
        {
            // Arrange
            var columnId = new ColumnId(Guid.NewGuid());

            // Act & Assert
            Assert.Throws<InvalidDomainException>(() => ColumnEdge.Create(columnId, columnId));
        }
    }
}
