/*************************************************************************
** V150toV151.sql
*************************************************************************/

DECLARE
	@nvcNewDbName nvarchar(1000),
	@nvcClassName nvarchar(1000),
	@nvcFieldName nvarchar(1000),
	@nvcNewFieldName nvarchar(1000),
	@sFieldNamesIn nvarchar(4000),
	@sFieldNamesOut nvarchar(4000),
	@sQry nvarchar(4000),
	@sNewDbOwner nvarchar(1000),
	@nFieldId int,
	@nEnglishWs int

set @nvcNewDbName = 'Version151DataMigration'
set @sNewDbOwner = @nvcNewDbName + '.dbo.'

-- Turn off all constraints for the duration.

set @sQry = @sNewDbOwner + 'ManageConstraints$'
exec @sQry  null, 'F', 'NOCHECK'

-- Copy CmObject fields (except UpdStmp, which can't be copied)

set @sQry = N'SET IDENTITY_INSERT ' + @sNewDbOwner + N'CmObject ON
	INSERT INTO ' + @sNewDbOwner + N'
		CmObject (Id,Guid$,Class$,Owner$,OwnFlid$,OwnOrd$,UpdDttm)
		select Id,Guid$,Class$,Owner$,OwnFlid$,OwnOrd$,UpdDttm from CmObject
	SET IDENTITY_INSERT ' + @sNewDbOwner + N'CmObject OFF'
exec (@sQry)

--== Copy Multilingual string tables. ==--

select @nEnglishWs = [id] from LgWritingSystem where Code = 740664001

set @sQry = N'Insert into ' + @sNewDbOwner + N'
	MultiStr$ (Flid, Obj, Ws, Txt, Fmt)
	select ms.flid, ms.obj, isnull(ws.id, ' + str(@nEnglishWs) + N'), ms.txt, ms.fmt
	from multiStr$ ms
	left outer join LgWritingSystem ws on ws.Code = ms.Ws'
exec (@sQry)

set @sQry = N'Insert into ' + @sNewDbOwner + N'
	MultiBigStr$ (Flid, Obj, Ws, Txt, Fmt)
	select mbs.flid, mbs.obj, isnull(ws.id, ' + str(@nEnglishWs) + N'), mbs.txt, mbs.fmt
	from multiBigStr$ mbs
	left outer join LgWritingSystem ws on ws.Code = mbs.Ws'
exec (@sQry)

set @sQry = N'Insert into ' + @sNewDbOwner + N'
	MultiTxt$ (Flid, Obj, Ws, Txt)
	select mt.flid, mt.obj, isnull(ws.id, ' + str(@nEnglishWs) + N'), mt.txt
	from multiTxt$ mt
	left outer join LgWritingSystem ws on ws.Code = mt.Ws'
exec (@sQry)

set @sQry = 'Insert into ' + @sNewDbOwner + N'
	MultiBigTxt$ (Flid, Obj, Ws, Txt)
	select mbt.flid, mbt.obj, isnull(ws.id, ' + str(@nEnglishWs) + N'), mbt.txt
	from multiBigTxt$ mbt
	left outer join LgWritingSystem ws on ws.Code = mbt.Ws'
exec (@sQry)

--== Copy rows from Field$ for custom fields. ==-

DECLARE custFldCursor CURSOR FOR
	select [id] from Field$ where custom <> 0
OPEN custFldCursor
FETCH NEXT FROM custFldCursor INTO @nFieldId
WHILE @@FETCH_STATUS = 0
BEGIN
	set @sQry = 'Insert into ' + @sNewDbOwner + 'Field$
		select * from Field$ where [id] = ' + str(@nFieldId)
	exec (@sQry)
	FETCH NEXT FROM custFldCursor INTO @nFieldId
END

CLOSE custFldCursor
DEALLOCATE custFldCursor

--== Copy fields of tables representing conceptual model classes ==--

DECLARE clsCursor CURSOR FOR
	select Name
	from class$
	where [name] != 'CmObject' --and [name] != 'LgEncoding' and [name] != 'LgWritingSystem'
	order by Base, Name

OPEN clsCursor
FETCH NEXT FROM clsCursor INTO @nvcClassName
WHILE @@FETCH_STATUS = 0 BEGIN
	--( Get field names

	DECLARE fldCursor CURSOR FOR
		SELECT sc.Name
		from syscolumns sc
		join sysobjects so on sc.[id] = so.[id]	and so.xtype = 'U' and so.[name] = @nvcClassName

	set @sFieldNamesIn = null
	set @sFieldNamesOut = null
	OPEN fldCursor
	FETCH NEXT FROM fldCursor INTO @nvcFieldName
	WHILE @@FETCH_STATUS = 0 BEGIN
		set @nvcNewFieldName = @nvcFieldName

		-- The code field was deleted from the LgWritingSystem and LgCollation tables.
		if ((@nvcClassName = 'LgWritingSystem' OR @nvcClassName = 'LgCollation') AND @nvcFieldName = 'Code') begin
			set @nvcFieldName = ''
			set @nvcNewFieldName = ''
		end
		else if (@nvcClassName = 'CmBaseAnnotation' and @nvcFieldName = 'LgWritingSystem') begin
			set @nvcNewFieldName = 'WritingSystem'
		end

		set @nvcFieldName = '[' + @nvcFieldName + ']'
		set @nvcNewFieldName = '[' + @nvcNewFieldName + ']'

		--( Now concatenate the field names to strings for the query.

		if (@sFieldNamesIn is null) begin -- null first time
			if (@nvcFieldName != '[]')
				set @sFieldNamesIn = @nvcFieldName
			if (@nvcNewFieldName != '[]')
				set @sFieldNamesOut = @nvcNewFieldName
		end
		else begin
			if (@nvcFieldName != '[]')
				set @sFieldNamesIn = @sFieldNamesIn + ', ' + @nvcFieldName
			if (@nvcNewFieldName != '[]')
				set @sFieldNamesOut = @sFieldNamesOut + ', ' + @nvcNewFieldName
		end

		FETCH NEXT FROM fldCursor INTO @nvcFieldName
	END

	CLOSE fldCursor
	DEALLOCATE fldCursor

	set @sQry = 'insert into ' + @sNewDbOwner + @nvcClassName + ' (' + @sFieldNamesOut + ') select ' + @sFieldNamesIn + ' from ' + @nvcClassName
	print '@sQry = ' + rtrim(@sQry)
	exec (@sQry)
	FETCH NEXT FROM clsCursor INTO @nvcClassName
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
FETCH NEXT FROM clsCursor INTO @nvcClassName
WHILE @@FETCH_STATUS = 0
BEGIN
	set @sQry =  'insert into ' + @sNewDbOwner + @nvcClassName +' select * from ' + @nvcClassName
	exec (@sQry)
	FETCH NEXT FROM clsCursor INTO @nvcClassName
END

CLOSE clsCursor
DEALLOCATE clsCursor

--== Some Value Updates ==--

--( Collations
SET @sQry = N'UPDATE ' + @sNewDbOwner + N'CmSortSpec
	set [PrimaryCollation] = null
	where [PrimaryCollation] = 0'
EXECUTE (@sQry)

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'CmSortSpec
	set [SecondaryCollation] = null
	where [SecondaryCollation] = 0'
EXECUTE (@sQry)

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'CmSortSpec
	set [TertiaryCollation] = null
	where [TertiaryCollation] = 0'
EXECUTE (@sQry)

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'CmSortSpec
	set [PrimaryCollation] = [PrimaryCollation] - 1
	where [PrimaryCollation] is not null and [PrimaryCollation] != 0'
EXECUTE (@sQry)

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'CmSortSpec
	set [SecondaryCollation] = [SecondaryCollation] - 1
	where [SecondaryCollation] is not null and [SecondaryCollation] != 0'
EXECUTE (@sQry)

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'CmSortSpec
	set [TertiaryCollation] = [TertiaryCollation] - 1
	where [TertiaryCollation] is not null and [TertiaryCollation] != 0'
EXECUTE (@sQry)


--( Writing Systems
SET @sQry = N'UPDATE ' + @sNewDbOwner + N'CmSortSpec
	set ' + @sNewDbOwner + N'CmSortSpec.PrimaryWs = isnull(lws.Id, ' + str(@nEnglishWs) + N')
	from ' + @sNewDbOwner + N'CmSortSpec css, LgWritingSystem lws
	where lws.Code = css.PrimaryWs and css.PrimaryWs != 0'
print '@sQry = ' + rtrim(@sQry)
EXECUTE (@sQry)

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'CmSortSpec
	set ' + @sNewDbOwner + N'CmSortSpec.SecondaryWs = isnull(lws.Id, ' + str(@nEnglishWs) + N')
	from ' + @sNewDbOwner + N'CmSortSpec css, LgWritingSystem lws
	where lws.Code = css.SecondaryWs and css.SecondaryWs != 0'
print '@sQry = ' + rtrim(@sQry)
EXECUTE (@sQry)

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'CmSortSpec
	set ' + @sNewDbOwner + N'CmSortSpec.TertiaryWs = isnull(lws.Id, ' + str(@nEnglishWs) + N')
	from ' + @sNewDbOwner + N'CmSortSpec css, LgWritingSystem lws
	where lws.Code = css.TertiaryWs and css.TertiaryWs != 0'
print '@sQry = ' + rtrim(@sQry)
EXECUTE (@sQry)

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'CmSortSpec
	set PrimaryWs = null
	where PrimaryWs = 0'
EXECUTE (@sQry)

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'CmSortSpec
	set SecondaryWs = null
	where SecondaryWs = 0'
EXECUTE (@sQry)

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'CmSortSpec
	set TertiaryWs = null
	where TertiaryWs = 0'
EXECUTE (@sQry)

--( Writing System
SET @sQry = N'UPDATE ' + @sNewDbOwner + N'ScrImportMapping
	set ' + @sNewDbOwner + N'ScrImportMapping.WritingSystem = isnull(lws.Id, ' + str(@nEnglishWs) + N')
	from ' + @sNewDbOwner + N'ScrImportMapping sim, LgWritingSystem lws
	where lws.Code = sim.WritingSystem and sim.[WritingSystem] != 0'
print '@sQry = ' + rtrim(@sQry)
EXECUTE (@sQry)

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'ScrImportMapping
	set [WritingSystem] = null
	where [WritingSystem] = 0'
EXECUTE (@sQry)

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'ReversalIndex
	set ' + @sNewDbOwner + N'ReversalIndex.WritingSystem = isnull(lws.Id, ' + str(@nEnglishWs) + N')
	from ' + @sNewDbOwner + N'ReversalIndex ri, LgWritingSystem lws
	where lws.Code = ri.WritingSystem
		and ri.[WritingSystem] != 0'
print '@sQry = ' + rtrim(@sQry)
EXECUTE (@sQry)

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'ReversalIndex
	set [WritingSystem] = null
	where [WritingSystem] = 0'
EXECUTE (@sQry)

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'WordformLookupList
	set ' + @sNewDbOwner + N'WordformLookupList.WritingSystem = isnull(lws.Id, ' + str(@nEnglishWs) + N')
	from ' + @sNewDbOwner + N'WordformLookupList wll, LgWritingSystem lws
	where lws.Code = wll.WritingSystem
		and wll.[WritingSystem] != 0'
print '@sQry = ' + rtrim(@sQry)
EXECUTE (@sQry)

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'WordformLookupList
	set [WritingSystem] = null
	where [WritingSystem] = 0'
EXECUTE (@sQry)

--( Writing System to WritingSystem or WsSelector

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'CmPossibilityList
	set WsSelector =  ' + @sNewDbOwner + N'CmPossibilityList.WritingSystem,
	WritingSystem = null
	where  ' + @sNewDbOwner + N'CmPossibilityList.WritingSystem < 0'
EXECUTE (@sQry)


SET @sQry = N'UPDATE ' + @sNewDbOwner + N'UserViewField
	set WsSelector =  ' + @sNewDbOwner + N'UserViewField.WritingSystem,
	WritingSystem = null
	where  ' + @sNewDbOwner + N'UserViewField.WritingSystem < 0'
EXECUTE (@sQry)

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'CmBaseAnnotation
	set WsSelector =  ' + @sNewDbOwner + N'CmBaseAnnotation.WritingSystem,
	WritingSystem = null
	where  ' + @sNewDbOwner + N'CmBaseAnnotation.WritingSystem < 0'
EXECUTE (@sQry)EXECUTE (@sQry)

SET @sQry = N'UPDATE ' + @sNewDbOwner + N'FsOpenFeature
	set WsSelector =  ' + @sNewDbOwner + N'FsOpenFeature.WritingSystem,
	WritingSystem = null
	where  ' + @sNewDbOwner + N'FsOpenFeature.WritingSystem < 0'
EXECUTE (@sQry)

--== Cleanup ==--

--( Turn on all constraints now we are done.

set @sQry = @sNewDbOwner + 'ManageConstraints$'
exec @sQry  null, 'F', 'CHECK'

GO