-- Update database from version 200187 to 200188
-- These stored functions are part of the work for implemented LT-6938 and fixing CLE-75.

BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Fix fnGetOwnedObjects$ to quit recursing after 16 levels deep if it's generating the
-- OrdKey field.  This helps fix CLE-75.
if object_id('fnGetOwnedObjects$') is not null begin
	print 'removing function fnGetOwnedObjects$'
	drop function [fnGetOwnedObjects$]
end
go
print 'creating function fnGetOwnedObjects$'
go
create function [fnGetOwnedObjects$] (
	@ObjId int=null,
	@hXMLDocObjList int=null,
	@grfcpt int=528482304,
	@fBaseClasses tinyint=0,
	@fSubClasses tinyint=0,
	@fRecurse tinyint=1,
	@riid int=NULL,
	@fCalcOrdKey tinyint=1 )
returns @ObjInfo table (
	[ObjId]		int		not null,
	[ObjClass]	int		null,
	[InheritDepth]	int		null		default(0),
	[OwnerDepth]	int		null		default(0),
	[RelObjId]	int		null,
	[RelObjClass]	int		null,
	[RelObjField]	int		null,
	[RelOrder]	int		null,
	[RelType]	int		null,
	[OrdKey]	varbinary(250)	null		default(0)
)
as
begin
	declare @nRowCnt int
	declare	@nObjId int, @nInheritDepth int, @nOwnerDepth int, @sOrdKey varchar(250)

	-- if NULL was specified as the mask assume that all objects are desired
	if @grfcpt is null set @grfcpt = 528482304

	-- at least one and only one object must be specified somewhere - objId paramater or XML
	if @objId is null and @hXMLDocObjList is null goto LFail
	if @objId is not null and @hXMLDocObjList is not null goto LFail

	-- determine if an object was supplied as an argument, if one was not use ObjInfoTbl$ as the list of objects
	if @objId is not null begin

		-- get the class of the specified object
		insert into @ObjInfo (ObjId, ObjClass, OrdKey)
		select	@objId, co.[Class$],
			-- calculate the order key even if @fCalcOrdKey = 0 because the overhead is very small here
			convert(varbinary, coalesce(co.[Owner$], 0)) +
			convert(varbinary, coalesce(co.[OwnFlid$], 0)) +
			convert(varbinary, coalesce(co.[OwnOrd$], 0))
		from	[CmObject] co
		where	co.[Id] = @objId
		if @@error <> 0 goto LFail
	end
	else begin

		-- parse the XML list of Object IDs and insert them into the table variable
		insert into @ObjInfo (ObjId, ObjClass, OrdKey)
		select	i.[Id], co.[Class$],
			-- calculate the order key even if @fCalcOrdKey = 0 because the overhead is very small here
			convert(varbinary, coalesce(co.[Owner$], 0)) +
			convert(varbinary, coalesce(co.[OwnFlid$], 0)) +
			convert(varbinary, coalesce(co.[OwnOrd$], 0))
		from	openxml (@hXMLDocObjList, '/root/Obj') with ([Id] int) i
			join [CmObject] co on co.[Id] = i.[Id]
		if @@error <> 0 goto LFail
	end

	set @nOwnerDepth = 1
	set @nRowCnt = 1
	while @nRowCnt > 0 begin
		-- determine if the order key should be calculated - if the order key is not needed a more
		--    effecient query can be used to generate the ownership tree
		if @fCalcOrdKey = 1 begin
			-- get the objects owned at the next depth and calculate the order key
			insert	into @ObjInfo
				(ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
			select 	co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[Class], co.[OwnFlid$], co.[OwnOrd$], f.[Type],
				oi.OrdKey+convert(varbinary, co.[Owner$]) + convert(varbinary, co.[OwnFlid$]) + convert(varbinary, coalesce(co.[OwnOrd$], 0))
			from 	[CmObject] co
					join @ObjInfo oi on co.[Owner$] = oi.[ObjId]
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	oi.[OwnerDepth] = @nOwnerDepth - 1
				and ( 	( @grfcpt & 8388608 = 8388608 and f.[Type] = 23 )
					or ( @grfcpt & 33554432 = 33554432 and f.[Type] = 25 )
					or ( @grfcpt & 134217728 = 134217728 and f.[Type] = 27 )
				)
		end
		else begin
			-- get the objects owned at the next depth and do not calculate the order key
			insert	into @ObjInfo
				(ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType)
			select 	co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[Class], co.[OwnFlid$], co.[OwnOrd$], f.[Type]
			from 	[CmObject] co
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	exists (select 	*
					from 	@ObjInfo oi
					where 	oi.[ObjId] = co.[Owner$]
						and oi.[OwnerDepth] = @nOwnerDepth - 1
				)
				and ( 	( @grfcpt & 8388608 = 8388608 and f.[Type] = 23 )
					or ( @grfcpt & 33554432 = 33554432 and f.[Type] = 25 )
					or ( @grfcpt & 134217728 = 134217728 and f.[Type] = 27 )
				)
		end
		set @nRowCnt=@@rowcount

		-- determine if the whole owning tree should be included in the results
		if @fRecurse = 0 break
		-- give up before we crash due to OrdKey getting too long
		if @fCalcOrdKey = 1 AND @nOwnerDepth >= 16 break

		set @nOwnerDepth = @nOwnerDepth + 1
	end

	--
	-- get all of the base classes of the object(s), including CmObject.
	--
	if @fBaseClasses = 1 begin
		insert	into @ObjInfo
			(ObjId, ObjClass, InheritDepth, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
		select	oi.[ObjId], p.[Dst], p.[Depth], oi.[OwnerDepth], oi.[RelObjId], oi.[RelObjClass], oi.[RelObjField], oi.[RelOrder], oi.[RelType], oi.[OrdKey]
		from	@ObjInfo oi
				join [ClassPar$] p on oi.[ObjClass] = p.[Src]
				join [Class$] c on c.[id] = p.[Dst]
		where	p.[Depth] > 0
		if @@error <> 0 goto LFail
	end
	--
	-- get all of the sub classes of the object(s)
	--
	if @fSubClasses = 1 begin
		insert	into @ObjInfo
			(ObjId, ObjClass, InheritDepth, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
		select	oi.[ObjId], p.[Src], -p.[Depth], oi.[OwnerDepth], oi.[RelObjId], oi.[RelObjClass], oi.[RelObjField], oi.[RelOrder], oi.[RelType], oi.[OrdKey]
		from	@ObjInfo oi
				join [ClassPar$] p on oi.[ObjClass] = p.[Dst] and InheritDepth = 0
				join [Class$] c on c.[id] = p.[Dst]
		where	p.[Depth] > 0
			and p.[Dst] <> 0
		if @@error <> 0 goto LFail
	end

	-- if a class was specified remove the owned objects that are not of that type of class; these objects were
	--    necessary in order to get a list of all of the referenced and referencing objects that were potentially
	--    the type of specified class
	if @riid is not null begin
		delete	@ObjInfo
		where 	not exists (
				select	*
				from	[ClassPar$] cp
				where	cp.[Dst] = @riid
					and cp.[Src] = [ObjClass]
			)
		if @@error <> 0 goto LFail
	end

	return
LFail:

	delete from @ObjInfo
	return
end
go


-- Fix GetPossibilities to return OwnerDepth and RelObjId instead of OrdKey, and to order by
-- OwnerDepth and RelOrder instead of OrdKey.  This helps fix CLE-75 (with more work done in
-- the C++ code).
if object_id('GetPossibilities') is not null begin
	print 'removing proc GetPossibilities'
	drop proc [GetPossibilities]
end
go
print 'creating proc GetPossibilities'
go
create proc [GetPossibilities]
	@ObjId int,
	@Ws int
as
	declare @uid uniqueidentifier,
			@retval int

	-- get all of the possibilities owned by the specified possibility list object
	declare @tblObjInfo table (
		[ObjId]		int		not null,
		[ObjClass]	int		null,
		[InheritDepth]	int		null	default(0),
		[OwnerDepth]	int		null	default(0),
		[RelObjId]	int		null,
		[RelObjClass]	int		null,
		[RelObjField]	int		null,
		[RelOrder]	int		null,
		[RelType]	int		null,
		[OrdKey]	varbinary(250)	null	default(0))

	insert into @tblObjInfo
		select * from fnGetOwnedObjects$(@ObjId, null, 176160768, 0, 0, 1, 7, 0)

	-- First return a count so that the caller can preallocate memory for the results.
	select count(*) from @tblObjInfo

	--
	--  get an ordered list of relevant writing system codes
	--
	declare @tblWs table (
		[WsId]	int not null, -- don't make unique. It shouldn't happen, but we don't want a crash if it does.
		[Ord]	int primary key clustered identity(1,1))
	--( 0xffffffff (-1) or 0xfffffffd (-3) = First string from a) ordered checked analysis
	-- writing systems b) any remaining analysis writing systems or stars if none of the above.
	if @Ws = 0xffffffff or @Ws = 0xfffffffd begin
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentAnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id
			order by caws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_AnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	--( 0xfffffffe (-2) or 0xfffffffc (-4) = First string from a) ordered checked vernacular
	-- writing systems b) any remaining vernacular writing systems or stars if none of the above.
	else if @Ws = 0xfffffffe or @Ws = 0xfffffffc begin
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentVernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id
			order by cvws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_VernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	--( 0xfffffffb = -5 = First string from a) ordered checked analysis writing systems
	-- b) ordered checked vernacular writing systems, c) any remaining analysis writing systems,
	-- d) any remaining vernacular writing systems or stars if none of the above.
	else if @Ws = 0xfffffffb begin
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentAnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id
			order by caws.Ord
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentVernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id
			where lws.id not in (select WsId from @tblWs)
			order by cvws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_AnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id and lws.id not in (select WsId from @tblWs)
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_VernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	--( 0xfffffffa = -6 = First string from a) ordered checked vernacular writing systems
	-- b) ordered checked analysis writing systems, c) any remaining vernacular writing systems,
	-- d) any remaining analysis writing systems or stars if none of the above.
	else if @Ws = 0xfffffffa begin
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentVernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id
			order by cvws.Ord
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentAnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id
			where lws.id not in (select WsId from @tblWs)
			order by caws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_VernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id and lws.id not in (select WsId from @tblWs)
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_AnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	else -- Hard coded value
		insert into @tblWs (WsId) Values(@Ws)

	-- Now that we have the desired writing systems in @tblWs, we can return the desired information.
	select
		o.ObjId,
		(select top 1 isnull(ca.[txt], '***') + ' - ' + isnull(cn.[txt], '***')
			from LgWritingSystem lws
			left outer join CmPossibility_Name cn on cn.[ws] = lws.[Id] and cn.[Obj] = o.[objId]
			left outer join CmPossibility_Abbreviation ca on ca.[ws] = lws.[Id] and ca.[Obj] = o.[objId]
			join @tblWs wstbl on wstbl.WsId = lws.id
			order by (
				select [Ord] = CASE
					WHEN cn.[txt] IS NOT NULL THEN wstbl.[ord]
					WHEN ca.[txt] IS NOT NULL THEN wstbl.[ord] + 9000
					ELSE wstbl.[Ord] + 99000
					END)),
		isnull((select top 1 lws.id
			from LgWritingSystem lws
			left outer join CmPossibility_Name cn on cn.[ws] = lws.[Id] and cn.[Obj] = o.[objId]
			left outer join CmPossibility_Abbreviation ca on ca.[ws] = lws.[Id] and ca.[Obj] = o.[objId]
			join @tblWs wstbl on wstbl.WsId = lws.id
			order by (
				select [Ord] = CASE
					WHEN cn.[txt] IS NOT NULL THEN wstbl.[ord]
					WHEN ca.[txt] IS NOT NULL THEN wstbl.[ord] + 9000
					ELSE wstbl.[Ord] + 99000
					END)
			), (select top 1 WsId from @tblws)),
		o.OwnerDepth, cp.ForeColor, cp.BackColor, cp.UnderColor, cp.UnderStyle, o.RelObjId
	from @tblObjInfo o
		left outer join CmPossibility cp on cp.[id] = o.[objId]
	order by o.OwnerDepth, o.RelOrder

	return @retval
