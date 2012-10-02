/***********************************************************************************************
 * Function: fnIsInOwnershipPath$
 *
 * Description:
 *	determines if the specified object is in the ownership path of the specified owning
 *	object
 *
 * Parameters:
 *	@ObjId=Id of the object;
 *	@OwnerObjId=Id of the owner object;
 *
 * Returns:
 * 	true (1) if the object is in the ownership path, false (0) if the object is not in the
 *	ownership path, or -1 if an error occured
 **********************************************************************************************/
if object_id('fnIsInOwnershipPath$') is not null begin
	print 'removing function fnIsInOwnershipPath$'
	drop function [fnIsInOwnershipPath$]
end
go
print 'creating function fnIsInOwnershipPath$'
go
create function [fnIsInOwnershipPath$] (
	@ObjId int,
	@OwnerObjId int )
returns tinyint
as
begin
	declare @nRowCnt int, @nOwnerDepth int
	declare @fInPath tinyint
	declare @ObjInfo table (
		[ObjId]		int		not null,
		[ObjClass]	int		null,
		[InheritDepth]	int		null		default(0),
		[OwnerDepth]	int		null		default(0),
		[RelObjId]	int		null,
		[RelObjClass]	int		null,
		[RelObjField]	int		null,
		[RelOrder]	int		null,
		[RelType]	int		null,
		[OrdKey]	varbinary(250)	null		default(0) )

	set @fInPath = 0

	-- get the class of the specified object
	insert into @ObjInfo (ObjId, ObjClass)
	select	@OwnerObjId, co.[Class$]
	from	[CmObject] co
	where	co.[Id] = @OwnerObjId
	if @@error <> 0 goto LFail

	set @nRowCnt = 1
	set @nOwnerDepth = 1
	while @nRowCnt > 0 begin
		-- determine if one of the objects at the current depth owns the specified object, if
		--    one does we can exit here
		if exists (
			select	*
			from	[CmObject] co
					join @ObjInfo oi on co.[Owner$] = oi.[ObjId]
			where	oi.[OwnerDepth] = @nOwnerDepth - 1
				and co.[Id] = @ObjId
			)
		begin
			set @fInPath = 1
			goto Finish
		end

		-- add all of the objects owned at the next depth to the object list
		insert	into @ObjInfo (ObjId, ObjClass, OwnerDepth, RelObjId)
		select 	co.[id], co.[Class$], @nOwnerDepth, co.[Owner$]
		from 	[CmObject] co
		where 	exists (select	*
				from 	@ObjInfo oi
				where 	oi.[ObjId] = co.[Owner$]
					and oi.[OwnerDepth] = @nOwnerDepth - 1
				)
		set @nRowCnt = @@rowcount

		set @nOwnerDepth = @nOwnerDepth + 1
	end

Finish:
	return @fInPath
LFail:
	return -1
end
go
