using kanban_lia.Services.DTOs;
using kanban_lia.Models.Domain;

namespace kanban_lia.Services
{
    public interface IBoardService
    {
        Task CreateAsync(string title);
        Task<Board?> GetByIdAsync(BoardId id);
        Task RenameAsync(RenameBoardDto dto);
        Task DeleteAsync(BoardId id);
    }
}
