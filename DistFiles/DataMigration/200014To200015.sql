-- update database from version 200014 to 200015
BEGIN TRANSACTION

if object_id('MatchingEntries') is not null begin
	if (select DbVer from Version$) = 200014
		print 'removing procedure MatchingEntries'
	drop proc MatchingEntries
end
if (select DbVer from Version$) = 200014
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


declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200014
begin
	update Version$ set DbVer = 200015
	COMMIT TRANSACTION
	print 'database updated to version 200015'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200014 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
