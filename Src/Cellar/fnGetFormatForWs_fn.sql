/***********************************************************************************************
 * Function: fnGetFormatForWs
 *
 * Description:
 *	Create the minimal format value for a string in the given writing system.  This is needed,
 *  for example, in data migration when the type of a field has changed from Unicode to String.
 *
 * Parameters:
 *	@ws = database id of a writing system
 *
 * Returns:
 *   varbinary(20) value containing the desired format value (which uses 19 of the 20 bytes)
 *
 * Notes:
 *   This is more deterministic and reliable than former approaches, which were not even as good
 *   as the following SQL:
 *		SELECT TOP 1 Fmt
 *		FROM MultiStr$
 *		WHERE Ws=@ws AND DATALENGTH(Fmt) = 19
 *		GROUP BY Fmt
 *		ORDER BY COUNT(Fmt) DESC
 **********************************************************************************************/
if object_id('fnGetFormatForWs') is not null begin
	print 'removing function fnGetFormatForWs'
	drop function fnGetFormatForWs
end
go

create function fnGetFormatForWs (@ws int)
returns varbinary(20)
as
begin
	DECLARE @hexVal varbinary(20)

	-- one run with one property (the writing system), starting at the beginning of the string
	SET @hexVal= 0x010000000000000000000000010006

	-- CAST (@ws AS varbinary(4)) puts the bytes in the wrong order for the format string,
	-- so we'll add it one byte at a time in the desired order.
	DECLARE @byte int, @x1 int
	SET @byte = @ws % 256
	SET @x1 = @ws / 256
	SET @hexVal = @hexVal  + CAST (@byte AS varbinary(1))

	SET @byte = @x1 % 256
	SET @x1 = @x1 / 256
	SET @hexVal = @hexVal  + CAST (@byte AS varbinary(1))

	SET @byte = @x1 % 256
	SET @x1 = @x1 / 256
	SET @hexVal = @hexVal  + CAST (@byte AS varbinary(1))

	SET @byte = @x1 % 256
	return @hexVal  + CAST (@byte AS varbinary(1))
end
go
