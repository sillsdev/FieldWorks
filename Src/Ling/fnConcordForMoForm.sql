/***********************************************************************************************
* Function: fnConcordForMoForm
*
* Description:
-- TODO (anyone): Is this description right?
*	Gets the beginning position of the paragraph, annotation Id, and the text.
*
* Parameters:
*	@nOwnFlid = Owning Field ID of texts
* 	@hvoForm = database id of the MoForm
*	@ntIds = The text Id must be in this set. It's a comma delimited list of IDs.
*
* Returns:
*	Table containing the object information in the format:
*		BeginObject = starting paragraph in the text.
*		BeginOffset = starting offset within paragraph.
*		Id = CmBaseAnnotation.Id
*
* Notes:
*	This function is virtually identical to fnConcordForMorphemes and fnCorcordForLexEntry.
*	Any change to those should be done here.
**********************************************************************************************/

IF OBJECT_ID('fnConcordForMoForm') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForMoForm'
	DROP FUNCTION fnConcordForMoForm
END
GO
PRINT 'creating function fnConcordForMoForm'
GO

CREATE FUNCTION dbo.fnConcordForMoForm(
	@nOwnFlid INT,
	@hvoForm INT,
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
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst AND wmb.Morph = @hvoForm
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
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst AND wmb.Morph = @hvoForm
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.BeginOffset, a.Id

	RETURN
END

GO
