-- update database FROM version 200132 to 200133
BEGIN TRANSACTION  --( will be rolled back if wrong version#
/*
 * Fixed some attributes in ScrMarkerMapping and ScrImportP6Project
 * that needed to be Unicode but were erroneously entered as 'string'.
 * They haven't been used for data yet so the migration just deletes them
 * and adds the corrected versions.
 */

-------------------------------------------------------------------------------
-- Fix the BeginMarker, EndMarker, and ICULocale fields in the ScrMarkerMapping
-- table
-------------------------------------------------------------------------------
delete from [Field$]
	where [Id] in (3016001, 3016002, 3016006)
	GO
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3016001, 15, 3016,
		null, 'BeginMarker',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3016002, 15, 3016,
		null, 'EndMarker',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3016006, 15, 3016,
		null, 'ICULocale',0,Null, null, null, null)
GO
--update the ScrMarkerMapping class
exec DefineCreateProc$ 3016
GO
exec UpdateClassView$ 3016
GO

-------------------------------------------------------------------------------
-- Fix the ParatextID field in the ScrImportP6Project table
-------------------------------------------------------------------------------
delete from [Field$]
	where [Id] = 3014001
GO
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3014001, 15, 3014,
		null, 'ParatextID',0,Null, null, null, null)
GO
--update the ScrImportP6Project class
exec DefineCreateProc$ 3014
GO
exec UpdateClassView$ 3014
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200132
begin
	UPDATE Version$ SET DbVer = 200133
	COMMIT TRANSACTION
	print 'database updated to version 200133'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200132 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO