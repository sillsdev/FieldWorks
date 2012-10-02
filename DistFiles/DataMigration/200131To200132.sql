-- update database FROM version 200131 to 200132
BEGIN TRANSACTION  --( will be rolled back if wrong version#
/*
 * Enhanced MatchingEntries to return lexeme form and citation form for gloss searches
 */

if object_id('MatchingEntries') is not null begin
	print 'removing proc MatchingEntries'
	drop proc MatchingEntries
end
print 'creating proc MatchingEntries'
go

CREATE    proc [MatchingEntries]
	@exactMatch BIT = 0,
	@cf nvarchar(4000),
	@lf nvarchar(4000),
	@af nvarchar(4000),
	@gl nvarchar(4000),
	@wsv int,
	@wsa int
AS
--( DECLARE @mainStart DATETIME,
--( 	@smallStart DATETIME,
--( 	@minorStart DATETIME,
--( 	@innerStart DATETIME

	declare @aftext nvarchar(4000),
		@gltext nvarchar(4000),
		@entryID int
	declare @fIsNocountOn int
	-- deterimine if no count is currently set to on
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- table variable to hold (sub)sense+entry id information.
--( SET @mainStart = GETDATE()
--( SET @smallStart = @mainStart
	declare @sensesAndEntries TABLE (
		SenseId INT PRIMARY KEY,
		EntryId	INT)
	INSERT INTO @sensesAndEntries (SenseId, EntryId)
	SELECT SenseId, EntryId
	FROM fnGetSensesAndEntries$()
--( PRINT '@sensesAndEntries rowcount: ' + CAST(@@ROWCOUNT AS varchar(12))
--( PRINT 'Time to load @sensesAndEntries table: ' + CAST(DATEDIFF(millisecond, @smallStart, getdate()) AS varchar(12))

	-- table variable to hold return information.
	declare @MatchingEntries table (
		DummyID int IDENTITY PRIMARY KEY,			-- 1 This row is returned at the end, but it is not used byt the client.
		EntryID int,						-- 2 Note: These numbers are to help the SP client knwo which column has which stuff.
		CFTxt nvarchar(4000) default N'***',	-- 4
		CFWs int,							-- 5
		LFTxt nvarchar(4000) default N'***',	-- 6
		LFWs int,							-- 7
		AFTxt nvarchar(4000) default N'***',	-- 8
		AFWs int,							-- 9
		GLTxt nvarchar(4000) default N'***',	-- 10
		GLWs int)							-- 11
	--( NB: These queries will return one or more rows for each matching entry.
	--( Since there can be multiple matching glosses and/or alternate forms,
	--( the entry will be 'duplicated' once for each multple matching gloss/alternate form.
	--( (See the notes on the following cursor for details on how these 'duplicates' are handled.)
--( SET @smallStart = GETDATE()
	IF @cf <> N'!'
	BEGIN
		IF @exactMatch = 1 BEGIN
			INSERT INTO @MatchingEntries (EntryID, CFTxt,CFWs)
			SELECT cf.Obj, cf.Txt, cf.Ws
			FROM LexEntry_CitationForm cf (readuncommitted)
			WHERE LOWER(RTRIM(LTRIM(cf.Txt))) = LOWER(RTRIM(LTRIM(@cf)))
				AND cf.Ws = @wsv
		END ELSE BEGIN
			INSERT INTO @MatchingEntries (EntryID, CFTxt,CFWs)
			SELECT cf.Obj, cf.Txt, cf.Ws
			FROM LexEntry_CitationForm cf (readuncommitted)
			WHERE LOWER(RTRIM(LTRIM(cf.Txt))) LIKE LOWER(RTRIM(LTRIM(@cf))) + N'%'
				AND cf.Ws = @wsv
		END
	END
--( PRINT 'Time to gather citation form matches: ' + CAST(DATEDIFF(millisecond, @smallStart, getdate()) AS varchar(12))

--( SET @smallStart = GETDATE()
	IF @lf <> N'!'
	BEGIN
		IF @exactMatch = 1 BEGIN
			INSERT INTO @MatchingEntries (EntryID, LFTxt,LFWs)
			SELECT lelf.Src, lf.Txt, lf.Ws
			FROM LexEntry_LexemeForm lelf (readuncommitted)
			JOIN MoForm_Form lf (readuncommitted) on lf.Obj = lelf.Dst
			WHERE LOWER(RTRIM(LTRIM(lf.Txt))) = LOWER(RTRIM(LTRIM(@lf)))
				 AND lf.Ws = @wsv
		END ELSE BEGIN
			INSERT INTO @MatchingEntries (EntryID, LFTxt,LFWs)
			SELECT lelf.Src, lf.Txt, lf.Ws
			FROM LexEntry_LexemeForm lelf (readuncommitted)
			JOIN MoForm_Form lf (readuncommitted) on lf.Obj = lelf.Dst
			WHERE LOWER(RTRIM(LTRIM(lf.Txt))) LIKE LOWER(RTRIM(LTRIM(@lf))) + N'%'
				AND lf.Ws = @wsv
		END
	END
--( PRINT 'Time to gather lexeme form matches: ' + CAST(DATEDIFF(millisecond, @smallStart, getdate()) AS varchar(12))

--( SET @smallStart = GETDATE()
	IF @af <> N'!'
	BEGIN
		IF @exactMatch = 1 BEGIN
			INSERT INTO @MatchingEntries (EntryID, AFTxt,AFWs)
			SELECT leaf.Src, af.Txt, af.Ws
			FROM LexEntry_AlternateForms leaf (readuncommitted)
			JOIN MoForm_Form af (readuncommitted) on af.Obj = leaf.Dst
			WHERE LOWER(RTRIM(LTRIM(af.Txt))) = LOWER(RTRIM(LTRIM(@af)))
				AND af.Ws = @wsv
		END ELSE BEGIN
			INSERT INTO @MatchingEntries (EntryID, AFTxt,AFWs)
			SELECT leaf.Src, af.Txt, af.Ws
			FROM LexEntry_AlternateForms leaf (readuncommitted)
			JOIN MoForm_Form af (readuncommitted) on af.Obj = leaf.Dst
			WHERE LOWER(RTRIM(LTRIM(af.Txt))) LIKE LOWER(RTRIM(LTRIM(@af))) + N'%'
				AND af.Ws = @wsv
		END
	END
--( PRINT 'Time to gather alternate form matches: ' + CAST(DATEDIFF(millisecond, @smallStart, getdate()) AS varchar(12))

--( SET @smallStart = GETDATE()
	IF @gl <> N'!'
	BEGIN
		IF @exactMatch = 1 BEGIN
			INSERT INTO @MatchingEntries (EntryID, GLTxt,GLWs)
			SELECT les.EntryId, gl.Txt, gl.Ws
			FROM @sensesAndEntries les
			JOIN LexSense_Gloss gl (readuncommitted) ON gl.Obj = les.SenseId AND gl.Ws=@wsa
			WHERE LOWER(RTRIM(LTRIM(gl.Txt))) = LOWER(RTRIM(LTRIM(@gl)))
				AND gl.Ws = @wsa
		END ELSE BEGIN
			INSERT INTO @MatchingEntries (EntryID, GLTxt,GLWs)
			SELECT les.EntryId, gl.Txt, gl.Ws
			FROM @sensesAndEntries les
			JOIN LexSense_Gloss gl (readuncommitted) ON gl.Obj = les.SenseId AND gl.Ws=@wsa
			WHERE LOWER(RTRIM(LTRIM(gl.Txt))) like LOWER(RTRIM(LTRIM(@gl))) + N'%'
				AND gl.Ws = @wsa
		END
	END
--( PRINT 'Time to gather gloss matches: ' + CAST(DATEDIFF(millisecond, @smallStart, getdate()) AS varchar(12))

--( SET @smallStart = GETDATE()
	DECLARE @hvo INT
	DECLARE @dummyId int, @keeperDummyId int, @hvoPrev int
	DECLARE @citFTxt nvarchar(4000), @citFPrev nvarchar(4000), @citFNew nvarchar(4000)
	DECLARE @lexFTxt nvarchar(4000), @lexFPrev nvarchar(4000), @lexFNew nvarchar(4000)
	DECLARE @alloTxt nvarchar(4000), @alloPrev nvarchar(4000), @alloNew nvarchar(4000)
	DECLARE @glossTxt nvarchar(4000), @glossPrev nvarchar(4000), @glossNew nvarchar(4000)
	--( Loops through all entries that have multiple rows in @MatchingEntries.
	SELECT TOP 1 @hvo = EntryId
	FROM @MatchingEntries
	GROUP BY EntryId
	HAVING ( COUNT(EntryId) > 1 )
	ORDER BY EntryId
	WHILE @@ROWCOUNT > 0
	BEGIN
		--( The spin through the @MatchingEntries table will rectify the problem with
		--( having multiple rows per entry in the end (cf. LT-3458).
		--( It solves that problem by concatenating the glosses and/or forms into one of the entry rows,
		--( while deleting the other rows.
		--( The expected result at the end of the cursor run will be only one entry hvo per entry.
		SET @hvoPrev = 0
		SET @citFPrev = N''
		SET @lexFPrev = N''
		SET @alloPrev = N''
		SET @glossPrev = N''

		SELECT TOP 1 @dummyId = DummyID, @citFTxt = CFTxt, @lexFTxt = LFTxt, @alloTxt = AFTxt, @glossTxt = GLTxt
		FROM @MatchingEntries
		WHERE EntryId = @hvo
		ORDER BY DummyID
		WHILE @@ROWCOUNT > 0
		BEGIN
			IF @hvo = @hvoPrev BEGIN
				--( Handle merging citation form
				IF @citFPrev = N'***' AND @citFTxt <> N'***'
					SET @citFNew = @citFTxt
				ELSE
					SET @citFNew = @citFPrev

				--( Handle merging lexeme form
				IF @lexFPrev = N'***' AND @lexFTxt <> N'***'
					SET @lexFNew = @lexFTxt
				ELSE
					SET @lexFNew = @lexFPrev

				--( Handle merging alternate forms
				SET @alloNew = @alloPrev
				--( If we already have some matching alternate form, then append it to the previous matching alternate forms.
				IF @alloPrev != @alloTxt AND @alloTxt != N'***' BEGIN
					IF @alloNew = N'***'
						SET @alloNew = @alloTxt
					ELSE
						SET @alloNew = @alloNew + N'; ' + @alloTxt
				END

				--( Handle merging glosses
				SET @glossNew = @glossPrev
				--( If we already have some matching gloss, then append it to the previous matching gloss.
				IF @glossPrev != @glossTxt AND @glossTxt <> N'***' BEGIN
					IF @glossNew = N'***'
						SET @glossNew = @glossTxt
					ELSE
						SET @glossNew = @glossNew + N'; ' + @glossTxt
				END

				--( Update the alternate form and gloss info entry row,
				--( but only for the 'keeper' row.
				UPDATE @MatchingEntries
				SET CFTxt = @citFNew,CFWs = @wsv, LFTxt = @lexFNew,LFWs = @wsv, AFTxt = @alloNew,AFWs = @wsv, GLTxt = @glossNew,GLWs = @wsa
					WHERE DummyID = @keeperDummyId

				--( Delete the extra entry row for the current dummy id.
				--( I (RandyR) think this approach with using a dummy id in the table is the real fix for LT-3178,
				--( as the previous code could have been overzealous in deleting too many rows.
				DELETE FROM @MatchingEntries
					WHERE DummyID = @dummyId
				SET @citFPrev = @citFNew
				SET @lexFPrev = @lexFNew
				SET @alloPrev = @alloNew
				SET @glossPrev = @glossNew
			END
			ELSE BEGIN
				--( We have found a new entry hvo, so remember some crucial information,
				--( which will be used in case the @MatchingEntries table has rows with duplicated entry hvos.
				--( If the entry hvo is not the same for the next rwo in the cursor, than all this remembering if for nothing,
				--( but we can't tell at this point, so we have to remember it.
				SET @keeperDummyId = @dummyId
				SET @citFPrev = @citFTxt
				SET @lexFPrev = @lexFTxt
				SET @alloPrev = @alloTxt
				SET @glossPrev = @glossTxt
				SET @hvoPrev = @hvo
			END
			SELECT TOP 1 @dummyId = DummyID, @citFTxt = CFTxt, @lexFTxt = LFTxt, @alloTxt = AFTxt, @glossTxt = GLTxt
			FROM @MatchingEntries
			WHERE DummyID > @dummyId AND EntryId = @hvo
			ORDER BY DummyID
		END

		-- Get next duplicate entry to work on, if any.
		SELECT TOP 1 @hvo = EntryId
		FROM @MatchingEntries
		WHERE EntryId > @hvo
		GROUP BY EntryId
		HAVING ( COUNT(EntryId) > 1 )
		ORDER BY EntryId
	END
	--( By this point, there should not be any rows with repeating entry hvos.
--( PRINT 'Time to remove duplicate rows in @MatchingEntries table: ' + CAST(DATEDIFF(millisecond, @smallStart, getdate()) AS varchar(12))

	--==( Near Final Passes )==--
	-- Try to find some kind of string for any items that have not matched,
	-- The rationale for finding some other string, even if it didn't match,
	-- is to help the user determine if an entry already exists/matches.
		-- REVIEW (SteveMiller): This "final pass" can probably be enhanced by
		-- moving the logic into the query
		-- 	select * from @MatchingEntries
	--( Set up a table variable to hold some vernacular writing systems.
	declare @wses TABLE (
		DummyId INT IDENTITY,
		Ws	INT PRIMARY KEY)
	declare @topId INT, @wsFoundling INT,
		@curRowcount INT, @miscRowCount INT

	-- Initialize @wses for working with vernacular writing systems.
	INSERT INTO @wses (Ws) VALUES (@wsv)
	INSERT INTO @wses (Ws)
	SELECT Dst
	FROM LanguageProject_CurrentVernacularWritingSystems (readuncommitted)
	WHERE Dst <> @wsv
	ORDER BY Ord

	-- Fill in any missing lexeme forms in @MatchingEntries.

	-- For each entry in @MatchingEntries with no LexemeForm that has a
	-- lexeme form in any relevant writing system,
	-- make a row in @lexemeForms indicating the entry, the form, and the
	-- best available writing system.
	-- Note we deliberately don't try to put the mff.Txt into the table.
	-- We can't get that into the output of the query because we don't want
	-- to group by it.
	declare @lexemeForms TABLE (
	Entry INT,
	FormId INT,
	DummyId INT)

	insert into @lexemeForms (Entry, FormId, DummyId)
	select me.EntryId, lf.id, min(ws.DummyId)
	from @MatchingEntries me
		join CmObject lf on lf.owner$ = me.EntryID and lf.OwnFlid$ = 5002029 and me.LFTxt = N'***'
		join MoForm_Form mff on mff.obj = lf.id
		join @wses ws on ws.Ws = mff.Ws
		group by me.EntryId, lf.id

	-- For each row in @lexemeForms, insert the relevant lexeme form info
	-- into @MatchingEntries.
	update @MatchingEntries set LfTxt = mff.Txt, LfWs=wss.ws
	from @lexemeForms lf
	join @wses wss on wss.DummyId = lf.DummyId
	join MoForm_Form mff on mff.Obj = lf.FormId and mff.Ws = wss.ws
	where EntryId = lf.Entry

	declare @citationForms TABLE (
	Entry INT,
	DummyId INT)

	insert into @citationForms (Entry, DummyId)
	select me.EntryId, min(ws.DummyId)
	from @MatchingEntries me
		join LexEntry_CitationForm lf on lf.obj = me.EntryID and me.CFTxt = N'***'
		join @wses ws on ws.Ws = lf.Ws
		group by me.EntryId

	-- For each row in @lexemeForms, insert the relevant lexeme form info
	-- into @MatchingEntries.
	update @MatchingEntries set CfTxt = lf.Txt, CfWs = wss.ws
	from @citationForms cf
	join @wses wss on wss.DummyId = cf.DummyId
	join LexEntry_CitationForm lf on lf.obj = cf.Entry and lf.Ws = wss.ws
	where EntryId = cf.Entry

--(	SET @smallStart = GETDATE()
	--( Try finding something for all alternate forms that didn't match.
	SELECT TOP 1 @topId = Src FROM LexEntry_AlternateForms (readuncommitted)
	IF @@ROWCOUNT > 0
	BEGIN
		declare @otherAlternateForms TABLE (
			Entry INT,
			FormId INT,
			DummyId INT)
		INSERT INTO @otherAlternateForms (Entry, FormId, DummyId)
		SELECT me.EntryId, af.Obj, min(ws.DummyId)
		FROM @MatchingEntries me
		JOIN LexEntry_AlternateForms leaf (readuncommitted) On leaf.Src = me.EntryId
		JOIN MoForm_Form af (readuncommitted) on af.Obj = leaf.Dst
		JOIN @wses ws ON ws.Ws = af.Ws
		WHERE AFTxt = N'***'
		group by me.EntryId, af.Obj

		UPDATE @MatchingEntries
		SET AFTxt = mff.Txt, AFWs = wss.Ws
		from @otherAlternateForms oaf
		join @wses wss on wss.DummyId = oaf.DummyId
		join MoForm_Form mff on mff.Obj = oaf.FormId and mff.Ws = wss.ws
		WHERE EntryID = oaf.Entry
	END
 --(PRINT 'Time to find some non-matching alternate forms: ' + CAST(DATEDIFF(millisecond, @smallStart, getdate()) AS varchar(12))
	DELETE @wses

--( SET @smallStart = GETDATE()
	SELECT TOP 1 @topId = Obj FROM LexSense_Gloss (readuncommitted)
	IF @@ROWCOUNT > 0
	BEGIN
		--( Reset the table variable for analyses wses.
		INSERT INTO @wses (Ws) VALUES (@wsa)
		INSERT INTO @wses (Ws)
		SELECT Dst
		FROM LanguageProject_CurrentAnalysisWritingSystems (readuncommitted)
		WHERE Dst <> @wsa
		ORDER BY Ord

		declare @sensesWithGlosses TABLE (
			Entry INT,
			SenseId INT,
			DummyId INT)
--( SET @minorStart = GETDATE()
		INSERT INTO @sensesWithGlosses (Entry, SenseId, DummyId)
		SELECT sae.EntryId, sae.SenseId, min(ws.DummyId)
		FROM @MatchingEntries me
		JOIN @sensesAndEntries sae ON me.EntryId = sae.EntryId
		JOIN LexSense_Gloss lsg (readuncommitted) ON lsg.Obj = sae.SenseId
		JOIN @wses ws ON ws.Ws = lsg.Ws
		WHERE me.GLTxt = N'***'
		group by sae.EntryId, sae.SenseId
--( PRINT '@sensesWithGlosses rowcount: ' + CAST(@@ROWCOUNT AS varchar(12))
--( PRINT 'Time to gather @sensesWithGlosses table: ' + CAST(DATEDIFF(millisecond, @minorStart, getdate()) AS varchar(12))

		UPDATE @MatchingEntries
		SET GLTxt = lsg.Txt, GLWs = wss.Ws
		from @sensesWithGlosses sg
		join @wses wss on wss.DummyId = sg.DummyId
		join LexSense_Gloss lsg (readuncommitted) ON lsg.Obj = sg.SenseId and lsg.Ws = wss.Ws
		WHERE EntryID = sg.Entry
	END
--( PRINT 'Time to find some non-matching glosses: ' + CAST(DATEDIFF(millisecond, @smallStart, getdate()) AS varchar(12))

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	IF @lf <> N'!'
		SELECT * FROM @MatchingEntries ORDER BY LFTxt, CFTxt, AFTxt, GLTxt
	ELSE
		SELECT * FROM @MatchingEntries ORDER BY GLTxt, LFTxt, CFTxt, AFTxt
--( PRINT 'Total time in SP: ' + CAST(DATEDIFF(millisecond, @mainStart, getdate()) AS varchar(12))
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200131
begin
	UPDATE Version$ SET DbVer = 200132
	COMMIT TRANSACTION
	print 'database updated to version 200132'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200131 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO