using kanban_lia.Services.DTOs;
using kanban_lia.Models.Domain.Boards;

namespace kanban_lia.Services
{
    public interface IBoardService
    {
        Task<BoardId> CreateAsync(string title);
        Task<Board?> GetByIdAsync(BoardId id);
        Task<bool> RenameAsync(RenameBoardDto dto);
        Task<bool> AddRootAsync(AddRootDto dto);
        Task<bool> RemoveRootAsync(RemoveRootDto dto);
        Task<bool> DeleteAsync(BoardId id);
    }
}
