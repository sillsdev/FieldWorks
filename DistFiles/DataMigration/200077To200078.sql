-- Update database from version 200077 to 200078
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Obtain the best possible default magic binary format for any description strings.
DECLARE @rgbFmt VARBINARY(8000), @wsEn INT, @txt NVARCHAR(4000), @mmtId INT, @flid INT --, @lrt int, @st int, @stp int, @guid uniqueidentifier
SELECT TOP 1 @wsEn=id FROM LgWritingSystem WHERE iculocale = 'en'
SELECT TOP 1 @rgbFmt=Fmt FROM MultiBigStr$ WHERE Ws=@wsEn ORDER BY DATALENGTH(Fmt)
SET @flid=7003

-- Update descriptions of all of the morph types.
SELECT TOP 1 @mmtId=Id FROM CmObject WHERE Guid$='56DB04BF-3D58-44CC-B292-4C8AA68538F4'
SET @txt=N'A particle is a word that does not belong to one of the main classes of words, is invariable in form, and typically has grammatical or pragmatic meaning.'
EXEC SetMultiBigStr$ @flid, @mmtId, @wsEn, @txt, @rgbFmt

SELECT TOP 1 @mmtId=Id FROM CmObject WHERE Guid$='D7F713DA-E8CF-11D3-9764-00C04F186933'
SET @txt=N'An infix is an affix that is inserted within a root or stem.'
EXEC SetMultiBigStr$ @flid, @mmtId, @wsEn, @txt, @rgbFmt

SELECT TOP 1 @mmtId=Id FROM CmObject WHERE Guid$='D7F713DB-E8CF-11D3-9764-00C04F186933'
SET @txt=N'A prefix is an affix that is joined before a root or stem.'
EXEC SetMultiBigStr$ @flid, @mmtId, @wsEn, @txt, @rgbFmt

SELECT TOP 1 @mmtId=Id FROM CmObject WHERE Guid$='D7F713DC-E8CF-11D3-9764-00C04F186933'
SET @txt=N'A simulfix is a change or replacement of vowels or consonants (usually vowels) which changes the meaning of a word.  (Note: the parser does not currently handle simulfixes.)'
EXEC SetMultiBigStr$ @flid, @mmtId, @wsEn, @txt, @rgbFmt

SELECT TOP 1 @mmtId=Id FROM CmObject WHERE Guid$='D7F713DD-E8CF-11D3-9764-00C04F186933'
SET @txt=N'A suffix is an affix that is attached to the end of a root or stem.'
EXEC SetMultiBigStr$ @flid, @mmtId, @wsEn, @txt, @rgbFmt

SELECT TOP 1 @mmtId=Id FROM CmObject WHERE Guid$='D7F713DE-E8CF-11D3-9764-00C04F186933'
SET @txt=N'A suprafix is a kind of affix in which a suprasegmental is superimposed on one or more syllables of the root or stem, signalling a particular  morphosyntactic operation.  (Note: the parser does not currently handle suprafixes.)'
EXEC SetMultiBigStr$ @flid, @mmtId, @wsEn, @txt, @rgbFmt

SELECT TOP 1 @mmtId=Id FROM CmObject WHERE Guid$='D7F713DF-E8CF-11D3-9764-00C04F186933'
SET @txt=N'A circumfix is an affix made up of two separate parts which surround and attach to a root or stem.'
EXEC SetMultiBigStr$ @flid, @mmtId, @wsEn, @txt, @rgbFmt

SELECT TOP 1 @mmtId=Id FROM CmObject WHERE Guid$='D7F713E1-E8CF-11D3-9764-00C04F186933'
SET @txt=N'An enclitic is a clitic that is phonologically joined at the end of a preceding word to form a single unit.'
EXEC SetMultiBigStr$ @flid, @mmtId, @wsEn, @txt, @rgbFmt

SELECT TOP 1 @mmtId=Id FROM CmObject WHERE Guid$='D7F713E2-E8CF-11D3-9764-00C04F186933'
SET @txt=N'A proclitic is a clitic that precedes the word to which it is phonologically joined.'
EXEC SetMultiBigStr$ @flid, @mmtId, @wsEn, @txt, @rgbFmt

