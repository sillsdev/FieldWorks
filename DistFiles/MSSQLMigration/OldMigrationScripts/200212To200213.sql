-- Update database from version 200212 to 200213
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

---------------------------------------------------------------------------------
-- FWNX-56: Shorten Create_Object_* stored procedure names
---------------------------------------------------------------------------------

if object_id('DefineCreateProc$') is not null begin
	print 'removing proc DefineCreateProc$'
	drop proc DefineCreateProc$
end
go

print 'creating proc GenMakeObjProc'
go
create proc [GenMakeObjProc]
	@clid int
as
	declare @Err int, @fIsNocountOn int
	declare @sDynSQL1 nvarchar(4000), @sDynSQL2 nvarchar(4000), @sDynSQL3 nvarchar(4000),
		@sDynSQL3a NVARCHAR(4000), @sDynSQL3b NVARCHAR(4000), @sDynSQL3c NVARCHAR(4000),
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

	set @sProcName = N'MakeObj_' + SUBSTRING(@sClass, 1, 22)

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
	SET @sDynSQL3a = N''
	SET @sDynSQL3b = N''
	SET @sDynSQL3c = N''
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
			SET @sDynSQL3 = N''
			if @nFieldType = 14 begin -- MultiStr$
				IF @nCustom = 0
					set @sDynSQLParamList = @sDynSQLParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt nvarchar(4000) = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt varbinary(8000) = null' + N', ' + char(13)
				ELSE -- IF @nCustom = 1
					set @sDynSQLCustomParamList = @sDynSQLCustomParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt nvarchar(4000) = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt varbinary(8000) = null' + N', ' + char(13)

				set @sDynSQL3 = char(9) +
					N'if @' + @sInheritClassName + N'_' + @sFieldName + N'_txt is not null begin' + char(13) +
					char(9) + char(9) + N'insert into [MultiStr$] with (rowlock) ([Flid],[Obj],[Ws],[Txt],[Fmt]) ' + char(13) +
					char(9) + char(9) + N'values (' + convert(nvarchar(11), @flid)+ N',@ObjId,' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws' + N',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt' + N',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt)' + CHAR(13) +
					CHAR(9) + CHAR(9) + N'set @Err = @@error' +
					CHAR(9) + CHAR(9) + N'if @Err <> 0 goto LCleanUp' + CHAR(13) +
					CHAR(9) + N'end' + CHAR(13)
			end
			else if @nFieldType = 16 begin -- MultiTxt$
				IF @nCustom = 0
					set @sDynSQLParamList = @sDynSQLParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt nvarchar(4000) = null' + N', ' + char(13)
				ELSE -- IF @nCustom = 1
					set @sDynSQLCustomParamList = @sDynSQLCustomParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt nvarchar(4000) = null' + N', ' + char(13)

				set @sDynSQL3 = @sDynSQL3 + char(9) +
					N'if @' + @sInheritClassName + N'_' + @sFieldName + N'_txt is not null begin' + char(13)
				SET @sDynSQL3 = @sDynSQL3 + CHAR(9) + CHAR(9) +
					N'INSERT INTO ' + @sInheritClassName + N'_' + @sFieldName +
					N' WITH (ROWLOCK) (Obj, Ws, Txt)' + CHAR(13) +
					char(9) + char(9) + N'values (@ObjId,' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws' + ',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt)'+ CHAR(13) +
					CHAR(9) + CHAR(9) + N'set @Err = @@error' +
					CHAR(9) + CHAR(9) + N'if @Err <> 0 goto LCleanUp' + CHAR(13) +
					CHAR(9) + N'end' + CHAR(13)
			end
			else if @nFieldType = 18 begin -- MultiBigStr$
				IF @nCustom = 0
					set @sDynSQLParamList = @sDynSQLParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt ntext = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt image = null' + N', ' + char(13)
				ELSE -- IF @nCustom = 1
					set @sDynSQLCustomParamList = @sDynSQLCustomParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt ntext = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt image = null' + N', ' + char(13)

				set @sDynSQL3 = @sDynSQL3 + char(9) +
					N'if @' + @sInheritClassName + N'_' + @sFieldName + N'_txt is not null begin' + char(13) +
					char(9) + char(9) + N'insert into [MultiBigStr$] with (rowlock) ([Flid],[Obj],[Ws],[Txt],[Fmt])' + char(13) +
					char(9) + char(9) + N'values (' + convert(nvarchar(11), @flid)+ N',@ObjId,' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws' + N',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt' + N',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt)'+ CHAR(13) +
					CHAR(9) + CHAR(9) + N'set @Err = @@error' +
					CHAR(9) + CHAR(9) + N'if @Err <> 0 goto LCleanUp' + CHAR(13) +
					CHAR(9) + N'end' + CHAR(13)
			end
			else if @nFieldType = 20 begin -- MultiBigTxt$
				IF @nCustom = 0
					set @sDynSQLParamList = @sDynSQLParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt ntext = null' + N', ' + char(13)
				ELSE -- IF @nCustom = 1
					set @sDynSQLCustomParamList = @sDynSQLCustomParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt ntext = null' + N', ' + char(13)

				set @sDynSQL3 = @sDynSQL3 + char(9) +
					N'if @' + @sInheritClassName + N'_' + @sFieldName + N'_txt is not null begin' + char(13) +
					char(9) + char(9) + N'insert into [MultiBigTxt$] with (rowlock) ([Flid],[Obj],[Ws],[Txt])' + char(13) +
					char(9) + char(9) + N'values (' + convert(nvarchar(11), @flid)+ N',@ObjId,' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws' + N',' +
					N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt)' + CHAR(13) +
					CHAR(9) + CHAR(9) + N'set @Err = @@error' +
					CHAR(9) + CHAR(9) + N'if @Err <> 0 goto LCleanUp' + CHAR(13) +
					CHAR(9) + N'end' + CHAR(13)
			end
			else begin
				IF @nCustom = 0
					set @sDynSQLParamList = @sDynSQLParamList + char(9) +
						N'@' + @sInheritClassName + '_' + @sFieldName + ' ' +
						dbo.fnGetColumnDef$(@nFieldType) + N',' + char(13)
				ELSE -- IF @nCustom = 1
					set @sDynSQLCustomParamList = @sDynSQLCustomParamList + char(9) +
						N'@' + @sInheritClassName + '_' + @sFieldName + ' ' +
						dbo.fnGetColumnDef$(@nFieldType) + N',' + char(13)

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
							set @sDynSQLParamList = @sDynSQLParamList + 'varbinary(8000) = null,' + char(13)
						ELSE -- IF @nCustom = 1
							set @sDynSQLCustomParamList = @sDynSQLCustomParamList + 'varbinary(8000) = null,' + char(13)
						END
					else if @nFieldType = 17 BEGIN
						IF @nCustom = 0
							set @sDynSQLParamList = @sDynSQLParamList + 'image = null,' + char(13)
						ELSE -- IF @nCustom = 1
							set @sDynSQLCustomParamList = @sDynSQLCustomParamList + 'image = null,' + char(13)
					END

					set @sFieldList = @sFieldList + N',[' + @sFieldName + N'_fmt]'
					set @sValuesList = @sValuesList + N', @' + @sInheritClassName + '_' + @sFieldName + '_fmt'
				end

			end

			IF LEN(@sDynSQL3a) < 3600
				SET @sDynSql3a = @sDynSql3a + @sDynSQL3
			ELSE IF LEN(@sDynSQL3b) < 3600
				SET @sDynSql3b = @sDynSql3b + @sDynSQL3
			ELSE
				SET @sDynSql3c = @sDynSql3c + @sDynSQL3

			FETCH curFieldList INTO @sFieldName, @nFieldType, @flid, @nCustom
		end --( @@fetch_status = 0

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

	IF LEN(@sDynSQLCustomParamList) != 0 BEGIN
		-- Move the trailing comma and whitespace to the beginning of the list.
		DECLARE @lenCustom INT, @lenNew INT
		SET @lenCustom = LEN(@sDynSQLCustomParamList)
		SET @lenNew = CHARINDEX(N',', @sDynSQLCustomParamList, @lenCustom - 4) - 1
		IF @lenNew = -1 SET @lenNew = @lenCustom	-- sanity check
		SET @sDynSQLCustomParamList = ',' + CHAR(13) + SUBSTRING(@sDynSQLCustomParamList, 1, @lenNew)
	END

	set @sDynSQLParamList =
N'
----------------------------------------------------------------
-- This stored procedure was generated with GenMakeObjProc --
----------------------------------------------------------------

--( Selected parameters can be used instead of the whole list. For example:
--( exec MakeObj_CmAgent @NewObjId = @NewObjId output,	@NewObjGuid = @NewObjGuid output

Create proc ['+@sProcName+N']' + char(13) +
	@sDynSQLParamList +
N'	@Owner int = null,
	@OwnFlid int = null,
	@StartObj int = null,
	@NewObjId int output,
	@NewObjGuid uniqueidentifier output,
	@fReturnTimestamp tinyint = 0,
	@NewObjTimestamp int = null output' +
	@sDynSQLCustomParamList + CHAR(13)

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

	exec (@sDynSQLParamList + @sDynSql1 + @sDynSQL2 + @sDynSQL3a + @sDynSQL3b + @sDynSQL3c +
		@sDynSQL4 + @sDynSQL5)
	set @Err = @@error
	if @Err <> 0 begin
		raiserror ('SQL Error %d: Unable to create procedure for class %s', 16, 1, @Err, @sClass)
		goto LCleanUp
	end

LCleanUp:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	return @Err
go

---------------------------------------------------------------------------------

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
				when 16 then 'MultiTxt$ (No Longer Exists)'
				when 18 then 'MultiBigStr$'
				when 20 then 'MultiBigTxt$'
				end
			--( NOTE (SteveMiller/JohnS): The MultiStr$, MultiBigStr$, and
			--( MultiBigTxt$ tables have foreign key constraints on Field$.ID.
			--( This means that the tables in these Multi* tables must be deleted
			--( before the Field$ row can be deleted. That means the delete
			--( command below probably never deletes anything. It's not bad to
			--( leave the code in, just in case something really weird happnes in
			--( the wild.
			IF @type != 16  -- MultiTxt$ data will be deleted when the table is dropped
			BEGIN
				set @sql = 'DELETE FROM [' + @sTable + '] WHERE [Flid] = ' + @sFlid
				exec (@sql)
				set @Err = @@error
				if @Err <> 0 goto LFail
			END

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
			select @DelId = [Id] FROM CmObject WHERE [OwnFlid$] = @sFlid
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
				IF OBJECT_ID(''ReplRC_' +
					SUBSTRING(@sClass, 1, 11) +  '_' + SUBSTRING(@sName, 1, 11) +
					''') IS NOT NULL
					DROP PROCEDURE [ReplRC_' + @sClass + '_' + @sName + ']
				IF OBJECT_ID(''ReplRS_' +
					SUBSTRING(@sClass, 1, 11) +  '_' + SUBSTRING(@sName, 1, 11) +
					''') IS NOT NULL
					DROP PROCEDURE [ReplRS_' + @sClass + '_' + @sName + ']'
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
			-- Remove Check Constraint from Numeric fields before dropping the column
			If @type = 2 begin
				select @sql = 'ALTER TABLE [' + @sClass + '] DROP CONSTRAINT [' + so.name + ']'
				from sysconstraints sc
					join sysobjects so on so.id = sc.constid and so.name like '_CK_%'
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

		--( Rebuild the delete trigger

		EXEC @Err = CreateDeleteObj @Clid
		IF @Err <> 0 GOTO LFail

		--( Rebuild MakeObj_*

		IF @nAbstract != 1 BEGIN
			EXEC @Err = GenMakeObjProc @Clid
			IF @Err <> 0 GOTO LFail
		END

		-- get the next custom field to process
		Select @sFlid= min([id]) from deleted  where [Id] > @sFlid

	end -- While loop

	--( Rebuild the stored function fnGetRefsToObj
	EXEC @Err = CreateGetRefsToObj
	IF @Err <> 0 GOTO LFail

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return

LFail:
	rollback tran
	return
go

---------------------------------------------------------------------------------

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
			END --( 16
			ELSE BEGIN --( 14, 18, or 20
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
					EXEC MakeObj_StText
						0, @recId, ' + @sFlid + ', null, @newId OUTPUT, @dummyGuid OUTPUT
					EXEC MakeObj_StTxtPara
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
				exec @Err = GenReplRCProc @sTableName
				if @Err <> 0 begin
					raiserror('TR_Field$_UpdateModel_Ins: Unable to create the procedure that handles reference collections for table %s',
							16, 1, @sName)
					goto LFail
				end

			end

			if @type = 28 begin
				set @sTableName = @sClass + '_' + @sName
				exec @Err = GenReplRSProc @sTableName, @sFlid
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
go

---------------------------------------------------------------------------------

IF OBJECT_ID('RenameField') IS NOT NULL BEGIN
	PRINT 'removing procedure RenameField';
	DROP PROCEDURE RenameField;
END
GO
PRINT 'creating procedure RenameField';
GO

CREATE PROCEDURE RenameField
	@FieldId INT,
	@NewFieldName NVARCHAR(100)
AS

	/******************************************************************
	** Warning! Do not use this procedure unless you absolutely know **
	** what you are doing! Then think about it twice. And back up    **
	** your database first.                                          **
	******************************************************************/

	--==( Setup )==--

	DECLARE
		@ClassId INT,
		@ClassName NVARCHAR(100),
		@DstClassName NVARCHAR(100),
		@Type INT,
		@OldFieldName NVARCHAR(100),
		@Sql NVARCHAR(4000);

	DECLARE @Field TABLE (
		Id INT,
		Type INT,
		Class INT,
		DstCls INT,
		--( Name NVARCHAR(100), Name not needed
		Custom TINYINT,
		CustomId UNIQUEIDENTIFIER,
		[Min] BIGINT,
		[Max] BIGINT,
		[Big] BIT,
		UserLabel NVARCHAR(100),
		HelpString NVARCHAR(100),
		ListRootId INT,
		WsSelector INT)

	SELECT
		@Type = Type,
		@ClassId = Class,
		@OldFieldName = f.Name,
		@ClassName = c.Name,
		@DstClassName = dst.Name
	FROM Field$ f
	JOIN Class$ c ON c.Id = f.Class
	LEFT OUTER JOIN Class$ dst ON dst.Id = f.DstCls
	WHERE f.Id = @FieldId;

	DISABLE TRIGGER TR_Field$_No_Upd ON Field$;

	--==============================--
	--==( 16 MultiUnicode        )==--
	--==( 26 ReferenceCollection )==--
	--==( 28 ReferenceSequence   )==--
	--==============================--

	IF @Type IN (16, 26, 28) BEGIN

		--( Save off the column info
		INSERT INTO @Field
		SELECT
			Id, Type, Class, DstCls, Custom, CustomId,
			[Min], [Max], [Big], UserLabel, HelpString, ListRootId, WsSelector
		FROM Field$
		WHERE Id = @FieldId

		--( Save off the data (of the attribute table)
		IF @Type = 16 BEGIN
			CREATE TABLE #Temp16 (Obj INT, Ws INT, Txt NVARCHAR(4000));
			SET @Sql = N'INSERT INTO #Temp16 SELECT Obj, Ws, Txt FROM ' +
				@ClassName + N'_' + @OldFieldName;
		END
--		IF @Type = 20 BEGIN --( Currently not used
--			CREATE TABLE #Temp20 (Obj INT, Ws INT, Txt NVARCHAR(4000));
--			SET @Sql = N'INSERT INTO #Temp20 SELECT Obj, Ws, Txt FROM ' +
--				@ClassName + N'_' + @OldFieldName;
--		END
		ELSE IF @Type = 26 BEGIN
			CREATE TABLE #Temp26 (Src INT, Dst INT);
			SET @Sql = N'INSERT INTO #Temp26 SELECT Src, Dst FROM ' +
				@ClassName + N'_' + @OldFieldName;
		END
		ELSE IF @Type = 28 BEGIN
			CREATE TABLE #Temp28 (Src INT, Dst INT, Ord INT);
			SET @Sql = N'INSERT INTO #Temp28 SELECT Src, Dst, Ord FROM ' +
				@ClassName + N'_' + @OldFieldName;
		END
		EXEC (@Sql);

		--( Remove the old column
		DELETE FROM Field$ WHERE Id = @FieldId

		--( Create the new column
		INSERT INTO Field$
			(Id, Type, Class, DstCls, Name, Custom, CustomId,
			[Min], [Max], [Big], UserLabel, HelpString, ListRootId, WsSelector)
		SELECT
			Id, Type, Class, DstCls, @NewFieldName, Custom, CustomId,
			[Min], [Max], [Big], UserLabel, HelpString, ListRootId, WsSelector
		FROM @Field

		--( Restore the data

		IF @Type = 16 BEGIN
			SET @Sql = N'INSERT INTO '+ @ClassName + N'_' + @NewFieldName +
				' SELECT Obj, Ws, Txt FROM #Temp16';
			EXEC (@Sql);
			DROP TABLE #Temp16;
		END
		ELSE IF @Type = 26 BEGIN
			SET @Sql = N'INSERT INTO '+ @ClassName + N'_' + @NewFieldName +
				' SELECT Src, Dst FROM #Temp26';
			EXEC (@Sql);
			DROP TABLE #Temp26;
		END
		ELSE IF @Type = 28 BEGIN
			SET @Sql = N'INSERT INTO '+ @ClassName + N'_' + @NewFieldName +
				' SELECT Src, Dst, Ord FROM #Temp28';
			EXEC (@Sql);
			DROP TABLE #Temp28;
		END;
	END;

	--===========================--
	--==( 14 MultiString      )==--
	--==( 18 MultiBigString   )==--
	--==( 23 OwningAtom       )==--
	--==( 25 OwningCollection )==--
	--==( 27 OwningSequence   )==--
	--===========================--

	ELSE IF @Type IN (14, 18, 23, 25, 27) BEGIN
		SET @Sql = N'DROP VIEW ' + @ClassName + N'_' + @OldFieldName;
		EXEC (@Sql);

		UPDATE Field$ SET Name = @NewFieldName WHERE Name = @OldFieldName

		SET @Sql = N'CREATE VIEW ' + @ClassName + N'_' + @NewFieldName + ' AS SELECT '
		IF @Type = 14
			SET @Sql = @Sql +
				N'[Obj], [Flid], [Ws], [Txt], [Fmt]
				FROM [MultiStr$]
				WHERE [Flid] = ' + CAST(@FieldId AS NVARCHAR(20))
		ELSE IF @Type = 18
			SET @Sql = @Sql +
				N'[Obj], [Flid], [Ws], [Txt], CAST(NULL AS VARBINARY) AS [Fmt]
				FROM [MultiBigStr$]
				WHERE [Flid] = ' + CAST(@FieldId AS NVARCHAR(20))
		ELSE IF @Type IN (23, 25)
			SET @Sql = @Sql +
				N'Owner$ AS Src, Id AS Dst
				FROM CmObject
				WHERE OwnFlid$ = ' + CAST(@FieldId AS NVARCHAR(20))
		ELSE IF @Type = 27
			SET @Sql = @Sql +
				N'Owner$ AS Src, Id AS Dst, OwnOrd$ AS Ord
				FROM CmObject
				WHERE OwnFlid$ = ' + CAST(@FieldId AS NVARCHAR(20))
		EXEC (@Sql);

		--==( Rebuild Stuff for the Class )==--

		--( Rebuild the class view. This should be fired in the Field$ insert last
		--( trigger TR_Field$UpdateModel_InsLast, though a lot of people add it to
		--( migration scripts:

		EXECUTE UpdateClassView$ @ClassId, 1

		--( Rebuild the function fnGetRefsToObj. Fired in the Field$ insert
		--( last trigger TR_Field$UpdateModel_InsLast:

		EXEC CreateGetRefsToObj

		--( Rebuild MakeObj_*	Fired in the Field$ insert last trigger
		--( TR_Field$UpdateModel_InsLast:

		EXEC GenMakeObjProc @ClassId;

	END --(  @Type IN (14, 18, 23, 25, 27)

	--================================--
	--==( Attribute in class table )==--
	--==( 1 Boolean                )==--
	--==( 2 Integer                )==--
	--==( 5 Time                   )==--
	--==( 6 Guid                   )==--
	--==( 8 Gendate                )==--
	--==( 9 Binary                 )==--
	--==( 13 String                )==--
	--==( 15 Unicode               )==--
	--==( 17 BigString             )==--
	--==( 19 CompDetails           )==--
	--==( 24 ReferenceAtom         )==--
	--================================--

	ELSE BEGIN

		--( Save off the column info
		INSERT INTO @Field
		SELECT
			Id, Type, Class, DstCls, Custom, CustomId,
			[Min], [Max], [Big], UserLabel, HelpString, ListRootId, WsSelector
		FROM Field$
		WHERE Id = @FieldId

		--( Save off the data
		SET @Sql = N'CREATE TABLE #Temp' + CAST(@Type AS NVARCHAR(2))
		IF @Type = 1
			CREATE TABLE #Temp1 (Id INT, Temp BIT);
		ELSE IF @Type = 2
			CREATE TABLE #Temp2 (Id INT, Temp INT);
		ELSE IF @Type = 3 --( currently not used
			CREATE TABLE #Temp3 (Id INT, Temp INT);
		ELSE IF @Type = 4 --( currently not used
			CREATE TABLE #Temp4 (Id INT, Temp INT);
		ELSE IF @Type = 5
			CREATE TABLE #Temp5 (Id INT, Temp DATETIME);
		ELSE IF @Type = 6
			CREATE TABLE #Temp6 (Id INT, Temp UNIQUEIDENTIFIER);
		ELSE IF @Type = 7 --( currently not used
			CREATE TABLE #Temp7 (Id INT, Temp IMAGE);
		ELSE IF @Type = 8
			CREATE TABLE #Temp8 (Id INT, Temp INT);
		ELSE IF @Type = 9
			CREATE TABLE #Temp9 (Id INT, Temp VARBINARY(8000));
		ELSE IF @Type = 13
			CREATE TABLE #Temp13 (Id INT, Temp NVARCHAR(4000), Temp_Fmt VARBINARY(8000));
		ELSE IF @Type = 15
			CREATE TABLE #Temp15 (Id INT, Temp VARCHAR(4000));
		ELSE IF @Type = 17
			CREATE TABLE #Temp17 (Id INT, Temp NTEXT, Temp_Fmt IMAGE);
		ELSE IF @Type = 19
			CREATE TABLE #Temp19 (Id INT, Temp NTEXT);
		ELSE IF @Type = 24
			CREATE TABLE #Temp24 (Id INT, Temp INT);

		IF @Type NOT IN (13, 17) --( not in String and BigString
			SET @Sql = N'INSERT INTO #Temp' + CAST(@Type AS NVARCHAR(2)) +
				N' SELECT Id, ' + @OldFieldName + N' FROM ' + @ClassName;
		ELSE
			SET @Sql = N'INSERT INTO #Temp' + CAST(@Type AS NVARCHAR(2)) +
				N' SELECT Id, ' + @OldFieldName + ', ' + @OldFieldName + N'_Fmt FROM ' + @ClassName;
		EXEC (@Sql);

		--( Remove the old column
		DELETE FROM Field$ WHERE Id = @FieldId;

		--( Create the new column
		INSERT INTO Field$
			(Id, Type, Class, DstCls, Name, Custom, CustomId,
			[Min], [Max], [Big], UserLabel, HelpString, ListRootId, WsSelector)
		SELECT
			Id, Type, Class, DstCls, @NewFieldName, Custom, CustomId,
			[Min], [Max], [Big], UserLabel, HelpString, ListRootId, WsSelector
		FROM @Field;

		--( Restore the data
		IF @Type NOT IN (13, 17)
			SET @Sql = N'UPDATE '+ @ClassName + N' SET ' + @NewFieldName + N' = Temp ' +
				N'FROM ' + @ClassName + N' AS c ' +
				N'JOIN #Temp' + CAST(@Type AS NVARCHAR(2)) + N' AS t ON t.Id = c.Id';
		ELSE
			SET @Sql = N'UPDATE '+ @ClassName + N' SET ' +
					@NewFieldName + N' = Temp, ' +
					@NewFieldName + N'_Fmt = Temp_Fmt ' +
				N'FROM ' + @ClassName + N' AS c ' +
				N'JOIN #Temp' + CAST(@Type AS NVARCHAR(2)) + N' AS t ON t.Id = c.Id';
		EXEC (@Sql);

		SET @Sql = N'DROP TABLE #Temp' + CAST(@Type AS NVARCHAR(2));
		EXEC (@Sql);
	END

	--==( Clean up )==--

	GO
	ENABLE TRIGGER TR_Field$_No_Upd ON Field$;
	ALTER TABLE Field$ CHECK CONSTRAINT ALL;
GO

---------------------------------------------------------------------------------

IF OBJECT_ID('TR_Class$_InsLast') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_Class$_InsLast'
	DROP TRIGGER TR_Class$_InsLast
END
GO
PRINT 'creating trigger TR_Class$_InsLast'
GO

CREATE TRIGGER TR_Class$_InsLast ON Class$ FOR INSERT
AS
	DECLARE
		@nErr INT,
		@nClassid INT,
		@nAbstract BIT

	SELECT @nClassId = Id, @nAbstract = Abstract FROM inserted

	--( Build the MakeObj_ stored procedure
	IF @nAbstract = 0 BEGIN
		EXEC @nErr = GenMakeObjProc @nClassId
		IF @nErr <> 0 GOTO LFail
	END

	--( Build the delete trigger
	EXEC @nErr = CreateDeleteObj @nClassId
	IF @nErr <> 0 GOTO LFail

	--( Rebuild the stored function fnGetRefsToObj
	EXEC @nErr = CreateGetRefsToObj
	IF @nErr <> 0 GOTO LFail

	RETURN

LFail:
	ROLLBACK TRANSACTION
	RETURN
GO


EXEC sp_settriggerorder 'TR_Class$_InsLast', 'last', 'INSERT'
GO

---------------------------------------------------------------------------------

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
		@nDstClsId INT,
		@nAbstract BIT,
		@nLoopLevel TINYINT,
		@fExit BIT

	DECLARE @tblSubclasses TABLE (ClassId INT, Abstract BIT, ClassLevel TINYINT)

	SELECT @nClassId = Class, @nDstClsId = DstCls FROM inserted
	SET @nLoopLevel = 1

	--==( Outer loop: all the classes for the level )==--

	--( This insert is necessary for any subclasses. It also
	--( gets Class$.Abstract for updating the MakeObj_*
	--( stored procedure.

	INSERT INTO @tblSubclasses
	SELECT @nClassId, c.Abstract, @nLoopLevel
	FROM Class$ c
	WHERE c.Id = @nClassId

	--( Rebuild the delete trigger

	EXEC @nErr = CreateDeleteObj @nClassId
	IF @nErr <> 0 GOTO LFail

	--( Rebuild MakeObj_*

	SELECT @nAbstract = Abstract FROM @tblSubClasses
	IF @nAbstract != 1 BEGIN
		EXEC @nErr = GenMakeObjProc @nClassId
		IF @nErr <> 0 GOTO LFail
	END

	SET @fExit = 0
	WHILE @fExit = 0 BEGIN

		--( Inner loop: update all classes subclassed from the previous
		--( set of classes.

		SELECT TOP 1 @nClassId = ClassId, @nAbstract = Abstract
		FROM @tblSubclasses
		WHERE ClassLevel = @nLoopLevel
		ORDER BY ClassId

		WHILE @@ROWCOUNT > 0 BEGIN

			--( Update the view

			EXEC @nErr = UpdateClassView$ @nClassId, 1
			IF @nErr <> 0 GOTO LFail

			--( Get next class

			SELECT TOP 1 @nClassId = ClassId, @nAbstract = Abstract
			FROM @tblSubclasses
			WHERE ClassLevel = @nLoopLevel AND ClassId > @nClassId
			ORDER BY ClassId
		END

		--( Load outer loop with next level
		SET @nLoopLevel = @nLoopLevel + 1

		INSERT INTO @tblSubclasses
		SELECT c.Id, c.Abstract, @nLoopLevel
		FROM @tblSubClasses sc
		JOIN Class$ c ON c.Base = sc.ClassId
		WHERE sc.ClassLevel = @nLoopLevel - 1

		IF @@ROWCOUNT = 0
			SET @fExit = 1
	END

	--( Rebuild the delete trigger for the destination class

	IF @nDstClsId IS NOT NULL BEGIN
		EXEC @nErr = CreateDeleteObj @nDstClsId
		IF @nErr <> 0 GOTO LFail
	END

	--( Rebuild the stored function fnGetRefsToObj (does all classes)
	EXEC @nErr = CreateGetRefsToObj
	IF @nErr <> 0 GOTO LFail

	RETURN

LFail:
	ROLLBACK TRANSACTION
	RETURN

GO

EXEC sp_settriggerorder 'TR_Field$_UpdateModel_InsLast', 'last', 'INSERT'
GO

---------------------------------------------------------------------------------

if object_id('FindOrCreateCmAgent') is not null
	drop proc FindOrCreateCmAgent
go
print 'creating proc FindOrCreateCmAgent'
go

create proc FindOrCreateCmAgent
	@agentName nvarchar(4000),
	@isHuman bit,
	@version  nvarchar(4000)
as
	DECLARE
		@retVal INT,
		@fIsNocountOn INT,
		@agentID int

	set @agentID = null

	-- determine if NO COUNT is currently set to ON
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	IF @version IS NULL
		select @agentID=aa.Id
		from CmAgent_ aa
		join CmAgent_Name aan on aan.Obj = aa.Id and aan.Txt=@agentName
		join LangProject lp On lp.Id = aa.Owner$
		where aa.Human=@isHuman and aa.Version IS NULL
	ELSE
		select @agentID=aa.Id
		from CmAgent_ aa
		join CmAgent_Name aan on aan.Obj = aa.Id and aan.Txt=@agentName
		join LangProject lp On lp.Id = aa.Owner$
		where aa.Human=@isHuman and aa.Version=@version

	-- Found extant one, so return it.
	if @agentID is not null
	begin
		set @retVal = 0
		goto FinishFinal
	end

	--== Need to make a new one ==--
	DECLARE @uid uniqueidentifier,
		@nTrnCnt INT,
		@sTranName VARCHAR(50),
		@wsEN int,
		@lpID int

	-- We don't need to wory about transactions, since the call to MakeObj_CmAgent
	-- wiil create waht is needed, and rool it back, if the creation fails.

	SELECT @wsEN=Obj
	FROM LgWritingSystem_Name
	WHERE Txt='English'

	SELECT TOP 1 @lpID=ID
	FROM LangProject
	ORDER BY ID

	exec @retVal = MakeObj_CmAgent
		@wsEN, @agentName,
		null,
		@isHuman,
		@version,
		@lpID,
		6001038, -- owning flid for CmAgent in LangProject
		null,
		@agentID out,
		@uid out

	if @retVal <> 0
	begin
		-- There was an error in MakeObj_CmAgent
		set @retVal = 1
		GOTO FinishClearID
	end

	SET @retVal = 0
	GOTO FinishFinal

FinishClearID:
	set @agentID = 0
	GOTO FinishFinal

FinishFinal:
	if @fIsNocountOn = 0 set nocount off
	select @agentID
	return @retVal

go

---------------------------------------------------------------------------------

if object_id('MakeMissingAnalysesFromLexicion') is not null begin
	drop proc MakeMissingAnalysesFromLexicion
end
go
print 'creating proc MakeMissingAnalysesFromLexicion'
go

CREATE  proc MakeMissingAnalysesFromLexicion
	@paraid int,
	@ws int
as

declare wf_cur cursor local static forward_only read_only for

select distinct wf.id wfid, mff.obj fid, ls.id lsid, msta.id msaid, lsg.Txt gloss, msta.PartOfSpeech pos
	from CmBaseAnnotation_ cba
	join WfiWordform wf on  cba.BeginObject = @paraid and cba.InstanceOf = wf.id -- annotations of this paragraph that are wordforms
	left outer join WfiAnalysis_ wa on wa.owner$ = wf.id
	-- if the above produced anything, with the restriction on wa.owner being null below, they are wordforms we want
	join WfiWordform_Form wff on wff.obj = wf.id
	join MoForm_Form mff on wff.Txt = mff.txt and mff.ws = wff.ws
	-- now we have ones whose form matches an MoForm in the same ws
	join CmObject mfo on mfo.id = mff.obj
	join CmObject leo on leo.id = mfo.owner$
	join LexSense_ ls on ls.owner$ = leo.id
	left outer join LexSense_Gloss lsg on lsg.obj = ls.id and lsg.ws = @ws
	left outer join MoStemMsa msta on msta.id = ls.MorphoSyntaxAnalysis
	-- combines with left outer join above for effect of
		-- "not exists (select * from WfiAnalysis_ wa where wa.owner$ = wf.id)"
	-- (that is, we want wordforms that have no analyses)
	-- but is faster
	where wa.owner$ is null

open wf_cur

declare @wfid int, @formid int, @senseid int,  @msaid int, @pos int
declare @gloss nvarchar(1000)
declare @NewObjGuid uniqueidentifier,
	@NewObjTimestamp int

-- 5062002 5062002
-- 5059011 5059011
-- 5059010 5059010
-- 5060001 50600001
fetch wf_cur into @wfid, @formid, @senseid, @msaid, @gloss, @pos
while @@fetch_status = 0 begin
	declare @analysisid int
	declare @mbid int
	declare @wgid int
	exec MakeObj_WfiAnalysis @wfid, 5062002, null, @analysisid out, @NewObjGuid out, 0, @NewObjTimestamp
	exec MakeObj_WfiMorphBundle null, null, null, @analysisid, 5059011, null, @mbid out, @NewObjGuid out, 0, @NewObjTimestamp
	exec MakeObj_WfiGloss @ws, @gloss, @analysisid, 5059010, null, @wgid out, @NewObjGuid out, 0, @NewObjTimestamp
	update WfiMorphBundle set Morph = @formid, Msa = @msaid, Sense = @senseid where id = @mbid
	update WfiAnalysis set Category = @pos where id = @analysisid
	fetch wf_cur into @wfid, @formid, @senseid, @msaid, @gloss, @pos
end
close wf_cur
deallocate wf_cur
go

---------------------------------------------------------------------------------

if object_id('SetAgentEval') is not null begin
	print 'removing proc SetAgentEval'
	drop proc SetAgentEval
end
go
print 'creating proc SetAgentEval'
go

CREATE PROC SetAgentEval
	@nAgentID INT,
	@nTargetID INT, --( A WfiAnalysis.ID or a WfiWordform.ID
	@nAccepted INT,
	@nvcDetails NVARCHAR(4000),
	@dtEval DATETIME
AS
	DECLARE
		@nIsNoCountOn INT,
		@nTranCount INT,
		@sysTranName SYSNAME,
		@nEvals INT,
		@nEvalId INT,
		@nNewObjId INT,
		@guidNewObj UNIQUEIDENTIFIER,
		@nNewObjTimeStamp INT,
		@nError INT,
		@nvcError NVARCHAR(100)

	SET @nError = 0

	SET @nIsNoCountOn = @@OPTIONS & 512
	IF @nIsNoCountOn = 0
		SET NOCOUNT ON

	--( Take care of transaction stuff
	SET @nTranCount = @@TRANCOUNT
	SET @sysTranName = 'SetAgentEval_tr' + CONVERT(VARCHAR(2), @@NESTLEVEL)
	IF @nTranCount = 0
		BEGIN TRAN @sysTranName
	ELSE
		SAVE TRAN @sysTranName

	--( See if we have an Agent Evaluation already
	SELECT TOP 1 @nEvalId = co.[Id]
	FROM CmAgentEvaluation cae
	JOIN CmObject co ON co.[Id] = cae.[Id]
		AND co.Owner$ = @nAgentID
	WHERE cae.Target = @nTargetID
	ORDER BY co.[Id]

	SET @nEvals = @@ROWCOUNT

	--== Remove Eval ==--

	--( If we don't know if the analysis is accepted or not,
	--( we don't really have an eval for it. And if we don't
	--( have an eval for it, we need to get rid of it.

	IF @nAccepted = 2 OR @nAccepted IS NULL BEGIN
		WHILE @nEvals > 0 BEGIN
			EXEC DeleteObj$ @nEvalId

			SELECT TOP 1 @nEvalId = co.[Id]
			FROM CmAgentEvaluation cae
			JOIN CmObject co ON co.[Id] = cae.[Id]
				AND co.Owner$ = @nAgentID
			WHERE cae.Target = @nTargetID
				AND co.[Id] > @nEvalId
			ORDER BY co.[Id]

			SET @nEvals = @@ROWCOUNT
		END
	END

	--== Create or Update Eval ==--

	--( Make sure the evaluation is set the way it should be.

	ELSE BEGIN

		--( Create a new Agent Evaluation
		IF @nEvals = 0 BEGIN

			EXEC @nError = MakeObj_CmAgentEvaluation
				@dtEval,
				@nAccepted,
				@nvcDetails,
				@nAgentId,					--(owner
				kflidCmAgent_Evaluations,	--(ownflid  23006
				NULL,						--(startobj
				@nNewObjId OUTPUT,
				@guidNewObj OUTPUT,
				0,			--(ReturnTimeStamp
				@nNewObjTimeStamp OUTPUT

			IF @nError != 0 BEGIN
				SET @nvcError = 'SetAgentEval: MakeObj_CmAgentEvaluation failed.'
				GOTO Fail
			END

			UPDATE CmAgentEvaluation  WITH (REPEATABLEREAD)
			SET Target = @nTargetID
			WHERE Id = @nNewObjId
		END

		--( Update the existing Agent Evaluation
		ELSE

			UPDATE CmAgentEvaluation WITH (REPEATABLEREAD)
			SET
				DateCreated = @dtEval,
				Accepted = @nAccepted,
				Details = @nvcDetails
			FROM CmAgentEvaluation cae
			JOIN CmObject co ON co.[Id] = cae.[Id]
				AND co.Owner$ = @nAgentID
			WHERE cae.Target = @nTargetID
		--( END
	END
	GOTO Finish

Finish:

	SET @nIsNoCountOn = @@OPTIONS & 512
	IF @nIsNoCountOn = 0
		SET NOCOUNT ON

	-- determine if a transaction or savepoint was created
	IF @nTranCount = 0
		COMMIT TRAN @sysTranName

	RETURN @nError

Fail:
	RAISERROR (@nvcError, 16, 1, @nError)
	IF @nTranCount !=0
		ROLLBACK TRAN @sysTranName

	RETURN @nError

GO

---------------------------------------------------------------------------------

IF OBJECT_ID('RenameClass') IS NOT NULL BEGIN
	PRINT 'removing procedure RenameClass';
	DROP PROCEDURE RenameClass;
END
GO
PRINT 'creating procedure RenameClass';
GO

CREATE PROCEDURE RenameClass
	@ClassId INT,
	@NewClassName NVARCHAR(100)
AS

	/******************************************************************
	** Warning! Do not use this procedure unless you absolutely know **
	** what you are doing! Then think about it twice. And back up    **
	** your database first.                                          **
	******************************************************************/

	--==( Setup )==--

	DECLARE
		@DummyClassId INT,
		@DummyFieldId INT,
		@FieldId INT,
		@Type INT,
		@Class INT,
		@DstCls INT,
		@FieldName NVARCHAR(100),
		@Custom TINYINT,
		@CustomId UNIQUEIDENTIFIER,
		@Min BIGINT,
		@Max BIGINT,
		@Big BIT,
		@UserLabel NVARCHAR(100),
		@ForeignKey NVARCHAR(100),
		@HelpString NVARCHAR(100),
		@ListRootId INT,
		@WsSelector INT,
		@OldClassName NVARCHAR(100),
		@ClassName NVARCHAR(100),
		@Abstract TINYINT,
		@Sql NVARCHAR(4000),
		@Debug BIT

	SET @Debug = 0

	SET @DummyClassId = 9999
	SELECT @OldClassName = Name, @Abstract = Abstract  FROM Class$ WHERE Id = @ClassId

	--==( Disable Security )==--

	SET @Sql = '
		ALTER TABLE Class$ NOCHECK CONSTRAINT ALL;
		ALTER TABLE ClassPar$ NOCHECK CONSTRAINT ALL;
		ALTER TABLE Field$ NOCHECK CONSTRAINT ALL;
		ALTER TABLE CmObject NOCHECK CONSTRAINT ALL;
		ALTER TABLE MultiStr$ NOCHECK CONSTRAINT ALL;
		ALTER TABLE MultiBigStr$ NOCHECK CONSTRAINT ALL;
		ALTER TABLE MultiBigTxt$ NOCHECK CONSTRAINT ALL;
		DISABLE TRIGGER TR_Field$_UpdateModel_Del ON Field$;
		DISABLE TRIGGER TR_Field$_No_Upd ON Field$;'
	EXEC (@Sql);

	--==( Create the new class )==--

	INSERT INTO Class$
		SELECT @DummyClassId, [Mod], Base, Abstract, @NewClassName
		FROM Class$
		WHERE Id = @ClassId

	--( Load the attributes of the new class.

	-- REVIEW (SteveMiller/AnnB): Will need to add XmlUI if it ever starts
	-- getting to used.

	DECLARE ClassAttributes CURSOR FOR
		SELECT
			Id, Type, Class, DstCls, Name, Custom, CustomId,
			[Min], [Max], Big, UserLabel, HelpString, ListRootId, WsSelector
		FROM Field$
		WHERE Class = @ClassId
		ORDER BY Id

	OPEN ClassAttributes
	FETCH NEXT FROM ClassAttributes INTO
		@FieldId, @Type, @Class, @DstCls, @FieldName, @Custom, @CustomId,
		@Min, @Max, @Big, @UserLabel, @HelpString, @ListRootId,	@WsSelector;

	IF @Debug = 1 BEGIN
		print '========================================================'
		print 'starting loop 1 for class ' + cast(@ClassId as nvarchar(20))
		print '========================================================'
	END
	WHILE @@FETCH_STATUS = 0 BEGIN
		EXEC RenameClass_RenameFieldId
			@ClassId, @DummyClassId, @FieldId, @DummyFieldId OUTPUT
		IF @Debug = 1 BEGIN
			print '@OldClassId: ' + CAST(@ClassId AS NVARCHAR(20))
			print '@NewClassId: ' + CAST(@DummyClassId AS NVARCHAR(20))
			print '@OldFieldId: ' + CAST(@FieldId AS NVARCHAR(20))
			print '@NewFieldId: ' + CAST(@DummyFieldId AS NVARCHAR(20))
			print '--------------------------------------------------------'
		END

		INSERT INTO Field$
			([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId],
			[Min], [Max], [Big], UserLabel, HelpString, ListRootId, WsSelector)
		VALUES
			(@DummyFieldId, @Type, @DummyClassId, @DstCls, @FieldName, @Custom, @CustomID,
				@Min, @Max, @Big, @UserLabel, @HelpString, @ListRootId, @WsSelector)

		FETCH NEXT FROM ClassAttributes INTO
			@FieldId, @Type, @Class, @DstCls, @FieldName, @Custom, @CustomId,
			@Min, @Max, @Big, @UserLabel, @HelpString, @ListRootId,	@WsSelector
	END
	CLOSE ClassAttributes
	DEALLOCATE ClassAttributes

	--==( Copy Data Over )==--

	SET @Sql = N'INSERT INTO ' + @NewClassName + N' SELECT * FROM ' + @OldClassName
	EXEC (@Sql)

	DECLARE NewAttributeTables CURSOR FOR
		SELECT Id, Type, f.Name
		FROM Field$ f
		WHERE Class = @ClassId
			AND Type IN (16, 26, 28) --( 16 MultiUnicode, 26 ReferenceCollection, 28 ReferenceSequence

	OPEN NewAttributeTables
	FETCH NEXT FROM NewAttributeTables INTO @DummyFieldId, @Type, @FieldName
	WHILE @@FETCH_STATUS = 0 BEGIN
		--( INSERT INTO CmPoss_Name SELECT * FROM CmPossibility_Name
		SET @Sql = N'INSERT INTO ' + @NewClassName + N'_' + @FieldName +
			N' SELECT * FROM ' + @OldClassName + N'_' + @FieldName
		EXEC (@Sql)
		FETCH NEXT FROM NewAttributeTables INTO @DummyFieldId, @Type, @FieldName
	END
	CLOSE NewAttributeTables
	DEALLOCATE NewAttributeTables

	--==( Delete the Old )==--

	--( Remove references of the object

	DECLARE Refs CURSOR FOR
		SELECT c.Name, f.Name, Type, Class
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.Class
		WHERE f.Class = @ClassId and f.Type IN (26, 28);
	OPEN Refs
	FETCH NEXT FROM Refs INTO @ClassName, @FieldName, @Type, @Class
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @Sql = N'ALTER TABLE ' + @ClassName + N'_' + @FieldName +
			N' DROP CONSTRAINT _FK_' + @ClassName + N'_' + @FieldName  + N'_Src';
		EXECUTE (@Sql);
		FETCH NEXT FROM Refs INTO @ClassName, @FieldName, @Type, @Class
	END
	CLOSE Refs
	DEALLOCATE Refs

	--( Remove referencing constraints to the object

	DECLARE OutsideRefs CURSOR FOR
		SELECT c.Name, f.Name, Type, Class, DstCls
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.Class
		WHERE f.DstCls = @ClassId;
	OPEN OutsideRefs
	FETCH NEXT FROM OutsideRefs INTO @ClassName, @FieldName, @Type, @Class, @DstCls
	WHILE @@FETCH_STATUS = 0 BEGIN
		IF @Type = 24 AND @ClassName != 'CmObject' BEGIN
			SET @Sql = N'ALTER TABLE ' + @ClassName + N' DROP CONSTRAINT _FK_' +
				@ClassName + N'_' + @FieldName;
			EXECUTE (@Sql);
		END
		IF @Type = 26 OR @Type = 28 BEGIN
			SET @Sql = N'ALTER TABLE ' + @ClassName + N'_' + @FieldName +
				N' DROP CONSTRAINT _FK_' + @ClassName + N'_' + @FieldName  + N'_Dst';
			EXECUTE (@Sql);
		END
		FETCH NEXT FROM OutsideRefs INTO @ClassName, @FieldName, @Type, @Class, @DstCls
	END
	CLOSE OutsideRefs
	DEALLOCATE OutsideRefs

	--( Remove subclass referencing constraints

	DECLARE Subclasses CURSOR FOR SELECT Name FROM Class$ WHERE Base = @ClassId;
	OPEN Subclasses
	FETCH NEXT FROM Subclasses INTO @ClassName
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @Sql = N'ALTER TABLE ' + @ClassName + N' DROP CONSTRAINT _FK_' +
			@ClassName + N'_id';
		EXECUTE (@Sql);
		FETCH NEXT FROM Subclasses INTO @ClassName
	END
	CLOSE Subclasses
	DEALLOCATE Subclasses

	--( Normally the Field$ delete trigger takes care of all this. However,
	--( the trigger also wipes out data, which we don't want.

	--( Drop all the constraints
	EXEC ManageConstraints$ @OldClassName, 'C', 'DROP'
	EXEC ManageConstraints$ @OldClassName, 'D', 'DROP'
	EXEC ManageConstraints$ @OldClassName, 'F', 'DROP'
	EXEC ManageConstraints$ @OldClassName, 'UQ', 'DROP'
	EXEC ManageConstraints$ @OldClassName, 'PK', 'DROP'

	DECLARE OldClassAttributes CURSOR FOR
		SELECT Id, Type, f.Name
		FROM Field$ f
		WHERE Class = @ClassId

	OPEN OldClassAttributes
	FETCH NEXT FROM OldClassAttributes INTO @FieldId, @Type, @FieldName
	WHILE @@FETCH_STATUS = 0 BEGIN
		--( Type 14, 18, 20, 23, 25, 27
		IF @Type IN (14, 18, 20, 23, 25, 27) BEGIN
			SET @Sql = N'DROP VIEW ' + @OldClassName + N'_' + @FieldName
			EXEC (@Sql)
		END

		--( Type 16, 26, or 28
		IF @Type IN (16, 26, 28) BEGIN
			SET @Sql = N'DROP TABLE ' + @OldClassName + N'_' + @FieldName
			EXEC (@Sql)

			IF @Type = 26 BEGIN
				SET @Sql = N'DROP PROCEDURE ReplRC_' +
					@OldClassName + N'_' + @FieldName
				EXEC (@Sql)
			END
			ELSE IF @Type = 28 BEGIN
				SET @Sql = N'DROP PROCEDURE ReplRS_' +
					@OldClassName + N'_' + @FieldName
				EXEC (@Sql)
			END
		END

		FETCH NEXT FROM OldClassAttributes INTO @FieldId, @Type, @FieldName
	END
	CLOSE OldClassAttributes
	DEALLOCATE OldClassAttributes

	--( Drop the table's view
	SET @Sql = N'DROP VIEW ' + @OldClassName + N'_'
	EXEC (@Sql)

	DELETE FROM Field$ WHERE Class = @ClassId
	DELETE FROM ClassPar$ WHERE Src = @ClassId
	DELETE FROM Class$ WHERE Id = @ClassId

	SET @Sql = N'DROP TABLE ' + @OldClassName
	EXEC (@Sql)

	-- This sp doesn't exist for abstract classes
	IF @Abstract = N'0' BEGIN

		SET @Sql = N'DROP PROCEDURE MakeObj_' + @OldClassName
		EXEC (@Sql)

	END

	--==( Now that the old class is gone, give the new class the proper id and name )==--

	--( Take care of the class and field

	UPDATE ClassPar$
	SET Src = @ClassId, Dst = @ClassId
	WHERE Src = @DummyClassId AND Dst = @DummyClassId

	UPDATE ClassPar$ SET Src = @ClassId WHERE Src = @DummyClassId
	UPDATE Class$ SET Id = @ClassId WHERE Id = @DummyClassId

	DECLARE CorrectFieldIds CURSOR FOR SELECT ID FROM Field$ f WHERE Class = @DummyClassId --ORDER BY Id
	OPEN CorrectFieldIds
	FETCH NEXT FROM CorrectFieldIds INTO @DummyFieldId
	IF @Debug = 1 BEGIN
		print '========================================================'
		print 'starting loop 2 for class ' + cast(@ClassId as nvarchar(20))
		print '========================================================'
	END
	WHILE @@FETCH_STATUS = 0 BEGIN
		EXECUTE RenameClass_RenameFieldId @DummyClassId, @ClassId, @DummyFieldId, @FieldId OUTPUT
		IF @Debug = 1 BEGIN
			print '@OldClassId: ' + CAST(@DummyClassId AS NVARCHAR(20))
			print '@NewClassId: ' + CAST(@ClassId AS NVARCHAR(20))
			print '@OldFieldId: ' + CAST(@DummyFieldId AS NVARCHAR(20))
			print '@NewFieldId: ' + CAST(@FieldId AS NVARCHAR(20))
			print '--------------------------------------------------------'
		END
		UPDATE Field$ SET Id = @FieldId WHERE Id = @DummyFieldId
		FETCH NEXT FROM CorrectFieldIds INTO @DummyFieldId
	END
	CLOSE CorrectFieldIds
	DEALLOCATE CorrectFieldIds

	UPDATE Field$ SET Class = @ClassId WHERE Class = @DummyClassId
	UPDATE Field$ SET DstCls = @ClassId WHERE DstCls = @DummyClassId

	--==( Rebuild )==--

	--( Rebuild Class View

	EXECUTE UpdateClassView$ @ClassId, 1

	--( Rebuild Multi Views and procedures ReplaceRef* (they still have class 999)

	DECLARE ClassAttributes CURSOR FOR
		SELECT Id, Type, f.Name
		FROM Field$ f
		WHERE Class = @ClassId
			-- REVIEW (SteveMiller/AnnB):  Type 20 has an ntext, which isn't being used.
			AND Type IN (14, 18, 23, 25, 26, 27, 28)

	OPEN ClassAttributes
	FETCH NEXT FROM ClassAttributes INTO @FieldId, @Type, @FieldName
	WHILE @@FETCH_STATUS = 0 BEGIN
		IF @Type = 26 BEGIN
			SET @Sql = @NewClassName + N'_' + @FieldName
			EXEC GenReplRCProc @Sql;
		END
		ELSE IF @Type = 28 BEGIN
			SET @Sql = @NewClassName + N'_' + @FieldName
			EXEC GenReplRSProc @Sql, @FieldId;
		END
		ELSE BEGIN
			SET @Sql = N'DROP VIEW ' + @NewClassName + N'_' + @FieldName
			EXEC (@Sql)

			SET @Sql = N'CREATE VIEW ' + @NewClassName + N'_' + @FieldName + ' AS SELECT '
			IF @Type = 14
				SET @Sql = @Sql +
					N'[Obj], [Flid], [Ws], [Txt], [Fmt]
					FROM [MultiStr$]
					WHERE [Flid] = ' + CAST(@FieldId AS NVARCHAR(20))
			ELSE IF @Type = 18
				SET @Sql = @Sql +
					N'[Obj], [Flid], [Ws], [Txt], CAST(NULL AS VARBINARY) AS [Fmt]
					FROM [MultiBigStr$]
					WHERE [Flid] = ' + CAST(@FieldId AS NVARCHAR(20))
			ELSE IF @Type IN (23, 25)
				SET @Sql = @Sql +
					N'Owner$ AS Src, Id AS Dst
					FROM CmObject
					WHERE OwnFlid$ = ' + CAST(@FieldId AS NVARCHAR(20))
			ELSE IF @Type = 27
				SET @Sql = @Sql +
					N'Owner$ AS Src, Id AS Dst, OwnOrd$ AS Ord
					FROM CmObject
					WHERE OwnFlid$ = ' + CAST(@FieldId AS NVARCHAR(20))
			EXEC (@Sql)
		END --( IF BEGIN

		FETCH NEXT FROM ClassAttributes INTO @FieldId, @Type, @FieldName
	END --( WHILE
	CLOSE ClassAttributes
	DEALLOCATE ClassAttributes

	--( Rebuild referencing constraints to the object

	DECLARE OutsideRefs CURSOR FOR
		SELECT c.Name, f.Name, Type, Class, DstCls
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.Class
		WHERE f.DstCls = @ClassId
			AND Class != @ClassId; --( The Field$ trigger already took care of self-referencing constraints
	OPEN OutsideRefs
	FETCH NEXT FROM OutsideRefs INTO @ClassName, @FieldName, @Type, @Class, @DstCls
	WHILE @@FETCH_STATUS = 0 BEGIN
		IF @Type = 24 AND @NewClassName != 'CmObject' BEGIN
			set @sql = 'ALTER TABLE [' + @ClassName + '] ADD CONSTRAINT [_FK_' +
				+ @ClassName + '_' + @FieldName + '] ' + CHAR(13) + CHAR(9) +
				' FOREIGN KEY ([' + @FieldName + ']) REFERENCES [' + @NewClassName + '] ([Id])'
			EXECUTE (@Sql);
		END
		--( This block is slightly modified from the Field$ insert trigger
		IF @Type = 26 OR @Type = 28 BEGIN
			set @Sql = N'ALTER TABLE [' + @ClassName + '_' + @FieldName +
				N'] ADD CONSTRAINT [_FK_' + @ClassName + N'_' + @FieldName + N'_Dst] ' +
				N'FOREIGN KEY ([Dst]) REFERENCES [' + @NewClassName + N'] ([Id])'
			exec (@sql)
		END
		FETCH NEXT FROM OutsideRefs INTO @ClassName, @FieldName, @Type, @Class, @DstCls;
	END;
	CLOSE OutsideRefs;
	DEALLOCATE OutsideRefs;

	--( Rebuild foreign keys.

	DECLARE ForeignKeys CURSOR FOR
		SELECT c.Name AS DstClsName, f.Name, Type, Class, DstCls
		FROM Field$ f
		JOIN Class$ c ON c.Id = f.DstCls
		WHERE f.Class = @ClassId
			AND DstCls = @ClassId; --( The Field$ trigger already took care of self-referencing constraints
	OPEN ForeignKeys
	FETCH NEXT FROM ForeignKeys INTO @ClassName, @FieldName, @Type, @Class, @DstCls
	WHILE @@FETCH_STATUS = 0 BEGIN
		IF @Type = 24 AND @NewClassName != 'CmObject' BEGIN
			set @sql = 'ALTER TABLE [' + @NewClassName + '] ADD CONSTRAINT [_FK_' +
				+ @NewClassName + '_' + @FieldName + '] ' + CHAR(13) + CHAR(9) +
				' FOREIGN KEY ([' + @FieldName + ']) REFERENCES [' + @ClassName + '] ([Id])'
			EXECUTE (@Sql);
		END
		--( This block is slightly modified from the Field$ insert trigger
		IF @Type = 26 OR @Type = 28 BEGIN
			set @Sql = N'ALTER TABLE [' + @NewClassName + '_' + @FieldName +
				N'] ADD CONSTRAINT [_FK_' + @NewClassName + N'_' + @FieldName + N'_Dst] ' +
				N'FOREIGN KEY ([Dst]) REFERENCES [' + @ClassName + N'] ([Id])'
			exec (@sql)
		END
		FETCH NEXT FROM ForeignKeys INTO @ClassName, @FieldName, @Type, @Class, @DstCls;
	END;
	CLOSE ForeignKeys;
	DEALLOCATE ForeignKeys;

	--( Rebuild subclass referencing constraints

	SET @sql = N' '
	DECLARE Subclasses CURSOR FOR SELECT Name FROM Class$ WHERE Base = @ClassId;
	OPEN Subclasses
	FETCH NEXT FROM Subclasses INTO @ClassName
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @ForeignKey = N'_FK_' + @ClassName + N'_Id';
		IF object_id(@ForeignKey, N'F') is null BEGIN
			SET @sql = @Sql + N'ALTER TABLE ' + @ClassName + N' WITH CHECK ADD CONSTRAINT ' + @ForeignKey +
					   N' FOREIGN KEY ([Id]) REFERENCES ' + @NewClassName + N' ([Id]);';

			If @Debug = 1
				Print 'SQL: ' + @sql;

			EXECUTE sp_executesql @sql;
			SET @sql = N' '
		END

		FETCH NEXT FROM Subclasses INTO @ClassName
	END
	CLOSE Subclasses
	DEALLOCATE Subclasses

	--( Rebuild the function fnGetRefsToObj

	EXEC CreateGetRefsToObj;

	IF @Abstract = N'0' BEGIN

		--( Rebuild MakeObj_*

		EXEC GenMakeObjProc @ClassId;

		--( Rebuild the delete object trigger. It was regenerated once already,
		--( but it picks up collection and sequence references from the dummy
		--( class gets picked up.

		EXEC CreateDeleteObj @ClassId;
	END

	--==( Cleanup )==--

	SET @Sql = '
		ENABLE TRIGGER TR_Field$_UpdateModel_Del ON Field$;
		ENABLE TRIGGER TR_Field$_No_Upd ON Field$

		ALTER TABLE Class$ CHECK CONSTRAINT ALL
		ALTER TABLE ClassPar$ CHECK CONSTRAINT ALL
		ALTER TABLE Field$ CHECK CONSTRAINT ALL
		ALTER TABLE CmObject CHECK CONSTRAINT ALL
		ALTER TABLE MultiStr$ CHECK CONSTRAINT ALL
		ALTER TABLE MultiBigStr$ CHECK CONSTRAINT ALL;
		ALTER TABLE MultiBigTxt$ CHECK CONSTRAINT ALL;';
	EXEC (@Sql);
GO

---------------------------------------------------------------------------------

--( Rebuild generated stored procedures

DECLARE	@ClassId INT, @Abstract BIT, @ClassName NVARCHAR(100), @Sql NVARCHAR(4000);
DECLARE Classes CURSOR FOR
	SELECT c.Id, c.Abstract, c.Name
	FROM Class$ c
OPEN Classes;
FETCH NEXT FROM Classes INTO @ClassId, @Abstract, @ClassName;
WHILE @@FETCH_STATUS = 0 BEGIN

	--( Changes at the class level: Drop the CreateObject_ procedure and
	--( generate the MakeObj_ procedure

	IF @Abstract = 0 BEGIN
		SET @Sql = 'CreateObject_' + @ClassName
		if object_id(@Sql) is not null begin
			SET @Sql = N'DROP PROCEDURE ' + @Sql
			EXECUTE (@Sql)
		end
		EXEC GenMakeObjProc @ClassId
	END
	FETCH NEXT FROM Classes INTO @ClassId, @Abstract, @ClassName;
END
CLOSE Classes;
DEALLOCATE Classes;
GO

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200212
BEGIN
	UPDATE Version$ SET DbVer = 200213
	COMMIT TRANSACTION
	PRINT 'database updated to version 200213'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200212 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
