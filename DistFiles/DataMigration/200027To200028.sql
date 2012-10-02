-- update database from version 200027 to 200028
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- Add stored procedure LockDetails
-------------------------------------------------------------------------------

/**************************************************************************
 * Procedure: LockDetails
 *
 * Description:
 * ============
 *	Returns Detailed lock info
 *
 * Parameters:
 * ===========
 *	@nvcOption: See sample calls below for options
 *	@nManageTempTable 1 = yes (default) stored proc will handle temp table,
 *		0 = no -- the calling program will manage a temp table called
 *		#tblLockInfo.
 *
 * Notes:
 * ======
 *	See: "Understanding Locking in SQL Server", "sp_lock", and "Displaying
 *	Locking Information" for more information about locks.
 *
 *	In sp_lock, the Status column shows if the lock has been obtained
 *	(GRANT), is blocking on another process (WAIT), or is being converted
 *	to another lock (CNVT). A lock being converted to another lock is held
 *	in one mode, but is waiting to acquire a stronger lock mode (for
 *	example, update to exclusive). When diagnosing blocking issues, a
 *	CNVT can be considered similar to WAIT.
 *
 *	A table variable can not be used when using EXEC. Stored functions
 *	can't access temp tables. A temp table was used here.
 *
 * Sample Calls:
 * =============
 *	EXEC LockDetails 'Blockers' --( will show just blocked process(es),
 *								and processes that might be blockers
 *	EXEC LockDetails 			--( for use with Query Analzer, will show
 *								locks with English descriptions
 *	EXEC LockDetails 'Generic'	--( Generic report of outstanding locks
 *************************************************************************/

IF OBJECT_ID('LockDetails') IS NOT NULL BEGIN
	PRINT 'removing procedure LockDetails'
	DROP PROCEDURE LockDetails
END
GO
PRINT 'creating procedure LockDetails'
GO

CREATE PROCEDURE LockDetails
	@nvcOption NVARCHAR(10) = NULL,
	@nManageTempTable TINYINT = 1
