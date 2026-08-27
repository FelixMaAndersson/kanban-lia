using kanban_lia.Models.Domain;
using kanban_lia.Services.Boards.DTOs;

namespace kanban_lia.Services.Boards
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
