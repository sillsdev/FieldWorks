-- Update database from version 200234 to 200235
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

---------------------------------------------------------------------------------
---- FDB-216, FDB-208: Remove XML calls from fnGetIdsFromString, DeleteObjects,
---- and their callers. Removed old procedure DeleteObj$.
---------------------------------------------------------------------------------

IF OBJECT_ID('fnGetIdsFromString') IS NOT NULL BEGIN
	PRINT 'removing function fnGetIdsFromString'
	DROP FUNCTION fnGetIdsFromString
END
GO
PRINT 'creating function fnGetIdsFromString'
GO

CREATE FUNCTION fnGetIdsFromString (
	@Ids NVARCHAR(MAX))
RETURNS @tabIds TABLE (ID INT)
AS
BEGIN
	--( This function works only if a comma is at the beginning and end
	--( of the string.
	IF SUBSTRING(@Ids, 1, 1) != N','
		SET @Ids = N',' + @Ids;
	IF SUBSTRING(@Ids, LEN(@Ids), 1) != N','
		SET @Ids = @Ids + N',';

	INSERT INTO @tabIds
	SELECT SUBSTRING(@Ids, n.N + 1, CHARINDEX(',', @Ids, n.N + 1) - n.N - 1)
	FROM Numbers n
	WHERE n.N < LEN(@Ids)
		AND SUBSTRING(@Ids, n.N, 1) = ',';  --Notice how we find the commas

	RETURN
END
GO

---------------------------------------------------------------------------------

if object_id('GenReplRCProc') is not null begin
	print 'removing proc GenReplRCProc';
	drop proc GenReplRCProc;
end
go
print 'Creating proc GenReplRCProc';
go
create proc [GenReplRCProc]
	@sTbl sysname
as
	declare @sDynSql nvarchar(4000), @sDynSql2 nvarchar(4000)
	declare @err int

	--( This procedure was built before we shortened up some of the procedure
	--( names from ReplaceRefColl_<tablename>_<fieldName> to
	--( ReplRC_<first 11 of tablename>_<first 11 of fieldname>. We need to tease
	--( them apart now from the @sTbl parameter, to name the procedure. We need
	--( to retain @sTbl for referencing the table itself.

	DECLARE
		@TableName SYSNAME,
		@FieldName SYSNAME,
		@Underscore INT,
		@ShortProcName SYSNAME;
	SET @UnderScore = CHARINDEX('_', @sTbl, 1);
	SET @TableName = SUBSTRING(@sTbl, 1, @Underscore - 1);
	SET @FieldName = SUBSTRING(@sTbl, @Underscore + 1, LEN(@sTbl) - @Underscore);
	SET @ShortProcName = SUBSTRING(@TableName, 1, 11) + N'_' + SUBSTRING(@FieldName, 1, 11)

	if object_id('ReplRC_' + @ShortProcName ) is not null begin
		set @sDynSql = 'alter '
	end
	else begin
		set @sDynSql = 'create '
	end

set @sDynSql = @sDynSql + N'
proc ReplRC_' + @ShortProcName +'
	@SrcObjId int,
	@ntInsIds NTEXT,
	@ntDelIds NTEXT
as
	declare	@fIsNocountOn int, @Err int, @nTrnCnt int, @sTranName varchar(300)
	declare @i int, @RowsAffected int

	set @Err = 0

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- determine if the procedure was called within a transaction; if yes then create a savepoint,
	--	otherwise create a transaction
	set @sTranName = ''ReplR_'+@ShortProcName+''' + convert(varchar(11), @@trancount)
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
	set @Err = @@error
	if @Err <> 0 begin
		raiserror(''ReplRC_'+@ShortProcName+': SQL Error %d; Unable to create a transaction'', 16, 1, @Err)
		goto LCleanup
	end

	-- determine if any object references should be removed
	IF @ntDelIds IS NOT NULL BEGIN
		-- objects may be listed in a collection more than once, and the delete list specifies how many
		--	occurrences of an object need to be removed; the above delete however removed all occurences,
		--	so the appropriate number of certain objects may need to be added back in

		-- create a temporary table to hold objects that are referenced more than once and at least one
		--	of the references is to be removed
		declare @t table (
			DstObjId int,
			Occurrences int,
			DelOccurrences int
		)
		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplRC_'+@ShortProcName+': SQL Error %d; Unable to create a temporary table'', 16, 1, @Err)
			goto LCleanup
		end

		-- get the objects that are referenced more than once along with the actual number of references; do this
		--	only for the objects where at least one reference is going to be removed

		INSERT INTO @t (DstObjId, DelOccurrences, Occurrences)
			SELECT jt.Dst, ol.DelCnt, COUNT(*)
			FROM ' + @sTbl + ' jt
			JOIN (
				SELECT Id ObjId, COUNT(*) DelCnt
				FROM dbo.fnGetIdsFromString(@ntDelIds)
				GROUP BY Id
				) AS ol ON jt.Dst = ol.ObjId
			WHERE jt.Src = @SrcObjId
			GROUP BY Dst, ol.DelCnt
			HAVING COUNT(*) > 1

		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplRC_'+@ShortProcName+': SQL Error %d; Unable to insert objects that are referenced more than once: SrcObjId(src) = %d'',
					16, 1, @Err, @SrcObjId)
			goto LCleanup
		end
'
set @sDynSql2 = N'
		-- remove the object references

		DELETE ' + @sTbl + '
			FROM ' + @sTbl + ' jt
			JOIN (SELECT Id FROM dbo.fnGetIdsFromString(@ntDelIds)) AS ol
				ON ol.Id = jt.Dst
			WHERE jt.Src = @SrcObjId

		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplRC_'+@ShortProcName+': SQL Error %d; Unable to delete objects from a reference collection: SrcObjId(src) = %d'',
					16, 1, @Err, @SrcObjId)
			goto LCleanup
		end

		-- reinsert the appropriate number of objects that had multiple references
		set @i = 0
		set @RowsAffected = 1 -- set to 1 to get inside of the loop
		while @RowsAffected > 0 begin
			insert into ['+@sTbl+'] ([Src], [Dst])
			select	@SrcObjid, [DstObjId]
			from	@t
			where	Occurrences - DelOccurrences > @i
			select @Err = @@error, @RowsAffected = @@rowcount
			if @Err <> 0 begin
				raiserror(''ReplRC_'+@ShortProcName+': SQL Error %d; Unable to reinsert objects into a reference collection: SrcObjId(src) = %d'',
						16, 1, @Err, @SrcObjId)
				goto LCleanup
			end
			set @i = @i + 1
		end

	end

	-- determine if any object references should be inserted
	IF @ntInsIds IS NOT NULL BEGIN

		INSERT INTO ' + @sTbl + ' WITH (REPEATABLEREAD) (Src, Dst)
			SELECT @SrcObjId, ol.Id
			FROM dbo.fnGetIdsFromString(@ntInsIds) AS ol

		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplRC_'+@ShortProcName+': SQL Error %d; Unable to insert objects into a reference collection: SrcObjId(src) = %d'',
					16, 1, @Err, @SrcObjId)
			goto LCleanup
		end
	end

LCleanup:
	if @Err <> 0 rollback tran @sTranName
	else if @nTrnCnt = 0 commit tran @sTranName

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	return @Err
'

	exec ( @sDynSql + @sDynSql2 )
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('GenReplRCProc: SQL Error %d: Unable to create or alter the procedure ReplRC_%s',
				16, 1, @Err, @ShortProcName)
		return @err
	end

	return 0
go

---------------------------------------------------------------------------------

--( Regenerate the ReplRC* stored procedures. (See the header comments of
--( GenReplRCProc for this code.)

		DECLARE @name NVARCHAR(MAX);
		DECLARE curReplRC CURSOR FOR
			SELECT c.Name + '_' + f.Name
			FROM Field$ f
			JOIN Class$ c ON c.Id = f.Class
			WHERE f.Type = 26
			ORDER BY 1;
		OPEN curReplRC
		FETCH curReplRC INTO @name;
		WHILE @@FETCH_STATUS = 0 BEGIN
			--PRINT @name --( nice for debugging, but can be taken out.
			EXEC GenReplRCProc @name
			FETCH curReplRC INTO @name;
		END
		CLOSE curReplRC;
		DEALLOCATE curReplRC;

GO

---------------------------------------------------------------------------------

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
	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf AND wa.Id = @hvoAnal
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
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
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.BeginOffset, a.Id

	RETURN
END

GO

---------------------------------------------------------------------------------

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
	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.Id AS AnnotationId, wamb.Ord, mff.Txt
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN MoForm_Form mff ON mff.Obj = wmb.Morph AND mff.Txt LIKE @nvcTextLike AND mff.Ws = @nWs
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
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
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.Id, wamb.Ord

	RETURN
END

GO

---------------------------------------------------------------------------------

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
	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst AND (SELECT dbo.fnGetEntryForSense(wmb.Sense)) = @hvoLexEntry
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
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
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.BeginOffset, a.Id

	RETURN
END

GO

---------------------------------------------------------------------------------

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
	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.Id AS AnnotationId, wamb.Ord, lsg.Txt
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	JOIN LexSense_Gloss lsg ON lsg.Obj = wmb.Sense AND lsg.Txt LIKE @nvcTextLike AND lsg.Ws = @nWs
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
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
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.Id, wamb.Ord

	RETURN
END

GO

---------------------------------------------------------------------------------

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
	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst AND wmb.Sense = @hvoSense
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
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
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.BeginOffset, a.Id

	RETURN
END

GO

---------------------------------------------------------------------------------

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
	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst AND wmb.Morph = @hvoForm
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
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
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.BeginOffset, a.Id

	RETURN
END

GO

---------------------------------------------------------------------------------

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
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
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
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.Id, wamb.Ord

	RETURN
END

GO

---------------------------------------------------------------------------------

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
	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_MorphBundles wamb ON wamb.Src = wa.Id
	JOIN WfiMorphBundle wmb ON wmb.Id = wamb.Dst
	LEFT OUTER JOIN MoStemMsa msm ON msm.Id= wmb.Msa
	LEFT OUTER JOIN MoInflAffMsa miam ON miam.Id= wmb.Msa
	LEFT OUTER JOIN MoDerivStepMsa mdsm ON mdsm.Id= wmb.Msa
	LEFT OUTER JOIN MoDerivAffMsa mdam ON mdam.Id= wmb.Msa
	LEFT OUTER JOIN MoUnclassifiedAffixMsa muam ON muam.Id= wmb.Msa
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
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
	LEFT OUTER JOIN MoInflAffMsa miam ON miam.Id= wmb.Msa
	LEFT OUTER JOIN MoDerivStepMsa mdsm ON mdsm.Id= wmb.Msa
	LEFT OUTER JOIN MoDerivAffMsa mdam ON mdam.Id= wmb.Msa
	LEFT OUTER JOIN MoUnclassifiedAffixMsa muam ON muam.Id= wmb.Msa
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
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

---------------------------------------------------------------------------------

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
	INSERT INTO @tblTextAnnotations
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiAnalysis wa ON wa.Id = a.InstanceOf
	JOIN WfiAnalysis_Meanings wam ON wam.Src = wa.Id AND wam.Dst = @hvoGloss
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	UNION
	SELECT a.BeginObject, a.BeginOffset, a.Id AS AnnotationId
	FROM CmBaseAnnotation_ a
	JOIN WfiGloss_ wg ON wg.Id = a.InstanceOf
	JOIN WfiAnalysis wa ON wa.Id = wg.Owner$
	JOIN WfiAnalysis_Meanings wam ON wam.Src = wa.Id AND wam.Dst = @hvoGloss
	JOIN StTxtPara_ p ON p.Id = a.BeginObject
	JOIN StText_ t ON t.Id = p.Owner$
	LEFT OUTER JOIN fnGetIdsFromString(@ntIds) i ON i.Id = t.Id
	WHERE (t.OwnFlid$ = @nOwnFlid OR i.Id IS NOT NULL)
	ORDER BY a.BeginObject, a.BeginOffset, a.Id

	RETURN
END

GO

---------------------------------------------------------------------------------

IF OBJECT_ID('DeleteObj$') IS NOT NULL BEGIN
	PRINT 'removing procedure DeleteObj$'
	DROP PROC DeleteObj$
END
GO

---------------------------------------------------------------------------------

IF OBJECT_ID('DeleteObjects') IS NOT NULL BEGIN
	PRINT 'removing procedure DeleteObjects'
	DROP PROC DeleteObjects
END
GO
PRINT 'creating procedure DeleteObjects'
GO

CREATE PROCEDURE DeleteObjects
	@ntIds NTEXT = NULL
AS
	DECLARE @tIds TABLE (ID INT, Level TINYINT)

	DECLARE
		@nRowCount INT,
		@nObjId INT,
		@nLevel INT,
		@nvcClassName NVARCHAR(100),
		@nvcSql NVARCHAR(1000),
		@nError INT

	SET @nError = 0

	--==( Load Ids )==--

	INSERT INTO @tIds
	SELECT f.ID, 0
	FROM dbo.fnGetIdsFromString(@ntIds) AS f

	--( Now find owned objects

	SET @nLevel = 1

	INSERT INTO @tIds
	SELECT o.ID, @nLevel
	FROM @tIds t
	JOIN CmObject o ON o.Owner$ = t.Id

	SET @nRowCount = @@ROWCOUNT
	WHILE @nRowCount != 0 BEGIN
		SET @nLevel = @nLevel + 1

		INSERT INTO @tIds
		SELECT o.ID, @nLevel
		FROM @tIds t
		JOIN CmObject o ON o.Owner$ = t.Id
		WHERE t.Level = @nLevel - 1

		SET @nRowCount = @@ROWCOUNT
	END
	SET @nLevel = @nLevel - 1

	--==( Delete objects )==--

	--( We're going to start out at the leaves and work
	--( toward the trunk.

	WHILE @nLevel >= 0	BEGIN

		SELECT TOP 1 @nObjId = t.ID, @nvcClassName = c.Name
		FROM @tIds t
		JOIN CmObject o ON o.Id = t.Id
		JOIN Class$ c ON c.ID = o.Class$
		WHERE t.Level = @nLevel
		ORDER BY t.Id

		SET @nRowCount = @@ROWCOUNT
		WHILE @nRowCount = 1 BEGIN
			SET @nvcSql = N'DELETE ' + @nvcClassName + N' WHERE Id = @nObjectID'
			EXEC sp_executesql @nvcSql, N'@nObjectID INT', @nObjectId = @nObjId
			SET @nError = @@ERROR
			IF @nError != 0
				GOTO Fail

			SELECT TOP 1 @nObjId = t.ID, @nvcClassName = c.Name
			FROM @tIds t
			JOIN CmObject o ON o.Id = t.Id
			JOIN Class$ c ON c.ID = o.Class$
			WHERE t.Id > @nobjId AND t.Level = @nLevel
			ORDER BY t.ID

			SET @nRowCount = @@ROWCOUNT
		END

		SET @nLevel = @nLevel - 1
	END

	RETURN 0

Fail:
	RETURN @nError
GO

---------------------------------------------------------------------------------

IF OBJECT_ID('MoveToOwnedAtom$') IS NOT NULL BEGIN
	PRINT 'removing procedure MoveToOwnedAtom$'
	DROP PROC MoveToOwnedAtom$
END
GO
PRINT 'creating procedure MoveToOwnedAtom$'
GO

create proc MoveToOwnedAtom$
	@ObjId int,
	@DstObjId int,
	@DstFlid int
as
	declare @sTranName varchar(50)
	declare	@fIsNocountOn int, @Err int, @nTrnCnt int
	declare @OldOwnerId int, @OldOwningFlid int, @nSrcType int,
		@nDstType int, @OriginalOrd int, @oldOwnedObject int,
		@StrId NVARCHAR(20);

	set @Err = 0

	-- transactions
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on
	set @sTranName = 'MoveToOwnedAtom$_' + convert(varchar(11), @@trancount)
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('MoveToOwnedAtom$: SQL Error %d; Unable to create a transaction', 16, 1, @Err)
		goto LFail
	end

	-- ( Get old owner information
	select @OldOwnerId=[Owner$], @OldOwningFlid=[OwnFlid$]
	from CmObject
	where [Id]=@ObjId

	-- ( Check new destination field type.
	select @nDstType = [Type]
	from Field$
	where [id] = @DstFlid
	if @nDstType <> 23 begin
		set @Err = 51000
		raiserror('MoveToOwnedAtom$: The destination must be to an owned atomic property', 16, 1)
		goto LFail
	end

	--( Check source property type
	select @nSrcType = [Type]
	from Field$
	where [id] = @OldOwningFlid
	if @nSrcType = 23 or @nSrcType = 25 or @nSrcType = 27 begin
		-- Any owning type is fine.
		set @Err = 0
	end
	else begin -- Other types not allowed.
		set @Err = 51000
		raiserror('MoveToOwnedAtom$: The source must be to an owning property', 16, 1)
		goto LFail
	end

	--( Delete current object owned in the atomic field, if it exists.
	select top 1 @oldOwnedObject = Id
	from CmObject
	where OwnFlid$=@DstFlid and Owner$=@DstObjId

	if @oldOwnedObject > 0 begin
		SET @StrId = CONVERT(NVARCHAR(20), @oldOwnedObject);
		EXEC DeleteObjects @StrId
		set @Err = @@error
		if @Err <> 0 begin
			raiserror('MoveToOwnedAtom$: SQL Error %d; Unable to delete old object.', 16, 1, @Err)
			goto LFail
		end
	end

	-- Store old OwnOrd$ value. (May be null.)
	select @OriginalOrd=[OwnOrd$]
	from CmObject
	where [Id]=OwnOrd$

	update	CmObject
	set [Owner$] = @DstObjId,
		[OwnFlid$] = @DstFlid,
		[OwnOrd$] = null
	where [Id] = @ObjId

	set @Err = @@error
	if @Err <> 0 begin
		raiserror('MoveToOwnedAtom$: SQL Error %d; Unable to update owners in CmObject: DstObjId(Owner$) = %d, DstFlid(OwnFlid$) = %d',
				16, 1, @Err, @DstObjId, @DstFlid)
		goto LFail
	end

	-- ( Renumber OwnOrd$ for items following in a sequence, if source was an owning sequence.
	if @OriginalOrd <> null begin
		update CmObject
		set [OwnOrd$]=[OwnOrd$] - 1
		where [Owner$]=@OldOwnerId and [OwnFlid$]=@OldOwningFlid and [OwnOrd$] > @OriginalOrd
	end

	-- stamp the owning objects as updated
	update CmObject
		set [UpdDttm] = getdate()
		where [Id] in (@OldOwnerId, @DstObjId)
		--( seems to execute as fast as a where clause written:
		--(    where [Id] = @OldOwnerId or [Id] =@DstObjId

	if @nTrnCnt = 0 commit tran @sTranName

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return 0

LFail:
	rollback tran @sTranName

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
GO

---------------------------------------------------------------------------------

if object_id('RemoveParserApprovedAnalyses$') is not null begin
	print 'removing proc RemoveParserApprovedAnalyses$'
	drop proc [RemoveParserApprovedAnalyses$]
end
go
print 'creating proc RemoveParserApprovedAnalyses$'
go

CREATE PROC [RemoveParserApprovedAnalyses$]
	@nWfiWordFormID INT
AS
	DECLARE
		@nIsNoCountOn INT,
		@nGonnerId INT,
		@nParserAgentId INT,
		@humanAgentId INT,
		@nError INT,
		@StrId NVARCHAR(20);

	SET @nIsNoCountOn = @@OPTIONS & 512
	IF @nIsNoCountOn = 0
		SET NOCOUNT ON

	SET @nError = 0

	-- Set checksum to zero
	UPDATE WfiWordform SET Checksum=0 WHERE Id=@nWfiWordFormID

	--( Get the parser agent id
	SELECT TOP 1 @nParserAgentId = Obj
	FROM CmAgent_Name
	WHERE Txt = N'M3Parser'
	-- Get Id of the 'default user' agent
	SELECT TOP 1 @humanAgentId = Obj
	FROM CmAgent_Name nme
	WHERE Txt = N'default user'

	--== Delete all parser evaluations that reference analyses owned by the @nWfiWordFormID wordform. ==--
	SELECT TOP 1 @nGonnerId = ae.[Id]
	FROM CmAgentEvaluation ae
	JOIN CmObject objae
		ON objae.[Id] = ae.[Id] AND objae.Owner$ = @nParserAgentId
	JOIN CmObject objanalysis
		ON objanalysis.[Id] = ae.Target
		AND objanalysis.Class$ = 5059 -- WfiAnalysis objects
		AND objanalysis.Owner$ = @nWfiWordFormID
	ORDER BY ae.[Id]
	WHILE @@ROWCOUNT != 0 BEGIN
		SET @StrId = CONVERT(NVARCHAR(20), @nGonnerId);
		EXEC @nError = DeleteObjects @StrId;
		IF @nError != 0
			GOTO Finish

		SELECT TOP 1 @nGonnerId = ae.[Id]
		FROM CmAgentEvaluation ae
		JOIN CmObject objae
			ON objae.[Id] = ae.[Id] AND objae.Owner$ = @nParserAgentId
		JOIN CmObject objanalysis
			ON objanalysis.[Id] = ae.Target
			AND objanalysis.Class$ = 5059 -- WfiAnalysis objects
			AND objanalysis.Owner$ = @nWfiWordFormID
		WHERE ae.[Id] > @nGonnerId
		ORDER BY ae.[Id]
	END

	--== Delete orphan analyses owned by the given @nWfiWordFormID wordform. ==--
	--== 'Orphan' means they have no evaluations ==--
	SELECT TOP 1 @nGonnerId = analysis.[Id]
	FROM CmObject analysis
	LEFT OUTER JOIN cmAgentEvaluation cae
		ON cae.Target = analysis.[Id]
	WHERE cae.Target IS NULL
		AND analysis.OwnFlid$ = 5062002		-- 5062002
		AND analysis.Owner$ = @nWfiWordFormID
	ORDER BY analysis.[Id]
	WHILE @@ROWCOUNT != 0 BEGIN
		SET @StrId = CONVERT(NVARCHAR(20), @nGonnerId);
		EXEC @nError = DeleteObjects @StrId;
		IF @nError != 0
			GOTO Finish

		SELECT TOP 1 @nGonnerId = analysis.[Id]
		FROM CmObject analysis
		LEFT OUTER JOIN cmAgentEvaluation cae
			ON cae.Target = analysis.[Id]
		WHERE cae.Target IS NULL
			AND analysis.[Id] > @nGonnerId
			AND analysis.OwnFlid$ = 5062002		-- 5062002
			AND analysis.Owner$ = @nWfiWordFormID
		ORDER BY analysis.[Id]
	END

Finish:
	IF @nIsNocountOn = 0 SET NOCOUNT OFF
	RETURN @nError

GO

---------------------------------------------------------------------------------

if object_id('RemoveUnusedAnalyses$') is not null begin
	print 'removing proc RemoveUnusedAnalyses$'
	drop proc [RemoveUnusedAnalyses$]
end
go
print 'creating proc RemoveUnusedAnalyses$'
go

CREATE PROCEDURE RemoveUnusedAnalyses$
	@nAgentId INT,
	@nWfiWordFormID INT,
	@dtEval DATETIME
AS
	DECLARE
		@nIsNoCountOn INT,
		@nGonnerID INT,
		@nError INT,
		@fMoreToDelete INT,
		@StrId NVARCHAR(20);

	SET @nIsNoCountOn = @@OPTIONS & 512
	IF @nIsNoCountOn = 0
		SET NOCOUNT ON

	SET @nGonnerId = NULL
	SET @nError = 0
	SET @fMoreToDelete = 0

	--== Delete all evaluations with null targets. ==--
	SELECT TOP 1 @nGonnerId = ae.[Id]
	FROM CmAgentEvaluation ae
	JOIN CmObject objae
		ON objae.[Id] = ae.[Id]
	WHERE ae.Target IS NULL
	ORDER BY ae.[Id]

	IF @@ROWCOUNT != 0 BEGIN
		SET @StrId = CONVERT(NVARCHAR(20), @nGonnerId);
		EXEC @nError = DeleteObjects @StrId;
		SET @fMoreToDelete = 1
		GOTO Finish
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

	IF @@ROWCOUNT != 0 BEGIN
		SET @StrId = CONVERT(NVARCHAR(20), @nGonnerId);
		EXEC @nError = DeleteObjects @StrId;
		SET @fMoreToDelete = 1
		GOTO Finish
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
			SET @fMoreToDelete = 1
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
		SET @StrId = CONVERT(NVARCHAR(20), @nGonnerId);
		EXEC @nError = DeleteObjects @StrId;
		SET @fMoreToDelete = 1
		GOTO Finish
	END

Finish:
	IF @nIsNocountOn = 0 SET NOCOUNT OFF
	SELECT @fMoreToDelete AS MoreToDelete
	RETURN @nError

GO

---------------------------------------------------------------------------------

if object_id('SetAgentEval') is not null begin
	print 'removing proc SetAgentEval'
	drop proc SetAgentEval
end
go
print 'creating proc SetAgentEval'
go

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
		@nvcError NVARCHAR(100),
		@StrId NVARCHAR(20);

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
			SET @StrId = CONVERT(NVARCHAR(20), @nEvalId);
			EXEC DeleteObjects @StrId;

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

			EXEC @nError = MakeObj_CmAgentEvaluation
				@dtEval,
				@nAccepted,
				@nvcDetails,
				@nAgentId,					--(owner
				23006,						--(ownflid  23006
				NULL,						--(startobj
				@nNewObjId OUTPUT,
				@guidNewObj OUTPUT,
				0,			--(ReturnTimeStamp
				@nNewObjTimeStamp OUTPUT

			IF @nError != 0 BEGIN
				SET @nvcError = 'SetAgentEval: MakeObj_CmAgentEvaluation failed.'
				GOTO Fail
			END

			UPDATE CmAgentEvaluation
			SET Target = @nTargetID
			WHERE Id = @nNewObjId
		END

		--( Update the existing Agent Evaluation
		ELSE

			UPDATE CmAgentEvaluation
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

GO

---------------------------------------------------------------------------------

if object_id('fnGetAddedNotebookObjects$') is not null begin
	print 'removing function fnGetAddedNotebookObjects$'
	drop function [fnGetAddedNotebookObjects$]
end
go
print 'creating function fnGetAddedNotebookObjects$'
go
create function [fnGetAddedNotebookObjects$] ()
returns @DelList table ([ObjId] int not null)
as
begin
	declare @nRowCnt int
	declare @nOwnerDepth int
	declare @Err int
	set @nOwnerDepth = 1
	set @Err = 0

	insert into @DelList
	select [Id] from CmObject
	where OwnFlid$ = 4001001 --( kflidRnResearchNbk_Records

	-- use a table variable to hold the possibility list item object ids
	declare @PossItems table (
		[ObjId] int primary key not null,
		[OwnerDepth] int null,
		[DateCreated] datetime null
	)

	-- Get the object ids for all of the possibility items in the lists used by Data Notebook
	-- (except for the Anthropology List, which is loaded separately).
	-- Note the hard-wired sets of possibility list flids.
	-- First, get the top-level possibility items from the standard data notebook lists.

	insert into @PossItems (ObjId, OwnerDepth, DateCreated)
	select co.[Id], @nOwnerDepth, cp.DateCreated
	from [CmObject] co
	join [CmPossibility] cp on cp.[id] = co.[id]
	join CmObject co2 on co2.[id] = co.Owner$ and co2.OwnFlid$ in (
			4001003, --( kflidRnResearchNbk_EventTypes
			6001025, --( kflidLangProject_ConfidenceLevels
			6001026, --( kflidLangProject_Restrictions
			6001027, --( kflidLangProject_WeatherConditions
			6001028, --( kflidLangProject_Roles
			6001029, --( kflidLangProject_AnalysisStatus
			6001030, --( kflidLangProject_Locations
			6001031, --( kflidLangProject_People
			6001032, --( kflidLangProject_Education
			6001033, --( kflidLangProject_TimeOfDay
			6001036  --( kflidLangProject_Positions
			)

	if @@error <> 0 goto LFail
	set @nRowCnt=@@rowcount

	-- Repeatedly get the list items owned at the next depth.

	while @nRowCnt > 0 begin
		set @nOwnerDepth = @nOwnerDepth + 1

		insert into @PossItems (ObjId, OwnerDepth, DateCreated)
		select co.[id], @nOwnerDepth, cp.DateCreated
		from [CmObject] co
		join [CmPossibility] cp on cp.[id] = co.[id]
		join @PossItems pi on pi.[ObjId] = co.[Owner$] and pi.[OwnerDepth] = @nOwnerDepth - 1

		if @@error <> 0 goto LFail
		set @nRowCnt=@@rowcount
	end

	-- Extract all the items which are newer than the language project, ie, which cannot be
	-- factory list items.
	-- Omit list items which are owned by other non-factory list items, since they will be
	-- deleted by deleting their owner.

	insert into @DelList
	select pi.ObjId
	from @PossItems pi
	join CmObject co on co.[id] = pi.ObjId
	where pi.DateCreated > (select top 1 DateCreated from CmProject order by DateCreated DESC)

	delete from @PossItems

	-- Get the object ids for all of the possibility items in the anthropology list.
	-- First, get the top-level possibility items from the anthropology list.

	set @nOwnerDepth = 1

	insert into @PossItems (ObjId, OwnerDepth, DateCreated)
	select co.[Id], @nOwnerDepth, cp.DateCreated
	from [CmObject] co
	join [CmPossibility] cp on cp.[id] = co.[id]
	where co.[Owner$] in (select id from CmObject where OwnFlid$ = 6001012)

	set @nRowCnt=@@rowcount
	if @@error <> 0 goto LFail

	-- Repeatedly get the anthropology list items owned at the next depth.

	while @nRowCnt > 0 begin
		set @nOwnerDepth = @nOwnerDepth + 1

		insert into @PossItems (ObjId, OwnerDepth, DateCreated)
		select co.[id], @nOwnerDepth, cp.DateCreated
		from [CmObject] co
		join [CmPossibility] cp on cp.[id] = co.[id]
		join @PossItems pi on pi.[ObjId] = co.[Owner$] and pi.[OwnerDepth] = @nOwnerDepth - 1

		if @@error <> 0 goto LFail
		set @nRowCnt=@@rowcount
	end

	declare @cAnthro int
	declare @cTimes int
	select @cAnthro = COUNT(*) from @PossItems
	select @cTimes = COUNT(distinct DateCreated) from @PossItems

	if @cTimes = @cAnthro begin
		-- Assume that none of them are factory if they all have different creation
		-- times.  This is true even if there's only one item.
		insert into @DelList
		select pi.ObjId
		from @PossItems pi
		where pi.OwnerDepth = 1
	end
	else if @cTimes != 1 begin
		-- assume that the oldest items are factory, the rest aren't
		insert into @DelList
		select pi.ObjId
		from @PossItems pi
		where pi.DateCreated > (select top 1 DateCreated from @PossItems order by DateCreated)
	end

return

LFail:
	delete from @DelList
	return
end

go

---------------------------------------------------------------------------------

if object_id('DeleteAddedNotebookObjects$') is not null begin
	print 'removing proc DeleteAddedNotebookObjects$'
	drop proc [DeleteAddedNotebookObjects$]
end
go
print 'creating proc DeleteAddedNotebookObjects$'
go
create proc [DeleteAddedNotebookObjects$]
as
	declare @Err int
	set @Err = 0

	-- determine if the procedure was called within a transaction;
	-- if yes then create a savepoint, otherwise create a transaction
	declare @nTrnCnt int
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran DelObj$_Tran
	else save tran DelObj$_Tran
	set @Err = @@error
	if @Err <> 0 begin
		raiserror ('DeleteAddedNotebookObjects$: SQL Error %d; Unable to create a transaction.', 16, 1, @Err)
		goto LFail
	end

	-- delete the objects (records and list items) added to the data notebook
	-- first, build a comma delimited string containing all of the object ids

	declare @ObjId int
	declare @CommaDelimited varchar(8000)
	declare @cObj int
	set @CommaDelimited = ',' --( The stored procedure will if you don't, anyway.
	set @cObj = 0

	DECLARE curObj CURSOR FOR SELECT [ObjId] FROM dbo.fnGetAddedNotebookObjects$()
	OPEN curObj
	FETCH NEXT FROM curObj INTO @ObjId
	WHILE @@FETCH_STATUS = 0
	BEGIN
		set @CommaDelimited = @CommaDelimited + cast(@ObjId as varchar(10)) + ','
		set @cObj = @cObj + 1
		if len(@CommaDelimited) > 7970 begin
			-- we are close to filling the string, so delete all the
			-- objects (in one swell foop).
			EXEC @Err = DeleteObjects @CommaDelimited;
			set @cObj = 0
			if @Err <> 0 goto LFail
		end
		FETCH NEXT FROM curObj INTO @ObjId
	END
	CLOSE curObj
	DEALLOCATE curObj

	if @cObj <> 0 begin
		-- now, delete all the objects (in one swell foop).
		EXEC @Err = DeleteObjects @CommaDelimited;
		if @Err <> 0 goto LFail
	end

	if @nTrnCnt = 0 commit tran DelObj$_Tran

	return 0

LFail:
	rollback tran DelObj$_Tran
	return @Err

GO

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200234
BEGIN
	UPDATE Version$ SET DbVer = 200235
	COMMIT TRANSACTION
	PRINT 'database updated to version 200235'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200234 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
