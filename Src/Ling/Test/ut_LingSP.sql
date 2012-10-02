--===================================================================
--== Agent
--===================================================================

IF OBJECT_ID('ut_AgentEval_IsAgentAgreement') IS NOT NULL BEGIN
	DROP PROCEDURE [ut_AgentEval_IsAgentAgreement]
END
GO
CREATE PROCEDURE ut_AgentEval_IsAgentAgreement AS
BEGIN

	DECLARE
		@nWordformId INT,
		@nAgentHumanId INT,
		@nAgentNonhumanId INT,
		@nEvalId3 INT,
		@fIsAgentAgreement BIT

	--== Check 1 ==--

	SET @nWordformId = 99999999
	SET @nAgentHumanId = 99999999
	SET @nAgentNonhumanId = 99999999
	SET @fIsAgentAgreement = 1

	EXEC IsAgentAgreement$ @nWordformId, @nAgentHumanId, @nAgentNonhumanId, @fIsAgentAgreement OUTPUT

	--( This should actually work, because both agree that there's nothing out there.
	IF @fIsAgentAgreement = 0
		EXEC tsu_failure 'IsAgentAgreement$ should pass'

	--== Check 2 ==--

	--( Make one of the evaluations bad

--TODO (SteveMller): Get this test going when we have a better data set.

--	UPDATE CmAgentEvaluation SET "Accepted" = 0 WHERE "Id" = @nEvalId3

-- 	EXEC IsAgentAgreement$ @nWordformId, @nAgentHumanId, @nAgentNonhumanId, @fIsAgentAgreement OUTPUT

-- 	IF @fIsAgentAgreement = 1 	--( We don't expect test 2 to pass
-- 		EXEC tsu_failure 'IsAgentAgreement$ should not pass'

END
GO

---------------------------------------------------------------------

IF OBJECT_ID('ut_AgentEval_FindOrCreateCmAgent') IS NOT NULL BEGIN
	DROP PROCEDURE [ut_AgentEval_FindOrCreateCmAgent]
END
GO
CREATE PROCEDURE ut_AgentEval_FindOrCreateCmAgent AS
BEGIN
DECLARE
		@Count1 INT,
		@Count2 INT

	--( Make sure the procedure finds an agent with a null version

	SELECT @Count1 = COUNT(*)
	FROM CmAgent a
	JOIN CmAgent_Name an ON an.Obj = a.Id
	WHERE an.Txt = 'default user' AND Human = 1 AND Version IS NULL;

	EXEC FindOrCreateCmAgent 'default user', 1, NULL;

	SELECT @Count2 = COUNT(*)
	FROM CmAgent a
	JOIN CmAgent_Name an ON an.Obj = a.Id
	WHERE an.Txt = 'default user' AND Human = 1 AND Version IS NULL;

	IF @Count1 != @Count2
		EXEC tsu_failure 'Agent "default user" should have been found, not duplicated.'

	--( Create a new agent

	SELECT @Count1 = COUNT(*)
	FROM CmAgent a
	JOIN CmAgent_Name an ON an.Obj = a.Id
	WHERE an.Txt = 'test user' AND Human = 0 AND Version = 'test version';

	EXEC FindOrCreateCmAgent 'test user', 0, 'test version';

	SELECT @Count2 = COUNT(*)
	FROM CmAgent a
	JOIN CmAgent_Name an ON an.Obj = a.Id
	WHERE an.Txt = 'test user' AND Human = 0 AND Version = 'test version';

	IF @Count1 = @Count2
		EXEC tsu_failure 'Agent "test user" was not created.'

END
GO

--===================================================================
--== Interlinear Text Tool
--===================================================================

IF OBJECT_ID('ut_Interlinear_fnGetDefaultAnalysisGloss') IS NOT NULL BEGIN
	DROP PROCEDURE [ut_Interlinear_fnGetDefaultAnalysisGloss]
END
GO
CREATE PROCEDURE ut_Interlinear_fnGetDefaultAnalysisGloss AS
BEGIN

	DECLARE
		@nStTxtParaId INT,
		@nCount INT,
		@nWordFormId INT,
		@nAnalysisId1 INT,
		@nGlossId1 INT,
		@nScore1 INT,
		@nAnalysisId2 INT,
		@nGlossId2 INT,
		@nScore2 INT

	SELECT @nCount = COUNT(wg.[Id])
	FROM wfiGloss wg
	JOIN cmObject og ON og.[Id] = wg.[Id]
	JOIN wfiAnalysis wa ON wa.[Id] = og.Owner$
	JOIN cmAnnotation a ON a.InstanceOf = wg.[Id]
	JOIN cmBaseAnnotation ba ON ba.[Id] = a.[Id]
	JOIN stTxtPara tp ON tp.[Id] = ba.BeginObject

	IF @nCount != 7
		EXEC tsu_failure 'Should have 7 wfiGloss records for StTxtPara record "pus yalola nihimbilira...".'

	DECLARE @tblGlossesAnalyses TABLE (
		AnalysisId INT,
		GlossId INT,
		Score INT)

	SELECT @nWordFormId = mt.Obj
	FROM WfiWordform_Form mt
	WHERE mt.Txt = 'nihimbilira'

	INSERT INTO @tblGlossesAnalyses
	SELECT * FROM dbo.fnGetDefaultAnalysisGloss(@nWordFormId)

	IF @@ROWCOUNT != 2
		EXEC tsu_failure 'Should have two records for "nihimbilira".'

	SELECT TOP 1
		@nAnalysisId1 = AnalysisId,
		@nGlossId1 = GlossId,
		@nScore1 = Score
	FROM @tblGlossesAnalyses
	ORDER BY Score DESC

	SELECT TOP 1
		@nAnalysisId2 = oanalysis.[Id],
		@nGlossId2 = ogloss.[Id],
		@nScore2 = (COUNT(a.InstanceOf) + 10000)
	FROM CmAnnotation a
	JOIN CmObject ogloss ON ogloss.[Id] = a.InstanceOf
	JOIN CmObject oanalysis ON oanalysis.[Id] = ogloss.Owner$
		AND oanalysis.Owner$ = @nWordFormId
	GROUP BY oanalysis.[Id], ogloss.[Id]
	ORDER BY (COUNT(a.InstanceOf) + 10000) DESC

	IF @nAnalysisId1 != @nAnalysisId2
		EXEC tsu_failure 'The analyses IDs aren''t lining up.'
	IF @nGlossId1 != @nGlossId2
		EXEC tsu_failure 'The glosses aren''t lining up.'
	IF @nScore1 != @nScore2
		EXEC tsu_failure 'The scores aren''t lining up.'
END
GO

-----------------------------------------------------------------------------

IF OBJECT_ID('ut_Interlinear_fnGetDefaultAnalysesGlosses') IS NOT NULL BEGIN
	DROP PROCEDURE [ut_Interlinear_fnGetDefaultAnalysesGlosses]
END
GO
CREATE PROCEDURE ut_Interlinear_fnGetDefaultAnalysesGlosses AS
BEGIN
	DECLARE @tblGlossesAnalyses TABLE (
		WordformId INT,
		AnalysisId INT,
		GlossId INT,
		BaseAnnotationId INT,
		InstanceOf INT,
		BeginOffset INT,
		EndOffset INT,
		UserApproved INT)

	DECLARE
		@nStTxtParaId INT,
		@nAnnotationTypeId INT,
		@nAnnotationType INT,
		@nWordFormId INT,
		@nMin INT,
		@nMax INT,
		@nCount INT

	SET @nMin = 1
	SET @nMax = 23

	SELECT @nStTxtParaId = tp.[Id]
	FROM StTxtPara tp
	WHERE SUBSTRING(Contents, @nMin, @nMax) = 'pus yalola nihimbilira.'

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Probable Data Change: should have a record in StTxtPara for "pus yalola nihimbilira...".'

	SELECT TOP 1 @nAnnotationTypeId = d.Id
	FROM CmAnnotation a
	JOIN CmAnnotationDefn d ON d.Id = a.AnnotationType
	JOIN CmPossibility_Name pn ON pn.Obj = d.Id
	WHERE pn.Txt = 'Wordform In Context'

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Probable Data change: Should have Possibility Name "Wordform in Context".'

	SELECT TOP 1 @nAnnotationType = d.Id
	FROM CmAnnotation a
	JOIN CmAnnotationDefn d ON d.Id = a.AnnotationType
	JOIN CmPossibility_Name pn ON pn.Obj = d.Id
	WHERE pn.Txt = 'Punctuation In Context'

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Probable Data change: Should have Possibility Name "Punctuation in Context".'

	-- REVIEW (SteveMiller): This may or may not be right, as don't really know if
	-- I understand JohnT's modifications. Sending a null through for the third
	-- parameter @nAnnotationType, the result set is 8, as originally written.
	-- Apparently a null means to drop out punctuation.

	INSERT INTO @tblGlossesAnalyses
	SELECT *
	FROM dbo.fnGetDefaultAnalysesGlosses(@nStTxtParaId, @nAnnotationTypeId, NULL)
	ORDER BY BeginOffset

	IF @@ROWCOUNT != 8
		EXEC tsu_failure 'Should have 8 records for "pus yalola nihimbilira...".'

	-- REVIEW (SteveMiller): This may or may not be right, as don't really know if
	-- I understand JohnT's modifications. Apparently adding the ID for puncutation
	-- in context returns 3 rows, in addition to the normal 8 wordform in context.

	DELETE @tblGlossesAnalyses

	INSERT INTO @tblGlossesAnalyses
	SELECT *
	FROM dbo.fnGetDefaultAnalysesGlosses(@nStTxtParaId, @nAnnotationTypeId, @nAnnotationType)
	ORDER BY BeginOffset

	IF @@ROWCOUNT != 11
		EXEC tsu_failure 'Should have 11 records for "pus yalola nihimbilira...".'

	SELECT @nWordFormId = wf.[Id]
	FROM WfiWordform wf
	JOIN WfiWordform_Form mt ON mt.Obj = wf.[Id]
		AND mt.Txt = 'nihimbilira'

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Should have found an Id for "nihimbilira".'

	SELECT @nCount = COUNT(*)
	FROM @tblGlossesAnalyses
	WHERE WordformId = @nWordformId
	GROUP BY WordformId

END
GO

--===================================================================
--== ParseBench
--===================================================================

IF OBJECT_ID('ut_ParseBench_Setup') IS NOT NULL BEGIN
	DROP PROCEDURE [ut_ParseBench_Setup]
END
GO
CREATE PROCEDURE ut_ParseBench_Setup AS
BEGIN

	--== Setup ==--

	DECLARE
		@nNonhumanId INT,
		@nHumanId INT,
		@nWritingSystemId INT

	--( "Normal - represents the parser using the current state of the grammar and lexicon"
	SELECT @nNonhumanId = [Id] FROM CmAgent WHERE [Human] = 0 AND Version = 'Normal'
	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Nonhuman agent not found. The database has changed.'

	SELECT @nHumanId = [Id] FROM CmAgent WHERE [Human] = 1 --( AND Version = 'Normal'
	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Human agent not found. The database has changed.'

	SELECT @nWritingSystemId = [Id] FROM LgWritingSystem WHERE [ICULocale] = 'xkal'
	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'ICU Locale xkal not found. The database has changed.'
END
GO

---------------------------------------------------------------------

IF OBJECT_ID('ut_ParseBench_CountRange') IS NOT NULL BEGIN
	DROP PROCEDURE [ut_ParseBench_CountRange]
END
GO
CREATE PROCEDURE ut_ParseBench_CountRange AS
BEGIN

	DECLARE
		@nWritingSystemId INT,
		@nNonhumanAgentId INT,
		@nCount INT

	DECLARE @tblParses TABLE (
		nId INT,
		nvcTxt NVARCHAR(4000))

	--( agent and writing system checks in setup
	SELECT @nWritingSystemId = [Id] FROM LgWritingSystem WHERE [ICULocale] = 'xkal'
	SELECT @nNonhumanAgentId = [Id] FROM CmAgent ca
		JOIN CmAgent_Name can on can.obj = ca.id
		WHERE [ca.Human] = 0 AND [ca.Version] = 'Normal' and can.txt = "M3Parser"

	--== Checks ==--

	INSERT INTO @tblParses
	SELECT DISTINCT [Id], [Txt]
	FROM dbo.fnGetParseCountRange(@nNonhumanAgentId, @nWritingSystemId, 1, 2, 2)

	SELECT @nCount = COUNT(nvcTxt) FROM @tblParses WHERE nvcTxt = 'yulalo'
	IF @nCount != 1
		EXEC tsu_failure 'Expecting "yulalo" to have 1 parse'

	DELETE FROM @tblParses

	INSERT INTO @tblParses
	SELECT DISTINCT [Id], [Txt]
	FROM dbo.fnGetParseCountRange(@nNonhumanAgentId, @nWritingSystemId, 1, 0, 0)
	WHERE Txt = 'nihimbilira'

	IF @@ROWCOUNT != 0
		EXEC tsu_failure 'Expecting "nihimbilra" to have 0 parses'

	END
GO

---------------------------------------------------------------------

-- TODO (SteveMiller): Finish this.

IF OBJECT_ID('ut_ParseBench_MatchAgentEvals') IS NOT NULL BEGIN
	DROP PROCEDURE [ut_ParseBench_MatchAgentEvals]
END
GO
CREATE PROCEDURE ut_ParseBench_MatchAgentEvals AS
BEGIN

	DECLARE
		@nWritingSystemId INT,
		@nNonhumanAgentId INT,
		@nHumanAgentId INT

	--( agent and writing system checks in setup
	SELECT @nWritingSystemId = [Id] FROM LgWritingSystem WHERE [ICULocale] = 'xkal'
	SELECT @nNonhumanAgentId = [Id] FROM CmAgent ca
		JOIN CmAgent_Name can on can.obj = ca.id
		WHERE [ca.Human] = 0 AND [ca.Version] = 'Normal' and can.txt = "M3Parser"
	SELECT @nHumanAgentId = [Id] FROM CmAgent WHERE [Human] = 1 AND [Version] IS NULL

END
GO

===================================================================
== Parser
===================================================================

--IF OBJECT_ID('ut_Parser_RemoveUnusedAnalyses$') IS NOT NULL BEGIN
--	DROP PROCEDURE [ut_Parser_RemoveUnusedAnalyses$]
--END
--GO
--CREATE PROCEDURE ut_Parser_RemoveUnusedAnalyses$ AS
--BEGIN
--
--	DECLARE
--		@nAgentId INT,
--		@nWordformId INT,
--		@nEvalId INT,
--		@dtNow DATETIME,
--		@nId INT
--
--	SELECT @dtNow = GETDATE()
--
--	SELECT TOP 1 @nAgentId = [Id] FROM CmAgent WHERE Human = 0
--
--	--( Analysis owned by a wordform, targeted by an agent eval which
--	--( is owned by a particular agent. Going for the wordform ID and
--	--( evaluation Id
--
--	SELECT TOP 1 @nWordformId = objAnal.Owner$, @nEvalId = ae.[Id]
--	FROM CmObject objanal
--	JOIN WfiWordForm wf ON wf.[Id] = objanal.Owner$
--	JOIN CmAgentEvaluation ae ON ae.Target = objanal.[Id]
--	JOIN CmObject objae ON objae.[Id] = ae.[Id] AND objae.Owner$ = @nAgentId
--	WHERE objanal.Class$ = 5059 --(WfiAnalysis objects
--
--	EXEC RemoveUnusedAnalyses$ @nAgentId, @nWordformId, @dtNow
--
--	--( Check to make sure the stale eval got whacked.
--
--	SET @nId = NULL
--	SELECT TOP 1 @nId = ae.[Id] FROM CmAgentEvaluation ae WHERE ae.[Id] = @nEvalId
--
--	IF @nId IS NOT NULL OR @@ROWCOUNT > 0
--		EXEC tsu_failure 'Not expecting stale eval'
--	--( Need another call to remove the orphan analysis, since we now do only one
--	--( change per call (JohnT, 19 Sep 2006)
--	EXEC RemoveUnusedAnalyses$ @nAgentId, @nWordformId, @dtNow
--	--( Check to make sure the orphaned analysis got whacked.
--
--	SELECT TOP 1 @nId = objanalysis.[id]
--	FROM CmObject objanalysis
--	LEFT OUTER JOIN CmAgentEvaluation ae ON ae.Target = objanalysis.[Id]
--	WHERE ae.Target IS NULL
--		AND objanalysis.OwnFlid$ = 5062002  --(WfiWordform.Analyses
--		AND objanalysis.Owner$ = @nWordformId
--
--	IF @nId IS NOT NULL OR @@ROWCOUNT > 0
--		EXEC tsu_failure 'Not expecting orphan analysis'
--
--END
--GO

---------------------------------------------------------------------

IF OBJECT_ID('ut_Parser_SetAgentEval') IS NOT NULL BEGIN
	DROP PROCEDURE [ut_Parser_SetAgentEval]
END
GO
CREATE PROCEDURE ut_Parser_SetAgentEval AS
BEGIN

	DECLARE
		@nAgentId INT,
		@nTargetId INT,
		@dtToday DATETIME,
		@nAccepted INT

	SELECT @nAgentId = [Id] FROM CmAgent WHERE Human = 1
	SELECT @dtToday = GETDATE()

	SELECT TOP 1 @nTargetId = co.[Id]
	FROM CmAgentEvaluation cae
	JOIN CmObject co ON co.[Id] = cae.[Id]
		AND co.[Owner$] = @nAgentID
	WHERE Accepted = 1

	--( Change the eval from accepted to not accepted.
	EXEC SetAgentEval @nAgentId, @nTargetId, 0, N'Test', @dtToday

	SELECT @nAccepted = cae.Accepted
	FROM CmAgentEvaluation cae
	JOIN CmObject co ON co.[Id] = cae.[Id]
		AND co.[Owner$] = @nAgentID
	WHERE cae.[Target] = @nTargetID

	IF @nAccepted != 0 OR @@ROWCOUNT = 0
		EXEC tsu_failure 'Should''ve changed from accepted to not accepted'

	--( Change the eval from not accepted to "dunno".
	EXEC SetAgentEval @nAgentId, @nTargetId, 2, N'Evals should be deleted', @dtToday

	SELECT TOP 1 @nAccepted = cae.Accepted
	FROM CmAgentEvaluation cae
	JOIN CmObject co ON co.[Id] = cae.[Id]
		AND co.[Owner$] = @nAgentID
	WHERE cae.[Target] = @nTargetID

	IF @@ROWCOUNT != 0
		EXEC tsu_failure 'Should''ve deleted existing evals'

END
GO

---------------------------------------------------------------------

IF OBJECT_ID('ut_Parser_UpdWfiAnalysisAndEval$') IS NOT NULL BEGIN
	DROP PROCEDURE ut_Parser_UpdWfiAnalysisAndEval$
END
GO
CREATE PROCEDURE ut_Parser_UpdWfiAnalysisAndEval$ AS
BEGIN

	DECLARE
		@nWfiWordformId INT,
		@nAgentId INT,
		@fAccepted BIT,
		@nAgentEvalId INT,
		@nvcDetails NVARCHAR(4000),
		@dtEval DATETIME,
		@nLexEntryId INT,
		@nMsaId INT,
		@nMoFormId INT,
		@nOrder INT,
		@ntXmlFormMsaPairIds NVARCHAR(4000),
		@nvcXML NVARCHAR(4000),
		@nAnalysisId INT,
		@uAppGuid UNIQUEIDENTIFIER,
		@nMsgType INT,
		@nReturn INT

	SET @nvcDetails = 'unit testing'
	SET @dtEval = GETDATE()

	--==( Try a particular set of pairs )==--

	SELECT @nWfiWordformId = Obj FROM WfiWordform_Form WHERE Txt = 'himbilira'
	IF @@ROWCOUNT != 1
		EXEC tsu_failure '"himbilira" not found in WfiWordform_Form. The database has changed.'

	SELECT @nAgentId = aes.Src, @fAccepted = ae.Accepted, @nAgentEvalId = ae.Id
	FROM CmAgentEvaluation ae
	JOIN CmAgent_Evaluations aes ON aes.Dst = ae.Id
	WHERE ae.Target = @nWfiWordformId

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'agent not found with an analysis for the wordform. The database has changed.'

	SELECT @nLexEntryId = Obj FROM LexEntry_CitationForm WHERE Txt = 'himbilira'
	IF @@ROWCOUNT != 1
		EXEC tsu_failure '"himbilira" not found in LexEntry_CitationForm. The database has changed.'

	--( MSA, Form/Morph, Ord
	--(
	--( The resulting string of the loop below should look something like this:
	--( <root><Pair MsaId="6128" FormId="6126" Ord="1"/><Pair MsaId="6113" FormId="6112" Ord="2"/><Pair MsaId="6134" FormId="6133" Ord="3"/></root>

	SET @nvcXML = '<root>'

	--( First time through we'll snag the target analysis ID while we have it.

	SELECT TOP 1 @nMsaId = wmb.MSA, @nMoFormId = wmb.Morph, @nOrder = wamb.Ord, @nAnalysisId = wamb.Src
	FROM WfiAnalysis_MorphBundles wamb
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN CmObject o ON o.Id = wamb.Src
		AND o.Owner$ = @nWfiWordformId
	ORDER BY Ord

	WHILE @@ROWCOUNT != 0 BEGIN
		SET @nvcXML = @nvcXML +
			'<Pair MsaId="' + CAST(@nMsaId AS NVARCHAR(10)) +
			'" FormId="' + CAST(@nMoFormId AS NVARCHAR(10)) +
			'" Ord="' + CAST(@nOrder AS NVARCHAR(10)) + '"/>'

		SELECT TOP 1 @nMsaId = wmb.MSA, @nMoFormId = wmb.Morph, @nOrder = wamb.Ord
		FROM WfiAnalysis_MorphBundles wamb
		JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
		JOIN CmObject o ON o.Id = wamb.Src
			AND o.Owner$ = @nWfiWordformId
		WHERE Ord > @nOrder
		ORDER BY Ord
	END
	SET @nvcXML = @nvcXML + '</root>'

	IF @nvcXML = '<root></root>'
		EXEC tsu_failure 'No morph bundles have been found for the wordform. The database has changed.'

	SET @uAppGuid = 'E8F3514C-D0B5-4978-AA07-56B27EE68779'
	SET @nMsgType = 7 --( SyncMsg.ksyncAddEntry
	EXEC @nReturn = UpdWfiAnalysisAndEval$
		@nAgentId, @nWfiWordformId, @nvcXML, @fAccepted, @nvcDetails, @dtEval

	IF @nReturn != 0
		EXEC tsu_failure 'Error in UpdWfiAnalysisAndEval$ I'

	SELECT @nvcDetails = ae.Details
	FROM CmAgentEvaluation ae
	WHERE Id = @nAgentEvalId AND Target = @nAnalysisId

	IF @nvcDetails != 'unit testing'
		EXEC tsu_failure 'Update I of the agent evaluation didn''t work.'

	--==( Test No Pairs Passed )==--

	SET @nvcXML = '<root></root>'

	SELECT TOP 1 @nWfiWordformId = wffa.Src, @nAnalysisId = wa.Id
	FROM WfiAnalysis wa
	JOIN WfiWordform_Analyses wffa ON wffa.Dst = wa.Id
	LEFT OUTER JOIN WfiAnalysis_MorphBundles mb ON mb.Src = wa.Id
	WHERE mb.Dst IS NULL

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Failed to find a single wordfrom with no bundles. The database has changed.'

	SELECT @nAgentEvalId = cae.Id FROM CmAgentEvaluation cae WHERE Target = @nAnalysisId
	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Failed to find an eval for the analysis. The database has changed.'

	EXEC @nReturn = UpdWfiAnalysisAndEval$
		@nAgentId, @nWfiWordformId, @nvcXML, @fAccepted, @nvcDetails, @dtEval

	IF @nReturn != 0
		EXEC tsu_failure 'Error in UpdWfiAnalysisAndEval$ II'

	--( SET @nvcDetails = ''  --( reset

	--( SELECT @nvcDetails = ae.Details
	--( FROM CmAgentEvaluation ae
	--( WHERE Id = @nAgentEvalId AND Target = @nAnalysisId

	--( IF @nvcDetails != 'unit testing'
	--( 	EXEC tsu_failure 'Update II of the agent evaluation didn''t work.'

	--TODO (SteveMiller): Lots more checks can be done here.

END
GO

--=============================================================================
--== Lex
--=============================================================================

IF OBJECT_ID('ut_Lex_Setup') IS NOT NULL BEGIN
	DROP PROCEDURE [ut_Lex_Setup]
END
GO
CREATE PROCEDURE ut_Lex_Setup AS
BEGIN
	DECLARE
		@nVernWS INT,
		@nAnalysisWS INT

	SELECT TOP 1 @nVernWS = Obj FROM LgWritingSystem_Name WHERE Txt = N'Kalaba'
	SELECT TOP 1 @nAnalysisWS = Obj FROM LgWritingSystem_Name WHERE Txt = N'English'
	IF @nVernWs IS NULL OR @nAnalysisWS IS NULL
		EXEC tsu_failure 'ut_Lex_Setup: "Kalaba" or "English" writing system not found. The database has changed.'

	SELECT TOP 1 @nAnalysisWS = Obj FROM LgWritingSystem_Name WHERE Txt = N'Spanishx'
	IF @nAnalysisWS IS NULL
		EXEC tsu_failure 'ut_Lex_Setup: "Spanish" writing system not found. The database has changed.'
END
GO

-------------------------------------------------------------------------------

IF OBJECT_ID('ut_Lex_fnGetEntryAltForms') IS NOT NULL BEGIN
	DROP PROCEDURE [ut_Lex_fnGetEntryAltForms]
END
GO
CREATE PROCEDURE ut_Lex_fnGetEntryAltForms AS
BEGIN
	DECLARE
		@nVernWS INT,
		@nEntryId INT,
		@nvcTxt NVARCHAR(4000)

	SELECT TOP 1 @nVernWS = Obj FROM LgWritingSystem_Name WHERE Txt = N'Kalaba'

	--( intitial checks

	SELECT @nEntryId = lf.Src
	FROM LexEntry_LexemeForm lf
	JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWs
	JOIN LexEntry_AlternateForms af ON af.Src = lf.Src
	JOIN MoForm_Form mff_af ON mff_af.Obj = af.Dst AND mff_af.WS = @nVernWs
	WHERE mff_lf.Txt = N'la' AND mff_af.Txt = 'a'

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Alternate form "a" not found for lexeme form "la". The database has changed.'

	SELECT @nEntryId = lf.Src
	FROM LexEntry_LexemeForm lf
	JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWs
	JOIN LexEntry_AlternateForms af ON af.Src = lf.Src
	JOIN MoForm_Form mff_af ON mff_af.Obj = af.Dst AND mff_af.WS = @nVernWs
	WHERE mff_lf.Txt = N'la' AND mff_af.Txt = 'zi'

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Alternate form "zi" not found for lexeme form "la". The database has changed.'

	--( run the function
	SELECT @nvcTxt = AltForm FROM dbo.fnGetEntryAltForms(@nEntryId, @nVernWS)

	--( check results
	IF @nvcTxt != N'a; zi'
		EXEC tsu_failure 'The alternate forms for lexeme form "la" should be "a; zi"'
END
GO

-------------------------------------------------------------------------------

IF OBJECT_ID('ut_Lex_fnGetEntryGlosses') IS NOT NULL BEGIN
	DROP PROCEDURE [ut_Lex_fnGetEntryGlosses]
END
GO
CREATE PROCEDURE ut_Lex_fnGetEntryGlosses AS
BEGIN
	DECLARE
		@nVernWS INT,
		@nAnalysisWS INT,
		@nEntryId INT,
		@nvcTxt NVARCHAR(4000)

	SELECT TOP 1 @nVernWS = Obj FROM LgWritingSystem_Name WHERE Txt = N'Kalaba'
	SELECT TOP 1 @nAnalysisWS = Obj FROM LgWritingSystem_Name WHERE Txt = N'English'

	--( intitial checks

	SELECT @nEntryId = lf.Src
	FROM CmObject o
	JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.Ws = @nAnalysisWS
	JOIN LexEntry_LexemeForm lf ON lf.Src = o.Owner$
	JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWs
	WHERE mff_lf.Txt = N'bili' AND lsg.Txt = N'to.see'

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Gloss "to.see" not found for lexeme form "bili". The database has changed.'

	SELECT @nEntryId = lf.Src
	FROM CmObject o
	JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.Ws = @nAnalysisWS
	JOIN LexEntry_LexemeForm lf ON lf.Src = o.Owner$
	JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWs
	WHERE mff_lf.Txt = N'bili' AND lsg.Txt = N'to.understand'

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Gloss "to.see" not found for lexeme form "understand". The database has changed.'

	--( run the function
	SELECT @nvcTxt = Gloss FROM fnGetEntryGlosses(@nEntryId, @nAnalysisWS)

	--( check results
	IF @nvcTxt != N'to.see; to.understand'
		EXEC tsu_failure 'The gloss for lexeme form "bili" should be "to.see; to.understand"'

END
GO

-------------------------------------------------------------------------------

IF OBJECT_ID('ut_Lex_MatchEntries') IS NOT NULL BEGIN
	DROP PROCEDURE [ut_Lex_MatchEntries]
END
GO
CREATE PROCEDURE ut_Lex_MatchEntries AS
BEGIN

	DECLARE
		@nVernWS INT,
		@nAnalysisWS INT,
		@nSpanishWS INT,
		@nSpanishOrd INT,
		@nEntryId INT,
		@nvcTxt NVARCHAR(4000),
		@nvcTxt2 NVARCHAR(4000),
		@maxSize INT

	SELECT TOP 1 @nVernWS = Obj FROM LgWritingSystem_Name WHERE Txt = N'Kalaba'
	SELECT TOP 1 @nAnalysisWS = Obj FROM LgWritingSystem_Name WHERE Txt = N'English'
	SELECT TOP 1 @nSpanishWS = Obj FROM LgWritingSystem_Name WHERE Txt = N'Spanish'
	SELECT @nSpanishOrd = MAX(Ord) + 1 FROM LangProject_CurAnalysisWss

	--==( intitial checks )==--

	SELECT @nEntryId = lf.Src
	FROM CmObject o
	JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.Ws = @nAnalysisWS
	JOIN LexEntry_LexemeForm lf ON lf.Src = o.Owner$
	JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWs
	WHERE mff_lf.Txt = N'bako' AND lsg.Txt = N'to.sell'

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Gloss "to.sell" not found for lexeme form "bako". The database has changed.'

	SELECT @nEntryId = lf.Src
	FROM CmObject o
	JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.Ws = @nAnalysisWS
	JOIN LexEntry_LexemeForm lf ON lf.Src = o.Owner$
	JOIN LexEntry_CitationForm cf ON cf.Obj = lf.Src AND cf.WS = @nVernWs
	JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWs
	WHERE mff_lf.Txt = N'bako' AND cf.Txt = N'himbakosa'

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Alternate form "himbakosa" not found for lexeme form "bako". The database has changed.'

	SELECT @nEntryId = lf.Src
	FROM CmObject o
	JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.Ws = @nAnalysisWS
	JOIN LexEntry_LexemeForm lf ON lf.Src = o.Owner$
	JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWs
	WHERE mff_lf.Txt = N'bi' AND lsg.Txt = N'Past'

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Gloss "Past" not found for lexeme form "bi". The database has changed.'

	--( Checks for alternate gloss

	SELECT TOP 1 @nEntryId = lf.Src
	FROM CmObject o
	JOIN LexEntry_LexemeForm lf ON lf.Src = o.Owner$
	JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWs
	WHERE mff_lf.Txt = N'gabi'

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Lexeme form "gabi" not found. The database has changed.'

	SELECT TOP 1 @nEntryId = lf.Src
	FROM CmObject o
	JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.Ws = @nAnalysisWS
	JOIN LexEntry_LexemeForm lf ON lf.Src = o.Owner$
	JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWs
	WHERE mff_lf.Txt = N'gabi'

	IF @@ROWCOUNT != 0
		EXEC tsu_failure 'Someone added an English gloss for lexeme form "gabi". The database has changed.'

	SELECT TOP 1 @nEntryId = lf.Src
	FROM CmObject o
	JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.Ws = @nSpanishWS
	JOIN LexEntry_LexemeForm lf ON lf.Src = o.Owner$
	JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWs
	WHERE mff_lf.Txt = N'gabi' AND lsg.Txt = N'pegar'

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Someone removed Spanish gloss "pegar" for lexeme form "gabi". The database has changed.'

	--( Checks for subsenses

	SELECT @nEntryId = lf.Src
	FROM CmObject o
	JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.Ws = @nAnalysisWS
	JOIN LexEntry_LexemeForm lf ON lf.Src = o.Owner$
	JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWs
	JOIN CmObject o2 ON o2.Owner$ = lsg.Obj
	JOIN LexSense_Gloss lsg2 ON lsg2.Obj = o2.Id AND lsg2.Txt = N'English subsense gloss1.1'
	WHERE mff_lf.Txt = N'underlying form' AND lsg.Txt = N'English gloss'

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Subsense has changed for "underlying form".'

	--==( Run )==--

	--( Currently the first parameter, @nExactMatches, is not supported, as
	--( nothing uses it.

	--( run the function for a normal search
	SET @nvcTxt = ''
	SELECT @nvcTxt = Gloss FROM fnMatchEntries(0, N'bako', N'bako', N'bako', N'!', @nVernWs, @nAnalysisWS, @maxSize)
	IF @nvcTxt != N'to.sell'
		EXEC tsu_failure 'The gloss for lexeme form "bako" should be "to.sell"'

	--( run for no lexeme search
	SET @nvcTxt = ''
	SELECT @nvcTxt = Gloss FROM fnMatchEntries(0, N'!', N'!', N'bako', N'!', @nVernWs, @nAnalysisWS, @maxSize)
	IF @@ROWCOUNT != 0
		EXEC tsu_failure 'Should not be searching for "bako" on lexical form'

	--( run for no lexeme search, searching on alternate form instead
	SET @nvcTxt = ''
	SELECT @nvcTxt = LexicalForm FROM fnMatchEntries(0, N'!', N'!', N'ko', N'!', @nVernWs, @nAnalysisWS, @maxSize)
	IF @nvcTxt != N'bi'
		EXEC tsu_failure 'The lexeme form "bi" could not be found for alternate form "ko"'

	--( run for gloss
	SET @nvcTxt = ''
	SELECT @nvcTxt = LexicalForm, @nvcTxt2 = AlternateForm
		 FROM fnMatchEntries(0, N'!', N'!', N'!', N'Past', @nVernWs, @nAnalysisWS, @maxSize)
	IF @nvcTxt != N'bi'
		EXEC tsu_failure 'Lexeme form "bi" could not be found for gloss "Past"'
	IF @nvcTxt2 != N'ko'
		EXEC tsu_failure 'Alternate form "ko" could not be found for gloss "Past"'

	--( Run for both lexeme form and gloss

	SET @nvcTxt = ''
	SELECT @nvcTxt = LexicalForm
		FROM fnMatchEntries(0, N'bako', N'bako', N'bako', N'p', @nVernWs, @nAnalysisWS, @maxSize)
		WHERE LexicalForm = 'bako'

	IF @nvcTxt != N'bako' --( should show up even though gloss for "bako" is "to.sell"
		EXEC tsu_failure 'Lexeme form "bako" could not be found when gloss starts with "p"'

	SET @nvcTxt = ''
	SELECT @nvcTxt = LexicalForm --( should show up even with no r forms found
		FROM fnMatchEntries(0, N'r', N'r', N'r', N'Past', @nVernWs, @nAnalysisWS, @maxSize)
		WHERE Gloss = 'Past'

	IF @nvcTxt != N'bi'
		EXEC tsu_failure 'Lexeme form "bi" could not be found when gloss is "Past"'

	--( Check gloss when primary venacular doesn't have a gloss, and a secondary (or more) does.

	INSERT INTO LangProject_CurAnalysisWss (Src, Dst, Ord)
		VALUES (1, @nSpanishWS, @nSpanishOrd) --( This makes Spanish a current vernacular

	SET @nvcTxt = ''
	SELECT @nvcTxt = Gloss
		FROM fnMatchEntries(0, N'g', N'g', N'g', N'!', @nVernWs, @nAnalysisWS, @maxSize)
		WHERE LexicalForm = N'gabi'

	IF @nvcTxt != N'pegar' BEGIN
		EXEC tsu_failure 'Gloss "pegar" could not be found for lexeme form "gabi".'
	END

	DELETE FROM LangProject_CurAnalysisWss WHERE Dst = @nSpanishWS

	--( Check for data overflow

	UPDATE lexsense_gloss
	SET Txt = N'11111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111'
	FROM CmObject o
	JOIN LexSense_Gloss lsg ON lsg.Obj = o.Id AND lsg.Ws = @nAnalysisWS
	JOIN LexEntry_LexemeForm lf ON lf.Src = o.Owner$
	JOIN MoForm_Form mff_lf ON mff_lf.Obj = lf.Dst AND mff_lf.WS = @nVernWs
	WHERE mff_lf.Txt = N'bako' AND lsg.Txt = N'to.sell'

	SET @nvcTxt = ''
	SELECT @nvcTxt = Gloss FROM fnMatchEntries(0, N'1', N'1', N'1', N'1', @nVernWs, @nAnalysisWS, @maxSize)
	--( If this test fails, you should see an error message, "String or binary data would be truncated."

	--( Check exact match

	SET @nvcTxt = ''
	SELECT @nvcTxt = Gloss
		FROM fnMatchEntries(1, N'bi', N'bi', N'bi', N'!', @nVernWs, @nAnalysisWS, @maxSize)

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'An exact search on "bi" should return only one row.'

	--( run to try to match on the gloss of a subsense and make sure we only return entries. (LT-6512)
	SELECT @nEntryId = EntryId
	FROM fnMatchEntries(0, N'new', N'!', N'!', N'English', @nVernWs, @nAnalysisWS, @maxSize) AS entries
	LEFT OUTER JOIN LexEntry le ON le.ID = entries.EntryId
	WHERE le.Id IS NULL

	IF @@ROWCOUNT != 0
		EXEC tsu_failure 'We should only return LexEntries from fnMatchEntries.'

END
GO

-------------------------------------------------------------------------------

IF OBJECT_ID('ut_Lex_ConcordForMorphemes') IS NOT NULL BEGIN
	DROP PROCEDURE ut_Lex_ConcordForMorphemes
END
GO
CREATE PROCEDURE ut_Lex_ConcordForMorphemes AS
BEGIN
	DECLARE
		@nWs INT,
		@nvcText NVARCHAR(4000),
		@nAnnotationId INT,
		@nBeginObject INT

	DECLARE @tblConcordForMorphemes TABLE (
		BeginObject INT,
		AnnotationId INT,
		Ord INT,
		Txt NVARCHAR(4000))

	SELECT @nWs = Id FROM LgWritingSystem WHERE IcuLocale = 'xkal'

	SELECT TOP 1 @nvcText = Txt
	FROM MoForm_Form
	WHERE Txt = 'la' AND Ws = @nWs

	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Could not find "yalo". The database has changed. Rowcount: '

	SELECT TOP 1 @nAnnotationId = ba.Id, @nBeginObject = BeginObject
	FROM moForm_Form mff
	JOIN WfiMorphBundle wmb ON wmb.Morph = mff.Obj
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Dst = wmb.Id
	JOIN WfiAnalysis wa ON wa.Id = wamb.Src
	JOIN CmBaseAnnotation_ ba ON ba.InstanceOf = wa.Id
	WHERE mff.Txt LIKE @nvcText AND mff.Ws = @nWs

	--( For some stupid reason I can't make @@ROWCOUNT work here. ;-/
	IF @nAnnotationId IS NULL OR @nBeginObject IS NULL
		EXEC tsu_failure 'Could not find annotations for "yalo". The database has changed.'

	--( The owning flid is Text.Contents
	INSERT INTO @tblConcordForMorphemes
	SELECT f.*
	FROM dbo.fnConcordForMorphemes(5054008, @nvcText, @nWs, '') f

	-- REVIEW (SteveMiller): This is not a very satisfactory test. I wish I could figure
	-- a way to get the result set down to one record instead of three. There's something
	-- here I'm still not understanding. This is checked in based on getting the same
	-- records as the original code did out of Sena 3_Interlinearized.
	--
	-- ConcordanceControlTests.cs has a number of tests not only for this, but for
	-- concord for lex gloss and lexemes.

	IF @@ROWCOUNT = 0
		EXEC tsu_failure 'fnConcordForMorphemes failed to get any records.'

END
GO

-------------------------------------------------------------------------------

-- REVIEW (SteveMiller): ConcordanceControlTests.cs has a number of tests for
-- concord for lex entries (fnConcordForLexGloss).

-------------------------------------------------------------------------------

-- REVIEW (SteveMiller): ConcordanceControlTests.cs has a number of tests for
-- concord for lex entries (fnConcordForLexGloss).

-------------------------------------------------------------------------------

IF OBJECT_ID('ut_Annot_GetTextAnnotations') IS NOT NULL BEGIN
	DROP PROCEDURE ut_Annot_GetTextAnnotations
END
GO
CREATE PROCEDURE ut_Annot_GetTextAnnotations AS
BEGIN
	DECLARE
		@nTextId INT,
		@nvcWordForm NVARCHAR(4000),
		@nvcGloss NVARCHAR(4000)

	SELECT @nTextId = Obj FROM CmMajorObject_Name WHERE LOWER(Txt) LIKE'%green%'
	IF @@ROWCOUNT != 1
		EXEC tsu_failure 'Could not find text "My green mat". The database has changed.'

	SELECT TOP 1 @nvcWordForm = WordForm, @nvcGloss = Gloss
	FROM fnGetTextAnnotations(N'My Green Mat', NULL, NULL)

	IF @nvcWordForm != N'pus'
		EXEC tsu_failure 'The first row does not have a word form of "pus".'
	IF @nvcGloss != N'green'
		EXEC tsu_failure 'The first row does not have a gloss of "green".'

	SELECT @nvcWordForm = WordForm
	FROM fnGetTextAnnotations(N'My Green Mat', NULL, NULL)
	WHERE BeginOffset = 22

	IF @nvcWordForm != N'.'
		EXEC tsu_failure 'The first sentence should end in a period.'
END
GO

-------------------------------------------------------------------------------
