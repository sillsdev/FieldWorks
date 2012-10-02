/***********************************************************************************************
 * Function: fnGetOwnershipPath$
 *
 * Description:
 *	returns objects that are in the ownership chain of the specified object(s)
 *
 * Parameters:
 *	@ObjId=Id of the object;
 * 	@hXMLDocObjList=handle to a parsed XML document that contains multiple object Ids - the
 *		ownership chain will be produced for each of these objectIds;
 *	@nDirection=determines if all objects in the owning chain are included (0), if only
 *		objects owned by the specified object(s) are included (1), or if only objects
 *		that own the specified object(s) are included (-1) in the results;
 *	@fRecurse=determinse if the owning tree should be tranversed (0=do not recurse the
 *		owning tree, 1=recurse the owning tree)
 *	@fCaclOrdKey=determines if the order key is calculated (0=do not caclulate the order
 *		key, 1=calculate the order key)
 *
 * Returns:
 *	Table containing the object information in the format:
 *		[ObjId]		int,
 *		[ObjClass]	int,
 *		[InheritDepth]	int,
 *		[OwnerDepth]	int,
 *		[RelObjId]	int,
 *		[RelObjClass]	int,
 *		[RelObjField]	int,
 *		[RelOrder]	int,
 *		[RelType]	int,
 *		[OrdKey]	varbinary(250)
 **********************************************************************************************/
if object_id('fnGetOwnershipPath$') is not null begin
	print 'removing function fnGetOwnershipPath$'
	drop function [fnGetOwnershipPath$]
end
go
print 'creating function fnGetOwnershipPath$'
go
create function [fnGetOwnershipPath$] (
	@ObjId int=null,
	@hXMLDocObjList int=null,
	@nDirection smallint=0,
	@fRecurse tinyint=1,
	@fCalcOrdKey tinyint=1 )
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
	declare @nRowCnt int, @nOwnerDepth int

	-- determine if an object was supplied as an argument, if one was not use ObjInfoTbl$ as the list of objects
	if @objId is not null begin

		-- get the class of the specified object
		insert into @ObjInfo
			(ObjId, ObjClass, OwnerDepth, InheritDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
		select	@objId, co.[Class$], 0, 0, co.[Owner$], f.[Class], co.[OwnFlid$], co.[OwnOrd$], f.[Type],
			-- go ahead and calculate the order key for depth 0 objects even if @fCalcOrdKey=0
			convert(varbinary, coalesce(co.[Owner$], 0)) +
			convert(varbinary, coalesce(co.[OwnFlid$], 0)) +
			convert(varbinary, coalesce(co.[OwnOrd$], 0))
		from	[CmObject] co
				left outer join [Field$] f on co.[OwnFlid$] = f.[Id]
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
			insert into @ObjInfo
				(ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
			select 	co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[DstCls], co.[OwnFlid$], co.[OwnOrd$], f.[Type],
				oi.OrdKey+convert(varbinary, co.[Owner$]) + convert(varbinary, co.[OwnFlid$]) + convert(varbinary, coalesce(co.[OwnOrd$], 0))
			from 	[CmObject] co
					join @ObjInfo oi on co.[Owner$] = oi.[ObjId]
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	oi.[OwnerDepth] = @nOwnerDepth - 1
		end
		else begin
			-- get the objects owned at the next depth and do not calculate the order key
			insert into @ObjInfo
				(ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType)
			select 	co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[DstCls], co.[OwnFlid$], co.[OwnOrd$], f.[Type]
			from 	[CmObject] co
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	exists (select 	*
					from 	@ObjInfo oi
					where 	oi.[ObjId] = co.[Owner$]
						and oi.[OwnerDepth] = @nOwnerDepth - 1
					)
		end
		set @nRowCnt = @@rowcount

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
		insert into @ObjInfo
			(ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
		select 	co.[Id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[Class], co.[OwnFlid$], co.[OwnOrd$], f.[Type], 0
		from 	[CmObject] co
				left join [Field$] f on f.[id] = co.[OwnFlid$]
		-- for this query the exists clause is more effecient than a join based on the ownership depth
		where 	exists (select	*
				from	@ObjInfo oi
				where	oi.[RelObjId] = co.[Id]
					and oi.[OwnerDepth] = @nOwnerDepth + 1
				)
		set @nRowCnt = @@rowcount

		if @fRecurse = 0 break
		set @nOwnerDepth = @nOwnerDepth - 1
	end

	return
LFail:
	delete @ObjInfo

	return
end
go
