/***********************************************************************************************
 * GetSubObjects$
 *
 * Description:
 *	retrievs all owned sub-objects related to a specified owning field - this is intended
 *	for recursive relationships, i.e. sub events
 *
 * Parameters:
 *	@uid=a unique Id that identifies this call's results set within the ObjInfoTbl$ table;
 *	@ObjId=Id of the owning object object
 *	@Flid=the Field ID of the field that contains the owning relationship
 *
 * Notes:
 *	If @ObjId is not specified this procedure works on all of the rows in the ObjInfTbl$
 *	where uid=@uid
 **********************************************************************************************/
if object_id('GetSubObjects$') is not null begin
	print 'removing proc GetSubObjects$'
	drop proc [GetSubObjects$]
end
go
print 'Creating proc GetSubObjects$'
go
create proc [GetSubObjects$]
	@uid uniqueidentifier output,
	@ObjId int=NULL,
	@Flid int
as
	declare @Err int, @nRowCnt int, @nOwnerDepth int
	declare	@fIsNocountOn int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- determine if an object was supplied as an argument, if one was not use ObjInfoTbl$ as the list of objects
	if @objId is not null begin
		-- get a unique value to identify this invocation's results
		set @uid = newid()

		-- get the class of the specified object
		insert into [ObjInfoTbl$] with (rowlock) (uid, ObjId, ObjClass)
		select	@uid, @objId, co.[Class$]
		from	[CmObject] co
		where	co.[Id] = @objId
		set @Err = @@error
		if @Err <> 0 goto Finish
	end
	else begin
		update	[ObjInfoTbl$] with (rowlock)
		set	[ObjClass]=co.[Class$], [OwnerDepth]=0, [InheritDepth]=0
		from	[ObjInfoTbl$] oi
				join [CmObject] co on oi.[ObjId] = co.[Id]
					and oi.[uid]=@uid
		set @Err = @@error
		if @Err <> 0 goto Finish
	end

	-- loop through the ownership hiearchy for all sub-objects based on the specified flid (field ID)
	set @nRowCnt = 1
	set @nOwnerDepth = 1
	while @nRowCnt > 0 begin
		insert into [ObjInfoTbl$] with (rowlock)
			(uid, ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType)
		select 	@uid, co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], null, co.[OwnFlid$], co.[OwnOrd$], 25
		from 	[CmObject] co
		where 	co.[OwnFlid$] = @flid
			and exists (
				select 	*
				from 	[ObjInfoTbl$] oi
				where 	oi.[ObjId] = co.[Owner$]
					and oi.[Uid] = @uid
					and oi.[OwnerDepth] = @nOwnerDepth - 1
				)
		select @nRowCnt=@@rowcount, @Err=@@error

		if @Err <> 0 goto Finish
		set @nOwnerDepth = @nOwnerDepth + 1
	end

Finish:
	-- reestablish the initial setting of nocount
	if @fIsNocountOn = 0 set nocount off
	return @Err
go
