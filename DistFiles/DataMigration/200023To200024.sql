-- update database from version 200023 to 20002
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------
-- Fix DefaultAnalysisGlosses was not including puntuation
-------------------------------------------------------------

if object_id('fnGetDefaultAnalysesGlosses') is not null begin
	print 'removing function fnGetDefaultAnalysesGlosses'
	drop function [fnGetDefaultAnalysesGlosses]
end
go
print 'creating function fnGetDefaultAnalysesGlosses'
go

create FUNCTION fnGetDefaultAnalysesGlosses (
	@nStTxtParaId INT, @nAnnotType INT, @nAnnotPunct INT)
RETURNS @tblDefaultAnalysesGlosses TABLE (
	WordformId INT,
	AnalysisId INT,
	GlossId INT,
	BaseAnnotationId INT,
	InstanceOf INT,
	BeginOffset INT,
	EndOffset INT)
AS BEGIN

	DECLARE
		@nWordformId INT,
		@nAnalysisId INT,
		@nGlossId INT

	declare @defaults table (
		WfId INT,
		AnalysisId INT,
		GlossId INT,
		[Score] INT)
	-- Get the 'real' (non-default) data
	INSERT INTO @tblDefaultAnalysesGlosses
	SELECT
		coalesce(wfwg.id, wfwa.id, a.InstanceOf) AS WordformId,
		coalesce(wawg.id, wai.id),
		wgi.id,
		ba.[Id] AS BaseAnnotationId,
		a.InstanceOf,
		ba.BeginOffset,
		ba.EndOffset
	FROM CmBaseAnnotation ba (readuncommitted)
	JOIN CmAnnotation a  (readuncommitted) ON a.[Id] = ba.[Id]
		AND a.AnnotationType = @nAnnotType
	-- these joins handle the case that instanceof is a WfiAnalysis; all values will be null otherwise
	LEFT OUTER JOIN WfiAnalysis wai (readuncommitted) ON wai.id = a.InstanceOf -- 'real' analysis (is the instanceOf)
	LEFT OUTER JOIN CmObject waio (readuncommitted) on waio.id = wai.id -- CmObject of analysis instanceof
	LEFT OUTER JOIN CmObject wfwa (readuncommitted) on wfwa.id = waio.owner$ -- wf that owns wai
	-- these joins handle the case that instanceof is a WfiGloss; all values will be null otherwise.
	LEFT OUTER JOIN WfiGloss wgi (readuncommitted) on wgi.id = a.instanceOf -- 'real' gloss (is the instanceof)
	LEFT OUTER JOIN CmObject wgio (readuncommitted) on wgio.id = wgi.id
	LEFT OUTER JOIN CmObject wawg (readuncommitted) on wawg.id = wgio.owner$ -- ananlyis that owns wgi
	LEFT OUTER JOIN CmObject wfwg (readuncommitted) on wfwg.id = wawg.owner$ -- wordform that owns wgi (indirectly)
	WHERE ba.BeginObject = @nStTxtParaId

	-- InstanceOf is a WfiAnalysis filling out a default gloss if possible.

	UPDATE @tblDefaultAnalysesGlosses SET GlossId = WgId
	FROM @tblDefaultAnalysesGlosses t
	JOIN (
		SELECT Sub2.WaId, Sub2.WgId, MAX(Sub2.CountInstance) AS MaxCountInstance
		FROM (
			SELECT t.InstanceOf AS WaId, wg.[Id] AS WgId, COUNT(ann.[Id]) AS CountInstance
			FROM @tblDefaultAnalysesGlosses t
			JOIN WfiGloss_ wg (READUNCOMMITTED) ON wg.Owner$ = t.InstanceOf
			LEFT OUTER JOIN CmAnnotation ann (READUNCOMMITTED) ON ann.InstanceOf = wg.[Id]
			GROUP BY t.InstanceOf, wg.[Id]
			) Sub2
		GROUP BY Sub2.WaId, Sub2.WgId
		) Sub1 ON Sub1.WaId = t.InstanceOf
	WHERE t.GlossId IS NULL

	-- WfiGlosses owned by those WfiWordforms

	UPDATE @tblDefaultAnalysesGlosses SET GlossId = WgId, AnalysisId = WaId
	FROM @tblDefaultAnalysesGlosses t
	JOIN (
		SELECT Sub2.WfId, Sub2.WaId, Sub2.WgId,
			MAX(Sub2.CountInstance) AS MaxCountInstance
		FROM (
			SELECT t.WordformId AS WfId, wa.[Id] AS WaId, wg.[Id] AS WgId,
				COUNT(ann.[Id]) AS CountInstance
			FROM @tblDefaultAnalysesGlosses t
			JOIN WfiAnalysis_ wa (READUNCOMMITTED) ON wa.Owner$ = t.WordformId
			JOIN WfiGloss_ wg (READUNCOMMITTED) ON wg.Owner$ = wa.[Id]
			LEFT OUTER JOIN CmAnnotation ann (READUNCOMMITTED) ON ann.InstanceOf = wg.[Id]
			GROUP BY t.WordformId, wa.[Id], wg.[Id]
			) Sub2
		GROUP BY Sub2.WfId, Sub2.WaId, Sub2.WgId
		) Sub1 ON Sub1.WfId = t.WordformId
	WHERE t.AnalysisId IS NULL

	-- Final option is InstanceOf is WfiWordform, there are analyses but no glosses

	UPDATE @tblDefaultAnalysesGlosses SET AnalysisId = WaId
	FROM @tblDefaultAnalysesGlosses t
	JOIN (
		SELECT Sub2.WfId, Sub2.WaId, MAX(Sub2.CountInstance) AS MaxCountInstance
		FROM (
			SELECT t.WordformId AS WfId, wa.[Id] AS WaId, COUNT(ann.[Id]) AS CountInstance
			FROM @tblDefaultAnalysesGlosses t
			JOIN WfiAnalysis_ wa (READUNCOMMITTED) ON wa.Owner$ = t.WordformId
			LEFT OUTER JOIN CmAnnotation ann (READUNCOMMITTED) ON ann.InstanceOf = wa.[Id]
			GROUP BY t.WordformId, wa.[Id]
			) Sub2
		GROUP BY Sub2.WfId, Sub2.WaId
		) Sub1 ON Sub1.WfId = t.WordformId
	WHERE t.AnalysisId IS NULL

	-- Also include punctuation annotations.
	INSERT INTO @tblDefaultAnalysesGlosses
	SELECT
		NULL,
		NULL,
		NULL,
		ba.[Id] AS BaseAnnotationId,
		NULL,
		ba.BeginOffset,
		ba.EndOffset
	FROM CmBaseAnnotation ba (readuncommitted)
	JOIN CmAnnotation a  (readuncommitted) ON a.[Id] = ba.[Id]
		AND a.AnnotationType = @nAnnotPunct
	WHERE ba.BeginObject = @nStTxtParaId

	RETURN
