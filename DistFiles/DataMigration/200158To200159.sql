-- update database FROM version 200158 to 200159
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

------------------------------------------------------------
-- fnGetSensesAndEntries$ was only used by fnMatchEntries
------------------------------------------------------------

if object_id('dbo.fnGetSensesAndEntries$') is not null begin
	print 'removing function fnGetSensesAndEntries$'
	drop function dbo.fnGetSensesAndEntries$
end
go

---------------------------------------------------------------
-- fnMatchEntries should not pay attention to case or space
---------------------------------------------------------------

IF OBJECT_ID('dbo.fnMatchEntries') IS NOT NULL BEGIN
	PRINT 'removing function fnMatchEntries'
	DROP FUNCTION dbo.fnMatchEntries
END
PRINT 'creating function fnMatchEntries'
GO

CREATE  FUNCTION dbo.fnMatchEntries(
	@nExactMatch BIT = 0,
	@nvcLexForm NVARCHAR(4000),
	@nvcCitForm NVARCHAR(4000),
	@nvcAltForm	NVARCHAR(4000),
	@nvcGloss NVARCHAR(4000),
	@nVernWS INT,
	@nAnalysisWS INT)
RETURNS @tblMatchingEntries TABLE (
		EntryId INT PRIMARY KEY,
		LexicalForm NVARCHAR(1000),
		LexicalFormWS INT,
		CitationForm NVARCHAR(1000),
		CitationFormWS INT,
		AlternateForm NVARCHAR(1000),
		AlternateFormWS INT,
		Gloss NVARCHAR(1000),
		GlossWS INT)
