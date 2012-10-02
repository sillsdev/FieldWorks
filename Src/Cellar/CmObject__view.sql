if object_id('CmObject_') is not null begin
	print 'removing view CmObject_'
	drop view [CmObject_]
end
go
print 'creating view CmObject_'
go
create view [CmObject_]
as
	select	[CmObject].*
	from	[CmObject]
go