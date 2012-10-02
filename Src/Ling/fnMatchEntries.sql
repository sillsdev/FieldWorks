/*****************************************************************************
 * fnMatchEntries
 *
 * Description:
 *	Matches up lexical entries with lexical forms, citation forms, alternate
 *	forms, and glosses. The string to search for can be different for the
 *	lexical form, citation form, alternate forms, and glosses can be different
 *	from each other. If none of the forms have a search string, and the gloss
 *	does, the functionality changes such that only one gloss is found. For
 *	languages with more than one vernacular writing system, and attempt is
 *	made to find a gloss in other writing systems if a gloss in they primary
 *	writing system can't be found. Unlike its predecessor, fnMatchEntries
 *	does not order output; the application should. Not only does this
 *	function allow embedding into a query, perhaps an application call can
 *	better use ICU for sorting with or without diacritics.
 *
 * Parameters:
 *	@nExactMatch	= do an exact match? Currently not supported, as nothing
 *						uses it.
 *	@nvcLexForm		= the lexical form to search for. Ignore if "!"
 *	@nvcCitForm		= the citation form to search for. Ignore if "!"
 *	@nvcAltForm		= the alternate form to search for. Ignore if "!"
 *  @nvcGloss		= the gloss to search for. Ignore if "!"
 *	@nVernWS		= the vernacular writing system ID
 *	@nAnalysisWS	= the analysis writing system ID
 *	@nMaxSize		= the max size allowed for strings searched for. Defaults
 *						to 900
 *
 * Sample Call:
 *	SELECT
 *		EntryId,
 *		ISNULL(LexicalForm), N'***') AS LexicalForm,
 *		LexicalFormWS,
 *		ISNULL(CitationForm, N'***') AS CitationForm,
 *		CitationFormWS,
 *		ISNULL(AlternateForm, N'***') AS AlternateForm,
 *		AlternateFormWS,
 *		ISNULL(Gloss, N'***') AS Gloss
 *		GlossWS,
 *		FROM fnMatchEntries(N'b', N'b', N'b', N'!', 40733, 40716)
 *		ORDER BY LexicalForm, CitationForm, AlternateForm, Gloss
 *
 * Returns:
 *	A memory table with EntryId, Lexical Form, Citation Forms, Alternate Forms,
 *	Glosses, and their associated writing systems.
 *
 * Notes:
 *	At the time this was written (Jan 2007, all known calls to the procedure
 *	feed the same string to the lexical, citation, and alternate form
 *	parameters.It was very tempting to refactor this out. However, the
 *	ability is cool, and JohnT says it could happen. So it stays.
 *
 *	Also at this time, I don't see any call from the app for exact matches.
 *	This function supports it, but it hasn't been tested much.
 *****************************************************************************/

IF OBJECT_ID('dbo.fnMatchEntries') IS NOT NULL BEGIN
	PRINT 'removing function fnMatchEntries'
	DROP FUNCTION dbo.fnMatchEntries
END
PRINT 'creating function fnMatchEntries'
GO

CREATE  FUNCTION dbo.fnMatchEntries(
	@nExactMatch BIT = 0,
	@nvcLexForm NVARCHAR(900),
	@nvcCitForm NVARCHAR(900),
	@nvcAltForm	NVARCHAR(900),
	@nvcGloss NVARCHAR(900),
	@nVernWS INT,
	@nAnalysisWS INT,
	@nMaxSize INT)
RETURNS @tblMatchingEntries TABLE (
		EntryId INT PRIMARY KEY,
		LexicalForm NVARCHAR(900),
		LexicalFormWS INT,
		CitationForm NVARCHAR(900),
		CitationFormWS INT,
		AlternateForm NVARCHAR(900),
		AlternateFormWS INT,
		Gloss NVARCHAR(900),
		GlossWS INT)
