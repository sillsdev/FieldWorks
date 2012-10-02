-- update database from version 200033 to 200034
BEGIN TRANSACTION

--( Steve Miller, Dec 7, 2006: Besides fixing the code, removed readuncommitted
--( here and in DeleteObj$.

GO
-------------------------------------------------------------------------------
ALTER proc [dbo].[DeleteObj$]
	@objId int = null,
	@hXMLDocObjList int=null
as
	declare @Err int, @nRowCnt int, @nTrnCnt int
	declare	@sQry nvarchar(4000)
	declare	@nObjClass int, @nInheritDepth int, @nOwnerDepth int, @nOrdrAndType tinyint,
		@sDelClass nvarchar(100), @sDelField nvarchar(100)
	declare	@fIsNocountOn int

	DECLARE
		@nObj INT,
		@nvcTableName NVARCHAR(60),
		@nFlid INT


	set @Err = 0
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- create a temporary table to hold the object hierarchy
	create table [#ObjInfoTbl$]
	(
		[ObjId]			int	not null,
		[ObjClass]		int	null,
		[InheritDepth]	int	null			default(0),
		[OwnerDepth]	int	null			default(0),
		[RelObjId]		int	null,
		[RelObjClass]	int	null,
		[RelObjField]	int	null,
		[RelOrder]		int	null,
		[RelType]		int	null,
		[OrdKey]		varbinary(250) null	default(0)
	)
	create nonclustered index #ObjInfoTbl$_Ind_ObjId on [#ObjInfoTbl$] (ObjId)
	create nonclustered index #ObjInfoTbl$_Ind_ObjClass on [#ObjInfoTbl$] (ObjClass)

	-- determine if the procedure was called within a transaction; if yes then create a savepoint,
	--    otherwise create a transaction
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran DelObj$_Tran
	else save tran DelObj$_Tran
	set @Err = @@error
	if @Err <> 0 begin
		raiserror ('DeleteObj$: SQL Error %d; Unable to create a transaction.', 16, 1, @Err)
		goto LFail
	end

	-- make sure objects were specified either in the XML or through the @ObjId parameter
	if ( @ObjId is null and @hXMLDocObjList is null ) or ( @ObjId is not null and @hXMLDocObjList is not null ) goto LFail

	-- get the owned objects
	insert into #ObjInfoTbl$
	select	*
	from	dbo.fnGetOwnedObjects$(@ObjId, @hXMLDocObjList, null, 1, 1, 1, null, 0)

	-- REVIEW (SteveMiller): A number of these delete statements originally had the SERIALIZABLE
	-- keyword rather than the ROWLOCK keyword. This is entirely keeping with good database
	-- code. However, we currently (Dec 2004) have a long-running transaction running to
	-- support undo/redo. Tests indicate that using the SERIALIZABLE keyword holds a lock for
	-- the duration of the undo transaction, which bars any further inserts/updates from
	-- happening. Using ROWLOCK lowers isolation and increases concurrency, a calculated
	-- risk until such time as the undo system gets rebuilt.

	--
	-- remove strings associated with the objects that will be deleted
	--
	delete	MultiStr$ WITH (REPEATABLEREAD)
	from [#ObjInfoTbl$] oi
	join [MultiStr$] ms on oi.[ObjId] = ms.[obj]
	set @Err = @@error
	if @Err <> 0 begin
		raiserror ('DeleteObj$: SQL Error %d; Unable to remove strings from the MultiStr$ table.', 16, 1, @Err)
		goto LFail
	end

	--( This query finds the class of the object, and from there determines
	--( which multitxt fields need deleting. It gets the first property first.
	--( Any remaining multitxt properties are found in the loop.

	SELECT TOP 1
		@nObj = oi.ObjId,
		@nFlid = f.[Id],
		@nvcTableName = c.[Name] + '_' + f.[Name]
	FROM Field$ f
	JOIN Class$ c ON c.[Id] = f.Class
	JOIN #ObjInfoTbl$ oi ON oi.ObjClass = f.Class AND f.Type = 16
	ORDER BY f.[Id]

	SET @nRowCnt = @@ROWCOUNT
	WHILE @nRowCnt > 0 BEGIN
		SET @sQry =
			N'DELETE ' + @nvcTableName + N' WITH (REPEATABLEREAD) ' + CHAR(13) +
			CHAR(9) + N'FROM #ObjInfoTbl$ oi' + CHAR(13) +
			CHAR(9) + N'JOIN ' + @nvcTableName + ' x ON x.Obj = @nObj'

		EXECUTE sp_executesql @sQry, N'@nObj INT', @nObj

		set @Err = @@error
		if @Err <> 0 begin
			raiserror ('DeleteObj$: SQL Error %d; Unable to remove strings from the MultiTxt$ table', 16, 1, @Err)
			goto LFail
		end

		SELECT TOP 1
			@nObj = oi.ObjId,
			@nFlid = f.[Id],
			@nvcTableName = c.[Name] + '_' + f.[Name]
		FROM Field$ f
		JOIN Class$ c ON c.[Id] = f.Class
		JOIN #ObjInfoTbl$ oi ON oi.ObjClass = f.Class AND f.Type = 16
		WHERE f.[Id] > @nFlid
		ORDER BY f.[Id]

		SET @nRowCnt = @@ROWCOUNT
	END

	delete MultiBigStr$ with (REPEATABLEREAD)
	from [#ObjInfoTbl$] oi
	join [MultiBigStr$] ms on oi.[ObjId] = ms.[obj]
	set @Err = @@error
	if @Err <> 0 begin
		raiserror ('DeleteObj$: SQL Error %d; Unable to remove strings from the MultiBigStr$ table.', 16, 1, @Err)
		goto LFail
	end
	delete MultiBigTxt$ with (REPEATABLEREAD)
	from [#ObjInfoTbl$] oi
	 join [MultiBigTxt$] ms on oi.[ObjId] = ms.[obj]
	set @Err = @@error
	if @Err <> 0 begin
		raiserror ('DeleteObj$: SQL Error %d; Unable to remove strings from the MultiBigTxt$ table.', 16, 1, @Err)
		goto LFail
	end

	--
	-- loop through the objects and delete all owned objects and clean-up all relationships
	--
	declare Del_Cur cursor fast_forward local for
	-- get the classes that reference (atomic, sequences, and collections) one of the owned classes
	select	oi.[ObjClass],
		min(oi.InheritDepth) as InheritDepth,
		max(oi.OwnerDepth) as OwnerDepth,
		c.[Name] as DelClassName,
		f.[Name] as DelFieldName,
		case f.[Type]
			when 24 then 1	-- atomic reference
			when 26 then 2	-- reference collection
			when 28 then 3	-- reference sequence
		end as OrdrAndType
	from	#ObjInfoTbl$ oi
			join [Field$] f on (oi.[ObjClass] = f.[DstCls] or 0 = f.[DstCls]) and f.[Type] in (24, 26, 28)
			join [Class$] c on f.[Class] = c.[Id]
	group by oi.[ObjClass], c.[Name], f.[Name], f.[Type]
	union all
	-- get the classes that are referenced by the owning classes
	select	oi.[ObjClass],
		min(oi.[InheritDepth]) as InheritDepth,
		max(oi.[OwnerDepth]) as OwnerDepth,
		c.[Name] as DelClassName,
		f.[Name] as DelFieldName,
		case f.[Type]
			when 26 then 4	-- reference collection
			when 28 then 5	-- reference sequence
		end as OrdrAndType
	from	[#ObjInfoTbl$] oi
			join [Class$] c on c.[Id] = oi.[ObjClass]
			join [Field$] f on f.[Class] = c.[Id] and f.[Type] in (26, 28)
	group by oi.[ObjClass], c.[Name], f.[Name], f.[Type]
	union all
	-- get the owned classes
	select	oi.[ObjClass],
		min(oi.[InheritDepth]) as InheritDepth,
		max(oi.[OwnerDepth]) as OwnerDepth,
		c.[Name] as DelClassName,
		NULL,
		6 as OrdrAndType
	from	#ObjInfoTbl$ oi
			join Class$ c on oi.ObjClass = c.Id
	group by oi.[ObjClass], c.Name
	order by OrdrAndType, InheritDepth asc, OwnerDepth desc, DelClassName

	open Del_Cur
	fetch Del_Cur into @nObjClass, @nInheritDepth, @nOwnerDepth, @sDelClass, @sDelField, @nOrdrAndType

	while @@fetch_status = 0 begin

		-- classes that contain refence pointers to this class
		if @nOrdrAndType = 1 begin
			set @sQry='update ['+@sDelClass+'] with (REPEATABLEREAD) set ['+@sDelField+']=NULL '+
				'from ['+@sDelClass+'] r '+
					'join [#ObjInfoTbl$] oi on r.['+@sDelField+'] = oi.[ObjId] '
		end
		-- classes that contain sequence or collection references to this class
		else if @nOrdrAndType = 2 or @nOrdrAndType = 3 begin
			set @sQry='delete ['+@sDelClass+'_'+@sDelField+'] with (REPEATABLEREAD) '+
				'from ['+@sDelClass+'_'+@sDelField+'] c '+
					'join [#ObjInfoTbl$] oi on c.[Dst] = oi.[ObjId] '
		end
		-- classes that are referenced by this class's collection or sequence references
		else if @nOrdrAndType = 4 or @nOrdrAndType = 5 begin
			set @sQry='delete ['+@sDelClass+'_'+@sDelField+'] with (REPEATABLEREAD) '+
				'from ['+@sDelClass+'_'+@sDelField+'] c '+
					'join [#ObjInfoTbl$] oi on c.[Src] = oi.[ObjId] '
		end
		-- remove class data
		else if @nOrdrAndType = 6 begin
			set @sQry='delete ['+@sDelClass+'] with (REPEATABLEREAD) '+
				'from ['+@sDelClass+'] o '+
					'join [#ObjInfoTbl$] oi on o.[id] = oi.[ObjId] '
		end

		set @sQry = @sQry +
				'where oi.[ObjClass]='+convert(nvarchar(11),@nObjClass)
		exec(@sQry)
		select @Err = @@error, @nRowCnt = @@rowcount

		if @Err <> 0 begin
			raiserror ('DeleteObj$: SQL Error %d; Unable to execute dynamic SQL.', 16, 1, @Err)
			goto LFail
		end

		fetch Del_Cur into @nObjClass, @nInheritDepth, @nOwnerDepth, @sDelClass, @sDelField, @nOrdrAndType
	end

	close Del_Cur
	deallocate Del_Cur

	--
	-- delete the objects in CmObject
	--
	delete CmObject with (REPEATABLEREAD)
	from #ObjInfoTbl$ do
	join CmObject co on do.[ObjId] = co.[id]
	set @Err = @@error
	if @Err <> 0 begin
		raiserror ('DeleteObj$: SQL Error %d; Unable to remove the objects from CmObject.', 16, 1, @Err)
		goto LFail
	end

	-- remove the temporary table used to hold the delete objects' information
	drop table #ObjInfoTbl$

	if @nTrnCnt = 0 commit tran DelObj$_Tran
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return 0

LFail:
	-- because the #ObjInfoTbl$ is a temporary table created within a procedure it is automatically
	--	removed by SQL Server, so it does not need to be explicitly deleted here

	rollback tran DelObj$_Tran
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err

GO
-------------------------------------------------------------------------------

-- First, let's clean up any leftover CmFolder objects.  As of Version 200034, none of these
-- objects are really being used, so any that exist are left over from antique versions which
-- have been migrated.

DECLARE @hvo INT, @sQry NVARCHAR(4000)

DECLARE cmoCursor CURSOR local static forward_only read_only FOR
	SELECT o.Id
	FROM CmObject o
	WHERE o.Class$ = 2	-- kclidCmFolder = 2
OPEN cmoCursor
FETCH NEXT FROM cmoCursor INTO @hvo
WHILE @@FETCH_STATUS = 0
BEGIN
	SET @sQry = 'EXEC DeleteObj$ ' + CONVERT(NVARCHAR(11), @hvo)
	EXEC (@sQry)
	FETCH NEXT FROM cmoCursor INTO @hvo
END
CLOSE cmoCursor
DEALLOCATE cmoCursor
GO


-- Fix the stored function fnGetDefaultAnalysesGlosses.

IF OBJECT_ID('fnGetDefaultAnalysesGlosses') IS NOT NULL BEGIN
	if (select DbVer from Version$) = 200033
		PRINT 'removing function fnGetDefaultAnalysesGlosses'
	DROP FUNCTION fnGetDefaultAnalysesGlosses
END
GO
if (select DbVer from Version$) = 200033
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
	JOIN CmAnnotation a  ON a.[Id] = ba.[Id]
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
	JOIN CmAnnotation a  ON a.[Id] = ba.[Id]
		AND a.AnnotationType = @nAnnotPunct
	WHERE ba.BeginObject = @nStTxtParaId

	RETURN
END
GO


declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200033
begin
	update Version$ set DbVer = 200034
	COMMIT TRANSACTION
	print 'database updated to version 200034'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200033 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
