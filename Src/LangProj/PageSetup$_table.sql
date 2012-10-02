/***********************************************************************************************
	PageSetup$
	Description:
		This table is used to persist Page Setup information.
***********************************************************************************************/
-- PageSetup$ table.
if not object_id('PageSetup$') is null
  begin print 'removing table PageSetup$' drop table PageSetup$
end
go


-- Strictly speaking, the "Id" column should have a foreign key constraint to the ID column of
-- CmMajorObject but this would make deletions difficult, and a few stray records in this
-- table won't hurt.
print 'creating table PageSetup$'
go
create table [PageSetup$] (
	[Id] int primary key clustered,
	[MarginLeft] int not null,
	[MarginRight] int not null,
	[MarginTop] int not null,
	[MarginBottom] int not null,
	[MarginHeader] int not null,
	[MarginFooter] int not null,
	[PaperSize] int not null,
	[PaperWidth] int not null,
	[PaperHeight] int not null,
	[Orientation] int not null,
	[Header] nvarchar(400),
	[Header_Fmt] varbinary(400),
	[Footer] nvarchar(400),
	[Footer_Fmt] varbinary(400),
	[PrintFirstHeader] bit
)
go
