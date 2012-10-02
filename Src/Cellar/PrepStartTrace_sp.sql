/**************************************************************************
 * Procedure: PrepStartTrace
 *
 * Description:
 *	This is a utility procedure to support StartDebugTrace,
 *	StartPerformanceTrace, or any other trace file that might come along.
 *
 * Parameters:
 *	@TracePath = Path for the trace. The file name and is added
 *		by the stored proc. Defaults to the root directory.
 *
 * Returns:
 *	0 if succesful, otherwise the error code.
 *
 * Notes:
 *	To stop the trace, use StopDebugTrace.
 *
 *	The easiest way to create code for a new type of trace file is (in
 *	SQL Server 2005):
 *
 *		1. Go to Profiler
 *		2. Use the interface to create a new trace with the events and
 *			columns you want
 *		3. Go to File/Export/Export Script Defin ition/For SQL Server 2005
 *		4. Copy the generated code here.
 *
 * Last Modified:
 *	8 August 2008
 *************************************************************************/

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