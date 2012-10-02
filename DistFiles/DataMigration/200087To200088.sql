-- Update database from version 200087 to 200088
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Fixed problem where some earlier migration failed to create stored procs
-- MoveToOwnedColl$ and MoveToOwnedSeq$

if object_id('MoveToOwnedColl$') is not null begin
	print 'removing proc MoveToOwnedColl$'
	drop proc [MoveToOwnedColl$]
end
go
print 'creating proc MoveToOwnedColl$'
go
/*
----------------------------------------------------------------------------------------------------
--( MoveToOwnedColl$
----------------------------------------------------------------------------------------------------
--( Responsibility:	S. A. Miller, previously Jonathan Richards
--( Last Reviewed:
--( Description:
--( 	Moves one or more objects from one list to a collection list
--(
--( Parameters:
--( 	@SrcObjId int,		-- The ID of the object that owns the source object(s)
--( 	@SrcFlid int,		-- The FLID (field ID) of the object attribute that owns the object(s)
--( 	@StartObj int = null,	-- The ID of the first object to be moved.
--( 	@EndObj int = null,	-- The ID of the last object to be moved
--( 	@DstObjId int,		-- The ID of the object which will own the object(s) moved
--( 	@DstFlid int		-- The FLID (field ID) of the object attribute that will own the object(s)
--(
--( Returns:
--( 	0 if successful, otherwise appropriate error code.
--(
--( Notes:
--( 	In various queries below, Owner$, OwnFlid$, and ID are all used in the where clause.
--( 	The ID field is a primary key, and uniquely identifies the row we
--( 	are interestend in.  The DstObjId and DstFlid fields can also be used to uniquely
--( 	identify the row. They are used together to validate that the DstObjID and
--( 	DstFld belongs to the ID.
--(
--( 	MoveOwnedSeq, from which this procedure originated, had the capability
--( 	to replace a set of objects in the target list. This functionaltfy is no longer
--( 	supported.
--(
--( 	Copied and modified from MoveOwnedSeq$
--(
--( Example Call:
--( 	Currently (June 2001) this is called from the stored procedure MoveOwnedObject. When and
--( 	if this becomes a stand-alone procedure, the call would look something like this:
--(
--( 		execute MoveToOwnedColl$ 1569, 4001001, 1660, 1660, 1579, 4004009, 1580
----------------------------------------------------------------------------------------------------
*/
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

	-- Remark these constants for production code. For coding and testing with Query Analyzer,
	-- unremark these constants and put an @ in front of the variables wherever they appear.









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


if object_id('MoveToOwnedSeq$') is not null begin
	print 'removing proc MoveToOwnedSeq$'
	drop proc [MoveToOwnedSeq$]
end
go
print 'creating proc MoveToOwnedSeq$'
go
/*
----------------------------------------------------------------------------------------------------
--( MoveToOwnedSeq$
----------------------------------------------------------------------------------------------------
--( Responsibility:	S. A. Miller, previously Jonathan Richards
--( Last Reviewed:
--(
--( Description:
--( 	Moves one or more objects from one list to a sequence list, or from one spot in a sequence
--( 	list to another spot in a sequence list.
--(
--( Parameters:
--( 	@SrcObjId int,		-- The ID of the object that owns the source object(s)
--( 	@SrcFlid int,		-- The FLID (field ID) of the object attribute that owns the object(s)
--( 	@StartObj int,		-- The ID of the first object to be moved
--( 	@EndObj int = null,	-- The ID of the last object to be moved
--( 	@DstObjId int,		-- The ID of the object which will own the object(s) moved
--( 	@DstFlid int,		-- The FLID (field ID) of the object attribute that will own the object(s)
--( 	@DstStartObj int = null	-- the ID of the object before which the object(s) will
--( 						-- be moved. If null, the objects will be appended
--( 						-- to the list.
--(
--( Returns:
--( 	0 if successful, otherwise appropriate error code.
--(
--( Notes:
--( 	In various queries below, Owner$, OwnFlid$, and ID are all used in the where clause.
--( 	The ID field is a primary key, and uniquely identifies the row we
--( 	are interestend in.  The DstObjId and DstFlid fields can also be used to uniquely
--( 	identify the row. They are used together to validate that the DstObjID and
--( 	DstFld belongs to the ID.
--(
--( 	MoveOwnedSeq, from which this procedure originated, had the capability
--( 	to replace a set of objects in the target list. This functionaltfy is no longer
--( 	supported.
--(
--( 	Copied and modified from MoveOwnedSeq$
--(
--( Example Call:
--( 	Currently (June 2001) this is called from the stored procedure MoveOwnedObject. When and
--( 	if this becomes a stand-alone procedure, the call would look something like this:
--(
--( 		execute MoveToOwnedSeq$ 1569, 4001001, 1660, 1660, 1579, 4004009, 1580
--(
--------------------------------------------------------------------------------------------------
*/
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

	-- Remark these constants for production code. For coding and testing with Query Analyzer,
	-- unremark these constants and put an @ in front of the variables wherever they appear.









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
		if @nSrcType = 27 begin
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
		else if @nSrcType = 25 or @nSrcType = 23 begin
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

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200087
begin
	update Version$ set DbVer = 200088
	COMMIT TRANSACTION
	print 'database updated to version 200088'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200087 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