AS
BEGIN
	DECLARE
		@nEntryId INT,
		@nCount INT

	-- Make sure we use a valid nMaxSize: allowing the caller to set it
	-- to a value between 1 and 900

	IF @nMaxSize IS NULL OR @nMaxSize > 900 OR @nMaxSize <= 0 BEGIN
		SET @nMaxSize = 900
	END

	SET @nvcLexForm = SUBSTRING(RTRIM(LTRIM(LOWER(@nvcLexForm))),1, @nMaxSize)
	SET @nvcCitForm = SUBSTRING(RTRIM(LTRIM(LOWER(@nvcCitForm))),1, @nMaxSize)
	SET @nvcAltForm = SUBSTRING(RTRIM(LTRIM(LOWER(@nvcAltForm))),1, @nMaxSize)
	SET @nvcGloss = SUBSTRING(RTRIM(LTRIM(LOWER(@nvcGloss))),1, @nMaxSize)

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
				AND SUBSTRING(RTRIM(LTRIM(LOWER(mff_lf.Txt))), 1, @nMaxSize) LIKE @nvcLexForm + N'%'
			--( matching citation forms
			UNION
			SELECT DISTINCT cf.Obj AS EntryID
			FROM LexEntry_CitationForm cf
			WHERE @nvcCitForm != N'!'
				AND SUBSTRING(RTRIM(LTRIM(LOWER(cf.Txt))), 1, @nMaxSize) LIKE @nvcCitForm + N'%'
				AND cf.WS = @nVernWS
			--( matching alternate forms
			UNION
			SELECT DISTINCT af.Src AS EntryID
			FROM LexEntry_AlternateForms af
			JOIN MoForm_Form mff_af ON mff_af.Obj = af.Dst AND mff_af.WS = @nVernWS
			WHERE @nvcAltForm != N'!'
				AND SUBSTRING(RTRIM(LTRIM(LOWER(mff_af.Txt))), 1, @nMaxSize) LIKE @nvcAltForm + N'%'
			--( matching glosses
			UNION
			SELECT DISTINCT o.Owner$ AS EntryID
			FROM LexSense_Gloss lsg
			JOIN CmObject o ON o.Id = lsg.Obj
			JOIN LexEntry le ON le.Id = o.Owner$
			WHERE @nvcGloss != N'!' AND lsg.Ws = @nAnalysisWS
				AND SUBSTRING(RTRIM(LTRIM(LOWER(lsg.Txt))), 1, @nMaxSize) LIKE @nvcGloss + N'%'
		END
		ELSE BEGIN --( IF ! @nExactMatch = 0
			INSERT INTO @tblMatchingEntries (EntryId)
			--( matching lexeme forms
			SELECT DISTINCT lf.Src AS EntryID
			FROM LexEntry_LexemeForm lf
			JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWS
			WHERE @nvcLexForm != N'!'
				AND SUBSTRING(RTRIM(LTRIM(mff_lf.Txt)), 1, @nMaxSize) = @nvcLexForm
			--( matching citation forms
			UNION
			SELECT DISTINCT cf.Obj AS EntryID
			FROM LexEntry_CitationForm cf
			WHERE @nvcCitForm != N'!'
				AND SUBSTRING(RTRIM(LTRIM(cf.Txt)), 1, @nMaxSize) = @nvcCitForm
				AND cf.WS = @nVernWS
			--( matching alternate forms
			UNION
			SELECT DISTINCT af.Src AS EntryID
			FROM LexEntry_AlternateForms af
			JOIN MoForm_Form mff_af ON mff_af.Obj = af.Dst AND mff_af.WS = @nVernWS
			WHERE @nvcAltForm != N'!'
				AND SUBSTRING(RTRIM(LTRIM(mff_af.Txt)), 1, @nMaxSize) = @nvcAltForm
			--( matching glosses
			UNION
			SELECT DISTINCT o.Owner$ AS EntryID
			FROM LexSense_Gloss lsg
			JOIN CmObject o ON o.Id = lsg.Obj
			JOIN LexEntry le ON le.Id = o.Owner$
			WHERE @nvcGloss != N'!' AND lsg.Ws = @nAnalysisWS
				AND SUBSTRING(RTRIM(LTRIM(lsg.Txt)), 1, @nMaxSize) = @nvcGloss
		END
	END --( IF @nvcLexForm != N'!' OR @nvcCitForm != '!' OR @nvcAltForm != '!'

	ELSE BEGIN --( IF @nvcGloss != '!' BEGIN
		IF @nExactMatch = 0 BEGIN
			INSERT INTO @tblMatchingEntries (EntryId)
			SELECT DISTINCT o.Owner$ AS EntryID
			FROM LexSense_Gloss lsg
			JOIN CmObject o ON o.Id = lsg.Obj
			JOIN LexEntry le ON le.Id = o.Owner$
			WHERE @nvcGloss != N'!' AND lsg.Ws = @nAnalysisWS
				AND SUBSTRING(RTRIM(LTRIM(LOWER(lsg.Txt))), 1, @nMaxSize) LIKE @nvcGloss + N'%'
		END
		ELSE BEGIN --( IF ! @nExactMatch = 0
			INSERT INTO @tblMatchingEntries (EntryId)
			SELECT DISTINCT o.Owner$ AS EntryID
			FROM LexSense_Gloss lsg
			JOIN CmObject o ON o.Id = lsg.Obj
			JOIN LexEntry le ON le.Id = o.Owner$
			WHERE @nvcGloss != N'!' AND lsg.Ws = @nAnalysisWS
				AND SUBSTRING(RTRIM(LTRIM(lsg.Txt)), 1, @nMaxSize) = @nvcGloss
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
			AlternateForm = (
				SELECT SUBSTRING(RTRIM(LTRIM(AltForm)), 1, @nMaxSize)
				FROM dbo.fnGetEntryAltForms(@nEntryId, @nVernWS)),
			AlternateFormWS = @nVernWS,
			Gloss = (
				SELECT SUBSTRING(RTRIM(LTRIM(Gloss)), 1, @nMaxSize)
				FROM dbo.fnGetEntryGlosses(@nEntryId, @nAnalysisWS)),
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

	SELECT @nCount = COUNT(*) FROM LangProject_CurAnalysisWss
	IF @nCount > 1 BEGIN

		UPDATE @tblMatchingEntries
		SET Gloss = lsg.Txt, GlossWS = lsg.WS
		FROM @tblMatchingEntries me
		JOIN CmObject o ON o.Owner$ = me.EntryId
		JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.WS != @nAnalysisWS
		JOIN (
			SELECT TOP 1 Dst
			FROM LangProject_CurAnalysisWss
			WHERE Dst != @nAnalysisWS
			ORDER BY Ord
			) caw ON caw.Dst = lsg.WS
		WHERE me.Gloss IS NULL

	END
	RETURN
END
GO
