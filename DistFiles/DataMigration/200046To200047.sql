-- update database from version 200046 to 200047
BEGIN TRANSACTION  --( will be rolled back if wrong version#

---------------------------------------------------------------------
-- Drop ReversalIndexEntry.Name
---------------------------------------------------------------------

DELETE FROM Field$ WHERE Id = 5053001
GO

---------------------------------------------------------------------
-- Rebuild CreateObject_* stored procedures
---------------------------------------------------------------------

--( CreateObject_* stored procedures were not being updated when a
--( field was added or dropped. Rebuild the whole lot of them here.
--( (The code is from LangProjSP.sql.)

begin
	print 'creating CreateObject_ procedures...'

	declare @sClassName sysname, @clid int

	declare class_cur cursor local fast_forward for
	select	[Name], [Id]
	from	[Class$]
	where	[Abstract] = 0

	-- loop through each non-abstract class and build an ObjectCreate_ procedure
	open class_cur
	fetch class_cur into @sClassName, @clid
	while @@fetch_status = 0 begin
		exec DefineCreateProc$ @clid
		fetch class_cur into @sClassName, @clid
	end

	close class_cur
	deallocate class_cur
end
go

---------------------------------------------------------------------
-- Add a call to DefineCreateProc$ from TR_Field$_UpdateModel_InsLast
---------------------------------------------------------------------

IF OBJECT_ID('TR_Field$_UpdateModel_InsLast') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_Field$_UpdateModel_InsLast'
	DROP TRIGGER TR_Field$_UpdateModel_InsLast
END
GO
PRINT 'creating trigger TR_Field$_UpdateModel_InsLast'
GO

CREATE TRIGGER TR_Field$_UpdateModel_InsLast ON Field$ FOR INSERT
AS
	DECLARE
		@nErr INT,
		@nClassid INT,
		@nAbstract INT

	SELECT @nClassId = Class FROM inserted
	SELECT @nAbstract = Abstract FROM Class$ WHERE Id = @nClassId

	--( Update the view

	EXEC @nErr = UpdateClassView$ @nClassId, 1
	IF @nErr <> 0 GOTO LFail

	--( Rebuild CreateObject_*

	IF @nAbstract != 1 BEGIN
		EXEC @nErr = DefineCreateProc$ @nClassId
		IF @nErr <> 0 GOTO LFail
	END

	RETURN

LFail:
	ROLLBACK TRANSACTION
	RETURN
GO

EXEC sp_settriggerorder 'TR_Field$_UpdateModel_InsLast', 'last', 'INSERT'
GO

---------------------------------------------------------------------
-- Add a call to DefineCreateProc$ from TR_Field$_UpdateModel_Del
---------------------------------------------------------------------

if object_id('TR_Field$_UpdateModel_Del') is not null begin
	print 'removing trigger TR_Field$_UpdateModel_Del'
	drop trigger TR_Field$_UpdateModel_Del
end
go
print 'creating trigger TR_Field$_UpdateModel_Del'
go
create trigger TR_Field$_UpdateModel_Del on Field$ for delete
as
	declare @Clid INT
	declare @DstCls INT
	declare @sName VARCHAR(100)
	declare @sClass VARCHAR(100)
	declare @sFlid VARCHAR(20)
	declare @Type INT
	DECLARE @nAbstract INT

	declare @Err INT
	declare @fIsNocountOn INT
	declare @sql VARCHAR(1000)

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- get the first custom field to process
	Select @sFlid= min([id]) from deleted

	-- loop through all of the custom fields to be deleted
	while @sFlid is not null begin

		-- get deleted fields
		select 	@Type = [Type], @Clid = [Class], @sName = [Name], @DstCls = [DstCls]
		from	deleted
		where	[Id] = @sFlid

		-- get class name
		select 	@sClass = [Name], @nAbstract = Abstract  from class$  where [Id] = @Clid

		if @type IN (14,16,18,20) begin
			-- Remove any data stored for this multilingual custom field.
			declare @sTable VARCHAR(20)
			set @sTable = case @type
				when 14 then 'MultiStr$'
				when 16 then 'MultiTxt$'
				when 18 then 'MultiBigStr$'
				when 20 then 'MultiBigTxt$'
				end
			IF @type != 16  -- MultiTxt$ data will be deleted when the table is dropped
				set @sql = 'DELETE FROM [' + @sTable + '] WHERE [Flid] = ' + @sFlid
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail

			-- Remove the view created for this multilingual custom field.
			IF @type != 16
				set @sql = 'DROP VIEW [' + @sClass + '_' + @sName + ']'
			ELSE
				SET @sql = 'DROP TABLE [' + @sClass + '_' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail
		end
		else if @type IN (23,25,27) begin
			-- Remove the view created for this custom OwningAtom/Collection/Sequence field.
			set @sql = 'DROP VIEW [' + @sClass + '_' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail
			-- Check for any objects stored for this custom OwningAtom/Collection/Sequence field.
			declare @DelId INT
			select @DelId = [Id] FROM CmObject (readuncommitted) WHERE [OwnFlid$] = @sFlid
			set @Err = @@error
			if @Err <> 0 goto LFail
			if @DelId is not null begin
				raiserror('TR_Field$_UpdateModel_Del: Unable to remove %s field until corresponding objects are deleted',
						16, 1, @sName)
				goto LFail
			end
		end
		else if @type IN (26,28) begin
			-- Remove the table created for this custom ReferenceCollection/Sequence field.
			set @sql = 'DROP TABLE [' + @sClass + '_' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail

			-- Remove the procedure that handles reference collections or sequences for
			-- the dropped table
			set @sql = N'
				IF OBJECT_ID(''ReplaceRefColl_' + @sClass +  '_' + @sName + ''') IS NOT NULL
					DROP PROCEDURE [ReplaceRefColl_' + @sClass + '_' + @sName + ']
				IF OBJECT_ID(''ReplaceRefSeq_' + @sClass +  '_' + @sName + ''') IS NOT NULL
					DROP PROCEDURE [ReplaceRefSeq_' + @sClass + '_' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail
		end
		else begin
			-- Remove the format column created if this was a custom String field.
			if @type in (13,17) begin
				set @sql = 'ALTER TABLE [' + @sClass + '] DROP COLUMN [' + @sName + '_Fmt]'
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			end
			-- Remove the constraint created if this was a custom ReferenceAtom field.
			-- Not necessary for CmObject : Foreign Key constraints are not created agains CmObject
			if @type = 24 begin
				declare @sTarget VARCHAR(100)
				select @sTarget = [Name] FROM [Class$] WHERE [Id] = @DstCls
				set @Err = @@error
				if @Err <> 0 goto LFail
				if @sTarget != 'CmObject' begin
					set @sql = 'ALTER TABLE [' + @sClass + '] DROP CONSTRAINT [' +
						'_FK_' + @sClass + '_' + @sName + ']'
					exec (@sql)
					set @Err = @@error
					if @Err <> 0 goto LFail
				end
			end
			-- Remove Default Constraint from Numeric or Date fields before dropping the column
			If @type in (1,2,3,4,5,8) begin
				select @sql = 'ALTER TABLE [' + @sClass + '] DROP CONSTRAINT [' + so.name + ']'
				from sysconstraints sc
					join sysobjects so on so.id = sc.constid and so.name like 'DF[_]%'
					join sysobjects so2 on so2.id = sc.id
					join syscolumns sco on sco.id = sc.id and sco.colid = sc.colid
				where so2.name = @sClass   -- Tablename
				and   sco.name = @sName    -- Fieldname
				and   so2.type = 'U'	   -- Userdefined table
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			end

			-- Remove the column created for this custom field.
			set @sql = 'ALTER TABLE [' + @sClass + '] DROP COLUMN [' + @sName + ']'
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail

			-- fix the view associated with this class.
			exec @Err = UpdateClassView$ @Clid, 1
			if @Err <> 0 goto LFail
		end

		--( Rebuild CreateObject_*

		IF @nAbstract != 1 BEGIN
			EXEC @Err = DefineCreateProc$ @Clid
			IF @Err <> 0 GOTO LFail
		END

		-- get the next custom field to process
		Select @sFlid= min([id]) from deleted  where [Id] > @sFlid

	end -- While loop

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return

LFail:
	rollback tran
	return
go

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200046
begin
	update Version$ set DbVer = 200047
	COMMIT TRANSACTION
	print 'database updated to version 200047'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200046 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
