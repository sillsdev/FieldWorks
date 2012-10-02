/*****************************************************************************
 * Procedure: SetParseFailureEvals
 *
 * Finds Analyses without an evaluation by the given agent, and creates new
 * evaluations that are marked for FAILURE.
 *
 * Parameters:
 * 		@nAgentID			ID of the agent
 *		@nWfiWordFormID		ID of the wordform
 *		@nvcDetails			Additional evaluation detail
 *		@dtEval				Date-time of the evaluation
 *
 * Returns:
 *		0 for success, otherwise an error code.
 *****************************************************************************/

if object_id('SetParseFailureEvals') is not null begin
	print 'removing proc SetParseFailureEvals'
	drop proc SetParseFailureEvals
end
go
print 'creating proc SetParseFailureEvals'
go

CREATE PROC SetParseFailureEvals
	@nAgentId INT,
	@nWfiWordFormID INT,
	@nvcDetails NVARCHAR(4000),
	@dtEval DATETIME
AS
	DECLARE @AnalObjId INT,
		@nError INT
	SET @nError = 0

	-- Get all the IDs for Analyses that belong to the wordform, but which don't have an
	-- evaluation belonging to the given agent.  These will all be set to FAILED.
	DECLARE @MatchingAnalyses TABLE (AnalysisId INT PRIMARY KEY) -- Hold matches in this table variable.
	INSERT INTO @MatchingAnalyses
	SELECT wwa.Dst
	FROM WfiWordForm_Analyses wwa
	LEFT OUTER JOIN CmAgentEvaluation_ cae ON cae.Target = wwa.Dst AND cae.Owner$=@nAgentId
	WHERE wwa.Src=@nWfiWordFormID AND cae.Accepted IS NULL

	SELECT TOP 1 @AnalObjId = [AnalysisId]
	FROM @MatchingAnalyses
	ORDER BY [AnalysisId]
	WHILE @@ROWCOUNT > 0
	BEGIN
		EXEC sp_executesql N'EXEC SetAgentEval @nAgentId, @AnalObjId, 0, @nvcDetails, @dtEval',
			N'@nAgentId INT, @AnalObjId INT, @nvcDetails NVARCHAR(4000), @dtEval DATETIME',
			@nAgentId, @AnalObjId, @nvcDetails, @dtEval
		IF @@ERROR <> 0
		BEGIN
			SET @nError = @@ERROR
			RAISERROR (N'UpdWfiAnalysisAndEval$: SetAgentEval failed', 16, 1, @nError)
			GOTO Finish
		END
		SELECT TOP 1 @AnalObjId = [AnalysisId]
		FROM @MatchingAnalyses
		WHERE [AnalysisId] > @AnalObjId
		ORDER BY [AnalysisId]
	END

Finish:
	RETURN @nError

GO
