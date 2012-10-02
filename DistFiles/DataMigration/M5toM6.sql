/*===================================================================
** M5toM6.sql
**
** Description: Migrate data from M5 to M6
** Responsibility: S. A. Miller
** Reviewed: Not yet
**
** Notes:
**
** This program is in several sections:
**
**	1) Data structure changes
**		-- Create tables
**		-- Add columns
**		-- Change column names
**	2) Stored procedure, Trigger changes, & function changes
**	3) Data Updates
**	4) OCM Changes
**
** Much of the following code is taken from other programs. The code
** for data structure changes comes from the generated file
** NewLangProj.sql. The code for stored procedures and triggers come
** from FwCore.sql. Note that constants have been substituted with
** numeric values, just as the live triggers have numeric values.
**
** Stored procedure changes are accomplished simply by dropping the
** old stored proc, and creating the new one.
**
** It is tempting to use existing SQL scripts, rather than copying code.
** however they are not distributed, and FwCore.sql would have to be
** split. This may be an option in the future, rather than copying
** thousands of lines of code and getting code bloat.
**
** Most of the logic to create the appropriate objects, tables,
** columns, constraints, etc. is in the triggers of Class$ and
** Field$.
**=================================================================*/

--( Local variables get wiped out as soon as a GO is issued. (Ack,
--( pooey.) The temp table is a work around.

CREATE TABLE #tblIsNoCountOn (iIsOn INT)
-- SQL SERVER COMPLAINS (AT LEAST IN THE PROFILER) IF WE DON'T DO THIS GO!
GO
DECLARE	@fIsNocountOn INT	--( lost after the first GO
SET @fIsNocountOn = @@options & 512
INSERT INTO #tblIsNoCountOn VALUES (@fIsNocountOn)
IF @fIsNocountOn = 0 SET NOCOUNT ON
GO

/*************************************************
 * Fix a defective trigger for Field$.  This is
 * copied from a Version 1 (aka M6) database.
 *************************************************/
ALTER  trigger TR_Field$_UpdateModel_Del on Field$ for delete
as
	declare @Clid INT
	declare @DstCls INT
	declare @sName VARCHAR(100)
	declare @sClass VARCHAR(100)
	declare @sFlid VARCHAR(20)
	declare @Type INT

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
		select 	@sClass = [Name]  from class$  where [Id] = @Clid


		if @type IN (14,16,18,20) begin
			-- Remove any data stored for this multilingual custom field.
			declare @sTable VARCHAR(20)
			set @sTable = case @type
				when 14 then 'MultiStr$'
				when 16 then 'MultiTxt$'
				when 18 then 'MultiBigStr$'
				when 20 then 'MultiBigTxt$'
				end
			set @sql = 'DELETE FROM [' + @sTable + '] WHERE [Flid] = ' + @sFlid
			exec (@sql)
			set @Err = @@error
			if @Err <> 0 goto LFail
			-- Remove the view created for this multilingual custom field.
			set @sql = 'DROP VIEW [' + @sClass + '_' + @sName + ']'
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

			-- Remove the procedure that handles reference sequences for the dropped table
			set @sql = 'DROP PROCEDURE [ReplaceRefSeq_' + @sClass + '_' + @sName + ']'
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
			-- Remove Default Constraint from Numeric fields before dropping the column
			If @type in (1,2,3,4,8) begin
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

		-- get the next custom field to process
		Select @sFlid= min([id]) from deleted  where [Id] > @sFlid

	end -- While loop

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return

LFail:
	rollback tran
	return

GO

/*************************
** Utility Stored Procs **
*************************/

--( This would go in functions, but the following table structure
--( updates call this stored proc.

--== DefineCreateProc$ ==--

--( From FwCore.sql

if object_id('[DefineCreateProc$]') is not null begin
	print 'removing proc DefineCreateProc$'
	drop proc [DefineCreateProc$]
end
go
print 'creating proc DefineCreateProc$'
go

create proc [DefineCreateProc$]
	@clid int
as
	declare @Err int, @fIsNocountOn int
	declare @sDynSQL1 nvarchar(4000), @sDynSQL2 nvarchar(4000), @sDynSQL3 nvarchar(4000),
		@sDynSQL4 nvarchar(4000), @sDynSQL5 nvarchar(4000), @sDynSQLParamList nvarchar(4000)
	declare @sValuesList nvarchar(1000)
	declare @fAbs tinyint, @sClass sysname, @sProcName sysname
	declare @sFieldName sysname, @nFieldType int, @flid int,
		@sFieldList nvarchar(4000), @sXMLTableDef nvarchar(4000)
	declare @sInheritClassName sysname, @nInheritClid int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- validate the class
	select	@fAbs = [Abstract],
		@sClass = [Name]
	from	[Class$]
	where	[Id] = @clid
	if @fAbs is null begin
		raiserror('Invalid class: clid=%d', 16, 1, @clid)
		set @Err = 50001
		goto LCleanUp
	end
	if @fAbs <> 0 begin
		raiserror('Cannot create procedure for abstract class %s', 16, 1, @sClass)
		set @Err = 50002
		goto LCleanUp
	end

	set @sProcName = N'CreateObject_' + @sClass

	-- if an old procedure exists remove it
	if exists (
		select	*
		from	sysobjects
		where	type = 'P'
			and name = @sProcName
		) begin
		set @sDynSQL1 = N'drop proc '+@sProcName
		exec (@sDynSQL1)
	end

	--
	-- build the parameter list and table insert statements
	--

	set @sDynSQL3=N''
	set @sDynSQL4=N''
	set @sDynSQLParamList=N''

	-- create a cursor to loop through the base classes and class
	declare curClassInheritPath cursor local fast_forward for
	select	c.[Name], c.[Id]
	from	[ClassPar$] cp join [Class$] c on cp.[Dst] = c.[Id]
	where	cp.[Src] = @clid
		and cp.[Dst] > 0
	order by cp.[Depth] desc

	open curClassInheritPath
	fetch curClassInheritPath into @sInheritClassName, @nInheritClid

	while @@fetch_status = 0 begin

		set @sValuesList=''

		-- create a cursor to assemble the field list
		declare curFieldList cursor local fast_forward for
		select	[Name], [Type], [Id]
		from	[Field$]
		where	[Class] = @nInheritClid
			and [Name] <> 'Id'
			-- do not include MultiString type columns nor relationship, e.g. reference sequence,
			--	type columns because these are all stored in tables external to the actual
			--	class table
			and [Type] not in (23, 24, 25, 26, 27, 28)
		order by [Id]

		open curFieldList
		fetch curFieldList into @sFieldName, @nFieldType, @flid

		set @sFieldList = N''
		set @sXMLTableDef = N''
		while @@fetch_status = 0 begin
			if @nFieldType = 14 begin -- MultiStr$
				set @sDynSQLParamList = @sDynSQLParamList + char(9) +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_enc int = null' + N', ' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt nvarchar(4000) = null' + N', ' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt varbinary(8000) = null' + N', ' + char(13)
				set @sDynSQL3 = @sDynSQL3 + char(9) +
					N'if @' + @sInheritClassName + N'_' + @sFieldName + N'_txt is not null begin' + char(13) +
					char(9) + char(9) + N'insert into [MultiStr$] with (rowlock) ([Flid],[Obj],[Enc],[Txt],[Fmt]) ' + char(13) +
					char(9) + char(9) + N'values (' + convert(nvarchar(11), @flid)+ N',@ObjId,' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_enc' + N',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt' + N',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt)'
				set @sDynSQL3 = @sDynSQL3 +
N'
		set @Err = @@error
		if @Err <> 0 goto LCleanUp
	end
'
			end
			else if @nFieldType = 16 begin -- MultiTxt$
				set @sDynSQLParamList = @sDynSQLParamList + char(9) +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_enc int = null' + N', ' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt nvarchar(4000) = null' + N', ' + char(13)
				set @sDynSQL3 = @sDynSQL3 + char(9) +
					N'if @' + @sInheritClassName + N'_' + @sFieldName + N'_txt is not null begin' + char(13) +
					char(9) + char(9) + N'insert into [MultiTxt$] with (rowlock) ([Flid],[Obj],[Enc],[Txt])' + char(13) +
					char(9) + char(9) + + N'values (' + convert(nvarchar(11), @flid)+ N',@ObjId,' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_enc' + ',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt)'
				set @sDynSQL3 = @sDynSQL3 +
N'
		set @Err = @@error
		if @Err <> 0 goto LCleanUp
	end
'
			end
			else if @nFieldType = 18 begin -- MultiBigStr$
				set @sDynSQLParamList = @sDynSQLParamList + char(9) +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_enc int = null' + N', ' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt ntext = null' + N', ' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt image = null' + N', ' + char(13)
				set @sDynSQL3 = @sDynSQL3 + char(9) +
					N'if @' + @sInheritClassName + N'_' + @sFieldName + N'_txt is not null begin' + char(13) +
					char(9) + char(9) + N'insert into [MultiBigStr$] with (rowlock) ([Flid],[Obj],[Enc],[Txt],[Fmt])' + char(13) +
					char(9) + char(9) + N'values (' + convert(nvarchar(11), @flid)+ N',@ObjId,' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_enc' + N',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt' + N',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt)'
				set @sDynSQL3 = @sDynSQL3 +
N'
		set @Err = @@error
		if @Err <> 0 goto LCleanUp
	end
'
			end
			else if @nFieldType = 20 begin -- MultiBigTxt$
				set @sDynSQLParamList = @sDynSQLParamList + char(9) +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_enc int = null' + N', ' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt ntext = null' + N', ' + char(13)
				set @sDynSQL3 = @sDynSQL3 + char(9) +
					N'if @' + @sInheritClassName + N'_' + @sFieldName + N'_txt is not null begin' + char(13) +
					char(9) + char(9) + N'insert into [MultiBigTxt$] with (rowlock) ([Flid],[Obj],[Enc],[Txt])' + char(13) +
					char(9) + char(9) + N'values (' + convert(nvarchar(11), @flid)+ N',@ObjId,' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_enc' + N',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt)'
				set @sDynSQL3 = @sDynSQL3 +
N'
		set @Err = @@error
		if @Err <> 0 goto LCleanUp
	end
'
			end
			else begin
				set @sDynSQLParamList = @sDynSQLParamList + char(9) + N'@' + @sInheritClassName + '_' + @sFieldName + ' ' +
					dbo.fnGetColumnDef$(@nFieldType) + N',' + char(13)
				set @sFieldList = @sFieldList + N',[' + @sFieldName + N']'

				if @sValuesList = '' set @sValuesList = N'@' + @sInheritClassName + N'_' + @sFieldName
				else set @sValuesList = @sValuesList + N', @' + @sInheritClassName + N'_' + @sFieldName

				if @nFieldType = 13 or @nFieldType = 17 begin -- String or BigString
					set @sDynSQLParamList = @sDynSQLParamList + char(9) + N'@' + @sInheritClassName + '_' + @sFieldName + '_fmt '
					if @nFieldType = 13 set @sDynSQLParamList = @sDynSQLParamList + 'varbinary(8000) = null,' + char(13)
					else if @nFieldType = 17 set @sDynSQLParamList = @sDynSQLParamList + 'image = null,' + char(13)

					set @sFieldList = @sFieldList + N',[' + @sFieldName + N'_fmt]'
					set @sValuesList = @sValuesList + N', @' + @sInheritClassName + '_' + @sFieldName + '_fmt'
				end

			end
			fetch curFieldList into @sFieldName, @nFieldType, @flid
		end

		close curFieldList
		deallocate curFieldList

		if @sFieldList <> N'' set @sDynSQL4 = @sDynSQL4 + char(13) + char(9) +
				N'insert into ['+@sInheritClassName+N'] ([Id]' + 	@sFieldList + N') ' + char(13) +
				char(9) + char(9) + N'values (@ObjId, ' + @sValuesList + N')'
		else set @sDynSQL4 = @sDynSQL4 + char(9) + N'insert into ['+@sInheritClassName+N'] with (rowlock) ([Id]) values(@ObjId)'
		set @sDynSQL4 = @sDynSQL4 + char(13) + char(9) + N'set @Err = @@error' + char(13) + char(9) + N'if @Err <> 0 goto LCleanUp' + char(13)

		fetch curClassInheritPath into @sInheritClassName, @nInheritClid
	end

	close curClassInheritPath
	deallocate curClassInheritPath

	--
	-- build the dynamic SQL strings
	--

		set @sDynSQLParamList =
N'
Create proc ['+@sProcName+N']' + char(13) + @sDynSQLParamList
	set @sDynSQL1 =
N'	@Owner int = null,
	@OwnFlid int = null,
	@StartObj int = null,
	@NewObjId int output,
	@NewObjGuid uniqueidentifier output,
	@fReturnTimestamp tinyint = 0,
	@NewObjTimestamp int = null output
as
	declare @fIsNocountOn int, @Err int, @nTrnCnt int, @sTranName sysname
	declare @OwnOrd int, @Type int, @ObjId int, @guid uniqueidentifier
	declare @DstClass int, @OwnerClass int, @OwnerFlidClass int

	set @nTrnCnt = null
	set @Type = null
	set @OwnOrd = null
	set @NewObjTimestamp = null

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- validate the new object''s owner arguments
	if @Owner is not null begin
		-- get the type of the @OwnFlid field and validate @OwnFlid as being a valid field
		select	@Type = [Type], @DstClass = [DstCls], @OwnerFlidClass = [Class]
		from	[Field$]
		where	[Id] = @OwnFlid
		if @@rowcount = 0 begin
			raiserror(''Owner field does not exist: OwnFlid=%d'', 16, 1, @OwnFlid)
			set @Err = 50001
			goto LCleanUp
		end
		if @Type not in (23, 25, 27) begin
			raiserror(''OwnFlid is not an owning relationship field: OwnFlid=%d Type=%d'', 16, 1, @Ownflid, @Type)
			set @Err = 50002
			goto LCleanUp
		end

		-- make sure the @OwnFlid field has a relationship with the ' + @sClass + N' class
		if @DstClass <> ' + convert(nvarchar(11), @clid) + N'  begin
			-- check the base classes
			if not exists (
				select	*
				from	[ClassPar$]
				where	[Src] = '+convert(nvarchar(11), @clid) + N' and [Dst] = @DstClass
			) begin
				raiserror(''OwnFlid does not relate to the ' + @sClass + N' class: OwnFlid=%d'', 16, 1, @OwnFlid)
				set @Err = 50003
				goto LCleanUp
			end
		end

		-- make sure that @OwnFlid is a field of the @Owner class
		select	@OwnerClass = [Class$]
		from	[CmObject] (readuncommitted)
		where	[Id] = @Owner
		if @@rowcount = 0 begin
			raiserror(''Owner object does not exist: Owner=%d'', 16, 1, @Owner)
			set @Err = 50004
			goto LCleanUp
		end
		if @OwnerClass <> @OwnerFlidClass begin
			-- check the base classes
			if not exists (
				select	*
				from	[ClassPar$]
				where	[Src] = @ownerClass and [Dst] = @OwnerFlidClass
			) begin
				raiserror(''OwnFlid is not a field of the owner class: Owner=%d, OwnerClass=%d, OwnFlid=%d'', 16, 1, @Owner, @OwnerClass, @OwnFlid)
				set @Err = 50005
				goto LCleanUp
			end
		end
	end

	-- determine if a transaction already exists; if one does then create a savepoint, otherwise create a
	--	transaction
	set @nTrnCnt = @@trancount
	set @sTranName = '''+@sProcName+N'_tr'' + convert(varchar(2), @@nestlevel)
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
'
	set @sDynSQL2 =
N'
	-- determine if the object is being added to an owning sequence
	if @Type = 27 begin

		-- determine if the objects should be added to the end of the list
		if @StartObj is null begin
			select	@ownOrd = coalesce(max([OwnOrd$])+1, 1)
			from	[CmObject] with (serializable)
			where	[Owner$] = @Owner
				and [OwnFlid$] = @OwnFlid
		end
		else begin
			-- get the ordinal value of the object that is located where the new object is to be inserted
			select	@OwnOrd = [OwnOrd$]
			from	[CmObject] with (repeatableread)
			where	[Id] = @StartObj
			if @OwnOrd is null begin
				raiserror(''The start object does not exist in the owning sequence: Owner=%d, OwnFlid=%d, StartObj=%d'', 16, 1, @Owner, @OwnFlid, @StartObj)
				set @Err = 50006
				goto LCleanUp
			end

			-- increment the ordinal value(s) of the object(s) in the sequence that occur at or after
			--	the new object(s)
			update	[CmObject] with (serializable)
			set	[OwnOrd$] = [OwnOrd$] + 1
			where	[Owner$] = @Owner
				and [OwnFlid$] = @OwnFlid
				and [OwnOrd$] >= @OwnOrd
		end
	end
	-- determine if the object is being added to an atomic owning relationship
	else if @Type = 23 begin
		-- make sure there isn''t already an object owned by @Owner
		if exists (
			select	*
			from	[CmObject] with (readuncommitted)
			where	[Owner$] = @Owner and [OwnFlid$] = @OwnFlid
			) begin
			raiserror(''An object is already owned by the atomic relationship: Owner=%d, OwnFlid=%d'', 16, 1, @Owner, @OwnFlid)
			set @Err = 50007
			goto LCleanUp
		end
	end

	set @guid = newid()
	insert into [CmObject] with (rowlock) ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
		values (@guid, '+convert(nvarchar(11), @clid)+N', @Owner, @OwnFlid, @OwnOrd)
	set @Err = @@error
	set @ObjId = @@identity
	if @Err <> 0 begin
		raiserror(''SQL Error %d: Unable to create the new object'', 16, 1, @Err)
		goto LCleanUp
	end

'
	set @sDynSQL5 =
N'

	-- set the output paramters
	set @NewObjId = @ObjId
	set @NewObjGuid = @guid
	if @fReturnTimestamp = 1 begin
		select	@NewObjTimestamp = [UpdStmp]
		from	[CmObject]
		where	[Id] = @NewObjId
	end

LCleanUp:

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- determine if a transaction or savepoint was created
	if @nTrnCnt is not null begin
		if @Err = 0 begin
			-- if a transaction was created within this procedure commit it
			if @nTrnCnt = 0 commit tran @sTranName
		end
		else begin
			rollback tran @sTranName
		end
	end

	return @Err
'

	--
	-- execute the dynamic SQL
	--
	exec (@sDynSQLParamList+@sDynSql1+@sDynSQL2+@sDynSQL3+@sDynSQL4+@sDynSQL5)
	set @Err = @@error
	if @Err <> 0 begin
		raiserror ('SQL Error %d: Unable to create procedure for class %s', 16, 1, @Err, @sClass)
		goto LCleanUp
	end

LCleanUp:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	return @Err

GO

--== UpdateClassView$ ==--

--( From FwCore.sql

if object_id('UpdateClassView$') is not null begin
	print 'removing proc UpdateClassView$'
	drop proc [UpdateClassView$]
end
go
print 'creating proc UpdateClassView$'
go

create proc [UpdateClassView$]
	@clid int,
	@fRebuildSubClassViews tinyint=0
as
	declare @sTable sysname, @sBaseTable sysname, @sSubClass sysname
	declare @sField sysname, @flid int, @nType int
	declare @sDynSql nvarchar(4000), @sViewtext nvarchar(4000)
	declare @Err int, @fIsNocountOn int, @nTrnCnt int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- validate the parameter
	select	@sTable = c.[Name], @sBaseTable = base.[Name]
	from	[Class$] c join [Class$] base on c.[Base] = base.[Id]
	where	c.[Id] = @clid
	if @sTable is null begin
		raiserror('Invalid class id %d', 16, 1, @clid)
		return 50001
	end
	if @sBaseTable is null begin
		raiserror('The Class$ table has been corrupted', 16, 1)
		return 50002
	end

	-- If this procedure was called within a transaction, then create a savepoint; otherwise
	--	create a transaction.
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin transaction UpdateClassView$_Tran
	else save transaction UpdateClassView$_Tran
	set @Err = @@error
	if @Err <> 0 goto LFail

	-- drop the existing view
	set @sDynSql = N'if object_id(''' + @sTable + N'_'') is not null drop view [' + @sTable + N'_]'
	exec (@sDynSql)
	set @Err = @@error
	if @Err <> 0 goto LFail

	set @sDynsql = N'create view [' + @sTable + '_]' + char(13) +
		N'as' + char(13) + N'select [' + @sBaseTable + N'_].*'

	declare fld_cur cursor local static forward_only read_only for
	select	[Id], [Name], [Type]
	from	[Field$]
	where	[Class] = @clid
		and ([Type] <= 9 or [Type] in (13,15,17,19,24) )
	order by [Id]

	open fld_cur
	fetch fld_cur into @flid, @sField, @nType
	while @@fetch_status = 0 begin
		set @sDynSql = @sDynSql + N',[' + @sTable + N'].[' + @sField + N']'

		-- check for strings, which have an additional format column
		if @nType = 13 or @nType = 17 set @sDynSql = @sDynSql + N',[' + @sTable + N'].[' + @sField + N'_Fmt]'

		fetch fld_cur into @flid, @sField, @nType
	end
	close fld_cur
	deallocate fld_cur

	set @sDynSql = @sDynSql + char(13) + N'from [' + @sBaseTable + N'_] join [' + @sTable + N'] on [' +
		@sBaseTable + N'_].[Id] = [' + @sTable + N'].[Id]'
	exec (@sDynSql)
	set @Err = @@error
	if @Err <> 0 goto LFail

	if @fRebuildSubClassViews = 1 begin

		-- Refresh all the views that depend on this class. This is necessary because views
		--	based on the * operator are not automatically updated when tables are altered
		declare SubClass_cur cursor local static forward_only read_only for
		select	c.[Name]
		from	[ClassPar$] cp join [Class$] c on cp.[Src] = c.[Id]
		where	[Dst] = @clid
			and [Depth] > 0
		order by [Depth] desc

		open SubClass_cur
		fetch SubClass_cur into @sSubClass
		while @@fetch_status = 0 begin

			-- store the view contents
			select	@sViewText = [text]
			from	syscomments
			where	id = object_id(@sSubClass+'_')
			if @@rowcount <> 1 begin
				if @@rowcount = 0 begin
					raiserror('Could not find contents of view %s_', 16, 1, @sSubClass)
					set @Err = 50003
					goto LFail
				end
				-- if this happens a new approach will be necessary!!
				raiserror('View is too large to fit into an 8K page and could not be recreated', 16, 1)
				set @Err = 50004
				goto LFail
			end

			-- remove the current view
			set @sDynSql = N'drop view [' + @sSubClass + '_]'
			exec ( @sDynSql )
			set @Err = @@error
			if @Err <> 0 goto LFail

			-- rebuild the view
			exec ( @sViewtext )
			set @Err = @@error
			if @Err <> 0 goto LFail

			fetch SubClass_cur into @sSubClass
		end
		close SubClass_cur
		deallocate SubClass_cur
	end

	if @nTrnCnt = 0 commit tran UpdateClassView$_Tran

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	return 0

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	rollback tran UpdateClassView$_Tran
	return @Err

GO

/***************************************
** Create and Update Table Structures **
***************************************/

/*== Remove CmOverlayTag ==*/

--( This code is currently in FwDbVerCheck::UpdateDbAsNeeded in FwDbVerCheck.cpp

--== Create Sync$ ==--

create table [Sync$]
(
	[Id] int primary key clustered identity(1,1),
	[LpInfoId] uniqueidentifier null,
	[Msg] int null,
	[ObjId] int null,
	[ObjFlid] int null
)

/*==  CmSortSpec ==*/

/*-- Create table CmSortSpec --*/

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(31, 0, 0, 0, 'CmSortSpec')

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31001, 15, 31,
		null, 'Name',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31002, 6, 31,
		null, 'App',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31003, 2, 31,
		null, 'ClassId',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31004, 15, 31,
		null, 'PrimaryField',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31005, 2, 31,
		null, 'PrimaryEnc',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31006, 2, 31,
		null, 'PrimaryWs',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31007, 2, 31,
		null, 'PrimaryCollType',0,Null, 0, 127, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31008, 2, 31,
		null, 'PrimaryCollation',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31009, 1, 31,
		null, 'PrimaryReverse',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31010, 15, 31,
		null, 'SecondaryField',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31011, 2, 31,
		null, 'SecondaryEnc',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31012, 2, 31,
		null, 'SecondaryWs',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31013, 2, 31,
		null, 'SecondaryCollType',0,Null, 0, 127, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31014, 2, 31,
		null, 'SecondaryCollation',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31015, 1, 31,
		null, 'SecondaryReverse',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31016, 15, 31,
		null, 'TertiaryField',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31017, 2, 31,
		null, 'TertiaryEnc',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31018, 2, 31,
		null, 'TertiaryWs',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31019, 2, 31,
		null, 'TertiaryCollType',0,Null, 0, 127, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31020, 2, 31,
		null, 'TertiaryCollation',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31021, 1, 31,
		null, 'TertiaryReverse',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(31022, 1, 31,
		null, 'IncludeSubentries',0,Null, null, null, null)

/*-- Create view CmSortSpec_ --*/

EXEC UpdateClassView$ 31, 1

/*-- Create stored procedure CreateObject_CmSortSpec --*/

EXEC DefineCreateProc$ 31

/*== Create LexSubentry and Alter LexMajorEntry ==*/

--( Migration must be done in this order:
--(
--(	1. Table LexSubentry must be created
--(	2. Move sub entry data from LexMajorEntry to LexSubentry
--(	3. Create LexSubEntry_
--(
--( Sometime after the sub entry data has been moved, the LexMajorEntry
--( columns can be dropped.

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(5007, 5, 5008, 0, 'LexSubentry')

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5007001, 27, 5007,
		5003, 'MainEntriesOrSenses',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5007002, 24, 5007,
		5021, 'SubentryType',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5007003, 14, 5007,
		null, 'LiteralMeaning',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5007004, 1, 5007,
		null, 'IsBodyWithHeadword',0,Null, null, null, null)
GO

--( Create view LexSubEnry_ must be done later. First, some data has to be moved.

/*-- Create stored procedure CreateObject_LexSubentry --*/

EXEC DefineCreateProc$ 5007
GO

/*-- Move LexMajorEntry Data to LexSubentry --*/

/* We're not supporting Lex stuff yet, and this is wrong, anyway.
INSERT INTO LexSubEntry ([Id], [SubentryType], [IsBodyWithHeadword])
SELECT [Id], [SubentryType], [IsBodyWithHeadword]
	FROM LexMajorEntry
	WHERE [SubEntryType] IS NOT NULL
GO */

/*-- Alter LexMajorEntry --*/

--( This is a dummy view, for the Field$ delete trigger, which calls
--( UpdateClassView$, which expects view LexSubEntry_ to be created
--( already. The proper view can't be created yet because of an error with
--( duplicate SubentryType column names.

CREATE VIEW LexSubentry_ AS SELECT * FROM LexSubentry
GO

DELETE FROM Field$ WHERE [Id] = 5008005	--( SubentryType
DELETE FROM Field$ WHERE [Id] = 5008006	--( IsBodyWithHeadword
GO

/*-- Create view LexSubEntry_ --*/

EXEC UpdateClassView$ 5007, 1

/*== FsFeatureDefn ==*/

--( Columns Name, Abbreviation, Description, and Default exist already

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(2007005, 16, 2007,
		null, 'GlossAbbreviation',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(2007006, 16, 2007,
		null, 'RightGlossSeparator',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(2007007, 1, 2007,
		null, 'ShowInGloss',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(2007008, 1, 2007,
		null, 'DisplayToRightOfValues',0,Null, null, null, null)
GO

/*== FsSymbolicFeatureValue ==*/

--( Columns Name, Abbreviation, and Description exist already

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(2017004, 16, 2017,
		null, 'GlossAbbreviation',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(2017005, 16, 2017,
		null, 'RightGlossSeparator',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(2017006, 1, 2017,
		null, 'ShowInGloss',0,Null, null, null, null)
GO

/*== LanguageProject ==*/

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(6001042, 15, 6001,
		null, 'ExtLinkRootDir',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(6001043, 25, 6001,
		31, 'SortSpecs',0,Null, null, null, null)
GO

/*== LexEntry   ==*/

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5002011, 27, 5002,
		5016, 'Senses',0,Null, null, null, null)
GO

/*== CmProject ==*/

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(1004, 5, 1,
		null, 'DateModified',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(1005, 18, 1,
		null, 'Description',0,Null, null, null, null)

GO

/*== Create LgCollation ==*/

/*-- Create Table LgCollation --*/

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(30, 0, 0, 0, 'LgCollation')

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(30001, 16, 30,
		null, 'Name',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(30002, 2, 30,
		null, 'WinLCID',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(30003, 15, 30,
		null, 'WinCollation',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(30004, 15, 30,
		null, 'IcuResourceName',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(30005, 19, 30,
		null, 'IcuResourceText',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(30006, 2, 30,
		null, 'Code',0,Null, null, null, null)
GO

/*-- Create view LgCollation_ --*/

EXEC UpdateClassView$ 30, 1

/*-- Create CreateObject_LgCollation --*/

EXEC DefineCreateProc$ 30
GO

/*== LgWritingSystem ==*/

--( Table LgCollations must be created first, for the Collations column

--( Columns Name, Description, Code, Abbr, Renderer, RendererInit exist already

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(25007, 15, 25,
		null, 'DefaultMonospace',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(25008, 15, 25,
		null, 'DefaultSansSerif',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(25009, 15, 25,
		null, 'DefaultSerif',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(25010, 15, 25,
		null, 'FontVariation',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(25011, 15, 25,
		null, 'KeyboardType',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(25012, 2, 25,
		null, 'LangId',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(25013, 15, 25,
		null, 'RendererType',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(25014, 1, 25,
		null, 'RightToLeft',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(25015, 2, 25,
		null, 'Locale',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(25016, 27, 25,
		30, 'Collations',0,Null, null, null, null)
GO

/*== ScrBookRef ==*/

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3004001, 16, 3004,
		null, 'BookName',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3004003, 16, 3004,
		null, 'BookAbbrev',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3004004, 16, 3004,
		null, 'BookNameAlt',0,Null, null, null, null)
GO

/***************************
** Renamed/Moved Columns  **
***************************/

/*== MoStemAllomorph PhoneEnviron -> PhoneEnv ==*/

--( Store off existing data
CREATE TABLE #tmpMoStemAllomorph_PhoneEnviron ([Src] INT, [Dst] INT)
-- SQL SERVER COMPLAINS (AT LEAST IN THE PROFILER) IF WE DON'T DO THIS GO!
GO
INSERT INTO #tmpMoStemAllomorph_PhoneEnviron
	SELECT * FROM MoStemAllomorph_PhoneEnviron

--( Out with the old, in with the new
DELETE FROM MoStemAllomorph_PhoneEnviron
GO

--( This is a dummy proc for the Field$ delete trigger
CREATE PROCEDURE ReplaceRefSeq_MoStemAllomorph_PhoneEnviron AS
	 --( Dummy proc
GO

DELETE FROM Field$ WHERE [Id] = 5045002

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5045002, 26, 5045,
		5097, 'PhoneEnv',0,Null, null, null, null)

--( Restore data
INSERT INTO MoStemAllomorph_PhoneEnv
	SELECT * FROM #tmpMoStemAllomorph_PhoneEnviron
-- SQL SERVER COMPLAINS (AT LEAST IN THE PROFILER) IF WE DON'T DO THIS GO!
GO
DROP TABLE #tmpMoStemAllomorph_PhoneEnviron

GO

---------------------------------------------------------------------

/**************************************************
** Stored Procedure, Trigger Changes, & Function **
**************************************************/

/*== TR_CmObject$_RI_Del ==*/

--( From FwCore.sql

if object_id('TR_CmObject$_RI_Del') is not null begin
	print 'removing trigger TR_CmObject$_RI_Del'
	drop trigger TR_CmObject$_RI_Del
end
go
print 'creating trigger TR_CmObject$_RI_Del'
go
create trigger TR_CmObject$_RI_Del on CmObject for delete
as
	declare @sDynSql nvarchar(4000)
	declare @iCurDelClsId int
	declare @fIsNocountOn int
	declare @uid uniqueidentifier

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- copy the deleted rows into a the ObjListTbl table - this is necessary since the logical "deleted"
	--	table is not in	the scope of the dynamic SQL
	set @uid = newid()
	insert into [ObjListTbl$] ([Uid], [ObjId], [Ord], [Class])
	select	@uid, [Id], coalesce([OwnOrd$], -1), [Class$]
	from	deleted
	if @@error <> 0 begin
		raiserror('TR_CmObject$_RI_Del: Unable to copy the logical DELETED table to the ObjListTbl table', 16, 1)
		goto LFail
	end

	-- get the first class to process
	select	@iCurDelClsId = min([Class])
	from	[ObjListTbl$] (serializable)
	where	[uid] = @uid
	if @@error <> 0 begin
		raiserror('TR_CmObject$_RI_Del: Unable to get the first deleted class', 16, 1)
		goto LFail
	end

	-- loop through all of the classes in the deleted logical table
	while @iCurDelClsId is not null begin
		select	@sDynSql =
			'if exists ( ' +
				'select * ' +
				'from ObjListTbl$ del (readuncommitted) join ' + [name] + ' c ' +
					'on del.[ObjId] = c.[Id] and del.[Class] = ' + convert(nvarchar(11), @iCurDelClsId) +
				'where del.[uid] = ''' + convert(varchar(255), @uid) + ''' ' +
			') begin ' +
				'raiserror(''Delete in CmObject violated referential integrity with %s'', 16, 1, ''' + [name] + ''') ' +
				'exec CleanObjListTbl$ ''' + convert(varchar(255), @uid) + ''' ' +
				'rollback tran ' +
			'end '
		from	[Class$]
		where	[Id] = @iCurDelClsId

		exec (@sDynSql)
		if @@error <> 0 begin
			raiserror('TR_CmObject$_RI_Del: Unable to execute dynamic SQL', 16, 1)
			goto LFail
		end

		-- get the next class to process
		select	@iCurDelClsId = min([Class])
		from	[ObjListTbl$] (serializable)
		where	[Class] > @iCurDelClsId
			and [uid] = @uid
	end

	exec CleanObjListTbl$ @uid

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return

LFail:
	-- because the transaction is ROLLBACKed the rows in the ObjListTbl$ will be removed
	rollback tran
	return
go

/*== TR_CmObject$_UpdDttm_Del ==*/

--( From FwCore.sql

if object_id('TR_CmObject$_UpdDttm_Del') is not null begin
	print 'removing trigger TR_CmObject$_UpdDttm_Del'
	drop trigger TR_CmObject$_UpdDttm_Del
end
go
print 'creating trigger TR_CmObject$_UpdDttm_Del'
go
create trigger TR_CmObject$_UpdDttm_Del on CmObject for delete
as
	declare @fIsNocountOn int
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	update CmObject set UpdDttm = getdate()
		from CmObject co JOIN deleted del on co.[id] = del.[owner$]

	if @@error <> 0 begin
		raiserror('TR_CmObject$_UpdDttm_Del: Unable to update owning object', 16, 1)
		goto LFail
	end

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return

LFail:
	-- because the transaction is ROLLBACKed the rows in the ObjListTbl$ will be removed
	rollback tran
	return
go


/*== GetLinkedObjs$ ==*/

--( From FwCore.sql

if object_id('GetLinkedObjs$') is not null begin
	print 'removing proc GetLinkedObjs$'
	drop proc [GetLinkedObjs$]
end
go
print 'creating proc GetLinkedObjs$'
go

create proc [GetLinkedObjs$]
	@ObjId int=null,
	@hXMLDocObjList int=null,
	@grfcpt int=528482304,
	@fBaseClasses bit=0,
	@fSubClasses bit=0,
	@fRecurse bit=1,
	@nRefDirection smallint=0,
	@riid int=null,
	@fCalcOrdKey bit=1
as
	declare @Err int, @nRowCnt int
	declare	@sQry nvarchar(1000), @sUid nvarchar(50)
	declare	@nObjId int, @nObjClass int, @nInheritDepth int, @nOwnerDepth int, @nClass int, @nField int,
		@nRelOrder int, @nType int, @nDirection int, @sClass sysname, @sField sysname,
		@sOrderField sysname, @sOrdKey varchar(250)
	declare	@fIsNocountOn int

	set @Err = 0

	CREATE TABLE [#OwnedObjsInfo$](
		[ObjId]		INT NOT NULL,
		[ObjClass]	INT NULL,
		[InheritDepth]	INT NULL DEFAULT(0),
		[OwnerDepth]	INT NULL DEFAULT(0),
		[RelObjId]	INT NULL,
		[RelObjClass]	INT NULL,
		[RelObjField]	INT NULL,
		[RelOrder]	INT NULL,
		[RelType]	INT NULL,
		[OrdKey]	VARBINARY(250) NULL DEFAULT(0))

	CREATE NONCLUSTERED INDEX #OwnedObjsInfoObjId ON #OwnedObjsInfo$ ([ObjId])

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- if null was specified as the mask assume that all objects are desired
	if @grfcpt is null set @grfcpt = 528482304

	-- make sure objects were specified either in the XML or through the @ObjId parameter
	if ( @ObjId is null and @hXMLDocObjList is null ) or ( @ObjId is not null and @hXMLDocObjList is not null ) goto LFail

	-- get the owned objects
	IF (176160768) & @grfcpt > 0 --( mask = owned obects
		INSERT INTO [#OwnedObjsInfo$]
			SELECT * FROM dbo.fnGetOwnedObjects$(
				@ObjId,
				@hXMLDocObjList,
				@grfcpt,
				@fBaseClasses,
				@fSubClasses,
				@fRecurse,
				@riid,
				@fCalcOrdKey)
	ELSE --( mask = referenced items or all: get all owned objects
		INSERT INTO [#OwnedObjsInfo$]
			SELECT * FROM dbo.fnGetOwnedObjects$(
				@ObjId,
				@hXMLDocObjList,
				528482304,
				@fBaseClasses,
				@fSubClasses,
				@fRecurse,
				@riid,
				@fCalcOrdKey)

	IF NOT (352321536) & @grfcpt > 0 --( mask = not referenced. In other words, mask is owned or all
		INSERT INTO [#ObjInfoTbl$]
			SELECT * FROM [#OwnedObjsInfo$]

	-- determine if any references should be included in the results
	if (352321536) & @grfcpt > 0 begin

		--
		-- get a list of all of the classes that reference each class associated with the specified object
		--	and a list of the classes that each associated class reference, then loop through them to
		--	get all of the objects that participate in the references
		--

		-- determine which reference direction should be included
		if @nRefDirection = 0 begin
			declare GetClassRefObj_cur cursor local static forward_only read_only for
			-- get the classes that reference (atomic, sequences, and collections) this class
			select 	oi.[ObjId], oi.[ObjClass], oi.[InheritDepth], oi.[OwnerDepth], f.[Type], c.[Name], c.[Id], f.[Name], f.[Id],
				1, -- references this class
				master.dbo.fn_varbintohexstr(oi.[OrdKey])
			from 	#OwnedObjsInfo$ oi
					join [Field$] f on f.[DstCls] = oi.[ObjClass]
						and ( 	( f.[type] = kcptReferenceAtom and @grfcpt & 352321536 = 352321536 )
							or ( f.[type] = kcptReferenceCollection and @grfcpt & 67108864 = 67108864 )
							or ( f.[type] = kcptReferenceSequence and @grfcpt & 268435456 = 268435456 )
						)
					join [Class$] c on f.[Class] = c.[Id]
--			where	(@riid is null or oi.[ObjClass] = @riid)
			union all
			-- get the classes that are referenced (atomic, sequences, and collections) by this class
			select	oi.[ObjId], oi.[ObjClass], oi.[InheritDepth], oi.[OwnerDepth], f.[Type], c.[Name], f.[DstCls], f.[Name], f.[Id],
				2, -- referenced by this class
				master.dbo.fn_varbintohexstr(oi.[OrdKey])
			from	#OwnedObjsInfo$ oi
					join [Class$] c on c.[Id] = oi.[ObjClass]
					join [Field$] f on f.[Class] = c.[Id]
						and ( 	( f.[type] = kcptReferenceAtom and @grfcpt & 352321536 = 352321536 )
							or ( f.[type] = kcptReferenceCollection and @grfcpt & 67108864 = 67108864 )
							or ( f.[type] = kcptReferenceSequence and @grfcpt & 268435456 = 268435456 )
						)
--			where	(@riid is null or f.[DstCls] = @riid)
			order by oi.[ObjId]
		end
		else if @nRefDirection = 1 begin
			declare GetClassRefObj_cur cursor local static forward_only read_only for
			-- get the classes that are referenced (atomic, sequences, and collections) by these classes
			select	oi.[ObjId], oi.[ObjClass], oi.[InheritDepth], oi.[OwnerDepth], f.[Type], c.[Name], f.[DstCls], f.[Name], f.[Id],
				2, -- referenced by this class
				master.dbo.fn_varbintohexstr(oi.[OrdKey])
			from	#OwnedObjsInfo$ oi
					join [Class$] c on c.[Id] = oi.[ObjClass]
					join [Field$] f on f.[Class] = c.[Id]
						and ( 	( f.[type] = kcptReferenceAtom and @grfcpt & 352321536 = 352321536 )
							or ( f.[type] = kcptReferenceCollection and @grfcpt & 67108864 = 67108864 )
							or ( f.[type] = kcptReferenceSequence and @grfcpt & 268435456 = 268435456 )
						)
--			where	(@riid is null or f.[DstCls] = @riid)
			order by oi.[ObjId]
		end
		else begin
			declare GetClassRefObj_cur cursor local static forward_only read_only for
			-- get the classes that reference (atomic, sequences, and collections) these classes
			-- do not include internal references between objects within the owning object hierarchy;
			--	this will be handled below
			select 	oi.[ObjId], oi.[ObjClass], oi.[InheritDepth], oi.[OwnerDepth], f.[Type], c.[Name], c.[Id], f.[Name], f.[Id],
				1, -- references this class
				master.dbo.fn_varbintohexstr(oi.[OrdKey])
			from 	#OwnedObjsInfo$ oi
					join [Field$] f on f.[DstCls] = oi.[ObjClass]
						and ( 	( f.[type] = kcptReferenceAtom and @grfcpt & 352321536 = 352321536 )
							or ( f.[type] = kcptReferenceCollection and @grfcpt & 67108864 = 67108864 )
							or ( f.[type] = kcptReferenceSequence and @grfcpt & 268435456 = 268435456 )
						)
					join [Class$] c on f.[Class] = c.[Id]
--			where	(@riid is null or oi.[ObjClass] = @riid)
			order by oi.[ObjId]
		end

		open GetClassRefObj_cur
		fetch GetClassRefObj_cur into @nObjId, @nObjClass, @nInheritDepth, @nOwnerDepth, @nType, @sClass, @nClass,
				@sField, @nField, @nDirection, @sOrdKey
		while @@fetch_status = 0 begin

			-- build the base part of the query
			set @sQry = 'insert into #ObjInfoTbl$ '+
					'(ObjId,ObjClass,InheritDepth,OwnerDepth,RelObjId,RelObjClass,RelObjField,RelOrder,RelType,OrdKey)' + char(13) +
					'select '

			-- determine if the reference is atomic
			if @nType = 24 begin
				-- determine if this class references an object's class within the object hierachy,
				--	and whether or not it should be included
				if @nDirection = 1 and (@nRefDirection = 0 or @nRefDirection = -1) begin
					set @sQry=@sQry+convert(nvarchar(11), @nObjId)+','+convert(nvarchar(11), @nObjClass)+','+
							convert(nvarchar(11), @nInheritDepth)+','+convert(nvarchar(11), @nOwnerDepth)+','+
							't.[Id],'+convert(nvarchar(11), @nClass)+','+convert(nvarchar(11), @nField)+','+
							'NULL,'+convert(nvarchar(11), @nType)+',convert(varbinary,'''+@sOrdKey+''') '+
						'from ['+@sClass+'] t (readuncommitted) '+
						'where ['+@sField+']='+convert(nvarchar(11),@nObjId)

					-- determine if only external references should be included - don't included
					--	references between objects within the owning hierarchy
					if @nRefDirection = -1 begin
						set @sQry = @sQry + 'and not exists (' +
								'select * from #OwnedObjsInfo$ oi ' +
								'where oi.[ObjId]=t.[Id] ' +
									'and oi.[RelType] not in (' +
									convert(nvarchar(11), 352321536)+',' +
									convert(nvarchar(11), 67108864)+',' +
									convert(nvarchar(11), 268435456)+'))'
					end
				end
				-- determine if this class is referenced by an object's class within the object hierachy,
				--	and whether or not it should be included
				else if @nDirection = 2 and (@nRefDirection = 0 or @nRefDirection = 1) begin
					set @sQry=@sQry+'['+@sField+'],'+
							convert(nvarchar(11), @nClass)+','+convert(nvarchar(11), @nInheritDepth)+','+
							convert(nvarchar(11), @nOwnerDepth)+','+convert(nvarchar(11), @nObjId)+','+
							convert(nvarchar(11), @nObjClass)+','+convert(nvarchar(11), @nField)+','+
							'NULL,'+convert(nvarchar(11), @nType)+',convert(varbinary,'''+@sOrdKey+''') '+
						'from ['+@sClass+'] (readuncommitted) ' +
						'where [id]='+convert(nvarchar(11),@nObjId)+' '+
							'and ['+@sField+'] is not null'
				end
			end
			else begin
				-- if the reference is ordered insert the order value, otherwise insert null
				if @nType = 28 set @sOrderField = '[Ord]'
				else set @sOrderField = 'NULL'

				-- determine if this class references an object's class and whether or not it should be
				--	included
				if @nDirection = 1 and (@nRefDirection = 0 or @nRefDirection = -1) begin
					set @sQry=@sQry+convert(nvarchar(11), @nObjId)+','+convert(nvarchar(11), @nObjClass)+','+
							convert(nvarchar(11), @nInheritDepth)+','+convert(nvarchar(11), @nOwnerDepth)+','+
							't.[Src],'+convert(nvarchar(11), @nClass)+','+convert(nvarchar(11), @nField)+','+
							@sOrderField+','+convert(nvarchar(11), @nType)+',convert(varbinary,'''+@sOrdKey+''') '+
						'from ['+@sClass+'_'+@sField+'] t (readuncommitted) '+
						'where t.[dst]='+convert(nvarchar(11),@nObjId)

					-- determine if only external references should be included - don't included
					--	references between objects within the owning hierarchy
					if @nRefDirection = -1 begin
						set @sQry = @sQry + 'and not exists (' +
								'select * from #OwnedObjsInfo$ oi ' +
								'where oi.[ObjId]=t.[Src] ' +
									'and oi.[RelType] not in (' +
									convert(nvarchar(11), 352321536)+',' +
									convert(nvarchar(11), 67108864)+',' +
									convert(nvarchar(11), 268435456)+'))'
					end
				end
				-- determine if this class is referenced by an object's class and whether or not it
				--	should be included
				else if @nDirection = 2 and (@nRefDirection = 0 or @nRefDirection = 1) begin
					set @sQry=@sQry+'[Dst],'+
							convert(nvarchar(11), @nClass)+','+convert(nvarchar(11), @nInheritDepth)+','+
							convert(nvarchar(11), @nOwnerDepth)+','+convert(nvarchar(11), @nObjId)+','+
							convert(nvarchar(11), @nObjClass)+','+convert(nvarchar(11), @nField)+','+
							@sOrderField+','+convert(nvarchar(11), @nType)+',convert(varbinary,'''+@sOrdKey+''') '+
						'from ['+@sClass+'_'+@sField+'] (readuncommitted) '+
						'where [src]='+convert(nvarchar(11),@nObjId)
				end
			end

			exec (@sQry)
			set @Err = @@error
			if @Err <> 0 begin
				raiserror ('GetLinkedObjects$: SQL Error %d; Error performing dynamic SQL.', 16, 1, @Err)

				close GetClassRefObj_cur
				deallocate GetClassRefObj_cur

				goto LFail
			end

			fetch GetClassRefObj_cur into @nObjId, @nObjClass, @nInheritDepth, @nOwnerDepth, @nType, @sClass,
					@nClass, @sField, @nField, @nDirection, @sOrdKey
		end

		close GetClassRefObj_cur
		deallocate GetClassRefObj_cur
	end

	-- if a class was specified remove the owned objects that are not of that type of class; these objects were
	--    necessary in order to get a list of all of the referenced and referencing objects that were potentially
	--    the type of specified class
	if @riid is not null begin
		delete	#ObjInfoTbl$
		where 	not exists (
				select	*
				from	[ClassPar$] cp
				where	cp.[Dst] = @riid
					and cp.[Src] = [ObjClass]
			)
		set @Err = @@error
		if @Err <> 0 begin
			raiserror ('GetLinkedObjects$: SQL Error %d; Unable to remove objects that are not the specified class %d.', 16, 1, @Err, @riid)
			goto LFail
		end
	end

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
go

/*== GetPossibilities ==*/

--( From FwCore.sql

if object_id('GetPossibilities') is not null begin
	print 'removing proc GetPossibilities'
	drop proc [GetPossibilities]
end
go
print 'creating proc GetPossibilities'
go
create proc [GetPossibilities]
	@ObjId int,
	@Enc int
as
	declare @uid uniqueidentifier,
			@retval int

	-- get all of the possibilities owned by the specified possibility list object
	declare @tblObjInfo table (
		[ObjId]		int		not null,
		[ObjClass]	int		null,
		[InheritDepth]	int		null	default(0),
		[OwnerDepth]	int		null	default(0),
		[RelObjId]	int		null,
		[RelObjClass]	int		null,
		[RelObjField]	int		null,
		[RelOrder]	int		null,
		[RelType]	int		null,
		[OrdKey]	varbinary(250)	null	default(0))

	insert into @tblObjInfo
		select * from fnGetOwnedObjects$(@ObjId, null, 176160768, 0, 0, 1, 7, 1)


	-- REVIEW: SteveMi. Is this command really necessary?

	select count(*) from @tblObjInfo

	if @Enc = 0xffffffff

		--( To avoid seeing stars, send a "magic" encoding of kencAnal.
		--( This will cause the query to return the first non-null string.
		--( Priority is given to encodings with the highest order.

		select
			o.ObjId,
			isnull((select top 1 txt
				from CmPossibility_Name cn (readuncommitted)
				left outer join LgEncoding le (readuncommitted) on le.[encoding] = cn.[enc]
				left outer join LanguageProject_AnalysisEncodings lpae (readuncommitted) on lpae.[dst] = le.[id]
				left outer join LanguageProject_CurrentAnalysisEncs lpcae (readuncommitted) on lpcae.[dst] = lpae.[dst]
				where cn.[Obj] = o.[objId]
				order by isnull(lpcae.[ord], 99999)), '***'),
			isnull((select top 1 txt
				from CmPossibility_Abbreviation ca (readuncommitted)
				left outer join LgEncoding le (readuncommitted) on le.[encoding] = ca.[enc]
				left outer join LanguageProject_AnalysisEncodings lpae (readuncommitted) on lpae.[dst] = le.[id]
				left outer join LanguageProject_CurrentAnalysisEncs lpcae (readuncommitted) on lpcae.[dst] = lpae.[dst]
				where ca.[Obj] = o.[objId]
				order by isnull(lpcae.[ord], 99999)), '***'),
			o.OrdKey, cp.ForeColor, cp.BackColor, cp.UnderColor, cp.UnderStyle
		from @tblObjInfo o
			left outer join CmPossibility cp (readuncommitted) on cp.[id] = o.[objId]
		order by o.OrdKey

	else if @Enc = 0xfffffffe

		--( To avoid seeing stars, send a "magic" encoding of kencVern.
		--( This will cause the query to return the first non-null string.
		--( Priority is given to encodings with the highest order.

		select
			o.ObjId,
			isnull((select top 1 txt
				from CmPossibility_Name cn (readuncommitted)
				left outer join LgEncoding le (readuncommitted) on le.[encoding] = cn.[enc]
				left outer join LanguageProject_VernacularEncodings lpve (readuncommitted) on lpve.[dst] = le.[id]
				left outer join LanguageProject_CurrentVernacularEncs lpcve (readuncommitted) on lpcve.[dst] = lpve.[dst]
				where cn.[Obj] = o.[objId]
				order by isnull(lpcve.[ord], 99999)), '***'),
			isnull((select top 1 txt
				from CmPossibility_Abbreviation ca (readuncommitted)
				left outer join LgEncoding le (readuncommitted) on le.[encoding] = ca.[enc]
				left outer join LanguageProject_VernacularEncodings lpve (readuncommitted) on lpve.[dst] = le.[id]
				left outer join LanguageProject_CurrentVernacularEncs lpcve (readuncommitted) on lpcve.[dst] = lpve.[dst]
				where ca.[Obj] = o.[objId]
				order by isnull(lpcve.[ord], 99999)), '***'),
			o.OrdKey, cp.ForeColor, cp.BackColor, cp.UnderColor, cp.UnderStyle
		from @tblObjInfo o
			left outer join CmPossibility cp (readuncommitted) on cp.[id] = o.[objId]
		order by o.OrdKey

	else
		select	o.ObjId, isnull(cn.txt, '***'), isnull(ca.txt, '***'), o.OrdKey,
			cp.ForeColor, cp.BackColor, cp.UnderColor, cp.UnderStyle
		from @tblObjInfo o
			left outer join [CmPossibility_Name] cn (readuncommitted)
				on cn.[Obj] = o.[ObjId] and cn.[Enc] = @Enc
			left outer join [CmPossibility_Abbreviation] ca (readuncommitted)
				on ca.[Obj] = o.[ObjId] and ca.[Enc] = @Enc
			left outer join CmPossibility cp (readuncommitted) on cp.[id] = o.[objId]
		order by o.OrdKey

	return @retval
go

/*== GetTagInfo$ ==*/

--( From FwCore.sql

if object_id('GetTagInfo$') is not null begin
	print 'removing procedure GetTagInfo$'
	drop proc [GetTagInfo$]
end
go
print 'creating proc GetTagInfo$'
go

create proc GetTagInfo$
	@iOwnerId int,
	@iEncoding int
as

	declare @fIsNocountOn int
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	--( if "magic" encoding is for analysis encodings
	if @iEncoding = 0xffffffff
		select
			[co].[Guid$],
			[opi].[Dst],
			isnull((select top 1 [ca].[txt]
				from CmPossibility_Abbreviation ca (readuncommitted)
				left outer join LgEncoding le (readuncommitted)
					on le.[encoding] = ca.[enc]
				left outer join LanguageProject_AnalysisEncodings lpae (readuncommitted)
					on lpae.[dst] = le.[id]
				left outer join LanguageProject_CurrentAnalysisEncs lpcae (readuncommitted)
					on lpcae.[dst] = lpae.[dst]
				where ca.[Obj] = [opi].[Dst]
				order by isnull(lpcae.[ord], 99999)), '***'),
			isnull((select top 1 [cn].[txt]
				from CmPossibility_Name cn (readuncommitted)
				left outer join LgEncoding le (readuncommitted)
					on le.[encoding] = cn.[enc]
				left outer join LanguageProject_AnalysisEncodings lpae (readuncommitted)
					on lpae.[dst] = le.[id]
				left outer join LanguageProject_CurrentAnalysisEncs lpcae (readuncommitted)
					on lpcae.[dst] = lpae.[dst]
				where cn.[Obj] = [opi].[Dst]
				order by isnull(lpcae.[ord], 99999)), '***'),
			[cp].[ForeColor],
			[cp].[BackColor],
			[cp].[UnderColor],
			[cp].[UnderStyle],
			[cp].[Hidden]
		from CmOverlay_PossItems [opi]
			join CmPossibility [cp] on [cp].[id] = [opi].[Dst]
			join CmObject [co] on [co].[id] = [cp].[id]
		where [opi].[Src] = @iOwnerId
		order by [opi].[Dst]

	--( if "magic" encoding is for vernacular encodings
	else if @iEncoding = 0xfffffffe
		select
			[co].[Guid$],
			[opi].[Dst],
			isnull((select top 1 txt
				from CmPossibility_Name cn (readuncommitted)
				left outer join LgEncoding le (readuncommitted)
					on le.[encoding] = cn.[enc]
				left outer join LanguageProject_VernacularEncodings lpve (readuncommitted)
					on lpve.[dst] = le.[id]
				left outer join LanguageProject_CurrentVernacularEncs lpcve (readuncommitted)
					on lpcve.[dst] = lpve.[dst]
				where cn.[Obj] = [opi].[Dst]
				order by isnull(lpcve.[ord], 99999)), '***'),
			isnull((select top 1 txt
				from CmPossibility_Abbreviation ca (readuncommitted)
				left outer join LgEncoding le (readuncommitted)
					on le.[encoding] = ca.[enc]
				left outer join LanguageProject_VernacularEncodings lpve (readuncommitted)
					on lpve.[dst] = le.[id]
				left outer join LanguageProject_CurrentVernacularEncs lpcve (readuncommitted)
					on lpcve.[dst] = lpve.[dst]
				where ca.[Obj] = [opi].[Dst]
				order by isnull(lpcve.[ord], 99999)), '***'),
			[cp].[ForeColor],
			[cp].[BackColor],
			[cp].[UnderColor],
			[cp].[UnderStyle],
			[cp].[Hidden]
		from CmOverlay_PossItems [opi]
			join CmPossibility [cp] on [cp].[id] = [opi].[Dst]
			join CmObject [co] on [co].[id] = [cp].[id]
		where [opi].[Src] = @iOwnerId
		order by [opi].[Dst]

	--( if one particular encoding is wanted
	else
		select
			[co].[Guid$],
			[opi].[Dst],
			isnull([ca].[txt], '***'),
			isnull([cn].[txt], '***'),
			[cp].[ForeColor],
			[cp].[BackColor],
			[cp].[UnderColor],
			[cp].[UnderStyle],
			[cp].[Hidden]
		from CmOverlay_PossItems [opi]
			join CmPossibility [cp] on [cp].[id] = [opi].[Dst]
			join CmObject [co] on [co].[id] = [cp].[id]
			left outer join CmPossibility_Abbreviation [ca] (readuncommitted)
				on [ca].[Obj] = [opi].[Dst] and [ca].[enc] = @iEncoding
			left outer join CmPossibility_Name cn (readuncommitted)
				on [cn].[Obj] = [opi].[Dst] and [cn].[enc] = @iEncoding
		where [opi].[Src] = @iOwnerId
		order by [opi].[Dst]

	--( if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

LFail:
	--( if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

go

/*== fnGetColumnDef$ ==*/

--( From FwCore.sql

if object_id('[fnGetColumnDef$]') is not null begin
	print 'removing function fnGetColumnDef$'
	drop function [fnGetColumnDef$]
end
go
print 'creating function fnGetColumnDef$'
go
create function [fnGetColumnDef$] (@nFieldType int)
returns nvarchar(1000)
as
begin
	return case @nFieldType
			when 1 then N'bit = 0'					-- Boolean
			when 2 then N'int = 0'					-- Integer
			when 3 then N'decimal(28,4) = 0'		-- Numeric
			when 4 then N'float = 0.0'				-- Float
			when 5 then N'datetime = null'			-- Time
			when 6 then N'uniqueidentifier = null'	-- Guid
			when 7 then N'image = null'				-- Image
			when 8 then N'int = 0'					-- GenDate
			when 9 then N'varbinary(8000) = null'	-- Binary
			when 13 then N'nvarchar(4000) = null'	-- String
			when 15 then N'nvarchar(4000) = null'	-- Unicode
			when 17 then N'ntext = null'			-- BigString
			when 19 then N'ntext = null'			-- BigUnicode
		end
end
go

/*== CreateOwnedObject$ ==*/

--( From FwCore.sql

if object_id('CreateOwnedObject$') is not null begin
	print 'removing proc CreateOwnedObject$'
	drop proc CreateOwnedObject$
end
go
print 'creating proc CreateOwnedObject$'
go
create proc [CreateOwnedObject$]
	@clid int,
	@id int output,
	@guid uniqueidentifier output,
	@owner int,
	@ownFlid int,
	@type int,			-- type of field (atomic, collection, or sequence)
	@StartObj int = null,		-- object to insert before - owned sequences
	@fGenerateResults tinyint = 0,	-- default to not generating results
	@nNumObjects int = 1,		-- number of objects to create
	@uid uniqueidentifier = null output
as
	declare @err int, @nTrnCnt int, @sTranName varchar(50)
	declare @depth int, @fAbs bit
	declare @sDynSql nvarchar(4000), @sTbl sysname, @sId varchar(11)
	declare @OwnOrd int
	declare @i int, @currId int, @currOrd int, @currListOrd int
	declare	@fIsNocountOn int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- validate the class
	select	@fAbs = [Abstract],
		@sTbl = [Name]
	from	[Class$]
	where	[Id] = @clid
	if @fAbs <> 0 begin
		RaisError('Cannot instantiate abstract class: %s', 16, 1, @sTbl)
		return 50001
	end
	-- get the inheritance depth
	select	@depth = [Depth]
	from	[ClassPar$]
	where	[Src] = @clid
		and [Dst] = 0

	-- determine if a transaction already exists; if one does then create a savepoint, otherwise create a
	--	transaction
	set @nTrnCnt = @@trancount
	set @sTranName = 'CreateOwnedObject$_tr' + convert(varchar(8), @@nestlevel)
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName

	-- determine if the object is being added to a sequence
	if @type = 27 begin

		-- determine if the object(s) should be added to the end of the sequence
		if @StartObj is null begin
			select	@ownOrd = coalesce(max([OwnOrd$])+1, 1)
			from	[CmObject] with (serializable)
			where	[Owner$] = @Owner
				and [OwnFlid$] = @OwnFlid
		end
		else begin
			-- get the ordinal value of the object that is located where the new object is to be inserted
			select	@OwnOrd = [OwnOrd$]
			from	[CmObject] with (repeatableread)
			where	[Id] = @StartObj

			-- increment the ordinal value(s) of the object(s) in the sequence that occur at or after the new object(s)
			update	[CmObject] with (serializable)
			set 	[OwnOrd$]=[OwnOrd$]+@nNumObjects
			where 	[Owner$] = @owner
				and [OwnFlid$] = @OwnFlid
				and [OwnOrd$] >= @OwnOrd
		end
	end

	-- determine if more than one object should be created; if more than one object is created the created objects IDs are stored
	--	in the ObjListTbl$ table so that the calling procedure/application can determine the IDs (the calling procedure or
	--	application is responsible for cleaning up the ObjListTlb$), otherwise if only one object is created the new object's
	--	ID is passed back to the calling procedure/application through output parameters -- the two approaches are warranted
	--	because it is ideal to avoid using the ObjListTbl$ if only one object is being created, also this maintains backward
	--	compatibility with existing code
	if @nNumObjects > 1 begin

		set @uid = NewId()

		set @i = 0
		set @currListOrd = coalesce(@ownOrd, 0)

		-- if an Id was supplied assume that the IDENTITY_INSERT setting is turned on and the incoming Id is legal
		if @id is not null begin
			while @i < @nNumObjects begin
				set @currId = @id + @i
				set @currOrd = @ownOrd + @i

				insert into [CmObject] ([Guid$], [Id], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
					values(newid(), @currId, @clid, @owner, @ownFlid, @currOrd)
				set @err = @@error
				if @Err <> 0 begin
					raiserror('Unable to create object: ID=%d, Class=%d, Owner=%d, OwnFlid=%d, OwnOrd=%d', 16, 1,
							@currId, @clid, @owner, @ownFlid, @currOrd)
					goto LFail
				end

				-- add the new object to the list of created objects
				insert into ObjListTbl$ with (rowlock) (uid, ObjId, Ord, Class)
					values (@uid, @id + @i, @currListOrd + @i, @clid)

				set @i = @i + 1
			end
		end
		else begin
			while @i < @nNumObjects begin
				set @currOrd = @ownOrd + @i

				insert into [CmObject] ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
					values(newid(), @clid, @owner, @ownFlid, @currOrd)
				set @err = @@error
				if @Err <> 0 begin
					raiserror('Unable to create object: Class=%d, Owner=%d, OwnFlid=%d, OwnOrd=%d', 16, 1,
							@clid, @owner, @ownFlid, @currOrd)
					goto LFail
				end
				set @id = @@identity

				-- add the new object to the list of created objects
				insert into ObjListTbl$ with (rowlock) (uid, ObjId, Ord, Class)
					values (@uid, @id, @currListOrd + @i, @clid)
				set @i = @i + 1
			end
		end

		-- insert the objects' Ids into all of the base classes
		while @depth > 0 begin
			set @depth = @depth - 1

			select	@sTbl = c.[Name]
			from	[ClassPar$] cp join [Class$] c on c.[Id] = cp.[Dst]
			where	cp.[Src] = @clid
				and cp.[Depth] = @depth
			set @sDynSql =  'insert into [' + @sTbl + '] ([Id]) '+
					'select [ObjId] ' +
					'from [ObjListTbl$] (readuncommitted) '+
					'where [uid] = '''+convert(varchar(250), @uid)+''''
			exec (@sDynSql)
			set @err = @@error
			if @Err <> 0 begin
				raiserror('Unable to add rows to the base table %s', 16, 1, @sTbl)
				goto LFail
			end
		end

		if @fGenerateResults = 1 begin
			select	ObjId
			from	ObjListTbl$ (readuncommitted)
			where	uid=@uid
			order by Ord
		end
	end
	else begin
		if @guid is null set @guid = NewId()

		-- if an Id was supplied assume that the IDENTITY_INSERT setting is turned on and the incoming Id is legal
		if @id is not null begin
			insert into [CmObject] ([Guid$], [Id], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
				values(@guid, @id, @clid, @owner, @ownFlid, @ownOrd)
			set @err = @@error
			if @Err <> 0 goto LFail
		end
		else begin
			insert into [CmObject] ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
				values(@guid, @clid, @owner, @ownFlid, @ownOrd)
			set @err = @@error
			if @Err <> 0 goto LFail
			set @id = @@identity
		end

		-- insert the object's Id into all of the base classes
		set @sId = convert(varchar(11), @id)
		while @depth > 0 begin
			set @depth = @depth - 1

			select	@sTbl = c.[Name]
			from	[ClassPar$] cp join [Class$] c on c.[Id] = cp.[Dst]
			where	cp.[Src] = @clid
				and cp.[Depth] = @depth
			if @@rowcount <> 1 begin
				raiserror('Corrupt ClassPar$ table: %d', 16, 1, @clid)
				set @err = @@error
				goto LFail
			end

			set @sDynSql = 'insert into [' + @sTbl + '] with (rowlock) ([Id]) values (' + @sId + ')'
			exec (@sDynSql)
			set @err = @@error
			if @Err <> 0 begin
				raiserror('Unable to add a row to the base table %s: ID=%s', 16, 1, @sTbl, @sId)
				goto LFail
			end
		end

		if @fGenerateResults = 1 begin
			select @id [Id], @guid [Guid]
		end
	end

	-- update the date/time of the owner
	UPDATE [CmObject] SET [UpdDttm] = GetDate()
		FROM [CmObject] WHERE [Id] = @owner

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	-- if a transaction was created within this procedure commit it
	if @nTrnCnt = 0 commit tran @sTranName
	return 0

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	rollback tran @sTranName
	return @err
go

/*== MoveToOwnedAtom$ ==*/

--( From FwCore.sql

if object_id('MoveToOwnedAtom$') is not null begin
	print 'removing proc MoveToOwnedAtom$'
	drop proc MoveToOwnedAtom$
end
go
print 'creating proc MoveToOwnedAtom$'
go

create proc MoveToOwnedAtom$
	@SrcObjId int,
	@SrcFlid int,
	@ObjId int = null,
	@DstObjId int,
	@DstFlid int
as
	declare @sTranName varchar(50)
	declare	@fIsNocountOn int, @Err int, @nTrnCnt int

	set @Err = 0

	-- transactions
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on
	set @sTranName = 'MoveToOwnedAtom$_' + convert(varchar(11), @@trancount)
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('MoveToOwnedAtom$: SQL Error %d; Unable to create a transaction', 16, 1, @Err)
		goto LFail
	end

	update	CmObject with (repeatableread)
	set [Owner$] = @DstObjId,
		[OwnFlid$] = @DstFlid,
		[OwnOrd$] = null
	where [Id] = @ObjId

	set @Err = @@error
	if @Err <> 0 begin
		raiserror('MoveToOwnedAtom$: SQL Error %d; Unable to update owners in CmObject: DstObjId(Owner$) = %d, DstFlid(OwnFlid$) = %d',
				16, 1, @Err, @DstObjId, @DstFlid)
		goto LFail
	end

	-- stamp the owning objects as updated
	update CmObject with (repeatableread)
		set [UpdDttm] = getdate()
		where [Id] in (@SrcObjId, @DstObjId)
		--( seems to execute as fast as a where clause written:
		--(    where [Id] = @SrcObjId or [Id] =@DstObjId

	if @nTrnCnt = 0 commit tran @sTranName

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return 0

LFail:
	rollback tran @sTranName

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
GO

/*== MoveToOwnedColl$ ==*/

--( From FwCore.sql

if object_id('MoveToOwnedColl$') is not null begin
	print 'removing proc MoveToOwnedColl$'
	drop proc MoveToOwnedColl$
end
go
print 'creating proc MoveToOwnedColl$'
go

create proc MoveToOwnedColl$
	@SrcObjId int,		-- The ID of the object that owns the source object(s)
	@SrcFlid int,		-- The FLID (field ID) of the object attribute that owns the object(s)
	@StartObj int = null,	-- The ID of the first object to be moved.
	@EndObj int = null,	-- The ID of the last object to be moved
	@DstObjId int,		-- The ID of the object which will own the object(s) moved
	@DstFlid int		-- The FLID (field ID) of the object attribute that will own the object(s)

as
	declare @sTranName varchar(50)
	declare @StartOrd int, @EndOrd int
	declare	@fIsNocountOn int, @Err int, @nTrnCnt int, @nSrcType int

	-- Remark these constants for production code. For coding and testing with Query Analyzer,
	-- unremark these constants and put an @ in front of the variables wherever they appear.
	/*
	declare @kcptOwningSequence int
	set @kcptOwningSequence = 27
	declare @kcptOwningCollection int
	set @kcptOwningCollection = 25
	declare @kcptOwningAtom int
	set @kcptOwningAtom = 23
	*/

	set @Err = 0

	-- transactions
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on
	set @sTranName = 'MoveToOwnedColl$_' + convert(varchar(11), @@trancount)
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('MoveToOwnedColl$: SQL Error %d; Unable to create a transaction', 16, 1, @Err)
		goto LFail
	end

	select @nSrcType = [Type]
	from Field$
	where [id] = @SrcFlid

	if @nSrcType = 27 begin  --( If source object is an owning sequence

		select	@StartOrd = [OwnOrd$]
		from	CmObject (repeatableread)
		where	[Owner$] = @SrcObjId
			and [OwnFlid$] = @SrcFlid
			and [Id] = @StartObj

		if @EndObj is null begin
			select	@EndOrd = max([OwnOrd$])
			from	CmObject (serializable)
			where	[Owner$] = @SrcObjId
				and [OwnFlid$] = @SrcFlid
		end
		else begin
			select	@EndOrd = [OwnOrd$]
			from	CmObject (repeatableread)
			where	[Owner$] = @SrcObjId
				and [OwnFlid$] = @SrcFlid
				and [Id] = @EndObj
		end

		if @EndOrd is not null and @EndOrd < @StartOrd begin
			raiserror('MoveToOwnedColl$: The starting ordinal value %d is greater than the ending ordinal value %d in CmObject: SrcObjId(Owner$) = %d SrcFlid(OwnFlid$) = %d StartObj(Id) = %d EndObj(Id) = %d',
					16, 1, @StartOrd, @EndOrd, @SrcObjId, @SrcFlid, @StartObj, @EndObj)
			set @Err = 51001
			goto LFail
		end

		update	CmObject with (repeatableread)
		set [Owner$] = @DstObjId,
			[OwnFlid$] = @DstFlid,
			[OwnOrd$] = null
		where	[Owner$] = @SrcObjId
			and [OwnFlid$] = @SrcFlid
			and [OwnOrd$] >= @StartOrd and [OwnOrd$] <= @EndOrd
	end
	else begin
		-- ENHANCE SteveMiller: Cannot yet move more than one object from a collection to a sequence.
		if @nSrcType = 25 and not @StartObj = @EndObj begin
			raiserror('MoveToOwnedSeq$: Cannot yet move more than one object from a collection to a sequence', 16, 1)
			set @Err = 51002
			goto LFail
		end

		update	CmObject with (repeatableread)
		set [Owner$] = @DstObjId,
			[OwnFlid$] = @DstFlid,
			[OwnOrd$] = null
		where [Id] = @StartObj
	end

	set @Err = @@error
	if @Err <> 0 begin
		raiserror('MoveToOwnedColl$: SQL Error %d; Unable to update owners in CmObject: DstObjId(Owner$) = %d, DstFlid(OwnFlid$) = %d, start = %d, end = %d',
				16, 1, @Err, @DstObjId, @DstFlid, @StartOrd, @EndOrd)
		goto LFail
	end

	-- stamp the owning objects as updated
	update CmObject with (repeatableread)
		set [UpdDttm] = getdate()
		where [Id] in (@SrcObjId, @DstObjId)
		--( seems to execute as fast as a where clause written:
		--(    where [Id] = @SrcObjId or [Id] =@DstObjId

	if @nTrnCnt = 0 commit tran @sTranName

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return 0

LFail:
	rollback tran @sTranName

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
go

/*== MoveToOwnedSeq$ ==*/

--( From FwCore.sql

if object_id('MoveToOwnedSeq$') is not null begin
	print 'removing proc MoveToOwnedSeq$'
	drop proc MoveToOwnedSeq$
end
go
print 'creating proc MoveToOwnedSeq$'
go

CREATE proc MoveToOwnedSeq$
	@SrcObjId int,
	@SrcFlid int,
	@StartObj int,
	@EndObj int = null,
	@DstObjId int,
	@DstFlid int,
	@DstStartObj int = null
as
	declare @sTranName varchar(50)
	declare @nDstType int
	declare @nMinOrd int, @StartOrd int, @EndOrd int, @DstStartOrd int, @DstEndOrd int, @DstEndDelOrd int
	declare @nSpaceAvail int, @nSpaceNeed int, @nNewOrdOffset int, @fMadeSpace tinyint
	declare @uid uniqueidentifier
	declare	@fIsNocountOn int, @Err int, @nTrnCnt int, @UpdStmp int, @RetVal int, @nSrcType int

	-- Remark these constants for production code. For coding and testing with Query Analyzer,
	-- unremark these constants and put an @ in front of the variables wherever they appear.
	/*
	declare @kcptOwningSequence int
	set @kcptOwningSequence = 27
	declare @kcptOwningCollection int
	set @kcptOwningCollection = 25
	declare @kcptOwningAtom int
	set @kcptOwningAtom = 23
	*/

	set @Err = 0
	set @fMadeSpace = 0

	--==Transactions ==--
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- determine if the procedure was called within a transaction; if yes then create a savepoint,
	--	otherwise create a transaction
	set @sTranName = 'MoveToOwnedSeq$_' + convert(varchar(11), @@trancount)
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('MoveToOwnedSeq$: SQL Error %d; Unable to create a transaction', 16, 1, @Err)
		goto LFail
	end

	--== Get Start and End Orders ==-

	--( See notes at the top of the file about the query where clause

	if @DstStartObj is null begin
		select	@DstStartOrd = coalesce(max([OwnOrd$]), -1) + 1
		from	CmObject (serializable)
		where	[Owner$] = @DstObjId
			and [OwnFlid$] = @DstFlid
	end
	else begin
		select	@DstStartOrd = [OwnOrd$]
		from	CmObject (repeatableread)
		where	[Owner$] = @DstObjId
			and [OwnFlid$] = @DstFlid
			and [Id] = @DstStartObj
	end

	--( Get type (atomic, collection, or sequence) of source
	select @nSrcType = [Type]
	from Field$
	where [id] = @SrcFlid

	if @nSrcType = 27 begin  --( If source object is an owning sequence
		-- get the starting and ending ordinal values
		select	@StartOrd = [OwnOrd$]
		from	CmObject (repeatableread)
		where	[Owner$] = @SrcObjId
			and [OwnFlid$] = @SrcFlid
			and [Id] = @StartObj
		if @EndObj is null begin
			select	@EndOrd = max([OwnOrd$])
			from	CmObject (serializable)
			where	[Owner$] = @SrcObjId
				and [OwnFlid$] = @SrcFlid
		end
		else begin
			select	@EndOrd = [OwnOrd$]
			from	CmObject (repeatableread)
			where	[Owner$] = @SrcObjId
				and [OwnFlid$] = @SrcFlid
				and [Id] = @EndObj
		end
	end

	-- If source object is an owning collection
	else if @nSrcType = 25 begin

		-- ENHANCE SteveMiller: Cannot yet move more than one object from a collection to a sequence.
		if not @StartObj = @EndObj begin
			raiserror('MoveToOwnedSeq$: Cannot yet move more than one object from a collection to a sequence', 16, 1)
			set @Err = 51000
			goto LFail
		end

		set @StartOrd = @DstStartOrd
		set @EndOrd = @StartOrd
	end

	-- If source object is an owning atom
	else if @nSrcType = 23 begin

		if not @StartObj = @EndObj begin
			raiserror('MoveToOwnedSeq$: Cannot move two atoms at the same time', 16, 1)
			set @Err = 51001
			goto LFail
		end

		set @StartOrd = @DstStartOrd
		set @EndOrd = @StartOrd
	end

	set @DstEndOrd = @DstStartOrd + @EndOrd - @StartOrd

	--== Validate the arguments  ==--
	if @StartOrd is null begin
		raiserror('MoveToOwnedSeq$: Unable to locate ordinal value in CmObject: SrcObjId(Owner$) = %d SrcFlid(OwnFlid$) = %d StartObj(Id) = %d',
				16, 1, @SrcObjId, @SrcFlid, @StartObj)
		set @Err = 51002
		goto LFail
	end
	if @EndOrd is null begin
		raiserror('MoveToOwnedSeq$: Unable to locate ordinal value in CmObject: SrcObjId(Owner$) = %d SrcFlid(OwnFlid$) = %d EndObj(Id) = %d',
				16, 1, @SrcObjId, @SrcFlid, @EndObj)
		set @Err = 51003
		goto LFail
	end
	if @DstStartOrd is null begin
		raiserror('MoveToOwnedSeq$: Unable to locate ordinal value in CmObject: DstObjId(Owner$) = %d DstFlid(OwnFlid$) = %d DstStartObj(Id) = %d',
				16, 1, @DstObjId, @DstFlid, @DstStartObj)
		set @Err = 51004
		goto LFail
	end
	if @EndOrd is not null and @EndOrd < @StartOrd begin
		raiserror('MoveToOwnedSeq$: The starting ordinal value %d is greater than the ending ordinal value %d in CmObject: SrcObjId(Owner$) = %d SrcFlid(OwnFlid$) = %d StartObj(Id) = %d EndObj(Id) = %d',
				16, 1, @StartOrd, @EndOrd, @SrcObjId, @SrcFlid, @StartObj, @EndObj)
		set @Err = 51005
		goto LFail
	end

	-- if the objects are not appended to the end of the destination list then determine if there is enough room
	if @DstStartObj is not null begin

		-- find the object with the largest ordinal value less than the destination start object's ordinal
		select @nMinOrd = coalesce(max([OwnOrd$]), -1)
		from	CmObject with (serializable)
		where	[Owner$] = @DstObjId
			and [OwnFlid$] = @DstFlid
			and [OwnOrd$] < @DstStartOrd
		set @Err = @@error
		if @Err <> 0 begin
			raiserror('MoveToOwnedSeq$: SQL Error %d; Unable to analyze the sequence in CmObject: DstObjId(Owner$) = %d, DstFlid(OwnFlid$) = %d',
					16, 1, @Err, @DstObjId, @DstFlid)
			goto LFail
		end

		set @nSpaceAvail = @DstStartOrd - @nMinOrd - 1
		set @nSpaceNeed = @EndOrd - @StartOrd + 1

		-- see if there is currently enough room for the objects under the destination object's sequence list;
		--	if there is not then make room
		if @nSpaceAvail < @nSpaceNeed begin

			set @fMadeSpace = 1

			update	CmObject with (repeatableread)
			set	[OwnOrd$] = [OwnOrd$] + @nSpaceNeed - @nSpaceAvail
			where	[Owner$] = @DstObjId
				and [OwnFlid$] = @DstFlid
				and [OwnOrd$] >= @DstStartOrd
			set @Err = @@error
			if @Err <> 0 begin
				raiserror('MoveToOwnedSeq$: SQL Error %d; Unable to increment the ordinal values in the CmObject: DstObjId(Owner$) = %d, DstFlid(OwnFlid$) = %d',
					16, 1, @Err, @DstObjId, @DstFlid)
				goto LFail
			end
		end

		set @nNewOrdOffset = @nMinOrd + 1 - @StartOrd
	end
	else begin
		set @nNewOrdOffset = @DstStartOrd - @StartOrd
	end

	-- determine if the source and destination owning sequence is the same
	if @SrcObjId = @DstObjId and @SrcFlid = @DstFlid begin

		-- if room was made below the objects that are to be moved then the objects to be moved also
		--	had their ordinal values modified, so calculate the new source ordinal numbers so that they
		--	will remain in the range of objects that are to be moved
		if @fMadeSpace = 1 and @StartOrd > @DstStartOrd begin
			set @StartOrd =  @StartOrd + @nSpaceNeed - @nSpaceAvail
			set @EndOrd = @EndOrd + @nSpaceNeed - @nSpaceAvail
			set @nNewOrdOffset = @DstStartOrd - @StartOrd
		end

		-- update the ordinals of the specified range of objects in the specified sequence
		update	CmObject with (repeatableread)
		set	[OwnOrd$] = [OwnOrd$] + @nNewOrdOffset
		where	[Owner$] = @SrcObjId
			and [OwnFlid$] = @SrcFlid
			and [OwnOrd$] >= @StartOrd
			and [OwnOrd$] <= @EndOrd
		set @Err = @@error
		if @Err <> 0 begin
			raiserror('MoveToOwnedSeq$: SQL Error %d; Unable to update ordinals in CmObject: DstObjId(Owner$) = %d, DstFlid(OwnFlid$) = %d, start = %d, end = %d',
				16, 1, @Err, @DstObjId, @DstFlid, @StartOrd, @EndOrd)
			goto LFail
		end
	end

	-- destination and source are not the same
	else begin
		if @nSrcType = 27 begin
			-- update the owner of the specified range of objects in the specified sequence
			update	CmObject with (repeatableread)
			set	[Owner$] = @DstObjId,
				[OwnFlid$] = @DstFlid,
				[OwnOrd$] = [OwnOrd$] + @nNewOrdOffset
			where	[Owner$] = @SrcObjId
				and [OwnFlid$] = @SrcFlid
				and [OwnOrd$] >= @StartOrd
				and [OwnOrd$] <= @EndOrd
		end
		else if @nSrcType = 25 or @nSrcType = 23 begin
			update	CmObject with (repeatableread)
			set	[Owner$] = @DstObjId,
				[OwnFlid$] = @DstFlid,
				[OwnOrd$] = @DstStartOrd + @nNewOrdOffset
			where	[Id] = @StartObj
		end

		set @Err = @@error
		if @Err <> 0 begin
			raiserror('MoveToOwnedSeq$: SQL Error %d; Unable to update owners in CmObject: DstObjId(Owner$) = %d, DstFlid(OwnFlid$) = %d, start = %d, end = %d',
				16, 1, @Err, @DstObjId, @DstFlid, @StartOrd, @EndOrd)
			goto LFail
		end
	end

	-- stamp the owning objects as updated
	update CmObject with (repeatableread)
		set [UpdDttm] = getdate()
		where [Id] in (@SrcObjId, @DstObjId)
		--( seems to execute as fast as a where clause written:
		--(    where [Id] = @SrcObjId or [Id] =@DstObjId

	if @nTrnCnt = 0 commit tran @sTranName

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return 0

LFail:
	rollback tran @sTranName

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
go

/*== TR_Field$_UpdateModel_Ins ==*/

--( From FwCore.sql

if object_id('TR_Field$_UpdateModel_Ins') is not null begin
	print 'removing trigger TR_Field$_UpdateModel_Ins'
	drop trigger [TR_Field$_UpdateModel_Ins]
end
go
print 'creating trigger TR_Field$_UpdateModel_Ins'
go

create trigger [TR_Field$_UpdateModel_Ins] on [Field$] for insert
as
	declare @sFlid VARCHAR(20)
	declare @Type INT
	declare @Clid INT
	declare @DstCls INT
	declare @sName sysname
	declare @sClass sysname
	declare @sTargetClass sysname
	declare @Min BIGINT
	declare @Max BIGINT
	declare @Big BIT
	declare @fIsCustom bit

	declare @sql VARCHAR(1000)
	declare @Err INT
	declare @fIsNocountOn INT

	declare @sMin VARCHAR(25)
	declare @sMax VARCHAR(25)
	declare @sTable VARCHAR(20)
	declare @sFmtArg VARCHAR(40)

	declare @sTableName sysname

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- get the first class to process
	Select @sFlid= min([id]) from inserted

	-- loop through all of the classes in the inserted logical table
	while @sFlid is not null begin

		-- get inserted data
		select 	@Type = [Type], @Clid = [Class], @sName = [Name], @DstCls = [DstCls], @Min = [Min], @Max = [Max], @Big = [Big], @fIsCustom = [Custom]
		from	inserted i
		where	[Id] = @sFlid

		-- get class name
		select 	@sClass = [Name]  from class$  where [Id] = @Clid

		-- get target class for Reference Objects
		if @Type in (24,26,28) begin
			select 	@sTargetClass = [Name]  from class$  where [Id] = @DstCls
		end

		if @type = 2 begin

			set @sMin = coalesce(convert(varchar(25), @Min), 0)
			set @sMax = coalesce(convert(varchar(25), @Max), 0)

			-- Add Integer to table sized based on Min/Max values supplied
			set @sql = 'ALTER TABLE [' + @sClass + '] ADD [' + @sName + '] '

			if @Min >= 0 and @Max <= 255
				set @sql = @sql + 'TINYINT NOT NULL DEFAULT ' + @sMin
			else if @Min >= -32768 and @Max <= 32767
				set @sql = @sql + 'SMALLINT NOT NULL DEFAULT ' + @sMin
			else if @Min < -2147483648 or @Max > 2147483647
				set @sql = @sql + 'BIGINT NOT NULL DEFAULT ' + @sMin
			else
				set @sql = @sql + 'INT NOT NULL DEFAULT ' + @sMin
			exec (@sql)
			if @@error <> 0 goto LFail

			-- Add Check constraint
			if @Min is not null and @Max is not null begin
				-- format as text

				set @sql = 'ALTER TABLE [' + @sClass + '] ADD CONSTRAINT [' +
					'_CK_' + @sClass + '_' + @sName + '] ' + CHAR(13) + CHAR(9) +
					' check ( [' + @sName + '] is null or ([' + @sName + '] >= ' + @sMin + ' and  [' + @sName + '] <= ' + @sMax + '))'
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			end

			-- fix the view associated with this class.
			if @fIsCustom = 1 begin
				exec @Err = UpdateClassView$ @clid, 1
				if @Err <> 0 goto LFail
			end

		end
		else if @type IN (14,16,18,20) begin
			-- Define the view and Set_ procedure for this multilingual custom field

			set @sTable = case @type
				when 14 then 'MultiStr$'
				when 16 then 'MultiTxt$'
				when 18 then 'MultiBigStr$'
				when 20 then 'MultiBigTxt$'
				end
			set @sFmtArg = case @type
				when 14 then '[Fmt]'
				when 16 then 'cast(null as varbinary) as [Fmt]'
				when 18 then '[Fmt]'
				when 20 then 'cast(null as varbinary) as [Fmt]'
				end
			set @sql = 'CREATE VIEW [' + @sClass + '_' + @sName + '] AS' + CHAR(13) +
				CHAR(9) + 'select [Obj], [Flid], [Enc], [Txt], ' + @sFmtArg + CHAR(13) +
				CHAR(9) + 'FROM [' + @sTable + ']' + CHAR(13) +
				CHAR(9) + 'WHERE [Flid] = ' + @sFlid
			exec (@sql)
			if @@error <> 0 goto LFail
		end
		else if @type IN (23,25,27) begin
			-- define the view for this OwningAtom/Collection/Sequence custom field.
			set @sql = 'CREATE VIEW [' + @sClass + '_' + @sName + '] AS' + CHAR(13) +
				CHAR(9) + 'select [Owner$] as [Src], [Id] as [Dst]'

			if @type = 27 set @sql = @sql + ', [OwnOrd$] as [Ord]'

			set @sql = @sql + CHAR(13) +
				CHAR(9) + 'FROM [CmObject]' + CHAR(13) +
				CHAR(9) + 'WHERE [OwnFlid$] = ' + @sFlid
			exec (@sql)
			if @@error <> 0 goto LFail
		end
		else if @type IN (26,28) begin
			-- define the table for this custom reference collection/sequence field.
			set @sql = 'CREATE TABLE [' + @sClass + '_' + @sName + '] (' + CHAR(13) +
				'[Src] INT NOT NULL,' + CHAR(13) +
				'[Dst] INT NOT NULL,' + CHAR(13)

			if @type = 28 set @sql = @sql + '[Ord] INT NOT NULL,' + CHAR(13)

			set @sql = @sql +
				'CONSTRAINT [_FK_' + @sClass + '_' + @sName + '_Src] ' +
				'FOREIGN KEY ([Src]) REFERENCES [' + @sClass + '] ([Id]),' + CHAR(13) +
				'CONSTRAINT [_FK_' + @sClass + '_' + @sName + '_Dst] ' +
				'FOREIGN KEY ([Dst]) REFERENCES [' + @sTargetClass + '] ([Id]),' + CHAR(13) +
				case @type
					when 26 then ')'
					when 28 then
						CHAR(9) + CHAR(9) + 'CONSTRAINT [_PK_' + @sClass + '_' + @sName + '] ' +
						'PRIMARY KEY CLUSTERED ([Src], [Ord])' + CHAR(13) + ')'
					end
			exec (@sql)
			if @@error <> 0 goto LFail

			if @type = 26 begin
				set @sql = 'create clustered index ' +
						@sClass + '_' + @sName + '_ind on ' +
						@sClass + '_' + @sName + ' ([Src], [Dst])'
				exec (@sql)
				if @@error <> 0 goto LFail

				set @sTableName = @sClass + '_' + @sName
				exec @Err = DefineReplaceRefCollProc$ @sTableName
				if @Err <> 0 begin
					raiserror('TR_Field$_UpdateModel_Ins: Unable to create the procedure that handles reference collections for table %s',
							16, 1, @sName)
					goto LFail
				end

			end

			if @type = 28 begin
				set @sTableName = @sClass + '_' + @sName
				exec @Err = DefineReplaceRefSeqProc$ @sTableName, @sFlid
				if @Err <> 0 begin
					raiserror('TR_Field$_UpdateModel_Ins: Unable to create the procedure that handles reference sequences for table %s',
							16, 1, @sName)
					goto LFail
				end
			end

		--( Insert trigger
			SET @sql = 'CREATE TRIGGER [TR_' + @sClass + '_' + @sName + '_DtTmIns]' + CHAR(13) +
				CHAR(9) + 'ON [' + @sClass + '_' + @sName + '] FOR INSERT ' + CHAR(13) +
				'AS ' + CHAR(13) +
				CHAR(9) + 'UPDATE CmObject SET UpdDttm = GetDate() ' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'FROM CmObject co JOIN inserted ins ON co.[id] = ins.[src] ' + CHAR(13) +
				CHAR(9) + CHAR(13)  +
				CHAR(9) + 'IF @@error <> 0 BEGIN' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'Raiserror(''TR_' + @sClass + '_' + @sName + '_DtTmIns]: ' +
					'Unable to update CmObject'', 16, 1)' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'GOTO LFail' + CHAR(13) +
				CHAR(9) + 'END' + CHAR(13) +
				CHAR(9) + 'RETURN' + CHAR(13) +
				CHAR(9) + CHAR(13) +
				CHAR(9) + 'LFail:' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'ROLLBACK TRAN' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'RETURN' + CHAR(13)
			EXEC (@sql)
			IF @@error <> 0 GOTO LFail

			--( Delete trigger
			SET @sql = 'CREATE TRIGGER [TR_' + @sClass + '_' + @sName + '_DtTmDel]' + CHAR(13) +
				CHAR(9) + 'ON [' + @sClass + '_' + @sName + '] FOR DELETE ' + CHAR(13) +
				'AS ' + CHAR(13) +
				CHAR(9) + 'UPDATE CmObject SET UpdDttm = GetDate() ' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'FROM CmObject co JOIN deleted del ON co.[id] = del.[src] ' + CHAR(13) +
				CHAR(9) + CHAR(13)  +
				CHAR(9) + 'IF @@error <> 0 BEGIN' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'Raiserror(''TR_' + @sClass + '_' + @sName + '_DtTmDel]: ' +
					'Unable to update CmObject'', 16, 1)' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'GOTO LFail' + CHAR(13) +
				CHAR(9) + 'END' + CHAR(13) +
				CHAR(9) + 'RETURN' + CHAR(13) +
				CHAR(9) + CHAR(13) +
				CHAR(9) + 'LFail:' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'ROLLBACK TRAN' + CHAR(13) +
				CHAR(9) + CHAR(9) + 'RETURN' + CHAR(13)
			EXEC (@sql)
			IF @@error <> 0 GOTO LFail

		end
		else begin
			-- add the custom field to the appropriate table
			set @sql = 'ALTER TABLE [' + @sClass + '] ADD [' + @sName + '] ' + case
				when @type = 1 then 'BIT NOT NULL DEFAULT 0'			-- Boolean
				when @type = 3 then 'DECIMAL(28,4) NOT NULL DEFAULT 0'		-- Numeric
				when @type = 4 then 'FLOAT NOT NULL DEFAULT 0.0'		-- Float
				when @type = 5 then 'DATETIME NULL'				-- Time
				when @type = 6 then 'UNIQUEIDENTIFIER NULL'			-- Guid
				when @type = 7 then 'IMAGE NULL'				-- Image
				when @type = 8 then 'INT NOT NULL DEFAULT 0'			-- GenDate
				when @type = 9 and @big = 0 then 'VARBINARY(8000) NULL'		-- Binary
				when @type = 9 and @big = 1 then 'IMAGE NULL'			-- Binary
				when @type = 13 then 'NVARCHAR(4000) NULL'			-- String
				when @type = 15 then 'NVARCHAR(4000) NULL'			-- Unicode
				when @type = 17 then 'NTEXT NULL'				-- BigString
				when @type = 19 then 'NTEXT NULL'				-- BigUnicode
				when @type = 24 then 'INT NULL'					-- ReferenceAtom
				end
			exec (@sql)
			if @@error <> 0 goto LFail
			if @type in (13,17)  begin
				set @sql = 'ALTER TABLE [' + @sClass + '] ADD ' + case @type
					when 13 then '[' + @sName + '_Fmt] VARBINARY(8000) NULL' -- String
					when 17 then '[' + @sName + '_Fmt] IMAGE NULL'			-- BigString
					end
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			end

			-- Set the 'Text In Row' option for the table if type is 7, 17 or 19.
			if @type in (7, 17, 19) exec sp_tableoption @sClass, 'text in row', '1000'

			-- don't create foreign key constraints on CmObject
			if @type = 24 and @sTargetClass != 'CmObject' begin
				set @sql = 'ALTER TABLE [' + @sClass + '] ADD CONSTRAINT [' +		-- ReferenceAtom
					'_FK_' + @sClass + '_' + @sName + '] ' + CHAR(13) + CHAR(9) +
					' FOREIGN KEY ([' + @sName + ']) REFERENCES [' + @sTargetClass + '] ([Id])'
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			end

			-- fix the view associated with this class.
			if @fIsCustom = 1 begin
				exec @Err = UpdateClassView$ @clid, 1
				if @Err <> 0 goto LFail
			end
		end

		-- get the next class to process
		Select @sFlid= min([id]) from inserted  where [Id] > @sFlid

	end  -- While loop

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return

LFail:
	rollback tran
	return
go

/*== GetOrderedMultiTxt ==*/

--( From LangProjSP.sql

if exists (select * from sysobjects where name = 'GetOrderedMultiTxt')
	drop proc GetOrderedMultiTxt
go
print 'creating proc GetOrderedMultiTxt'
go

create proc GetOrderedMultiTxt
	@id int,
	@flid int,
	@anal tinyint = 1
as

	declare @fIsNocountOn int
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	declare @iFieldType int
	select @iFieldType = [Type] from Field$ where [Id] = @flid

	--== Analysis Encodings ==--

	if @anal = 1
	begin

		-- MultiStr$ --
		if @iFieldType = 14
			select
				isnull(ms.[txt], '***') txt,
				ms.[enc],
				isnull(lpcae.[ord], 99998) [ord]
			from MultiStr$ ms (readuncommitted)
			left outer join LgEncoding le (readuncommitted) on le.[encoding] = ms.[enc]
			left outer join LanguageProject_AnalysisEncodings lpae (readuncommitted) on lpae.[dst] = le.[id]
			left outer join LanguageProject_CurrentAnalysisEncs lpcae (readuncommitted) on lpcae.[dst] = lpae.[dst]
			where ms.[obj] = @id and ms.[flid] = @flid
			union all
			select '***', 0, 99999
			order by isnull([ord], 99998)

		-- MultiBigStr$ --
		else if @iFieldType = 18
		begin
			--( See note 2 in the header
			declare @tblMultiBigStrAnalysis table (
				[txt] ntext,
				[enc] int,
				[ord] int primary key)

			insert into @tblMultiBigStrAnalysis
			select
				isnull(mbs.[txt], '***') txt,
				mbs.[enc],
				isnull(lpcae.[ord], 99998) [ord]
			from MultiBigStr$ mbs (readuncommitted)
			left outer join LgEncoding le (readuncommitted) on le.[encoding] = mbs.[enc]
			left outer join LanguageProject_AnalysisEncodings lpae (readuncommitted) on lpae.[dst] = le.[id]
			left outer join LanguageProject_CurrentAnalysisEncs lpcae (readuncommitted) on lpcae.[dst] = lpae.[dst]
			where mbs.[obj] = @id and mbs.[flid] = @flid
			order by isnull([ord], 99998)

			insert into @tblMultiBigStrAnalysis
			select convert(ntext, '***') [txt], 0 [enc], 99999 [ord]

			select * from @tblMultiBigStrAnalysis order by [ord]
		end

		-- MultiBigTxt$ --
		else if @iFieldType = 20
		begin
			--( See note 2 in the header
			declare @tblMultiBigTxtAnalysis table (
				[txt] ntext,
				[enc] int,
				[ord] int primary key)

			insert into @tblMultiBigTxtAnalysis
			select
				isnull(mbt.[txt], '***') txt,
				mbt.[enc],
				isnull(lpcae.[ord], 99998) [ord]
			from MultiBigTxt$ mbt (readuncommitted)
			left outer join LgEncoding le (readuncommitted) on le.[encoding] = mbt.[enc]
			left outer join LanguageProject_AnalysisEncodings lpae (readuncommitted) on lpae.[dst] = le.[id]
			left outer join LanguageProject_CurrentAnalysisEncs lpcae (readuncommitted) on lpcae.[dst] = lpae.[dst]
			where mbt.[obj] = @id and mbt.[flid] = @flid
			order by isnull([ord], 99998)

			insert into @tblMultiBigTxtAnalysis
			select convert(ntext, '***') [txt], 0 [enc], 99999 [ord]

			select * from @tblMultiBigTxtAnalysis order by [ord]
		end

		-- MultiTxt$, and anything else --
		else --(  kcptMultiUnicode, and whatever else falls through
			select
				isnull(mt.[txt], '***') txt,
				mt.[enc],
				isnull(lpcae.[ord], 99998) [ord]
			from MultiTxt$ mt (readuncommitted)
			left outer join LgEncoding le (readuncommitted) on le.[encoding] = mt.[enc]
			left outer join LanguageProject_AnalysisEncodings lpae (readuncommitted) on lpae.[dst] = le.[id]
			left outer join LanguageProject_CurrentAnalysisEncs lpcae (readuncommitted) on lpcae.[dst] = lpae.[dst]
			where mt.[obj] = @id and mt.[flid] = @flid
			union all
			select '***', 0, 99999
			order by isnull([ord], 99998)
	end

	--== Vernacular Encodings ==--

	else if @anal = 0
	begin

		-- MultiStr$ --
		if @iFieldType = 14
			select
				isnull(ms.[txt], '***') txt,
				ms.[enc],
				isnull(lpcve.[ord], 99998) [ord]
			from MultiStr$ ms (readuncommitted)
			left outer join LgEncoding le (readuncommitted) on le.[encoding] = ms.[enc]
			left outer join LanguageProject_VernacularEncodings lpve (readuncommitted) on lpve.[dst] = le.[id]
			left outer join LanguageProject_CurrentVernacularEncs lpcve (readuncommitted) on lpcve.[dst] = lpve.[dst]
			where ms.[obj] = @id and ms.[flid] = @flid
			union all
			select '***', 0, 99999
			order by isnull([ord], 99998)

		-- MultiBigStr$ --
		else if @iFieldType = 18
		begin
			--( See note 2 in the header
			declare @tblMultiBigStrVernacular table (
				[txt] ntext,
				[enc] int,
				[ord] int primary key)

			insert into @tblMultiBigStrVernacular
			select
				isnull(mbs.[txt], '***') txt,
				mbs.[enc],
				isnull(lpcve.[ord], 99998) [ord]
			from MultiBigStr$ mbs (readuncommitted)
			left outer join LgEncoding le (readuncommitted) on le.[encoding] = mbs.[enc]
			left outer join LanguageProject_VernacularEncodings lpve (readuncommitted) on lpve.[dst] = le.[id]
			left outer join LanguageProject_CurrentVernacularEncs lpcve (readuncommitted) on lpcve.[dst] = lpve.[dst]
			where mbs.[obj] = @id and mbs.[flid] = @flid
			order by isnull([ord], 99998)

			insert into @tblMultiBigStrVernacular
			select convert(ntext, '***') [txt], 0 [enc], 99999 [ord]

			select * from @tblMultiBigStrVernacular order by [ord]
		end

		-- MultiBigTxt$ --
		else if @iFieldType = 20
		begin
			--( See note 2 in the header
			declare @tblMultiBigTxtVernacular table (
				[txt] ntext,
				[enc] int,
				[ord] int primary key)

			insert into @tblMultiBigTxtVernacular
			select
				isnull(mbt.[txt], '***') txt,
				mbt.[enc],
				isnull(lpcve.[ord], 99998) [ord]
			from MultiBigTxt$ mbt (readuncommitted)
			left outer join LgEncoding le (readuncommitted) on le.[encoding] = mbt.[enc]
			left outer join LanguageProject_VernacularEncodings lpve (readuncommitted) on lpve.[dst] = le.[id]
			left outer join LanguageProject_CurrentVernacularEncs lpcve (readuncommitted) on lpcve.[dst] = lpve.[dst]
			where mbt.[obj] = @id and mbt.[flid] = @flid
			order by isnull([ord], 99998)

			insert into @tblMultiBigTxtVernacular
			select convert(ntext, '***') [txt], 0 [enc], 99999 [ord]

			select * from @tblMultiBigTxtVernacular order by [ord]
		end

		-- MultiTxt$, and everything else --
		else --(  kcptMultiUnicode, and whatever else falls through
			select
				isnull(mt.[txt], '***') txt,
				mt.[enc],
				isnull(lpcve.[ord], 99998) ord
			from MultiTxt$ mt (readuncommitted)
			left outer join LgEncoding le (readuncommitted) on le.[encoding] = mt.[enc]
			left outer join LanguageProject_VernacularEncodings lpve (readuncommitted) on lpve.[dst] = le.[id]
			left outer join LanguageProject_CurrentVernacularEncs lpcve (readuncommitted) on lpcve.[dst] = lpve.[dst]
			where mt.[obj] = @id and mt.[flid] = @flid
			union all
			select '***', 0, 99999
			order by isnull([ord], 99998)

	end
	else
		raiserror('@anal flag not set correctly', 16, 1)
		goto LFail

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	go

/*== TR_StTxtPara_Owner_Ins ==*/

--( From LangProjSP.sql

IF object_id('TR_StTxtPara_Owner_Ins') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_StTxtPara_Owner_Ins'
	DROP TRIGGER [TR_StTxtPara_Owner_Ins]
END
GO
PRINT 'creating trigger TR_StTxtPara_Owner_Ins'
GO

CREATE TRIGGER [TR_StTxtPara_Owner_Ins] on [StTxtPara] FOR INSERT
AS
	DECLARE @iErr INT, @fIsNocountOn INT

	SET @fIsNocountOn = @@options & 512
	IF @fIsNocountOn = 0 SET NOCOUNT ON

	UPDATE owner
		SET [UpdDttm] = getdate()
		FROM inserted ins
		JOIN [CmObject] AS owned ON owned.[Id] = ins.[Id]
		JOIN [CmObject] AS owner ON owner.[Id] = owned.Owner$

	UPDATE grandowner
		SET [UpdDttm] = getdate()
		FROM inserted ins
		JOIN [CmObject] AS owned ON owned.[Id] = ins.[Id]
		JOIN [CmObject] AS owner ON owner.[Id] = owned.Owner$
		JOIN [CmObject] AS grandowner ON grandowner.[Id] = owner.Owner$

	SET @iErr = @@error
	IF @iErr <> 0 GOTO LFail

	IF @fIsNocountOn = 0 SET NOCOUNT OFF
	RETURN

LFail:
	ROLLBACK TRAN
	IF @fIsNocountOn = 0 SET NOCOUNT OFF
	Raiserror ('TR_StTxtPara_Owner_Ins: SQL Error %d; Unable to insert rows into the StTxtPara.', 16, 1, @iErr)
	RETURN
GO

IF object_id('TR_StTxtPara_Owner_UpdDel') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_StTxtPara_Owner_UpdDel'
	DROP TRIGGER [TR_StTxtPara_Owner_UpdDel]
END
GO
PRINT 'creating trigger TR_StTxtPara_Owner_UpdDel'
GO

/*== TR_StTxtPara_Owner_UpdDel ==*/

--( From LangProjSP.sql

CREATE TRIGGER [TR_StTxtPara_Owner_UpdDel] on [StTxtPara] FOR UPDATE, DELETE
AS
	DECLARE @iErr INT, @fIsNocountOn INT

	SET @fIsNocountOn = @@options & 512
	IF @fIsNocountOn = 0 SET NOCOUNT ON

	UPDATE owner
		SET [UpdDttm] = getdate()
		FROM deleted del
		JOIN [CmObject] AS owned ON owned.[Id] = del.[Id]
		JOIN [CmObject] AS owner ON owner.[Id] = owned.Owner$

	UPDATE grandowner
		SET [UpdDttm] = getdate()
		FROM deleted del
		JOIN [CmObject] AS owned ON owned.[Id] = del.[Id]
		JOIN [CmObject] AS owner ON owner.[Id] = owned.Owner$
		JOIN [CmObject] AS grandowner ON grandowner.[Id] = owner.Owner$

	SET @iErr = @@error
	IF @iErr <> 0 GOTO LFail

	IF @fIsNocountOn = 0 SET NOCOUNT OFF
	RETURN

LFail:
	ROLLBACK TRAN
	IF @fIsNocountOn = 0 SET NOCOUNT OFF
	Raiserror ('TR_StTxtPara_Owner_UpdDel: SQL Error %d; Unable to insert rows into the StTxtPara.', 16, 1, @iErr)
	RETURN
GO

/*== TR_StPara_Owner_Ins ==*/

--( From LangProjSP.sql

IF object_id('TR_StPara_Owner_Ins') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_StPara_Owner_Ins'
	DROP TRIGGER [TR_StPara_Owner_Ins]
END
GO
PRINT 'creating trigger TR_StPara_Owner_Ins'
GO

CREATE TRIGGER [TR_StPara_Owner_Ins] on [StPara] FOR INSERT
AS
	DECLARE @iErr INT, @fIsNocountOn INT

	SET @fIsNocountOn = @@options & 512
	IF @fIsNocountOn = 0 SET NOCOUNT ON

	UPDATE owner
		SET [UpdDttm] = getdate()
		FROM inserted ins
		JOIN [CmObject] AS owned ON owned.[Id] = ins.[Id]
		JOIN [CmObject] AS owner ON owner.[Id] = owned.Owner$

	UPDATE grandowner
		SET [UpdDttm] = getdate()
		FROM inserted ins
		JOIN [CmObject] AS owned ON owned.[Id] = ins.[Id]
		JOIN [CmObject] AS owner ON owner.[Id] = owned.Owner$
		JOIN [CmObject] AS grandowner ON grandowner.[Id] = owner.Owner$

	SET @iErr = @@error
	IF @iErr <> 0 GOTO LFail

	IF @fIsNocountOn = 0 SET NOCOUNT OFF
	RETURN

LFail:
	ROLLBACK TRAN
	IF @fIsNocountOn = 0 SET NOCOUNT OFF
	Raiserror ('TR_StPara_Owner_Ins: SQL Error %d; Unable to insert rows into the StPara.', 16, 1, @iErr)
	RETURN
go

/*== TR_StPara_Owner_UpdDel ==*/

--( From LangProjSP.sql

IF object_id('TR_StPara_Owner_UpdDel') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_StPara_Owner_UpdDel'
	DROP TRIGGER [TR_StPara_Owner_UpdDel]
END
GO
PRINT 'creating trigger TR_StPara_Owner_UpdDel'
GO

CREATE TRIGGER [TR_StPara_Owner_UpdDel] on [StPara] FOR UPDATE, DELETE
AS
	DECLARE @iErr INT, @fIsNocountOn INT

	SET @fIsNocountOn = @@options & 512
	IF @fIsNocountOn = 0 SET NOCOUNT ON

	UPDATE owner
		SET [UpdDttm] = getdate()
		FROM deleted del
		JOIN [CmObject] AS owned ON owned.[Id] = del.[Id]
		JOIN [CmObject] AS owner ON owner.[Id] = owned.Owner$

	UPDATE grandowner
		SET [UpdDttm] = getdate()
		FROM deleted del
		JOIN [CmObject] AS owned ON owned.[Id] = del.[Id]
		JOIN [CmObject] AS owner ON owner.[Id] = owned.Owner$
		JOIN [CmObject] AS grandowner ON grandowner.[Id] = owner.Owner$

	SET @iErr = @@error
	IF @iErr <> 0 GOTO LFail

	IF @fIsNocountOn = 0 SET NOCOUNT OFF
	RETURN

LFail:
	ROLLBACK TRAN
	IF @fIsNocountOn = 0 SET NOCOUNT OFF
	Raiserror ('TR_StPara_Owner_UpdDel: SQL Error %d; Unable to insert rows into the StPara.', 16, 1, @iErr)
	RETURN
go

/*== TR_StText_Owner_Ins ==*/

--( From LangProjSP.sql

IF object_id('TR_StText_Owner_Ins') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_StText_Owner_Ins'
	DROP TRIGGER [TR_StText_Owner_Ins]
END
GO
PRINT 'creating trigger TR_StText_Owner_Ins'
GO

CREATE TRIGGER [TR_StText_Owner_Ins] on [StText] FOR INSERT
AS
	DECLARE @iErr INT, @fIsNocountOn INT

	SET @fIsNocountOn = @@options & 512
	IF @fIsNocountOn = 0 SET NOCOUNT ON

	UPDATE owner
		SET [UpdDttm] = getdate()
		FROM inserted ins
		JOIN [CmObject] AS owned ON owned.[Id] = ins.[Id]
		JOIN [CmObject] AS owner ON owner.[Id] = owned.Owner$

	SET @iErr = @@error
	IF @iErr <> 0 GOTO LFail

	IF @fIsNocountOn = 0 SET NOCOUNT OFF
	RETURN

LFail:
	ROLLBACK TRAN
	IF @fIsNocountOn = 0 SET NOCOUNT OFF
	Raiserror ('TR_StText_Owner_Ins: SQL Error %d; Unable to insert rows into the StText.', 16, 1, @iErr)
	RETURN
go

/*== TR_StText_Owner_UpdDel ==*/

--( From LangProjSP.sql

IF object_id('TR_StText_Owner_UpdDel') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_StText_Owner_UpdDel'
	DROP TRIGGER [TR_StText_Owner_UpdDel]
END
GO
PRINT 'creating trigger TR_StText_Owner_UpdDel'
GO

CREATE TRIGGER [TR_StText_Owner_UpdDel] on [StText] FOR UPDATE, DELETE
AS
	DECLARE @iErr INT, @fIsNocountOn INT

	SET @fIsNocountOn = @@options & 512
	IF @fIsNocountOn = 0 SET NOCOUNT ON

	UPDATE owner
		SET [UpdDttm] = getdate()
		FROM deleted del
		JOIN [CmObject] AS owned ON owned.[Id] = del.[Id]
		JOIN [CmObject] AS owner ON owner.[Id] = owned.Owner$

	SET @iErr = @@error
	IF @iErr <> 0 GOTO LFail

	IF @fIsNocountOn = 0 SET NOCOUNT OFF
	RETURN

LFail:
	ROLLBACK TRAN
	IF @fIsNocountOn = 0 SET NOCOUNT OFF
	Raiserror ('TR_StText_Owner_UpdDel: SQL Error %d; Unable to insert rows into the StText.', 16, 1, @iErr)
	RETURN
go

/*== CopyPossibilityList$ ==*/

--( From FwCore.sql

IF object_id('CopyPossibilityList$') IS NOT NULL BEGIN
	PRINT 'removing proc CopyPossibilityList$'
	DROP TRIGGER [TR_StText_Owner_UpdDel]
END
GO
PRINT 'creating proc CopyPossibilityList$'
GO

CREATE PROCEDURE [CopyPossibilityList$]
	@iSrcObjId INT,
	@iNewObjId INT OUTPUT,
	@uidNewObjGuid UNIQUEIDENTIFIER OUTPUT
AS
	DECLARE @iOwnFlid INT, @iNewFlid INT
	DECLARE @nvcTxt NVARCHAR(4000)
	DECLARE @tiCopy TINYINT

	SELECT @iOwnFlid = [OwnFlid$] FROM CmObject WHERE [Id] = @iSrcObjid

	--( (SteveMi) The create custom field once went here. I'm told that custom
	--( custom stuff is ownerless.

	--( Find a unique name

	SELECT @nvcTxt = 'Copy of ' + [Txt]
	FROM MultiTxt$
	WHERE [Obj] = @iSrcObjId AND [Flid] = 5001 --( 5001 is Name flid

	SELECT @tiCopy = COUNT([Obj])
	FROM MultiTxt$
	WHERE [Flid] = 5001 AND [Txt] LIKE @nvcTxt + ' (%'

	SET @tiCopy = @tiCopy + 1

	--== Create Possibility List ==--

	DECLARE @iNameEnc INT, @nvcName NVARCHAR(4000)
	DECLARE @dtToday DATETIME
	DECLARE @iDescEnc INT, @nvcDesc NVARCHAR(4000), @vbFormat VARBINARY(8000)
	DECLARE @iDepth INT, @iPreventChoiceAboveLevel INT,
		@fIsSorted BIT, @fIsClosed BIT, @fPreventDuplicates BIT, @fPreventNodeChoices BIT,
		@nvcHelpFile NVARCHAR(4000),
		@fUseExtendedFields BIT,
		@tiDisplayOption TINYINT,
		@iItemClsId INT,
		@fIsVernacular BIT
	DECLARE @iAbbrevEnc INT, @nvcAbbrev NVARCHAR(4000)

	SELECT @iNameEnc = [Enc],
		@nvcName = 'Copy of ' + [Txt] + ' (' + RTRIM(CONVERT(CHAR(3), @tiCopy)) + ')'
	FROM MultiTxt$
	WHERE [Obj] = @iSrcObjId AND [Flid] = 5001  --( 5001 is Name flid

	SELECT	@iDescEnc = [Enc], @nvcDesc = [Txt], @vbFormat = [Fmt]
	FROM MultiBigStr$
	WHERE [Obj] = @iSrcObjId AND [Flid] = 5004 --( 5004 is Description flid

	SELECT
		@iDepth = [Depth],
		@iPreventChoiceAboveLevel = [PreventChoiceAboveLevel],
		@fIsSorted = [IsSorted],
		@fIsClosed = [IsClosed],
		@fPreventDuplicates = [PreventDuplicates],
		@fPreventNodeChoices = [PreventNodeChoices],
		@nvcHelpFile = [HelpFile],
		@fUseExtendedFields = [UseExtendedFields],
		@tiDisplayOption = [DisplayOption],
		@iItemClsId = [ItemClsId],
		@fIsVernacular = [IsVernacular]
	FROM CmPossibilityList
	WHERE [Id] = @iSrcObjId

	SELECT @iAbbrevEnc = [Enc],
		@nvcAbbrev = [Txt] + 'Copy' + RTRIM(CONVERT(CHAR(3), @tiCopy))
	FROM MultiTxt$
	WHERE [Obj] = @iSrcObjId AND [Flid] = 8010  --( 8010 is abbreviation flid

	SET @dtToday = GetDate()

	--( Create the possibility list
	EXEC CreateObject_CmPossibilityList
		@iNameEnc,	--( Name encoding
		@nvcName,	--( Name
		@dtToday,	--( Date created
		@dtToday,	--( Date modified
		@iDescEnc,	--( Description encoding
		@nvcDesc,	--( Description
		@vbFormat,	--( Description formatting
		@iDepth,
		@iPreventChoiceAboveLevel,
		@fIsSorted,
		@fIsClosed,
		@fPreventDuplicates,
		@fPreventNodeChoices,
		@iAbbrevEnc,	--( Abbreviation encoding
		@nvcAbbrev,	--( Abbreviation
		@nvcHelpFile,
		@fUseExtendedFields,
		@tiDisplayOption,
		@iItemClsId,
		@fIsVernacular,
		NULL,	--( Owner ID
		NULL,	--( Owning Flid
		NULL,	--( Start Obj
		@iNewObjId OUTPUT,
		@uidNewObjGuid OUTPUT,
		0,		--( Return Time Stamp
		NULL	--( Time Stamp

	--== Create Items ==--

	DECLARE @iPossId INT,
		@iSortSpec INT,
		@iConfidence INT,
		@iStatus INT,
		@nvcHelpId NVARCHAR(4000),
		@iForeColor INT,
		@iBackColor INT,
		@iUnderColor INT,
		@iUnderStyle INT,
		@fHidden BIT

	DECLARE	@iOwnOrd INT,
		@iPossNewId INT,
		@uidPossNewGuid UNIQUEIDENTIFIER

	DECLARE @iAliasEnc INT,
		@nvcAliasTxt NVARCHAR(4000) --( for Location and People

	--( Create Locations
	IF @iItemClsId = 12 --( 12 is Location
	BEGIN
		DECLARE curCmLocation CURSOR FOR
		SELECT
			p.[Id],
			p.[SortSpec],
			p.[Confidence],
			p.[Status],
			p.[HelpId],
			p.[ForeColor],
			p.[BackColor],
			p.[UnderColor],
			p.[UnderStyle],
			p.[Hidden]
		FROM CmPossibility p
		JOIN CmObject o ON p.[Id] = o.[Id]
		WHERE [Owner$] = @iSrcObjId
		ORDER BY o.OwnOrd$

		--( Loop through each of the locations
		OPEN curCmLocation
		FETCH NEXT FROM curCmLocation INTO
			@iPossId,
			@iSortSpec,
			@iConfidence,
			@iStatus,
			@nvcHelpId,
			@iForeColor,
			@iBackColor,
			@iUnderColor,
			@iUnderStyle,
			@fHidden
		WHILE @@FETCH_STATUS = 0
		BEGIN

			SELECT @iNameEnc = [Enc], @nvcName = [Txt]
			FROM MultiTxt$
			WHERE [Obj] = @iPossId AND [Flid] = 7001  --( 7001 is Name flid

			SELECT @iAbbrevEnc = [Enc], @nvcAbbrev = [Txt]
			FROM MultiTxt$
			WHERE [Obj] = @iPossId AND [Flid] = 7002  --( 7002 is abbreviation flid

			SELECT @iDescEnc = [Enc], @nvcDesc = [Txt], @vbFormat = [Fmt]
			FROM MultiBigStr$
			WHERE [Obj] = @iPossId AND [Flid] =  7003 --( 7003 is Description flid

			SELECT @iOwnFlid = [OwnFlid$], @iOwnOrd = [OwnOrd$]
			FROM CmObject
			WHERE [Id] = @iPossId

			SET @dtToday = GetDate()

			SELECT @iAliasEnc = [Enc], @nvcAliasTxt = [Txt]
			FROM MultiTxt$
			WHERE [Obj] = @iPossId AND [Flid] = 12001 --( 12001 is alias flid

			EXEC CreateObject_CmLocation
				@iNameEnc,
				@nvcName,
				@iAbbrevEnc,
				@nvcAbbrev,
				@iDescEnc,
				@nvcDesc,
				@vbFormat,
				@iSortSpec,
				@dtToday,
				@dtToday,
				@nvcHelpId,
				@iForeColor,
				@iBackColor,
				@iUnderColor,
				@iUnderStyle,
				@fHidden,
				@iAliasEnc,
				@nvcAliasTxt,
				@iNewObjId,
				@iOwnFlid,
				NULL,	--( Start Obj
				@iPossNewId OUTPUT,
				@uidPossNewGuid,
				0,	--( Return Time Stamp
				NULL	--( New Obj Time Stamp

			FETCH NEXT FROM curCmLocation INTO
				@iPossId,
				@iSortSpec,
				@iConfidence,
				@iStatus,
				@nvcHelpId,
				@iForeColor,
				@iBackColor,
				@iUnderColor,
				@iUnderStyle,
				@fHidden
		END

		CLOSE curCmLocation
		DEALLOCATE curCmLocation
	END

	--( Create Person
	ELSE IF @iItemClsId = 13 --( 13 is Person
	BEGIN
		DECLARE @tiGender TINYINT,
			@iDateOfBirth INT,
			@iPlaceOfBirth INT,
			@fIsResearcher BIT,
			@iEducation INT,
			@iDateOfDeath INT

		DECLARE curCmPerson CURSOR FOR
		SELECT
			poss.[Id],
			poss.[SortSpec],
			poss.[Confidence],
			poss.[Status],
			poss.[HelpId],
			poss.[ForeColor],
			poss.[BackColor],
			poss.[UnderColor],
			poss.[UnderStyle],
			poss.[Hidden],
			pers.[Gender],
			pers.[DateOfBirth],
			pers.[PlaceOfBirth],
			pers.[IsResearcher],
			pers.[Education],
			pers.[DateOfDeath]
		FROM CmPossibility poss
		JOIN CmObject o ON poss.[Id] = o.[Id]
		JOIN cmPerson pers ON pers.[Id] = poss.[Id]
		WHERE o.[Owner$] = @iSrcObjId
		ORDER BY o.OwnOrd$

		--( Loop through each of the persons
		OPEN curCmPerson
		FETCH NEXT FROM curCmPerson INTO
			@iPossId,
			@iSortSpec,
			@iConfidence,
			@iStatus,
			@nvcHelpId,
			@iForeColor,
			@iBackColor,
			@iUnderColor,
			@iUnderStyle,
			@fHidden,
			@tiGender,
			@iDateOfBirth,
			@iPlaceOfBirth,
			@fIsResearcher,
			@iEducation,
			@iDateOfDeath
		WHILE @@FETCH_STATUS = 0
		BEGIN
			SELECT @iNameEnc = [Enc], @nvcName = [Txt]
			FROM MultiTxt$
			WHERE [Obj] = @iPossId AND [Flid] = 7001  --( 7001 is Name flid

			SELECT @iAbbrevEnc = [Enc], @nvcAbbrev = [Txt]
			FROM MultiTxt$
			WHERE [Obj] = @iPossId AND [Flid] = 7002  --( 7002 is abbreviation flid

			SELECT @iDescEnc = [Enc], @nvcDesc = [Txt], @vbFormat = [Fmt]
			FROM MultiBigStr$
			WHERE [Obj] = @iPossId AND [Flid] = 7003 --( 7003 is Description flid

			SELECT @iOwnFlid = [OwnFlid$], @iOwnOrd = [OwnOrd$]
			FROM CmObject
			WHERE [Id] = @iPossId

			SELECT @iAliasEnc = [Enc], @nvcAliasTxt = [Txt]
			FROM MultiTxt$
			WHERE [Obj] = @iPossId AND [Flid] = 13001 --( 13001 is alias flid

			EXEC CreateObject_CmPerson
				@iNameEnc,
				@nvcName,
				@iAbbrevEnc,
				@nvcAbbrev,
				@iDescEnc,
				@nvcDesc,
				@vbFormat,
				@iSortSpec,
				@dtToday,
				@dtToday,
				@nvcHelpId,
				@iForeColor,
				@iBackColor,
				@iUnderColor,
				@iUnderStyle,
				@fHidden,
				@iAliasEnc,
				@nvcAliasTxt,
				@tiGender,
				@iDateOfBirth,
				--@iPlaceOfBirth,	--( NOT IN PROC
				@fIsResearcher,
				--@iEducation,		 --( NOT IN PROC
				@iDateOfDeath,
				@iNewObjId,
				@iOwnFlid,
				NULL,	--( Start Obj
				@iPossNewId OUTPUT,
				@uidPossNewGuid,
				0,	--( Return Time Stamp
				NULL	--( New Obj Time Stamp

			FETCH NEXT FROM curCmPerson INTO
				@iPossId,
				@iSortSpec,
				@iConfidence,
				@iStatus,
				@nvcHelpId,
				@iForeColor,
				@iBackColor,
				@iUnderColor,
				@iUnderStyle,
				@fHidden,
				@tiGender,
				@iDateOfBirth,
				@iPlaceOfBirth,
				@fIsResearcher,
				@iEducation,
				@iDateOfDeath
		END

		CLOSE curCmPerson
		DEALLOCATE curCmPerson
	END

	--( Create Possibility and other stuff
	ELSE
	BEGIN
		DECLARE curCmPossibility CURSOR FOR
		SELECT
			p.[Id],
			p.[SortSpec],
			p.[Confidence],
			p.[Status],
			p.[HelpId],
			p.[ForeColor],
			p.[BackColor],
			p.[UnderColor],
			p.[UnderStyle],
			p.[Hidden]
		FROM CmPossibility p
		JOIN CmObject o ON p.[Id] = o.[Id]
		WHERE [Owner$] = @iSrcObjId
		ORDER BY o.OwnOrd$

		--( Loop through each of the possibilities
		OPEN curCmPossibility
		FETCH NEXT FROM curCmPossibility INTO
			@iPossId,
			@iSortSpec,
			@iConfidence,
			@iStatus,
			@nvcHelpId,
			@iForeColor,
			@iBackColor,
			@iUnderColor,
			@iUnderStyle,
			@fHidden
		WHILE @@FETCH_STATUS = 0
		BEGIN

			SELECT @iNameEnc = [Enc], @nvcName = [Txt]
			FROM MultiTxt$
			WHERE [Obj] = @iPossId AND [Flid] = 7001  --( 7001 is Name flid

			SELECT @iAbbrevEnc = [Enc], @nvcAbbrev = [Txt]
			FROM MultiTxt$
			WHERE [Obj] = @iPossId AND [Flid] = 7002  --( 7002 is abbreviation flid

			SELECT @iDescEnc = [Enc], @nvcDesc = [Txt], @vbFormat = [Fmt]
			FROM MultiBigStr$
			WHERE [Obj] = @iPossId AND [Flid] = 7003 --( 7003 is Description flid

			SELECT @iOwnFlid = [OwnFlid$], @iOwnOrd = [OwnOrd$]
			FROM CmObject
			WHERE [Id] = @iPossId

			SET @dtToday = GetDate()

			EXEC CreateObject_CmPossibility
				@iNameEnc,
				@nvcName,
				@iAbbrevEnc,
				@nvcAbbrev,
				@iDescEnc,
				@nvcDesc,
				@vbFormat,
				@iSortSpec,
				@dtToday,
				@dtToday,
				@nvcHelpId,
				@iForeColor,
				@iBackColor,
				@iUnderColor,
				@iUnderStyle,
				@fHidden,
				@iNewObjId,
				@iOwnFlid,
				NULL,	--( Start Obj
				@iPossNewId,
				@uidPossNewGuid,
				0,	--( Return Time Stamp
				NULL	--( New Obj Time Stamp

			FETCH NEXT FROM curCmPossibility INTO
				@iPossId,
				@iSortSpec,
				@iConfidence,
				@iStatus,
				@nvcHelpId,
				@iForeColor,
				@iBackColor,
				@iUnderColor,
				@iUnderStyle,
				@fHidden
		END

		CLOSE curCmPossibility
		DEALLOCATE curCmPossibility

	END

go

---------------------------------------------------------------------
/*****************
** Data Updates **
*****************/

/*== LgEncoding ==*/

UPDATE LgEncoding SET [Locale] = 1033 WHERE [Locale] = 0

/*== LgWritingSystem ==*/

UPDATE LgWritingSystem SET [Locale] = le.[Locale]
FROM lgencoding le
	JOIN lgencoding_writingsystems lews ON lews.[Src] = le.[Id]
	JOIN lgwritingsystem lws ON lws.[Id] = lews.[Dst]
GO

/*== Load LgCollation ==*/

--( LgWritingSystem must be modified before this section is run.

--( This temp proc will be called from a loop, which follows.

CREATE PROCEDURE CreateLgCollation
	@iOwner INT,
	@iWinLocale INT
AS
	DECLARE @ti TINYINT
	DECLARE @nvcWinLoc NVARCHAR(100)
	DECLARE @nvcWinLocale NVARCHAR(100)
	DECLARE @nvcNameText NVARCHAR(100)

	--( Set the first part of the Windows Location Name
	SELECT @nvcWinLoc = CASE @iWinLocale
		WHEN 1033 THEN 'Latin1_General'
		WHEN 1034 THEN 'Traditional_Spanish'
		WHEN 1036 THEN 'French'
		WHEN 1040 THEN 'Latin1_General'
		WHEN 1078 THEN 'Latin1_General'
		WHEN 10250 THEN 'Modern_Spanish'
		END

	--( Create 4 language encodings for each writing system:
	--(    1. Default (neither case nor accent sensitive)
	--(    2. Case sensititve
	--(    3. Accent sensitive
	--(    4. Both case and accent sensitive

	SET @ti = 1
	WHILE @ti < 5
	BEGIN
		SELECT @nvcWinLocale = CASE @ti
			WHEN 1 THEN @nvcWinLoc + '_CI_AI'
			WHEN 2 THEN @nvcWinLoc + '_CS_AI'
			WHEN 3 THEN @nvcWinLoc + '_CI_AS'
			WHEN 4 THEN @nvcWinLoc + '_CS_AS'
			END

		SELECT @nvcNameText = CASE @ti
			WHEN 1 THEN 'Default Collation'
			WHEN 2 THEN 'Case Sensitive'
			WHEN 3 THEN 'Accent Sensitive'
			WHEN 4 THEN 'Case and Accent Sensitive'
			END

		EXEC CreateObject_LgCollation
			740664001, --( English encoding
			@nvcNameText, --( name txt
			@iWinLocale, --( Windows LCID
			@nvcWinLocale, --(Windows Collation
			NULL, NULL, --( ICU Resource Name and Text
			@ti, --( Collation_Code, to determine sensitivity
			@iOwner, --( Owner
			25016,	--(OwnFlid
			NULL, --( StartObj
			NULL, --( NewObjId
			NULL, --( NewObjGuid
			0, --(ReturnTimestamp
			NULL --(NewObjTimestamp

		SET @ti = @ti + 1
	END

GO

--( Create LgCollations for each of the Writing Systems

DECLARE curWritingSystems CURSOR FOR
	SELECT 	[Id], [Locale] FROM lgWritingSystem
DECLARE @iWsId INT, @iWsLocale INT
OPEN curWritingSystems
FETCH NEXT FROM curWritingSystems INTO @iWsId, @iWsLocale
WHILE @@FETCH_STATUS = 0
BEGIN
	EXEC CreateLgCollation @iWsId, @iWsLocale
	FETCH NEXT FROM curWritingSystems INTO @iWsId, @iWsLocale
END
CLOSE curWritingSystems
DEALLOCATE curWritingSystems

DROP PROCEDURE CreateLgCollation
GO

/*== Style Changes ==*/

UPDATE StStyle SET [Name] = 'Internal Link' WHERE [Name] = 'Link Characters'

--( Ken indicates that copying the
--(	binary information from the Rule column of another
--(	database like this could be wrong if the encoding
--(	is different. If so, it may need a C++ post-process.

DECLARE @iStyleOwner INT
SELECT TOP 1 @iStyleOwner = Owner$ FROM StStyle_
IF @@ROWCOUNT = 0
	SELECT @iStyleOwner = [Id] FROM CmObject WHERE [Class$] = 6001

EXEC CreateObject_StStyle
	'Added Text',	--( StStyle_Name
	1,		--( StStyle_Type
	0x00019C0236C1A2252C0000000000000300080000002F9060000A0000002F9060000500030003000000E9BD8B370000000000000300080000002F9060000A0000002F906000050003000300000059BE1E700000000000000300080000002F9060000A0000002F9060000500030003000000,		--( StStyle_Rules
--	0,		--( StStyle_IsPublishedTextStyle
--	0,		--( StStyle_IsBuiltIn
--	0,		--( StStyle_IsModified
	@iStyleOwner,	--( Owner
	6001023,	--( OwnFlid
	NULL,		--( StartObj
	NULL,		--( NewObjId
	NULL,		--( NewObjGuid
	0,		--( ReturnTimeStamp
	NULL		--( NewObjTimeStamp

EXEC CreateObject_StStyle
	'Deleted Text',	--( StStyle_Name
	1,		--( StStyle_Type
	0x00019C0236C1A2252C000000000000030008000000FF0000000A000000FF0000000500030001000000E9BD8B37000000000000030008000000FF0000000A000000FF000000050003000100000059BE1E70000000000000030008000000FF0000000A000000FF0000000500030001000000,		--( StStyle_Rules
--	0,		--( StStyle_IsPublishedTextStyle
--	0,		--( StStyle_IsBuiltIn
--	0,		--( StStyle_IsModified
	@iStyleOwner,	--( Owner
	6001023,	--( OwnFlid
	NULL,		--( StartObj
	NULL,		--( NewObjId
	NULL,		--( NewObjGuid
	0,		--( ReturnTimeStamp
	NULL		--( NewObjTimeStamp

EXEC CreateObject_StStyle
	'External Link', --( StStyle_Name
	1,		--( StStyle_Type
	0x00019C025AC1A2252C0000000000000300080000007F007F000A0000007F007F000500030003000000E9BD8B370000000000000300080000007F007F000A0000007F007F00050003000300000059BE1E700000000000000300080000007F007F000A0000007F007F00050003000300000079AA62980000000000000300080000007F007F000A0000007F007F000500030003000000E9D413B60000000000000300080000007F007F000A0000007F007F000500030003000000,		--( StStyle_Rules
--	0,		--( StStyle_IsPublishedTextStyle
--	0,		--( StStyle_IsBuiltIn
--	0,		--( StStyle_IsModified
	@iStyleOwner,	--( Owner
	6001023,	--( OwnFlid
	NULL,		--( StartObj
	NULL,		--( NewObjId
	NULL,		--( NewObjGuid
	0,		--( ReturnTimeStamp
	NULL		--( NewObjTimeStamp

EXEC CreateObject_StStyle
	'Language Code', --( StStyle_Name
	1,		--( StStyle_Type
	0x00019C020EC1A2252C000000000000020006000100401F0000080000002F60FF00,		--( StStyle_Rules
--	0,		--( StStyle_IsPublishedTextStyle
--	0,		--( StStyle_IsBuiltIn
--	0,		--( StStyle_IsModified
	@iStyleOwner,	--( Owner
	6001023,	--( OwnFlid
	NULL,		--( StartObj
	NULL,		--( NewObjId
	NULL,		--( NewObjGuid
	0,		--( ReturnTimeStamp
	NULL		--( NewObjTimeStamp

GO

/*== User View Field (in MultiTxt$) Changes ==*/

--( CmPossibility changes

--( 7001 kflidCmPossibility_Name
UPDATE MultiTxt$
SET [Txt] = '(single-line text field):  The official or commonly understood name or term used for the list item.'
WHERE [Flid] = 20002
	AND [Enc] = 740664001
	AND [Txt] = 'Longer, descriptive name for the list item.'

--( 7002 kflidCmPossibility_Abbreviation
UPDATE MultiTxt$
SET [Txt] = '(single-line text field):  A short identifier for the list item.  This is usually an official or commonly understood abbreviation such as a person''s initials, if available.'
WHERE [Flid] = 20002
	AND [Enc] = 740664001
	AND [Txt] = 'Short identifier (about 3 characters) for the list item.'

--( 7003 kflidCmPossibility_Description
UPDATE MultiTxt$
SET [Txt] = '(single-line text field):  A brief description of the list item.  The contents of this field can appear in the expanded Chooser dialog.'
WHERE [Flid] = 20002
	AND [Enc] = 740664001
	AND [Txt] = 'Prose description of the list item.'

--( 7006 kflidCmPossibility_SortSpec
--( This appears to be the same between earlier and later versions.

--( 7007 kflidCmPossibility_Restrictions
UPDATE MultiTxt$
SET [Txt] = '(list reference field):  Specifies any limitations on the distribution of information associated with this list item.'
WHERE [Flid] = 20002
	AND [Enc] = 740664001
	AND [Txt] = 'An indication of any limitations on the distribution of information associated with this list item.'

--( 7008 kflidCmPossibility_Confidence
UPDATE MultiTxt$
SET [Txt] = '(list reference field):  The reliability of the data in this list item.'
WHERE [Flid] = 20002
	AND [Enc] = 740664001
	AND [Txt] = 'An indication of the reliability of the data in this list item.'

--( 7010 kflidCmPossibility_DateCreated

UPDATE MultiTxt$
SET [Txt] = '(date field):  The date and time this list item was first created.'
WHERE [Flid] = 20002
	AND [Enc] = 740664001
	AND [Txt] = 'The date and time this list item was first created.'

--( 7011 kflidCmPossibility_DateModified
UPDATE MultiTxt$
SET [Txt] = '(date field):  The date and time this list item was last modified.'
WHERE [Flid] = 20002
	AND [Enc] = 740664001
	AND [Txt] = 'The date and time this list item was last modified.'

--( 7012 kflidCmPossibility_Discussion
UPDATE MultiTxt$
SET [Txt] = '(multiparagraph text field):  The main body of information about this list item.'
WHERE [Flid] = 20002
	AND [Enc] = 740664001
	AND [Txt] = 'Write-up of all that is known about this list item.'

--( 7013 kflidCmPossibility_Researchers
UPDATE MultiTxt$
SET [Txt] = '(list reference field):  The names of people who obtained, entered, or analyzed the information in this list item.'
WHERE [Flid] = 20002
	AND [Enc] = 740664001
	AND [Txt] = 'Names of people who provided information about this list item.'

--( 7014 kstidCmPossibility_HelpId
UPDATE MultiTxt$
SET [Txt] = '(single-line text field):  Used to associate a topics list item with a help file topic that describes it.  The help file is a compiled HTML file and is identified in the Topics List Properties dialog Writing Systems tab.'
WHERE [Flid] = 20002
	AND [Enc] = 740664001
	AND [Txt] = 'Id used to link to help file.'

--( CmPerson changes

--( 1279 kstidCmPerson_Name, flid 7001
UPDATE MultiTxt$
SET [Txt] = '(single-line text field):  The complete name of the person.  Whenever possible, enter names in a consistent pattern such as ''John P. Doe'' or ''Doe, John P.'''
WHERE [Flid] = 20002
	AND [Enc] = 740664001
	AND [Txt] = 'A person''s full name'

--( 1280 kstidCmPerson_Discussion, flid 7012
UPDATE MultiTxt$
SET [Txt] = '(multiparagraph text field):  Biographical information for the person.'
WHERE [Flid] = 20002
	AND [Enc] = 740664001
	AND [Txt] = 'Biographical information for the person.'

--( 1188 kstidCmPerson_Alias, flid 13001
UPDATE MultiTxt$
SET [Txt] = '(single-line text field):  The assumed or special-usage name of the person.'
FROM MultiTxt$ mt
JOIN UserViewField uvf ON mt.[Obj] = uvf.[Id]
WHERE mt.[Flid] = 20002
	AND mt.[Enc] = 740664001
	AND uvf.[Flid] = 13001

--( 1189 kstidCmPerson_Gender, flid 13003
UPDATE MultiTxt$
SET [Txt] = '(list reference field):  Gender (list reference field):  The sex of the person.'
FROM MultiTxt$ mt
JOIN UserViewField uvf ON mt.[Obj] = uvf.[Id]
WHERE mt.[Flid] = 20002
	AND mt.[Enc] = 740664001
	AND uvf.[Flid] = 13003

--( 1190 kstidCmPerson_DateOfBirth, flid 13004
UPDATE MultiTxt$
SET [Txt] = '(date field):  The date the person was born.'
FROM MultiTxt$ mt
JOIN UserViewField uvf ON mt.[Obj] = uvf.[Id]
WHERE mt.[Flid] = 20002
	AND mt.[Enc] = 740664001
	AND uvf.[Flid] = 13004

--( 1191 kstidCmPerson_PlaceOfBirth, flid 13006
UPDATE MultiTxt$
SET [Txt] = '(list reference field):  The location where the person was born.'
FROM MultiTxt$ mt
JOIN UserViewField uvf ON mt.[Obj] = uvf.[Id]
WHERE mt.[Flid] = 20002
	AND mt.[Enc] = 740664001
	AND uvf.[Flid] = 13006

--( 1192 kstidCmPerson_IsResearcher, flid 13008
UPDATE MultiTxt$
SET [Txt] = '(list reference field):  Specifies whether or not the person is a researcher contributing to this project.'
FROM MultiTxt$ mt
JOIN UserViewField uvf ON mt.[Obj] = uvf.[Id]
WHERE mt.[Flid] = 20002
	AND mt.[Enc] = 740664001
	AND uvf.[Flid] = 13008

--( 1193 kstidCmPerson_PlacesOfResidence, flid 13009
UPDATE MultiTxt$
SET [Txt] = '(list reference field):  The place where the person lives or calls home.'
FROM MultiTxt$ mt
JOIN UserViewField uvf ON mt.[Obj] = uvf.[Id]
WHERE mt.[Flid] = 20002
	AND mt.[Enc] = 740664001
	AND uvf.[Flid] = 13009

--( 1194 kstidCmPerson_Education, flid 13010
UPDATE MultiTxt$
SET [Txt] = '(list reference field):  The highest level of schooling completed by the person.'
FROM MultiTxt$ mt
JOIN UserViewField uvf ON mt.[Obj] = uvf.[Id]
WHERE mt.[Flid] = 20002
	AND mt.[Enc] = 740664001
	AND uvf.[Flid] = 13010

--( 1195 kstidCmPerson_DateOfDeath, flid 13011
UPDATE MultiTxt$
SET [Txt] = '(date field):  The date the person died.'
FROM MultiTxt$ mt
JOIN UserViewField uvf ON mt.[Obj] = uvf.[Id]
WHERE mt.[Flid] = 20002
	AND mt.[Enc] = 740664001
	AND uvf.[Flid] = 13011

--( 1196 kstidCmPerson_Positions, flid 13013
UPDATE MultiTxt$
SET [Txt] = '(list reference field):  The social position, job title, rank, religious office and other capabilities of the person.'
FROM MultiTxt$ mt
JOIN UserViewField uvf ON mt.[Obj] = uvf.[Id]
WHERE mt.[Flid] = 20002
	AND mt.[Enc] = 740664001
	AND uvf.[Flid] = 13013

--( CmLocation changes

--( 1281 kstidCmLocation_Name, flid 7001

UPDATE MultiTxt$
SET [Txt] = '(single-line text field):  Specifies a location by a place''s name or a unique descriptive phrase.  Enter a more detailed description in the Short Description field, and a complete discussion in the discussion field.'
FROM MultiTxt$ mt
JOIN UserViewField_ uvf ON mt.[Obj] = uvf.[Id]
JOIN UserViewRec uvr ON uvr.[Id] = uvf.[Owner$]
WHERE mt.[Flid] = 20002
	AND mt.[Enc] = 740664001
	AND uvf.[Flid] = 7001
	AND uvr.clsid = 12

--( 1197 kstidCmLocation_Alias, flid 12001
UPDATE MultiTxt$
SET [Txt] = '(single-line text field):  The assumed or special-usage name of the location.'
FROM MultiTxt$ mt
JOIN UserViewField uvf ON mt.[Obj] = uvf.[Id]
WHERE mt.[Flid] = 20002
	AND mt.[Enc] = 740664001
	AND uvf.[Flid] = 12001

GO

---------------------------------------------------------------------
/****************
** OCM changes **
*****************/

--== CreateObject_CmAnthroItemM5toM6 ==--

--( This is a wrapper sproc around CreateObject_CmAnthroItem,
--( the purpose of which is to provide common code for a worker
--( routine.

CREATE PROCEDURE CreateObject_CmAnthroItemM5toM6
	@nvcName NVARCHAR(4000),
	@nvcAbbreviation_Txt NVARCHAR(4000),
	@nvcHelpId NVARCHAR(4000),
	@nvcOwnerHelpId NVARCHAR(4000)
AS
	DECLARE @dToday DATETIME
	DECLARE @iId INT
	DECLARE @uidGuid UNIQUEIDENTIFIER
	DECLARE @iOwner INT
	DECLARE @iOwnFlid INT

	SET @dToday = GetDate()

	--( Get the Owner$ and OwnFlid$
	SELECT @iOwner = Owner$, @iOwnFlid = OwnFlid$
	FROM CmPossibility_
	WHERE HelpId = @nvcOwnerHelpId

	IF @@ROWCOUNT = 0
		PRINT @nvcOwnerHelpId + ' not found'

	--( Create the new object
	EXEC CreateObject_CmAnthroItem
		740664001, --( English encoding
		@nvcName, --( name
		740664001, --( English encoding
		@nvcAbbreviation_Txt, --( abbreviation
		NULL, NULL, NULL,  --( description stuff
		0, --( SortSpec
		@dToday, @dToday,
		@nvcHelpId, --( HelpId
		-1073741824, -1073741824, -1073741824, --( colors
		0, --( UnderStyle
		0, --( Hidden
		@iOwner, @iOwnFlid, --( Owner, OwnFlid
		NULL, --( StartObj: order; null = end of list
		@iId OUTPUT, --( NewObjId
		@uidGuid OUTPUT

GO

--== New and Updated Items ==--

--( 000

UPDATE CmPossibility
SET HelpId = 'MaterialNotRelevant'
WHERE HelpId = 'MaterialNotCategorized'

UPDATE MultiTxt$
SET Txt = 'Material Not Relevant'
WHERE Txt = 'Material Not Categorized'

--( 107

EXEC CreateObject_CmAnthroItemM5toM6
	'Diagnostic Material Attributes',
	'107',
	'DiagnosticMaterialAttributes',
	'Orientation'

--( 110

UPDATE CmPossibility
SET HelpId = 'InformationSources'
WHERE HelpId = 'Bibliography'

UPDATE MultiTxt$
SET Txt = 'Information Sources'
WHERE Txt = 'Bibliography'

UPDATE MultiTxt$
SET Txt = '110'
WHERE Txt = '11'

--( 111

UPDATE CmPossibility
SET HelpId = 'CitationsofDocumentsintheHRAFC'
WHERE HelpId = 'SourcesProcessed'

UPDATE MultiTxt$
SET Txt = 'Citations of Documents in the HRAF Collection'
WHERE Txt = 'Sources Processed'

--( 112

UPDATE CmPossibility
SET HelpId = 'AdditionalBibliography'
WHERE HelpId = 'SourcesConsulted'

UPDATE MultiTxt$
SET Txt = 'Additional Bibliography'
WHERE Txt = 'Sources Consulted'

--( 113

UPDATE CmPossibility
SET HelpId = 'InformationSourcesListedinOthe'
WHERE HelpId = 'AdditionalReferences'

UPDATE MultiTxt$
SET Txt = 'Information Sources Listed in Other Works'
WHERE Txt = 'Additional References'

--( 114

UPDATE CmPossibility
SET HelpId = 'ReviewsandCritiques'
WHERE HelpId = 'Comments'

UPDATE MultiTxt$
SET Txt = 'Reviews and Critiques'
WHERE Txt = 'Comments'

--( 116

UPDATE CmPossibility
SET HelpId = 'CompleteTextsofHRAFDocuments'
WHERE HelpId = 'Texts'

UPDATE MultiTxt$
SET Txt = 'Complete Texts of HRAF Documents'
WHERE Txt = 'Texts'

--( 119

EXEC CreateObject_CmAnthroItemM5toM6
	'Artifact and Archive Collections',
	'119',
	'ArtifactandArchiveCollections',
	'InformationSources'

--( 120

UPDATE CmPossibility
SET HelpId = 'ResearchMethods'
WHERE HelpId = 'Methodology'

UPDATE MultiTxt$
SET Txt = 'Research Methods'
WHERE Txt = 'Methodology'

UPDATE MultiTxt$
SET Txt = '120'
WHERE Txt = '12'

--( 121

UPDATE CmPossibility
SET HelpId = 'TheoreticalOrientationinResear'
WHERE HelpId = 'TheoreticalOrientation'

UPDATE MultiTxt$
SET Txt = 'Theoretical Orientation in Research and Its Results'
WHERE Txt = 'Theoretical Orientation'

--( 122

UPDATE CmPossibility
SET HelpId = 'PracticalPreparationsInConduct'
WHERE HelpId = 'PracticalPreparations'

UPDATE MultiTxt$
SET Txt = 'Practical Preparations in Conducting Fieldwork'
WHERE Txt = 'Practical Preparations'

--( 123

UPDATE CmPossibility
SET HelpId = 'ObservationalRoleInResearch'
WHERE HelpId = 'ObservationalRole'

UPDATE MultiTxt$
SET Txt = 'Observational Role in Research'
WHERE Txt = 'Observational Role'

--( 124

UPDATE CmPossibility
SET HelpId = 'InterviewingInResearch'
WHERE HelpId = 'Interviewing'

UPDATE MultiTxt$
SET Txt = 'Interviewing in Research'
WHERE Txt = 'Interviewing'

--( 125

UPDATE CmPossibility
SET HelpId = 'TestsandSchdulesAdministeredI'
WHERE HelpId = 'TestsandSchedules'

UPDATE MultiTxt$
SET Txt = 'Tests and Schedules Administered in the Field'
WHERE Txt = 'Tests and Schedules'

--( 126

UPDATE CmPossibility
SET HelpId = 'RecordingandCollectingInTheFie'
WHERE HelpId = 'RecordingandCollecting'

UPDATE MultiTxt$
SET Txt = 'Recording and Collecting in the Field'
WHERE Txt = 'Recording and Collecting'

--( 127

UPDATE CmPossibility
SET HelpId = 'Historical and Achival Research'
WHERE HelpId = 'HistoricalResearch'

UPDATE MultiTxt$
SET Txt = 'Historical and Archival Research'
WHERE Txt = 'Historical Research'

--( 129

EXEC CreateObject_CmAnthroItemM5toM6
	'Archaeological Survey Methods',
	'129',
	'ArchaeologicalSurveyMethods',
	'ResearchMethods'

--( 1210

EXEC CreateObject_CmAnthroItemM5toM6
	'Archaeological Excavation  Methods',
	'1210',
	'ArchaeologicalExcavationMethod',
	'ResearchMethods'

--( 1211

EXEC CreateObject_CmAnthroItemM5toM6
	'Dating Methods in Archaeology',
	'1211',
	'DatingMethodsInArchaeology',
	'ResearchMethods'

--( 1212

EXEC CreateObject_CmAnthroItemM5toM6
	'Laboratory Analysis of Materials other than Dating Methods in Archaeology',
	'1212',
	'LaboratoryAnalysisOfMaterialsO',
	'ResearchMethods'

--( 1213

EXEC CreateObject_CmAnthroItemM5toM6
	'Comparative Data',
	'1213',
	'ComparativeData',
	'ResearchMethods'

--( 138

EXEC CreateObject_CmAnthroItemM5toM6
	'Post Depositional Processes in Archaeological Site',
	'138',
	'PostDepositionalProcessesInArc',
	'Geography'

--( 140

UPDATE MultiTxt$
SET Txt = '140'
WHERE Txt = '14'

--( 144

UPDATE CmPossibility
SET HelpId = 'RacialIdentification'
WHERE HelpId = 'RacialAffinities'

UPDATE MultiTxt$
SET Txt = 'Racial Identification'
WHERE Txt = 'Racial Affinities'

--( 150

UPDATE MultiTxt$
SET Txt = '150'
WHERE Txt = '15'

--( 160

UPDATE MultiTxt$
SET Txt = '160'
WHERE Txt = '16'

--( 167

UPDATE CmPossibility
SET HelpId = 'ExternalMigration'
WHERE HelpId = 'ImmigrationandEmigration'

UPDATE MultiTxt$
SET Txt = 'External Migration'
WHERE Txt = 'Immigration and Emigration'

--( 170

UPDATE MultiTxt$
SET Txt = '170'
WHERE Txt = '17'

--( 171

UPDATE CmPossibility
SET HelpId = 'ComparativeEvidence'
WHERE HelpId = 'DistributionalEvidence'

UPDATE MultiTxt$
SET Txt = 'Comparative Evidence'
WHERE Txt = 'Distributional Evidence'

--( 172

UPDATE CmPossibility
SET HelpId = 'Prehistory'
WHERE HelpId = 'Archeology'

UPDATE MultiTxt$
SET Txt = 'Prehistory'
WHERE Txt = 'Archeology'

--( 175

UPDATE CmPossibility
SET HelpId = 'History'
WHERE HelpId = 'RecordedHistory'

UPDATE MultiTxt$
SET Txt = 'History'
WHERE Txt = 'Recorded History'

--( 178

UPDATE CmPossibility
SET HelpId = 'SocioculturalTrends'
WHERE HelpId = 'SocioCulturalTrends'

UPDATE MultiTxt$
SET Txt = 'Sociocultural Trends'
WHERE Txt = 'Socio-Cultural Trends'

--( 1710

EXEC CreateObject_CmAnthroItemM5toM6
	'Cultural Revitalization and Ethnogenesis',
	'1710',
	'CulturalRevitalizationAndEthno',
	'Geography'

--( 180

UPDATE MultiTxt$
SET Txt = '180'
WHERE Txt = '18'

--( 182

UPDATE CmPossibility
SET HelpId = 'FunctionalAndAdaptationalInter'
WHERE HelpId = 'Function'

UPDATE MultiTxt$
SET Txt = 'Functional and Adaptational Interpretations'
WHERE Txt = 'Function'

--( 186

UPDATE CmPossibility
SET HelpId = 'CulturalIdentityAndPride'
WHERE HelpId = 'Ethnocentrism'

UPDATE MultiTxt$
SET Txt = 'Cultural Identity and Pride'
WHERE Txt = 'Ethnocentrism'

--( 190

UPDATE MultiTxt$
SET Txt = '190'
WHERE Txt = '19'

--( 195

UPDATE CmPossibility
SET HelpId = 'Sociolinguistics'
WHERE HelpId = 'Stylistics'

UPDATE MultiTxt$
SET Txt = 'Sociolinguistics'
WHERE Txt = 'Stylistics'

--( 200

UPDATE MultiTxt$
SET Txt = '200'
WHERE Txt = '20'

--( 205

UPDATE CmPossibility
SET HelpId = 'Mail'
WHERE HelpId = 'PostalSystem'

UPDATE MultiTxt$
SET Txt = 'Mail'
WHERE Txt = 'Postal System'

--( 2010

EXEC CreateObject_CmAnthroItemM5toM6
	'Internet Communications',
	'2010',
	'InternetCommunications',
	'Communication'

--( 210

UPDATE MultiTxt$
SET Txt = '210'
WHERE Txt = '21'

--( 220

UPDATE MultiTxt$
SET Txt = '220'
WHERE Txt = '22'

--( 230

UPDATE MultiTxt$
SET Txt = '230'
WHERE Txt = '23'

--( 240

UPDATE MultiTxt$
SET Txt = '240'
WHERE Txt = '24'

--( 250

UPDATE MultiTxt$
SET Txt = '250'
WHERE Txt = '25'

--( 260

UPDATE MultiTxt$
SET Txt = '260'
WHERE Txt = '26'

--( 270

UPDATE CmPossibility
SET HelpId = 'DrinkAndDrugs'
WHERE HelpId = 'DrinkDrugsandIndulgence'

UPDATE MultiTxt$
SET Txt = 'Drink and Drugs'
WHERE Txt = 'Drink, Drugs, and Indulgence'

UPDATE MultiTxt$
SET Txt = '270'
WHERE Txt = '27'

--( 276

UPDATE CmPossibility
SET HelpId = 'RecreationalAndNontherapeuticD'
WHERE HelpId = 'NarcoticsandStimulants'

UPDATE MultiTxt$
SET Txt = 'Recreational and Non-therapeutic Drugs'
WHERE Txt = 'Narcotics and Stimulants'

--( 280

UPDATE MultiTxt$
SET Txt = '280'
WHERE Txt = '28'

--( 290

UPDATE MultiTxt$
SET Txt = '290'
WHERE Txt = '29'

--( 300

UPDATE MultiTxt$
SET Txt = '300'
WHERE Txt = '30'

--( 304

UPDATE CmPossibility
SET HelpId = 'BodyAlterations'
WHERE HelpId = 'Mutilation'

UPDATE MultiTxt$
SET Txt = 'Mail'
WHERE Txt = 'Body Alterations'

--( 310

UPDATE MultiTxt$
SET Txt = '310'
WHERE Txt = '31'

--( 320

UPDATE MultiTxt$
SET Txt = '320'
WHERE Txt = '32'

--( 330

UPDATE MultiTxt$
SET Txt = '330'
WHERE Txt = '33'

--( 340

UPDATE MultiTxt$
SET Txt = '340'
WHERE Txt = '34'

--( 350

UPDATE MultiTxt$
SET Txt = '350'
WHERE Txt = '35'

--( 360

UPDATE MultiTxt$
SET Txt = '360'
WHERE Txt = '36'

--( 364

UPDATE CmPossibility
SET HelpId = 'RefuseDisposalAndSanitaryFacil'
WHERE HelpId = 'SanitaryFacilities'

UPDATE MultiTxt$
SET Txt = 'Mail'
WHERE Txt = 'Refuse Disposal and Sanitary Facillities'

--( 370

UPDATE MultiTxt$
SET Txt = '370'
WHERE Txt = '37'

--( 380

UPDATE MultiTxt$
SET Txt = '380'
WHERE Txt = '38'

--( 390

UPDATE MultiTxt$
SET Txt = '390'
WHERE Txt = '39'

--( 400

UPDATE MultiTxt$
SET Txt = '400'
WHERE Txt = '40'

--( 408

EXEC CreateObject_CmAnthroItemM5toM6
	'Computer Technology',
	'408',
	'ComputerTechnology',
	'GCGeneralCultureandChange'

--( 410

UPDATE MultiTxt$
SET Txt = '410'
WHERE Txt = '41'

--( 420

UPDATE MultiTxt$
SET Txt = '420'
WHERE Txt = '42'

--( 430

UPDATE MultiTxt$
SET Txt = '430'
WHERE Txt = '43'

--( 440

UPDATE MultiTxt$
SET Txt = '440'
WHERE Txt = '44'

--( 450

UPDATE MultiTxt$
SET Txt = '450'
WHERE Txt = '45'

--( 460

UPDATE MultiTxt$
SET Txt = '460'
WHERE Txt = '46'

--( 462

UPDATE CmPossibility
SET HelpId = 'DivisionofLaborByGender'
WHERE HelpId = 'DivisionofLaborbySex'

UPDATE MultiTxt$
SET Txt = 'Division of Labor by Gender'
WHERE Txt = 'Division of Labor by Sex'

--( 470

UPDATE MultiTxt$
SET Txt = '470'
WHERE Txt = '47'

--( 480

UPDATE MultiTxt$
SET Txt = '480'
WHERE Txt = '48'

--( 490

UPDATE MultiTxt$
SET Txt = '490'
WHERE Txt = '49'

--( 500

UPDATE MultiTxt$
SET Txt = '500'
WHERE Txt = '50'

--( 510

UPDATE MultiTxt$
SET Txt = '510'
WHERE Txt = '51'

--( 520

UPDATE MultiTxt$
SET Txt = '520'
WHERE Txt = '52'

--( 530

UPDATE CmPossibility
SET HelpId = 'Arts'
WHERE HelpId = 'FineArts'

UPDATE MultiTxt$
SET Txt = 'Arts'
WHERE Txt = 'Fine Arts'

UPDATE MultiTxt$
SET Txt = '530'
WHERE Txt = '53'

--( 535

UPDATE CmPossibility
SET HelpId = 'Dance'
WHERE HelpId = 'Dancing'

UPDATE MultiTxt$
SET Txt = 'Dance'
WHERE Txt = 'Dancing'

--( 5310

EXEC CreateObject_CmAnthroItemM5toM6
	'Verbal Arts',
	'5310',
	'VerbalArts',
	'Arts'

--( 5311

EXEC CreateObject_CmAnthroItemM5toM6
	'Visual Arts',
	'5311',
	'VisualArts',
	'Arts'

--( 540

UPDATE CmPossibility
SET HelpId = 'CommercializedEntertainment'
WHERE HelpId = 'Entertainment'

UPDATE MultiTxt$
SET Txt = 'Commercialized Entertainment'
WHERE Txt = 'Entertainment'

UPDATE MultiTxt$
SET Txt = '540'
WHERE Txt = '54'

--( 548

UPDATE CmPossibility
SET HelpId = 'IllegalEntertainment'
WHERE HelpId = 'OrganizedVice'

UPDATE MultiTxt$
SET Txt = 'Illegal Entertainment'
WHERE Txt = 'Organized Vice'

--( 550

UPDATE MultiTxt$
SET Txt = '550'
WHERE Txt = '55'

--( 560

UPDATE MultiTxt$
SET Txt = '560'
WHERE Txt = '56'

--( 562

UPDATE CmPossibility
SET HelpId = 'GenderStatus'
WHERE HelpId = 'SexStatus'

UPDATE MultiTxt$
SET Txt = 'Gender Status'
WHERE Txt = 'Sex Status'

--( 570

UPDATE MultiTxt$
SET Txt = '570'
WHERE Txt = '57'

--( 580

UPDATE MultiTxt$
SET Txt = '580'
WHERE Txt = '58'

--( 588

UPDATE CmPossibility
SET HelpId = 'SpecialUnionsAndMarriages'
WHERE HelpId = 'IrregularUnions'

UPDATE MultiTxt$
SET Txt = 'Special Unions and Marriages'
WHERE Txt = 'Irregular Unions'

--( 590

UPDATE MultiTxt$
SET Txt = '590'
WHERE Txt = '59'

--( 600

UPDATE MultiTxt$
SET Txt = '600'
WHERE Txt = '60'

--( 610

UPDATE MultiTxt$
SET Txt = '610'
WHERE Txt = '61'

--( 620

UPDATE MultiTxt$
SET Txt = '620'
WHERE Txt = '62'

--( 622

UPDATE CmPossibility
SET HelpId = 'CommunityHeads'
WHERE HelpId = 'Headmen'

UPDATE MultiTxt$
SET Txt = 'Community Heads'
WHERE Txt = 'Headmen'

--( 629

EXEC CreateObject_CmAnthroItemM5toM6
	'Inter-ethnic Relations',
	'629',
	'InterEthnicRelations',
	'SLSocialLifeandIdentity'

--( 630

UPDATE MultiTxt$
SET Txt = '630'
WHERE Txt = '63'

--( 640

UPDATE MultiTxt$
SET Txt = '640'
WHERE Txt = '64'

--( 650

UPDATE MultiTxt$
SET Txt = '650'
WHERE Txt = '65'

--( 660

UPDATE MultiTxt$
SET Txt = '660'
WHERE Txt = '66'

--( 670

UPDATE MultiTxt$
SET Txt = '670'
WHERE Txt = '67'

--( 677

EXEC CreateObject_CmAnthroItemM5toM6
	'Organized Crime',
	'677',
	'OrganizedCrime',
	'Law'

--( 680

UPDATE MultiTxt$
SET Txt = '680'
WHERE Txt = '68'

--( 687

UPDATE CmPossibility
SET HelpId = 'OffensesAgainstTheState'
WHERE HelpId = 'OffensesagainsttheState'

UPDATE MultiTxt$
SET Txt = 'Offenses Against the State'
WHERE Txt = 'Offenses against the State'

--( 690

UPDATE MultiTxt$
SET Txt = '690'
WHERE Txt = '69'

--( 700

UPDATE MultiTxt$
SET Txt = '700'
WHERE Txt = '70'

--( 710

UPDATE MultiTxt$
SET Txt = '710'
WHERE Txt = '71'

--( 720

UPDATE MultiTxt$
SET Txt = '720'
WHERE Txt = '72'

--( 730

UPDATE MultiTxt$
SET Txt = '730'
WHERE Txt = '73'

--( 732

UPDATE CmPossibility
SET HelpId = 'Disabilities'
WHERE HelpId = 'Handicapped'

UPDATE MultiTxt$
SET Txt = 'Disabilities'
WHERE Txt = 'Handicapped'

--( 740

UPDATE MultiTxt$
SET Txt = '740'
WHERE Txt = '74'

--( 750

UPDATE MultiTxt$
SET Txt = '750'
WHERE Txt = '75'

--( 756

UPDATE CmPossibility
SET HelpId = 'ShamansAndPsychotherapists'
WHERE HelpId = 'Psychotherapists'

UPDATE MultiTxt$
SET Txt = 'Shamans and Psychotherapists'
WHERE Txt = 'Psychotherapists'

--( 760

UPDATE MultiTxt$
SET Txt = '760'
WHERE Txt = '76'

--( 770

UPDATE MultiTxt$
SET Txt = '770'
WHERE Txt = '77'

--( 780

UPDATE MultiTxt$
SET Txt = '780'
WHERE Txt = '78'

--( 783

UPDATE CmPossibility
SET HelpId = 'PurificationandAtonement'
WHERE HelpId = 'PurificationandExpiation'

UPDATE MultiTxt$
SET Txt = 'Purification and Atonement'
WHERE Txt = 'Purification and Expiation'

--( 786

UPDATE CmPossibility
SET HelpId = 'EcstaticReligiousPractices'
WHERE HelpId = 'Orgies'

UPDATE MultiTxt$
SET Txt = 'Ecstatic Religious Practices'
WHERE Txt = 'Orgies'

--( 790

UPDATE MultiTxt$
SET Txt = '790'
WHERE Txt = '79'

--( 792

UPDATE CmPossibility
SET HelpId = 'ProphetsAndAscetics'
WHERE HelpId = 'HolyMen'

UPDATE MultiTxt$
SET Txt = 'Prophets and Ascetics'
WHERE Txt = 'Holy Men'

--( 795

UPDATE CmPossibility
SET HelpId = 'ReligiousDenominations'
WHERE HelpId = 'Sects'

UPDATE MultiTxt$
SET Txt = 'Religious Denominations'
WHERE Txt = 'Sects'

--( 800

UPDATE MultiTxt$
SET Txt = '800'
WHERE Txt = '80'

--( 810

UPDATE CmPossibility
SET HelpId = 'SciencesAndHumanities'
WHERE HelpId = 'ExactKnowledge'

UPDATE MultiTxt$
SET Txt = 'Sciences and Humanities'
WHERE Txt = 'Exact Knowledge'

UPDATE MultiTxt$
SET Txt = '810'
WHERE Txt = '81'

--( 815

UPDATE CmPossibility
SET HelpId = 'Science'
WHERE HelpId = 'PureScience'

UPDATE MultiTxt$
SET Txt = 'Science'
WHERE Txt = 'Pure Science'

--( 820

UPDATE MultiTxt$
SET Txt = '820'
WHERE Txt = '82'

--( 830

UPDATE MultiTxt$
SET Txt = '830'
WHERE Txt = '83'

--( 840

UPDATE MultiTxt$
SET Txt = '840'
WHERE Txt = '84'

--( 850

UPDATE MultiTxt$
SET Txt = '850'
WHERE Txt = '85'

--( 860

UPDATE MultiTxt$
SET Txt = '860'
WHERE Txt = '86'

--( 870

UPDATE MultiTxt$
SET Txt = '870'
WHERE Txt = '87'

--( 880

UPDATE MultiTxt$
SET Txt = '880'
WHERE Txt = '88'

--( 890

EXEC CreateObject_CmAnthroItemM5toM6
	'Gender Roles and Issues',
	'690',
	'GenderRolesandIssues',
	'LCLifeCycle'

--( 900

EXEC CreateObject_CmAnthroItemM5toM6
	'Texts',
	'900',
	'Texts',
	'InformationSources'

--( 901

EXEC CreateObject_CmAnthroItemM5toM6
	'Texts in the Speaker''s Language',
	'901',
	'TextsInTheSpeakersLanguage',
	'Texts'

--( 902

EXEC CreateObject_CmAnthroItemM5toM6
	'Texts Translated into English',
	'902',
	'TextsTranslatedIntoEnglish',
	'Texts'

--( 903

EXEC CreateObject_CmAnthroItemM5toM6
	'Interlinear Translations',
	'903',
	'InterlinearTranslations',
	'Texts'

--( TX

DECLARE @dToday DATETIME
DECLARE @iId INT
DECLARE @uidGuid UNIQUEIDENTIFIER

SET @dToday = GetDate()

DECLARE @iAnthroListId INT
SELECT @iAnthroListId = Dst FROM LanguageProject_AnthroList

EXEC CreateObject_CmAnthroItem
	740664001,
	'Texts and Other Categories',
	740664001,
	'TX',
	NULL, NULL, NULL,
	0,
	@dToday, @dToday,
	'TXTextsAndOtherCategories',
	-1073741824, -1073741824, -1073741824,
	0,
	0,
	@iAnthroListId, 8008, --( Owner, OwnFlid
	NULL, --( StartObj: order; null = end of list
	@iId OUTPUT, --( NewObjId
	@uidGuid OUTPUT
GO

--( 910

EXEC CreateObject_CmAnthroItemM5toM6
	'Archaeological Measures, Techniques, and Analyses',
	'910',
	'ArchaeologicalMeasuresTechniqu',
	'TXTextsAndOtherCategories'

--( 911

EXEC CreateObject_CmAnthroItemM5toM6
	'Chronologies and Culture Sequences',
	'911',
	'ChronologiesAndCultureSequence',
	'ArchaeologicalMeasuresTechniqu'

--( 912

EXEC CreateObject_CmAnthroItemM5toM6
	'Cultural Stratigraphy',
	'912',
	'CulturalStratigraphy',
	'ArchaeologicalMeasuresTechniqu'

--( 913

EXEC CreateObject_CmAnthroItemM5toM6
	'Functional Specialization Areas',
	'913',
	'FunctionalSpecializationAreas',
	'ArchaeologicalMeasuresTechniqu'

--( 914

EXEC CreateObject_CmAnthroItemM5toM6
	'Typologies and Classifications',
	'914',
	'TypologiesAndClassifications',
	'ArchaeologicalMeasuresTechniqu'

--( 915

EXEC CreateObject_CmAnthroItemM5toM6
	'Archaeological Inventories',
	'915',
	'ArchaeologicalInventories',
	'ArchaeologicalMeasuresTechniqu'

DROP PROCEDURE CreateObject_CmAnthroItemM5toM6

/************
** Cleanup **
************/

DECLARE	@fIsNocountOn INT
SELECT @fIsNocountOn = iIsOn FROM #tblIsNoCountOn
IF @fIsNocountOn = 0 SET NOCOUNT OFF
DROP TABLE #tblIsNoCountOn