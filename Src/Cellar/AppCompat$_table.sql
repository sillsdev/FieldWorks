
-- AppCompat$ table.
if object_id('AppCompat$') is not null begin
	print 'removing proc AppCompat$'
	drop proc [AppCompat$]
end
go
print 'creating table AppCompat$'
create table [AppCompat$] (
	[AppGuid] uniqueidentifier primary key,
	[AppName] nvarchar(200),
	[EarliestCompatVer] int not null,
	[LastKnownCompatVer] int not null,
)
go

insert into AppCompat$([AppGuid], [AppName], [EarliestCompatVer], [LastKnownCompatVer])
values('39886581-4DD5-11d4-8078-0000C0FB81B5', 'FwNotebook', 500, 500);
insert into AppCompat$([AppGuid], [AppName], [EarliestCompatVer], [LastKnownCompatVer])
values('5EA62D01-7A78-11d4-8078-0000C0FB81B5', 'FwChoicesListEditor', 500, 500);
insert into AppCompat$([AppGuid], [AppName], [EarliestCompatVer], [LastKnownCompatVer])
values('A7D421E1-1DD3-11d5-B720-0010A4B54856', 'TE', 500, 500);
insert into AppCompat$([AppGuid], [AppName], [EarliestCompatVer], [LastKnownCompatVer])
values('8645FA4B-EE90-11D2-A9B8-0080C87B6086', 'FwMorphologyModelEditor', 500, 500);
insert into AppCompat$([AppGuid], [AppName], [EarliestCompatVer], [LastKnownCompatVer])
values('8645fa4d-ee90-11d2-a9b8-0080c87b6086', 'FwLexEd', 500, 500);
insert into AppCompat$([AppGuid], [AppName], [EarliestCompatVer], [LastKnownCompatVer])
values('76230C21-7084-11d5-83CD-0050BA78F57C', 'FwExplorer', 500, 500);
go
