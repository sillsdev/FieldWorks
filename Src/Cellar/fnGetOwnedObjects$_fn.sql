/***********************************************************************************************
 * Function: fnGetOwnedObjects$
 *
 * Description:
 *	returns objects that are owned by the specified object(s) (directly and, if fRecurse is
 *  true, also indirectly).
 *
 * Parameters:
 *	@ObjIds = list of object IDs
 *	@grfcpt=mask that indicates what types of owning properties included to find owned objects;
 *		a combination of 8388608 = 2^23 for owning atomic,
 *		33554432 = 2^25 for owning collection, and
 *		134217728 = 2^27 for owing sequence
 *		null to get all owned objects;
 *	@fBaseClasses=a flag that determines if the base classes of owned objects are included
 *		in the object list (e.g., rows for each object + all superclasses including CmObject.
 *		So if a CmPerson is included, it will also have a row for CmPossibility and CmObject);
 *	@fSubClasses=flag that determines if the sub classes of owned objects are included in
 *		the object list (note that invalid subclasses may be returned -- all subclasses of
 *		the basic target class from Field$ are returned);
 *	@fRecurse=determinse if the owning tree should be tranversed (0=do not recurse the
 *		owning tree, 1=recurse the owning tree);
 *	@riid = return only rows where the class = @riid, or the class is a subclass of @riid
 *		(this is most useful if @fSubClasses is nonzero since otherwise the returned result
 *		table is likely to be empty). Original note from author: "if a class was specified
 *		remove the owned objects that are not of that type of class; these objects were
 *		necessary in order to get a list of all of the referenced and referencing objects
 *		that were potentially the type of specified class"
 *	@fCaclOrdKey=determines if the order key is calculated (0=do not caclulate the order
 *		key, 1=calculate the order key)
 *
 *	If fBaseClasses and fSubclasses are both false, the result has one row for each
 *		owned object, with inheritdepth 0.
 *	If fBaseClasses is true, there are additional rows (for each object) for each of its
 *		base classes (including CmObject), with inheritdepth indicating how many classes
 *		are in the inheritance chain between the actual class of the object and the
 *		class indicated in ObjClass. (Note that only in the row where inheritdepth is 0
 *		is ObjClass the actual class of the object indicted by objid.)
 *	If fSubclasses is true, there are additional rows (for each object) for each of
 *		the subclasses of its actual class, with (-InheritDepth) indicating how many
 *		classes intervene between ObjClass and the actual class of ObjId.
 *
 * Returns:
 *	Table containing the object information in the format:
 *		[ObjId]		int,
 *		[ObjClass]	int,
 *		[InheritDepth]	int,
 *		[OwnerDepth]	int,
 *		[RelObjId]	int,  -- (misnamed!) owner of ObjId
 *		[RelObjClass]	int, -- class of RelObjId
 *		[RelObjField]	int, -- Field$ id of field in which RelObjId owns ObjId
 *		[RelOrder]	int, -- ord of ObjId in field RelObjId of owner RelObjId
 *		[RelType]	int, -- value from CmTypes.h indicating type of RelObjField (owning atomic, collection, seq).
 *		[OrdKey]	varbinary(250) -- ordering key which groups rows for same object, then flid, then ord.
 **********************************************************************************************/
if object_id('fnGetOwnedObjects$') is not null begin
	print 'removing function fnGetOwnedObjects$'
	drop function [fnGetOwnedObjects$]
