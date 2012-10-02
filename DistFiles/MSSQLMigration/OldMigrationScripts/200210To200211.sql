-- Update database from version 200210 to 200211
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FWNX-57: Some ReplaceRefColl_ stored procedure names too long.
-------------------------------------------------------------------------------

if object_id('DefineReplaceRefCollProc$') is not null begin
	drop proc DefineReplaceRefCollProc$
end

-------------------------------------------------------------------------------

if object_id('GenReplRCProc') is not null begin
	print 'removing proc GenReplRCProc';
	drop proc GenReplRCProc;
end
go
print 'Creating proc GenReplRCProc';
go
create proc [GenReplRCProc]
	@sTbl sysname
as
	declare @sDynSql nvarchar(4000), @sDynSql2 nvarchar(4000)
	declare @err int

	--( This procedure was built before we shortened up some of the procedure
	--( names from ReplaceRefColl_<tablename>_<fieldName> to
	--( ReplRC_<first 11 of tablename>_<first 11 of fieldname>. We need to tease
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

	if object_id('ReplRC_' + @ShortProcName ) is not null begin
		set @sDynSql = 'alter '
	end
	else begin
		set @sDynSql = 'create '
	end

set @sDynSql = @sDynSql + N'
proc ReplRC_' + @ShortProcName +'
	@SrcObjId int,
	@ntInsIds NTEXT,
	@ntDelIds NTEXT
as
	declare	@fIsNocountOn int, @Err int, @nTrnCnt int, @sTranName varchar(300)
	declare @i int, @RowsAffected int

	set @Err = 0

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- determine if the procedure was called within a transaction; if yes then create a savepoint,
	--	otherwise create a transaction
	set @sTranName = ''ReplR_'+@ShortProcName+''' + convert(varchar(11), @@trancount)
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
	set @Err = @@error
	if @Err <> 0 begin
		raiserror(''ReplRC_'+@ShortProcName+': SQL Error %d; Unable to create a transaction'', 16, 1, @Err)
		goto LCleanup
	end

	-- determine if any object references should be removed
	IF @ntDelIds IS NOT NULL BEGIN
		-- objects may be listed in a collection more than once, and the delete list specifies how many
		--	occurrences of an object need to be removed; the above delete however removed all occurences,
		--	so the appropriate number of certain objects may need to be added back in

		-- create a temporary table to hold objects that are referenced more than once and at least one
		--	of the references is to be removed
		declare @t table (
			DstObjId int,
			Occurrences int,
			DelOccurrences int
		)
		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplRC_'+@ShortProcName+': SQL Error %d; Unable to create a temporary table'', 16, 1, @Err)
			goto LCleanup
		end

		-- get the objects that are referenced more than once along with the actual number of references; do this
		--	only for the objects where at least one reference is going to be removed

		INSERT INTO @t (DstObjId, DelOccurrences, Occurrences)
			SELECT jt.Dst, ol.DelCnt, COUNT(*)
			FROM ' + @sTbl + ' jt (REPEATABLEREAD)
			JOIN (
				SELECT Id ObjId, COUNT(*) DelCnt
				FROM dbo.fnGetIdsFromString(@ntDelIds, NULL)
				GROUP BY Id
				) AS ol ON jt.Dst = ol.ObjId
			WHERE jt.Src = @SrcObjId
			GROUP BY Dst, ol.DelCnt
			HAVING COUNT(*) > 1

		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplRC_'+@ShortProcName+': SQL Error %d; Unable to insert objects that are referenced more than once: SrcObjId(src) = %d'',
					16, 1, @Err, @SrcObjId)
			goto LCleanup
		end
'
set @sDynSql2 = N'
		-- remove the object references

		DELETE ' + @sTbl + '
			FROM ' + @sTbl + ' jt
			JOIN (SELECT Id FROM dbo.fnGetIdsFromString(@ntDelIds, NULL)) AS ol
				ON ol.Id = jt.Dst
			WHERE jt.Src = @SrcObjId

		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplRC_'+@ShortProcName+': SQL Error %d; Unable to delete objects from a reference collection: SrcObjId(src) = %d'',
					16, 1, @Err, @SrcObjId)
			goto LCleanup
		end

		-- reinsert the appropriate number of objects that had multiple references
		set @i = 0
		set @RowsAffected = 1 -- set to 1 to get inside of the loop
		while @RowsAffected > 0 begin
			insert into ['+@sTbl+'] with (REPEATABLEREAD) ([Src], [Dst])
			select	@SrcObjid, [DstObjId]
			from	@t
			where	Occurrences - DelOccurrences > @i
			select @Err = @@error, @RowsAffected = @@rowcount
			if @Err <> 0 begin
				raiserror(''ReplRC_'+@ShortProcName+': SQL Error %d; Unable to reinsert objects into a reference collection: SrcObjId(src) = %d'',
						16, 1, @Err, @SrcObjId)
				goto LCleanup
			end
			set @i = @i + 1
		end

	end

	-- determine if any object references should be inserted
	IF @ntInsIds IS NOT NULL BEGIN

		INSERT INTO ' + @sTbl + ' WITH (REPEATABLEREAD) (Src, Dst)
			SELECT @SrcObjId, ol.Id
			FROM dbo.fnGetIdsFromString(@ntInsIds, NULL) AS ol

		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplRC_'+@ShortProcName+': SQL Error %d; Unable to insert objects into a reference collection: SrcObjId(src) = %d'',
					16, 1, @Err, @SrcObjId)
			goto LCleanup
		end
	end

LCleanup:
	if @Err <> 0 rollback tran @sTranName
	else if @nTrnCnt = 0 commit tran @sTranName

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	return @Err
'

	exec ( @sDynSql + @sDynSql2 )
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('GenReplRCProc: SQL Error %d: Unable to create or alter the procedure ReplRC_%s',
				16, 1, @Err, @ShortProcName)
		return @err
	end

	return 0
go

-------------------------------------------------------------------------------

if object_id('ReplaceRefColl$') is not null begin
	print 'removing proc ReplaceRefColl$'
	drop proc [ReplaceRefColl$]
end
go
print 'creating proc ReplaceRefColl$'
go
create proc [ReplaceRefColl$]
	@flid int,
	@SrcObjId int,
	@ntInsIds NTEXT,
	@ntDelIds NTEXT
as
	declare @Err int
	DECLARE @nvcSql NVARCHAR(500)

	SELECT @nvcSql = N'EXEC ReplRC_' + SUBSTRING(c.Name, 1, 11) + N'_' + SUBSTRING(f.Name, 1, 11) +
		N' @nSrcObjId, @ntInsertIds, @ntDeleteIds'
		FROM Field$ f JOIN Class$ c ON f.Class = c.Id
		WHERE f.Id = @flid AND f.Type = 26

	if @@rowcount <> 1 begin
		raiserror('ReplaceRefColl$: Invalid flid: %d', 16, 1, @flid)
		return 50000
	end

	EXECUTE sp_executesql @nvcSql,
		N'@nSrcObjId INT, @ntInsertIds NTEXT, @ntDeleteIds NTEXT',
		@nSrcObjId = @SrcObjId,
		@ntInsertIds = @ntInsIds,
		@ntDeleteIds = @ntDelIds

	set @Err = @@error
	if @Err <> 0 begin
		raiserror('ReplaceRefColl$: SQL Error %d: Unable to perform replace: %d', 16, 1, @Err, @nvcSql)
		return @Err
	end

	return 0