SELECT TOP 1 @mmtId=Id FROM CmObject WHERE Guid$='D7F713E4-E8CF-11D3-9764-00C04F186933'
SET @txt=N'A bound root is a root which cannot occur as a separate word apart from any other morpheme.'
EXEC SetMultiBigStr$ @flid, @mmtId, @wsEn, @txt, @rgbFmt

SELECT TOP 1 @mmtId=Id FROM CmObject WHERE Guid$='D7F713E5-E8CF-11D3-9764-00C04F186933'
SET @txt=N'A root is the portion of a word that (i) is common to a set of derived or inflected forms, if any, when all affixes are removed, (ii) is not further analyzable into meaningful elements, being morphologically simple, and, (iii) carries the principle portion of meaning of the words in which it functions.'
EXEC SetMultiBigStr$ @flid, @mmtId, @wsEn, @txt, @rgbFmt

SELECT TOP 1 @mmtId=Id FROM CmObject WHERE Guid$='D7F713E7-E8CF-11D3-9764-00C04F186933'
SET @txt=N'A bound stem is a stem  which cannot occur as a separate word apart from any other morpheme.'
EXEC SetMultiBigStr$ @flid, @mmtId, @wsEn, @txt, @rgbFmt

SELECT TOP 1 @mmtId=Id FROM CmObject WHERE Guid$='D7F713E8-E8CF-11D3-9764-00C04F186933'
SET @txt=N'A stem is the root or roots of a word, together with any derivational affixes, to which inflectional affixes are added." (LinguaLinks Library).  A stem "may consist solely of a single root morpheme (i.e. a ''simple'' stem as in ''man''), or of two root morphemes (e.g. a ''compound'' stem, as in ''blackbird''), or of a root morpheme plus a derivational affix (i.e. a ''complex'' stem, as in ''manly'', ''unmanly'', ''manliness'').  All have in common the notion that it is to the stem that inflectional affixes are attached." (Crystal, 1997:362)'
EXEC SetMultiBigStr$ @flid, @mmtId, @wsEn, @txt, @rgbFmt

SELECT TOP 1 @mmtId=Id FROM CmObject WHERE Guid$='18D9B1C3-B5B6-4c07-B92C-2FE1D2281BD4'
SET @txt=N'An infixing interfix is an infix that can occur between two roots or stems.'
EXEC SetMultiBigStr$ @flid, @mmtId, @wsEn, @txt, @rgbFmt

SELECT TOP 1 @mmtId=Id FROM CmObject WHERE Guid$='AF6537B0-7175-4387-BA6A-36547D37FB13'
SET @txt=N'A prefixing interfix is a prefix that can occur between two roots or stems.'
EXEC SetMultiBigStr$ @flid, @mmtId, @wsEn, @txt, @rgbFmt

SELECT TOP 1 @mmtId=Id FROM CmObject WHERE Guid$='3433683D-08A9-4bae-AE53-2A7798F64068'
SET @txt=N'A suffixing interfix is an suffix that can occur between two roots or stems.'
EXEC SetMultiBigStr$ @flid, @mmtId, @wsEn, @txt, @rgbFmt

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
if object_id('MatchingEntries') is not null begin
	print 'removing proc MatchingEntries'
	drop proc MatchingEntries
end
print 'creating proc MatchingEntries'
go

CREATE    proc [MatchingEntries]
	@cf nvarchar(4000),
	@lf nvarchar(4000),
	@af nvarchar(4000),
	@gl nvarchar(4000),
	@wsv int,
	@wsa int
