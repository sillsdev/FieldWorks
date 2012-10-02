-- update database FROM version 200123 to 200124
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-- (FWM-106) To WfiWordform, add an integer basic property of Checksum
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5062004, 2, 5062, null, 'Checksum',0,Null, null, null, null)
GO

-- (LT-3512) Need a data migration to change the <MaxAnalysesToReturn> parser parameter from -1 to 10
declare @param nvarchar(4000)
select top 1 @param = ParserParameters from MomorphologicalData
update MoMorphologicalData
	set ParserParameters = replace(@param, '<MaxAnalysesToReturn>-1</', '<MaxAnalysesToReturn>10</')
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200123
begin
	UPDATE Version$ SET DbVer = 200124
	COMMIT TRANSACTION
	print 'database updated to version 200124'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200123 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO