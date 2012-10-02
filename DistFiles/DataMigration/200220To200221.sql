-- Update database from version 200220 to 200221
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FDB-202: Migration for Key Terms Enhancements (FWM-141)
-------------------------------------------------------------------------------

--( Among other things, ChkItem gets turned into ChkTerm

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(5126, 5, 0, 0, 'ChkRendering')
go

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5126001, 24, 5126,
		5062, 'SurfaceForm',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5126002, 24, 5126,
		5060, 'Meaning',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5126003, 16, 5126,
		null, 'Explanation',0,Null, null, null, null)
go


insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(5125, 5, 7, 0, 'ChkTerm')
go

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5125001, 27, 5125,
		5116, 'Occurrences',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5125002, 16, 5125,
		null, 'SeeAlso',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5125003, 25, 5125,
		5126, 'Renderings',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5125004, 2, 5125,
		null, 'TermId',0,Null, null, null, null)
go

--( Move ChkItem Ids into ChkTerm
INSERT INTO ChkTerm (Id) SELECT Id FROM ChkItem
--( Make sure the CmObject records match the ChkTerm rows now
UPDATE CmObject SET Class$ = 5125 WHERE Class$ = 5115
--( "Move" ChkItem.Occurrences to ChkTerm.Occurrences
UPDATE CmObject SET OwnFlid$ = 5125001 WHERE OwnFlid$ = 5115001
--( Now whack ChkItem
DELETE ChkItem_Senses
TRUNCATE TABLE ChkItem
DELETE FROM Field$ WHERE Id = 5115002
DELETE FROM Field$ WHERE Id = 5115001
DELETE FROM ClassPar$ WHERE Src = 5115
DELETE FROM Class$ WHERE Id = 5115
GO

--( Add ChkRef.Location

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5116006, 2, 5116,
		null, 'Location',0,Null, null, null, null)
GO

--( Remove ChkRef.Comment

DELETE FROM ChkRef_Comment
DELETE FROM Field$ WHERE Id = 5116003
GO

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200220
BEGIN
	UPDATE Version$ SET DbVer = 200221
	COMMIT TRANSACTION
	PRINT 'database updated to version 200221'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200220 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
