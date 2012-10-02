-- update database from version 200114 to 200115
BEGIN TRANSACTION  --( will be rolled back if wrong version#

---------------------------------------------------------------------
-- Update UpdWfiAnalysisAndEval$
---------------------------------------------------------------------
if object_id('UpdWfiAnalysisAndEval$') is not null begin
	print 'removing proc UpdWfiAnalysisAndEval$'
	drop proc [UpdWfiAnalysisAndEval$]
end
go
print 'creating proc UpdWfiAnalysisAndEval$'
go

/*****************************************************************************
 * Procedure: UpdWfiAnalysisAndEval$
 *
 * Description:
 *		Performs 2 tasks: 1) Finds the WfiAnalysis, and if not found, creates
 *		a new one, via FindOrCreateWfiAnalysis. 2) Updates the Agent
 *		Evaluation, and if not found, creates a new one.
 *
 * Parameters:
 * 		@nAgentID			ID of the agent
 *		@nWfiWordFormID		ID of the wordform
 *		@ntXmlFormMsaPairIds	XML string passed to FindOrCreateWfiAnalysis.
 *		@fAccepted			Has the agent evaluation been accepted?
 *		@nvcDetails			Additional evaluation detail
 *		@dtEval			Date-time of the evaluation
 *		@uAppGuid			App guid used in Sync$ table
 *		@nMsgType			Type of change for the Sync$ table
 *
 * Returns:
 *		0 for success, otherwise an error code.
 *****************************************************************************/
CREATE PROC [UpdWfiAnalysisAndEval$]
	@nAgentId INT,
	@nWfiWordFormID INT,
	@ntXmlFormMsaPairIds NTEXT,
	@fAccepted BIT,
	@nvcDetails NVARCHAR(4000),
	@dtEval DATETIME,
	@uAppGuid UNIQUEIDENTIFIER,
	@nMsgType INT
AS
	DECLARE @nIsNoCountOn INT,
		@nError INT,
		@nvcError NVARCHAR(100),
		@nAgentEvalId INT,
		@nPairRowcount INT,
		@rowcount INT,
		@innerRowCount INT,
		@AnalObjId INT
	DECLARE	@Pair TABLE (MsaId INT, FormId INT, Ord INT, SenseCount INT default 0)

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
			SELECT ol.[MsaId], ol.[FormId], ol.[Ord], 0
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

	BEGIN --( Start Sanity check block
		--( Do sanity check first. Check for same owner for MoForm and MSA.
		--(
		--( Query logic:
		--(    1. If any rows are returned from the query, we have a problem
		--(       with the data.
		--(    2. A single row is sufficient to throw out data, so we return
		--(       only "TOP 1" from the query.
		--(    3. We don't care what the value of the single-row field is,
		--(       only that we returned a row, given our criteria.
		--(    4. We want a missing owner to return a row, so we do an outer
		--(       join on CmObject for both the MSA owner and the form owner.
		--(    5. Nulls can't be compared, so null owners are converted to
		--(       values.
		--(    6. We can't give a null MSA owner and a null Form owner the
		--(       same value, or the query would clear.
		DECLARE @PairChk TABLE ([Dummy] BIT)
		INSERT INTO @PairChk
			SELECT TOP 1 0 --( A dummy value for the one & only record
			FROM @Pair p
			LEFT OUTER JOIN CmObject msaowner ON msaowner.[Id] = p.[MsaId]
			LEFT OUTER JOIN CmObject formowner ON formowner.[Id] = p.[FormId]
			WHERE ISNULL(msaowner.[Owner$], -1) != ISNULL(formowner.[Owner$], -2)
		IF @@ROWCOUNT != 0
		BEGIN
			-- They have to be owned by the same object
			SET @nvcError = 'UpdWfiAnalysisAndEval$: At least one MSA/Form pair were not owned by the same lexical entry.'
			GOTO Fail
		END
	END --( End Sanity check block

	--( Figure out how many analyses need to be found, based on 'sense combinations'.
	--( The answer will be in @senseComboCount at the end of the loop.
	DECLARE @senseComboCount INT,
		@currentSenseCount INT,
		@CurOrd INT,
		@CurMsaId INT
	SET @senseComboCount = 1
	SELECT TOP 1 @CurMsaId = [MsaId], @CurOrd = [Ord]
	FROM @Pair
	ORDER BY [Ord]
	SET @rowcount = @@ROWCOUNT
	WHILE @rowcount > 0
	BEGIN --( Start @rowcount > 0
		SELECT @currentSenseCount = COUNT(*)
		FROM LexSense (READUNCOMMITTED)
		WHERE [MorphoSyntaxAnalysis] = @CurMsaId
		IF @currentSenseCount > 0
		BEGIN
			UPDATE @Pair
			SET [SenseCount] = @currentSenseCount
			WHERE [MsaId] = @CurMsaId
			SET @senseComboCount = @senseComboCount * @currentSenseCount
		END
		--( Get next pair to work on.
		SELECT TOP 1 @CurMsaId = [MsaId], @CurOrd = [Ord]
		FROM @Pair
		WHERE [Ord] > @CurOrd
		ORDER BY [Ord]
		SET @rowcount = @@ROWCOUNT
	END --( End @rowcount > 0

	-- Try to find match(es) that already exist.
	--( A "match" is one in which
	--( the number of morph bundles equal the number of the MoForm and
	--( MorphoSyntaxAnanlysis (MSA) IDs passed in to the stored procedure.
	DECLARE @nTrnCnt INT,
		@sTranName VARCHAR(50),
		@CurMBId INT
	DECLARE @MatchingAnalyses TABLE (AnalysisId INT PRIMARY KEY) -- Hold matches in this table variable.

	INSERT INTO @MatchingAnalyses (AnalysisId)
	SELECT DupChkLst.[ObjId]
	FROM	(
		SELECT
			wamb.Src AS ObjId,
			COUNT(*) AS Cnt
		FROM WfiWordform_Analyses wwfa
		JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wwfa.Dst
		JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
		JOIN @Pair pair ON pair.FormId = wmb.Morph
			AND pair.MsaId = wmb.msa
			AND pair.Ord = wamb.Ord
		WHERE wwfa.Src = @nWfiWordFormID
		GROUP BY wamb.Src
		-- find all analyses that have the same number of Morphs as the new analysis
		HAVING COUNT(*) = @nPairRowcount
		) AS DupChkLst
	SET @rowcount = @@ROWCOUNT

	-- If @MatchingAnalyses has too few rows,
	-- then we need to add enough new analyses to match the sense combo count.
	WHILE @rowcount < @senseComboCount
	BEGIN
		DECLARE @uid UNIQUEIDENTIFIER,
			@CurFormId INT
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
			-- There was an error in CreateOwnedObject
			IF @nTrnCnt = 0 ROLLBACK TRAN @sTranName
			SET @nvcError = 'UpdWfiAnalysisAndEval$: CreateOwnedObject could not create a new analysis object.'
			GOTO Fail
		END
		--( Add new one to the @MatchingAnalyses table variable
		INSERT INTO @MatchingAnalyses (AnalysisId) VALUES (@AnalObjId)

		-- Loop through all MSA/form pairs and create WfiMorphBundle for each.
		SELECT TOP 1 @CurMsaId = [MsaId], @CurFormId = [FormId], @CurOrd = [Ord]
		FROM @Pair
		ORDER BY [Ord]
		SET @innerRowCount = @@ROWCOUNT
		WHILE @innerRowCount > 0
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
				-- There was an error in CreateOwnedObject
				IF @nTrnCnt = 0 ROLLBACK TRAN @sTranName
				SET @nvcError = 'UpdWfiAnalysisAndEval$: CreateOwnedObject could not create a new morph bundle object.'
				GOTO Fail
			END

			-- Add MoForm, MSA, and the default sense.
			UPDATE WfiMorphBundle
			SET [Morph] = @CurFormId, [Msa] = @CurMsaId
			WHERE id = @CurMBId
			IF @@ERROR <> 0
			BEGIN
				-- Couldn't update form and msa data.
				IF @nTrnCnt = 0 ROLLBACK TRAN @sTranName
				SET @nvcError = 'UpdWfiAnalysisAndEval$: Could not update the new morph bundle.'
				GOTO Fail
			END
			--( Get next pair to work on.
			SELECT TOP 1 @CurMsaId = [MsaId], @CurFormId = [FormId], @CurOrd = [Ord]
			FROM @Pair
			WHERE [Ord] > @CurOrd
			ORDER BY [Ord]
			SET @innerRowCount = @@ROWCOUNT
		END
		IF @nTrnCnt = 0 COMMIT TRAN @sTranName
		SELECT @rowcount = count(*)
		FROM @MatchingAnalyses
	END

	--( Fix up the sense property of each morph bundle, if needed.
	DECLARE @curSenseId INT
	DECLARE @sensesRowCount INT
	DECLARE @mbRowCount INT
	SET @curMBId = null

	SELECT TOP 1 @CurMsaId = [MsaId], @CurOrd = [Ord], @currentSenseCount = [SenseCount]
	FROM @Pair
	ORDER BY [Ord]
	SET @rowcount = @@ROWCOUNT
	WHILE @rowcount > 0
	BEGIN --( Start @rowcount > 0
		IF @currentSenseCount = 1 OR @currentSenseCount = @senseComboCount
		BEGIN --( IF @currentSenseCount = 1 OR @currentSenseCount = @senseComboCount
			SET @curSenseId = NULL --( Don't want the one from a previous call, if this query goes nowhere.
			SELECT TOP 1 @curSenseId = [Id]
			FROM LexSense (READUNCOMMITTED)
			WHERE [MorphoSyntaxAnalysis] = @CurMsaId
			ORDER BY [Id]

			SELECT TOP 1 @curMBId = mb.[Id]
			FROM @MatchingAnalyses ma, WfiAnalysis_MorphBundles v_anal_mb, WfiMorphBundle mb
			WHERE ma.[AnalysisId] = v_anal_mb.[Src]
				AND v_anal_mb.[Dst] = mb.[Id]
				AND v_anal_mb.[Ord] = @CurOrd
				AND mb.[Msa] = @CurMsaId
			ORDER BY mb.[Id]
			SET @mbRowCount = @@ROWCOUNT
			WHILE @mbRowCount > 0
			BEGIN --( Start @mbRowCount > 0
				--( Only set the sense when it is null for the bundle.
				DECLARE @mbSense INT
				Select @mbSense = [Sense]
				FROM WfiMorphBundle
				WHERE [Id] = @curMBId

				IF @mbSense is null
				BEGIN --( IF @mbSense is null
					SET @nTrnCnt = @@TRANCOUNT
					SET @sTranName = 'UpdateSenseCombos_tr' + CONVERT(VARCHAR(8), @@NESTLEVEL)
					IF @nTrnCnt = 0 BEGIN TRAN @sTranName
					ELSE SAVE TRAN @sTranName
					--( Update the sense info for the current bu8ndle.
					UPDATE WfiMorphBundle
					SET [Sense] = @curSenseId
					WHERE [Id] = @curMBId
					IF @@ERROR <> 0
					BEGIN --( Start @@ERROR <> 0
						-- Couldn't update sense data.
						IF @nTrnCnt = 0 ROLLBACK TRAN @sTranName
						SET @nvcError = 'UpdWfiAnalysisAndEval$: Could not update the senses in the morph bundle.'
						GOTO Fail
					END --( End @@ERROR <> 0

					--( Add row to Sync$ table for the WfiMorphBundle change
					INSERT sync$ (LpInfoId, Msg, ObjId, ObjFlid)
					VALUES (@uAppGuid, @nMsgType, @curMBId, 5112004)
					IF @@ERROR <> 0
					BEGIN --( Start @@ERROR <> 0
						-- Couldn't Add info to Sync$ table.
						IF @nTrnCnt = 0 ROLLBACK TRAN @sTranName
						SET @nvcError = 'UpdWfiAnalysisAndEval$: StoreSyncRec$ failed'
						GOTO Fail
					END --( End @@ERROR <> 0
					IF @nTrnCnt = 0 COMMIT TRAN @sTranName
				END --( IF @mbSense is null

				IF @currentSenseCount > 1
				BEGIN
					SELECT TOP 1 @curSenseId = [Id]
					FROM LexSense (READUNCOMMITTED)
					WHERE [MorphoSyntaxAnalysis] = @CurMsaId AND [Id] > @curSenseId
					ORDER BY [Id]
				END

				SELECT TOP 1 @curMBId = mb.[Id]
				FROM @MatchingAnalyses ma, WfiAnalysis_MorphBundles v_anal_mb, WfiMorphBundle mb
				WHERE ma.[AnalysisId] = v_anal_mb.[Src]
					AND v_anal_mb.[Dst] = mb.[Id]
					AND v_anal_mb.[Ord] = @CurOrd
					AND mb.[Msa] = @CurMsaId
					AND mb.[Id] > @curMBId
				ORDER BY mb.[Id]
				SET @mbRowCount = @@ROWCOUNT
			END --( End @mbRowCount > 0
		END --( IF @currentSenseCount = 1 OR @currentSenseCount = @senseComboCount
		--( Get next pair to work on.
		SELECT TOP 1 @CurMsaId = [MsaId], @CurOrd = [Ord], @currentSenseCount = [SenseCount]
		FROM @Pair
		WHERE [Ord] > @CurOrd
		ORDER BY [Ord]
		SET @rowcount = @@ROWCOUNT
	END --( End @rowcount > 0

	--== Update or create the evaluation for each analysis ID in the @MatchingAnalyses table variable. ==--
	SELECT TOP 1 @AnalObjId = [AnalysisId]
	FROM @MatchingAnalyses
	ORDER BY [AnalysisId]
	SET @rowcount = @@ROWCOUNT
	WHILE @rowcount != 0
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
		SET @rowcount = @@ROWCOUNT
	END

	GOTO Finish

Fail:
	RAISERROR (@nvcError, 16, 1, @nError)

Finish:
	IF @nIsNocountOn = 0 SET NOCOUNT OFF
	RETURN @nError

GO

---------------------------------------------------------------------
-- Remove fnIsPrime$
---------------------------------------------------------------------
if object_id('fnIsPrime$') is not null begin
	print 'removing function fnIsPrime$'
	drop function [fnIsPrime$]
end
go

---------------------------------------------------------------------

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200114
begin
	update Version$ set DbVer = 200115
	COMMIT TRANSACTION
	print 'database updated to version 200115'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200114 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
