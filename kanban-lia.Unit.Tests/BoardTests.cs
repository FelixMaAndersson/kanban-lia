using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Exceptions;

namespace kanban_lia.Tests
{
    public class BoardTests
    {
        [Fact]
        public void Create_WithValidTitle_CreatesBoard()
        {
            var board = Board.Create("Testboard");

            Assert.Equal("Testboard", board.Title);
        }

        [Fact]
        public void Create_WithEmptyTitle_ThrowsException()
        {
            Assert.Throws<InvalidDomainException>(() => Board.Create(""));
        }

        [Fact]
        public void Create_WithTooLongTitle_ThrowsException()
        {
            var title = new string('a', 256);

            Assert.Throws<InvalidDomainException>(() => Board.Create(title));
        }

        [Fact]
        public void Rename_WithValidTitle_RenamesBoard()
        {
            var board = Board.Create("Testboard");
            board.Rename("Renamedboard");

            Assert.Equal("Renamedboard", board.Title);
        }

        [Fact]
        public void Rename_WithEmptyTitle_ThrowsException()
        {
            var board = Board.Create("Testboard");

            Assert.Throws<InvalidDomainException>(() => board.Rename(""));
        }

        [Fact]
        public void Rename_WithTooLongTitle_ThrowsException()
        {
            var board = Board.Create("Testboard");

            Assert.Throws<InvalidDomainException>(() => board.Rename(new string('a', 256)));
        }
    }
}
