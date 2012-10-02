-- Update database FROM version 200160 to 200161

--( ALTER DATABASE will only work in autocommit mode. This means
--( we can't have any explicit transactions. If you copy this for
--( the next data migration, be sure to unremark the three
--( commands using explicit transactions.
--BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-----------------------------------------------------------------------
-- Set auto_close off on all databases that currently have it set on.
-- This is because SQL Server takes 1-2 seconds per database to open
-- and close in routines such as File/Open. It may be slowing down the
-- system in other places too. If the databases aleady have it set off,
-- this routine won't take any extra time.
-----------------------------------------------------------------------

DECLARE
	@nvcDbName NVARCHAR(4000),
	@nAutoClose INT,
	@nvcSql NVARCHAR(4000)

DECLARE curDbs CURSOR LOCAL FAST_FORWARD FOR
	SELECT Name
	FROM master..sysdatabases
	WHERE Name NOT IN ('master', 'model', 'msdb', 'tempdb')

OPEN curDbs
FETCH curDbs INTO @nvcDbName
WHILE @@FETCH_STATUS = 0 BEGIN
	SELECT @nAutoClose = DATABASEPROPERTY(@nvcDbName, 'IsAutoClose')
	IF @nAutoClose = 1 BEGIN
		SET @nvcSql = N'ALTER DATABASE [' + @nvcDbName + N'] SET AUTO_CLOSE OFF'
		EXECUTE sp_executesql @nvcSql
	END
	FETCH curDbs INTO @nvcDbName
END
CLOSE curDbs
DEALLOCATE curDbs

GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200160
BEGIN
	UPDATE [Version$] SET [DbVer] = 200161
--	COMMIT TRANSACTION
	PRINT 'database updated to version 200161'
END

ELSE
BEGIN
--	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200160 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
