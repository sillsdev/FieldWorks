-- Update database from version 200084 to 200085
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

if object_id('MoveOwnedObject$') is not null begin
	print 'removing proc MoveOwnedObject$'
	drop proc [MoveOwnedObject$]
end
go
print 'creating proc MoveOwnedObject$'
go

----------------------------------------------------------------------------------------------------
--( MoveOwnedObject$
----------------------------------------------------------------------------------------------------
--( Responsibility:	S. A. Miller, previously Jonathan Richards
--( Last Reviewed:
--(
--( Description:
--( 	Moves one or more objects from one list to another list, or from one spot in a list
--( 	to another spot in a list.
--(
--( Parameters:
--( 	@SrcObjId int,		-- The ID of the object that owns the source object(s)
--( 	@SrcFlid int,		-- The FLID (field ID) of the object attribute that owns the object(s)
--( 	@ListStmp int,		-- The timestamp value of the object(s) to be moved. Unused.
--( 	@StartObj int = null,	-- The ID of the first object to be moved
--( 	@EndObj int = null,	-- The ID of the last object to be moved
--( 	@DstObjId int,		-- The ID of the object which will own the object(s) moved
--( 	@DstFlid int,		-- The FLID (field ID) of the object attribute that will own the object(s)
--( 	@DstStartObj int = null	-- the ID of the object before which the object(s) will
--( 						-- be moved. If null, the objects will be appended
--( 						-- to the list. Used for MoveToOwnedSeq$ only.
--( 	See comments after each of the parameters below.
--(
--( Returns:
--( 	0 if successful, otherwise appropriate error code.
--(
--( Notes:
--( 	This is a "wrapper" procedure. The intent is that eventually the procedures this
--( 	procedure calls will be stand-alone procedures. As I understand it, some
--( 	COM objects have to be written before that can happen.
--(
--( 	MoveOwnedSeq, from which this procedure originated, had the capability
--( 	to replace a set of objects in the target list. This functionaltfy is no longer
--( 	supported
--(
--( 	Copied and modified from MoveOwnSeq$
--(
--( Example Call:
--( 	execute MoveOwnedObject$ 1569, 4001001, NULL, 1660, 1660, 1579, 4004009, 1580
--(
--( Useful Query Examples:
--( 	select *
--( 	from cmObject
--( 	where owner$ = 1569
--( 		and ownflid$ = 4001001
--(
--( 	select *
--( 	from cmObject
--( 	where owner$ = 1579
--( 		and ownflid$ = 4004009
--------------------------------------------------------------------------------------------------
create proc MoveOwnedObject$
	@SrcObjId int,
	@SrcFlid int,
	@ListStmp int,
	@StartObj int = null,
	@EndObj int = null,
	@DstObjId int,
	@DstFlid int,
	@DstStartObj int = null
as
	declare @Err int, @nDstType int

	/*
	-- Remark these constants for production code. For coding and testing with Query Analyzer,
	-- unremark these constants and put an @ in front of the variables wherever they appear.
	declare @kcptOwningSequence int
	set @kcptOwningSequence = 27
	declare @kcptOwningCollection int
	set @kcptOwningCollection = 25
	declare @kcptOwningAtom int
	set @kcptOwningAtom = 23
	*/

	set @Err = 0

	-- Get if atomic, collection, or sequence for destination
	select @nDstType = [Type]
	from Field$
	where [id] = @DstFlid

	-- Destination type is owning sequence
	if @nDstType = 27 begin
		execute MoveToOwnedSeq$ @SrcObjId, @SrcFlid, @StartObj, @EndObj, @DstObjId, @DstFlid, @DstStartObj
	end

	-- Destinaton type is owning collection
	else if @nDstType = 25 begin
		execute MoveToOwnedColl$ @SrcObjId, @SrcFlid, @StartObj, @EndObj, @DstObjId, @DstFlid
	end

	-- Destination type is owning atomic
	else if @nDstType = 23 begin
		if not @StartObj = @EndObj begin
			set @Err = 51000
			raiserror('MoveOwnedObject$: The starting and ending object IDs and  must be the same when moving an object to an owned atomic', 16, 1, @Err)
			goto LFail
		end
		execute MoveToOwnedAtom$ @StartObj, @DstObjId, @DstFlid
	end

	-- Other types not allowed
	else begin
		set @Err = 51001
		raiserror('MoveOwnedObject$: Only owned sequences and collections allowed', 16, 1, @Err)
		goto LFail
	end

	return 0

LFail:
	return @Err
GO

if object_id('MoveToOwnedAtom$') is not null begin
	print 'removing proc MoveToOwnedAtom$'
	drop proc [MoveToOwnedAtom$]
end
go
print 'creating proc MoveToOwnedAtom$'
go
----------------------------------------------------------------------------------------------------
--( MoveToOwnedAtom$
----------------------------------------------------------------------------------------------------
--( Responsibility:	S. A. Miller, previously Jonathan Richards
--( Last Reviewed:
--(
--( Description:
--( 	Moves an object to an atomic object
--(
--( Parameters:
--( 	@ObjId int,	-- The ID of the object to be moved.
--( 	@DstObjId int,		-- The ID of the object which will own the object(s) moved
--( 	@DstFlid int		-- The FLID (field ID) of the object attribute that will own the object(s)
--(
--( Returns:
--( 	0 if successful, otherwise appropriate error code.
--(
--( Notes:
--( 	MoveOwnedSeq, from which this procedure originated, had the capability
--( 	to replace a set of objects in the target list. This functionaltfy is no longer
--( 	supported.
--(
--( 	Copied and modified from MoveToOwnedColl$
--(
--( Example Call:
--( 	Currently (June 2001) this is called from the stored procedure MoveOwnedObject. When and
--( 	if this becomes a stand-alone procedure, the call would look something like this:
--(
--( 		execute MoveToOwnedAtom$ 1660, 1579, 4004009
----------------------------------------------------------------------------------------------------
create proc MoveToOwnedAtom$
	@ObjId int,
	@DstObjId int,
	@DstFlid int
as
	declare @sTranName varchar(50)
	declare	@fIsNocountOn int, @Err int, @nTrnCnt int
	declare @OldOwnerId int, @OldOwningFlid int, @nSrcType int, @nDstType int, @OriginalOrd int, @oldOwnedObject int

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
	from CmObject with (repeatableread)
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
	select top 1 @oldOwnedObject=Id
	from CmObject
	where OwnFlid$=@DstFlid and Owner$=@DstObjId
	if @oldOwnedObject > 0 begin
		exec DeleteObj$ @oldOwnedObject
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

	update	CmObject with (repeatableread)
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
		update CmObject with (repeatableread)
		set [OwnOrd$]=[OwnOrd$] - 1
		where [Owner$]=@OldOwnerId and [OwnFlid$]=@OldOwningFlid and [OwnOrd$] > @OriginalOrd
	end

	-- stamp the owning objects as updated
	update CmObject with (repeatableread)
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

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200084
begin
	update Version$ set DbVer = 200085
	COMMIT TRANSACTION
	print 'database updated to version 200085'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200084 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO