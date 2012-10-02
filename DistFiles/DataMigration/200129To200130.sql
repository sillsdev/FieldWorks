-- update database FROM version 200129 to 200130

--( Steve Miller, Dec. 7, 2006: took out readuncommitted

BEGIN TRANSACTION  --( will be rolled back if wrong version#
/*
 * Added COLLATE specification for fn_varbintohexstr. Without it, we can get a crash
 * when the server has a different default collation from the database.
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
				-- Note the COLLATE statement is required here when using database servers with non-US collation!!
				master.dbo.fn_varbintohexstr(oi.[OrdKey]) COLLATE SQL_Latin1_General_CP1_CI_AS
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
				-- Note the COLLATE statement is required here when using database servers with non-US collation!!
				master.dbo.fn_varbintohexstr(oi.[OrdKey]) COLLATE SQL_Latin1_General_CP1_CI_AS
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

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200129
begin
	UPDATE Version$ SET DbVer = 200130
	COMMIT TRANSACTION
	print 'database updated to version 200130'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200129 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO