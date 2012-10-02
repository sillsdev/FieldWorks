-- MultiBigTxt$ table.
if object_id('MultiBigTxt$') is not null begin
	print 'removing table MultiBigTxt$'
	drop table [MultiBigTxt$]
end
go
print 'creating table MultiBigTxt$'
create table [MultiBigTxt$] (
	[Flid]		int		not null	references [Field$] ([Id]),
	[Obj]		int		not null,
	[Ws]		int		not null,
	[Txt]		ntext		COLLATE Latin1_General_BIN not null,

	constraint [_PK_MultiBigTxt$] primary key clustered ([Flid], [Obj], [Ws])
)
create nonclustered index Ind_MultiBigTxt$ on MultiBigTxt$(obj)
go

-- Set 'Text In Row' option for MultiBigTxt$.
exec sp_tableoption 'MultiBigTxt$', 'text in row', '4000'
go
