/***********************************************************************************************
 * Function: fnConcordForPartOfSpeech
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
 *	This function is very similar to fnConcordForLexGloss, fnConcordForLexEntry, and
 *   fnConcordForMorphemes. Any changes there should probably be done here.
 **********************************************************************************************/

IF OBJECT_ID('fnConcordForPartOfSpeech') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForPartOfSpeech'
	DROP FUNCTION fnConcordForPartOfSpeech
END
GO
PRINT 'creating function fnConcordForPartOfSpeech'
GO

CREATE FUNCTION [dbo].[fnConcordForPartOfSpeech](
	@nOwnFlid INT,
	@hvoPOS INT,
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
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	LEFT OUTER JOIN MoStemMsa msm ON msm.Id= wmb.Msa
	LEFT OUTER JOIN MoInflAffMsa miam ON miam.Id= wmb.Msa
	LEFT OUTER JOIN MoDerivStepMsa mdsm ON mdsm.Id= wmb.Msa
	LEFT OUTER JOIN MoDerivAffMsa mdam ON mdam.Id= wmb.Msa
	LEFT OUTER JOIN MoUnclassifiedAffixMsa muam ON muam.Id= wmb.Msa
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE ((t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL) AND
		   (wa.Category=@hvoPOS OR
			msm.PartOfSpeech=@hvoPOS OR
			miam.PartOfSpeech=@hvoPOS OR
			mdsm.PartOfSpeech=@hvoPOS OR
			mdam.ToPartOfSpeech=@hvoPOS OR
			muam.PartOfSpeech=@hvoPOS))
	UNION
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	LEFT OUTER JOIN MoStemMsa msm ON msm.Id= wmb.Msa
	LEFT OUTER JOIN MoInflAffMsa miam ON miam.Id= wmb.Msa
	LEFT OUTER JOIN MoDerivStepMsa mdsm ON mdsm.Id= wmb.Msa
	LEFT OUTER JOIN MoDerivAffMsa mdam ON mdam.Id= wmb.Msa
	LEFT OUTER JOIN MoUnclassifiedAffixMsa muam ON muam.Id= wmb.Msa
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE ((t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL) AND
		   (wa.Category=@hvoPOS OR
			msm.PartOfSpeech=@hvoPOS OR
			miam.PartOfSpeech=@hvoPOS OR
			mdsm.PartOfSpeech=@hvoPOS OR
			mdam.ToPartOfSpeech=@hvoPOS OR
			muam.PartOfSpeech=@hvoPOS))
	ORDER BY a.BeginObject, a.BeginOffset, a.Id

	RETURN
END

GO
