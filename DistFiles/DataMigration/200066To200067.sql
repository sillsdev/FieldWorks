-- Update database from version 200066 to 200067
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- 1) If MoInflectionalAffixMsa_Slots is empty, then copy over the existing
-- MoInflectionalAffixMsa_Slot information.
-- (This will cover the bug in the migration to 200066 that didn't check
--  to make sure the existing slot information was non-null.

DECLARE @cSlots int
SELECT @cSlots=Count(*) FROM MoInflectionalAffixMsa_Slots
/*
--DEBUG: Remove all table rows in order to test our conditional patch below.
IF @cSlots<>0 BEGIN
	SELECT COUNT(*) FROM MoInflectionalAffixMsa_Slots -- DEBUG
	DELETE MoInflectionalAffixMsa_Slots -- DEBUG
	SELECT COUNT(*) FROM MoInflectionalAffixMsa_Slots -- DEBUG
END -- DEBUG
*/

IF @cSlots=0 BEGIN

	INSERT INTO MoInflectionalAffixMsa_Slots (Src, Dst)
		SELECT Id, Slot
		FROM MoInflectionalAffixMsa
		WHERE Slot is not null

END -- IF @cSlots

-- 2) Delete MoInflectionalAffixMsa_Slot field
DELETE FROM Field$ WHERE Id=5038006 	-- MoInflectionalAffixMsa_Slot

/*
--DEBUG
SELECT * FROM MoInflectionalAffixMsa
SELECT * FROM Class$ WHERE Name='MoInflectionalAffixMsa'
SELECT * FROM Field$ WHERE Name='Slots'
SELECT * FROM MoInflectionalAffixMsa_Slots

SELECT * FROM MoInflectionalAffixMsa_
SELECT miam.Id, miam.Slot, miams.Dst 'Slots Dst' FROM MoInflectionalAffixMsa_Slots miams
	JOIN MoInflectionalAffixMsa miam on miam.Id=miams.Src -- DEBUG
*/

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200066
begin
	update Version$ set DbVer = 200067
	COMMIT TRANSACTION
	print 'database updated to version 200067'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200066 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
