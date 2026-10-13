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
