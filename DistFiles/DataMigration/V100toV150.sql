DECLARE @sNewDbName nvarchar(1000)
DECLARE @clsName nvarchar(1000)
DECLARE @modClsName nvarchar(1000)
DECLARE @fldName nvarchar(1000), @modFldName nvarchar(1000)
DECLARE @sFldNamesIn nvarchar(4000), @sFldNamesOut nvarchar(4000)
DECLARE @sQry nvarchar(4000)
DECLARE @sNewDbOwner nvarchar(1000)
DECLARE @CustFldId nvarchar(20)

set @sNewDbName = 'Version150DataMigration'
set @sNewDbOwner = @sNewDbName + '.dbo.'

--Turn off all constraints for the duration.

set @sQry = @sNewDbOwner + 'ManageConstraints$'
exec @sQry  null, 'F', 'NOCHECK'

-- Copy CmObject fields (except UpdStmp, which can't be copied)

set @sQry = N'SET IDENTITY_INSERT ' + @sNewDbOwner + 'CmObject ON
	INSERT INTO ' + @sNewDbOwner +
		'CmObject (Id,Guid$,Class$,Owner$,OwnFlid$,OwnOrd$,UpdDttm)
		select Id,Guid$,Class$,Owner$,OwnFlid$,OwnOrd$,UpdDttm from CmObject
	SET IDENTITY_INSERT ' + @sNewDbOwner + 'CmObject OFF'
exec (@sQry)

--== Copy Multilingual string tables. ==--

set @sQry = 'Insert into ' + @sNewDbOwner + 'MultiStr$ (Flid, Obj, Ws, Txt, Fmt) select Flid, Obj, Enc, Txt, Fmt from MultiStr$'
exec (@sQry)
set @sQry = 'Insert into ' + @sNewDbOwner + 'MultiBigStr$ (Flid, Obj, Ws, Txt, Fmt) select Flid, Obj, Enc, Txt, Fmt from MultiBigStr$'
exec (@sQry)
set @sQry = 'Insert into ' + @sNewDbOwner + 'MultiTxt$ (Flid, Obj, Ws, Txt) select Flid, Obj, Enc, Txt from MultiTxt$'
exec (@sQry)
set @sQry = 'Insert into ' + @sNewDbOwner + 'MultiBigTxt$ (Flid, Obj, Ws, Txt) select Flid, Obj, Enc, Txt from MultiBigTxt$'
exec (@sQry)

--== Copy LgEncoding and LgWritingSystem fields as relevant into new LgWritingSystem ==--

/*
	[Locale],
	[Renderer],
	[RendererInit],
	[DefaultMonospace],
*/

/*
SELECT
	e.[Id],
	e.[Encoding],
	e.[Locale],
	ws.[DefaultMonospace],
	ws.[DefaultSansSerif],
	ws.[DefaultSerif],
	ws.[FontVariation],
	ws.[KeyboardType],
	ws.[RightToLeft]
*/

set @sQry = 'Insert into ' + @sNewDbOwner + 'LgWritingSystem (
	[Id],
	[Code],
	[Locale],
	[DefaultMonospace],
	[DefaultSansSerif],
	[DefaultSerif],
	[FontVariation],
	[KeyboardType],
	[RightToLeft]
	)
