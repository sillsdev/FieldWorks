-- update database from version 200011 to 200012
BEGIN TRANSACTION

if object_id('MatchingEntries') is not null begin
	if (select DbVer from Version$) = 200011
		print 'removing procedure MatchingEntries'
	drop proc MatchingEntries
end
if (select DbVer from Version$) = 200011
	print 'creating procedure MatchingEntries'
go

/*****************************************************************************
 * MatchingEntries
 *
 * Description:
 *	Returns a table with zero or more rows of LexEntry objects
 *	which match in one or more of the parameters.
 * Parameters:
 *	@cf = the citation form to find.
 *	@uf = the underlying form to find.
 *	@af = the allomorphs to find.
 *	@gl = the gloss to find.
 *	@wsv = vernacular ws for matching cf, uf, af
 *	@wsa = analysis ws for matching gl
 * Returns:
 *	Result set containing table of cf, uf, af, gl for each match.
 *****************************************************************************/
create proc [MatchingEntries]
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
		SELECT	le.[Id], le.Class$, cf.Txt, mff.Txt
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
			join LexSense_Gloss lsg (readuncommitted) On lsg.Obj=SenseId and lsg.ws = @wsa

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


go

/********************************************************************
 * Function: fnGetDefaultAnalysisGloss
 *
 * Description:
 *	Returns a ranked table of various wfiAnalyis and wfiGloss
 *	objects. Built for use in the Interlinear Text Tool.
 *
 * Parameters:
 *	@nWfiWordFormId INT = object ID for the word of interest
 *
 * Sample Call:
 *
 *	SELECT TOP 1 *
 *	FROM dbo.fnGetDefaultAnalysisGloss(3920)
 *	ORDER BY Score DESC
 *******************************************************************/

IF OBJECT_ID('fnGetDefaultAnalysisGloss') IS NOT NULL BEGIN
	if (select DbVer from Version$) = 200011
		PRINT 'removing function fnGetDefaultAnalysisGloss'
	DROP FUNCTION fnGetDefaultAnalysisGloss
END
GO
if (select DbVer from Version$) = 200011
	PRINT 'creating function fnGetDefaultAnalysisGloss'
GO

CREATE FUNCTION fnGetDefaultAnalysisGloss (
	@nWfiWordFormId INT)
RETURNS @tblScore TABLE (
	AnalysisId INT,
	GlossId INT,
	[Score] INT)
