-- update database from version 200013 to 200014
BEGIN TRANSACTION

if object_id('MatchingEntries') is not null begin
	if (select DbVer from Version$) = 200013
		print 'removing procedure MatchingEntries'
	drop proc MatchingEntries
end
GO
if (select DbVer from Version$) = 200013
	print 'creating procedure MatchingEntries'
GO

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
		UFTxt nvarchar(4000) default '***',	-- 4
		AFTxt nvarchar(4000) default '***',	-- 5
		GLTxt nvarchar(4000) default '***'	-- 6
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

	insert into @MatchingEntries (EntryID, Class, CFTxt, UFTxt)
		SELECT	le.[Id], le.Class$, isnull(cf.Txt, '***'), isnull(mff.Txt, '***')
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
			insert into @MatchingEntries (EntryID, Class, AFTxt)
			values (@entryID, @class, @AFTxt)
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
		where Txt LIKE RTRIM(LTRIM(@gl)) + '%' and ws = @wsa

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
			insert into @MatchingEntries (EntryID, Class, GLTxt)
			values (@entryID, @class, @GLTxt)
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

	declare @curFinalPass CURSOR
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
		if @cftext = '***'
		begin
			select top 1 @CFTxt=Txt
			from LexEntry_CitationForm (readuncommitted)
			where Obj = @entryID

			if @CFTxt is not null
			begin
				update @MatchingEntries
				set CFTxt=@CFTxt
				where EntryID=@entryID
			end
		end
		if @uftext = '***'
		begin
			select top 1 @UFTxt=uff.Txt
			from LexEntry_UnderlyingForm uf (readuncommitted)
			join MoForm_Form uff (readuncommitted) ON uff.Obj = uf.Dst and uff.ws = @wsv
			where uf.Src = @entryID

			if @UFTxt is not null
			begin
				update @MatchingEntries
				set UFTxt=@UFTxt
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

			if @AFTxt is not null
			begin
				update @MatchingEntries
				set AFTxt=@AFTxt
				where EntryID=@entryID
			end
		end
		if @gltext = '***'
		begin
			SELECT top 1 @GLTxt=lsg.Txt
			FROM dbo.fnGetSensesInEntry$(@entryID)
			left outer join LexSense_Gloss lsg (readuncommitted) On lsg.Obj=SenseId and lsg.ws = @wsa

			if @GLTxt is not null
			begin
				update @MatchingEntries
				set GLTxt=@GLTxt
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

IF OBJECT_ID('fnGetDefaultAnalysesGlosses') IS NOT NULL BEGIN
	if (select DbVer from Version$) = 200013
		PRINT 'removing function fnGetDefaultAnalysesGlosses'
	DROP FUNCTION fnGetDefaultAnalysesGlosses
END
GO
if (select DbVer from Version$) = 200013
	PRINT 'creating function fnGetDefaultAnalysesGlosses'
GO

CREATE FUNCTION fnGetDefaultAnalysesGlosses (
	@nStTxtParaId INT, @nAnnotType INT)
RETURNS @tblDefaultAnalysesGlosses TABLE (
	WordformId INT,
	AnalysisId INT,
	GlossId INT,
	BaseAnnotationId INT,
	InstanceOf INT,
	BeginOffset INT,
	EndOffset INT)
