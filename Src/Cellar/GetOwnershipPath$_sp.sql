/***********************************************************************************************
 * GetOwnershipPath$
 *
 * Description:
 *	retrieves objects that are in the ownership chain of the specified object(s)
 *
 * Parameters:
 *	@uid=a unique Id that identifies this call's results set;
 *	@ObjId=Id of the object;
 *	@nDirection=determines if all objects in the owning chain are included (0), if only
 *		objects owned by the specified object(s) are included (1), or if only objects
 *		that own the specified object(s) are included (-1) in the results;
 *	@fRecurse=determinse if the owning tree should be tranversed (0=do not recurse the
 *		owning tree, 1=recurse the owning tree)
 *	@fCaclOrdKey=determines if the order key is calculated (0=do not caclulate the order
 *		key, 1=calculate the order key)
 *
 * Returns:
 *	0 if successful, otherwise an error code
 *
 * Notes:
 *	If @ObjId is not specified this procedure works on all of the rows in the ObjInfTbl$
 *	where uid=@uid
 **********************************************************************************************/
if object_id('GetOwnershipPath$') is not null begin
	print 'removing proc GetOwnershipPath$'
	drop proc [GetOwnershipPath$]
end
go
print 'creating proc GetOwnershipPath$'
go
create proc [GetOwnershipPath$]
	@uid uniqueidentifier output,
	@ObjId int=NULL,
	@nDirection smallint=0,
	@fRecurse tinyint=1,
	@fCalcOrdKey tinyint=1
