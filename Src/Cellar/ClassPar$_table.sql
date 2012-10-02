-- ClassPar$ table.
if object_id('ClassPar$') is not null begin
	print 'removing proc ClassPar$'
	drop proc [ClassPar$]
end
go
print 'creating table ClassPar$'
create table [ClassPar$] (
	[Src]		int		not null	references [Class$] ([Id]),
	[Dst]		int		not null	references [Class$] ([Id]),
	[Depth] 	int 		not null,

	constraint [_CK_ClassPar$_Depth] check (([Depth] > 0 and [Src] <> [Dst]) or ([Depth] = 0 and [Src] = [Dst])),
	constraint [_UQ_ClassPar$_Depth] unique ([Src], [Depth]),
	constraint [_PK_ClassPar$] primary key clustered ([Src], [Dst])
)
create nonclustered index Ind_ClassPar$_Dst_Src on ClassPar$([Dst], [Src])
go
