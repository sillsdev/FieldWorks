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