end
go
print 'creating function fnGetOwnedObjects$'
go
create function [fnGetOwnedObjects$] (
	@ObjIds NVARCHAR(MAX),
	@grfcpt int=kgrfcptAll,
	@fBaseClasses tinyint=0,
	@fSubClasses tinyint=0,
	@fRecurse tinyint=1,
	@riid int=NULL,
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
	declare @nRowCnt int
	declare	@nObjId int, @nInheritDepth int, @nOwnerDepth int, @sOrdKey varchar(250)

	-- if NULL was specified as the mask assume that all objects are desired
	if @grfcpt is null
		set @grfcpt = 528482304 --( kgrfcptAll

	insert into @ObjInfo (ObjId, ObjClass, OrdKey)
		select	i.[Id], co.[Class$],
			-- calculate the order key even if @fCalcOrdKey = 0 because the overhead is very small here
			convert(varbinary, coalesce(co.[Owner$], 0)) +
			convert(varbinary, coalesce(co.[OwnFlid$], 0)) +
			convert(varbinary, coalesce(co.[OwnOrd$], 0))
		FROM fnGetIdsFromString(@ObjIds) i
		JOIN CmObject co ON co.Id = i.Id;

	if @@error <> 0
		goto LFail

	-- TODO (SteveMiller): These queries really need to be optimized. See FDB-219.

	set @nOwnerDepth = 1
	set @nRowCnt = 1
	while @nRowCnt > 0 begin
		-- determine if the order key should be calculated - if the order key is not needed a more
		--    effecient query can be used to generate the ownership tree
		if @fCalcOrdKey = 1 begin
			-- get the objects owned at the next depth and calculate the order key
			insert	into @ObjInfo
				(ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
			select 	co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[Class], co.[OwnFlid$], co.[OwnOrd$], f.[Type],
				oi.OrdKey+convert(varbinary, co.[Owner$]) + convert(varbinary, co.[OwnFlid$]) + convert(varbinary, coalesce(co.[OwnOrd$], 0))
			from 	[CmObject] co
					join @ObjInfo oi on co.[Owner$] = oi.[ObjId]
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	oi.[OwnerDepth] = @nOwnerDepth - 1
				and ( 	( @grfcpt & 8388608 = 8388608 and f.[Type] = 23 )
					or ( @grfcpt & 33554432 = 33554432 and f.[Type] = 25 )
					or ( @grfcpt & 134217728 = 134217728 and f.[Type] = 27 )
				)
		end
		else begin
			-- get the objects owned at the next depth and do not calculate the order key
			insert	into @ObjInfo
				(ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType)
			select 	co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[Class], co.[OwnFlid$], co.[OwnOrd$], f.[Type]
			from 	[CmObject] co
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	exists (select 	*
					from 	@ObjInfo oi
					where 	oi.[ObjId] = co.[Owner$]
						and oi.[OwnerDepth] = @nOwnerDepth - 1
				)
				and ( 	( @grfcpt & 8388608 = 8388608 and f.[Type] = 23 )
					or ( @grfcpt & 33554432 = 33554432 and f.[Type] = 25 )
					or ( @grfcpt & 134217728 = 134217728 and f.[Type] = 27 )
				)
		end
		set @nRowCnt=@@rowcount

		-- determine if the whole owning tree should be included in the results
		if @fRecurse = 0 break
		-- give up before we crash due to OrdKey getting too long
		if @fCalcOrdKey = 1 AND @nOwnerDepth >= 16 break

		set @nOwnerDepth = @nOwnerDepth + 1
	end

	--
	-- get all of the base classes of the object(s), including CmObject.
	--
	if @fBaseClasses = 1 begin
		insert	into @ObjInfo
			(ObjId, ObjClass, InheritDepth, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
		select	oi.[ObjId], p.[Dst], p.[Depth], oi.[OwnerDepth], oi.[RelObjId], oi.[RelObjClass], oi.[RelObjField], oi.[RelOrder], oi.[RelType], oi.[OrdKey]
		from	@ObjInfo oi
				join [ClassPar$] p on oi.[ObjClass] = p.[Src]
				join [Class$] c on c.[id] = p.[Dst]
		where	p.[Depth] > 0
		if @@error <> 0 goto LFail
	end
	--
	-- get all of the sub classes of the object(s)
	--
	if @fSubClasses = 1 begin
		insert	into @ObjInfo
			(ObjId, ObjClass, InheritDepth, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
		select	oi.[ObjId], p.[Src], -p.[Depth], oi.[OwnerDepth], oi.[RelObjId], oi.[RelObjClass], oi.[RelObjField], oi.[RelOrder], oi.[RelType], oi.[OrdKey]
		from	@ObjInfo oi
				join [ClassPar$] p on oi.[ObjClass] = p.[Dst] and InheritDepth = 0
				join [Class$] c on c.[id] = p.[Dst]
		where	p.[Depth] > 0
			and p.[Dst] <> 0
		if @@error <> 0 goto LFail
	end

	-- if a class was specified remove the owned objects that are not of that type of class; these objects were
	--    necessary in order to get a list of all of the referenced and referencing objects that were potentially
	--    the type of specified class
	if @riid is not null begin
		delete	@ObjInfo
		where 	not exists (
				select	*
				from	[ClassPar$] cp
				where	cp.[Dst] = @riid
					and cp.[Src] = [ObjClass]
			)
		if @@error <> 0 goto LFail
	end

	return
LFail:

	delete from @ObjInfo
	return
end
go
