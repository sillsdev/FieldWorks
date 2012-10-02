/***********************************************************************************************
 * Stored Procedure: LogInfo$
 *
 * Description:
 *  Returns info about the SQL Server log file.
 *
 * Notes:
 *	This can be tricky, because any command that changes data will change the numbers of
 *	the log file, which are the numbers we're looking at.
 *
 *  I've debated whether to pass the database name in, but currently the stored procedure is
 *  written to be smart enough to fire on the current database.
 *
 *	Originally written because the log file is filling up the disk causing fatal, unrecoverable
 *	errors.
 **********************************************************************************************/

IF OBJECT_ID('LogInfo$') IS NOT NULL BEGIN
	PRINT 'removing procedure LogInfo$'
	DROP PROCEDURE [LogInfo$]
END
GO
PRINT 'creating procedure LogInfo$'
GO

CREATE PROCEDURE LogInfo$
	@ncLogFileName NCHAR(128) OUTPUT,
	@nLogFileSize INT OUTPUT,
	@nLogFileSpaceUsed INT OUTPUT,
	@bnLogFileMaxSize BIGINT OUTPUT,
	@nSpaceAvailForLogFile INT OUTPUT
AS
BEGIN
	DECLARE
		@rLogUsedPercent REAL

	--( We'll get the log file size from sysfiles after doing the temp tables,
	--( because the size can change. status of 0x40 indicates the log file

	SELECT @ncLogFileName = [filename] FROM sysfiles WHERE ([status] & 0x40) <> 0

	--( This block can change the numbers on the log file, so it is executed before
	--( other code. We'll add more to mbfree momentarily.

	CREATE TABLE #fixeddrives (drive CHAR(1) PRIMARY KEY, mbfree INTEGER NOT NULL)
	--( xp_fixeddrives is not documented by MS, but it is quite a bit on the web. See, for example,
	--( http://www.mssqlcity.com/FAQ/Devel/get_list_of_drives.htm
	INSERT INTO #fixeddrives EXEC master..xp_fixeddrives
	SELECT @nSpaceAvailForLogFile = mbfree FROM #fixeddrives WHERE drive = LEFT(@ncLogFileName, 1)
	DROP TABLE #fixeddrives
	--( xp_fixeddrives expresses space in Mb. We want it in Kb
	SET @nSpaceAvailForLogFile = @nSpaceAvailForLogFile * 1024

	--( We have a create temp table after this, but there's no helping it.

	SELECT
		@nLogFileSize = [size],
		@bnLogFileMaxSize = [maxsize]
	FROM sysfiles
	WHERE ([status] & 0x40) <> 0 -- status of 0x40 indicates the log file

	--( SQL Server returns the size of the file in 8-KB pages. We want it in KB. The conversion
	--( factor is 8.

	SET @nLogFileSize = @nLogFileSize * 8

	--( SQL Server returns the max size of the file in 8-KB pages. We want it in KB. The conversion
	--( factor 8. The value of maxsize could also be 0 (no growth) or -1 (grow until disk full).

	IF @bnLogFileMaxSize > 0
		SET @bnLogFileMaxSize = @bnLogFileMaxSize * 8

	--( This block can change the numbers on the log file, so it is executed after the
	--( previous code. We would use a table variable here, but SQL Server will give an
	--( error "EXECUTE cannot be used as a source when inserting into a table variable."

	--( DBCC SQLPERF (LOGSPACE) returns space used as a percentage.

	CREATE TABLE #LogInfo (DbName NVARCHAR(128), LogSize REAL, LogUsed REAL, Status TINYINT)
	INSERT INTO #LogInfo EXEC ('DBCC SQLPERF (LOGSPACE)')
	SELECT @rLogUsedPercent = LogUsed FROM #LogInfo WHERE [DbName] = DB_NAME()
	DROP TABLE #LogInfo

	SET @nLogFileSpaceUsed = ROUND(@nLogFileSize * (@rLogUsedPercent / 100), 0)

	--( Now calc how much space is available for the log file.

	IF @bnLogFileMaxSize = -1
		--(	@nSpaceAvailForLogFile already has free disk space.
		SET @nSpaceAvailForLogFile = @nSpaceAvailForLogFile + (@nLogFileSize - @nLogFileSpaceUsed)
	ELSE IF @bnLogFileMaxSize = 0
		SET @nSpaceAvailForLogFile = @nLogFileSize - @nLogFileSpaceUsed
	ELSE
		SET @nSpaceAvailForLogFile = @bnLogFileMaxSize  - @nLogFileSpaceUsed

END
GO
