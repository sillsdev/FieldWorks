/**************************************************************************
 * Procedure: StartDebugTrace
 *
 * Description:
 *	Creates a trace file, turns on events and data columns to use, and
 *  starts the trace. For debugging.
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
