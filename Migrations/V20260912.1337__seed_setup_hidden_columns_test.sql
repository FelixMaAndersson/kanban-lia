-- =========================================
-- Boards
-- =========================================

DECLARE @BoardId1 UNIQUEIDENTIFIER =
    '11111111-1111-1111-1111-111111111111';

DECLARE @BoardId2 UNIQUEIDENTIFIER =
    '11111111-1111-1111-1111-111111111112';

INSERT INTO Boards (Id, Title)
VALUES
    (@BoardId1, 'Developement'),
    (@BoardId2, 'Testing');


-- =========================================
-- Columns - Board 1
-- =========================================

DECLARE @InboxColumnId1 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222220';

DECLARE @RejectedColumnId1 UNIQUEIDENTIFIER =
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


INSERT INTO Columns (Id, BoardId, Title, Position, RequestWritable)
VALUES
    (@InboxColumnId1,      @BoardId1, 'Inbox',        0, 1),
    (@RejectedColumnId1,   @BoardId1, 'Rejected',     1, 0),
    (@TodoColumnId1,       @BoardId1, 'Todo',         2, 1),
    (@DoingColumnId1,      @BoardId1, 'Doing',        3, 1),
    (@SendToTestColumnId1, @BoardId1, 'Send to Test', 4, 1),
    (@TestingColumnId1,    @BoardId1, 'Testing',      5, 1),
    (@ReleasedColumnId1,   @BoardId1, 'Released',     6, 1);


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


INSERT INTO Columns (Id, BoardId, Title, Position, RequestWritable)
VALUES
    (@InboxColumnId2,      @BoardId2, 'Inbox',      0, 1),
    (@ToTestColumnId2,     @BoardId2, 'To Test',    1, 1),
    (@RejectColumnId2,     @BoardId2, 'Reject',     2, 1),
    (@TestingColumnId2,    @BoardId2, 'Testing',    3, 1),
    (@DeploymentColumnId2, @BoardId2, 'Deployment', 4, 1),
    (@ReleasedColumnId2,   @BoardId2, 'Released',   5, 1);


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
    (@InboxColumnId1, @InboxColumnId2);

-- Todo <-> Inbox
INSERT INTO ColumnEdges (FromColumnId, ToColumnId)
VALUES
    (@TodoColumnId1, @InboxColumnId2);

-- Doing <-> Inbox
INSERT INTO ColumnEdges (FromColumnId, ToColumnId)
VALUES
    (@DoingColumnId1, @InboxColumnId2);

-- Send to Test <-> To Test
INSERT INTO ColumnEdges (FromColumnId, ToColumnId)
VALUES
    (@SendToTestColumnId1, @ToTestColumnId2);


-- Rejected <-> Reject
INSERT INTO ColumnEdges (FromColumnId, ToColumnId)
VALUES
    (@RejectedColumnId1, @RejectColumnId2);


-- Testing <-> Testing
INSERT INTO ColumnEdges (FromColumnId, ToColumnId)
VALUES
    (@TestingColumnId1, @TestingColumnId2);


-- Testing <-> Deployment
INSERT INTO ColumnEdges (FromColumnId, ToColumnId)
VALUES
    (@TestingColumnId1, @DeploymentColumnId2);


-- Released <-> Released
INSERT INTO ColumnEdges (FromColumnId, ToColumnId)
VALUES
    (@ReleasedColumnId1, @ReleasedColumnId2);