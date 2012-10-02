-- ObjInfoTbl$_Owned view.
if object_id('ObjInfoTbl$_Owned') is not null begin
	print 'removing view ObjInfoTbl$_Owned'
	drop view [ObjInfoTbl$_Owned]
end
go
print 'creating view ObjInfoTbl$_Owned'
go
create view [ObjInfoTbl$_Owned]
as
	select	*
	from	ObjInfoTbl$
	where	[RelType] in (kcptOwningAtom, kcptOwningCollection, kcptOwningSequence)
go