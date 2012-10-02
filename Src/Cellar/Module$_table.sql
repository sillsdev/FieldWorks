-- Module$ table.
if object_id('Module$') is not null begin
	print 'removing proc Module$'
	drop proc [Module$]
end
go
print 'creating table Module$'
create table [Module$] (
	[Id] 		int 				primary key clustered,
	[Name] 		nvarchar(kcchMaxName) not null	unique,
	[Ver] 		int 		not null,
	[VerBack] 	int 		not null,

	constraint [_CK_Module$_VerBack] check (0 < [VerBack] and [VerBack] <= [Ver])
)
go
