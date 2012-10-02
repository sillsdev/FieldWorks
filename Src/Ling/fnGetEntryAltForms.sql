/*****************************************************************************
 * fnGetEntryAltForms
 *
 * Description:
 *	Gets alternate forms for a given lexical entry. Supporting function of
 *	fnMatchEntries.
 *
 * Parameters:
 *	@nSrc		= the entry ID
 *	@nVernWS	= the vernacular writing system ID
 *
 * Sample Call:
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

IF OBJECT_ID('dbo.fnGetEntryAltForms') IS NOT NULL BEGIN
	PRINT 'removing function fnGetEntryAltForms'
	DROP FUNCTION dbo.fnGetEntryAltForms
END
PRINT 'creating function fnGetEntryAltForms'
GO

CREATE  FUNCTION dbo.fnGetEntryAltForms(@nSrc INT, @nVernWs INT)
RETURNS @tblAltForm TABLE (AltForm NVARCHAR(4000)) -- returns zero or one row
AS
BEGIN
	DECLARE @nvcText NVARCHAR(4000)

	SELECT TOP 1 @nvcText = mff_af.Txt
	FROM LexEntry_AlternateForms af
	JOIN MoForm_Form mff_af ON mff_af.Obj = af.Dst AND mff_af.WS = @nVernWs
	WHERE af.Src = @nSrc
	ORDER BY mff_af.Txt

	IF @@ROWCOUNT != 0
		INSERT INTO @tblAltForm (AltForm) VALUES (@nvcText)

	WHILE @@ROWCOUNT != 0 BEGIN
		SELECT TOP 1 @nvcText = mff_af.Txt
		FROM LexEntry_AlternateForms af
		JOIN MoForm_Form mff_af ON mff_af.Obj = af.Dst AND mff_af.WS = @nVernWs
		WHERE af.Src = @nSrc AND mff_af.Txt > @nvcText
		ORDER BY mff_af.Txt

		IF @@ROWCOUNT != 0
			UPDATE @tblAltForm SET AltForm = AltForm + N'; ' + @nvcText
	END

	RETURN
END
GO
