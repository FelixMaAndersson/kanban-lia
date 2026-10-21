namespace kanban_lia.Models.Events
{
    /// <summary>
    /// Describes a column-to-column movement of placed entities for placement change notifications.
    /// </summary>
    /// <param name="SourceColumnId">Identifier of the column the entities moved from, or <see langword="null"/> when
    /// the placement was created without a previous column.</param>
    /// <param name="TargetColumnId">Identifier of the column the entities moved to.</param>
    public record PlacementChange(
    Guid? SourceColumnId,
    Guid TargetColumnId
);
}
