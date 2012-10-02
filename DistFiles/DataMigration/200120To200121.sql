-- update database FROM version 200120 to 200121
BEGIN TRANSACTION  --( will be rolled back if wrong version#

--( Add new function for get sense (and sub-sense) and entry IDs together.
if object_id('dbo.fnGetSensesAndEntries$') is not null begin
	print 'removing function fnGetSensesAndEntries$'
	drop function dbo.fnGetSensesAndEntries$
end
print 'creating function fnGetSensesAndEntries$'
go
/*****************************************************************************
 * fnGetSensesAndEntries$
 *
 * Description:
 *	Returns the lexical senses associated with all of the entries.
 *
 * Returns:
 *	A table with entry ids accociated with all the senses it owns.
 *****************************************************************************/
CREATE  FUNCTION dbo.fnGetSensesAndEntries$()
RETURNS @sensesAndEntries TABLE (
	SenseId INT PRIMARY KEY,
	EntryId	INT,
	OwnFlid INT) -- Note: OwnFlid is used internally, bu has no real significance in the returned table.
AS
BEGIN
	INSERT INTO @sensesAndEntries
		SELECT ls.id, ls.owner$, ls.OwnFlid$
		FROM CmObject ls
		WHERE ls.Class$ = 5016
	WHILE @@ROWCOUNT > 0
	BEGIN
		UPDATE @sensesAndEntries
		SET EntryId = ls.owner$, OwnFlid = ls.OwnFlid$
		FROM CmObject ls
		JOIN @sensesAndEntries ss
			ON ss.OwnFlid = 5016003
			AND ls.id = ss.EntryId
		WHERE ss.SenseId = SenseId
	END

	RETURN
END
GO

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
	declare @aftext nvarchar(4000),
		@gltext nvarchar(4000),
		@entryID int
	declare @fIsNocountOn int
	-- deterimine if no count is currently set to on
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- table variable to hold (sub)sense+entry id information.
	declare @sensesAndEntries TABLE (
		SenseId INT PRIMARY KEY,
		EntryId	INT)
	INSERT INTO @sensesAndEntries (SenseId, EntryId)
	SELECT SenseId, EntryId
	FROM fnGetSensesAndEntries$()

	-- table variable to hold return information.
	declare @MatchingEntries table (
		DummyID int IDENTITY PRIMARY KEY,			-- 1 This row is returned at the end, but it is not used byt the client.
		EntryID int,						-- 2 Note: These numbers are to help the SP client knwo which column has which stuff.
		CFTxt nvarchar(4000) default '***',	-- 4
		CFWs int,							-- 5
		LFTxt nvarchar(4000) default '***',	-- 6
		LFWs int,							-- 7
		AFTxt nvarchar(4000) default '***',	-- 8
		AFWs int,							-- 9
		GLTxt nvarchar(4000) default '***',	-- 10
		GLWs int)							-- 11
	--( NB: These queries will return one or more rows for each matching entry.
	--( Since there can be multiple matching glosses and/or alternate forms,
	--( the entry will be 'duplicated' once for each multple matching gloss/alternate form.
	--( (See the notes on the following cursor for details on how these 'duplicates' are handled.)
	IF @cf <> N'!'
	BEGIN
		INSERT INTO @MatchingEntries (EntryID, CFTxt,CFWs)
		SELECT cf.Obj, cf.Txt, cf.Ws
		FROM LexEntry_CitationForm cf (readuncommitted)
		WHERE LOWER(RTRIM(LTRIM(cf.Txt))) LIKE LOWER(RTRIM(LTRIM(@cf))) + '%'
			AND cf.Ws = @wsv
	END
	IF @lf <> N'!'
	BEGIN
		INSERT INTO @MatchingEntries (EntryID, LFTxt,LFWs)
		SELECT lelf.Src, lf.Txt, lf.Ws
		FROM LexEntry_LexemeForm lelf (readuncommitted)
		JOIN MoForm_Form lf (readuncommitted) on lf.Obj = lelf.Dst
		WHERE LOWER(RTRIM(LTRIM(lf.Txt))) LIKE LOWER(RTRIM(LTRIM(@lf))) + '%'
			 AND lf.Ws = @wsv
	END
	IF @af <> N'!'
	BEGIN
		INSERT INTO @MatchingEntries (EntryID, AFTxt,AFWs)
		SELECT leaf.Src, af.Txt, af.Ws
		FROM LexEntry_AlternateForms leaf (readuncommitted)
		JOIN MoForm_Form af (readuncommitted) on af.Obj = leaf.Dst
		WHERE LOWER(RTRIM(LTRIM(af.Txt))) LIKE LOWER(RTRIM(LTRIM(@af))) + '%'
			AND af.Ws = @wsv
	END
	IF @gl <> N'!'
	BEGIN
		INSERT INTO @MatchingEntries (EntryID, GLTxt,GLWs)
		SELECT les.EntryId, gl.Txt, gl.Ws
		FROM @sensesAndEntries les
		JOIN LexSense_Gloss gl (readuncommitted) ON gl.Obj = les.SenseId AND gl.Ws=@wsa
		WHERE LOWER(RTRIM(LTRIM(gl.Txt))) like LOWER(RTRIM(LTRIM(@gl))) + '%'
			AND gl.Ws = @wsa
	END

	DECLARE @hvo INT

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
		DECLARE @dummyId int, @keeperDummyId int, @hvoPrev int
		DECLARE @citFTxt nvarchar(4000), @citFPrev nvarchar(4000), @citFNew nvarchar(4000)
		DECLARE @lexFTxt nvarchar(4000), @lexFPrev nvarchar(4000), @lexFNew nvarchar(4000)
		DECLARE @alloTxt nvarchar(4000), @alloPrev nvarchar(4000), @alloNew nvarchar(4000)
		DECLARE @glossTxt nvarchar(4000), @glossPrev nvarchar(4000), @glossNew nvarchar(4000)
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
	declare @topId INT, @wsFoundling INT
	declare @missingTxt TABLE (
		EntryId INT PRIMARY KEY)
	--( Try finding something for all alternate forms that didn't match.
	SELECT TOP 1 @topId = Src FROM LexEntry_AlternateForms (readuncommitted)
	IF @topId > 0
	BEGIN
		INSERT INTO @wses (Ws) VALUES (@wsv)
		INSERT INTO @wses (Ws)
		SELECT Dst
		FROM LanguageProject_CurrentVernacularWritingSystems (readuncommitted)
		WHERE Dst <> @wsv
		ORDER BY Ord

		--( Get new collection of rows from @MatchingEntries that have missing glosses
		INSERT INTO @missingTxt (EntryId)
		SELECT EntryId
		FROM @MatchingEntries
		WHERE AFTxt = N'***'

		SELECT TOP 1 @entryID = EntryID
		FROM @missingTxt
		ORDER BY EntryID
		WHILE @@ROWCOUNT > 0
		BEGIN
			SELECT TOP 1 @aftext = af.Txt, @wsFoundling = af.Ws
			FROM LexEntry_AlternateForms leaf (readuncommitted)
			JOIN MoForm_Form af (readuncommitted) on af.Obj = leaf.Dst
			JOIN @wses ws ON ws.Ws = af.Ws
			WHERE leaf.Src = @entryID
			ORDER BY ws.DummyId
			IF @@ROWCOUNT > 0 -- Found one.
				UPDATE @MatchingEntries
				SET AFTxt = @aftext, AFWs = @wsFoundling
				WHERE EntryID = @entryID
			--( Get next row to work on, if any.
			SELECT TOP 1 @entryID = EntryID
			FROM @missingTxt
			WHERE EntryID > @entryID
			ORDER BY EntryID
		END
		DELETE @wses
		DELETE @missingTxt
	END

	--( Reset the table variable for analyses wses.
	SELECT TOP 1 @topId = Obj FROM LexSense_Gloss (readuncommitted)
	IF @topId > 0
	BEGIN
		INSERT INTO @wses (Ws) VALUES (@wsa)
		INSERT INTO @wses (Ws)
		SELECT Dst
		FROM LanguageProject_CurrentAnalysisWritingSystems (readuncommitted)
		WHERE Dst <> @wsa
		ORDER BY Ord

		--( Get new collection of rows from @MatchingEntries that have missing glosses
		INSERT INTO @missingTxt (EntryId)
		SELECT EntryId
		FROM @MatchingEntries
		WHERE GLTxt = N'***'

		--( Get new collection of entry+senses, which is presumably reduced in numbers.
		declare @sensesAndEntriesGlosses TABLE (
			SenseId INT PRIMARY KEY,
			EntryId	INT)
		INSERT INTO @sensesAndEntriesGlosses (SenseId, EntryId)
		SELECT sae.SenseId, sae.EntryId
		FROM @sensesAndEntries sae
		JOIN @missingTxt mg ON mg.EntryId = sae.EntryId

		--( Try finding something for any glosses that didn't match.
		--( This loop is the current bottleneck in Randy's ZPI database.
		SELECT TOP 1 @entryID = EntryID
		FROM @missingTxt
		ORDER BY EntryID
		WHILE @@ROWCOUNT > 0
		BEGIN
			SELECT TOP 1 @gltext = lsg.Txt, @wsFoundling = lsg.Ws
			FROM @sensesAndEntriesGlosses les
			JOIN LexSense_Gloss lsg (readuncommitted) ON lsg.Obj = les.SenseId
			JOIN @wses ws ON ws.Ws = lsg.Ws
			WHERE les.EntryId = @entryID
			ORDER BY ws.DummyId
			IF @@ROWCOUNT > 0
				UPDATE @MatchingEntries
				SET GLTxt = @gltext, GLWs = @wsFoundling
				WHERE EntryID = @entryID
			--( Get next row to work on, if any.
			SELECT TOP 1 @entryID = EntryID
			FROM @missingTxt
			WHERE EntryID > @entryID
			ORDER BY EntryID
		END
	END

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
if @dbVersion = 200120
begin
	UPDATE Version$ SET DbVer = 200121
	COMMIT TRANSACTION
	print 'database updated to version 200121'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200120 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO