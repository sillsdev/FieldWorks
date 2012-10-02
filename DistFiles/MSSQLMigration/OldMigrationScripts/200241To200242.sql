-- Update database from version 200241 to 200242
BEGIN TRANSACTION  --( will be rolled back if wrong version #)

-------------------------------------------------------------------------------
-- February 12, 2009 Ann Bush,  FWM-159 Change for Syntactic Markup
-- LangProject - Add TextMarkupTags: referencing atomic to CmPossibilityList.
-- Add new list, with 4 lists containing subpossibilities, add CmAnnotationDefn
-- LT-7727 add-on Create LiftResidue Property in LexEntryRef
-- FWM-156 add-on create FsFeatureSystem object pointed to by PhFeatureSystem (OA)
-------------------------------------------------------------------------------
--==( LgWritingSystem )==--
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(24032, 19, 24,
		null, 'CapitalizationInfo',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(24033, 19, 24,
		null, 'QuotationMarks',0,Null, null, null, null)
go

--==( cmPicture )==--
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(48004, 18, 48,
		null, 'Description',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(48005, 2, 48,
		null, 'LayoutPos',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(48006, 2, 48,
		null, 'ScaleFactor',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(48007, 2, 48,
		null, 'LocationRangeType',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(48008, 2, 48,
		null, 'LocationMin',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(48009, 2, 48,
		null, 'LocationMax',0,Null, null, null, null)
go

--==( cmFile )==--
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(47005, 14, 47,
		null, 'Copyright',0,Null, null, null, null)
go

-----------------------------------------------------------------------------------
------ Finish or roll back transaction as applicable
-----------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200241
BEGIN
	UPDATE Version$ SET DbVer = 200242
	COMMIT TRANSACTION
	PRINT 'database updated to version 200242'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200241 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