AS
BEGIN
	DECLARE
		@nEntryId INT,
		@nCount INT

	--==( Get EntryIDs )==--

	--( This block is for searches on any lexical, citation, or alternate form
	IF @nvcLexForm != N'!' OR @nvcCitForm != '!' OR @nvcAltForm != '!' BEGIN
		IF @nExactMatch = 0 BEGIN
			INSERT INTO @tblMatchingEntries (EntryId)
			--( matching lexeme forms
			SELECT DISTINCT lf.Src AS EntryID
			FROM LexEntry_LexemeForm lf
			JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWS
			WHERE @nvcLexForm != N'!'
				AND RTRIM(LTRIM(LOWER(mff_lf.Txt))) LIKE RTRIM(LTRIM(LOWER(@nvcLexForm))) + N'%'
			--( matching citation forms
			UNION
			SELECT DISTINCT lf.Src AS EntryID
			FROM LexEntry_LexemeForm lf
			JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst
			JOIN LexEntry_CitationForm cf ON cf.Obj = lf.Src
			WHERE @nvcCitForm != N'!'
				AND RTRIM(LTRIM(LOWER(cf.Txt))) LIKE RTRIM(LTRIM(LOWER(@nvcCitForm))) + N'%'
				AND cf.WS = @nVernWS
			--( matching alternate forms
			UNION
			SELECT DISTINCT af.Src AS EntryID
			FROM LexEntry_AlternateForms af
			JOIN MoForm_Form mff_af ON mff_af.Obj = af.Dst AND mff_af.WS = @nVernWS
			WHERE @nvcAltForm != N'!'
				AND RTRIM(LTRIM(LOWER(mff_af.Txt))) LIKE RTRIM(LTRIM(LOWER(@nvcAltForm))) + N'%'
			--( matching glosses
			UNION
			SELECT DISTINCT o.Owner$ AS EntryID
			FROM LexSense_Gloss lsg
			JOIN CmObject o ON o.Id = lsg.Obj
			WHERE @nvcGloss != N'!' AND lsg.Ws = @nAnalysisWS
				AND RTRIM(LTRIM(LOWER(lsg.Txt))) LIKE RTRIM(LTRIM(LOWER(@nvcGloss))) + N'%'
		END
		ELSE BEGIN --( IF ! @nExactMatch = 0
			INSERT INTO @tblMatchingEntries (EntryId)
			--( matching lexeme forms
			SELECT DISTINCT lf.Src AS EntryID
			FROM LexEntry_LexemeForm lf
			JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWS
			WHERE @nvcLexForm != N'!'
				AND RTRIM(LTRIM(LOWER(mff_lf.Txt))) = RTRIM(LTRIM(LOWER(@nvcLexForm)))
			--( matching citation forms
			UNION
			SELECT DISTINCT lf.Src AS EntryID
			FROM LexEntry_LexemeForm lf
			JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst
			JOIN LexEntry_CitationForm cf ON cf.Obj = lf.Src
			WHERE @nvcCitForm != N'!'
				AND RTRIM(LTRIM(LOWER(cf.Txt))) = RTRIM(LTRIM(LOWER(@nvcCitForm)))
				AND cf.WS = @nVernWS
			--( matching alternate forms
			UNION
			SELECT DISTINCT af.Src AS EntryID
			FROM LexEntry_AlternateForms af
			JOIN MoForm_Form mff_af ON mff_af.Obj = af.Dst AND mff_af.WS = @nVernWS
			WHERE @nvcAltForm != N'!'
				AND RTRIM(LTRIM(LOWER(mff_af.Txt))) = RTRIM(LTRIM(LOWER(@nvcAltForm)))
			--( matching glosses
			UNION
			SELECT DISTINCT o.Owner$ AS EntryID
			FROM LexSense_Gloss lsg
			JOIN CmObject o ON o.Id = lsg.Obj
			WHERE @nvcGloss != N'!'
				AND RTRIM(LTRIM(LOWER(lsg.Ws))) = RTRIM(LTRIM(LOWER(@nAnalysisWS)))
				AND lsg.Txt = @nvcGloss
		END
	END --( IF @nvcLexForm != N'!' OR @nvcCitForm != '!' OR @nvcAltForm != '!'

	ELSE BEGIN --( IF @nvcGloss != '!' BEGIN
		IF @nExactMatch = 0 BEGIN
			INSERT INTO @tblMatchingEntries (EntryId)
			SELECT DISTINCT lf.Src AS EntryID
			FROM LexEntry_LexemeForm lf
			JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWS
			JOIN CmObject o ON o.Owner$ = lf.Src
			JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.Ws = @nAnalysisWS
			WHERE @nvcGloss != N'!'
				AND RTRIM(LTRIM(LOWER(lsg.Txt))) LIKE RTRIM(LTRIM(LOWER(@nvcGloss))) + N'%'
		END
		ELSE BEGIN --( IF ! @nExactMatch = 0
			INSERT INTO @tblMatchingEntries (EntryId)
			SELECT DISTINCT lf.Src AS EntryID
			FROM LexEntry_LexemeForm lf
			JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWS
			JOIN CmObject o ON o.Owner$ = lf.Src
			JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.Ws = @nAnalysisWS
			WHERE @nvcGloss != N'!'
				AND RTRIM(LTRIM(LOWER(lsg.Txt))) = RTRIM(LTRIM(LOWER(@nvcGloss)))
		END
	END

	--==( Fill in Info for Entry IDs )==--

	--( Get forms and glosses for each of the hits from the above query.
	DECLARE curEntries CURSOR FOR
		SELECT EntryId FROM @tblMatchingEntries me ORDER BY EntryId
	OPEN curEntries
	FETCH NEXT FROM curEntries INTO @nEntryId
	WHILE @@FETCH_STATUS = 0 BEGIN

		UPDATE @tblMatchingEntries SET
			LexicalForm = mff_lf.Txt,
			LexicalFormWS = mff_lf.WS,
			CitationForm = cf.Txt,
			CitationFormWS = cf.WS,
			AlternateForm = (SELECT AltForm FROM dbo.fnGetEntryAltForms(@nEntryId, @nVernWS)),
			AlternateFormWS = @nVernWS,
			Gloss = (SELECT Gloss FROM dbo.fnGetEntryGlosses(@nEntryId, @nAnalysisWS)),
			GlossWS = @nAnalysisWS
		FROM @tblMatchingEntries me
		LEFT OUTER JOIN LexEntry_LexemeForm lf ON lf.Src = me.EntryID
		LEFT OUTER JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWS
		LEFT OUTER JOIN LexEntry_CitationForm cf ON cf.Obj = me.EntryID AND cf.WS = @nVernWS
		WHERE EntryId = @nEntryId

		FETCH NEXT FROM curEntries INTO @nEntryId
	END
	CLOSE curEntries
	DEALLOCATE curEntries

	--==( More getting glosses )==--

	--( If we can't get a gloss in the selected writing system, try for one in
	--( the other writing systems. If there is only one analysis writing system,
	--( never mind.

	SELECT @nCount = COUNT(*) FROM LanguageProject_CurrentAnalysisWritingSystems
	IF @nCount > 1 BEGIN

		UPDATE @tblMatchingEntries
		SET Gloss = lsg.Txt, GlossWS = lsg.WS
		FROM @tblMatchingEntries me
		JOIN CmObject o ON o.Owner$ = me.EntryId
		JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.WS != @nAnalysisWS
		JOIN (
			SELECT TOP 1 Dst
			FROM LanguageProject_CurrentAnalysisWritingSystems
			WHERE Dst != @nAnalysisWS
			ORDER BY Ord
			) caw ON caw.Dst = lsg.WS
		WHERE me.Gloss IS NULL

	END
	RETURN
END
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200158
BEGIN
	UPDATE [Version$] SET [DbVer] = 200159
	COMMIT TRANSACTION
	PRINT 'database updated to version 200159'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200158 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
