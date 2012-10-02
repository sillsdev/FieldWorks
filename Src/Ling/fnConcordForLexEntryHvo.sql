/***********************************************************************************************
* Function: fnConcordForLexEntryHvo
*
* Description:
*	Gets the beginning paragraph, position within the paragraph, and annotation Id.
*
* Parameters:
*	@nOwnFlid = Owning Field ID of texts
*	@hvoLexEntry = database id of the LexEntry.
*	@ntIds = The text Id must be in this set. It's a comma delimited list of IDs.
*
* Returns:
*	Table containing the object information in the format:
*		BeginObject = starting paragraph.
*		BeginOffset = starting position in paragraph.
*		Id = CmBaseAnnotation.Id
*
* Notes:
*	This function is similar to fnConcordForMorphemes and fnConcordForLexGloss.
*	Any change in those two files should be done here.
**********************************************************************************************/

IF OBJECT_ID('fnConcordForLexEntryHvo') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForLexEntryHvo'
	DROP FUNCTION fnConcordForLexEntryHvo
END
GO
PRINT 'creating function fnConcordForLexEntryHvo'
GO

CREATE FUNCTION dbo.fnConcordForLexEntryHvo(
	@nOwnFlid INT,
	@hvoLexEntry INT,
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
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst AND (SELECT dbo.fnGetEntryForSense(wmb.Sense)) = @hvoLexEntry
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	UNION
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst AND (SELECT dbo.fnGetEntryForSense(wmb.Sense)) = @hvoLexEntry
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.BeginOffset, a.Id

	RETURN
END

GO