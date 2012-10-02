/********************************************************************
 * Function: fnGetDefaultAnalysesGlosses
 *
 * Description:
 *	Returns information about word and punctuation-level annotations
 *  contained in an StTxtPara object. Built for use in the Interlinear
 *  Text Tool.
 *
 * Parameters:
 *	@nStTxtParaId INT = object ID for the structured paragraph of
 *						interest.
 * @nAnnotType, @nAnnotPunct = object IDs for the CmAnnotationDefns
 * for Twfics and punct-in-context objects.
 *
 * Sample Call (with currently correct IDs for TestLangProj and the
 * one currently interesting interlinear paragraph):
 *
 *	select * from dbo.fnGetDefaultAnalysesGlosses(184, 9721, 9724)
 *   order by BeginOffset
 *
 *
 * Notes:
 *  the order of the columns is somewhat confusing. It is easiest to
 *  understand by considering first the BaseAnnotationId, InstanceOf,
 *	BeginOffset, and EndOffset columns. These return the ID and
 *	the other corresponding fields of the CmBaseAnnotations
 *  that point at the target paragraph (in their BeginObject field),
 *  and whose annotation type is @nAnnotType or @nAnnotPunct--in other words,
 *	the query has one row for each word-in-context and punctuation annotation
 *  that exist for the paragraph.
 *
 *	The first three columns give the actual default information,
 *	and the last an indication of whether some human agent has
 *	somehow validated the proposed default.
 *	There are distinct cases, depending on the type of the InstanceOf column
 *	and what defaults are available.
 *
 *	1. InstanceOf is a WfiGloss. (word is fully analyzed)
 *		GlossId = InstanceOf;
 *		AnalysisId is owner of GlossId;
 *		WordformId is owner of AnalysisId.
 *		UserApproved is true. (assumed because no other agent produces WfiGlosses)
 *	2. InstanceOf is a WfiAnalysis. (word is partly analyzed)
 *		AnalysisId = InstanceOf
 *		WordformId is owner of AnalysisId.
 *		UserApproved is true. (assumed since this is a confirmed analysis)
 *		2.1 InstanceOf WfiAnalaysis owns at least one WfiGloss
 *			GlossId is the id of one of those WfiGlosses. If there is a choice
 *			it is the one most commonly used (elsewhere in interlinear texts).
 *		2.2 InstanceOf WfiAnalyis owns no WfiGlosses
 *			GlossId is null
 *	3. InstanceOf is a WfiWordform (word is not analyzed at all)
 *		WordformId = InstanceOf.
 *		3.1 Wordform owns (at one remove) at least one WfiGloss
 *			GlossId is the id of one of those WfiGlosses. If there is a choice
 *			it is the one most commonly used (elsewhere in interlinear texts).
 *			AnalysisId is the owner of GlossId.
 *			UserApproved is 1 (any WfiGloss is assumed user-approved).
 *		3.1 Wordform owns at least one WfiAnalysis that has no negative
 *		human evaluations, but owns no WfiGlosses:
 *			GlossId is null.
 *			AnalysisId is the id of one of the owned WfiAnalyses. If there is a choice
 *			it is the one most commonly used (elsewhere in interlinear texts).
 *			UserApproved is 1 if there is a positive human evaluation of the analysis,
 *			otherwise zero.
 *		3.2 Wordform owns no WfiAnalyses (except possibly ones with a negative
 *		human evaluation).
 *			GlossId is null
 *			AnalysisId is null.
 *			UserApproved is 0 (rather arbitrarily - there's no analysis proposed at all).
 *
 * Another way to look at it is that for each annotation, the program is going to show information
 * about a wordform, possibly one of its analyses, and possibly one of the glosses
 * of that analysis. Some of that information may involve guesswork. What is known
 * definitely is indicated by InstanceOf, the choice currently stored in the annotation. If some of
 * the information is guesswork, UserApproved indicates whether a user has
 * validated this as a plausible guess for this wordform.
 *
 * In addition to the Twfic analyses (identified by having an InstanceOf
 * that is a WfiWordform, WfiAnalysis, or WfiInterpretation), it
 * returns punctuation annotations for the paragraph. For these,
 * the InstanceOf, gloss, annotation, and wordform columns are null.
 *
 *******************************************************************/

IF OBJECT_ID('fnGetDefaultAnalysesGlosses') IS NOT NULL BEGIN
	PRINT 'removing procedure fnGetDefaultAnalysesGlosses'
	DROP FUNCTION fnGetDefaultAnalysesGlosses
END
GO
PRINT 'creating function fnGetDefaultAnalysesGlosses'
GO

CREATE FUNCTION fnGetDefaultAnalysesGlosses (
	@nStTxtParaId INT, @nAnnotType INT, @nAnnotPunct INT)