go

-------------------------------------------------------------------------------
-- FWNX-58: Some ReplaceRefSeq_ stored procedure names too long.
-------------------------------------------------------------------------------


if object_id('ReplaceRefSeqProc$') is not null begin
	drop proc ReplaceRefSeqProc$
end
go

---------------------------------------------------------------------------------

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
		from	['+@sTbl+'] with (REPEATABLEREAD)
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

		delete	[' + @sTbl + '] with (REPEATABLEREAD)
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
			from	['+@sTbl+'] with (REPEATABLEREAD)
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
				update	[' + @sTbl + '] with (REPEATABLEREAD)
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
			from	['+@sTbl+'] with (REPEATABLEREAD)
			where	[Src] = @SrcObjId
		end

		insert into [' + @sTbl + '] with (REPEATABLEREAD) ([Src], [Dst], [Ord])
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

-------------------------------------------------------------------------------

if object_id('ReplaceRefSeq$') is not null begin
	print 'removing proc ReplaceRefSeq$'
	drop proc [ReplaceRefSeq$]
end
go
print 'creating proc ReplaceRefSeq$'
go
create proc [ReplaceRefSeq$]
	@flid int,
	@SrcObjId int,
	@ListStmp int,
	@hXMLdoc int,
	@StartObj int = null,
	@StartObjOccurrence int = 1,
	@EndObj int = null,
	@EndObjOccurrence int = 1,
	@fRemoveXMLDoc tinyint = 1
as
	declare @Err int
	declare	@sDynSql varchar(500)

	select	@sDynSql = 'exec ReplRS_' +
			SUBSTRING(c.[Name], 1, 11) + '_' + SUBSTRING(f.[Name], 1, 11) + ' ' +
			coalesce(convert(varchar(11), @SrcObjId), 'null') + ',' +
			coalesce(convert(varchar(11), @ListStmp), 'null') + ',' +
			coalesce(convert(varchar(11), @hXMLdoc), 'null') + ',' +
			coalesce(convert(varchar(11), @StartObj), 'null') + ',' +
			coalesce(convert(varchar(11), @StartObjOccurrence), 'null') + ',' +
			coalesce(convert(varchar(11), @EndObj), 'null') + ',' +
			coalesce(convert(varchar(11), @EndObjOccurrence), 'null') + ',' +
			coalesce(convert(varchar(3), @fRemoveXMLDoc), 'null')
	from	Field$ f join Class$ c on f.[Class] = c.[Id]
	where	f.[Id] = @flid and f.[Type] = kcptReferenceSequence

	if @@rowcount <> 1 begin
		raiserror('ReplaceRefSeq$: Invalid flid: %d', 16, 1, @flid)
		return 50000
	end

	exec (@sDynSql)
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('ReplaceRefSeq$: SQL Error %d: Unable to perform replace: %d', 16, 1, @Err, @sDynSql)
		return @Err
	end

	return 0
go

-------------------------------------------------------------------------------
-- FWNX-55: Some stored procedure names too long.
-------------------------------------------------------------------------------

IF OBJECT_ID('RenameClass') IS NOT NULL BEGIN
	PRINT 'removing procedure RenameClass';
	DROP PROCEDURE RenameClass;
END
GO
PRINT 'creating procedure RenameClass';
GO

CREATE PROCEDURE RenameClass
	@ClassId INT,
	@NewClassName NVARCHAR(100)
