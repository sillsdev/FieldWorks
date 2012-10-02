/*****************************************************************************
 * IsValidObject$
 *
 * Description: Determines whether the given class is the actual type or a
 * base class type of the given object id.
 *
 * Parameters:
 *	@idOfObjectToCheck	HVO of object to check
 *	@class 			Class ID
 *	@fValid  out		Set to 0 if object does not exist or is not
 *				the type (or subtype) of the given class
 *
 * Returns: nothing
 *
 *****************************************************************************/

if object_id('IsValidObject$') is not null begin
	print 'removing proc IsValidObject$'
	drop proc [IsValidObject$]
end
go
print 'creating proc IsValidObject$'
go

create proc IsValidObject$
	@idOfObjectToCheck int,
	@class int,
	@fValid int out
as
	DECLARE @actualClass int

	select @actualClass = class$ from CmObject where id = @idOfObjectToCheck
	if @class = @actualclass
		set @fValid = 1
	else
		exec ClassIsDerivedFrom$ @actualClass, @class, @fValid out
GO
