/*******************************************************************************
* Class$ table
*
* Description:
*   Creates the Class$ table
*
* Dependencies:
*   spx_Drop_Table
*	udf_New_GUID
*
* Revision History:
*  ?, Yves Blouin: Created
*  ?, Steve Miller: Did some debugging
*  May 2008, Steve Miller/Ann Bush: Added this header; some minor changes.
*******************************************************************************/

EXECUTE PROCEDURE spx_Drop_Table('Field$');
COMMIT;
/*
create table [Field$] (
		[Id]                int                                primary key clustered,
		[Type]                int                not null,
		[Class]                int                not null        references [Class$] ([Id]),
		[DstCls]        int                null                references [Class$] ([Id]),
		[Name]                 nvarchar(kcchMaxName) not null,
		[Custom]         tinyint                not null         default 1,
		[CustomId]         uniqueidentifier null                default newid(),
		[Min]                bigint                null                default null,
		[Max]                bigint                null                default null,
		[Big]                bit                null                default 0,
		UserLabel        NVARCHAR(kcchMaxName) NULL DEFAULT NULL,
		HelpString        NVARCHAR(kcchMaxName) NULL DEFAULT NULL,
		ListRootId        INT NULL DEFAULT NULL,
		WsSelector        INT NULL,
		XmlUI                NTEXT NULL
		constraint [_UQ_Field$_Class_Fieldname]        unique ([class], [name]),
		constraint [_CK_Field$_DstCls]
				check (([Type] < kcptMinObj and [DstCls] is null) or
						([Type] >= kcptMinObj and [DstCls] is not null)),
		constraint [_CK_Field$_Custom]
				check (([Custom] = 0 and [CustomId] is null) or
						([Custom] = 1 and [CustomId] is not null)),
		constraint [_CK_Field$_Type_Integer]
				check (([Type] <> kcptInteger and [Min] is null and [Max] is null) or
						[Type] = kcptInteger),
		constraint [_CK_Field$_Type]
				check ([Type] In (        kcptBoolean,
										kcptInteger,
										kcptNumeric,
										kcptFloat,
										kcptTime,
										kcptGuid,
										kcptImage,
										kcptGenDate,
										kcptBinary,
										kcptString,
										kcptMultiString,
										kcptUnicode,
										kcptMultiUnicode,
										kcptBigString,
										kcptMultiBigString,
										kcptBigUnicode,
										kcptMultiBigUnicode,
										kcptOwningAtom,
										kcptReferenceAtom,
										kcptOwningCollection,
										kcptReferenceCollection,
										kcptOwningSequence,
										kcptReferenceSequence)),
		constraint [_CK_Field$_MinMax] check ((Max is null and min is null) or (Type = kcptInteger and Max >= Min))
)

*/
CREATE TABLE Field$
(
  "ID"                 INTEGER NOT NULL,
  "TYPE"               INTEGER NOT NULL,
  "CLASS"              INTEGER NOT NULL,
  "DSTCLS"             INTEGER,
  "NAME"               VARCHAR(31) CHARACTER SET NONE NOT NULL,
  "CUSTOM"             CHAR(1) CHARACTER SET OCTETS NOT NULL,
  "CUSTOMID"           CHAR(16) CHARACTER SET UTF8,
  "MIN"                BIGINT,
  "MAX"                BIGINT,
  "BIG"                CHAR(2) CHARACTER SET OCTETS,
  "USERLABEL"          VARCHAR(100) CHARACTER SET UTF8,
  "HELPSTRING"         VARCHAR(100) CHARACTER SET UTF8,
  "LISTROOTID"         INTEGER,
  "WSSELECTOR"         INTEGER,
  --( VARCHAR(10921) exceeds the limit on UTF8, and this field
  --( is currently not in use (June 2007).
  "XMLUI"              VARCHAR(10921) CHARACTER SET UNICODE_FSS
);
COMMIT;
ALTER TABLE Field$ ADD CONSTRAINT PK_FIELD$ PRIMARY KEY ("ID");
COMMIT;
ALTER TABLE Field$ ADD CONSTRAINT UQ_FIELD$_CLASS_NAME UNIQUE ("CLASS", "NAME");
COMMIT;
ALTER TABLE Field$ ADD CONSTRAINT FK_FIELD$_CLASS$_CLASS FOREIGN KEY ("CLASS") REFERENCES Class$ ("ID");
COMMIT;
-- TODO (SteveMiller):
-- Currently (June 2007), field 6001006 has a DstCls of 5054, which gets created
-- after field 6001006, causing this constraint to fire. It's probably still
-- worth having, but after the database gets created. Either the INSERT has to
-- be moved, or this constraint should be created later.
--
-- ALTER TABLE Field$ ADD CONSTRAINT FK_FIELD$_CLASS$_DSTCLS FOREIGN KEY ("DSTCLS") REFERENCES Class$ ("ID");
-- COMMIT;
ALTER TABLE Field$ ADD CONSTRAINT CK_FIELD$_DSTCLS_TYPE CHECK
  (("TYPE" < kcptMinObj AND "DSTCLS" IS NULL)
  OR ("TYPE" >= kcptMinObj AND "DSTCLS" IS NOT NULL));
COMMIT;
-- TODO (SteveMiller):
-- For some reason beyond me, this check constraint fails when it shouldn't.
--
-- ALTER TABLE Field$ ADD CONSTRAINT CK_FIELD$_CUSTOM_CUSTOMID CHECK
--  (("CUSTOM" = 0 AND "CUSTOMID" IS NULL)
--  OR ("CUSTOM" = 1 AND "CUSTOMID" IS NOT NULL));
COMMIT;
ALTER TABLE Field$ ADD CONSTRAINT CK_FIELD$_TYPE_MIN_MAX CHECK
  (("TYPE" <> kcptInteger AND "MAX" IS NULL AND "MIN" IS NULL) OR "TYPE" = kcptInteger);
COMMIT;
ALTER TABLE Field$ ADD CONSTRAINT CK_FIELD$_TYPE_INTEGER CHECK
  (("MIN" IS NULL AND "MAX" IS NULL) OR ("TYPE" = kcptInteger AND "MAX" >= "MIN"));
COMMIT;

ALTER TABLE Field$ ADD CONSTRAINT CK_FIELD$_TYPE CHECK
  ("TYPE" IN (kcptBoolean,
										kcptInteger,
										kcptNumeric,
										kcptFloat,
										kcptTime,
										kcptGuid,
										kcptImage,
										kcptGenDate,
										kcptBinary,
										kcptString,
										kcptMultiString,
										kcptUnicode,
										kcptMultiUnicode,
										kcptBigString,
										kcptMultiBigString,
										kcptBigUnicode,
										kcptMultiBigUnicode,
										kcptOwningAtom,
										kcptReferenceAtom,
										kcptOwningCollection,
										kcptReferenceCollection,
										kcptOwningSequence,
										kcptReferenceSequence));
COMMIT;
CREATE INDEX IX_FIELD$_DSTCLS ON Field$("DSTCLS");
COMMIT;

/* T_BI0_Field$ Trigger */
#include <T_BI0_Field$.trg>

/* T_BU0_Field$ Trigger */
#include <T_BU0_Field$.trg>