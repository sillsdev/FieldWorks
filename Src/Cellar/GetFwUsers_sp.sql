/*****************************************************************************
 * GetFwUsers
 *
 * Description: Get the list of users defined for the current FW server. Any
 * underscores in the username are replaced with spaces
 *
 * Parameters:
 *	none
 * Returns: list of users
 *
 *****************************************************************************/

if object_id('GetFwUsers') is not null begin
	print 'removing proc GetFwUsers'
	drop procedure GetFwUsers
end
go
print 'creating procedure GetFwUsers'
go

create proc GetFwUsers
as
	select REPLACE(name, '_', ' ') "name"
	from master.dbo.syslogins
	where isntgroup != 1
	and isntuser !=1
	and name != 'FWDeveloper'
	and name != 'sa'
GO
