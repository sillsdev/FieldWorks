/***********************************************************************************************
 * Function: fnConcordForAnalysis
 *
 * Description:
 *	Gets the beginning position of the paragraph, annotation Id, and the text.
 *
 * Parameters:
 *	@nOwnFlid = Owning Field ID of texts
 * 	@hvoAnal = database id of the WfiAnalysis object of interest.
 *	@ntIds = The scripture text Ids of interest must be in this comma delimited list of IDs.
 *
 * Returns:
 *	Table containing the object information in the format:
 *		BeginObject = starting point in the text.
*		BeginOffset = starting position in paragraph.
 *		Txt = The morpheme
 *
 * Notes:
 *	This function is virtually identical to fnConcordForLexGloss, fnConcordForLexEntry, and
 *   fnConcordForMorphemes. Any changes there should probably be done here.
 **********************************************************************************************/

IF OBJECT_ID('fnConcordForAnalysis') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForAnalysis'
	DROP FUNCTION fnConcordForAnalysis
END
GO
PRINT 'creating function fnConcordForAnalysis'
GO

CREATE FUNCTION [dbo].[fnConcordForAnalysis](
	@nOwnFlid INT,
	@hvoAnal INT,
	@ntIds NTEXT)
RETURNS @tblTextAnnotations TABLE (
	BeginObject INT,
	BeginOffset INT,
	AnnotationId INT)
AS
BEGIN
	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf AND wa.Id = @hvoAnal
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	UNION
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$ AND wa.Id = @hvoAnal
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.BeginOffset, a.Id

	RETURN
END

GO
