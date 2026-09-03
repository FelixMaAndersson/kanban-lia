ALTER TABLE dbo.Placements
ADD CONSTRAINT FK_Placements_Boards_BoardId 
	FOREIGN KEY (BoardId) 
	REFERENCES dbo.Boards(Id);