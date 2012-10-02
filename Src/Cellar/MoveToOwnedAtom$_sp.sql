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

IF OBJECT_ID('MoveToOwnedAtom$') IS NOT NULL BEGIN
	PRINT 'removing procedure MoveToOwnedAtom$'
	DROP PROC MoveToOwnedAtom$
END
GO
PRINT 'creating procedure MoveToOwnedAtom$'
GO

create proc MoveToOwnedAtom$
	@ObjId int,
	@DstObjId int,
	@DstFlid int
as
	declare @sTranName varchar(50)
	declare	@fIsNocountOn int, @Err int, @nTrnCnt int
	declare @OldOwnerId int, @OldOwningFlid int, @nSrcType int,
		@nDstType int, @OriginalOrd int, @oldOwnedObject int,
		@StrId NVARCHAR(20);

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
	from CmObject
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
	select top 1 @oldOwnedObject = Id
	from CmObject
	where OwnFlid$=@DstFlid and Owner$=@DstObjId

	if @oldOwnedObject > 0 begin
		SET @StrId = CONVERT(NVARCHAR(20), @oldOwnedObject);
		EXEC DeleteObjects @StrId
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

	update	CmObject
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
		update CmObject
		set [OwnOrd$]=[OwnOrd$] - 1
		where [Owner$]=@OldOwnerId and [OwnFlid$]=@OldOwningFlid and [OwnOrd$] > @OriginalOrd
	end

	-- stamp the owning objects as updated
	update CmObject
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
