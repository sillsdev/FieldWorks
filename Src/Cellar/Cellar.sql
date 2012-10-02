#include "Cellar.sqi"

#ifdef DEBUG
print 'removing foreign key constraints'
begin
	declare @strSql varchar(kcchMaxSql),
		@bFlag bit,
		@iRows int

	set @bFlag = 0

	while @bFlag = 0
	begin
		select top 1 @strSql = 'alter table '+so2.name+' drop constraint '+ so.name
		from sysconstraints sc, sysobjects so, sysobjects so2
		where 	sc.constid = so.id
			and sc.id = so2.id
			and so.name like 'FK%'

		set @iRows = @@rowcount
		if @iRows = 0 set @bFlag = 1
		else  exec (@strSql)
	end
end
go
#endif

-- _Meta_Class table.
#ifdef DEBUG
if exists ( select * from sysobjects where id = object_id('_Meta_Class') )
begin
	print 'removing table _Meta_Class'
	drop table _Meta_Class
end
go
#endif
print 'creating table _Meta_Class'
create table [_Meta_Class] (
	[ID] int primary key clustered,
	[Base] int not null,
	foreign key ([Base]) references [_Meta_Class] ([ID]),
	[ClsID] uniqueidentifier unique not null,
	[Abstract] bit,
	[Name] nvarchar(kcchMaxName) unique not null,
)
go


-- _Meta_Field table.
#ifdef DEBUG
if exists ( select * from sysobjects where id = object_id('_Meta_Field') )
begin
	print 'removing table _Meta_Field'
	drop table _Meta_Field
end
go
#endif
print 'creating table _Meta_Field'
create table [_Meta_Field] (
	[ID] int primary key clustered identity(1, 1),
	[Type] int not null,
	[Class] int not null,
	foreign key ([Class]) references [_Meta_Class] ([ID]),
	[DstCls] int null,
	foreign key ([DstCls]) references [_Meta_Class] ([ID]),
	check ((([Type] < kcptMinObj) and ([DstCls] is null)) or (([Type] >= kcptMinObj) and ([DstCls] is not null))),
	[Name] nvarchar(kcchMaxName) not null,
	unique ([Class], [Name]),
)
go


-- _Meta_Class_Closure table.
#ifdef DEBUG
if exists ( select * from sysobjects where id = object_id('_Meta_Class_Closure') )
begin
	print 'removing table _Meta_Class_Closure'
	drop table _Meta_Class_Closure
end
go
#endif
print 'creating table _Meta_Class_Closure'
create table [_Meta_Class_Closure] (
	[SrcID] int not null,
	foreign key ([SrcID]) references [_Meta_Class] ([ID]),
	[DstID] int not null,
	foreign key ([DstID]) references [_Meta_Class] ([ID]),
	primary key clustered ([SrcID], [DstID]),
	[Depth] int not null,
	check ([Depth] >= 0)
)

go


-- _Add_Class_Core stored procedure. This adds the class to _Meta_Class and the appropriate
-- rows to _Meta_Class_Closure. The parameters should not be null.
-- REVIEW ShonK: should this check for the existance of the class?
if exists ( select * from sysobjects where id = object_id('_Add_Class_Core') )
begin
	print 'removing proc _Add_Class_Core'
	drop proc _Add_Class_Core
end
go
print 'creating proc _Add_Class_Core'
go
create procedure [_Add_Class_Core]
	@id int,
	@base int,
	@clsid uniqueidentifier,
	@abstract bit,
	@name nvarchar(kcchMaxName)
as
	SpBegin()

	declare @depth int
	declare @idT int

	insert into [_Meta_Class] ([ID], [Base], [ClsID], [Abstract], [Name])
		values(@id, @base, @clsid, @abstract, @name)
	SpCheck(@@error)

	insert into [_Meta_Class_Closure] ([SrcID], [DstID], [Depth]) values(@id, @id, 0)
	SpCheck(@@error)

	set @depth = 1
	set @idT = @id
	while @base <> @idT begin
		insert into [_Meta_Class_Closure] ([SrcID], [DstID], [Depth])
			values(@id, @base, @depth)
		SpCheck(@@error)

		set @idT = @base
		set @base = (select [Base] from [_Meta_Class] where [ID]=@idT)
		SpCheck(@@error)

		set @depth = @depth + 1
	end

	SpEnd()
