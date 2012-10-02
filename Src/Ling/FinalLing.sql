-- A couple of ad-hoc indexes. (Review SteveMi(JohnT): should we fudge the update Field$ trigger to make these?
create index AnnInstanceOfIdx on CmAnnotation(InstanceOf)
go
create index BaBeginObjectIdx on CmBaseAnnotation(BeginObject)
go
