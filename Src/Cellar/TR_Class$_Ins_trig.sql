/***********************************************************************************************
 * Trigger: TR_Class$_Ins
 *
 * Description:
 *	This trigger creates a table for each inserted row, fills in ClassPar$, and creates an
 *	update trigger on the newly created table that maintains the CmObject UpdDttm column
 *	(this effectively updates the timestamp column that is used for optimistic locking)
 *
 * Type: 	Insert
 * Table:	Class$
 *
 * Notes:
 *	The Class$ table is only modified when the model is initial built. Thus, this trigger
 *	is largely responsible for creating the initial model - as new rows are added to Class$
 *	this trigger builds the corresponding tables for each class.
 **********************************************************************************************/
if object_id('TR_Class$_Ins') is not null begin
	print 'removing trigger TR_Class$_Ins'
	drop trigger [TR_Class$_Ins]
end
go
print 'creating trigger TR_Class$_Ins'
go
create trigger [TR_Class$_Ins] on [Class$] for insert
as
	declare @clid int, @clidBase int, @depth int, @clidT int, @clidBaseT int
	declare @sName sysname, @sBase sysname
	declare @Abstract bit
	declare @sql varchar(4000)
	declare @fIsNocountOn int
	declare @Err int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	select	top 1
		@clid = [Id],
		@clidBase = [Base],
		@sName = [Name],
		@Abstract = [Abstract]
	from	inserted
	order by [Id]

	set @clidBaseT = @clidBase
	while @@rowcount > 0 begin
		-- Fill in ClassPar$
		insert into [ClassPar$] ([Src], [Dst], [Depth])
			values(@clid, @clid, 0)
		if @@error <> 0 goto LFail

		set @depth = 1
		set @clidT = @clid
		while @clidBaseT <> @clidT begin
			insert into [ClassPar$] ([Src], [Dst], [Depth])
				values(@clid, @clidBaseT, @depth)
			if @@error <> 0 goto LFail

			set @clidT = @clidBaseT
			select	@clidBaseT = [Base]
			from	[Class$]
			where	[Id] = @clidT
			if @@rowcount = 0 break

			set @depth = @depth + 1
		end

		-- Create Database Model from Class$ entries (CmObject table pre-created)
		If @sName <> 'CmObject' Begin

			-- Create Table
			select	@sBase = [Name]
			from	[Class$]
			where	[id] = @clidBase

			set @sql = 'create table [' + @sName + '] ([id] int constraint [_PK_'
					+ @sName + '] primary key clustered'

			  -- CmObject foreign keys are handled through a trigger
			if @sBase <> 'CmObject' set @sql = @sql +  ', constraint [_FK_'
					+ @sName + '_id] foreign key ([Id]) references [' + @sBase + '] ([Id])'

			set @sql = @sql + ')'
			exec (@sql)
			if @Err <> 0 Begin
				raiserror('TR_Class$_Ins: SQL Error %d; Failed to Create Table %s', 16, 1, @Err, @sName)
				goto LFail
			end

			-- Create Trigger
			set @sql = 'create trigger [TR_' + @sName + '_TStmp] on [' + @sName + '] for update' + char(13) +
				'as' + char(13) +
				'update	CmObject' + char(13) +
				'set 	UpdDttm = getdate()' + char(13) +
				'from 	CmObject co join inserted i on co.[Id] = i.[Id]'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 Begin
				raiserror('TR_Class$_Ins: SQL Error %d; Failed to Create Trigger for %s',16,1,@Err,@sName)
				goto LFail
			end
		end

		set @clidT = @clid
		select top 1
			@clid = [Id],
			@clidBase = [Base],
			@sName = [Name]
		from	inserted
		where	[Id] > @clidT
		order by [Id]
	end

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	return

LFail:
	rollback tran
	raiserror('TR_Class$_Ins Failed on class %s ', 16,1, @sName)
go
