/*******************************************************************************
* spx_Drop_All_Exceptions
*
* Description:
*
*
* Parameters:
*   none
*
* Dependencies:
*   spx_Drop_Dependencies
*
* Revision History:
*   6 November 2004, Yves Blouin: Created
*   7 May 2008, Steve Miller: Added this header
*******************************************************************************/

SET TERM !!;
CREATE OR ALTER PROCEDURE spx_Drop_All_Exceptions
  RETURNS (
	out_Message   VARCHAR(1000))
AS
  DECLARE VARIABLE v_SQL        VARCHAR(1000);
  DECLARE VARIABLE v_Object     VARCHAR(31);
  DECLARE VARIABLE v_Name       VARCHAR(31);
  DECLARE VARIABLE v_Exists     INTEGER;
BEGIN
  out_Message = 'spx_Drop_All_Exceptions';
  BEGIN
	FOR SELECT
		o.RDB$Exception_Name
	  FROM
		RDB$Exceptions o
	  INTO
		v_Object
	DO
	BEGIN
	  EXECUTE PROCEDURE spx_Drop_Dependencies
		(:v_Object)
		RETURNING_VALUES :out_Message;
	  v_SQL = 'DROP EXCEPTION ' || v_Object;
	  EXECUTE STATEMENT v_SQL;
	END
  END
  IF (v_SQL IS NOT NULL) THEN
	out_Message = v_SQL;
END !!
SET TERM ;!!
COMMIT;
