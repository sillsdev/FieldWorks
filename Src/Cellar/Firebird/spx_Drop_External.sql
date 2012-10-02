/*******************************************************************************
* spx_Drop_External
*
* Description:
*
*
* Parameters:
*   in_External:
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
CREATE OR ALTER PROCEDURE spx_Drop_External (
	in_External      VARCHAR(31))
  RETURNS (
	out_Message   VARCHAR(1000))
AS
  DECLARE VARIABLE v_SQL        VARCHAR(1000);
  DECLARE VARIABLE v_Object     VARCHAR(31);
  DECLARE VARIABLE v_External      VARCHAR(31);
BEGIN
  out_Message = 'spx_Drop_External(''' || in_External || ''')';
  v_External = UPPER(in_External);
  /*
	RDB$System_Flag = 0 isn't implemented for UDFs
	to indicate user as opposed to system UDFs
  */
  BEGIN
	FOR SELECT
		o.RDB$Function_Name
	  FROM
		RDB$Functions o
	  WHERE
		o.RDB$Function_Name = :v_External
	  INTO
		v_Object
	DO
	BEGIN
	  /* This would not work ~*/
	  /* TODO: Is this procedure being used anywhere? If not, should it be
	  whacked? */
	  EXECUTE PROCEDURE spx_Drop_Dependencies
		(:v_Object)
		RETURNING_VALUES :out_Message;
	  v_SQL = 'DROP EXTERNAL FUNCTION ' || v_Object;
	  EXECUTE STATEMENT v_SQL;
	END
  END
  IF (v_SQL IS NOT NULL) THEN
	out_Message = v_SQL;
END !!
SET TERM ;!!
COMMIT;
