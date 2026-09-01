using AutoMapper;

using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Mappings;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Services.Boards;
using kanban_lia.Services.Boards.DTOs;
using kanban_lia.Services.Boards.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;

namespace kanban_lia.Integration.Tests;

public class BoardServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly IBoardRepository _repository;
    private readonly BoardService _service;
    private static readonly IMapper _mapper = new MapperConfiguration(
    cfg =>
    {
        cfg.AddProfile(new BoardProfile());
    },
    NullLoggerFactory.Instance).CreateMapper();

    public BoardServiceTests(DatabaseFixture fixture)
    {
        _repository = new BoardRepository(fixture.DbFactory);
        _service = new BoardService(_repository, _mapper);
    }

    [Fact]
    public async Task CreateAsync_WithValidProperties_CreatesBoard()
    {
        var id = await _service.CreateAsync("Testboard");
        var board = await _repository.GetByIdAsync(id);

        Assert.NotNull(board);
        Assert.Equal("Testboard", board.Title);
    }

    [Fact]
    public async Task GetById_WithExistingBoard_ReturnsBoard()
    {
        var id = await _service.CreateAsync("Testboard");

        var board = await _service.GetByIdAsync(id);

        Assert.NotNull(board);
        Assert.Equal(id.Id, board.Id);
    }

    [Fact]
    public async Task GetById_WithNonExistingBoard_ThrowsException()
    {
        var id = new BoardId(Guid.NewGuid());

        await Assert.ThrowsAsync<BoardNotFoundException>(() => _service.GetByIdAsync(id));
    }

    [Fact]
    public async Task RenameAsync_WithExistingBoard_RenamesBoard()
    {
        var id = await _service.CreateAsync("Testboard");

        await _service.RenameAsync(new RenameBoardDto(id.Id, "Renamedboard"));

        var board = await _repository.GetByIdAsync(id);

        Assert.NotNull(board);
        Assert.Equal("Renamedboard", board.Title);
    }

    [Fact]
    public async Task RenameAsync_WithNonExistingBoard_ThrowsException()
    {
        var id = new BoardId(Guid.NewGuid());

        await Assert.ThrowsAsync<BoardNotFoundException>(() => _service.RenameAsync(new RenameBoardDto(id.Id, "Renamedboard")));
    }

    [Fact]
    public async Task AddRootAsync_WithExistingBoard_AddsRoot()
    {
        var boardId = await _service.CreateAsync("Testboard");
        var entityId = new EntityId(Guid.NewGuid());

        await _service.AddRootAsync(new AddRootDto(boardId.Id, entityId.Id));

        var rootExists = await _repository.RootExistsAsync(
        boardId,
        entityId);

        Assert.True(rootExists);
    }

    [Fact]
    public async Task AddRootAsync_WithNonExistingBoard_ThrowsException()
    {
        var boardId = new BoardId(Guid.NewGuid());
        var entityId = new EntityId(Guid.NewGuid());

        await Assert.ThrowsAsync<BoardNotFoundException>(() => _service.AddRootAsync(new AddRootDto(boardId.Id, entityId.Id)));
    }

    [Fact]
    public async Task AddRootAsync_WithDuplicateRoot_ThrowsException()
    {
        var boardId = await _service.CreateAsync("Testboard");
        var entityId = new EntityId(Guid.NewGuid());

        await _service.AddRootAsync(new AddRootDto(boardId.Id, entityId.Id));

        await Assert.ThrowsAsync<RootAlreadyExistsException>(() => _service.AddRootAsync(new AddRootDto(boardId.Id, entityId.Id)));
    }

    [Fact]
    public async Task RemoveRootAsync_WithExistingBoard_RemovesRoot()
    {
        var boardId = await _service.CreateAsync("Testboard");
        var entityId = new EntityId(Guid.NewGuid());

        await _service.AddRootAsync(new AddRootDto(boardId.Id, entityId.Id));

        await _service.RemoveRootAsync(new RemoveRootDto(boardId.Id, entityId.Id));

        var rootExists = await _repository.RootExistsAsync(
        boardId,
        entityId);

        Assert.False(rootExists);
    }

    [Fact]
    public async Task RemoveRootAsync_WithNonExistingBoard_ThrowsException()
    {
        var boardId = new BoardId(Guid.NewGuid());
        var entityId = new EntityId(Guid.NewGuid());

        await Assert.ThrowsAsync<BoardNotFoundException>(() => _service.RemoveRootAsync(new RemoveRootDto(boardId.Id, entityId.Id)));
    }

    [Fact]
    public async Task RemoveRootAsync_WithNoRoot_ThrowsException()
    {
        var boardId = await _service.CreateAsync("Testboard");
        var entityId = new EntityId(Guid.NewGuid());

        await Assert.ThrowsAsync<RootNotFoundException>(() => _service.RemoveRootAsync(new RemoveRootDto(boardId.Id, entityId.Id)));
    }

    [Fact]
    public async Task DeleteAsync_WithExistingBoard_DeletesBoard()
    {
        var id = await _service.CreateAsync("Testboard");

        await _service.DeleteAsync(id);

        var board = await _repository.GetByIdAsync(id);

        Assert.Null(board);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingBoard_ThrowsException()
    {
        var id = new BoardId(Guid.NewGuid());

        await Assert.ThrowsAsync<BoardNotFoundException>(() => _service.DeleteAsync(id));
    }
}
