using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements.DTOs;
using kanban_lia.Services.IntegrationEvents.Models;
using kanban_lia.Services.Placements.DTOs;

namespace kanban_lia.Services.Placements
{
    /// <summary>
    /// Coordinates creation and retrieval of placements, which track where entities sit within board columns.
    /// </summary>
    /// <remarks>Creation applies a list of placement operations, possibly spanning multiple boards connected by
    /// column edges, and reports per-entity successes and failures instead of failing the whole batch.</remarks>
    public interface IPlacementService
    {
        /// <summary>
        /// Creates or moves placements for the given operations, recording correlation, causation, and actor
        /// information on any resulting integration events.
        /// </summary>
        /// <param name="dtos">The placement operations to apply; each targets a column on one affected board.</param>
        /// <param name="correlationId">Identifier used to correlate all events produced by this request.</param>
        /// <param name="causationEventId">Identifier of the event that caused this request, if any.</param>
        /// <param name="actor">The actor on whose behalf the placements are created.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <param name="requestingBoardId">Board the request originated from, if known; used to scope validation.
        /// </param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the entities that
        /// succeeded and a failure entry for each entity that did not.</returns>
        Task<PlacementCreateResult> CreateAsync(
            IEnumerable<PlacementOperationDto> dtos,
            Guid correlationId,
            Guid? causationEventId,
            Actor actor,
            CancellationToken cancellationToken,
            BoardId? requestingBoardId = null);
        /// <summary>
        /// Gets the current placements of the given entities on the given board.
        /// </summary>
        /// <param name="dto">Identifiers of the entities and the board to look up.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the current
        /// placements for the requested entities.</returns>
        Task<IEnumerable<PlacementDto>> GetCurrentAsync(GetPlacementDto dto);
        /// <summary>
        /// Gets the current placements of every entity on the given board.
        /// </summary>
        /// <param name="boardId">Identifier of the board whose placements are requested.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the board's current
        /// placements.</returns>
        Task<IEnumerable<PlacementDto>> GetCurrentByBoardAsync(BoardId boardId);
        /// <summary>
        /// Gets the current placements of the entities currently in the given column.
        /// </summary>
        /// <param name="columnId">Identifier of the column whose placements are requested.</param>
        /// <param name="boardId">Identifier of the board the column belongs to.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the column's
        /// current placements.</returns>
        Task<IEnumerable<PlacementDto>> GetCurrentByColumnAsync(ColumnId columnId, BoardId boardId);
    }
}