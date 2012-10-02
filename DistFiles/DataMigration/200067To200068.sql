-- Update database from version 200067 to 200068
BEGIN TRANSACTION  --( will be rolled back if wrong version#)
---------------------------------------------------------------------

if object_id('DeleteObj$') is not null begin
	print 'removing proc DeleteObj$'
	drop proc [DeleteObj$]
end
go
print 'creating proc DeleteObj$'
go
create proc [DeleteObj$]
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
	from [#ObjInfoTbl$] oi (readuncommitted)
	join [MultiStr$] ms (readuncommitted) on oi.[ObjId] = ms.[obj]
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
	JOIN #ObjInfoTbl$ oi (readuncommitted) ON oi.ObjClass = f.Class AND f.Type = 16
	ORDER BY f.[Id]

	SET @nRowCnt = @@ROWCOUNT
	WHILE @nRowCnt > 0 BEGIN

		SET @sQry = N'DELETE ' + @nvcTableName + N' WITH (REPEATABLEREAD) WHERE Obj = @nObj'
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
		JOIN #ObjInfoTbl$ oi (readuncommitted) ON oi.ObjClass = f.Class AND f.Type = 16
		WHERE f.[Id] > @nFlid
		ORDER BY f.[Id]

		SET @nRowCnt = @@ROWCOUNT
	END

	delete MultiBigStr$ with (REPEATABLEREAD)
	from [#ObjInfoTbl$] oi (readuncommitted)
	join [MultiBigStr$] ms (readuncommitted) on oi.[ObjId] = ms.[obj]
	set @Err = @@error
	if @Err <> 0 begin
		raiserror ('DeleteObj$: SQL Error %d; Unable to remove strings from the MultiBigStr$ table.', 16, 1, @Err)
		goto LFail
	end
	delete MultiBigTxt$ with (REPEATABLEREAD)
	from [#ObjInfoTbl$] oi (readuncommitted)
	 join [MultiBigTxt$] ms (readuncommitted) on oi.[ObjId] = ms.[obj]
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
			when kcptReferenceAtom then 1		-- atomic reference
			when kcptReferenceCollection then 2	-- reference collection
			when kcptReferenceSequence then 3	-- reference sequence
		end as OrdrAndType
	from	#ObjInfoTbl$ oi
			join [Field$] f on (oi.[ObjClass] = f.[DstCls] or 0 = f.[DstCls]) and f.[Type] in (kcptReferenceAtom, kcptReferenceCollection, kcptReferenceSequence)
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
			when kcptReferenceCollection then 4	-- reference collection
			when kcptReferenceSequence then 5	-- reference sequence
		end as OrdrAndType
	from	[#ObjInfoTbl$] oi
			join [Class$] c on c.[Id] = oi.[ObjClass]
			join [Field$] f on f.[Class] = c.[Id] and f.[Type] in (kcptReferenceCollection, kcptReferenceSequence)
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
				'from ['+@sDelClass+'] r  (readuncommitted)'+
					'join [#ObjInfoTbl$] oi (readuncommitted) on r.['+@sDelField+'] = oi.[ObjId] '
		end
		-- classes that contain sequence or collection references to this class
		else if @nOrdrAndType = 2 or @nOrdrAndType = 3 begin
			set @sQry='delete ['+@sDelClass+'_'+@sDelField+'] with (REPEATABLEREAD) '+
				'from ['+@sDelClass+'_'+@sDelField+'] c  (readuncommitted)'+
					'join [#ObjInfoTbl$] oi (readuncommitted) on c.[Dst] = oi.[ObjId] '
		end
		-- classes that are referenced by this class's collection or sequence references
		else if @nOrdrAndType = 4 or @nOrdrAndType = 5 begin
			set @sQry='delete ['+@sDelClass+'_'+@sDelField+'] with (REPEATABLEREAD) '+
				'from ['+@sDelClass+'_'+@sDelField+'] c  (readuncommitted)'+
					'join [#ObjInfoTbl$] oi (readuncommitted) on c.[Src] = oi.[ObjId] '
		end
		-- remove class data
		else if @nOrdrAndType = 6 begin
			set @sQry='delete ['+@sDelClass+'] with (REPEATABLEREAD) '+
				'from ['+@sDelClass+'] o  (readuncommitted)'+
					'join [#ObjInfoTbl$] oi (readuncommitted) on o.[id] = oi.[ObjId] '
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
	from #ObjInfoTbl$ do (readuncommitted)
	join CmObject co (readuncommitted) on do.[ObjId] = co.[id]
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
go

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200067
begin
	update Version$ set DbVer = 200068
	COMMIT TRANSACTION
	print 'database updated to version 200068'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200067 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
