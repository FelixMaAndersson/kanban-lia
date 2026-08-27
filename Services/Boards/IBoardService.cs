using kanban_lia.Models.Domain.Boards;
using kanban_lia.Services.Boards.DTOs;

namespace kanban_lia.Services.Boards
{
    public interface IBoardService
    {
        Task CreateAsync(string title);
        Task<Board?> GetByIdAsync(BoardId id);
        Task<bool> RenameAsync(RenameBoardDto dto);
        Task<bool> AddRootAsync(AddRootDto dto);
        Task<bool> RemoveRootAsync(RemoveRootDto dto);
        Task<bool> DeleteAsync(BoardId id);
    }
}
