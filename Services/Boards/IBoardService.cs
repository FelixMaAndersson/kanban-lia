using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Boards.DTOs;
using kanban_lia.Services.Boards.DTOs;

namespace kanban_lia.Services.Boards
{
    public interface IBoardService
    {
        Task<BoardId> CreateAsync(string title);
        Task<BoardDto?> GetByIdAsync(BoardId id);
        Task<bool> RenameAsync(RenameBoardDto dto);
        Task<bool> AddRootAsync(AddRootDto dto);
        Task<bool> RemoveRootAsync(RemoveRootDto dto);
        Task<bool> DeleteAsync(BoardId id);
    }
}
