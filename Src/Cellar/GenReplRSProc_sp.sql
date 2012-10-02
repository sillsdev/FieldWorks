/***********************************************************************************************
 * GenReplRSProc
 *
 * Description:
 *	Creates the procedure that handles reference sequences for a particular table
 *
 * Parameters:
 *	@sTbl = the name of the reference sequence joiner table
 *	@flid = the field ID of the field that contains the reference sequence relationship
 *
 * Returns:
 *	0 if successful, otherwise the appropriate error code
 *
 * Notes:
 *	This is a slightly modified form of its predecessor, ReplaceRefSeqProc$, which created
 *	the stored procedure DefineReplaceRefSeqProc$. I don't know why there was a disconnect
 *	between file name and procedure name. The dollar sign is coming off because I have heard
 *	it creates havoc on Linux machines.
 **********************************************************************************************/

if object_id('GenReplRSProc') is not null begin
	print 'removing proc GenReplRSProc'
	drop proc [GenReplRSProc]
end
go
print 'Creating proc GenReplRSProc'
go
create proc [GenReplRSProc]
	@sTbl sysname,
	@flid int
as
	declare @sDynSql nvarchar(4000), @sDynSql2 nvarchar(4000), @sDynSql3 nvarchar(4000), @sDynSql4 nvarchar(4000)
	declare @err int

	--( This procedure was built before we shortened up some of the procedure
	--( names from ReplaceRefSeq_<tablename>_<fieldName> to
	--( ReplRS_<first 11 of tablename>_<first 11 of fieldname>. We need to tease
	--( them apart now from the @sTbl parameter, to name the procedure. We need
	--( to retain @sTbl for referencing the table itself.

	DECLARE
		@TableName SYSNAME,
		@FieldName SYSNAME,
		@Underscore INT,
		@ShortProcName SYSNAME;
	SET @UnderScore = CHARINDEX('_', @sTbl, 1);
	SET @TableName = SUBSTRING(@sTbl, 1, @Underscore - 1);
	SET @FieldName = SUBSTRING(@sTbl, @Underscore + 1, LEN(@sTbl) - @Underscore);
	SET @ShortProcName = SUBSTRING(@TableName, 1, 11) + N'_' + SUBSTRING(@FieldName, 1, 11)

	if object_id('ReplRS_' + @ShortProcName) is not null begin
		set @sDynSql = 'alter '
	end
	else begin
		set @sDynSql = 'create '
	end

set @sDynSql = @sDynSql +
N'proc ReplRS_' + @ShortProcName +'
	@SrcObjId int,
	@ListStmp int,
	@hXMLdoc int = null,
	@StartObj int = null,
	@StartObjOccurrence int = 1,
	@EndObj int = null,
	@EndObjOccurrence int = 1,
	@fRemoveXMLdoc tinyint = 1
as
	declare	@fIsNocountOn int, @Err int, @nTrnCnt int, @sTranName varchar(300)
	declare @nNumObjs int, @iCurObj int, @nMinOrd int, @StartOrd int, @EndOrd int
	declare @nSpaceAvail int
	declare @UpdStmp int

	set @Err = 0

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on
'
/*
	Cannot do this until SeqStmp$ is enabled.
	select @UpdStmp = [UpdStmp] from SeqStmp$ where [Src] = @SrcObjId and [Flid] = ' + convert(varchar(11), @flid) + '
	if @UpdStmp is not null and @ListStmp <> @UpdStmp begin
		raiserror(''The sequence list in '+@sTbl+' has been modified: SrcObjId = %d SrcFlid = ' + convert(varchar(11), @flid) + '.'',
				16, 1, @SrcObjId)
		return 50000
	end
*/
set @sDynSql = @sDynSql +
N'
	-- determine if the procedure was called within a transaction; if yes then create a savepoint,
	--	otherwise create a transaction
	set @sTranName = ''ReplRS_'+@ShortProcName+''' + convert(varchar(11), @@trancount)
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
	set @Err = @@error
	if @Err <> 0 begin
		raiserror(''ReplRS_'+@ShortProcName+': SQL Error %d; Unable to create a transaction'', 16, 1, @Err)
		goto LFail
	end

	-- get the starting and ending ordinal values
	set @EndOrd = null
	set @StartOrd = null
	if @StartObj is null begin
		-- since the @StartObj is null the list of objects should be added to the end of the sequence, so
		--	get the maximum ord value and add 1
		select	@StartOrd = coalesce(max([Ord]), 0) + 1
		from	['+@sTbl+']
		where	[Src] = @SrcObjId
	end
	else begin
		-- create a temporary table to hold all of the ord values associated with Src=@SrcObjId and (Dst=
		--	@StartObj or Dst=@EndObj); this table will have an identity column so subsequent queries
		--	can easily determine which ord value is associated with a particular position in a sequence
		declare @t table (
			Occurrence int identity(1,1),
			IsStart	tinyint,
			Ord int
		)

'
set @sDynSql2 = N'
		-- determine if an end object was not specified, or if the start and end object are the same
		if @EndObj is null or (@EndObj = @StartObj) begin
			-- only collect occurrences for the start object

			-- limit the number of returned rows from a select based on the desired occurrence; this will
			--	avoid processing beyond the desired occurrence
			if @EndObj is null set rowcount @StartObjOccurrence
			else set rowcount @EndObjOccurrence

			-- insert all of the Ord values associated with @StartObj
			insert into @t (IsStart, Ord)
			select	1, [Ord]
			from	[' + @sTbl + ']
			where	[Src] = @SrcObjId
				and [Dst] = @StartObj
			order by [Ord]
			set @Err = @@error
			if @Err <> 0 begin
				raiserror(''ReplRS_'+@ShortProcName+': SQL Error %d; Unable to insert ord values into the temporary table'', 16, 1, @Err)
				goto LFail
			end

			-- make selects return all rows
			set rowcount 0

			-- determine if the end and start objects are the same; if they are then search for the
			--	end object''s ord value based on the specified occurrence
			if @EndObj = @StartObj begin
				select	@EndOrd = [Ord]
				from	@t
				where	[Occurrence] = @EndObjOccurrence
			end
		end
		else begin
			-- insert Ord values associated with @StartObj and @EndObj
			insert into @t ([IsStart], [Ord])
			select	case [Dst]
					when @StartObj then 1
					else 0
				end,
				[Ord]
			from	[' + @sTbl + ']
			where	[Src] = @SrcObjId
				and ( [Dst] = @StartObj
					or [Dst] = @EndObj )
			order by 1 desc, [Ord]
			set @Err = @@error
			if @Err <> 0 begin
				raiserror(''ReplRS_'+@ShortProcName+': SQL Error %d; Unable to insert ord values into the temporary table'', 16, 1, @Err)
				goto LFail
			end

			-- get the end ord value associated with @EndObjOccurrence
			select	@EndOrd = [Ord]
			from	@t
			where	[IsStart] = 0
				and [Occurrence] = @EndObjOccurrence +
					( select max([Occurrence]) from @t where [IsStart] = 1 )
		end

		-- get the start ord value associated with @StartObjOccurrence
		select	@StartOrd = [Ord]
		from	@t
		where	[IsStart] = 1
			and [Occurrence] = @StartObjOccurrence

	end
'
set @sDynSql3 = N'
	-- validate the arguments
	if @StartOrd is null begin
		raiserror(''ReplRS_'+@ShortProcName+': Unable to locate ordinal value: SrcObjId(Src) = %d, StartObj(Dst) = %d, StartObjOccurrence = %d'',
				16, 1, @SrcObjId, @StartObj, @StartObjOccurrence)
		set @Err = 50001
		goto LFail
	end
	if @EndOrd is null and @EndObj is not null begin
		raiserror(''ReplRS_'+@ShortProcName+': Unable to locate ordinal value: SrcObjId(Src) = %d, EndObj(Dst) = %d, EndObjOccurrence = %d'',
				16, 1, @SrcObjId, @EndObj, @EndObjOccurrence)
		set @Err = 50002
		goto LFail
	end
	if @EndOrd is not null and @EndOrd < @StartOrd begin
		raiserror(''ReplRS_'+@ShortProcName+': The starting ordinal value %d is greater than the ending ordinal value %d: SrcObjId(Src) = %d, StartObj(Dst) = %d, StartObjOccurrence = %d, EndObj(Dst) = %d, EndObjOccurrence = %d'',
				16, 1, @StartOrd, @EndOrd, @SrcObjId, @StartObj, @StartObjOccurrence, @EndObj, @EndObjOccurrence)
		set @Err = 50003
		goto LFail
	end

	-- check for a delete/replace
	if @EndObj is not null begin

		delete	[' + @sTbl + ']
		where	[Src] = @SrcObjId
			and [Ord] >= @StartOrd
			and [Ord] <= @EndOrd
		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplRS_'+@ShortProcName+': SQL Error %d; Unable to remove objects between %d and %d for source = %d'',
					16, 1, @Err, @StartOrd, @EndOrd, @SrcObjId)
			goto LFail
		end
	end

	-- determine if any objects are going to be inserted
	if @hXMLDoc is not null begin
		-- get the number of objects to be inserted
		select	@nNumObjs = count(*)
		from 	openxml(@hXMLdoc, ''/root/Obj'') with (Id int)
		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplRS_'+@ShortProcName+': SQL Error %d; Unable to process XML document: document handle = %d'',
					16, 1, @hXMLdoc)
			goto LFail
		end
'
set @sDynSql4 = N'
		-- if the objects are not appended to the end of the list then determine if there is enough room
		if @StartObj is not null begin

			-- find the largest ordinal value less than the start object''s ordinal value
			select	@nMinOrd = coalesce(max([Ord]), -1) + 1
			from	['+@sTbl+']
			where	[Src] = @SrcObjId
				and [Ord] < @StartOrd

			-- determine if a range of objects was deleted; if objects were deleted then there is more room
			--	available
			if @EndObj is not null begin
				-- the actual space available could be determined, but this would involve another
				--	query (this query would look for the minimum Ord value greater than @EndOrd);
				--	however, it is known that at least up to the @EndObj is available

				set @nSpaceAvail = @EndOrd - @nMinOrd
				if @nMinOrd > 0 set @nSpaceAvail = @nSpaceAvail + 1
			end
			else begin
				set @nSpaceAvail = @StartOrd - @nMinOrd
			end

			-- determine if space needs to be made
			if @nSpaceAvail < @nNumObjs begin
				update	[' + @sTbl + ']
				set	[Ord] = [Ord] + @nNumObjs - @nSpaceAvail
				where	[Src] = @SrcObjId
					and [Ord] >= @nMinOrd
				set @Err = @@error
				if @Err <> 0 begin
					raiserror(''ReplRS_'+@ShortProcName+': SQL Error %d; Unable to increment the ordinal values; src = %d'',
							16, 1, @Err, @SrcObjId)
					goto LFail
				end
			end
		end
		else begin
			-- find the largest ordinal value plus one
			select	@nMinOrd = coalesce(max([Ord]), -1) + 1
			from	['+@sTbl+']
			where	[Src] = @SrcObjId
		end

		insert into [' + @sTbl + '] ([Src], [Dst], [Ord])
		select	@SrcObjId, ol.[Id], ol.[Ord] + @nMinOrd
		from 	openxml(@hXMLdoc, ''/root/Obj'') with (Id int, Ord int) ol
		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplRS_'+@ShortProcName+': SQL Error %d; Unable to insert objects into the reference sequence table'',
					16, 1, @Err)
			goto LFail
		end
	end

	if @nTrnCnt = 0 commit tran @sTranName

	-- determine if the XML document should be removed
	if @fRemoveXMLdoc = 1 and @hXMLDoc is not null exec sp_xml_removedocument @hXMLDoc

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return 0

LFail:
	rollback tran @sTranName

	-- determine if the XML document should be removed
	if @fRemoveXMLdoc = 1 and @hXMLDoc is not null exec sp_xml_removedocument @hXMLDoc

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
'

	exec ( @sDynSql + @sDynSql2 + @sDynSql3 + @sDynSql4 )
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('GenReplRSProc: SQL Error %d: Unable to create or alter the procedure ReplRS_%s',
				16, 1, @Err, @ShortProcName)
		return @err
	end

	return 0
go
