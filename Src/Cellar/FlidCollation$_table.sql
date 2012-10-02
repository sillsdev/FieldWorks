--( FlidCollation$ table.
IF OBJECT_ID('FlidCollation$') IS NOT NULL BEGIN
	PRINT 'removing table FlidCollation$'
	DROP TABLE [FlidCollation$]
END
GO
PRINT 'creating table FlidCollation$'
GO
CREATE TABLE FlidCollation$ (
	[Id]			INT	PRIMARY KEY CLUSTERED IDENTITY(1,1),
	[Ws]			INT NOT NULL,
	[CollationId]	INT	NOT NULL,
	[Flid]			INT NOT NULL)
