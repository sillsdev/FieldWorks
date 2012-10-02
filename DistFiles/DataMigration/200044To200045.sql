-- update database from version 200044 to 200045
BEGIN TRANSACTION  --( will be rolled back if wrong version#

------------------------------------------------------------------------------------------------
-- Fixed missing CreateObject_ procedures from recent new classes.
-- Added new stored TransferAnnotation stored procedure.
------------------------------------------------------------------------------------------------
exec DefineCreateProc$ 5115
go
exec DefineCreateProc$ 5116
go
exec DefineCreateProc$ 5117
go

/*****************************************************************************
 * TransferAnnotation
 *
 * Description: Transfer all annotations which both begin and end on 'oldObj'
 * to point to 'newObj' instead. Also add 'delta' to the BeginOffset and
 * EndOffset of each transferred annotation.
 *
 * Parameters:
 *	@oldObj -- object to move annotations from
 *	@newObj -- object to move them to
 *	@delta -- amount to add to BeginOffset and EndOffset
 *
 * Returns: none
  *****************************************************************************/

if object_id('TransferAnnotation') is not null begin
	print 'removing proc TransferAnnotation'
	drop procedure TransferAnnotation
end
go
print 'creating procedure TransferAnnotation'
go
create proc TransferAnnotation
	@oldObj int,
	@newObj int,
	@delta int
as
update CmBaseAnnotation set
	BeginOffset = BeginOffset + @delta, BeginObject = @newObj,
	EndOffset = EndOffset + @delta, EndObject = @newObj
where BeginObject = @oldObj and EndObject = @oldObj
GO

-------------------------------------------------------------------------------

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200044
begin
	update Version$ set DbVer = 200045
	COMMIT TRANSACTION
	print 'database updated to version 200045'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200044 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
