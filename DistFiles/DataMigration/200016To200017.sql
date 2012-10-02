-- update database from version 200016 to 200017

--( Steve Miller, December 7, 2006: removed readuncommitted to get rid of
--( locking conflicts

BEGIN TRANSACTION

-- FindOrCreateWfiAnalysis has been incorporated into UpdWfiAnalysisAndEval$
-- because of problems getting a colelction of ids from FindOrCreateWfiAnalysis.
-- Therefore, it is being deleted, not updated.
if object_id('FindOrCreateWfiAnalysis') is not null begin
	if (select DbVer from Version$) = 200016
		print 'removing procedure FindOrCreateWfiAnalysis'
	drop proc FindOrCreateWfiAnalysis
end
if object_id('RemoveUnusedAnalyses$') is not null begin
	if (select DbVer from Version$) = 200016
		print 'removing procedure RemoveUnusedAnalyses$'
	drop proc RemoveUnusedAnalyses$
end
if (select DbVer from Version$) = 200016
	print 'creating procedure RemoveUnusedAnalyses$'
go

/*****************************************************************************
 *	Procedure: RemoveUnusedAnalyses$
 *
 *	Description:
 *		Performs 2 tasks:
 *		1) Deletes any "stale" evaluations, those that have an old date that
 *			are no longer valid.
 *		2) Removes Word form Analyses that are not referenced by an
 *			evaluation.
 *
 *	Parameters:
 * 		@nAgentID			ID of the agent
 *		@nWfiWordFormID		ID of the wordform
 *		@dtEval			Date-time of the evaluation
 *
 *  Selects:
 *		0 if everything has been deleted (or an error has occurred), 1 if there
 *		is more stuff remaining to delete (deletes a maximum of 16 objects at a
 *		 time).
 *	Returns:
 *		0 for success, otherwise the error code returned by DeleteObj$
 *****************************************************************************/

-- TODO (SteveMiller/RandyR): Determine if the orphaned records should really
--							be deleted by a trigger.
CREATE PROCEDURE RemoveUnusedAnalyses$
	@nAgentId INT,
	@nWfiWordFormID INT,
	@dtEval DATETIME
AS
	DECLARE
		@nIsNoCountOn INT,
		@nGonnerID INT,
		@nError INT,
		@nCountDeleted INT,
		@fMoreToDelete INT

	SET @nIsNoCountOn = @@OPTIONS & 512
	IF @nIsNoCountOn = 0
		SET NOCOUNT ON

	SET @nGonnerId = NULL
	SET @nError = 0
	SET @nCountDeleted = 0
	SET @fMoreToDelete = 0

	--== Delete all evaluations with null targets. ==--
	SELECT TOP 1 @nGonnerId = ae.[Id]
	FROM CmAgentEvaluation ae
	JOIN CmObject objae
		ON objae.[Id] = ae.[Id]
	WHERE ae.Target IS NULL
	ORDER BY ae.[Id]

	WHILE @@ROWCOUNT != 0 BEGIN
		IF @nCountDeleted >= 16
		BEGIN
			SET @fMoreToDelete = 1
			GOTO Finish
		END

		EXEC @nError = DeleteObj$ @nGonnerId
		SET @nCountDeleted = @nCountDeleted + 1

		IF @nError != 0
			GOTO Finish

		SELECT TOP 1 @nGonnerId = ae.[Id]
		FROM CmAgentEvaluation ae
		JOIN CmObject objae
			ON objae.[Id] = ae.[Id]
		WHERE ae.[Id] > @nGonnerId AND ae.Target IS NULL
		ORDER BY ae.[Id]
	END

	--== Delete stale evaluations on analyses ==--
	SELECT TOP 1 @nGonnerId = ae.[Id]
	FROM CmAgentEvaluation ae
	JOIN CmObject objae
		ON objae.[Id] = ae.[Id] AND objae.Owner$ = @nAgentId
	JOIN CmObject objanalysis
		ON objanalysis.[Id] = ae.Target
		AND objanalysis.Class$ = 5059 -- WfiAnalysis objects
		AND objanalysis.Owner$ = @nWfiWordFormID
	WHERE ae.DateCreated < @dtEval
	ORDER BY ae.[Id]

	WHILE @@ROWCOUNT != 0 BEGIN
		IF @nCountDeleted >= 16
		BEGIN
			SET @fMoreToDelete = 1
			GOTO Finish
		END

		EXEC @nError = DeleteObj$ @nGonnerId
		SET @nCountDeleted = @nCountDeleted + 1

		IF @nError != 0
			GOTO Finish

		SELECT TOP 1 @nGonnerId = ae.[Id]
		FROM CmAgentEvaluation ae
		JOIN CmObject objae
			ON objae.[Id] = ae.[Id] AND objae.Owner$ = @nAgentId
		JOIN CmObject objanalysis
			ON objanalysis.[Id] = ae.Target
			AND objanalysis.Class$ = 5059 -- WfiAnalysis objects
			AND objanalysis.Owner$ = @nWfiWordFormID
		WHERE ae.[Id] > @nGonnerId AND ae.DateCreated < @dtEval
		ORDER BY ae.[Id]
	END

	--== Make sure all analyses have human evaluations, if they, or glosses they own, are referred to by a WIC annotation. ==--
	DECLARE @adID INT, @analId INT, @humanAgentId INT, @rowcount INT, @rowcount2 INT, @evalId INT

	-- Get the ID of the CmAnnotationDefn that is the WIC type.
	SELECT @adID=Id
	FROM CmObject
	WHERE Guid$='eb92e50f-ba96-4d1d-b632-057b5c274132'

	-- Get Id of the first 'default user' human agent
	SELECT TOP 1 @humanAgentId = a.Id
	FROM CmAgent a
	JOIN CmAgent_Name nme
		ON a.Id = nme.Obj
	WHERE a.Human = 1 AND nme.Txt = 'default user'

	SELECT TOP 1 @analId = wa.[Id]
	FROM WfiAnalysis_ wa
	left outer JOIN WfiGloss_ gloss
		ON gloss.Owner$ = wa.Id
	JOIN CmAnnotation ann
		ON ann.InstanceOf = wa.[Id] OR ann.[InstanceOf] = gloss.[Id]
	JOIN CmObject ad
		ON ann.AnnotationType = ad.Id AND ad.Id = @adID
	WHERE wa.[Owner$] = @nWfiWordFormID
	ORDER BY wa.[Id]

	WHILE @@ROWCOUNT != 0 BEGIN
		IF @nCountDeleted >= 16
		BEGIN
			SET @fMoreToDelete = 1
			GOTO Finish
		END

		SELECT @evalId=Id
		FROM cmAgentEvaluation_ cae
		WHERE Target = @analId AND Owner$ = @humanAgentId

		IF @@ROWCOUNT = 0
		BEGIN
			EXEC @nError = SetAgentEval
				@humanAgentId,
				@analId,
				1,
				'Set by RemoveUnusedAnalyses$',
				@dtEval
			SET @nCountDeleted = @nCountDeleted + 1
			IF @nError != 0
				GOTO Finish
		END

		SELECT TOP 1 @analId = wa.[Id]
		FROM WfiAnalysis_ wa
		left outer JOIN WfiGloss_ gloss
			ON gloss.Owner$ = wa.Id
		JOIN CmAnnotation ann
			ON ann.InstanceOf = wa.[Id] OR ann.[InstanceOf] = gloss.[Id]
		JOIN CmObject ad
			ON ann.AnnotationType = ad.Id AND ad.Id = @adID
		WHERE wa.[Id] > @analId AND wa.[Owner$] = @nWfiWordFormID
		ORDER BY wa.[Id]
	END

	--== Delete orphan analyses, which have no evaluations ==--
	SELECT TOP 1 @nGonnerId = analysis.[Id]
	FROM CmObject analysis
	LEFT OUTER JOIN cmAgentEvaluation cae
		ON cae.Target = analysis.[Id]
	WHERE cae.Target IS NULL
		AND analysis.OwnFlid$ = 5062002		-- kflidWfiWordform_Analyses
		AND analysis.Owner$ = @nWfiWordFormID
	ORDER BY analysis.[Id]

	WHILE @@ROWCOUNT != 0 BEGIN
		IF @nCountDeleted >= 16
		BEGIN
			SET @fMoreToDelete = 1
			GOTO Finish
		END

		EXEC @nError = DeleteObj$ @nGonnerId

		IF @nError != 0
			GOTO Finish
		SET @nCountDeleted = @nCountDeleted + 1

		SELECT TOP 1 @nGonnerId = analysis.[Id]
		FROM CmObject analysis
		LEFT OUTER JOIN cmAgentEvaluation cae
			ON cae.Target = analysis.[Id]
		WHERE cae.Target IS NULL
			AND analysis.[Id] > @nGonnerId
			AND analysis.OwnFlid$ = 5062002		-- kflidWfiWordform_Analyses
			AND analysis.Owner$ = @nWfiWordFormID
		ORDER BY analysis.[Id]
	END

Finish:
	IF @nIsNocountOn = 0 SET NOCOUNT OFF
	SELECT @fMoreToDelete AS MoreToDelete
	RETURN @nError

go

if object_id('UpdWfiAnalysisAndEval$') is not null begin
	if (select DbVer from Version$) = 200016
		print 'removing procedure UpdWfiAnalysisAndEval$'
	drop proc UpdWfiAnalysisAndEval$
end
if (select DbVer from Version$) = 200016
	print 'creating procedure UpdWfiAnalysisAndEval$'
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
 *
 * Returns:
 *		0 for success, otherwise an error code.
 *****************************************************************************/

-- TODO (SteveMiller/JohnH): -- Needs testing when data becomes available.
CREATE PROC [UpdWfiAnalysisAndEval$]
	@nAgentId INT,
	@nWfiWordFormID INT,
	@ntXmlFormMsaPairIds NTEXT,
	@fAccepted BIT,
	@nvcDetails NVARCHAR(4000),
	@dtEval DATETIME
AS
	DECLARE
		@nIsNoCountOn INT,
		@nTranCount INT,
		@sysTranName SYSNAME,
		@nError INT,
		@nvcError NVARCHAR(100),
		@nAgentEvalId INT,
		@nPairRowcount INT,
		@rowcount INT,
		@AnalObjId INT

	SET @nIsNoCountOn = @@OPTIONS & 512
	IF @nIsNoCountOn = 0
		SET NOCOUNT ON

	SET @nError = 0

	--== Find or create the analysis ==--
	DECLARE @hDoc INT
	DECLARE @Pair TABLE (MsaId INT, FormId INT, Ord INT)
	DECLARE @PairChk TABLE ([Dummy] BIT)

	-- Process XML inputs.
	exec sp_xml_preparedocument @hDoc output, @ntXmlFormMsaPairIds
	if @@error <> 0 begin
		exec sp_xml_removedocument @hDoc
		SET @nvcError = 'UpdWfiAnalysisAndEval$: Could not create XML document handle.'
		GOTO Fail
	end

	-- Read XML data into table variable.
	INSERT INTO @Pair
		select ol.MsaId, ol.FormId, ol.Ord
		from	openxml(@hDoc, '/root/Pair')
		with ([MsaId] int, [FormId] int, Ord int) as ol
	SET @nPairRowcount = @@ROWCOUNT

	-- Do sanity check first. Check for same owner for MoForm and MSA.
	-- REVIEW (SteveMiller): As written, the whole procedure fails with
	-- one mismatch. It could be modified to give a list of mismatches.
	--( Response RandyR: I see no reason to worry about which ones mismatched, at this point.
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
	INSERT INTO @PairChk
		SELECT TOP 1 0 --( A dummy value for the one & only record
		FROM @Pair p
		LEFT OUTER JOIN CmObject msaowner ON msaowner.[Id] = p.MsaId
		LEFT OUTER JOIN CmObject formowner ON formowner.[Id] = p.FormId
		WHERE ISNULL(msaowner.Owner$, -1) != ISNULL(formowner.Owner$, -2)
	if @@ROWCOUNT != 0
	begin
		-- They have to be owned by the same object
		SET @nvcError = 'UpdWfiAnalysisAndEval$: At least one MSA/Form pair were not owned by the same lexical entry.'
		GOTO Fail
	end

	-- Try to find one(s) that already exist.
	DECLARE @MatchingAnalyses TABLE (AnalysisId INT primary key) -- Hold matches in this table variable.
	IF @nPairRowcount = 0 BEGIN
		-- No substance to analysis, so look for ones.
		INSERT INTO @MatchingAnalyses (AnalysisId)
			select wa.Id
			from WfiAnalysis wa
			left outer join WfiAnalysis_MorphBundles mb ON mb.Src = wa.Id
			where mb.Dst is null
			order by wa.Id
		SET @rowcount = @@rowcount
	end
	else begin
		-- Look for a match with substance.
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
		SET @rowcount = @@rowcount
	end

	-- Turn loose of the handle.
	exec sp_xml_removedocument @hDoc
	if @@error <> 0 begin
		SET @nvcError = 'UpdWfiAnalysisAndEval$: Could not remove XML document handle.'
		GOTO Fail
	end

	-- If @MatchingAnalyses has no rows, then we need to create a new analysis.
	IF @rowcount = 0
	BEGIN
		DECLARE @uid uniqueidentifier,
			@nTrnCnt INT,
			@sTranName VARCHAR(50),
			@CurOrd INT,
			@CurFormId INT,
			@CurMsaId INT,
			@CurMBId INT

		-- Determine if a transaction already exists.
		-- If one does then create a savepoint, otherwise create a transaction.
		set @nTrnCnt = @@trancount
		set @sTranName = 'NewWfiAnalysis_tr' + convert(varchar(8), @@nestlevel)
		if @nTrnCnt = 0 begin tran @sTranName
		else save tran @sTranName

		-- Create a new WfiAnalysis, and add it to the wordform
		set @uid = null
		exec @nError = CreateOwnedObject$
			5059,
			@AnalObjId output,
			null,
			@nWfiWordFormID,
			5062002,
			25,
			null,
			0,
			1,
			@uid output
		if @nError <> 0
		begin
			-- There was an error in CreateOwnedObject
			if @nTrnCnt = 0 rollback tran @sTranName
			SET @nvcError = 'UpdWfiAnalysisAndEval$: CreateOwnedObject could not create a new analysis object.'
			GOTO Fail
		end
		INSERT INTO @MatchingAnalyses (AnalysisId) VALUES (@AnalObjId)

		-- Loop through all MSA/form pairs and create WfiMorphBundle for each.
		DECLARE @idsCur2 CURSOR,
			@defaultSenseID INT
		set @idsCur2 = CURSOR FAST_FORWARD for
			SELECT MsaId, FormId
			FROM @Pair
			ORDER BY Ord
		open @idsCur2
		fetch next from @idsCur2 into @CurMsaId, @CurFormId
		while @@fetch_status = 0
		begin
			-- Find default sense
			SELECT TOP 1 @defaultSenseID = senses.[DST]
			FROM LexEntry le
			JOIN MoMorphoSyntaxAnalysis_ msa
				ON msa.[Owner$] = le.[ID]
				AND msa.[ID] = @CurMsaId
			JOIN LexEntry_Senses senses
				ON le.[ID] = senses.[Src]
			ORDER BY senses.[Ord]

			-- Create a new WfiMorphBundle, and add it to the analysis.
			set @uid = null
			set @CurMBId = null
			exec @nError = CreateOwnedObject$
				5112,
				@CurMBId output,
				null,
				@AnalObjId,
				5059011,
				27,
				null,
				0,
				1,
				@uid output
			if @nError <> 0
			begin
				-- There was an error in CreateOwnedObject
				close idsCur2
				deallocate idsCur2
				if @nTrnCnt = 0 rollback tran @sTranName
				SET @nvcError = 'UpdWfiAnalysisAndEval$: CreateOwnedObject could not create a new morph bundle object.'
				GOTO Fail
			end
			-- Add MoForm, MSA, and the default sense.
			UPDATE WfiMorphBundle
			SET Morph = @CurFormId, Msa = @CurMsaId, Sense = @defaultSenseID
			WHERE id = @CurMBId
			if @@error <> 0
			begin
				-- Couldn't update form, msa, and sense data.
				close idsCur2
				deallocate idsCur2
				if @nTrnCnt = 0 rollback tran @sTranName
				SET @nvcError = 'UpdWfiAnalysisAndEval$: Could not update the new morph bundle.'
				GOTO Fail
			end
			fetch next from @idsCur2 into @CurMsaId, @CurFormId
		end
		close @idsCur2
		deallocate @idsCur2
		if @nTrnCnt = 0 commit tran @sTranName
	END

	--== Update or create the evaluation for each analysis ID in the @MatchingAnalyses table variable. ==--
	SELECT TOP 1 @AnalObjId = [AnalysisId]
	FROM @MatchingAnalyses
	ORDER BY [AnalysisId]

	WHILE @@ROWCOUNT != 0
	BEGIN
		EXEC @nError = SetAgentEval
			@nAgentId,
			@AnalObjId,
			@fAccepted,
			@nvcDetails,
			@dtEval
		IF @nError != 0
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

if object_id('SetAgentEval') is not null begin
	if (select DbVer from Version$) = 200016
		print 'removing procedure SetAgentEval'
	drop proc SetAgentEval
end
if (select DbVer from Version$) = 200016
	print 'creating procedure SetAgentEval'
go

/*****************************************************************************
 * Procedure: SetAgentEval
 *
 * Description:
 *		Updates an agent evaluation with the latest acceptance, details, and
 *		date-time of the evaluation. Creates a new agent evaluation if one
 *		doesn't already exist. If the Agent doesn't know, the accepted flag
 *		is set to 2 (or optionally NULL), and the associated evaluations will
 *		be deleted.
 *
 * Parameters:
 * 		@nAgentID	ID of the agent
 *      @nTargetID	ID of the thing analyzed, whether a WfiAnalysis or a
 *						WfiWordform.
 *		@nAccepted	Has the agent evaluation been accepted by Agent?
 *						0 = not accepted
 *						1 = accepted
 *						2 = don't know
 *						NULL = don't know
 *		@nvcDetails	Additional detail
 *		@dtEval		Date-time of the evaluation
 *
 * Returns:
 *		0 for success, otherwise an error code.
 *****************************************************************************/
CREATE PROC SetAgentEval
	@nAgentID INT,
	@nTargetID INT, --( A WfiAnalysis.ID or a WfiWordform.ID
	@nAccepted INT,
	@nvcDetails NVARCHAR(4000),
	@dtEval DATETIME
AS
	DECLARE
		@nIsNoCountOn INT,
		@nTranCount INT,
		@sysTranName SYSNAME,
		@nEvals INT,
		@nEvalId INT,
		@nNewObjId INT,
		@guidNewObj UNIQUEIDENTIFIER,
		@nNewObjTimeStamp INT,
		@nError INT,
		@nvcError NVARCHAR(100)

	SET @nError = 0

	SET @nIsNoCountOn = @@OPTIONS & 512
	IF @nIsNoCountOn = 0
		SET NOCOUNT ON

	--( Take care of transaction stuff
	SET @nTranCount = @@TRANCOUNT
	SET @sysTranName = 'SetAgentEval_tr' + CONVERT(VARCHAR(2), @@NESTLEVEL)
	IF @nTranCount = 0
		BEGIN TRAN @sysTranName
	ELSE
		SAVE TRAN @sysTranName

	--( See if we have an Agent Evaluation already
	SELECT TOP 1 @nEvalId = co.[Id]
	FROM CmAgentEvaluation cae
	JOIN CmObject co ON co.[Id] = cae.[Id]
		AND co.Owner$ = @nAgentID
	WHERE cae.Target = @nTargetID
	ORDER BY co.[Id]

	SET @nEvals = @@ROWCOUNT

	--== Remove Eval ==--

	--( If we don't know if the analysis is accepted or not,
	--( we don't really have an eval for it. And if we don't
	--( have an eval for it, we need to get rid of it.

	IF @nAccepted = 2 OR @nAccepted IS NULL BEGIN
		WHILE @nEvals > 0 BEGIN
			EXEC DeleteObj$ @nEvalId

			SELECT TOP 1 @nEvalId = co.[Id]
			FROM CmAgentEvaluation cae
			JOIN CmObject co ON co.[Id] = cae.[Id]
				AND co.Owner$ = @nAgentID
			WHERE cae.Target = @nTargetID
				AND co.[Id] > @nEvalId
			ORDER BY co.[Id]

			SET @nEvals = @@ROWCOUNT
		END
	END

	--== Create or Update Eval ==--

	--( Make sure the evaluation is set the way it should be.

	ELSE BEGIN

		--( Create a new Agent Evaluation
		IF @nEvals = 0 BEGIN

			EXEC @nError = CreateObject_CmAgentEvaluation
				@dtEval,
				@nAccepted,
				@nvcDetails,
				@nAgentId,					--(owner
				23006,	--(ownflid  23006
				NULL,						--(startobj
				@nNewObjId OUTPUT,
				@guidNewObj OUTPUT,
				0,			--(ReturnTimeStamp
				@nNewObjTimeStamp OUTPUT

			IF @nError != 0 BEGIN
				SET @nvcError = 'SetAgentEval: CreateObject_CmAgentEvaluation failed.'
				GOTO Fail
			END

			UPDATE CmAgentEvaluation with (serializable)
			SET Target = @nTargetID
			WHERE Id = @nNewObjId
		END

		--( Update the existing Agent Evaluation
		ELSE

			UPDATE CmAgentEvaluation with (serializable)
			SET
				DateCreated = @dtEval,
				Accepted = @nAccepted,
				Details = @nvcDetails
			FROM CmAgentEvaluation cae
			JOIN CmObject co ON co.[Id] = cae.[Id]
				AND co.Owner$ = @nAgentID
			WHERE cae.Target = @nTargetID
		--( END
	END
	GOTO Finish

Finish:

	SET @nIsNoCountOn = @@OPTIONS & 512
	IF @nIsNoCountOn = 0
		SET NOCOUNT ON

	-- determine if a transaction or savepoint was created
	IF @nTranCount = 0
		COMMIT TRAN @sysTranName

	RETURN @nError

Fail:
	RAISERROR (@nvcError, 16, 1, @nError)
	IF @nTranCount !=0
		ROLLBACK TRAN @sysTranName

	RETURN @nError
go


declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200016
begin
	update Version$ set DbVer = 200017
	COMMIT TRANSACTION
	print 'database updated to version 200017'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200016 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO