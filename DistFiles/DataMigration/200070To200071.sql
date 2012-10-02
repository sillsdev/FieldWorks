-- Update database from version 200070 to 200071
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- (FWM-86) Updated MoInflectionalAffixMsa : InflectionFeatures from own collection to owning atomic.
-- But no data migration necessary according to AndyB.

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200070
begin
	update Version$ set DbVer = 200071
	COMMIT TRANSACTION
	print 'database updated to version 200071'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200070 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
