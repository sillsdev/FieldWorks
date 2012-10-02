/***********************************************************************************************
 * GenReplRCProc
 *
 * Description:
 *	Generates the procedures that handle reference collections for a particular table
 *
 * Parameters:
 *	@sTbl = the name of the reference sequence joiner table
 *
 * Returns:
 *	0 if successful, otherwise the appropriate error code
 *
 * Notes:
 *	This procedure is slightly altered from its predecessor, DefineReplaceRefCollProc$.
 *
 *	To regenerate all the ReplRC stored procedures, execute this code:

		DECLARE @name NVARCHAR(MAX);
		DECLARE curReplRC CURSOR FOR
			SELECT c.Name + '_' + f.Name
			FROM Field$ f
			JOIN Class$ c ON c.Id = f.Class
			WHERE f.Type = 26
			ORDER BY 1;
		OPEN curReplRC
		FETCH curReplRC INTO @name;
		WHILE @@FETCH_STATUS = 0 BEGIN
			PRINT @name --( nice for debugging, but can be taken out.
			EXEC GenReplRCProc @name
			FETCH curReplRC INTO @name;
		END
		CLOSE curReplRC;
		DEALLOCATE curReplRC;
 **********************************************************************************************/

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
			FROM ' + @sTbl + ' jt
			JOIN (
				SELECT Id ObjId, COUNT(*) DelCnt
				FROM dbo.fnGetIdsFromString(@ntDelIds)
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
			JOIN (SELECT Id FROM dbo.fnGetIdsFromString(@ntDelIds)) AS ol
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
			insert into ['+@sTbl+'] ([Src], [Dst])
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
			FROM dbo.fnGetIdsFromString(@ntInsIds) AS ol

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
