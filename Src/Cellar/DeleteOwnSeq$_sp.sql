/***********************************************************************************************
 * DeleteOwnSeq$
 *
 * Description:
 *	Deletes a specified range of owned sequence objects
 *
 * Parameters:
 *	@SrcObjId = the object that owns the source sequence
 *	@SrcFlid = the FLID of the object that owns the source sequence
 *	@ListStmp = the timestamp value associated with the sequence when it was read from the
 *		database (can be 0)
 *	@StartObj = the starting object in the sequence of the objects that are to be removed
 *	@EndObj = the ending object in the sequence of the objects that are to be removed; if
 *		null then the objects from the starting object to the end of the sequence will
 *		be removed
 *
 * Returns:
 *	0 if successful, otherwise the appropriate error code
 *
 * REVIEW (SteveMiller): This procedure is called only by VwOleDbDa.cpp(7133).
 * Changed (Steve McConnel): Fixed a couple of bugs so that removing a column(s) in Data Notebook
 *                           actually works (DN-696).
 **********************************************************************************************/
if object_id('DeleteOwnSeq$') is not null begin
	print 'removing proc DeleteOwnSeq$'
	drop proc [DeleteOwnSeq$]
end
go
print 'creating proc DeleteOwnSeq$'
go
create proc [DeleteOwnSeq$]
	@SrcObjId int,
	@SrcFlid int,
	@ListStmp int,
	@StartObj int,
	@EndObj int = null
as
	declare @sTranName varchar(50)
	declare @StartOrd int, @EndOrd int
	declare @guid uniqueidentifier
	declare	@fIsNocountOn int, @Err int, @nTrnCnt int, @UpdStmp int,
		@nvcId NVARCHAR(20)

	DECLARE @tblIds TABLE (Id NVARCHAR(20))

	set @Err = 0

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- determine if the procedure was called within a transaction; if yes then create a savepoint,
	--	otherwise create a transaction
	set @sTranName = 'DeleteOwnSeq$_' + convert(varchar(11), @@trancount)
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('DeleteOwnSeq$: SQL Error %d; Unable to create a transaction', 16, 1, @Err)
		goto LFail
	end

	-- get the starting and ending ordinal values
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

	-- validate the parameters
	if @StartOrd is null begin
		raiserror('DeleteOwnSeq$: Unable to locate ordinal value in CmObject: SrcObjId(Owner$) = %d SrcFlid(OwnFlid$) = %d StartObj(Id) = %d',
				16, 1, @SrcObjId, @SrcFlid, @StartObj)
		set @Err = 51001
		goto LFail
	end
	if @EndOrd is null begin
		raiserror('DeleteOwnSeq$: Unable to locate ordinal value in CmObject: SrcObjId(Owner$) = %d SrcFlid(OwnFlid$) = %d EndObj(Id) = %d',
				16, 1, @SrcObjId, @SrcFlid, @EndObj)
		set @Err = 51002
		goto LFail
	end
	if @EndOrd < @StartOrd begin
		raiserror('DeleteOwnSeq$: The starting ordinal value %d is greater than the ending ordinal value %d in CmObject: SrcObjId(Owner$) = %d SrcFlid(OwnFlid$) = %d StartObj(Id) = %d EndObj(Id) = %d',
				16, 1, @StartOrd, @EndOrd, @SrcObjId, @SrcFlid, @StartObj, @EndObj)
		set @Err = 51004
		goto LFail
	end

	-- get the list of objects that are to be deleted and insert them into the ObjInfoTbl$ table
	--set @guid = newid()
	--insert into ObjInfoTbl$ with (rowlock) (uid, ObjId)
	--select	@guid, [Id]
	INSERT INTO @tblIds
	SELECT CONVERT(NVARCHAR(20), Id)
	from	CmObject
	where	[Owner$] = @SrcObjId
		and [OwnFlid$] = @SrcFlid
		and [OwnOrd$] >= @StartOrd
		and [OwnOrd$] <= @EndOrd

	set @Err = @@error
	if @Err <> 0 begin
		raiserror('DeleteOwnSeq$: SQL Error %d; Unable to insert objects into @tblIds; SrcObjId(Owner$) = %d SrcFlid(OwnFlid$) = %d StartObj(Id) = %d EndObj(Id) = %d',
				16, 1, @Err, @SrcObjId, @SrcFlid, @StartObj, @EndObj)
		goto LFail
	end

	SELECT TOP 1 @nvcId = Id FROM @tblIds ORDER BY Id
	WHILE @@ROWCOUNT != 0 BEGIN
		--exec @Err = DeleteObject$ @guid, null, 1
		EXEC DeleteObjects @nvcId
		SELECT TOP 1 @nvcId = Id FROM @tblIds WHERE ID > @nvcId ORDER BY Id
	END
	if @Err <> 0 goto LFail

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
