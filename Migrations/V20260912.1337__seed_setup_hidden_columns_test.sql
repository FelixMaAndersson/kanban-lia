-- =========================================
-- Boards
-- =========================================

DECLARE @BoardId1 UNIQUEIDENTIFIER =
    '11111111-1111-1111-1111-111111111111';

DECLARE @BoardId2 UNIQUEIDENTIFIER =
    '11111111-1111-1111-1111-111111111112';

INSERT INTO Boards (Id, Title)
VALUES
    (@BoardId1, 'Board 1'),
    (@BoardId2, 'Board 2');


-- =========================================
-- Columns - Board 1
-- =========================================

DECLARE @RejectedColumnId1 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222220';

DECLARE @InboxColumnId1 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222221';

DECLARE @TodoColumnId1 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222222';

DECLARE @DoingColumnId1 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222223';

DECLARE @SendToTestColumnId1 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222224';

DECLARE @TestingColumnId1 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222225';

DECLARE @ReleasedColumnId1 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222226';


INSERT INTO Columns (Id, BoardId, Title, Position)
VALUES
    (@RejectedColumnId1,   @BoardId1, 'Rejected',     0),
    (@InboxColumnId1,      @BoardId1, 'Inbox',        1),
    (@TodoColumnId1,       @BoardId1, 'Todo',         2),
    (@DoingColumnId1,      @BoardId1, 'Doing',        3),
    (@SendToTestColumnId1, @BoardId1, 'Send to Test', 4),
    (@TestingColumnId1,    @BoardId1, 'Testing',      5),
    (@ReleasedColumnId1,   @BoardId1, 'Released',     6);


-- =========================================
-- Columns - Board 2
-- =========================================

DECLARE @InboxColumnId2 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222227';

DECLARE @ToTestColumnId2 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222228';

DECLARE @RejectColumnId2 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222229';

DECLARE @TestingColumnId2 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222230';

DECLARE @DeploymentColumnId2 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222231';

DECLARE @ReleasedColumnId2 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222232';


INSERT INTO Columns (Id, BoardId, Title, Position)
VALUES
    (@InboxColumnId2,      @BoardId2, 'Inbox',      0),
    (@ToTestColumnId2,     @BoardId2, 'To Test',    1),
    (@RejectColumnId2,     @BoardId2, 'Reject',     2),
    (@TestingColumnId2,    @BoardId2, 'Testing',    3),
    (@DeploymentColumnId2, @BoardId2, 'Deployment', 4),
    (@ReleasedColumnId2,   @BoardId2, 'Released',   5);


-- =========================================
-- Roots
-- =========================================

DECLARE @RootEntityId UNIQUEIDENTIFIER =
    'c3550d6e-2d34-f111-ae9c-3cecef9b8585';

INSERT INTO BoardRoots (BoardId, EntityId)
VALUES
    (@BoardId1, @RootEntityId),
    (@BoardId2, @RootEntityId);


-- =========================================
-- Column Edges
-- =========================================

-- Inbox <-> Inbox
INSERT INTO ColumnEdges (FromColumnId, ToColumnId)
VALUES
    (@InboxColumnId1, @InboxColumnId2),
    (@InboxColumnId2, @InboxColumnId1);

-- Todo <-> Inbox
INSERT INTO ColumnEdges (FromColumnId, ToColumnId)
VALUES
    (@TodoColumnId1, @InboxColumnId2),
    (@InboxColumnId2, @TodoColumnId1);

-- Doing <-> Inbox
INSERT INTO ColumnEdges (FromColumnId, ToColumnId)
VALUES
    (@DoingColumnId1, @InboxColumnId2),
    (@InboxColumnId2, @DoingColumnId1);

-- Send to Test <-> To Test
INSERT INTO ColumnEdges (FromColumnId, ToColumnId)
VALUES
    (@SendToTestColumnId1, @ToTestColumnId2),
    (@ToTestColumnId2, @SendToTestColumnId1);


-- Rejected <-> Reject
INSERT INTO ColumnEdges (FromColumnId, ToColumnId)
VALUES
    (@RejectedColumnId1, @RejectColumnId2),
    (@RejectColumnId2, @RejectedColumnId1);


-- Testing <-> Testing
INSERT INTO ColumnEdges (FromColumnId, ToColumnId)
VALUES
    (@TestingColumnId1, @TestingColumnId2),
    (@TestingColumnId2, @TestingColumnId1);


-- Testing <-> Deployment
INSERT INTO ColumnEdges (FromColumnId, ToColumnId)
VALUES
    (@TestingColumnId1, @DeploymentColumnId2),
    (@DeploymentColumnId2, @TestingColumnId1);


-- Released <-> Released
INSERT INTO ColumnEdges (FromColumnId, ToColumnId)
VALUES
    (@ReleasedColumnId1, @ReleasedColumnId2),
    (@ReleasedColumnId2, @ReleasedColumnId1);