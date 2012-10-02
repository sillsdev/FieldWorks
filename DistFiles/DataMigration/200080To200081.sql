-- Update database from version 200080 to 200081
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Update four PartOfSpeech abbreviations in the three supported writing systems.
DECLARE @en int, @es int, @fr int, @posId int

SELECT top 1 @en=Id
FROM LgWritingSystem
WHERE ICULocale=N'en'

SELECT top 1 @es=Id
FROM LgWritingSystem
WHERE ICULocale=N'es'

SELECT top 1 @fr=Id
FROM LgWritingSystem
WHERE ICULocale=N'fr'

-- Adverb
SELECT @posId=Id
FROM PartOfSpeech
WHERE CatalogSourceId=N'Adverb'
-- Update the abbreviations in the three supported writing systems.
UPDATE CmPossibility_Abbreviation
SET Txt=N'Adv'
WHERE Obj=@posId AND Ws=@en

UPDATE CmPossibility_Abbreviation
SET Txt=N'Adv'
WHERE Obj=@posId AND Ws=@es

UPDATE CmPossibility_Abbreviation
SET Txt=N'Adv'
WHERE Obj=@posId AND Ws=@fr

-- Noun
SELECT @posId=Id
FROM PartOfSpeech
WHERE CatalogSourceId=N'Noun'
-- Update the abbreviations in the three supported writing systems.
UPDATE CmPossibility_Abbreviation
SET Txt=N'N'
WHERE Obj=@posId AND Ws=@en

UPDATE CmPossibility_Abbreviation
SET Txt=N'Sus'
WHERE Obj=@posId AND Ws=@es

UPDATE CmPossibility_Abbreviation
SET Txt=N'N'
WHERE Obj=@posId AND Ws=@fr

-- Pronoun
SELECT @posId=Id
FROM PartOfSpeech
WHERE CatalogSourceId=N'Pronoun'
-- Update the abbreviations in the three supported writing systems.
UPDATE CmPossibility_Abbreviation
SET Txt=N'Pro'
WHERE Obj=@posId AND Ws=@en

UPDATE CmPossibility_Abbreviation
SET Txt=N'Pro'
WHERE Obj=@posId AND Ws=@es

UPDATE CmPossibility_Abbreviation
SET Txt=N'Pro'
WHERE Obj=@posId AND Ws=@fr

-- Verb
SELECT @posId=Id
FROM PartOfSpeech
WHERE CatalogSourceId=N'Verb'
-- Update the abbreviations in the three supported writing systems.
UPDATE CmPossibility_Abbreviation
SET Txt=N'V'
WHERE Obj=@posId AND Ws=@en

UPDATE CmPossibility_Abbreviation
SET Txt=N'V'
WHERE Obj=@posId AND Ws=@es

UPDATE CmPossibility_Abbreviation
SET Txt=N'V'
WHERE Obj=@posId AND Ws=@fr

GO

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
		@class int, @UIWs int
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

	select top 1 @UIWs=id
	from LgWritingSystem
	where ICULocale='en'
	if @UIWs is null set @UIWs=@wsa

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
			insert into @MatchingEntries (EntryID,  Class,  AFTxt,  CFWs, LFWs, AFWs, GLWs)
			values (		      @entryID, @class, @AFTxt, @wsv, @wsv, @wsv, @wsa)
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
			insert into @MatchingEntries (EntryID,  Class,  GLTxt,  CFWs, LFWs, AFWs, GLWs)
			values (		      @entryID, @class, @GLTxt, @wsv, @wsv, @wsv, @wsa)
		else
			update @MatchingEntries
			set GLTxt=@GLTxt
			where EntryID=@entryID

		set @prevID = @senseId
		FETCH NEXT FROM @curSenses INTO @senseId, @gltext
	END
	CLOSE @curSenses
	DEALLOCATE @curSenses

	-- Set some wses to the uiws
	update @MatchingEntries
	set CFWs=@UIWs
	where CFTxt='***'
	update @MatchingEntries
	set LFWs=@UIWs
	where LFTxt = '***'

	--==( Near Final Pass )==--
	-- REVIEW (SteveMiller): This "final pass" can probably be enhanced by
	-- moving the logic into the query
	-- 	select * from @MatchingEntries
	-- Try to find some kind of string for any items that have not matched,
	declare @curFinalPass CURSOR, @rowcount int, @wsFoundling int
	set @curFinalPass = CURSOR FAST_FORWARD for
		select EntryID, AFTxt, GLTxt -- , CFTxt, LFTxt
		from @MatchingEntries
		where
			AFTxt = '***'
			or GLTxt = '***'
			-- or LFTxt = '***'
			-- or CFTxt = '***'

	OPEN @curFinalPass
	FETCH NEXT FROM @curFinalPass INTO @entryID, @aftext, @gltext -- , @cftext, @lftext
	WHILE @@FETCH_STATUS = 0
	BEGIN
		set @rowcount = 0
		--( Just leave the *** for missing CF, as per LT-2545.
		-- if @cftext = '***'
		--	update @MatchingEntries
		--	set CFWs=@UIWs
		--	where EntryID=@entryID
		--if @lftext = '***'
		--	update @MatchingEntries
		--	set LFWs=@UIWs
		--	where EntryID=@entryID
		if @aftext = '***'
		begin
			select top 1 @AFTxt=amf.Txt, @wsFoundling=@wsv
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
				update @MatchingEntries
				set AFTxt=@AFTxt, AFWs=@wsFoundling
				where EntryID=@entryID
			else
				update @MatchingEntries
				set AFWs=@UIWs
				where EntryID=@entryID
		end
		if @gltext = '***'
		begin
			SELECT top 1 @GLTxt=lsg.Txt, @wsFoundling=@wsa
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
				update @MatchingEntries
				set GLTxt=@GLTxt, GLWs=@wsFoundling
				where EntryID=@entryID
			else
				update @MatchingEntries
				set GLWs=@UIWs
				where EntryID=@entryID
		end
		FETCH NEXT FROM @curFinalPass INTO @entryID, @aftext, @gltext -- , @cftext, @lftext
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
if @dbVersion = 200080
begin
	update Version$ set DbVer = 200081
	COMMIT TRANSACTION
	print 'database updated to version 200081'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200080 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
