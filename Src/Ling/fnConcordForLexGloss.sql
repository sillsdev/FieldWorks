/***********************************************************************************************
* Function: fnConcordForLexGloss
*
* Description:
-- TODO (anyone): Is this description right?
*	Gets the beginning position of the paragraph, annotation Id, and the text.
*
* Parameters:
*	@nOwnFlid = Owning Field ID of texts
* 	@nvcTextLike = Get only morphology forms that match the patter
*	@nWs = The writing system of the above text.
*	@ntIds = The text Id must be in this set. It's a comma delimited list of IDs.
*
* Returns:
*	Table containing the object information in the format:
*		BeginObject = starting point in the text.
*		Id = CmBaseAnnotation.Id
*		Ord = Order within the WfiAnalysis
*		Txt = The morpheme
*
* Notes:
*	This function is virtually identical to fnConcordForMorphemes and fnCorcordForLexEntry.
*	Any change to those should be done here.
*
*	This code was originally part of LexText\Interlinear\ConcordanceControl.cs.
*
*	Note from the original author: "Minimally, we need to filter out scripture if none is
*	selected, or TE is not installed. Otherwise, we need to include the selected scripture in
*	our search." This is the reason for the funny OR clause in the WHERE statements.
*
*	Note from the original author: "NOTE: assumes that the TextOwnFlid in the SQL is an StText
*	that owns the paragraph referred to by CmBaseAnnotation.BeginObject."
**********************************************************************************************/

IF OBJECT_ID('fnConcordForLexGloss') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForLexGloss'
	DROP FUNCTION fnConcordForLexGloss
END
GO
PRINT 'creating function fnConcordForLexGloss'
GO

CREATE FUNCTION dbo.fnConcordForLexGloss(
	@nOwnFlid INT,
	@nvcTextLike NVARCHAR(4000),
	@nWs INT,
	@ntIds NTEXT)
RETURNS @tblTextAnnotations TABLE (
	BeginObject INT,
	AnnotationId INT,
	Ord INT,
	Txt NVARCHAR(4000))
AS
BEGIN
	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.Id AS AnnotationId, wamb.Ord, lsg.Txt
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN LexSense_Gloss lsg ON lsg.Obj = wmb.Sense AND lsg.Txt LIKE @nvcTextLike AND lsg.Ws = @nWs
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	UNION
	SELECT a.BeginObject, a.Id AS AnnotationId, wamb.Ord, lsg.Txt
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN LexSense_Gloss lsg ON lsg.Obj = wmb.Sense AND lsg.Txt LIKE @nvcTextLike AND lsg.Ws = @nWs
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.Id, wamb.Ord

	RETURN
END

GO
