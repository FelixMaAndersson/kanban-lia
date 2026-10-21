namespace kanban_lia.Endpoints.Columns.Requests
{
    /// <summary>
    /// Record <c>CreateColumnRequest</c> is the JSON body a client posts to create a column on a board.
    /// </summary>
    /// <remarks>The endpoint maps it onto a <c>CreateColumnDto</c> and validates the title (non-empty, at most 255
    /// characters) and the position (not negative) through <c>Column.Create</c>; violations surface as a 400 problem
    /// response. Value-based record equality applies.</remarks>
    /// <param name="BoardId">Board the new column is created on; the board must already exist.</param>
    /// <param name="Title">Title of the new column.</param>
    /// <param name="Position">Left-to-right ordering position of the column within its board; must not be negative.</param>
    /// <param name="RequestWritable">Whether the column accepts placements that clients request; defaults to
    /// <see langword="true"/>.</param>
    public record CreateColumnRequest(
        Guid BoardId,
        string Title,
        int Position,
        bool RequestWritable = true);
}
