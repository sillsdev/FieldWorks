-- Update database from version 200071 to 200072
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- (FWM-86) change MoInflectionalAffixMsa_InflectionFeatures Field from OwningCollection to OwningAtomic

-- If MoInflectionalAffixMsa_InflectionFeatures is an OwningCollection (Type=25) we need to
-- change it to OwningAtomic (Type=23)
IF (SELECT COUNT(Id) FROM Field$ WHERE Id=5038001 AND Type=25) <> 0
BEGIN

	-- 1) First delete the field record containing the old OwningCollection type value (25)
	DELETE FROM Field$ WHERE Id=5038001 AND Type=25

	-- 2) Now reinsert the Field$ record with the new values including the new OwningAtomic Type (23)
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(5038001, 23, 5038, 57, 'InflectionFeatures',0,Null, null, null, null)
END

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200071
begin
	update Version$ set DbVer = 200072
	COMMIT TRANSACTION
	print 'database updated to version 200072'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200071 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
