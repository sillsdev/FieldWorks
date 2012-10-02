-- update database FROM version 200096 to 200097

-- Steve Miller, Dec. 6, 2006: Took out readuncommitted.

BEGIN TRANSACTION  --( will be rolled back if wrong version#
/*
 * This was revised 1/19/2006 to provide a much faster process (5 min instead of 5 hours).
 * Also, the new version doesn't delete MSAs owned by rules which the earlier version
 * accidentally did.
 * LT-3125 - Find and Delete unreferenced (unused) MSAs
 * 1) Makes changes to stored Procedures relating to getting link references to an object
 * 2) Searches through existing MSAs and deletes the ones not being used.
 */

/*
 * The following stored procedure changes were made.
 *  GetLinkedObjs$ - increased @sOrdKey size from varchar(250) to varchar(502)
 *  GetLinkedObjects$ - ditto. Used master.dbo.fn_varbintohexstr() for hex string conversion.
 *  GetIncomingRefs$ - marked @uid as output parameter in call to GetLinkedObjects$
 *  GetIncomingRefsPrepDel$ - marked @uid as output parameter in call to GetIncomingRefs$
 */
if object_id('GetLinkedObjs$') is not null begin
	print 'removing proc GetLinkedObjs$'
	drop proc [GetLinkedObjs$]
end
go
print 'creating proc GetLinkedObjs$'
go

CREATE  proc [GetLinkedObjs$]
	@ObjId int=null,
	@hXMLDocObjList int=null,
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

	-- make sure objects were specified either in the XML or through the @ObjId parameter
	if ( @ObjId is null and @hXMLDocObjList is null ) or ( @ObjId is not null and @hXMLDocObjList is not null )
		goto LFail

	-- get the owned objects
	IF (176160768) & @grfcpt > 0 --( mask = owned obects
		INSERT INTO [#OwnedObjsInfo$]
			SELECT * FROM dbo.fnGetOwnedObjects$(
				@ObjId,
				@hXMLDocObjList,
				@grfcpt,
				@fBaseClasses,
				@fSubClasses,
				@fRecurse,
				@riid,
				@fCalcOrdKey)
	ELSE --( mask = referenced items or all: get all owned objects
		INSERT INTO [#OwnedObjsInfo$]
			SELECT * FROM dbo.fnGetOwnedObjects$(
				@ObjId,
				@hXMLDocObjList,
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
				master.dbo.fn_varbintohexstr(oi.[OrdKey])
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
				master.dbo.fn_varbintohexstr(oi.[OrdKey])
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
				master.dbo.fn_varbintohexstr(oi.[OrdKey])
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
				master.dbo.fn_varbintohexstr(oi.[OrdKey])
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
					'(ObjId,ObjClass,InheritDepth,OwnerDepth,RelObjId,RelObjClass,RelObjField,RelOrder,RelType,OrdKey)' + char(13) +
					'select '

			-- determine if the reference is atomic
			if @nType = 24 begin
				-- determine if this class references an object's class within the object hierachy,
				--	and whether or not it should be included
				if @nDirection = 1 and (@nRefDirection = 0 or @nRefDirection = -1) begin
					set @sQry=@sQry+convert(nvarchar(11), @nObjId)+','+convert(nvarchar(11), @nObjClass)+','+
							convert(nvarchar(11), @nInheritDepth)+','+convert(nvarchar(11), @nOwnerDepth)+','+
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
							convert(nvarchar(11), @nClass)+','+convert(nvarchar(11), @nInheritDepth)+','+
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
							convert(nvarchar(11), @nInheritDepth)+','+convert(nvarchar(11), @nOwnerDepth)+','+
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
							convert(nvarchar(11), @nClass)+','+convert(nvarchar(11), @nInheritDepth)+','+
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

if object_id('GetLinkedObjects$') is not null begin
	print 'removing proc GetLinkedObjects$'
	drop proc [GetLinkedObjects$]
end
go
print 'creating proc GetLinkedObjects$'
go
CREATE  proc [GetLinkedObjects$]
	@uid uniqueidentifier output,
	@ObjId int=NULL,
	@grfcpt int=528482304,
	@fBaseClasses tinyint=0,
	@fSubClasses tinyint=0,
	@fRecurse tinyint=1,
	@nRefDirection smallint=0,
	@riid int=NULL,
	@fCalcOrdKey tinyint=1
as
	declare @Err int, @nRowCnt int
	declare	@sQry nvarchar(1000), @sUid nvarchar(50)
	declare	@nObjId int, @nObjClass int, @nInheritDepth int, @nOwnerDepth int, @nClass int, @nField int,
		@nRelOrder int, @nType int, @nDirection int, @sClass sysname, @sField sysname,
		@sOrderField sysname, @sOrdKey varchar(502)
	declare	@fIsNocountOn int

	set @Err = 0

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- if null was specified as the mask assume that all objects are desired
	if @grfcpt is null set @grfcpt = 528482304

	-- determine if an object was supplied as an argument, if one was not use ObjInfoTbl$ as the list of objects
	if @objId is not null begin
		-- get a unique value to identify this invocation's results
		set @uid = newid()

		-- get the class of the specified object
		insert into [ObjInfoTbl$] with (rowlock) (uid, ObjId, ObjClass, OrdKey)
		select	@uid, @objId, co.[Class$],
			-- calculate the order key even if @fCalcOrdKey = 0 because the overhead is very small here
			convert(varbinary, coalesce(co.[Owner$], 0)) +
			convert(varbinary, coalesce(co.[OwnFlid$], 0)) +
			convert(varbinary, coalesce(co.[OwnOrd$], 0))
		from	[CmObject] co
		where	co.[Id] = @objId

		set @Err = @@error
		if @Err <> 0 begin
			set @sUid = convert(nvarchar(50), @Uid)
			raiserror ('GetLinkedObjects$: SQL Error %d; Unable to insert the initial object into the ObjInfoTbl$ table (UID=%s).', 16, 1, @Err, @sUid)
			goto LFail
		end
	end
	else begin
		update	[ObjInfoTbl$] with (rowlock)
		set	[ObjClass]=co.[Class$], [OwnerDepth]=0, [InheritDepth]=0,
			-- calculate the order key even if @fCalcOrdKey = 0 because the overhead is very small here
			[OrdKey]=convert(varbinary, coalesce(co.[Owner$], 0)) +
				convert(varbinary, coalesce(co.[OwnFlid$], 0)) +
				convert(varbinary, coalesce(co.[OwnOrd$], 0))
		from	[ObjInfoTbl$] oi
				join [CmObject] co on oi.[ObjId] = co.[Id]
		where	oi.[uid]=@uid
	end

	-- determine if the whole owning tree should be included in the results
	set @nOwnerDepth = 1
	set @nRowCnt = 1
	while @nRowCnt > 0
	begin
		-- determine if the order key should be calculated - if the order key is not needed a more
		--    effecient query can be used to generate the ownership tree
		if @fCalcOrdKey = 1 begin
			-- get the objects owned at the next depth and calculate the order key
			insert	into [ObjInfoTbl$] with (rowlock)
				(uid, ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
			select 	@uid, co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[Class], co.[OwnFlid$], co.[OwnOrd$], f.[Type],
				oi.OrdKey+convert(varbinary, co.[Owner$]) + convert(varbinary, co.[OwnFlid$]) + convert(varbinary, coalesce(co.[OwnOrd$], 0))
			from 	[CmObject] co
					join [ObjInfoTbl$] oi on co.[Owner$] = oi.[ObjId]
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	oi.[Uid]=@uid
				and oi.[OwnerDepth] = @nOwnerDepth - 1
				and ( 	( @grfcpt & 8388608 = 8388608 and f.[Type] = 23 )
					or ( @grfcpt & 33554432 = 33554432 and f.[Type] = 25 )
					or ( @grfcpt & 134217728 = 134217728 and f.[Type] = 27 )
				)
		end
		else begin
			-- get the objects owned at the next depth and do not calculate the order key
			insert	into [ObjInfoTbl$] with (rowlock)
				(uid, ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType)
			select 	@uid, co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[Class], co.[OwnFlid$], co.[OwnOrd$], f.[Type]
			from 	[CmObject] co
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	exists (select 	*
					from 	[ObjInfoTbl$] oi
					where 	oi.[ObjId] = co.[Owner$]
						and oi.[Uid] = @uid
						and oi.[OwnerDepth] = @nOwnerDepth - 1
				)
				and ( 	( @grfcpt & 8388608 = 8388608 and f.[Type] = 23 )
					or ( @grfcpt & 33554432 = 33554432 and f.[Type] = 25 )
					or ( @grfcpt & 134217728 = 134217728 and f.[Type] = 27 )
				)
		end
		select @nRowCnt=@@rowcount, @Err=@@error
		if @Err <> 0 begin
			set @sUid = convert(nvarchar(50), @Uid)
			raiserror ('GetLinkedObjects$: SQL Error %d; Unable to traverse owning hierachy (UID=%s).', 16, 1, @Err, @sUid)
			goto LFail
		end

		if @fRecurse = 0 break
		set @nOwnerDepth = @nOwnerDepth + 1
	end

	--
	-- get all of the base classes of the object(s)
	--
	if @fBaseClasses = 1 begin
		insert	into ObjInfoTbl$ with (rowlock)
			(uid, ObjId, ObjClass, InheritDepth, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
		select	@uid, oi.[ObjId], p.[Dst], p.[Depth], oi.[OwnerDepth], oi.[RelObjId], oi.[RelObjClass], oi.[RelObjField], oi.[RelOrder], oi.[RelType], oi.[OrdKey]
		from	[ObjInfoTbl$] oi
				join [ClassPar$] p on oi.[ObjClass] = p.[Src]
				join [Class$] c on c.[id] = p.[Dst]
		where	p.[Depth] > 0 and p.[Dst] <> 0
			and [Uid]=@uid

		set @Err = @@error
		if @Err <> 0 begin
			set @sUid = convert(nvarchar(50), @Uid)
			raiserror ('GetLinkedObjects$: SQL Error %d; Unable to get base classes (UID=%s).', 16, 1, @Err, @sUid)
			goto LFail
		end
	end
	--
	-- get all of the sub classes of the object(s)
	--
	if @fSubClasses = 1 begin
		insert	into ObjInfoTbl$ with (rowlock)
			(uid, ObjId, ObjClass, InheritDepth, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
		select	@uid, oi.[ObjId], p.[Src], -p.[Depth], oi.[OwnerDepth], oi.[RelObjId], oi.[RelObjClass], oi.[RelObjField], oi.[RelOrder], oi.[RelType], oi.[OrdKey]
		from	[ObjInfoTbl$] oi
				join [ClassPar$] p on oi.[ObjClass] = p.[Dst] and InheritDepth = 0
				join [Class$] c on c.[id] = p.[Dst]
		where	p.[Depth] > 0 and p.[Dst] <> 0
			and [Uid] = @uid

		set @Err = @@error
		if @Err <> 0 begin
			set @sUid = convert(nvarchar(50), @Uid)
			raiserror ('GetLinkedObjects$: SQL Error %d; Unable to get sub classes (UID=%s).', 16, 1, @Err, @sUid)
			goto LFail
		end
	end

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
				convert(varchar, oi.[OrdKey])
			from 	[ObjInfoTbl$] oi
					join [Field$] f on f.[DstCls] = oi.[ObjClass]
						and ( 	( f.[type] = 24 and @grfcpt & 16777216 = 16777216 )
							or ( f.[type] = 26 and @grfcpt & 67108864 = 67108864 )
							or ( f.[type] = 28 and @grfcpt & 268435456 = 268435456 )
						)
					join [Class$] c on f.[Class] = c.[Id]
			where	[Uid]=@uid
				and (@riid is null or oi.[ObjClass] = @riid)
			union all
			-- get the classes that are referenced (atomic, sequences, and collections) by this class
			select	oi.[ObjId], oi.[ObjClass], oi.[InheritDepth], oi.[OwnerDepth], f.[Type], c.[Name], f.[DstCls], f.[Name], f.[Id],
				2, -- referenced by this class
				master.dbo.fn_varbintohexstr(oi.[OrdKey])
			from	[ObjInfoTbl$] oi
					join [Class$] c on c.[Id] = oi.[ObjClass]
					join [Field$] f on f.[Class] = c.[Id]
						and ( 	( f.[type] = 24 and @grfcpt & 16777216 = 16777216 )
							or ( f.[type] = 26 and @grfcpt & 67108864 = 67108864 )
							or ( f.[type] = 28 and @grfcpt & 268435456 = 268435456 )
						)
			where	[Uid]=@uid
				and (@riid is null or f.[DstCls] = @riid)
			order by oi.[ObjId]
		end
		else if @nRefDirection = 1 begin
			declare GetClassRefObj_cur cursor local static forward_only read_only for
			-- get the classes that are referenced (atomic, sequences, and collections) by these classes
			select	oi.[ObjId], oi.[ObjClass], oi.[InheritDepth], oi.[OwnerDepth], f.[Type], c.[Name], f.[DstCls], f.[Name], f.[Id],
				2, -- referenced by this class
				convert(varchar, oi.[OrdKey])
			from	[ObjInfoTbl$] oi
					join [Class$] c on c.[Id] = oi.[ObjClass]
					join [Field$] f on f.[Class] = c.[Id]
						and ( 	( f.[type] = 24 and @grfcpt & 16777216 = 16777216 )
							or ( f.[type] = 26 and @grfcpt & 67108864 = 67108864 )
							or ( f.[type] = 28 and @grfcpt & 268435456 = 268435456 )
						)
			where	[Uid]=@uid
				and (@riid is null or f.[DstCls] = @riid)
			order by oi.[ObjId]
		end
		else begin
			declare GetClassRefObj_cur cursor local static forward_only read_only for
			-- get the classes that reference (atomic, sequences, and collections) these classes
			-- do not include internal references between objects within the owning object hierarchy;
			--	this will be handled below
			select 	oi.[ObjId], oi.[ObjClass], oi.[InheritDepth], oi.[OwnerDepth], f.[Type], c.[Name], c.[Id], f.[Name], f.[Id],
				1, -- references this class
				master.dbo.fn_varbintohexstr(oi.[OrdKey])
			from 	[ObjInfoTbl$] oi
					join [Field$] f on f.[DstCls] = oi.[ObjClass]
						and ( 	( f.[type] = 24 and @grfcpt & 16777216 = 16777216 )
							or ( f.[type] = 26 and @grfcpt & 67108864 = 67108864 )
							or ( f.[type] = 28 and @grfcpt & 268435456 = 268435456 )
						)
					join [Class$] c on f.[Class] = c.[Id]
			where	[Uid]=@uid
				and (@riid is null or oi.[ObjClass] = @riid)
			order by oi.[ObjId]
		end

		open GetClassRefObj_cur
		fetch GetClassRefObj_cur into @nObjId, @nObjClass, @nInheritDepth, @nOwnerDepth, @nType, @sClass, @nClass,
				@sField, @nField, @nDirection, @sOrdKey
		while @@fetch_status = 0 begin

			-- build the base part of the query
			set @sQry = 'insert into [ObjInfoTbl$] with (rowlock) '+
					'(uid,ObjId,ObjClass,InheritDepth,OwnerDepth,RelObjId,RelObjClass,RelObjField,RelOrder,RelType,OrdKey)' + char(13) +
					'select '''+convert(nvarchar(255), @uid)+''','

			-- determine if the reference is atomic
			if @nType = 24 begin
				-- determine if this class references an object's class within the object hierachy,
				--	and whether or not it should be included
				if @nDirection = 1 and (@nRefDirection = 0 or @nRefDirection = -1) begin
					set @sQry=@sQry+convert(nvarchar(11), @nObjId)+','+convert(nvarchar(11), @nObjClass)+','+
							convert(nvarchar(11), @nInheritDepth)+','+convert(nvarchar(11), @nOwnerDepth)+','+
							't.[Id],'+convert(nvarchar(11), @nClass)+','+convert(nvarchar(11), @nField)+','+
							'NULL,'+convert(nvarchar(11), @nType)+',convert(varbinary,'''+@sOrdKey+''') '+
						'from ['+@sClass+'] t '+
						'where ['+@sField+']='+convert(nvarchar(11),@nObjId)

					-- determine if only external references should be included - don't included
					--	references between objects within the owning hierarchy
					if @nRefDirection = -1 begin
						set @sQry = @sQry + 'and not exists (' +
								'select * from [ObjInfoTbl$] oi ' +
								'where oi.[ObjId]=t.[Id] ' +
									'and oi.[uid] = '''+convert(nvarchar(255), @uid)+ '''' +
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
							convert(nvarchar(11), @nClass)+','+convert(nvarchar(11), @nInheritDepth)+','+
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
							convert(nvarchar(11), @nInheritDepth)+','+convert(nvarchar(11), @nOwnerDepth)+','+
							't.[Src],'+convert(nvarchar(11), @nClass)+','+convert(nvarchar(11), @nField)+','+
							@sOrderField+','+convert(nvarchar(11), @nType)+',convert(varbinary,'''+@sOrdKey+''') '+
						'from ['+@sClass+'_'+@sField+'] t '+
						'where t.[dst]='+convert(nvarchar(11),@nObjId)

					-- determine if only external references should be included - don't included
					--	references between objects within the owning hierarchy
					if @nRefDirection = -1 begin
						set @sQry = @sQry + 'and not exists (' +
								'select * from [ObjInfoTbl$] oi ' +
								'where oi.[ObjId]=t.[Src] ' +
									'and oi.[uid] = '''+convert(nvarchar(255), @uid)+ '''' +
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
							convert(nvarchar(11), @nClass)+','+convert(nvarchar(11), @nInheritDepth)+','+
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
				set @sUid = convert(nvarchar(50), @Uid)
				raiserror ('GetLinkedObjects$: SQL Error %d; Error performing dynamic SQL (UID=%s).', 16, 1, @Err, @sUid)

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
		delete	[ObjInfoTbl$]
		where 	[Uid] = @uid
			and not exists (
				select	*
				from	[ClassPar$] cp
				where	cp.[Dst] = @riid
					and cp.[Src] = [ObjClass]
			)
		set @Err = @@error
		if @Err <> 0 begin
			set @sUid = convert(nvarchar(50), @Uid)
			raiserror ('GetLinkedObjects$: SQL Error %d; Unable to remove objects that are not the specified class %d (UID=%s).', 16, 1, @Err, @riid, @sUid)
			goto LFail
		end
	end

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
GO

if object_id('GetIncomingRefs$') is not null begin
	print 'removing proc GetIncomingRefs$'
	drop proc [GetIncomingRefs$]
end
go
print 'creating proc GetIncomingRefs$'
go

create proc [GetIncomingRefs$]
	@uid uniqueidentifier output,
	@ObjId int=null,
	@fRecurse tinyint=1,
	@fDelOwnTree tinyint=1
as
	declare @Err int

	exec @Err = GetLinkedObjects$ @uid output, @ObjId, 528482304, 1, 0, @fRecurse, -1, null, 0
	return @Err

GO

if object_id('GetIncomingRefsPrepDel$') is not null begin
	print 'removing proc GetIncomingRefsPrepDel$'
	drop proc [GetIncomingRefsPrepDel$]
end
go
print 'creating proc GetIncomingRefsPrepDel$'
go

create proc [GetIncomingRefsPrepDel$]
	@uid uniqueidentifier output,
	@ObjId int=NULL
as
	declare @Err int, @sUid nvarchar(50)

	-- get incoming references, but do not delete the ownership tree
	exec @Err=GetIncomingRefs$ @uid output, @ObjId, 1, 0
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('GetIncomingRefsPrepDel$: SQL Error %d; Unable to get incoming references (UID=%s).', 16, 1, @Err, @sUid)
	end

	return @Err

GO


/*
 *  Search through existing MSAs and delete the ones not being used.
 */
/*************************************************************************
 * CreateDeleteObj
 *
 * Description:
 *	Creates a delete trigger for each object that deletes related
 *	properties to the object. used on database creation, or when the
 *	object properties get changed.
 *
 * Parameters:
 *	@nClassId = Class to create the trigger for
 *
 * Notes:
 *	Since this is a delete trigger, most of the object properties will
 *	go away with the record of this table. We still have to deal with:
 *
 *		1. References to this object
 *		2. string properties, stored in other tables
 *		3. properties in its parent class, stored in other tables
 *
 *	But before doing that, we must make sure the objects owned by this
 *	one go away.
 *
 *	The SERIALIZABLE locking hint sets these commands to the SERIALIZABLE
 *	transaction level. If we need greater concurrency, we may want to
 *	drop down to REPEATABLEREAD. See "Locking Hints" and "SET TRANSACTION
 *	LEVEL" on Books On Line (BOL).
 *************************************************************************/

IF OBJECT_ID('CreateDeleteObj') IS NOT NULL BEGIN
	PRINT 'removing procedure CreateDeleteObj'
	DROP PROC CreateDeleteObj
END
GO
PRINT 'creating procedure CreateDeleteObj'
GO

CREATE PROCEDURE CreateDeleteObj
	@nClassId INT
AS
	DECLARE
		@nvcObjClassName NVARCHAR(100),
		@nvcClassName NVARCHAR(100),
		@nFieldId INT,
		@nvcFieldName NVARCHAR(100),
		@nvcProcName NVARCHAR(120),  --( max possible size + a couple spare
		@nvcQuery1 VARCHAR(4000), --( 4000's not big enough; need more than 1 string
		@nvcQuery2 VARCHAR(4000),
		@nvcQuery3 VARCHAR(4000),
		@nvcQuery4 VARCHAR(4000),
		@fBuildProc BIT, --( Currently all tables are getting one
		@nvcDropQuery NVARCHAR(140),
		@nvcObjName NVARCHAR(100),
		@nOwnedClassId INT,
		@nDebug TINYINT

	SET @nDebug = 0

	SELECT @nvcObjClassName = c.Name FROM Class$ c WHERE c.Id = @nClassId

	SET @fBuildProc = 0
	SET @nvcProcName = N'TR_' + @nvcObjClassName + N'_ObjDel_Del'
	SET @nvcQuery1 = ''
	SET @nvcQuery2 = ''
	SET @nvcQuery3 = ''
	SET @nvcQuery4 = ''

	--( The initial part of the CREATE TRIGGER command
	IF OBJECT_ID(@nvcProcName) IS NULL
		SET @nvcQuery1 = N'CREATE'
	ELSE
		SET @nvcQuery1 = N'ALTER'

	--( This assumes only one ID (row) in deleted

	SET @nvcQuery1 = @nvcQuery1 +
		N' TRIGGER ' + @nvcProcName + N' ON ' + @nvcObjClassName + CHAR(13) +
		N'INSTEAD OF DELETE ' + CHAR(13) +
		N'AS ' + CHAR(13)
	IF @nDebug = 1
		SET @nvcQuery1 = @nvcQuery1 +
			CHAR(9) + N'PRINT ''TRIGGER ' + @nvcProcName +
				N' ON ' + @nvcObjClassName + N' INSTEAD OF DELETE ''' + CHAR(13) +
			CHAR(9) + CHAR(13)
	SET @nvcQuery1 = @nvcQuery1 +
		CHAR(9) + N'/* == This trigger generated by CreateDeleteObj == */ ' + CHAR(13) +
		CHAR(9) + CHAR(13) +
		CHAR(9) + N'DECLARE @nObjId INT ' + CHAR(13) +
		CHAR(9) + N'SELECT @nObjId = d.Id FROM deleted d' + CHAR(13) +
		CHAR(9) + CHAR(13)

	--==( Delete references *to* this object )==--

	--( atomic references to this object

	SELECT TOP 1 @nFieldId = f.Id, @nvcClassName = c.Name, @nvcFieldName = f.Name
	FROM Field$ f
	JOIN Class$ c ON c.Id = f.Class
	WHERE f.DstCls = @nClassId AND f.Type = 24
	ORDER BY f.Id

	IF @@ROWCOUNT != 0 BEGIN
		SET @fBuildProc = 1
		SET @nvcQuery1 = @nvcQuery1 + CHAR(9) +
			'/* Delete atomic references *to* this object */ ' + CHAR(13) +
			CHAR(9) + CHAR(13)
	END
	WHILE @@ROWCOUNT != 0 BEGIN
		SET @nvcQuery1 = @nvcQuery1 +
			CHAR(9) + N'UPDATE ' + @nvcClassName + N' WITH (SERIALIZABLE) ' + CHAR(13) +
			CHAR(9) + N'SET "' + @nvcFieldName + N'" = NULL ' + CHAR(13) +
			CHAR(9) + N'WHERE "' + @nvcFieldName + N'" = @nObjId ' + CHAR(13) +
			CHAR(9) + CHAR(13)

		SELECT TOP 1 @nFieldId = f.Id, @nvcClassName = c.Name, @nvcFieldName = f.Name
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.Class
		WHERE f.Id > @nFieldId AND f.DstCls = @nClassId AND f.Type = 24
		ORDER BY f.Id
	END

	--( collection and sequence refences

	SELECT TOP 1 @nFieldId = f.Id, @nvcClassName = c.Name, @nvcFieldName = f.Name
	FROM Field$ f
	JOIN Class$ c ON c.Id = f.Class
	WHERE f.DstCls = @nClassId AND f.Type IN (26, 28)
	ORDER BY f.Id

	IF @@ROWCOUNT != 0 BEGIN
		SET @fBuildProc = 1
		SET @nvcQuery2 = @nvcQuery2 + CHAR(9) +
			'/* Delete collection and sequence references *to* this object */ ' + CHAR(13) +
			CHAR(9) + CHAR(13)
	END
	WHILE @@ROWCOUNT != 0 BEGIN
		SET @nvcQuery2 = @nvcQuery2 +
			CHAR(9) + N'DELETE ' + @nvcClassName + N'_' + @nvcFieldName + N' WITH (SERIALIZABLE) ' + CHAR(13) +
			CHAR(9) + N'WHERE "Dst" = @nObjId ' + CHAR(13) +
			CHAR(9) + CHAR(13)

		SELECT TOP 1 @nFieldId = f.Id, @nvcClassName = c.Name, @nvcFieldName = f.Name
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.Class
		WHERE f.Id > @nFieldId AND f.DstCls = @nClassId AND f.Type IN (26, 28)
		ORDER BY f.Id
	END

	--==( Delete references *of* this object )==--

	--( Atomic references will get wiped out autmatically when this record
	--( goes away.

	--( Collection and Sequence refences

	SELECT TOP 1 @nFieldId = f.Id, @nvcClassName = c.Name, @nvcFieldName = f.Name
	FROM Field$ f
	JOIN Class$ c ON c.Id = f.Class
	WHERE f.Class = @nClassId AND f.Type IN (26, 28)
	ORDER BY f.Id

	IF @@ROWCOUNT != 0 BEGIN
		SET @fBuildProc = 1
		SET @nvcQuery3 = @nvcQuery3 + CHAR(9) +
			'/* Delete references *of* this object */ ' + CHAR(13) +
			CHAR(9) + CHAR(13)
	END
	WHILE @@ROWCOUNT != 0 BEGIN
		SET @nvcQuery3 = @nvcQuery3 +
			CHAR(9) + N'DELETE ' + @nvcClassName + N'_' + @nvcFieldName + N' WITH (SERIALIZABLE) ' + CHAR(13) +
			CHAR(9) + N'WHERE "Src" = @nObjId ' + CHAR(13) +
			CHAR(9) + CHAR(13)

		SELECT TOP 1 @nFieldId = f.Id, @nvcClassName = c.Name, @nvcFieldName = f.Name
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.Class
		WHERE f.Id > @nFieldId AND f.Class = @nClassId AND f.Type IN (26, 28)
		ORDER BY f.Id
	END

	--==( Delete strings of this object )==--

	SET @nvcQuery4 = @nvcQuery4 +
		CHAR(9) + N'/* Delete any strings of this object */ ' + CHAR(13) +
		CHAR(9) + CHAR(13)

	--( If any MultiStr$ properties, create delete code.
	SELECT TOP 1 @nFieldId = Id FROM Field$ WHERE Class = @nClassId AND Type = 14
	IF @@ROWCOUNT != 0 BEGIN
		SET @fBuildProc = 1
		SET @nvcQuery4 = @nvcQuery4 +
			CHAR(9) + N'DELETE MultiStr$ WITH (SERIALIZABLE) ' + CHAR(13) +
			CHAR(9) + N'WHERE "Obj" = @nObjId ' + CHAR(13) +
			CHAR(9) + CHAR(13)
	END

	--( If any MultiTxt$ properties, create delete code.
	SELECT TOP 1 @nFieldId = f."ID", @nvcClassName = c."NAME", @nvcFieldName = f."NAME"
	FROM Field$ f
	JOIN Class$ c ON c."ID" = f."CLASS"
	WHERE f."CLASS" = @nClassId AND f."TYPE" = 16
	ORDER BY f."ID"

	WHILE @@ROWCOUNT != 0 BEGIN
		SET @fBuildProc = 1
		SET @nvcQuery4 = @nvcQuery4 +
			CHAR(9) + N'DELETE ' + @nvcClassName + N'_' + @nvcFieldName + N' WITH (SERIALIZABLE) ' + CHAR(13) +
			CHAR(9) + N'WHERE "Obj" = @nObjId ' + CHAR(13) +
			CHAR(9) + CHAR(13)

		SELECT TOP 1 @nFieldId = f."ID", @nvcClassName = c."NAME", @nvcFieldName = f."NAME"
		FROM Field$ f
		JOIN Class$ c ON c."ID" = f."CLASS"
		WHERE f.ID > @nFieldId AND f."CLASS" = @nClassId AND f."TYPE" = 16
		ORDER BY f."ID"
	END

	--( If any MultiBigStr$ properties, create delete code.

	SELECT TOP 1 @nFieldId = Id FROM Field$ WHERE Class = @nClassId AND Type = 18
	IF @@ROWCOUNT != 0 BEGIN
		SET @fBuildProc = 1
		SET @nvcQuery4 = @nvcQuery4 +
			CHAR(9) + N'DELETE MultiBigStr$ WITH (SERIALIZABLE) ' + CHAR(13) +
			CHAR(9) + N'WHERE "Obj" = @nObjId ' + CHAR(13) +
			CHAR(9) + CHAR(13)
	END

	--( If any MultiBigTxt$ properties, create delete code.

	SELECT TOP 1 @nFieldId = Id FROM Field$ WHERE Class = @nClassId AND Type = 20
	IF @@ROWCOUNT != 0 BEGIN
		SET @fBuildProc = 1
		SET @nvcQuery4 = @nvcQuery4 +
			CHAR(9) + N'DELETE MultiBigTxt$ WITH (SERIALIZABLE) ' + CHAR(13) +
			CHAR(9) + N'WHERE "Obj" = @nObjId ' + CHAR(13) +
			CHAR(9) + CHAR(13)
	END

	--==( Delete this row, since this is an DELETE INSTEAD OF trigger )==--

	SET @fBuildProc = 1
	SET @nvcQuery4 = @nvcQuery4 +
		CHAR(9) + N'/* Delete this row (for INSTEAD OF DELETE trigger) */ ' + CHAR(13) +
		CHAR(9) + CHAR(13)
	SET @nvcQuery4 = @nvcQuery4 +
		CHAR(9) + N'DELETE ' + @nvcObjClassName + N' WITH (SERIALIZABLE) ' + CHAR(13) +
		CHAR(9) + N'WHERE "Id" = @nObjId ' + CHAR(13) +
		CHAR(9) + CHAR(13)

	--==( Delete properties in parent class )==--

	--( This will delete properties *only* in the parent class,
	--( because the parent class will have the same call to
	--( delete properties in *its* parent class. The parent
	--( class has a depth of 1.

	SELECT @nvcClassName = c.Name
	FROM ClassPar$ cp
	JOIN Class$ c ON c.Id = cp.Dst
	WHERE cp.Src = @nClassId AND cp.Depth = 1

	IF @@ROWCOUNT = 1 BEGIN	--( should only be CmObject that misses
		SET @fBuildProc = 1
		SET @nvcQuery4 = @nvcQuery4 +
			CHAR(9) + N'/* Delete properties in parent class */' + CHAR(13) +
			CHAR(9) + CHAR(13)
		SET @nvcQuery4 = @nvcQuery4 +
			CHAR(9) + N'DELETE ' + @nvcClassName + N' WITH (SERIALIZABLE) ' + CHAR(13) +
			CHAR(9) + N'WHERE "Id" = @nObjId ' + CHAR(13) +
			CHAR(9) + CHAR(13)
	END

	--==( Create the new trigger )==--

	IF @fBuildProc = 1 BEGIN
		IF @nDebug = 1 BEGIN
			PRINT '---- query1 ----'
			PRINT @nvcQuery1
			PRINT CHAR(9) + '---- query2 ----'
			PRINT @nvcQuery2
			PRINT CHAR(9) + '---- query3 ----'
			PRINT @nvcQuery3
			PRINT CHAR(9) + '---- query4 ----'
			PRINT @nvcQuery4
		END

		EXECUTE (@nvcQuery1 + @nvcQuery2 + @nvcQuery3 + @nvcQuery4)
	END
GO
---------------------------------------------------------------------------

/*************************************************************************
 * Function fnGetIdsFromNtext
 *
 * Description:
 *	Loads a table variable with object IDs from an NTEXT parameter
 *	which has IDs either in a comma delimited list or in an XML doc.
 *
 * Parameters:
 *	@ntIds	= either a comma delimited list or an XML doc of IDs. Can
 *				be a singe ID.
 *
 * Returns:
 *	@tabIds	= table variable of IDs.
 *
 * Notes:
 *************************************************************************/

IF OBJECT_ID('fnGetIdsFromNtext') IS NOT NULL BEGIN
	PRINT 'removing function fnGetIdsFromNtext'
	DROP FUNCTION fnGetIdsFromNtext
END
GO
PRINT 'creating function fnGetIdsFromNtext'
GO

CREATE FUNCTION fnGetIdsFromNtext (@ntIds NTEXT)
RETURNS @tabIds TABLE ("ID" INT, "CLASSNAME" NVARCHAR(100))
AS
BEGIN
	DECLARE
		@nComma1 INT,
		@nComma2 INT,
		@nId INT,
		@hXmlIds INT,
		@nError INT

	SET @nError = 0

	--==( Load Ids )==--

	--( Load from a comma delimited list.

	IF SUBSTRING(@ntIds, 1, 1) != '<' BEGIN
		SET @nComma1 = 0
		SET @nComma2 = CHARINDEX(',', @ntIds)

		--( Only one object
		IF @nComma2 = 0
			SET @nId = SUBSTRING(@ntIds, 1, DATALENGTH(@ntIds))

		--( List of objects
		ELSE BEGIN
			WHILE @nComma2 > 0 BEGIN
				SET @nId = SUBSTRING(@ntIds, @nComma1 + 1, @nComma2 - 1 - @nComma1)

				INSERT INTO @tabIds
				SELECT @nId, c."NAME"
				FROM CmObject o
				JOIN Class$ c ON c."ID" = o.Class$
				WHERE o."ID" = @nId

				SET @nComma1 = @nComma2
				SET @nComma2 = CHARINDEX(',', @ntIds, @nComma2 + 1)
			END

			--( @nComma1 now has the last comma, not @nComma2
			SET @nId = SUBSTRING(@ntIds, @nComma1 + 1, DATALENGTH(@ntIds) - @nComma1)
		END

		--( Take care of only or last object.
		INSERT INTO @tabIds
		SELECT @nId, c."NAME"
		FROM CmObject o
		JOIN Class$ c ON c."ID" = o.Class$
		WHERE o."ID" = @nId
	END

	--( Load from an XML string
	ELSE BEGIN
		EXECUTE sp_xml_preparedocument @hXmlIds OUTPUT, @ntIds
		SET @nError = @@ERROR
		IF @nError != 0
			GOTO Fail

		INSERT INTO @tabIds
		SELECT i."ID", c."NAME"
		FROM OPENXML(@hXmlIds, '/root/Obj') WITH ("ID" INT) i
		JOIN CmObject o ON o."ID" = i."ID"
		JOIN Class$ c ON c."ID" = o."CLASS$"

		EXECUTE sp_xml_removedocument @hXmlIds
		SET @nError = @@ERROR
		IF @nError != 0
			GOTO Fail
	END
	RETURN

Fail:
	DELETE FROM @tabIds
	INSERT INTO @tabIds VALUES (-1, NULL)
	RETURN
END
GO
---------------------------------------------------------------------------

/*************************************************************************
 * DeleteObjects
 *
 * Description:
 *	Deletes an object and its owned objects.
 *
 * Parameters:
 *	@nClassId = Class to create the trigger for
 *
 * Notes:
 *	Before an object can be deleted, references to it must be deleted.
 *	But before that, the objects that this object owns must be deleted.
 *	This procedure was designed to be called recursively.
 *************************************************************************/

IF OBJECT_ID('DeleteObjects') IS NOT NULL BEGIN
	PRINT 'removing procedure DeleteObjects'
	DROP PROC DeleteObjects
END
GO
PRINT 'creating procedure DeleteObjects'
GO

CREATE PROCEDURE DeleteObjects
	@ntIds NTEXT
AS
	DECLARE @tIds TABLE ("ID" INT, "CLASSNAME" NVARCHAR(100), "LEVEL" TINYINT)

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
	SELECT f."ID", f."CLASSNAME", 0
	FROM dbo.fnGetIdsFromNtext(@ntIds) AS f

	--( Now find owned objects

	SET @nLevel = 1

	INSERT INTO @tIds
	SELECT o."ID", c."NAME", @nLevel
	FROM @tIds t
	JOIN CmObject o ON o.Owner$ = t.Id
	JOIN Class$ c ON c."ID" = o.Class$

	SET @nRowCount = @@ROWCOUNT
	WHILE @nRowCount != 0 BEGIN
		SET @nLevel = @nLevel + 1

		INSERT INTO @tIds
		SELECT o."ID", c."NAME", @nLevel
		FROM @tIds t
		JOIN CmObject o ON o.Owner$ = t.Id
		JOIN Class$ c ON c."ID" = o.Class$
		WHERE t."LEVEL" = @nLevel - 1

		SET @nRowCount = @@ROWCOUNT
	END
	SET @nLevel = @nLevel - 1

	--==( Delete objects )==--

	--( We're going to start out at the leaves and work
	--( toward the trunk.

	WHILE @nLevel >= 0	BEGIN

		SELECT TOP 1 @nObjId = t."ID", @nvcClassName = t."CLASSNAME"
		FROM @tIds t
		WHERE t."LEVEL" = @nLevel
		ORDER BY t."ID"

		SET @nRowCount = @@ROWCOUNT
		WHILE @nRowCount = 1 BEGIN
			SET @nvcSql = N'DELETE ' + @nvcClassName + N' WHERE Id = @nObjectID'
			EXEC sp_executesql @nvcSql, N'@nObjectID INT', @nObjectId = @nObjId
			SET @nError = @@ERROR
			IF @nError != 0
				GOTO Fail

			SELECT TOP 1 @nObjId = t."ID", @nvcClassName = t."CLASSNAME"
			FROM @tIds t
			WHERE t.Id > @nobjId AND t."LEVEL" = @nLevel
			ORDER BY t."ID"

			SET @nRowCount = @@ROWCOUNT
		END

		SET @nLevel = @nLevel - 1
	END

	RETURN 0

Fail:
	RETURN @nError
GO

---------------------------------------------------------------------------

DECLARE
	@nId INT,
	@sId NVARCHAR(20),
	@nDebug TINYINT

SET @nDebug = 1

EXEC CreateDeleteObj 0		--( CmObject
EXEC CreateDeleteObj 5041	--( MoMorphoSyntaxAnalysis
EXEC CreateDeleteObj 5001	--( MoStemMsa
EXEC CreateDeleteObj 5032	--( MoDerivationalStepMsa
EXEC CreateDeleteObj 5117	--( MoUnclassifiedAffixMsa
EXEC CreateDeleteObj 5031	--( MoDerivationalAffixMsa
EXEC CreateDeleteObj 5038	--( MoInflectionalAffixMsa
EXEC CreateDeleteObj 60		--( FsAbstractStructure
EXEC CreateDeleteObj 57		--( FsFeatureStructure
EXEC CreateDeleteObj 58		--( FsFeatureStructureDisjunction
EXEC CreateDeleteObj 51		--( FsClosedValue
EXEC CreateDeleteObj 53		--( FsComplexValue
EXEC CreateDeleteObj 54		--( FsDisjunctiveValue
EXEC CreateDeleteObj 61		--( FsNegatedValue
EXEC CreateDeleteObj 63		--( FsOpenValue
EXEC CreateDeleteObj 64		--( FsSharedValue

IF @nDebug = 1 BEGIN
	DECLARE @msaCnt int
	SELECT @msaCnt=COUNT(Dst) from LexEntry_MorphoSyntaxAnalyses
	PRINT '(Original) LexEntry MSA count = ' +  convert(varchar(6), @msaCnt)
END

DECLARE curMsa CURSOR LOCAL STATIC FORWARD_ONLY READ_ONLY FOR
SELECT msa."ID"
FROM MoMorphoSyntaxAnalysis msa
/* references to MSAs */
LEFT OUTER JOIN LexSense ls ON ls.MorphoSyntaxAnalysis = msa."ID" --( atomic
LEFT OUTER JOIN MoMorphemeAdhocCoProhibition macp ON macp.FirstMorpheme = msa."ID" --( atomic
LEFT OUTER JOIN WfiMorphBundle wmb ON wmb.Msa = msa."ID" --( atomic
LEFT OUTER JOIN MoMorphoSyntaxAnalysis_Components mmsac ON mmsac.Dst = msa."ID" --( col/seq
LEFT OUTER JOIN MoMorphemeAdhocCoProhibition_Morphemes mmacpm ON mmacpm.Dst = msa."ID" --( col/seq
LEFT OUTER JOIN MoMorphemeAdhocCoProhibition_RestOfMorphemes mmacprom ON mmacprom.Dst = msa."ID" --( col/seq
/* references to MSA subclasses */
LEFT OUTER JOIN MoDerivation md ON md.StemMsa = msa."ID" --( atomic
LEFT OUTER JOIN MoDerivationalAffixApp mdaa ON mdaa.AffixMsa = msa."ID" --( atomic
LEFT OUTER JOIN MoStemName msn ON msn.DefaultAffix = msa."ID"
LEFT OUTER JOIN MoInflAffixSlotApp miasa ON miasa.AffixMsa = msa."ID"
/* ownerships to MSA subclasses */
--( The owning MoEndocentricCompound_OverridingMsa is newer.
--LEFT OUTER JOIN MoEndocentricCompound_OverridingMsa mecomsa ON mecomsa.Dst = msa."ID"
LEFT OUTER JOIN MoExocentricCompound_ToMsa mxcomsa ON mxcomsa.Dst = msa."ID"
LEFT OUTER JOIN MoBinaryCompoundRule_LeftMSA mbcrl ON mbcrl.Dst = msa."ID"
LEFT OUTER JOIN MoBinaryCompoundRule_RightMSA mbcrr ON mbcrr.Dst = msa."ID"
LEFT OUTER JOIN MoDerivationalAffixApp_OutputMsa mdaao ON mdaao.Dst = msa."ID"
--( LexEntry is the owner of the MSAs we're tring to delete.
--( LEFT OUTER JOIN LexEntry_MorphoSyntaxAnalyses lemsa ON lemsa.Dst = msa."ID"
/* check references to MSAs */
WHERE ls.MorphoSyntaxAnalysis IS NULL
	AND macp.FirstMorpheme IS NULL
	AND wmb.Msa IS NULL
	AND mmsac.Dst IS NULL
	AND mmacpm.Dst IS NULL
	AND mmacprom.Dst IS NULL
	/* references to MSA subclasses */
	AND md.StemMsa IS NULL
	AND mdaa.AffixMsa IS NULL
	AND msn.DefaultAffix IS NULL
	AND miasa.AffixMsa IS NULL
	/* check ownerships to MSA subclasses */
	--( AND mecomsa.Src IS NULL
	AND mxcomsa.Src IS NULL
	AND mbcrl.Src IS NULL
	AND mbcrr.Src IS NULL
	AND mdaao.Src IS NULL
	--( AND lemsa.Src IS NULL

OPEN curMsa
FETCH NEXT FROM curMsa INTO @nId
WHILE @@FETCH_STATUS = 0
BEGIN
	SET @sId = CONVERT(NVARCHAR(10), @nId)
	EXEC DeleteObjects @sId
	FETCH NEXT FROM curMsa INTO @nId
END
CLOSE curMsa
DEALLOCATE curMsa

IF @nDebug = 1 BEGIN
	SELECT @msaCnt=COUNT(Dst) from LexEntry_MorphoSyntaxAnalyses
	PRINT '(Migrated) LexEntry MSA count = ' +  convert(varchar(6), @msaCnt)
END

DROP TRIGGER TR_CmObject_ObjDel_Del
DROP TRIGGER TR_MoMorphoSyntaxAnalysis_ObjDel_Del
DROP TRIGGER TR_MoStemMsa_ObjDel_Del
DROP TRIGGER TR_MoDerivationalStepMsa_ObjDel_Del
DROP TRIGGER TR_MoUnclassifiedAffixMsa_ObjDel_Del
DROP TRIGGER TR_MoDerivationalAffixMsa_ObjDel_Del
DROP TRIGGER TR_MoInflectionalAffixMsa_ObjDel_Del
DROP TRIGGER TR_FsAbstractStructure_ObjDel_Del
DROP TRIGGER TR_FsFeatureStructure_ObjDel_Del
DROP TRIGGER TR_FsFeatureStructureDisjunction_ObjDel_Del
DROP TRIGGER TR_FsClosedValue_ObjDel_Del
DROP TRIGGER TR_FsComplexValue_ObjDel_Del
DROP TRIGGER TR_FsDisjunctiveValue_ObjDel_Del
DROP TRIGGER TR_FsNegatedValue_ObjDel_Del
DROP TRIGGER TR_FsOpenValue_ObjDel_Del
DROP TRIGGER TR_FsSharedValue_ObjDel_Del

--( Some are beyond this migration. Therefore these have to be deleted,
--( and added again later. <sigh>

DROP PROCEDURE CreateDeleteObj
DROP FUNCTION fnGetIdsFromNtext
DROP PROCEDURE DeleteObjects
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200096
begin
	UPDATE Version$ SET DbVer = 200097
	COMMIT TRANSACTION
	print 'database updated to version 200097'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200096 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO