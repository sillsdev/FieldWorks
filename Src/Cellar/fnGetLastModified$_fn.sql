/***********************************************************************************************
 * Function: fnGetLastModified$
 *
 * Description:
 *	retrieves the latest modification date and time of the specified object and objets owned
 *	by the specified object
 *
 * Parameters:
 *	@ObjId=the object Id
 *
 * Returns:
 *	The date and time of the last modification
 **********************************************************************************************/
if object_id('fnGetLastModified$') is not null begin
	print 'removing function fnGetLastModified$'
	drop function [fnGetLastModified$]
end
go
print 'creating function fnGetLastModified$'
go
create function [fnGetLastModified$] (@ObjId int)
returns smalldatetime
as
begin
	declare @dttmLastUpdate smalldatetime

	-- get all objects owned by the specified object
	select	@dttmLastUpdate = max(co.[UpdDttm])
	from	fnGetOwnershipPath$ (@Objid, null, 1, 1, 0) oi
			join [CmObject] co on oi.[ObjId] = co.[Id]

	return @dttmLastUpdate
end
go
