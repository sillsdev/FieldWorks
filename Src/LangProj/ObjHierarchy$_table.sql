/***********************************************************************************************
	SQL scripts used to arrange CmObject data from a FieldWorks database
	in a hierarchical structure to facilitate writing the data to a file in XML format.
***********************************************************************************************/

if not object_id('ObjHierarchy$') is null begin
	print 'removing table ObjHierarchy$'
	drop table ObjHierarchy$
end
print 'creating table ObjHierarchy$'
go
create table ObjHierarchy$ (
	strDepth varchar(50),
	intDepth int,
	ownOrd int,
	ownFlid int,
	owner int,
	class int,
	guid uniqueidentifier,
	id int
)
create index idx_id on ObjHierarchy$ (id)
create index idx_strDepth on ObjHierarchy$ (strDepth)
create index idx_flid on ObjHierarchy$ (ownFlid)
create index idx_owner on ObjHierarchy$ (owner)
go
