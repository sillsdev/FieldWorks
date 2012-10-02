-- Update database from version 200254 to 200255
BEGIN TRANSACTION  --( will be rolled back if wrong version#)


-------------------------------------------------------------------------------
-- CLE-88: Fix a bug introduced by the data migration from 200240To200241.sql
-- (which has been fixed, but too late for many databases.
-------------------------------------------------------------------------------

UPDATE CmPossibilityList SET ItemClsid=7 WHERE ItemClsid=8

-------------------------------------------------------------------------------
-- LT-9665: concording on 'Lex Entry' line concorded on the morphemes line if
-- there were any allomorphs.
-------------------------------------------------------------------------------

IF OBJECT_ID('fnConcordForLexEntry') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForLexEntry'
	DROP FUNCTION fnConcordForLexEntry
END
GO
PRINT 'creating function fnConcordForLexEntry'
GO

CREATE FUNCTION fnConcordForLexEntry(
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
	SELECT a.BeginObject, a.Id AS AnnotationId, wamb.Ord, lf.Txt
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN MoForm_ mf ON mf.Id = wmb.Morph	-- wmb.Morph may be an allomorph or a lexeme form
	JOIN LexEntry_LexemeForm lelf ON lelf.Src = mf.Owner$
	JOIN MoForm_Form lf ON lf.Obj = lelf.Dst AND lf.Txt LIKE @nvcTextLike AND lf.Ws = @nWs
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	UNION
	SELECT a.BeginObject, a.Id AS AnnotationId, wamb.Ord, lf.Txt
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN MoForm_ mf ON mf.Id = wmb.Morph	-- wmb.Morph may be an allomorph or a lexeme form
	JOIN LexEntry_LexemeForm lelf ON lelf.Src = mf.Owner$
	JOIN MoForm_Form lf ON lf.Obj = lelf.Dst AND lf.Txt LIKE @nvcTextLike AND lf.Ws = @nWs
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.Id, wamb.Ord

	RETURN
END
GO


---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200254
BEGIN
	UPDATE Version$ SET DbVer = 200255
	COMMIT TRANSACTION
	PRINT 'database updated to version 20025'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200254 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
