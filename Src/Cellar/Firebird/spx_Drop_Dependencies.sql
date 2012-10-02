/*******************************************************************************
* spx_Drop_Dependencies
*
* Description:
*   Drop the dependencies of a given object.
*
* Parameters:
*   in_DependentOn:
*   out_Message:
*
* Dependencies:
*   None
*
* Revision History:
*   6 November 2004, Yves Blouin: Created
*   7 May 2008, Steve Miller: Added this header
*******************************************************************************/

SET TERM !!;
CREATE OR ALTER PROCEDURE spx_Drop_Dependencies (
	in_DependentOn  VARCHAR(31))
  RETURNS (
	out_Message   VARCHAR(1000))
AS
  DECLARE VARIABLE v_SQL          VARCHAR(1000);
  DECLARE VARIABLE v_Object       VARCHAR(31);
  DECLARE VARIABLE v_RefTable     VARCHAR(31);
  DECLARE VARIABLE v_DependentOn  VARCHAR(31);
BEGIN
  out_Message = 'spx_Drop_Dependencies(''' || in_DependentOn || ''')';
  v_DependentOn = UPPER(in_DependentOn);
  BEGIN
	FOR SELECT DISTINCT
		o.RDB$View_Name
	  FROM
		RDB$View_Relations o
	  WHERE
		o.RDB$Relation_Name = :v_DependentOn
	  INTO
		v_Object
	DO
	BEGIN
	  v_SQL = 'DROP VIEW ' || v_Object;
	  EXECUTE STATEMENT v_SQL;
	END
	FOR SELECT DISTINCT
		f.RDB$Relation_Name,
		r.RDB$Constraint_Name
	  FROM
		RDB$Relation_Constraints o,
		RDB$Ref_Constraints r,
		RDB$Relation_Constraints f
	  WHERE
		o.RDB$Relation_Name = :v_DependentOn
		AND o.RDB$Constraint_Type IN ('PRIMARY KEY')
		AND r.RDB$Const_Name_UQ = o.RDB$Constraint_Name
		AND f.RDB$Constraint_Name = r.RDB$Constraint_Name
	  INTO
		:v_RefTable,
		:v_Object
	DO
	BEGIN
	  v_SQL = 'ALTER TABLE ' || v_RefTable
		|| ' DROP CONSTRAINT ' || v_Object;
	  EXECUTE STATEMENT v_SQL;
	END
	FOR SELECT DISTINCT
		o.RDB$Constraint_Name
	  FROM
		RDB$Relation_Constraints o
	  WHERE
		o.RDB$Relation_Name = :v_DependentOn
		AND o.RDB$Constraint_Type IN ('FOREIGN KEY', 'CHECK', 'PRIMARY KEY')
	  INTO
		v_Object
	DO
	BEGIN
	  v_SQL = 'ALTER TABLE ' || v_DependentOn
		|| ' DROP CONSTRAINT ' || v_Object;
	  EXECUTE STATEMENT v_SQL;
	END
	FOR SELECT DISTINCT
		o.RDB$Dependent_Name
	  FROM
		RDB$Dependencies o
	  WHERE
		o.RDB$Depended_On_Name = :v_DependentOn
		AND o.RDB$Dependent_Type = 5
	  INTO
		v_Object
	DO
	BEGIN
	  EXECUTE PROCEDURE spx_Drop_Dependencies(:v_Object)
		RETURNING_VALUES :out_Message;
	  v_SQL = 'DROP PROCEDURE ' || v_Object;
	  EXECUTE STATEMENT v_SQL;
	END
	/*
	  A Generator never has dependencies.
	  Code included for completeness only.
	*/
	/*
	FOR SELECT DISTINCT
		o.RDB$Dependent_Name
	  FROM
		RDB$Dependencies o
	  WHERE
		o.RDB$Depended_On_Name = :v_DependentOn
		AND o.RDB$Dependent_Type = 14
	  INTO
		v_Object
	DO
	BEGIN
	  EXECUTE PROCEDURE spx_Drop_Dependencies(:v_Object)
		RETURNING_VALUES :out_Message;
	  v_SQL = 'DELETE FROM RDB$GENERATORS WHERE RDB$GENERATOR_NAME = ''' || v_Object || '''';
	  EXECUTE STATEMENT v_SQL;
	END
	*/
	FOR SELECT DISTINCT
		o.RDB$Dependent_Name
	  FROM
		RDB$Dependencies o,
		RDB$Triggers t
	  WHERE
		o.RDB$Depended_On_Name = :v_DependentOn
		AND o.RDB$Dependent_Name = t.RDB$Trigger_Name
		AND o.RDB$Dependent_Type = 2
		AND t.RDB$System_Flag = 0
	  INTO
		v_Object
	DO
	BEGIN
	  v_SQL = 'DROP TRIGGER ' || v_Object;
	  EXECUTE STATEMENT v_SQL;
	END
  END
  IF (v_SQL IS NOT NULL) THEN
	out_Message = v_SQL;
END !!
SET TERM ;!!
COMMIT;
