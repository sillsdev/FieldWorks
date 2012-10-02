/***********************************************************************************************
 * CleanObjInfoTbl$
 *
 * Description:
 *	removes a results set from the ObjInfoTbl$ table
 *
 * Parameters:
 *	@uid=unique Id associated with the subset of data that should be removed from the
 *		ObjInfoTbl$ table
 *
 * Returns:
 *	0 if successful, otherwise an error code
 **********************************************************************************************/
if object_id('CleanObjInfoTbl$') is not null begin
	print 'removing proc CleanObjInfoTbl$'
	drop proc [CleanObjInfoTbl$]
end
go
print 'creating proc CleanObjInfoTbl$'
go
create proc [CleanObjInfoTbl$]
	@uid uniqueidentifier
as
	declare @fIsNocountOn int, @Err int, @sUid nvarchar(50)

	set @Err = 0

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- remove the specified rows from the ObjInfoTbl$ table
	delete	[ObjInfoTbl$] with (rowlock)
	where	[uid] = @uid

	set @Err = @@error
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('CleanObjInfoTbl$: SQL Error %d; Unable to remove rows from the ObjInfoTbl$ table (UID=%s).', 16, 1, @Err, @sUid)
	end

	if @fIsNocountOn = 0 set nocount off

	return @Err
go
