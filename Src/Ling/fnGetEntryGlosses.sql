/*****************************************************************************
 * fnGetEntryGlosses
 *
 * Description:
 *	Gets glosses for a given lexical entry. Supporting function of
 *	fnMatchEntries.
 *
 * Parameters:
 *	@nSrc			= the entry ID
 *	@nAnalysisWS	= the analysis writing system ID
 *
 * Sample Call:
 *	UPDATE @tblMatchingEntries SET
 *		AlternateForm = (
 *			SELECT AltForm FROM dbo.fnGetEntryAltForms(@nEntryId, @nVernWS)),
 *		AlternateFormWS = @nVernWS,
 *		Gloss = (
 *			SELECT Gloss FROM dbo.fnGetEntryGlosses(@nEntryId, @nAnalysisWS)),
 *		GlossWS = @nAnalysisWS
 *		WHERE EntryId = @nEntryId
 *
 * Returns:
 *	A memory table with a single row and a single column. This is so the
 *	function can be used within a query, such as the above calling sample.
 *****************************************************************************/

IF OBJECT_ID('dbo.fnGetEntryGlosses') IS NOT NULL BEGIN
	PRINT 'removing function fnGetEntryGlosses'
	DROP FUNCTION dbo.fnGetEntryGlosses
END
PRINT 'creating function fnGetEntryGlosses'
GO

CREATE  FUNCTION dbo.fnGetEntryGlosses(@nSrc INT, @nAnalysisWS INT)
RETURNS @tblGloss TABLE (Gloss NVARCHAR(4000)) -- returns zero or one row
AS
BEGIN
	DECLARE @nvcText NVARCHAR(4000)

	SELECT TOP 1 @nvcText = lsg.Txt --( main sense; subsenses not covered currently
	FROM CmObject o
	JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.Ws = @nAnalysisWS
	WHERE o.Owner$ = @nSrc
	ORDER BY lsg.Txt

	IF @@ROWCOUNT != 0
		INSERT INTO @tblGloss (Gloss) VALUES (@nvcText)

	WHILE @@ROWCOUNT != 0 BEGIN
		SELECT TOP 1 @nvcText = lsg.Txt
		FROM CmObject o
		JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.Ws = @nAnalysisWS
		WHERE o.Owner$ = @nSrc AND lsg.Txt > @nvcText
		ORDER BY lsg.Txt

		IF @@ROWCOUNT != 0
			UPDATE @tblGloss SET Gloss = Gloss + N'; ' + @nvcText
	END

	RETURN
END
GO
