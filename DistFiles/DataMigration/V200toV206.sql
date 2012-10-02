/*************************************************************************
** V200toV206.sql
*************************************************************************/
-- note this is called with an implicit USE command set to the old database.
DECLARE
	@nvcNewDbName nvarchar(1000),
	@nvcClassName nvarchar(1000),
	@nvcFieldName nvarchar(1000),
	@nvcNewFieldName nvarchar(1000),
	@sFieldNamesIn nvarchar(4000),
	@sFieldNamesOut nvarchar(4000),
	@sQry nvarchar(4000),
	@sQry2 NVARCHAR(4000),
	@nvcTableName nvarchar(1000),
	@sNewDb nvarchar(1000),
	@nFieldId int,
	@nEnglishWs int,
	@dToday datetime,
	@guidWhatever uniqueidentifier,
	@nOwnerId INT,
	@nId INT,
	@nPossListId INT

--( Using these variables in dynamic SQL  was a  good idea.
--( However, I haven't yet figured away to use them in cursors
--( nor in setting a variable with the TOP 1 result set.
--( When you change the new db name here, be sure to change it
--( elsewhere in the program.

set @nvcNewDbName = 'Version206DataMigration'
set @sNewDb = @nvcNewDbName + '.dbo.'

-- Turn off all constraints for the duration.

set @sQry = @sNewDb + 'ManageConstraints$'
exec @sQry  null, 'F', 'NOCHECK'

--==( Copy CmObject fields )==--

--(except UpdStmp, which can't be copied)

set @sQry = N'SET IDENTITY_INSERT ' + @sNewDb + N'CmObject ON
	INSERT INTO ' + @sNewDb + N'
		CmObject (Id,Guid$,Class$,Owner$,OwnFlid$,OwnOrd$,UpdDttm)
		select Id,Guid$,Class$,Owner$,OwnFlid$,OwnOrd$,UpdDttm from CmObject
	SET IDENTITY_INSERT ' + @sNewDb + N'CmObject OFF'
exec (@sQry)

--== Copy Multilingual string tables. ==--

select @nEnglishWs = [id] from LgWritingSystem where ICULocale = N'en'

set @sQry = N'Insert into ' + @sNewDb + N'
	MultiStr$ (Flid, Obj, Ws, Txt, Fmt)
	select ms.flid, ms.obj, isnull(ws.[id], ' + str(@nEnglishWs) + N'), ms.txt, ms.fmt
	from multiStr$ ms
	left outer join LgWritingSystem ws on ws.[Id] = ms.Ws'
exec (@sQry)

set @sQry = N'Insert into ' + @sNewDb + N'
	MultiBigStr$ (Flid, Obj, Ws, Txt, Fmt)
	select mbs.flid, mbs.obj, isnull(ws.id, ' + str(@nEnglishWs) + N'), mbs.txt, mbs.fmt
	from multiBigStr$ mbs
	left outer join LgWritingSystem ws on ws.[Id] = mbs.Ws'
exec (@sQry)

--( This is where MultiTxt was. It isn't going to be here anymore.

set @sQry = 'Insert into ' + @sNewDb + N'
	MultiBigTxt$ (Flid, Obj, Ws, Txt)
	select mbt.flid, mbt.obj, isnull(ws.id, ' + str(@nEnglishWs) + N'), mbt.txt
	from multiBigTxt$ mbt
	left outer join LgWritingSystem ws on ws.[Id] = mbt.Ws'
exec (@sQry)

--==( Copy rows from Field$ for custom fields. )==-
DECLARE @stmin nvarchar(11), @stmax nvarchar(11), @stbig nvarchar(11), @stDstCls nvarchar(11)
DECLARE @id int, @type int, @class int, @dstcls int, @name nvarchar(1000), @custom int, @customid uniqueidentifier, @min int, @max int, @big bit
DECLARE fldCursor CURSOR local static forward_only read_only FOR
	SELECT [Id] FROM Field$ WHERE custom = 1
OPEN fldCursor
FETCH NEXT FROM fldCursor INTO @nId
WHILE @@FETCH_STATUS = 0
BEGIN
	select @id=[Id], @type=[Type], @class=[Class], @dstcls=[DstCls], @name=[Name], @custom=[Custom],
		@customid=[CustomId], @min=[Min], @max=[Max], @big=[Big] from Field$ where id = @nId
	if (@dstcls is null) set @stDstCls = 'null' else set @stDstCls = CONVERT(nvarchar(11), @dstCls)
	if (@min is null) set @stmin = 'null' else set @stmin = CONVERT(nvarchar(11), @min)
	if (@max is null) set @stmax = 'null' else set @stmax = CONVERT(nvarchar(11), @max)
	if (@big is null) set @stbig = 'null' else set @stbig = CONVERT(nvarchar(11), @big)
	set @sQry = 'insert into ' + @sNewDb + 'Field$
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values (' + CONVERT(nvarchar(11), @id) + ', ' + CONVERT(nvarchar(11), @type) + ', ' +
		CONVERT(nvarchar(11), @class) + ', ' + @stDstCls + ', ''' + @name +
		''', ' + CONVERT(nvarchar(4), @custom) + ', ''' + CONVERT(nvarchar(40), @customid) + ''', ' +
		@stmin + ', ' + @stmax + ', ' + @stbig + ')'
	print (@sQry)
	exec (@sQry)
	FETCH NEXT FROM fldCursor INTO @nId
END

CLOSE fldCursor
DEALLOCATE fldCursor

--==( Copy fields of tables representing conceptual model classes )==--

-- Made CmAnnotationDefn a subclass of CmPossibility (Name and Description deleted from CmAnnotationDefn
-- so their data must move to the corresponding superclass fields).
set @sQry = 'insert into ' + @sNewDb + 'CmPossibility (Id)' + CHAR(13) + CHAR(10) +
	' select Id from CmAnnotationDefn'
print '@sQry = ' + rtrim(@sQry)
exec (@sQry)

-- 35001 == kflidCmAnnotationDefn_Name, 35002 == kflidCmAnnotationDefn_Description
set @sQry = 'insert into ' + @sNewDb + 'CmPossibility_Name (Obj, Ws, Txt)' + CHAR(13) + CHAR(10) +
	' select Obj, Ws, Txt from MultiTxt$' + CHAR(13) + CHAR(10) +
	' where Flid = 35001'
print '@sQry = ' + rtrim(@sQry)
exec (@sQry)

-- Some C++ code will have to handle generating default values for the Description's Fmt field.
set @sQry = 'insert into ' + @sNewDb + 'MultiStr$ (Obj, Flid, Ws, Txt, Fmt)' + CHAR(13) + CHAR(10) +
	' select Obj, 7003, Ws, Txt, null from MultiTxt$' + CHAR(13) + CHAR(10) +
	' where Flid = 35002'
print '@sQry = ' + rtrim(@sQry)
exec (@sQry)

--( This block gets the bulk of the data from the old database into the new one.
--( The outer loop goes through CmObject, which has class names, and therefore
--( the table names. The inner loop puts together a string of fields to copy from
--( the old to the new. Fields that are dropped from the old are excluded by
--( virtue of the query. Fields that have changed names will also be dropped, and
--( must be handled elsewhere. Fields that are new don't exist in the old, and so can
--( be safely ignored. This string is concatenated to another to produce an
--( INSERT INTO string at the bottom of the loop.

DECLARE clsCursor CURSOR local static forward_only read_only FOR
	select Name
	from class$
	where [name] != 'CmObject'
	order by Base, Name

OPEN clsCursor
FETCH NEXT FROM clsCursor INTO @nvcClassName
WHILE @@FETCH_STATUS = 0 BEGIN

	--( Get field names. The join condition drops out fields in the old database
	--( that are no longer needed.
	DECLARE fldCursor CURSOR local static forward_only read_only FOR
		SELECT sc.Name
		from syscolumns sc
		join sysobjects so on sc.[id] = so.[id]
			and so.xtype = 'U' and so.[name] = @nvcClassName
		join Version206DataMigration.dbo.syscolumns sc1 on sc1.[Name] = sc.[Name]
		join Version206DataMigration.dbo.sysobjects so1 on so1.[Name] = so.[Name]
			and so1.[id] = sc1.[id] and so1.xtype = 'U'

	set @sFieldNamesIn = null
	set @sFieldNamesOut = null
	OPEN fldCursor
	FETCH NEXT FROM fldCursor INTO @nvcFieldName
	WHILE @@FETCH_STATUS = 0 BEGIN
		set @nvcNewFieldName = @nvcFieldName

		--( We can change field names here, An example:
		--(
		--( if (@nvcClassName = 'StFootnote' and @nvcFieldName = 'DisplayBackReference') begin
		--(		set @nvcNewFieldName = 'DisplayFootnoteReference'
		--( end

		set @nvcFieldName = '[' + @nvcFieldName + ']'
		set @nvcNewFieldName = '[' + @nvcNewFieldName + ']'

		--( Nothing needs to be done for the following:
		--(
		--( LexEntry_Pronunciations went from a collection to atomic. It is a view against
		--( CmObject. No data exists.
		--(
		--( LexEntry_OrthographicVariants is removed. It was converted from MultiTxt$ in v.2
		--( to its own table in v.2.2. No data exists.
		--(
		--( StTxtPara_Translations went from a reference to an owning. Or in db terms,
		--( from its own table to a view against CmObject. No data
		--( Now concatenate the field names to strings for the query.

		if (@nvcFieldName != '[]' and @nvcNewFieldName != '[]') begin
			if (@sFieldNamesIn is null) begin -- null first time
				set @sFieldNamesIn = @nvcFieldName
				set @sFieldNamesOut = @nvcNewFieldName
			end
			else begin
				set @sFieldNamesIn = @sFieldNamesIn + ', ' + @nvcFieldName
				set @sFieldNamesOut = @sFieldNamesOut + ', ' + @nvcNewFieldName
			end
		end

		FETCH NEXT FROM fldCursor INTO @nvcFieldName
	END

	CLOSE fldCursor
	DEALLOCATE fldCursor

	set @sQry = 'insert into ' + @sNewDb + @nvcClassName + ' (' + @sFieldNamesOut +
		') select ' + @sFieldNamesIn + ' from ' + @nvcClassName
	print '@sQry = ' + rtrim(@sQry)
	exec (@sQry)
	FETCH NEXT FROM clsCursor INTO @nvcClassName
END

CLOSE clsCursor
DEALLOCATE clsCursor

--==( Copy joiner tables )==--

--( The joiner tables are field types 26 and 28, for the class reference
--( relations. The new database is joined in to drop any deleted fields.


DECLARE tblCursor CURSOR local static forward_only read_only FOR
	SELECT c.[Name] + '_' + f.[Name]
	FROM Field$ f
	JOIN Class$ c on c.[Id] = f.[Class]
	JOIN Version206DataMigration.dbo.Field$ f1 on f1.[Id] = f.[Id] -- don't copy deleted fields!
	WHERE f.Type in (26,28)
	ORDER BY c.[Name], f.[Name];

OPEN tblCursor
FETCH NEXT FROM tblCursor INTO @nvcTableName
WHILE @@FETCH_STATUS = 0
BEGIN
	set @sQry =  'insert into ' + @sNewDb + @nvcTableName + ' select * from ' + @nvcTableName
	exec (@sQry)
	FETCH NEXT FROM tblCursor INTO @nvcTableName
END

CLOSE tblCursor
DEALLOCATE tblCursor

--==( Some Value Updates )==--

--( OK, we have all the data brought across. Now it's time to make
--( any necessary data changes.

--( Copy from the old MultiTxt$ table into the new individual field tables.
DECLARE
	@nRowCount INT,
	@nFlid INT,
	@nvcSql NVARCHAR(200)

SELECT TOP 1 @nFlid = [Id]
FROM Field$
WHERE Type = 16
ORDER BY [Id]
SET @nRowCount = @@ROWCOUNT
WHILE @nRowCount > 0 BEGIN
	-- 35001 = kflidCmAnnotationDefn_Name, 35002 = kflidCmAnnotationDefn_Description
	if (@nFlid != 35001 AND @nFlid != 35002) begin
		EXEC Version206DataMigration.dbo.GetMultiTableName @nFlid, @nvcTableName OUTPUT

		SET @nvcSql = N'INSERT INTO ' + @sNewDb + @nvcTableName + CHAR(13) +
			N'SELECT Obj, Ws, Txt FROM MultiTxt$ WHERE Flid = ' + CONVERT(nvarchar(11), @nFlid)
		EXECUTE (@nvcSql)
	end

	SELECT TOP 1 @nFlid = [Id]
	FROM Field$
	WHERE Type = 16 AND [Id] > @nFlid
	ORDER BY [Id]

	SET @nRowCount = @@ROWCOUNT
END

-- Now switch all operations to the new temporary table since we've already copied the data from
-- the old original database.
use [Version206DataMigration]



--( Fix the superclass of class Text

INSERT INTO CmMajorObject ([Id]) SELECT [Id] FROM [Text]

--( LexMinorEntry.MainEntryOrSense has a changed signature,
--( but the data should not be in the field yet.

--( LexSubEntry.MainEntriesOrSenses has a changed signature,
--( but the data should not be in the field yet.

--( Add 2 CmAgent objects to the AnalyzingAgents field of the LanguageProject.
--( This was done somewhere around the ubiquitous version 200002, so we first
--( look to see if they already exist.

DECLARE @nAgent INT
SELECT @nAgent = COUNT(*) FROM LanguageProject_AnalyzingAgents
IF @nAgent = 0 BEGIN
	DECLARE @hvoAgent INT,
		@guid UNIQUEIDENTIFIER,
		@hvoLangProj INT,
		@wsEng INT,
		@clidCmAgent INT,
		@clidStText INT,
		@clidStTxtPara INT,
		@flidLanguageProject_AnalyzingAgents INT,
		@flidCmAgent_Notes INT,
		@flidStText_Paragraphs INT,
		@cptOwningAtom INT,
		@cptOwningCollection INT,
		@cptOwningSequence INT,
		@hvoText INT,
		@hvoPara INT

	SET @clidCmAgent = 23
	SET @flidLanguageProject_AnalyzingAgents = 6001038
	SET @flidCmAgent_Notes = 23004
	SET @flidStText_Paragraphs = 14001
	SET @cptOwningAtom = 23
	SET @cptOwningCollection = 25
	SET @cptOwningSequence = 27
	SET @clidStText = 14
	SET @clidStTxtPara = 16

	SELECT @wsEng = Id FROM LgWritingSystem WHERE ICULocale = N'en'
	SELECT @hvoLangProj = [Id] FROM LanguageProject

	DECLARE @agentName nvarchar(1000)
	-- Create the Human agent.
	EXEC CreateOwnedObject$ @clidCmAgent, @hvoAgent out, @guid out, @hvoLangProj,
			@flidLanguageProject_AnalyzingAgents, @cptOwningCollection

	SET @agentName = N'default user'
	UPDATE CmAgent SET Human = 1 WHERE [Id] = @hvoAgent
	INSERT INTO CmAgent_Name (Obj, Ws, Txt)
		VALUES (@hvoAgent, @wsEng, @agentName)
	set @guid = null
	EXEC CreateOwnedObject$ @clidStText, @hvoText out, @guid out, @hvoAgent,
			@flidCmAgent_Notes, @cptOwningAtom
	set @guid = null
	EXEC CreateOwnedObject$ @clidStTxtPara, @hvoPara out, @guid out, @hvoText,
			@flidStText_Paragraphs, @cptOwningSequence

	-- Create the Inhuman agent.
	set @guid = null
	set @hvoAgent = null
	EXEC CreateOwnedObject$ @clidCmAgent, @hvoAgent out, @guid out, @hvoLangProj,
			@flidLanguageProject_AnalyzingAgents, @cptOwningCollection
	UPDATE CmAgent SET Human = 0, Version = N'Normal' WHERE [Id] = @hvoAgent
	SET @agentName = N'M3Parser'
	INSERT INTO CmAgent_Name (Obj, Ws, Txt)
		VALUES (@hvoAgent, @wsEng, @agentName)
	set @hvoText = null
	set @guid = null
	EXEC CreateOwnedObject$ @clidStText, @hvoText out, @guid out, @hvoAgent,
			@flidCmAgent_Notes, @cptOwningAtom
	set @hvoPara = null
	set @guid = null
	EXEC CreateOwnedObject$ @clidStTxtPara, @hvoPara out, @guid out, @hvoText,
			@flidStText_Paragraphs, @cptOwningSequence
END


--==( Remove Data )==--

--( This is the place to get rid of data that we don't want anymore.

-- Delete any old lexical database and start afresh. (No one should have been using
-- the lexical database in version 2)
DECLARE @lexdb int
SELECT @lexdb = dst from LanguageProject_LexicalDatabase
IF @lexdb is not null BEGIN
	exec DeleteObj$ @lexdb
END

-- Delete the old translation tags and reload them.
DECLARE @trtags int
SELECT @trtags = dst from LanguageProject_TranslationTags
IF @trtags is not null BEGIN
	exec DeleteObj$ @trtags
END

-- Delete the old annotation definitions and reload them.
DECLARE @anndef int
SELECT @anndef = dst from LanguageProject_AnnotationDefinitions
IF @anndef is not null BEGIN
	exec DeleteObj$ @anndef
END

--( Get rid of unwanted user view stuff
DECLARE @hvo INT
DECLARE uvrCursor CURSOR local static forward_only read_only FOR
	select uvr.id
	from UserView uv
	join UserView_Records juvr ON juvr.Src = uv.id
	join UserViewRec uvr ON uvr.id = juvr.Dst
	where uv.App = '5EA62D01-7A78-11D4-8078-0000C0FB81B5' -- CLE
		and uvr.Clsid IN (5036, 5037);
OPEN uvrCursor
FETCH NEXT FROM uvrCursor INTO @hvo
WHILE @@FETCH_STATUS = 0
BEGIN
	set @sQry =  'EXEC ' + @sNewDb + 'DeleteObj$ ' + CONVERT(nvarchar(11), @hvo)
	exec (@sQry)
	FETCH NEXT FROM uvrCursor INTO @hvo
END

CLOSE uvrCursor
DEALLOCATE uvrCursor

DECLARE uvfCursor CURSOR local static forward_only read_only FOR
	select uvf.id
	from UserView uv
	join UserView_Records juvr ON juvr.Src = uv.id
	join UserViewRec uvr ON uvr.id = juvr.Dst
	join UserViewRec_Fields juvrf ON juvrf.src = uvr.id
	join UserViewField uvf ON uvf.Id = juvrf.Dst
	where uv.App = '5EA62D01-7A78-11D4-8078-0000C0FB81B5' -- CLE
		and uvr.Clsid IN (5021, 5042, 5049) -- Just delete the fields for these classes (uvf.Id)
		and uvf.Flid IN (5049005, 5049006, 5042001, 5042002, 5042003, 5021001);
OPEN uvfCursor
FETCH NEXT FROM uvfCursor INTO @hvo
WHILE @@FETCH_STATUS = 0
BEGIN
	set @sQry =  'EXEC ' + @sNewDb + 'DeleteObj$ ' + CONVERT(nvarchar(11), @hvo)
	exec (@sQry)
	FETCH NEXT FROM uvfCursor INTO @hvo
END

CLOSE uvfCursor
DEALLOCATE uvfCursor

--( Remove "fAllomorph Conditions"
SELECT @nId = Obj FROM CmMajorObject_Name WHERE Txt = 'fAllomorph Conditions'
IF @@ROWCOUNT = 1 BEGIN
	SET @sQry =  'EXEC ' + @sNewDb + 'DeleteObj$ ' + CONVERT(nvarchar(11), @nId)
	EXEC (@sQry)
END

--( Remove "sAllomorph Condititons"
SELECT @nId = Obj FROM CmMajorObject_Name WHERE Txt = 'sAllomorph Conditions'
IF @@ROWCOUNT = 1 BEGIN
	SET @sQry =  'EXEC ' + @sNewDb + 'DeleteObj$ ' + CONVERT(nvarchar(11), @nId)
	EXEC (@sQry)
END

--( Remove all objects of defunct classes CmFolderObject (3) and
--( FsComplexFeature (2004).

DECLARE cfoCursor CURSOR local static forward_only read_only FOR
	SELECT [Id] FROM CmObject WHERE Class$ in (3, 2004)
OPEN cfoCursor
FETCH NEXT FROM cfoCursor INTO @hvo
WHILE @@FETCH_STATUS = 0
BEGIN
	set @sQry = 'EXEC ' + @sNewDb + 'DeleteObj$ ' + CONVERT(nvarchar(11), @hvo)
	exec (@sQry)
	FETCH NEXT FROM cfoCursor INTO @hvo
END

CLOSE cfoCursor
DEALLOCATE cfoCursor

--( Change Class$ from 2001 to 49, 2002 to 50, etc for all objects.
--( Also change OwnFlid$ from 2001002 to 49002, 2002001 to 50001, etc.
--( Maybe there shouldn't be any such objects, but let's be safe!  (The class
--( and field names all stayed the same, just the class and field numbers
--( were changed.)

set @sQry = 'ALTER TABLE ' + @sNewDb + 'CmObject DISABLE TRIGGER TR_CmObject_ValidateOwner'
exec (@sQry)

UPDATE CmObject SET Class$ = 49 WHERE Class$ = 2001
UPDATE CmObject SET Class$ = 50 WHERE Class$ = 2002
UPDATE CmObject SET Class$ = 51 WHERE Class$ = 2003
UPDATE CmObject SET Class$ = 53 WHERE Class$ = 2005
UPDATE CmObject SET Class$ = 54 WHERE Class$ = 2006
UPDATE CmObject SET Class$ = 55 WHERE Class$ = 2007
UPDATE CmObject SET Class$ = 56 WHERE Class$ = 2008
UPDATE CmObject SET Class$ = 57 WHERE Class$ = 2009
UPDATE CmObject SET Class$ = 58 WHERE Class$ = 2010
UPDATE CmObject SET Class$ = 59 WHERE Class$ = 2011
UPDATE CmObject SET Class$ = 60 WHERE Class$ = 2012
UPDATE CmObject SET Class$ = 61 WHERE Class$ = 2013
UPDATE CmObject SET Class$ = 62 WHERE Class$ = 2014
UPDATE CmObject SET Class$ = 63 WHERE Class$ = 2015
UPDATE CmObject SET Class$ = 64 WHERE Class$ = 2016
UPDATE CmObject SET Class$ = 65 WHERE Class$ = 2017

UPDATE CmObject SET OwnFlid$ = 49002 WHERE OwnFlid$ = 2001002
UPDATE CmObject SET OwnFlid$ = 50001 WHERE OwnFlid$ = 2002001
UPDATE CmObject SET OwnFlid$ = 51001 WHERE OwnFlid$ = 2003001
UPDATE CmObject SET OwnFlid$ = 53001 WHERE OwnFlid$ = 2005001
UPDATE CmObject SET OwnFlid$ = 54001 WHERE OwnFlid$ = 2006001
UPDATE CmObject SET OwnFlid$ = 55001 WHERE OwnFlid$ = 2007001
UPDATE CmObject SET OwnFlid$ = 55002 WHERE OwnFlid$ = 2007002
UPDATE CmObject SET OwnFlid$ = 55003 WHERE OwnFlid$ = 2007003
UPDATE CmObject SET OwnFlid$ = 55004 WHERE OwnFlid$ = 2007004
UPDATE CmObject SET OwnFlid$ = 55005 WHERE OwnFlid$ = 2007005
UPDATE CmObject SET OwnFlid$ = 55006 WHERE OwnFlid$ = 2007006
UPDATE CmObject SET OwnFlid$ = 55007 WHERE OwnFlid$ = 2007007
UPDATE CmObject SET OwnFlid$ = 55008 WHERE OwnFlid$ = 2007008
UPDATE CmObject SET OwnFlid$ = 56001 WHERE OwnFlid$ = 2008001
UPDATE CmObject SET OwnFlid$ = 56002 WHERE OwnFlid$ = 2008002
UPDATE CmObject SET OwnFlid$ = 56003 WHERE OwnFlid$ = 2008003
UPDATE CmObject SET OwnFlid$ = 57001 WHERE OwnFlid$ = 2009001
UPDATE CmObject SET OwnFlid$ = 57002 WHERE OwnFlid$ = 2009002
UPDATE CmObject SET OwnFlid$ = 57003 WHERE OwnFlid$ = 2009003
UPDATE CmObject SET OwnFlid$ = 58001 WHERE OwnFlid$ = 2010001
UPDATE CmObject SET OwnFlid$ = 59001 WHERE OwnFlid$ = 2011001
UPDATE CmObject SET OwnFlid$ = 59002 WHERE OwnFlid$ = 2011002
UPDATE CmObject SET OwnFlid$ = 59003 WHERE OwnFlid$ = 2011003
UPDATE CmObject SET OwnFlid$ = 59004 WHERE OwnFlid$ = 2011004
UPDATE CmObject SET OwnFlid$ = 61001 WHERE OwnFlid$ = 2013001
UPDATE CmObject SET OwnFlid$ = 62002 WHERE OwnFlid$ = 2014002
UPDATE CmObject SET OwnFlid$ = 62003 WHERE OwnFlid$ = 2014003
UPDATE CmObject SET OwnFlid$ = 63001 WHERE OwnFlid$ = 2015001
UPDATE CmObject SET OwnFlid$ = 64001 WHERE OwnFlid$ = 2016001
UPDATE CmObject SET OwnFlid$ = 65001 WHERE OwnFlid$ = 2017001
UPDATE CmObject SET OwnFlid$ = 65002 WHERE OwnFlid$ = 2017002
UPDATE CmObject SET OwnFlid$ = 65003 WHERE OwnFlid$ = 2017003
UPDATE CmObject SET OwnFlid$ = 65004 WHERE OwnFlid$ = 2017004
UPDATE CmObject SET OwnFlid$ = 65005 WHERE OwnFlid$ = 2017005
UPDATE CmObject SET OwnFlid$ = 65006 WHERE OwnFlid$ = 2017006

set @sQry = 'ALTER TABLE ' + @sNewDb + 'CmObject ENABLE TRIGGER TR_CmObject_ValidateOwner'
exec (@sQry)

--== Cleanup ==--

--( Turn on all constraints now we are done.

set @sQry = @sNewDb + 'ManageConstraints$'
exec @sQry  null, 'F', 'CHECK'

GO
