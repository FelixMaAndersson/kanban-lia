using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements.DTOs;
using kanban_lia.Services.IntegrationEvents.Models;
using kanban_lia.Services.Placements.DTOs;

namespace kanban_lia.Services.Placements;

public interface IPlacementService
{
    Task<PlacementCreateResult> CreateAsync(
        IEnumerable<PlacementOperationDto> dtos,
        Guid correlationId,
        Guid? causationEventId,
        Actor actor,
        CancellationToken cancellationToken,
        BoardId? requestingBoardId = null);
    Task<IEnumerable<PlacementDto>> GetCurrentAsync(GetPlacementDto dto);
    Task<IEnumerable<PlacementDto>> GetCurrentByBoardAsync(BoardId boardId);
    Task<IEnumerable<PlacementDto>> GetCurrentByColumnAsync(ColumnId columnId, BoardId boardId);
}