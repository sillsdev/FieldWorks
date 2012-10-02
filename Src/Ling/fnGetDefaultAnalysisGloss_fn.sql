/********************************************************************
 * Function: fnGetDefaultAnalysisGloss
 *
 * Description:
 *	Returns a ranked table of various wfiAnalyis and wfiGloss
 *	objects. Built for use in the Interlinear Text Tool.
 *
 * Parameters:
 *	@nWfiWordFormId INT = object ID for the word of interest
 *
 * Sample Call:
 *
 *	SELECT TOP 1 *
 *	FROM dbo.fnGetDefaultAnalysisGloss(3920)
 *	ORDER BY Score DESC
 *******************************************************************/

IF OBJECT_ID('fnGetDefaultAnalysisGloss') IS NOT NULL BEGIN
	PRINT 'removing procedure fnGetDefaultAnalysisGloss'
	DROP FUNCTION fnGetDefaultAnalysisGloss
END
GO
PRINT 'creating function fnGetDefaultAnalysisGloss'
GO

CREATE FUNCTION fnGetDefaultAnalysisGloss (
	@nWfiWordFormId INT)
RETURNS @tblScore TABLE (
	AnalysisId INT,
	GlossId INT,
	[Score] INT)
AS BEGIN

	INSERT INTO @tblScore
		--( wfiGloss is an InstanceOf
		SELECT
			oanalysis.[Id],
			ogloss.[Id],
			(COUNT(ann.InstanceOf) + 10000) --( needs higher # than wfiAnalsys
		FROM CmAnnotation ann
		JOIN WfiGloss g ON g.[Id] = ann.InstanceOf
		JOIN CmObject ogloss ON ogloss.[Id] = g.[Id]
		JOIN CmObject oanalysis ON oanalysis.[Id] = ogloss.Owner$
			AND oanalysis.Owner$ = @nWfiWordFormId
		JOIN WfiAnalysis a ON a.[Id] = oanalysis.[Id]
		GROUP BY oanalysis.[Id], ogloss.[Id]
	UNION ALL
		--( wfiAnnotation is an InstanceOf
		SELECT
			oanalysis.[Id],
			NULL,
			COUNT(ann.InstanceOf)
		FROM CmAnnotation ann
		JOIN CmObject oanalysis ON oanalysis.[Id] = ann.InstanceOf
			AND oanalysis.Owner$ = @nWfiWordFormId
		JOIN WfiAnalysis a ON a.[Id] = oanalysis.[Id]
		-- this is a tricky way of eliminating analyses where there exists
		-- a negative evaluation by a human agent.
		LEFT OUTER JOIN (
				SELECT ae.Target
				FROM CmAgentEvaluation_ ae
				JOIN CmAgent a ON a.Id = ae.Owner$ AND Human = 1
				WHERE ae.Accepted = 0)
					aae ON aae.Target = a.Id
			WHERE aae.Target IS NULL
		GROUP BY oanalysis.[Id]

	--( If the gloss and analysis ID are all null, there
	--( are no annotations, but an analysis (and, possibly, a gloss) still might exist.

	IF @@ROWCOUNT = 0

		INSERT INTO @tblScore
		SELECT TOP 1
			oanalysis.[Id],
			wg.id,
			0
		FROM CmObject oanalysis
		left outer join WfiGloss_ wg on wg.owner$ = oanalysis.id
		LEFT OUTER JOIN (
				SELECT ae.Target
				FROM CmAgentEvaluation_ ae
				JOIN CmAgent a ON a.Id = ae.Owner$ AND Human = 1
				WHERE ae.Accepted = 0)
					aae ON aae.Target = oanalysis.Id
		WHERE oanalysis.Owner$ = @nWfiWordFormId and aae.Target IS NULL

	RETURN
END
GO
