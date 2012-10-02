-- update database FROM version 200115 to 200116

BEGIN TRANSACTION  --( will be rolled back if wrong version#
	-- Change ColumnInfo for the Category CmFilter from
	--  ScrScriptureNote, AnnotationType to
	--  ScrScriptureNote, Categories
UPDATE CmFilter
SET [ColumnInfo]='3018,3018002'
WHERE [Name]='Category' AND [ColumnInfo]='3018,34003'

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200115
begin
	UPDATE Version$ SET DbVer = 200116
	COMMIT TRANSACTION
	print 'database updated to version 200116'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200115 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
