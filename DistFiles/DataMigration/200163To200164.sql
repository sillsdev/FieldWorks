-- update database FROM version 200163 to 200164

BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- Delete ScrScriptureNotes that don't have their type set. This is rare but
-- can happen because the TE import process used to be able to create an
-- annotation without completely initializing it.
-------------------------------------------------------------------------------
IF EXISTS(SELECT * FROM Scripture)
BEGIN
	DECLARE @id int

	DECLARE ScrAnnotationsCur CURSOR FOR
	SELECT [id] FROM ScrScriptureNote_
	where AnnotationType is null

	OPEN ScrAnnotationsCur

	FETCH NEXT FROM ScrAnnotationsCur INTO @id

	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC DeleteObj$ @objId = @id

		FETCH NEXT FROM ScrAnnotationsCur
		INTO @id
	END

	CLOSE ScrAnnotationsCur
	DEALLOCATE ScrAnnotationsCur

END
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200163
begin
	UPDATE Version$ SET DbVer = 200164
	COMMIT TRANSACTION
	print 'database updated to version 200164'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200163 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
