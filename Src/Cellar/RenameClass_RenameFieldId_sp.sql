IF OBJECT_ID('RenameClass_RenameFieldId') IS NOT NULL BEGIN
	PRINT 'removing procedure RenameClass_RenameFieldId';
	DROP PROCEDURE RenameClass_RenameFieldId;
END
GO
PRINT 'creating procedure RenameClass_RenameFieldId';
GO

CREATE PROCEDURE RenameClass_RenameFieldId
	@OldClassId INT,
	@NewClassId INT,
	@OldFieldId INT,
	@NewFieldId INT OUTPUT
AS

	------------------------------------------------------------------
	--( Note! This is a supporting procedure of RenameClass_sp.sql )--
	------------------------------------------------------------------

	SET @NewFieldId =
		CAST(
			CAST(@NewClassId AS VARCHAR(20)) +
			SUBSTRING(
				CAST(@OldFieldId AS VARCHAR(20)),
				LEN(@OldClassId) + 1,
				LEN(@OldFieldId) - LEN(@OldClassId))
		AS INT);
GO