/*******************************************************************************
* spx_Drop_Generator
*
* Description:
*   Drops a generator
*
* Parameters:
*   in_Object: The generator being dropped.
*   out_Message:
*
* Dependencies:
*   spx_Drop_Dependencies
*
* Revision History:
*   6 November 2004, Yves Blouin: Created
*   7 May 2008, Steve Miller: Added this header
*******************************************************************************/

SET TERM !!;
CREATE OR ALTER PROCEDURE spx_Drop_Generator (
	in_Object     VARCHAR(31))
  RETURNS (
	out_Message   VARCHAR(1000))
AS
  DECLARE VARIABLE v_SQL        VARCHAR(1000);
  DECLARE VARIABLE v_Object     VARCHAR(31);
BEGIN
  out_Message = 'spx_Drop_Generator(''' || in_Object || ''')';
  v_Object = UPPER(in_Object);
  BEGIN
	EXECUTE PROCEDURE spx_Drop_Dependencies
	  (:v_Object)
	  RETURNING_VALUES :out_Message;
	v_SQL = 'DELETE FROM RDB$GENERATORS WHERE RDB$GENERATOR_NAME = ''' || v_Object || '''';
	EXECUTE STATEMENT v_SQL;
  END
  IF (v_SQL IS NOT NULL) THEN
	out_Message = v_SQL;
END !!
SET TERM ;!!
COMMIT;
