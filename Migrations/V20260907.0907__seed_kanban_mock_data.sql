
-- =========================================
-- Board
-- =========================================

DECLARE @BoardId UNIQUEIDENTIFIER =
    '11111111-1111-1111-1111-111111111111';

INSERT INTO Boards
(
    Id,
    Title
)
VALUES
(
    @BoardId,
    'Mock Board'
);


-- =========================================
-- Columns
-- =========================================

DECLARE @TodoColumnId UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222221';

DECLARE @DoingColumnId UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222222';

DECLARE @DoneColumnId UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222223';


INSERT INTO Columns
(
    Id,
    BoardId,
    Title,
    Position
)
VALUES
(
    @TodoColumnId,
    @BoardId,
    'Todo',
    0
);

INSERT INTO Columns
(
    Id,
    BoardId,
    Title,
    Position
)
VALUES
(
    @DoingColumnId,
    @BoardId,
    'Doing',
    1
);

INSERT INTO Columns
(
    Id,
    BoardId,
    Title,
    Position
)
VALUES
(
    @DoneColumnId,
    @BoardId,
    'Done',
    2
);


-- =========================================
-- Placements
-- =========================================

-- Entity 1: Todo -> Doing

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
    '3f2504e0-4f89-41d3-9a0c-0305e82c3301',
    @BoardId,
    @TodoColumnId,
    'a0',
    '2026-09-01T10:00:00'
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
    '3f2504e0-4f89-41d3-9a0c-0305e82c3301',
    @BoardId,
    @DoingColumnId,
    'a0',
    '2026-09-01T11:00:00'
);


-- Entity 2: Todo

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
    '7c9e6679-7425-40de-944b-e07fc1f90ae2',
    @BoardId,
    @TodoColumnId,
    'a0',
    '2026-09-01T10:15:00'
);


-- Entity 3: Todo -> Doing -> Done

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
    '550e8400-e29b-41d4-a716-446655440003',
    @BoardId,
    @TodoColumnId,
    'a1',
    '2026-09-01T10:30:00'
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
    '550e8400-e29b-41d4-a716-446655440003',
    @BoardId,
    @DoingColumnId,
    'a1',
    '2026-09-01T11:30:00'
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
    '550e8400-e29b-41d4-a716-446655440003',
    @BoardId,
    @DoneColumnId,
    'a0',
    '2026-09-01T12:30:00'
);


-- Entity 4: Doing -> Todo

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
    '6ba7b810-9dad-41d1-80b4-00c04fd43004',
    @BoardId,
    @DoingColumnId,
    'a2',
    '2026-09-01T10:45:00'
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
    '6ba7b810-9dad-41d1-80b4-00c04fd43004',
    @BoardId,
    @TodoColumnId,
    'a1',
    '2026-09-01T13:00:00'
);