AS BEGIN

	DECLARE
		@nWordformId INT,
		@nAnalysisId INT,
		@nGlossId INT

	declare @defaults table (
		WfId INT,
		AnalysisId INT,
		GlossId INT,
		[Score] INT)
	-- Get the 'real' (non-default) data
	INSERT INTO @tblDefaultAnalysesGlosses
	SELECT
		coalesce(wfwg.id, wfwa.id, a.InstanceOf) AS WordformId,
		coalesce(wawg.id, wai.id),
		wgi.id,
		ba.[Id] AS BaseAnnotationId,
		a.InstanceOf,
		ba.BeginOffset,
		ba.EndOffset
	FROM CmBaseAnnotation ba
	JOIN CmAnnotation a  (readuncommitted) ON a.[Id] = ba.[Id]
		AND a.AnnotationType = @nAnnotType
	-- these joins handle the case that instanceof is a WfiAnalysis; all values will be null otherwise
	LEFT OUTER JOIN WfiAnalysis wai ON wai.id = a.InstanceOf -- 'real' analysis (is the instanceOf)
	LEFT OUTER JOIN CmObject waio on waio.id = wai.id -- CmObject of analysis instanceof
	LEFT OUTER JOIN CmObject wfwa on wfwa.id = waio.owner$ -- wf that owns wai
	-- these joins handle the case that instanceof is a WfiGloss; all values will be null otherwise.
	LEFT OUTER JOIN WfiGloss wgi on wgi.id = a.instanceOf -- 'real' gloss (is the instanceof)
	LEFT OUTER JOIN CmObject wgio on wgio.id = wgi.id
	LEFT OUTER JOIN CmObject wawg on wawg.id = wgio.owner$ -- ananlyis that owns wgi
	LEFT OUTER JOIN CmObject wfwg on wfwg.id = wawg.owner$ -- wordform that owns wgi (indirectly)
	WHERE ba.BeginObject = @nStTxtParaId

	-- InstanceOf is a WfiAnalysis filling out a default gloss if possible.

	UPDATE @tblDefaultAnalysesGlosses SET GlossId = WgId
	FROM @tblDefaultAnalysesGlosses t
	JOIN (
		SELECT Sub2.WaId, Sub2.WgId, MAX(Sub2.CountInstance) AS MaxCountInstance
		FROM (
			SELECT t.InstanceOf AS WaId, wg.[Id] AS WgId, COUNT(ann.[Id]) AS CountInstance
			FROM @tblDefaultAnalysesGlosses t
			JOIN WfiGloss_ wg (READUNCOMMITTED) ON wg.Owner$ = t.InstanceOf
			LEFT OUTER JOIN CmAnnotation ann (READUNCOMMITTED) ON ann.InstanceOf = wg.[Id]
			GROUP BY t.InstanceOf, wg.[Id]
			) Sub2
		GROUP BY Sub2.WaId, Sub2.WgId
		) Sub1 ON Sub1.WaId = t.InstanceOf
	WHERE t.GlossId IS NULL

	-- WfiGlosses owned by those WfiWordforms

	UPDATE @tblDefaultAnalysesGlosses SET GlossId = WgId, AnalysisId = WaId
	FROM @tblDefaultAnalysesGlosses t
	JOIN (
		SELECT Sub2.WfId, Sub2.WaId, Sub2.WgId,
			MAX(Sub2.CountInstance) AS MaxCountInstance
		FROM (
			SELECT t.WordformId AS WfId, wa.[Id] AS WaId, wg.[Id] AS WgId,
				COUNT(ann.[Id]) AS CountInstance
			FROM @tblDefaultAnalysesGlosses t
			JOIN WfiAnalysis_ wa (READUNCOMMITTED) ON wa.Owner$ = t.WordformId
			JOIN WfiGloss_ wg (READUNCOMMITTED) ON wg.Owner$ = wa.[Id]
			LEFT OUTER JOIN CmAnnotation ann (READUNCOMMITTED) ON ann.InstanceOf = wg.[Id]
			GROUP BY t.WordformId, wa.[Id], wg.[Id]
			) Sub2
		GROUP BY Sub2.WfId, Sub2.WaId, Sub2.WgId
		) Sub1 ON Sub1.WfId = t.WordformId
	WHERE t.AnalysisId IS NULL

	-- Final option is InstanceOf is WfiWordform, there are analyses but no glosses

	UPDATE @tblDefaultAnalysesGlosses SET AnalysisId = WaId
	FROM @tblDefaultAnalysesGlosses t
	JOIN (
		SELECT Sub2.WfId, Sub2.WaId, MAX(Sub2.CountInstance) AS MaxCountInstance
		FROM (
			SELECT t.WordformId AS WfId, wa.[Id] AS WaId, COUNT(ann.[Id]) AS CountInstance
			FROM @tblDefaultAnalysesGlosses t
			JOIN WfiAnalysis_ wa (READUNCOMMITTED) ON wa.Owner$ = t.WordformId
			LEFT OUTER JOIN CmAnnotation ann (READUNCOMMITTED) ON ann.InstanceOf = wa.[Id]
			GROUP BY t.WordformId, wa.[Id]
			) Sub2
		GROUP BY Sub2.WfId, Sub2.WaId
		) Sub1 ON Sub1.WfId = t.WordformId
	WHERE t.AnalysisId IS NULL

	RETURN
END
GO

if object_id('CountUpToDateParas') is not null begin
	if (select DbVer from Version$) = 200013
		print 'removing procedure CountUpToDateParas'
	drop proc [CountUpToDateParas]
end
go
if (select DbVer from Version$) = 200013
	print 'creating procedure CountUpToDateParas'
go

create proc CountUpToDateParas
	@atid int, @stid int
as
select count(tp.id) from StTxtPara_ tp (readuncommitted)
	join CmBaseAnnotation_ cb (readuncommitted) on cb.BeginObject = tp.id and cb.AnnotationType = @atid
		and cast(cast(tp.UpdStmp as bigint) as NVARCHAR(20)) = cast(cb.CompDetails as NVARCHAR(20))
	where tp.owner$ = @stid
	group by tp.owner$
go

