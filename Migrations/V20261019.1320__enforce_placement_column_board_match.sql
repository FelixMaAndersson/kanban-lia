IF EXISTS
(
    SELECT 1
    FROM dbo.Placements AS p
    INNER JOIN dbo.Columns AS c
        ON c.Id = p.ColumnId
    WHERE p.BoardId <> c.BoardId
)
BEGIN
    ;THROW 51000,
        'Cannot add placement board/column constraint: mismatched placements already exist.',
        1;
END;

ALTER TABLE dbo.Columns
ADD CONSTRAINT UQ_Columns_Id_BoardId
    UNIQUE (Id, BoardId);

ALTER TABLE dbo.Placements
ADD CONSTRAINT FK_Placements_Column_Board
    FOREIGN KEY (ColumnId, BoardId)
    REFERENCES dbo.Columns (Id, BoardId);
