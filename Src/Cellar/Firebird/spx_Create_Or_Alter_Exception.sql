/*******************************************************************************
* spx_Create_Or_Alter_Exception
*
* Description:
*        Set up exception handling.
*
* Parameters:
*   in_Name: Exception name
*   in_Message: Exception message
*
* Dependencies:
*   None
*
* Revision History:
*   19 March, 2006, Yves Blouin: Created
*   7 May 2008, Steve Miller: Added this header; minor changes
*******************************************************************************/

SET TERM !!;
CREATE OR ALTER PROCEDURE spx_Create_Or_Alter_Exception (
	in_Name     VARCHAR(31),
	in_Message  VARCHAR(77))
  RETURNS (
	out_Message   VARCHAR(1000))
AS
  DECLARE VARIABLE v_SQL        VARCHAR(1000);
  DECLARE VARIABLE v_Object     VARCHAR(31);
  DECLARE VARIABLE v_Name       VARCHAR(31);
  DECLARE VARIABLE v_Exists     INTEGER;
BEGIN
  out_Message = 'spx_Create_Or_Alter_Exception(''' || in_Name || ''')';
  v_Name = UPPER(in_Name);
  SELECT
	  COUNT(1)
	FROM
	  RDB$Exceptions o
	WHERE
	  o.RDB$Exception_Name = :v_Name
	INTO
	  v_Exists;
  IF (v_Exists = 0) THEN
	v_SQL = 'CREATE';
  ELSE
	v_SQL = 'ALTER' ;
  v_SQL = v_SQL || ' EXCEPTION ' || v_Name
	|| ' ''' || in_Message || '''';
  IF (v_SQL IS NOT NULL) THEN
	out_Message = v_SQL;
  EXECUTE STATEMENT v_SQL;
END !!
SET TERM ;!!
COMMIT;
