-- Update database from version 200065 to 200066
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- 1) Insert new Slots field into MoInflectionalAffixMsa
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5038007, 26, 5038, 5036, 'Slots',0,Null, null, null, null)

-- 2) Copy existing slot information into the new table
INSERT INTO MoInflectionalAffixMsa_Slots (Src, Dst)
	SELECT Id, Slot
	FROM MoInflectionalAffixMsa
	WHERE Slot is not null
	-- Note this query was fixed from an earlier version, but it will be rerun later
	-- when we remove the Slot property.
/*
--DEBUG
SELECT * FROM MoInflectionalAffixMsa
SELECT * FROM Class$ WHERE Name='MoInflectionalAffixMsa'
SELECT * FROM Field$ WHERE Name='Slots'
SELECT * FROM MoInflectionalAffixMsa_Slots
*/
---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200065
begin
	update Version$ set DbVer = 200066
	COMMIT TRANSACTION
	print 'database updated to version 200066'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200065 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
