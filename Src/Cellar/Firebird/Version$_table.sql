/*******************************************************************************
* Version$ table
*
* Description:
*   Creates the Version$ table
*
* Dependencies:
*   spx_Drop_Table
*   udf_New_GUID
*
* Revision History:
*   3 March, 2005, Yves Blouin: Created
*   7 May 2008, Steve Miller/Ann Bush: Added this header; some minor changes.
*******************************************************************************/

EXECUTE PROCEDURE spx_Drop_Table('Version$');
COMMIT;

CREATE TABLE Version$
(
  "DBVER"              INTEGER NOT NULL,
  "GUID$"              CHAR(16) CHARACTER SET OCTETS NOT NULL,
  "DATESTDVER"         TIMESTAMP NOT NULL,
  "DATECUSTOMIZED"     TIMESTAMP,
  "PARDBVER"           INTEGER,
  "PARGUID"            CHAR(16) CHARACTER SET OCTETS,
  "PARDATESTDVER"      TIMESTAMP,
  "PARDATECUSTOMIZED"  TIMESTAMP
);
COMMIT;

ALTER TABLE Version$ ADD CONSTRAINT PK_VERSION$ PRIMARY KEY ("DBVER");
COMMIT;

/*
  YB FB FW UDF test to see if we can
  use udf_New_GUID without passing it an input value
  then the trigger T_BI0_Version$ would no longer be required
*/
INSERT INTO Version$("DBVER", "GUID$", "DATESTDVER", "DATECUSTOMIZED",
		"PARDBVER", "PARGUID", "PARDATESTDVER", "PARDATECUSTOMIZED")
	VALUES (200217, udf_New_GUID(), CURRENT_TIMESTAMP, NULL, NULL, NULL, NULL, NULL);
COMMIT;
