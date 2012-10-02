/*******************************************************************************
* spx_Valid_New_Constraint_Name
*
* Description:
*   Returns a valid constraint name for a table.
*
* Parameters:
*   in_Name: Constraint name
*
* Returns:
*    Valid constraint name
*
* Dependencies:
*   udf_StrLen
*   udf_RTrim
*   udf_SubStrLen
*
* Revision History:
*   22 June, 2007, Yves Blouin: Created
*   7 May 2008, Steve Miller: Added this header; removed use of --
*******************************************************************************/

SET TERM !!;
CREATE OR ALTER PROCEDURE spx_Valid_New_Constraint_Name (
	 inName VARCHAR(128) CHARACTER SET UTF8)
RETURNS (
	 outValidName  VARCHAR(31) CHARACTER SET UTF8)
AS
	 DECLARE VARIABLE vCount SMALLINT;
	 DECLARE VARIABLE vDupCount SMALLINT;
	 DECLARE VARIABLE vString VARCHAR(30) CHARACTER SET UTF8;
	 DECLARE VARIABLE vTrimLength SMALLINT;
BEGIN
	 outValidName = NULL;
	 inName = UPPER(inName);

	 /* If name is longer than 31 characters then determine a name within
		31 characters that will be unique. */
	 IF (udf_StrLen(inName) > 31) THEN
		 outValidName = UPPER(SUBSTRING(:inName FROM 1 FOR 31));
	 ELSE
		 outValidName = inName;
	 vDupCount = 0;
	 /* System tables are still in CHARACTER SET UNICODE_FSS. */
	 SELECT COUNT(1) AS NameCount
		 FROM RDB$Relation_Constraints
		 WHERE RDB$Constraint_Name = CAST(:outValidName AS VARCHAR(31) CHARACTER SET UNICODE_FSS)
		 INTO :vCount;

	 WHILE (vCount > 0) DO
	 BEGIN
		 vString = CAST(vDupCount AS VARCHAR(30) CHARACTER SET UTF8);
		 vTrimLength = udf_StrLen(
			 CAST(udf_RTrim(outValidName) AS VARCHAR(31) CHARACTER SET UTF8)
			 || vString) - 31;
		 IF (vTrimLength < 0) THEN
			 vTrimLength = 0;
		 outValidName = CAST(
			 udf_SubStrLen(outValidName, 1, udf_StrLen(outValidName) - vTrimLength)
			 AS VARCHAR(31) CHARACTER SET UTF8)
			 || vString;
		 SELECT COUNT(1) AS Name_Count
			 FROM RDB$Relation_Constraints
			 WHERE RDB$Constraint_Name = CAST(:outValidName AS VARCHAR(31) CHARACTER SET UNICODE_FSS)
			 INTO :vCount;
		 vDupCount = vDupCount + 1;
	 END /* (v_Count > 0) */
	 outValidName = CAST(UPPER(outValidName) AS VARCHAR(31) CHARACTER SET UTF8);
	 SUSPEND;
END !!
SET TERM ;!!
COMMIT;
