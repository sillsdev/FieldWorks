-- ObjListTbl$ table.
if object_id('ObjListTbl$') is not null begin
	print 'removing table ObjListTbl$'
	drop table [ObjListTbl$]
end
go
print 'creating table ObjListTbl$'
go
create table [ObjListTbl$] (
	[uid] 	uniqueidentifier	not null,
	[ObjId] int			not null,
	[Ord] 	int			not null,
	[Class]	int			null
)
create nonclustered index IND_ObjListTbl$ on ObjListTbl$ (uid, ObjId)
go