AS

	/******************************************************************
	** Warning! Do not use this procedure unless you absolutely know **
	** what you are doing! Then think about it twice. And back up    **
	** your database first.                                          **
	******************************************************************/

	--==( Setup )==--

	DECLARE
		@DummyClassId INT,
		@DummyFieldId INT,
		@FieldId INT,
		@Type INT,
		@Class INT,
		@DstCls INT,
		@FieldName NVARCHAR(100),
		@Custom TINYINT,
		@CustomId UNIQUEIDENTIFIER,
		@Min BIGINT,
		@Max BIGINT,
		@Big BIT,
		@UserLabel NVARCHAR(100),
		@ForeignKey NVARCHAR(100),
		@HelpString NVARCHAR(100),
		@ListRootId INT,
		@WsSelector INT,
		@OldClassName NVARCHAR(100),
		@ClassName NVARCHAR(100),
		@Abstract TINYINT,
		@Sql NVARCHAR(4000),
		@Debug BIT

	SET @Debug = 0

	SET @DummyClassId = 9999
	SELECT @OldClassName = Name, @Abstract = Abstract  FROM Class$ WHERE Id = @ClassId

	--==( Disable Security )==--

	SET @Sql = '
		ALTER TABLE Class$ NOCHECK CONSTRAINT ALL;
		ALTER TABLE ClassPar$ NOCHECK CONSTRAINT ALL;
		ALTER TABLE Field$ NOCHECK CONSTRAINT ALL;
		ALTER TABLE CmObject NOCHECK CONSTRAINT ALL;
		ALTER TABLE MultiStr$ NOCHECK CONSTRAINT ALL;
		ALTER TABLE MultiBigStr$ NOCHECK CONSTRAINT ALL;
		ALTER TABLE MultiBigTxt$ NOCHECK CONSTRAINT ALL;
		DISABLE TRIGGER TR_Field$_UpdateModel_Del ON Field$;
		DISABLE TRIGGER TR_Field$_No_Upd ON Field$;'
	EXEC (@Sql);

	--==( Create the new class )==--

	INSERT INTO Class$
		SELECT @DummyClassId, [Mod], Base, Abstract, @NewClassName
		FROM Class$
		WHERE Id = @ClassId

	--( Load the attributes of the new class.

	-- REVIEW (SteveMiller/AnnB): Will need to add XmlUI if it ever starts
	-- getting to used.

	DECLARE ClassAttributes CURSOR FOR
		SELECT
			Id, Type, Class, DstCls, Name, Custom, CustomId,
			[Min], [Max], Big, UserLabel, HelpString, ListRootId, WsSelector
		FROM Field$
		WHERE Class = @ClassId
		ORDER BY Id

	OPEN ClassAttributes
	FETCH NEXT FROM ClassAttributes INTO
		@FieldId, @Type, @Class, @DstCls, @FieldName, @Custom, @CustomId,
		@Min, @Max, @Big, @UserLabel, @HelpString, @ListRootId,	@WsSelector;

	IF @Debug = 1 BEGIN
		print '========================================================'
		print 'starting loop 1 for class ' + cast(@ClassId as nvarchar(20))
		print '========================================================'
	END
	WHILE @@FETCH_STATUS = 0 BEGIN
		EXEC RenameClass_RenameFieldId
			@ClassId, @DummyClassId, @FieldId, @DummyFieldId OUTPUT
		IF @Debug = 1 BEGIN
			print '@OldClassId: ' + CAST(@ClassId AS NVARCHAR(20))
			print '@NewClassId: ' + CAST(@DummyClassId AS NVARCHAR(20))
			print '@OldFieldId: ' + CAST(@FieldId AS NVARCHAR(20))
			print '@NewFieldId: ' + CAST(@DummyFieldId AS NVARCHAR(20))
			print '--------------------------------------------------------'
		END

		INSERT INTO Field$
			([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId],
			[Min], [Max], [Big], UserLabel, HelpString, ListRootId, WsSelector)
		VALUES
			(@DummyFieldId, @Type, @DummyClassId, @DstCls, @FieldName, @Custom, @CustomID,
				@Min, @Max, @Big, @UserLabel, @HelpString, @ListRootId, @WsSelector)

		FETCH NEXT FROM ClassAttributes INTO
			@FieldId, @Type, @Class, @DstCls, @FieldName, @Custom, @CustomId,
			@Min, @Max, @Big, @UserLabel, @HelpString, @ListRootId,	@WsSelector
	END
	CLOSE ClassAttributes
	DEALLOCATE ClassAttributes

	--==( Copy Data Over )==--

	SET @Sql = N'INSERT INTO ' + @NewClassName + N' SELECT * FROM ' + @OldClassName
	EXEC (@Sql)

	DECLARE NewAttributeTables CURSOR FOR
		SELECT Id, Type, f.Name
		FROM Field$ f
		WHERE Class = @ClassId
			AND Type IN (16, 26, 28) --( 16 MultiUnicode, 26 ReferenceCollection, 28 ReferenceSequence

	OPEN NewAttributeTables
	FETCH NEXT FROM NewAttributeTables INTO @DummyFieldId, @Type, @FieldName
	WHILE @@FETCH_STATUS = 0 BEGIN
		--( INSERT INTO CmPoss_Name SELECT * FROM CmPossibility_Name
		SET @Sql = N'INSERT INTO ' + @NewClassName + N'_' + @FieldName +
			N' SELECT * FROM ' + @OldClassName + N'_' + @FieldName
		EXEC (@Sql)
		FETCH NEXT FROM NewAttributeTables INTO @DummyFieldId, @Type, @FieldName
	END
	CLOSE NewAttributeTables
	DEALLOCATE NewAttributeTables

	--==( Delete the Old )==--

	--( Remove references of the object

	DECLARE Refs CURSOR FOR
		SELECT c.Name, f.Name, Type, Class
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.Class
		WHERE f.Class = @ClassId and f.Type IN (26, 28);
	OPEN Refs
	FETCH NEXT FROM Refs INTO @ClassName, @FieldName, @Type, @Class
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @Sql = N'ALTER TABLE ' + @ClassName + N'_' + @FieldName +
			N' DROP CONSTRAINT _FK_' + @ClassName + N'_' + @FieldName  + N'_Src';
		EXECUTE (@Sql);
		FETCH NEXT FROM Refs INTO @ClassName, @FieldName, @Type, @Class
	END
	CLOSE Refs
	DEALLOCATE Refs

	--( Remove referencing constraints to the object

	DECLARE OutsideRefs CURSOR FOR
		SELECT c.Name, f.Name, Type, Class, DstCls
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.Class
		WHERE f.DstCls = @ClassId;
	OPEN OutsideRefs
	FETCH NEXT FROM OutsideRefs INTO @ClassName, @FieldName, @Type, @Class, @DstCls
	WHILE @@FETCH_STATUS = 0 BEGIN
		IF @Type = 24 AND @ClassName != 'CmObject' BEGIN
			SET @Sql = N'ALTER TABLE ' + @ClassName + N' DROP CONSTRAINT _FK_' +
				@ClassName + N'_' + @FieldName;
			EXECUTE (@Sql);
		END
		IF @Type = 26 OR @Type = 28 BEGIN
			SET @Sql = N'ALTER TABLE ' + @ClassName + N'_' + @FieldName +
				N' DROP CONSTRAINT _FK_' + @ClassName + N'_' + @FieldName  + N'_Dst';
			EXECUTE (@Sql);
		END
		FETCH NEXT FROM OutsideRefs INTO @ClassName, @FieldName, @Type, @Class, @DstCls
	END
	CLOSE OutsideRefs
	DEALLOCATE OutsideRefs

	--( Remove subclass referencing constraints

	DECLARE Subclasses CURSOR FOR SELECT Name FROM Class$ WHERE Base = @ClassId;
	OPEN Subclasses
	FETCH NEXT FROM Subclasses INTO @ClassName
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @Sql = N'ALTER TABLE ' + @ClassName + N' DROP CONSTRAINT _FK_' +
			@ClassName + N'_id';
		EXECUTE (@Sql);
		FETCH NEXT FROM Subclasses INTO @ClassName
	END
	CLOSE Subclasses
	DEALLOCATE Subclasses

	--( Normally the Field$ delete trigger takes care of all this. However,
	--( the trigger also wipes out data, which we don't want.

	--( Drop all the constraints
	EXEC ManageConstraints$ @OldClassName, 'C', 'DROP'
	EXEC ManageConstraints$ @OldClassName, 'D', 'DROP'
	EXEC ManageConstraints$ @OldClassName, 'F', 'DROP'
	EXEC ManageConstraints$ @OldClassName, 'UQ', 'DROP'
	EXEC ManageConstraints$ @OldClassName, 'PK', 'DROP'

	DECLARE OldClassAttributes CURSOR FOR
		SELECT Id, Type, f.Name
		FROM Field$ f
		WHERE Class = @ClassId

	OPEN OldClassAttributes
	FETCH NEXT FROM OldClassAttributes INTO @FieldId, @Type, @FieldName
	WHILE @@FETCH_STATUS = 0 BEGIN
		--( Type 14, 18, 20, 23, 25, 27
		IF @Type IN (14, 18, 20, 23, 25, 27) BEGIN
			SET @Sql = N'DROP VIEW ' + @OldClassName + N'_' + @FieldName
			EXEC (@Sql)
		END

		--( Type 16, 26, or 28
		IF @Type IN (16, 26, 28) BEGIN
			SET @Sql = N'DROP TABLE ' + @OldClassName + N'_' + @FieldName
			EXEC (@Sql)

			IF @Type = 26 BEGIN
				SET @Sql = N'DROP PROCEDURE ReplRC_' +
					@OldClassName + N'_' + @FieldName
				EXEC (@Sql)
			END
			ELSE IF @Type = 28 BEGIN
				SET @Sql = N'DROP PROCEDURE ReplRS_' +
					@OldClassName + N'_' + @FieldName
				EXEC (@Sql)
			END
		END

		FETCH NEXT FROM OldClassAttributes INTO @FieldId, @Type, @FieldName
	END
	CLOSE OldClassAttributes
	DEALLOCATE OldClassAttributes

	--( Drop the table's view
	SET @Sql = N'DROP VIEW ' + @OldClassName + N'_'
	EXEC (@Sql)

	DELETE FROM Field$ WHERE Class = @ClassId
	DELETE FROM ClassPar$ WHERE Src = @ClassId
	DELETE FROM Class$ WHERE Id = @ClassId

	SET @Sql = N'DROP TABLE ' + @OldClassName
	EXEC (@Sql)

	-- This sp doesn't exist for abstract classes
	IF @Abstract = N'0' BEGIN

		SET @Sql = N'DROP PROCEDURE CreateObject_' + @OldClassName
		EXEC (@Sql)

	END

	--==( Now that the old class is gone, give the new class the proper id and name )==--

	--( Take care of the class and field

	UPDATE ClassPar$
	SET Src = @ClassId, Dst = @ClassId
	WHERE Src = @DummyClassId AND Dst = @DummyClassId

	UPDATE ClassPar$ SET Src = @ClassId WHERE Src = @DummyClassId
	UPDATE Class$ SET Id = @ClassId WHERE Id = @DummyClassId

	DECLARE CorrectFieldIds CURSOR FOR SELECT ID FROM Field$ f WHERE Class = @DummyClassId --ORDER BY Id
	OPEN CorrectFieldIds
	FETCH NEXT FROM CorrectFieldIds INTO @DummyFieldId
	IF @Debug = 1 BEGIN
		print '========================================================'
		print 'starting loop 2 for class ' + cast(@ClassId as nvarchar(20))
		print '========================================================'
	END
	WHILE @@FETCH_STATUS = 0 BEGIN
		EXECUTE RenameClass_RenameFieldId @DummyClassId, @ClassId, @DummyFieldId, @FieldId OUTPUT
		IF @Debug = 1 BEGIN
			print '@OldClassId: ' + CAST(@DummyClassId AS NVARCHAR(20))
			print '@NewClassId: ' + CAST(@ClassId AS NVARCHAR(20))
			print '@OldFieldId: ' + CAST(@DummyFieldId AS NVARCHAR(20))
			print '@NewFieldId: ' + CAST(@FieldId AS NVARCHAR(20))
			print '--------------------------------------------------------'
		END
		UPDATE Field$ SET Id = @FieldId WHERE Id = @DummyFieldId
		FETCH NEXT FROM CorrectFieldIds INTO @DummyFieldId
	END
	CLOSE CorrectFieldIds
	DEALLOCATE CorrectFieldIds

	UPDATE Field$ SET Class = @ClassId WHERE Class = @DummyClassId
	UPDATE Field$ SET DstCls = @ClassId WHERE DstCls = @DummyClassId

	--==( Rebuild )==--

	--( Rebuild Class View

	EXECUTE UpdateClassView$ @ClassId, 1

	--( Rebuild Multi Views and procedures ReplaceRef* (they still have class 999)

	DECLARE ClassAttributes CURSOR FOR
		SELECT Id, Type, f.Name
		FROM Field$ f
		WHERE Class = @ClassId
			-- REVIEW (SteveMiller/AnnB):  Type 20 has an ntext, which isn't being used.
			AND Type IN (14, 18, 23, 25, 26, 27, 28)

	OPEN ClassAttributes
	FETCH NEXT FROM ClassAttributes INTO @FieldId, @Type, @FieldName
	WHILE @@FETCH_STATUS = 0 BEGIN
		IF @Type = 26 BEGIN
			SET @Sql = @NewClassName + N'_' + @FieldName
			EXEC GenReplRCProc @Sql;
		END
		ELSE IF @Type = 28 BEGIN
			SET @Sql = @NewClassName + N'_' + @FieldName
			EXEC GenReplRSProc @Sql, @FieldId;
		END
		ELSE BEGIN
			SET @Sql = N'DROP VIEW ' + @NewClassName + N'_' + @FieldName
			EXEC (@Sql)

			SET @Sql = N'CREATE VIEW ' + @NewClassName + N'_' + @FieldName + ' AS SELECT '
			IF @Type = 14
				SET @Sql = @Sql +
					N'[Obj], [Flid], [Ws], [Txt], [Fmt]
					FROM [MultiStr$]
					WHERE [Flid] = ' + CAST(@FieldId AS NVARCHAR(20))
			ELSE IF @Type = 18
				SET @Sql = @Sql +
					N'[Obj], [Flid], [Ws], [Txt], CAST(NULL AS VARBINARY) AS [Fmt]
					FROM [MultiBigStr$]
					WHERE [Flid] = ' + CAST(@FieldId AS NVARCHAR(20))
			ELSE IF @Type IN (23, 25)
				SET @Sql = @Sql +
					N'Owner$ AS Src, Id AS Dst
					FROM CmObject
					WHERE OwnFlid$ = ' + CAST(@FieldId AS NVARCHAR(20))
			ELSE IF @Type = 27
				SET @Sql = @Sql +
					N'Owner$ AS Src, Id AS Dst, OwnOrd$ AS Ord
					FROM CmObject
					WHERE OwnFlid$ = ' + CAST(@FieldId AS NVARCHAR(20))
			EXEC (@Sql)
		END --( IF BEGIN

		FETCH NEXT FROM ClassAttributes INTO @FieldId, @Type, @FieldName
	END --( WHILE
	CLOSE ClassAttributes
	DEALLOCATE ClassAttributes

	--( Rebuild referencing constraints to the object

	DECLARE OutsideRefs CURSOR FOR
		SELECT c.Name, f.Name, Type, Class, DstCls
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.Class
		WHERE f.DstCls = @ClassId
			AND Class != @ClassId; --( The Field$ trigger already took care of self-referencing constraints
	OPEN OutsideRefs
	FETCH NEXT FROM OutsideRefs INTO @ClassName, @FieldName, @Type, @Class, @DstCls
	WHILE @@FETCH_STATUS = 0 BEGIN
		IF @Type = 24 AND @NewClassName != 'CmObject' BEGIN
			set @sql = 'ALTER TABLE [' + @ClassName + '] ADD CONSTRAINT [_FK_' +
				+ @ClassName + '_' + @FieldName + '] ' + CHAR(13) + CHAR(9) +
				' FOREIGN KEY ([' + @FieldName + ']) REFERENCES [' + @NewClassName + '] ([Id])'
			EXECUTE (@Sql);
		END
		--( This block is slightly modified from the Field$ insert trigger
		IF @Type = 26 OR @Type = 28 BEGIN
			set @Sql = N'ALTER TABLE [' + @ClassName + '_' + @FieldName +
				N'] ADD CONSTRAINT [_FK_' + @ClassName + N'_' + @FieldName + N'_Dst] ' +
				N'FOREIGN KEY ([Dst]) REFERENCES [' + @NewClassName + N'] ([Id])'
			exec (@sql)
		END
		FETCH NEXT FROM OutsideRefs INTO @ClassName, @FieldName, @Type, @Class, @DstCls;
	END;
	CLOSE OutsideRefs;
	DEALLOCATE OutsideRefs;

	--( Rebuild foreign keys.

	DECLARE ForeignKeys CURSOR FOR
		SELECT c.Name AS DstClsName, f.Name, Type, Class, DstCls
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.DstCls
		WHERE f.Class = @ClassId
			AND DstCls = @ClassId; --( The Field$ trigger already took care of self-referencing constraints
	OPEN ForeignKeys
	FETCH NEXT FROM ForeignKeys INTO @ClassName, @FieldName, @Type, @Class, @DstCls
	WHILE @@FETCH_STATUS = 0 BEGIN
		IF @Type = 24 AND @NewClassName != 'CmObject' BEGIN
			set @sql = 'ALTER TABLE [' + @NewClassName + '] ADD CONSTRAINT [_FK_' +
				+ @NewClassName + '_' + @FieldName + '] ' + CHAR(13) + CHAR(9) +
				' FOREIGN KEY ([' + @FieldName + ']) REFERENCES [' + @ClassName + '] ([Id])'
			EXECUTE (@Sql);
		END
		--( This block is slightly modified from the Field$ insert trigger
		IF @Type = 26 OR @Type = 28 BEGIN
			set @Sql = N'ALTER TABLE [' + @NewClassName + '_' + @FieldName +
				N'] ADD CONSTRAINT [_FK_' + @NewClassName + N'_' + @FieldName + N'_Dst] ' +
				N'FOREIGN KEY ([Dst]) REFERENCES [' + @ClassName + N'] ([Id])'
			exec (@sql)
		END
		FETCH NEXT FROM ForeignKeys INTO @ClassName, @FieldName, @Type, @Class, @DstCls;
	END;
	CLOSE ForeignKeys;
	DEALLOCATE ForeignKeys;

	--( Rebuild subclass referencing constraints

	SET @sql = N' '
	DECLARE Subclasses CURSOR FOR SELECT Name FROM Class$ WHERE Base = @ClassId;
	OPEN Subclasses
	FETCH NEXT FROM Subclasses INTO @ClassName
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @ForeignKey = N'_FK_' + @ClassName + N'_Id';
		IF object_id(@ForeignKey, N'F') is null BEGIN
			SET @sql = @Sql + N'ALTER TABLE ' + @ClassName + N' WITH CHECK ADD CONSTRAINT ' + @ForeignKey +
					   N' FOREIGN KEY ([Id]) REFERENCES ' + @NewClassName + N' ([Id]);';

			If @Debug = 1
				Print 'SQL: ' + @sql;

			EXECUTE sp_executesql @sql;
			SET @sql = N' '
		END

		FETCH NEXT FROM Subclasses INTO @ClassName
	END
	CLOSE Subclasses
	DEALLOCATE Subclasses

	--( Rebuild the function fnGetRefsToObj

	EXEC CreateGetRefsToObj;

	IF @Abstract = N'0' BEGIN

		--( Rebuild CreateObject_*

		EXEC DefineCreateProc$ @ClassId;

		--( Rebuild the delete object trigger. It was regenerated once already,
		--( but it picks up collection and sequence references from the dummy
		--( class gets picked up.

		EXEC CreateDeleteObj @ClassId;
	END

	--==( Cleanup )==--

	SET @Sql = '
		ENABLE TRIGGER TR_Field$_UpdateModel_Del ON Field$;
		ENABLE TRIGGER TR_Field$_No_Upd ON Field$

		ALTER TABLE Class$ CHECK CONSTRAINT ALL
		ALTER TABLE ClassPar$ CHECK CONSTRAINT ALL
		ALTER TABLE Field$ CHECK CONSTRAINT ALL
		ALTER TABLE CmObject CHECK CONSTRAINT ALL
		ALTER TABLE MultiStr$ CHECK CONSTRAINT ALL
		ALTER TABLE MultiBigStr$ CHECK CONSTRAINT ALL;
		ALTER TABLE MultiBigTxt$ CHECK CONSTRAINT ALL;';
	EXEC (@Sql);
GO

---------------------------------------------------------------------------------

if object_id('TR_Field$_UpdateModel_Del') is not null begin
	print 'removing trigger TR_Field$_UpdateModel_Del'
	drop trigger TR_Field$_UpdateModel_Del
end
go
print 'creating trigger TR_Field$_UpdateModel_Del'
go
create trigger TR_Field$_UpdateModel_Del on Field$ for delete
as
	declare @Clid INT
	declare @DstCls INT
	declare @sName VARCHAR(100)
	declare @sClass VARCHAR(100)
	declare @sFlid VARCHAR(20)
	declare @Type INT
	DECLARE @nAbstract INT

	declare @Err INT
	declare @fIsNocountOn INT
	declare @sql VARCHAR(1000)

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- get the first custom field to process
	Select @sFlid= min([id]) from deleted

	-- loop through all of the custom fields to be deleted
	while @sFlid is not null begin

		-- get deleted fields
		select 	@Type = [Type], @Clid = [Class], @sName = [Name], @DstCls = [DstCls]
		from	deleted
		where	[Id] = @sFlid

		-- get class name
		select 	@sClass = [Name], @nAbstract = Abstract  from class$  where [Id] = @Clid

		if @type IN (14,16,18,20) begin
			-- Remove any data stored for this multilingual custom field.
			declare @sTable VARCHAR(20)
			set @sTable = case @type
				when 14 then 'MultiStr$'
				when 16 then 'MultiTxt$ (No Longer Exists)'
				when 18 then 'MultiBigStr$'
				when 20 then 'MultiBigTxt$'
				end
			--( NOTE (SteveMiller/JohnS): The MultiStr$, MultiBigStr$, and
			--( MultiBigTxt$ tables have foreign key constraints on Field$.ID.
			--( This means that the tables in these Multi* tables must be deleted
			--( before the Field$ row can be deleted. That means the delete
			--( command below probably never deletes anything. It's not bad to
			--( leave the code in, just in case something really weird happnes in
			--( the wild.
			IF @type != 16  -- MultiTxt$ data will be deleted when the table is dropped
			BEGIN
				set @sql = 'DELETE FROM [' + @sTable + '] WHERE [Flid] = ' + @sFlid
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			END

			-- Remove the view created for this multilingual custom field.
			IF @type != 16
				set @sql = 'DROP VIEW [' + @sClass + '_' + @sName + ']'
			ELSE
				SET @sql = 'DROP TABLE [' + @sClass + '_' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail
		end
		else if @type IN (23,25,27) begin
			-- Remove the view created for this custom OwningAtom/Collection/Sequence field.
			set @sql = 'DROP VIEW [' + @sClass + '_' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail
			-- Check for any objects stored for this custom OwningAtom/Collection/Sequence field.
			declare @DelId INT
			select @DelId = [Id] FROM CmObject WHERE [OwnFlid$] = @sFlid
			set @Err = @@error
			if @Err <> 0 goto LFail
			if @DelId is not null begin
				raiserror('TR_Field$_UpdateModel_Del: Unable to remove %s field until corresponding objects are deleted',
						16, 1, @sName)
				goto LFail
			end
		end
		else if @type IN (26,28) begin
			-- Remove the table created for this custom ReferenceCollection/Sequence field.
			set @sql = 'DROP TABLE [' + @sClass + '_' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail

			-- Remove the procedure that handles reference collections or sequences for
			-- the dropped table
			set @sql = N'
				IF OBJECT_ID(''ReplRC_' +
					SUBSTRING(@sClass, 1, 11) +  '_' + SUBSTRING(@sName, 1, 11) +
					''') IS NOT NULL
					DROP PROCEDURE [ReplRC_' + @sClass + '_' + @sName + ']
				IF OBJECT_ID(''ReplRS_' +
					SUBSTRING(@sClass, 1, 11) +  '_' + SUBSTRING(@sName, 1, 11) +
					''') IS NOT NULL
					DROP PROCEDURE [ReplRS_' + @sClass + '_' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail
		end
		else begin
			-- Remove the format column created if this was a custom String field.
			if @type in (13,17) begin
				set @sql = 'ALTER TABLE [' + @sClass + '] DROP COLUMN [' + @sName + '_Fmt]'
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			end
			-- Remove the constraint created if this was a custom ReferenceAtom field.
			-- Not necessary for CmObject : Foreign Key constraints are not created agains CmObject
			if @type = 24 begin
				declare @sTarget VARCHAR(100)
				select @sTarget = [Name] FROM [Class$] WHERE [Id] = @DstCls
				set @Err = @@error
				if @Err <> 0 goto LFail
				if @sTarget != 'CmObject' begin
					set @sql = 'ALTER TABLE [' + @sClass + '] DROP CONSTRAINT [' +
						'_FK_' + @sClass + '_' + @sName + ']'
					exec (@sql)
					set @Err = @@error
					if @Err <> 0 goto LFail
				end
			end
			-- Remove Default Constraint from Numeric or Date fields before dropping the column
			If @type in (1,2,3,4,5,8) begin
				select @sql = 'ALTER TABLE [' + @sClass + '] DROP CONSTRAINT [' + so.name + ']'
				from sysconstraints sc
					join sysobjects so on so.id = sc.constid and so.name like 'DF[_]%'
					join sysobjects so2 on so2.id = sc.id
					join syscolumns sco on sco.id = sc.id and sco.colid = sc.colid
				where so2.name = @sClass   -- Tablename
				and   sco.name = @sName    -- Fieldname
				and   so2.type = 'U'	   -- Userdefined table
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			end
			-- Remove Check Constraint from Numeric fields before dropping the column
			If @type = 2 begin
				select @sql = 'ALTER TABLE [' + @sClass + '] DROP CONSTRAINT [' + so.name + ']'
				from sysconstraints sc
					join sysobjects so on so.id = sc.constid and so.name like '_CK_%'
					join sysobjects so2 on so2.id = sc.id
					join syscolumns sco on sco.id = sc.id and sco.colid = sc.colid
				where so2.name = @sClass   -- Tablename
				and   sco.name = @sName    -- Fieldname
				and   so2.type = 'U'	   -- Userdefined table
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			end

			-- Remove the column created for this custom field.
			set @sql = 'ALTER TABLE [' + @sClass + '] DROP COLUMN [' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail

			-- fix the view associated with this class.
			exec @Err = UpdateClassView$ @Clid, 1
			if @Err <> 0 goto LFail
		end

		--( Rebuild the delete trigger

		EXEC @Err = CreateDeleteObj @Clid
		IF @Err <> 0 GOTO LFail

		--( Rebuild CreateObject_*

		IF @nAbstract != 1 BEGIN
			EXEC @Err = DefineCreateProc$ @Clid
			IF @Err <> 0 GOTO LFail
		END

		-- get the next custom field to process
		Select @sFlid= min([id]) from deleted  where [Id] > @sFlid

	end -- While loop

	--( Rebuild the stored function fnGetRefsToObj
	EXEC @Err = CreateGetRefsToObj
	IF @Err <> 0 GOTO LFail

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return

LFail:
	rollback tran
	return
go

---------------------------------------------------------------------------------

if object_id('TR_Field$_UpdateModel_Ins') is not null begin
	print 'removing trigger TR_Field$_UpdateModel_Ins'
	drop trigger [TR_Field$_UpdateModel_Ins]
end
go
print 'creating trigger TR_Field$_UpdateModel_Ins'
go
create trigger [TR_Field$_UpdateModel_Ins] on [Field$] for insert
as
	declare @sFlid VARCHAR(20)
	declare @Type INT
	declare @Clid INT
	declare @DstCls INT
	declare @sName sysname
	declare @sClass sysname
	declare @sTargetClass sysname
	declare @Min BIGINT
	declare @Max BIGINT
	declare @Big BIT
	declare @fIsCustom bit

	declare @sql NVARCHAR(1000)
	declare @Err INT
	declare @fIsNocountOn INT

	declare @sMin VARCHAR(25)
	declare @sMax VARCHAR(25)
	declare @sTable VARCHAR(20)
	declare @sFmtArg VARCHAR(40)

	declare @sTableName sysname

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- get the first class to process
	Select @sFlid= min([id]) from inserted

	-- loop through all of the classes in the inserted logical table
	while @sFlid is not null begin

		-- get inserted data
		select 	@Type = [Type], @Clid = [Class], @sName = [Name], @DstCls = [DstCls], @Min = [Min], @Max = [Max], @Big = [Big], @fIsCustom = [Custom]
		from	inserted i
		where	[Id] = @sFlid

		-- get class name
		select 	@sClass = [Name]  from class$  where [Id] = @Clid

		-- get target class for Reference Objects
		if @Type in (24,26,28) begin
			select 	@sTargetClass = [Name]  from class$  where [Id] = @DstCls
		end

		if @type = 2 begin

			set @sMin = coalesce(convert(varchar(25), @Min), 0)
			set @sMax = coalesce(convert(varchar(25), @Max), 0)

			-- Add Integer to table sized based on Min/Max values supplied
			set @sql = 'ALTER TABLE [' + @sClass + '] ADD [' + @sName + '] '

			if @Min >= 0 and @Max <= 255
				set @sql = @sql + 'TINYINT NOT NULL DEFAULT ' + @sMin
			else if @Min >= -32768 and @Max <= 32767
				set @sql = @sql + 'SMALLINT NOT NULL DEFAULT ' + @sMin
			else if @Min < -2147483648 or @Max > 2147483647
				set @sql = @sql + 'BIGINT NOT NULL DEFAULT ' + @sMin
			else
				set @sql = @sql + 'INT NOT NULL DEFAULT ' + @sMin
			exec (@sql)
			if @@error <> 0 goto LFail

			-- Add Check constraint
			if @Min is not null and @Max is not null begin
				-- format as text

				set @sql = 'ALTER TABLE [' + @sClass + '] ADD CONSTRAINT [' +
					'_CK_' + @sClass + '_' + @sName + '] ' + CHAR(13) + CHAR(9) +
					' check ( [' + @sName + '] is null or ([' + @sName + '] >= ' + @sMin + ' and  [' + @sName + '] <= ' + @sMax + '))'
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			end

		end
		else if @type IN (14,16,18,20) begin
			-- Define the view or table for this multilingual custom field

			set @sTable = case @type
				when 14 then 'MultiStr$'
				when 18 then 'MultiBigStr$'
				when 20 then 'MultiBigTxt$'
				end
			set @sFmtArg = case @type
				when 14 then '[Fmt]'
				when 18 then '[Fmt]'
				when 20 then 'cast(null as varbinary) as [Fmt]'
				end

			IF @type = 16 BEGIN
				-- TODO (SteveMiller): The Txt field really ought to be cut down
				-- to 83 or less for a number of reasons. First, indexes don't do
				-- a whole lot of good when they get beyond 83 characters. Second,
				-- a lot of these fields probably don't need more than 80, and if
				-- they do, ought to be put in a different field. Third, Firebird
				-- indexes don't go beyond 83.
				--
				-- The fields currently larger than 83 (or 40, for that matter),
				-- is flids 7001 and 20002. 6001004 does in TestLangProj, but that's
				-- "bogus data". These two might become MultiBigTxt fields. Ideally,
				-- word forms ought to be cut down to 83, but Ken indicates that
				-- idioms might be stored there.

				--( See notes under string tables about Latin1_General_BIN

				SET @sql = N'CREATE TABLE ' + @sClass + N'_' + @sName + N' ( ' + CHAR(13) + CHAR(10) +
					CHAR(9) + N'[Obj] INT NOT NULL, ' + CHAR(13) + CHAR(10) +
					CHAR(9) + N'[Ws] INT NOT NULL, ' + CHAR(13) + CHAR(10) +
					CHAR(9) + N'[Txt] NVARCHAR(4000) COLLATE Latin1_General_BIN NOT NULL)'
				EXEC sp_executesql @sql

				SET @sql = N'CREATE INDEX pk_' + @sClass + N'_' + @sName +
					N' ON ' + @sClass + N'_' + @sName + N'(Obj, WS)'
				EXEC sp_executesql @sql
				-- We need these indexes for at least the MakeMissingAnalysesFromLexicion to
				-- perform reasonably. (This is used in parsing interlinear texts.)
				-- SQLServer allows this index even though these fields are
				-- currently too big to index if the whole length is used. In practice, the
				-- limit of 450 unicode characters is very(!) unlikely to be exceeded for
				-- a single wordform or morpheme form.
				if @sName = 'Form' and (@sClass = 'MoForm' or @sClass = 'WfiWordform') BEGIN
					SET @sql = N'CREATE INDEX Ind_' + @sClass + N'_' + @sName +
						N'_Txt ON ' + @sClass + N'_' + @sName + N'(txt)'
					EXEC sp_executesql @sql
				END
			END --( 16
			ELSE BEGIN --( 14, 18, or 20
				set @sql = 'CREATE VIEW [' + @sClass + '_' + @sName + '] AS' + CHAR(13) + CHAR(10) +
					CHAR(9) + 'select [Obj], [Flid], [Ws], [Txt], ' + @sFmtArg + CHAR(13) + CHAR(10) +
					CHAR(9) + 'FROM [' + @sTable + ']' + CHAR(13) + CHAR(10) +
					CHAR(9) + 'WHERE [Flid] = ' + @sFlid
				exec (@sql)
			END
			if @@error <> 0 goto LFail

		end
		else if @type IN (23,25,27) begin
			-- define the view for this OwningAtom/Collection/Sequence custom field.
			set @sql = 'CREATE VIEW [' + @sClass + '_' + @sName + '] AS' + CHAR(13) + CHAR(10) +
				CHAR(9) + 'select [Owner$] as [Src], [Id] as [Dst]'

			if @type = 27 set @sql = @sql + ', [OwnOrd$] as [Ord]'

			set @sql = @sql + CHAR(13) +
				CHAR(9) + 'FROM [CmObject]' + CHAR(13) + CHAR(10) +
				CHAR(9) + 'WHERE [OwnFlid$] = ' + @sFlid
			exec (@sql)
			if @@error <> 0 goto LFail

			--( Adding an owning atomic StText field requires all existing instances
			--( of the owning class to possess an empty StText and StTxtPara.

			IF @type = 23 AND @DstCls = 14 BEGIN
				SET @sql = '
				DECLARE @recId INT,
					@newId int,
					@dummyId int,
					@dummyGuid uniqueidentifier

				DECLARE curOwners CURSOR FOR SELECT [id] FROM ' + @sClass + '
				OPEN curOwners
				FETCH NEXT FROM curOwners INTO @recId
				WHILE @@FETCH_STATUS = 0
				BEGIN
					EXEC CreateObject_StText
						0, @recId, ' + @sFlid + ', null, @newId OUTPUT, @dummyGuid OUTPUT
					EXEC CreateObject_StTxtPara
						null, null, null, null, null, null, @newId, 14001, null, @dummyId, @dummyGuid OUTPUT
					FETCH NEXT FROM curOwners INTO @recId
				END
				CLOSE curOwners
				DEALLOCATE curOwners'

				EXEC (@sql)
			END

		end
		else if @type IN (26,28) begin
			-- define the table for this custom reference collection/sequence field.
			set @sql = 'CREATE TABLE [' + @sClass + '_' + @sName + '] (' + CHAR(13) +
				'[Src] INT NOT NULL,' + CHAR(13) +
				'[Dst] INT NOT NULL,' + CHAR(13)

			if @type = 28 set @sql = @sql + '[Ord] INT NOT NULL,' + CHAR(13)

			set @sql = @sql +
				'CONSTRAINT [_FK_' + @sClass + '_' + @sName + '_Src] ' +
				'FOREIGN KEY ([Src]) REFERENCES [' + @sClass + '] ([Id]),' + CHAR(13) +
				'CONSTRAINT [_FK_' + @sClass + '_' + @sName + '_Dst] ' +
				'FOREIGN KEY ([Dst]) REFERENCES [' + @sTargetClass + '] ([Id]),' + CHAR(13) +
				case @type
					when 26 then ')'
					when 28 then
						CHAR(9) + CHAR(9) + 'CONSTRAINT [_PK_' + @sClass + '_' + @sName + '] ' +
						'PRIMARY KEY CLUSTERED ([Src], [Ord])' + CHAR(13) + ')'
					end
			exec (@sql)
			if @@error <> 0 goto LFail

			if @type = 26 begin
				set @sql = 'create clustered index ' +
						@sClass + '_' + @sName + '_ind on ' +
						@sClass + '_' + @sName + ' ([Src], [Dst])'
				exec (@sql)
				if @@error <> 0 goto LFail

				set @sTableName = @sClass + '_' + @sName
				exec @Err = GenReplRCProc @sTableName
				if @Err <> 0 begin
					raiserror('TR_Field$_UpdateModel_Ins: Unable to create the procedure that handles reference collections for table %s',
							16, 1, @sName)
					goto LFail
				end

			end

			if @type = 28 begin
				set @sTableName = @sClass + '_' + @sName
				exec @Err = GenReplRSProc @sTableName, @sFlid
				if @Err <> 0 begin
					raiserror('TR_Field$_UpdateModel_Ins: Unable to create the procedure that handles reference sequences for table %s',
							16, 1, @sName)
					goto LFail
				end
			end

			--( Insert trigger
			SET @sql = 'CREATE TRIGGER [TR_' + @sClass + '_' + @sName + '_DtTmIns]' + CHAR(13) +
				CHAR(9) + 'ON [' + @sClass + '_' + @sName + '] FOR INSERT ' + CHAR(13) +
				'AS ' + CHAR(13) +
				CHAR(9) + 'UPDATE CmObject SET UpdDttm = GetDate() ' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'FROM CmObject co JOIN inserted ins ON co.[id] = ins.[src] ' + CHAR(13) +
				CHAR(9) + CHAR(13)  +
				CHAR(9) + 'IF @@error <> 0 BEGIN' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'Raiserror(''TR_' + @sClass + '_' + @sName + '_DtTmIns]: ' +
					'Unable to update CmObject'', 16, 1)' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'GOTO LFail' + CHAR(13) +
				CHAR(9) + 'END' + CHAR(13) +
				CHAR(9) + 'RETURN' + CHAR(13) +
				CHAR(9) + CHAR(13) +
				CHAR(9) + 'LFail:' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'ROLLBACK TRAN' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'RETURN' + CHAR(13)
			EXEC (@sql)
			IF @@error <> 0 GOTO LFail

			--( Delete trigger
			SET @sql = 'CREATE TRIGGER [TR_' + @sClass + '_' + @sName + '_DtTmDel]' + CHAR(13) +
				CHAR(9) + 'ON [' + @sClass + '_' + @sName + '] FOR DELETE ' + CHAR(13) +
				'AS ' + CHAR(13) +
				CHAR(9) + 'UPDATE CmObject SET UpdDttm = GetDate() ' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'FROM CmObject co JOIN deleted del ON co.[id] = del.[src] ' + CHAR(13) +
				CHAR(9) + CHAR(13)  +
				CHAR(9) + 'IF @@error <> 0 BEGIN' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'Raiserror(''TR_' + @sClass + '_' + @sName + '_DtTmDel]: ' +
					'Unable to update CmObject'', 16, 1)' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'GOTO LFail' + CHAR(13) +
				CHAR(9) + 'END' + CHAR(13) +
				CHAR(9) + 'RETURN' + CHAR(13) +
				CHAR(9) + CHAR(13) +
				CHAR(9) + 'LFail:' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'ROLLBACK TRAN' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'RETURN' + CHAR(13)
			EXEC (@sql)
			IF @@error <> 0 GOTO LFail

		end
		else begin
			-- add the custom field to the appropriate table
			set @sql = 'ALTER TABLE [' + @sClass + '] ADD [' + @sName + '] ' + case
				when @type = 1 then 'BIT NOT NULL DEFAULT 0'			-- Boolean
				when @type = 3 then 'DECIMAL(28,4) NOT NULL DEFAULT 0'		-- Numeric
				when @type = 4 then 'FLOAT NOT NULL DEFAULT 0.0'		-- Float
				-- Time: default to current time except for fields in LgWritingSystem.
				when @type = 5 AND @sClass != 'LgWritingSystem' then 'DATETIME NULL DEFAULT GETDATE()'
				when @type = 5 AND @sClass = 'LgWritingSystem' then 'DATETIME NULL'
				when @type = 6 then 'UNIQUEIDENTIFIER NULL'			-- Guid
				when @type = 7 then 'IMAGE NULL'				-- Image
				when @type = 8 then 'INT NOT NULL DEFAULT 0'			-- GenDate
				when @type = 9 and @Big is null then 'VARBINARY(8000) NULL'		-- Binary
				when @type = 9 and @Big = 0 then 'VARBINARY(8000) NULL'		-- Binary
				when @type = 9 and @Big = 1 then 'IMAGE NULL'			-- Binary
				when @type = 13 then 'NVARCHAR(4000) NULL'			-- String
				when @type = 15 then 'NVARCHAR(4000) NULL'			-- Unicode
				when @type = 17 then 'NTEXT NULL'				-- BigString
				when @type = 19 then 'NTEXT NULL'				-- BigUnicode
				when @type = 24 then 'INT NULL'					-- ReferenceAtom
				end
			exec (@sql)
			if @@error <> 0 goto LFail
			if @type in (13,17)  begin
				set @sql = 'ALTER TABLE [' + @sClass + '] ADD ' + case @type
					when 13 then '[' + @sName + '_Fmt] VARBINARY(8000) NULL' -- String
					when 17 then '[' + @sName + '_Fmt] IMAGE NULL'			-- BigString
					end
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			end

			-- Set the 'Text In Row' option for the table if type is 7, 17 or 19.
			if @type in (7, 17, 19) exec sp_tableoption @sClass, 'text in row', '1000'

			-- don't create foreign key constraints on CmObject
			if @type = 24 and @sClass != 'CmObject' begin
				set @sql = 'ALTER TABLE [' + @sClass + '] ADD CONSTRAINT [' +		-- ReferenceAtom
					'_FK_' + @sClass + '_' + @sName + '] ' + CHAR(13) + CHAR(9) +
					' FOREIGN KEY ([' + @sName + ']) REFERENCES [' + @sTargetClass + '] ([Id])'
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			end

		end

		-- get the next class to process
		Select @sFlid= min([id]) from inserted  where [Id] > @sFlid

	end  -- While loop

	--( UpdateClassView$ is executed in TR_Field$_UpdateModel_InsLast, which is created
	--( in LangProjSP.sql.

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return

LFail:
	rollback tran
	return
go

---------------------------------------------------------------------------------

--( Rebuild generated stored procedures

DECLARE
	@ClassId1 INT,
	@ClassId2 INT,
	@Abstract BIT,
	@ClassName NVARCHAR(100),
	@FieldId INT,
	@Type INT,
	@FieldName NVARCHAR(100),
	@Sql NVARCHAR(4000);
SET @ClassId2 = 0;

DECLARE ClassFields CURSOR FOR
	SELECT c.Id, c.Abstract, c.Name, f.Id, f.Type, f.Name
	FROM Field$ f
	JOIN Class$ c ON c.Id = f.Class;
OPEN ClassFields;
FETCH NEXT FROM ClassFields INTO @ClassId1, @Abstract, @ClassName, @FieldId, @Type, @FieldName;
WHILE @@FETCH_STATUS = 0 BEGIN

	--( Changes at the class level: Drop the CreateObject_ procedure and
	--( generate the MakeObj_ procedure

	/* --( This will go in later
	IF @ClassId2 != @ClassId1 AND @Abstract = 0 BEGIN
		SET @Sql = 'CreateObject_' + @ClassName
		if object_id(@Sql) is not null begin
			SET @Sql = N'DROP PROCEDURE ' + @Sql
			EXECUTE (@Sql)
		end
		EXEC GenMakeObjProc @ClassId1
	END
	*/
	SET @ClassId2 = @ClassId1;

	--( Changes at the field level. Drop the ReplaceRefColl_ or ReplaceRefSeq_
	--( columns and generate the ReplRC_ or ReplRS_ procedure.

	IF @Type = 26 BEGIN
		SET @Sql = N'ReplaceRefColl_' + @ClassName + N'_' + @FieldName
		if object_id(@Sql) is not null begin
			SET @Sql = N'DROP PROCEDURE ' + @Sql
			EXECUTE (@Sql)
		end
		--( GenReplRCProc shortens the class and field names appropriately
		SET @Sql = @ClassName + N'_' + @FieldName;
		EXEC GenReplRCProc @Sql;
	END
	ELSE IF @Type = 28 BEGIN
		SET @Sql = N'ReplaceRefSeq_' + @ClassName + N'_' + @FieldName
		if object_id(@Sql) is not null begin
			SET @Sql = N'DROP PROCEDURE ' + @Sql
			EXECUTE (@Sql)
		end
		--( GenReplRSProc shortens the class and field names appropriately
		SET @Sql = @ClassName + N'_' + @FieldName;
		EXEC GenReplRSProc @Sql, @FieldId;
	END

	FETCH NEXT FROM ClassFields INTO @ClassId1, @Abstract, @ClassName, @FieldId, @Type, @FieldName;
END
CLOSE ClassFields;
DEALLOCATE ClassFields;
GO

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200210
BEGIN
	UPDATE Version$ SET DbVer = 200211
	COMMIT TRANSACTION
	PRINT 'database updated to version 200211'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200210 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
