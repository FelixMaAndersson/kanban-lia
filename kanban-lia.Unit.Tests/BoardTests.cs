using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Exceptions;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Tests
{
    public class BoardTests
    {
        [Fact]
        public void Create_WithValidTitle_CreatesBoard()
        {
            // Arrange & Act
            var board = Board.Create("Testboard");

            // Assert
            Assert.Equal("Testboard", board.Title);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public void Create_WithEmptyTitle_ThrowsException(string title)
        {
            // Everything
            Assert.Throws<InvalidDomainException>(() => Board.Create(title));
        }

        [Fact]
        public void Create_WithTooLongTitle_ThrowsException()
        {
            // Arrange
            var title = new string('a', 256);

            // Act & Assert
            Assert.Throws<InvalidDomainException>(() => Board.Create(title));
        }

        [Fact]
        public void Create_WhenUsed_GeneratesId()
        {
            // Arrange & Act
            var board = Board.Create("Testboard");

            // Assert
            Assert.NotEqual(board.Id.Id, Guid.Empty);
        }

        [Fact]
        public void Create_WhenUsed_GeneratesUniqueIds()
        {
            // Arrange & Act
            var board1 = Board.Create("Testboard1");
            var board2 = Board.Create("Testboard2");

            // Assert
            Assert.NotEqual(board1.Id, board2.Id);
        }

        [Fact]
        public void Restore_WithValidValues_RestoresBoard()
        {
            var id = new BoardId();
            var title = "Testboard";

            var board = Board.Restore(id, title);

            Assert.Equal(id, board.Id);
            Assert.Equal(title, board.Title);
        }

        [Fact]
        public void Rename_WithValidTitle_RenamesBoard()
        {
            // Arrange
            var board = Board.Create("Testboard");

            // Act
            board.Rename("Renamedboard");

            // Assert
            Assert.Equal("Renamedboard", board.Title);
        }

        [Fact]
        public void Rename_WithInvalidTitle_PreservesOriginalTitle()
        {
            // Arrange
            var board = Board.Create("Testboard");

            // Act
            Assert.Throws<InvalidDomainException>(() => board.Rename(""));

            // Assert
            Assert.Equal("Testboard", board.Title);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public void Rename_WithEmptyTitle_ThrowsException(string title)
        {
            // Arrange
            var board = Board.Create("Testboard");

            // Act & Assert
            Assert.Throws<InvalidDomainException>(() => board.Rename(title));
        }

        [Fact]
        public void Rename_WithTooLongTitle_ThrowsException()
        {
            // Arrange
            var board = Board.Create("Testboard");

            // Act & Assert
            Assert.Throws<InvalidDomainException>(() => board.Rename(new string('a', 256)));
        }

        [Fact]
        public void AddRoot_WhenUsed_AddsRootToBoard()
        {
            // Arrange
            var board = Board.Create("Testboard");
            var rootId = new EntityId(Guid.NewGuid());

            // Act
            board.AddRoot(rootId);

            // Assert
            Assert.Single(board.Roots);
        }

        [Fact]
        public void AddRoot_WithDuplicateRoot_AddsOnlyOnce()
        {
            // Arrange
            var board = Board.Create("Testboard");
            var rootId = new EntityId(Guid.NewGuid());

            board.AddRoot(rootId);

            // Act
            board.AddRoot(rootId);

            // Assert
            Assert.Single(board.Roots);
        }

        [Fact]
        public void RemoveRoot_WhenUsed_RemovesRootFromBoard()
        {
            // Arrange
            var board = Board.Create("Testboard");
            var rootId = new EntityId(Guid.NewGuid());

            board.AddRoot(rootId);

            // Act
            board.RemoveRoot(rootId);

            // Assert
            Assert.Empty(board.Roots);
        }

        [Fact]
        public void RemoveRoot_WhenRootDoesNotExist_DoesNothing()
        {
            // Arrange
            var board = Board.Create("Testboard");
            var rootId = new EntityId(Guid.NewGuid());

            // Act
            board.RemoveRoot(rootId);

            // Assert
            Assert.Empty(board.Roots);
        }
    }
}
