DbgNuke(MoveToOwnedColl$, proc)
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
	/*
	declare @kcptOwningSequence int
	set @kcptOwningSequence = 27
	declare @kcptOwningCollection int
	set @kcptOwningCollection = 25
	declare @kcptOwningAtom int
	set @kcptOwningAtom = 23
	*/

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

	if @nSrcType = kcptOwningSequence begin  --( If source object is an owning sequence

		select	@StartOrd = [OwnOrd$]
		from	CmObject
		where	[Owner$] = @SrcObjId
			and [OwnFlid$] = @SrcFlid
			and [Id] = @StartObj

		if @EndObj is null begin
			select	@EndOrd = max([OwnOrd$])
			from	CmObject
			where	[Owner$] = @SrcObjId
				and [OwnFlid$] = @SrcFlid
		end
		else begin
			select	@EndOrd = [OwnOrd$]
			from	CmObject
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

		update	CmObject
		set [Owner$] = @DstObjId,
			[OwnFlid$] = @DstFlid,
			[OwnOrd$] = null
		where	[Owner$] = @SrcObjId
			and [OwnFlid$] = @SrcFlid
			and [OwnOrd$] >= @StartOrd and [OwnOrd$] <= @EndOrd
	end
	else begin
		-- ENHANCE SteveMiller: Cannot yet move more than one object from a collection to a sequence.
		if @nSrcType = kcptOwningCollection and not @StartObj = @EndObj begin
			raiserror('MoveToOwnedSeq$: Cannot yet move more than one object from a collection to a sequence', 16, 1)
			set @Err = 51002
			goto LFail
		end

		update	CmObject
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
	update CmObject
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
