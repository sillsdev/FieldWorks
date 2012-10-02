-- update database FROM version 200118 to 200119
BEGIN TRANSACTION  --( will be rolled back if wrong version#


/*****************************************************************************
 * MatchingEntries
 *
 * Description:
 *	Returns a table with zero or more rows of LexEntry objects which match in
 *	one or more of the parameters.  Set the string parameter to N'!' if you
 *  don't care about matching that particular item.  The @cf, @lf, and @af
 *  parameters are typically set to the same value.  Matching is always from
 *  the beginning of the string value, with any number of trailing characters
 *  allowed.
 * Parameters:
 *	@cf = the citation form to find.
 *	@lf = the lexeme form to find.
 *	@af = the allomorphs to find.
 *	@gl = the gloss to find.
 *	@wsv = vernacular ws for matching cf, lf, af
 *	@wsa = analysis ws for matching gl
 * Returns:
 *	Result set containing table of cf, lf, af, gl for each match, ordered by
 *  lf,cf,af,gl if @lf <> N'!', otherwise ordered by gl,lf,cf,af.
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
		DummyID int IDENTITY PRIMARY KEY,			-- 1 This row is returned at the end, but it is not used byt the client.
		EntryID int,						-- 2 Note: These numbers are to help the SP client knwo which column has which stuff.
		Class int,							-- 3
		CFTxt nvarchar(4000) default '***',	-- 4
		CFWs int,							-- 5
		LFTxt nvarchar(4000) default '***',	-- 6
		LFWs int,							-- 7
		AFTxt nvarchar(4000) default '***',	-- 8
		AFWs int,							-- 9
		GLTxt nvarchar(4000) default '***',	-- 10
		GLWs int							-- 11
		)

	--( We have no way to get at the User UI writing system, so we have to
	--( just use English. Thsi is used to set the WS for various strings, if they end up being '***',
	--( as the SP client needs a WS for each string.
	select top 1 @UIWs=id
	from LgWritingSystem (readuncommitted)
	where ICULocale='en'
	if @UIWs is null set @UIWs=@wsa

	--( NB: This query will return one, or more, rows for each matching entry.
	--( Since there can be multiple matching glosses and/or alternate forms,
	--( the entry will be 'duplicated' once for each multple matching gloss/alternate form.
	--( (See the notes on the following cursor for details on how these 'duplicates' are handled.)
	--( The fnGetSensesInEntry$ function is beleived to be too slow,
	--( so we only try to find matching glosses on the top level slnses here.
	--( This isn't what is really wanted, however, as the user wants to find matching glosses,
	--( no matter which sense it belongs to.
	--( I tried running the fnGetSensesInEntry$ on Dennis' data, and it took 11 seconds the first time
	--( it was called. It took no noticable time for subsequent calls in QA,
	--( and that was for gathering info for all entries and all senses.
	--( I (RandyR) think we could get by with using fnGetSensesInEntry$ here, if the SP's client has taken steps to assure
	--( the function had been called previously to get it into SQL Server's cache.
	INSERT INTO @MatchingEntries (EntryID, Class, CFTxt,CFWs, LFTxt,LFWs, AFTxt,AFWs, GLTxt,GLWs)
	SELECT le.Id 'EntryId', co.Class$ 'Class',
		isnull(cf.Txt, N'***') 'CFTxt', isnull(cf.Ws, @wsv) 'CFWs',
		isnull(lf.Txt, N'***') 'LFTxt', isnull(lf.Ws, @wsv) 'LFWs',
		isnull(af.Txt, N'***') 'AFTxt', isnull(af.Ws, @wsv) 'AFWs',
		isnull(gl.Txt, N'***') 'GLTxt', isnull(gl.Ws, @wsa) 'GLWs'
	FROM LexEntry le (readuncommitted)
	JOIN CmObject co (readuncommitted) ON co.Id = le.Id
	LEFT OUTER JOIN LexEntry_CitationForm cf (readuncommitted) ON cf.Obj=le.Id AND cf.Ws=@wsv
	LEFT OUTER JOIN LexEntry_LexemeForm lelf (readuncommitted) ON lelf.Src = le.Id
	LEFT OUTER JOIN MoForm_Form lf (readuncommitted) on lf.Obj = lelf.Dst AND lf.Ws=@wsv
	LEFT OUTER JOIN LexEntry_AlternateForms leaf (readuncommitted) ON leaf.Src = le.Id
	LEFT OUTER JOIN MoForm_Form af (readuncommitted) on af.Obj = leaf.Dst AND af.Ws=@wsv
	LEFT OUTER JOIN LexEntry_Senses les (readuncommitted) ON les.Src = le.Id
	LEFT OUTER JOIN LexSense_Gloss gl (readuncommitted) ON gl.Obj = les.Dst AND gl.Ws=@wsa
	WHERE LOWER(RTRIM(LTRIM(cf.Txt))) LIKE LOWER(RTRIM(LTRIM(@cf))) + '%'
	   OR LOWER(RTRIM(LTRIM(lf.Txt))) LIKE LOWER(RTRIM(LTRIM(@lf))) + '%'
	   OR LOWER(RTRIM(LTRIM(af.Txt))) LIKE LOWER(RTRIM(LTRIM(@af))) + '%'
	   OR LOWER(RTRIM(LTRIM(gl.Txt))) like LOWER(RTRIM(LTRIM(@gl))) + '%'
	ORDER BY le.Id, gl.Txt -- This is ordered by the entry id, so that like entry hvos are together for the following cursor to work on them.

	--( The spin through the @MatchingEntries table via the cursor will rectify the problem with
	--( having multiple rows per entry in the end (cf. LT-3458).
	--( It solves that problem by concatenating the glosses and/or forms into one of the entry rows,
	--( while deleting the other rows.
	--( The expected result at the end of the cursor run will be only one entry hvo per entry.
	DECLARE @cur CURSOR
	SET @cur = CURSOR FAST_FORWARD FOR
		SELECT DummyID, EntryId, AFTxt, GLTxt
		FROM @MatchingEntries
	DECLARE @dummyId int, @keeperDummyId int, @hvo int, @hvoPrev int
	DECLARE @alloTxt nvarchar(4000), @alloPrev nvarchar(4000), @alloNew nvarchar(4000)
	DECLARE @glossTxt nvarchar(4000), @glossPrev nvarchar(4000), @glossNew nvarchar(4000)
	SET @hvoPrev = 0
	SET @alloPrev = N''
	SET @glossPrev = N''
	OPEN @cur
	FETCH NEXT FROM @cur INTO @dummyId, @hvo, @alloTxt, @glossTxt
	WHILE @@FETCH_STATUS = 0
	BEGIN
		IF @hvo = @hvoPrev BEGIN
			SET @alloNew = @alloPrev
			--( If we already have some matching alternate form, then append it to the previous matching alternate forms.
			IF @alloPrev != @alloTxt SET @alloNew = @alloNew + N'; ' + @alloTxt
			SET @glossNew = @glossPrev
			--( If we already have some matching gloss, then append it to the previous matching gloss.
			IF @glossPrev != @glossTxt SET @glossNew = @glossNew + N'; ' + @glossTxt
			--( Update the alternate form and gloss info entry row,
			--( but only for the 'keeper' row.
			UPDATE @MatchingEntries
			SET AFTxt=@alloNew, GLTxt=@glossNew
				WHERE DummyID=@keeperDummyId
			--( Delete the extra entry row for the current dummy id.
			--( I (RandyR) think this approach with using a dummy id in the table is the real fix for LT-3178,
			--( as the previous code could have been overzealous in deleting too many rows.
			DELETE FROM @MatchingEntries
				WHERE DummyID=@dummyId
			SET @alloPrev=@alloNew
			SET @glossPrev=@glossNew
		END
		ELSE BEGIN
			--( We have found a new entry hvo, so remember some crucial information,
			--( which will be used in case the @MatchingEntries table has rows with duplicated entry hvos.
			--( If the entry hvo is not the same for the next rwo in the cursor, than all this remembering if for nothing,
			--( but we can't tell at this point, so we have to remember it.
			SET @alloPrev=@alloTxt
			SET @glossPrev=@glossTxt
			-- We need to remember which row is to be updated in the code above,
			-- which handles cases where the entry hvo are the same.
			SET @keeperDummyId = @dummyId
		END
		SET @hvoPrev=@hvo

		FETCH NEXT FROM @cur INTO @dummyId, @hvo, @alloTxt, @glossTxt
	END
	CLOSE @cur
	DEALLOCATE @cur
	--( By this point, there should not be any rows with repeating entry hvos.

	-- Set some wses to the uiws
	--( Set the ws for the citation form, but only if it is '***'.
	update @MatchingEntries
	set CFWs=@UIWs
	where CFTxt='***'
	--( Set the ws for the lexeme form, but only if it is '***'.
	update @MatchingEntries
	set LFWs=@UIWs
	where LFTxt = '***'

	--==( Near Final Pass )==--
	-- Try to find some kind of string for any items that have not matched,
	-- The rationale for finding some other string, even if it didn't match,
	-- is to help the user determine if an entry already exists/matches.
		-- REVIEW (SteveMiller): This "final pass" can probably be enhanced by
		-- moving the logic into the query
		-- 	select * from @MatchingEntries
	declare @curFinalPass CURSOR, @rowcount int, @wsFoundling int
	set @curFinalPass = CURSOR FAST_FORWARD for
		select EntryID, AFTxt, GLTxt -- , CFTxt, LFTxt
		from @MatchingEntries
		where
			AFTxt = '***'
			or GLTxt = '***'
			-- or LFTxt = '***' (Leave out for now, as per LT-2545, but leave code in, for when it gets restored, however.)
			-- or CFTxt = '***' (Leave out for now, as per LT-2545, but leave code in, for when it gets restored, however.)
	OPEN @curFinalPass
	FETCH NEXT FROM @curFinalPass INTO @entryID, @aftext, @gltext -- , @cftext, @lftext
	WHILE @@FETCH_STATUS = 0
	BEGIN
		set @rowcount = 0
		--( Just leave the *** for missing CF, as per LT-2545.
		--( Leave code in, for when it gets restored, however.
		-- if @cftext = '***'
		--	update @MatchingEntries
		--	set CFWs=@UIWs
		--	where EntryID=@entryID
		--if @lftext = '***'
		--	update @MatchingEntries
		--	set LFWs=@UIWs
		--	where EntryID=@entryID
		if @aftext = '***'
		begin --( @aftext = '***'
			--( Some form is better than nothing at all, so try to get one.
			--( Try getting a non-match in the given @wsv writing system.
			select top 1 @AFTxt=amf.Txt, @wsFoundling=@wsv
			from LexEntry_ le (readuncommitted)
			join LexEntry_AlternateForms lea (readuncommitted) ON lea.Src = le.id
			join MoForm_Form amf (readuncommitted) ON amf.Obj = lea.Dst and amf.ws = @wsv
			where le.id = @entryID
			set @rowcount = @@rowcount
			if @rowcount = 0
			begin
				-- No non-match exists in the @wsv ws, so try all of them.
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
		end --( @aftext = '***'

		if @gltext = '***'
		begin
			--( Some gloss is better than nothing at all, so try to get one.
			-- Table variable to hold all owned senses, whether directly owned by the entry or by some other sense.
			-- Since the fnGetSensesInEntry$ may be called twice, then store the results of its call once,
			-- and use it up to two times.
			DECLARE @OwnedSenses TABLE (SenseID INT PRIMARY KEY)
			INSERT INTO @OwnedSenses (SenseID)
			SELECT SenseID
			FROM dbo.fnGetSensesInEntry$(@entryID)

			--( Try getting a non-match in the given @wsa writing system.
			SELECT top 1 @GLTxt=lsg.Txt, @wsFoundling=@wsa
			FROM @OwnedSenses os
			JOIN LexSense_Gloss lsg (readuncommitted)
				ON lsg.Obj = os.SenseId AND lsg.ws = @wsa
			SET @rowcount = @@rowcount
			if @rowcount = 0
			begin
				-- No non-match exists in the wsa ws, so try all of them.
				SELECT top 1 @GLTxt=lsg.Txt, @wsFoundling=lsg.Ws
				FROM @OwnedSenses os
				JOIN LexSense_Gloss lsg (readuncommitted)
					ON lsg.Obj = os.SenseId
				JOIN LanguageProject_CurrentAnalysisWritingSystems ws (readuncommitted)
					ON ws.Dst = lsg.Ws
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

	IF @lf <> N'!'
		SELECT * FROM @MatchingEntries ORDER BY LFTxt, CFTxt, AFTxt, GLTxt
	ELSE
		SELECT * FROM @MatchingEntries ORDER BY GLTxt, LFTxt, CFTxt, AFTxt
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200118
begin
	UPDATE Version$ SET DbVer = 200119
	COMMIT TRANSACTION
	print 'database updated to version 200119'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200118 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO