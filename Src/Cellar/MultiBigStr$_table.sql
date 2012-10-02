--( The section for creating the MultiTxt$ table used to go here.
--( It has since been moved to TR_Field$_UpdateModel_Ins

-- MultiBigStr$ table.
if object_id('MultiBigStr$') is not null begin
	print 'removing table MultiBigStr$'
	drop table [MultiBigStr$]
end
go
print 'creating table MultiBigStr$'
create table [MultiBigStr$] (
	[Flid]		int		not null	references [Field$] ([Id]),
	[Obj]		int		not null,
	[Ws]		int		not null,
	[Txt] 		ntext		COLLATE Latin1_General_BIN not null,
	[Fmt] 		image		not null,

	constraint [_PK_MultiBigStr$] primary key clustered ([Flid], [Obj], [Ws])
)
create nonclustered index Ind_MultiBigStr$ on MultiBigStr$(obj)
go

-- Set 'Text In Row' option for MultiBigStr$.
exec sp_tableoption 'MultiBigStr$', 'text in row', '4000'
go
