using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Columns.DTOs
{
    /// <summary>
    /// Data transfer object carrying a request to rename a column.
    /// </summary>
    /// <remarks>Produced by mapping a rename request and passed to the column service, which loads the column,
    /// applies the domain's title rules and persists the change; a column that cannot be found is reported as a
    /// not-found error rather than a silent no-op.</remarks>
    /// <param name="Id">Identifier of the column to rename.</param>
    /// <param name="NewTitle">Replacement title, validated by the column's domain rules.</param>
    public record RenameColumnDto(
        ColumnId Id,
        string NewTitle);
}
