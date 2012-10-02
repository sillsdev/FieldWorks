-- Update database from version 200091 to 200092
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- This fixes LT-2988. ParserParameters were set in 21 Jun 2004, but a migration was not included.

declare @parserParams nvarchar(4000)
select @parserParams=ParserParameters from MoMorphologicalData
if @parserParams is null
  BEGIN
	update MoMorphologicalData set ParserParameters='<ParserParameters><XAmple><MaxPrefixes>5</MaxPrefixes><MaxInfixes>1</MaxInfixes><MaxSuffixes>5</MaxSuffixes><MaxInterfixes>0</MaxInterfixes><MaxNulls>1</MaxNulls><MaxAnalysesToReturn>-1</MaxAnalysesToReturn></XAmple></ParserParameters>'
  END
GO

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200091
begin
	update Version$ set DbVer = 200092
	COMMIT TRANSACTION
	print 'database updated to version 200092'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200091 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
