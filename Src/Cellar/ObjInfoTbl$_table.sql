
-- ObjInfoTbl$ table.
if object_id('ObjInfoTbl$') is not null begin
	print 'removing table ObjInfoTbl$'
	drop table [ObjInfoTbl$]
end
go
print 'creating table ObjInfoTbl$'
go
create table [ObjInfoTbl$]
(
	[uid]		uniqueidentifier not null,
	[ObjId]		int		not null,
	[ObjClass]	int		null,
	[InheritDepth]	int		null		default(0),
	[OwnerDepth]	int		null		default(0),
	[RelObjId]	int		null,
	[RelObjClass]	int		null,
	[RelObjField]	int		null,
	[RelOrder]	int		null,
	[RelType]	int		null,
	[OrdKey]	varbinary(250)	null		default(0)
)
create nonclustered index Ind_ObjInfoTbl$_Id on ObjInfoTbl$(uid, ObjId)
go