go


-- _Add_Class stored procedure. If @clsid is null, a guid is generated.
-- Returns the ID of the class.
-- REVIEW ShonK: should this check for the existance of the class?
if exists ( select * from sysobjects where id = object_id('_Add_Class') )
begin
	print 'removing proc _Add_Class'
	drop proc _Add_Class
end
go
print 'creating proc _Add_Class'
go
create procedure [_Add_Class]
	@id int output,
	@base int,
	@clsid uniqueidentifier output,
	@abstract bit,
	@name nvarchar(kcchMaxName)
as
	declare @e int

	if @clsid is null set @clsid = newid()
	set @id = (select max([ID]) + 1 from [_Meta_Class])
	execute @e = _Add_Class_Core @id, @base, @clsid, @abstract, @name
	return @e
go


-- Add the CmObject class.
execute _Add_Class_Core 0, 0, '3787D2A0-B1E1-11d3-8D8A-005004DEFEC4', 1, 'CmObject'


-- _Text_Prop table.
#ifdef DEBUG
if exists ( select * from sysobjects where id = object_id('_Text_Prop') )
begin
	print 'removing table _Text_Prop'
	drop table _Text_Prop
end
go
#endif
print 'creating table _Text_Prop'
create table [_Text_Prop] (
	[ID] int primary key clustered identity(1, 1),
	[Fmt] varbinary(kcbMaxVarBin) not null,
)

go

-- _Ensure_Text_Prop stored procedure. This checks for an existing _Text_Prop with the
-- given format information. If it exists, its ID is returned. If it doesn't exist, a row is
-- created and its ID is returned.
if exists ( select * from sysobjects where id = object_id('_Ensure_Text_Prop') )
begin
	print 'removing proc _Ensure_Text_Prop'
	drop proc _Ensure_Text_Prop
end
go
print 'creating proc _Ensure_Text_Prop'
go
create procedure [_Ensure_Text_Prop]
	@fmt varbinary(kcbMaxVarBin),
	@id int output
as
	declare @cb int

	set @cb = len(@fmt)
	select top 1 @id=[ID] from [_Text_Prop] where len([Fmt])=@cb and [Fmt]=@fmt
	if @id is null begin
		insert into [_Text_Prop] values(@fmt)
		set @id = @@identity
	end
	return @@error
go

-- Insert a reference into a sequence table. This shifts current values at or above @ord up
-- by one position.
create procedure [_InsertSeqRefBefore]
	@tbl varchar(kcchMaxName),
	@src int,
	@dst int,
	@ord int
as
	SpBegin()

	declare @sql varchar(kcchMaxSql)
	declare @sSrc varchar(kcchMaxInt)
	declare @sDst varchar(kcchMaxInt)
	declare @sOrd varchar(kcchMaxInt)

	set @sSrc = convert(varchar(kcchMaxInt), @src)
	set @sDst = convert(varchar(kcchMaxInt), @dst)
	set @sOrd = convert(varchar(kcchMaxInt), @ord)

	set @sql = 'update ' + @tbl + ' set [Ord] = [Ord] + 1 where [Ord] >= ' + @sOrd
	exec (@sql)
	SpCheck(@@error)

	set @sql = 'insert ' + @tbl + ' ([SrcID], [DstID], [Ord]) values(' +
		@sSrc + ',' + @sDst + ',' + @sOrd + ')'
	exec (@sql)
	SpCheck(@@error)

	SpEnd()
go


-- Set a reference in a sequence table. This doesn't shift existing values. If there is an
-- existing value at @ord, it is overwritten. Passing null for @dst causes the element to be
-- removed from the table.
create procedure [_SetSeqRef]
	@tbl varchar(kcchMaxName),
	@src int,
	@dst int,
	@ord int
