if object_id('UpdWfiAnalysisAndEval$') is not null begin
	print 'removing proc UpdWfiAnalysisAndEval$'
	drop proc [UpdWfiAnalysisAndEval$]
end
go
print 'creating proc UpdWfiAnalysisAndEval$'
go

/*****************************************************************************
 * [NOTE: UpdWfiAnalysisAndEval$ SP has been ported to C#. It is now in the ProcessAnalysis method of ParseFiler. (RandyR, 9 November 2008)]
 * Procedure: UpdWfiAnalysisAndEval$
 *
 * Description: Performs two main tasks:
 *
 *	1) Finds extant matching WfiAnalysis object(s), and if not found,
 *		creates a new one.
 *	2) Updates the Agent Evaluation, and if not found, creates a new one.
 *
 * Parameters:
 * 		@nAgentID			ID of the agent
 *		@nWfiWordFormID		ID of the wordform
 *		@ntXmlFormMsaPairIds	XML string passed to FindOrCreateWfiAnalysis.
 *		@fAccepted			Has the agent evaluation been accepted?
 *		@nvcDetails			Additional evaluation detail
 *		@dtEval			Date-time of the evaluation
 *
 * Returns:
 *		0 for success, otherwise an error code.
 *
 * Notes:
 *	The only program that calls this today (4 September 2009) is
 *	ParserFiler.cs.
 *****************************************************************************/
CREATE PROC [UpdWfiAnalysisAndEval$]
	@nAgentId INT,
	@nWfiWordFormID INT,
	@ntXmlFormMsaPairIds NTEXT,
	@fAccepted BIT,
	@nvcDetails NVARCHAR(4000),
	@dtEval DATETIME
