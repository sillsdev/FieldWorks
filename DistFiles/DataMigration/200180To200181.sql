-- update database FROM version 200180 to 200181
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- Add new Concordance scripts
-------------------------------------------------------------------------------

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
	DECLARE @tblIds TABLE (Id INT)

	INSERT INTO @tblIds SELECT f.ID FROM dbo.fnGetIdsFromString(@ntIds, NULL) AS f

	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.Id AS AnnotationId, wamb.Ord, lsg.Txt
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN LexSense_Gloss lsg ON lsg.Obj = wmb.Sense AND lsg.Txt LIKE @nvcTextLike AND lsg.Ws = @nWs
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR t.Id IS NOT NULL)
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
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.Id, wamb.Ord

	RETURN
END

GO

-------------------------------------------------------------------------------

IF OBJECT_ID('fnConcordForLexEntry') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForLexEntry'
	DROP FUNCTION fnConcordForLexEntry
END
GO
PRINT 'creating function fnConcordForLexEntry'
GO

CREATE FUNCTION dbo.fnConcordForLexEntry(
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
	DECLARE @tblIds TABLE (Id INT)

	INSERT INTO @tblIds SELECT f.ID FROM dbo.fnGetIdsFromString(@ntIds, NULL) AS f

	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.Id AS AnnotationId, wamb.Ord, mff.Txt
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN MoForm_Form mff ON mff.Obj = wmb.Morph AND mff.Txt LIKE @nvcTextLike AND mff.Ws = @nWs
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR t.Id IS NOT NULL)
	UNION
	SELECT a.BeginObject, a.Id AS AnnotationId, wamb.Ord, mff.Txt
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN MoForm_Form mff ON mff.Obj = wmb.Morph AND mff.Txt LIKE @nvcTextLike AND mff.Ws = @nWs
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.Id, wamb.Ord

	RETURN
END

GO

-------------------------------------------------------------------------------

IF OBJECT_ID('fnConcordForMorphemes') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForMorphemes'
	DROP FUNCTION fnConcordForMorphemes
END
GO
PRINT 'creating function fnConcordForMorphemes'
GO

CREATE FUNCTION dbo.fnConcordForMorphemes(
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
	DECLARE @tblIds TABLE (Id INT)

	INSERT INTO @tblIds SELECT f.ID FROM dbo.fnGetIdsFromString(@ntIds, NULL) AS f

	INSERT INTO @tblTextAnnotations
	--( Get analyses
	SELECT a.BeginObject, a.Id AS AnnotationId, wamb.Ord, u.Txt
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN (
		SELECT wmb.Id, mff.Txt
		FROM WfiMorphBundle wmb
		JOIN MoForm_Form mff ON mff.Obj = wmb.Morph AND mff.Txt LIKE @nvcTextLike AND mff.Ws = @nWs
		UNION
		SELECT f.Obj, f.Txt
		FROM WfiMorphBundle_Form f
		WHERE f.Txt LIKE @nvcTextLike AND f.Ws = @nWs
		) u ON u.Id = wamb.Dst
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR t.Id IS NOT NULL)
	--( Get glosses of analyses
	UNION
	SELECT a.BeginObject, a.Id AS AnnotationId, wamb.Ord, u.Txt
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN (
		SELECT wmb.Id, mff.Txt
		FROM WfiMorphBundle wmb
		JOIN MoForm_Form mff ON mff.Obj = wmb.Morph AND mff.Txt LIKE @nvcTextLike AND mff.Ws = @nWs
		UNION
		SELECT f.Obj, f.Txt
		FROM WfiMorphBundle_Form f
		WHERE f.Txt LIKE @nvcTextLike AND f.Ws = @nWs
		) u ON u.Id = wamb.Dst
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.Id, wamb.Ord

	RETURN
END

GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200180
BEGIN
	UPDATE [Version$] SET [DbVer] = 200181
	COMMIT TRANSACTION
	PRINT 'database updated to version 200181'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200180 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
