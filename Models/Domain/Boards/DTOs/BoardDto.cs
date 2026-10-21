using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Models.Domain.Boards.DTOs
{
    /// <summary>
    /// Data-transfer representation of a board together with the identifiers of the entities rooted on it.
    /// </summary>
    /// <param name="Id">Identifier of the board.</param>
    /// <param name="Title">Display title of the board.</param>
    /// <param name="Roots">Identifiers of the entities that serve as roots on the board.</param>
    public record BoardDto(
        BoardId Id,
        string Title,
        IReadOnlyCollection<EntityId> Roots
    );
}