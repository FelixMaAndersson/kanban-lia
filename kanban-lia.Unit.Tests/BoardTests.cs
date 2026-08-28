using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Exceptions;

namespace kanban_lia.Tests
{
    [TestClass]
    public class BoardTests
    {
        [TestMethod]
        public void Create_WithValidTitle_CreatesBoard()
        {
            var board = Board.Create("Testboard");

            Assert.AreEqual("Testboard", board.Title);
        }

        [TestMethod]
        public void Create_WithEmptyTitle_ThrowsException()
        {
            Assert.ThrowsExactly<InvalidDomainException>(() => Board.Create(""));
        }

        [TestMethod]
        public void Create_WithTooLongTitle_ThrowsException()
        {
            var title = new string('a', 256);

            Assert.ThrowsExactly<InvalidDomainException>(() => Board.Create(title));
        }

        [TestMethod]
        public void Rename_WithValidTitle_RenamesBoard()
        {
            var board = Board.Create("Testboard");
            board.Rename("Renamedboard");

            Assert.AreEqual("Renamedboard", board.Title);
        }

        [TestMethod]
        public void Rename_WithEmptyTitle_ThrowsException()
        {
            var board = Board.Create("Testboard");

            Assert.ThrowsExactly<InvalidDomainException>(() => board.Rename(""));
        }

        [TestMethod]
        public void Rename_WithTooLongTitle_ThrowsException()
        {
            var board = Board.Create("Testboard");

            Assert.ThrowsExactly<InvalidDomainException>(() => board.Rename(new string('a', 256)));
        }
    }
}
