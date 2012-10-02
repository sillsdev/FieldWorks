/***********************************************************************************************
 * Procedure: GetLinkedObjs$
 *
 * Description:
 *	retrieves objects owned or referenced by the specifed object(s)
 *
 * Parameters:
 *	@ObjIds = list of object IDs
 *	@grfcpt=mask that indicates what types of related objects should be retrieved;
 *	@fBaseClasses=a flag that determines if the base classes of owned objects are included
 *		in the object list (e.g., rows for each object + all superclasses except CmObject.
 *		So if a CmPerson is included, it will also have a row for CmPossibility)
 *	@fSubClasses=flag that determines if the sub classes of owned objects are included in
 *		the object list;
 *	@fRecurse=a flag that determines if the owning tree is traversed;
 *	@nRefDirection=determines which reference directions will be included in the results
 *		(0=both, 1=referenced by this/these object(s), -1 reference this/these objects)
 *	@riid=only return objects of this class (including subclasses of this class). NULL
 *		returns all classes;
 *	@fCalcOrdKey=flag that determines if the order key is calculated;
 *
 * Returns:
 *	0 if successful, otherwise an error code
 *
 * Notes:
 *	This version of GetLinkedObjects$ is much more blocking and deadlock safe because it does
 *	not use the ObjInfoTbl$ table. Instead, this procedure expects the calling application or
 *	procedure to create a temporary table named #ObjInfoTbl$. See the below assumptions
 *	section for the structure of this table.
 *
 * Assumptions:
 *	This procedure assumes that the calling application/procedure has created the
 *	temporary table #ObjInfoTbl$ with the following structure:
 *
 *	create table [#ObjInfoTbl$]
 *	(
 *		[ObjId]		int		not null,
 *		[ObjClass]	int		null,
 *		[InheritDepth]	int		null		default(0),
 *		[OwnerDepth]	int		null		default(0),
 *		[RelObjId]	int		null,
 *		[RelObjClass]	int		null,
 *		[RelObjField]	int		null,
 *		[RelOrder]	int		null,
 *		[RelType]	int		null,
 *		[OrdKey]	varbinary(250)	null		default(0)
 *	)
 *
 *	It is recommended that in most cases the calling application/procedure also create a
 *	nonclustered index on at least the ObjId.
 *
 * Notes:
 *	This procedure apparently started out life as GetLinkedObjects2$, an "experimental
 *  work in progress" of Jonathan Richards'. Since the two procedures were identical,
 *  GetLinkedObjects2$ was eliminated. Jonathan named this GetLinkedObjects$, but the
 *  older version is firmly entrenched into some code at this time, so I (Steve Miller)
 *  renamed it to its current name.
 **********************************************************************************************/
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
