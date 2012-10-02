/***********************************************************************************************
 * CleanObjListTbl$
 *
 * Description:
 *	removes a results set from the ObjInfoTbl$ table
 *
 * Parameters:
 *	@uid=unique identifier associated with the list of objects that is to be removed
 *
 * Returns:
 *	0 if successful, otherwise an error code
 **********************************************************************************************/
if object_id('CleanObjListTbl$') is not null begin
	print 'removing proc CleanObjListTbl$'
	drop proc [CleanObjListTbl$]
end
go
print 'creating proc CleanObjListTbl$'
go
create proc [CleanObjListTbl$]
	@uid uniqueidentifier
as
	declare @fIsNocountOn int, @Err int, @sUid nvarchar(50)

	set @Err = 0

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- remove the specified rows from the ObjListTbl$ table
	delete	[ObjListTbl$] with (rowlock)
	where	[uid] = @uid

	set @Err = @@error
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('CleanObjListTbl$: SQL Error %d; Unable to remove rows from the ObjInfoTbl$ table (UID=%s).', 16, 1, @Err, @sUid)
	end

	if @fIsNocountOn = 0 set nocount off

	return @Err
go
