-- Update database from version 200060 to 200061
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Add and readjust mapping types and make several labels plural
update LexReferenceType set MappingType = 13 where MappingType = 8
update LexReferenceType set MappingType = 10 where MappingType = 7
update LexReferenceType set MappingType = 8 where MappingType = 6
update LexReferenceType set MappingType = 6 where MappingType = 5
update LexReferenceType set MappingType = 5 where MappingType = 4
update LexReferenceType set MappingType = 4 where MappingType = 3
update LexReferenceType set MappingType = 3 where MappingType = 2
update LexReferenceType set MappingType = 2 where MappingType = 22
update LexReferenceType set MappingType = 7 where MappingType = 27
update CmPossibility_Name set txt = 'Parts'
from CmPossibility_Name cn, LexReferenceType lrt
where cn.obj = lrt.id and txt = 'Part'
update CmPossibility_Name set txt = 'Specifics'
from CmPossibility_Name cn, LexReferenceType lrt
where cn.obj = lrt.id and txt = 'Specific'
update CmPossibility_Name set txt = 'Synonyms'
from CmPossibility_Name cn, LexReferenceType lrt
where cn.obj = lrt.id and txt = 'Synonym'
update CmPossibility_Name set txt = 'Confer'
from CmPossibility_Name cn, LexReferenceType lrt
where cn.obj = lrt.id and txt = 'See'
update CmPossibility_Abbreviation set txt = 'cf'
from CmPossibility_Abbreviation cn, LexReferenceType lrt
where cn.obj = lrt.id and txt = 'see'
go

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200060
begin
	update Version$ set DbVer = 200061
	COMMIT TRANSACTION
	print 'database updated to version 200061'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200060 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
