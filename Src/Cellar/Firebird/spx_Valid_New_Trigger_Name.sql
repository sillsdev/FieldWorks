/*******************************************************************************
* spx_Valid_New_Trigger_Name
*
* Description:
*
*
* Parameters:
*   in_External:
*   out_Message:
*
* Dependencies:
*   none
*
* Revision History:
*   This is on its third version in SVN, and it's hard to tell what happened
*        to it.
*   7 May 2008, Steve Miller: Added this header
*******************************************************************************/

/*SET TERM !!;
CREATE OR ALTER PROCEDURE spx_Valid_New_Trigger_Name (
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

	 --( If name is longer than 31 characters then determine a name within
	 --( 31 characters that will be unique.
	 IF (udf_StrLen(inName) > 31) THEN
		 outValidName = UPPER(SUBSTRING(:inName FROM 1 FOR 31));
	 ELSE
		 outValidName = inName;
	 vDupCount = 0;
	 --( System tables are still in CHARACTER SET UNICODE_FSS.
	 SELECT COUNT(1) AS NameCount
		 FROM RDB$Triggers
		 WHERE RDB$Trigger_Name = CAST(:outValidName AS VARCHAR(31) CHARACTER SET UNICODE_FSS)
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
			 FROM RDB$Triggers
			 WHERE RDB$Trigger_Name = CAST(:outValidName AS VARCHAR(31) CHARACTER SET UNICODE_FSS)
			 INTO :vCount;
		 vDupCount = vDupCount + 1;
	 END --( (v_Count > 0)
	 outValidName = CAST(UPPER(outValidName) AS VARCHAR(31) CHARACTER SET UTF8);
	 SUSPEND;
END !!
SET TERM ;!!
COMMIT;  */

SET TERM !! ;
CREATE OR ALTER PROCEDURE spx_Valid_New_Trigger_Name (
	   inPrefix VARCHAR(20),
	   inClid INT,
	   inFlid INT)
RETURNS (outValidName VARCHAR(31))
AS
	   DECLARE VARIABLE vClass VARCHAR(11);
	   DECLARE VARIABLE vField VARCHAR(11);
BEGIN

	 vClass = COALESCE(CAST(inClid AS VARCHAR(11)), '');
	 vField = COALESCE(CAST(inFlid AS VARCHAR(11)), '');
	 outValidName = SUBSTRING(inPrefix || vClass || vField FROM 1 FOR 31);

END !!
SET TERM ; !!
COMMIT;
