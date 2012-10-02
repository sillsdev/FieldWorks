-- update database from version 200017 to 200018
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------
-- Updated stored procs from FwCore.sql.
-------------------------------------------------------------

if object_id('TR_CmObject$_RI_Del') is not null begin
	print 'removing trigger TR_CmObject$_RI_Del'
	drop trigger TR_CmObject$_RI_Del
end
go
print 'creating trigger TR_CmObject$_RI_Del'
go
create trigger TR_CmObject$_RI_Del on CmObject for delete
as
	declare @sDynSql nvarchar(4000)
	declare @iCurDelClsId int
	declare @fIsNocountOn int
	declare @uid uniqueidentifier

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	--( 	The following query is against the "deleted" table. This table
	--( is poorly documented in SQL Server documentation, but as I recall, both
	--( Oracle and FoxPro have a similar concept. The rows marked as deleted are
	--( here. The query inserts them into a scratch table ObjListTbl$. This table
	--( isn't one of the temp tables, or a table variable, but a scratch table the
	--( authors set up. It's used here because dynamic SQL isn't able to see the
	--( deleted table.
	--(	    Note also the use of newid(). This generates a new, unique ID. However,
	--( it happens only once, not for the whole table. The reason for this, I think,
	--( is that another user might be using the same scratch table concurrently. The
	--( second user would have a different ID than the first user. This makes sure
	--( each user is using their own rows in the same scratch table.

	-- copy the deleted rows into a the ObjListTbl table - this is necessary since the logical "deleted"
	--	table is not in	the scope of the dynamic SQL
	set @uid = newid()
	insert into [ObjListTbl$] ([Uid], [ObjId], [Ord], [Class])
	select	@uid, [Id], coalesce([OwnOrd$], -1), [Class$]
	from	deleted
	if @@error <> 0 begin
		raiserror('TR_CmObject$_RI_Del: Unable to copy the logical DELETED table to the ObjListTbl table', 16, 1)
		goto LFail
	end

	-- REVIEW (SteveMiller): With the use of IDs, the SERIALIZABLE keyword shouldn't be
	-- needed here.

	-- get the first class to process
	select	@iCurDelClsId = min([Class])
	from	[ObjListTbl$] (REPEATABLEREAD)
	where	[uid] = @uid
	if @@error <> 0 begin
		raiserror('TR_CmObject$_RI_Del: Unable to get the first deleted class', 16, 1)
		goto LFail
	end

	-- loop through all of the classes in the deleted logical table
	while @iCurDelClsId is not null begin

		--(    In SQL Server, you can set a variable with a SELECT statement,
		--( as long as the query returns a single row. In this case, the code
		--( queries the Class$ table on the Class.ID, to return the name of the
		--( class.  The name of the Class is concatenated into a string.
		--(    In this system, remember: 1) Each class is mapped to a table,
		--( whether it is an abstract or concrete class.  2) Each class is
		--( subclassed from CmObject. 3) The data in an object will therefore
		--( be persisted in more than one table: CmObject, and at least one table
		--( mapped to a subclass. This is foundational to understanding this database.
		--(    The query in the dynamic SQL joins the data in the scatch table, which
		--( came from the deleted table, which originally came from CmObject--we are
		--( in the CmObject trigger. The join is on the object ID. So this is checking
		--( to see if some of some of the object's data is still in one of the subclass
		--( tables. If so, it rolls back the transaction and raises an error. In this
		--( system, you must remove the object's persisted data in the subclass(es)
		--( before removing the persisted data in CmObject.

		-- REVIEW (SteveMiller): Is it necessary to make sure the data in the subclass
		-- tables is removed before removing the persisted data in the CmObject table?
		-- The presence of this	check makes referential integrity very tight. However,
		-- it comes at the cost of performance. We may want to return to this if we ever
		-- need some speed in the object deletion process. If nothing else, the dynamic
		-- SQL can be converted into a EXEC sp_executesql command, which is more efficient.

		select	@sDynSql =
			'if exists ( ' +
				'select * ' +
				'from ObjListTbl$ del join ' + [name] + ' c ' +
					'on del.[ObjId] = c.[Id] and del.[Class] = ' + convert(nvarchar(11), @iCurDelClsId) +
				'where del.[uid] = ''' + convert(varchar(255), @uid) + ''' ' +
			') begin ' +
				'raiserror(''Delete in CmObject violated referential integrity with %s'', 16, 1, ''' + [name] + ''') ' +
				'exec CleanObjListTbl$ ''' + convert(varchar(255), @uid) + ''' ' +
				'rollback tran ' +
			'end '
		from	[Class$]
		where	[Id] = @iCurDelClsId

		exec (@sDynSql)
		if @@error <> 0 begin
			raiserror('TR_CmObject$_RI_Del: Unable to execute dynamic SQL', 16, 1)
			goto LFail
		end

		-- get the next class to process
		select	@iCurDelClsId = min([Class])
		from	[ObjListTbl$] (REPEATABLEREAD)
		where	[Class] > @iCurDelClsId
			and [uid] = @uid
	end

	exec CleanObjListTbl$ @uid

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return

LFail:
	-- because the transaction is ROLLBACKed the rows in the ObjListTbl$ will be removed
	rollback tran
	return
go

-------------------------------------------------------
if object_id('fnGetObjInOwnershipPathWithId$') is not null begin
	print 'removing function fnGetObjInOwnershipPathWithId$'
	drop function [fnGetObjInOwnershipPathWithId$]
end
go
print 'creating function fnGetObjInOwnershipPathWithId$'
go
create function [fnGetObjInOwnershipPathWithId$] (
	@objId int=null,
	@hXMLDocObjList int=null,
	@riid int )
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
	declare	@iOwner int, @iOwnerClass int, @iCurObjId int, @iPrevObjId int

	-- determine if an object was supplied as an argument, if one was not use ObjInfoTbl$ as the list of objects
	if @objId is not null begin

		-- get the class of the specified object
		insert into @ObjInfo (ObjId, ObjClass, InheritDepth, OwnerDepth, ordkey)
		select	@objId, co.[Class$], null, null, null
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

	select	@iCurObjId=min(ObjId)
	from	@ObjInfo

	while @iCurObjId is not null begin
		set @iPrevObjId = @iCurObjId

		-- loop up (objects that own the specified objects) through the ownership hierarchy until the specified type (class=riid) of
		-- 	owning object is found or the top of the ownership hierarchy is reached
		set @iOwnerClass = 0
		while @iOwnerClass <> @riid begin
			select top 1
				@iOwner = co.[Owner$],
				@iOwnerClass = f.[Class]
			from	[CmObject] co
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	co.[id] = @iCurObjId

			if @@rowcount > 0 set @iCurObjId = @iOwner
			else begin
				set @iCurObjId = null
				break
			end
		end

		if @iCurObjId is not null begin
			-- update the ObjInfoTbl$ so that specified object(s) is/are related to the specified type of
			--    object (class=riid) that owns it
			update	@ObjInfo
			set	[RelObjId]=@iOwner,
				[RelObjClass]=(
					select co.[Class$]
					from [CmObject] co
					where co.[id]=@iOwner)
			where	[ObjId]=@iPrevObjId
			if @@error <> 0 goto LFail
		end

		-- if the user specified an object there was only one object to process and we can therefore
		--    break out of the loop
		if @objId is not null break

		select	@iCurObjId=min(ObjId)
		from	@ObjInfo
		where	[ObjId] > @iPrevObjId
	end

	return
LFail:
	delete @ObjInfo
	return
end
go

-------------------------------------------------------------------------------------
if object_id('[DefineCreateProc$]') is not null begin
	print 'removing proc DefineCreateProc$'
	drop proc [DefineCreateProc$]
end
go
print 'creating proc DefineCreateProc$'
go
create proc [DefineCreateProc$]
	@clid int
as
	declare @Err int, @fIsNocountOn int
	declare @sDynSQL1 nvarchar(4000), @sDynSQL2 nvarchar(4000), @sDynSQL3 nvarchar(4000),
		@sDynSQL4 nvarchar(4000), @sDynSQL5 nvarchar(4000), @sDynSQLParamList nvarchar(4000)
	declare @sValuesList nvarchar(1000)
	declare @fAbs tinyint, @sClass sysname, @sProcName sysname
	declare @sFieldName sysname, @nFieldType int, @flid int,
		@sFieldList nvarchar(4000), @sXMLTableDef nvarchar(4000)
	declare @sInheritClassName sysname, @nInheritClid int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- validate the class
	select	@fAbs = [Abstract],
		@sClass = [Name]
	from	[Class$]
	where	[Id] = @clid
	if @fAbs is null begin
		raiserror('Invalid class: clid=%d', 16, 1, @clid)
		set @Err = 50001
		goto LCleanUp
	end
	if @fAbs <> 0 begin
		raiserror('Cannot create procedure for abstract class %s', 16, 1, @sClass)
		set @Err = 50002
		goto LCleanUp
	end

	set @sProcName = N'CreateObject_' + @sClass

	-- if an old procedure exists remove it
	if exists (
		select	*
		from	sysobjects
		where	type = 'P'
			and name = @sProcName
		) begin
		set @sDynSQL1 = N'drop proc '+@sProcName
		exec (@sDynSQL1)
	end

	--
	-- build the parameter list and table insert statements
	--

	set @sDynSQL3=N''
	set @sDynSQL4=N''
	set @sDynSQLParamList=N''

	-- create a cursor to loop through the base classes and class
	declare curClassInheritPath cursor local fast_forward for
	select	c.[Name], c.[Id]
	from	[ClassPar$] cp join [Class$] c on cp.[Dst] = c.[Id]
	where	cp.[Src] = @clid
		and cp.[Dst] > 0
	order by cp.[Depth] desc

	open curClassInheritPath
	fetch curClassInheritPath into @sInheritClassName, @nInheritClid

	while @@fetch_status = 0 begin

		set @sValuesList=''

		-- create a cursor to assemble the field list
		declare curFieldList cursor local fast_forward for
		select	[Name], [Type], [Id]
		from	[Field$]
		where	[Class] = @nInheritClid
			and [Name] <> 'Id'
			-- do not include MultiString type columns nor relationship, e.g. reference sequence,
			--	type columns because these are all stored in tables external to the actual
			--	class table
			and [Type] not in (23, 24, 25, 26, 27, 28)
		order by [Id]

		open curFieldList
		fetch curFieldList into @sFieldName, @nFieldType, @flid

		set @sFieldList = N''
		set @sXMLTableDef = N''
		while @@fetch_status = 0 begin
			if @nFieldType = 14 begin -- MultiStr$
				set @sDynSQLParamList = @sDynSQLParamList + char(9) +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt nvarchar(4000) = null' + N', ' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt varbinary(8000) = null' + N', ' + char(13)
				set @sDynSQL3 = @sDynSQL3 + char(9) +
					N'if @' + @sInheritClassName + N'_' + @sFieldName + N'_txt is not null begin' + char(13) +
					char(9) + char(9) + N'insert into [MultiStr$] with (rowlock) ([Flid],[Obj],[Ws],[Txt],[Fmt]) ' + char(13) +
					char(9) + char(9) + N'values (' + convert(nvarchar(11), @flid)+ N',@ObjId,' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws' + N',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt' + N',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt)'
				set @sDynSQL3 = @sDynSQL3 +
N'
		set @Err = @@error
		if @Err <> 0 goto LCleanUp
	end
'
			end
			else if @nFieldType = 16 begin -- MultiTxt$
				set @sDynSQLParamList = @sDynSQLParamList + char(9) +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt nvarchar(4000) = null' + N', ' + char(13)
				set @sDynSQL3 = @sDynSQL3 + char(9) +
					N'if @' + @sInheritClassName + N'_' + @sFieldName + N'_txt is not null begin' + char(13)
				SET @sDynSQL3 = @sDynSQL3 + CHAR(9) + CHAR(9) +
					N'INSERT INTO ' + @sInheritClassName + N'_' + @sFieldName +
					N' WITH (ROWLOCK) (Obj, Ws, Txt)' + CHAR(13) +
					char(9) + char(9) + N'values (@ObjId,' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws' + ',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt)'
				set @sDynSQL3 = @sDynSQL3 +
N'
		set @Err = @@error
		if @Err <> 0 goto LCleanUp
	end
'
			end
			else if @nFieldType = 18 begin -- MultiBigStr$
				set @sDynSQLParamList = @sDynSQLParamList + char(9) +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt ntext = null' + N', ' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt image = null' + N', ' + char(13)
				set @sDynSQL3 = @sDynSQL3 + char(9) +
					N'if @' + @sInheritClassName + N'_' + @sFieldName + N'_txt is not null begin' + char(13) +
					char(9) + char(9) + N'insert into [MultiBigStr$] with (rowlock) ([Flid],[Obj],[Ws],[Txt],[Fmt])' + char(13) +
					char(9) + char(9) + N'values (' + convert(nvarchar(11), @flid)+ N',@ObjId,' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws' + N',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt' + N',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt)'
				set @sDynSQL3 = @sDynSQL3 +
N'
		set @Err = @@error
		if @Err <> 0 goto LCleanUp
	end
'
			end
			else if @nFieldType = 20 begin -- MultiBigTxt$
				set @sDynSQLParamList = @sDynSQLParamList + char(9) +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt ntext = null' + N', ' + char(13)
				set @sDynSQL3 = @sDynSQL3 + char(9) +
					N'if @' + @sInheritClassName + N'_' + @sFieldName + N'_txt is not null begin' + char(13) +
					char(9) + char(9) + N'insert into [MultiBigTxt$] with (rowlock) ([Flid],[Obj],[Ws],[Txt])' + char(13) +
					char(9) + char(9) + N'values (' + convert(nvarchar(11), @flid)+ N',@ObjId,' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws' + N',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt)'
				set @sDynSQL3 = @sDynSQL3 +
N'
		set @Err = @@error
		if @Err <> 0 goto LCleanUp
	end
'
			end
			else begin
				set @sDynSQLParamList = @sDynSQLParamList + char(9) + N'@' + @sInheritClassName + '_' + @sFieldName + ' ' +
					dbo.fnGetColumnDef$(@nFieldType) + N',' + char(13)
				set @sFieldList = @sFieldList + N',[' + @sFieldName + N']'

				if @sValuesList = '' set @sValuesList = N'@' + @sInheritClassName + N'_' + @sFieldName
				else set @sValuesList = @sValuesList + N', @' + @sInheritClassName + N'_' + @sFieldName

				if @nFieldType = 13 or @nFieldType = 17 begin -- String or BigString
					set @sDynSQLParamList = @sDynSQLParamList + char(9) + N'@' + @sInheritClassName + '_' + @sFieldName + '_fmt '
					if @nFieldType = 13 set @sDynSQLParamList = @sDynSQLParamList + 'varbinary(8000) = null,' + char(13)
					else if @nFieldType = 17 set @sDynSQLParamList = @sDynSQLParamList + 'image = null,' + char(13)

					set @sFieldList = @sFieldList + N',[' + @sFieldName + N'_fmt]'
					set @sValuesList = @sValuesList + N', @' + @sInheritClassName + '_' + @sFieldName + '_fmt'
				end

			end
			fetch curFieldList into @sFieldName, @nFieldType, @flid
		end

		close curFieldList
		deallocate curFieldList

		if @sFieldList <> N'' set @sDynSQL4 = @sDynSQL4 + char(13) + char(9) +
				N'insert into ['+@sInheritClassName+N'] ([Id]' + 	@sFieldList + N') ' + char(13) +
				char(9) + char(9) + N'values (@ObjId, ' + @sValuesList + N')'
		else set @sDynSQL4 = @sDynSQL4 + char(9) + N'insert into ['+@sInheritClassName+N'] with (rowlock) ([Id]) values(@ObjId)'
		set @sDynSQL4 = @sDynSQL4 + char(13) + char(9) + N'set @Err = @@error' + char(13) + char(9) + N'if @Err <> 0 goto LCleanUp' + char(13)

		fetch curClassInheritPath into @sInheritClassName, @nInheritClid
	end

	close curClassInheritPath
	deallocate curClassInheritPath

	--
	-- build the dynamic SQL strings
	--

		set @sDynSQLParamList =
N'
Create proc ['+@sProcName+N']' + char(13) + @sDynSQLParamList
	set @sDynSQL1 =
N'	@Owner int = null,
	@OwnFlid int = null,
	@StartObj int = null,
	@NewObjId int output,
	@NewObjGuid uniqueidentifier output,
	@fReturnTimestamp tinyint = 0,
	@NewObjTimestamp int = null output
as
	declare @fIsNocountOn int, @Err int, @nTrnCnt int, @sTranName sysname
	declare @OwnOrd int, @Type int, @ObjId int, @guid uniqueidentifier
	declare @DstClass int, @OwnerClass int, @OwnerFlidClass int

	set @nTrnCnt = null
	set @Type = null
	set @OwnOrd = null
	set @NewObjTimestamp = null

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- validate the new object''s owner arguments
	if @Owner is not null begin
		-- get the type of the @OwnFlid field and validate @OwnFlid as being a valid field
		select	@Type = [Type], @DstClass = [DstCls], @OwnerFlidClass = [Class]
		from	[Field$]
		where	[Id] = @OwnFlid
		if @@rowcount = 0 begin
			raiserror(''Owner field does not exist: OwnFlid=%d'', 16, 1, @OwnFlid)
			set @Err = 50001
			goto LCleanUp
		end
		if @Type not in (23, 25, 27) begin
			raiserror(''OwnFlid is not an owning relationship field: OwnFlid=%d Type=%d'', 16, 1, @Ownflid, @Type)
			set @Err = 50002
			goto LCleanUp
		end

		-- make sure the @OwnFlid field has a relationship with the ' + @sClass + N' class
		if @DstClass <> ' + convert(nvarchar(11), @clid) + N'  begin
			-- check the base classes
			if not exists (
				select	*
				from	[ClassPar$]
				where	[Src] = '+convert(nvarchar(11), @clid) + N' and [Dst] = @DstClass
			) begin
				raiserror(''OwnFlid does not relate to the ' + @sClass + N' class: OwnFlid=%d'', 16, 1, @OwnFlid)
				set @Err = 50003
				goto LCleanUp
			end
		end

		-- make sure that @OwnFlid is a field of the @Owner class
		select	@OwnerClass = [Class$]
		from	[CmObject]
		where	[Id] = @Owner
		if @@rowcount = 0 begin
			raiserror(''Owner object does not exist: Owner=%d'', 16, 1, @Owner)
			set @Err = 50004
			goto LCleanUp
		end
		if @OwnerClass <> @OwnerFlidClass begin
			-- check the base classes
			if not exists (
				select	*
				from	[ClassPar$]
				where	[Src] = @ownerClass and [Dst] = @OwnerFlidClass
			) begin
				raiserror(''OwnFlid is not a field of the owner class: Owner=%d, OwnerClass=%d, OwnFlid=%d'', 16, 1, @Owner, @OwnerClass, @OwnFlid)
				set @Err = 50005
				goto LCleanUp
			end
		end
	end

	-- determine if a transaction already exists; if one does then create a savepoint, otherwise create a
	--	transaction
	set @nTrnCnt = @@trancount
	set @sTranName = '''+@sProcName+N'_tr'' + convert(varchar(2), @@nestlevel)
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
'
	set @sDynSQL2 =
N'
	-- determine if the object is being added to an owning sequence
	if @Type = 27 begin

		-- determine if the objects should be added to the end of the list

		-- REVIEW (SteveMiller): A number of these delete statements originally had the SERIALIZABLE
		-- keyword rather than the REPEATABLEREAD keyword. This is entirely keeping with good db
		-- code. However, we currently (Dec 2004) have a long-running transaction running to
		-- support undo/redo. Doc indicate that using the SERIALIZABLE keyword holds a lock for
		-- the duration of the undo transaction, which bars any further inserts/updates from
		-- happening. Using REPEATABLEREAD lowers isolation and increases concurrency, a
		-- calculated risk until such time as the undo system gets rebuilt.

		if @StartObj is null begin
			select	@ownOrd = coalesce(max([OwnOrd$])+1, 1)
			from	[CmObject] with (REPEATABLEREAD)
			where	[Owner$] = @Owner
				and [OwnFlid$] = @OwnFlid
		end
		else begin
			-- get the ordinal value of the object that is located where the new object is to be inserted
			select	@OwnOrd = [OwnOrd$]
			from	[CmObject] with (repeatableread)
			where	[Id] = @StartObj
			if @OwnOrd is null begin
				raiserror(''The start object does not exist in the owning sequence: Owner=%d, OwnFlid=%d, StartObj=%d'', 16, 1, @Owner, @OwnFlid, @StartObj)
				set @Err = 50006
				goto LCleanUp
			end

			-- increment the ordinal value(s) of the object(s) in the sequence that occur at or after
			--	the new object(s)
			update	[CmObject] with (REPEATABLEREAD)
			set	[OwnOrd$] = [OwnOrd$] + 1
			where	[Owner$] = @Owner
				and [OwnFlid$] = @OwnFlid
				and [OwnOrd$] >= @OwnOrd
		end
	end
	-- determine if the object is being added to an atomic owning relationship
	else if @Type = 23 begin
		-- make sure there isn''t already an object owned by @Owner
		if exists (
			select	*
			from	[CmObject]
			where	[Owner$] = @Owner and [OwnFlid$] = @OwnFlid
			) begin
			raiserror(''An object is already owned by the atomic relationship: Owner=%d, OwnFlid=%d'', 16, 1, @Owner, @OwnFlid)
			set @Err = 50007
			goto LCleanUp
		end
	end

	set @guid = newid()
	insert into [CmObject] with (rowlock) ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
		values (@guid, '+convert(nvarchar(11), @clid)+N', @Owner, @OwnFlid, @OwnOrd)
	set @Err = @@error
	set @ObjId = @@identity
	if @Err <> 0 begin
		raiserror(''SQL Error %d: Unable to create the new object'', 16, 1, @Err)
		goto LCleanUp
	end

'
	set @sDynSQL5 =
N'

	-- set the output paramters
	set @NewObjId = @ObjId
	set @NewObjGuid = @guid
	if @fReturnTimestamp = 1 begin
		select	@NewObjTimestamp = [UpdStmp]
		from	[CmObject]
		where	[Id] = @NewObjId
	end

LCleanUp:

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- determine if a transaction or savepoint was created
	if @nTrnCnt is not null begin
		if @Err = 0 begin
			-- if a transaction was created within this procedure commit it
			if @nTrnCnt = 0 commit tran @sTranName
		end
		else begin
			rollback tran @sTranName
		end
	end

	return @Err
'

	--
	-- execute the dynamic SQL
	--
	exec (@sDynSQLParamList+@sDynSql1+@sDynSQL2+@sDynSQL3+@sDynSQL4+@sDynSQL5)
	set @Err = @@error
	if @Err <> 0 begin
		raiserror ('SQL Error %d: Unable to create procedure for class %s', 16, 1, @Err, @sClass)
		goto LCleanUp
	end

LCleanUp:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	return @Err
go

------------------------------------------------------


if object_id('CreateOwnedObject$') is not null begin
	print 'removing proc CreateOwnedObject$'
	drop proc CreateOwnedObject$
end
go
print 'creating proc CreateOwnedObject$'
go
create proc [CreateOwnedObject$]
	@clid int,
	@id int output,
	@guid uniqueidentifier output,
	@owner int,
	@ownFlid int,
	@type int,			-- type of field (atomic, collection, or sequence)
	@StartObj int = null,		-- object to insert before - owned sequences
	@fGenerateResults tinyint = 0,	-- default to not generating results
	@nNumObjects int = 1,		-- number of objects to create
	@uid uniqueidentifier = null output
as
	declare @err int, @nTrnCnt int, @sTranName varchar(50)
	declare @depth int, @fAbs bit
	declare @sDynSql nvarchar(4000), @sTbl sysname, @sId varchar(11)
	declare @OwnOrd int
	declare @i int, @currId int, @currOrd int, @currListOrd int
	declare	@fIsNocountOn int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- validate the class
	select	@fAbs = [Abstract], @sTbl = [Name]
	from	[Class$]
	where	[Id] = @clid
	if @fAbs <> 0 begin
		RaisError('Cannot instantiate abstract class: %s', 16, 1, @sTbl)
		return 50001
	end
	-- get the inheritance depth
	select	@depth = [Depth]
	from	[ClassPar$]
	where	[Src] = @clid
		and [Dst] = 0

	-- determine if a transaction already exists; if one does then create a savepoint, otherwise create a
	--	transaction
	set @nTrnCnt = @@trancount
	set @sTranName = 'CreateOwnedObject$_tr' + convert(varchar(8), @@nestlevel)
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName

	-- determine if the object is being added to a sequence
	if @type = 27 /* Owning Sequence */ begin

		-- determine if the object(s) should be added to the end of the sequence
		if @StartObj is null begin
			select	@ownOrd = coalesce(max([OwnOrd$])+1, 1)
			from	[CmObject] WITH (REPEATABLEREAD)
			where	[Owner$] = @Owner
				and [OwnFlid$] = @OwnFlid
		end
		else begin
			-- get the ordinal value of the object that is located where the new object is to be inserted
			select	@OwnOrd = [OwnOrd$]
			from	[CmObject] with (repeatableread)
			where	[Id] = @StartObj

			-- increment the ordinal value(s) of the object(s) in the sequence that occur at or after the new object(s)
			update	[CmObject] WITH (REPEATABLEREAD)
			set 	[OwnOrd$]=[OwnOrd$]+@nNumObjects
			where 	[Owner$] = @owner
				and [OwnFlid$] = @OwnFlid
				and [OwnOrd$] >= @OwnOrd
		end
	end

	-- determine if more than one object should be created; if more than one object is created the created objects IDs are stored
	--	in the ObjListTbl$ table so that the calling procedure/application can determine the IDs (the calling procedure or
	--	application is responsible for cleaning up the ObjListTlb$), otherwise if only one object is created the new object's
	--	ID is passed back to the calling procedure/application through output parameters -- the two approaches are warranted
	--	because it is ideal to avoid using the ObjListTbl$ if only one object is being created, also this maintains backward
	--	compatibility with existing code
	if @nNumObjects > 1 begin

		set @uid = NewId()

		set @i = 0
		set @currListOrd = coalesce(@ownOrd, 0)

		-- if an Id was supplied assume that the IDENTITY_INSERT setting is turned on and the incoming Id is legal
		if @id is not null begin
			while @i < @nNumObjects begin
				set @currId = @id + @i
				set @currOrd = @ownOrd + @i

				insert into [CmObject] ([Guid$], [Id], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
					values(newid(), @currId, @clid, @owner, @ownFlid, @currOrd)
				set @err = @@error
				if @Err <> 0 begin
					raiserror('Unable to create object: ID=%d, Class=%d, Owner=%d, OwnFlid=%d, OwnOrd=%d', 16, 1,
							@currId, @clid, @owner, @ownFlid, @currOrd)
					goto LFail
				end

				-- add the new object to the list of created objects
				insert into ObjListTbl$ with (rowlock) (uid, ObjId, Ord, Class)
					values (@uid, @id + @i, @currListOrd + @i, @clid)

				set @i = @i + 1
			end
		end
		else begin
			while @i < @nNumObjects begin
				set @currOrd = @ownOrd + @i

				insert into [CmObject] ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
					values(newid(), @clid, @owner, @ownFlid, @currOrd)
				set @err = @@error
				if @Err <> 0 begin
					raiserror('Unable to create object: Class=%d, Owner=%d, OwnFlid=%d, OwnOrd=%d', 16, 1,
							@clid, @owner, @ownFlid, @currOrd)
					goto LFail
				end
				set @id = @@identity

				-- add the new object to the list of created objects
				insert into ObjListTbl$ with (rowlock) (uid, ObjId, Ord, Class)
					values (@uid, @id, @currListOrd + @i, @clid)
				set @i = @i + 1
			end
		end

		-- insert the objects' Ids into all of the base classes
		while @depth > 0 begin
			set @depth = @depth - 1

			select	@sTbl = c.[Name]
			from	[ClassPar$] cp
			join [Class$] c  on c.[Id] = cp.[Dst]
			where	cp.[Src] = @clid
				and cp.[Depth] = @depth
			set @sDynSql =  'insert into [' + @sTbl + '] ([Id]) '+
					'select [ObjId] ' +
					'from [ObjListTbl$] '+
					'where [uid] = '''+convert(varchar(250), @uid)+''''
			exec (@sDynSql)
			set @err = @@error
			if @Err <> 0 begin
				raiserror('Unable to add rows to the base table %s', 16, 1, @sTbl)
				goto LFail
			end
		end

		if @fGenerateResults = 1 begin
			select	ObjId
			from	ObjListTbl$
			where	uid=@uid
			order by Ord
		end
	end
	else begin
		if @guid is null set @guid = NewId()

		-- if an Id was supplied assume that the IDENTITY_INSERT setting is turned on and the incoming Id is legal
		if @id is not null begin
			insert into [CmObject] ([Guid$], [Id], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
				values(@guid, @id, @clid, @owner, @ownFlid, @ownOrd)
			set @err = @@error
			if @Err <> 0 goto LFail
		end
		else begin
			insert into [CmObject] ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
				values(@guid, @clid, @owner, @ownFlid, @ownOrd)
			set @err = @@error
			if @Err <> 0 goto LFail
			set @id = @@identity
		end

		-- insert the object's Id into all of the base classes
		set @sId = convert(varchar(11), @id)
		while @depth > 0 begin
			set @depth = @depth - 1

			select	@sTbl = c.[Name]
			from	[ClassPar$] cp
			join [Class$] c  on c.[Id] = cp.[Dst]
			where	cp.[Src] = @clid
				and cp.[Depth] = @depth
			if @@rowcount <> 1 begin
				raiserror('Corrupt ClassPar$ table: %d', 16, 1, @clid)
				set @err = @@error
				goto LFail
			end

			set @sDynSql = 'insert into [' + @sTbl + '] with (rowlock) ([Id]) values (' + @sId + ')'
			exec (@sDynSql)
			set @err = @@error
			if @Err <> 0 begin
				raiserror('Unable to add a row to the base table %s: ID=%s', 16, 1, @sTbl, @sId)
				goto LFail
			end
		end

		if @fGenerateResults = 1 begin
			select @id [Id], @guid [Guid]
		end
	end

	-- update the date/time of the owner
	UPDATE [CmObject] SET [UpdDttm] = GetDate()
		FROM [CmObject] WHERE [Id] = @owner

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	-- if a transaction was created within this procedure commit it
	if @nTrnCnt = 0 commit tran @sTranName
	return 0

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	rollback tran @sTranName
	return @err
go

------------------------------------------------------

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
			CHAR(9) + N'JOIN ' + @nvcTableName + ' x (READUNCOMMITTED) ON x.Obj = @nObj'

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
go


----------------------------------------------------------
if object_id('DeleteObject$') is not null begin
	print 'removing proc DeleteObject$'
	drop proc [DeleteObject$]
end
go
print 'creating proc DeleteObject$'
go

create proc [DeleteObject$]
	@uid uniqueidentifier output,
	@objId int = null,
	@fRemoveObjInfo tinyint = 1
as
	declare @Err int, @nRowCnt int, @nTrnCnt int
	declare	@sQry nvarchar(400), @sUid nvarchar(50)
	declare	@nObjClass int, @nInheritDepth int, @nOwnerDepth int, @nOrdrAndType tinyint, @sDelClass nvarchar(100), @sDelField nvarchar(100)
	declare	@fIsNocountOn int

	DECLARE
		@nObj INT,
		@nvcTableName NVARCHAR(60),
		@nFlid INT

	set @Err = 0
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- determine if the procedure was called within a transaction; if yes then create a savepoint,
	--    otherwise create a transaction
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran DelObj$_Tran
	else save tran DelObj$_Tran
	set @Err = @@error
	if @Err <> 0 begin
		raiserror ('DeleteObject$: SQL Error %d; Unable to create a transaction.', 16, 1, @Err)
		goto LFail
	end

	-- get the ownership heirarchy
	exec @Err = GetOwnershipPath$ @uid output, @ObjId, 1, 1, 0
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('DeleteObject$: SQL Error %d; Unable to get the owning hierarchy (UID=%s).', 16, 1, @Err, @sUid)
		goto LFail
	end

	-- get all of the base classes of the object(s)
	insert	into ObjInfoTbl$ with (rowlock)
		(uid, ObjId, ObjClass, InheritDepth, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType)
	select	@uid, oi.[ObjId], p.[Dst], p.[Depth], oi.[OwnerDepth], oi.[RelObjId], oi.[RelObjClass], oi.[RelObjField], oi.[RelOrder], oi.[RelType]
	from	[ObjInfoTbl$] oi
			join [ClassPar$] p on oi.[ObjClass] = p.[Src]
			join [Class$] c on c.[id] = p.[Dst]
	where	p.[Depth] > 0 and p.[Dst] <> 0
		and [Uid]=@uid
	set @Err = @@error
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('DeleteObject$: SQL Error %d; Unable to get the base class object(s) of the object(s) in the owning hierarchy (UID=%s).', 16, 1, @Err, @sUid)
		goto LFail
	end
	-- get all of the sub classes of the object(s)
	insert	into ObjInfoTbl$ with (rowlock)
		(uid, ObjId, ObjClass, InheritDepth, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType)
	select	@uid, oi.[ObjId], p.[Src], -p.[Depth], oi.[OwnerDepth], oi.[RelObjId], oi.[RelObjClass], oi.[RelObjField], oi.[RelOrder], oi.[RelType]
	from	[ObjInfoTbl$] oi
			join [ClassPar$] p on oi.[ObjClass] = p.[Dst] and InheritDepth = 0
			join [Class$] c on c.[id] = p.[Dst]
	where	p.[Depth] > 0 and p.[Dst] <> 0
		and [Uid] = @uid
	set @Err = @@error
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('DeleteObject$: SQL Error %d; Unable to get the sub class object(s) of the object(s) in the owning hierarchy (UID=%s).', 16, 1, @Err, @sUid)
		goto LFail
	end

	--
	-- remove strings associated with the objects that will be deleted
	--
	delete	MultiStr$ with (REPEATABLEREAD)
	from [ObjInfoTbl$] oi
	join [MultiStr$] ms on oi.[ObjId] = ms.[obj]
	where oi.[Uid] = @uid
	set @Err = @@error
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('DeleteObject$: SQL Error %d; Unable to remove strings from the MultiStr$ table (UID=%s).', 16, 1, @Err, @sUid)
		goto LFail
	end

	--( This query finds the class of the object, and from there deteremines
	--( which multitxt fields need deleting. It gets the first property first.
	--( Any remaining multitxt properties are found in the loop.

	SELECT TOP 1
		@nObj = oi.ObjId,
		@nFlid = f.[Id],
		@nvcTableName = c.[Name] + '_' + f.[Name]
	FROM Field$ f
	JOIN Class$ c ON c.[Id] = f.Class
	JOIN CmObject o ON o.Class$ = f.Class AND Type = 16
	JOIN ObjInfoTbl$ oi ON oi.ObjId = o.[Id]
	ORDER BY f.[Id]

	SET @nRowCnt = @@ROWCOUNT
	WHILE @nRowCnt > 0 BEGIN
		SET @sQry =
			N'DELETE ' + @nvcTableName + N' WITH (REPEATABLEREAD) ' + CHAR(13) +
			CHAR(9) + N'FROM ObjInfoTbl$ oi' + CHAR(13) +
			CHAR(9) + N'JOIN ' + @nvcTableName + ' x ON x.Obj = @nObj'

		EXECUTE sp_executesql @sQry, N'@nObj INT', @nObj

		set @Err = @@error
		if @Err <> 0 begin
			raiserror ('DeleteObj$: SQL Error %d; Unable to remove strings from the MultiTxt table', 16, 1, @Err)
			goto LFail
		end

		SELECT TOP 1
			@nObj = oi.ObjId,
			@nFlid = f.[Id],
			@nvcTableName = c.[Name] + '_' + f.[Name]
		FROM Field$ f
		JOIN Class$ c ON c.[Id] = f.Class
		JOIN CmObject o ON o.Class$ = f.Class AND Type = 16
		JOIN ObjInfoTbl$ oi ON oi.ObjId = o.[Id]
		WHERE f.[Id] > @nFlid
		ORDER BY f.[Id]

		SET @nRowCnt = @@ROWCOUNT
	END

	delete MultiBigStr$ with (REPEATABLEREAD)
	from [ObjInfoTbl$] oi
	join [MultiBigStr$] ms on oi.[ObjId] = ms.[obj]
	where oi.[Uid] = @uid
	set @Err = @@error
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('DeleteObject$: SQL Error %d; Unable to remove strings from the MultiBigStr$ table (UID=%s).', 16, 1, @Err, @sUid)
		goto LFail
	end
	delete MultiBigTxt$ with (REPEATABLEREAD)
	from [ObjInfoTbl$] oi
	join [MultiBigTxt$] ms on oi.[ObjId] = ms.[obj]
	where oi.[Uid] = @uid
	set @Err = @@error
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('DeleteObject$: SQL Error %d; Unable to remove strings from the MultiBigTxt$ table (UID=%s).', 16, 1, @Err, @sUid)
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
	from	ObjInfoTbl$ oi (REPEATABLEREAD)
			join [Field$] f on oi.[ObjClass] = f.[DstCls] and f.[Type] in (24, 26, 28)
			join [Class$] c on f.[Class] = c.[Id]
	where	oi.[Uid] = @uid
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
	from	[ObjInfoTbl$] oi (REPEATABLEREAD)
			join [Class$] c on c.[Id] = oi.[ObjClass]
			join [Field$] f on f.[Class] = c.[Id] and f.[Type] in (26, 28)
	where	oi.[Uid] = @uid
	group by oi.[ObjClass], c.[Name], f.[Name], f.[Type]
	union all
	-- get the owned classes
	select	oi.[ObjClass],
		min(oi.[InheritDepth]) as InheritDepth,
		max(oi.[OwnerDepth]) as OwnerDepth,
		c.[Name] as DelClassName,
		NULL,
		6 as OrdrAndType
	from	ObjInfoTbl$ oi (REPEATABLEREAD)
			join Class$ c on oi.ObjClass = c.Id
	where	oi.[Uid] = @uid
	group by oi.[ObjClass], c.Name
	order by OrdrAndType, InheritDepth asc, OwnerDepth desc, DelClassName

	open Del_Cur
	fetch Del_Cur into @nObjClass, @nInheritDepth, @nOwnerDepth, @sDelClass, @sDelField, @nOrdrAndType

	while @@fetch_status = 0 begin

		-- classes that contain refence pointers to this class
		if @nOrdrAndType = 1 begin
			set @sQry='update ['+@sDelClass+'] with (REPEATABLEREAD) set ['+@sDelField+']=NULL '+
				'from ['+@sDelClass+'] r '+
					'join [ObjInfoTbl$] oi on r.['+@sDelField+'] = oi.[ObjId] '
		end
		-- classes that contain sequence or collection references to this class
		else if @nOrdrAndType = 2 or @nOrdrAndType = 3 begin
			set @sQry='delete ['+@sDelClass+'_'+@sDelField+'] with (REPEATABLEREAD)'+
				'from ['+@sDelClass+'_'+@sDelField+'] c '+
					'join [ObjInfoTbl$] oi on c.[Dst] = oi.[ObjId] '
		end
		-- classes that are referenced by this class's collection or sequence references
		else if @nOrdrAndType = 4 or @nOrdrAndType = 5 begin
			set @sQry='delete ['+@sDelClass+'_'+@sDelField+'] with (REPEATABLEREAD)'+
				'from ['+@sDelClass+'_'+@sDelField+'] c '+
					'join [ObjInfoTbl$] oi on c.[Src] = oi.[ObjId] '
		end
		-- remove class data
		else if @nOrdrAndType = 6 begin
			set @sQry='delete ['+@sDelClass+'] with (REPEATABLEREAD)'+
				'from ['+@sDelClass+'] o '+
					'join [ObjInfoTbl$] oi on o.[id] = oi.[ObjId] '
		end

		set @sQry = @sQry +
				'where oi.[ObjClass]='+convert(nvarchar(11),@nObjClass)+' '+
					'and oi.[Uid]='''+convert(varchar(250), @uid)+''''
		exec(@sQry)
		select @Err = @@error, @nRowCnt = @@rowcount

		if @Err <> 0 begin
			set @sUid = convert(nvarchar(50), @Uid)
			raiserror ('DeleteObject$: SQL Error %d; Unable to execute dynamic SQL (UID=%s).', 16, 1, @Err, @sUid)
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
	from ObjInfoTbl$ do
	join CmObject co on do.[ObjId] = co.[id] and do.[uid] = @uid
	set @Err = @@error
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('DeleteObject$: SQL Error %d; Unable to remove the objects from CmObject (UID=%s).', 16, 1, @Err, @sUid)
		goto LFail
	end

	-- determine if the ObjInfoTbl$ should be cleaned up
	if @fRemoveObjInfo = 1 begin
		exec @Err=CleanObjInfoTbl$ @uid
		if @Err <> 0 begin
			set @sUid = convert(nvarchar(50), @Uid)
			raiserror ('DeleteObject$: SQL Error %d; Unable to remove rows from the ObjInfoTbl$ table (UID=%s).', 16, 1, @Err, @sUid)
			goto LFail
		end
	end

	if @nTrnCnt = 0 commit tran DelObj$_Tran
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return 0

LFail:
	rollback tran DelObj$_Tran
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
go

--------------------------------------------------------------

if object_id('DeletePrepDelObjects$') is not null begin
	print 'removing proc DeletePrepDelObjects$'
	drop proc [DeletePrepDelObjects$]
end
go
print 'creating proc DeletePrepDelObjects$'
go
create proc [DeletePrepDelObjects$]
	@uid uniqueidentifier,
	@fRemoveObjInfo tinyint = 1
as
	declare @Err int, @nRowCnt int, @nTrnCnt int
	declare	@sQry nvarchar(400), @sUid nvarchar(50)
	declare	@nObjClass int, @nInheritDepth int, @nOwnerDepth int, @nOrdrAndType tinyint, @sDelClass nvarchar(100), @sDelField nvarchar(100)
	declare	@fIsNocountOn int

	DECLARE
		@nObj INT,
		@nvcTableName NVARCHAR(60),
		@nFlid INT

	set @Err = 0
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- determine if the procedure was called within a transaction; if yes then create a savepoint,
	--    otherwise create a transaction
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran DelObj$_Tran
	else save tran DelObj$_Tran
	set @Err = @@error
	if @Err <> 0 begin
		raiserror ('DeletePrepDelObjects$: SQL Error %d; Unable to create a transaction.', 16, 1, @Err)
		goto LFail
	end

	--
	-- remove strings associated with the objects that will be deleted
	--
	delete	MultiStr$
	from [ObjInfoTbl$] oi join [MultiStr$] ms on oi.[ObjId] = ms.[obj]
	where oi.[Uid] = @uid
	set @Err = @@error
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('DeletePrepDelObjects$: SQL Error %d; Unable to remove strings from the MultiStr$ table (UID=%s).', 16, 1, @Err, @sUid)
		goto LFail
	end

	--( This query finds the class of the object, and from there deteremines
	--( which multitxt fields need deleting. It gets the first property first.
	--( Any remaining multitxt properties are found in the loop.

	SELECT TOP 1
		@nObj = oi.ObjId,
		@nFlid = f.[Id],
		@nvcTableName = c.[Name] + '_' + f.[Name]
	FROM Field$ f
	JOIN Class$ c ON c.[Id] = f.Class
	JOIN CmObject o ON o.Class$ = f.Class AND Type = 16
	JOIN ObjInfoTbl$ oi ON oi.ObjId = o.[Id]
	ORDER BY f.[Id]

	SET @nRowCnt = @@ROWCOUNT
	WHILE @nRowCnt > 0 BEGIN
		SET @sQry =
			N'DELETE ' + @nvcTableName + N' WITH (REPEATABLEREAD) ' + CHAR(13) +
			CHAR(9) + N'FROM ObjInfoTbl$ oi' + CHAR(13) +
			CHAR(9) + N'JOIN ' + @nvcTableName + ' x ON x.Obj = @nObj'

		EXECUTE sp_executesql @sQry, N'@nObj INT', @nObj

		set @Err = @@error
		if @Err <> 0 begin
			raiserror ('DeleteObj$: SQL Error %d; Unable to remove strings from the MultiTxt table ', 16, 1, @Err)
			goto LFail
		end

		SELECT TOP 1
			@nObj = oi.ObjId,
			@nFlid = f.[Id],
			@nvcTableName = c.[Name] + '_' + f.[Name]
		FROM Field$ f
		JOIN Class$ c ON c.[Id] = f.Class
		JOIN CmObject o ON o.Class$ = f.Class AND Type = 16
		JOIN ObjInfoTbl$ oi ON oi.ObjId = o.[Id]
		WHERE f.[Id] > @nFlid
		ORDER BY f.[Id]

		SET @nRowCnt = @@ROWCOUNT
	END

	delete MultiBigStr$
	from [ObjInfoTbl$] oi join [MultiBigStr$] ms on oi.[ObjId] = ms.[obj]
	where oi.[Uid] = @uid
	set @Err = @@error
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('DeletePrepDelObjects$: SQL Error %d; Unable to remove strings from the MultiBigStr$ table (UID=%s).', 16, 1, @Err, @sUid)
		goto LFail
	end
	delete MultiBigTxt$
	from [ObjInfoTbl$] oi join [MultiBigTxt$] ms on oi.[ObjId] = ms.[obj]
	where oi.[Uid] = @uid
	set @Err = @@error
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('DeletePrepDelObjects$: SQL Error %d; Unable to remove strings from the MultiBigTxt$ table (UID=%s).', 16, 1, @Err, @sUid)
		goto LFail
	end

	--
	-- loop through the objects and delete all owned objects and clean-up all relationships
	--
	declare Del_Cur cursor fast_forward local for
	-- get the external classes that reference (atomic, sequences, and collections) one of the owned classes
	select	oi.ObjClass,
		min(oi.InheritDepth) as InheritDepth,
		max(oi.OwnerDepth) as OwnerDepth,
		c.Name as DelClassName,
		f.Name as DelFieldName,
		case oi.[RelType]
			when 24 then 1	-- atomic reference
			when 26 then 2	-- reference collection
			when 28 then 3	-- reference sequence
		end as OrdrAndType
	from	ObjInfoTbl$ oi (REPEATABLEREAD) join Class$ c on oi.RelObjClass = c.Id
			join Field$ f on oi.RelObjField = f.Id
	where	oi.[Uid] = @uid
		and oi.[RelType] in (24,26,28)
	group by oi.ObjClass, c.Name, f.Name, oi.RelType
	union all
	-- get internal references - the call to GetIncomingRefsPrepDel$ only found external references
	select	oi.ObjClass,
		min(oi.InheritDepth) as InheritDepth,
		max(oi.OwnerDepth) as OwnerDepth,
		c.Name as DelClassName,
		f.Name as DelFieldName,
		case oi.[RelType]
			when 24 then 1	-- atomic reference
			when 26 then 2	-- reference collection
			when 28 then 3	-- reference sequence
		end as OrdrAndType
	from	ObjInfoTbl$ oi (REPEATABLEREAD) join Field$ f on f.[DstCls] = oi.[ObjClass] and f.Type in (24,26,28)
			join Class$ c on c.[Id] = f.[Class]
	where	oi.[Uid] = @uid
		and exists (
			select	*
			from	ObjInfoTbl$ oi2
			where	oi2.[Uid] = @uid
				and oi2.[ObjClass] = f.[Class]
		)
	group by oi.ObjClass, c.Name, f.Name, oi.RelType
	union all
	-- get the classes that are referenced by the owning classes
	select	oi.ObjClass,
		min(oi.InheritDepth) as InheritDepth,
		max(oi.OwnerDepth) as OwnerDepth,
		c.Name as DelClassName,
		f.Name as DelFieldName,
		case f.[Type]
			when 26 then 4	-- reference collection
			when 28 then 5	-- reference sequence
		end as OrdrAndType
	from	[ObjInfoTbl$] oi (REPEATABLEREAD) join [Class$] c on c.[Id] = oi.[ObjClass]
			join [Field$] f on f.[Class] = c.[Id] and f.[Type] in (26, 28 /*Ref Col,Seq*/)
	where	oi.[Uid] = @uid
		and ( oi.[RelType] in (23,25,27 /*Owning Atom,Col,Seq*/) or oi.[RelType] is null )
	group by oi.ObjClass, c.Name, f.Name, f.[Type]
	union all
	-- get the owned classes
	select	oi.ObjClass,
		min(oi.InheritDepth) as InheritDepth,
		max(oi.OwnerDepth) as OwnerDepth,
		c.Name as DelClassName,
		NULL,
		6 as OrdrAndType
	from	ObjInfoTbl$ oi (REPEATABLEREAD) join Class$ c on oi.ObjClass = c.Id
	where	oi.[Uid] = @uid
		and ( oi.[RelType] in (23,25,27) or oi.[RelType] is null )
	group by oi.ObjClass, c.Name
	order by OrdrAndType, InheritDepth asc, OwnerDepth desc, DelClassName

	open Del_Cur
	fetch Del_Cur into @nObjClass, @nInheritDepth, @nOwnerDepth, @sDelClass, @sDelField, @nOrdrAndType

	while @@fetch_status = 0 begin

		-- classes that contain refence pointers to this class
		if @nOrdrAndType = 1 begin
			set @sQry='update ['+@sDelClass+'] set ['+@sDelField+']=NULL '+
				'from ['+@sDelClass+'] r '+
					'join [ObjInfoTbl$] oi on r.['+@sDelField+'] = oi.[ObjId] '
		end
		-- classes that contain sequence or collection references to this class
		else if @nOrdrAndType = 2 or @nOrdrAndType = 3 begin
			set @sQry='delete ['+@sDelClass+'_'+@sDelField+'] '+
				'from ['+@sDelClass+'_'+@sDelField+'] c '+
					'join [ObjInfoTbl$] oi on c.[Dst] = oi.[ObjId] '
		end
		-- classes that are referenced by this class's collection or sequence references
		else if @nOrdrAndType = 4 or @nOrdrAndType = 5 begin
			set @sQry='delete ['+@sDelClass+'_'+@sDelField+'] '+
				'from ['+@sDelClass+'_'+@sDelField+'] c '+
					'join [ObjInfoTbl$] oi on c.[Src] = oi.[ObjId] '
		end
		-- remove class data
		else if @nOrdrAndType = 6 begin
			set @sQry='delete ['+@sDelClass+'] '+
				'from ['+@sDelClass+'] o '+
					'join [ObjInfoTbl$] oi on o.[id] = oi.[ObjId] '
		end

		set @sQry = @sQry +
				'where oi.[ObjClass]='+convert(nvarchar(11),@nObjClass)+' '+
					'and oi.[Uid]='''+convert(varchar(250), @uid)+''''
		exec(@sQry)
		select @Err = @@error, @nRowCnt = @@rowcount

		if @Err <> 0 begin
			set @sUid = convert(nvarchar(50), @Uid)
			raiserror ('DeletePrepDelObjects$: SQL Error %d; Unable to execute dynamic SQL (UID=%s).', 16, 1, @Err, @sUid)
			goto LFail
		end

		fetch Del_Cur into @nObjClass, @nInheritDepth, @nOwnerDepth, @sDelClass, @sDelField, @nOrdrAndType
	end

	close Del_Cur
	deallocate Del_Cur

	--
	-- delete the objects in CmObject
	--
	delete CmObject
	from ObjInfoTbl$ do join CmObject co on do.[ObjId] = co.[id]

	set @Err = @@error
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('DeletePrepDelObjects$: SQL Error %d; Unable to remove the objects from the CmObject table (UID=%s).', 16, 1, @Err, @sUid)
		goto LFail
	end

	-- determine if the ObjInfoTbl$ should be cleaned up
	if @fRemoveObjInfo = 1 begin
		exec @Err=CleanObjInfoTbl$ @uid
		if @Err <> 0 begin
			set @sUid = convert(nvarchar(50), @Uid)
			raiserror ('DeletePrepDelObjects$: SQL Error %d; Unable to remove rows from the ObjInfoTbl$ table (UID=%s).', 16, 1, @Err, @sUid)
			goto LFail
		end
	end

	if @nTrnCnt = 0 commit tran DelObj$_Tran

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return 0

LFail:
	rollback tran DelObj$_Tran
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
go

-------------------------------------------------------------------

if object_id('DefineReplaceRefCollProc$') is not null begin
	print 'removing proc DefineReplaceRefCollProc$'
	drop proc [DefineReplaceRefCollProc$]
end
go
print 'Creating proc DefineReplaceRefCollProc$'
go
create proc [DefineReplaceRefCollProc$]
	@sTbl sysname
as
	declare @sDynSql nvarchar(4000), @sDynSql2 nvarchar(4000)
	declare @err int

	if object_id('ReplaceRefColl_' + @sTbl ) is not null begin
		set @sDynSql = 'alter '
	end
	else begin
		set @sDynSql = 'create '
	end

set @sDynSql = @sDynSql + N'
proc ReplaceRefColl_' + @sTbl +'
	@SrcObjId int,
	@hXMLDocInsert int = null,
	@hXMLDocDelete int = null,
	@fRemoveXMLDocs tinyint = 1
as
	declare	@fIsNocountOn int, @Err int, @nTrnCnt int, @sTranName varchar(300)
	declare @i int, @RowsAffected int

	set @Err = 0

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- determine if the procedure was called within a transaction; if yes then create a savepoint,
	--	otherwise create a transaction
	set @sTranName = ''ReplaceRef$_'+@sTbl+''' + convert(varchar(11), @@trancount)
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
	set @Err = @@error
	if @Err <> 0 begin
		raiserror(''ReplaceRefSeq_'+@sTbl+': SQL Error %d; Unable to create a transaction'', 16, 1, @Err)
		goto LCleanup
	end

	-- determine if any object references should be removed
	if @hXMLDocDelete is not null begin
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
			raiserror(''ReplaceRefColl_'+@sTbl+': SQL Error %d; Unable to create a temporary table'', 16, 1, @Err)
			goto LCleanup
		end

		-- get the objects that are referenced more than once along with the actual number of references; do this
		--	only for the objects where at least one reference is going to be removed
		insert into @t (DstObjId, DelOccurrences, Occurrences)
		select	jt.[Dst], ol.[DelCnt], count(*)
		from	['+@sTbl+'] jt (REPEATABLEREAD)
			join (
				select	[Id] ObjId, count(*) DelCnt
				from	openxml(@hXMLDocDelete, ''/root/Obj'') with ([Id] int)
				group by [Id]
			) as ol on jt.[Dst] = ol.[ObjId]
		where	jt.[Src] = @SrcObjid
		group by jt.[Dst], ol.[DelCnt]
		having count(*) > 1
		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplaceRefColl_'+@sTbl+': SQL Error %d; Unable to insert objects that are referenced more than once: SrcObjId(src) = %d'',
					16, 1, @Err, @SrcObjId)
			goto LCleanup
		end
'
set @sDynSql2 = N'
		-- remove the object references
		-- for some reason the preferable DELETE...FROM query generates server exceptions when the openxml statement is
		--	used, so the only alternative is to use IN (...)
		delete	['+@sTbl+']
		where	[Src] = @SrcObjId
			and [Dst] in (
				select	[Id]
				from	openxml(@hXMLDocDelete, ''/root/Obj'') with ([Id] int)
			)
		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplaceRefColl_'+@sTbl+': SQL Error %d; Unable to delete objects from a reference collection: SrcObjId(src) = %d'',
					16, 1, @Err, @SrcObjId)
			goto LCleanup
		end

		-- reinsert the appropriate number of objects that had multiple references
		set @i = 0
		set @RowsAffected = 1 -- set to 1 to get inside of the loop
		while @RowsAffected > 0 begin
			insert into ['+@sTbl+'] with (REPEATABLEREAD) ([Src], [Dst])
			select	@SrcObjid, [DstObjId]
			from	@t
			where	Occurrences - DelOccurrences > @i
			select @Err = @@error, @RowsAffected = @@rowcount
			if @Err <> 0 begin
				raiserror(''ReplaceRefColl_'+@sTbl+': SQL Error %d; Unable to reinsert objects into a reference collection: SrcObjId(src) = %d'',
						16, 1, @Err, @SrcObjId)
				goto LCleanup
			end
			set @i = @i + 1
		end

	end

	-- determine if any object references should be inserted
	if @hXMLDocInsert is not null begin

		insert into ['+@sTbl+'] with (REPEATABLEREAD) ([Src], [Dst])
		select	@SrcObjId, ol.[Id]
		from	openxml(@hXMLdocInsert, ''/root/Obj'') with (Id int) as ol
		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplaceRefColl_'+@sTbl+': SQL Error %d; Unable to insert objects into a reference collection: SrcObjId(src) = %d'',
					16, 1, @Err, @SrcObjId)
			goto LCleanup
		end
	end

LCleanup:
	if @Err <> 0 rollback tran @sTranName
	else if @nTrnCnt = 0 commit tran @sTranName

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	-- determine if the XML documents should be removed
	if @fRemoveXMLDocs = 1 begin
		if @hXMLdocInsert is not null exec sp_xml_removedocument @hXMLdocInsert
		if @hXMLdocDelete is not null and @hXMLdocDelete <> @hXMLdocInsert exec sp_xml_removedocument @hXMLdocDelete
	end

	return @Err
'

	exec ( @sDynSql + @sDynSql2 )
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('DefineReplaceRefCollProc: SQL Error %d: Unable to create or alter the procedure ReplaceRefColl_%s$',
				16, 1, @Err, @sTbl)
		return @err
	end

	return 0
go

------------------------------------------------------------------------------------
if object_id('DefineReplaceRefSeqProc$') is not null begin
	print 'removing proc DefineReplaceRefSeqProc$'
	drop proc [DefineReplaceRefSeqProc$]
end
go
print 'Creating proc DefineReplaceRefSeqProc$'
go
create proc [DefineReplaceRefSeqProc$]
	@sTbl sysname,
	@flid int
as
	declare @sDynSql nvarchar(4000), @sDynSql2 nvarchar(4000), @sDynSql3 nvarchar(4000), @sDynSql4 nvarchar(4000)
	declare @err int

	if object_id('ReplaceRefSeq_' + @sTbl) is not null begin
		set @sDynSql = 'alter '
	end
	else begin
		set @sDynSql = 'create '
	end

set @sDynSql = @sDynSql +
N'proc ReplaceRefSeq_' + @sTbl +'
	@SrcObjId int,
	@ListStmp int,
	@hXMLdoc int = null,
	@StartObj int = null,
	@StartObjOccurrence int = 1,
	@EndObj int = null,
	@EndObjOccurrence int = 1,
	@fRemoveXMLdoc tinyint = 1
as
	declare	@fIsNocountOn int, @Err int, @nTrnCnt int, @sTranName varchar(300)
	declare @nNumObjs int, @iCurObj int, @nMinOrd int, @StartOrd int, @EndOrd int
	declare @nSpaceAvail int
	declare @UpdStmp int

	set @Err = 0

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on
'
/*
	Cannot do this until SeqStmp$ is enabled.
	select @UpdStmp = [UpdStmp] from SeqStmp$ where [Src] = @SrcObjId and [Flid] = ' + convert(varchar(11), @flid) + '
	if @UpdStmp is not null and @ListStmp <> @UpdStmp begin
		raiserror(''The sequence list in '+@sTbl+' has been modified: SrcObjId = %d SrcFlid = ' + convert(varchar(11), @flid) + '.'',
				16, 1, @SrcObjId)
		return 50000
	end
*/
set @sDynSql = @sDynSql +
N'
	-- determine if the procedure was called within a transaction; if yes then create a savepoint,
	--	otherwise create a transaction
	set @sTranName = ''ReplaceRefSeq_'+@sTbl+''' + convert(varchar(11), @@trancount)
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
	set @Err = @@error
	if @Err <> 0 begin
		raiserror(''ReplaceRefSeq_'+@sTbl+': SQL Error %d; Unable to create a transaction'', 16, 1, @Err)
		goto LFail
	end

	-- get the starting and ending ordinal values
	set @EndOrd = null
	set @StartOrd = null
	if @StartObj is null begin
		-- since the @StartObj is null the list of objects should be added to the end of the sequence, so
		--	get the maximum ord value and add 1
		select	@StartOrd = coalesce(max([Ord]), 0) + 1
		from	['+@sTbl+'] with (REPEATABLEREAD)
		where	[Src] = @SrcObjId
	end
	else begin
		-- create a temporary table to hold all of the ord values associated with Src=@SrcObjId and (Dst=
		--	@StartObj or Dst=@EndObj); this table will have an identity column so subsequent queries
		--	can easily determine which ord value is associated with a particular position in a sequence
		declare @t table (
			Occurrence int identity(1,1),
			IsStart	tinyint,
			Ord int
		)

'
set @sDynSql2 = N'
		-- determine if an end object was not specified, or if the start and end object are the same
		if @EndObj is null or (@EndObj = @StartObj) begin
			-- only collect occurrences for the start object

			-- limit the number of returned rows from a select based on the desired occurrence; this will
			--	avoid processing beyond the desired occurrence
			if @EndObj is null set rowcount @StartObjOccurrence
			else set rowcount @EndObjOccurrence

			-- insert all of the Ord values associated with @StartObj
			insert into @t (IsStart, Ord)
			select	1, [Ord]
			from	[' + @sTbl + ']
			where	[Src] = @SrcObjId
				and [Dst] = @StartObj
			order by [Ord]
			set @Err = @@error
			if @Err <> 0 begin
				raiserror(''ReplaceRefSeq_'+@sTbl+': SQL Error %d; Unable to insert ord values into the temporary table'', 16, 1, @Err)
				goto LFail
			end

			-- make selects return all rows
			set rowcount 0

			-- determine if the end and start objects are the same; if they are then search for the
			--	end object''s ord value based on the specified occurrence
			if @EndObj = @StartObj begin
				select	@EndOrd = [Ord]
				from	@t
				where	[Occurrence] = @EndObjOccurrence
			end
		end
		else begin
			-- insert Ord values associated with @StartObj and @EndObj
			insert into @t ([IsStart], [Ord])
			select	case [Dst]
					when @StartObj then 1
					else 0
				end,
				[Ord]
			from	[' + @sTbl + ']
			where	[Src] = @SrcObjId
				and ( [Dst] = @StartObj
					or [Dst] = @EndObj )
			order by 1 desc, [Ord]
			set @Err = @@error
			if @Err <> 0 begin
				raiserror(''ReplaceRefSeq_'+@sTbl+': SQL Error %d; Unable to insert ord values into the temporary table'', 16, 1, @Err)
				goto LFail
			end

			-- get the end ord value associated with @EndObjOccurrence
			select	@EndOrd = [Ord]
			from	@t
			where	[IsStart] = 0
				and [Occurrence] = @EndObjOccurrence +
					( select max([Occurrence]) from @t where [IsStart] = 1 )
		end

		-- get the start ord value associated with @StartObjOccurrence
		select	@StartOrd = [Ord]
		from	@t
		where	[IsStart] = 1
			and [Occurrence] = @StartObjOccurrence

	end
'
set @sDynSql3 = N'
	-- validate the arguments
	if @StartOrd is null begin
		raiserror(''ReplaceRefSeq_'+@sTbl+': Unable to locate ordinal value: SrcObjId(Src) = %d, StartObj(Dst) = %d, StartObjOccurrence = %d'',
				16, 1, @SrcObjId, @StartObj, @StartObjOccurrence)
		set @Err = 50001
		goto LFail
	end
	if @EndOrd is null and @EndObj is not null begin
		raiserror(''ReplaceRefSeq_'+@sTbl+': Unable to locate ordinal value: SrcObjId(Src) = %d, EndObj(Dst) = %d, EndObjOccurrence = %d'',
				16, 1, @SrcObjId, @EndObj, @EndObjOccurrence)
		set @Err = 50002
		goto LFail
	end
	if @EndOrd is not null and @EndOrd < @StartOrd begin
		raiserror(''ReplaceRefSeq_'+@sTbl+': The starting ordinal value %d is greater than the ending ordinal value %d: SrcObjId(Src) = %d, StartObj(Dst) = %d, StartObjOccurrence = %d, EndObj(Dst) = %d, EndObjOccurrence = %d'',
				16, 1, @StartOrd, @EndOrd, @SrcObjId, @StartObj, @StartObjOccurrence, @EndObj, @EndObjOccurrence)
		set @Err = 50003
		goto LFail
	end

	-- check for a delete/replace
	if @EndObj is not null begin

		delete	[' + @sTbl + '] with (REPEATABLEREAD)
		where	[Src] = @SrcObjId
			and [Ord] >= @StartOrd
			and [Ord] <= @EndOrd
		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplaceRefSeq_'+@sTbl+': SQL Error %d; Unable to remove objects between %d and %d for source = %d'',
					16, 1, @Err, @StartOrd, @EndOrd, @SrcObjId)
			goto LFail
		end
	end

	-- determine if any objects are going to be inserted
	if @hXMLDoc is not null begin
		-- get the number of objects to be inserted
		select	@nNumObjs = count(*)
		from 	openxml(@hXMLdoc, ''/root/Obj'') with (Id int)
		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplaceRefSeq_'+@sTbl+': SQL Error %d; Unable to process XML document: document handle = %d'',
					16, 1, @hXMLdoc)
			goto LFail
		end
'
set @sDynSql4 = N'
		-- if the objects are not appended to the end of the list then determine if there is enough room
		if @StartObj is not null begin

			-- find the largest ordinal value less than the start object''s ordinal value
			select	@nMinOrd = coalesce(max([Ord]), -1) + 1
			from	['+@sTbl+'] with (REPEATABLEREAD)
			where	[Src] = @SrcObjId
				and [Ord] < @StartOrd

			-- determine if a range of objects was deleted; if objects were deleted then there is more room
			--	available
			if @EndObj is not null begin
				-- the actual space available could be determined, but this would involve another
				--	query (this query would look for the minimum Ord value greater than @EndOrd);
				--	however, it is known that at least up to the @EndObj is available

				set @nSpaceAvail = @EndOrd - @nMinOrd
				if @nMinOrd > 0 set @nSpaceAvail = @nSpaceAvail + 1
			end
			else begin
				set @nSpaceAvail = @StartOrd - @nMinOrd
			end

			-- determine if space needs to be made
			if @nSpaceAvail < @nNumObjs begin
				update	[' + @sTbl + '] with (REPEATABLEREAD)
				set	[Ord] = [Ord] + @nNumObjs - @nSpaceAvail
				where	[Src] = @SrcObjId
					and [Ord] >= @nMinOrd
				set @Err = @@error
				if @Err <> 0 begin
					raiserror(''ReplaceRefSeq_'+@sTbl+': SQL Error %d; Unable to increment the ordinal values; src = %d'',
							16, 1, @Err, @SrcObjId)
					goto LFail
				end
			end
		end
		else begin
			-- find the largest ordinal value plus one
			select	@nMinOrd = coalesce(max([Ord]), -1) + 1
			from	['+@sTbl+'] with (REPEATABLEREAD)
			where	[Src] = @SrcObjId
		end

		insert into [' + @sTbl + '] with (REPEATABLEREAD) ([Src], [Dst], [Ord])
		select	@SrcObjId, ol.[Id], ol.[Ord] + @nMinOrd
		from 	openxml(@hXMLdoc, ''/root/Obj'') with (Id int, Ord int) ol
		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplaceRefSeq_'+@sTbl+': SQL Error %d; Unable to insert objects into the reference sequence table'',
					16, 1, @Err)
			goto LFail
		end
	end

	if @nTrnCnt = 0 commit tran @sTranName

	-- determine if the XML document should be removed
	if @fRemoveXMLdoc = 1 and @hXMLDoc is not null exec sp_xml_removedocument @hXMLDoc

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return 0

LFail:
	rollback tran @sTranName

	-- determine if the XML document should be removed
	if @fRemoveXMLdoc = 1 and @hXMLDoc is not null exec sp_xml_removedocument @hXMLDoc

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
'

	exec ( @sDynSql + @sDynSql2 + @sDynSql3 + @sDynSql4 )
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('DefineReplaceRefSeqProc: SQL Error %d: Unable to create or alter the procedure ReplaceRefSeq_%s$',
				16, 1, @Err, @sTbl)
		return @err
	end

	return 0
go

-----------------------------------------------------------------------------------
if object_id('MoveToOwnedColl$') is not null begin
	print 'removing proc MoveToOwnedColl$'
	drop proc [MoveToOwnedColl$]
end
go
print 'Creating proc MoveToOwnedColl$'
go

create proc MoveToOwnedColl$
	@SrcObjId int,		-- The ID of the object that owns the source object(s)
	@SrcFlid int,		-- The FLID (field ID) of the object attribute that owns the object(s)
	@StartObj int = null,	-- The ID of the first object to be moved.
	@EndObj int = null,	-- The ID of the last object to be moved
	@DstObjId int,		-- The ID of the object which will own the object(s) moved
	@DstFlid int		-- The FLID (field ID) of the object attribute that will own the object(s)

as
	declare @sTranName varchar(50)
	declare @StartOrd int, @EndOrd int
	declare	@fIsNocountOn int, @Err int, @nTrnCnt int, @nSrcType int

	set @Err = 0

	-- transactions
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on
	set @sTranName = 'MoveToOwnedColl$_' + convert(varchar(11), @@trancount)
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('MoveToOwnedColl$: SQL Error %d; Unable to create a transaction', 16, 1, @Err)
		goto LFail
	end

	select @nSrcType = [Type]
	from Field$
	where [id] = @SrcFlid

	if @nSrcType = 27 begin  --( If source object is an owning sequence

		select	@StartOrd = [OwnOrd$]
		from	CmObject (repeatableread)
		where	[Owner$] = @SrcObjId
			and [OwnFlid$] = @SrcFlid
			and [Id] = @StartObj

		if @EndObj is null begin
			select	@EndOrd = max([OwnOrd$])
			from	CmObject (REPEATABLEREAD)
			where	[Owner$] = @SrcObjId
				and [OwnFlid$] = @SrcFlid
		end
		else begin
			select	@EndOrd = [OwnOrd$]
			from	CmObject (repeatableread)
			where	[Owner$] = @SrcObjId
				and [OwnFlid$] = @SrcFlid
				and [Id] = @EndObj
		end

		if @EndOrd is not null and @EndOrd < @StartOrd begin
			raiserror('MoveToOwnedColl$: The starting ordinal value %d is greater than the ending ordinal value %d in CmObject: SrcObjId(Owner$) = %d SrcFlid(OwnFlid$) = %d StartObj(Id) = %d EndObj(Id) = %d',
					16, 1, @StartOrd, @EndOrd, @SrcObjId, @SrcFlid, @StartObj, @EndObj)
			set @Err = 51001
			goto LFail
		end

		update	CmObject with (repeatableread)
		set [Owner$] = @DstObjId,
			[OwnFlid$] = @DstFlid,
			[OwnOrd$] = null
		where	[Owner$] = @SrcObjId
			and [OwnFlid$] = @SrcFlid
			and [OwnOrd$] >= @StartOrd and [OwnOrd$] <= @EndOrd
	end
	else begin
		-- ENHANCE SteveMiller: Cannot yet move more than one object from a collection to a sequence.
		-- if source is an  Owning Collection, and more than one object, complain
		if @nSrcType = 25 and not @StartObj = @EndObj begin
			raiserror('MoveToOwnedSeq$: Cannot yet move more than one object from a collection to a sequence', 16, 1)
			set @Err = 51002
			goto LFail
		end

		update	CmObject with (repeatableread)
		set [Owner$] = @DstObjId,
			[OwnFlid$] = @DstFlid,
			[OwnOrd$] = null
		where [Id] = @StartObj
	end

	set @Err = @@error
	if @Err <> 0 begin
		raiserror('MoveToOwnedColl$: SQL Error %d; Unable to update owners in CmObject: DstObjId(Owner$) = %d, DstFlid(OwnFlid$) = %d, start = %d, end = %d',
				16, 1, @Err, @DstObjId, @DstFlid, @StartOrd, @EndOrd)
		goto LFail
	end

	-- stamp the owning objects as updated
	update CmObject with (repeatableread)
		set [UpdDttm] = getdate()
		where [Id] in (@SrcObjId, @DstObjId)
		--( seems to execute as fast as a where clause written:
		--(    where [Id] = @SrcObjId or [Id] =@DstObjId

	if @nTrnCnt = 0 commit tran @sTranName

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return 0

LFail:
	rollback tran @sTranName

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
go

-----------------------------------------------------------------------------------
if object_id('MoveToOwnedSeq$') is not null begin
	print 'removing proc MoveToOwnedSeq$'
	drop proc [MoveToOwnedSeq$]
end
go
print 'Creating proc MoveToOwnedSeq$'
go

CREATE proc MoveToOwnedSeq$
	@SrcObjId int,
	@SrcFlid int,
	@StartObj int,
	@EndObj int = null,
	@DstObjId int,
	@DstFlid int,
	@DstStartObj int = null
as
	declare @sTranName varchar(50)
	declare @nDstType int
	declare @nMinOrd int, @StartOrd int, @EndOrd int, @DstStartOrd int, @DstEndOrd int, @DstEndDelOrd int
	declare @nSpaceAvail int, @nSpaceNeed int, @nNewOrdOffset int, @fMadeSpace tinyint
	declare @uid uniqueidentifier
	declare	@fIsNocountOn int, @Err int, @nTrnCnt int, @UpdStmp int, @RetVal int, @nSrcType int

	set @Err = 0
	set @fMadeSpace = 0

	--==Transactions ==--
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- determine if the procedure was called within a transaction; if yes then create a savepoint,
	--	otherwise create a transaction
	set @sTranName = 'MoveToOwnedSeq$_' + convert(varchar(11), @@trancount)
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('MoveToOwnedSeq$: SQL Error %d; Unable to create a transaction', 16, 1, @Err)
		goto LFail
	end

	--== Get Start and End Orders ==-

	--( See notes at the top of the file about the query where clause

	if @DstStartObj is null begin
		select	@DstStartOrd = coalesce(max([OwnOrd$]), -1) + 1
		from	CmObject (REPEATABLEREAD)
		where	[Owner$] = @DstObjId
			and [OwnFlid$] = @DstFlid
	end
	else begin
		select	@DstStartOrd = [OwnOrd$]
		from	CmObject (repeatableread)
		where	[Owner$] = @DstObjId
			and [OwnFlid$] = @DstFlid
			and [Id] = @DstStartObj
	end

	--( Get type (atomic, collection, or sequence) of source
	select @nSrcType = [Type]
	from Field$
	where [id] = @SrcFlid

	if @nSrcType = 27 begin  --( If source object is an owning sequence
		-- get the starting and ending ordinal values
		select	@StartOrd = [OwnOrd$]
		from	CmObject (repeatableread)
		where	[Owner$] = @SrcObjId
			and [OwnFlid$] = @SrcFlid
			and [Id] = @StartObj
		if @EndObj is null begin
			select	@EndOrd = max([OwnOrd$])
			from	CmObject (REPEATABLEREAD)
			where	[Owner$] = @SrcObjId
				and [OwnFlid$] = @SrcFlid
		end
		else begin
			select	@EndOrd = [OwnOrd$]
			from	CmObject (repeatableread)
			where	[Owner$] = @SrcObjId
				and [OwnFlid$] = @SrcFlid
				and [Id] = @EndObj
		end
	end

	-- If source object is an owning collection
	else if @nSrcType = 25 begin

		-- ENHANCE SteveMiller: Cannot yet move more than one object from a collection to a sequence.
		if not @StartObj = @EndObj begin
			raiserror('MoveToOwnedSeq$: Cannot yet move more than one object from a collection to a sequence', 16, 1)
			set @Err = 51000
			goto LFail
		end

		set @StartOrd = @DstStartOrd
		set @EndOrd = @StartOrd
	end

	-- If source object is an owning atom
	else if @nSrcType = 23 begin

		if not @StartObj = @EndObj begin
			raiserror('MoveToOwnedSeq$: Cannot move two atoms at the same time', 16, 1)
			set @Err = 51001
			goto LFail
		end

		set @StartOrd = @DstStartOrd
		set @EndOrd = @StartOrd
	end

	set @DstEndOrd = @DstStartOrd + @EndOrd - @StartOrd

	--== Validate the arguments  ==--
	if @StartOrd is null begin
		raiserror('MoveToOwnedSeq$: Unable to locate ordinal value in CmObject: SrcObjId(Owner$) = %d SrcFlid(OwnFlid$) = %d StartObj(Id) = %d',
				16, 1, @SrcObjId, @SrcFlid, @StartObj)
		set @Err = 51002
		goto LFail
	end
	if @EndOrd is null begin
		raiserror('MoveToOwnedSeq$: Unable to locate ordinal value in CmObject: SrcObjId(Owner$) = %d SrcFlid(OwnFlid$) = %d EndObj(Id) = %d',
				16, 1, @SrcObjId, @SrcFlid, @EndObj)
		set @Err = 51003
		goto LFail
	end
	if @DstStartOrd is null begin
		raiserror('MoveToOwnedSeq$: Unable to locate ordinal value in CmObject: DstObjId(Owner$) = %d DstFlid(OwnFlid$) = %d DstStartObj(Id) = %d',
				16, 1, @DstObjId, @DstFlid, @DstStartObj)
		set @Err = 51004
		goto LFail
	end
	if @EndOrd is not null and @EndOrd < @StartOrd begin
		raiserror('MoveToOwnedSeq$: The starting ordinal value %d is greater than the ending ordinal value %d in CmObject: SrcObjId(Owner$) = %d SrcFlid(OwnFlid$) = %d StartObj(Id) = %d EndObj(Id) = %d',
				16, 1, @StartOrd, @EndOrd, @SrcObjId, @SrcFlid, @StartObj, @EndObj)
		set @Err = 51005
		goto LFail
	end

	-- if the objects are not appended to the end of the destination list then determine if there is enough room
	if @DstStartObj is not null begin

		-- find the object with the largest ordinal value less than the destination start object's ordinal
		select @nMinOrd = coalesce(max([OwnOrd$]), -1)
		from	CmObject with (REPEATABLEREAD)
		where	[Owner$] = @DstObjId
			and [OwnFlid$] = @DstFlid
			and [OwnOrd$] < @DstStartOrd
		set @Err = @@error
		if @Err <> 0 begin
			raiserror('MoveToOwnedSeq$: SQL Error %d; Unable to analyze the sequence in CmObject: DstObjId(Owner$) = %d, DstFlid(OwnFlid$) = %d',
					16, 1, @Err, @DstObjId, @DstFlid)
			goto LFail
		end

		set @nSpaceAvail = @DstStartOrd - @nMinOrd - 1
		set @nSpaceNeed = @EndOrd - @StartOrd + 1

		-- see if there is currently enough room for the objects under the destination object's sequence list;
		--	if there is not then make room
		if @nSpaceAvail < @nSpaceNeed begin

			set @fMadeSpace = 1

			update	CmObject with (repeatableread)
			set	[OwnOrd$] = [OwnOrd$] + @nSpaceNeed - @nSpaceAvail
			where	[Owner$] = @DstObjId
				and [OwnFlid$] = @DstFlid
				and [OwnOrd$] >= @DstStartOrd
			set @Err = @@error
			if @Err <> 0 begin
				raiserror('MoveToOwnedSeq$: SQL Error %d; Unable to increment the ordinal values in the CmObject: DstObjId(Owner$) = %d, DstFlid(OwnFlid$) = %d',
					16, 1, @Err, @DstObjId, @DstFlid)
				goto LFail
			end
		end

		set @nNewOrdOffset = @nMinOrd + 1 - @StartOrd
	end
	else begin
		set @nNewOrdOffset = @DstStartOrd - @StartOrd
	end

	-- determine if the source and destination owning sequence is the same
	if @SrcObjId = @DstObjId and @SrcFlid = @DstFlid begin

		-- if room was made below the objects that are to be moved then the objects to be moved also
		--	had their ordinal values modified, so calculate the new source ordinal numbers so that they
		--	will remain in the range of objects that are to be moved
		if @fMadeSpace = 1 and @StartOrd > @DstStartOrd begin
			set @StartOrd =  @StartOrd + @nSpaceNeed - @nSpaceAvail
			set @EndOrd = @EndOrd + @nSpaceNeed - @nSpaceAvail
			set @nNewOrdOffset = @DstStartOrd - @StartOrd
		end

		-- update the ordinals of the specified range of objects in the specified sequence
		update	CmObject with (repeatableread)
		set	[OwnOrd$] = [OwnOrd$] + @nNewOrdOffset
		where	[Owner$] = @SrcObjId
			and [OwnFlid$] = @SrcFlid
			and [OwnOrd$] >= @StartOrd
			and [OwnOrd$] <= @EndOrd
		set @Err = @@error
		if @Err <> 0 begin
			raiserror('MoveToOwnedSeq$: SQL Error %d; Unable to update ordinals in CmObject: DstObjId(Owner$) = %d, DstFlid(OwnFlid$) = %d, start = %d, end = %d',
				16, 1, @Err, @DstObjId, @DstFlid, @StartOrd, @EndOrd)
			goto LFail
		end
	end

	-- destination and source are not the same
	else begin
		if @nSrcType = 27 begin		-- Owning Sequence
			-- update the owner of the specified range of objects in the specified sequence
			update	CmObject with (repeatableread)
			set	[Owner$] = @DstObjId,
				[OwnFlid$] = @DstFlid,
				[OwnOrd$] = [OwnOrd$] + @nNewOrdOffset
			where	[Owner$] = @SrcObjId
				and [OwnFlid$] = @SrcFlid
				and [OwnOrd$] >= @StartOrd
				and [OwnOrd$] <= @EndOrd
		end
		else if @nSrcType = 25 or @nSrcType = 23 begin	-- Owning Collection or Atom
			update	CmObject with (repeatableread)
			set	[Owner$] = @DstObjId,
				[OwnFlid$] = @DstFlid,
				[OwnOrd$] = @DstStartOrd + @nNewOrdOffset
			where	[Id] = @StartObj
		end

		set @Err = @@error
		if @Err <> 0 begin
			raiserror('MoveToOwnedSeq$: SQL Error %d; Unable to update owners in CmObject: DstObjId(Owner$) = %d, DstFlid(OwnFlid$) = %d, start = %d, end = %d',
				16, 1, @Err, @DstObjId, @DstFlid, @StartOrd, @EndOrd)
			goto LFail
		end
	end

	-- stamp the owning objects as updated
	update CmObject with (repeatableread)
		set [UpdDttm] = getdate()
		where [Id] in (@SrcObjId, @DstObjId)
		--( seems to execute as fast as a where clause written:
		--(    where [Id] = @SrcObjId or [Id] =@DstObjId

	if @nTrnCnt = 0 commit tran @sTranName

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return 0

LFail:
	rollback tran @sTranName

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
go
-------------------------------------------------------------------------------
if object_id('DeleteOwnSeq$') is not null begin
	print 'removing proc DeleteOwnSeq$'
	drop proc [DeleteOwnSeq$]
end
go
print 'creating proc DeleteOwnSeq$'
go
create proc [DeleteOwnSeq$]
	@SrcObjId int,
	@SrcFlid int,
	@ListStmp int,
	@StartObj int,
	@EndObj int = null
as
	declare @sTranName varchar(50)
	declare @StartOrd int, @EndOrd int
	declare @guid uniqueidentifier
	declare	@fIsNocountOn int, @Err int, @nTrnCnt int, @UpdStmp int

	set @Err = 0

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

/*
	select @UpdStmp = [UpdStmp] from SeqStmp$ where [Src] = @SrcObjId and [Flid] = @SrcFlid
	if @UpdStmp is not null and @ListStmp <> @UpdStmp begin
		raiserror('The sequence list in CmObject has been modified: SrcObjId(Owner$) = %d SrcFlid(OwnFlid$) = %d.',
				16, 1, @SrcObjId, @SrcFlid)
		if @fIsNocountOn = 0 set nocount off
		return 50000
	end
*/

	-- determine if the procedure was called within a transaction; if yes then create a savepoint,
	--	otherwise create a transaction
	set @sTranName = 'DeleteOwnSeq$_' + convert(varchar(11), @@trancount)
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('DeleteOwnSeq$: SQL Error %d; Unable to create a transaction', 16, 1, @Err)
		goto LFail
	end

	-- get the starting and ending ordinal values
	select	@StartOrd = [OwnOrd$]
	from	CmObject (repeatableread)
	where	[Owner$] = @SrcObjId
		and [OwnFlid$] = @SrcFlid
		and [Id] = @StartObj
	if @EndObj is null begin
		select	@EndOrd = max([OwnOrd$])
		from	CmObject (REPEATABLEREAD)
		where	[Owner$] = @SrcObjId
			and [OwnFlid$] = @SrcFlid
	end
	else begin
		select	@EndOrd = [OwnOrd$]
		from	CmObject (repeatableread)
		where	[Owner$] = @SrcObjId
			and [OwnFlid$] = @SrcFlid
			and [Id] = @EndObj
	end

	-- validate the parameters
	if @StartOrd is null begin
		raiserror('DeleteOwnSeq$: Unable to locate ordinal value in CmObject: SrcObjId(Owner$) = %d SrcFlid(OwnFlid$) = %d StartObj(Id) = %d',
				16, 1, @SrcObjId, @SrcFlid, @StartObj)

		set @Err = 51001
		goto LFail
	end
	if @EndOrd is null begin
		raiserror('DeleteOwnSeq$: Unable to locate ordinal value in CmObject: SrcObjId(Owner$) = %d SrcFlid(OwnFlid$) = %d EndObj(Id) = %d',
				16, 1, @SrcObjId, @SrcFlid, @EndObj)
		set @Err = 51002
		goto LFail
	end
	if @EndOrd < @StartOrd begin
		raiserror('DeleteOwnSeq$: The starting ordinal value %d is greater than the ending ordinal value %d in CmObject: SrcObjId(Owner$) = %d SrcFlid(OwnFlid$) = %d StartObj(Id) = %d EndObj(Id) = %d',
				16, 1, @StartOrd, @EndOrd, @SrcObjId, @SrcFlid, @StartObj, @EndObj)
		set @Err = 51004
		goto LFail
	end

	-- get the list of objects that are to be deleted and insert them into the ObjInfoTbl$ table
	set @guid = newid()
	insert into ObjInfoTbl$ with (rowlock) (uid, ObjId)
	select	@guid, [Id]
	from	CmObject with (REPEATABLEREAD)
	where	[Owner$] = @SrcObjId
		and [OwnFlid$] = @SrcFlid
		and [OwnOrd$] >= @StartOrd
		and [OwnOrd$] <= @EndOrd
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('DeleteOwnSeq$: SQL Error %d; Unable to insert objects into the ObjInfoTbl$; SrcObjId(Owner$) = %d SrcFlid(OwnFlid$) = %d StartObj(Id) = %d EndObj(Id) = %d',
				16, 1, @Err, @SrcObjId, @SrcFlid, @StartObj, @EndObj)
		goto LFail
	end

	-- delete the objects that were inserted into the ObjInfoTbl$
	exec @Err = DeleteObject$ @guid, null, 1
	if @Err <> 0 goto LFail

	if @nTrnCnt = 0 commit tran @sTranName

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return 0

LFail:
	rollback tran @sTranName

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
go

-------------------------------------------------------------------------------

if object_id('AddCustomField$') is not null begin
	print 'removing proc AddCustomField$'
	drop proc [AddCustomField$]
end
go
print 'creating proc AddCustomField$'
go
create proc [AddCustomField$]
	@flid int output,
	@name varchar(100),
	@type int,
	@clid int,
	@clidDst int = null,
	@Min bigint = null,
	@Max bigint = null,
	@Big bit = null,
	@nvcUserLabel	NVARCHAR(100) = NULL,
	@nvcHelpString	NVARCHAR(100) = NULL,
	@nListRootId	INT  = NULL,
	@nWsSelector	INT = NULL,
	@ntXmlUI		NTEXT = NULL
AS
	declare @flidNew int, @flidMax int
	declare @sql varchar(1000)
	declare @Err int

	-- If this procedure was called within a transaction, then create a savepoint; otherwise
	-- create a transaction.
	declare @nTrnCnt int
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran AddCustomField$_Tran
	else save tran AddCustomField$_Tran

	-- calculate the new flid
	select	@flidMax = max([Id])
	from	[Field$] (REPEATABLEREAD)
	where	[Class] = @clid
	if @flidMax is null or @flidMax - @clid * 1000 < 500 set @flidNew = 1000 * @clid + 500
	else set @flidNew = @flidMax + 1
	set @flid = @flidNew

	-- perform the insert into Field$
	insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [Min], [Max], [Big],
		UserLabel, HelpString, ListRootId, WsSelector, XmlUI)
	values (@flidNew, @type, @clid, @clidDst, @name, 1, @Min, @Max, @Big,
		@nvcUserLabel, @nvcHelpString, @nListRootId, @nWsSelector, @ntXmlUI)

	set @Err = @@error
	if @Err <> 0 goto HandleError

	if @nTrnCnt = 0 commit tran AddCustomField$_Tran
	return 0

HandleError:
	rollback tran AddCustomField$_Tran
	return @Err
go

-------------------------------------------------------------------------------
-- From LingSp.sql
-------------------------------------------------------------------------------
if object_id('SetAgentEval') is not null begin
	print 'removing proc SetAgentEval'
	drop proc SetAgentEval
end
go
print 'creating proc SetAgentEval'
go

/*****************************************************************************
 * Procedure: SetAgentEval
 *
 * Description:
 *		Updates an agent evaluation with the latest acceptance, details, and
 *		date-time of the evaluation. Creates a new agent evaluation if one
 *		doesn't already exist. If the Agent doesn't know, the accepted flag
 *		is set to 2 (or optionally NULL), and the associated evaluations will
 *		be deleted.
 *
 * Parameters:
 * 		@nAgentID	ID of the agent
 *      @nTargetID	ID of the thing analyzed, whether a WfiAnalysis or a
 *						WfiWordform.
 *		@nAccepted	Has the agent evaluation been accepted by Agent?
 *						0 = not accepted
 *						1 = accepted
 *						2 = don't know
 *						NULL = don't know
 *		@nvcDetails	Additional detail
 *		@dtEval		Date-time of the evaluation
 *
 * Returns:
 *		0 for success, otherwise an error code.
 *****************************************************************************/

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
		@nvcError NVARCHAR(100)

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
			EXEC DeleteObj$ @nEvalId

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

			EXEC @nError = CreateObject_CmAgentEvaluation
				@dtEval,
				@nAccepted,
				@nvcDetails,
				@nAgentId,					--(owner
				23006,	--(ownflid  23006
				NULL,						--(startobj
				@nNewObjId OUTPUT,
				@guidNewObj OUTPUT,
				0,			--(ReturnTimeStamp
				@nNewObjTimeStamp OUTPUT

			IF @nError != 0 BEGIN
				SET @nvcError = 'SetAgentEval: CreateObject_CmAgentEvaluation failed.'
				GOTO Fail
			END

			UPDATE CmAgentEvaluation  WITH (REPEATABLEREAD)
			SET Target = @nTargetID
			WHERE Id = @nNewObjId
		END

		--( Update the existing Agent Evaluation
		ELSE

			UPDATE CmAgentEvaluation WITH (REPEATABLEREAD)
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

-------------------------------------------------------------------------------

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200017
begin
	update Version$ set DbVer = 200018
	COMMIT TRANSACTION
	print 'database updated to version 200018'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200017 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO