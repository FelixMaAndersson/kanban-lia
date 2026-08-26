using kanban_lia.Services.DTOs;
using kanban_lia.Models.Domain;

namespace kanban_lia.Services
{
    public interface IBoardService
    {
        Task<Board> CreateAsync(string title);
        Task<Board?> GetByIdAsync(BoardId id);
        Task RenameAsync(RenameBoardDto dto);
        Task AddRootAsync(AddRootDto dto);
        Task RemoveRootAsync(RemoveRootDto dto);
        Task DeleteAsync(BoardId id);
    }
}
