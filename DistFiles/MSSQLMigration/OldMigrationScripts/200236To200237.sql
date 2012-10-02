-- Update database from version 200236 to 200237
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- LT-8851: Add field LiftResidue (BigUnicode) to classes LexEntry, LexEtymology,
-- LexExampleSentence, LexPronunciation, LexReference, LexSense, MoForm, and MoMorphSynAnalysis.
-- In FW6.0.4 fixed this migration to allow more than 19 custom fields without crashing. This will only help
--    users migrating from FW5.4 to 6.0.4. One variable was missed when this was changed in 20019To200220.sql.
-------------------------------------------------------------------------------

-------------------------------------------------------------------------------
-- LT-10571: Change NVARCHAR(1000) to NVARCHAR(MAX)
-------------------------------------------------------------------------------
if object_id('[GenMakeObjProc]') is not null begin
	print 'removing proc GenMakeObjProc'
	drop proc [GenMakeObjProc]
end
go
print 'creating proc GenMakeObjProc'
go
create proc [GenMakeObjProc]
	@clid int
as
	declare @Err int, @fIsNocountOn int
	declare @sDynSQL1 nvarchar(max), @sDynSQL2 nvarchar(max), @sDynSQL3 nvarchar(max),
		@sDynSQL3a nvarchar(max), @sDynSQL3b nvarchar(max), @sDynSQL3c nvarchar(max),
		@sDynSQL4 nvarchar(max), @sDynSQL5 nvarchar(max), @sDynSQLParamList nvarchar(max),
		@sDynSQLCustomParamList nvarchar(max)
	declare @sValuesList nvarchar(max)
	declare @fAbs tinyint, @sClass sysname, @sProcName sysname
	declare @sFieldName sysname, @nFieldType int, @flid int,
		@sFieldList nvarchar(max), @sXMLTableDef nvarchar(max)
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
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt nvarchar(max) = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt varbinary(max) = null' + N', ' + char(13)
				ELSE -- IF @nCustom = 1
					set @sDynSQLCustomParamList = @sDynSQLCustomParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt nvarchar(max) = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_fmt varbinary(max) = null' + N', ' + char(13)

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
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt nvarchar(max) = null' + N', ' + char(13)
				ELSE -- IF @nCustom = 1
					set @sDynSQLCustomParamList = @sDynSQLCustomParamList + char(9) +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_ws int = null' + N', ' +
						N'@' + @sInheritClassName + N'_' + @sFieldName + N'_txt nvarchar(max) = null' + N', ' + char(13)

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

-------------------------------------------------------------------------------
-- Add LiftResidue fields.
-------------------------------------------------------------------------------

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5002033, 19, 5002,
		null, 'LiftResidue',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5004004, 19, 5004,
		null, 'LiftResidue',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5113005, 19, 5113,
		null, 'LiftResidue',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5014007, 19, 5014,
		null, 'LiftResidue',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5016031, 19, 5016,
		null, 'LiftResidue',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5120004, 19, 5120,
		null, 'LiftResidue',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5035004, 19, 5035,
		null, 'LiftResidue',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5041004, 19, 5041,
		null, 'LiftResidue',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(24030, 19, 24,
		null, 'MatchedPairs',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(24031, 19, 24,
		null, 'PunctuationPatterns',0,Null, null, null, null)
go
---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200236
BEGIN
	UPDATE Version$ SET DbVer = 200237
	COMMIT TRANSACTION
	PRINT 'database updated to version 200237'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200236 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