go


-- Fix bug with matching against Scripture text ids.
IF OBJECT_ID('fnConcordForLexEntry') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForLexEntry'
	DROP FUNCTION fnConcordForLexEntry
END
GO
PRINT 'creating function fnConcordForLexEntry'
GO

CREATE FUNCTION dbo.fnConcordForLexEntry(
	@nOwnFlid INT,
	@nvcTextLike NVARCHAR(4000),
	@nWs INT,
	@ntIds NTEXT)
RETURNS @tblTextAnnotations TABLE (
	BeginObject INT,
	AnnotationId INT,
	Ord INT,
	Txt NVARCHAR(4000))
AS
BEGIN
	DECLARE @tblIds TABLE (Id INT)

	INSERT INTO @tblIds SELECT f.ID FROM dbo.fnGetIdsFromString(@ntIds, NULL) AS f

	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.Id AS AnnotationId, wamb.Ord, mff.Txt
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN MoForm_Form mff ON mff.Obj = wmb.Morph AND mff.Txt LIKE @nvcTextLike AND mff.Ws = @nWs
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	UNION
	SELECT a.BeginObject, a.Id AS AnnotationId, wamb.Ord, mff.Txt
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN MoForm_Form mff ON mff.Obj = wmb.Morph AND mff.Txt LIKE @nvcTextLike AND mff.Ws = @nWs
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.Id, wamb.Ord

	RETURN
