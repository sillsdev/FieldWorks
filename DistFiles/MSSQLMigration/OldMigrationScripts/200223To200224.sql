-- Update database from version 200223 to 200224
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FDB-203: Fix trace error message. Also enhance the debugging trace, and
-- create a new trace for watching performance.
-------------------------------------------------------------------------------

--( Remove the old trace procedures.

if object_id('StartTrace$') is not null begin
	print 'removing proc StartTrace$'
	drop procedure StartTrace$
end
go

if object_id('StopTrace$') is not null begin
	print 'removing proc StopTrace$'
	drop procedure StopTrace$
end
go

if object_id('StopTraces$') is not null begin
	print 'removing proc StopTraces$'
	drop procedure StopTraces$
end
go

---------------------------------------------------------------------------------
--( Build in the new procedures

if object_id('StopTrace') is not null begin
	print 'removing proc StopTrace'
	drop procedure StopTrace
end
go
print 'creating procedure StopTrace'
go

CREATE PROCEDURE StopTrace
	@TraceFile NVARCHAR(MAX) = N'Fw'
AS
	DECLARE	@TraceId INT;
	SET @TraceId = NULL;

	SELECT TOP 1 @TraceId = traceid  --( should be only 1
	FROM FN_TRACE_GETINFO(0)
	WHERE property = 2 --( file name
		AND CONVERT(NVARCHAR(MAX), value) LIKE N'%' + @TraceFile + N'%';

	IF (@TraceId IS NOT NULL) BEGIN
		EXEC SP_TRACE_SETSTATUS @TraceId, 0; --( stop the trace
		EXEC SP_TRACE_SETSTATUS @TraceId, 2; --( close the trace
	END
GO

---------------------------------------------------------------------------------

if object_id('PrepStartTrace') is not null begin
	print 'removing proc PrepStartTrace';
	drop procedure PrepStartTrace;
end
go
print 'creating procedure PrepStartTrace';
go

CREATE PROCEDURE PrepStartTrace
	@TraceFile NVARCHAR(256),
	@TraceId INT OUTPUT
AS
	DECLARE
		@Return INT,
		@ErrorMessage NVARCHAR(2000),
		@Command VARCHAR(512),
		@CmdShellReturn INT,
		@MaxFileSize BIGINT;

	SET @Return = 0;

	--( sp_trace_create automatically puts a .trc extension on the file name
	IF UPPER(RIGHT(@TraceFile, 4)) = N'.TRC' BEGIN
		SET @TraceFile = SUBSTRING(@TraceFile, 1, LEN(@TraceFile) - 4);
	END

	--( Stop the trace if it is already running.

	EXEC @Return = StopTrace @TraceFile
	IF @Return = 1 BEGIN
		SET @ErrorMessage = N'StartTrace: Couldn''t stop trace ' + @TraceFile + N'.trc';
		RAISERROR (@ErrorMessage, 16, 1)
	END

	--( Delete the file(s) if they exist

	SET @Command = 'if exist ' + @TraceFile + '.trc del ' + @TraceFile + '*.trc';
	EXEC @CmdShellReturn = master..xp_cmdshell @Command, NO_OUTPUT;
	IF NOT @CmdShellReturn = 0 BEGIN --( If not 0, then it will be 1
		SET @ErrorMessage = N'StopTrace: Couldn''t delete file ' + @TraceFile;
		RAISERROR (@ErrorMessage, 16, 1)
	END

	--( Start the new trace

	SET @maxfilesize = 5;
	EXEC @Return = sp_trace_create @TraceID OUTPUT, 2, @TraceFile, @MaxFileSize, NULL;
	IF (@Return != 0) BEGIN
		SET @ErrorMessage = N'StartTrace: Couldn''t start ' + @TraceFile + N'.trc.';
		RAISERROR (@ErrorMessage, 16, 1)
	END
GO

---------------------------------------------------------------------------------

if object_id('StartDebugTrace') is not null begin
	print 'removing proc StartDebugTrace';
	drop procedure StartDebugTrace;
end
go
print 'creating procedure StartDebugTrace';
go

CREATE PROCEDURE StartDebugTrace
	@TraceFile NVARCHAR(256) = N'C:\FwDebug'
AS
	DECLARE @TraceId INT
	EXEC PrepStartTrace @TraceFile, @TraceId OUTPUT

	/****************************************************/
	/* Created by: SQL Server Profiler 2005             */
	/* Date: 08/07/2008  05:21:39 PM         */
	/****************************************************/

	-- Client side File and Table cannot be scripted

	-- Set the events
	declare @on bit
	set @on = 1
	exec sp_trace_setevent @TraceID, 196, 7, @on
	exec sp_trace_setevent @TraceID, 196, 8, @on
	exec sp_trace_setevent @TraceID, 196, 12, @on
	exec sp_trace_setevent @TraceID, 196, 1, @on
	exec sp_trace_setevent @TraceID, 196, 9, @on
	exec sp_trace_setevent @TraceID, 196, 6, @on
	exec sp_trace_setevent @TraceID, 196, 10, @on
	exec sp_trace_setevent @TraceID, 196, 14, @on
	exec sp_trace_setevent @TraceID, 196, 26, @on
	exec sp_trace_setevent @TraceID, 196, 11, @on
	exec sp_trace_setevent @TraceID, 196, 35, @on
	exec sp_trace_setevent @TraceID, 196, 51, @on
	exec sp_trace_setevent @TraceID, 78, 7, @on
	exec sp_trace_setevent @TraceID, 78, 8, @on
	exec sp_trace_setevent @TraceID, 78, 12, @on
	exec sp_trace_setevent @TraceID, 78, 9, @on
	exec sp_trace_setevent @TraceID, 78, 6, @on
	exec sp_trace_setevent @TraceID, 78, 10, @on
	exec sp_trace_setevent @TraceID, 78, 14, @on
	exec sp_trace_setevent @TraceID, 78, 26, @on
	exec sp_trace_setevent @TraceID, 78, 11, @on
	exec sp_trace_setevent @TraceID, 78, 35, @on
	exec sp_trace_setevent @TraceID, 78, 51, @on
	exec sp_trace_setevent @TraceID, 74, 7, @on
	exec sp_trace_setevent @TraceID, 74, 8, @on
	exec sp_trace_setevent @TraceID, 74, 9, @on
	exec sp_trace_setevent @TraceID, 74, 6, @on
	exec sp_trace_setevent @TraceID, 74, 10, @on
	exec sp_trace_setevent @TraceID, 74, 14, @on
	exec sp_trace_setevent @TraceID, 74, 26, @on
	exec sp_trace_setevent @TraceID, 74, 11, @on
	exec sp_trace_setevent @TraceID, 74, 35, @on
	exec sp_trace_setevent @TraceID, 74, 51, @on
	exec sp_trace_setevent @TraceID, 74, 12, @on
	exec sp_trace_setevent @TraceID, 76, 7, @on
	exec sp_trace_setevent @TraceID, 76, 8, @on
	exec sp_trace_setevent @TraceID, 76, 9, @on
	exec sp_trace_setevent @TraceID, 76, 6, @on
	exec sp_trace_setevent @TraceID, 76, 10, @on
	exec sp_trace_setevent @TraceID, 76, 14, @on
	exec sp_trace_setevent @TraceID, 76, 26, @on
	exec sp_trace_setevent @TraceID, 76, 11, @on
	exec sp_trace_setevent @TraceID, 76, 35, @on
	exec sp_trace_setevent @TraceID, 76, 51, @on
	exec sp_trace_setevent @TraceID, 76, 12, @on
	exec sp_trace_setevent @TraceID, 53, 7, @on
	exec sp_trace_setevent @TraceID, 53, 8, @on
	exec sp_trace_setevent @TraceID, 53, 9, @on
	exec sp_trace_setevent @TraceID, 53, 6, @on
	exec sp_trace_setevent @TraceID, 53, 10, @on
	exec sp_trace_setevent @TraceID, 53, 14, @on
	exec sp_trace_setevent @TraceID, 53, 26, @on
	exec sp_trace_setevent @TraceID, 53, 11, @on
	exec sp_trace_setevent @TraceID, 53, 35, @on
	exec sp_trace_setevent @TraceID, 53, 51, @on
	exec sp_trace_setevent @TraceID, 53, 12, @on
	exec sp_trace_setevent @TraceID, 70, 7, @on
	exec sp_trace_setevent @TraceID, 70, 8, @on
	exec sp_trace_setevent @TraceID, 70, 12, @on
	exec sp_trace_setevent @TraceID, 70, 9, @on
	exec sp_trace_setevent @TraceID, 70, 6, @on
	exec sp_trace_setevent @TraceID, 70, 10, @on
	exec sp_trace_setevent @TraceID, 70, 14, @on
	exec sp_trace_setevent @TraceID, 70, 26, @on
	exec sp_trace_setevent @TraceID, 70, 11, @on
	exec sp_trace_setevent @TraceID, 70, 35, @on
	exec sp_trace_setevent @TraceID, 70, 51, @on
	exec sp_trace_setevent @TraceID, 77, 7, @on
	exec sp_trace_setevent @TraceID, 77, 8, @on
	exec sp_trace_setevent @TraceID, 77, 12, @on
	exec sp_trace_setevent @TraceID, 77, 9, @on
	exec sp_trace_setevent @TraceID, 77, 6, @on
	exec sp_trace_setevent @TraceID, 77, 10, @on
	exec sp_trace_setevent @TraceID, 77, 14, @on
	exec sp_trace_setevent @TraceID, 77, 26, @on
	exec sp_trace_setevent @TraceID, 77, 11, @on
	exec sp_trace_setevent @TraceID, 77, 35, @on
	exec sp_trace_setevent @TraceID, 77, 51, @on
	exec sp_trace_setevent @TraceID, 16, 7, @on
	exec sp_trace_setevent @TraceID, 16, 15, @on
	exec sp_trace_setevent @TraceID, 16, 8, @on
	exec sp_trace_setevent @TraceID, 16, 12, @on
	exec sp_trace_setevent @TraceID, 16, 9, @on
	exec sp_trace_setevent @TraceID, 16, 6, @on
	exec sp_trace_setevent @TraceID, 16, 10, @on
	exec sp_trace_setevent @TraceID, 16, 14, @on
	exec sp_trace_setevent @TraceID, 16, 26, @on
	exec sp_trace_setevent @TraceID, 16, 11, @on
	exec sp_trace_setevent @TraceID, 16, 35, @on
	exec sp_trace_setevent @TraceID, 16, 51, @on
	exec sp_trace_setevent @TraceID, 193, 31, @on
	exec sp_trace_setevent @TraceID, 193, 35, @on
	exec sp_trace_setevent @TraceID, 193, 51, @on
	exec sp_trace_setevent @TraceID, 193, 14, @on
	exec sp_trace_setevent @TraceID, 137, 15, @on
	exec sp_trace_setevent @TraceID, 137, 51, @on
	exec sp_trace_setevent @TraceID, 137, 1, @on
	exec sp_trace_setevent @TraceID, 137, 26, @on
	exec sp_trace_setevent @TraceID, 22, 7, @on
	exec sp_trace_setevent @TraceID, 22, 31, @on
	exec sp_trace_setevent @TraceID, 22, 8, @on
	exec sp_trace_setevent @TraceID, 22, 1, @on
	exec sp_trace_setevent @TraceID, 22, 9, @on
	exec sp_trace_setevent @TraceID, 22, 6, @on
	exec sp_trace_setevent @TraceID, 22, 10, @on
	exec sp_trace_setevent @TraceID, 22, 14, @on
	exec sp_trace_setevent @TraceID, 22, 26, @on
	exec sp_trace_setevent @TraceID, 22, 11, @on
	exec sp_trace_setevent @TraceID, 22, 35, @on
	exec sp_trace_setevent @TraceID, 22, 51, @on
	exec sp_trace_setevent @TraceID, 22, 12, @on
	exec sp_trace_setevent @TraceID, 21, 7, @on
	exec sp_trace_setevent @TraceID, 21, 31, @on
	exec sp_trace_setevent @TraceID, 21, 8, @on
	exec sp_trace_setevent @TraceID, 21, 1, @on
	exec sp_trace_setevent @TraceID, 21, 9, @on
	exec sp_trace_setevent @TraceID, 21, 6, @on
	exec sp_trace_setevent @TraceID, 21, 10, @on
	exec sp_trace_setevent @TraceID, 21, 14, @on
	exec sp_trace_setevent @TraceID, 21, 26, @on
	exec sp_trace_setevent @TraceID, 21, 11, @on
	exec sp_trace_setevent @TraceID, 21, 35, @on
	exec sp_trace_setevent @TraceID, 21, 51, @on
	exec sp_trace_setevent @TraceID, 21, 12, @on
	exec sp_trace_setevent @TraceID, 33, 7, @on
	exec sp_trace_setevent @TraceID, 33, 31, @on
	exec sp_trace_setevent @TraceID, 33, 8, @on
	exec sp_trace_setevent @TraceID, 33, 1, @on
	exec sp_trace_setevent @TraceID, 33, 9, @on
	exec sp_trace_setevent @TraceID, 33, 6, @on
	exec sp_trace_setevent @TraceID, 33, 10, @on
	exec sp_trace_setevent @TraceID, 33, 14, @on
	exec sp_trace_setevent @TraceID, 33, 26, @on
	exec sp_trace_setevent @TraceID, 33, 11, @on
	exec sp_trace_setevent @TraceID, 33, 35, @on
	exec sp_trace_setevent @TraceID, 33, 51, @on
	exec sp_trace_setevent @TraceID, 33, 12, @on
	exec sp_trace_setevent @TraceID, 127, 7, @on
	exec sp_trace_setevent @TraceID, 127, 8, @on
	exec sp_trace_setevent @TraceID, 127, 9, @on
	exec sp_trace_setevent @TraceID, 127, 6, @on
	exec sp_trace_setevent @TraceID, 127, 10, @on
	exec sp_trace_setevent @TraceID, 127, 14, @on
	exec sp_trace_setevent @TraceID, 127, 26, @on
	exec sp_trace_setevent @TraceID, 127, 11, @on
	exec sp_trace_setevent @TraceID, 127, 35, @on
	exec sp_trace_setevent @TraceID, 127, 51, @on
	exec sp_trace_setevent @TraceID, 127, 12, @on
	exec sp_trace_setevent @TraceID, 67, 7, @on
	exec sp_trace_setevent @TraceID, 67, 31, @on
	exec sp_trace_setevent @TraceID, 67, 8, @on
	exec sp_trace_setevent @TraceID, 67, 1, @on
	exec sp_trace_setevent @TraceID, 67, 9, @on
	exec sp_trace_setevent @TraceID, 67, 6, @on
	exec sp_trace_setevent @TraceID, 67, 10, @on
	exec sp_trace_setevent @TraceID, 67, 14, @on
	exec sp_trace_setevent @TraceID, 67, 26, @on
	exec sp_trace_setevent @TraceID, 67, 11, @on
	exec sp_trace_setevent @TraceID, 67, 35, @on
	exec sp_trace_setevent @TraceID, 67, 51, @on
	exec sp_trace_setevent @TraceID, 67, 12, @on
	exec sp_trace_setevent @TraceID, 55, 7, @on
	exec sp_trace_setevent @TraceID, 55, 8, @on
	exec sp_trace_setevent @TraceID, 55, 9, @on
	exec sp_trace_setevent @TraceID, 55, 6, @on
	exec sp_trace_setevent @TraceID, 55, 10, @on
	exec sp_trace_setevent @TraceID, 55, 14, @on
	exec sp_trace_setevent @TraceID, 55, 26, @on
	exec sp_trace_setevent @TraceID, 55, 11, @on
	exec sp_trace_setevent @TraceID, 55, 35, @on
	exec sp_trace_setevent @TraceID, 55, 51, @on
	exec sp_trace_setevent @TraceID, 55, 12, @on
	exec sp_trace_setevent @TraceID, 80, 7, @on
	exec sp_trace_setevent @TraceID, 80, 8, @on
	exec sp_trace_setevent @TraceID, 80, 12, @on
	exec sp_trace_setevent @TraceID, 80, 9, @on
	exec sp_trace_setevent @TraceID, 80, 6, @on
	exec sp_trace_setevent @TraceID, 80, 10, @on
	exec sp_trace_setevent @TraceID, 80, 14, @on
	exec sp_trace_setevent @TraceID, 80, 26, @on
	exec sp_trace_setevent @TraceID, 80, 11, @on
	exec sp_trace_setevent @TraceID, 80, 35, @on
	exec sp_trace_setevent @TraceID, 80, 51, @on
	exec sp_trace_setevent @TraceID, 69, 7, @on
	exec sp_trace_setevent @TraceID, 69, 8, @on
	exec sp_trace_setevent @TraceID, 69, 12, @on
	exec sp_trace_setevent @TraceID, 69, 9, @on
	exec sp_trace_setevent @TraceID, 69, 6, @on
	exec sp_trace_setevent @TraceID, 69, 10, @on
	exec sp_trace_setevent @TraceID, 69, 14, @on
	exec sp_trace_setevent @TraceID, 69, 26, @on
	exec sp_trace_setevent @TraceID, 69, 11, @on
	exec sp_trace_setevent @TraceID, 69, 35, @on
	exec sp_trace_setevent @TraceID, 69, 51, @on
	exec sp_trace_setevent @TraceID, 162, 7, @on
	exec sp_trace_setevent @TraceID, 162, 31, @on
	exec sp_trace_setevent @TraceID, 162, 8, @on
	exec sp_trace_setevent @TraceID, 162, 1, @on
	exec sp_trace_setevent @TraceID, 162, 9, @on
	exec sp_trace_setevent @TraceID, 162, 6, @on
	exec sp_trace_setevent @TraceID, 162, 10, @on
	exec sp_trace_setevent @TraceID, 162, 14, @on
	exec sp_trace_setevent @TraceID, 162, 26, @on
	exec sp_trace_setevent @TraceID, 162, 11, @on
	exec sp_trace_setevent @TraceID, 162, 35, @on
	exec sp_trace_setevent @TraceID, 162, 51, @on
	exec sp_trace_setevent @TraceID, 162, 12, @on
	exec sp_trace_setevent @TraceID, 148, 11, @on
	exec sp_trace_setevent @TraceID, 148, 51, @on
	exec sp_trace_setevent @TraceID, 148, 12, @on
	exec sp_trace_setevent @TraceID, 148, 14, @on
	exec sp_trace_setevent @TraceID, 148, 26, @on
	exec sp_trace_setevent @TraceID, 148, 1, @on
	exec sp_trace_setevent @TraceID, 25, 7, @on
	exec sp_trace_setevent @TraceID, 25, 15, @on
	exec sp_trace_setevent @TraceID, 25, 8, @on
	exec sp_trace_setevent @TraceID, 25, 1, @on
	exec sp_trace_setevent @TraceID, 25, 9, @on
	exec sp_trace_setevent @TraceID, 25, 10, @on
	exec sp_trace_setevent @TraceID, 25, 26, @on
	exec sp_trace_setevent @TraceID, 25, 11, @on
	exec sp_trace_setevent @TraceID, 25, 35, @on
	exec sp_trace_setevent @TraceID, 25, 51, @on
	exec sp_trace_setevent @TraceID, 25, 12, @on
	exec sp_trace_setevent @TraceID, 25, 6, @on
	exec sp_trace_setevent @TraceID, 25, 14, @on
	exec sp_trace_setevent @TraceID, 59, 1, @on
	exec sp_trace_setevent @TraceID, 59, 14, @on
	exec sp_trace_setevent @TraceID, 59, 26, @on
	exec sp_trace_setevent @TraceID, 59, 35, @on
	exec sp_trace_setevent @TraceID, 59, 51, @on
	exec sp_trace_setevent @TraceID, 59, 12, @on
	exec sp_trace_setevent @TraceID, 60, 7, @on
	exec sp_trace_setevent @TraceID, 60, 8, @on
	exec sp_trace_setevent @TraceID, 60, 9, @on
	exec sp_trace_setevent @TraceID, 60, 6, @on
	exec sp_trace_setevent @TraceID, 60, 10, @on
	exec sp_trace_setevent @TraceID, 60, 14, @on
	exec sp_trace_setevent @TraceID, 60, 26, @on
	exec sp_trace_setevent @TraceID, 60, 11, @on
	exec sp_trace_setevent @TraceID, 60, 35, @on
	exec sp_trace_setevent @TraceID, 60, 51, @on
	exec sp_trace_setevent @TraceID, 60, 12, @on
	exec sp_trace_setevent @TraceID, 27, 7, @on
	exec sp_trace_setevent @TraceID, 27, 15, @on
	exec sp_trace_setevent @TraceID, 27, 8, @on
	exec sp_trace_setevent @TraceID, 27, 1, @on
	exec sp_trace_setevent @TraceID, 27, 9, @on
	exec sp_trace_setevent @TraceID, 27, 10, @on
	exec sp_trace_setevent @TraceID, 27, 26, @on
	exec sp_trace_setevent @TraceID, 27, 11, @on
	exec sp_trace_setevent @TraceID, 27, 35, @on
	exec sp_trace_setevent @TraceID, 27, 51, @on
	exec sp_trace_setevent @TraceID, 27, 12, @on
	exec sp_trace_setevent @TraceID, 27, 6, @on
	exec sp_trace_setevent @TraceID, 27, 14, @on
	exec sp_trace_setevent @TraceID, 61, 7, @on
	exec sp_trace_setevent @TraceID, 61, 31, @on
	exec sp_trace_setevent @TraceID, 61, 8, @on
	exec sp_trace_setevent @TraceID, 61, 1, @on
	exec sp_trace_setevent @TraceID, 61, 9, @on
	exec sp_trace_setevent @TraceID, 61, 6, @on
	exec sp_trace_setevent @TraceID, 61, 10, @on
	exec sp_trace_setevent @TraceID, 61, 14, @on
	exec sp_trace_setevent @TraceID, 61, 11, @on
	exec sp_trace_setevent @TraceID, 61, 35, @on
	exec sp_trace_setevent @TraceID, 61, 51, @on
	exec sp_trace_setevent @TraceID, 61, 12, @on
	exec sp_trace_setevent @TraceID, 14, 7, @on
	exec sp_trace_setevent @TraceID, 14, 8, @on
	exec sp_trace_setevent @TraceID, 14, 1, @on
	exec sp_trace_setevent @TraceID, 14, 9, @on
	exec sp_trace_setevent @TraceID, 14, 6, @on
	exec sp_trace_setevent @TraceID, 14, 10, @on
	exec sp_trace_setevent @TraceID, 14, 14, @on
	exec sp_trace_setevent @TraceID, 14, 26, @on
	exec sp_trace_setevent @TraceID, 14, 11, @on
	exec sp_trace_setevent @TraceID, 14, 35, @on
	exec sp_trace_setevent @TraceID, 14, 51, @on
	exec sp_trace_setevent @TraceID, 14, 12, @on
	exec sp_trace_setevent @TraceID, 15, 7, @on
	exec sp_trace_setevent @TraceID, 15, 15, @on
	exec sp_trace_setevent @TraceID, 15, 8, @on
	exec sp_trace_setevent @TraceID, 15, 9, @on
	exec sp_trace_setevent @TraceID, 15, 6, @on
	exec sp_trace_setevent @TraceID, 15, 10, @on
	exec sp_trace_setevent @TraceID, 15, 14, @on
	exec sp_trace_setevent @TraceID, 15, 26, @on
	exec sp_trace_setevent @TraceID, 15, 11, @on
	exec sp_trace_setevent @TraceID, 15, 35, @on
	exec sp_trace_setevent @TraceID, 15, 51, @on
	exec sp_trace_setevent @TraceID, 15, 12, @on
	exec sp_trace_setevent @TraceID, 17, 7, @on
	exec sp_trace_setevent @TraceID, 17, 8, @on
	exec sp_trace_setevent @TraceID, 17, 12, @on
	exec sp_trace_setevent @TraceID, 17, 1, @on
	exec sp_trace_setevent @TraceID, 17, 9, @on
	exec sp_trace_setevent @TraceID, 17, 6, @on
	exec sp_trace_setevent @TraceID, 17, 10, @on
	exec sp_trace_setevent @TraceID, 17, 14, @on
	exec sp_trace_setevent @TraceID, 17, 26, @on
	exec sp_trace_setevent @TraceID, 17, 11, @on
	exec sp_trace_setevent @TraceID, 17, 35, @on
	exec sp_trace_setevent @TraceID, 17, 51, @on
	exec sp_trace_setevent @TraceID, 100, 7, @on
	exec sp_trace_setevent @TraceID, 100, 8, @on
	exec sp_trace_setevent @TraceID, 100, 1, @on
	exec sp_trace_setevent @TraceID, 100, 9, @on
	exec sp_trace_setevent @TraceID, 100, 6, @on
	exec sp_trace_setevent @TraceID, 100, 10, @on
	exec sp_trace_setevent @TraceID, 100, 14, @on
	exec sp_trace_setevent @TraceID, 100, 26, @on
	exec sp_trace_setevent @TraceID, 100, 11, @on
	exec sp_trace_setevent @TraceID, 100, 35, @on
	exec sp_trace_setevent @TraceID, 100, 51, @on
	exec sp_trace_setevent @TraceID, 100, 12, @on
	exec sp_trace_setevent @TraceID, 10, 7, @on
	exec sp_trace_setevent @TraceID, 10, 15, @on
	exec sp_trace_setevent @TraceID, 10, 31, @on
	exec sp_trace_setevent @TraceID, 10, 8, @on
	exec sp_trace_setevent @TraceID, 10, 9, @on
	exec sp_trace_setevent @TraceID, 10, 10, @on
	exec sp_trace_setevent @TraceID, 10, 26, @on
	exec sp_trace_setevent @TraceID, 10, 11, @on
	exec sp_trace_setevent @TraceID, 10, 35, @on
	exec sp_trace_setevent @TraceID, 10, 51, @on
	exec sp_trace_setevent @TraceID, 10, 12, @on
	exec sp_trace_setevent @TraceID, 10, 6, @on
	exec sp_trace_setevent @TraceID, 10, 14, @on
	exec sp_trace_setevent @TraceID, 11, 7, @on
	exec sp_trace_setevent @TraceID, 11, 8, @on
	exec sp_trace_setevent @TraceID, 11, 9, @on
	exec sp_trace_setevent @TraceID, 11, 6, @on
	exec sp_trace_setevent @TraceID, 11, 10, @on
	exec sp_trace_setevent @TraceID, 11, 14, @on
	exec sp_trace_setevent @TraceID, 11, 26, @on
	exec sp_trace_setevent @TraceID, 11, 11, @on
	exec sp_trace_setevent @TraceID, 11, 35, @on
	exec sp_trace_setevent @TraceID, 11, 51, @on
	exec sp_trace_setevent @TraceID, 11, 12, @on
	exec sp_trace_setevent @TraceID, 42, 7, @on
	exec sp_trace_setevent @TraceID, 42, 8, @on
	exec sp_trace_setevent @TraceID, 42, 1, @on
	exec sp_trace_setevent @TraceID, 42, 9, @on
	exec sp_trace_setevent @TraceID, 42, 6, @on
	exec sp_trace_setevent @TraceID, 42, 10, @on
	exec sp_trace_setevent @TraceID, 42, 14, @on
	exec sp_trace_setevent @TraceID, 42, 26, @on
	exec sp_trace_setevent @TraceID, 42, 11, @on
	exec sp_trace_setevent @TraceID, 42, 35, @on
	exec sp_trace_setevent @TraceID, 42, 51, @on
	exec sp_trace_setevent @TraceID, 42, 12, @on
	exec sp_trace_setevent @TraceID, 42, 5, @on
	exec sp_trace_setevent @TraceID, 45, 7, @on
	exec sp_trace_setevent @TraceID, 45, 8, @on
	exec sp_trace_setevent @TraceID, 45, 1, @on
	exec sp_trace_setevent @TraceID, 45, 9, @on
	exec sp_trace_setevent @TraceID, 45, 10, @on
	exec sp_trace_setevent @TraceID, 45, 26, @on
	exec sp_trace_setevent @TraceID, 45, 11, @on
	exec sp_trace_setevent @TraceID, 45, 35, @on
	exec sp_trace_setevent @TraceID, 45, 51, @on
	exec sp_trace_setevent @TraceID, 45, 12, @on
	exec sp_trace_setevent @TraceID, 45, 5, @on
	exec sp_trace_setevent @TraceID, 45, 6, @on
	exec sp_trace_setevent @TraceID, 45, 14, @on
	exec sp_trace_setevent @TraceID, 45, 15, @on
	exec sp_trace_setevent @TraceID, 72, 7, @on
	exec sp_trace_setevent @TraceID, 72, 8, @on
	exec sp_trace_setevent @TraceID, 72, 12, @on
	exec sp_trace_setevent @TraceID, 72, 9, @on
	exec sp_trace_setevent @TraceID, 72, 6, @on
	exec sp_trace_setevent @TraceID, 72, 10, @on
	exec sp_trace_setevent @TraceID, 72, 14, @on
	exec sp_trace_setevent @TraceID, 72, 26, @on
	exec sp_trace_setevent @TraceID, 72, 11, @on
	exec sp_trace_setevent @TraceID, 72, 35, @on
	exec sp_trace_setevent @TraceID, 72, 51, @on
	exec sp_trace_setevent @TraceID, 71, 7, @on
	exec sp_trace_setevent @TraceID, 71, 8, @on
	exec sp_trace_setevent @TraceID, 71, 12, @on
	exec sp_trace_setevent @TraceID, 71, 9, @on
	exec sp_trace_setevent @TraceID, 71, 6, @on
	exec sp_trace_setevent @TraceID, 71, 10, @on
	exec sp_trace_setevent @TraceID, 71, 14, @on
	exec sp_trace_setevent @TraceID, 71, 26, @on
	exec sp_trace_setevent @TraceID, 71, 11, @on
	exec sp_trace_setevent @TraceID, 71, 35, @on
	exec sp_trace_setevent @TraceID, 71, 51, @on
	exec sp_trace_setevent @TraceID, 12, 7, @on
	exec sp_trace_setevent @TraceID, 12, 15, @on
	exec sp_trace_setevent @TraceID, 12, 31, @on
	exec sp_trace_setevent @TraceID, 12, 8, @on
	exec sp_trace_setevent @TraceID, 12, 1, @on
	exec sp_trace_setevent @TraceID, 12, 9, @on
	exec sp_trace_setevent @TraceID, 12, 6, @on
	exec sp_trace_setevent @TraceID, 12, 10, @on
	exec sp_trace_setevent @TraceID, 12, 14, @on
	exec sp_trace_setevent @TraceID, 12, 26, @on
	exec sp_trace_setevent @TraceID, 12, 11, @on
	exec sp_trace_setevent @TraceID, 12, 35, @on
	exec sp_trace_setevent @TraceID, 12, 51, @on
	exec sp_trace_setevent @TraceID, 12, 12, @on
	exec sp_trace_setevent @TraceID, 13, 7, @on
	exec sp_trace_setevent @TraceID, 13, 8, @on
	exec sp_trace_setevent @TraceID, 13, 12, @on
	exec sp_trace_setevent @TraceID, 13, 1, @on
	exec sp_trace_setevent @TraceID, 13, 9, @on
	exec sp_trace_setevent @TraceID, 13, 6, @on
	exec sp_trace_setevent @TraceID, 13, 10, @on
	exec sp_trace_setevent @TraceID, 13, 14, @on
	exec sp_trace_setevent @TraceID, 13, 26, @on
	exec sp_trace_setevent @TraceID, 13, 11, @on
	exec sp_trace_setevent @TraceID, 13, 35, @on
	exec sp_trace_setevent @TraceID, 13, 51, @on

	-- Set the Filters
	declare @intfilter int
	declare @bigintfilter bigint

	exec sp_trace_setfilter @TraceID, 10, 0, 7, N'SQL Server Profiler - 89ebc6ab-5e02-4b96-a29f-5ec754c3b303'
	-- Set the trace status to start
	exec sp_trace_setstatus @TraceID, 1

	-- display trace id for future references
	--select TraceID=@TraceID
	goto finish

	finish:
go

---------------------------------------------------------------------------------

if object_id('StartPerformanceTrace') is not null begin
	print 'removing proc StartPerformanceTrace';
	drop procedure StartPerformanceTrace;
end
go
print 'creating procedure StartPerformanceTrace';
go

CREATE PROCEDURE StartPerformanceTrace
	@TraceFile NVARCHAR(256) = N'C:\FwPerformance'
AS
	DECLARE @TraceId INT
	EXEC PrepStartTrace @TraceFile, @TraceId OUTPUT


	/****************************************************/
	/* Created by: SQL Server Profiler 2005             */
	/* Date: 08/07/2008  06:44:28 PM         */
	/****************************************************/

	-- Client side File and Table cannot be scripted

	-- Set the events
	declare @on bit
	set @on = 1
	exec sp_trace_setevent @TraceID, 78, 7, @on
	exec sp_trace_setevent @TraceID, 78, 8, @on
	exec sp_trace_setevent @TraceID, 78, 12, @on
	exec sp_trace_setevent @TraceID, 78, 60, @on
	exec sp_trace_setevent @TraceID, 78, 9, @on
	exec sp_trace_setevent @TraceID, 78, 6, @on
	exec sp_trace_setevent @TraceID, 78, 10, @on
	exec sp_trace_setevent @TraceID, 78, 14, @on
	exec sp_trace_setevent @TraceID, 78, 26, @on
	exec sp_trace_setevent @TraceID, 78, 3, @on
	exec sp_trace_setevent @TraceID, 78, 11, @on
	exec sp_trace_setevent @TraceID, 78, 35, @on
	exec sp_trace_setevent @TraceID, 78, 51, @on
	exec sp_trace_setevent @TraceID, 74, 7, @on
	exec sp_trace_setevent @TraceID, 74, 8, @on
	exec sp_trace_setevent @TraceID, 74, 9, @on
	exec sp_trace_setevent @TraceID, 74, 6, @on
	exec sp_trace_setevent @TraceID, 74, 10, @on
	exec sp_trace_setevent @TraceID, 74, 14, @on
	exec sp_trace_setevent @TraceID, 74, 26, @on
	exec sp_trace_setevent @TraceID, 74, 3, @on
	exec sp_trace_setevent @TraceID, 74, 11, @on
	exec sp_trace_setevent @TraceID, 74, 35, @on
	exec sp_trace_setevent @TraceID, 74, 51, @on
	exec sp_trace_setevent @TraceID, 74, 12, @on
	exec sp_trace_setevent @TraceID, 74, 60, @on
	exec sp_trace_setevent @TraceID, 53, 7, @on
	exec sp_trace_setevent @TraceID, 53, 8, @on
	exec sp_trace_setevent @TraceID, 53, 9, @on
	exec sp_trace_setevent @TraceID, 53, 6, @on
	exec sp_trace_setevent @TraceID, 53, 10, @on
	exec sp_trace_setevent @TraceID, 53, 14, @on
	exec sp_trace_setevent @TraceID, 53, 26, @on
	exec sp_trace_setevent @TraceID, 53, 3, @on
	exec sp_trace_setevent @TraceID, 53, 11, @on
	exec sp_trace_setevent @TraceID, 53, 35, @on
	exec sp_trace_setevent @TraceID, 53, 51, @on
	exec sp_trace_setevent @TraceID, 53, 12, @on
	exec sp_trace_setevent @TraceID, 53, 60, @on
	exec sp_trace_setevent @TraceID, 70, 7, @on
	exec sp_trace_setevent @TraceID, 70, 8, @on
	exec sp_trace_setevent @TraceID, 70, 12, @on
	exec sp_trace_setevent @TraceID, 70, 60, @on
	exec sp_trace_setevent @TraceID, 70, 9, @on
	exec sp_trace_setevent @TraceID, 70, 6, @on
	exec sp_trace_setevent @TraceID, 70, 10, @on
	exec sp_trace_setevent @TraceID, 70, 14, @on
	exec sp_trace_setevent @TraceID, 70, 26, @on
	exec sp_trace_setevent @TraceID, 70, 3, @on
	exec sp_trace_setevent @TraceID, 70, 11, @on
	exec sp_trace_setevent @TraceID, 70, 35, @on
	exec sp_trace_setevent @TraceID, 70, 51, @on
	exec sp_trace_setevent @TraceID, 75, 7, @on
	exec sp_trace_setevent @TraceID, 75, 8, @on
	exec sp_trace_setevent @TraceID, 75, 12, @on
	exec sp_trace_setevent @TraceID, 75, 60, @on
	exec sp_trace_setevent @TraceID, 75, 9, @on
	exec sp_trace_setevent @TraceID, 75, 6, @on
	exec sp_trace_setevent @TraceID, 75, 10, @on
	exec sp_trace_setevent @TraceID, 75, 14, @on
	exec sp_trace_setevent @TraceID, 75, 26, @on
	exec sp_trace_setevent @TraceID, 75, 3, @on
	exec sp_trace_setevent @TraceID, 75, 11, @on
	exec sp_trace_setevent @TraceID, 75, 35, @on
	exec sp_trace_setevent @TraceID, 75, 51, @on
	exec sp_trace_setevent @TraceID, 77, 7, @on
	exec sp_trace_setevent @TraceID, 77, 8, @on
	exec sp_trace_setevent @TraceID, 77, 12, @on
	exec sp_trace_setevent @TraceID, 77, 60, @on
	exec sp_trace_setevent @TraceID, 77, 9, @on
	exec sp_trace_setevent @TraceID, 77, 6, @on
	exec sp_trace_setevent @TraceID, 77, 10, @on
	exec sp_trace_setevent @TraceID, 77, 14, @on
	exec sp_trace_setevent @TraceID, 77, 26, @on
	exec sp_trace_setevent @TraceID, 77, 3, @on
	exec sp_trace_setevent @TraceID, 77, 11, @on
	exec sp_trace_setevent @TraceID, 77, 35, @on
	exec sp_trace_setevent @TraceID, 77, 51, @on
	exec sp_trace_setevent @TraceID, 25, 7, @on
	exec sp_trace_setevent @TraceID, 25, 15, @on
	exec sp_trace_setevent @TraceID, 25, 8, @on
	exec sp_trace_setevent @TraceID, 25, 1, @on
	exec sp_trace_setevent @TraceID, 25, 9, @on
	exec sp_trace_setevent @TraceID, 25, 2, @on
	exec sp_trace_setevent @TraceID, 25, 10, @on
	exec sp_trace_setevent @TraceID, 25, 26, @on
	exec sp_trace_setevent @TraceID, 25, 3, @on
	exec sp_trace_setevent @TraceID, 25, 11, @on
	exec sp_trace_setevent @TraceID, 25, 35, @on
	exec sp_trace_setevent @TraceID, 25, 51, @on
	exec sp_trace_setevent @TraceID, 25, 12, @on
	exec sp_trace_setevent @TraceID, 25, 60, @on
	exec sp_trace_setevent @TraceID, 25, 13, @on
	exec sp_trace_setevent @TraceID, 25, 6, @on
	exec sp_trace_setevent @TraceID, 25, 14, @on
	exec sp_trace_setevent @TraceID, 60, 7, @on
	exec sp_trace_setevent @TraceID, 60, 8, @on
	exec sp_trace_setevent @TraceID, 60, 9, @on
	exec sp_trace_setevent @TraceID, 60, 6, @on
	exec sp_trace_setevent @TraceID, 60, 10, @on
	exec sp_trace_setevent @TraceID, 60, 14, @on
	exec sp_trace_setevent @TraceID, 60, 26, @on
	exec sp_trace_setevent @TraceID, 60, 3, @on
	exec sp_trace_setevent @TraceID, 60, 11, @on
	exec sp_trace_setevent @TraceID, 60, 35, @on
	exec sp_trace_setevent @TraceID, 60, 51, @on
	exec sp_trace_setevent @TraceID, 60, 12, @on
	exec sp_trace_setevent @TraceID, 60, 60, @on
	exec sp_trace_setevent @TraceID, 27, 7, @on
	exec sp_trace_setevent @TraceID, 27, 15, @on
	exec sp_trace_setevent @TraceID, 27, 8, @on
	exec sp_trace_setevent @TraceID, 27, 1, @on
	exec sp_trace_setevent @TraceID, 27, 9, @on
	exec sp_trace_setevent @TraceID, 27, 2, @on
	exec sp_trace_setevent @TraceID, 27, 10, @on
	exec sp_trace_setevent @TraceID, 27, 26, @on
	exec sp_trace_setevent @TraceID, 27, 3, @on
	exec sp_trace_setevent @TraceID, 27, 11, @on
	exec sp_trace_setevent @TraceID, 27, 35, @on
	exec sp_trace_setevent @TraceID, 27, 51, @on
	exec sp_trace_setevent @TraceID, 27, 12, @on
	exec sp_trace_setevent @TraceID, 27, 60, @on
	exec sp_trace_setevent @TraceID, 27, 13, @on
	exec sp_trace_setevent @TraceID, 27, 6, @on
	exec sp_trace_setevent @TraceID, 27, 14, @on
	exec sp_trace_setevent @TraceID, 119, 7, @on
	exec sp_trace_setevent @TraceID, 119, 15, @on
	exec sp_trace_setevent @TraceID, 119, 31, @on
	exec sp_trace_setevent @TraceID, 119, 8, @on
	exec sp_trace_setevent @TraceID, 119, 1, @on
	exec sp_trace_setevent @TraceID, 119, 9, @on
	exec sp_trace_setevent @TraceID, 119, 6, @on
	exec sp_trace_setevent @TraceID, 119, 10, @on
	exec sp_trace_setevent @TraceID, 119, 14, @on
	exec sp_trace_setevent @TraceID, 119, 3, @on
	exec sp_trace_setevent @TraceID, 119, 11, @on
	exec sp_trace_setevent @TraceID, 119, 35, @on
	exec sp_trace_setevent @TraceID, 119, 51, @on
	exec sp_trace_setevent @TraceID, 119, 12, @on
	exec sp_trace_setevent @TraceID, 119, 60, @on
	exec sp_trace_setevent @TraceID, 119, 13, @on
	exec sp_trace_setevent @TraceID, 121, 7, @on
	exec sp_trace_setevent @TraceID, 121, 15, @on
	exec sp_trace_setevent @TraceID, 121, 31, @on
	exec sp_trace_setevent @TraceID, 121, 8, @on
	exec sp_trace_setevent @TraceID, 121, 1, @on
	exec sp_trace_setevent @TraceID, 121, 9, @on
	exec sp_trace_setevent @TraceID, 121, 6, @on
	exec sp_trace_setevent @TraceID, 121, 10, @on
	exec sp_trace_setevent @TraceID, 121, 14, @on
	exec sp_trace_setevent @TraceID, 121, 3, @on
	exec sp_trace_setevent @TraceID, 121, 11, @on
	exec sp_trace_setevent @TraceID, 121, 35, @on
	exec sp_trace_setevent @TraceID, 121, 51, @on
	exec sp_trace_setevent @TraceID, 121, 12, @on
	exec sp_trace_setevent @TraceID, 121, 60, @on
	exec sp_trace_setevent @TraceID, 121, 13, @on
	exec sp_trace_setevent @TraceID, 120, 7, @on
	exec sp_trace_setevent @TraceID, 120, 15, @on
	exec sp_trace_setevent @TraceID, 120, 31, @on
	exec sp_trace_setevent @TraceID, 120, 8, @on
	exec sp_trace_setevent @TraceID, 120, 1, @on
	exec sp_trace_setevent @TraceID, 120, 9, @on
	exec sp_trace_setevent @TraceID, 120, 6, @on
	exec sp_trace_setevent @TraceID, 120, 10, @on
	exec sp_trace_setevent @TraceID, 120, 14, @on
	exec sp_trace_setevent @TraceID, 120, 3, @on
	exec sp_trace_setevent @TraceID, 120, 11, @on
	exec sp_trace_setevent @TraceID, 120, 35, @on
	exec sp_trace_setevent @TraceID, 120, 51, @on
	exec sp_trace_setevent @TraceID, 120, 12, @on
	exec sp_trace_setevent @TraceID, 120, 60, @on
	exec sp_trace_setevent @TraceID, 120, 13, @on
	exec sp_trace_setevent @TraceID, 58, 7, @on
	exec sp_trace_setevent @TraceID, 58, 15, @on
	exec sp_trace_setevent @TraceID, 58, 31, @on
	exec sp_trace_setevent @TraceID, 58, 8, @on
	exec sp_trace_setevent @TraceID, 58, 1, @on
	exec sp_trace_setevent @TraceID, 58, 9, @on
	exec sp_trace_setevent @TraceID, 58, 10, @on
	exec sp_trace_setevent @TraceID, 58, 26, @on
	exec sp_trace_setevent @TraceID, 58, 3, @on
	exec sp_trace_setevent @TraceID, 58, 11, @on
	exec sp_trace_setevent @TraceID, 58, 35, @on
	exec sp_trace_setevent @TraceID, 58, 51, @on
	exec sp_trace_setevent @TraceID, 58, 12, @on
	exec sp_trace_setevent @TraceID, 58, 60, @on
	exec sp_trace_setevent @TraceID, 58, 13, @on
	exec sp_trace_setevent @TraceID, 58, 6, @on
	exec sp_trace_setevent @TraceID, 58, 14, @on
	exec sp_trace_setevent @TraceID, 165, 12, @on
	exec sp_trace_setevent @TraceID, 165, 1, @on
	exec sp_trace_setevent @TraceID, 165, 2, @on
	exec sp_trace_setevent @TraceID, 165, 14, @on
	exec sp_trace_setevent @TraceID, 165, 3, @on
	exec sp_trace_setevent @TraceID, 165, 51, @on
	exec sp_trace_setevent @TraceID, 97, 7, @on
	exec sp_trace_setevent @TraceID, 97, 8, @on
	exec sp_trace_setevent @TraceID, 97, 9, @on
	exec sp_trace_setevent @TraceID, 97, 2, @on
	exec sp_trace_setevent @TraceID, 97, 10, @on
	exec sp_trace_setevent @TraceID, 97, 14, @on
	exec sp_trace_setevent @TraceID, 97, 26, @on
	exec sp_trace_setevent @TraceID, 97, 3, @on
	exec sp_trace_setevent @TraceID, 97, 11, @on
	exec sp_trace_setevent @TraceID, 97, 35, @on
	exec sp_trace_setevent @TraceID, 97, 51, @on
	exec sp_trace_setevent @TraceID, 97, 12, @on
	exec sp_trace_setevent @TraceID, 97, 60, @on
	exec sp_trace_setevent @TraceID, 98, 7, @on
	exec sp_trace_setevent @TraceID, 98, 8, @on
	exec sp_trace_setevent @TraceID, 98, 9, @on
	exec sp_trace_setevent @TraceID, 98, 2, @on
	exec sp_trace_setevent @TraceID, 98, 10, @on
	exec sp_trace_setevent @TraceID, 98, 14, @on
	exec sp_trace_setevent @TraceID, 98, 26, @on
	exec sp_trace_setevent @TraceID, 98, 3, @on
	exec sp_trace_setevent @TraceID, 98, 11, @on
	exec sp_trace_setevent @TraceID, 98, 35, @on
	exec sp_trace_setevent @TraceID, 98, 51, @on
	exec sp_trace_setevent @TraceID, 98, 12, @on
	exec sp_trace_setevent @TraceID, 98, 60, @on
	exec sp_trace_setevent @TraceID, 122, 7, @on
	exec sp_trace_setevent @TraceID, 122, 8, @on
	exec sp_trace_setevent @TraceID, 122, 1, @on
	exec sp_trace_setevent @TraceID, 122, 9, @on
	exec sp_trace_setevent @TraceID, 122, 2, @on
	exec sp_trace_setevent @TraceID, 122, 10, @on
	exec sp_trace_setevent @TraceID, 122, 14, @on
	exec sp_trace_setevent @TraceID, 122, 26, @on
	exec sp_trace_setevent @TraceID, 122, 3, @on
	exec sp_trace_setevent @TraceID, 122, 11, @on
	exec sp_trace_setevent @TraceID, 122, 35, @on
	exec sp_trace_setevent @TraceID, 122, 51, @on
	exec sp_trace_setevent @TraceID, 122, 12, @on
	exec sp_trace_setevent @TraceID, 122, 60, @on
	exec sp_trace_setevent @TraceID, 168, 7, @on
	exec sp_trace_setevent @TraceID, 168, 8, @on
	exec sp_trace_setevent @TraceID, 168, 1, @on
	exec sp_trace_setevent @TraceID, 168, 9, @on
	exec sp_trace_setevent @TraceID, 168, 2, @on
	exec sp_trace_setevent @TraceID, 168, 6, @on
	exec sp_trace_setevent @TraceID, 168, 10, @on
	exec sp_trace_setevent @TraceID, 168, 14, @on
	exec sp_trace_setevent @TraceID, 168, 26, @on
	exec sp_trace_setevent @TraceID, 168, 3, @on
	exec sp_trace_setevent @TraceID, 168, 11, @on
	exec sp_trace_setevent @TraceID, 168, 35, @on
	exec sp_trace_setevent @TraceID, 168, 51, @on
	exec sp_trace_setevent @TraceID, 168, 12, @on
	exec sp_trace_setevent @TraceID, 168, 60, @on
	exec sp_trace_setevent @TraceID, 146, 7, @on
	exec sp_trace_setevent @TraceID, 146, 8, @on
	exec sp_trace_setevent @TraceID, 146, 1, @on
	exec sp_trace_setevent @TraceID, 146, 9, @on
	exec sp_trace_setevent @TraceID, 146, 2, @on
	exec sp_trace_setevent @TraceID, 146, 10, @on
	exec sp_trace_setevent @TraceID, 146, 14, @on
	exec sp_trace_setevent @TraceID, 146, 26, @on
	exec sp_trace_setevent @TraceID, 146, 3, @on
	exec sp_trace_setevent @TraceID, 146, 11, @on
	exec sp_trace_setevent @TraceID, 146, 35, @on
	exec sp_trace_setevent @TraceID, 146, 51, @on
	exec sp_trace_setevent @TraceID, 146, 12, @on
	exec sp_trace_setevent @TraceID, 146, 60, @on
	exec sp_trace_setevent @TraceID, 190, 7, @on
	exec sp_trace_setevent @TraceID, 190, 15, @on
	exec sp_trace_setevent @TraceID, 190, 8, @on
	exec sp_trace_setevent @TraceID, 190, 9, @on
	exec sp_trace_setevent @TraceID, 190, 6, @on
	exec sp_trace_setevent @TraceID, 190, 10, @on
	exec sp_trace_setevent @TraceID, 190, 14, @on
	exec sp_trace_setevent @TraceID, 190, 26, @on
	exec sp_trace_setevent @TraceID, 190, 3, @on
	exec sp_trace_setevent @TraceID, 190, 11, @on
	exec sp_trace_setevent @TraceID, 190, 35, @on
	exec sp_trace_setevent @TraceID, 190, 51, @on
	exec sp_trace_setevent @TraceID, 190, 12, @on
	exec sp_trace_setevent @TraceID, 190, 60, @on
	exec sp_trace_setevent @TraceID, 190, 13, @on
	exec sp_trace_setevent @TraceID, 14, 7, @on
	exec sp_trace_setevent @TraceID, 14, 8, @on
	exec sp_trace_setevent @TraceID, 14, 1, @on
	exec sp_trace_setevent @TraceID, 14, 9, @on
	exec sp_trace_setevent @TraceID, 14, 2, @on
	exec sp_trace_setevent @TraceID, 14, 6, @on
	exec sp_trace_setevent @TraceID, 14, 10, @on
	exec sp_trace_setevent @TraceID, 14, 14, @on
	exec sp_trace_setevent @TraceID, 14, 26, @on
	exec sp_trace_setevent @TraceID, 14, 3, @on
	exec sp_trace_setevent @TraceID, 14, 11, @on
	exec sp_trace_setevent @TraceID, 14, 35, @on
	exec sp_trace_setevent @TraceID, 14, 51, @on
	exec sp_trace_setevent @TraceID, 14, 12, @on
	exec sp_trace_setevent @TraceID, 14, 60, @on
	exec sp_trace_setevent @TraceID, 15, 7, @on
	exec sp_trace_setevent @TraceID, 15, 15, @on
	exec sp_trace_setevent @TraceID, 15, 8, @on
	exec sp_trace_setevent @TraceID, 15, 9, @on
	exec sp_trace_setevent @TraceID, 15, 6, @on
	exec sp_trace_setevent @TraceID, 15, 10, @on
	exec sp_trace_setevent @TraceID, 15, 14, @on
	exec sp_trace_setevent @TraceID, 15, 26, @on
	exec sp_trace_setevent @TraceID, 15, 3, @on
	exec sp_trace_setevent @TraceID, 15, 11, @on
	exec sp_trace_setevent @TraceID, 15, 35, @on
	exec sp_trace_setevent @TraceID, 15, 51, @on
	exec sp_trace_setevent @TraceID, 15, 12, @on
	exec sp_trace_setevent @TraceID, 15, 60, @on
	exec sp_trace_setevent @TraceID, 17, 7, @on
	exec sp_trace_setevent @TraceID, 17, 8, @on
	exec sp_trace_setevent @TraceID, 17, 12, @on
	exec sp_trace_setevent @TraceID, 17, 60, @on
	exec sp_trace_setevent @TraceID, 17, 1, @on
	exec sp_trace_setevent @TraceID, 17, 9, @on
	exec sp_trace_setevent @TraceID, 17, 2, @on
	exec sp_trace_setevent @TraceID, 17, 6, @on
	exec sp_trace_setevent @TraceID, 17, 10, @on
	exec sp_trace_setevent @TraceID, 17, 14, @on
	exec sp_trace_setevent @TraceID, 17, 26, @on
	exec sp_trace_setevent @TraceID, 17, 3, @on
	exec sp_trace_setevent @TraceID, 17, 11, @on
	exec sp_trace_setevent @TraceID, 17, 35, @on
	exec sp_trace_setevent @TraceID, 17, 51, @on
	exec sp_trace_setevent @TraceID, 100, 7, @on
	exec sp_trace_setevent @TraceID, 100, 8, @on
	exec sp_trace_setevent @TraceID, 100, 1, @on
	exec sp_trace_setevent @TraceID, 100, 9, @on
	exec sp_trace_setevent @TraceID, 100, 6, @on
	exec sp_trace_setevent @TraceID, 100, 10, @on
	exec sp_trace_setevent @TraceID, 100, 14, @on
	exec sp_trace_setevent @TraceID, 100, 26, @on
	exec sp_trace_setevent @TraceID, 100, 3, @on
	exec sp_trace_setevent @TraceID, 100, 11, @on
	exec sp_trace_setevent @TraceID, 100, 35, @on
	exec sp_trace_setevent @TraceID, 100, 51, @on
	exec sp_trace_setevent @TraceID, 100, 12, @on
	exec sp_trace_setevent @TraceID, 100, 60, @on
	exec sp_trace_setevent @TraceID, 10, 7, @on
	exec sp_trace_setevent @TraceID, 10, 15, @on
	exec sp_trace_setevent @TraceID, 10, 31, @on
	exec sp_trace_setevent @TraceID, 10, 8, @on
	exec sp_trace_setevent @TraceID, 10, 9, @on
	exec sp_trace_setevent @TraceID, 10, 2, @on
	exec sp_trace_setevent @TraceID, 10, 10, @on
	exec sp_trace_setevent @TraceID, 10, 18, @on
	exec sp_trace_setevent @TraceID, 10, 26, @on
	exec sp_trace_setevent @TraceID, 10, 3, @on
	exec sp_trace_setevent @TraceID, 10, 11, @on
	exec sp_trace_setevent @TraceID, 10, 35, @on
	exec sp_trace_setevent @TraceID, 10, 51, @on
	exec sp_trace_setevent @TraceID, 10, 12, @on
	exec sp_trace_setevent @TraceID, 10, 60, @on
	exec sp_trace_setevent @TraceID, 10, 13, @on
	exec sp_trace_setevent @TraceID, 10, 6, @on
	exec sp_trace_setevent @TraceID, 10, 14, @on
	exec sp_trace_setevent @TraceID, 11, 7, @on
	exec sp_trace_setevent @TraceID, 11, 8, @on
	exec sp_trace_setevent @TraceID, 11, 9, @on
	exec sp_trace_setevent @TraceID, 11, 2, @on
	exec sp_trace_setevent @TraceID, 11, 6, @on
	exec sp_trace_setevent @TraceID, 11, 10, @on
	exec sp_trace_setevent @TraceID, 11, 14, @on
	exec sp_trace_setevent @TraceID, 11, 26, @on
	exec sp_trace_setevent @TraceID, 11, 3, @on
	exec sp_trace_setevent @TraceID, 11, 11, @on
	exec sp_trace_setevent @TraceID, 11, 35, @on
	exec sp_trace_setevent @TraceID, 11, 51, @on
	exec sp_trace_setevent @TraceID, 11, 12, @on
	exec sp_trace_setevent @TraceID, 11, 60, @on
	exec sp_trace_setevent @TraceID, 38, 7, @on
	exec sp_trace_setevent @TraceID, 38, 8, @on
	exec sp_trace_setevent @TraceID, 38, 1, @on
	exec sp_trace_setevent @TraceID, 38, 9, @on
	exec sp_trace_setevent @TraceID, 38, 6, @on
	exec sp_trace_setevent @TraceID, 38, 10, @on
	exec sp_trace_setevent @TraceID, 38, 14, @on
	exec sp_trace_setevent @TraceID, 38, 26, @on
	exec sp_trace_setevent @TraceID, 38, 3, @on
	exec sp_trace_setevent @TraceID, 38, 11, @on
	exec sp_trace_setevent @TraceID, 38, 35, @on
	exec sp_trace_setevent @TraceID, 38, 51, @on
	exec sp_trace_setevent @TraceID, 38, 12, @on
	exec sp_trace_setevent @TraceID, 38, 60, @on
	exec sp_trace_setevent @TraceID, 35, 7, @on
	exec sp_trace_setevent @TraceID, 35, 8, @on
	exec sp_trace_setevent @TraceID, 35, 1, @on
	exec sp_trace_setevent @TraceID, 35, 9, @on
	exec sp_trace_setevent @TraceID, 35, 6, @on
	exec sp_trace_setevent @TraceID, 35, 10, @on
	exec sp_trace_setevent @TraceID, 35, 14, @on
	exec sp_trace_setevent @TraceID, 35, 26, @on
	exec sp_trace_setevent @TraceID, 35, 3, @on
	exec sp_trace_setevent @TraceID, 35, 11, @on
	exec sp_trace_setevent @TraceID, 35, 35, @on
	exec sp_trace_setevent @TraceID, 35, 51, @on
	exec sp_trace_setevent @TraceID, 35, 12, @on
	exec sp_trace_setevent @TraceID, 35, 60, @on
	exec sp_trace_setevent @TraceID, 34, 7, @on
	exec sp_trace_setevent @TraceID, 34, 8, @on
	exec sp_trace_setevent @TraceID, 34, 1, @on
	exec sp_trace_setevent @TraceID, 34, 9, @on
	exec sp_trace_setevent @TraceID, 34, 6, @on
	exec sp_trace_setevent @TraceID, 34, 10, @on
	exec sp_trace_setevent @TraceID, 34, 14, @on
	exec sp_trace_setevent @TraceID, 34, 26, @on
	exec sp_trace_setevent @TraceID, 34, 3, @on
	exec sp_trace_setevent @TraceID, 34, 11, @on
	exec sp_trace_setevent @TraceID, 34, 51, @on
	exec sp_trace_setevent @TraceID, 34, 12, @on
	exec sp_trace_setevent @TraceID, 34, 60, @on
	exec sp_trace_setevent @TraceID, 36, 7, @on
	exec sp_trace_setevent @TraceID, 36, 8, @on
	exec sp_trace_setevent @TraceID, 36, 1, @on
	exec sp_trace_setevent @TraceID, 36, 9, @on
	exec sp_trace_setevent @TraceID, 36, 6, @on
	exec sp_trace_setevent @TraceID, 36, 10, @on
	exec sp_trace_setevent @TraceID, 36, 14, @on
	exec sp_trace_setevent @TraceID, 36, 26, @on
	exec sp_trace_setevent @TraceID, 36, 3, @on
	exec sp_trace_setevent @TraceID, 36, 11, @on
	exec sp_trace_setevent @TraceID, 36, 35, @on
	exec sp_trace_setevent @TraceID, 36, 51, @on
	exec sp_trace_setevent @TraceID, 36, 12, @on
	exec sp_trace_setevent @TraceID, 36, 60, @on
	exec sp_trace_setevent @TraceID, 43, 7, @on
	exec sp_trace_setevent @TraceID, 43, 15, @on
	exec sp_trace_setevent @TraceID, 43, 8, @on
	exec sp_trace_setevent @TraceID, 43, 1, @on
	exec sp_trace_setevent @TraceID, 43, 9, @on
	exec sp_trace_setevent @TraceID, 43, 10, @on
	exec sp_trace_setevent @TraceID, 43, 26, @on
	exec sp_trace_setevent @TraceID, 43, 3, @on
	exec sp_trace_setevent @TraceID, 43, 11, @on
	exec sp_trace_setevent @TraceID, 43, 35, @on
	exec sp_trace_setevent @TraceID, 43, 51, @on
	exec sp_trace_setevent @TraceID, 43, 12, @on
	exec sp_trace_setevent @TraceID, 43, 60, @on
	exec sp_trace_setevent @TraceID, 43, 13, @on
	exec sp_trace_setevent @TraceID, 43, 6, @on
	exec sp_trace_setevent @TraceID, 43, 14, @on
	exec sp_trace_setevent @TraceID, 37, 7, @on
	exec sp_trace_setevent @TraceID, 37, 8, @on
	exec sp_trace_setevent @TraceID, 37, 1, @on
	exec sp_trace_setevent @TraceID, 37, 9, @on
	exec sp_trace_setevent @TraceID, 37, 10, @on
	exec sp_trace_setevent @TraceID, 37, 26, @on
	exec sp_trace_setevent @TraceID, 37, 3, @on
	exec sp_trace_setevent @TraceID, 37, 11, @on
	exec sp_trace_setevent @TraceID, 37, 35, @on
	exec sp_trace_setevent @TraceID, 37, 51, @on
	exec sp_trace_setevent @TraceID, 37, 12, @on
	exec sp_trace_setevent @TraceID, 37, 60, @on
	exec sp_trace_setevent @TraceID, 37, 6, @on
	exec sp_trace_setevent @TraceID, 37, 14, @on
	exec sp_trace_setevent @TraceID, 72, 7, @on
	exec sp_trace_setevent @TraceID, 72, 8, @on
	exec sp_trace_setevent @TraceID, 72, 12, @on
	exec sp_trace_setevent @TraceID, 72, 60, @on
	exec sp_trace_setevent @TraceID, 72, 9, @on
	exec sp_trace_setevent @TraceID, 72, 6, @on
	exec sp_trace_setevent @TraceID, 72, 10, @on
	exec sp_trace_setevent @TraceID, 72, 14, @on
	exec sp_trace_setevent @TraceID, 72, 26, @on
	exec sp_trace_setevent @TraceID, 72, 3, @on
	exec sp_trace_setevent @TraceID, 72, 11, @on
	exec sp_trace_setevent @TraceID, 72, 35, @on
	exec sp_trace_setevent @TraceID, 72, 51, @on
	exec sp_trace_setevent @TraceID, 71, 7, @on
	exec sp_trace_setevent @TraceID, 71, 8, @on
	exec sp_trace_setevent @TraceID, 71, 12, @on
	exec sp_trace_setevent @TraceID, 71, 60, @on
	exec sp_trace_setevent @TraceID, 71, 9, @on
	exec sp_trace_setevent @TraceID, 71, 6, @on
	exec sp_trace_setevent @TraceID, 71, 10, @on
	exec sp_trace_setevent @TraceID, 71, 14, @on
	exec sp_trace_setevent @TraceID, 71, 26, @on
	exec sp_trace_setevent @TraceID, 71, 3, @on
	exec sp_trace_setevent @TraceID, 71, 11, @on
	exec sp_trace_setevent @TraceID, 71, 35, @on
	exec sp_trace_setevent @TraceID, 71, 51, @on
	exec sp_trace_setevent @TraceID, 12, 7, @on
	exec sp_trace_setevent @TraceID, 12, 15, @on
	exec sp_trace_setevent @TraceID, 12, 31, @on
	exec sp_trace_setevent @TraceID, 12, 8, @on
	exec sp_trace_setevent @TraceID, 12, 1, @on
	exec sp_trace_setevent @TraceID, 12, 9, @on
	exec sp_trace_setevent @TraceID, 12, 6, @on
	exec sp_trace_setevent @TraceID, 12, 10, @on
	exec sp_trace_setevent @TraceID, 12, 14, @on
	exec sp_trace_setevent @TraceID, 12, 18, @on
	exec sp_trace_setevent @TraceID, 12, 26, @on
	exec sp_trace_setevent @TraceID, 12, 3, @on
	exec sp_trace_setevent @TraceID, 12, 11, @on
	exec sp_trace_setevent @TraceID, 12, 35, @on
	exec sp_trace_setevent @TraceID, 12, 51, @on
	exec sp_trace_setevent @TraceID, 12, 12, @on
	exec sp_trace_setevent @TraceID, 12, 60, @on
	exec sp_trace_setevent @TraceID, 12, 13, @on
	exec sp_trace_setevent @TraceID, 13, 7, @on
	exec sp_trace_setevent @TraceID, 13, 8, @on
	exec sp_trace_setevent @TraceID, 13, 12, @on
	exec sp_trace_setevent @TraceID, 13, 60, @on
	exec sp_trace_setevent @TraceID, 13, 1, @on
	exec sp_trace_setevent @TraceID, 13, 9, @on
	exec sp_trace_setevent @TraceID, 13, 6, @on
	exec sp_trace_setevent @TraceID, 13, 10, @on
	exec sp_trace_setevent @TraceID, 13, 14, @on
	exec sp_trace_setevent @TraceID, 13, 26, @on
	exec sp_trace_setevent @TraceID, 13, 3, @on
	exec sp_trace_setevent @TraceID, 13, 11, @on
	exec sp_trace_setevent @TraceID, 13, 35, @on
	exec sp_trace_setevent @TraceID, 13, 51, @on

	-- Set the Filters
	declare @intfilter int
	declare @bigintfilter bigint

	exec sp_trace_setfilter @TraceID, 10, 0, 7, N'SQL Server Profiler - 89ebc6ab-5e02-4b96-a29f-5ec754c3b303'
	-- Set the trace status to start
	exec sp_trace_setstatus @TraceID, 1

	-- display trace id for future references
	--select TraceID=@TraceID
	goto finish

	finish:
go

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200223
BEGIN
	UPDATE Version$ SET DbVer = 200224
	COMMIT TRANSACTION
	PRINT 'database updated to version 200224'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200223 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
