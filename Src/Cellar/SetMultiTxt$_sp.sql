/***********************************************************************************************
 * SetMultiTxt$
 *
 * Description:
 *	Inserts, updates, or deletes a multi-string in the SetMultiTxt$ string table
 *
 * Parameters:
 *	@flid=field id of the string
 *	@obj=the object that owns the string
 *	@ws=the string's language writing system
 *	@txt=the text contents of the string, if null then the multi-string will be deleted
 *
 * Returns:
 *	0 if successful, otherwise an error code
 **********************************************************************************************/
if object_id('SetMultiTxt$') is not null begin
	print 'removing procedure SetMultiTxt$'
	drop proc [SetMultiTxt$]
end
go
print 'creating proc SetMultiTxt$'
go
create proc [SetMultiTxt$]
	@flid int,
	@obj int,
	@ws int,
	@txt nvarchar(4000)
as
	declare @sTranName varchar(50), @nTrnCnt int
	declare	@fIsNocountOn int, @Err int
	declare @nRowCnt int
	DECLARE
		@nvcTable NVARCHAR(60),
		@nvcSql NVARCHAR(4000)

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- determine if a transaction already exists; if one does then create a savepoint,
	--	otherwise create a transaction
	set @nTrnCnt = @@trancount
	set @sTranName = 'SetMultiTxt$_tr' + convert(varchar(8), @@nestlevel)
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName

	EXEC GetMultiTableName @flid, @nvcTable OUTPUT

	-- determine if the string should be removed
	if @txt is null or len(@txt) = 0 begin
		SET @nvcSql = N'DELETE FROM ' + @nvcTable + CHAR(13) +
			N'WHERE Obj = @nObj AND Ws = @nWs'

		EXECUTE sp_executesql @nvcSql, N'@nObj INT, @nWs INT', @obj, @ws
		set @Err = @@error
		if @Err <> 0 goto LCleanUp
	end
	else begin
		-- attempt to update the string, if no rows are updated then insert the string
		SET @nvcSql =
			N'UPDATE ' + @nvcTable + CHAR(13) +
			N'SET Txt = @nvcTxt ' + CHAR(13) +
			N'WHERE Obj = @nObj AND Ws = @nWs'
		EXECUTE sp_executesql @nvcSql,
			N'@nvcTxt NVARCHAR(4000), @nObj INT, @nWs INT',
			@txt, @obj, @ws

		select @Err = @@error, @nRowCnt = @@rowcount
		if @Err <> 0
			goto LCleanUp

		if @nRowCnt = 0 begin
			SET @nvcSql =
				N'INSERT INTO ' + @nvcTable +
				N' (Obj, Ws, Txt)' + CHAR(13) +
				CHAR(9) + N'VALUES (@nObj, @nWs, @nvcTxt)'
			EXECUTE sp_executesql @nvcSQL,
				N'@nvcTxt NVARCHAR(4000), @nObj INT, @nWs INT',
				@txt, @obj, @ws
			set @Err = @@error
			if @Err <> 0 goto LCleanUp
		end
	end

	-- update the timestamp column in CmObject by updating the update date and time
	update	[CmObject]
	set	[UpdDttm] = getdate()
	where	[Id] = @obj
	set @Err = @@error
	if @Err <> 0 goto LCleanUp

LCleanUp:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	if @Err = 0 begin
		-- if a transaction was created within this procedure commit it
		if @nTrnCnt = 0 commit tran @sTranName
	end
	else begin
		rollback tran @sTranName
	end

	return @err
go
