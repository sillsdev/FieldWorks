/***********************************************************************************************
 * GetObjInOwnershipPathWithId$
 *
 * Description:
 *	retrieves the closest object(s) in the ownership path of class riid
 *
 * Parameters:
 *	@ObjId=Id of the object;
 *	@riid=class of the retrieved object(s)
 *
 * Returns:
 *	0 if successful, otherwise an error code
 *
 * Notes:
 *	if @ObjId is not specified this procedure works on all of the rows in the ObjInfTbl$
 *	where uid=@uid
 **********************************************************************************************/
if object_id('GetObjInOwnershipPathWithId$') is not null begin
	print 'removing proc GetObjInOwnershipPathWithId$'
	drop proc [GetObjInOwnershipPathWithId$]
end
go
print 'creating proc GetObjInOwnershipPathWithId$'
go
create proc [GetObjInOwnershipPathWithId$]
	@uid uniqueidentifier output,
	@objId int=NULL,
	@riid int
as
	declare	@iOwner int, @iOwnerClass int, @iCurObjId int, @iPrevObjId int,
		@Err int, @fIsNoCountOn int
	declare @sUid nvarchar(50)

	set @Err = 0
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- determine if an object was supplied as an argument, if one was not use ObjInfoTbl$ as the list of objects
	if @objId is not null begin
		-- get a unique value to identify this invocation's results
		set @uid = newid()

		-- get the class of the specified object
		insert into [ObjInfoTbl$] with (rowlock) (uid, ObjId, ObjClass, InheritDepth, OwnerDepth, ordkey)
		select	@uid, @objId, co.[Class$], null, null, null
		from	[CmObject] co
		where	co.[Id] = @objId

		set @Err = @@error
		if @Err <> 0 begin
			set @sUid = convert(nvarchar(50), @Uid)
			raiserror ('GetObjInOwnershipPathWithId$: SQL Error %d; Unable to insert the initial object (UID=%s).', 16, 1, @Err, @sUid)
			goto LFail
		end
	end
	else begin
		update	[ObjInfoTbl$] with (rowlock)
		set	[ObjClass]=co.[Class$], [OwnerDepth]=null, [InheritDepth]=null, [RelObjId]=null,
			[RelObjClass]=null, [RelObjField]=null,	[RelOrder]=null, [RelType]=null, [OrdKey]=null
		from	[ObjInfoTbl$] oi
				join [CmObject] co on oi.[ObjId] = co.[Id]
		where	oi.[uid]=@uid

		set @Err = @@error
		if @Err <> 0 begin
			set @sUid = convert(nvarchar(50), @Uid)
			raiserror ('GetObjInOwnershipPathWithId$: SQL Error %d; Unable to update the initial object(s) (UID=%s).', 16, 1, @Err, @sUid)
			goto LFail
		end
	end

	select	@iCurObjId=min(ObjId)
	from	ObjInfoTbl$ (REPEATABLEREAD)
	where	uid=@uid

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

		if @iCurObjId is not null
		begin
			-- update the ObjInfoTbl$ so that specified object(s) is/are related to the specified type of
			--    object (class=riid) that owns it
			update	[ObjInfoTbl$] with (rowlock)
			set	[RelObjId]=@iOwner,
				[RelObjClass]=(
					select co.[Class$]
					from [CmObject] co
					where co.[id]=@iOwner)
			where	[uid]=@uid
				and [ObjId]=@iPrevObjId

			set @Err = @@error
			if @Err <> 0 begin
				set @sUid = convert(nvarchar(50), @Uid)
				raiserror ('GetObjInOwnershipPathWithId$: SQL Error %d; Unable to update object relationship information (UID=%s).', 16, 1, @Err, @sUid)
				goto LFail
			end
		end

		-- if the user specified an object there was only one object to process and we can therefore
		--    break out of the loop
		if @objId is not null break

		select	@iCurObjId=min(ObjId)
		from	[ObjInfoTbl$] (REPEATABLEREAD)
		where	[uid]=@uid
			and [ObjId] > @iPrevObjId
	end

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
go
