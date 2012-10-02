/***********************************************************************************************
	Table to synchronize data updates between applications.
***********************************************************************************************/
print 'creating table Sync$'
create table [Sync$]
(
	[Id] int primary key clustered identity(1,1),
	[LpInfoId] uniqueidentifier null,
	[Msg] int null,
	[ObjId] int null,
	[ObjFlid] int null
)
go