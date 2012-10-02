/*******************************************************************************
* XML_To_Id
*
* Description:
*
*
* Parameters:
*   io_String:
*
* Dependencies:
*   UDF_STRPOS
*
* Revision History:
*   23 July, 2007, Yves Blouin: Created
*   7 May 2008, Steve Miller: Added this header, removed use of --
*******************************************************************************/

SET TERM !!;

CREATE OR ALTER PROCEDURE XML_To_Id
 (
	io_String VARCHAR(32765)
 )
RETURNS
 (
	Id INTEGER
 )
AS
DECLARE VARIABLE v_ValueBegin INTEGER;
DECLARE VARIABLE v_ValueEnd INTEGER;
BEGIN
  /* If case-insensitive comparison is required then use UPPER */
  v_ValueBegin = UDF_STRPOS('<Obj Id="', io_String) + 9;
  WHILE (v_ValueBegin > 9) DO
	BEGIN
	  v_ValueEnd = UDF_STRPOS('"/>', io_String);
	  Id = SUBSTRING(io_String FROM v_ValueBegin FOR v_ValueEnd - v_ValueBegin);
	  io_String = SUBSTRING(io_String FROM v_ValueEnd + 3);
	  SUSPEND;
	  /* If case-insensitive comparison is required then use UPPER */
	  v_ValueBegin = UDF_STRPOS('<Obj Id="', io_String) + 9;
	END
END !!
SET TERM ;!!
