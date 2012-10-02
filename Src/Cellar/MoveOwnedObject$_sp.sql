DbgNuke(MoveOwnedObject$, proc)
go
print 'creating proc MoveOwnedObject$'
go
/*
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
*/
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
	if @nDstType = kcptOwningSequence begin
		execute MoveToOwnedSeq$ @SrcObjId, @SrcFlid, @StartObj, @EndObj, @DstObjId, @DstFlid, @DstStartObj
	end

	-- Destinaton type is owning collection
	else if @nDstType = kcptOwningCollection begin
		execute MoveToOwnedColl$ @SrcObjId, @SrcFlid, @StartObj, @EndObj, @DstObjId, @DstFlid
	end

	-- Destination type is owning atomic
	else if @nDstType = kcptOwningAtom begin
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
go
