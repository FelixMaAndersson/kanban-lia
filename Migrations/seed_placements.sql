DECLARE @BoardId UNIQUEIDENTIFIER =
    '11111111-1111-1111-1111-111111111111';

DECLARE @TodoColumnId UNIQUEIDENTIFIER =
(
    SELECT Id
    FROM Columns
    WHERE BoardId = @BoardId
      AND Title = 'Todo'
);

DECLARE @DoingColumnId UNIQUEIDENTIFIER =
(
    SELECT Id
    FROM Columns
    WHERE BoardId = @BoardId
      AND Title = 'Doing'
);

DECLARE @DoneColumnId UNIQUEIDENTIFIER =
(
    SELECT Id
    FROM Columns
    WHERE BoardId = @BoardId
      AND Title = 'Done'
);

INSERT INTO Placements
(
    EntityId,
    BoardId,
    ColumnId,
    SortKey,
    Timestamp
)
VALUES
(
    'c4550d6e-2d34-f111-ae9c-3cecef9b8585',
    @BoardId,
    @TodoColumnId,
    'a0',
    SYSUTCDATETIME()
),
(
    'd2550d6e-2d34-f111-ae9c-3cecef9b8585',
    @BoardId,
    @TodoColumnId,
    'a1',
    SYSUTCDATETIME()
),
(
    '09d5166e-2d34-f111-ae9c-3cecef9b8585',
    @BoardId,
    @DoingColumnId,
    'a0',
    SYSUTCDATETIME()
),
(
    '4e62e26d-2d34-f111-ae9c-3cecef9b8585',
    @BoardId,
    @DoneColumnId,
    'a0',
    SYSUTCDATETIME()
);