AS BEGIN

	INSERT INTO @tblScore
		--( wfiGloss is an InstanceOf
		SELECT
			oanalysis.[Id],
			ogloss.[Id],
			(COUNT(ann.InstanceOf) + 10000) --( needs higher # than wfiAnalsys
		FROM CmAnnotation ann (READUNCOMMITTED)
		JOIN WfiGloss g  (READUNCOMMITTED) ON g.[Id] = ann.InstanceOf
		JOIN CmObject ogloss (READUNCOMMITTED) ON ogloss.[Id] = g.[Id]
		JOIN CmObject oanalysis (READUNCOMMITTED) ON oanalysis.[Id] = ogloss.Owner$
			AND oanalysis.Owner$ = @nWfiWordFormId
		JOIN WfiAnalysis a (READUNCOMMITTED) ON a.[Id] = oanalysis.[Id]
		GROUP BY oanalysis.[Id], ogloss.[Id]
	UNION ALL
		--( wfiAnnotation is an InstanceOf
		SELECT
			oanalysis.[Id],
			NULL,
			COUNT(ann.InstanceOf)
		FROM CmAnnotation ann (READUNCOMMITTED)
		JOIN CmObject oanalysis (READUNCOMMITTED) ON oanalysis.[Id] = ann.InstanceOf
			AND oanalysis.Owner$ = @nWfiWordFormId
		JOIN WfiAnalysis a (READUNCOMMITTED) ON a.[Id] = oanalysis.[Id]
		GROUP BY oanalysis.[Id]

	--( If the gloss and analysis ID are all null, there
	--( are no annotations, but an analysis (and, possibly, a gloss) still might exist.

	IF @@ROWCOUNT = 0

		INSERT INTO @tblScore
		SELECT TOP 1
			oanalysis.[Id],
			wg.id,
			0
		FROM CmObject oanalysis (READUNCOMMITTED)
		left outer join WfiGloss_ wg on wg.owner$ = oanalysis.id
		WHERE oanalysis.Owner$ = @nWfiWordFormId

	RETURN
END
GO

if object_id('MakeMissingAnalysesFromLexicion') is not null begin
	if (select DbVer from Version$) = 200011
		print 'removing procedure MakeMissingAnalysesFromLexicion'
	drop proc MakeMissingAnalysesFromLexicion
end
go
if (select DbVer from Version$) = 200011
	print 'creating procedure MakeMissingAnalysesFromLexicion'
go
/***********************************************************************************************
 * MakeMissingAnalysesFromLexicion
 * Description: This procedure creates a WfiAnalyis and WfiGloss for words in a paragraph
 * that have no known analysis but which exactly match the form of an MoForm somewhere.
 * We begin by identifying unique WfiWordforms linked to the paragraph by an annotation
 * which own no WfiAnalyses, and which have a form that matches the form of some MoForm.
 * For each of these, we find the first sense of the lex entry that owns the MoForm, its gloss,
 * and (if possible) a PartOfSpeech that is the POS of the MoStemMsa that is the MSA of the
 * sense.
 * For each such WfiWordform, we create a WfiAnalysis and set its POS to the POS we found;
 * create a WfiMorphBundle owned by the WfiAnalysis, and set its sense, msa, and form;
 * and create a WfiGloss owned by the WfiAnalysis and set its form to the gloss of the sense.
 * In other words, we create an analysis that has one morpheme, the MoForm we found,
 * and let its sense and MSA be determined by the first sense of that form, both at the
 * morpheme and gloss level.
 * Parameters:
 *    @paraid - the paragraph we want to apply the heuristic to
 *    @ws - the ws we're using for glosses, typically DefaultAnalWs.
 * Return: 0 if successful, otherwise 1.
***********************************************************************************************/
create proc MakeMissingAnalysesFromLexicion
	@paraid int,
	@ws int
as

declare wf_cur cursor local static forward_only read_only for

select distinct wf.id wfid, mff.obj fid, ls.id lsid, msao.id msaid, lsg.Txt gloss, pos.id pos
	from CmBaseAnnotation_ cba (readuncommitted)
	join WfiWordform_ wf (readuncommitted) on  cba.BeginObject = @paraid and cba.InstanceOf = wf.id
	join WfiWordform_Form wff (readuncommitted) on wff.obj = wf.id
	left outer join WfiAnalysis_ wa on wa.owner$ = wf.id and wa.owner$ is null  -- faster version of and not exists (select * from WfiAnalysis_ wa where wa.owner$ = wf.id)
	join MoForm_Form mff (readuncommitted) on wff.Txt = mff.txt
	join CmObject mfo (readuncommitted) on mfo.id = mff.obj
	join CmObject leo (readuncommitted) on leo.id = mfo.owner$
	join LexSense_ ls (readuncommitted) on ls.owner$ = leo.id
	left outer join LexSense_Gloss lsg (readuncommitted) on lsg.obj = ls.id and lsg.ws = @ws
	left outer join CmObject msao (readuncommitted) on msao.id = ls.MorphoSyntaxAnalysis
	left outer join MoStemMsa msta (readuncommitted) on msta.id = ls.MorphoSyntaxAnalysis
	left outer join CmPossibility pos (readuncommitted) on pos.id = msta.PartOfSpeech

open wf_cur

declare @wfid int, @formid int, @senseid int,  @msaid int, @pos int
declare @gloss nvarchar(1000)
declare @NewObjGuid uniqueidentifier,
	@NewObjTimestamp int

-- kflidWfiWordform_Analyses 5062002
-- kflidWfiAnalysis_MorphBundles 5059011
-- kflidWfiAnalysis_Meanings 5059010
-- kflidWfiGloss_Form 50600001
fetch wf_cur into @wfid, @formid, @senseid, @msaid, @gloss, @pos
while @@fetch_status = 0 begin
	declare @analysisid int
	declare @mbid int
	declare @wgid int
	exec CreateObject_WfiAnalysis @wfid, 5062002, null, @analysisid out, @NewObjGuid out, 0, @NewObjTimestamp
	exec CreateObject_WfiMorphBundle null, null, null, @analysisid, 5059011, null, @mbid out, @NewObjGuid out, 0, @NewObjTimestamp
	exec CreateObject_WfiGloss @ws, @gloss, @analysisid, 5059010, null, @wgid out, @NewObjGuid out, 0, @NewObjTimestamp
	update WfiMorphBundle set Morph = @formid, Msa = @msaid, Sense = @senseid where id = @mbid
	update WfiAnalysis set Category = @pos where id = @analysisid
	fetch wf_cur into @wfid, @formid, @senseid, @msaid, @gloss, @pos
end
close wf_cur
deallocate wf_cur
go


declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200011
begin
	update Version$ set DbVer = 200012
	COMMIT TRANSACTION
	print 'database updated to version 200012'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200011 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
