/***********************************************************************************************
 * Trigger: TR_CmObject$_RI_Del
 *
 * Description:
 *	This trigger handles the referential integrity of CmObject. Thus, it makes sure that
 *	rows are not modified or deleted that would break a foreign key type reference from
 *	another table to CmObject. In other words, it makes sure that an object's rows are not
 *	removed from CmObject while it still exists in other class tables.
 *
 * Type: 	Update, Delete
 * Table:	CmObject
 *
 * Notes:
 *	A trigger was used instead of several foreign key relationships for performance
 *	reasons. If foreign keys are used then whenever a row is deleted from CmObject, SQL
 *	Server would have to check every table that references CmObject, regardless of the type
 *	of object being deleted, and regardless of the type of class (table) that references
 *	CmObject. Thus, instead, the trigger determines the type of object that is being deleted,
 *	and only checks the corresponding class.
 *
 **********************************************************************************************/
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
	from	[ObjListTbl$]
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
		from	[ObjListTbl$]
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
