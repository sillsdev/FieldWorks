/***********************************************************************************************
 * CreateOwnedObject$
 *
 * Description:
 *	Creates an object given its class id.
 *
 * Paramters:
 *	If @id is null, the object id is generated and returned.
 *  If @guid is null, the guid is generated and returned.
 *
 * Returns:
 * 	0 if successful, otherwise an error
 **********************************************************************************************/


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
	select	@fAbs = [Abstract], @sTbl = [Name]
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
	if @type = kcptOwningSequence begin

		-- determine if the object(s) should be added to the end of the sequence
		if @StartObj is null begin
			select	@ownOrd = coalesce(max([OwnOrd$])+1, 1)
			from	[CmObject]
			where	[Owner$] = @Owner
				and [OwnFlid$] = @OwnFlid
		end
		else begin
			-- get the ordinal value of the object that is located where the new object is to be inserted
			select	@OwnOrd = [OwnOrd$]
			from	[CmObject]
			where	[Id] = @StartObj

			-- increment the ordinal value(s) of the object(s) in the sequence that occur at or after the new object(s)
			update	[CmObject]
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
			from	[ClassPar$] cp
			join [Class$] c on c.[Id] = cp.[Dst]
			where	cp.[Src] = @clid
				and cp.[Depth] = @depth
			set @sDynSql =  'insert into [' + @sTbl + '] ([Id]) '+
					'select [ObjId] ' +
					'from [ObjListTbl$] '+
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
			from	ObjListTbl$
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
			from	[ClassPar$] cp
			join [Class$] c on c.[Id] = cp.[Dst]
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