AS
	DECLARE @nIsNoCountOn INT,
		@nError INT,
		@nvcError NVARCHAR(100),
		@nAgentEvalId INT,
		@nPairRowcount INT,
		@rowcount INT,
		@AnalObjId INT
	DECLARE	@Pair TABLE (MsaId INT, FormId INT, Ord INT)

	SET @nIsNoCountOn = @@OPTIONS & 512
	IF @nIsNoCountOn = 0
		SET NOCOUNT ON
	SET @nError = 0

	BEGIN --( Start Block for xml handling
		-- Process XML inputs.
		DECLARE @hDoc INT
		EXEC sp_xml_preparedocument @hDoc OUTPUT, @ntXmlFormMsaPairIds
		IF @@error <> 0 BEGIN
			EXEC sp_xml_removedocument @hDoc
			SET @nvcError = 'UpdWfiAnalysisAndEval$: Could not create XML document handle.'
			GOTO Fail
		END
		-- Read XML data into table variable.
		INSERT INTO @Pair
			SELECT ol.[MsaId], ol.[FormId], ol.[Ord]
			FROM OPENXML(@hDoc, '/root/Pair')
			WITH ([MsaId] INT, [FormId] INT, [Ord] INT) AS ol
		SET @nPairRowcount = @@ROWCOUNT
		-- Turn loose of the handle.
		EXEC sp_xml_removedocument @hDoc
		IF @@ERROR <> 0 BEGIN
			SET @nvcError = 'UpdWfiAnalysisAndEval$: Could not remove XML document handle.'
			GOTO Fail
		END
		--( Analysis failure, if @nPairRowcount is 0, and nothing more can be done.
		IF @nPairRowcount = 0 GOTO Finish
	END --( End Block for xml handling

	-- Try to find match(es) that already exist.
	-- A "match" is one in which
	-- (1) the number of morph bundles equal the number of the MoForm and
	--	MorphoSyntaxAnanlysis (MSA) IDs passed in to the stored procedure, and
	-- (2) The IDs of each MSA+Form pair match those of the corresponding WfiMorphBundle.
	DECLARE @nTrnCnt INT,
		@sTranName VARCHAR(50),
		@CurMBId INT
	DECLARE @MatchingAnalyses TABLE (AnalysisId INT PRIMARY KEY) -- Hold matches in this table variable.
	INSERT INTO @MatchingAnalyses (AnalysisId)
		select DupChkLst.[ObjId]
		from	(select	WfiAMBS_1.[Src] ObjId, count(*) Cnt
			from	WfiAnalysis_MorphBundles WfiAMBS_1
				join CmObject CmO ON WfiAMBS_1.[Src] = CmO.[ID]
					and CmO.[OwnFlid$] = 5062002 -- Analyses FLID in the WfiWordform class
					and CmO.[Owner$] = @nWfiWordFormID
			group by WfiAMBS_1.[Src]
			-- find all analyses that have the same number of Morphs as the new analysis
			having	count(*) = @nPairRowcount
			) DupChkLst
		where	DupChkLst.[Cnt] = (
				-- if the number of matching rows between the new analysis and an existing
				--	analysis' morphs is the same as the number of rows in the new analysis
				--	then there is a collision
				select	count(*)
				from	WfiAnalysis_MorphBundles WfiAMBS_2
				JOIN WfiMorphBundle mb ON mb.[Id] = WfiAMBS_2.[Dst]
				JOIN @Pair NewWfiAM ON mb.[Morph] = NewWfiAM.[FormId]
					and mb.[msa] = NewWfiAM.[MsaId]
					and WfiAMBS_2.[Ord] = NewWfiAM.[Ord]
					and WfiAMBS_2.[Src] = DupChkLst.[ObjId]
				join CmObject cmoForm
					On cmoForm.Class$ IN (5027, 5028, 5029, 5045)
						and cmoForm.Id=NewWfiAM.[FormId]
				join CmObject cmoMSA
					On cmoMSA.Class$ IN (5001, 5031, 5032, 5038)
						and cmoMSA.Id=NewWfiAM.[MsaId]
			)
	IF @@ROWCOUNT = 0 --( @@ROWCOUNT is the number of rows inserted into @MatchingAnalyses
	BEGIN
		-- If @MatchingAnalyses has no rows, then we need to create a new analysis.
		DECLARE @uid UNIQUEIDENTIFIER,
			@CurFormId INT,
			@CurMsaId INT,
			@CurOrd INT
		-- Determine if a transaction already exists.
		-- If one does then create a savepoint, otherwise create a transaction.
		SET @nTrnCnt = @@TRANCOUNT
		SET @sTranName = 'NewWfiAnalysis_tr' + CONVERT(VARCHAR(8), @@NESTLEVEL)
		IF @nTrnCnt = 0 BEGIN TRAN @sTranName
		ELSE SAVE TRAN @sTranName

		-- Create a new WfiAnalysis, and add it to the wordform
		SET @uid = null
		SET @AnalObjId = null
		EXEC CreateOwnedObject$
			5059,
			@AnalObjId OUTPUT,
			null,
			@nWfiWordFormID,
			5062002,
			25,
			null,
			0,
			1,
			@uid OUTPUT
		IF @@ERROR <> 0
		BEGIN
			-- There was an error in CreateOwnedObject. The parser
			-- must have gotten out of sync, and we don't want this
			-- analysis anymore. Rollback and move on.
			IF @nTrnCnt = 0
				ROLLBACK TRAN @sTranName
			SET @nError = 0
			GOTO Finish
		END
		--( Add new analysis ID to the @MatchingAnalyses table variable.
		INSERT INTO @MatchingAnalyses (AnalysisId) VALUES (@AnalObjId)

		-- Loop through all MSA/form pairs and create WfiMorphBundle for each.
		SELECT TOP 1 @CurMsaId = [MsaId], @CurFormId = [FormId], @CurOrd = [Ord]
		FROM @Pair
		ORDER BY [Ord]
		WHILE @@ROWCOUNT > 0
		BEGIN
			-- Create a new WfiMorphBundle, and add it to the analysis.
			SET @uid = null
			SET @CurMBId = null
			EXEC CreateOwnedObject$
				5112,
				@CurMBId OUTPUT,
				null,
				@AnalObjId,
				5059011,
				27,
				null,
				0,
				1,
				@uid OUTPUT
			IF @@ERROR <> 0
			BEGIN
				-- There was an error in CreateOwnedObject. The parser
				-- must have gotten out of sync, and we don't want this
				-- morph bundle anymore. Rollback and move on.
				IF @nTrnCnt = 0
					ROLLBACK TRAN @sTranName
				SET @nError = 0
				GOTO Finish
			END

			-- Set MoForm and MSA.
			UPDATE WfiMorphBundle
			SET [Morph] = @CurFormId, [Msa] = @CurMsaId
			WHERE id = @CurMBId
			IF @@ERROR <> 0
			BEGIN
				IF @nTrnCnt = 0
					ROLLBACK TRAN @sTranName
				SET @nError = 0
				GOTO Finish
			END
			--( Get next pair to work on.
			SELECT TOP 1 @CurMsaId = [MsaId], @CurFormId = [FormId], @CurOrd = [Ord]
			FROM @Pair
			WHERE [Ord] > @CurOrd
			ORDER BY [Ord]
		END
		IF @nTrnCnt = 0
			COMMIT TRAN @sTranName
		END

	--== Update or create the evaluation for each analysis ID in the @MatchingAnalyses table variable. ==--
	SELECT TOP 1 @AnalObjId = [AnalysisId]
	FROM @MatchingAnalyses
	ORDER BY [AnalysisId]
	WHILE @@ROWCOUNT > 0
	BEGIN
		EXEC sp_executesql N'EXEC SetAgentEval @nAgentId, @AnalObjId, @fAccepted, @nvcDetails, @dtEval',
			N'@nAgentId INT, @AnalObjId INT, @fAccepted INT, @nvcDetails NVARCHAR(4000), @dtEval DATETIME',
			@nAgentId, @AnalObjId, @fAccepted, @nvcDetails, @dtEval
		IF @@ERROR <> 0
		BEGIN
			SET @nvcError = 'UpdWfiAnalysisAndEval$: SetAgentEval failed'
			GOTO Fail
		END
		SELECT TOP 1 @AnalObjId = [AnalysisId]
		FROM @MatchingAnalyses
		WHERE [AnalysisId] > @AnalObjId
		ORDER BY [AnalysisId]
	END

	GOTO Finish

Fail:
	RAISERROR (@nvcError, 16, 1, @nError)

Finish:
	IF @nIsNocountOn = 0 SET NOCOUNT OFF
	RETURN @nError

GO
