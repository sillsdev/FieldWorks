-- Update database from version 200088 to 200089
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

/***********************************************************************************************
 * DefineCreateProc$
 *
 * Description:
 *	This procedure creates the CreateObject_... procedure for a given class.
 *
 * Paramters:
 *	@clid=class Id of the class related to the new procedure
 *
 * Returns:
 * 	0 if successful, otherwise an error
 **********************************************************************************************/

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
		@sDynSQL4 nvarchar(4000), @sDynSQL5 nvarchar(4000), @sDynSQLParamList nvarchar(4000),
		@sDynSQLCustomParamList NVARCHAR(4000)
	declare @sValuesList nvarchar(1000)
	declare @fAbs tinyint, @sClass sysname, @sProcName sysname
	declare @sFieldName sysname, @nFieldType int, @flid int,
		@sFieldList nvarchar(4000), @sXMLTableDef nvarchar(4000)
	declare @sInheritClassName sysname, @nInheritClid int
	DECLARE @nCustom TINYINT

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
	SET @sDynSQLCustomParamList = N''

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
		select	[Name], [Type], [Id], Custom
		from	[Field$]
		where	[Class] = @nInheritClid
			and [Name] <> 'Id'
			-- do not include MultiString type columns nor relationship, e.g. reference sequence,
			--	type columns because these are all stored in tables external to the actual
			--	class table
			and [Type] not in (23, 24, 25, 26, 27, 28)
		--( We want to put custom fields last, so that the parameter list doesn't get
		--( reshuffled when a custom field gets added to a super or base class. That
		--( would throw off calling programs.
		order by Custom, [Id]

		open curFieldList
		FETCH curFieldList INTO @sFieldName, @nFieldType, @flid, @nCustom

		set @sFieldList = N''
		set @sXMLTableDef = N''
		while @@fetch_status = 0 begin
			if @nFieldType = 14 begin -- MultiStr$
				IF @nCustom = 0
					set @sDynSQLParamList = @sDynSQLParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt nvarchar(4000) = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt varbinary(8000) = null' + N', ' + CHAR(13) + CHAR(10)
				ELSE -- IF @nCustom = 1
					set @sDynSQLCustomParamList = @sDynSQLCustomParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt nvarchar(4000) = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt varbinary(8000) = null' + N', ' + CHAR(13) + CHAR(10)

				set @sDynSQL3 = @sDynSQL3 + char(9) +
					N'if @' + @sInheritClassName + N'_' + @sFieldName + N'_txt is not null begin' + CHAR(13) + CHAR(10) +
					char(9) + char(9) + N'insert into [MultiStr$] with (rowlock) ([Flid],[Obj],[Ws],[Txt],[Fmt]) ' + CHAR(13) + CHAR(10) +
					char(9) + char(9) + N'values (' + convert(nvarchar(11), @flid)+ N',@ObjId,' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws' + N',' +
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
				IF @nCustom = 0
					set @sDynSQLParamList = @sDynSQLParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt nvarchar(4000) = null' + N', ' + CHAR(13) + CHAR(10)
				ELSE -- IF @nCustom = 1
					set @sDynSQLCustomParamList = @sDynSQLCustomParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt nvarchar(4000) = null' + N', ' + CHAR(13) + CHAR(10)

				set @sDynSQL3 = @sDynSQL3 + char(9) +
					N'if @' + @sInheritClassName + N'_' + @sFieldName + N'_txt is not null begin' + CHAR(13) + CHAR(10)
				SET @sDynSQL3 = @sDynSQL3 + CHAR(9) + CHAR(9) +
					N'INSERT INTO ' + @sInheritClassName + N'_' + @sFieldName +
					N' WITH (ROWLOCK) (Obj, Ws, Txt)' + CHAR(13) + CHAR(10) +
					char(9) + char(9) + N'values (@ObjId,' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws' + ',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt)'
				set @sDynSQL3 = @sDynSQL3 +
N'
		set @Err = @@error
		if @Err <> 0 goto LCleanUp
	end
'
			end
			else if @nFieldType = 18 begin -- MultiBigStr$
				IF @nCustom = 0
					set @sDynSQLParamList = @sDynSQLParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt ntext = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt image = null' + N', ' + CHAR(13) + CHAR(10)
				ELSE -- IF @nCustom = 1
					set @sDynSQLCustomParamList = @sDynSQLCustomParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt ntext = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt image = null' + N', ' + CHAR(13) + CHAR(10)

				set @sDynSQL3 = @sDynSQL3 + char(9) +
					N'if @' + @sInheritClassName + N'_' + @sFieldName + N'_txt is not null begin' + CHAR(13) + CHAR(10) +
					char(9) + char(9) + N'insert into [MultiBigStr$] with (rowlock) ([Flid],[Obj],[Ws],[Txt],[Fmt])' + CHAR(13) + CHAR(10) +
					char(9) + char(9) + N'values (' + convert(nvarchar(11), @flid)+ N',@ObjId,' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws' + N',' +
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
				IF @nCustom = 0
					set @sDynSQLParamList = @sDynSQLParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt ntext = null' + N', ' + CHAR(13) + CHAR(10)
				ELSE -- IF @nCustom = 1
					set @sDynSQLCustomParamList = @sDynSQLCustomParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt ntext = null' + N', ' + CHAR(13) + CHAR(10)

				set @sDynSQL3 = @sDynSQL3 + char(9) +
					N'if @' + @sInheritClassName + N'_' + @sFieldName + N'_txt is not null begin' + CHAR(13) + CHAR(10) +
					char(9) + char(9) + N'insert into [MultiBigTxt$] with (rowlock) ([Flid],[Obj],[Ws],[Txt])' + CHAR(13) + CHAR(10) +
					char(9) + char(9) + N'values (' + convert(nvarchar(11), @flid)+ N',@ObjId,' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws' + N',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt)'
				set @sDynSQL3 = @sDynSQL3 +
N'
		set @Err = @@error
		if @Err <> 0 goto LCleanUp
	end
'
			end
			else begin
				IF @nCustom = 0
					set @sDynSQLParamList = @sDynSQLParamList + char(9) +
						N'@' + @sInheritClassName + '_' + @sFieldName + ' ' +
						dbo.fnGetColumnDef$(@nFieldType) + N',' + CHAR(13) + CHAR(10)
				ELSE -- IF @nCustom = 1
					set @sDynSQLCustomParamList = @sDynSQLCustomParamList + char(9) +
						N'@' + @sInheritClassName + '_' + @sFieldName + ' ' +
						dbo.fnGetColumnDef$(@nFieldType) + N',' + CHAR(13) + CHAR(10)

				set @sFieldList = @sFieldList + N',[' + @sFieldName + N']'

				if @sValuesList = ''
					set @sValuesList = N'@' + @sInheritClassName + N'_' + @sFieldName
				else
					set @sValuesList = @sValuesList + N', @' + @sInheritClassName + N'_' + @sFieldName

				if @nFieldType = 13 or @nFieldType = 17 begin -- String or BigString
					IF @nCustom = 0
						set @sDynSQLParamList = @sDynSQLParamList + char(9) +
							N'@' + @sInheritClassName + '_' + @sFieldName + '_fmt '
					ELSE -- IF @nCustom = 1
						set @sDynSQLCustomParamList = @sDynSQLCustomParamList + char(9) +
							N'@' + @sInheritClassName + '_' + @sFieldName + '_fmt '

					if @nFieldType = 13 BEGIN
						IF @nCustom = 0
							set @sDynSQLParamList = @sDynSQLParamList + 'varbinary(8000) = null,' + CHAR(13) + CHAR(10)
						ELSE -- IF @nCustom = 1
							set @sDynSQLCustomParamList = @sDynSQLCustomParamList + 'varbinary(8000) = null,' + CHAR(13) + CHAR(10)
						END
					else if @nFieldType = 17 BEGIN
						IF @nCustom = 0
							set @sDynSQLParamList = @sDynSQLParamList + 'image = null,' + CHAR(13) + CHAR(10)
						ELSE -- IF @nCustom = 1
							set @sDynSQLCustomParamList = @sDynSQLCustomParamList + 'image = null,' + CHAR(13) + CHAR(10)
					END

					set @sFieldList = @sFieldList + N',[' + @sFieldName + N'_fmt]'
					set @sValuesList = @sValuesList + N', @' + @sInheritClassName + '_' + @sFieldName + '_fmt'
				end

			end

			FETCH curFieldList INTO @sFieldName, @nFieldType, @flid, @nCustom
		end

		close curFieldList
		deallocate curFieldList

		if @sFieldList <> N'' set @sDynSQL4 = @sDynSQL4 + CHAR(13) + CHAR(10) + char(9) +
				N'insert into ['+@sInheritClassName+N'] ([Id]' + 	@sFieldList + N') ' + CHAR(13) + CHAR(10) +
				char(9) + char(9) + N'values (@ObjId, ' + @sValuesList + N')'
		else set @sDynSQL4 = @sDynSQL4 + char(9) + N'insert into ['+@sInheritClassName+N'] with (rowlock) ([Id]) values(@ObjId)'
		set @sDynSQL4 = @sDynSQL4 + CHAR(13) + CHAR(10) + char(9) + N'set @Err = @@error' + CHAR(13) + CHAR(10) + char(9) + N'if @Err <> 0 goto LCleanUp' + CHAR(13) + CHAR(10)

		fetch curClassInheritPath into @sInheritClassName, @nInheritClid
	end

	close curClassInheritPath
	deallocate curClassInheritPath

	--
	-- build the dynamic SQL strings
	--

	IF LEN(@sDynSQLCustomParamList) != 0 BEGIN
--		SET @sDynSQLCustomParamList = ',' + CHAR(13) + CHAR(10) +
--			SUBSTRING(@sDynSQLCustomParamList, 1, LEN(@sDynSQLCustomParamList) - 3)
		-- Move the trailing comma and whitespace to the beginning of the list.
		DECLARE @lenCustom INT, @lenNew INT
		SET @lenCustom = LEN(@sDynSQLCustomParamList)
		SET @lenNew = CHARINDEX(N',', @sDynSQLCustomParamList, @lenCustom - 4) - 1
		IF @lenNew = -1 SET @lenNew = @lenCustom	-- sanity check
		SET @sDynSQLCustomParamList = ',' + CHAR(13) + CHAR(10) + SUBSTRING(@sDynSQLCustomParamList, 1, @lenNew)
	END

	set @sDynSQLParamList =
N'
----------------------------------------------------------------
-- This stored procedure was generated with DefineCreateProc$ --
----------------------------------------------------------------

--( Selected parameters can be used instead of the whole list. For example:
--( exec CreateObject_CmAgent @NewObjId = @NewObjId output,	@NewObjGuid = @NewObjGuid output

Create proc ['+@sProcName+N']' + CHAR(13) + CHAR(10) +
	@sDynSQLParamList +
N'	@Owner int = null,
	@OwnFlid int = null,
	@StartObj int = null,
	@NewObjId int output,
	@NewObjGuid uniqueidentifier output,
	@fReturnTimestamp tinyint = 0,
	@NewObjTimestamp int = null output' +
	@sDynSQLCustomParamList + CHAR(13) + CHAR(10)

	set @sDynSQL1 =
N'as
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
		from	[CmObject]
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

		-- REVIEW (SteveMiller): SERIALIZABLE here is entirely keeping with good db code. However,
		-- SERIALIZABLE holds a lock for the duration of the transaction, which bars any further
		-- inserts/updates from happening. This should be acceptable here, but could be a place to
		-- look if locking problems occur.

		if @StartObj is null begin
			select	@ownOrd = coalesce(max([OwnOrd$])+1, 1)
			from	[CmObject] with (SERIALIZABLE)
			where	[Owner$] = @Owner
				and [OwnFlid$] = @OwnFlid
		end
		else begin
			-- get the ordinal value of the object that is located where the new object is to be inserted
			select	@OwnOrd = [OwnOrd$]
			from	[CmObject] with (SERIALIZABLE)
			where	[Id] = @StartObj
			if @OwnOrd is null begin
				raiserror(''The start object does not exist in the owning sequence: Owner=%d, OwnFlid=%d, StartObj=%d'', 16, 1, @Owner, @OwnFlid, @StartObj)
				set @Err = 50006
				goto LCleanUp
			end

			-- increment the ordinal value(s) of the object(s) in the sequence that occur at or after
			--	the new object(s)
			update	[CmObject] with (SERIALIZABLE)
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
			from	[CmObject]
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

	exec (@sDynSQLParamList + @sDynSql1+@sDynSQL2+@sDynSQL3+@sDynSQL4+@sDynSQL5)
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
/***********************************************************************************************
 * Trigger: TR_Field$_UpdateModel_Ins
 *
 * Description:
 *	This trigger updates the database model based on row inserts into the Field$ table.
 *	Basically, it alters the underlying class table to add a new column (or columns)
 *	depending on its type. This trigger handles both the initial model creation and custom
 *	fields.
 *
 * Type: 	Insert
 * Table:	Field$
 *
 * Notes:
 *	When custom fields are added, the trigger also rebuilds the class views. This is
 *	necessary because the * operator in views does not pick up ALTER TABLE changes made to
 *	the underlying class tables.
 *
 *	The fields UserLabel, HelpString, ListRootId, WsSelector, and XmlUI were added to this
 *	table much later than the other fields. They are meant to be used with custom fields,
 *	but they have no impact on column creation here.
 **********************************************************************************************/
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

	declare @sql NVARCHAR(1000)
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

		end
		else if @type IN (14,16,18,20) begin
			-- Define the view or table for this multilingual custom field

			set @sTable = case @type
				when 14 then 'MultiStr$'
				when 18 then 'MultiBigStr$'
				when 20 then 'MultiBigTxt$'
				end
			set @sFmtArg = case @type
				when 14 then '[Fmt]'
				when 18 then '[Fmt]'
				when 20 then 'cast(null as varbinary) as [Fmt]'
				end

			IF @type = 16 BEGIN
				-- TODO (SteveMiller): The Txt field really ought to be cut down
				-- to 83 or less for a number of reasons. First, indexes don't do
				-- a whole lot of good when they get beyond 83 characters. Second,
				-- a lot of these fields probably don't need more than 80, and if
				-- they do, ought to be put in a different field. Third, Firebird
				-- indexes don't go beyond 83.
				--
				-- The fields currently larger than 83 (or 40, for that matter),
				-- is flids 7001 and 20002. 6001004 does in TestLangProj, but that's
				-- "bogus data". These two might become MultiBigTxt fields. Ideally,
				-- word forms ought to be cut down to 83, but Ken indicates that
				-- idioms might be stored there.

				--( See notes under string tables about Latin1_General_BIN

				SET @sql = N'CREATE TABLE ' + @sClass + N'_' + @sName + N' ( ' + CHAR(13) + CHAR(10) +
					CHAR(9) + N'[Obj] INT NOT NULL, ' + CHAR(13) + CHAR(10) +
					CHAR(9) + N'[Ws] INT NOT NULL, ' + CHAR(13) + CHAR(10) +
					CHAR(9) + N'[Txt] NVARCHAR(4000) COLLATE Latin1_General_BIN NOT NULL)'
				EXEC sp_executesql @sql

				SET @sql = N'CREATE INDEX pk_' + @sClass + N'_' + @sName +
					N' ON ' + @sClass + N'_' + @sName + N'(Obj, WS)'
				EXEC sp_executesql @sql
				-- We need these indexes for at least the MakeMissingAnalysesFromLexicion to
				-- perform reasonably. (This is used in parsing interlinear texts.)
				-- SQLServer allows this index even though these fields are
				-- currently too big to index if the whole length is used. In practice, the
				-- limit of 450 unicode characters is very(!) unlikely to be exceeded for
				-- a single wordform or morpheme form.
				if @sName = 'Form' and (@sClass = 'MoForm' or @sClass = 'WfiWordform') BEGIN
					SET @sql = N'CREATE INDEX Ind_' + @sClass + N'_' + @sName +
						N'_Txt ON ' + @sClass + N'_' + @sName + N'(txt)'
					EXEC sp_executesql @sql
				END
		END
			ELSE BEGIN
				set @sql = 'CREATE VIEW [' + @sClass + '_' + @sName + '] AS' + CHAR(13) + CHAR(10) +
					CHAR(9) + 'select [Obj], [Flid], [Ws], [Txt], ' + @sFmtArg + CHAR(13) + CHAR(10) +
					CHAR(9) + 'FROM [' + @sTable + ']' + CHAR(13) + CHAR(10) +
					CHAR(9) + 'WHERE [Flid] = ' + @sFlid
				exec (@sql)
			END
			if @@error <> 0 goto LFail

		end
		else if @type IN (23,25,27) begin
			-- define the view for this OwningAtom/Collection/Sequence custom field.
			set @sql = 'CREATE VIEW [' + @sClass + '_' + @sName + '] AS' + CHAR(13) + CHAR(10) +
				CHAR(9) + 'select [Owner$] as [Src], [Id] as [Dst]'

			if @type = 27 set @sql = @sql + ', [OwnOrd$] as [Ord]'

			set @sql = @sql + CHAR(13) +
				CHAR(9) + 'FROM [CmObject]' + CHAR(13) + CHAR(10) +
				CHAR(9) + 'WHERE [OwnFlid$] = ' + @sFlid
			exec (@sql)
			if @@error <> 0 goto LFail

			--( Adding an owning atomic StText field requires all existing instances
			--( of the owning class to possess an empty StText and StTxtPara.

			IF @type = 23 AND @DstCls = 14 BEGIN
				SET @sql = '
				DECLARE @recId INT,
					@newId int,
					@dummyId int,
					@dummyGuid uniqueidentifier

				DECLARE curOwners CURSOR FOR SELECT [id] FROM ' + @sClass + '
				OPEN curOwners
				FETCH NEXT FROM curOwners INTO @recId
				WHILE @@FETCH_STATUS = 0
				BEGIN
					EXEC CreateObject_StText
						0, @recId, ' + @sFlid + ', null, @newId OUTPUT, @dummyGuid OUTPUT
					EXEC CreateObject_StTxtPara
						null, null, null, null, null, null, @newId, 14001, null, @dummyId, @dummyGuid OUTPUT
					FETCH NEXT FROM curOwners INTO @recId
				END
				CLOSE curOwners
				DEALLOCATE curOwners'

				EXEC (@sql)
			END

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
				-- Time: default to current time except for fields in LgWritingSystem.
				when @type = 5 AND @sClass != 'LgWritingSystem' then 'DATETIME NULL DEFAULT GETDATE()'
				when @type = 5 AND @sClass = 'LgWritingSystem' then 'DATETIME NULL'
				when @type = 6 then 'UNIQUEIDENTIFIER NULL'			-- Guid
				when @type = 7 then 'IMAGE NULL'				-- Image
				when @type = 8 then 'INT NOT NULL DEFAULT 0'			-- GenDate
				when @type = 9 and @Big is null then 'VARBINARY(8000) NULL'		-- Binary
				when @type = 9 and @Big = 0 then 'VARBINARY(8000) NULL'		-- Binary
				when @type = 9 and @Big = 1 then 'IMAGE NULL'			-- Binary
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
			if @type = 24 and @sClass != 'CmObject' begin
				set @sql = 'ALTER TABLE [' + @sClass + '] ADD CONSTRAINT [' +		-- ReferenceAtom
					'_FK_' + @sClass + '_' + @sName + '] ' + CHAR(13) + CHAR(9) +
					' FOREIGN KEY ([' + @sName + ']) REFERENCES [' + @sTargetClass + '] ([Id])'
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			end

		end

		-- get the next class to process
		Select @sFlid= min([id]) from inserted  where [Id] > @sFlid

	end  -- While loop

	--( UpdateClassView$ is executed in TR_Field$_UpdateModel_InsLast, which is created
	--( in LangProjSP.sql.

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return

LFail:
	rollback tran
	return
GO

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200088
begin
	update Version$ set DbVer = 200089
	COMMIT TRANSACTION
	print 'database updated to version 200089'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200088 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
