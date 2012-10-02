/***********************************************************************************************
 * Trigger: TR_CmObject$_UpdDttm_Del
 *
 * Description:
 *	Updates the UpdDttm on owers of deleted objects.
 **********************************************************************************************/

if object_id('TR_CmObject$_UpdDttm_Del') is not null begin
	print 'removing trigger TR_CmObject$_UpdDttm_Del'
	drop trigger TR_CmObject$_UpdDttm_Del
end
go
print 'creating trigger TR_CmObject$_UpdDttm_Del'
go
create trigger TR_CmObject$_UpdDttm_Del on CmObject for delete
as
	declare @fIsNocountOn int
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	update CmObject set UpdDttm = getdate()
		from CmObject co JOIN deleted del on co.[id] = del.[owner$]

	if @@error <> 0 begin
		raiserror('TR_CmObject$_UpdDttm_Del: Unable to update owning object', 16, 1)
		goto LFail
	end

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return

LFail:
	-- because the transaction is ROLLBACKed the rows in the ObjListTbl$ will be removed
	rollback tran
	return
go