/***********************************************************************************************
 * Procedure: NoteInterlinProcessTime
 *
 * Description:
 *	For each paragraph in a specified StText, ensures that there is a CmBaseAnnotation
 *	of type Process Time (actually the type is passed as an argument) whose BeginObject
 *	is the paragraph, and sets its CompDetails to a representation of the UpdStmp of the
 * 	paragraph.
 *
 * Parameters:
 *	@atid int=id of the attribute defin for process type (app typically has it cached)
 * 	@stid int=id of the StText whose paragraphs are to be marked.
 **********************************************************************************************/
if object_id('NoteInterlinProcessTime') is not null begin
	if (select DbVer from Version$) = 200013
		print 'removing procedure NoteInterlinProcessTime'
	drop proc [NoteInterlinProcessTime]
end
go
if (select DbVer from Version$) = 200013
	print 'creating procedure NoteInterlinProcessTime'
go

create proc NoteInterlinProcessTime
	@atid int, @stid int
as

declare MakeAnnCursor cursor local static forward_only read_only for
select tp.id from StTxtPara_ tp (readuncommitted)
left outer join CmBaseAnnotation_ cb (readuncommitted) on cb.BeginObject = tp.id and cb.AnnotationType = @atid
where tp.owner$ = @stid and cb.id is null

declare @tpid int,
	@NewObjGuid uniqueidentifier,
	@cbaId int
open MakeAnnCursor
	fetch MakeAnnCursor into @tpid
	while @@fetch_status = 0 begin
		exec CreateObject$ 37, @cbaId out, @NewObjGuid out
		update CmBaseAnnotation set BeginObject = @tpid where id = @cbaId
		update CmAnnotation set AnnotationType = @atid where id = @cbaId
		set @cbaId = null
		set @NewObjGuid = null
		fetch MakeAnnCursor into @tpid
	end
close MakeAnnCursor
deallocate MakeAnnCursor
update CmBaseAnnotation_
	set CompDetails = cast(cast(tp.UpdStmp as bigint) as NVARCHAR(20))
	from CmBaseAnnotation_ cba (readuncommitted)
		join StTxtPara_ tp (readuncommitted) on cba.BeginObject = tp.id and tp.owner$ = @stid
	where cba.AnnotationType = @atid
go

go
create index AnnInstanceOfIdx on CmAnnotation(InstanceOf)
go
create index BaBeginObjectIdx on CmBaseAnnotation(BeginObject)
go


--( Check whether we have a specific CmAnnotationDefn with the proper Guid$ yet.
--( If not, make it so.

DECLARE @ws INT, @date DATETIME, @owner INT, @newid INT, @guid UNIQUEIDENTIFIER,
	@fmt VARBINARY(4000)
SELECT @owner = id FROM CmObject WHERE Guid$ = '8D4CBD80-0DCA-4A83-8A1F-9DB3AA4CFF54'
SET @date = Getdate()
SELECT @ws = id FROM LgWritingSystem WHERE IcuLocale = 'en'

SELECT @newid=co.Id, @guid=co.Guid$
FROM CmObject co
JOIN CmPossibility_Name pn on pn.Obj = co.Id AND pn.Ws = @ws AND pn.Txt = N'Process Time'
JOIN CmPossibility_Abbreviation pa on pa.Obj = co.Id AND pa.Ws = @ws AND pa.Txt = N'pt'
JOIN MultiBigStr$ pd on pd.Obj = co.Id AND pd.Flid = 7003 AND pd.Ws = @ws AND pd.Txt LIKE N'Used internally by the program to keep track of when some process was last applied to an object.'
WHERE co.Class$ = 35 AND co.Owner$ = @owner and co.OwnFlid$ = 7004

IF @@rowcount = 0 BEGIN
	-- We'll assume the format for the shortest similar MultiBigStr$ will be suitable for the
	-- new entry.  This is not absolutely guaranteed, but has an extremely high probability.
	-- Putting in an empty Fmt value is guaranteed to be incorrect!
	SELECT TOP 1 @fmt=CONVERT(VARBINARY(4000), mbs.Fmt)
	FROM MultiBigStr$ mbs
	WHERE Flid = 7003 AND Ws = @ws AND LEN(CONVERT(NVARCHAR(4000), mbs.Txt)) > 1
	ORDER BY LEN(CONVERT(NVARCHAR(4000), mbs.Txt))
	EXEC CreateObject_CmAnnotationDefn  @ws, N'Process Time', @ws, N'pt', @ws,
		N'Used internally by the program to keep track of when some process was last applied to an object.',
		@fmt, 0, @date, @date, null, -1073741824, -1073741824, -1073741824, 0,
		0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, @owner, 7004, null,
		@newid output, @guid output
END
IF @guid != '20CF6C1C-9389-4380-91F5-DFA057003D51' BEGIN
	UPDATE CmObject SET Guid$ = '20CF6C1C-9389-4380-91F5-DFA057003D51' WHERE [Id] = @newid
END
go


declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200013
begin
	update Version$ set DbVer = 200014
	COMMIT TRANSACTION
	print 'database updated to version 200014'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200013 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
