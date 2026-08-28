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
            var column = Column.Create("Test Column", 0, new BoardId(Guid.NewGuid()));

            Assert.Equal("Test Column", column.Title);
        }

        [Fact]
        public void Create_WithEmptyTitle_ThrowsInvalidDomainException()
        {
            Assert.Throws<InvalidDomainException>(() => Column.Create("", 0, new BoardId(Guid.NewGuid())));
        }

        [Fact]
        public void Create_WithTooLongTitle_ThrowsInvalidDomainException()
        {
            var longTitle = new string('a', 256);

            Assert.Throws<InvalidDomainException>(() => Column.Create(longTitle, 0, new BoardId(Guid.NewGuid())));
        }

        [Fact]
        public void Create_WithNegativePosition_ThrowsInvalidDomainException()
        {
            Assert.Throws<InvalidDomainException>(() => Column.Create("Test Column", -1, new BoardId(Guid.NewGuid())));
        }

        [Fact]
        public void Rename_WithValidTitle_UpdatesTitle()
        {
            var column = Column.Create("Test Column", 0, new BoardId(Guid.NewGuid()));
            column.Rename("New Title");

            Assert.Equal("New Title", column.Title);
        }

        [Fact]
        public void Rename_WithEmptyTitle_ThrowsInvalidDomainException()
        {
            var column = Column.Create("Test Column", 0, new BoardId(Guid.NewGuid()));

            Assert.Throws<InvalidDomainException>(() => column.Rename(""));
        }

        [Fact]
        public void Rename_WithTooLongTitle_ThrowsInvalidDomainException()
        {
            var column = Column.Create("Test Column", 0, new BoardId(Guid.NewGuid()));
            var longTitle = new string('a', 256);

            Assert.Throws<InvalidDomainException>(() => column.Rename(longTitle));
        }
    }
}
