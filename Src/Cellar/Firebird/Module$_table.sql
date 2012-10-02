/*******************************************************************************
* Module$ table
*
* Description:
*   Creates the Module$ table
*
* Dependencies:
*   spx_Drop_Table
*
* Revision History:
*  22 June, 2007, Yves Blouin: Created
*   7 May 2008, Steve Miller/Ann Bush: Added this header; some minor changes.
*******************************************************************************/

EXECUTE PROCEDURE spx_Drop_Table('Module$');
COMMIT;

CREATE TABLE Module$
(
  "ID"                 INTEGER NOT NULL,
  "NAME"               VARCHAR(100) CHARACTER SET UTF8 NOT NULL,
  "VER"                INTEGER NOT NULL,
  "VERBACK"            INTEGER NOT NULL
);
COMMIT;
ALTER TABLE Module$ ADD CONSTRAINT PK_MODULE$ PRIMARY KEY ("ID");
COMMIT;
ALTER TABLE Module$ ADD CONSTRAINT UQ_MODULE$_NAME UNIQUE ("NAME");
COMMIT;
ALTER TABLE Module$ ADD CONSTRAINT CK_MODULE$_VERBACK CHECK ("VERBACK" BETWEEN 1 AND "VER");
COMMIT;
