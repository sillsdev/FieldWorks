-- update database from version 200045 to 200046
BEGIN TRANSACTION  --( will be rolled back if wrong version#

---------------------------------------------------------------------
-- Data fixes per Ken Zook
---------------------------------------------------------------------

-- Split PartOfSpeechOrSlot into PartOfSpeech and Slot
update MoInflectionalAffixMsa
	set Slot = PartOfSpeechOrSlot, PartOfSpeech = co.owner$, PartOfSpeechOrSlot = null
from MoInflectionalAffixMsa mia, CmObject co
where mia.PartOfSpeechOrSlot = co.id and co.class$ = 5036

update MoInflectionalAffixMsa
	set Slot = null, PartOfSpeech = PartOfSpeechOrSlot, PartOfSpeechOrSlot = null
from MoInflectionalAffixMsa mia, CmObject co
where mia.PartOfSpeechOrSlot = co.id and co.class$ = 5049

---------------------------------------------------------------------
-- Dropped one index and added another to CmObject in FwCore.sql.
---------------------------------------------------------------------

DROP INDEX CmObject.Ind_CmObject_OwnFlid$_Id_Owner$_OwnOrd$

CREATE INDEX Ind_CmObject_Guid$ ON dbo.CmObject (Guid$)

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
		-- No substance to analysis, so look for ones with no morphemes for this wordform
		INSERT INTO @MatchingAnalyses (AnalysisId)
			select wa.Id
			from WfiAnalysis_ wa
			left outer join WfiAnalysis_MorphBundles mb ON mb.Src = wa.Id
			where mb.Dst is null and wa.Owner$ = @nWfiWordFormID
			order by wa.Id
		SET @rowcount = @@rowcount
	end
	else begin
		-- Look for a match with substance.
		INSERT INTO @MatchingAnalyses (AnalysisId)
			--( A "match with substance" is one in which
			--( the number of morph bundles equal the number of the MoForm and
			--( MorphoSyntaxAnanlysis (MSA) IDs passed in to the stored procedure.
			--(
			--( Randy Regnier, April 19, 2005:
			--( [the count is] crucial, since if the count of bundles don't match
			--( the number of pairs (corresponds to a bundle), they can't possibly
			--( be the "same" analysis...When the count is the same, that does not
			--( ensure the analysis is the same, so the next most crucial part of
			--( the testing is that the ids of the pairs given in the input match
			--( those found in the DB, and in the precise order given. No variation
			--( at all is allowed.
			select DupChkLst.[ObjId]
			from	(
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
			SELECT MsaId, FormId FROM @Pair ORDER BY Ord
		open @idsCur2
		fetch next from @idsCur2 into @CurMsaId, @CurFormId
		while @@fetch_status = 0
		begin

			-- Find default sense
			/* LT-1102 removes this capability, whcih was added as per LT-35.
			SELECT TOP 1 @defaultSenseID = senses.[DST]
			FROM LexEntry le
			JOIN MoMorphoSyntaxAnalysis_ msa
				ON msa.[Owner$] = le.[ID]
				AND msa.[ID] = @CurMsaId
			JOIN LexEntry_Senses senses
				ON le.[ID] = senses.[Src]
			ORDER BY senses.[Ord]
			*/

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
			SET Morph = @CurFormId, Msa = @CurMsaId -- ( Remove. as per LT-1102, Sense = @defaultSenseID
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
		EXEC sp_executesql N'EXEC SetAgentEval @nAgentId, @AnalObjId, @fAccepted, @nvcDetails, @dtEval',
			N'@nAgentId INT, @AnalObjId INT, @fAccepted INT, @nvcDetails NVARCHAR(4000), @dtEval DATETIME',
			@nAgentId, @AnalObjId, @fAccepted, @nvcDetails, @dtEval

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

---------------------------------------------------------------------

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200045
begin
	update Version$ set DbVer = 200046
	COMMIT TRANSACTION
	print 'database updated to version 200046'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200045 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
