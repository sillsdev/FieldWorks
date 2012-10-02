/*****************************************************************************
 * ClassIsDerivedFrom$
 *
 * Description: Determines whether the class passed as the @class is really
 * a subclass of the given base class.
 *
 * Parameters:
 *	@class		Class ID of class which is purportedly a subclass of
 *			@baseClass
 *	@baseClass 	Class ID of class which is purportedly a base class of
 *			@class
 *	@fValid  out	Set to 0 if @class is not a valid class ID or if it is
 *			not a subclass of the given base class
 *
 * Returns: nothing
 *
 *****************************************************************************/
if object_id('ClassIsDerivedFrom$') is not null begin
	print 'removing proc ClassIsDerivedFrom$'
	drop proc [ClassIsDerivedFrom$]
end
go
print 'creating proc ClassIsDerivedFrom$'
go

create proc ClassIsDerivedFrom$
	@class int,
	@baseClass int,
	@fValid int out
as
	DECLARE @actualBase int
	select @actualBase = Base from class$ where id = @class
	if @actualBase = @baseClass
		set @fValid = 1
	else begin
		if @actualBase > 0
			exec ClassIsderivedFrom$ @actualBase, @baseClass, @fValid out
		else
			set @fValid = 0
	end
GO
