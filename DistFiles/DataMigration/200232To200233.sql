-- Update database from version 200232 to 200233
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- This stored procedure is part of the solution to LT-8762:
-- Cannot import unchanged XML dump of Sena 3
-------------------------------------------------------------------------------

/*****************************************************************************
 * Procedure: DeleteRefOwnedObjects
 *
 * Find any objects which claim to be owned by a reference field, and delete
 * them.
 *
 *****************************************************************************/
if object_id('DeleteRefOwnedObjects') is not null begin
	print 'removing proc DeleteRefOwnedObjects'
	drop proc DeleteRefOwnedObjects
end
go
print 'creating proc DeleteRefOwnedObjects'
go

CREATE PROC DeleteRefOwnedObjects
AS
DECLARE @hvoBad INT
DECLARE @ObjList nvarchar(4000)
DECLARE badObjs CURSOR FAST_FORWARD FOR
	SELECT co.Id
	FROM CmObject co
	JOIN Field$ f ON f.Id=co.OwnFlid$ AND f.Type IN (24,26,28)
OPEN badObjs
FETCH badObjs INTO @hvoBad
WHILE @@FETCH_STATUS = 0 BEGIN
	IF @ObjList IS NULL SET @ObjList = N','
	SET @ObjList = @ObjList + CAST(@hvoBad as nvarchar(10)) + N','
	IF LEN(@ObjList) > 3980 BEGIN
		EXEC DeleteObjects @ObjList
		SET @ObjList = NULL
		END
	FETCH badObjs INTO @hvoBad
	END
CLOSE badObjs
DEALLOCATE badObjs
IF NOT @ObjList IS NULL EXEC DeleteObjects @ObjList
GO

EXEC DeleteRefOwnedObjects

drop proc DeleteRefOwnedObjects
print 'removing proc DeleteRefOwnedObjects since we don''t think we''ll ever need it again!'
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200232
BEGIN
	UPDATE Version$ SET DbVer = 200233
	COMMIT TRANSACTION
	PRINT 'database updated to version 200233'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200232 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
