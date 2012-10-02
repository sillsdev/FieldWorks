-- update database FROM version 200106 to 200107

BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- Add Productivity Restrictions possibility list
-- Make some name changes to existing possibility lists
-------------------------------------------------------------------------------

declare @id int, @mdata int, @ws int, @guid uniqueidentifier
select top 1 @mdata = id from MoMorphologicalData
select top 1 @ws = id from LgWritingSystem where IcuLocale = 'en'
exec CreateOwnedObject$ 8, @id output, @guid output, @mdata, 5040009, 23
update CmPossibilityList set Depth = 1, ItemClsid = 7, WsSelector = -3 where id = @id
exec SetMultiTxt$ 5001, @id, @ws, 'Productivity Restrictions'
exec SetMultiTxt$ 8010, @id, @ws, 'ProdRests'

update CmMajorObject_Name set txt = 'Academic Domains' where txt = 'Domain Types'
update CmMajorObject_Name set txt = 'Minor Entry Conditions' where txt = 'Allomorph Conditions'
update CmMajorObject_Name set txt = 'Usages' where txt = 'Usage Types'
update CmMajorObject_Name set txt = 'Education Levels' where txt = 'Education'
update CmPossibilityList_Abbreviation set txt = 'MECond' where txt = 'AlloCond'
update CmPossibilityList_Abbreviation set txt = 'AcDom' where txt = 'DomTyp'
update CmPossibilityList_Abbreviation set txt = 'Use' where txt = 'UseTyp'
update CmPossibilityList_Abbreviation set txt = 'EdLev' where txt = 'Ed'

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200106
begin
	UPDATE Version$ SET DbVer = 200107
	COMMIT TRANSACTION
	print 'database updated to version 200107'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200106 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