as
	SpBegin()

	declare @sql varchar(kcchMaxSql)
	declare @sSrc varchar(kcchMaxInt)
	declare @sOrd varchar(kcchMaxInt)

	set @sSrc = convert(varchar(kcchMaxInt), @src)
	set @sOrd = convert(varchar(kcchMaxInt), @ord)

	if @dst is null begin
		set @sql = 'delete ' + @tbl + ' where [SrcID]=' + @sSrc + ' and [Ord]=' + @sOrd
		exec (@sql)
		SpCheck(@@error)
	end
	else begin
		declare @sDst varchar(kcchMaxInt)
		set @sDst = convert(varchar(kcchMaxInt), @dst)

		set @sql = 'update ' + @tbl + ' set [DstID]=' + @sDst +
			' where [SrcID]=' + @sSrc + ' and [Ord]=' + @sOrd
		exec (@sql)
		SpCheck(@@error)

		if @@rowcount = 0 begin
			set @sql = 'insert ' + @tbl + ' ([SrcID], [DstID], [Ord]) values(' +
				@sSrc + ',' + @sDst + ',' + @sOrd + ')'
			exec (@sql)
			SpCheck(@@error)
		end
	end

	SpEnd()
go


-- Delete a reference in a sequence table. This shifts current values above @ord down
-- by one position.
create procedure [_DelSeqRef]
	@tbl varchar(kcchMaxName),
	@src int,
	@ord int
as
	SpBegin()

	declare @sql varchar(kcchMaxSql)
	declare @sSrc varchar(kcchMaxInt)
	declare @sOrd varchar(kcchMaxInt)

	set @sSrc = convert(varchar(kcchMaxInt), @src)
	set @sOrd = convert(varchar(kcchMaxInt), @ord)

	set @sql = 'delete ' + @tbl + ' where [SrcID]=' + @sSrc + ' and [Ord]=' + @sOrd
	exec (@sql)
	SpCheck(@@error)

	set @sql = 'update ' + @tbl + ' set [Ord] = [Ord] - 1 where [Ord] > ' + @sOrd
	exec (@sql)
	SpCheck(@@error)

	SpEnd()
go


-- CmObject table.
#ifdef DEBUG
if exists ( select * from sysobjects where id = object_id('CmObject') )
begin
	print 'removing table CmObject'
	drop table CmObject
end
go
#endif
print 'creating table CmObject'
create table [CmObject] (
	[ID] int primary key clustered,
	[ObjID] uniqueidentifier unique not null,
	[Class] int not null,
	foreign key ([Class]) references [_Meta_Class] ([ID])
)
go

-- Create_CmObject_Core stored procedure. If @oid is null this creates the guid.
-- If @id is null, this uses the one more than the max of the current values.
-- This returns the resulting id.
if exists ( select * from sysobjects where id = object_id('Create_CmObject_Core') )
begin
	print 'removing proc Create_CmObject_Core'
	drop proc Create_CmObject_Core
end
go
print 'creating proc Create_CmObject_Core'
go
create procedure [Create_CmObject_Core]
	@id int output,
	@oid uniqueidentifier output,
	@idCls int
as
	if @oid is null set @oid = newid()
	if @id is null begin
		set @id = (select max([ID]) + 1 from [CmObject])
		if @id is null set @id = 1
	end
	insert into [CmObject] ([ID], [ObjID], [Class]) values(@id, @oid, @idCls)
	return @@error
go


if exists ( select * from sysobjects where id = object_id('cmObject_View') )
begin
	print 'removing proc cmObject_View'
	drop view cmObject_View
end
go
print 'creating view CmObject_View'
go
create view [CmObject_View] as select [CmObject].* from [CmObject]
go


#define PHASE1 1
#include "Objects.sqi"
#undef PHASE1

#define PHASE2 1
#include "Objects.sqi"
#undef PHASE2