AS
	declare @CFTxt nvarchar(4000), @cftext nvarchar(4000),
		@LFTxt nvarchar(4000), @lftext nvarchar(4000),
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
		LFTxt nvarchar(4000) default '***',	-- 5
		LFWs int,				-- 6
		AFTxt nvarchar(4000) default '***',	-- 7
		AFWs int,				-- 8
		GLTxt nvarchar(4000) default '***',	-- 9
		GLWs int				-- 10
		)

	--==( Citation and Lexeme Forms )==--
	--( We're looking for citation forms or lexeme forms that match
	--( the citation or lexeme forms passed in.

	-- REVIEW (SteveMiller): LexEntry_CitationForm and MoForm_Form both take
	-- writing system IDs. If you are interested in only one writing system,
	-- the query should go faster by joining on ws as well as on obj, to make
	-- better use of the indexes. If more than one writing system can be returned,
	-- we'll have to put more thought into how to retrieve the proper Txt
	-- field from the appropriate writing system.
	insert into @MatchingEntries (EntryID, Class,     CFTxt,                 CFWs, LFTxt,                  LFWs, AFWs, GLWs)
		SELECT	              le.[Id], le.Class$, isnull(cf.Txt, '***'), @wsv, isnull(mff.Txt, '***'), @wsv, @wsv, @wsa
		FROM LexEntry_ le (READUNCOMMITTED)
		LEFT OUTER JOIN LexEntry_CitationForm cf (READUNCOMMITTED) ON cf.Obj = le.[Id] and cf.ws = @wsv
		LEFT OUTER JOIN LexEntry_LexemeForm lf (READUNCOMMITTED) ON lf.Src = le.[Id]
		LEFT OUTER JOIN MoForm_Form mff (readuncommitted) ON mff.Obj = lf.Dst and mff.ws = @wsv
		WHERE (LOWER(RTRIM(LTRIM(cf.Txt))) LIKE LOWER(RTRIM(LTRIM(@cf))) + '%'
			OR LOWER(RTRIM(LTRIM(mff.Txt))) LIKE LOWER(RTRIM(LTRIM(@lf))) + '%')

	--==( Alternate Allomorph Forms )==--

	-- REVIEW (SteveMiller): Cursors are notoriously slow in databases. I
	-- expect these to bog down the proc as soon as we get any quantity of
	-- data. As of this writing, we have 62 records in LexEntry.

	--( We're looking for allomorph forms that match the allomorph form
	--( passed in.

	declare @curAllos CURSOR
	set @curAllos = CURSOR FAST_FORWARD for
		select le.id, le.Class$, amf.Txt
		from LexEntry_ le (readuncommitted)
		join LexEntry_AlternateForms lea (readuncommitted) ON lea.Src = le.id
		join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst and amf.ws = @wsv
		where LOWER(RTRIM(LTRIM(amf.Txt))) LIKE LOWER(RTRIM(LTRIM(@af))) + '%'

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
			insert into @MatchingEntries (EntryID, Class, AFTxt, CFWs, LFWs, AFWs, GLWs)
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
			insert into @MatchingEntries (EntryID, Class, GLTxt, CFWs, LFWs, AFWs, GLWs)
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
		select EntryID, CFTxt, LFTxt, AFTxt, GLTxt
		from @MatchingEntries
		where	LFTxt = '***'
			or AFTxt = '***'
			or GLTxt = '***'
			-- or CFTxt = '***'

	OPEN @curFinalPass
	FETCH NEXT FROM @curFinalPass INTO @entryID, @cftext, @lftext, @aftext, @gltext
	WHILE @@FETCH_STATUS = 0
	BEGIN
		set @rowcount = 0
		--( Just leave the *** for missing CF, as per LT-2545.
		--( if @cftext = '***'
		--( begin
		--( 	-- Try a ws other than @wsv, since it wasn't there earlier.
		--( 	select top 1 @CFTxt=Txt
		--( 	from LexEntry_CitationForm (readuncommitted)
		--( 	where Obj = @entryID
		--( 	set @rowcount = @@rowcount
		--(
		--( 	if @rowcount = 0 -- Nothing for any other ws, so try getting it from the *last* allomorph.
		--( 	begin
		--( 		-- See if the lexeme form has it for @wsv.
		--( 		select @CFTxt=amf.Txt, @wsFoundling=amf.Ws
		--( 		from LexEntry_ le (readuncommitted)
		--( 		join LexEntry_LexemeForm lea (readuncommitted) ON lea.Src = le.id
		--( 		join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst and amf.ws = @wsv
		--( 		where le.id = @entryID
		--( 		set @rowcount = @@rowcount
		--( 	end
		--(
		--( 	if @rowcount = 0 -- Try any other ws on the lexeme form
		--( 	begin
		--( 		select top 1 @CFTxt=amf.Txt, @wsFoundling=amf.Ws
		--( 		from LexEntry_ le (readuncommitted)
		--( 		join LexEntry_LexemeForm lea (readuncommitted) ON lea.Src = le.id
		--( 		join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst
		--( 		join LanguageProject_CurrentVernacularWritingSystems ws (readuncommitted) ON ws.Dst = amf.Ws
		--( 		where le.id = @entryID
		--( 		ORDER BY ws.ord
		--( 		set @rowcount = @@rowcount
		--( 	end
		--(
		--( 	if @rowcount > 0 -- Found one somewhere.
		--( 	begin
		--( 		update @MatchingEntries
		--( 		set CFTxt=@CFTxt, GLWs=@wsFoundling
		--( 		where EntryID=@entryID
		--( 	end
		--( end
		if @lftext = '***'
		begin
			select top 1 @LFTxt=lff.Txt
			from LexEntry_LexemeForm lf (readuncommitted)
			join MoForm_Form lff (readuncommitted) ON lff.Obj = lf.Dst and lff.ws = @wsv
			where lf.Src = @entryID
			set @rowcount = @@rowcount

			if @rowcount = 0 -- Nothing for the @wsv ws, so try something for any ws on the real UF.
			begin
				select top 1 @LFTxt=lff.Txt, @wsFoundling=lff.Ws
				from LexEntry_LexemeForm lf (readuncommitted)
				join MoForm_Form lff (readuncommitted) ON lff.Obj = lf.Dst
				join LanguageProject_CurrentVernacularWritingSystems ws (readuncommitted) ON ws.Dst = lff.Ws
				where lf.Src = @entryID
				ORDER BY ws.Ord
				set @rowcount = @@rowcount
			end

--			if @rowcount = 0 -- Try @wsv on the *last* allomorph
--			begin
--				select top 1 @UFTxt=amf.Txt, @wsFoundling=amf.Ws
--				from LexEntry_ le (readuncommitted)
--				join LexEntry_Allomorphs lea (readuncommitted) ON lea.Src = le.id
--				join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst and amf.ws = @wsv
--				where le.id = @entryID
--				ORDER BY lea.Ord DESC
--				set @rowcount = @@rowcount
--			end
--
--			if @rowcount = 0 -- Try any other ws on the *last* allomorph
--			begin
--				select top 1 @UFTxt=amf.Txt, @wsFoundling=amf.Ws
--				from LexEntry_ le (readuncommitted)
--				join LexEntry_Allomorphs lea (readuncommitted) ON lea.Src = le.id
--				join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst
--				join LanguageProject_CurrentVernacularWritingSystems ws (readuncommitted) ON ws.Dst = amf.Ws
--				where le.id = @entryID
--				ORDER BY lea.Ord DESC, ws.ord
--				set @rowcount = @@rowcount
--			end

			if @rowcount > 0 -- Found one somewhere.
			begin
				update @MatchingEntries
				set LFTxt=@LFTxt, GLWs=@wsFoundling
				where EntryID=@entryID
			end
		end
		if @aftext = '***'
		begin
			select top 1 @AFTxt=amf.Txt
			from LexEntry_ le (readuncommitted)
			join LexEntry_AlternateForms lea (readuncommitted) ON lea.Src = le.id
			join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst and amf.ws = @wsv
			where le.id = @entryID
			set @rowcount = @@rowcount

			if @rowcount = 0 -- Nothing for the @wsv ws, so try all of them.
			begin
				SELECT top 1 @AFTxt=amf.Txt, @wsFoundling=amf.Ws
				from LexEntry_ le (readuncommitted)
				join LexEntry_AlternateForms lea (readuncommitted) ON lea.Src = le.id
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
		FETCH NEXT FROM @curFinalPass INTO @entryID, @cftext, @lftext, @aftext, @gltext
	END
	CLOSE @curFinalPass
	DEALLOCATE @curFinalPass

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	select *
	from @MatchingEntries

GO

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200077
begin
	update Version$ set DbVer = 200078
	COMMIT TRANSACTION
	print 'database updated to version 200078'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200077 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
