-- update database FROM version 200138 to 200139
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- Make sure that fnGetRefsToObj is properly generated in databases created
-- over the past five days.
-------------------------------------------------------------------------------

EXEC CreateGetRefsToObj

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200138
begin
	UPDATE Version$ SET DbVer = 200139
	COMMIT TRANSACTION
	print 'database updated to version 200139'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200138 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO