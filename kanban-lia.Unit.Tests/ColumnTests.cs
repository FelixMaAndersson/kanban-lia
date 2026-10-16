using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Exceptions;

namespace kanban_lia.Tests
{
    public class ColumnTests
    {
        [Fact]
        public void Create_WithValidTitle_SetsTitle()
        {
            var column = Column.Create(new ColumnId(Guid.NewGuid()), new BoardId(Guid.NewGuid()), "Test Column", 0);

            Assert.Equal("Test Column", column.Title);
        }

        [Fact]
        public void Create_WithEmptyTitle_ThrowsInvalidDomainException()
        {
            Assert.Throws<InvalidDomainException>(() => Column.Create(new ColumnId(Guid.NewGuid()), new BoardId(Guid.NewGuid()), "", 0));
        }

        [Fact]
        public void Create_WithTooLongTitle_ThrowsInvalidDomainException()
        {
            var longTitle = new string('a', 256);

            Assert.Throws<InvalidDomainException>(() => Column.Create(new ColumnId(Guid.NewGuid()), new BoardId(Guid.NewGuid()), longTitle, 0));
        }

        [Fact]
        public void Create_WithNegativePosition_ThrowsInvalidDomainException()
        {
            Assert.Throws<InvalidDomainException>(() => Column.Create(new ColumnId(Guid.NewGuid()), new BoardId(Guid.NewGuid()), "Test Column", -1));
        }

        [Fact]
        public void Create_WhenUsed_GeneratesId()
        {
            // Arrange & Act
            var column = Column.Create(null, new BoardId(Guid.NewGuid()), "Test Column", 0);

            // Assert
            Assert.NotEqual(column.Id.Id, Guid.Empty);
        }

        [Fact]
        public void Create_WhenUsed_GeneratesUniqueIds()
        {
            // Arrange & Act
            var column1 = Column.Create(null, new BoardId(Guid.NewGuid()), "Test Column 1", 0);
            var column2 = Column.Create(null, new BoardId(Guid.NewGuid()), "Test Column 2", 0);

            // Assert
            Assert.NotEqual(column1.Id, column2.Id);
        }

        [Fact]
        public void Create_WhenUsed_DefaultsRequestWritableToTrue()
        {
            // Arrange & Act
            var column = Column.Create(null, new BoardId(Guid.NewGuid()), "Test Column", 0);

            // Assert
            Assert.True(column.RequestWritable);
        }

        [Fact]
        public void Create_WhenUsed_AllowsPositionZero()
        {
            // Arrange & Act
            var column = Column.Create(null, new BoardId(Guid.NewGuid()), "Test Column", 0);

            // Assert
            Assert.Equal(0, column.Position);
        }

        [Fact]
        public void Restore_WithValidValues_RestoresColumn()
        {
            var columnId = new ColumnId();
            var boardId = new BoardId();
            var title = "Testboard";
            var postion = 0;
            var writable = true;

            var column = Column.Restore(columnId, boardId, title, postion, writable);

            Assert.Equal(columnId, column.Id);
            Assert.Equal(boardId, column.BoardId);
            Assert.Equal(title, column.Title);
            Assert.Equal(postion, column.Position);
            Assert.Equal(writable, column.RequestWritable);
        }

        [Fact]
        public void Rename_WithValidTitle_UpdatesTitle()
        {
            var column = Column.Create(new ColumnId(Guid.NewGuid()), new BoardId(Guid.NewGuid()), "Test Column", 0);
            column.Rename("New Title");

            Assert.Equal("New Title", column.Title);
        }

        [Fact]
        public void Rename_WithEmptyTitle_ThrowsInvalidDomainException()
        {
            var column = Column.Create(new ColumnId(Guid.NewGuid()), new BoardId(Guid.NewGuid()), "Test Column", 0);

            Assert.Throws<InvalidDomainException>(() => column.Rename(""));
        }

        [Fact]
        public void Rename_WithTooLongTitle_ThrowsInvalidDomainException()
        {
            var column = Column.Create(new ColumnId(Guid.NewGuid()), new BoardId(Guid.NewGuid()), "Test Column", 0);
            var longTitle = new string('a', 256);

            Assert.Throws<InvalidDomainException>(() => column.Rename(longTitle));
        }
    }
}
