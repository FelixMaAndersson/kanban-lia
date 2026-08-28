using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Exceptions;

namespace kanban_lia.Tests
{
    [TestClass]
    public class ColumnTests
    {
        [TestMethod]
        public void Create_WithValidTitle_SetsTitle()
        {
            var column = Column.Create("Test Column", 0, new BoardId(Guid.NewGuid()));

            Assert.AreEqual("Test Column", column.Title, "Title was not set correctly.");
        }

        [TestMethod]
        public void Create_WithEmptyTitle_ThrowsInvalidDomainException()
        {
            Assert.ThrowsExactly<InvalidDomainException>(() => Column.Create("", 0, new BoardId(Guid.NewGuid())), "Title cannot be empty.");
        }

        [TestMethod]
        public void Create_WithTooLongTitle_ThrowsInvalidDomainException()
        {
            var longTitle = new string('a', 256);

            Assert.ThrowsExactly<InvalidDomainException>(() => Column.Create(longTitle, 0, new BoardId(Guid.NewGuid())), "Title cannot be longer than 255 characters.");
        }

        [TestMethod]
        public void Create_WithNegativePosition_ThrowsInvalidDomainException()
        {
            Assert.ThrowsExactly<InvalidDomainException>(() => Column.Create("Test Column", -1, new BoardId(Guid.NewGuid())), "Position cannot be negative.");
        }

        [TestMethod]
        public void Rename_WithValidTitle_UpdatesTitle()
        {
            var column = Column.Create("Test Column", 0, new BoardId(Guid.NewGuid()));
            column.Rename("New Title");

            Assert.AreEqual("New Title", column.Title, "Title was not updated correctly.");
        }

        [TestMethod]
        public void Rename_WithEmptyTitle_ThrowsInvalidDomainException()
        {
            var column = Column.Create("Test Column", 0, new BoardId(Guid.NewGuid()));

            Assert.ThrowsExactly<InvalidDomainException>(() => column.Rename(""), "Renamed title cannot be empty.");
        }

        [TestMethod]
        public void Rename_WithTooLongTitle_ThrowsInvalidDomainException()
        {
            var column = Column.Create("Test Column", 0, new BoardId(Guid.NewGuid()));
            var longTitle = new string('a', 256);

            Assert.ThrowsExactly<InvalidDomainException>(() => column.Rename(longTitle), "Renamed title cannot be longer than 255 characters.");
        }
    }
}