SELECT
	e.[Id],
	e.[Encoding],
	e.[Locale],
	''DefaultMonospace'' = CASE
		WHEN CHARINDEX('';'', ws.RendererInit) > 0 THEN
			--( 2nd substring argument: one off second semicolon position
			SUBSTRING(ws.RendererInit,
				CHARINDEX('';'', ws.RendererInit, CHARINDEX('';'', ws.RendererInit) + 1) + 1,
				LEN(ws.RendererInit))
		ELSE
			NULL
		END,
	''DefaultSansSerif'' = CASE
		--( 3rd substring agrument: get the second semicolon position, then subtract
		--( the first semicolon position
		WHEN CHARINDEX('';'', ws.RendererInit) > 0 THEN
			SUBSTRING(ws.RendererInit, CHARINDEX('';'', ws.RendererInit) + 1,
				CHARINDEX('';'', ws.RendererInit, CHARINDEX('';'', ws.RendererInit) + 1) -
				CHARINDEX('';'', ws.RendererInit) - 1)
		ELSE
			NULL
		END,
	''DefaultSerif'' = CASE
		WHEN CHARINDEX('';'', ws.RendererInit) > 0 THEN
			SUBSTRING(ws.RendererInit, 1, CHARINDEX('';'', ws.RendererInit) - 1)
		ELSE
			NULL
		END,
	--( No one in the field should have a graphite engine installed, but here it is, anyway.
	''FontVariation'' = CASE --( Note that we''re checking for a colon here, not a semicolon
		WHEN CHARINDEX('':'', ws.RendererInit) > 0 THEN
			SUBSTRING(ws.RendererInit, CHARINDEX('':'', ws.RendererInit) + 1, LEN(ws.RendererInit))
		ELSE
			NULL
		END,
	ws.[KeyboardType],
	ws.[RightToLeft]
FROM LgWritingSystem ws
JOIN LgEncoding e ON ws.[Id] = e.[WritingSystemDef]'

exec (@sQry)

--( Change Owners
--(---------------------------------------------
--( Steve McConnel and Ken Zook assure us the only thing old LgWritingSystem
--( owns is Collations. The interface doesn't support custom fields on old
--( LgWritingSystem, and no one should mess with the XML. The old field ID for
--( Collation was 25016. The new field ID is 24018.

-- omitting the @sNewDbOwner from the join is crucial for some bizarre SQLish reason

PRINT 'Moving ownership of LgCollation records'

set @sQry = 'update ' + @sNewDbOwner + 'CmObject
set Owner$ = ows.Owner$, OwnFlid$ = 24018
from ' + @sNewDbOwner + 'CmObject coll
join CmObject ows on coll.Owner$ = ows.[Id]
where coll.OwnFlid$ = 25016'
exec (@sQry)

-- Remove old LgWritingSystem objects from the updated database

set @sQry = 'DELETE FROM ' + @sNewDbOwner + 'CmObject
	where Class$ = 25'
exec (@sQry)

--== Copy rows from Field$ for custom fields. ==-

-- The following code should do this but fails
--
--set @sQry = 'Insert into ' + @sNewDbOwner + 'Field$
--	select * from Field$ where custom <> 0'
--exec (@sQry)

DECLARE custFldCursor CURSOR FOR
	select [id] from Field$ where custom <> 0
OPEN custFldCursor
FETCH NEXT FROM custFldCursor INTO @CustFldId
WHILE @@FETCH_STATUS = 0
BEGIN
	set @sQry = 'Insert into ' + @sNewDbOwner + 'Field$
		select * from Field$ where [id] = ' + @CustFldId
	exec (@sQry)
	FETCH NEXT FROM custFldCursor INTO @CustFldId
END

CLOSE custFldCursor
DEALLOCATE custFldCursor

--== Copy fields of tables representing conceptual model classes ==--

DECLARE clsCursor CURSOR FOR
	select Name
	from class$
	where [name] != 'CmObject' and [name] != 'LgEncoding' and [name] != 'LgWritingSystem'
	order by Base, Name

OPEN clsCursor
FETCH NEXT FROM clsCursor INTO @clsName
WHILE @@FETCH_STATUS = 0
BEGIN
	set @modClsName = @clsName
	if (@clsName = 'CmAnalyzingAgent')
		set @modClsName = 'CmAgent'
	else if (@clsName = 'MoMorphoSyntaxInfo')
		set @modClsName = 'MoMorphoSyntaxAnalysis'
	else if (@clsName = 'MoDerivationalAffixMsi')
		set @modClsName = 'MoDerivationalAffixMsa'
	else if (@clsName = 'MoDerivationalStepMsi')
		set @modClsName = 'MoDerivationalStepMsa'
	else if (@clsName = 'MoInflectionalAffixMsi')
		set @modClsName = 'MoInflectionalAffixMsa'
	else if (@clsName = 'MoStemMsi')
		set @modClsName = 'MoStemMsa'

	--( Get field names

	DECLARE fldCursor CURSOR FOR
		SELECT sc.Name
		from syscolumns sc
		join sysobjects so on sc.[id] = so.[id]	and so.xtype = 'U' and so.[name] = @clsName

	set @sFldNamesIn = null
	set @sFldNamesOut = null

	OPEN fldCursor
	if (@@FETCH_STATUS = 0)
	  FETCH NEXT FROM fldCursor INTO @fldName

	WHILE @@FETCH_STATUS = 0
	BEGIN
	  set @modFldName = @fldName

	  if @clsName = 'LexSense' AND @fldName = 'MorphoSyntaxInfo'
		set @modFldName = 'MorphoSyntaxAnalysis'
	  else if @clsName = 'MoDerivation' AND @fldName = 'StemMsi'
		set @modFldName = 'StemMsa'
	  else if (@clsName = 'MoDerivationalAffixApp' OR @clsName = 'MoInflAffixSlotApp') AND @fldName = 'AffixMsi'
		set @modFldName = 'AffixMsa'
	  else if @clsName = 'MoMorphType' AND (@fldName = 'Postfix_Fmt' OR @fldName = 'Prefix_Fmt') Begin
		set @modFldName = ''
		set @fldName = ''
	  End
	  else if (@fldName = 'PrimaryEnc')
		set @modFldName = 'PrimaryWs'
	  else if (@fldName = 'SecondaryEnc')
		set @modFldName = 'SecondaryWs'
	  else if (@fldName = 'TertiaryEnc')
		set @modFldName = 'TertiaryWs'
	  if (@fldName = 'PrimaryWs' OR @fldName = 'SecondaryWs' OR @fldName = 'TertiaryWs') Begin
		set @modFldName = ''
		set @fldName = ''
	  End
	  else if (@fldName = 'Encoding')
		set @modFldName = 'WritingSystem'

	  set @fldName = '[' + @fldName + ']'
	  set @modFldName = '[' + @modFldName + ']'

		--( Now concatenate the field names to strings for the query.

	  if (@SFldNamesIn is null) begin -- null first time
		set @sFldNamesIn = @fldName
		set @sFldNamesOut = @modFldName
	  end
	  else begin
		if (@fldName != '[]')
				set @sFldNamesIn = @sFldNamesIn + ', ' + @fldName
		if (@modFldName != '[]')
				set @sFldNamesOut = @sFldNamesOut + ', ' + @modFldName
	  end

	  FETCH NEXT FROM fldCursor INTO @fldName
	END

	CLOSE fldCursor
	DEALLOCATE fldCursor

	set @sQry = 'insert into ' + @sNewDbOwner + @modClsName + ' (' + @sFldNamesOut + ') select ' + @sFldNamesIn + ' from ' + @clsName
	exec (@sQry)
	FETCH NEXT FROM clsCursor INTO @clsName
END

CLOSE clsCursor
DEALLOCATE clsCursor

-- Copy joiner tables

DECLARE clsCursor CURSOR FOR
	SELECT [Name]
	FROM sysobjects so
	where not exists (select * from class$ cd where cd.[name] = so.[name])
		and xtype = 'U' AND [Name] NOT LIKE '%$%'
	ORDER BY [Name]

OPEN clsCursor
FETCH NEXT FROM clsCursor INTO @clsName
WHILE @@FETCH_STATUS = 0
BEGIN
	set @modClsName = @clsName
	if (@clsName = 'MoMorphoSyntaxInfo_Components')
		set @modClsName = 'MoMorphoSyntaxAnalysis_Components'
	else if (@clsName = 'WfiAnalysis_Msis')
		set @modClsName = 'WfiAnalysis_Msas'
	else if (@clsName = 'LanguageProject_AnalysisEncodings')
		set @modClsName = 'LanguageProject_AnalysisWritingSystems'
	else if (@clsName = 'LanguageProject_CurrentVernacularEncs')
		set @modClsName = 'LanguageProject_CurrentVernacularWritingSystems'
	else if (@clsName = 'LanguageProject_CurrentPronunciationEncs')
		set @modClsName = 'LanguageProject_CurrentPronunciationWritingSystems'
	else if (@clsName = 'LanguageProject_CurrentAnalysisEncs')
		set @modClsName = 'LanguageProject_CurrentAnalysisWritingSystems'
	else if (@clsName = 'LanguageProject_VernacularEncodings')
		set @modClsName = 'LanguageProject_VernacularWritingSystems'

	set @sQry =  'insert into ' + @sNewDbOwner + @modClsName +' select * from ' + @clsName
	exec (@sQry)
	FETCH NEXT FROM clsCursor INTO @clsName
END

CLOSE clsCursor
DEALLOCATE clsCursor

--== Install some default values ==--

SET @sQry = 'UPDATE ' + @sNewDbOwner +
	'CmPossibilityList SET [WritingSystem] = -6 WHERE [ItemClsid] IN (12, 13)'
EXECUTE (@sQry)

SET @sQry = 'UPDATE ' + @sNewDbOwner +
	'CmPossibilityList SET [WritingSystem] = -3 WHERE [ItemClsid] NOT IN (12, 13)'
EXECUTE (@sQry)

--( For UserViewFields, set all to a default, then get more specific.

SET @sQry = 'UPDATE ' + @sNewDbOwner + 'UserViewField SET [WritingSystem] = -1'
EXECUTE (@sQry)

SET @sQry = 'UPDATE ' + @sNewDbOwner + 'UserViewField SET [WritingSystem] = -3 ' +
'FROM ' + @sNewDbOwner + 'UserViewField uvf ' +
'JOIN ' + @sNewDbOwner + 'Field$ f ON uvf.[flid] = f.[id] where uvf.type in (1,2)'
EXECUTE (@sQry)

SET @sQry = 'UPDATE ' + @sNewDbOwner + 'UserViewField SET [WritingSystem] = -6 ' +
'FROM ' + @sNewDbOwner + 'UserViewField uvf ' +
'JOIN ' + @sNewDbOwner + 'Field$ f ON uvf.[flid] = f.[id] where f.[dstcls] in (12,13)'
EXECUTE (@sQry)

-- People and Location name/abbr views need writing system to show vernacular then analysis.
-- This includes the RnEvent_Participant field.
SET @sQry = 'UPDATE ' + @sNewDbOwner + 'UserViewField SET [WritingSystem] = -6 ' +
'where id in ' +
'(SELECT uvf.id FROM ' + @sNewDbOwner + 'UserViewField_ uvf ' +
'JOIN ' + @sNewDbOwner + 'UserViewRec uvr ON uvr.id = uvf.owner$ ' +
'WHERE (uvf.flid in (7001,7002) and uvr.clsid in (12,13)) or uvf.flid = 4006002)'
EXECUTE (@sQry)

--== Cleanup ==--

--( Turn on all constraints now we are done.

set @sQry = @sNewDbOwner + 'ManageConstraints$'
exec @sQry  null, 'F', 'CHECK'

GO