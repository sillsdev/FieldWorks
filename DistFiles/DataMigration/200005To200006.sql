-- update database from version 200005 to 200006
BEGIN TRANSACTION

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(17011, 2, 17,
		null, 'Context',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(17012, 2, 17,
		null, 'Structure',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(17013, 2, 17,
		null, 'Function',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(17014, 16, 17,
		null, 'Usage',0,Null, null, null, null)
exec UpdateClassView$ 17, 1

declare @ws int, @date datetime, @owner int, @newid int, @guid uniqueidentifier
select @owner = id from CmObject where Guid$ = '8D4CBD80-0DCA-4A83-8A1F-9DB3AA4CFF54'
set @date = Getdate()
select @ws = id from LgWritingSystem where IcuLocale = 'en'
exec CreateObject_CmAnnotationDefn @ws, 'Wordform In Context', @ws, 'wfic', @ws, 'Used in interlinearized text for each wordform.', '', 0,
  @date, @date, null, -1073741824, -1073741824, -1073741824, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, @owner, 7004, null, @newid output, @guid output
update CmObject set Guid$ = 'EB92E50F-BA96-4D1D-B632-057B5C274132' where id = @newid
exec CreateObject_CmAnnotationDefn @ws, 'Punctuation In Context', @ws, 'pic', @ws, 'Used in interlinearized text for each non-wordforming punctuation.', '', 0,
  @date, @date, null, -1073741824, -1073741824, -1073741824, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, @owner, 7004, null, @newid output, @guid output
update CmObject set Guid$ = 'CFECB1FE-037A-452D-A35B-59E06D15F4DF' where id = @newid
go
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
 *
 * Sample Call (with a StTxtPara.Id):
 *
 *	SELECT *
 *	FROM dbo.fnGetDefaultAnalysesGlosses(192)
 *	ORDER BY BeginOffset
 *
 * Notes:
 *	To see a ranking of all analyses and glosses with annotations
 *	attached:
 *
 *	SELECT *
 *	FROM dbo.fnGetDefaultAnalysisGloss(2740)
 *	ORDER BY Score DESC
 *
 *	AnalysisId  GlossId     Score
 *	----------- ----------- -----------
 *	2742        2743        10002
 *	2742        2744        10001
 *
 *	To get the gloss and analysis with the highest score:
 *
 *	SELECT TOP 1 *
 *	FROM dbo.fnGetDefaultAnalysisGloss(2740)
 *	ORDER BY Score DESC
 *
 * In addition to the Twfic analyses (identified by having an InstanceOf
 * that is a WfiWordform, WfiAnalysis, or WfiInterpretation), it
 * returns punctuation annotations for the paragraph. For these,
 * the InstanceOf, gloss, annotation, and wordform columns are null.
 *
 *******************************************************************/

IF OBJECT_ID('fnGetDefaultAnalysesGlosses') IS NOT NULL BEGIN
	if (select DbVer from Version$) = 200005
		PRINT 'removing function fnGetDefaultAnalysesGlosses'
	DROP FUNCTION fnGetDefaultAnalysesGlosses
END
GO
if (select DbVer from Version$) = 200005
	PRINT 'creating function fnGetDefaultAnalysesGlosses'
GO

CREATE FUNCTION fnGetDefaultAnalysesGlosses (
	@nStTxtParaId INT)
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

	--==( Get Most Data )==--

	--( This query is a union of three queries:
	--(
	--( 	1. WfiGloss owned by WfiAnalysis, owned by WfiWordform
	--(		2. WfiAnalysis owned by wfiWordform, and default gloss
	--(		3. WfiWordform default analysis and gloss.
	--(
	--( A couple of these pieces can't be picked up in one query, and
	--( will be retrieved next.

	--( wfiGloss is an InstanceOf
	INSERT INTO @tblDefaultAnalysesGlosses
	SELECT
		wf.[Id] AS WordformId,
		wa.[Id] AS AnalysisId,
		wg.[Id] AS GlossId,
		ba.[Id] AS BaseAnnotationId,
		a.InstanceOf,
		ba.BeginOffset,
		ba.EndOffset
	FROM CmBaseAnnotation ba
	JOIN CmAnnotation a ON a.[Id] = ba.[Id]
	JOIN StTxtPara tp ON tp.[Id] = ba.BeginObject
	JOIN WfiGloss wg ON wg.[Id] = a.InstanceOf
	JOIN CmObject wgobj ON wgobj.[Id] = wg.[Id]
	JOIN WfiAnalysis wa ON wa.[Id] = wgobj.Owner$
	JOIN CmObject waobj ON waobj.[Id] = wa.[Id]
	JOIN WfiWordform wf ON wf.[Id] = waobj.Owner$
	WHERE ba.BeginObject = @nStTxtParaId
	UNION ALL
	--( wfiAnalysis is an InstanceOf
	SELECT
		wf.[Id] AS WordformId,
		wa.[Id] AS AnalysisId,
		NULL AS GlossId,
		ba.[Id] AS BaseAnnotationId,
		a.InstanceOf,
		ba.BeginOffset,
		ba.EndOffset
	FROM CmBaseAnnotation ba
	JOIN CmAnnotation a ON a.[Id] = ba.[Id]
	JOIN StTxtPara tp ON tp.[Id] = ba.BeginObject
	JOIN WfiAnalysis wa ON wa.[Id] = a.InstanceOf
	JOIN CmObject waobj ON waobj.[Id] = wa.[Id]
	JOIN WfiWordform wf ON wf.[Id] = waobj.Owner$
	WHERE ba.BeginObject = @nStTxtParaId
	UNION ALL
	--( wfiWordform is an InstanceOf
	SELECT
		wf.[Id] AS WordformId,
		NULL AS AnalysisId,
		NULL AS GlossId,
		ba.[Id] AS BaseAnnotationId,
		a.InstanceOf,
		ba.BeginOffset,
		ba.EndOffset
	FROM CmBaseAnnotation ba
	JOIN CmAnnotation a ON a.[Id] = ba.[Id]
	JOIN StTxtPara tp ON tp.[Id] = ba.BeginObject
	JOIN WfiWordform wf ON wf.[Id] = a.InstanceOf
	WHERE ba.BeginObject = @nStTxtParaId

	--( Default analyses and glosses:
	--(
	--( This function is supposed to get a default analysis
	--( and default gloss for the current wordform only if
	--( the current analysis is incomplete. In other words,
	--( If the user has specified the gloss already, use
	--( that.

	--==( Get Default analysis and gloss for wordforms )==--

	SET @nWordformId = NULL

	SELECT TOP 1 @nWordformId = WordformId
	FROM @tblDefaultAnalysesGlosses
	WHERE GlossId IS NULL AND AnalysisId IS NULL
	ORDER BY WordformId

	WHILE @nWordformId IS NOT NULL BEGIN
		-- this query could return no rows, in which case, it doesn't modify
		-- the variables. If we get no rows we don't want to set a value.
		set @nAnalysisId = NULL
		set @nGlossId = NULL
		SELECT TOP 1 @nAnalysisId = AnalysisId, @nGlossId = GlossId
		FROM dbo.fnGetDefaultAnalysisGloss(@nWordformId)
		ORDER BY Score DESC

		UPDATE @tblDefaultAnalysesGlosses SET GlossId = @nGlossId, AnalysisId = @nAnalysisId
		WHERE WordformId = @nWordformId AND GlossId IS NULL AND AnalysisId IS NULL

		DECLARE @WfIdOld int
		SET @WfIdOld = @nWordformId
		SET @nWordformId = NULL

		SELECT TOP 1 @nWordformId = WordformId
		FROM @tblDefaultAnalysesGlosses
		WHERE GlossId IS NULL AND AnalysisId IS NULL
			AND WordformId > @WfIdOld
		ORDER BY WordformId
	END

	--==( Get Default gloss for analysis)==--

	SET @nWordformId = NULL

	SELECT TOP 1 @nWordformId = WordformId
	FROM @tblDefaultAnalysesGlosses
	WHERE GlossId IS NULL AND AnalysisId IS NOT NULL
	ORDER BY WordformId

	WHILE @nWordformId IS NOT NULL BEGIN
		set @nGlossId = NULL
		SELECT TOP 1 @nGlossId = GlossId
		FROM dbo.fnGetDefaultAnalysisGloss(@nWordformId)
		ORDER BY Score DESC

		UPDATE @tblDefaultAnalysesGlosses SET GlossId = @nGlossId
		WHERE WordformId = @nWordformId AND GlossId IS NULL
			AND AnalysisId IS NOT NULL

		SET @WfIdOld = @nWordformId
		SET @nWordformId = NULL


		SELECT TOP 1 @nWordformId = WordformId
		FROM @tblDefaultAnalysesGlosses
		WHERE GlossId IS NULL AND AnalysisId IS NOT NULL
			AND WordformId > @WfIdOld
		ORDER BY WordformId
	END

	-- Now add the punctuation annotations, identified by (a) pointing at this
	-- paragraph, and (b) having a type which is an annotation defn with the right GUID.
	INSERT INTO @tblDefaultAnalysesGlosses
	SELECT
		NULL AS WordformId,
		NULL AS AnalysisId,
		NULL AS GlossId,
		ba.[Id] AS BaseAnnotationId,
		NULL,
		ba.BeginOffset,
		ba.EndOffset
	FROM CmBaseAnnotation ba
	join CmAnnotation ca on ba.id = ca.id
	join CmObject andefn on ca.AnnotationType = andefn.id AND ba.BeginObject = @nStTxtParaId
		AND andefn.Guid$ = 'CFECB1FE-037A-452D-A35B-59E06D15F4DF'

	RETURN
END
GO


declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200005
begin
	update Version$ set DbVer = 200006
	COMMIT TRANSACTION
	print 'database updated to version 200006'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200005 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