END
go
-------------------------------------------------------------
-- Block senses, as per LT-1102.
-------------------------------------------------------------

if object_id('UpdWfiAnalysisAndEval$') is not null begin
	print 'removing proc UpdWfiAnalysisAndEval$'
	drop proc [UpdWfiAnalysisAndEval$]
end
go
print 'creating proc UpdWfiAnalysisAndEval$'
go

create proc [UpdWfiAnalysisAndEval$]
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
		LEFT OUTER JOIN CmObject msaowner (readuncommitted) ON msaowner.[Id] = p.MsaId
		LEFT OUTER JOIN CmObject formowner (readuncommitted) ON formowner.[Id] = p.FormId
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
			from WfiAnalysis wa (readuncommitted)
			left outer join WfiAnalysis_MorphBundles mb (readuncommitted) ON mb.Src = wa.Id
			where mb.Dst is null
			order by wa.Id
		SET @rowcount = @@rowcount
	end
	else begin
		-- Look for a match with substance.
		INSERT INTO @MatchingAnalyses (AnalysisId)
			select DupChkLst.[ObjId]
			from	(select	WfiAMBS_1.[Src] ObjId, count(*) Cnt
				from	WfiAnalysis_MorphBundles WfiAMBS_1 (readuncommitted)
					join CmObject CmO (readuncommitted) ON WfiAMBS_1.[Src] = CmO.[ID]
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
					from	WfiAnalysis_MorphBundles WfiAMBS_2 (readuncommitted)
					JOIN WfiMorphBundle mb (readuncommitted) ON mb.[Id] = WfiAMBS_2.[Dst]
					JOIN @Pair NewWfiAM ON mb.[Morph] = NewWfiAM.[FormId]
						and mb.[msa] = NewWfiAM.[MsaId]
						and WfiAMBS_2.[Ord] = NewWfiAM.[Ord]
						and WfiAMBS_2.[Src] = DupChkLst.[ObjId]
					join CmObject cmoForm (readuncommitted)
						On cmoForm.Class$ IN (5027, 5028, 5029, 5045)
							and cmoForm.Id=NewWfiAM.[FormId]
					join CmObject cmoMSA (readuncommitted)
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
/* LT-1102 removes this capability, whcih was added as per LT-35.
			-- Find default sense
			SELECT TOP 1 @defaultSenseID = senses.[DST]
			FROM LexEntry le (READUNCOMMITTED)
			JOIN MoMorphoSyntaxAnalysis_ msa (READUNCOMMITTED)
				ON msa.[Owner$] = le.[ID]
				AND msa.[ID] = @CurMsaId
			JOIN LexEntry_Senses senses (READUNCOMMITTED)
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

go
-------------------------------------------------------------------------------

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200023
begin
	update Version$ set DbVer = 200024
	COMMIT TRANSACTION
	print 'database updated to version 200024'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200023 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO