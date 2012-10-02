-- Update database from version 200248 to 200249
BEGIN TRANSACTION  --( will be rolled back if wrong version #)
-------------------------------------------------------------------------------
-- Fix LT-9085: "'Show Word Gloss in Concordance' from Word Analyses area, Word Gloss field, is
--               finding too many results and/or wrong in Concordance."
-- Really fixed it this time as well as LT-9401 that occurred from the previous mistake.

IF OBJECT_ID('fnConcordForWfiGloss') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForWfiGloss'
	DROP FUNCTION fnConcordForWfiGloss
END
GO
PRINT 'creating function fnConcordForWfiGloss'
GO

CREATE FUNCTION dbo.fnConcordForWfiGloss(
	@nOwnFlid INT,
	@hvoGloss INT,
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
	JOIN WfiAnalysis_Meanings wam ON wam.Src = wa.Id AND wam.Dst = @hvoGloss
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	UNION
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf AND wg.Id = @hvoGloss
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.BeginOffset, a.Id

	RETURN
END

GO

-----------------------------------------------------------------------------------
------ Finish or roll back transaction as applicable
-----------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200248
BEGIN
	UPDATE Version$ SET DbVer = 200249
	COMMIT TRANSACTION
	PRINT 'database updated to version 200249'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200248 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
