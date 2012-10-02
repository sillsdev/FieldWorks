-- update database from version 200042 to 200043
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
--( Set ownership of any ReversalIndex objects to the lexicon.
declare @ldbID int
SELECT  top 1 @ldbID=ID
FROM LexicalDatabase

UPDATE CmObject
SET Owner$=@ldbID, OwnFlid$=5005017
WHERE Class$=5052

GO

---------------------------------------------------------------------------------------------------------------------------------------------

--( Put in the new trigger, which is in addition to TR_Field$_UpdateModel_Ins

IF OBJECT_ID('TR_Field$_UpdateModel_InsLast') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_Field$_UpdateModel_InsLast'
	DROP TRIGGER TR_Field$_UpdateModel_InsLast
END
GO
PRINT 'creating trigger TR_Field$_UpdateModel_InsLast'
GO

CREATE TRIGGER TR_Field$_UpdateModel_InsLast ON Field$ FOR INSERT
AS
	DECLARE
		@nErr INT,
		@nClassid INT

	SELECT @nClassId = Class FROM inserted
	print @nClassId

	EXEC @nErr = UpdateClassView$ @nClassId, 1
	IF @nErr <> 0 GOTO LFail

	RETURN

LFail:
	ROLLBACK TRANSACTION
	RETURN
GO


EXEC sp_settriggerorder 'TR_Field$_UpdateModel_InsLast', 'last', 'INSERT'
GO

-------------------------------------------------------------------------------

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200042
begin
	update Version$ set DbVer = 200043
	COMMIT TRANSACTION
	print 'database updated to version 200042'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200042 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
