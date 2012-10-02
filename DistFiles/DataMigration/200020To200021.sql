-- update database from version 200020 to 200021
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------
-- Updated function from FwCore.sql which caused problems with getting correct answer from GetLinkedObjs$
-- Updated MatchingEntries SP to do a caseless comparison for glosses.
-------------------------------------------------------------
if object_id('fnGetOwnedObjects$') is not null begin
	print 'removing function fnGetOwnedObjects$'
	drop function [fnGetOwnedObjects$]
end
go
print 'creating function fnGetOwnedObjects$'
go
create function [fnGetOwnedObjects$] (
	@ObjId int=null,
	@hXMLDocObjList int=null,
	@grfcpt int=528482304,
	@fBaseClasses tinyint=0,
	@fSubClasses tinyint=0,
	@fRecurse tinyint=1,
	@riid int=NULL,
	@fCalcOrdKey tinyint=1 )
returns @ObjInfo table (
	[ObjId]		int		not null,
	[ObjClass]	int		null,
	[InheritDepth]	int		null		default(0),
	[OwnerDepth]	int		null		default(0),
	[RelObjId]	int		null,
	[RelObjClass]	int		null,
	[RelObjField]	int		null,
	[RelOrder]	int		null,
	[RelType]	int		null,
	[OrdKey]	varbinary(250)	null		default(0)
)
as
begin
	declare @nRowCnt int
	declare	@nObjId int, @nInheritDepth int, @nOwnerDepth int, @sOrdKey varchar(250)

	-- if NULL was specified as the mask assume that all objects are desired
	if @grfcpt is null set @grfcpt = 528482304

	-- at least one and only one object must be specified somewhere - objId paramater or XML
	if @objId is null and @hXMLDocObjList is null goto LFail
	if @objId is not null and @hXMLDocObjList is not null goto LFail

	-- determine if an object was supplied as an argument, if one was not use ObjInfoTbl$ as the list of objects
	if @objId is not null begin

		-- get the class of the specified object
		insert into @ObjInfo (ObjId, ObjClass, OrdKey)
		select	@objId, co.[Class$],
			-- calculate the order key even if @fCalcOrdKey = 0 because the overhead is very small here
			convert(varbinary, coalesce(co.[Owner$], 0)) +
			convert(varbinary, coalesce(co.[OwnFlid$], 0)) +
			convert(varbinary, coalesce(co.[OwnOrd$], 0))
		from	[CmObject] co (readuncommitted)
		where	co.[Id] = @objId
		if @@error <> 0 goto LFail
	end
	else begin

		-- parse the XML list of Object IDs and insert them into the table variable
		insert into @ObjInfo (ObjId, ObjClass, OrdKey)
		select	i.[Id], co.[Class$],
			-- calculate the order key even if @fCalcOrdKey = 0 because the overhead is very small here
			convert(varbinary, coalesce(co.[Owner$], 0)) +
			convert(varbinary, coalesce(co.[OwnFlid$], 0)) +
			convert(varbinary, coalesce(co.[OwnOrd$], 0))
		from	openxml (@hXMLDocObjList, '/root/Obj') with ([Id] int) i
			join [CmObject] co (readuncommitted) on co.[Id] = i.[Id]
		if @@error <> 0 goto LFail
	end

	set @nOwnerDepth = 1
	set @nRowCnt = 1
	while @nRowCnt > 0 begin
		-- determine if the order key should be calculated - if the order key is not needed a more
		--    effecient query can be used to generate the ownership tree
		if @fCalcOrdKey = 1 begin
			-- get the objects owned at the next depth and calculate the order key
			insert	into @ObjInfo
				(ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
			select 	co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[Class], co.[OwnFlid$], co.[OwnOrd$], f.[Type],
				oi.OrdKey+convert(varbinary, co.[Owner$]) + convert(varbinary, co.[OwnFlid$]) + convert(varbinary, coalesce(co.[OwnOrd$], 0))
			from 	[CmObject] co (readuncommitted)
					join @ObjInfo oi on co.[Owner$] = oi.[ObjId]
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	oi.[OwnerDepth] = @nOwnerDepth - 1
				and ( 	( @grfcpt & 8388608 = 8388608 and f.[Type] = 23 )
					or ( @grfcpt & 33554432 = 33554432 and f.[Type] = 25 )
					or ( @grfcpt & 134217728 = 134217728 and f.[Type] = 27 )
				)
		end
		else begin
			-- get the objects owned at the next depth and do not calculate the order key
			insert	into @ObjInfo
				(ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType)
			select 	co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[Class], co.[OwnFlid$], co.[OwnOrd$], f.[Type]
			from 	[CmObject] co (readuncommitted)
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	exists (select 	*
					from 	@ObjInfo oi
					where 	oi.[ObjId] = co.[Owner$]
						and oi.[OwnerDepth] = @nOwnerDepth - 1
				)
				and ( 	( @grfcpt & 8388608 = 8388608 and f.[Type] = 23 )
					or ( @grfcpt & 33554432 = 33554432 and f.[Type] = 25 )
					or ( @grfcpt & 134217728 = 134217728 and f.[Type] = 27 )
				)
		end
		set @nRowCnt=@@rowcount

		-- determine if the whole owning tree should be included in the results
		if @fRecurse = 0 break

		set @nOwnerDepth = @nOwnerDepth + 1
	end

	--
	-- get all of the base classes of the object(s), including CmObject.
	--
	if @fBaseClasses = 1 begin
		insert	into @ObjInfo
			(ObjId, ObjClass, InheritDepth, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
		select	oi.[ObjId], p.[Dst], p.[Depth], oi.[OwnerDepth], oi.[RelObjId], oi.[RelObjClass], oi.[RelObjField], oi.[RelOrder], oi.[RelType], oi.[OrdKey]
		from	@ObjInfo oi
				join [ClassPar$] p on oi.[ObjClass] = p.[Src]
				join [Class$] c on c.[id] = p.[Dst]
		where	p.[Depth] > 0
		if @@error <> 0 goto LFail
	end
	--
	-- get all of the sub classes of the object(s)
	--
	if @fSubClasses = 1 begin
		insert	into @ObjInfo
			(ObjId, ObjClass, InheritDepth, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
		select	oi.[ObjId], p.[Src], -p.[Depth], oi.[OwnerDepth], oi.[RelObjId], oi.[RelObjClass], oi.[RelObjField], oi.[RelOrder], oi.[RelType], oi.[OrdKey]
		from	@ObjInfo oi
				join [ClassPar$] p on oi.[ObjClass] = p.[Dst] and InheritDepth = 0
				join [Class$] c on c.[id] = p.[Dst]
		where	p.[Depth] > 0
			and p.[Dst] <> 0
		if @@error <> 0 goto LFail
	end

	-- if a class was specified remove the owned objects that are not of that type of class; these objects were
	--    necessary in order to get a list of all of the referenced and referencing objects that were potentially
	--    the type of specified class
	if @riid is not null begin
		delete	@ObjInfo
		where 	not exists (
				select	*
				from	[ClassPar$] cp
				where	cp.[Dst] = @riid
					and cp.[Src] = [ObjClass]
			)
		if @@error <> 0 goto LFail
	end

	return
LFail:

	delete from @ObjInfo
	return
end

go

if object_id('MatchingEntries') is not null begin
	print 'removing proc MatchingEntries'
	drop proc MatchingEntries
end
go
print 'creating proc MatchingEntries'
go

CREATE    proc [MatchingEntries]
	@cf nvarchar(4000),
	@uf nvarchar(4000),
	@af nvarchar(4000),
	@gl nvarchar(4000),
	@wsv int,
	@wsa int
AS
	declare @CFTxt nvarchar(4000), @cftext nvarchar(4000),
		@UFTxt nvarchar(4000), @uftext nvarchar(4000),
		@AFTxt nvarchar(4000), @aftext nvarchar(4000),
		@GLTxt nvarchar(4000), @gltext nvarchar(4000),
		@entryID int, @senseID int, @prevID int, @ObjId int,
		@class int
	declare @fIsNocountOn int
	-- deterimine if no count is currently set to on
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- table variable to hold return information.
	declare @MatchingEntries table (
		EntryID int primary key,		-- 1
		Class int,				-- 2
		CFTxt nvarchar(4000) default '***',	-- 3
		CFWs int,				-- 4
		UFTxt nvarchar(4000) default '***',	-- 5
		UFWs int,				-- 6
		AFTxt nvarchar(4000) default '***',	-- 7
		AFWs int,				-- 8
		GLTxt nvarchar(4000) default '***',	-- 9
		GLWs int				-- 10
		)

	--==( Citation and Underlying Forms )==--

	--( We're looking for citation forms or underlying forms that match
	--( the citation or underlying forms passed in.

	-- REVIEW (SteveMiller): LexEntry_CitationForm and MoForm_Form both take
	-- writing system IDs. If you are interested in only one writing system,
	-- the query should go faster by joining on ws as well as on obj, to make
	-- better use of the indexes. If more than writing system can be returned,
	-- we'll have to put more thought into how to retrieve the proper Txt
	-- field from the appropriate writing system.

	insert into @MatchingEntries (EntryID, Class, CFTxt, CFWs, UFTxt, UFWs, AFWs, GLWs)
		SELECT	le.[Id], le.Class$, isnull(cf.Txt, '***'), @wsv, isnull(mff.Txt, '***'), @wsv, @wsv, @wsa
		FROM LexEntry_ le (READUNCOMMITTED)
		LEFT OUTER JOIN LexEntry_CitationForm cf (READUNCOMMITTED) ON cf.Obj = le.[Id] and cf.ws = @wsv
		LEFT OUTER JOIN LexEntry_UnderlyingForm uf (READUNCOMMITTED) ON uf.Src = le.[Id]
		LEFT OUTER JOIN MoForm_Form mff (readuncommitted) ON mff.Obj = uf.Dst and mff.ws = @wsv
		WHERE (cf.Txt LIKE RTRIM(LTRIM(@cf)) + '%'OR mff.Txt LIKE RTRIM(LTRIM(@uf)) + '%')

	--==( Allomorph Forms )==--

	-- REVIEW (SteveMiller): Cursors are nototriously slow in databases. I
	-- expect these to bog down the proc as soon as we get any quantity of
	-- data. As of this writing, we have 62 records in LexEntry.

	--( We're looking for allomorph forms that match the allomorph form
	--( passed in.

	declare @curAllos CURSOR
	set @curAllos = CURSOR FAST_FORWARD for
		select le.id, le.Class$, amf.Txt
		from LexEntry_ le (readuncommitted)
		join LexEntry_Allomorphs lea (readuncommitted) ON lea.Src = le.id
		join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst and amf.ws = @wsv
		where amf.Txt LIKE RTRIM(LTRIM(@af)) + '%'

	OPEN @curAllos
	FETCH NEXT FROM @curAllos INTO @entryID, @class, @aftext
	WHILE @@FETCH_STATUS = 0
	BEGIN
		if @prevID = @entryID
			set @AFTxt = @AFTxt + '; ' + @aftext
		else
			set @AFTxt = @aftext
		select @ObjId=EntryID
		from @MatchingEntries
		where EntryID=@entryID
		if @@ROWCOUNT = 0
			insert into @MatchingEntries (EntryID, Class, AFTxt, CFWs, UFWs, AFWs, GLWs)
			values (@entryID, @class, @AFTxt, @wsv, @wsv, @wsv, @wsa)
		else
			update @MatchingEntries
			set AFTxt=@AFTxt
			where EntryID=@entryID
		set @prevID = @entryID
		FETCH NEXT FROM @curAllos INTO @entryID, @class, @aftext
	END
	CLOSE @curAllos
	DEALLOCATE @curAllos

	--==( Senses )==--

	declare @curSenses CURSOR
	declare @OwnerId int, @OwnFlid int
	set @prevID = 0
	set @curSenses = CURSOR FAST_FORWARD for
		select Obj, Txt
		from LexSense_Gloss (readuncommitted)
		where LOWER(RTRIM(LTRIM(Txt))) LIKE LOWER(RTRIM(LTRIM(@gl))) + '%' and ws = @wsa

	OPEN @curSenses
	FETCH NEXT FROM @curSenses INTO @senseId, @gltext
	WHILE @@FETCH_STATUS = 0
	BEGIN
		set @OwnFlid = 0
		set @entryID = @SenseId
		-- Loop until we find an owning flid of 5002011.
		while @OwnFlid != 5002011
		begin
			select 	@OwnerId=isnull(Owner$, 0), @OwnFlid=OwnFlid$
			from	CmObject (readuncommitted)
			where	Id=@entryID
			set @entryID=@OwnerId
			if @OwnerId = 0
				return 1
		end

		select @class=class$
		from CmObject (readuncommitted)
		where id=@entryID

		if @prevID = @senseId
			set @GLTxt = @GLTxt + '; ' + @gltext
		else
			set @GLTxt = @gltext

		select @ObjId=EntryID
		from @MatchingEntries
		where EntryID=@entryID

		if @@ROWCOUNT = 0
			insert into @MatchingEntries (EntryID, Class, GLTxt, CFWs, UFWs, AFWs, GLWs)
			values (@entryID, @class, @GLTxt, @wsv, @wsv, @wsv, @wsa)
		else
			update @MatchingEntries
			set GLTxt=@GLTxt
			where EntryID=@entryID

		set @prevID = @senseId
		FETCH NEXT FROM @curSenses INTO @senseId, @gltext
	END
	CLOSE @curSenses
	DEALLOCATE @curSenses

	--==( Final Pass )==--

	-- REVIEW (SteveMiller): This "final pass" can probably be enhanced by
	-- moving the logic into the query
	--
	-- 	select * from @MatchingEntries
	--
	-- at the bottom of this proc. (This query is the true "final pass"
	-- at the data for the proc.)

	-- Try to find some kind of string for any items that have not matched,

	declare @curFinalPass CURSOR, @rowcount int, @wsFoundling int
	set @curFinalPass = CURSOR FAST_FORWARD for
		select EntryID, CFTxt, UFTxt, AFTxt, GLTxt
		from @MatchingEntries
		where	CFTxt = '***'
			or UFTxt = '***'
			or AFTxt = '***'
			or GLTxt = '***'

	OPEN @curFinalPass
	FETCH NEXT FROM @curFinalPass INTO @entryID, @cftext, @uftext, @aftext, @gltext
	WHILE @@FETCH_STATUS = 0
	BEGIN
		set @rowcount = 0
		if @cftext = '***'
		begin
			-- Try a ws other than @wsv, since it wasn't there earlier.
			select top 1 @CFTxt=Txt
			from LexEntry_CitationForm (readuncommitted)
			where Obj = @entryID
			set @rowcount = @@rowcount

			if @rowcount = 0 -- Nothing for any other ws, so try getting it from the *last* allomorph.
			begin
				-- See if the last allomorph has it for @wsv.
				select top 1 @CFTxt=amf.Txt, @wsFoundling=amf.Ws
				from LexEntry_ le (readuncommitted)
				join LexEntry_Allomorphs lea (readuncommitted) ON lea.Src = le.id
				join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst and amf.ws = @wsv
				where le.id = @entryID
				ORDER BY lea.Ord DESC
				set @rowcount = @@rowcount
			end

			if @rowcount = 0 -- Try any other ws on the *last* allomorph
			begin
				select top 1 @CFTxt=amf.Txt, @wsFoundling=amf.Ws
				from LexEntry_ le (readuncommitted)
				join LexEntry_Allomorphs lea (readuncommitted) ON lea.Src = le.id
				join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst
				join LanguageProject_CurrentVernacularWritingSystems ws (readuncommitted) ON ws.Dst = amf.Ws
				where le.id = @entryID
				ORDER BY lea.Ord DESC, ws.ord
				set @rowcount = @@rowcount
			end

			if @rowcount > 0 -- Found one somewhere.
			begin
				update @MatchingEntries
				set CFTxt=@CFTxt, GLWs=@wsFoundling
				where EntryID=@entryID
			end
		end
		if @uftext = '***'
		begin
			select top 1 @UFTxt=uff.Txt
			from LexEntry_UnderlyingForm uf (readuncommitted)
			join MoForm_Form uff (readuncommitted) ON uff.Obj = uf.Dst and uff.ws = @wsv
			where uf.Src = @entryID
			set @rowcount = @@rowcount

			if @rowcount = 0 -- Nothing for the @wsv ws, so try something for any ws on the real UF.
			begin
				select top 1 @UFTxt=uff.Txt, @wsFoundling=uff.Ws
				from LexEntry_UnderlyingForm uf (readuncommitted)
				join MoForm_Form uff (readuncommitted) ON uff.Obj = uf.Dst
				join LanguageProject_CurrentVernacularWritingSystems ws (readuncommitted) ON ws.Dst = uff.Ws
				where uf.Src = @entryID
				ORDER BY ws.Ord
				set @rowcount = @@rowcount
			end

			if @rowcount = 0 -- Try @wsv on the *last* allomorph
			begin
				select top 1 @UFTxt=amf.Txt, @wsFoundling=amf.Ws
				from LexEntry_ le (readuncommitted)
				join LexEntry_Allomorphs lea (readuncommitted) ON lea.Src = le.id
				join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst and amf.ws = @wsv
				where le.id = @entryID
				ORDER BY lea.Ord DESC
				set @rowcount = @@rowcount
			end

			if @rowcount = 0 -- Try any other ws on the *last* allomorph
			begin
				select top 1 @UFTxt=amf.Txt, @wsFoundling=amf.Ws
				from LexEntry_ le (readuncommitted)
				join LexEntry_Allomorphs lea (readuncommitted) ON lea.Src = le.id
				join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst
				join LanguageProject_CurrentVernacularWritingSystems ws (readuncommitted) ON ws.Dst = amf.Ws
				where le.id = @entryID
				ORDER BY lea.Ord DESC, ws.ord
				set @rowcount = @@rowcount
			end

			if @rowcount > 0 -- Found one somewhere.
			begin
				update @MatchingEntries
				set UFTxt=@UFTxt, GLWs=@wsFoundling
				where EntryID=@entryID
			end
		end
		if @aftext = '***'
		begin
			select top 1 @AFTxt=amf.Txt
			from LexEntry_ le (readuncommitted)
			join LexEntry_Allomorphs lea (readuncommitted) ON lea.Src = le.id
			join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst and amf.ws = @wsv
			where le.id = @entryID
			set @rowcount = @@rowcount

			if @rowcount = 0 -- Nothing for the @wsv ws, so try all of them.
			begin
				SELECT top 1 @AFTxt=amf.Txt, @wsFoundling=amf.Ws
				from LexEntry_ le (readuncommitted)
				join LexEntry_Allomorphs lea (readuncommitted) ON lea.Src = le.id
				join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst
				join LanguageProject_CurrentVernacularWritingSystems ws (readuncommitted) ON ws.Dst = amf.Ws
				where le.id = @entryID
				ORDER BY ws.Ord
				set @rowcount = @@rowcount
			end

			if @rowcount > 0 -- Found one somewhere.
			begin
				update @MatchingEntries
				set AFTxt=@AFTxt, GLWs=@wsFoundling
				where EntryID=@entryID
			end
		end
		if @gltext = '***'
		begin
			SELECT top 1 @GLTxt=lsg.Txt, @wsFoundling=lsg.Ws
			FROM dbo.fnGetSensesInEntry$(@entryID)
			join LexSense_Gloss lsg (readuncommitted) On lsg.Obj=SenseId and lsg.ws = @wsa
			set @rowcount = @@rowcount

			if @rowcount = 0 -- Nothing for the @wsa ws, so try all of them.
			begin
				SELECT top 1 @GLTxt=lsg.Txt, @wsFoundling=lsg.Ws
				FROM dbo.fnGetSensesInEntry$(@entryID)
				join LexSense_Gloss lsg (readuncommitted) On lsg.Obj=SenseId
				join LanguageProject_CurrentAnalysisWritingSystems ws (readuncommitted) ON ws.Dst = lsg.Ws
				ORDER BY ws.Ord
				set @rowcount = @@rowcount
			end

			if @rowcount > 0 -- Found one somewhere.
			begin
				update @MatchingEntries
				set GLTxt=@GLTxt, GLWs=@wsFoundling
				where EntryID=@entryID
			end
		end
		FETCH NEXT FROM @curFinalPass INTO @entryID, @cftext, @uftext, @aftext, @gltext
	END
	CLOSE @curFinalPass
	DEALLOCATE @curFinalPass

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	select *
	from @MatchingEntries

GO

IF OBJECT_ID('CopyObj$') IS NOT NULL BEGIN
	PRINT 'removing procedure CopyObj$'
	DROP PROC [CopyObj$]
END
GO
PRINT 'creating procedure CopyObj$'
GO

CREATE PROCEDURE [CopyObj$]
	@nTopSourceObjId INT,  	--( the top owning object
	@nTopDestOwnerId INT,	--( The ID of the owner of the top object we're creating here
	@nTopDestOwnerFlid INT,		--( The owning field ID of the top object we're creating here
	@nTopDestObjId INT OUTPUT	--( the ID for the new object we're creating here
AS

	DECLARE
		@nSourceObjId INT,
		@nOwnerID INT,
		@nOwnerFieldId INT, --(owner flid
		@nRelObjId INT,
		@nRelObjFieldId INT,  --( related object flid
		@guidNew UNIQUEIDENTIFIER,
		@nFirstOuterLoop TINYINT,
		@nDestObjId INT,
		@nOwnerDepth INT,
		@nInheritDepth INT,
		@nClass INT,
		@nvcClassName NVARCHAR(100),
		@nvcQuery NVARCHAR(4000)

	DECLARE
		@nvcFieldsList NVARCHAR(4000),
		@nvcValuesList NVARCHAR(4000),
		@nColumn INT,
		@sysColumn SYSNAME,
		@nFlid INT,
		@nType INT,
		@nSourceClassId INT,
		@nDestClassId INT,
		@nvcFieldName NVARCHAR(100),
		@nvcTableName NVARCHAR(100)

	CREATE TABLE #SourceObjs (
		[ObjId]			INT		not null,
		[ObjClass]		INT		null,
		[InheritDepth]	INT		null		DEFAULT(0),
		[OwnerDepth]	INT		null		DEFAULT(0),
		[RelObjId]		INT		null,
		[RelObjClass]	INT		null,
		[RelObjField]	INT		null,
		[RelOrder]		INT		null,
		[RelType]		INT		null,
		[OrdKey]		VARBINARY(250)	null	DEFAULT(0),
		[ClassName]		NVARCHAR(100),
		[DestinationID]	INT		NULL)

	SET @nFirstOuterLoop = 1
	SET @nOwnerId = @nTopDestOwnerId
	SET @nOwnerFieldId = @nTopDestOwnerFlid

	--== Get the Object Tree ==--

	INSERT INTO #SourceObjs
		SELECT oo.*, c.[Name], NULL
		FROM dbo.fnGetOwnedObjects$(@nTopSourceObjId, NULL, NULL, 1, 0, 1, null, 1) oo
		JOIN Class$ c ON c.[Id] = oo.[ObjClass]
		WHERE oo.[ObjClass] <> 0 -- Have to block CmObject rows, now that fnGetOwnedObjects$ returns the CmObject table.
		ORDER BY [OwnerDepth], [InheritDepth] DESC, [ObjClass]

	--== Create CmObject Records ==--

	--( Create all the CmObjects for all the objects copied

	DECLARE curNewCmObjects CURSOR FAST_FORWARD FOR
		SELECT DISTINCT [ObjId], [RelObjId], [RelObjField] FROM #SourceObjs

	OPEN curNewCmObjects
	FETCH curNewCmObjects INTO @nSourceObjId, @nRelObjId, @nRelObjFieldId
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @guidNew = NEWID()

		INSERT INTO CmObject WITH (ROWLOCK) ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
			SELECT @guidNew, [Class$], @nOwnerId, @nOwnerFieldId, [OwnOrd$]
			FROM CmObject
			WHERE [Id] = @nSourceObjId

		SET @nDestObjId = @@IDENTITY

		UPDATE #SourceObjs SET [DestinationID] = @nDestObjId WHERE [ObjId] = @nSourceObjID

		--== Copy records in "multi" string tables ==--

		--( Tables of the former MultiTxt$ table are now  set in a separate loop.

		INSERT INTO MultiStr$ WITH (ROWLOCK) ([Flid], [Obj], [Ws], [Txt], [Fmt])
		SELECT [Flid], @nDestObjID, [WS], [Txt], [Fmt]
		FROM MultiStr$  (READUNCOMMITTED)
		WHERE [Obj] = @nSourceObjId

		--( As of this writing, MultiBigTxt$ is not used anywhere in the conceptual
		--( model, and it is impossible to use it through the interface.

		INSERT INTO MultiBigTxt$ WITH (ROWLOCK) ([Flid], [Obj], [Ws], [Txt])
		SELECT [Flid], @nDestObjID, [WS], [Txt]
		FROM MultiBigTxt$ (READUNCOMMITTED)
		WHERE [Obj] = @nSourceObjId

		INSERT INTO MultiBigStr$ WITH (ROWLOCK) ([Flid], [Obj], [Ws], [Txt], [Fmt])
		SELECT [Flid], @nDestObjID, [WS], [Txt], [Fmt]
		FROM MultiBigStr$ (READUNCOMMITTED)
		WHERE [Obj] = @nSourceObjId

		--( If the object ID = the top owning object ID
		IF @nFirstOuterLoop = 1
			SET @nTopDestObjId = @nDestObjID --( sets the output parameter

		SET @nFirstOuterLoop = 0

		--( This fetch is different than the one at the top of the loop!
		FETCH curNewCmObjects INTO @nSourceObjId, @nRelObjId, @nOwnerFieldId

		SELECT @nOwnerId = [DestinationId] FROM #SourceObjs WHERE ObjId = @nRelObjId
	END
	CLOSE curNewCmObjects
	DEALLOCATE curNewCmObjects

	--== Create All Other Records ==--

	DECLARE curRecs CURSOR FAST_FORWARD FOR
		SELECT DISTINCT [OwnerDepth], [InheritDepth], [ObjClass], [ClassName]
		FROM #SourceObjs
		ORDER BY [OwnerDepth], [InheritDepth] DESC, [ObjClass]

	OPEN curRecs
	FETCH curRecs INTO 	@nOwnerDepth, @nInheritDepth, @nClass, @nvcClassName
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @nvcFieldsList = N''
		SET @nvcValuesList = N''
		SET @nColumn = 1
		SET @sysColumn = COL_NAME(OBJECT_ID(@nvcClassName), @nColumn)
		WHILE @sysColumn IS NOT NULL BEGIN
			IF @nColumn > 1 BEGIN --( not the first time in loop
				SET @nvcFieldsList = @nvcFieldsList + N', '
				SET @nvcValuesList = @nvcValuesList + N', '
			END
			SET @nvcFieldName = N'[' + UPPER(@sysColumn) + N']'

			--( Field list for insert
			SET @nvcFieldsList = @nvcFieldsList + @nvcFieldName

			--( Vales to put into those fields
			IF @nvcFieldName = '[ID]'
				SET @nvcValuesList = @nvcValuesList + N' so.[DestinationId] '
			ELSE IF @nvcFieldName = N'[DATECREATED]' OR @nvcFieldName = N'[DATEMODIFIED]'
				SET @nvcValuesList = @nvcValuesList + N'CURRENT_TIMESTAMP'
			ELSE
				SET @nvcValuesList = @nvcValuesList + @nvcFieldName

			SET @nColumn = @nColumn + 1
			SET @sysColumn = COL_NAME(OBJECT_ID(@nvcClassName), @nColumn)
		END

		SET @nvcQuery =
			N'INSERT INTO ' + @nvcClassName + ' WITH (ROWLOCK) (
				' + @nvcFieldsList + ')
			SELECT ' + @nvcValuesList + N'
			FROM ' + @nvcClassName + ' cn (READUNCOMMITTED)
			JOIN #SourceObjs so ON
				so.[OwnerDepth] = ' + STR(@nOwnerDepth) + N' AND
				so.[InheritDepth] = ' + STR(@nInheritDepth) + N' AND
				so.[ObjClass] = ' + STR(@nClass) + N' AND
				so.[ObjId] = cn.[Id]'

		EXEC (@nvcQuery)

		--== Copy References ==--

		DECLARE curReferences CURSOR LOCAL STATIC FORWARD_ONLY READ_ONLY FOR
		SELECT f.[Id], f.[Type], f.[Name]
		FROM Field$ f (READUNCOMMITTED)
		WHERE f.[Class] = @nClass AND
			(f.[Type] = 26 OR f.[Type] = 28)

		OPEN curReferences
		FETCH curReferences INTO @nFlid, @nType, @nvcFieldName
		WHILE @@FETCH_STATUS = 0 BEGIN

			SET @nvcQuery = N'INSERT INTO ' + @nvcClassName + N'_' + @nvcFieldName + N' ([Src], [Dst]'

			IF @nType = 26
				SET @nvcQuery = @nvcQuery + N')
					SELECT DestinationId, r.[Dst]
					FROM '
			ELSE --( IF @nType = 28
				SET @nvcQuery = @nvcQuery + N', [Ord])
					SELECT DestinationId, r.[Dst], r.[Ord]
					FROM '

				SET @nvcQuery = @nvcQuery + @nvcClassName + N'_' + @nvcFieldName + N' r (READUNCOMMITTED)
					JOIN #SourceObjs ON
						[OwnerDepth] = @nOwnerDepth AND
						[InheritDepth] =  @nInheritDepth AND
						[ObjClass] = @nClass AND
						[ObjId] = r.[Src]'

				EXEC sp_executesql @nvcQuery,
					N'@nOwnerDepth INT, @nInheritDepth INT, @nClass INT',
					@nOwnerDepth, @nInheritDepth, @nClass

			FETCH curReferences INTO @nFlid, @nType, @nvcFieldName
		END
		CLOSE curReferences
		DEALLOCATE curReferences

		FETCH curRecs INTO 	@nOwnerDepth, @nInheritDepth, @nClass, @nvcClassName
	END
	CLOSE curRecs
	DEALLOCATE curRecs

	--== References to Copied Objects ==--

	--( objects can point to themselves. For instance, the People list has a
		--( researcher that points back to the People list. For copies, this reference
		--( back to itself needs to have the new object, not the old source object. For
		--( all other reference properties, the old object will do fine.

	DECLARE curRefs2CopiedObjs CURSOR LOCAL STATIC FORWARD_ONLY READ_ONLY FOR
		SELECT DISTINCT c.[Name] + N'_' + f.[Name]
		FROM Class$ c
		JOIN Field$ f ON f.[Class] = c.[Id]
		JOIN #SourceObjs s ON s.[ObjClass] = c.[Id]
		WHERE f.[Type] = 26 OR f.[Type] = 28

	OPEN curRefs2CopiedObjs
	FETCH curRefs2CopiedObjs INTO @nvcTableName
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @nvcQuery = N'UPDATE ' + @nvcTableName + N'
			SET [Dst] = #SourceObjs.[DestinationId]
			FROM ' + @nvcTableName + N', #SourceObjs
			WHERE ' + @nvcTableName + N'.[Dst] = #SourceObjs.[ObjId]'

		EXEC sp_executesql @nvcQuery

		FETCH curRefs2CopiedObjs INTO @nvcTableName
	END
	CLOSE curRefs2CopiedObjs
	DEALLOCATE curRefs2CopiedObjs

	--== MultiTxt$ Records ==--

	DECLARE curMultiTxt CURSOR FAST_FORWARD FOR
		SELECT DISTINCT ObjId, ObjClass, ClassName, DestinationId
		FROM #SourceObjs

	--( First get the object IDs and their class we're working with
	OPEN curMultiTxt
	FETCH curMultiTxt INTO @nSourceObjId, @nClass, @nvcClassName, @nDestObjId
	WHILE @@FETCH_STATUS = 0 BEGIN

		--( Now copy into each of the multitxt fields
		SELECT TOP 1 @nFlid = [Id], @nvcFieldName = [Name]
		FROM Field$
		WHERE Class = @nClass AND Type = 16
		ORDER BY [Id]

		WHILE @@ROWCOUNT > 0 BEGIN
			SET @nvcQuery =
				N'INSERT INTO ' + @nvcClassName + N'_' + @nvcFieldName + N' ' +
				N'WITH (ROWLOCK) (Obj, Ws, Txt)' + CHAR(13) +
				CHAR(9) + N'SELECT @nDestObjId, WS, Txt' + CHAR(13) +
				CHAR(9) + N'FROM ' + @nvcClassName + N'_' + @nvcFieldName + ' (READUNCOMMITTED)' + CHAR(13) +
				CHAR(9) + N'WHERE Obj = @nSourceObjId'

			EXECUTE sp_executesql @nvcQuery,
				N'@nDestObjId INT, @nSourceObjId INT',
				@nDestObjID, @nSourceObjId

			SELECT TOP 1 @nFlid = [Id], @nvcFieldName = [Name]
			FROM Field$
			WHERE [Id] > @nFlid AND Class = @nClass AND Type = 16
			ORDER BY [Id]
		END

		FETCH curMultiTxt INTO @nSourceObjId, @nClass, @nvcClassName, @nDestObjId
	END
	CLOSE curMultiTxt
	DEALLOCATE curMultiTxt

	DROP TABLE #SourceObjs

GO

-------------------------------------------------------------------------------

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200020
begin
	update Version$ set DbVer = 200021
	COMMIT TRANSACTION
	print 'database updated to version 200021'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200020 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO