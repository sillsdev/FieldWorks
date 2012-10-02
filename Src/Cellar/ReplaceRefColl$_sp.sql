/***********************************************************************************************
 * ReplaceRefColl$
 *
 * Description: General procedure that calls the appropriate ReplaceRefColl_ procedure
 *
 * Parameters:
 *	@flid = the FLID of the field that contains the reference sequence relationship
 *	@SrcObjId = the id of the object that contains the reference sequence relationship
 *	@ntInsIds = the list of insert reference objects
 *	@ntDelIds = the list of delete reference objects
 *
 * Returns:
 *	0 if successful, otherwise the appropriate error code
 **********************************************************************************************/

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
