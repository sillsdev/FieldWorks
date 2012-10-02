-- Update database from version 200061 to 200062
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Add the new ImportResidue attribute for LexEntry (LT-1888)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5002028, 15, 5002, null, 'ImportResidue',0,null, null, null, null)

-- Add the new ImportResidue attribute for LexSense (LT-1888)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5016028, 15, 5016, null, 'ImportResidue',0,null, null, null, null)

-- Fix migration issue (from 200054 to 200055) for temporary name (LT-1881)
if object_id('ReplaceRefSeq_LexEntry_MainEntriesOrSenses2') is not null begin
	print 'removing proc ReplaceRefSeq_LexEntry_MainEntriesOrSenses2'
	drop proc ReplaceRefSeq_LexEntry_MainEntriesOrSenses2
end

if object_id('ReplaceRefSeq_LexEntry_MainEntriesOrSenses') is not null begin
	print 'removing proc ReplaceRefSeq_LexEntry_MainEntriesOrSenses'
	drop proc ReplaceRefSeq_LexEntry_MainEntriesOrSenses
end
print 'creating proc ReplaceRefSeq_LexEntry_MainEntriesOrSenses'
go
create proc ReplaceRefSeq_LexEntry_MainEntriesOrSenses
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

	-- determine if the procedure was called within a transaction; if yes then create a savepoint,
	--	otherwise create a transaction
	set @sTranName = 'ReplaceRefSeq_LexEntry_MainEntriesOrSenses' + convert(varchar(11), @@trancount)
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('ReplaceRefSeq_LexEntry_MainEntriesOrSenses: SQL Error %d; Unable to create a transaction', 16, 1, @Err)
		goto LFail
	end

	-- get the starting and ending ordinal values
	set @EndOrd = null
	set @StartOrd = null
	if @StartObj is null begin
		-- since the @StartObj is null the list of objects should be added to the end of the sequence, so
		--	get the maximum ord value and add 1
		select	@StartOrd = coalesce(max([Ord]), 0) + 1
		from	[LexEntry_MainEntriesOrSenses] with (REPEATABLEREAD)
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
			from	[LexEntry_MainEntriesOrSenses] (readuncommitted)
			where	[Src] = @SrcObjId
				and [Dst] = @StartObj
			order by [Ord]
			set @Err = @@error
			if @Err <> 0 begin
				raiserror('ReplaceRefSeq_LexEntry_MainEntriesOrSenses: SQL Error %d; Unable to insert ord values into the temporary table', 16, 1, @Err)
				goto LFail
			end

			-- make selects return all rows
			set rowcount 0

			-- determine if the end and start objects are the same; if they are then search for the
			--	end object's ord value based on the specified occurrence
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
			from	[LexEntry_MainEntriesOrSenses] (readuncommitted)
			where	[Src] = @SrcObjId
				and ( [Dst] = @StartObj
					or [Dst] = @EndObj )
			order by 1 desc, [Ord]
			set @Err = @@error
			if @Err <> 0 begin
				raiserror('ReplaceRefSeq_LexEntry_MainEntriesOrSenses: SQL Error %d; Unable to insert ord values into the temporary table', 16, 1, @Err)
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

	-- validate the arguments
	if @StartOrd is null begin
		raiserror('ReplaceRefSeq_LexEntry_MainEntriesOrSenses: Unable to locate ordinal value: SrcObjId(Src) = %d, StartObj(Dst) = %d, StartObjOccurrence = %d',
				16, 1, @SrcObjId, @StartObj, @StartObjOccurrence)
		set @Err = 50001
		goto LFail
	end
	if @EndOrd is null and @EndObj is not null begin
		raiserror('ReplaceRefSeq_LexEntry_MainEntriesOrSenses: Unable to locate ordinal value: SrcObjId(Src) = %d, EndObj(Dst) = %d, EndObjOccurrence = %d',
				16, 1, @SrcObjId, @EndObj, @EndObjOccurrence)
		set @Err = 50002
		goto LFail
	end
	if @EndOrd is not null and @EndOrd < @StartOrd begin
		raiserror('ReplaceRefSeq_LexEntry_MainEntriesOrSenses: The starting ordinal value %d is greater than the ending ordinal value %d: SrcObjId(Src) = %d, StartObj(Dst) = %d, StartObjOccurrence = %d, EndObj(Dst) = %d, EndObjOccurrence = %d',
				16, 1, @StartOrd, @EndOrd, @SrcObjId, @StartObj, @StartObjOccurrence, @EndObj, @EndObjOccurrence)
		set @Err = 50003
		goto LFail
	end

	-- check for a delete/replace
	if @EndObj is not null begin

		delete	[LexEntry_MainEntriesOrSenses] with (REPEATABLEREAD)
		where	[Src] = @SrcObjId
			and [Ord] >= @StartOrd
			and [Ord] <= @EndOrd
		set @Err = @@error
		if @Err <> 0 begin
			raiserror('ReplaceRefSeq_LexEntry_MainEntriesOrSenses: SQL Error %d; Unable to remove objects between %d and %d for source = %d',
					16, 1, @Err, @StartOrd, @EndOrd, @SrcObjId)
			goto LFail
		end
	end

	-- determine if any objects are going to be inserted
	if @hXMLDoc is not null begin
		-- get the number of objects to be inserted
		select	@nNumObjs = count(*)
		from 	openxml(@hXMLdoc, '/root/Obj') with (Id int)
		set @Err = @@error
		if @Err <> 0 begin
			raiserror('ReplaceRefSeq_LexEntry_MainEntriesOrSenses: SQL Error %d; Unable to process XML document: document handle = %d',
					16, 1, @hXMLdoc)
			goto LFail
		end

		-- if the objects are not appended to the end of the list then determine if there is enough room
		if @StartObj is not null begin

			-- find the largest ordinal value less than the start object's ordinal value
			select	@nMinOrd = coalesce(max([Ord]), -1) + 1
			from	[LexEntry_MainEntriesOrSenses] with (REPEATABLEREAD)
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
				update	[LexEntry_MainEntriesOrSenses] with (REPEATABLEREAD)
				set	[Ord] = [Ord] + @nNumObjs - @nSpaceAvail
				where	[Src] = @SrcObjId
					and [Ord] >= @nMinOrd
				set @Err = @@error
				if @Err <> 0 begin
					raiserror('ReplaceRefSeq_LexEntry_MainEntriesOrSenses: SQL Error %d; Unable to increment the ordinal values; src = %d',
							16, 1, @Err, @SrcObjId)
					goto LFail
				end
			end
		end
		else begin
			-- find the largest ordinal value plus one
			select	@nMinOrd = coalesce(max([Ord]), -1) + 1
			from	[LexEntry_MainEntriesOrSenses] with (REPEATABLEREAD)
			where	[Src] = @SrcObjId
		end

		insert into [LexEntry_MainEntriesOrSenses] with (REPEATABLEREAD) ([Src], [Dst], [Ord])
		select	@SrcObjId, ol.[Id], ol.[Ord] + @nMinOrd
		from 	openxml(@hXMLdoc, '/root/Obj') with (Id int, Ord int) ol
		set @Err = @@error
		if @Err <> 0 begin
			raiserror('ReplaceRefSeq_LexEntry_MainEntriesOrSenses: SQL Error %d; Unable to insert objects into the reference sequence table',
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

GO

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200061
begin
	update Version$ set DbVer = 200062
	COMMIT TRANSACTION
	print 'database updated to version 200062'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200061 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
