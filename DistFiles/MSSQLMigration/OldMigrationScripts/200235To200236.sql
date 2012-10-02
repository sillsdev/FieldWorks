-- Update database from version 200235 to 200236
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

---------------------------------------------------------------------------------
---- FDB-213, FDB-214, and FDB-215: Remove XML calls from fnGetOwnedObjects$,
---- GetLinkedObjs$, GetUndoDelObjInfo, and their callers. Removed old procedure
---- GetPossKeyWord.
---- FDB-226: Make GetOrderedMultiTxt able to handle multiple object IDs;
---- remove obsolete GetOrderedMultiTxtXml$.
---------------------------------------------------------------------------------

if object_id('GetPossKeyword') is not null begin
	print 'removing proc GetPossKeyword'
	drop proc [GetPossKeyword]
end
go

---------------------------------------------------------------------------------

if object_id('fnGetOwnedObjects$') is not null begin
	print 'removing function fnGetOwnedObjects$'
	drop function [fnGetOwnedObjects$]
end
go
print 'creating function fnGetOwnedObjects$'
go
create function [fnGetOwnedObjects$] (
	@ObjIds NVARCHAR(MAX),
	@grfcpt int=kgrfcptAll,
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
	if @grfcpt is null
		set @grfcpt = 528482304 --( kgrfcptAll

	insert into @ObjInfo (ObjId, ObjClass, OrdKey)
		select	i.[Id], co.[Class$],
			-- calculate the order key even if @fCalcOrdKey = 0 because the overhead is very small here
			convert(varbinary, coalesce(co.[Owner$], 0)) +
			convert(varbinary, coalesce(co.[OwnFlid$], 0)) +
			convert(varbinary, coalesce(co.[OwnOrd$], 0))
		FROM fnGetIdsFromString(@ObjIds) i
		JOIN CmObject co ON co.Id = i.Id;

	if @@error <> 0
		goto LFail

	-- TODO (SteveMiller): These queries really need to be optimized. See FDB-219.

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

---------------------------------------------------------------------------------

if object_id('GetLinkedObjs$') is not null begin
	print 'removing proc GetLinkedObjs$'
	drop proc [GetLinkedObjs$]
end
go
print 'creating proc GetLinkedObjs$'
go

create proc [GetLinkedObjs$]
	@ObjIds NVARCHAR(MAX),
	@grfcpt int=528482304,
	@fBaseClasses bit=0,
	@fSubClasses bit=0,
	@fRecurse bit=1,
	@nRefDirection smallint=0,
	@riid int=null,
	@fCalcOrdKey bit=1
as
	declare @Err int, @nRowCnt int
	declare	@sQry nvarchar(1000), @sUid nvarchar(50)
	declare	@nObjId int, @nObjClass int, @nInheritDepth int, @nOwnerDepth int, @nClass int, @nField int,
		@nRelOrder int, @nType int, @nDirection int, @sClass sysname, @sField sysname,
		@sOrderField sysname, @sOrdKey varchar(502)
	declare	@fIsNocountOn int

	set @Err = 0

	CREATE TABLE [#OwnedObjsInfo$](
		[ObjId]		INT NOT NULL,
		[ObjClass]	INT NULL,
		[InheritDepth]	INT NULL DEFAULT(0),
		[OwnerDepth]	INT NULL DEFAULT(0),
		[RelObjId]	INT NULL,
		[RelObjClass]	INT NULL,
		[RelObjField]	INT NULL,
		[RelOrder]	INT NULL,
		[RelType]	INT NULL,
		[OrdKey]	VARBINARY(250) NULL DEFAULT(0))

	CREATE NONCLUSTERED INDEX #OwnedObjsInfoObjId ON #OwnedObjsInfo$ ([ObjId])

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- if null was specified as the mask assume that all objects are desired
	if @grfcpt is null set @grfcpt = 528482304

	-- get the owned objects
	IF (176160768) & @grfcpt > 0 --( mask = owned obects
		INSERT INTO [#OwnedObjsInfo$]
			SELECT * FROM dbo.fnGetOwnedObjects$(
				@ObjIds,
				@grfcpt,
				@fBaseClasses,
				@fSubClasses,
				@fRecurse,
				@riid,
				@fCalcOrdKey)
	ELSE --( mask = referenced items or all: get all owned objects
		INSERT INTO [#OwnedObjsInfo$]
			SELECT * FROM dbo.fnGetOwnedObjects$(
				@ObjIds,
				528482304,
				@fBaseClasses,
				@fSubClasses,
				@fRecurse,
				@riid,
				@fCalcOrdKey)

	IF NOT (352321536) & @grfcpt > 0 --( mask = not referenced. In other words, mask is owned or all
		INSERT INTO [#ObjInfoTbl$]
			SELECT * FROM [#OwnedObjsInfo$]

	-- determine if any references should be included in the results
	if (352321536) & @grfcpt > 0 begin

		--
		-- get a list of all of the classes that reference each class associated with the specified object
		--	and a list of the classes that each associated class reference, then loop through them to
		--	get all of the objects that participate in the references
		--

		-- determine which reference direction should be included
		if @nRefDirection = 0 begin
			declare GetClassRefObj_cur cursor local static forward_only read_only for
			-- get the classes that reference (atomic, sequences, and collections) this class
			select 	oi.[ObjId], oi.[ObjClass], oi.[InheritDepth], oi.[OwnerDepth], f.[Type], c.[Name], c.[Id], f.[Name], f.[Id],
				1, -- references this class
				-- Note the COLLATE statement is required here when using database servers with non-US collation!!
				master.dbo.fn_varbintohexstr(oi.[OrdKey]) COLLATE SQL_Latin1_General_CP1_CI_AS
			from 	#OwnedObjsInfo$ oi
					join [Field$] f on f.[DstCls] = oi.[ObjClass]
						and ( 	( f.[type] = 24 and @grfcpt & 16777216 = 16777216 )
							or ( f.[type] = 26 and @grfcpt & 67108864 = 67108864 )
							or ( f.[type] = 28 and @grfcpt & 268435456 = 268435456 )
						)
					join [Class$] c on f.[Class] = c.[Id]
			union all
			-- get the classes that are referenced (atomic, sequences, and collections) by this class
			select	oi.[ObjId], oi.[ObjClass], oi.[InheritDepth], oi.[OwnerDepth], f.[Type], c.[Name], f.[DstCls], f.[Name], f.[Id],
				2, -- referenced by this class
				-- Note the COLLATE statement is required here when using database servers with non-US collation!!
				master.dbo.fn_varbintohexstr(oi.[OrdKey]) COLLATE SQL_Latin1_General_CP1_CI_AS
			from	#OwnedObjsInfo$ oi
					join [Class$] c on c.[Id] = oi.[ObjClass]
					join [Field$] f on f.[Class] = c.[Id]
						and ( 	( f.[type] = 24 and @grfcpt & 16777216 = 16777216 )
							or ( f.[type] = 26 and @grfcpt & 67108864 = 67108864 )
							or ( f.[type] = 28 and @grfcpt & 268435456 = 268435456 )
						)
			order by oi.[ObjId]
		end
		else if @nRefDirection = 1 begin
			declare GetClassRefObj_cur cursor local static forward_only read_only for
			-- get the classes that are referenced (atomic, sequences, and collections) by these classes
			select	oi.[ObjId], oi.[ObjClass], oi.[InheritDepth], oi.[OwnerDepth], f.[Type], c.[Name], f.[DstCls], f.[Name], f.[Id],
				2, -- referenced by this class
				-- Note the COLLATE statement is required here when using database servers with non-US collation!!
				master.dbo.fn_varbintohexstr(oi.[OrdKey]) COLLATE SQL_Latin1_General_CP1_CI_AS
			from	#OwnedObjsInfo$ oi
					join [Class$] c on c.[Id] = oi.[ObjClass]
					join [Field$] f on f.[Class] = c.[Id]
						and ( 	( f.[type] = 24 and @grfcpt & 16777216 = 16777216 )
							or ( f.[type] = 26 and @grfcpt & 67108864 = 67108864 )
							or ( f.[type] = 28 and @grfcpt & 268435456 = 268435456 )
						)
			order by oi.[ObjId]
		end
		else begin
			declare GetClassRefObj_cur cursor local static forward_only read_only for
			-- get the classes that reference (atomic, sequences, and collections) these classes
			-- do not include internal references between objects within the owning object hierarchy;
			--	this will be handled below
			select 	oi.[ObjId], oi.[ObjClass], oi.[InheritDepth], oi.[OwnerDepth], f.[Type], c.[Name], c.[Id], f.[Name], f.[Id],
				1, -- references this class
				-- Note the COLLATE statement is required here when using database servers with non-US collation!!
				master.dbo.fn_varbintohexstr(oi.[OrdKey]) COLLATE SQL_Latin1_General_CP1_CI_AS
			from 	#OwnedObjsInfo$ oi
					join [Field$] f on f.[DstCls] = oi.[ObjClass]
						and ( 	( f.[type] = 24 and @grfcpt & 16777216 = 16777216 )
							or ( f.[type] = 26 and @grfcpt & 67108864 = 67108864 )
							or ( f.[type] = 28 and @grfcpt & 268435456 = 268435456 )
						)
					join [Class$] c on f.[Class] = c.[Id]
			order by oi.[ObjId]
		end

		open GetClassRefObj_cur
		fetch GetClassRefObj_cur into @nObjId, @nObjClass, @nInheritDepth, @nOwnerDepth, @nType, @sClass, @nClass,
				@sField, @nField, @nDirection, @sOrdKey
		while @@fetch_status = 0 begin

			-- build the base part of the query
			set @sQry = 'insert into #ObjInfoTbl$ '+
					'(ObjId,ObjClass,OwnerDepth,RelObjId,RelObjClass,RelObjField,RelOrder,RelType,OrdKey)' + char(13) +
					'select '

			-- determine if the reference is atomic
			if @nType = 24 begin
				-- determine if this class references an object's class within the object hierachy,
				--	and whether or not it should be included
				if @nDirection = 1 and (@nRefDirection = 0 or @nRefDirection = -1) begin
					set @sQry=@sQry+convert(nvarchar(11), @nObjId)+','+convert(nvarchar(11), @nObjClass)+','+
							convert(nvarchar(11), @nOwnerDepth)+','+
							't.[Id],'+convert(nvarchar(11), @nClass)+','+convert(nvarchar(11), @nField)+','+
							'NULL,'+convert(nvarchar(11), @nType)+',convert(varbinary,'''+@sOrdKey+''') '+
						'from ['+@sClass+'] t '+
						'where ['+@sField+']='+convert(nvarchar(11),@nObjId)

					-- determine if only external references should be included - don't included
					--	references between objects within the owning hierarchy
					if @nRefDirection = -1 begin
						set @sQry = @sQry + 'and not exists (' +
								'select * from #OwnedObjsInfo$ oi ' +
								'where oi.[ObjId]=t.[Id] ' +
									'and oi.[RelType] not in (' +
									convert(nvarchar(11), 16777216)+',' +
									convert(nvarchar(11), 67108864)+',' +
									convert(nvarchar(11), 268435456)+'))'
					end
				end
				-- determine if this class is referenced by an object's class within the object hierachy,
				--	and whether or not it should be included
				else if @nDirection = 2 and (@nRefDirection = 0 or @nRefDirection = 1) begin
					set @sQry=@sQry+'['+@sField+'],'+
							convert(nvarchar(11), @nClass)+','+
							convert(nvarchar(11), @nOwnerDepth)+','+convert(nvarchar(11), @nObjId)+','+
							convert(nvarchar(11), @nObjClass)+','+convert(nvarchar(11), @nField)+','+
							'NULL,'+convert(nvarchar(11), @nType)+',convert(varbinary,'''+@sOrdKey+''') '+
						'from ['+@sClass+'] ' +
						'where [id]='+convert(nvarchar(11),@nObjId)+' '+
							'and ['+@sField+'] is not null'
				end
			end
			else begin
				-- if the reference is ordered insert the order value, otherwise insert null
				if @nType = 28 set @sOrderField = '[Ord]'
				else set @sOrderField = 'NULL'

				-- determine if this class references an object's class and whether or not it should be
				--	included
				if @nDirection = 1 and (@nRefDirection = 0 or @nRefDirection = -1) begin
					set @sQry=@sQry+convert(nvarchar(11), @nObjId)+','+convert(nvarchar(11), @nObjClass)+','+
							convert(nvarchar(11), @nOwnerDepth)+','+
							't.[Src],'+convert(nvarchar(11), @nClass)+','+convert(nvarchar(11), @nField)+','+
							@sOrderField+','+convert(nvarchar(11), @nType)+',convert(varbinary,'''+@sOrdKey+''') '+
						'from ['+@sClass+'_'+@sField+'] t '+
						'where t.[dst]='+convert(nvarchar(11),@nObjId)

					-- determine if only external references should be included - don't included
					--	references between objects within the owning hierarchy
					if @nRefDirection = -1 begin
						set @sQry = @sQry + 'and not exists (' +
								'select * from #OwnedObjsInfo$ oi ' +
								'where oi.[ObjId]=t.[Src] ' +
									'and oi.[RelType] not in (' +
									convert(nvarchar(11), 16777216)+',' +
									convert(nvarchar(11), 67108864)+',' +
									convert(nvarchar(11), 268435456)+'))'
					end
				end
				-- determine if this class is referenced by an object's class and whether or not it
				--	should be included
				else if @nDirection = 2 and (@nRefDirection = 0 or @nRefDirection = 1) begin
					set @sQry=@sQry+'[Dst],'+
							convert(nvarchar(11), @nClass)+','+
							convert(nvarchar(11), @nOwnerDepth)+','+convert(nvarchar(11), @nObjId)+','+
							convert(nvarchar(11), @nObjClass)+','+convert(nvarchar(11), @nField)+','+
							@sOrderField+','+convert(nvarchar(11), @nType)+',convert(varbinary,'''+@sOrdKey+''') '+
						'from ['+@sClass+'_'+@sField+'] '+
						'where [src]='+convert(nvarchar(11),@nObjId)
				end
			end

			exec (@sQry)
			set @Err = @@error
			if @Err <> 0 begin
				raiserror ('GetLinkedObjs$: SQL Error %d; Error performing dynamic SQL.', 16, 1, @Err)

				close GetClassRefObj_cur
				deallocate GetClassRefObj_cur

				goto LFail
			end

			fetch GetClassRefObj_cur into @nObjId, @nObjClass, @nInheritDepth, @nOwnerDepth, @nType, @sClass,
					@nClass, @sField, @nField, @nDirection, @sOrdKey
		end

		close GetClassRefObj_cur
		deallocate GetClassRefObj_cur
	end

	-- if a class was specified remove the owned objects that are not of that type of class; these objects were
	--    necessary in order to get a list of all of the referenced and referencing objects that were potentially
	--    the type of specified class
	if @riid is not null begin
		delete	#ObjInfoTbl$
		where 	not exists (
				select	*
				from	[ClassPar$] cp
				where	cp.[Dst] = @riid
					and cp.[Src] = [ObjClass]
			)
		set @Err = @@error
		if @Err <> 0 begin
			raiserror ('GetLinkedObjs$: SQL Error %d; Unable to remove objects that are not the specified class %d.', 16, 1, @Err, @riid)
			goto LFail
		end
	end

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err

GO

---------------------------------------------------------------------------------

IF OBJECT_ID('CopyObj$') IS NOT NULL BEGIN
	PRINT 'removing procedure CopyObj$'
	DROP PROC [CopyObj$]
END
GO
PRINT 'creating procedure CopyObj$'
GO
CREATE PROCEDURE [CopyObj$]
	@nTopSourceObjId INT,
	@nTopDestOwnerId INT,
	@nTopDestOwnerFlid INT,
	@hvoDstStart INT = NULL,
	@nTopDestObjId INT OUTPUT
AS

	DECLARE
		@nDstStartOrd INT,
		@nMinOrd INT,
		@nSpaceAvail INT,
		@nSpaceNeed INT,
		@nSourceObjId INT,
		@nOwnerID INT,
		@nOwnerFieldId INT, --(owner flid
		@nRelObjId INT,
		@nRelObjFieldId INT,  --( related object flid
		@guidNew UNIQUEIDENTIFIER,
		@nFirstOuterLoop TINYINT,
		@nDestObjId INT,
		@nOwnerDepth INT,
		@nInheritDepth INT,
		@nClass INT,
		@nvcClassName NVARCHAR(100),
		@nvcQuery NVARCHAR(4000),
		@nvcFieldsList NVARCHAR(4000),
		@nvcValuesList NVARCHAR(4000),
		@nColumn INT,
		@sysColumn SYSNAME,
		@nFlid INT,
		@nType INT,
		@nTopDestType INT,
		@nSourceClassId INT,
		@nDestClassId INT,
		@nvcFieldName NVARCHAR(100),
		@nvcTableName NVARCHAR(100),
		@StrId NVARCHAR(20);

	-- Remark these constants for production code. For coding and testing with Query Analyzer,
	-- unremark these constants and put an @ in front of the variables wherever they appear.
	/*
	declare @kcptOwningSequence int
	set @kcptOwningSequence = 27
	*/

	SET @nFirstOuterLoop = 1
	SET @nOwnerId = @nTopDestOwnerId
	SET @nOwnerFieldId = @nTopDestOwnerFlid

	SELECT @nTopDestType = Type FROM Field$ WHERE Id = @nTopDestOwnerFlid

	IF (@nTopDestType != 27 AND @hvoDstStart IS NOT NULL) BEGIN
		RAISERROR('hvoDstStart must be NULL for objects that are not in an owning sequence: Object ID=%d, Dest Owner ID=%d, Dest Owner Flid=%d, Dest Start Obj=%d', 16, 1,
			@nTopSourceObjId, @nTopDestOwnerId,	@nTopDestOwnerFlid, @hvoDstStart)
		GOTO LFail
	END
	IF (@nTopDestType = 27) BEGIN

		IF @hvoDstStart is null BEGIN
			SELECT	@nDstStartOrd = coalesce(max([OwnOrd$]), -1) + 1
			FROM	CmObject
			WHERE	[Owner$] = @nTopDestOwnerId
				and [OwnFlid$] = @nTopDestOwnerFlid
		END
		ELSE BEGIN
			SELECT	@nDstStartOrd = [OwnOrd$]
			FROM	CmObject
			WHERE	[Owner$] = @nTopDestOwnerId
				and [OwnFlid$] = @nTopDestOwnerFlid
				and [Id] = @hvoDstStart

			IF @nDstStartOrd is null BEGIN
				RAISERROR('If hvoDstStart is not NULL, it must be an existing object in the destination sequence: Object ID=%d, Dest Owner ID=%d, Dest Owner Flid=%d, Dest Start Obj=%d', 16, 1,
					@nTopSourceObjId, @nTopDestOwnerId,	@nTopDestOwnerFlid, @hvoDstStart)
				GOTO LFail
			END

			-- If the objects are not appended to the end of the destination list then determine if there is enough room

			-- Find the object with the largest ordinal value less than the destination start object's ordinal
			SELECT @nMinOrd = coalesce(max([OwnOrd$]), -1)
			FROM	CmObject
			WHERE	[Owner$] = @nTopDestOwnerId
				and [OwnFlid$] = @nTopDestOwnerFlid
				and [OwnOrd$] < @nDstStartOrd

			SET @nSpaceAvail = @nDstStartOrd - @nMinOrd - 1
			SET @nSpaceNeed = 1 -- Using a variable rather than hard-coding the 1 to make it more self-documenting and to prepare the way for future enhancement to copy multiple objects

			-- see if there is currently enough room for the objects under the destination object's sequence list;
			--	if there is not then make room
			IF @nSpaceAvail < @nSpaceNeed BEGIN
				UPDATE	CmObject
				SET	[OwnOrd$] = [OwnOrd$] + @nSpaceNeed - @nSpaceAvail
				WHERE	[Owner$] = @nTopDestOwnerId
					and [OwnFlid$] = @nTopDestOwnerFlid
					and [OwnOrd$] >= @nDstStartOrd
			END

			-- We can now stick the (first -- only one for now) copied object anwhere in the gap bounded
			-- (inclusively) at the bottom by (@nMinOrd + 1) and at the top by
			-- (@nMinOrd + 1 + MAX(0, (@nSpaceAvail - @nSpaceNeed))). We'll stick it at the lowest position
			-- because a) the calculation is much easier and b) it's probably slightly more likely that a
			-- subsequent copy will want to insert something else after this thing we're inserting
			SET @nDstStartOrd = @nMinOrd + 1
		END
	END

	CREATE TABLE #SourceObjs (
		[ObjId]			INT		not null,
		[ObjClass]		INT		null,
		[InheritDepth]	INT		null		DEFAULT(0),
		[OwnerDepth]	INT		null		DEFAULT(0),
		[RelObjId]		INT		null,
		[RelObjClass]	INT		null,
		[RelObjField]	INT		null,
		[RelOrder]		INT		null,
		[RelType]		INT		null,
		[OrdKey]		VARBINARY(250)	null	DEFAULT(0),
		[ClassName]		NVARCHAR(100),
		[DestinationID]	INT		NULL)

	--== Get the Object Tree ==--

	SET @StrId = CAST(@nTopSourceObjId AS NVARCHAR(20));

	INSERT INTO #SourceObjs
		SELECT oo.*, c.[Name], NULL
		FROM dbo.fnGetOwnedObjects$(@StrId, NULL, 1, 0, 1, null, 1) oo
		JOIN Class$ c ON c.[Id] = oo.[ObjClass]
		WHERE oo.[ObjClass] <> 0 -- Have to block CmObject rows

	--== Create CmObject Records ==--

	--( Create all the CmObjects for all the objects copied

	DECLARE curNewCmObjects CURSOR FAST_FORWARD FOR
		SELECT  MAX([ObjId]), MAX([RelObjId]), MAX([RelObjField])
		FROM #SourceObjs
		GROUP BY [ObjId], [RelObjId], [RelObjField]
		ORDER BY MIN([OwnerDepth]), MAX([InheritDepth]) DESC

	OPEN curNewCmObjects
	FETCH curNewCmObjects INTO @nSourceObjId, @nRelObjId, @nRelObjFieldId
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @guidNew = NEWID()

	-- First (root) object gets OwnOrd$ computed above, others copy from destination.
		IF @nFirstOuterLoop = 1
			INSERT INTO CmObject WITH (ROWLOCK) ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
				SELECT @guidNew, [Class$], @nOwnerId, @nOwnerFieldId, @nDstStartOrd
				FROM CmObject
				WHERE [Id] = @nSourceObjId
		ELSE
			INSERT INTO CmObject WITH (ROWLOCK) ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
				SELECT @guidNew, [Class$], @nOwnerId, @nOwnerFieldId, [OwnOrd$]
				FROM CmObject
				WHERE [Id] = @nSourceObjId


		SET @nDestObjId = @@IDENTITY

		UPDATE #SourceObjs SET [DestinationID] = @nDestObjId WHERE [ObjId] = @nSourceObjID

		--== Copy records in "multi" string tables ==--

		--( Tables of the former MultiTxt$ table are now set in a separate loop.

		INSERT INTO MultiStr$ WITH (ROWLOCK) ([Flid], [Obj], [Ws], [Txt], [Fmt])
		SELECT [Flid], @nDestObjID, [WS], [Txt], [Fmt]
		FROM MultiStr$
		WHERE [Obj] = @nSourceObjId

		--( As of this writing, MultiBigTxt$ is not used anywhere in the conceptual
		--( model, and it is impossible to use it through the interface.

		INSERT INTO MultiBigTxt$ WITH (ROWLOCK) ([Flid], [Obj], [Ws], [Txt])
		SELECT [Flid], @nDestObjID, [WS], [Txt]
		FROM MultiBigTxt$
		WHERE [Obj] = @nSourceObjId

		INSERT INTO MultiBigStr$ WITH (ROWLOCK) ([Flid], [Obj], [Ws], [Txt], [Fmt])
		SELECT [Flid], @nDestObjID, [WS], [Txt], [Fmt]
		FROM MultiBigStr$
		WHERE [Obj] = @nSourceObjId

		--( If the object ID = the top owning object ID
		IF @nFirstOuterLoop = 1
			SET @nTopDestObjId = @nDestObjID --( sets the output parameter

		SET @nFirstOuterLoop = 0

		--( This fetch is different than the one at the top of the loop!
		FETCH curNewCmObjects INTO @nSourceObjId, @nRelObjId, @nOwnerFieldId

		SELECT @nOwnerId = [DestinationId] FROM #SourceObjs WHERE ObjId = @nRelObjId
	END
	CLOSE curNewCmObjects
	DEALLOCATE curNewCmObjects

	--== Create All Other Records ==--

	DECLARE curRecs CURSOR FAST_FORWARD FOR
		SELECT DISTINCT [OwnerDepth], [InheritDepth], [ObjClass], [ClassName]
		FROM #SourceObjs
		ORDER BY [OwnerDepth], [InheritDepth] DESC, [ObjClass]

	OPEN curRecs
	FETCH curRecs INTO 	@nOwnerDepth, @nInheritDepth, @nClass, @nvcClassName
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @nvcFieldsList = N''
		SET @nvcValuesList = N''
		SET @nColumn = 1
		SET @sysColumn = COL_NAME(OBJECT_ID(@nvcClassName), @nColumn)
		WHILE @sysColumn IS NOT NULL BEGIN
			IF @nColumn > 1 BEGIN --( not the first time in loop
				SET @nvcFieldsList = @nvcFieldsList + N', '
				SET @nvcValuesList = @nvcValuesList + N', '
			END
			SET @nvcFieldName = N'[' + UPPER(@sysColumn) + N']'

			--( Field list for insert
			SET @nvcFieldsList = @nvcFieldsList + @nvcFieldName

			--( Vales to put into those fields
			IF @nvcFieldName = '[ID]'
				SET @nvcValuesList = @nvcValuesList + N' so.[DestinationId] '
			ELSE IF @nvcFieldName = N'[DATECREATED]' OR @nvcFieldName = N'[DATEMODIFIED]'
				SET @nvcValuesList = @nvcValuesList + N'CURRENT_TIMESTAMP'
			ELSE
				SET @nvcValuesList = @nvcValuesList + @nvcFieldName

			SET @nColumn = @nColumn + 1
			SET @sysColumn = COL_NAME(OBJECT_ID(@nvcClassName), @nColumn)
		END

		SET @nvcQuery =
			N'INSERT INTO ' + @nvcClassName + ' WITH (ROWLOCK) (
				' + @nvcFieldsList + ')
			SELECT ' + @nvcValuesList + N'
			FROM ' + @nvcClassName + ' cn
			JOIN #SourceObjs so ON
				so.[OwnerDepth] = ' + STR(@nOwnerDepth) + N' AND
				so.[InheritDepth] = ' + STR(@nInheritDepth) + N' AND
				so.[ObjClass] = ' + STR(@nClass) + N' AND
				so.[ObjId] = cn.[Id]'

		EXEC (@nvcQuery)

		--== Copy References ==--

		DECLARE curReferences CURSOR LOCAL STATIC FORWARD_ONLY READ_ONLY FOR
		SELECT f.[Id], f.[Type], f.[Name]
		FROM Field$ f
		WHERE f.[Class] = @nClass AND
			(f.[Type] = 26 OR f.[Type] = 28)

		OPEN curReferences
		FETCH curReferences INTO @nFlid, @nType, @nvcFieldName
		WHILE @@FETCH_STATUS = 0 BEGIN

			SET @nvcQuery = N'INSERT INTO ' + @nvcClassName + N'_' + @nvcFieldName + N' ([Src], [Dst]'

			IF @nType = 26
				SET @nvcQuery = @nvcQuery + N')
					SELECT DestinationId, r.[Dst]
					FROM '
			ELSE --( IF @nType = 28
				SET @nvcQuery = @nvcQuery + N', [Ord])
					SELECT DestinationId, r.[Dst], r.[Ord]
					FROM '

				SET @nvcQuery = @nvcQuery + @nvcClassName + N'_' + @nvcFieldName + N' r
					JOIN #SourceObjs ON
						[OwnerDepth] = @nOwnerDepth AND
						[InheritDepth] =  @nInheritDepth AND
						[ObjClass] = @nClass AND
						[ObjId] = r.[Src]'

				EXEC sp_executesql @nvcQuery,
					N'@nOwnerDepth INT, @nInheritDepth INT, @nClass INT',
					@nOwnerDepth, @nInheritDepth, @nClass

			FETCH curReferences INTO @nFlid, @nType, @nvcFieldName
		END
		CLOSE curReferences
		DEALLOCATE curReferences

		FETCH curRecs INTO 	@nOwnerDepth, @nInheritDepth, @nClass, @nvcClassName
	END
	CLOSE curRecs
	DEALLOCATE curRecs

	--== References to Copied Objects ==--

	--( objects can point to themselves. For instance, the People list has a
		--( researcher that points back to the People list. For copies, this reference
		--( back to itself needs to have the new object, not the old source object. For
		--( all other reference properties, the old object will do fine.

	DECLARE curRefs2CopiedObjs CURSOR LOCAL STATIC FORWARD_ONLY READ_ONLY FOR
		SELECT DISTINCT c.[Name] + N'_' + f.[Name]
		FROM Class$ c
		JOIN Field$ f ON f.[Class] = c.[Id]
		JOIN #SourceObjs s ON s.[ObjClass] = c.[Id]
		WHERE f.[Type] = 26 OR f.[Type] = 28

	OPEN curRefs2CopiedObjs
	FETCH curRefs2CopiedObjs INTO @nvcTableName
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @nvcQuery = N'UPDATE ' + @nvcTableName + N'
			SET [Dst] = #SourceObjs.[DestinationId]
			FROM ' + @nvcTableName + N', #SourceObjs
			WHERE ' + @nvcTableName + N'.[Dst] = #SourceObjs.[ObjId]'

		EXEC sp_executesql @nvcQuery

		FETCH curRefs2CopiedObjs INTO @nvcTableName
	END
	CLOSE curRefs2CopiedObjs
	DEALLOCATE curRefs2CopiedObjs

	--== MultiTxt$ Records ==--

	DECLARE curMultiTxt CURSOR FAST_FORWARD FOR
		SELECT DISTINCT ObjId, ObjClass, ClassName, DestinationId
		FROM #SourceObjs

	--( First get the object IDs and their class we're working with
	OPEN curMultiTxt
	FETCH curMultiTxt INTO @nSourceObjId, @nClass, @nvcClassName, @nDestObjId
	WHILE @@FETCH_STATUS = 0 BEGIN

		--( Now copy into each of the multitxt fields
		SELECT TOP 1 @nFlid = [Id], @nvcFieldName = [Name]
		FROM Field$
		WHERE Class = @nClass AND Type = 16
		ORDER BY [Id]

		WHILE @@ROWCOUNT > 0 BEGIN
			SET @nvcQuery =
				N'INSERT INTO ' + @nvcClassName + N'_' + @nvcFieldName + N' ' +
				N'WITH (ROWLOCK) (Obj, Ws, Txt)' + CHAR(13) +
				CHAR(9) + N'SELECT @nDestObjId, WS, Txt' + CHAR(13) +
				CHAR(9) + N'FROM ' + @nvcClassName + N'_' + @nvcFieldName + CHAR(13) +
				CHAR(9) + N'WHERE Obj = @nSourceObjId'

			EXECUTE sp_executesql @nvcQuery,
				N'@nDestObjId INT, @nSourceObjId INT',
				@nDestObjID, @nSourceObjId

			SELECT TOP 1 @nFlid = [Id], @nvcFieldName = [Name]
			FROM Field$
			WHERE [Id] > @nFlid AND Class = @nClass AND Type = 16
			ORDER BY [Id]
		END

		FETCH curMultiTxt INTO @nSourceObjId, @nClass, @nvcClassName, @nDestObjId
	END
	CLOSE curMultiTxt
	DEALLOCATE curMultiTxt

	DROP TABLE #SourceObjs

LFail:

GO

---------------------------------------------------------------------------------

--( CreateGetRefsToObj had only a change in the header comments.

---------------------------------------------------------------------------------


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
	declare
		@uid uniqueidentifier,
		@retval int,
		@StrId NVARCHAR(20);

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

	SET @StrId = CAST(@ObjId AS NVARCHAR(20));

	insert into @tblObjInfo
		select * from fnGetOwnedObjects$(@StrId, 176160768, 0, 0, 1, 7, 0)

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
			from LangProject_CurAnalysisWss caws
			join LgWritingSystem lws on caws.dst = lws.id
			order by caws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LangProject_AnalysisWss caws
			join LgWritingSystem lws on caws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	--( 0xfffffffe (-2) or 0xfffffffc (-4) = First string from a) ordered checked vernacular
	-- writing systems b) any remaining vernacular writing systems or stars if none of the above.
	else if @Ws = 0xfffffffe or @Ws = 0xfffffffc begin
		insert into @tblWs (WsId)
			select lws.id
			from LangProject_CurVernWss cvws
			join LgWritingSystem lws on cvws.dst = lws.id
			order by cvws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LangProject_VernWss cvws
			join LgWritingSystem lws on cvws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	--( 0xfffffffb = -5 = First string from a) ordered checked analysis writing systems
	-- b) ordered checked vernacular writing systems, c) any remaining analysis writing systems,
	-- d) any remaining vernacular writing systems or stars if none of the above.
	else if @Ws = 0xfffffffb begin
		insert into @tblWs (WsId)
			select lws.id
			from LangProject_CurAnalysisWss caws
			join LgWritingSystem lws on caws.dst = lws.id
			order by caws.Ord
		insert into @tblWs (WsId)
			select lws.id
			from LangProject_CurVernWss cvws
			join LgWritingSystem lws on cvws.dst = lws.id
			where lws.id not in (select WsId from @tblWs)
			order by cvws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LangProject_AnalysisWss caws
			join LgWritingSystem lws on caws.dst = lws.id and lws.id not in (select WsId from @tblWs)
		insert into @tblWs (WsId)
			select distinct lws.id
			from LangProject_VernWss cvws
			join LgWritingSystem lws on cvws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	--( 0xfffffffa = -6 = First string from a) ordered checked vernacular writing systems
	-- b) ordered checked analysis writing systems, c) any remaining vernacular writing systems,
	-- d) any remaining analysis writing systems or stars if none of the above.
	else if @Ws = 0xfffffffa begin
		insert into @tblWs (WsId)
			select lws.id
			from LangProject_CurVernWss cvws
			join LgWritingSystem lws on cvws.dst = lws.id
			order by cvws.Ord
		insert into @tblWs (WsId)
			select lws.id
			from LangProject_CurAnalysisWss caws
			join LgWritingSystem lws on caws.dst = lws.id
			where lws.id not in (select WsId from @tblWs)
			order by caws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LangProject_VernWss cvws
			join LgWritingSystem lws on cvws.dst = lws.id and lws.id not in (select WsId from @tblWs)
		insert into @tblWs (WsId)
			select distinct lws.id
			from LangProject_AnalysisWss caws
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

---------------------------------------------------------------------------------

IF OBJECT_ID('GetUndoDelObjInfo') is not null BEGIN
	PRINT 'removing proc GetUndoDelObjInfo'
	DROP PROC GetUndoDelObjInfo
END
GO
PRINT 'creating proc GetUndoDelObjInfo'
GO
CREATE PROC GetUndoDelObjInfo
	@ObjIds NVARCHAR(MAX)
AS
	-- make sure that nocount is turned on
	DECLARE @fIsNocountOn INT
	SET @fIsNocountOn = @@options & 512
	IF @fIsNocountOn = 0 SET NOCOUNT ON

	CREATE TABLE  #ObjInfoForDelete (
		ObjId			INT NOT NULL,
		ObjClass		INT NULL,
		InheritDepth	INT NULL DEFAULT(0),
		OwnerDepth		INT NULL DEFAULT(0),
		Owner			INT NULL,
		OwnerClass		INT NULL,
		OwnFlid			INT NULL,
		OwnOrd			INT NULL,
		OwnPropType		INT NULL,
		OrdKey			VARBINARY(250) NULL DEFAULT(0))
	CREATE NONCLUSTERED INDEX #Ind_ObjInfo_ObjId ON dbo.#ObjInfoForDelete (ObjId)

	INSERT INTO #ObjInfoForDelete
	SELECT * FROM dbo.fnGetOwnedObjects$(
		@ObjIds,		-- list of object ids
		null,			-- we want all owning prop types
		1,				-- we want base class records
		0,				-- but not subclasses
		1,				-- we want recursion (all owned, not just direct)
		null,			-- we want objects of any class
		0)				-- we don't need an 'order key'

	DECLARE @PropType INT
	SET @PropType = 1
	DECLARE @ClassName NVARCHAR(100)
	DECLARE @FieldName NVARCHAR(100)
	DECLARE @Flid INT

	DECLARE @props TABLE(type INT, flid INT)
	INSERT INTO @props
	SELECT DISTINCT f.type, f.id
	FROM Field$ f
	JOIN #ObjInfoForDelete oo ON oo.ObjClass = f.class

	DECLARE @sQry NVARCHAR(4000)
	DECLARE @sPropType NVARCHAR(20)
	DECLARE @sFlid NVARCHAR(20)

	SELECT TOP 1 @flid = flid, @PropType = type FROM @props ORDER BY flid
	WHILE @@rowcount > 0
	BEGIN
		SELECT @FieldName = f.Name, @Flid = f.Id, @ClassName = c.Name
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.Class
		WHERE f.Type = @PropType AND f.Id = @Flid
		SET @sPropType = CONVERT(NVARCHAR(20), @PropType)
		SET @sFlid = CONVERT(NVARCHAR(20), @Flid)

		SET @sQry = 'insert into #UndoDelObjInfo select ' + @sPropType + ',Id,' + @sFlid + ', '
		IF @PropType in (1,2,8,24) BEGIN	-- Boolean, Integer, GenDate, RefAtomic
			SET @sQry = @sQry +
				'[' + @FieldName + '], null, null, null, null, null ' +
				'from ' + @ClassName + ' where Id in (select ObjId from #ObjInfoForDelete) ' +
				'and [' + @FieldName + '] is not null'
		END
		-- 3 (Numeric) and 4 (Float) are never used (as of January 2005)
		ELSE IF @PropType = 5 BEGIN		-- Time
			SET @sQry = @sQry +
				'null, [' + @FieldName + '], null, null, null, null ' +
				'from ' + @ClassName + ' where Id in (select ObjId from #ObjInfoForDelete) ' +
				'and [' + @FieldName + '] is not null'
		END
		ELSE IF @PropType = 6 BEGIN		-- Guid
			SET @sQry = @sQry +
				'null, null, [' + @FieldName + '], null, null, null ' +
				'from ' + @ClassName + ' where Id in (select ObjId from #ObjInfoForDelete) ' +
				'and [' + @FieldName + '] is not null'
		END
		ELSE IF @PropType in (7,9) BEGIN	-- Image, Binary
			SET @sQry = @sQry +
				'null, null, null, [' + @FieldName + '], null, null ' +
				'from ' + @ClassName + ' where Id in (select ObjId from #ObjInfoForDelete) ' +
				'and [' + @FieldName + '] is not null'
		END
		-- 10,11,12 are unassigned values (as of January 2005)
		ELSE IF @PropType in (13,17) BEGIN		-- String, BigString
			SET @sQry = @sQry + 'null, null, null, ' +
				@FieldName + '_Fmt, [' + @FieldName + '], null ' +
				'from ' + @ClassName + ' where Id in (select ObjId from #ObjInfoForDelete) ' +
				'and [' + @FieldName + '] is not null'
		END
		ELSE IF @PropType in (14,18) BEGIN		-- MultiString, MultiBigString
			SET @sQry = 'insert into #UndoDelObjInfo select ' + @sPropType + ',Obj,' + @sFlid +
				', Ws, null, null, Fmt, Txt, null ' +
				'from ' + @ClassName + '_' + @FieldName +
				' where Obj in (select ObjId from #ObjInfoForDelete) and Txt is not null'
		END
		ELSE IF @PropType in (15,19) BEGIN		-- Unicode, BigUnicode
			SET @sQry = @sQry +
				'null, null, null, null, [' + @FieldName + '], null ' +
				'from ' + @ClassName + ' where Id in (select ObjId from #ObjInfoForDelete) ' +
				'and [' + @FieldName + '] is not null'
		END
		ELSE IF @PropType in (16,20) BEGIN		-- MultiUnicode, MultiBigUnicode
			-- (MultiBigUnicode is unused as of January 2005)
			SET @sQry = 'insert into #UndoDelObjInfo select ' + @sPropType + ',Obj,' + @sFlid +
				', Ws, null, null, null, Txt, null ' +
				'from ' + @ClassName + '_' + @FieldName +
				' where Obj in (select ObjId from #ObjInfoForDelete) and Txt is not null'
		END
		-- 21,22 are unassigned (as of January 2005)
		-- 23,25,27 are Owning Properties, which are handled differently from Value/Reference
		--          Properties
		ELSE IF @PropType = 26 BEGIN		-- RefCollection
			SET @sQry = 'insert into #UndoDelObjInfo select ' + @sPropType + ',Src,' + @sFlid +
				', Dst, null, null, null, null, null ' +
				'from ' + @ClassName + '_' + @FieldName +
				' where Src in (select ObjId from #ObjInfoForDelete)'
		END
		ELSE IF @PropType = 28 BEGIN		-- RefSequence
			SET @sQry = 'insert into #UndoDelObjInfo select ' + @sPropType + ',Src,' + @sFlid +
				', Dst, null, null, null, null, Ord ' +
				'from ' + @ClassName + '_' + @FieldName +
				' where Src in (select ObjId from #ObjInfoForDelete) order by Ord'
		END
		ELSE BEGIN
			SET @sQry = null
		END
		IF (@sQry is not null) BEGIN
		--	PRINT @sQry
			EXEC (@sQry)
		END
		SELECT TOP 1 @flid = flid, @PropType = type FROM @props WHERE flid > @flid ORDER BY flid
	END

	-- Now do incoming references. Note that we exclude references where the SOURCE of the
	-- reference is in the deleted object collection, as those references will be reinstated
	-- by code restoring the forward ref properties.  Incoming references are marked in the
	-- table by negative values in the Type field.

	DELETE FROM @props
	INSERT INTO @props
	SELECT DISTINCT f.type, f.id
	FROM Field$ f
	JOIN #ObjInfoForDelete oo ON oo.ObjClass = f.DstCls AND f.type IN (24, 26, 28)

	SELECT TOP 1 @flid = flid, @PropType = type FROM @props ORDER BY flid
	WHILE @@rowcount > 0
	BEGIN
		SELECT @FieldName = f.Name, @Flid = f.Id, @ClassName = c.Name
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.Class
		WHERE f.Type = @PropType AND f.Id = @Flid
		SET @sPropType = CONVERT(NVARCHAR(20), @PropType)
		SET @sFlid = CONVERT(NVARCHAR(20), @Flid)

		IF @PropType = 24 BEGIN				-- RefAtomic
			SET @sQry = 'insert into #UndoDelObjInfo select -' + @sPropType +
				', [' + @FieldName + '], ' + @sFlid +
				', Id, null, null, null, null, null ' +
				'from ' + @ClassName +
				' where [' + @FieldName + '] in (select ObjId from #ObjInfoForDelete)' +
				' and Id not in (select ObjId from #ObjInfoForDelete)'
		END
		ELSE IF @PropType = 26 BEGIN		-- RefCollection
			SET @sQry = 'insert into #UndoDelObjInfo select -' + @sPropType + ',Dst,' + @sFlid +
				', Src, null, null, null, null, null ' +
				'from ' + @ClassName + '_' + @FieldName +
				' where Dst in (select ObjId from #ObjInfoForDelete)' +
				' and Src not in (select ObjId from #ObjInfoForDelete)' +
				' order by Src'
		END
		ELSE IF @PropType = 28 BEGIN		-- RefSequence
			SET @sQry = 'insert into #UndoDelObjInfo select -' + @sPropType + ',Dst,' + @sFlid +
				', Src, null, null, null, null, Ord ' +
				'from ' + @ClassName + '_' + @FieldName +
				' where Dst in (select ObjId from #ObjInfoForDelete)' +
				' and Src not in (select ObjId from #ObjInfoForDelete)' +
				' order by Src, Ord'
		END
		ELSE BEGIN
			SET @sQry = null
		END
		IF (@sQry is not null) BEGIN
		--	PRINT @sQry
			EXEC (@sQry)
		END
		SELECT TOP 1 @flid = flid, @PropType = type FROM @props WHERE flid > @flid ORDER BY flid
	END

	-- if we turned on nocount, turn it off
	IF @fIsNocountOn = 0 SET NOCOUNT OFF

	RETURN @@error
GO

---------------------------------------------------------------------------------

if exists (select * from sysobjects where name = 'GetOrderedMultiTxtXML$')
	print 'removing procedure GetOrderedMultiTxtXML$'
	drop proc GetOrderedMultiTxtXML$
go

---------------------------------------------------------------------------------

if exists (select * from sysobjects where name = 'GetOrderedMultiTxt')
	print 'removing procedure GetOrderedMultiTxt'
	drop proc GetOrderedMultiTxt
go
print 'creating proc GetOrderedMultiTxt'
go

create proc GetOrderedMultiTxt
	@ObjIds NVARCHAR(MAX),
	@flid int,
	@anal tinyint = 1
as

	declare @fIsNocountOn int
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	declare
		@iFieldType int,
		@nvcTable NVARCHAR(60),
		@nvcSql NVARCHAR(4000)

	select @iFieldType = [Type] from Field$ where [Id] = @flid
	EXEC GetMultiTableName @flid, @nvcTable OUTPUT

	--== Analysis WritingSystems ==--

	if @anal = 1
	begin

		-- MultiStr$ --
		if @iFieldType = 14 --( kcptMultiString
			select
				isnull(ms.[txt], '***') txt,
				ms.[ws],
				isnull(lpcae.[ord], 99998) [ord]
			FROM fnGetIdsFromString(@ObjIds) i
			JOIN MultiStr$ ms ON ms.Flid = @Flid AND ms.Obj = i.Id
			left outer join LgWritingSystem le on le.[Id] = ms.[ws]
			left outer join LangProject_AnalysisWss lpae on lpae.[dst] = le.[id]
			left outer join LangProject_CurAnalysisWss lpcae on lpcae.[dst] = lpae.[dst]
			union all
			select '***', 0, 99999
			order by isnull([ord], 99998)

		-- MultiBigStr$ --
		else if @iFieldType = 18 --( kcptMultiBigString
		begin
			--( See note 2 in the header
			declare @tblMultiBigStrAnalysis table (
				--( See the notes under string tables in FwCore.sql about the
				--( COLLATE clause.
				Txt NTEXT COLLATE Latin1_General_BIN,
				[ws] int,
				[ord] int primary key)

			insert into @tblMultiBigStrAnalysis
			select
				isnull(mbs.[txt], '***') txt,
				mbs.[ws],
				isnull(lpcae.[ord], 99998) [ord]
			FROM fnGetIdsFromString(@ObjIds) i
			JOIN MultiBigStr$ mbs ON mbs.Flid = @Flid AND mbs.Obj = i.Id
			left outer join LgWritingSystem le on le.[Id] = mbs.[ws]
			left outer join LangProject_AnalysisWss lpae on lpae.[dst] = le.[id]
			left outer join LangProject_CurAnalysisWss lpcae on lpcae.[dst] = lpae.[dst]
			order by isnull([ord], 99998)

			insert into @tblMultiBigStrAnalysis
			select convert(ntext, '***') [txt], 0 [ws], 99999 [ord]

			select * from @tblMultiBigStrAnalysis order by [ord]
		end

		-- MultiBigTxt$ --
		else if @iFieldType = 20 --( kcptMultiBigUnicode
		begin
			--( See note 2 in the header
			declare @tblMultiBigTxtAnalysis table (
				--( See the notes under string tables in FwCore.sql about the
				--( COLLATE clause.
				Txt NTEXT COLLATE Latin1_General_BIN,
				[ws] int,
				[ord] int primary key)

			insert into @tblMultiBigTxtAnalysis
			select
				isnull(mbt.[txt], '***') txt,
				mbt.[ws],
				isnull(lpcae.[ord], 99998) [ord]
			FROM fnGetIdsFromString(@ObjIds) i
			JOIN MultiBigTxt$ mbt ON mbt.Flid = @Flid AND mbt.Obj = i.Id
			left outer join LgWritingSystem le on le.[Id] = mbt.[ws]
			left outer join LangProject_AnalysisWss lpae on lpae.[dst] = le.[id]
			left outer join LangProject_CurAnalysisWss lpcae on lpcae.[dst] = lpae.[dst]
			order by isnull([ord], 99998)

			insert into @tblMultiBigTxtAnalysis
			select convert(ntext, '***') [txt], 0 [ws], 99999 [ord]

			select * from @tblMultiBigTxtAnalysis order by [ord]
		end

		-- MultiTxt$ --
		else if @iFieldType = 16 BEGIN  --( kcptMultiUnicode
			SET @nvcSql =
				N'select ' + CHAR(13) +
					N'isnull(mt.[txt], ''***'') txt, ' + CHAR(13) +
					N'mt.[ws], ' + CHAR(13) +
					N'isnull(lpcae.[ord], 99998) [ord] ' + CHAR(13) +
				N'FROM fnGetIdsFromString(''' + @ObjIds + N''') i ' + CHAR(13) +
				N'JOIN ' + @nvcTable + ' mt ON mt.Obj = i.Id ' + + CHAR(13) +
				N'left outer join LgWritingSystem le on le.[Id] = mt.[ws] ' + CHAR(13) +
				N'left outer join LangProject_AnalysisWss lpae ' +
					N'on lpae.[dst] = le.[id] ' + CHAR(13) +
				N'left outer join LangProject_CurAnalysisWss lpcae ' +
					N'on lpcae.[dst] = lpae.[dst] ' + CHAR(13) +
				N'union all ' + CHAR(13) +
				N'select ''***'', 0, 99999 ' + CHAR(13) +
				N'order by isnull([ord], 99998) '

			EXECUTE (@nvcSql);
		END

	end

	--== Vernacular WritingSystems ==--

	else if @anal = 0
	begin

		-- MultiStr$ --
		if @iFieldType = 14 --( kcptMultiString
			select
				isnull(ms.[txt], '***') txt,
				ms.[ws],
				isnull(lpcve.[ord], 99998) [ord]
			FROM fnGetIdsFromString(@ObjIds) i
			JOIN MultiStr$ ms ON ms.Flid = @Flid AND ms.Obj = i.Id
			left outer join LgWritingSystem le on le.[Id] = ms.[ws]
			left outer join LangProject_VernWss lpve on lpve.[dst] = le.[id]
			left outer join LangProject_CurVernWss lpcve on lpcve.[dst] = lpve.[dst]
			union all
			select '***', 0, 99999
			order by isnull([ord], 99998)

		-- MultiBigStr$ --
		else if @iFieldType = 18 --( kcptMultiBigString
		begin
			--( See note 2 in the header
			declare @tblMultiBigStrVernacular table (
				--( See the notes under string tables in FwCore.sql about the
				--( COLLATE clause.
				Txt NTEXT COLLATE Latin1_General_BIN,
				[ws] int,
				[ord] int primary key)

			insert into @tblMultiBigStrVernacular
			select
				isnull(mbs.[txt], '***') txt,
				mbs.[ws],
				isnull(lpcve.[ord], 99998) [ord]
			FROM fnGetIdsFromString(@ObjIds) i
			JOIN MultiBigStr$ mbs ON mbs.Flid = @Flid AND mbs.Obj = i.Id
			left outer join LgWritingSystem le on le.[Id] = mbs.[ws]
			left outer join LangProject_VernWss lpve on lpve.[dst] = le.[id]
			left outer join LangProject_CurVernWss lpcve on lpcve.[dst] = lpve.[dst]
			order by isnull([ord], 99998)

			insert into @tblMultiBigStrVernacular
			select convert(ntext, '***') [txt], 0 [ws], 99999 [ord]

			select * from @tblMultiBigStrVernacular order by [ord]
		end

		-- MultiBigTxt$ --
		else if @iFieldType = 20 --( kcptMultiBigUnicode
		begin
			--( See note 2 in the header
			declare @tblMultiBigTxtVernacular table (
				--( See the notes under string tables in FwCore.sql about the
				--( COLLATE clause.
				Txt NTEXT COLLATE Latin1_General_BIN,
				[ws] int,
				[ord] int primary key)

			insert into @tblMultiBigTxtVernacular
			select
				isnull(mbt.[txt], '***') txt,
				mbt.[ws],
				isnull(lpcve.[ord], 99998) [ord]
			FROM fnGetIdsFromString(@ObjIds) i
			JOIN MultiBigTxt$ mbt ON mbt.Flid = @Flid AND mbt.Obj = i.Id
			left outer join LgWritingSystem le on le.[Id] = mbt.[ws]
			left outer join LangProject_VernWss lpve on lpve.[dst] = le.[id]
			left outer join LangProject_CurVernWss lpcve on lpcve.[dst] = lpve.[dst]
			order by isnull([ord], 99998)

			insert into @tblMultiBigTxtVernacular
			select convert(ntext, '***') [txt], 0 [ws], 99999 [ord]

			select * from @tblMultiBigTxtVernacular order by [ord]
		end

		-- MultiTxt$ --
		else if @iFieldType = 16 BEGIN --( kcptMultiUnicode
			SET @nvcSql =
				N' select ' + CHAR(13) +
					N'isnull(mt.[txt], ''***'') txt, ' + CHAR(13) +
					N'mt.[ws], ' + CHAR(13) +
					N'isnull(lpcve.[ord], 99998) ord ' + CHAR(13) +
				N'FROM fnGetIdsFromString(''' + @ObjIds + N''') i ' + CHAR(13) +
				N'JOIN ' + @nvcTable + ' mt ON mt.Obj = i.Id ' + + CHAR(13) +
				N'left outer join LgWritingSystem le on le.[Id] = mt.[ws] ' + CHAR(13) +
				N'left outer join LangProject_VernWss lpve ' +
					N'on lpve.[dst] = le.[id] ' + CHAR(13) +
				N'left outer join LangProject_CurVernWss lpcve ' +
					N'on lpcve.[dst] = lpve.[dst] ' + CHAR(13) +
				N'union all ' + CHAR(13) +
				N'select ''***'', 0, 99999 ' + CHAR(13) +
				N'order by isnull([ord], 99998) '

			EXECUTE (@nvcSql);
		END
	end
	else
		raiserror('@anal flag not set correctly', 16, 1)
		goto LFail

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	go

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200235
BEGIN
	UPDATE Version$ SET DbVer = 200236
	COMMIT TRANSACTION
	PRINT 'database updated to version 200236'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200235 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