RETURNS @tblDefaultAnalysesGlosses TABLE (
	WordformId INT,
	AnalysisId INT,
	GlossId INT,
	BaseAnnotationId INT,
	InstanceOf INT,
	BeginOffset INT,
	EndOffset INT,
	UserApproved INT)
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
		ba.EndOffset,
		CASE WHEN (wai.id is not null or wawg.id is not null) then 1 else 0 end -- default is to assume not user-approved.
	FROM CmBaseAnnotation ba
	JOIN CmAnnotation a ON a.[Id] = ba.[Id]
		AND a.AnnotationType = @nAnnotType
	-- these joins handle the case that instanceof is a WfiAnalysis; all values will be null otherwise
	LEFT OUTER JOIN WfiAnalysis wai ON wai.id = a.InstanceOf -- 'real' analysis (is the instanceOf)
	LEFT OUTER JOIN CmObject waio on waio.id = wai.id -- CmObject of analysis instanceof
	LEFT OUTER JOIN CmObject wfwa on wfwa.id = waio.owner$ -- wf that owns wai
	-- these joins handle the case that instanceof is a WfiGloss; all values will be null otherwise.
	LEFT OUTER JOIN WfiGloss wgi on wgi.id = a.instanceOf -- 'real' gloss (is the instanceof)
	LEFT OUTER JOIN CmObject wgio on wgio.id = wgi.id
	LEFT OUTER JOIN CmObject wawg on wawg.id = wgio.owner$ -- ananlyis that owns wgi
	LEFT OUTER JOIN CmObject wfwg on wfwg.id = wawg.owner$ -- wordform that owns wgi (indirectly)
	WHERE ba.BeginObject = @nStTxtParaId

	-- InstanceOf is a WfiAnalysis; we fill out a default gloss if possible.
	-- If we find a WfiGloss we assume the user approves of the owning analysis. Leave UserApproved 1.

	UPDATE @tblDefaultAnalysesGlosses SET GlossId = WgId, UserApproved = 1
	FROM @tblDefaultAnalysesGlosses t
	JOIN (
		SELECT Sub2.WaId, Sub2.WgId, MAX(Sub2.CountInstance) AS MaxCountInstance
		FROM (
			SELECT t.InstanceOf AS WaId, wg.[Id] AS WgId, COUNT(ann.[Id]) AS CountInstance
			FROM @tblDefaultAnalysesGlosses t
			JOIN WfiGloss_ wg ON wg.Owner$ = t.InstanceOf
			LEFT OUTER JOIN CmAnnotation ann ON ann.InstanceOf = wg.[Id]
			GROUP BY t.InstanceOf, wg.[Id]
			) Sub2
		GROUP BY Sub2.WaId, Sub2.WgId
		) Sub1 ON Sub1.WaId = t.InstanceOf
	WHERE t.GlossId IS NULL

	-- InstanceOf is a WfiWordform. Find best WfiGloss owned by each such WfiWordform.
	-- If we find one assume owning analysis is user-approved.

	UPDATE @tblDefaultAnalysesGlosses SET GlossId = WgId, AnalysisId = WaId, UserApproved = 1
	FROM @tblDefaultAnalysesGlosses t
	JOIN (
		SELECT Sub2.WfId, Sub2.WaId, Sub2.WgId,
			MAX(Sub2.CountInstance) AS MaxCountInstance
		FROM (
			SELECT t.WordformId AS WfId, wa.[Id] AS WaId, wg.[Id] AS WgId,
				COUNT(ann.[Id]) AS CountInstance
			FROM @tblDefaultAnalysesGlosses t
			JOIN WfiAnalysis_ wa ON wa.Owner$ = t.WordformId
			JOIN WfiGloss_ wg ON wg.Owner$ = wa.[Id]
			LEFT OUTER JOIN CmAnnotation ann ON ann.InstanceOf = wg.[Id]
			GROUP BY t.WordformId, wa.[Id], wg.[Id]
			) Sub2
		GROUP BY Sub2.WfId, Sub2.WaId, Sub2.WgId
		) Sub1 ON Sub1.WfId = t.WordformId
	WHERE t.AnalysisId IS NULL

	-- Final option is InstanceOf is WfiWordform, there are analyses but no glosses
	-- Here we have to look to see whether the user approves the analysis.
	-- If the user specifically disapproves, don't even consider it (see final where clause)
	-- If the user approves set UserApproved.

	UPDATE @tblDefaultAnalysesGlosses SET AnalysisId = WaId, UserApproved = coalesce(HumanAccepted, 0)
	FROM @tblDefaultAnalysesGlosses t
	JOIN (
		SELECT Sub2.WfId, Sub2.WaId, MAX(Sub2.CountInstance) AS MaxCountInstance --, max(ag) as ag
		FROM (
			SELECT
				t.WordformId AS WfId,
				wa.[Id] AS WaId,
				COUNT(ann.[Id]) AS CountInstance --,
				--count(ag.id) as ag
			FROM @tblDefaultAnalysesGlosses t
			JOIN WfiAnalysis_ wa ON wa.Owner$ = t.WordformId
			LEFT OUTER JOIN CmAnnotation ann ON ann.InstanceOf = wa.[Id]
			/*
			WHERE NOT EXISTS (
				SELECT *
				FROM CmAgentEvaluation_ ae
				JOIN CmAgent a ON a.Id = ae.Owner$ AND Human = 1
				WHERE ae.Target = wa.Id AND ae.Accepted = 0)
			*/
			LEFT OUTER JOIN (
				SELECT ae.Target
				FROM CmAgentEvaluation_ ae
				JOIN CmAgent a ON a.Id = ae.Owner$ AND Human = 1
				WHERE ae.Accepted = 0)
					aae ON aae.Target = wa.Id
			WHERE aae.Target IS NULL
			GROUP BY t.WordformId, wa.[Id]
			) Sub2
		GROUP BY Sub2.WfId, Sub2.WaId
		) Sub1 ON Sub1.WfId = t.WordformId
	LEFT OUTER JOIN (
		SELECT ae.Target, ae.Accepted AS HumanAccepted
		FROM CmAgentEvaluation_ ae
		JOIN CmAgent a ON a.Id = ae.Owner$ AND Human = 1
		) aea ON aea.Target = waId
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
		ba.EndOffset,
		1 -- arbitrary
	FROM CmBaseAnnotation ba
	JOIN CmAnnotation a ON a.[Id] = ba.[Id]
		AND a.AnnotationType = @nAnnotPunct
	WHERE ba.BeginObject = @nStTxtParaId

	RETURN
END
GO
