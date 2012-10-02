-- Update database from version 200211 to 200212
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- ReplaceRefSeq had a constant in it that made the procedure fail in the last
-- migration. This will probably fix LT-8164 and LT-8165.
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
	where	f.[Id] = @flid and f.[Type] = 28

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

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200211
BEGIN
	UPDATE Version$ SET DbVer = 200212
	COMMIT TRANSACTION
	PRINT 'database updated to version 200212'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200211 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
