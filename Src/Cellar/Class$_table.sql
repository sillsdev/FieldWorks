-- Class$ table.
if object_id('Class$') is not null begin
	print 'removing proc Class$'
	drop proc [Class$]
end
go
print 'creating table Class$'
create table [Class$] (
	[Id]		int				primary key clustered,
	[Mod]		int		not null	references [Module$] ([Id]),
	[Base]		int		not null	references [Class$] ([Id]),
	[Abstract]	bit,
	[Name]		nvarchar(kcchMaxName) not null	unique
)
go
