DECLARE @BoardId2 UNIQUEIDENTIFIER =
    '11111111-1111-1111-1111-111111111112';

DECLARE @NoEdgeColumnId2 UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222233';

INSERT INTO Columns
(
    Id,
    BoardId,
    Title,
    Position,
    RequestWritable
)
VALUES
(
    @NoEdgeColumnId2,
    @BoardId2,
    'No Edge',
    6,
    1
);