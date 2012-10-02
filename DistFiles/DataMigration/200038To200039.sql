-- update database from version 200038 to 200039
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- NoteInterlinProcessTime needs to return the set of created objects
-------------------------------------------------------------------------------

--( Add MoUnclassifiedAffixMsa

INSERT INTO Class$ (Id, Mod, Base, Abstract, Name)
VALUES(5117, 5, 5041, 0, 'MoUnclassifiedAffixMsa')

GO
exec DefineCreateProc$ 5117
GO

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (5117001, 24, 5117, 5049, 'PartOfSpeech', 0, NULL, NULL)

GO

-------------------------------------------------------------------------------

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200038
begin
	update Version$ set DbVer = 200039
	COMMIT TRANSACTION
	print 'database updated to version 200039'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200038 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