as
	declare @Err int, @nRowCnt int, @nOwnerDepth int, @fIsNocountOn int, @sUid nvarchar(50)

	set @Err = 0

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- determine if an object was supplied as an argument, if one was not use ObjInfoTbl$ as the list of objects
	if @objId is not null begin
		-- get a unique value to identify this invocation's results
		set @uid = newid()

		-- get the class of the specified object
		insert into [ObjInfoTbl$] with (rowlock)
			(uid, ObjId, ObjClass, OwnerDepth, InheritDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
		select	@uid, @objId, co.[Class$], 0, 0, co.[Owner$], f.[Class], co.[OwnFlid$], co.[OwnOrd$], f.[Type],
			-- go ahead and calculate the order key for depth 0 objects even if @fCalcOrdKey=0
			convert(varbinary, coalesce(co.[Owner$], 0)) +
			convert(varbinary, coalesce(co.[OwnFlid$], 0)) +
			convert(varbinary, coalesce(co.[OwnOrd$], 0))
		from	[CmObject] co
				left outer join [Field$] f on co.[OwnFlid$] = f.[Id]
		where	co.[Id] = @objId

		set @Err = @@error
		if @Err <> 0 begin
			set @sUid = convert(nvarchar(50), @Uid)
			raiserror ('GetOwnershipPath$: SQL Error %d; Error inserting initial object (UID=%s).', 16, 1, @Err, @sUid)
			goto LFail
		end
	end
	else begin
		update	[ObjInfoTbl$] with (rowlock)
		set	[ObjClass]=co.[Class$], [OwnerDepth]=0, [InheritDepth]=0, [RelObjId]=co.[Owner$], [RelObjClass]=f.[Class],
			[RelObjField]=co.[OwnFlid$], [RelOrder]=co.[OwnOrd$], [RelType]=f.[Type],
			-- go ahead and calculate the order key for depth 0 objects even if @fCalcOrdKey=0
			[OrdKey]=convert(varbinary, coalesce(co.[Owner$], 0)) +
				convert(varbinary, coalesce(co.[OwnFlid$], 0)) +
				convert(varbinary, coalesce(co.[OwnOrd$], 0))
		from	[ObjInfoTbl$] oi
				join [CmObject] co on oi.[ObjId] = co.[Id]
				left outer join [Field$] f on co.[OwnFlid$] = f.[Id]
		where	oi.[uid]=@uid

		set @Err = @@error
		if @Err <> 0 begin
			set @sUid = convert(nvarchar(50), @Uid)
			raiserror ('GetOwnershipPath$: SQL Error %d; Unable to update initial objects (UID=%s).', 16, 1, @Err, @sUid)
			goto LFail
		end
	end

	-- determine if the objects owned by the specified object(s) should be included in the results
	if @nDirection = 0 or @nDirection = 1 begin
		set @nRowCnt = 1
		set @nOwnerDepth = 1
	end
	else set @nRowCnt = 0
	while @nRowCnt > 0 begin

		-- determine if the order key should be calculated - if the order key is not needed a more
		--    effecient query can be used to generate the ownership tree
		if @fCalcOrdKey = 1 begin
			-- get the objects owned at the next depth and calculate the order key
			insert into [ObjInfoTbl$] with (rowlock)
				(uid, ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
			select 	@uid, co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[DstCls], co.[OwnFlid$], co.[OwnOrd$], f.[Type],
				oi.OrdKey+convert(varbinary, co.[Owner$]) + convert(varbinary, co.[OwnFlid$]) + convert(varbinary, coalesce(co.[OwnOrd$], 0))
			from 	[CmObject] co
					join [ObjInfoTbl$] oi on co.[Owner$] = oi.[ObjId]
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	[Uid]=@uid
				and oi.[OwnerDepth] = @nOwnerDepth - 1
		end
		else begin
			-- get the objects owned at the next depth and do not calculate the order key
			insert into [ObjInfoTbl$] with (rowlock)
				(uid, ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType)
			select 	@uid, co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[DstCls], co.[OwnFlid$], co.[OwnOrd$], f.[Type]
			from 	[CmObject] co
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	exists (select 	*
					from 	[ObjInfoTbl$] oi
					where 	oi.[ObjId] = co.[Owner$]
						and oi.[Uid] = @uid
						and oi.[OwnerDepth] = @nOwnerDepth - 1
					)
		end
		select @nRowCnt=@@rowcount, @Err=@@error

		if @Err <> 0 begin
			set @sUid = convert(nvarchar(50), @Uid)
			raiserror ('GetOwnershipPath$: SQL Error %d; Unable to traverse owning hierachy - owned object(s) (UID=%s).', 16, 1, @Err, @sUid)
			goto LFail
		end

		if @fRecurse = 0 break
		set @nOwnerDepth = @nOwnerDepth + 1
	end

	-- determine if the heirarchy of objects that own the specified object(s) should be included in the results
	if @nDirection = 0 or @nDirection = -1 begin
		set @nRowCnt = 1
		set @nOwnerDepth = -1
	end
	else set @nRowCnt = 0
	while @nRowCnt > 0 begin
		-- REVIEW: JDR
		-- possibly calculate the OrdKey for this direction as well - if this is done it may be easiest to calculate all of the order
		-- keys at the very end
		insert into [ObjInfoTbl$] with (rowlock)
			(uid, ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
		select 	@uid, co.[Id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[Class], co.[OwnFlid$], co.[OwnOrd$], f.[Type], 0
		from 	[CmObject] co
				left join [Field$] f on f.[id] = co.[OwnFlid$]
		-- for this query the exists clause is more effecient than a join based on the ownership depth
		where 	exists (select	*
				from	ObjInfoTbl$ oi
				where	oi.[RelObjId] = co.[Id]
					and oi.[Uid]=@uid
					and oi.[OwnerDepth] = @nOwnerDepth + 1
				)
		select @nRowCnt=@@rowcount, @Err=@@error

		if @Err <> 0 begin
			set @sUid = convert(nvarchar(50), @Uid)
			raiserror ('GetOwnershipPath$: SQL Error %d; Unable to traverse owning hierachy - object(s) that own the specified object(s) (UID=%s).', 16, 1, @Err, @sUid)
			goto LFail
		end
		if @fRecurse = 0 break
		set @nOwnerDepth = @nOwnerDepth - 1
	end

	set @Err = 0
LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
go
