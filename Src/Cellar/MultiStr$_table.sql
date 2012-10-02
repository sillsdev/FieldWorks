--( The Txt field is collated to Latin1_General_BIN because Yi,
--( IPA, and Khmer at least are not in the Microsoft collation
--( tables currently. This makes equality tests fail. We have
--( discovered however that they equate correctly in binary
--( collations. How this effects sorting is yet to be seen, and
--( we are talking about ways to fix that.

-- MultiStr$ table.
if object_id('MultiStr$') is not null begin
	print 'removing table MultiStr$'
	drop table [MultiStr$]
end
go
print 'creating table MultiStr$'
create table [MultiStr$] (
	[Flid]		int		not null	references [Field$] ([Id]),
	[Obj] 		int		not null,
	[Ws]		int		not null,
	[Txt] 		nvarchar(kcchMaxUniVarChar) COLLATE Latin1_General_BIN not null,
	[Fmt] 		varbinary(kcbMaxVarBin)	not null,

	constraint [_PK_MultiStr$] primary key clustered ([Flid], [Obj], [Ws])
)
create nonclustered index Ind_MultiStr$ on MultiStr$(obj)
go