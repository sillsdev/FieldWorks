/**************************************************************************
 * Procedure: StopTrace
 *
 * Description:
 *	Stops the debug trace started by either procedure StartDebugTrace or
 *	procedure StartPerformanceTrace.
 *
 * Parameters:
 *	@TraceFile = Name of the file to stop. Should be either "FwDebug" or
 *		"FwPerformance", though others can be started and stopped as well.
 *
 * Returns:
 *	0 if succesful, 1 if not.
 *
 * Last Modified:
 *	8 August 2008
 *************************************************************************/

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
