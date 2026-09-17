
-- =========================================
-- Board
-- =========================================

DECLARE @BoardId1 UNIQUEIDENTIFIER =
    '11111111-1111-1111-1111-111111111111';

INSERT INTO Boards
(
    Id,
    Title
)
VALUES
(
    @BoardId1,
    'Board 1'
);


DECLARE @BoardId2 UNIQUEIDENTIFIER =
    '11111111-1111-1111-1111-111111111112';

INSERT INTO Boards
(
    Id,
    Title
)
VALUES
(
    @BoardId2,
    'Board 2'
);



-- =========================================
-- Columns Board 1
-- =========================================
DECLARE @InboxColumnId1 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222220';

DECLARE @TodoColumnId1 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222221';

DECLARE @DoingColumnId1 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222222';

DECLARE @DoneColumnId1 UNIQUEIDENTIFIER =
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
    @InboxColumnId1,
    @BoardId1,
    'Inbox',
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
    @TodoColumnId1,
    @BoardId1,
    'Todo',
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
    @DoingColumnId1,
    @BoardId1,
    'Doing',
    2
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
    @DoneColumnId1,
    @BoardId1,
    'Done',
    3
);


-- =========================================
-- Columns Board 2
-- =========================================
DECLARE @InboxColumnId2 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222224';

DECLARE @TodoColumnId2 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222225';

DECLARE @DoneColumnId2 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222226';

    INSERT INTO Columns
(
    Id,
    BoardId,
    Title,
    Position
)
VALUES
(
    @InboxColumnId2,
    @BoardId2,
    'Inbox',
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
    @TodoColumnId2,
    @BoardId2,
    'Todo',
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
    @DoneColumnId2,
    @BoardId2,
    'Done',
    2
);

-- =========================================
-- Roots
-- =========================================

INSERT INTO BoardRoots
(
    BoardId,
    EntityId
)
VALUES
(
    '11111111-1111-1111-1111-111111111111',
    'c3550d6e-2d34-f111-ae9c-3cecef9b8585'
);

INSERT INTO BoardRoots
(
    BoardId,
    EntityId
)
VALUES
(
    '11111111-1111-1111-1111-111111111112',
    'c3550d6e-2d34-f111-ae9c-3cecef9b8585'
);

-- =========================================
-- Edges
-- =========================================

-- Board 1: Todo (1) -> Board 2: Todo (1)
INSERT INTO ColumnEdges
(
    FromColumnId,
    ToColumnId
)
VALUES
(
    @TodoColumnId1,
    @TodoColumnId2
);

-- Board 1: Doing (2) -> Board 2: Todo (1)
INSERT INTO ColumnEdges
(
    FromColumnId,
    ToColumnId
)
VALUES
(
    @DoingColumnId1,
    @TodoColumnId2
);

-- Board 1: Done (3) -> Board 2: Done (2)
INSERT INTO ColumnEdges
(
    FromColumnId,
    ToColumnId
)
VALUES
(
    @DoneColumnId1,
    @DoneColumnId2
);