END

GO

-- Fix bug with matching against Scripture text ids.
IF OBJECT_ID('fnConcordForLexGloss') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForLexGloss'
	DROP FUNCTION fnConcordForLexGloss
END
GO
PRINT 'creating function fnConcordForLexGloss'
GO

CREATE FUNCTION dbo.fnConcordForLexGloss(
	@nOwnFlid INT,
	@nvcTextLike NVARCHAR(4000),
	@nWs INT,
	@ntIds NTEXT)
RETURNS @tblTextAnnotations TABLE (
	BeginObject INT,
	AnnotationId INT,
	Ord INT,
	Txt NVARCHAR(4000))
AS
BEGIN
	DECLARE @tblIds TABLE (Id INT)

	INSERT INTO @tblIds SELECT f.ID FROM dbo.fnGetIdsFromString(@ntIds, NULL) AS f

	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.Id AS AnnotationId, wamb.Ord, lsg.Txt
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN LexSense_Gloss lsg ON lsg.Obj = wmb.Sense AND lsg.Txt LIKE @nvcTextLike AND lsg.Ws = @nWs
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	UNION
	SELECT a.BeginObject, a.Id AS AnnotationId, wamb.Ord, lsg.Txt
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN LexSense_Gloss lsg ON lsg.Obj = wmb.Sense AND lsg.Txt LIKE @nvcTextLike AND lsg.Ws = @nWs
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.Id, wamb.Ord

	RETURN
