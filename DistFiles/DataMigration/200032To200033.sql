-- update database from version 200032 to 200033
BEGIN TRANSACTION  --( will be rolled back if wrong version#


-------------------------------------------------------------
-- Added interfix morph types
-------------------------------------------------------------
declare @list int, @en int, @es int, @fr int, @type int, @now datetime, @guid uniqueidentifier
select @now = getdate()
select @list=id from CmObject where guid$ = 'd7f713d8-e8cf-11d3-9764-00c04f186933'
select @en=id from LgWritingSystem where ICULocale = 'en'
select @es=id from LgWritingSystem where ICULocale = 'es'
select @fr=id from LgWritingSystem where ICULocale = 'fr'
exec CreateObject_MoMorphType @en, 'infixing interfix', @en, 'ifxnfx', NULL, NULL, NULL, 0,
	@now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 1, '-', '-', 0,
	@list, 8008, NULL, @type output, @guid output
insert CmPossibility_Name (obj, ws, txt) values (@type, @fr, 'interfixe de infixe')
insert CmPossibility_Name (obj, ws, txt) values (@type, @es, 'interfijo de tipo infijo')
insert CmPossibility_Abbreviation (obj, ws, txt) values (@type, @fr, 'ifxnfx')
insert CmPossibility_Abbreviation (obj, ws, txt) values (@type, @es, 'ifjnfj')
update CmObject set guid$ = '18D9B1C3-B5B6-4c07-B92C-2FE1D2281BD4' where id = @type
exec CreateObject_MoMorphType @en, 'prefixing interfix', @en, 'pfxnfx', NULL, NULL, NULL, 0,
	@now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 1, '-', NULL, 0,
	@list, 8008, NULL, @type output, @guid output
insert CmPossibility_Name (obj, ws, txt) values (@type, @fr, 'interfixe de préfixe')
insert CmPossibility_Name (obj, ws, txt) values (@type, @es, 'interfijo de tipo prefijoo')
insert CmPossibility_Abbreviation (obj, ws, txt) values (@type, @fr, 'pfxnfx')
insert CmPossibility_Abbreviation (obj, ws, txt) values (@type, @es, 'pfjnfj')
update CmObject set guid$ = 'AF6537B0-7175-4387-BA6A-36547D37FB13' where id = @type
exec CreateObject_MoMorphType @en, 'suffixing interfix', @en, 'sfxnfx', NULL, NULL, NULL, 0,
	@now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 1, NULL, '-', 0,
	@list, 8008, NULL, @type output, @guid output
insert CmPossibility_Name (obj, ws, txt) values (@type, @fr, 'interfixe de suffixe')
insert CmPossibility_Name (obj, ws, txt) values (@type, @es, 'interfijo de tipo sufijo')
insert CmPossibility_Abbreviation (obj, ws, txt) values (@type, @fr, 'sfxnfx')
insert CmPossibility_Abbreviation (obj, ws, txt) values (@type, @es, 'sfjnfj')
update CmObject set guid$ = '3433683D-08A9-4bae-AE53-2A7798F64068' where id = @type
GO

-------------------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200032
begin
	update Version$ set DbVer = 200033
	COMMIT TRANSACTION
	print 'database updated to version 200033'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200032 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
