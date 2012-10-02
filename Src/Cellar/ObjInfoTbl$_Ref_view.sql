-- ObjInfoTbl$_Ref view.
if object_id('ObjInfoTbl$_Ref') is not null begin
	print 'removing view ObjInfoTbl$_Ref'
	drop view [ObjInfoTbl$_Ref]
end
go
print 'creating view ObjInfoTbl$_Ref'
go
create view [ObjInfoTbl$_Ref]
as
	select	*
	from	ObjInfoTbl$
	where	[RelType] in (kcptReferenceAtom, kcptReferenceCollection, kcptReferenceSequence)

go