END

GO


-- Fix bug with matching against Scripture text ids.
IF OBJECT_ID('fnConcordForMorphemes') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForMorphemes'
	DROP FUNCTION fnConcordForMorphemes
END
GO
PRINT 'creating function fnConcordForMorphemes'
GO

CREATE FUNCTION dbo.fnConcordForMorphemes(
	@nOwnFlid INT,
	@nvcTextLike NVARCHAR(4000),
	@nWs INT,
	@ntIds NTEXT)
RETURNS @tblTextAnnotations TABLE (
	BeginObject INT,
	AnnotationId INT,
	Ord INT,
	Txt NVARCHAR(4000))
AS
BEGIN
	DECLARE @tblIds TABLE (Id INT)

	INSERT INTO @tblIds SELECT f.ID FROM dbo.fnGetIdsFromString(@ntIds, NULL) AS f

	INSERT INTO @tblTextAnnotations
	--( Get analyses
	SELECT a.BeginObject, a.Id AS AnnotationId, wamb.Ord, u.Txt
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN (
		SELECT wmb.Id, mff.Txt
		FROM WfiMorphBundle wmb
		JOIN MoForm_Form mff ON mff.Obj = wmb.Morph AND mff.Txt LIKE @nvcTextLike AND mff.Ws = @nWs
		UNION
		SELECT f.Obj, f.Txt
		FROM WfiMorphBundle_Form f
		WHERE f.Txt LIKE @nvcTextLike AND f.Ws = @nWs
		) u ON u.Id = wamb.Dst
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	--( Get glosses of analyses
	UNION
	SELECT a.BeginObject, a.Id AS AnnotationId, wamb.Ord, u.Txt
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN (
		SELECT wmb.Id, mff.Txt
		FROM WfiMorphBundle wmb
		JOIN MoForm_Form mff ON mff.Obj = wmb.Morph AND mff.Txt LIKE @nvcTextLike AND mff.Ws = @nWs
		UNION
		SELECT f.Obj, f.Txt
		FROM WfiMorphBundle_Form f
		WHERE f.Txt LIKE @nvcTextLike AND f.Ws = @nWs
		) u ON u.Id = wamb.Dst
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.Id, wamb.Ord

	RETURN
END

GO


-- fnConcordForAnalysis was implemented as part of LT-6938.
IF OBJECT_ID('fnConcordForAnalysis') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForAnalysis'
	DROP FUNCTION fnConcordForAnalysis
END
GO
PRINT 'creating function fnConcordForAnalysis'
GO

CREATE FUNCTION [dbo].[fnConcordForAnalysis](
	@nOwnFlid INT,
	@hvoAnal INT,
	@ntIds NTEXT)
RETURNS @tblTextAnnotations TABLE (
	BeginObject INT,
	BeginOffset INT,
	AnnotationId INT)
AS
BEGIN
	DECLARE @tblIds TABLE (Id INT)

	INSERT INTO @tblIds SELECT f.ID FROM dbo.fnGetIdsFromString(@ntIds, NULL) AS f

	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf AND wa.Id = @hvoAnal
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	UNION
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$ AND wa.Id = @hvoAnal
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.BeginOffset, a.Id

	RETURN
END

GO


-- fnConcordForLexEntryHvo was implemented as part of LT-6938.
IF OBJECT_ID('fnConcordForLexEntryHvo') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForLexEntryHvo'
	DROP FUNCTION fnConcordForLexEntryHvo
END
GO
PRINT 'creating function fnConcordForLexEntryHvo'
GO

CREATE FUNCTION dbo.fnConcordForLexEntryHvo(
	@nOwnFlid INT,
	@hvoLexEntry INT,
	@ntIds NTEXT)
RETURNS @tblTextAnnotations TABLE (
	BeginObject INT,
	BeginOffset INT,
	AnnotationId INT)
AS
BEGIN
	DECLARE @tblIds TABLE (Id INT)

	INSERT INTO @tblIds SELECT f.ID FROM dbo.fnGetIdsFromString(@ntIds, NULL) AS f

	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst AND (SELECT dbo.fnGetEntryForSense(wmb.Sense)) = @hvoLexEntry
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	UNION
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst AND (SELECT dbo.fnGetEntryForSense(wmb.Sense)) = @hvoLexEntry
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.BeginOffset, a.Id

	RETURN
END

GO


-- fnConcordForLexSense was implemented as part of LT-6938.
IF OBJECT_ID('fnConcordForLexSense') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForLexSense'
	DROP FUNCTION fnConcordForLexSense
END
GO
PRINT 'creating function fnConcordForLexSense'
GO

CREATE FUNCTION dbo.fnConcordForLexSense(
	@nOwnFlid INT,
	@hvoSense INT,
	@ntIds NTEXT)
RETURNS @tblTextAnnotations TABLE (
	BeginObject INT,
	BeginOffset INT,
	AnnotationId INT)
AS
BEGIN
	DECLARE @tblIds TABLE (Id INT)

	INSERT INTO @tblIds SELECT f.ID FROM dbo.fnGetIdsFromString(@ntIds, NULL) AS f

	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst AND wmb.Sense = @hvoSense
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	UNION
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst AND wmb.Sense = @hvoSense
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.BeginOffset, a.Id

	RETURN
END

GO


-- fnConcordForMoForm was implemented as part of LT-6938.
IF OBJECT_ID('fnConcordForMoForm') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForMoForm'
	DROP FUNCTION fnConcordForMoForm
END
GO
PRINT 'creating function fnConcordForMoForm'
GO

CREATE FUNCTION dbo.fnConcordForMoForm(
	@nOwnFlid INT,
	@hvoForm INT,
	@ntIds NTEXT)
RETURNS @tblTextAnnotations TABLE (
	BeginObject INT,
	BeginOffset INT,
	AnnotationId INT)
AS
BEGIN
	DECLARE @tblIds TABLE (Id INT)

	INSERT INTO @tblIds SELECT f.ID FROM dbo.fnGetIdsFromString(@ntIds, NULL) AS f

	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst AND wmb.Morph = @hvoForm
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	UNION
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst AND wmb.Morph = @hvoForm
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.BeginOffset, a.Id

	RETURN
END

GO


-- fnConcordForPartOfSpeech was implemented as part of LT-6938.
IF OBJECT_ID('fnConcordForPartOfSpeech') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForPartOfSpeech'
	DROP FUNCTION fnConcordForPartOfSpeech
END
GO
PRINT 'creating function fnConcordForPartOfSpeech'
GO

CREATE FUNCTION [dbo].[fnConcordForPartOfSpeech](
	@nOwnFlid INT,
	@hvoPOS INT,
	@ntIds NTEXT)
RETURNS @tblTextAnnotations TABLE (
	BeginObject INT,
	BeginOffset INT,
	AnnotationId INT)
AS
BEGIN
	DECLARE @tblIds TABLE (Id INT)

	INSERT INTO @tblIds SELECT f.ID FROM dbo.fnGetIdsFromString(@ntIds, NULL) AS f

	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	LEFT OUTER JOIN MoStemMsa msm ON msm.Id= wmb.Msa
	LEFT OUTER JOIN MoInflectionalAffixMsa miam ON miam.Id= wmb.Msa
	LEFT OUTER JOIN MoDerivationalStepMsa mdsm ON mdsm.Id= wmb.Msa
	LEFT OUTER JOIN MoDerivationalAffixMsa mdam ON mdam.Id= wmb.Msa
	LEFT OUTER JOIN MoUnclassifiedAffixMsa muam ON muam.Id= wmb.Msa
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE ((t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL) AND
		   (wa.Category=@hvoPOS OR
			msm.PartOfSpeech=@hvoPOS OR
			miam.PartOfSpeech=@hvoPOS OR
			mdsm.PartOfSpeech=@hvoPOS OR
			mdam.ToPartOfSpeech=@hvoPOS OR
			muam.PartOfSpeech=@hvoPOS))
	UNION
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	LEFT OUTER JOIN MoStemMsa msm ON msm.Id= wmb.Msa
	LEFT OUTER JOIN MoInflectionalAffixMsa miam ON miam.Id= wmb.Msa
	LEFT OUTER JOIN MoDerivationalStepMsa mdsm ON mdsm.Id= wmb.Msa
	LEFT OUTER JOIN MoDerivationalAffixMsa mdam ON mdam.Id= wmb.Msa
	LEFT OUTER JOIN MoUnclassifiedAffixMsa muam ON muam.Id= wmb.Msa
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE ((t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL) AND
		   (wa.Category=@hvoPOS OR
			msm.PartOfSpeech=@hvoPOS OR
			miam.PartOfSpeech=@hvoPOS OR
			mdsm.PartOfSpeech=@hvoPOS OR
			mdam.ToPartOfSpeech=@hvoPOS OR
			muam.PartOfSpeech=@hvoPOS))
	ORDER BY a.BeginObject, a.BeginOffset, a.Id

	RETURN
END

GO


-- fnConcordForWfiGloss was implemented as part of LT-6938.
IF OBJECT_ID('fnConcordForWfiGloss') IS NOT NULL BEGIN
	PRINT 'removing function fnConcordForWfiGloss'
	DROP FUNCTION fnConcordForWfiGloss
END
GO
PRINT 'creating function fnConcordForWfiGloss'
GO

CREATE FUNCTION dbo.fnConcordForWfiGloss(
	@nOwnFlid INT,
	@hvoGloss INT,
	@ntIds NTEXT)
RETURNS @tblTextAnnotations TABLE (
	BeginObject INT,
	BeginOffset INT,
	AnnotationId INT)
AS
BEGIN
	DECLARE @tblIds TABLE (Id INT)

	INSERT INTO @tblIds SELECT f.ID FROM dbo.fnGetIdsFromString(@ntIds, NULL) AS f

	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_Meanings wam ON wam.Src = wa.Id AND wam.Dst = @hvoGloss
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	UNION
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$
	JOIN WfiAnalysis_Meanings wam ON wam.Src = wa.Id AND wam.Dst = @hvoGloss
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN @tblIds i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.BeginOffset, a.Id

	RETURN
END

GO


-- fnGetEntryForSense was implemented as part of LT-6938.
IF OBJECT_ID('fnGetEntryForSense') IS NOT NULL BEGIN
	PRINT 'removing function fnGetEntryForSense'
	DROP FUNCTION fnGetEntryForSense
END
GO
PRINT 'creating function fnGetEntryForSense'
GO

CREATE FUNCTION dbo.fnGetEntryForSense(@hvoSense INT)
RETURNS INT
AS
BEGIN
	declare @hvoEntry int, @OwnFlid int, @ObjId int

	set @hvoEntry = 0
	if @hvoSense < 1 return(@hvoEntry)	-- Bad Id

	set @OwnFlid = 0
	set @ObjId = @hvoSense

	-- Loop until we find an owning flid of 5002011 (or null for some ownership error).
	while @OwnFlid != 5002011
	begin
		select 	@hvoEntry=isnull(Owner$, 0), @OwnFlid=OwnFlid$
		from	CmObject
		where	Id=@ObjId

		set @ObjId=@hvoEntry
		if @hvoEntry = 0
			return(@hvoEntry)
	end

	return(@hvoEntry)
END
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200187
BEGIN
	UPDATE Version$ SET DbVer = 200188
	COMMIT TRANSACTION
	PRINT 'database updated to version 200188'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200187 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
