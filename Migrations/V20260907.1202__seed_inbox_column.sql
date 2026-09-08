DECLARE @BoardId UNIQUEIDENTIFIER =
    '11111111-1111-1111-1111-111111111111';

DECLARE @InboxColumnId UNIQUEIDENTIFIER =
    '22222222-2222-2222-2222-222222222220';


-- Move existing columns one step to the right to make space for the Inbox column at position 0
UPDATE Columns
SET Position = Position + 1
WHERE BoardId = @BoardId;


-- Insert Inbox at position 0
INSERT INTO Columns
(
    Id,
    BoardId,
    Title,
    Position
)
VALUES
(
    @InboxColumnId,
    @BoardId,
    'Inbox',
    0
);