AS

	DECLARE
		@nShowDescriptions TINYINT,
		@nShowBlocksOnly TINYINT

	IF UPPER(@nvcOption) = 'GENERIC' BEGIN
		SET @nShowDescriptions = 0
		SET @nShowBlocksOnly = 0
	END
	ELSE IF UPPER(@nvcOption) = 'BLOCKERS' BEGIN
		SET @nShowDescriptions = 0
		SET @nShowBlocksOnly = 1
	END
	ELSE BEGIN --( IF @nvcOption IS NULL OR UPPER(@nvcOption) = 'DETAILS'
		SET @nShowDescriptions = 1
		SET @nShowBlocksOnly = 0
	END

	DECLARE @tblBlocked TABLE (
		spid INT,
		dbid INT,
		ObjId INT,
		IndId INT,
		Type CHAR(5),
		Resource CHAR(20),
		Mode CHAR(10),
		Status CHAR(10))

	IF @nManageTempTable = 1
		CREATE TABLE #tblLockInfo (
			spid INT,
			dbid INT,
			ObjId INT,
			IndId INT,
			Type CHAR(5),
			Resource CHAR(20),
			Mode CHAR(10),
			Status CHAR(10))

	INSERT INTO #tblLockInfo EXEC sp_lock

	--==( Showing descriptions )==--

	IF @nShowDescriptions != 0

		SELECT
			li.spid,
			(SELECT DISTINCT program_name
				FROM master..sysprocesses
				WHERE spid = li.spid) AS ProgramName,
			li.dbid,
			DB_NAME(li.dbid) AS DatabaseName,
			li.ObjId,
			ISNULL(OBJECT_NAME(li.ObjId), '') AS ObjectName,
			li.IndId,
			li.Type,
			TypeDescription = CASE li.Type
				WHEN 'RID' THEN 'Single row lock'
				WHEN 'KEY' THEN 'Row lock in an index, to protect key ranges in serializable'
				WHEN 'PAG' THEN 'Data or Index page lock'
				WHEN 'EXT' THEN 'Contiguous group of 8 data pages or indexes pages'
				WHEN 'TAB' THEN 'Entire table lock, including data and indexes'
				WHEN 'DB' THEN 'Database lock'
				END,
			li.Resource,
			ResourceDescription = CASE li.Resource
				WHEN 'RID' THEN 'Row identifier'
				WHEN 'KEY' THEN 'Hex # used internally by SQL Server'
				WHEN 'PAG' THEN 'Page number The page is identified by a fileid:page ' +
					'combination, where fileid is the fileid in the sysfiles ' +
					'table, and page is the logical page number within that file.'
				WHEN 'EXT' THEN 'First page number in the extent being locked. The page ' +
					'is identified by a fileid:page combination.'
				WHEN 'TAB' THEN 'See ObjId column for the ID of the table.'
				WHEN 'DB' THEN 'See DbId column for the ID of the database.'
				ELSE ''
				END,
			li.Mode,
			ModeDescription = CASE li.Mode
				WHEN 'IS' THEN 'Intent shared'
				WHEN 'S' THEN 'Shared'
				WHEN 'U' THEN 'Update'
				WHEN 'IX' THEN 'Intent Exclusive'
				WHEN 'SIX' THEN 'Shared Intent Exclusive'
				WHEN 'X' THEN 'Exclusive'
				WHEN 'Sch-M' THEN 'Schema Modification'
				WHEN 'Sch-S' THEN 'Schema Stability'
				WHEN 'BU' THEN 'Bulk Update'
				END,
			li.Status
		FROM #tblLockInfo li

	ELSE BEGIN --( IF @nShowDescriptions = 0

		--==( If looking for blocked and blocking processes )==--

		IF @nShowBlocksOnly = 1 BEGIN

			INSERT INTO @tblBlocked
			SELECT spid, dbid, ObjId, IndId, Type, Resource, Mode, Status
			FROM #tblLockInfo
			WHERE Status = 'WAIT'

			SELECT
				b.spid,
				(SELECT DISTINCT program_name
					FROM master..sysprocesses
					WHERE spid = b.spid) AS ProgramName,
				b.dbid,
				DB_NAME(b.dbid) AS DatabaseName,
				b.ObjId,
				ISNULL(OBJECT_NAME(b.ObjId), '') AS ObjectName,
				b.IndId,
				b.Type,
				b.Resource,
				b.Mode,
				b.Status
			FROM @tblBlocked b
			UNION
			SELECT
				li.spid,
				(SELECT DISTINCT program_name
					FROM master..sysprocesses
					WHERE spid = li.spid) AS ProgramName,
				li.dbid,
				DB_NAME(li.dbid) AS DatabaseName,
				li.ObjId,
				ISNULL(OBJECT_NAME(li.ObjId), '') AS ObjectName,
				li.IndId,
				li.Type,
				li.Resource,
				li.Mode,
				li.Status
			FROM @tblBlocked b
			JOIN #tblLockInfo li ON li.dbid = b.dbid
				AND li.ObjId = b.ObjId
				AND li.Resource = b.Resource
			WHERE li.Status != 'WAIT'
		END

		--==( Generic report of locks for calling program )==--

		ELSE --( IF @nShowBlocksOnly = 0
			SELECT
				li.spid,
				(SELECT DISTINCT program_name
					FROM master..sysprocesses
					WHERE spid = li.spid) AS ProgramName,
				li.dbid,
				DB_NAME(li.dbid) AS DatabaseName,
				li.ObjId,
				ISNULL(OBJECT_NAME(li.ObjId), '') AS ObjectName,
				li.IndId,
				li.Type,
				li.Resource,
				li.Mode,
				li.Status
			FROM #tblLockInfo li
			WHERE DB_NAME(li.dbid) != 'tempdb' AND DB_NAME(li.dbid) != 'master'
	END --( IF @nShowDescriptions = 0

	IF @nManageTempTable = 1
		DROP TABLE #tblLockInfo

	RETURN
GO

-------------------------------------------------------------------------------

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200027
begin
	update Version$ set DbVer = 200028
	COMMIT TRANSACTION
	print 'database updated to version 200028'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200027 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
