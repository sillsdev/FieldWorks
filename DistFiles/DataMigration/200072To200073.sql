-- Update database from version 200072 to 200073
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- (FWM-88) --
-- Add FsFeatureDefn.CatalogSourceId: unicode
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(55009, 15, 55,
		null, 'CatalogSourceId',0,Null, null, null, null)
-- Add FsFeatureStructureType.CatalogSourceId: unicode
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(59005, 15, 59,
		null, 'CatalogSourceId',0,Null, null, null, null)
-- Add FsSymbolicFeatureValue.CatalogSourceId: unicode
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(65007, 15, 65,
		null, 'CatalogSourceId',0,Null, null, null, null)

-- (Add LexEntry properties LexemeForm & AlternateForms) --
-- Add LexEntry_LexemeForm : OwningAtomic of MoForm
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5002029, 23, 5002,
		5035, 'LexemeForm',0,Null, null, null, null)
-- Add LexEntry_AlternateForms : OwningSequence of MoForm
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5002030, 27, 5002,
		5035, 'AlternateForms',0,Null, null, null, null)
-- Add MoForm_IsAbstract : boolean
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5035003, 1, 5035,
		null, 'IsAbstract',0,null, null, null, null)

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200072
begin
	update Version$ set DbVer = 200073
	COMMIT TRANSACTION
	print 'database updated to version 200073'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200072 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
