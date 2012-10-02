-- update database FROM version 200099 to 200100
BEGIN TRANSACTION  --( will be rolled back if wrong version#


-------------------------------------------------------------------------------
-- Fix existing MoEndocentricCompounds
-------------------------------------------------------------------------------

DECLARE @hvo INT, @newId INT, @guid uniqueidentifier
DECLARE eccCursor CURSOR local static forward_only read_only FOR
	SELECT [Id] FROM MoEndoCentricCompound
OPEN eccCursor
FETCH NEXT FROM eccCursor INTO @hvo
WHILE @@FETCH_STATUS = 0
BEGIN
	set @newId = NULL
	select @newId=dst from MoEndoCentricCompound_OverridingMsa where src = @hvo
	if @newId is NULL BEGIN
		EXEC CreateObject_MoStemMsa null, @hvo, 5033002, 0, @NewId OUTPUT, @guid OUTPUT
	END
	FETCH NEXT FROM eccCursor INTO @hvo
END
CLOSE eccCursor
DEALLOCATE eccCursor
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200099
begin
	UPDATE Version$ SET DbVer = 200100
	COMMIT TRANSACTION
	print 'database updated to version 200100'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200099 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
