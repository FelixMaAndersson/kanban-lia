namespace kanban_lia.Endpoints.Columns.Requests
{
    /// <summary>
    /// Request payload for renaming an existing column.
    /// </summary>
    /// <param name="Id">The identifier of the column to rename.</param>
    /// <param name="NewTitle">The title to assign to the column.</param>
    public record RenameColumnRequest(
        Guid Id,
        string NewTitle);
}
