/*******************************************************************************
* Class$ table
*
* Description:
*   Creates the Class$ table
*
* Dependencies:
*   spx_Drop_Table
*
* Revision History:
*  22 June, 2007, Yves Blouin: Created
*   7 May 2008, Steve Miller/Ann Bush: Added this header; some minor changes.
*******************************************************************************/

EXECUTE PROCEDURE spx_Drop_Table('Class$');
COMMIT;

CREATE TABLE Class$
(
  "ID"                 INTEGER NOT NULL,
  "MOD"                INTEGER NOT NULL,
  "BASE"               INTEGER NOT NULL,
  "ABSTRACT"           CHAR(2) CHARACTER SET OCTETS,
  "NAME"               VARCHAR(100)CHARACTER SET UTF8 NOT NULL
);
COMMIT;

ALTER TABLE Class$ ADD CONSTRAINT PK_CLASS$ PRIMARY KEY ("ID");
COMMIT;
ALTER TABLE Class$ ADD CONSTRAINT UQ_CLASS$_NAME UNIQUE ("NAME");
COMMIT;
ALTER TABLE Class$ ADD CONSTRAINT FK_CLASS$_MODULE$ FOREIGN KEY ("MOD") REFERENCES Module$ ("ID");
COMMIT;
ALTER TABLE Class$ ADD CONSTRAINT FK_CLASS$_CLASS$ FOREIGN KEY ("BASE") REFERENCES Class$ ("ID");
COMMIT;
