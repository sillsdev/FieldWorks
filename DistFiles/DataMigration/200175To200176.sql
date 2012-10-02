-- update database FROM version 200175 to 200176
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- Use comma delimited lists instead of XML for IDs in ReplaceRefColl_*
-- procedures.
-------------------------------------------------------------------------------

if object_id('DefineReplaceRefCollProc$') is not null begin
	print 'removing proc DefineReplaceRefCollProc$'
	drop proc [DefineReplaceRefCollProc$]
end
go
print 'Creating proc DefineReplaceRefCollProc$'
go
create proc [DefineReplaceRefCollProc$]
	@sTbl sysname
as
	declare @sDynSql nvarchar(4000), @sDynSql2 nvarchar(4000)
	declare @err int

	if object_id('ReplaceRefColl_' + @sTbl ) is not null begin
		set @sDynSql = 'alter '
	end
	else begin
		set @sDynSql = 'create '
	end

set @sDynSql = @sDynSql + N'
proc ReplaceRefColl_' + @sTbl +'
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
	set @sTranName = ''ReplaceRef$_'+@sTbl+''' + convert(varchar(11), @@trancount)
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
	set @Err = @@error
	if @Err <> 0 begin
		raiserror(''ReplaceRefSeq_'+@sTbl+': SQL Error %d; Unable to create a transaction'', 16, 1, @Err)
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
			raiserror(''ReplaceRefColl_'+@sTbl+': SQL Error %d; Unable to create a temporary table'', 16, 1, @Err)
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
			raiserror(''ReplaceRefColl_'+@sTbl+': SQL Error %d; Unable to insert objects that are referenced more than once: SrcObjId(src) = %d'',
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
			raiserror(''ReplaceRefColl_'+@sTbl+': SQL Error %d; Unable to delete objects from a reference collection: SrcObjId(src) = %d'',
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
				raiserror(''ReplaceRefColl_'+@sTbl+': SQL Error %d; Unable to reinsert objects into a reference collection: SrcObjId(src) = %d'',
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
			raiserror(''ReplaceRefColl_'+@sTbl+': SQL Error %d; Unable to insert objects into a reference collection: SrcObjId(src) = %d'',
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
		raiserror('DefineReplaceRefCollProc: SQL Error %d: Unable to create or alter the procedure ReplaceRefColl_%s$',
				16, 1, @Err, @sTbl)
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

	SELECT @nvcSql = N'EXEC ReplaceRefColl_' + c.Name + N'_' + f.Name +
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

--( Update all the ReplaceRefColl_* stored procedures

DECLARE @nvcTableName NVARCHAR(200)
DECLARE curRefCols CURSOR FOR
	SELECT c.Name + N'_' + f.Name
	FROM Class$ c
	JOIN Field$ f ON f.Class = c.Id
	WHERE f.Type = 26

OPEN curRefCols
FETCH NEXT FROM curRefCols INTO @nvcTableName
WHILE @@FETCH_STATUS = 0 BEGIN
	EXEC DefineReplaceRefCollProc$ @nvcTableName
	FETCH NEXT FROM curRefCols INTO @nvcTableName
END
CLOSE curRefCols
DEALLOCATE curRefCols
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200175
BEGIN
	UPDATE [Version$] SET [DbVer] = 200176
	COMMIT TRANSACTION
	PRINT 'database updated to version 200176'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200175 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
