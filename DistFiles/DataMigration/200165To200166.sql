-- Remove readuncommited usage.
-- UPDATE Version$ SET DbVer = 200165
-- update database FROM version 200165 to 200166

BEGIN TRANSACTION  --( will be rolled back if wrong version#

-- Just delete these two, since they are not used.

IF OBJECT_ID('fnGetWordformParses2') IS NOT NULL BEGIN
	PRINT 'removing function fnGetWordformParses2'
	DROP FUNCTION fnGetWordformParses2
END
GO

IF OBJECT_ID('fnGetWordformParses') IS NOT NULL BEGIN
	PRINT 'removing function fnGetWordformParses'
	DROP FUNCTION fnGetWordformParses
END
GO

-- Remove readuncommited from all of these.

IF OBJECT_ID('dbo.ClearSyncTable$') IS NOT NULL BEGIN
	PRINT 'removing proc ClearSyncTable$'
	DROP PROC dbo.ClearSyncTable$
END
GO
PRINT 'creating proc ClearSyncTable$'
GO

CREATE proc [dbo].[ClearSyncTable$]
	@dbName nvarchar(4000)
as
	declare	@fIsNocountOn int

	-- check for the arbitrary case where the db name is null
	if @dbName is null return 0

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	if (not exists(select spid from master.dbo.sysprocesses sproc
		join master.dbo.sysdatabases sdb on sdb.dbid = sproc.dbid and name = @dbName
		where sproc.spid != @@spid))
		truncate table sync$

	select max(id) from sync$

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	return 0
GO

IF OBJECT_ID('CopyObj$') IS NOT NULL BEGIN
	PRINT 'removing procedure CopyObj$'
	DROP PROC [CopyObj$]
END
GO
PRINT 'creating procedure CopyObj$'
GO
CREATE PROCEDURE [dbo].[CopyObj$]
	@nTopSourceObjId INT,  	--( the top owning object
	@nTopDestOwnerId INT,	--( The ID of the owner of the top object we're creating here
	@nTopDestOwnerFlid INT,		--( The owning field ID of the top object we're creating here
	@nTopDestObjId INT OUTPUT	--( the ID for the new object we're creating here
AS

	DECLARE
		@nSourceObjId INT,
		@nOwnerID INT,
		@nOwnerFieldId INT, --(owner flid
		@nRelObjId INT,
		@nRelObjFieldId INT,  --( related object flid
		@guidNew UNIQUEIDENTIFIER,
		@nFirstOuterLoop TINYINT,
		@nDestObjId INT,
		@nOwnerDepth INT,
		@nInheritDepth INT,
		@nClass INT,
		@nvcClassName NVARCHAR(100),
		@nvcQuery NVARCHAR(4000)

	DECLARE
		@nvcFieldsList NVARCHAR(4000),
		@nvcValuesList NVARCHAR(4000),
		@nColumn INT,
		@sysColumn SYSNAME,
		@nFlid INT,
		@nType INT,
		@nSourceClassId INT,
		@nDestClassId INT,
		@nvcFieldName NVARCHAR(100),
		@nvcTableName NVARCHAR(100)

	CREATE TABLE #SourceObjs (
		[ObjId]			INT		not null,
		[ObjClass]		INT		null,
		[InheritDepth]	INT		null		DEFAULT(0),
		[OwnerDepth]	INT		null		DEFAULT(0),
		[RelObjId]		INT		null,
		[RelObjClass]	INT		null,
		[RelObjField]	INT		null,
		[RelOrder]		INT		null,
		[RelType]		INT		null,
		[OrdKey]		VARBINARY(250)	null	DEFAULT(0),
		[ClassName]		NVARCHAR(100),
		[DestinationID]	INT		NULL)

	SET @nFirstOuterLoop = 1
	SET @nOwnerId = @nTopDestOwnerId
	SET @nOwnerFieldId = @nTopDestOwnerFlid

	--== Get the Object Tree ==--

	INSERT INTO #SourceObjs
		SELECT oo.*, c.[Name], NULL
		FROM dbo.fnGetOwnedObjects$(@nTopSourceObjId, NULL, NULL, 1, 0, 1, null, 1) oo
		JOIN Class$ c ON c.[Id] = oo.[ObjClass]
		WHERE oo.[ObjClass] <> 0 -- Have to block CmObject rows, now that fnGetOwnedObjects$ returns the CmObject table.

	--== Create CmObject Records ==--

	--( Create all the CmObjects for all the objects copied

	DECLARE curNewCmObjects CURSOR FAST_FORWARD FOR
		SELECT  MAX([ObjId]), MAX([RelObjId]), MAX([RelObjField])
		FROM #SourceObjs
		GROUP BY [ObjId], [RelObjId], [RelObjField]
		ORDER BY MIN([OwnerDepth]), MAX([InheritDepth]) DESC

	OPEN curNewCmObjects
	FETCH curNewCmObjects INTO @nSourceObjId, @nRelObjId, @nRelObjFieldId
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @guidNew = NEWID()

		INSERT INTO CmObject WITH (ROWLOCK) ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
			SELECT @guidNew, [Class$], @nOwnerId, @nOwnerFieldId, [OwnOrd$]
			FROM CmObject
			WHERE [Id] = @nSourceObjId

		SET @nDestObjId = @@IDENTITY

		UPDATE #SourceObjs SET [DestinationID] = @nDestObjId WHERE [ObjId] = @nSourceObjID

		--== Copy records in "multi" string tables ==--

		--( Tables of the former MultiTxt$ table are now  set in a separate loop.

		INSERT INTO MultiStr$ WITH (ROWLOCK) ([Flid], [Obj], [Ws], [Txt], [Fmt])
		SELECT [Flid], @nDestObjID, [WS], [Txt], [Fmt]
		FROM MultiStr$
		WHERE [Obj] = @nSourceObjId

		--( As of this writing, MultiBigTxt$ is not used anywhere in the conceptual
		--( model, and it is impossible to use it through the interface.

		INSERT INTO MultiBigTxt$ WITH (ROWLOCK) ([Flid], [Obj], [Ws], [Txt])
		SELECT [Flid], @nDestObjID, [WS], [Txt]
		FROM MultiBigTxt$
		WHERE [Obj] = @nSourceObjId

		INSERT INTO MultiBigStr$ WITH (ROWLOCK) ([Flid], [Obj], [Ws], [Txt], [Fmt])
		SELECT [Flid], @nDestObjID, [WS], [Txt], [Fmt]
		FROM MultiBigStr$
		WHERE [Obj] = @nSourceObjId

		--( If the object ID = the top owning object ID
		IF @nFirstOuterLoop = 1
			SET @nTopDestObjId = @nDestObjID --( sets the output parameter

		SET @nFirstOuterLoop = 0

		--( This fetch is different than the one at the top of the loop!
		FETCH curNewCmObjects INTO @nSourceObjId, @nRelObjId, @nOwnerFieldId

		SELECT @nOwnerId = [DestinationId] FROM #SourceObjs WHERE ObjId = @nRelObjId
	END
	CLOSE curNewCmObjects
	DEALLOCATE curNewCmObjects

	--== Create All Other Records ==--

	DECLARE curRecs CURSOR FAST_FORWARD FOR
		SELECT DISTINCT [OwnerDepth], [InheritDepth], [ObjClass], [ClassName]
		FROM #SourceObjs
		ORDER BY [OwnerDepth], [InheritDepth] DESC, [ObjClass]

	OPEN curRecs
	FETCH curRecs INTO 	@nOwnerDepth, @nInheritDepth, @nClass, @nvcClassName
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @nvcFieldsList = N''
		SET @nvcValuesList = N''
		SET @nColumn = 1
		SET @sysColumn = COL_NAME(OBJECT_ID(@nvcClassName), @nColumn)
		WHILE @sysColumn IS NOT NULL BEGIN
			IF @nColumn > 1 BEGIN --( not the first time in loop
				SET @nvcFieldsList = @nvcFieldsList + N', '
				SET @nvcValuesList = @nvcValuesList + N', '
			END
			SET @nvcFieldName = N'[' + UPPER(@sysColumn) + N']'

			--( Field list for insert
			SET @nvcFieldsList = @nvcFieldsList + @nvcFieldName

			--( Vales to put into those fields
			IF @nvcFieldName = '[ID]'
				SET @nvcValuesList = @nvcValuesList + N' so.[DestinationId] '
			ELSE IF @nvcFieldName = N'[DATECREATED]' OR @nvcFieldName = N'[DATEMODIFIED]'
				SET @nvcValuesList = @nvcValuesList + N'CURRENT_TIMESTAMP'
			ELSE
				SET @nvcValuesList = @nvcValuesList + @nvcFieldName

			SET @nColumn = @nColumn + 1
			SET @sysColumn = COL_NAME(OBJECT_ID(@nvcClassName), @nColumn)
		END

		SET @nvcQuery =
			N'INSERT INTO ' + @nvcClassName + ' WITH (ROWLOCK) (
				' + @nvcFieldsList + ')
			SELECT ' + @nvcValuesList + N'
			FROM ' + @nvcClassName + ' cn
			JOIN #SourceObjs so ON
				so.[OwnerDepth] = ' + STR(@nOwnerDepth) + N' AND
				so.[InheritDepth] = ' + STR(@nInheritDepth) + N' AND
				so.[ObjClass] = ' + STR(@nClass) + N' AND
				so.[ObjId] = cn.[Id]'

		EXEC (@nvcQuery)

		--== Copy References ==--

		DECLARE curReferences CURSOR LOCAL STATIC FORWARD_ONLY READ_ONLY FOR
		SELECT f.[Id], f.[Type], f.[Name]
		FROM Field$ f
		WHERE f.[Class] = @nClass AND
			(f.[Type] = 26 OR f.[Type] = 28)

		OPEN curReferences
		FETCH curReferences INTO @nFlid, @nType, @nvcFieldName
		WHILE @@FETCH_STATUS = 0 BEGIN

			SET @nvcQuery = N'INSERT INTO ' + @nvcClassName + N'_' + @nvcFieldName + N' ([Src], [Dst]'

			IF @nType = 26
				SET @nvcQuery = @nvcQuery + N')
					SELECT DestinationId, r.[Dst]
					FROM '
			ELSE --( IF @nType = 28
				SET @nvcQuery = @nvcQuery + N', [Ord])
					SELECT DestinationId, r.[Dst], r.[Ord]
					FROM '

				SET @nvcQuery = @nvcQuery + @nvcClassName + N'_' + @nvcFieldName + N' r
					JOIN #SourceObjs ON
						[OwnerDepth] = @nOwnerDepth AND
						[InheritDepth] =  @nInheritDepth AND
						[ObjClass] = @nClass AND
						[ObjId] = r.[Src]'

				EXEC sp_executesql @nvcQuery,
					N'@nOwnerDepth INT, @nInheritDepth INT, @nClass INT',
					@nOwnerDepth, @nInheritDepth, @nClass

			FETCH curReferences INTO @nFlid, @nType, @nvcFieldName
		END
		CLOSE curReferences
		DEALLOCATE curReferences

		FETCH curRecs INTO 	@nOwnerDepth, @nInheritDepth, @nClass, @nvcClassName
	END
	CLOSE curRecs
	DEALLOCATE curRecs

	--== References to Copied Objects ==--

	--( objects can point to themselves. For instance, the People list has a
		--( researcher that points back to the People list. For copies, this reference
		--( back to itself needs to have the new object, not the old source object. For
		--( all other reference properties, the old object will do fine.

	DECLARE curRefs2CopiedObjs CURSOR LOCAL STATIC FORWARD_ONLY READ_ONLY FOR
		SELECT DISTINCT c.[Name] + N'_' + f.[Name]
		FROM Class$ c
		JOIN Field$ f ON f.[Class] = c.[Id]
		JOIN #SourceObjs s ON s.[ObjClass] = c.[Id]
		WHERE f.[Type] = 26 OR f.[Type] = 28

	OPEN curRefs2CopiedObjs
	FETCH curRefs2CopiedObjs INTO @nvcTableName
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @nvcQuery = N'UPDATE ' + @nvcTableName + N'
			SET [Dst] = #SourceObjs.[DestinationId]
			FROM ' + @nvcTableName + N', #SourceObjs
			WHERE ' + @nvcTableName + N'.[Dst] = #SourceObjs.[ObjId]'

		EXEC sp_executesql @nvcQuery

		FETCH curRefs2CopiedObjs INTO @nvcTableName
	END
	CLOSE curRefs2CopiedObjs
	DEALLOCATE curRefs2CopiedObjs

	--== MultiTxt$ Records ==--

	DECLARE curMultiTxt CURSOR FAST_FORWARD FOR
		SELECT DISTINCT ObjId, ObjClass, ClassName, DestinationId
		FROM #SourceObjs

	--( First get the object IDs and their class we're working with
	OPEN curMultiTxt
	FETCH curMultiTxt INTO @nSourceObjId, @nClass, @nvcClassName, @nDestObjId
	WHILE @@FETCH_STATUS = 0 BEGIN

		--( Now copy into each of the multitxt fields
		SELECT TOP 1 @nFlid = [Id], @nvcFieldName = [Name]
		FROM Field$
		WHERE Class = @nClass AND Type = 16
		ORDER BY [Id]

		WHILE @@ROWCOUNT > 0 BEGIN
			SET @nvcQuery =
				N'INSERT INTO ' + @nvcClassName + N'_' + @nvcFieldName + N' ' +
				N'WITH (ROWLOCK) (Obj, Ws, Txt)' + CHAR(13) +
				CHAR(9) + N'SELECT @nDestObjId, WS, Txt' + CHAR(13) +
				CHAR(9) + N'FROM ' + @nvcClassName + N'_' + @nvcFieldName + CHAR(13) +
				CHAR(9) + N'WHERE Obj = @nSourceObjId'

			EXECUTE sp_executesql @nvcQuery,
				N'@nDestObjId INT, @nSourceObjId INT',
				@nDestObjID, @nSourceObjId

			SELECT TOP 1 @nFlid = [Id], @nvcFieldName = [Name]
			FROM Field$
			WHERE [Id] > @nFlid AND Class = @nClass AND Type = 16
			ORDER BY [Id]
		END

		FETCH curMultiTxt INTO @nSourceObjId, @nClass, @nvcClassName, @nDestObjId
	END
	CLOSE curMultiTxt
	DEALLOCATE curMultiTxt

	DROP TABLE #SourceObjs
GO

if object_id('CreateOwnedObject$') is not null begin
	print 'removing proc CreateOwnedObject$'
	drop proc CreateOwnedObject$
end
go
print 'creating proc CreateOwnedObject$'
go
create proc [dbo].[CreateOwnedObject$]
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
	if @type = 27 begin

		-- determine if the object(s) should be added to the end of the sequence
		if @StartObj is null begin
			select	@ownOrd = coalesce(max([OwnOrd$])+1, 1)
			from	[CmObject] WITH (REPEATABLEREAD)
			where	[Owner$] = @Owner
				and [OwnFlid$] = @OwnFlid
		end
		else begin
			-- get the ordinal value of the object that is located where the new object is to be inserted
			select	@OwnOrd = [OwnOrd$]
			from	[CmObject] with (repeatableread)
			where	[Id] = @StartObj

			-- increment the ordinal value(s) of the object(s) in the sequence that occur at or after the new object(s)
			update	[CmObject] WITH (REPEATABLEREAD)
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

if object_id('DeletePrepDelObjects$') is not null begin
	print 'removing proc DeletePrepDelObjects$'
	drop proc [DeletePrepDelObjects$]
end
go
print 'creating proc DeletePrepDelObjects$'
go
create proc [dbo].[DeletePrepDelObjects$]
	@uid uniqueidentifier,
	@fRemoveObjInfo tinyint = 1
as
	declare @Err int, @nRowCnt int, @nTrnCnt int
	declare	@sQry nvarchar(400), @sUid nvarchar(50)
	declare	@nObjClass int, @nInheritDepth int, @nOwnerDepth int, @nOrdrAndType tinyint, @sDelClass nvarchar(100), @sDelField nvarchar(100)
	declare	@fIsNocountOn int

	DECLARE
		@nObj INT,
		@nvcTableName NVARCHAR(60),
		@nFlid INT

	set @Err = 0
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- determine if the procedure was called within a transaction; if yes then create a savepoint,
	--    otherwise create a transaction
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran DelObj$_Tran
	else save tran DelObj$_Tran
	set @Err = @@error
	if @Err <> 0 begin
		raiserror ('DeletePrepDelObjects$: SQL Error %d; Unable to create a transaction.', 16, 1, @Err)
		goto LFail
	end

	--
	-- remove strings associated with the objects that will be deleted
	--
	delete	MultiStr$
	from [ObjInfoTbl$] oi join [MultiStr$] ms on oi.[ObjId] = ms.[obj]
	where oi.[Uid] = @uid
	set @Err = @@error
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('DeletePrepDelObjects$: SQL Error %d; Unable to remove strings from the MultiStr$ table (UID=%s).', 16, 1, @Err, @sUid)
		goto LFail
	end

	--( This query finds the class of the object, and from there deteremines
	--( which multitxt fields need deleting. It gets the first property first.
	--( Any remaining multitxt properties are found in the loop.

	SELECT TOP 1
		@nObj = oi.ObjId,
		@nFlid = f.[Id],
		@nvcTableName = c.[Name] + '_' + f.[Name]
	FROM Field$ f
	JOIN Class$ c ON c.[Id] = f.Class
	JOIN CmObject o ON o.Class$ = f.Class AND Type = 16
	JOIN ObjInfoTbl$ oi ON oi.ObjId = o.[Id]
	ORDER BY f.[Id]

	SET @nRowCnt = @@ROWCOUNT
	WHILE @nRowCnt > 0 BEGIN
		SET @sQry =
			N'DELETE ' + @nvcTableName + N' WITH (REPEATABLEREAD) ' + CHAR(13) +
			CHAR(9) + N'FROM ObjInfoTbl$ oi' + CHAR(13) +
			CHAR(9) + N'JOIN ' + @nvcTableName + ' x ON x.Obj = @nObj'

		EXECUTE sp_executesql @sQry, N'@nObj INT', @nObj

		set @Err = @@error
		if @Err <> 0 begin
			raiserror ('DeleteObj$: SQL Error %d; Unable to remove strings from the MultiTxt table ', 16, 1, @Err)
			goto LFail
		end

		SELECT TOP 1
			@nObj = oi.ObjId,
			@nFlid = f.[Id],
			@nvcTableName = c.[Name] + '_' + f.[Name]
		FROM Field$ f
		JOIN Class$ c ON c.[Id] = f.Class
		JOIN CmObject o ON o.Class$ = f.Class AND Type = 16
		JOIN ObjInfoTbl$ oi ON oi.ObjId = o.[Id]
		WHERE f.[Id] > @nFlid
		ORDER BY f.[Id]

		SET @nRowCnt = @@ROWCOUNT
	END

	delete MultiBigStr$
	from [ObjInfoTbl$] oi join [MultiBigStr$] ms on oi.[ObjId] = ms.[obj]
	where oi.[Uid] = @uid
	set @Err = @@error
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('DeletePrepDelObjects$: SQL Error %d; Unable to remove strings from the MultiBigStr$ table (UID=%s).', 16, 1, @Err, @sUid)
		goto LFail
	end
	delete MultiBigTxt$
	from [ObjInfoTbl$] oi join [MultiBigTxt$] ms on oi.[ObjId] = ms.[obj]
	where oi.[Uid] = @uid
	set @Err = @@error
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('DeletePrepDelObjects$: SQL Error %d; Unable to remove strings from the MultiBigTxt$ table (UID=%s).', 16, 1, @Err, @sUid)
		goto LFail
	end

	--
	-- loop through the objects and delete all owned objects and clean-up all relationships
	--
	declare Del_Cur cursor fast_forward local for
	-- get the external classes that reference (atomic, sequences, and collections) one of the owned classes
	select	oi.ObjClass,
		min(oi.InheritDepth) as InheritDepth,
		max(oi.OwnerDepth) as OwnerDepth,
		c.Name as DelClassName,
		f.Name as DelFieldName,
		case oi.[RelType]
			when 24 then 1		-- atomic reference
			when 26 then 2	-- reference collection
			when 28 then 3	-- reference sequence
		end as OrdrAndType
	from	ObjInfoTbl$ oi (REPEATABLEREAD) join Class$ c on oi.RelObjClass = c.Id
			join Field$ f on oi.RelObjField = f.Id
	where	oi.[Uid] = @uid
		and oi.[RelType] in (24,26,28)
	group by oi.ObjClass, c.Name, f.Name, oi.RelType
	union all
	-- get internal references - the call to GetIncomingRefsPrepDel$ only found external references
	select	oi.ObjClass,
		min(oi.InheritDepth) as InheritDepth,
		max(oi.OwnerDepth) as OwnerDepth,
		c.Name as DelClassName,
		f.Name as DelFieldName,
		case oi.[RelType]
			when 24 then 1		-- atomic reference
			when 26 then 2	-- reference collection
			when 28 then 3	-- reference sequence
		end as OrdrAndType
	from	ObjInfoTbl$ oi (REPEATABLEREAD) join Field$ f on f.[DstCls] = oi.[ObjClass] and f.Type in (24,26,28)
			join Class$ c on c.[Id] = f.[Class]
	where	oi.[Uid] = @uid
		and exists (
			select	*
			from	ObjInfoTbl$ oi2
			where	oi2.[Uid] = @uid
				and oi2.[ObjClass] = f.[Class]
		)
	group by oi.ObjClass, c.Name, f.Name, oi.RelType
	union all
	-- get the classes that are referenced by the owning classes
	select	oi.ObjClass,
		min(oi.InheritDepth) as InheritDepth,
		max(oi.OwnerDepth) as OwnerDepth,
		c.Name as DelClassName,
		f.Name as DelFieldName,
		case f.[Type]
			when 26 then 4	-- reference collection
			when 28 then 5	-- reference sequence
		end as OrdrAndType
	from	[ObjInfoTbl$] oi (REPEATABLEREAD) join [Class$] c on c.[Id] = oi.[ObjClass]
			join [Field$] f on f.[Class] = c.[Id] and f.[Type] in (26, 28)
	where	oi.[Uid] = @uid
		and ( oi.[RelType] in (23,25,27) or oi.[RelType] is null )
	group by oi.ObjClass, c.Name, f.Name, f.[Type]
	union all
	-- get the owned classes
	select	oi.ObjClass,
		min(oi.InheritDepth) as InheritDepth,
		max(oi.OwnerDepth) as OwnerDepth,
		c.Name as DelClassName,
		NULL,
		6 as OrdrAndType
	from	ObjInfoTbl$ oi (REPEATABLEREAD) join Class$ c on oi.ObjClass = c.Id
	where	oi.[Uid] = @uid
		and ( oi.[RelType] in (23,25,27) or oi.[RelType] is null )
	group by oi.ObjClass, c.Name
	order by OrdrAndType, InheritDepth asc, OwnerDepth desc, DelClassName

	open Del_Cur
	fetch Del_Cur into @nObjClass, @nInheritDepth, @nOwnerDepth, @sDelClass, @sDelField, @nOrdrAndType

	while @@fetch_status = 0 begin

		-- classes that contain refence pointers to this class
		if @nOrdrAndType = 1 begin
			set @sQry='update ['+@sDelClass+'] set ['+@sDelField+']=NULL '+
				'from ['+@sDelClass+'] r '+
					'join [ObjInfoTbl$] oi on r.['+@sDelField+'] = oi.[ObjId] '
		end
		-- classes that contain sequence or collection references to this class
		else if @nOrdrAndType = 2 or @nOrdrAndType = 3 begin
			set @sQry='delete ['+@sDelClass+'_'+@sDelField+'] '+
				'from ['+@sDelClass+'_'+@sDelField+'] c '+
					'join [ObjInfoTbl$] oi on c.[Dst] = oi.[ObjId] '
		end
		-- classes that are referenced by this class's collection or sequence references
		else if @nOrdrAndType = 4 or @nOrdrAndType = 5 begin
			set @sQry='delete ['+@sDelClass+'_'+@sDelField+'] '+
				'from ['+@sDelClass+'_'+@sDelField+'] c '+
					'join [ObjInfoTbl$] oi on c.[Src] = oi.[ObjId] '
		end
		-- remove class data
		else if @nOrdrAndType = 6 begin
			set @sQry='delete ['+@sDelClass+'] '+
				'from ['+@sDelClass+'] o '+
					'join [ObjInfoTbl$] oi on o.[id] = oi.[ObjId] '
		end

		set @sQry = @sQry +
				'where oi.[ObjClass]='+convert(nvarchar(11),@nObjClass)+' '+
					'and oi.[Uid]='''+convert(varchar(250), @uid)+''''
		exec(@sQry)
		select @Err = @@error, @nRowCnt = @@rowcount

		if @Err <> 0 begin
			set @sUid = convert(nvarchar(50), @Uid)
			raiserror ('DeletePrepDelObjects$: SQL Error %d; Unable to execute dynamic SQL (UID=%s).', 16, 1, @Err, @sUid)
			goto LFail
		end

		fetch Del_Cur into @nObjClass, @nInheritDepth, @nOwnerDepth, @sDelClass, @sDelField, @nOrdrAndType
	end

	close Del_Cur
	deallocate Del_Cur

	--
	-- delete the objects in CmObject
	--
	delete CmObject
	from ObjInfoTbl$ do join CmObject co on do.[ObjId] = co.[id]

	set @Err = @@error
	if @Err <> 0 begin
		set @sUid = convert(nvarchar(50), @Uid)
		raiserror ('DeletePrepDelObjects$: SQL Error %d; Unable to remove the objects from the CmObject table (UID=%s).', 16, 1, @Err, @sUid)
		goto LFail
	end

	-- determine if the ObjInfoTbl$ should be cleaned up
	if @fRemoveObjInfo = 1 begin
		exec @Err=CleanObjInfoTbl$ @uid
		if @Err <> 0 begin
			set @sUid = convert(nvarchar(50), @Uid)
			raiserror ('DeletePrepDelObjects$: SQL Error %d; Unable to remove rows from the ObjInfoTbl$ table (UID=%s).', 16, 1, @Err, @sUid)
			goto LFail
		end
	end

	if @nTrnCnt = 0 commit tran DelObj$_Tran

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return 0

LFail:
	rollback tran DelObj$_Tran
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
go
if object_id('fnGetLastModified$') is not null begin
	print 'removing function fnGetLastModified$'
	drop function [fnGetLastModified$]
end
go
print 'creating function fnGetLastModified$'
go
create function [dbo].[fnGetLastModified$] (@ObjId int)
returns smalldatetime
as
begin
	declare @dttmLastUpdate smalldatetime

	-- get all objects owned by the specified object
	select	@dttmLastUpdate = max(co.[UpdDttm])
	from	fnGetOwnershipPath$ (@Objid, null, 1, 1, 0) oi
			join [CmObject] co on oi.[ObjId] = co.[Id]

	return @dttmLastUpdate
end
go

if object_id('fnGetObjInOwnershipPathWithId$') is not null begin
	print 'removing function fnGetObjInOwnershipPathWithId$'
	drop function [fnGetObjInOwnershipPathWithId$]
end
go
print 'creating function fnGetObjInOwnershipPathWithId$'
go
create function [dbo].[fnGetObjInOwnershipPathWithId$] (
	@objId int=null,
	@hXMLDocObjList int=null,
	@riid int )
returns @ObjInfo table (
	[ObjId]		int		not null,
	[ObjClass]	int		null,
	[InheritDepth]	int		null		default(0),
	[OwnerDepth]	int		null		default(0),
	[RelObjId]	int		null,
	[RelObjClass]	int		null,
	[RelObjField]	int		null,
	[RelOrder]	int		null,
	[RelType]	int		null,
	[OrdKey]	varbinary(250)	null		default(0)
)
as
begin
	declare	@iOwner int, @iOwnerClass int, @iCurObjId int, @iPrevObjId int

	-- determine if an object was supplied as an argument, if one was not use ObjInfoTbl$ as the list of objects
	if @objId is not null begin

		-- get the class of the specified object
		insert into @ObjInfo (ObjId, ObjClass, InheritDepth, OwnerDepth, ordkey)
		select	@objId, co.[Class$], null, null, null
		from	[CmObject] co
		where	co.[Id] = @objId
		if @@error <> 0 goto LFail
	end
	else begin

		-- parse the XML list of Object IDs and insert them into the table variable
		insert into @ObjInfo (ObjId, ObjClass, OrdKey)
		select	i.[Id], co.[Class$],
			-- calculate the order key even if @fCalcOrdKey = 0 because the overhead is very small here
			convert(varbinary, coalesce(co.[Owner$], 0)) +
			convert(varbinary, coalesce(co.[OwnFlid$], 0)) +
			convert(varbinary, coalesce(co.[OwnOrd$], 0))
		from	openxml (@hXMLDocObjList, '/root/Obj') with ([Id] int) i
			join [CmObject] co on co.[Id] = i.[Id]
		if @@error <> 0 goto LFail
	end

	select	@iCurObjId=min(ObjId)
	from	@ObjInfo

	while @iCurObjId is not null begin
		set @iPrevObjId = @iCurObjId

		-- loop up (objects that own the specified objects) through the ownership hierarchy until the specified type (class=riid) of
		-- 	owning object is found or the top of the ownership hierarchy is reached
		set @iOwnerClass = 0
		while @iOwnerClass <> @riid begin
			select top 1
				@iOwner = co.[Owner$],
				@iOwnerClass = f.[Class]
			from	[CmObject] co
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	co.[id] = @iCurObjId

			if @@rowcount > 0 set @iCurObjId = @iOwner
			else begin
				set @iCurObjId = null
				break
			end
		end

		if @iCurObjId is not null begin
			-- update the ObjInfoTbl$ so that specified object(s) is/are related to the specified type of
			--    object (class=riid) that owns it
			update	@ObjInfo
			set	[RelObjId]=@iOwner,
				[RelObjClass]=(
					select co.[Class$]
					from [CmObject] co
					where co.[id]=@iOwner)
			where	[ObjId]=@iPrevObjId
			if @@error <> 0 goto LFail
		end

		-- if the user specified an object there was only one object to process and we can therefore
		--    break out of the loop
		if @objId is not null break

		select	@iCurObjId=min(ObjId)
		from	@ObjInfo
		where	[ObjId] > @iPrevObjId
	end

	return
LFail:
	delete @ObjInfo
	return
end
go

IF OBJECT_ID('fnGetOwnedIds') IS NOT NULL BEGIN
	PRINT 'removing function fnGetOwnedIds'
	DROP FUNCTION fnGetOwnedIds
END
GO
PRINT 'creating function fnGetOwnedIds'
GO
CREATE FUNCTION [dbo].[fnGetOwnedIds] (
	@nOwner INT,
	@nTopFlid INT,
	@nSubFlid INT)
RETURNS @tblObjects TABLE (
	[Id] INT,
	Guid$ UNIQUEIDENTIFIER,
	Class$ INT ,
	Owner$ INT,
	OwnFlid$ INT,
	OwnOrd$ INT,
	UpdStmp BINARY(8),
	UpdDttm SMALLDATETIME,
	[Level] INT)
AS
BEGIN
	DECLARE
		@nLevel INT,
		@nRowCount INT

	IF @nTopFlid IS NULL
		SET @nTopFlid = 8008 --( Possibility
	IF @nSubFlid IS NULL
		SET @nSubFlid = 7004 --( Subpossibility

	--( Get the first level of owned objects
	SET @nLevel = 1

	INSERT INTO @tblObjects
	SELECT
		[Id],
		Guid$,
		Class$,
		Owner$,
		OwnFlid$,
		OwnOrd$,
		UpdStmp,
		UpdDttm,
		@nLevel
	FROM CmObject
	WHERE Owner$ = @nOwner AND OwnFlid$ = @nTopFlid --( e.g. possibility, 8008

	SET @nRowCount = @@ROWCOUNT --( Using @@ROWCOUNT alone was flakey in the loop.

	--( Get the sublevels of owned objects
	WHILE @nRowCount != 0 BEGIN

		INSERT INTO @tblObjects
		SELECT
			o.[Id],
			o.Guid$,
			o.Class$,
			o.Owner$,
			o.OwnFlid$,
			o.OwnOrd$,
			o.UpdStmp,
			o.UpdDttm,
			(@nLevel + 1)
		FROM @tblObjects obj
		JOIN CmObject o ON o.Owner$ = obj.[Id]
			AND  o.OwnFlid$ = @nSubFlid --( e.g. subpossibility, 7004
		WHERE obj.[Level] = @nLevel

		SET @nRowCount = @@ROWCOUNT
		SET @nLevel = @nLevel + 1
	END

	RETURN
END
GO
if object_id('fnGetOwnedObjects$') is not null begin
	print 'removing function fnGetOwnedObjects$'
	drop function [fnGetOwnedObjects$]
end
go
print 'creating function fnGetOwnedObjects$'
go
create function [dbo].[fnGetOwnedObjects$] (
	@ObjId int=null,
	@hXMLDocObjList int=null,
	@grfcpt int=528482304,
	@fBaseClasses tinyint=0,
	@fSubClasses tinyint=0,
	@fRecurse tinyint=1,
	@riid int=NULL,
	@fCalcOrdKey tinyint=1 )
returns @ObjInfo table (
	[ObjId]		int		not null,
	[ObjClass]	int		null,
	[InheritDepth]	int		null		default(0),
	[OwnerDepth]	int		null		default(0),
	[RelObjId]	int		null,
	[RelObjClass]	int		null,
	[RelObjField]	int		null,
	[RelOrder]	int		null,
	[RelType]	int		null,
	[OrdKey]	varbinary(250)	null		default(0)
)
as
begin
	declare @nRowCnt int
	declare	@nObjId int, @nInheritDepth int, @nOwnerDepth int, @sOrdKey varchar(250)

	-- if NULL was specified as the mask assume that all objects are desired
	if @grfcpt is null set @grfcpt = 528482304

	-- at least one and only one object must be specified somewhere - objId paramater or XML
	if @objId is null and @hXMLDocObjList is null goto LFail
	if @objId is not null and @hXMLDocObjList is not null goto LFail

	-- determine if an object was supplied as an argument, if one was not use ObjInfoTbl$ as the list of objects
	if @objId is not null begin

		-- get the class of the specified object
		insert into @ObjInfo (ObjId, ObjClass, OrdKey)
		select	@objId, co.[Class$],
			-- calculate the order key even if @fCalcOrdKey = 0 because the overhead is very small here
			convert(varbinary, coalesce(co.[Owner$], 0)) +
			convert(varbinary, coalesce(co.[OwnFlid$], 0)) +
			convert(varbinary, coalesce(co.[OwnOrd$], 0))
		from	[CmObject] co
		where	co.[Id] = @objId
		if @@error <> 0 goto LFail
	end
	else begin

		-- parse the XML list of Object IDs and insert them into the table variable
		insert into @ObjInfo (ObjId, ObjClass, OrdKey)
		select	i.[Id], co.[Class$],
			-- calculate the order key even if @fCalcOrdKey = 0 because the overhead is very small here
			convert(varbinary, coalesce(co.[Owner$], 0)) +
			convert(varbinary, coalesce(co.[OwnFlid$], 0)) +
			convert(varbinary, coalesce(co.[OwnOrd$], 0))
		from	openxml (@hXMLDocObjList, '/root/Obj') with ([Id] int) i
			join [CmObject] co on co.[Id] = i.[Id]
		if @@error <> 0 goto LFail
	end

	set @nOwnerDepth = 1
	set @nRowCnt = 1
	while @nRowCnt > 0 begin
		-- determine if the order key should be calculated - if the order key is not needed a more
		--    effecient query can be used to generate the ownership tree
		if @fCalcOrdKey = 1 begin
			-- get the objects owned at the next depth and calculate the order key
			insert	into @ObjInfo
				(ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
			select 	co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[Class], co.[OwnFlid$], co.[OwnOrd$], f.[Type],
				oi.OrdKey+convert(varbinary, co.[Owner$]) + convert(varbinary, co.[OwnFlid$]) + convert(varbinary, coalesce(co.[OwnOrd$], 0))
			from 	[CmObject] co
					join @ObjInfo oi on co.[Owner$] = oi.[ObjId]
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	oi.[OwnerDepth] = @nOwnerDepth - 1
				and ( 	( @grfcpt & 8388608 = 8388608 and f.[Type] = 23 )
					or ( @grfcpt & 33554432 = 33554432 and f.[Type] = 25 )
					or ( @grfcpt & 134217728 = 134217728 and f.[Type] = 27 )
				)
		end
		else begin
			-- get the objects owned at the next depth and do not calculate the order key
			insert	into @ObjInfo
				(ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType)
			select 	co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[Class], co.[OwnFlid$], co.[OwnOrd$], f.[Type]
			from 	[CmObject] co
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	exists (select 	*
					from 	@ObjInfo oi
					where 	oi.[ObjId] = co.[Owner$]
						and oi.[OwnerDepth] = @nOwnerDepth - 1
				)
				and ( 	( @grfcpt & 8388608 = 8388608 and f.[Type] = 23 )
					or ( @grfcpt & 33554432 = 33554432 and f.[Type] = 25 )
					or ( @grfcpt & 134217728 = 134217728 and f.[Type] = 27 )
				)
		end
		set @nRowCnt=@@rowcount

		-- determine if the whole owning tree should be included in the results
		if @fRecurse = 0 break

		set @nOwnerDepth = @nOwnerDepth + 1
	end

	--
	-- get all of the base classes of the object(s), including CmObject.
	--
	if @fBaseClasses = 1 begin
		insert	into @ObjInfo
			(ObjId, ObjClass, InheritDepth, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
		select	oi.[ObjId], p.[Dst], p.[Depth], oi.[OwnerDepth], oi.[RelObjId], oi.[RelObjClass], oi.[RelObjField], oi.[RelOrder], oi.[RelType], oi.[OrdKey]
		from	@ObjInfo oi
				join [ClassPar$] p on oi.[ObjClass] = p.[Src]
				join [Class$] c on c.[id] = p.[Dst]
		where	p.[Depth] > 0
		if @@error <> 0 goto LFail
	end
	--
	-- get all of the sub classes of the object(s)
	--
	if @fSubClasses = 1 begin
		insert	into @ObjInfo
			(ObjId, ObjClass, InheritDepth, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
		select	oi.[ObjId], p.[Src], -p.[Depth], oi.[OwnerDepth], oi.[RelObjId], oi.[RelObjClass], oi.[RelObjField], oi.[RelOrder], oi.[RelType], oi.[OrdKey]
		from	@ObjInfo oi
				join [ClassPar$] p on oi.[ObjClass] = p.[Dst] and InheritDepth = 0
				join [Class$] c on c.[id] = p.[Dst]
		where	p.[Depth] > 0
			and p.[Dst] <> 0
		if @@error <> 0 goto LFail
	end

	-- if a class was specified remove the owned objects that are not of that type of class; these objects were
	--    necessary in order to get a list of all of the referenced and referencing objects that were potentially
	--    the type of specified class
	if @riid is not null begin
		delete	@ObjInfo
		where 	not exists (
				select	*
				from	[ClassPar$] cp
				where	cp.[Dst] = @riid
					and cp.[Src] = [ObjClass]
			)
		if @@error <> 0 goto LFail
	end

	return
LFail:

	delete from @ObjInfo
	return
end
go

if object_id('fnGetOwnershipPath$') is not null begin
	print 'removing function fnGetOwnershipPath$'
	drop function [fnGetOwnershipPath$]
end
go
print 'creating function fnGetOwnershipPath$'
go
create function [dbo].[fnGetOwnershipPath$] (
	@ObjId int=null,
	@hXMLDocObjList int=null,
	@nDirection smallint=0,
	@fRecurse tinyint=1,
	@fCalcOrdKey tinyint=1 )
returns @ObjInfo table (
	[ObjId]		int		not null,
	[ObjClass]	int		null,
	[InheritDepth]	int		null		default(0),
	[OwnerDepth]	int		null		default(0),
	[RelObjId]	int		null,
	[RelObjClass]	int		null,
	[RelObjField]	int		null,
	[RelOrder]	int		null,
	[RelType]	int		null,
	[OrdKey]	varbinary(250)	null		default(0)
)
as
begin
	declare @nRowCnt int, @nOwnerDepth int

	-- determine if an object was supplied as an argument, if one was not use ObjInfoTbl$ as the list of objects
	if @objId is not null begin

		-- get the class of the specified object
		insert into @ObjInfo
			(ObjId, ObjClass, OwnerDepth, InheritDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
		select	@objId, co.[Class$], 0, 0, co.[Owner$], f.[Class], co.[OwnFlid$], co.[OwnOrd$], f.[Type],
			-- go ahead and calculate the order key for depth 0 objects even if @fCalcOrdKey=0
			convert(varbinary, coalesce(co.[Owner$], 0)) +
			convert(varbinary, coalesce(co.[OwnFlid$], 0)) +
			convert(varbinary, coalesce(co.[OwnOrd$], 0))
		from	[CmObject] co
				left outer join [Field$] f on co.[OwnFlid$] = f.[Id]
		where	co.[Id] = @objId
		if @@error <> 0 goto LFail
	end
	else begin

		-- parse the XML list of Object IDs and insert them into the table variable
		insert into @ObjInfo (ObjId, ObjClass, OrdKey)
		select	i.[Id], co.[Class$],
			-- calculate the order key even if @fCalcOrdKey = 0 because the overhead is very small here
			convert(varbinary, coalesce(co.[Owner$], 0)) +
			convert(varbinary, coalesce(co.[OwnFlid$], 0)) +
			convert(varbinary, coalesce(co.[OwnOrd$], 0))
		from	openxml (@hXMLDocObjList, '/root/Obj') with ([Id] int) i
			join [CmObject] co on co.[Id] = i.[Id]
		if @@error <> 0 goto LFail
	end

	-- determine if the objects owned by the specified object(s) should be included in the results
	if @nDirection = 0 or @nDirection = 1 begin
		set @nRowCnt = 1
		set @nOwnerDepth = 1
	end
	else set @nRowCnt = 0
	while @nRowCnt > 0 begin

		-- determine if the order key should be calculated - if the order key is not needed a more
		--    effecient query can be used to generate the ownership tree
		if @fCalcOrdKey = 1 begin
			-- get the objects owned at the next depth and calculate the order key
			insert into @ObjInfo
				(ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
			select 	co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[DstCls], co.[OwnFlid$], co.[OwnOrd$], f.[Type],
				oi.OrdKey+convert(varbinary, co.[Owner$]) + convert(varbinary, co.[OwnFlid$]) + convert(varbinary, coalesce(co.[OwnOrd$], 0))
			from 	[CmObject] co
					join @ObjInfo oi on co.[Owner$] = oi.[ObjId]
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	oi.[OwnerDepth] = @nOwnerDepth - 1
		end
		else begin
			-- get the objects owned at the next depth and do not calculate the order key
			insert into @ObjInfo
				(ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType)
			select 	co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[DstCls], co.[OwnFlid$], co.[OwnOrd$], f.[Type]
			from 	[CmObject] co
					join [Field$] f on f.[id] = co.[OwnFlid$]
			where 	exists (select 	*
					from 	@ObjInfo oi
					where 	oi.[ObjId] = co.[Owner$]
						and oi.[OwnerDepth] = @nOwnerDepth - 1
					)
		end
		set @nRowCnt = @@rowcount

		if @fRecurse = 0 break
		set @nOwnerDepth = @nOwnerDepth + 1
	end

	-- determine if the heirarchy of objects that own the specified object(s) should be included in the results
	if @nDirection = 0 or @nDirection = -1 begin
		set @nRowCnt = 1
		set @nOwnerDepth = -1
	end
	else set @nRowCnt = 0
	while @nRowCnt > 0 begin
		insert into @ObjInfo
			(ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType, OrdKey)
		select 	co.[Id], co.[Class$], @nOwnerDepth, co.[Owner$], f.[Class], co.[OwnFlid$], co.[OwnOrd$], f.[Type], 0
		from 	[CmObject] co
				left join [Field$] f on f.[id] = co.[OwnFlid$]
		-- for this query the exists clause is more effecient than a join based on the ownership depth
		where 	exists (select	*
				from	@ObjInfo oi
				where	oi.[RelObjId] = co.[Id]
					and oi.[OwnerDepth] = @nOwnerDepth + 1
				)
		set @nRowCnt = @@rowcount

		if @fRecurse = 0 break
		set @nOwnerDepth = @nOwnerDepth - 1
	end

	return
LFail:
	delete @ObjInfo

	return
end
go

if object_id('fnGetSubObjects$') is not null begin
	print 'removing function GetSubObjects$'
	drop function [fnGetSubObjects$]
end
go
print 'Creating function fnGetSubObjects$'
go
create function [dbo].[fnGetSubObjects$] (
	@ObjId int=null,
	@hXMLDocObjList int=null,
	@Flid int )
returns @ObjInfo table (
	[ObjId]		int		not null,
	[ObjClass]	int		null,
	[InheritDepth]	int		null		default(0),
	[OwnerDepth]	int		null		default(0),
	[RelObjId]	int		null,
	[RelObjClass]	int		null,
	[RelObjField]	int		null,
	[RelOrder]	int		null,
	[RelType]	int		null,
	[OrdKey]	varbinary(250)	null		default(0)
)
as
begin
	declare @nRowCnt int, @nOwnerDepth int

	-- determine if an object was supplied as an argument, if one was not use ObjInfoTbl$ as the list of objects
	if @objId is not null begin

		-- get the class of the specified object
		insert into @ObjInfo (ObjId, ObjClass)
		select	@objId, co.[Class$]
		from	[CmObject] co
		where	co.[Id] = @objId
		if @@error <> 0 goto LFail
	end
	else begin

		-- parse the XML list of Object IDs and insert them into the table variable
		insert into @ObjInfo (ObjId, ObjClass, OrdKey)
		select	i.[Id], co.[Class$],
			-- calculate the order key even if @fCalcOrdKey = 0 because the overhead is very small here
			convert(varbinary, coalesce(co.[Owner$], 0)) +
			convert(varbinary, coalesce(co.[OwnFlid$], 0)) +
			convert(varbinary, coalesce(co.[OwnOrd$], 0))
		from	openxml (@hXMLDocObjList, '/root/Obj') with ([Id] int) i
			join [CmObject] co on co.[Id] = i.[Id]
		if @@error <> 0 goto LFail
	end

	-- loop through the ownership hiearchy for all sub-objects based on the specified flid (field ID)
	set @nRowCnt = 1
	set @nOwnerDepth = 1
	while @nRowCnt > 0 begin
		insert into @ObjInfo
			(ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, RelObjField, RelOrder, RelType)
		select 	co.[id], co.[Class$], @nOwnerDepth, co.[Owner$], null, co.[OwnFlid$], co.[OwnOrd$], 25
		from 	[CmObject] co
		where 	co.[OwnFlid$] = @flid
			and exists (
				select 	*
				from 	@ObjInfo oi
				where 	oi.[ObjId] = co.[Owner$]
					and oi.[OwnerDepth] = @nOwnerDepth - 1
				)
		set @nRowCnt = @@rowcount
		set @nOwnerDepth = @nOwnerDepth + 1
	end

	return
LFail:
	delete @ObjInfo
	return
end
go

if object_id('fnIsInOwnershipPath$') is not null begin
	print 'removing function fnIsInOwnershipPath$'
	drop function [fnIsInOwnershipPath$]
end
go
print 'creating function fnIsInOwnershipPath$'
go
create function [dbo].[fnIsInOwnershipPath$] (
	@ObjId int,
	@OwnerObjId int )
returns tinyint
as
begin
	declare @nRowCnt int, @nOwnerDepth int
	declare @fInPath tinyint
	declare @ObjInfo table (
		[ObjId]		int		not null,
		[ObjClass]	int		null,
		[InheritDepth]	int		null		default(0),
		[OwnerDepth]	int		null		default(0),
		[RelObjId]	int		null,
		[RelObjClass]	int		null,
		[RelObjField]	int		null,
		[RelOrder]	int		null,
		[RelType]	int		null,
		[OrdKey]	varbinary(250)	null		default(0) )

	set @fInPath = 0

	-- get the class of the specified object
	insert into @ObjInfo (ObjId, ObjClass)
	select	@OwnerObjId, co.[Class$]
	from	[CmObject] co
	where	co.[Id] = @OwnerObjId
	if @@error <> 0 goto LFail

	set @nRowCnt = 1
	set @nOwnerDepth = 1
	while @nRowCnt > 0 begin
		-- determine if one of the objects at the current depth owns the specified object, if
		--    one does we can exit here
		if exists (
			select	*
			from	[CmObject] co
					join @ObjInfo oi on co.[Owner$] = oi.[ObjId]
			where	oi.[OwnerDepth] = @nOwnerDepth - 1
				and co.[Id] = @ObjId
			)
		begin
			set @fInPath = 1
			goto Finish
		end

		-- add all of the objects owned at the next depth to the object list
		insert	into @ObjInfo (ObjId, ObjClass, OwnerDepth, RelObjId)
		select 	co.[id], co.[Class$], @nOwnerDepth, co.[Owner$]
		from 	[CmObject] co
		where 	exists (select	*
				from 	@ObjInfo oi
				where 	oi.[ObjId] = co.[Owner$]
					and oi.[OwnerDepth] = @nOwnerDepth - 1
				)
		set @nRowCnt = @@rowcount

		set @nOwnerDepth = @nOwnerDepth + 1
	end

Finish:
	return @fInPath
LFail:
	return -1
end
go

if object_id('GetLinkedObjs$') is not null begin
	print 'removing proc GetLinkedObjs$'
	drop proc [GetLinkedObjs$]
end
go
print 'creating proc GetLinkedObjs$'
go
create proc [dbo].[GetLinkedObjs$]
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
		@sOrderField sysname, @sOrdKey varchar(502)
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
	if ( @ObjId is null and @hXMLDocObjList is null ) or ( @ObjId is not null and @hXMLDocObjList is not null )
		goto LFail

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
				-- Note the COLLATE statement is required here when using database servers with non-US collation!!
				master.dbo.fn_varbintohexstr(oi.[OrdKey]) COLLATE SQL_Latin1_General_CP1_CI_AS
			from 	#OwnedObjsInfo$ oi
					join [Field$] f on f.[DstCls] = oi.[ObjClass]
						and ( 	( f.[type] = 24 and @grfcpt & 16777216 = 16777216 )
							or ( f.[type] = 26 and @grfcpt & 67108864 = 67108864 )
							or ( f.[type] = 28 and @grfcpt & 268435456 = 268435456 )
						)
					join [Class$] c on f.[Class] = c.[Id]
			union all
			-- get the classes that are referenced (atomic, sequences, and collections) by this class
			select	oi.[ObjId], oi.[ObjClass], oi.[InheritDepth], oi.[OwnerDepth], f.[Type], c.[Name], f.[DstCls], f.[Name], f.[Id],
				2, -- referenced by this class
				-- Note the COLLATE statement is required here when using database servers with non-US collation!!
				master.dbo.fn_varbintohexstr(oi.[OrdKey]) COLLATE SQL_Latin1_General_CP1_CI_AS
			from	#OwnedObjsInfo$ oi
					join [Class$] c on c.[Id] = oi.[ObjClass]
					join [Field$] f on f.[Class] = c.[Id]
						and ( 	( f.[type] = 24 and @grfcpt & 16777216 = 16777216 )
							or ( f.[type] = 26 and @grfcpt & 67108864 = 67108864 )
							or ( f.[type] = 28 and @grfcpt & 268435456 = 268435456 )
						)
			order by oi.[ObjId]
		end
		else if @nRefDirection = 1 begin
			declare GetClassRefObj_cur cursor local static forward_only read_only for
			-- get the classes that are referenced (atomic, sequences, and collections) by these classes
			select	oi.[ObjId], oi.[ObjClass], oi.[InheritDepth], oi.[OwnerDepth], f.[Type], c.[Name], f.[DstCls], f.[Name], f.[Id],
				2, -- referenced by this class
				-- Note the COLLATE statement is required here when using database servers with non-US collation!!
				master.dbo.fn_varbintohexstr(oi.[OrdKey]) COLLATE SQL_Latin1_General_CP1_CI_AS
			from	#OwnedObjsInfo$ oi
					join [Class$] c on c.[Id] = oi.[ObjClass]
					join [Field$] f on f.[Class] = c.[Id]
						and ( 	( f.[type] = 24 and @grfcpt & 16777216 = 16777216 )
							or ( f.[type] = 26 and @grfcpt & 67108864 = 67108864 )
							or ( f.[type] = 28 and @grfcpt & 268435456 = 268435456 )
						)
			order by oi.[ObjId]
		end
		else begin
			declare GetClassRefObj_cur cursor local static forward_only read_only for
			-- get the classes that reference (atomic, sequences, and collections) these classes
			-- do not include internal references between objects within the owning object hierarchy;
			--	this will be handled below
			select 	oi.[ObjId], oi.[ObjClass], oi.[InheritDepth], oi.[OwnerDepth], f.[Type], c.[Name], c.[Id], f.[Name], f.[Id],
				1, -- references this class
				-- Note the COLLATE statement is required here when using database servers with non-US collation!!
				master.dbo.fn_varbintohexstr(oi.[OrdKey]) COLLATE SQL_Latin1_General_CP1_CI_AS
			from 	#OwnedObjsInfo$ oi
					join [Field$] f on f.[DstCls] = oi.[ObjClass]
						and ( 	( f.[type] = 24 and @grfcpt & 16777216 = 16777216 )
							or ( f.[type] = 26 and @grfcpt & 67108864 = 67108864 )
							or ( f.[type] = 28 and @grfcpt & 268435456 = 268435456 )
						)
					join [Class$] c on f.[Class] = c.[Id]
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
						'from ['+@sClass+'] t '+
						'where ['+@sField+']='+convert(nvarchar(11),@nObjId)

					-- determine if only external references should be included - don't included
					--	references between objects within the owning hierarchy
					if @nRefDirection = -1 begin
						set @sQry = @sQry + 'and not exists (' +
								'select * from #OwnedObjsInfo$ oi ' +
								'where oi.[ObjId]=t.[Id] ' +
									'and oi.[RelType] not in (' +
									convert(nvarchar(11), 16777216)+',' +
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
						'from ['+@sClass+'] ' +
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
						'from ['+@sClass+'_'+@sField+'] t '+
						'where t.[dst]='+convert(nvarchar(11),@nObjId)

					-- determine if only external references should be included - don't included
					--	references between objects within the owning hierarchy
					if @nRefDirection = -1 begin
						set @sQry = @sQry + 'and not exists (' +
								'select * from #OwnedObjsInfo$ oi ' +
								'where oi.[ObjId]=t.[Src] ' +
									'and oi.[RelType] not in (' +
									convert(nvarchar(11), 16777216)+',' +
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
						'from ['+@sClass+'_'+@sField+'] '+
						'where [src]='+convert(nvarchar(11),@nObjId)
				end
			end

			exec (@sQry)
			set @Err = @@error
			if @Err <> 0 begin
				raiserror ('GetLinkedObjs$: SQL Error %d; Error performing dynamic SQL.', 16, 1, @Err)

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
			raiserror ('GetLinkedObjs$: SQL Error %d; Unable to remove objects that are not the specified class %d.', 16, 1, @Err, @riid)
			goto LFail
		end
	end

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
go
if object_id('GetPossibilities') is not null begin
	print 'removing proc GetPossibilities'
	drop proc [GetPossibilities]
end
go
print 'creating proc GetPossibilities'
go
create proc [dbo].[GetPossibilities]
	@ObjId int,
	@Ws int
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

	-- First return a count so that the caller can preallocate memory for the results.
	select count(*) from @tblObjInfo

	--
	--  get an ordered list of relevant writing system codes
	--
	declare @tblWs table (
		[WsId]	int not null, -- don't make unique. It shouldn't happen, but we don't want a crash if it does.
		[Ord]	int primary key clustered identity(1,1))
	--( 0xffffffff (-1) or 0xfffffffd (-3) = First string from a) ordered checked analysis
	-- writing systems b) any remaining analysis writing systems or stars if none of the above.
	if @Ws = 0xffffffff or @Ws = 0xfffffffd begin
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentAnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id
			order by caws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_AnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	--( 0xfffffffe (-2) or 0xfffffffc (-4) = First string from a) ordered checked vernacular
	-- writing systems b) any remaining vernacular writing systems or stars if none of the above.
	else if @Ws = 0xfffffffe or @Ws = 0xfffffffc begin
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentVernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id
			order by cvws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_VernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	--( 0xfffffffb = -5 = First string from a) ordered checked analysis writing systems
	-- b) ordered checked vernacular writing systems, c) any remaining analysis writing systems,
	-- d) any remaining vernacular writing systems or stars if none of the above.
	else if @Ws = 0xfffffffb begin
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentAnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id
			order by caws.Ord
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentVernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id
			where lws.id not in (select WsId from @tblWs)
			order by cvws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_AnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id and lws.id not in (select WsId from @tblWs)
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_VernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	--( 0xfffffffa = -6 = First string from a) ordered checked vernacular writing systems
	-- b) ordered checked analysis writing systems, c) any remaining vernacular writing systems,
	-- d) any remaining analysis writing systems or stars if none of the above.
	else if @Ws = 0xfffffffa begin
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentVernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id
			order by cvws.Ord
		insert into @tblWs (WsId)
			select lws.id
			from LanguageProject_CurrentAnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id
			where lws.id not in (select WsId from @tblWs)
			order by caws.Ord
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_VernacularWritingSystems cvws
			join LgWritingSystem lws on cvws.dst = lws.id and lws.id not in (select WsId from @tblWs)
		insert into @tblWs (WsId)
			select distinct lws.id
			from LanguageProject_AnalysisWritingSystems caws
			join LgWritingSystem lws on caws.dst = lws.id and lws.id not in (select WsId from @tblWs)
	end
	else -- Hard coded value
		insert into @tblWs (WsId) Values(@Ws)

	-- Now that we have the desired writing systems in @tblWs, we can return the desired information.
	select
		o.ObjId,
		(select top 1 isnull(ca.[txt], '***') + ' - ' + isnull(cn.[txt], '***')
			from LgWritingSystem lws
			left outer join CmPossibility_Name cn on cn.[ws] = lws.[Id] and cn.[Obj] = o.[objId]
			left outer join CmPossibility_Abbreviation ca on ca.[ws] = lws.[Id] and ca.[Obj] = o.[objId]
			join @tblWs wstbl on wstbl.WsId = lws.id
			order by (
				select [Ord] = CASE
					WHEN cn.[txt] IS NOT NULL THEN wstbl.[ord]
					WHEN ca.[txt] IS NOT NULL THEN wstbl.[ord] + 9000
					ELSE wstbl.[Ord] + 99000
					END)),
		isnull((select top 1 lws.id
			from LgWritingSystem lws
			left outer join CmPossibility_Name cn on cn.[ws] = lws.[Id] and cn.[Obj] = o.[objId]
			left outer join CmPossibility_Abbreviation ca on ca.[ws] = lws.[Id] and ca.[Obj] = o.[objId]
			join @tblWs wstbl on wstbl.WsId = lws.id
			order by (
				select [Ord] = CASE
					WHEN cn.[txt] IS NOT NULL THEN wstbl.[ord]
					WHEN ca.[txt] IS NOT NULL THEN wstbl.[ord] + 9000
					ELSE wstbl.[Ord] + 99000
					END)
			), (select top 1 WsId from @tblws)),
		o.OrdKey, cp.ForeColor, cp.BackColor, cp.UnderColor, cp.UnderStyle
	from @tblObjInfo o
		left outer join CmPossibility cp on cp.[id] = o.[objId]
	order by o.OrdKey

	return @retval
go

if object_id('GetPossKeyword') is not null begin
	print 'removing proc GetPossKeyword'
	drop proc [GetPossKeyword]
end
go
print 'creating proc GetPossKeyword'
go
create proc [dbo].[GetPossKeyword]
	@ObjId int,
	@Ws int,
	@sKeyword nvarchar(250)
as
	declare @retval int

	-- get all of the possibilities owned by the specified possibility list object

	declare @tblObjInfo table (
		[ObjId]	int not null,
		[ObjClass] int null,
		[InheritDepth] int null default(0),
		[OwnerDepth] int null default(0),
		[RelObjId] int null,
		[RelObjClass] int null,
		[RelObjField] int null,
		[RelOrder] int null,
		[RelType] int null,
		[OrdKey] varbinary(250)	null default(0))


	insert into @tblObjInfo
		select * from fnGetOwnedObjects$(@ObjId, null, 176160768, 1, 0, 1, null, 1)

	-- Fudge this for now so it doesn't crash.
	if @Ws = 0xffffffff or @Ws = 0xfffffffd or @Ws = 0xfffffffb

		--( To avoid seeing stars, send a "magic" writing system of 0xffffffff.
		--( This will cause the query to return the first non-null string.
		--( Priority is givin to encodings with the highest order.

		select
			o.ObjId,
			isnull((select top 1 txt
				from CmPossibility_Name cn
				left outer join LgWritingSystem le on le.[Id] = cn.[ws]
				left outer join LanguageProject_AnalysisWritingSystems lpaws on lpaws.[dst] = le.[id]
				left outer join LanguageProject_CurrentAnalysisWritingSystems lpcaws on lpcaws.[dst] = lpaws.[dst]
				where cn.[Obj] = o.[objId] and cn.[Txt] like '%' + @sKeyword + '%'
				order by isnull(lpcaws.[ord], 99999)), '***'),
			isnull((select top 1 txt
				from CmPossibility_Abbreviation ca
				left outer join LgWritingSystem le on le.[Id] = ca.[ws]
				left outer join LanguageProject_AnalysisWritingSystems lpaws on lpaws.[dst] = le.[id]
				left outer join LanguageProject_CurrentAnalysisWritingSystems lpcaws on lpcaws.[dst] = lpaws.[dst]
				where ca.[Obj] = o.[objId]
				order by isnull(lpcaws.[ord], 99999)), '***'),
			o.OrdKey
		from @tblObjInfo o
		where o.[ObjClass] = 7  -- CmPossibility
		order by o.OrdKey

	else if @Ws = 0xfffffffe or @Ws = 0xfffffffc or @Ws = 0xfffffffa

		--( To avoid seeing stars, send a "magic" writing system of 0xfffffffe.
		--( This will cause the query to return the first non-null string.
		--( Priority is givin to encodings with the highest order.

		select
			o.ObjId,
			isnull((select top 1 txt
				from CmPossibility_Name cn
				left outer join LgWritingSystem le on le.[Id] = cn.[ws]
				left outer join LanguageProject_VernacularWritingSystems lpvws on lpvws.[dst] = le.[id]
				left outer join LanguageProject_CurrentVernacularWritingSystems lpcvws on lpcvws.[dst] = lpvws.[dst]
				where cn.[Obj] = o.[objId] and cn.[Txt] like '%' + @sKeyword + '%'
				order by isnull(lpcvws.[ord], 99999)), '***'),
			isnull((select top 1 txt
				from CmPossibility_Abbreviation ca
				left outer join LgWritingSystem le on le.[Id] = ca.[ws]
				left outer join LanguageProject_VernacularWritingSystems lpvws on lpvws.[dst] = le.[id]
				left outer join LanguageProject_CurrentVernacularWritingSystems lpcvws on lpcvws.[dst] = lpvws.[dst]
				where ca.[Obj] = o.[objId]
				order by isnull(lpcvws.[ord], 99999)), '***'),
			o.OrdKey
		from @tblObjInfo o
		where o.[ObjClass] = 7  -- CmPossibility
		order by o.OrdKey

	else
		select	o.ObjId, isnull(cn.txt, '***'), isnull(ca.txt, '***'), o.OrdKey
			from @tblObjInfo o
				left outer join [CmPossibility_Name] cn
					on cn.[Obj] = o.[ObjId] and cn.[Ws] = @Ws
				left outer join [CmPossibility_Abbreviation] ca
					on ca.[Obj] = o.[ObjId] and ca.[Ws] = @Ws
			where o.[ObjClass] = 7  -- CmPossibility
				and cn.[Txt] like '%' + @sKeyword + '%'
			order by o.OrdKey

	return @retval
go
if object_id('GetTagInfo$') is not null begin
	print 'removing procedure GetTagInfo$'
	drop proc [GetTagInfo$]
end
go
print 'creating proc GetTagInfo$'
go
create proc [dbo].[GetTagInfo$]
	@iOwnerId int,
	@iWritingSystem int
as

	declare @fIsNocountOn int
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- TODO (SteveM) This needs to be fixed to handle 0xfffffffb and 0xfffffffa properly.
	--( if "magic" writing system is for analysis encodings
	if @iWritingSystem = 0xffffffff or @iWritingSystem = 0xfffffffd or @iWritingSystem = 0xfffffffb
		select
			[co].[Guid$],
			[opi].[Dst],
			isnull((select top 1 [ca].[txt]
				from CmPossibility_Abbreviation ca
				left outer join LgWritingSystem le
					on le.[Id] = ca.[ws]
				left outer join LanguageProject_AnalysisWritingSystems lpaws
					on lpaws.[dst] = le.[id]
				left outer join LanguageProject_CurrentAnalysisWritingSystems lpcaws
					on lpcaws.[dst] = lpaws.[dst]
				where ca.[Obj] = [opi].[Dst]
				order by isnull(lpcaws.[ord], 99999)), '***'),
			isnull((select top 1 [cn].[txt]
				from CmPossibility_Name cn
				left outer join LgWritingSystem le
					on le.[Id] = cn.[ws]
				left outer join LanguageProject_AnalysisWritingSystems lpaws
					on lpaws.[dst] = le.[id]
				left outer join LanguageProject_CurrentAnalysisWritingSystems lpcaws
					on lpcaws.[dst] = lpaws.[dst]
				where cn.[Obj] = [opi].[Dst]
				order by isnull(lpcaws.[ord], 99999)), '***'),
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

	--( if "magic" writing system is for vernacular encodings
	else if @iWritingSystem = 0xfffffffe or @iWritingSystem = 0xfffffffc or @iWritingSystem = 0xfffffffa
		select
			[co].[Guid$],
			[opi].[Dst],
			isnull((select top 1 txt
				from CmPossibility_Abbreviation ca
				left outer join LgWritingSystem le
					on le.[Id] = ca.[ws]
				left outer join LanguageProject_VernacularWritingSystems lpvws
					on lpvws.[dst] = le.[id]
				left outer join LanguageProject_CurrentVernacularWritingSystems lpcvws
					on lpcvws.[dst] = lpvws.[dst]
				where ca.[Obj] = [opi].[Dst]
				order by isnull(lpcvws.[ord], 99999)), '***'),
			isnull((select top 1 txt
				from CmPossibility_Name cn
				left outer join LgWritingSystem le
					on le.[Id] = cn.[ws]
				left outer join LanguageProject_VernacularWritingSystems lpvws
					on lpvws.[dst] = le.[id]
				left outer join LanguageProject_CurrentVernacularWritingSystems lpcvws
					on lpcvws.[dst] = lpvws.[dst]
				where cn.[Obj] = [opi].[Dst]
				order by isnull(lpcvws.[ord], 99999)), '***'),
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

	--( if one particular writing system is wanted
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
			left outer join CmPossibility_Abbreviation [ca]
				on [ca].[Obj] = [opi].[Dst] and [ca].[ws] = @iWritingSystem
			left outer join CmPossibility_Name cn
				on [cn].[Obj] = [opi].[Dst] and [cn].[ws] = @iWritingSystem
		where [opi].[Src] = @iOwnerId
		order by [opi].[Dst]

	--( if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

LFail:
	--( if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
go

if object_id('IsValidObject$') is not null begin
	print 'removing proc IsValidObject$'
	drop proc [IsValidObject$]
end
go
print 'creating proc IsValidObject$'
go
create proc [dbo].[IsValidObject$]
	@idOfObjectToCheck int,
	@class int,
	@fValid int out
as
	DECLARE @actualClass int

	select @actualClass = class$ from CmObject where id = @idOfObjectToCheck
	if @class = @actualclass
		set @fValid = 1
	else
		exec ClassIsDerivedFrom$ @actualClass, @class, @fValid out
GO
if object_id('ObjInfoTbl$_Owned') is not null begin
	print 'removing view ObjInfoTbl$_Owned'
	drop view [ObjInfoTbl$_Owned]
end
go
print 'creating view ObjInfoTbl$_Owned'
go
create view [dbo].[ObjInfoTbl$_Owned]
as
	select	*
	from	ObjInfoTbl$
	where	[RelType] in (23, 25, 27)
go
if object_id('ObjInfoTbl$_Ref') is not null begin
	print 'removing view ObjInfoTbl$_Ref'
	drop view [ObjInfoTbl$_Ref]
end
go
print 'creating view ObjInfoTbl$_Ref'
go
create view [dbo].[ObjInfoTbl$_Ref]
as
	select	*
	from	ObjInfoTbl$
	where	[RelType] in (24, 26, 28)
go
if object_id('DefineReplaceRefSeqProc$') is not null begin
	print 'removing proc DefineReplaceRefSeqProc$'
	drop proc [DefineReplaceRefSeqProc$]
end
go
print 'Creating proc DefineReplaceRefSeqProc$'
go
create proc [dbo].[DefineReplaceRefSeqProc$]
	@sTbl sysname,
	@flid int
as
	declare @sDynSql nvarchar(4000), @sDynSql2 nvarchar(4000), @sDynSql3 nvarchar(4000), @sDynSql4 nvarchar(4000)
	declare @err int

	if object_id('ReplaceRefSeq_' + @sTbl) is not null begin
		set @sDynSql = 'alter '
	end
	else begin
		set @sDynSql = 'create '
	end

set @sDynSql = @sDynSql +
N'proc ReplaceRefSeq_' + @sTbl +'
	@SrcObjId int,
	@ListStmp int,
	@hXMLdoc int = null,
	@StartObj int = null,
	@StartObjOccurrence int = 1,
	@EndObj int = null,
	@EndObjOccurrence int = 1,
	@fRemoveXMLdoc tinyint = 1
as
	declare	@fIsNocountOn int, @Err int, @nTrnCnt int, @sTranName varchar(300)
	declare @nNumObjs int, @iCurObj int, @nMinOrd int, @StartOrd int, @EndOrd int
	declare @nSpaceAvail int
	declare @UpdStmp int

	set @Err = 0

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on
'

set @sDynSql = @sDynSql +
N'
	-- determine if the procedure was called within a transaction; if yes then create a savepoint,
	--	otherwise create a transaction
	set @sTranName = ''ReplaceRefSeq_'+@sTbl+''' + convert(varchar(11), @@trancount)
	set @nTrnCnt = @@trancount
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName
	set @Err = @@error
	if @Err <> 0 begin
		raiserror(''ReplaceRefSeq_'+@sTbl+': SQL Error %d; Unable to create a transaction'', 16, 1, @Err)
		goto LFail
	end

	-- get the starting and ending ordinal values
	set @EndOrd = null
	set @StartOrd = null
	if @StartObj is null begin
		-- since the @StartObj is null the list of objects should be added to the end of the sequence, so
		--	get the maximum ord value and add 1
		select	@StartOrd = coalesce(max([Ord]), 0) + 1
		from	['+@sTbl+'] with (REPEATABLEREAD)
		where	[Src] = @SrcObjId
	end
	else begin
		-- create a temporary table to hold all of the ord values associated with Src=@SrcObjId and (Dst=
		--	@StartObj or Dst=@EndObj); this table will have an identity column so subsequent queries
		--	can easily determine which ord value is associated with a particular position in a sequence
		declare @t table (
			Occurrence int identity(1,1),
			IsStart	tinyint,
			Ord int
		)

'
set @sDynSql2 = N'
		-- determine if an end object was not specified, or if the start and end object are the same
		if @EndObj is null or (@EndObj = @StartObj) begin
			-- only collect occurrences for the start object

			-- limit the number of returned rows from a select based on the desired occurrence; this will
			--	avoid processing beyond the desired occurrence
			if @EndObj is null set rowcount @StartObjOccurrence
			else set rowcount @EndObjOccurrence

			-- insert all of the Ord values associated with @StartObj
			insert into @t (IsStart, Ord)
			select	1, [Ord]
			from	[' + @sTbl + ']
			where	[Src] = @SrcObjId
				and [Dst] = @StartObj
			order by [Ord]
			set @Err = @@error
			if @Err <> 0 begin
				raiserror(''ReplaceRefSeq_'+@sTbl+': SQL Error %d; Unable to insert ord values into the temporary table'', 16, 1, @Err)
				goto LFail
			end

			-- make selects return all rows
			set rowcount 0

			-- determine if the end and start objects are the same; if they are then search for the
			--	end object''s ord value based on the specified occurrence
			if @EndObj = @StartObj begin
				select	@EndOrd = [Ord]
				from	@t
				where	[Occurrence] = @EndObjOccurrence
			end
		end
		else begin
			-- insert Ord values associated with @StartObj and @EndObj
			insert into @t ([IsStart], [Ord])
			select	case [Dst]
					when @StartObj then 1
					else 0
				end,
				[Ord]
			from	[' + @sTbl + ']
			where	[Src] = @SrcObjId
				and ( [Dst] = @StartObj
					or [Dst] = @EndObj )
			order by 1 desc, [Ord]
			set @Err = @@error
			if @Err <> 0 begin
				raiserror(''ReplaceRefSeq_'+@sTbl+': SQL Error %d; Unable to insert ord values into the temporary table'', 16, 1, @Err)
				goto LFail
			end

			-- get the end ord value associated with @EndObjOccurrence
			select	@EndOrd = [Ord]
			from	@t
			where	[IsStart] = 0
				and [Occurrence] = @EndObjOccurrence +
					( select max([Occurrence]) from @t where [IsStart] = 1 )
		end

		-- get the start ord value associated with @StartObjOccurrence
		select	@StartOrd = [Ord]
		from	@t
		where	[IsStart] = 1
			and [Occurrence] = @StartObjOccurrence

	end
'
set @sDynSql3 = N'
	-- validate the arguments
	if @StartOrd is null begin
		raiserror(''ReplaceRefSeq_'+@sTbl+': Unable to locate ordinal value: SrcObjId(Src) = %d, StartObj(Dst) = %d, StartObjOccurrence = %d'',
				16, 1, @SrcObjId, @StartObj, @StartObjOccurrence)
		set @Err = 50001
		goto LFail
	end
	if @EndOrd is null and @EndObj is not null begin
		raiserror(''ReplaceRefSeq_'+@sTbl+': Unable to locate ordinal value: SrcObjId(Src) = %d, EndObj(Dst) = %d, EndObjOccurrence = %d'',
				16, 1, @SrcObjId, @EndObj, @EndObjOccurrence)
		set @Err = 50002
		goto LFail
	end
	if @EndOrd is not null and @EndOrd < @StartOrd begin
		raiserror(''ReplaceRefSeq_'+@sTbl+': The starting ordinal value %d is greater than the ending ordinal value %d: SrcObjId(Src) = %d, StartObj(Dst) = %d, StartObjOccurrence = %d, EndObj(Dst) = %d, EndObjOccurrence = %d'',
				16, 1, @StartOrd, @EndOrd, @SrcObjId, @StartObj, @StartObjOccurrence, @EndObj, @EndObjOccurrence)
		set @Err = 50003
		goto LFail
	end

	-- check for a delete/replace
	if @EndObj is not null begin

		delete	[' + @sTbl + '] with (REPEATABLEREAD)
		where	[Src] = @SrcObjId
			and [Ord] >= @StartOrd
			and [Ord] <= @EndOrd
		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplaceRefSeq_'+@sTbl+': SQL Error %d; Unable to remove objects between %d and %d for source = %d'',
					16, 1, @Err, @StartOrd, @EndOrd, @SrcObjId)
			goto LFail
		end
	end

	-- determine if any objects are going to be inserted
	if @hXMLDoc is not null begin
		-- get the number of objects to be inserted
		select	@nNumObjs = count(*)
		from 	openxml(@hXMLdoc, ''/root/Obj'') with (Id int)
		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplaceRefSeq_'+@sTbl+': SQL Error %d; Unable to process XML document: document handle = %d'',
					16, 1, @hXMLdoc)
			goto LFail
		end
'
set @sDynSql4 = N'
		-- if the objects are not appended to the end of the list then determine if there is enough room
		if @StartObj is not null begin

			-- find the largest ordinal value less than the start object''s ordinal value
			select	@nMinOrd = coalesce(max([Ord]), -1) + 1
			from	['+@sTbl+'] with (REPEATABLEREAD)
			where	[Src] = @SrcObjId
				and [Ord] < @StartOrd

			-- determine if a range of objects was deleted; if objects were deleted then there is more room
			--	available
			if @EndObj is not null begin
				-- the actual space available could be determined, but this would involve another
				--	query (this query would look for the minimum Ord value greater than @EndOrd);
				--	however, it is known that at least up to the @EndObj is available

				set @nSpaceAvail = @EndOrd - @nMinOrd
				if @nMinOrd > 0 set @nSpaceAvail = @nSpaceAvail + 1
			end
			else begin
				set @nSpaceAvail = @StartOrd - @nMinOrd
			end

			-- determine if space needs to be made
			if @nSpaceAvail < @nNumObjs begin
				update	[' + @sTbl + '] with (REPEATABLEREAD)
				set	[Ord] = [Ord] + @nNumObjs - @nSpaceAvail
				where	[Src] = @SrcObjId
					and [Ord] >= @nMinOrd
				set @Err = @@error
				if @Err <> 0 begin
					raiserror(''ReplaceRefSeq_'+@sTbl+': SQL Error %d; Unable to increment the ordinal values; src = %d'',
							16, 1, @Err, @SrcObjId)
					goto LFail
				end
			end
		end
		else begin
			-- find the largest ordinal value plus one
			select	@nMinOrd = coalesce(max([Ord]), -1) + 1
			from	['+@sTbl+'] with (REPEATABLEREAD)
			where	[Src] = @SrcObjId
		end

		insert into [' + @sTbl + '] with (REPEATABLEREAD) ([Src], [Dst], [Ord])
		select	@SrcObjId, ol.[Id], ol.[Ord] + @nMinOrd
		from 	openxml(@hXMLdoc, ''/root/Obj'') with (Id int, Ord int) ol
		set @Err = @@error
		if @Err <> 0 begin
			raiserror(''ReplaceRefSeq_'+@sTbl+': SQL Error %d; Unable to insert objects into the reference sequence table'',
					16, 1, @Err)
			goto LFail
		end
	end

	if @nTrnCnt = 0 commit tran @sTranName

	-- determine if the XML document should be removed
	if @fRemoveXMLdoc = 1 and @hXMLDoc is not null exec sp_xml_removedocument @hXMLDoc

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return 0

LFail:
	rollback tran @sTranName

	-- determine if the XML document should be removed
	if @fRemoveXMLdoc = 1 and @hXMLDoc is not null exec sp_xml_removedocument @hXMLDoc

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @Err
'

	exec ( @sDynSql + @sDynSql2 + @sDynSql3 + @sDynSql4 )
	set @Err = @@error
	if @Err <> 0 begin
		raiserror('DefineReplaceRefSeqProc: SQL Error %d: Unable to create or alter the procedure ReplaceRefSeq_%s$',
				16, 1, @Err, @sTbl)
		return @err
	end

	return 0
go
if object_id('TR_CmObject$_RI_Del') is not null begin
	print 'removing trigger TR_CmObject$_RI_Del'
	drop trigger TR_CmObject$_RI_Del
end
go
print 'creating trigger TR_CmObject$_RI_Del'
go
create trigger [dbo].[TR_CmObject$_RI_Del] on [dbo].[CmObject] for delete
as
	declare @sDynSql nvarchar(4000)
	declare @iCurDelClsId int
	declare @fIsNocountOn int
	declare @uid uniqueidentifier

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	--( 	The following query is against the "deleted" table. This table
	--( is poorly documented in SQL Server documentation, but as I recall, both
	--( Oracle and FoxPro have a similar concept. The rows marked as deleted are
	--( here. The query inserts them into a scratch table ObjListTbl$. This table
	--( isn't one of the temp tables, or a table variable, but a scratch table the
	--( authors set up. It's used here because dynamic SQL isn't able to see the
	--( deleted table.
	--(	    Note also the use of newid(). This generates a new, unique ID. However,
	--( it happens only once, not for the whole table. The reason for this, I think,
	--( is that another user might be using the same scratch table concurrently. The
	--( second user would have a different ID than the first user. This makes sure
	--( each user is using their own rows in the same scratch table.

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

	-- REVIEW (SteveMiller): With the use of IDs, the SERIALIZABLE keyword shouldn't be
	-- needed here.

	-- get the first class to process
	select	@iCurDelClsId = min([Class])
	from	[ObjListTbl$] (REPEATABLEREAD)
	where	[uid] = @uid
	if @@error <> 0 begin
		raiserror('TR_CmObject$_RI_Del: Unable to get the first deleted class', 16, 1)
		goto LFail
	end

	-- loop through all of the classes in the deleted logical table
	while @iCurDelClsId is not null begin

		--(    In SQL Server, you can set a variable with a SELECT statement,
		--( as long as the query returns a single row. In this case, the code
		--( queries the Class$ table on the Class.ID, to return the name of the
		--( class.  The name of the Class is concatenated into a string.
		--(    In this system, remember: 1) Each class is mapped to a table,
		--( whether it is an abstract or concrete class.  2) Each class is
		--( subclassed from CmObject. 3) The data in an object will therefore
		--( be persisted in more than one table: CmObject, and at least one table
		--( mapped to a subclass. This is foundational to understanding this database.
		--(    The query in the dynamic SQL joins the data in the scatch table, which
		--( came from the deleted table, which originally came from CmObject--we are
		--( in the CmObject trigger. The join is on the object ID. So this is checking
		--( to see if some of some of the object's data is still in one of the subclass
		--( tables. If so, it rolls back the transaction and raises an error. In this
		--( system, you must remove the object's persisted data in the subclass(es)
		--( before removing the persisted data in CmObject.

		-- REVIEW (SteveMiller): Is it necessary to make sure the data in the subclass
		-- tables is removed before removing the persisted data in the CmObject table?
		-- The presence of this	check makes referential integrity very tight. However,
		-- it comes at the cost of performance. We may want to return to this if we ever
		-- need some speed in the object deletion process. If nothing else, the dynamic
		-- SQL can be converted into a EXEC sp_executesql command, which is more efficient.

		select	@sDynSql =
			'if exists ( ' +
				'select * ' +
				'from ObjListTbl$ del join ' + [name] + ' c ' +
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
		from	[ObjListTbl$] (REPEATABLEREAD)
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
if object_id('TR_CmObject_ValidateOwner') is not null begin
	print 'removing trigger TR_CmObject_ValidateOwner'
	drop trigger [TR_CmObject_ValidateOwner]
end
go
print 'creating trigger TR_CmObject_ValidateOwner'
go
create trigger [dbo].[TR_CmObject_ValidateOwner] on [dbo].[CmObject] for update
as
	--( We used to check to not allow an object's class to be changed,
	--( similar to the check for update([Id]) immediately below. We
	--( have since found the need to change Lex Entries. For instance,
	--( a LexSubEntry can turn into a LexMajorEntry.

	if update([Id]) begin
		raiserror('An object''s Id cannot be changed', 16, 1)
		rollback tran
	end

	-- only perform checks if one of the following columns are updated: id, owner$, ownflid$, or
	--	ownord$	because updates to UpdDttm or UpdStmp do not require the below validations
	if not ( update([Owner$]) or update([OwnFlid$]) or update([OwnOrd$]) )  return

	declare @idBad int, @own int, @flid int, @ord int, @cnt int
	declare @dupownId int, @dupseqId int
	declare @fIsNocountOn int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	if update([Owner$]) or update([OwnFlid$]) or update([OwnOrd$]) begin
		-- Get the owner's class and make sure it is a subclass of the field's type. Get the
		--	inserted object's class and make sure it is a subclass of the field's dst type.
		-- 	Make sure the OwnOrd$ field is consistent with the field type (if not sequence
		--	then it should be null). Make sure more than one object is not added as a child
		--	of an object with an atomic owning relationship. Make sure there are no duplicate
		--	Ord values within a sequence
		select top 1
			@idBad = ins.[Id],
			@own = ins.[Owner$],
			@flid = ins.[OwnFlid$],
			@ord = ins.[OwnOrd$],
			@dupownId = dupown.[Id],
			@dupseqId = dupseq.[Id]
		from	inserted ins
			-- If there is no owner, there is nothing to check so an inner join is OK here.
			join [CmObject] own on own.[Id] = ins.[Owner$]
			-- The constraints on CmObject guarantee this join.
			join [Field$] fld on fld.[Id] = ins.[OwnFlid$]
			-- If this join has no matches the owner is of the wrong type.
			left outer join [ClassPar$] ot on ot.[Src] = own.[Class$]
				and ot.[Dst] = fld.[Class]
			-- If this join has no matches the inserted object is of the wrong type.
			left outer join [ClassPar$] it on it.[Src] = ins.[Class$]
				and it.[Dst] = fld.[DstCls]
			-- if this join has matches there is more than one owned object in an atomic relationship
			left outer join [CmObject] dupown on fld.[Type] = 23 and dupown.[Owner$] = ins.[Owner$]
				and dupown.[OwnFlid$] = ins.[OwnFlid$]
				and dupown.[Id] <> ins.[Id]
			-- if this join has matches there is a duplicate sequence order in a sequence relationship
			left outer join [CmObject] dupseq on fld.[Type] = 27 and dupseq.[Owner$] = ins.[Owner$]
				and dupseq.[OwnFlid$] = ins.[OwnFlid$]
				and dupseq.[OwnOrd$] = ins.[OwnOrd$]
				and dupseq.[Id] <> ins.[Id]
		where
			ot.[Src] is null
			or it.[Src] is null
			or (fld.[Type] = 23 and ins.[OwnOrd$] is not null)
			or (fld.[Type] = 25 and ins.[OwnOrd$] is not null)
			or (fld.[Type] = 27 and ins.[OwnOrd$] is null)
			or dupown.[Id] is not null
			or dupseq.[Id] is not null

		if @@rowcount <> 0 begin
			if @dupownId is not null begin
				raiserror('More than one owned object in an atomic relationship: New ID=%d, Owner=%d, OwnFlid=%d, Already Owned Id=%d', 16, 1,
						@idBad, @own, @flid, @dupownId)
			end
			else if @dupseqId is not null begin
				raiserror('Duplicate OwnOrd in a sequence relationship: New ID=%d, Owner=%d, OwnFlid=%d, OwnOrd=%d, Duplicate Id=%d', 16, 1,
						@idBad, @own, @flid, @ord, @dupseqId)
			end
			else begin
				raiserror('Bad owner information ID=%d, Owner$=%d, OwnFlid$=%d, OwnOrd$=%d', 16, 1, @idBad, @own, @flid, @ord)
			end
			rollback tran
		end
	end

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
go
if object_id('TR_Field$_UpdateModel_Del') is not null begin
	print 'removing trigger TR_Field$_UpdateModel_Del'
	drop trigger TR_Field$_UpdateModel_Del
end
go
print 'creating trigger TR_Field$_UpdateModel_Del'
go
create trigger [dbo].[TR_Field$_UpdateModel_Del] on [dbo].[Field$] for delete
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

		--( Rebuild the delete trigger

		EXEC @Err = CreateDeleteObj @Clid
		IF @Err <> 0 GOTO LFail

		--( Rebuild CreateObject_*

		IF @nAbstract != 1 BEGIN
			EXEC @Err = DefineCreateProc$ @Clid
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

if exists (select * from sysobjects where name = 'GetOrderedMultiTxt')
	drop proc GetOrderedMultiTxt
go
print 'creating proc GetOrderedMultiTxt'
go
create proc [dbo].[GetOrderedMultiTxt]
	@id int,
	@flid int,
	@anal tinyint = 1
as

	declare @fIsNocountOn int
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	declare
		@iFieldType int,
		@nvcTable NVARCHAR(60),
		@nvcSql NVARCHAR(4000)

	select @iFieldType = [Type] from Field$ where [Id] = @flid
	EXEC GetMultiTableName @flid, @nvcTable OUTPUT

	--== Analysis WritingSystems ==--

	if @anal = 1
	begin

		-- MultiStr$ --
		if @iFieldType = 14
			select
				isnull(ms.[txt], '***') txt,
				ms.[ws],
				isnull(lpcae.[ord], 99998) [ord]
			from MultiStr$ ms
			left outer join LgWritingSystem le on le.[Id] = ms.[ws]
			left outer join LanguageProject_AnalysisWritingSystems lpae on lpae.[dst] = le.[id]
			left outer join LanguageProject_CurrentAnalysisWritingSystems lpcae on lpcae.[dst] = lpae.[dst]
			where ms.[obj] = @id and ms.[flid] = @flid
			union all
			select '***', 0, 99999
			order by isnull([ord], 99998)

		-- MultiBigStr$ --
		else if @iFieldType = 18
		begin
			--( See note 2 in the header
			declare @tblMultiBigStrAnalysis table (
				--( See the notes under string tables in FwCore.sql about the
				--( COLLATE clause.
				Txt NTEXT COLLATE Latin1_General_BIN,
				[ws] int,
				[ord] int primary key)

			insert into @tblMultiBigStrAnalysis
			select
				isnull(mbs.[txt], '***') txt,
				mbs.[ws],
				isnull(lpcae.[ord], 99998) [ord]
			from MultiBigStr$ mbs
			left outer join LgWritingSystem le on le.[Id] = mbs.[ws]
			left outer join LanguageProject_AnalysisWritingSystems lpae on lpae.[dst] = le.[id]
			left outer join LanguageProject_CurrentAnalysisWritingSystems lpcae on lpcae.[dst] = lpae.[dst]
			where mbs.[obj] = @id and mbs.[flid] = @flid
			order by isnull([ord], 99998)

			insert into @tblMultiBigStrAnalysis
			select convert(ntext, '***') [txt], 0 [ws], 99999 [ord]

			select * from @tblMultiBigStrAnalysis order by [ord]
		end

		-- MultiBigTxt$ --
		else if @iFieldType = 20
		begin
			--( See note 2 in the header
			declare @tblMultiBigTxtAnalysis table (
				--( See the notes under string tables in FwCore.sql about the
				--( COLLATE clause.
				Txt NTEXT COLLATE Latin1_General_BIN,
				[ws] int,
				[ord] int primary key)

			insert into @tblMultiBigTxtAnalysis
			select
				isnull(mbt.[txt], '***') txt,
				mbt.[ws],
				isnull(lpcae.[ord], 99998) [ord]
			from MultiBigTxt$ mbt
			left outer join LgWritingSystem le on le.[Id] = mbt.[ws]
			left outer join LanguageProject_AnalysisWritingSystems lpae on lpae.[dst] = le.[id]
			left outer join LanguageProject_CurrentAnalysisWritingSystems lpcae on lpcae.[dst] = lpae.[dst]
			where mbt.[obj] = @id and mbt.[flid] = @flid
			order by isnull([ord], 99998)

			insert into @tblMultiBigTxtAnalysis
			select convert(ntext, '***') [txt], 0 [ws], 99999 [ord]

			select * from @tblMultiBigTxtAnalysis order by [ord]
		end

		-- MultiTxt$ --
		else if @iFieldType = 16 BEGIN
			SET @nvcSql =
				N'select ' + CHAR(13) +
					N'isnull(mt.[txt], ''***'') txt, ' + CHAR(13) +
					N'mt.[ws], ' + CHAR(13) +
					N'isnull(lpcae.[ord], 99998) [ord] ' + CHAR(13) +
				N'from ' + @nvcTable + N' mt ' + CHAR(13) +
				N'left outer join LgWritingSystem le on le.[Id] = mt.[ws] ' + CHAR(13) +
				N'left outer join LanguageProject_AnalysisWritingSystems lpae ' +
					N'on lpae.[dst] = le.[id] ' + CHAR(13) +
				N'left outer join LanguageProject_CurrentAnalysisWritingSystems lpcae ' +
					N'on lpcae.[dst] = lpae.[dst] ' + CHAR(13) +
				N'where mt.[obj] = @id ' + CHAR(13) +
				N'union all ' + CHAR(13) +
				N'select ''***'', 0, 99999 ' + CHAR(13) +
				N'order by isnull([ord], 99998) '

			EXEC sp_executesql @nvcSql, N'@id INT', @id
		END

	end

	--== Vernacular WritingSystems ==--

	else if @anal = 0
	begin

		-- MultiStr$ --
		if @iFieldType = 14
			select
				isnull(ms.[txt], '***') txt,
				ms.[ws],
				isnull(lpcve.[ord], 99998) [ord]
			from MultiStr$ ms
			left outer join LgWritingSystem le on le.[Id] = ms.[ws]
			left outer join LanguageProject_VernacularWritingSystems lpve on lpve.[dst] = le.[id]
			left outer join LanguageProject_CurrentVernacularWritingSystems lpcve on lpcve.[dst] = lpve.[dst]
			where ms.[obj] = @id and ms.[flid] = @flid
			union all
			select '***', 0, 99999
			order by isnull([ord], 99998)

		-- MultiBigStr$ --
		else if @iFieldType = 18
		begin
			--( See note 2 in the header
			declare @tblMultiBigStrVernacular table (
				--( See the notes under string tables in FwCore.sql about the
				--( COLLATE clause.
				Txt NTEXT COLLATE Latin1_General_BIN,
				[ws] int,
				[ord] int primary key)

			insert into @tblMultiBigStrVernacular
			select
				isnull(mbs.[txt], '***') txt,
				mbs.[ws],
				isnull(lpcve.[ord], 99998) [ord]
			from MultiBigStr$ mbs
			left outer join LgWritingSystem le on le.[Id] = mbs.[ws]
			left outer join LanguageProject_VernacularWritingSystems lpve on lpve.[dst] = le.[id]
			left outer join LanguageProject_CurrentVernacularWritingSystems lpcve on lpcve.[dst] = lpve.[dst]
			where mbs.[obj] = @id and mbs.[flid] = @flid
			order by isnull([ord], 99998)

			insert into @tblMultiBigStrVernacular
			select convert(ntext, '***') [txt], 0 [ws], 99999 [ord]

			select * from @tblMultiBigStrVernacular order by [ord]
		end

		-- MultiBigTxt$ --
		else if @iFieldType = 20
		begin
			--( See note 2 in the header
			declare @tblMultiBigTxtVernacular table (
				--( See the notes under string tables in FwCore.sql about the
				--( COLLATE clause.
				Txt NTEXT COLLATE Latin1_General_BIN,
				[ws] int,
				[ord] int primary key)

			insert into @tblMultiBigTxtVernacular
			select
				isnull(mbt.[txt], '***') txt,
				mbt.[ws],
				isnull(lpcve.[ord], 99998) [ord]
			from MultiBigTxt$ mbt
			left outer join LgWritingSystem le on le.[Id] = mbt.[ws]
			left outer join LanguageProject_VernacularWritingSystems lpve on lpve.[dst] = le.[id]
			left outer join LanguageProject_CurrentVernacularWritingSystems lpcve on lpcve.[dst] = lpve.[dst]
			where mbt.[obj] = @id and mbt.[flid] = @flid
			order by isnull([ord], 99998)

			insert into @tblMultiBigTxtVernacular
			select convert(ntext, '***') [txt], 0 [ws], 99999 [ord]

			select * from @tblMultiBigTxtVernacular order by [ord]
		end

		-- MultiTxt$ --
		else if @iFieldType = 16 BEGIN
			SET @nvcSql =
				N' select ' + CHAR(13) +
					N'isnull(mt.[txt], ''***'') txt, ' + CHAR(13) +
					N'mt.[ws], ' + CHAR(13) +
					N'isnull(lpcve.[ord], 99998) ord ' + CHAR(13) +
				N'from ' + @nvcTable + N' mt ' + CHAR(13) +
				N'left outer join LgWritingSystem le on le.[Id] = mt.[ws] ' + CHAR(13) +
				N'left outer join LanguageProject_VernacularWritingSystems lpve ' +
					N'on lpve.[dst] = le.[id] ' + CHAR(13) +
				N'left outer join LanguageProject_CurrentVernacularWritingSystems lpcve ' +
					N'on lpcve.[dst] = lpve.[dst] ' + CHAR(13) +
				N'where mt.[obj] = @id ' + CHAR(13) +
				N'union all ' + CHAR(13) +
				N'select ''***'', 0, 99999 ' + CHAR(13) +
				N'order by isnull([ord], 99998) '

			EXEC sp_executesql @nvcSql, N'@id INT', @id
		END
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

if object_id('GetOrderedMultiTxtXml$') is not null begin
	print 'removing procedure GetOrderedMultiTxtXml$'
	drop proc [GetOrderedMultiTxtXml$]
end
go
print 'creating proc GetOrderedMultiTxtXml$'
go
create proc [dbo].[GetOrderedMultiTxtXml$]
	@hXMLDocObjList int = null,
	@iFlid int,
	@tiAnal tinyint = 1
as

	declare @fIsNocountOn int
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	DECLARE
		@nvcTable NVARCHAR(60),
		@nvcSql NVARCHAR(4000)

	EXEC GetMultiTableName @iflid, @nvcTable OUTPUT

	if @tiAnal = 1 BEGIN

		SET @nvcSql = N'select ' + CHAR(13) +
			N'ids.[Id], ' + CHAR(13) +
			N'isnull((select top 1 isnull(mt.[txt], ''***'') ' + CHAR(13) +
				N'from ' + @nvcTable + N' mt ' + CHAR(13) +
				N'left outer join LgWritingSystem le on le.[Id] = mt.[ws] ' + CHAR(13) +
				N'left outer join LanguageProject_AnalysisWritingSystems lpae ' +
					N'on lpae.[dst] = le.[id] ' + CHAR(13) +
				N'left outer join LanguageProject_CurrentAnalysisWritingSystems lpcae ' +
					N'on lpcae.[dst] = lpae.[dst] ' + CHAR(13) +
				N'where mt.[obj] = ids.[Id] ' + CHAR(13) +
				N'order by isnull(lpcae.[ord], 99999)), ''***'') as [txt] , ' + CHAR(13) +
			N'isnull((select top 1 isnull(mt.[ws], 0) ' + CHAR(13) +
				N'from ' + @nvcTable + N' mt ' + CHAR(13) +
				N'left outer join LgWritingSystem le on le.[Id] = mt.[ws] ' + CHAR(13) +
				N'left outer join LanguageProject_AnalysisWritingSystems lpae ' +
					N' on lpae.[dst] = le.[id] ' + CHAR(13) +
				N'left outer join LanguageProject_CurrentAnalysisWritingSystems lpcae ' +
					N'on lpcae.[dst] = lpae.[dst] ' + CHAR(13) +
				N'where mt.[obj] = ids.[Id] ' + CHAR(13) +
				N'order by isnull(lpcae.[ord], 99999)), 0) as [ws] ' + CHAR(13) +
			N'from openxml (@hXMLDocObjList, ''/root/Obj'') with ([Id] int) ids '

		EXEC sp_executesql @nvcSql, N'@hXMLDocObjList INT', @hXMLDocObjList
	END
	else if @tiAnal = 0 BEGIN

		SET @nvcSql = N'select ' + CHAR(13) +
			N'ids.[Id], ' + CHAR(13) +
			N'isnull((select top 1 isnull(mt.[txt], ''***'') as [txt] ' + CHAR(13) +
				N'from ' + @nvcTable + N' mt ' + CHAR(13) +
				N'left outer join LgWritingSystem le on le.[Id] = mt.[ws] ' + CHAR(13) +
				N'left outer join LanguageProject_VernacularWritingSystems lpve ' +
					N'on lpve.[dst] = le.[id] ' + CHAR(13) +
				N'left outer join LanguageProject_CurrentVernacularWritingSystems lpcve ' +
					N'on lpcve.[dst] = lpve.[dst] ' + CHAR(13) +
				N'where mt.[obj] = ids.[Id] ' + CHAR(13) +
				N'order by isnull(lpcve.[ord], 99999)), ''***'') , ' + CHAR(13) +
			N'isnull((select top 1 isnull(mt.[ws], 0) as [ws] ' + CHAR(13) +
				N'from ' + @nvcTable + N' mt ' + CHAR(13) +
				N'left outer join LgWritingSystem le on le.[Id] = mt.[ws] ' + CHAR(13) +
				N'left outer join LanguageProject_VernacularWritingSystems lpve ' +
					N'on lpve.[dst] = le.[id] ' + CHAR(13) +
				N'left outer join LanguageProject_CurrentVernacularWritingSystems lpcve ' +
					N'on lpcve.[dst] = lpve.[dst] ' + CHAR(13) +
				N'where mt.[obj] = ids.[Id] ' + CHAR(13) +
				N'order by isnull(lpcve.[ord], 99999)), 0) ' + CHAR(13) +
			N'from openxml (@hXMLDocObjList, ''/root/Obj'') with ([Id] int) ids '

		EXEC sp_executesql @nvcSql, N'@hXMLDocObjList INT', @hXMLDocObjList
	END
	else begin
		raiserror('@tiAnal flag not set correctly', 16, 1)
		goto LFail
	end

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
go

if object_id('dbo.GetStTexts$') is not null
begin
	print 'Removing procedure: GetStTexts$'
	drop proc GetStTexts$
end
go
print 'Creating procedure: GetStTexts$'
go
create proc [dbo].[GetStTexts$]
	@uid uniqueidentifier
as
	declare @StTextClassId int

	-- get the StText (structured text) class type
	select	@StTextClassId = id
	from	Class$
	where	name = 'StText'

	-- select the structured text information based on the IDs of class type StText
	select	stp.Src,
		stp.Dst,
		sp.StyleRules,
		sttp.Contents
	from	StText_Paragraphs stp
			join StPara sp on sp.Id = stp.Dst
			join ObjInfoTbl$ oi on stp.Src = oi.ObjId
			left outer join StTxtPara sttp on sttp.Id = sp.Id
	where	oi.uid = @uid
		and oi.ObjClass = @StTextClassId
go

if object_id('NoteInterlinProcessTime') is not null begin
	print 'removing proc NoteInterlinProcessTime'
	drop proc [NoteInterlinProcessTime]
end
go
print 'creating proc NoteInterlinProcessTime'
go
create proc [dbo].[NoteInterlinProcessTime]
	@atid INT, @stid INT,
	@nvNew nvarchar(4000) output
AS BEGIN

	declare @lpid int
	select top 1 @lpid = id from LanguageProject

	set @nvNew = ''

	declare MakeAnnCursor cursor local static forward_only read_only for
	select tp.id from StTxtPara_ tp
	left outer join CmBaseAnnotation_ cb
					on cb.BeginObject = tp.id and cb.AnnotationType = @atid
	where tp.owner$ = @stid and cb.id is null

	declare @tpid int,
		@NewObjGuid uniqueidentifier,
		@cbaId int
	open MakeAnnCursor
		fetch MakeAnnCursor into @tpid
		while @@fetch_status = 0 begin
			exec CreateOwnedObject$ 37, @cbaId out, @NewObjGuid out, @lpid, 6001044, 25
			set @nvNew = @nvNew + ',' + cast(@cbaId as nvarchar(8))
			update CmBaseAnnotation set BeginObject = @tpid where id = @cbaId
			update CmAnnotation set AnnotationType = @atid where id = @cbaId
			set @cbaId = null
			set @NewObjGuid = null
			fetch MakeAnnCursor into @tpid
		end
	close MakeAnnCursor
	deallocate MakeAnnCursor

	update CmBaseAnnotation_
	set CompDetails = cast(cast(tp.UpdStmp as bigint) as NVARCHAR(20))
	from CmBaseAnnotation_ cba
	join StTxtPara_ tp on cba.BeginObject = tp.id and tp.owner$ = @stid
	where cba.AnnotationType = @atid

	return @@ERROR
END
GO

if exists (select *
			 from sysobjects
			where name = 'GetNewFootnoteGuids')
	drop proc GetNewFootnoteGuids
go
print 'creating proc GetNewFootnoteGuids'
go
create proc [dbo].[GetNewFootnoteGuids]
	@bookId	int,
	@revId	int
as
	select	bookfn.Guid$ "ScrBookFootnoteGuid",
		revfn.Guid$ "RevisionFootnoteGuid",
		revfn.ownord$
	from	StFootnote_ bookfn
	join	StFootnote_ revfn on bookfn.ownord$ = revfn.ownord$
	where	bookfn.owner$ = @bookId
	and	revfn.owner$ = @revId
	order by revfn.ownord$
GO

if exists (select *
			 from sysobjects
			where name = 'GetParasWithORCs')
drop proc GetParasWithORCs
go
print 'creating proc GetParasWithORCs'
go
create proc [dbo].[GetParasWithORCs] @revId int
as
begin
	select	p.[Id] "id", p.OwnOrd$ "pord", t.OwnFlid$ "tflid", s.OwnOrd$ "sord", 1 "t_or_s"
	from	StTxtPara_ p
	join	StText_ t on p.Owner$ = t.[Id]
	join	ScrSection_ s on t.Owner$ = s.[Id]
	join	ScrBook b on s.Owner$ = b.[Id]
	and	b.[id] = @revId
	where	p.Contents COLLATE Latin1_General_BIN like N'%' + NCHAR(0xFFFC) + '%' COLLATE Latin1_General_BIN
	union all
	select	p.[Id], p.OwnOrd$, 0, 0, 0
	from	StTxtPara_ p
	join	StText_ t on p.Owner$ = t.[Id]
	join	ScrBook b on t.Owner$ = b.[Id]
	and	t.OwnFlid$ = 3002004
	and	b.[id] = @revId
	where	p.Contents COLLATE Latin1_General_BIN like N'%' + NCHAR(0xFFFC) + '%' COLLATE Latin1_General_BIN

	order by t_or_s, sord, tflid, pord--select PATINDEX('85BD0CE977CE49629850205F8B73C741', CAST(CAST(Contents_fmt AS varbinary(8000)) AS nvarchar(4000)))
end
GO

if object_id('fnGetAddedNotebookObjects$') is not null begin
	print 'removing function fnGetAddedNotebookObjects$'
	drop function [fnGetAddedNotebookObjects$]
end
go
print 'creating function fnGetAddedNotebookObjects$'
go
create function [dbo].[fnGetAddedNotebookObjects$] ()
returns @DelList table ([ObjId] int not null)
as
begin
	declare @nRowCnt int
	declare @nOwnerDepth int
	declare @Err int
	set @nOwnerDepth = 1
	set @Err = 0

	insert into @DelList
	select [Id] from CmObject
	where OwnFlid$ = 4001001

	-- use a table variable to hold the possibility list item object ids
	declare @PossItems table (
		[ObjId] int primary key not null,
		[OwnerDepth] int null,
		[DateCreated] datetime null
	)

	-- Get the object ids for all of the possibility items in the lists used by Data Notebook
	-- (except for the Anthropology List, which is loaded separately).
	-- Note the hard-wired sets of possibility list flids.
	-- First, get the top-level possibility items from the standard data notebook lists.

	insert into @PossItems (ObjId, OwnerDepth, DateCreated)
	select co.[Id], @nOwnerDepth, cp.DateCreated
	from [CmObject] co
	join [CmPossibility] cp on cp.[id] = co.[id]
	join CmObject co2 on co2.[id] = co.Owner$ and co2.OwnFlid$ in (
			4001003,
			6001025,
			6001026,
			6001027,
			6001028,
			6001029,
			6001030,
			6001031,
			6001032,
			6001033,
			6001036
			)

	if @@error <> 0 goto LFail
	set @nRowCnt=@@rowcount

	-- Repeatedly get the list items owned at the next depth.

	while @nRowCnt > 0 begin
		set @nOwnerDepth = @nOwnerDepth + 1

		insert into @PossItems (ObjId, OwnerDepth, DateCreated)
		select co.[id], @nOwnerDepth, cp.DateCreated
		from [CmObject] co
		join [CmPossibility] cp on cp.[id] = co.[id]
		join @PossItems pi on pi.[ObjId] = co.[Owner$] and pi.[OwnerDepth] = @nOwnerDepth - 1

		if @@error <> 0 goto LFail
		set @nRowCnt=@@rowcount
	end

	-- Extract all the items which are newer than the language project, ie, which cannot be
	-- factory list items.
	-- Omit list items which are owned by other non-factory list items, since they will be
	-- deleted by deleting their owner.

	insert into @DelList
	select pi.ObjId
	from @PossItems pi
	join CmObject co on co.[id] = pi.ObjId
	where pi.DateCreated > (select top 1 DateCreated from CmProject order by DateCreated DESC)

	delete from @PossItems

	-- Get the object ids for all of the possibility items in the anthropology list.
	-- First, get the top-level possibility items from the anthropology list.

	set @nOwnerDepth = 1

	insert into @PossItems (ObjId, OwnerDepth, DateCreated)
	select co.[Id], @nOwnerDepth, cp.DateCreated
	from [CmObject] co
	join [CmPossibility] cp on cp.[id] = co.[id]
	where co.[Owner$] in (select id from CmObject where OwnFlid$ = 6001012)

	set @nRowCnt=@@rowcount
	if @@error <> 0 goto LFail

	-- Repeatedly get the anthropology list items owned at the next depth.

	while @nRowCnt > 0 begin
		set @nOwnerDepth = @nOwnerDepth + 1

		insert into @PossItems (ObjId, OwnerDepth, DateCreated)
		select co.[id], @nOwnerDepth, cp.DateCreated
		from [CmObject] co
		join [CmPossibility] cp on cp.[id] = co.[id]
		join @PossItems pi on pi.[ObjId] = co.[Owner$] and pi.[OwnerDepth] = @nOwnerDepth - 1

		if @@error <> 0 goto LFail
		set @nRowCnt=@@rowcount
	end

	declare @cAnthro int
	declare @cTimes int
	select @cAnthro = COUNT(*) from @PossItems
	select @cTimes = COUNT(distinct DateCreated) from @PossItems

	if @cTimes = @cAnthro begin
		-- Assume that none of them are factory if they all have different creation
		-- times.  This is true even if there's only one item.
		insert into @DelList
		select pi.ObjId
		from @PossItems pi
		where pi.OwnerDepth = 1
	end
	else if @cTimes != 1 begin
		-- assume that the oldest items are factory, the rest aren't
		insert into @DelList
		select pi.ObjId
		from @PossItems pi
		where pi.DateCreated > (select top 1 DateCreated from @PossItems order by DateCreated)
	end

return

LFail:
	delete from @DelList
	return
end
go

if object_id('CountUpToDateParas') is not null begin
	print 'removing proc CountUpToDateParas'
	drop proc [CountUpToDateParas]
end
go
print 'creating proc CountUpToDateParas'
go
create proc [dbo].[CountUpToDateParas]
	@atid int, @stid int
as
select count(tp.id) from StTxtPara_ tp
	join CmBaseAnnotation_ cb on cb.BeginObject = tp.id and cb.AnnotationType = @atid
		and cast(cast(tp.UpdStmp as bigint) as NVARCHAR(20)) = cast(cb.CompDetails as NVARCHAR(20))
	where tp.owner$ = @stid
	group by tp.owner$
go

if object_id('CreateParserProblemAnnotation') is not null begin
	print 'removing proc CreateParserProblemAnnotation'
	drop proc CreateParserProblemAnnotation
end
print 'creating proc CreateParserProblemAnnotation'
go
create proc [dbo].[CreateParserProblemAnnotation]
	@CompDetails ntext,
	@BeginObject_WordformID int,
	@Source_AgentID int,
	@AnnotationType_AnnDefID int
AS
	DECLARE
		@retVal INT,
		@fIsNocountOn INT,
		@lpid INT,
		@nTrnCnt INT,
		@sTranName VARCHAR(50),
		@uid uniqueidentifier,
		@annID INT

	-- determine if NO COUNT is currently set to ON
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- @lpid will be the annotation's owner.
	SELECT TOP 1 @lpID=ID
	FROM LanguageProject
	ORDER BY ID

	-- Determine if a transaction already exists.
	-- If one does then create a savepoint, otherwise create a transaction.
	set @nTrnCnt = @@trancount
	set @sTranName = 'CreateParserProblemAnnotation_tr' + convert(varchar(8), @@nestlevel)
	if @nTrnCnt = 0 begin tran @sTranName
	else save tran @sTranName

	-- Create a new CmBaseAnnotation, and add it to the LanguageProject
	set @uid = null
	exec @retVal = CreateOwnedObject$
		37, -- 37
		@annID output,
		null,
		@lpid,
		6001044, -- kflidLanguageProject_Annotations
		25, --25
		null,
		0,
		1,
		@uid output

	if @retVal <> 0
	begin
		-- There was an error in CreateOwnedObject
		set @retVal = 1
		GOTO FinishRollback
	end

	-- Update values.
	UPDATE CmAnnotation
	SET CompDetails=@CompDetails,
		Source=@Source_AgentID,
		AnnotationType=@AnnotationType_AnnDefID
	WHERE ID = @annID
	if @@error <> 0
	begin
		-- Couldn't update CmAnnotation data.
		set @retVal = 2
		goto FinishRollback
	end
	UPDATE CmBaseAnnotation
	SET BeginObject=@BeginObject_WordformID
	WHERE ID = @annID
	if @@error <> 0
	begin
		-- Couldn't update CmBaseAnnotation data.
		set @retVal = 3
		goto FinishRollback
	end

	if @nTrnCnt = 0 commit tran @sTranName
	SET @retVal = 0
	GOTO FinishFinal

FinishRollback:
	if @nTrnCnt = 0 rollback tran @sTranName
	GOTO FinishFinal

FinishFinal:
	if @fIsNocountOn = 0 set nocount off
	return @retval
go

if object_id('DisplayName_LexEntry') is not null begin
	print 'removing proc DisplayName_LexEntry'
	drop proc DisplayName_LexEntry
end
print 'creating proc DisplayName_LexEntry'
go
create  proc [dbo].[DisplayName_LexEntry]
	@XMLIds ntext = null
as

declare @retval int, @fIsNocountOn int,
	@LeId int, @Class int, @HNum int, @FullTxt nvarchar(4000),
	@FormId int, @Ord int, @Flid int, @FormTxt nvarchar(4000), @FormFmt int, @FormEnc int,
	@SenseId int, @SenseGloss nvarchar(4000), @SenseFmt int, @SenseEnc int,
	@myCursor CURSOR

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- Gather two encodings,
	select top 1 @SenseEnc=le.Id
	from LanguageProject_CurrentAnalysisWritingSystems ce
	join LgWritingSystem le On le.Id = ce.Dst
	order by ce.Src, ce.ord
	select top 1 @FormEnc=le.Id
	from LanguageProject_CurrentVernacularWritingSystems ce
	join LgWritingSystem le On le.Id = ce.Dst
	order by ce.Src, ce.ord

	--Table variable.
	declare @DisplayNameLexEntry table (
		LeId int primary key,
		Class int,
		HNum int default 0,
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		FullTxt NVARCHAR(4000) COLLATE Latin1_General_BIN,
		FormId int default 0,
		Ord int default 0,
		Flid int default 0,
		FormTxt nvarchar(4000),
		FormFmt int,
		FormEnc int,
		SenseId int default 0,
		SenseGloss nvarchar(4000),
		SenseFmt int,
		SenseEnc int
		)

	if @XMLIds is null begin
		-- Do all lex entries.
		set @myCursor = CURSOR FAST_FORWARD for
			select Id, Class$
			from CmObject
			where Class$=5002
			order by id
		open @myCursor
	end
	else begin
		-- Do lex entries provided in xml string.
		declare @hdoc int
		exec sp_xml_preparedocument @hdoc output, @XMLIds
		if @@error <> 0 begin
			set @retval = 1
			goto LExitNoCursor
		end
		set @myCursor = CURSOR FAST_FORWARD for
			select cmo.Id, cmo.Class$
			from	openxml(@hdoc, '/root/Obj')
			with ([Id] int) as ol
			-- Remove all pretenders, since they won't 'join'.
			join CmObject cmo
				On ol.Id=cmo.Id
				and cmo.Class$=5002
			order by ol.[Id]
		open @myCursor
		-- Turn loose of the handle
		exec sp_xml_removedocument @hdoc
		if @@error <> 0 begin
			set @retval = 1
			goto LExitWithCursor
		end
	end

	-- REVIEW (SteveMiller): MultiTxt$.fmt was always NULL. Don't understand the
	-- reason for @FormFmt being set to Fmt. Changed to cast(null as varbinary).
	-- Loop through all ids.
	fetch next from @myCursor into @LeId, @Class
	while @@fetch_status = 0
	begin
		-- ( We will try to get @FormTxt from objects in this order:
		-- ( 1. Citation form
		-- ( 2. Lexeme form
		-- ( 3. Last alternate form
		-- ( 4. 'no form available'
		-- Citation form
		select top 1 @FormId=0, @Ord=0, @Flid = 5002003, @FormTxt=Txt,
					@FormFmt=cast(null as varbinary)
		from LexEntry_CitationForm
		where Obj=@LeId and Ws=@FormEnc
		if @@rowcount = 0 begin
			-- Lexeme form
			select top 1 @FormId=f.Obj, @Ord=0, @Flid = 5035001, @FormTxt=f.Txt,
							@FormFmt=cast(null as varbinary)
			from LexEntry_LexemeForm lf
			join MoForm_Form f On f.Obj=lf.Dst and f.Ws=@FormEnc
			where lf.Src=@LeId
			if @@rowcount = 0 begin
				-- First alternate form
				select top 1 @FormId=f.Obj, @Ord=a.Ord, @Flid=5035001, @FormTxt=f.Txt,
						@FormFmt=cast(null as varbinary)
				from LexEntry_AlternateForms a
				join MoForm_Form f On f.Obj=a.Dst and f.Ws=@FormEnc
				where a.Src=@LeId
				ORDER BY a.Ord
				if @@rowcount = 0 begin
					-- ( Give up.
					set @FormId = 0
					set @Ord = 0
					set @Flid = 0
					set @FormTxt = 'no form available'
				end
			end
		end
		set @FullTxt = @FormTxt

		-- Deal with homograph number.
		select @HNum=HomographNumber
		from LexEntry
		where Id=@LeId
		if @HNum > 0
			set @FullTxt = @FullTxt + '-' + cast(@HNum as nvarchar(100))

		-- Deal with conceptual model class.

		-- Deal with sense gloss.
		select top 1 @SenseId=ls.Id, @SenseGloss = isnull(g.Txt, '***'), @SenseFmt= cast(null as varbinary)
		from LexEntry_Senses mes
		left outer join LexSense ls
			On ls.Id=mes.Dst
		left outer join LexSense_Gloss g
			On g.Obj=ls.Id and g.Ws=@SenseEnc
		where mes.Src=@LeId
		order by mes.Ord
		set @FullTxt = @FullTxt + ' : ' + @SenseGloss

		insert into @DisplayNameLexEntry (LeId, Class, HNum, FullTxt,
					FormId, Ord, Flid, FormTxt, FormFmt, FormEnc,
					SenseId, SenseGloss, SenseFmt, SenseEnc)
			values (@LeId, @Class, @HNum, @FullTxt,
					@FormId, @Ord, @Flid, @FormTxt, @FormFmt, @FormEnc,
					@SenseId, @SenseGloss, @SenseFmt, @SenseEnc)
		-- Try for another one.
		fetch next from @myCursor into @LeId, @Class
	end

	set @retval = 0
	select * from @DisplayNameLexEntry order by FullTxt

LExitWithCursor:
	close @myCursor
	deallocate @myCursor

LExitNoCursor:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @retval
go

if object_id('DisplayName_MoForm') is not null begin
	print 'removing proc DisplayName_MoForm'
	drop proc DisplayName_MoForm
end
print 'creating proc DisplayName_MoForm'
go
create proc [dbo].[DisplayName_MoForm]
	@hvo int = null
as

declare @retval int, @fIsNocountOn int,
	@DisplayName nvarchar(4000), @pfxMarker nvarchar(2), @sfxMarker nvarchar(2),
	@AlloId int, @AlloClass int, @AlloOwner int, @AlloFlid int,
	@AlloTxt nvarchar(4000), @AlloFmt int, @AlloWs int,
	@SenseId int, @SenseTxt nvarchar(4000), @SenseFmt int, @SenseWs int,
	@CfTxt nvarchar(4000), @CfFmt int, @CfWs int,
	@myCursor CURSOR

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- table variable to hold return information.
	declare @DisplayNameMoForm table (
		DisplayName nvarchar(4000), --1
		AlloId int,	-- 2
		AlloClass int,	-- 3
		AlloOwner int,	-- 4
		AlloFlid int,	-- 5
		AlloTxt nvarchar(4000),	-- 6
		AlloFmt int,	-- 7
		AlloWs int,	-- 8
		SenseId int,	--
		SenseTxt nvarchar(4000),	-- 10
		SenseFmt int,	-- 11
		SenseWs int,	-- 12
		CfTxt nvarchar(4000),	-- 13
		CfFmt int,	-- 14
		CfWs int)	-- 15

	--Note: This can't be a table variable, because we do:
	-- insert #DNLE exec DisplayName_LexEntry null
	--And that can't be done using table variables.
	create table #DNLE (
		LeId int primary key,
		Class int,
		HNum int default 0,
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		FullTxt NVARCHAR(4000) COLLATE Latin1_General_BIN,
		FormId int default 0,
		Ord int default 0,
		Flid int default 0,
		FormTxt nvarchar(4000),
		FormFmt int,
		FormEnc int,
		SenseId int default 0,
		SenseGloss nvarchar(4000),
		SenseFmt int,
		SenseEnc int
		)
	-- ( Class ids are:
	-- (   5027	MoAffixAllomorph
	-- (   5045	MoStemAllomorph
	-- ( --5029	MoAffixProcess is not used at this point.
	-- ( Owner Field ids are:
	-- (   5002029	LexEntry_LexemeForm
	-- (   5002030	LexEntry_AlternateForms
	if @hvo is null begin
		insert #DNLE exec DisplayName_LexEntry null
		-- Do all MoForms that are owned in the LexemeForm and AlternateForms
		-- properties of LexEntry.
		set @myCursor = CURSOR FAST_FORWARD for
			select cmo.Id, cmo.Class$, cmo.OwnFlid$
			from CmObject cmo
			where cmo.Class$ IN (5027, 5045) and cmo.OwnFlid$ IN (5002029, 5002030)
			order by cmo.Id
		open @myCursor
	end
	else begin
		-- Do only the MoForm provided by @hvo.
		set @myCursor = CURSOR FAST_FORWARD for
			select cmo.Id, cmo.Class$, cmo.OwnFlid$
			from CmObject cmo
			where cmo.Id = @hvo
				and cmo.Class$ IN (5027, 5045) and OwnFlid$ IN (5002029, 5002030)
		open @myCursor
	end

	-- Loop through all ids.
	fetch next from @myCursor into @AlloId, @AlloClass, @AlloFlid
	while @@fetch_status = 0
	begin
		-- Get display name for LexEntry.
		declare @XMLLEId nvarchar(4000), @cnt int

		select @AlloOwner=Owner$
		from CmObject
		where Id=@AlloId

		if @hvo is not null begin
			set @XMLLEId = '<root><Obj Id="' + cast(@AlloOwner as nvarchar(100)) + '"/></root>'
			insert #DNLE exec DisplayName_LexEntry @XMLLEId
		end

		select @SenseId=SenseId, @SenseTxt=isnull(SenseGloss, '***'), @SenseFmt=SenseFmt,
				@SenseWs=SenseEnc, @AlloWs=FormEnc,
				@CfTxt=FormTxt, @CfFmt=FormFmt, @CfWs=FormEnc
		from #DNLE
		where LeId=@AlloOwner

		-- REVIEW (SteveMiller): MultiTxt$.fmt was always NULL. Don't understand the
		-- reason for @AlloFmt being set to Fmt. Changed to cast(null as varbinary).
		select @AlloTxt=isnull(Txt, '***'), @AlloFmt = cast(null as varbinary)
		from MoForm_Form
		where Ws=@AlloWs and Obj=@AlloId

		select @pfxMarker=isnull(mmt.Prefix, ''), @sfxMarker=isnull(mmt.Postfix, '')
		from MoForm f
		left outer join MoMorphType mmt On f.MorphType=mmt.Id
		where f.Id=@AlloId

		set @DisplayName =
				@pfxMarker + @AlloTxt + @sfxMarker + ' (' + @SenseTxt + '): ' + @CfTxt

		if @hvo is not null
			truncate table #DNLE

		--Put everything in temporary table
		insert @DisplayNameMoForm (DisplayName,
			AlloId, AlloClass, AlloOwner, AlloFlid, AlloTxt, AlloFmt, AlloWs,
			SenseId, SenseTxt, SenseFmt, SenseWs,
			CfTxt, CfFmt, CfWs)
		values (@DisplayName,
			@AlloId, @AlloClass, @AlloOwner, @AlloFlid, @AlloTxt, @AlloFmt, @AlloWs,
			@SenseId, @SenseTxt, @SenseFmt, @SenseWs,
			@CfTxt, @CfFmt, @CfWs)
		-- Try for another MoForm.
		fetch next from @myCursor into @AlloId, @AlloClass, @AlloFlid
	end

	set @retval = 0
	select * from @DisplayNameMoForm order by AlloTxt

LExitWithCursor:
	close @myCursor
	deallocate @myCursor

LExitNoCursor:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	drop table #DNLE

	return @retval
go

if object_id('DisplayName_Msa') is not null begin
	drop proc DisplayName_MSA
end
go
print 'creating proc DisplayName_Msa'
go
create proc [dbo].[DisplayName_Msa]
	@XMLIds ntext = null, @ShowForm bit = 1
as

declare @retval int, @fIsNocountOn int,
	@MsaId int, @MsaClass int, @MsaForm nvarchar(4000),
	@FormId int, @FormClass int, @FormOwner int, @FormFlid int,
		@FormTxt nvarchar(4000), @FormFmt int, @FormEnc int,
	@SenseId int, @SenseTxt nvarchar(4000), @SenseFmt int, @SenseEnc int,
	@POSaID int, @POSaTxt nvarchar(4000), @POSaFmt int, @POSaEnc int,
	@POSbID int, @POSbTxt nvarchar(4000), @POSbFmt int, @POSbEnc int,
	@SlotTxt nvarchar(4000), @SlotsTxt nvarchar(4000), @rowCnt int,
	@myCursor CURSOR

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- table variable to hold return information.
	declare @DisplayNameMsa table (
		MsaId int,	-- 1
		MsaClass int,	-- 2
		MsaForm nvarchar(4000),	-- 3
		FormId int,	-- 4
		FormClass int,	-- 5
		FormOwner int,	-- 6
		FormFlid int,	-- 7
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		FormTxt NVARCHAR(4000) COLLATE Latin1_General_BIN, -- 8
		FormFmt int,	-- 9
		FormEnc int,	-- 10
		SenseId int,	-- 11
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		SenseTxt NVARCHAR(4000) COLLATE Latin1_General_BIN, -- 12
		SenseFmt int,	-- 13
		SenseEnc int,	-- 14
		POSaID int,	-- 15
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		POSaTxt NVARCHAR(4000) COLLATE Latin1_General_BIN, --16
		POSaFmt int,	-- 17
		POSaEnc int,	-- 18
		POSbID int,	-- 19
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		POSbTxt NVARCHAR(4000) COLLATE Latin1_General_BIN, --20
		POSbFmt int,	-- 21
		POSbEnc int	-- 22
		)

	--( Need to deal with: @FormClass.

	--Note: This can't be a table variable, because we do:
	-- insert #DNLE exec DisplayName_LexEntry null
	--And that can't be done using table variables.
	create table #DNLE (
		LeId int primary key,
		Class int,
		HNum int default 0,
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		FullTxt NVARCHAR(4000) COLLATE Latin1_General_BIN,
		FormId int default 0,
		Ord int default 0,
		Flid int default 0,
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		FormTxt NVARCHAR(4000) COLLATE Latin1_General_BIN,
		FormFmt int,
		FormEnc int,
		SenseId int default 0,
		SenseGloss nvarchar(4000),
		SenseFmt int,
		SenseEnc int
		)

	--( class ids are:
	--( 5001	MoStemMsa
	--( 5031	MoDerivationalAffixMsa
	--( 5032	MoDerivationalStepMsa
	--( 5038	MoInflectionalAffixMsa
	--( 5117	MoUnclassifiedAffixMsa

	if @XMLIds is null begin
		insert #DNLE exec DisplayName_LexEntry null
		-- Do all MSAes.
		set @myCursor = CURSOR FAST_FORWARD for
			select Id, Class$
			from CmObject
			where Class$ IN (5001, 5031, 5032, 5038, 5117)
			order by Id
		open @myCursor
	end
	else begin
		-- Do MSAes provided in xml string.
		declare @hdoc int
		exec sp_xml_preparedocument @hdoc output, @XMLIds
		if @@error <> 0 begin
			set @retval = 1
			goto LExitNoCursor
		end
		set @myCursor = CURSOR FAST_FORWARD for
			select cmo.Id, cmo.Class$
			from	openxml(@hdoc, '/root/Obj')
			with ([Id] int) as ol
			-- Remove all pretenders, since they won't 'join'.
			join CmObject cmo
				On ol.Id=cmo.Id
				and cmo.Class$ IN (5001, 5031, 5032, 5038, 5117)
			order by ol.[Id]
		open @myCursor
		-- Turn loose of the handle
		exec sp_xml_removedocument @hdoc
		if @@error <> 0 begin
			set @retval = 1
			goto LExitWithCursor
		end
	end

	-- Loop through all ids.
	fetch next from @myCursor into @MsaId, @MsaClass
	while @@fetch_status = 0
	begin
		-- Get display name for LexEntry.
		declare @LeId int, @XMLLEId nvarchar(4000), @cnt int

		select @LeId=Owner$
		from CmObject
		where Id=@MsaId

		set @XMLLEId = '<root><Obj Id="' + cast(@LeId as nvarchar(100)) + '"/></root>'

		if @XMLIds is not null
			insert #DNLE exec DisplayName_LexEntry @XMLLEId
		select @MsaForm=FullTxt,
			@FormId=FormId, @FormFlid=Flid, @FormTxt=FormTxt, @FormFmt=FormFmt, @FormEnc=FormEnc,
			@SenseId=SenseId, @SenseTxt=SenseGloss, @SenseFmt=SenseFmt, @SenseEnc=SenseEnc
		from #DNLE
		where LeId=@LeId
		if @ShowForm = 0
			set @MsaForm = ''
		else
			set @MsaForm = @MsaForm + ' '
		if @FormId = 0
			set @FormOwner = @LeId
		else
			set @FormOwner = @FormId
		if @XMLIds is not null
			truncate table #DNLE

		-- REVIEW (SteveMiller): MultiTxt$.fmt was always NULL. Don't understand the
		-- reason for @POSaFmt being set to Fmt. Changed to cast(null as varbinary).

		if @MsaClass=5001 begin		--MoStemMsa
			select top 1 @POSaID=pos.Id, @POSaTxt=isnull(nm.Txt, '***'),
					@POSaFmt=cast(null as varbinary), @POSaEnc=nm.Ws
			from MoStemMsa msa
			left outer join PartOfSpeech pos On pos.Id=msa.PartOfSpeech
			left outer join CmPossibility_Name nm On nm.Obj=pos.Id and nm.Ws=@SenseEnc
			where msa.Id=@MsaId
			set @MsaForm = @MsaForm + 'stem/root: ' + @POSaTxt
		end
		else if @MsaClass=5038 begin --MoInflectionalAffixMsa
			select top 1 @POSaID=pos.Id, @POSaTxt=isnull(nm.Txt, '***'),
					@POSaFmt=cast(null as varbinary), @POSaEnc=nm.Ws
			from MoInflectionalAffixMsa msa
			left outer join PartOfSpeech pos On pos.Id=msa.PartOfSpeech
			left outer join CmPossibility_Name nm On nm.Obj=pos.Id and nm.Ws=@SenseEnc
			where msa.Id=@MsaId

			SET @SlotsTxt=''

			select top 1 @SlotTxt=slot_nm.Txt
			from MoInflectionalAffixMsa_Slots msa_as
			join MoInflAffixSlot slot On slot.Id=msa_as.Dst
			join MoInflAffixSlot_Name slot_nm On slot_nm.Obj=slot.Id and slot_nm.Ws=@SenseEnc
			where @MsaId=msa_as.Src
			ORDER BY slot_nm.Txt
			SET @cnt = @@rowcount

			while (@cnt > 0)
			BEGIN
				IF @SlotsTxt=''
					SET @SlotsTxt=@SlotTxt
				ELSE
					SET @SlotsTxt=@SlotsTxt + '/' + @SlotTxt

				select top 1 @SlotTxt=slot_nm.Txt
				from MoInflectionalAffixMsa_Slots msa_as
				join MoInflAffixSlot slot On slot.Id=msa_as.Dst
				join MoInflAffixSlot_Name slot_nm On slot_nm.Obj=slot.Id and slot_nm.Ws=@SenseEnc
				where @MsaId=msa_as.Src AND slot_nm.Txt > @SlotTxt
				ORDER BY slot_nm.Txt
				SET @cnt = @@rowcount
			END

			if @SlotsTxt='' SET @SlotsTxt=null
			set @MsaForm = @MsaForm + 'inflectional: ' + @POSaTxt + ':(' + isnull(@SlotsTxt, '***') + ')'
		end
		else if @MsaClass=5031 begin	--MoDerivationalAffixMsa
			-- FromPartOfSpeech
			select top 1 @POSaID=pos.Id, @POSaTxt=isnull(nm.Txt, '***'),
					@POSaFmt=cast(null as varbinary), @POSaEnc=nm.Ws
			from MoDerivationalAffixMsa msa
			left outer join PartOfSpeech pos On pos.Id=msa.FromPartOfSpeech
			left outer join CmPossibility_Name nm On nm.Obj=pos.Id and nm.Ws=@SenseEnc
			where msa.Id=@MsaId
			-- ToPartOfSpeech
			select top 1 @POSbID=pos.Id, @POSbTxt=isnull(nm.Txt, '***'),
					@POSbFmt=cast(null as varbinary), @POSbEnc=nm.Ws
			from MoDerivationalAffixMsa msa
			left outer join PartOfSpeech pos On pos.Id=msa.ToPartOfSpeech
			left outer join CmPossibility_Name nm On nm.Obj=pos.Id and nm.Ws=@SenseEnc
			where msa.Id=@MsaId
			set @MsaForm = @MsaForm + 'derivational: ' + @POSaTxt + ' to ' + @POSbTxt
		end
		else if @MsaClass=5117 begin	--MoUnclassifiedAffixMsa
			select top 1 @POSaID=pos.Id, @POSaTxt=isnull(nm.Txt, '***'),
					@POSaFmt=cast(null as varbinary), @POSaEnc=nm.Ws
			from MoUnclassifiedAffixMsa msa
			left outer join PartOfSpeech pos On pos.Id=msa.PartOfSpeech
			left outer join CmPossibility_Name nm On nm.Obj=pos.Id and nm.Ws=@SenseEnc
			where msa.Id=@MsaId
			set @MsaForm = @MsaForm + 'unclassified: ' + @POSaTxt
		end
		else if @MsaClass=5032 begin	--MoDerivationalStepMsa
			select top 1 @POSaID=pos.Id, @POSaTxt=isnull(nm.Txt, '***'),
					@POSaFmt=cast(null as varbinary), @POSaEnc=nm.Ws
			from MoDerivationalStepMsa msa
			left outer join PartOfSpeech pos On pos.Id=msa.PartOfSpeech
			left outer join CmPossibility_Name nm On nm.Obj=pos.Id and nm.Ws=@SenseEnc
			where msa.Id=@MsaId
			set @MsaForm = @MsaForm + ' : ' + @POSaTxt
		end

		--Put everything in temporary table
		insert @DisplayNameMsa (MsaId, MsaClass,
			MsaForm, FormId, FormClass, FormOwner, FormFlid, FormTxt, FormFmt, FormEnc,
			SenseId, SenseTxt, SenseFmt, SenseEnc,
			POSaID, POSaTxt, POSaFmt, POSaEnc,
			POSbID, POSbTxt, POSbFmt, POSbEnc)
		values (@MsaId, @MsaClass, @MsaForm,
			@FormId, @FormClass, @FormOwner, @FormFlid, @FormTxt, @FormFmt, @FormEnc,
			@SenseId, @SenseTxt, @SenseFmt, @SenseEnc,
			@POSaID, @POSaTxt, @POSaFmt, @POSaEnc,
			@POSbID, @POSbTxt, @POSbFmt, @POSbEnc)
		-- Try for another MSA.
		fetch next from @myCursor into @MsaId, @MsaClass
	end

	set @retval = 0
	select * from @DisplayNameMsa order by MsaForm

LExitWithCursor:
	close @myCursor
	deallocate @myCursor

LExitNoCursor:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	drop table #DNLE

	return @retval
go

if object_id('DisplayName_PhEnvironment') is not null
	drop proc DisplayName_PhEnvironment
go
print 'creating proc DisplayName_PhEnvironment'
go
create proc [dbo].[DisplayName_PhEnvironment]
	@XMLIds ntext = null
as
	declare @retval int, @fIsNocountOn int,
		@EnvId int, @EnvTxt nvarchar(4000),
		@CurContext int, @Txt nvarchar(4000),
		@myCursor CURSOR

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	--Table variable.
	declare @DisplayNamePhEnvironment table (
		EnvId int primary key,
		--( See the notes under string tables in FwCore.sql about the
		--( COLLATE clause.
		EnvTxt NVARCHAR(4000) COLLATE Latin1_General_BIN
		)

	if @XMLIds is null begin
		-- Do all environments.
		set @myCursor = CURSOR FAST_FORWARD for
			select Id
			from PhEnvironment
			order by id
		open @myCursor
	end
	else begin
		-- Do environments provided in xml string.
		declare @hdoc int
		exec sp_xml_preparedocument @hdoc output, @XMLIds
		if @@error <> 0 begin
			set @retval = 1
			goto LExitNoCursor
		end
		set @myCursor = CURSOR FAST_FORWARD for
			select cmo.Id
			from	openxml(@hdoc, '/root/Obj')
			with ([Id] int) as ol
			-- Remove all pretenders, since they won't 'join'.
			join CmObject cmo
				On ol.Id=cmo.Id
				and cmo.Class$ = 5097
			order by ol.[Id]
		open @myCursor
		-- Turn loose of the handle
		exec sp_xml_removedocument @hdoc
		if @@error <> 0 begin
			set @retval = 1
			goto LExitWithCursor
		end
	end

	-- Loop through all ids.
	fetch next from @myCursor into @EnvId
	while @@fetch_status = 0
	begin
		select @EnvTxt = isnull(StringRepresentation, '_')
		from PhEnvironment env
		where Id = @EnvId

		-- Update the table variable
		insert @DisplayNamePhEnvironment (EnvId, EnvTxt)
		values (@EnvId, @EnvTxt)

		-- Try for another one.
		fetch next from @myCursor into @EnvId
	end

	select * from @DisplayNamePhEnvironment
	set @retval = 0

LExitWithCursor:
	close @myCursor
	deallocate @myCursor

LExitNoCursor:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @retval
go

if object_id('DisplayName_PhPhonologicalContext') is not null begin
	drop proc DisplayName_PhPhonologicalContext
end
go
print 'creating proc DisplayName_PhPhonologicalContext'
go
create proc [dbo].[DisplayName_PhPhonologicalContext]
	@XMLIds ntext = null
as
	declare @retval int, @fIsNocountOn int,
		@CtxId int, @CtxForm nvarchar(4000),
		@myCursor CURSOR

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	--Table variable.
	declare @DisplayNamePhPhonologicalContext table (
		CtxId int primary key,
		CtxForm nvarchar(4000)
		)

	if @XMLIds is null begin
		-- Do all contexts.
		set @myCursor = CURSOR FAST_FORWARD for
			select Id
			from PhPhonologicalContext
			order by id
		open @myCursor
	end
	else begin
		-- Do contexts provided in xml string.
		declare @hdoc int
		exec sp_xml_preparedocument @hdoc output, @XMLIds
		if @@error <> 0 begin
			set @retval = 1
			goto LExitNoCursor
		end
		set @myCursor = CURSOR FAST_FORWARD for
			select cmo.Id
			from	openxml(@hdoc, '/root/Obj')
			with ([Id] int) as ol
			-- Remove all pretenders, since they won't 'join'.
			join CmObject cmo
				On ol.Id=cmo.Id
				and cmo.Class$ IN (5082, 5083, 5085, 5086, 5087)
			order by ol.[Id]
		open @myCursor
		-- Turn loose of the handle
		exec sp_xml_removedocument @hdoc
		if @@error <> 0 begin
			set @retval = 1
			goto LExitWithCursor
		end
	end

	-- Loop through all ids.
	fetch next from @myCursor into @CtxId
	while @@fetch_status = 0
	begin
		exec @retval = DisplayName_PhPhonologicalContextID @CtxId, @CtxForm output
		if @retval > 0 begin
			delete @DisplayNamePhPhonologicalContext
			goto LExitWithCursor
		end
		-- Update the temporary table
		insert @DisplayNamePhPhonologicalContext (CtxId, CtxForm)
		values (@CtxId, @CtxForm)

		-- Try for another one.
		fetch next from @myCursor into @CtxId
	end

	select * from @DisplayNamePhPhonologicalContext
	set @retval = 0

LExitWithCursor:
	close @myCursor
	deallocate @myCursor

LExitNoCursor:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	return @retval
go

if object_id('DisplayName_PhPhonologicalContextID') is not null begin
	drop proc DisplayName_PhPhonologicalContextID
end
go
print 'creating proc DisplayName_PhPhonologicalContextID'
go
create proc DisplayName_PhPhonologicalContextID
	@ContextId int,
	@ContextString nvarchar(4000) output
as
	return 0
go
print 'altering proc DisplayName_PhPhonologicalContextID'
go
ALTER proc [dbo].[DisplayName_PhPhonologicalContextID]
	@ContextId int,
	@ContextString nvarchar(4000) output
as
	declare @retval int,
		@CurId int, @Txt nvarchar(4000),
		@class int,
		@CurSeqId int, @SeqTxt nvarchar(4000), @wantSpace bit, @CurOrd int,
		@fIsNocountOn int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	set @ContextString = ''

	-- Check for legal class.
	select @CurId = isnull(Id, 0)
	from CmObject cmo
	where cmo.Id = @ContextId
		 -- Check for class being a subclass of PhPhonologicalContext
		and cmo.Class$ IN (5082, 5083, 5085, 5086, 5087)

	if @CurId > 0 begin
		select @class = Class$
		from CmObject
		where Id = @CurId

		-- Deal with subclass specific contexts.
		if @class = 5082 begin	-- PhIterationContext
			select @CurSeqId = isnull(Member, 0)
			from PhIterationContext mem
			where mem.Id = @ContextId
			if @CurSeqId = 0 begin
				set @ContextString = '(***)'
				set @retval = 1
				goto LExit
			end
			exec @retval = DisplayName_PhPhonologicalContextID @CurSeqId, @Txt output
			if @retval != 0 begin
				set @ContextString = '(***)'
				goto LExit
			end
			set @ContextString = '(' + @Txt + ')'
		end
		else if @class = 5083 begin	-- PhSequenceContext
			set @wantSpace = 0
			select top 1 @CurSeqId = Dst, @CurOrd = mem.ord
			from PhSequenceContext_Members mem
			where mem.Src = @ContextId
			order by mem.Ord
			while @@rowcount > 0 begin
				set @SeqTxt = '***'
				exec @retval = DisplayName_PhPhonologicalContextID @CurSeqId, @SeqTxt output
				if @retval != 0 begin
					set @ContextString = '***'
					goto LExit
				end
				if @wantSpace = 1
					set @ContextString = @ContextString + ' '
				set @wantSpace = 1
				set @ContextString = @ContextString + @SeqTxt
				-- Try to get next one
				select top 1 @CurSeqId = Dst, @CurOrd = mem.ord
				from PhSequenceContext_Members mem
				where mem.Src = @ContextId and mem.Ord > @CurOrd
				order by mem.Ord
			end
			--set @ContextString = 'PhSequenceContext'
		end
		else if @class = 5085 begin	-- PhSimpleContextBdry
			select top 1 @Txt = isnull(nm.Txt, '***')
			from PhSimpleContextBdry ctx
			join PhTerminalUnit tu On tu.Id = ctx.FeatureStructure
			join PhTerminalUnit_Codes cds On cds.Src = tu.Id
			join PhCode_Representation nm On nm.Obj = cds.Dst
			where ctx.Id = @CurId
			order by cds.Ord, nm.Ws
			set @ContextString = @Txt
		end
		else if @class = 5086 begin	-- PhSimpleContextNC
			select top 1 @Txt = isnull(nm.Txt, '***')
			from PhSimpleContextNC ctx
			join PhNaturalClass_Name nm On nm.Obj = ctx.FeatureStructure
			where ctx.Id = @CurId
			order by nm.Ws
			set @ContextString = '[' + @Txt + ']'
		end
		else if @class = 5087 begin	-- PhSimpleContextSeg
			select top 1 @Txt = isnull(nm.Txt, '***')
			from PhSimpleContextSeg ctx
			join PhTerminalUnit tu On tu.Id = ctx.FeatureStructure
			join PhTerminalUnit_Codes cds On cds.Src = tu.Id
			join PhCode_Representation nm On nm.Obj = cds.Dst
			where ctx.Id = @CurId
			order by cds.Ord, nm.Ws
			set @ContextString = @Txt
		end
		else begin
			set @ContextString = '***'
			set @retval = 1
			goto LExit
		end
	end
	else begin
		set @ContextString = '***'
		set @retval = 1
		goto LExit
	end
	set @retval = 0
LExit:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @retval
go

if object_id('DisplayName_PhTerminalUnit') is not null begin
	drop proc DisplayName_PhTerminalUnit
end
go
print 'creating proc DisplayName_PhTerminalUnit'
go
create proc [dbo].[DisplayName_PhTerminalUnit]
	@XMLIds ntext = null,
	@Cls int = 5092	-- PhPhoneme. 5091 is PhBdryMarker
as

declare @retval int, @fIsNocountOn int,
	@TUId int, @TUForm nvarchar(4000),
	@myCursor CURSOR

	if @Cls < 5091 or @Cls > 5092
		return 1	-- Wrong class.

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	--Table variable.
	declare @DisplayNameTU table (
		TUId int,	-- 1
		TUForm nvarchar(4000)	-- 2
		)

	if @XMLIds is null begin
		-- Do all MSAes.
		set @myCursor = CURSOR FAST_FORWARD for
			select Id
			from CmObject
			where Class$ = @Cls
			order by Id
		open @myCursor
	end
	else begin
		-- Do MSAes provided in xml string.
		declare @hdoc int
		exec sp_xml_preparedocument @hdoc output, @XMLIds
		if @@error <> 0 begin
			set @retval = 1
			goto LExitNoCursor
		end
		set @myCursor = CURSOR FAST_FORWARD for
			select cmo.Id
			from	openxml(@hdoc, '/root/Obj')
			with ([Id] int) as ol
			-- Remove all pretenders, since they won't 'join'.
			join CmObject cmo
				On ol.Id=cmo.Id
				and cmo.Class$ = @Cls
			order by ol.[Id]
		open @myCursor
		-- Turn loose of the handle
		exec sp_xml_removedocument @hdoc
		if @@error <> 0 begin
			set @retval = 1
			goto LExitWithCursor
		end
	end

	-- Loop through all ids.
	fetch next from @myCursor into @TUId
	while @@fetch_status = 0
	begin
		set @TUForm = '***'
		select top 1 @TUForm = isnull(Txt, '***')
		from PhTerminalUnit_Name
		where Obj = @TUId
		order by Ws

		select top 1 @TUForm = @TUForm + ' : ' + isnull(r.Txt, '***')
		from PhTerminalUnit_Codes c
		left outer join PhCode_Representation r On r.Obj = c.Dst
		where c.Src = @TUId
		order by c.Ord, r.Ws

		--Put everything in temporary table
		insert @DisplayNameTU (TUId, TUForm)
		values (@TUId, @TUForm)
		-- Try for another MSA.
		fetch next from @myCursor into @TUId
	end

	set @retval = 0
	select * from @DisplayNameTU order by TUForm

LExitWithCursor:
	close @myCursor
	deallocate @myCursor

LExitNoCursor:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @retval
go

if object_id('FindOrCreateCmAgent') is not null
	drop proc FindOrCreateCmAgent
go
print 'creating proc FindOrCreateCmAgent'
go
create proc [dbo].[FindOrCreateCmAgent]
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

	select @agentID=aa.Id
	from CmAgent_ aa
	join CmAgent_Name aan on aan.Obj = aa.Id and aan.Txt=@agentName
	join LanguageProject lp On lp.Id = aa.Owner$
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

	-- We don't need to wory about transactions, since the call to CreateObject_CmAgent
	-- wiil create waht is needed, and rool it back, if the creation fails.

	SELECT @wsEN=Obj
	FROM LgWritingSystem_Name
	WHERE Txt='English'

	SELECT TOP 1 @lpID=ID
	FROM LanguageProject
	ORDER BY ID

	exec @retVal = CreateObject_CmAgent
		@wsEN, @agentName,
		null,
		@isHuman,
		@version,
		@lpID,
		6001038, -- owning flid for CmAgent in LanguageProject
		null,
		@agentID out,
		@uid out

	if @retVal <> 0
	begin
		-- There was an error in CreateObject_CmAgent
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

IF OBJECT_ID('fnGetDefaultAnalysesGlosses') IS NOT NULL BEGIN
	PRINT 'removing procedure fnGetDefaultAnalysesGlosses'
	DROP FUNCTION fnGetDefaultAnalysesGlosses
END
GO
PRINT 'creating function fnGetDefaultAnalysesGlosses'
GO
CREATE FUNCTION [dbo].[fnGetDefaultAnalysesGlosses] (
	@nStTxtParaId INT, @nAnnotType INT, @nAnnotPunct INT)
RETURNS @tblDefaultAnalysesGlosses TABLE (
	WordformId INT,
	AnalysisId INT,
	GlossId INT,
	BaseAnnotationId INT,
	InstanceOf INT,
	BeginOffset INT,
	EndOffset INT,
	UserApproved INT)
AS BEGIN

	DECLARE
		@nWordformId INT,
		@nAnalysisId INT,
		@nGlossId INT

	declare @defaults table (
		WfId INT,
		AnalysisId INT,
		GlossId INT,
		[Score] INT)
	-- Get the 'real' (non-default) data
	INSERT INTO @tblDefaultAnalysesGlosses
	SELECT
		coalesce(wfwg.id, wfwa.id, a.InstanceOf) AS WordformId,
		coalesce(wawg.id, wai.id),
		wgi.id,
		ba.[Id] AS BaseAnnotationId,
		a.InstanceOf,
		ba.BeginOffset,
		ba.EndOffset,
		CASE WHEN (wai.id is not null or wawg.id is not null) then 1 else 0 end -- default is to assume not user-approved.
	FROM CmBaseAnnotation ba
	JOIN CmAnnotation a ON a.[Id] = ba.[Id]
		AND a.AnnotationType = @nAnnotType
	-- these joins handle the case that instanceof is a WfiAnalysis; all values will be null otherwise
	LEFT OUTER JOIN WfiAnalysis wai ON wai.id = a.InstanceOf -- 'real' analysis (is the instanceOf)
	LEFT OUTER JOIN CmObject waio on waio.id = wai.id -- CmObject of analysis instanceof
	LEFT OUTER JOIN CmObject wfwa on wfwa.id = waio.owner$ -- wf that owns wai
	-- these joins handle the case that instanceof is a WfiGloss; all values will be null otherwise.
	LEFT OUTER JOIN WfiGloss wgi on wgi.id = a.instanceOf -- 'real' gloss (is the instanceof)
	LEFT OUTER JOIN CmObject wgio on wgio.id = wgi.id
	LEFT OUTER JOIN CmObject wawg on wawg.id = wgio.owner$ -- ananlyis that owns wgi
	LEFT OUTER JOIN CmObject wfwg on wfwg.id = wawg.owner$ -- wordform that owns wgi (indirectly)
	WHERE ba.BeginObject = @nStTxtParaId

	-- InstanceOf is a WfiAnalysis; we fill out a default gloss if possible.
	-- If we find a WfiGloss we assume the user approves of the owning analysis. Leave UserApproved 1.

	UPDATE @tblDefaultAnalysesGlosses SET GlossId = WgId, UserApproved = 1
	FROM @tblDefaultAnalysesGlosses t
	JOIN (
		SELECT Sub2.WaId, Sub2.WgId, MAX(Sub2.CountInstance) AS MaxCountInstance
		FROM (
			SELECT t.InstanceOf AS WaId, wg.[Id] AS WgId, COUNT(ann.[Id]) AS CountInstance
			FROM @tblDefaultAnalysesGlosses t
			JOIN WfiGloss_ wg ON wg.Owner$ = t.InstanceOf
			LEFT OUTER JOIN CmAnnotation ann ON ann.InstanceOf = wg.[Id]
			GROUP BY t.InstanceOf, wg.[Id]
			) Sub2
		GROUP BY Sub2.WaId, Sub2.WgId
		) Sub1 ON Sub1.WaId = t.InstanceOf
	WHERE t.GlossId IS NULL

	-- InstanceOf is a WfiWordform. Find best WfiGloss owned by each such WfiWordform.
	-- If we find one assume owning analysis is user-approved.

	UPDATE @tblDefaultAnalysesGlosses SET GlossId = WgId, AnalysisId = WaId, UserApproved = 1
	FROM @tblDefaultAnalysesGlosses t
	JOIN (
		SELECT Sub2.WfId, Sub2.WaId, Sub2.WgId,
			MAX(Sub2.CountInstance) AS MaxCountInstance
		FROM (
			SELECT t.WordformId AS WfId, wa.[Id] AS WaId, wg.[Id] AS WgId,
				COUNT(ann.[Id]) AS CountInstance
			FROM @tblDefaultAnalysesGlosses t
			JOIN WfiAnalysis_ wa ON wa.Owner$ = t.WordformId
			JOIN WfiGloss_ wg ON wg.Owner$ = wa.[Id]
			LEFT OUTER JOIN CmAnnotation ann ON ann.InstanceOf = wg.[Id]
			GROUP BY t.WordformId, wa.[Id], wg.[Id]
			) Sub2
		GROUP BY Sub2.WfId, Sub2.WaId, Sub2.WgId
		) Sub1 ON Sub1.WfId = t.WordformId
	WHERE t.AnalysisId IS NULL

	-- Final option is InstanceOf is WfiWordform, there are analyses but no glosses
	-- Here we have to look to see whether the user approves the analysis.
	-- If the user specifically disapproves, don't even consider it (see final where clause)
	-- If the user approves set UserApproved.

	UPDATE @tblDefaultAnalysesGlosses SET AnalysisId = WaId, UserApproved = coalesce(HumanAccepted, 0)
	FROM @tblDefaultAnalysesGlosses t
	JOIN (
		SELECT Sub2.WfId, Sub2.WaId, MAX(Sub2.CountInstance) AS MaxCountInstance --, max(ag) as ag
		FROM (
			SELECT
				t.WordformId AS WfId,
				wa.[Id] AS WaId,
				COUNT(ann.[Id]) AS CountInstance --,
				--count(ag.id) as ag
			FROM @tblDefaultAnalysesGlosses t
			JOIN WfiAnalysis_ wa ON wa.Owner$ = t.WordformId
			LEFT OUTER JOIN CmAnnotation ann ON ann.InstanceOf = wa.[Id]
			LEFT OUTER JOIN (
				SELECT ae.Target
				FROM CmAgentEvaluation_ ae
				JOIN CmAgent a ON a.Id = ae.Owner$ AND Human = 1
				WHERE ae.Accepted = 0)
					aae ON aae.Target = wa.Id
			WHERE aae.Target IS NULL
			GROUP BY t.WordformId, wa.[Id]
			) Sub2
		GROUP BY Sub2.WfId, Sub2.WaId
		) Sub1 ON Sub1.WfId = t.WordformId
	LEFT OUTER JOIN (
		SELECT ae.Target, ae.Accepted AS HumanAccepted
		FROM CmAgentEvaluation_ ae
		JOIN CmAgent a ON a.Id = ae.Owner$ AND Human = 1
		) aea ON aea.Target = waId
	WHERE t.AnalysisId IS NULL
	-- Also include punctuation annotations.
	INSERT INTO @tblDefaultAnalysesGlosses
	SELECT
		NULL,
		NULL,
		NULL,
		ba.[Id] AS BaseAnnotationId,
		NULL,
		ba.BeginOffset,
		ba.EndOffset,
		1 -- arbitrary
	FROM CmBaseAnnotation ba
	JOIN CmAnnotation a ON a.[Id] = ba.[Id]
		AND a.AnnotationType = @nAnnotPunct
	WHERE ba.BeginObject = @nStTxtParaId

	RETURN
END
GO

IF OBJECT_ID('fnGetDefaultAnalysisGloss') IS NOT NULL BEGIN
	PRINT 'removing procedure fnGetDefaultAnalysisGloss'
	DROP FUNCTION fnGetDefaultAnalysisGloss
END
GO
PRINT 'creating function fnGetDefaultAnalysisGloss'
GO
CREATE FUNCTION [dbo].[fnGetDefaultAnalysisGloss] (
	@nWfiWordFormId INT)
RETURNS @tblScore TABLE (
	AnalysisId INT,
	GlossId INT,
	[Score] INT)
AS BEGIN

	INSERT INTO @tblScore
		--( wfiGloss is an InstanceOf
		SELECT
			oanalysis.[Id],
			ogloss.[Id],
			(COUNT(ann.InstanceOf) + 10000) --( needs higher # than wfiAnalsys
		FROM CmAnnotation ann
		JOIN WfiGloss g ON g.[Id] = ann.InstanceOf
		JOIN CmObject ogloss ON ogloss.[Id] = g.[Id]
		JOIN CmObject oanalysis ON oanalysis.[Id] = ogloss.Owner$
			AND oanalysis.Owner$ = @nWfiWordFormId
		JOIN WfiAnalysis a ON a.[Id] = oanalysis.[Id]
		GROUP BY oanalysis.[Id], ogloss.[Id]
	UNION ALL
		--( wfiAnnotation is an InstanceOf
		SELECT
			oanalysis.[Id],
			NULL,
			COUNT(ann.InstanceOf)
		FROM CmAnnotation ann
		JOIN CmObject oanalysis ON oanalysis.[Id] = ann.InstanceOf
			AND oanalysis.Owner$ = @nWfiWordFormId
		JOIN WfiAnalysis a ON a.[Id] = oanalysis.[Id]
		-- this is a tricky way of eliminating analyses where there exists
		-- a negative evaluation by a human agent.
		LEFT OUTER JOIN (
				SELECT ae.Target
				FROM CmAgentEvaluation_ ae
				JOIN CmAgent a ON a.Id = ae.Owner$ AND Human = 1
				WHERE ae.Accepted = 0)
					aae ON aae.Target = a.Id
			WHERE aae.Target IS NULL
		GROUP BY oanalysis.[Id]

	--( If the gloss and analysis ID are all null, there
	--( are no annotations, but an analysis (and, possibly, a gloss) still might exist.

	IF @@ROWCOUNT = 0

		INSERT INTO @tblScore
		SELECT TOP 1
			oanalysis.[Id],
			wg.id,
			0
		FROM CmObject oanalysis
		left outer join WfiGloss_ wg on wg.owner$ = oanalysis.id
		LEFT OUTER JOIN (
				SELECT ae.Target
				FROM CmAgentEvaluation_ ae
				JOIN CmAgent a ON a.Id = ae.Owner$ AND Human = 1
				WHERE ae.Accepted = 0)
					aae ON aae.Target = oanalysis.Id
		WHERE oanalysis.Owner$ = @nWfiWordFormId and aae.Target IS NULL

	RETURN
END
GO

if object_id('fnGetParseCountRange') is not null begin
	print 'removing function fnGetParseCountRange'
	drop function [fnGetParseCountRange]
end
go
print 'creating function fnGetParseCountRange'
go
CREATE FUNCTION [dbo].[fnGetParseCountRange] (
	@nAgentId INT,
	@nWritingSystem INT,
	@nAccepted BIT,
	@nRangeMin INT,
	@nRangeMax INT)
RETURNS @tblWfiWordFormsCount TABLE (
	[Id] INT,
	--( See the notes under string tables in FwCore.sql about the
	--( COLLATE clause.
	Txt NVARCHAR(4000) COLLATE Latin1_General_BIN,
	EvalCount INT)
AS
BEGIN

	--( See Class Diagram CmAgent in the doc.
	--(-------------------------------------------
	--( CmAgentEvaluation.Target -->
	--(		CmObject --( subclassed as )-->
	--(		WfiWordForm or WfiAnalysis
	--(
	--(	WfiWordForm.Analyses -->
	--(		WfiAnalysis
	--(-------------------------------------------
	--( The Target of CmAgentEvaluation may either
	--( be a WfiWordForm, or a WfiAnalysis owned
	--( by a WfiWordForm. We want the latter.

	IF @nRangeMax != 0 BEGIN

		IF @nAccepted IS NULL

			INSERT INTO @tblWfiWordFormsCount
			SELECT wordformform.[Obj], wordformform.Txt, COUNT(wordformform.[Obj]) AS EvalCount
			FROM CmAgentEvaluation agenteval
			JOIN CmObject oagenteval ON oagenteval.[Id] = agenteval.[Id]
				AND oagenteval.[Owner$] = @nAgentId
			--( Don't need to join WfiAnalysis or WfiAnalysis_ here
			JOIN CmObject oanalysis ON oanalysis.[Id] = agenteval.[Target]
			JOIN WfiWordForm_Form wordformform ON wordformform.Obj = oanalysis.[Owner$]
				AND wordformform.ws = @nWritingSystem --( WfiWordForm_Form is actually MultiTxt$ with flid
			GROUP BY wordformform.[Obj], wordformform.Txt
			HAVING COUNT(wordformform.[Obj]) BETWEEN @nRangeMin AND @nRangeMax
			ORDER BY wordformform.Txt

		ELSE

			INSERT INTO @tblWfiWordFormsCount
			SELECT wordformform.[Obj], wordformform.Txt, COUNT(wordformform.[Obj]) AS EvalCount
			FROM CmAgentEvaluation agenteval
			JOIN CmObject oagenteval ON oagenteval.[Id] = agenteval.[Id]
				AND oagenteval.[Owner$] = @nAgentId
			--( Don't need to join WfiAnalysis or WfiAnalysis_ here
			JOIN CmObject oanalysis ON oanalysis.[Id] = agenteval.[Target]
			JOIN WfiWordForm_Form wordformform ON wordformform.Obj = oanalysis.[Owner$]
				AND wordformform.ws = @nWritingSystem --( WfiWordForm_Form is actually MultiTxt$ with flid
			WHERE agenteval.accepted = @nAccepted
			GROUP BY wordformform.[Obj], wordformform.Txt
			HAVING COUNT(wordformform.[Obj]) BETWEEN @nRangeMin AND @nRangeMax
			ORDER BY wordformform.Txt

	END
	ELSE --( IF @nRangeMax = 0

		--( 0 Parses:	wordform has an evaluation, but analyses--if
		--(				any--don't have evaluations

		--( Randy Regnier:
		--( I think it will have an evaluation, but for cases where the
		--( parser couldn't come up with any parses at all, I add a CmBaseAnnotation,
		--( and set its InstanceOfRAHvo and BeginObjectRAHvo to the HVO of the wordform.
		--( The CompDetails of the annotation will say "Analysis Failure".
		--( <snip>
		--( John Thomson: Which 'it' will have an evaluation?
		--( <snip>
		--( RR: We add evaluations to both the wordform and any parses retruned by the
		--( parser. In the case of no parses being returned, we jsut add an evaluation
		--( top the wordform, along with the annotation.

		INSERT INTO @tblWfiWordFormsCount
		SELECT wordformform.[Obj], wordformform.Txt, 0 AS EvalCount
		FROM WfiWordForm_Form wordformform
		JOIN CmAgentEvaluation agenteval ON agenteval.Target = wordformform.Obj
		JOIN CmObject oagenteval ON oagenteval.[Id] = agenteval.[Id]
			AND oagenteval.[Owner$] = @nAgentId
		LEFT OUTER JOIN CmObject oAnalysis ON oAnalysis.Owner$ = wordformform.Obj
		LEFT OUTER JOIN CmAgentEvaluation aneval ON aneval.Target = oanalysis.[Id]
		WHERE aneval.Target IS NULL

	RETURN
END
GO

if object_id('dbo.fnGetSensesInEntry$') is not null begin
	print 'removing function fnGetSensesInEntry$'
	drop function dbo.fnGetSensesInEntry$
end
print 'creating function fnGetSensesInEntry$'
go
CREATE FUNCTION [dbo].[fnGetSensesInEntry$] (
	@nEntryId INT )
RETURNS @tblLexSenses TABLE (
	EntryId INT,
	OwnerId	INT,
	SenseId	INT,
	Ord	INT,
	depth INT,
	SenseNum NVARCHAR(1000),
	SenseNumDummy NVARCHAR(1000) )
AS
BEGIN
	DECLARE
		@nCurDepth INT,
		@nRowCount INT,
		@vcStr VARCHAR(100),
		@SenseId INT

	SET @nCurDepth = 0

	--== Get senses for all entries ==--

	IF @nEntryId IS NULL BEGIN
		-- insert lexical sense at the highest depth - sense related directly to the specified entry
		insert into @tblLexSenses
		select 	le.[Id], les.Src, les.Dst, les.ord, @nCurDepth,
			replicate('', 5-len(convert(nvarchar(10), les.ord)))+convert(nvarchar(10), les.ord),
			replicate('  ', 5-len(convert(nvarchar(10), les.ord)))+convert(nvarchar(10), les.ord)
		from LexEntry_Senses les
		JOIN LexEntry le ON le.[Id] = les.[Src]

		-- loop through the reference sequence hierarchy getting each of the senses at every depth
		set @nRowCount = @@rowcount
		while @nRowCount > 0
		begin
			set @nCurDepth = @nCurDepth + 1

			insert into @tblLexSenses
			select 	ls.EntryId, ls.SenseId, lst.Dst, lst.ord, @nCurDepth,
				SenseNum+'.'+replicate('', 5-len(convert(nvarchar(10), lst.ord)))+convert(nvarchar(10), lst.ord),
				SenseNumDummy+'.'+replicate('  ', 5-len(convert(nvarchar(10), lst.ord)))+convert(nvarchar(10), lst.ord)
			from	@tblLexSenses ls
			join lexSense_Senses lst on ls.SenseId = lst.Src
			where	depth = @nCurDepth - 1
			--( The original procedure had an order by SenseNumDummy here.

			set @nRowCount = @@rowcount
		end
	END

	--== Get senses for specified entry ==--

	ELSE BEGIN
		-- insert lexical sense at the highest depth - sense related directly to the specified entry
		insert into @tblLexSenses
		select 	@nEntryId, les.Src, les.Dst, les.ord, @nCurDepth,
			replicate('', 5-len(convert(nvarchar(10), les.ord)))+convert(nvarchar(10), les.ord),
			replicate('  ', 5-len(convert(nvarchar(10), les.ord)))+convert(nvarchar(10), les.ord)
		from	LexEntry_Senses les
		where	les.Src = @nEntryId

		-- loop through the reference sequence hierarchy getting each of the senses at every depth
		set @nRowCount = @@rowcount
		while @nRowCount > 0
		begin
			set @nCurDepth = @nCurDepth + 1

			insert into @tblLexSenses
			select 	@nEntryId, ls.SenseId, lst.Dst, lst.ord, @nCurDepth,
				SenseNum+'.'+replicate('', 5-len(convert(nvarchar(10), lst.ord)))+convert(nvarchar(10), lst.ord),
				SenseNumDummy+'.'+replicate('  ', 5-len(convert(nvarchar(10), lst.ord)))+convert(nvarchar(10), lst.ord)
			from	@tblLexSenses ls
			join lexSense_Senses lst on ls.SenseId = lst.Src
			where	depth = @nCurDepth - 1
			--( The original procedure had an order by SenseNumDummy here.

			set @nRowCount = @@rowcount
		end
	END

	RETURN
END
go

if object_id('GetEntriesAndSenses$') is not null begin
	print 'removing proc GetEntriesAndSenses$'
	drop proc GetEntriesAndSenses$
end
print 'creating proc GetEntriesAndSenses$'
go
create proc [dbo].[GetEntriesAndSenses$]
	@LdbId as integer = null,
	@aenc as integer = null,
	@vws as integer = null
as
	declare @fIsNocountOn int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- Make sure we have the LDB id.
	if @LdbId is null begin
		select top 1 @LdbId=ldb.Id
		from LexicalDatabase ldb
		order by ldb.Id
	end

	-- Make sure we have the analysis writing system
	if @aenc is null begin
		select top 1 @aenc=Lg.Id
		from languageProject_CurrentAnalysisWritingSystems cae
		join LgWritingSystem lg On Lg.Id=cae.Dst
		order by cae.ord
	end

	-- Make sure we have the vernacular writing system
	if @vws is null begin
		select top 1 @vws=Lg.Id
		from languageProject_CurrentVernacularWritingSystems cve
		join LgWritingSystem lg On Lg.Id=cve.Dst
		order by cve.ord
	end

	DECLARE @tblSenses TABLE (
		entryId int,
		ownrId int,
		sensId int,
		ord int,
		depth int,
		sensNum nvarchar(1000)	)

	declare @leId as int
	SET @leId = NULL --( NULL gets all entries in fnGetSensesInEntry$

	INSERT INTO @tblSenses
		SELECT
			EntryId,
			OwnerId,
			SenseId,
			Ord,
			Depth,
			SenseNum
		FROM dbo.fnGetSensesInEntry$(@leId)

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	-- REVIEW (SteveMiller): MultiTxt$.fmt was always NULL. Don't understand the reason
	-- for them being selected here.

	-- Select entry information
	select le.Id, le.Class$, le.HomographNumber,
		isnull(cf.Txt, 'N/F') As CitationForm,
		cast(null as varbinary) As CitationFormFmt,
		isnull(mfuf.Txt, 'N/F') As UnderlyingForm,
		cast(null as varbinary) As UnderlyingFormFmt,
		isnull(mflf.Txt, 'no form') As LexicalForm,
		cast(null as varbinary) As LexicalFormFmt
	from LexEntry_ le
	left outer join LexEntry_CitationForm cf On cf.Obj=le.Id and cf.Ws=@vws
	left outer join LexEntry_LexemeForm uf On uf.Src=le.Id
	left outer join MoForm_Form mfuf On mfuf.Obj=uf.Dst and mfuf.Ws=@vws
	left outer join LexEntry_AlternateForms a On a.Src=le.Id
	left outer join MoForm_Form mflf On mflf.Obj=a.Dst and mflf.Ws=@vws
	where @ldbId=le.Owner$
	order by le.Id

	-- REVIEW (SteveMiller): MultiTxt$.fmt was always NULL. Don't understand the reason
	-- for them being selected here.

	-- Select sense information in another rowset
	select ls.entryId As EntryId,
		isnull(ls.sensId, 0) As SenseID,
		ls.sensNum As SenseNum,
		isnull(lsg.Txt, 'no gloss') As Gloss,
		cast(null as varbinary) As GlossFmt,
		isnull(lsd.Txt, 'no def') As Definition,
		cast(null as varbinary) As DefinitionFmt
	from @tblSenses ls
	left outer join LexSense_Gloss lsg On lsg.Obj=ls.sensId and lsg.Ws=@aenc
	left outer join LexSense_Definition lsd On lsd.Obj=ls.sensId and lsd.Ws=@aenc
	order by ls.entryId, ls.sensNum

	return 0
go

if object_id('GetEntryForSense') is not null begin
	drop proc GetEntryForSense
end
print 'creating proc GetEntryForSense'
go
create proc [dbo].[GetEntryForSense]
	@SenseId as integer
as
	declare @OwnerId int, @OwnFlid int, @ObjId int
	declare @fIsNocountOn int

	set @OwnerId = 0
	if @SenseId < 1 return 1	-- Bad Id

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	set @OwnFlid = 0
	set @ObjId = @SenseId

	-- Loop until we find an owning flid of 5002011 (or null for some ownership error).
	while @OwnFlid != 5002011
	begin
		select 	@OwnerId=isnull(Owner$, 0), @OwnFlid=OwnFlid$
		from	CmObject
		where	Id=@ObjId

		set @ObjId=@OwnerId
		if @OwnerId = 0
			return 1
	end

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	-- select the sense back to the caller
	select 	@OwnerId LeId
	return 0
go

if object_id('GetHeadwordsForEntriesOrSenses') is not null begin
	print 'removing function GetHeadwordsForEntriesOrSenses'
	drop function [GetHeadwordsForEntriesOrSenses]
end
print 'creating function GetHeadwordsForEntriesOrSenses'
go
CREATE FUNCTION [dbo].[GetHeadwordsForEntriesOrSenses] ()
RETURNS @ObjIdInfo TABLE (
	ObjId int,
	ClassId int,
	Headword nvarchar(4000))
AS
BEGIN

DECLARE @nvcAllo nvarchar(4000), @nHomograph int, @nvcPostfix nvarchar(4000),
		@nvcPrefix nvarchar(4000), @nvcHeadword nvarchar(4000)
DECLARE @objId int, @objOwner int, @objOwnFlid int, @objClass int, @hvoEntry int,
	@nvcSenseNum nvarchar(4000), @objId2 int
INSERT INTO @ObjIdInfo (ObjId, ClassId, Headword)
	SELECT Dst, NULL, NULL FROM LexReference_Targets
	UNION
	SELECT Dst, NULL, NULL FROM LexEntry_MainEntriesOrSenses
DECLARE cur CURSOR local static forward_only read_only FOR
	SELECT id, Class$, Owner$, OwnFlid$
		FROM CmObject
		WHERE Id in (SELECT ObjId FROM @ObjIdInfo)
OPEN cur
FETCH NEXT FROM cur INTO @objId, @objClass, @objOwner, @objOwnFlid
WHILE @@FETCH_STATUS = 0
BEGIN
	IF @objClass = 5002 BEGIN -- LexEntry
		SET @hvoEntry=@objId
	END
	ELSE BEGIN
		IF @objOwnFlid = 5002011 BEGIN -- LexEntry_Senses
			SET @hvoEntry=@objOwner
		END
		ELSE BEGIN
			while @objOwnFlid != 5002011
			begin
				set @objId2=@objOwner
				select 	@objOwner=isnull(Owner$, 0), @objOwnFlid=OwnFlid$
				from	CmObject
				where	Id=@objId2
				if @objOwner = 0 begin
					SET @objOwnFlid = 5002011
				end
			end
			SET @hvoEntry=@objOwner
		END
	END

	SELECT @nvcAllo=f.Txt, @nHomograph=le.HomographNumber, @nvcPostfix=t.Postfix,
			@nvcPrefix=t.Prefix, @nvcSenseNum=s.SenseNum
		FROM LexEntry le
		LEFT OUTER JOIN LexEntry_LexemeForm a on a.Src=le.id
		LEFT OUTER JOIN MoForm_Form f on f.Obj=a.Dst
		LEFT OUTER JOIN MoForm mf on mf.Id=a.Dst
		LEFT OUTER JOIN MoMorphType t on t.Id=mf.MorphType
		LEFT OUTER JOIN dbo.fnGetSensesInEntry$ (@hvoEntry) s on s.SenseId=@objId
		WHERE le.Id = @hvoEntry

	IF @nvcPrefix is null SET @nvcHeadword=@nvcAllo
	ELSE SET @nvcHeadword=@nvcPrefix+@nvcAllo
	IF @nvcPostfix is not null SET @nvcHeadword=@nvcHeadword+@nvcPostfix
	IF @nHomograph <> 0 SET @nvcHeadword=@nvcHeadword+CONVERT(nvarchar(20), @nHomograph)
	IF @nvcSenseNum is not null SET @nvcHeadword=@nvcHeadword+' '+@nvcSenseNum
	UPDATE @ObjIdInfo SET Headword=@nvcHeadword, ClassId=@objClass
		WHERE ObjId=@objId

	FETCH NEXT FROM cur INTO @objId, @objClass, @objOwner, @objOwnFlid
END
CLOSE cur
DEALLOCATE cur

RETURN
END
go

if object_id('dbo.GetSensesForSense') is not null begin
	print 'removing proc GetSensesForSense'
	drop proc dbo.GetSensesForSense
end
print 'creating proc GetSensesForSense'
go
create proc [dbo].[GetSensesForSense]
	@SenseId as integer
as
	declare @nCurDepth int, @rowCnt int, @str varchar(100)
	declare @fIsNocountOn int

	set @nCurDepth = 0

	declare @lexSenses table (
		ownrId	int,
		sensId	int,
		ord	int,
		depth	int,
		sensNum	nvarchar(1000)
	)

	-- deterimine if no count is currently set to on
	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	insert into @lexSenses
	select 	Src, Dst, ord, @nCurDepth, convert(nvarchar(10), ord+1)
	from	lexSense_Senses
	where	Src = @SenseId

	set @rowCnt = @@rowcount
	while @rowCnt > 0
	begin
		set @nCurDepth = @nCurDepth + 1

		insert into @lexSenses
		select 	lst.Src, lst.Dst, lst.ord, @nCurDepth, sensNum+'.'+replicate(' ', 5-len(convert(nvarchar(10), lst.ord+1)))+convert(nvarchar(10), lst.ord+1)
		from	@lexSenses ls
		join lexSense_Senses lst on ls.sensId = lst.Src
		where	depth = @nCurDepth - 1

		set @rowCnt = @@rowcount
	end

	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off

	select 	*
	from 	@lexSenses
	order by sensNum
go

IF OBJECT_ID('IsAgentAgreement$') IS NOT NULL BEGIN
	PRINT 'removing procedure IsAgentAgreement$'
	DROP PROCEDURE IsAgentAgreement$
END
GO
PRINT 'creating procedure IsAgentAgreement$'
GO
CREATE PROCEDURE [dbo].[IsAgentAgreement$] (
	@nWfiWordFormId INT,
	@nAgentId1 INT,
	@nAgentId2 INT,
	@fAgreement BIT OUTPUT)
AS
BEGIN

	DECLARE @tblAgentEvals1 TABLE ([Id] INT, Target INT, [Accepted] BIT)
	DECLARE @tblAgentEvals2 TABLE ([Id] INT, Target INT, [Accepted] BIT)

	DECLARE
		@nCount1 INT,
		@nCount2 INT

	SET @fAgreement = 1

	INSERT INTO @tblAgentEvals1
	SELECT ae.[Id], ae.Target, ae.[Accepted]
	FROM CmAgentEvaluation ae
	JOIN CmObject oAgentEval ON oAgentEval.[Id] = ae.[Id]
		AND oAgentEval.Owner$ = @nAgentId1
	JOIN CmObject oWordAnal ON oWordAnal.[Id] = ae.Target
		AND oWordAnal.Owner$ = @nWfiWordFormId

	INSERT INTO @tblAgentEvals2
	SELECT ae.[Id], ae.Target, ae.[Accepted]
	FROM CmAgentEvaluation ae
	JOIN CmObject oAgentEval ON oAgentEval.[Id] = ae.[Id]
		AND oAgentEval.Owner$ = @nAgentId2
	JOIN CmObject oWordAnal ON oWordAnal.[Id] = ae.Target
		AND oWordAnal.Owner$ = @nWfiWordFormId

	--( Make sure all are accepted

	SELECT @nCount1 = COUNT(*) FROM @tblAgentEvals1 WHERE [Accepted] = 0
	SELECT @nCount2 = COUNT(*) FROM @tblAgentEvals2 WHERE [Accepted] = 0

	IF @nCount1 + @nCount2 > 0
		SET @fAgreement = 0

	--( All evaluations are marked accepted. Make sure the analyses
	--( from the two different agents line up

	ELSE BEGIN
		SET @nCount1 = 0

		SELECT @nCount1 = COUNT(*)
		FROM @tblAgentEvals1 a1
		RIGHT OUTER JOIN @tblAgentEvals2 a2 ON a2.Target = a1.Target
		WHERE a1.[Id] IS NULL

		IF @nCount1 > 0
			SET @fAgreement = 0

		ELSE BEGIN
			SET @nCount1 = 0

			SELECT @nCount1 = COUNT(*)
			FROM @tblAgentEvals2 a2
			RIGHT OUTER JOIN @tblAgentEvals1 a1 ON a1.Target = a2.Target
			WHERE a2.[Id] IS NULL

			IF @nCount1 > 0
				SET @fAgreement = 0
		END
	END

	RETURN @fAgreement
END
GO

if object_id('MakeMissingAnalysesFromLexicion') is not null begin
	drop proc MakeMissingAnalysesFromLexicion
end
go
print 'creating proc MakeMissingAnalysesFromLexicion'
go
CREATE  proc [dbo].[MakeMissingAnalysesFromLexicion]
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
	exec CreateObject_WfiAnalysis @wfid, 5062002, null, @analysisid out, @NewObjGuid out, 0, @NewObjTimestamp
	exec CreateObject_WfiMorphBundle null, null, null, @analysisid, 5059011, null, @mbid out, @NewObjGuid out, 0, @NewObjTimestamp
	exec CreateObject_WfiGloss @ws, @gloss, @analysisid, 5059010, null, @wgid out, @NewObjGuid out, 0, @NewObjTimestamp
	update WfiMorphBundle set Morph = @formid, Msa = @msaid, Sense = @senseid where id = @mbid
	update WfiAnalysis set Category = @pos where id = @analysisid
	fetch wf_cur into @wfid, @formid, @senseid, @msaid, @gloss, @pos
end
close wf_cur
deallocate wf_cur
go

if object_id('PATRString_FsFeatureStructure') is not null begin
	drop proc PATRString_FsFeatureStructure
end
go
if object_id('PATRString_FsAbstractStructure') is not null begin
	drop proc PATRString_FsAbstractStructure
end
go
if object_id('PATRString_FsFeatureSpecification') is not null begin
	drop proc PATRString_FsFeatureSpecification
end
go

-- Create to 'empty' SPs.
print 'creating proc PATRString_FsFeatureSpecification'
go
create proc [dbo].[PATRString_FsFeatureSpecification]
	@Def nvarchar(1),
	@Id int,
	@PATRString nvarchar(4000) output
as
	return 1
go
print 'creating proc PATRString_FsAbstractStructure'
go
create proc [dbo].[PATRString_FsAbstractStructure]
	@Def nvarchar(1),
	@Id int,
	@PATRString nvarchar(4000) output
as
	return 1
go
-- Create real top-level SPs.
print 'altering proc PATRString_FsAbstractStructure'
go
ALTER proc [dbo].[PATRString_FsAbstractStructure]
	@Def nvarchar(1),
	@Id int,
	@PATRString nvarchar(4000) output
as
	declare @fIsNocountOn int, @retval int,
		@fNeedSpace bit, @CurDstId int,
		@Txt NVARCHAR(4000),
		@Class int,
		@fNeedSlash bit

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- Get class info.
	select @Class = Class$
	from CmObject
	where Id = @Id

	if @Class = 2009 begin	-- FsFeatureStructure
		set @PATRString = '['

		-- Handle disjunctions, if any
		select top 1 @CurDstId = Dst
		from FsFeatureStructure_FeatureDisjunctions
		where Src = @Id
		order by Dst
		while @@rowcount > 0 begin
			exec @retval = PATRString_FsAbstractStructure @Def, @CurDstId, @Txt output
			if @retval != 0 begin
				set @PATRString = '[]'
				goto LFail
			end
			set @PATRString = @PATRString + @Txt
			-- Try getting another one
			select top 1 @CurDstId = Dst
			from FsFeatureStructure_FeatureDisjunctions
			where Src = @Id and Dst > @CurDstId
			order by Dst
		end

		-- Handle FeatureSpecs, if any
		set @fNeedSpace = 0
		select top 1 @CurDstId = Dst
		from FsFeatureStructure_FeatureSpecs
		where Src = @Id
		order by Dst
		while @@rowcount > 0 begin
			exec @retval = PATRString_FsFeatureSpecification @Def, @CurDstId, @Txt output
			if @retval != 0 begin
				set @PATRString = '[]'
				goto LFail
			end
			if @fNeedSpace = 1 set @PATRString = @PATRString + ' '
			else set @fNeedSpace = 1
			set @PATRString = @PATRString + @Txt
			-- Try getting another one
			select top 1 @CurDstId = Dst
			from FsFeatureStructure_FeatureSpecs
			where Src = @Id and Dst > @CurDstId
			order by Dst
		end

		set @PATRString = @PATRString + ']'
	end
	else if @Class = 2010 begin	-- FsFeatureStructureDisjunction
		set @PATRString = '{'
		-- Handle contents, if any
		set @fNeedSlash = 0
		select top 1 @CurDstId = Dst
		from FsFeatureStructureDisjunction_Contents
		where Src = @Id
		order by Dst
		while @@rowcount > 0 begin
			exec @retval = PATRString_FsAbstractStructure @Def, @CurDstId, @Txt output
			if @retval != 0 begin
				set @PATRString = ''
				goto LFail
			end
			if @fNeedSlash = 1 set @PATRString = @PATRString + ' '
			else set @fNeedSlash = 1
			set @PATRString = @PATRString + @Txt
			-- Try getting another one
			select top 1 @CurDstId = Dst
			from FsFeatureStructureDisjunction_Contents
			where Src = @Id and Dst > @CurDstId
			order by Dst
		end
		set @PATRString = @PATRString + '}'
	end
	else begin	-- unknown class.
		set @retval = 1
		set @PATRString = '[]'
		goto LFail
	end
	set @retval = 0
LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @retval
go
print 'altering proc PATRString_FsFeatureSpecification'
go
ALTER proc [dbo].[PATRString_FsFeatureSpecification]
	@Def nvarchar(1),
	@Id int,
	@PATRString nvarchar(4000) output
as
	declare @fIsNocountOn int, @retval int,
		@tCount int, @cCur int, @CurId int,
		@CurDstId int, @Class int,
		@ValueId int, @ValueClass int, @FDID int,
		@Label nvarchar(4000), @Value nvarchar(4000)

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	-- Get name of FsFeatureDefn.
	-- If there is no name or FsFeatureDefn, then quit.
	-- We don't care which writing system is used.
	select top 1 @Label = fdn.Txt, @FDID = fd.Id
	from FsFeatureSpecification fs
	join FsFeatureDefn fd On fs.Feature = fd.Id
	join FsFeatureDefn_Name fdn On fd.Id = fdn.Obj
	where fs.Id = @Id
	order by Ws
	-- Check for null value in @PATRString
	if @Label is null begin
		set @PATRString = ''
		set @retval = 1
		goto LFail
	end

	-- Handle various values in subclasses of FsFeatureSpecification
	select @Class = Class$
	from CmObject
	where Id = @Id
	if @Class = 2003 begin	-- FsClosedValue
		select top 1 @Value = Txt
		from FsClosedValue cv
		join FsSymbolicFeatureValue sfv On cv.Value = sfv.Id
		join FsSymbolicFeatureValue_Name sfvn On sfvn.Obj = sfv.Id
		where cv.Id = @Id
		if @Value is null begin
			-- Try default value.
			select @FDID=Dst
			from FsFeatureDefn_Default
			where Src=@FDID
			exec @retval = PATRString_FsFeatureSpecification '!', @FDID, @Value output
			if @retval != 0 begin
				set @PATRString = ''
				set @retval = 1
				goto LFail
			end
			set @PATRString = @Value
		end
		else set @PATRString = @Label + ':' + @Def + @Value
	end
	else if @Class = 2006 begin	-- FsDisjunctiveValue
		set @PATRString = @Label + ':{'
		set @tCount = 0
		select top 1 @CurDstId = Dst
		from FsDisjunctiveValue_Value
		where Src = @Id
		order by Dst
		set @Value = ''
		while @@rowcount > 0 begin
			if @tCount > 0 set @PATRString = @PATRString + ' '
			if @Def = '!' set @PATRString = @PATRString + '!'
			set @tCount = 1
			select top 1 @Value = Txt
			from FsSymbolicFeatureValue sfv
			join FsSymbolicFeatureValue_Name sfvn On sfvn.Obj = sfv.Id
			where sfv.Id = @CurDstId
			if @Value is null begin
				-- Try default value.
				select @FDID=Dst
				from FsFeatureDefn_Default
				where Src=@FDID
				exec @retval = PATRString_FsFeatureSpecification '!', @FDID, @Value output
				if @retval != 0 begin
					set @PATRString = ''
					set @retval = 1
					goto LFail
				end
				set @PATRString = @PATRString + @Value
			end
			else set @PATRString = @PATRString + @Value
			-- Try getting another one
			select top 1 @CurDstId = Dst
			from FsDisjunctiveValue_Value
			where Src = @Id and Dst > @CurDstId
			order by Dst
		end
		set @PATRString = @PATRString + '}'
	end
	else if @Class = 2013 begin	-- FsNegatedValue
		select top 1 @Value = Txt
		from FsNegatedValue nv
		join FsSymbolicFeatureValue sfv On nv.Value = sfv.Id
		join FsSymbolicFeatureValue_Name sfvn On sfvn.Obj = sfv.Id
		where nv.Id = @Id
		if @Value is null begin
			-- Try default value.
			select @FDID=Dst
			from FsFeatureDefn_Default
			where Src=@FDID
			exec @retval = PATRString_FsFeatureSpecification '!', @FDID, @Value output
			if @retval != 0 begin
				set @PATRString = ''
				set @retval = 1
				goto LFail
			end
			set @PATRString = @Value
		end
		else set @PATRString = @Label + ':~' + @Def + @Value
	end
	else if @Class = 2005 begin	-- FsComplexValue
		-- Need to get class of Value, so we call the right SP.
		select @ValueClass = cmo.Class$, @ValueId = cmo.Id
		from FsComplexValue_Value cvv
		join CmObject cmo On cvv.Dst = cmo.Id
		where cvv.Src = @Id
		if @ValueClass is null or @ValueId is null begin
			declare @cmpxFS int, @cmpxValId int
			-- Try default value.
			select @cmpxFS=Dst
			from FsFeatureDefn_Default
			where Src=@FDID
			if @cmpxFS is null begin
				set @PATRString = ''
				set @retval = 1
				goto LFail
			end
			select @cmpxValId=Dst
			from FsComplexValue_Value
			where Src=@cmpxFS
			if @cmpxValId is null begin
				set @PATRString = ''
				set @retval = 1
				goto LFail
			end
			exec @retval = PATRString_FsAbstractStructure '!', @cmpxValId, @Value output
			if @retval != 0 begin
				set @PATRString = ''
				set @retval = 1
				goto LFail
			end
			set @PATRString = @Value
		end
		else if @ValueClass = 2009 or @ValueClass = 2010 begin
			-- FsFeatureStructure or FsFeatureDisjunction
			exec @retval = PATRString_FsAbstractStructure @Def, @ValueId, @Value output
			if @retval != 0 begin
				set @PATRString = ''
				set @retval = 1
				goto LFail
			end
			set @PATRString = @Label + ':' + @Def + @Value
		end
		else begin	-- Bad class.
			set @PATRString = ''
			set @retval = 1
			goto LFail
		end
		set @PATRString = @Label + ':' + @Value
	end
	else if @Class = 2015 begin	-- FsOpenValue
		-- We don't care which writing system is used.
		select top 1 @Value = Txt
		from FsOpenValue_Value
		where Obj=@Id
		order by Ws
		if @Value is null begin
			set @PATRString = ''
			set @retval = 1
			goto LFail
		end
		set @PATRString = @Label + ':' + @Value
	end
	else if @Class = 2016 begin	-- FsSharedValue
		-- We don't do FsSharedValue at the moment.
		set @PATRString = ''
		set @retval = 1
		goto LFail
	end
	else begin
		-- Unknown class
		set @PATRString = ''
		set @retval = 1
		goto LFail
	end

	set @retval = 0
LFail:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @retval
go
print 'creating proc PATRString_FsFeatureStructure'
go
create proc [dbo].[PATRString_FsFeatureStructure]
	@XMLOut bit = 0,
	@XMLIds ntext = null
as
	declare @retval int,
		@CurId int, @Txt nvarchar(4000),
		@fIsNocountOn int

	set @fIsNocountOn = @@options & 512
	if @fIsNocountOn = 0 set nocount on

	--Table variable.
	declare @FS table (
		Id int,
		PATRTxt nvarchar(4000) )

	if @XMLIds is null begin
		-- Do all feature structures.
		insert into @FS (Id, PATRTxt)
			select	Id, '[]'
			from	FsFeatureStructure_
			where OwnFlid$ != 2005001 -- Owned by FsComplexValue
				and OwnFlid$ != 2010001 -- Owned by FsFeatureStructureDisjunction
	end
	else begin
		-- Do feature structures provided in xml string.
		declare @hdoc int
		exec sp_xml_preparedocument @hdoc output, @XMLIds
		if @@error <> 0 begin
			set @retval = 1
			goto LExit
		end
		insert into @FS (Id, PATRTxt)
			select	ol.[Id], '[]'
			from	openxml(@hdoc, '/FeatureStructures/FeatureStructure')
			with ([Id] int) as ol
			-- Remove all pretenders, since they won't 'join'.
			join CmObject cmo
				On ol.Id=cmo.Id
				and cmo.Class$=2009 -- Check for class being FsFeatureStructure
				and cmo.OwnFlid$ != 2005001 -- Owned by FsComplexValue
				and cmo.OwnFlid$ != 2010001 -- Owned by FsFeatureStructureDisjunction
		-- Turn loose of the handle
		exec sp_xml_removedocument @hdoc
		if @@error <> 0 begin
			set @retval = 1
			goto LExit
		end
	end

	-- Loop through all ids.
	select top 1 @CurId = Id
	from @FS
	order by Id
	while @@rowcount > 0 begin
		-- Call PATRString_FsAbstractStructure for each ID. It will return the PATR string.
		exec @retval = PATRString_FsAbstractStructure '', @CurId, @Txt output
		-- Note: If @retval is not 0, then we already are set to use '[]'
		-- for the string, so nothing mnore need be done.
		if @retval = 0 begin
			update @FS
			Set PATRTxt = @Txt
			where Id = @CurId
		end
		-- Try for another one.
		select top 1 @CurId = Id
		from @FS
		where Id > @CurId
		order by Id
	end

	if @XMLOut = 0
		select * from @FS
	else
		select * from @FS for xml auto
	set @retval = 0
LExit:
	-- if nocount was turned on turn it off
	if @fIsNocountOn = 0 set nocount off
	return @retval
go

if object_id('RemoveParserApprovedAnalyses$') is not null begin
	print 'removing proc RemoveParserApprovedAnalyses$'
	drop proc [RemoveParserApprovedAnalyses$]
end
go
print 'creating proc RemoveParserApprovedAnalyses$'
go
CREATE PROC [dbo].[RemoveParserApprovedAnalyses$]
	@nWfiWordFormID INT
AS
	DECLARE
		@nIsNoCountOn INT,
		@nGonnerId INT,
		@nParserAgentId INT,
		@humanAgentId INT,
		@nError INT

	SET @nIsNoCountOn = @@OPTIONS & 512
	IF @nIsNoCountOn = 0
		SET NOCOUNT ON

	SET @nError = 0

	-- Set checksum to zero
	UPDATE WfiWordform SET Checksum=0 WHERE Id=@nWfiWordFormID

	--( Get the parser agent id
	SELECT TOP 1 @nParserAgentId = Obj
	FROM CmAgent_Name
	WHERE Txt = N'M3Parser'
	-- Get Id of the 'default user' agent
	SELECT TOP 1 @humanAgentId = Obj
	FROM CmAgent_Name nme
	WHERE Txt = N'default user'

	--== Delete all parser evaluations that reference analyses owned by the @nWfiWordFormID wordform. ==--
	SELECT TOP 1 @nGonnerId = ae.[Id]
	FROM CmAgentEvaluation ae
	JOIN CmObject objae
		ON objae.[Id] = ae.[Id] AND objae.Owner$ = @nParserAgentId
	JOIN CmObject objanalysis
		ON objanalysis.[Id] = ae.Target
		AND objanalysis.Class$ = 5059 -- WfiAnalysis objects
		AND objanalysis.Owner$ = @nWfiWordFormID
	ORDER BY ae.[Id]
	WHILE @@ROWCOUNT != 0 BEGIN
		EXEC @nError = DeleteObj$ @nGonnerId
		IF @nError != 0
			GOTO Finish

		SELECT TOP 1 @nGonnerId = ae.[Id]
		FROM CmAgentEvaluation ae
		JOIN CmObject objae
			ON objae.[Id] = ae.[Id] AND objae.Owner$ = @nParserAgentId
		JOIN CmObject objanalysis
			ON objanalysis.[Id] = ae.Target
			AND objanalysis.Class$ = 5059 -- WfiAnalysis objects
			AND objanalysis.Owner$ = @nWfiWordFormID
		WHERE ae.[Id] > @nGonnerId
		ORDER BY ae.[Id]
	END

	--== Delete orphan analyses owned by the given @nWfiWordFormID wordform. ==--
	--== 'Orphan' means they have no evaluations ==--
	SELECT TOP 1 @nGonnerId = analysis.[Id]
	FROM CmObject analysis
	LEFT OUTER JOIN cmAgentEvaluation cae
		ON cae.Target = analysis.[Id]
	WHERE cae.Target IS NULL
		AND analysis.OwnFlid$ = 5062002		-- 5062002
		AND analysis.Owner$ = @nWfiWordFormID
	ORDER BY analysis.[Id]
	WHILE @@ROWCOUNT != 0 BEGIN
		EXEC @nError = DeleteObj$ @nGonnerId
		IF @nError != 0
			GOTO Finish

		SELECT TOP 1 @nGonnerId = analysis.[Id]
		FROM CmObject analysis
		LEFT OUTER JOIN cmAgentEvaluation cae
			ON cae.Target = analysis.[Id]
		WHERE cae.Target IS NULL
			AND analysis.[Id] > @nGonnerId
			AND analysis.OwnFlid$ = 5062002		-- 5062002
			AND analysis.Owner$ = @nWfiWordFormID
		ORDER BY analysis.[Id]
	END

Finish:
	IF @nIsNocountOn = 0 SET NOCOUNT OFF
	RETURN @nError
GO

if object_id('RemoveUnusedAnalyses$') is not null begin
	print 'removing proc RemoveUnusedAnalyses$'
	drop proc [RemoveUnusedAnalyses$]
end
go
print 'creating proc RemoveUnusedAnalyses$'
go
CREATE PROCEDURE [dbo].[RemoveUnusedAnalyses$]
	@nAgentId INT,
	@nWfiWordFormID INT,
	@dtEval DATETIME
AS
	DECLARE
		@nIsNoCountOn INT,
		@nGonnerID INT,
		@nError INT,
		@fMoreToDelete INT

	SET @nIsNoCountOn = @@OPTIONS & 512
	IF @nIsNoCountOn = 0
		SET NOCOUNT ON

	SET @nGonnerId = NULL
	SET @nError = 0
	SET @fMoreToDelete = 0

	--== Delete all evaluations with null targets. ==--
	SELECT TOP 1 @nGonnerId = ae.[Id]
	FROM CmAgentEvaluation ae
	JOIN CmObject objae
		ON objae.[Id] = ae.[Id]
	WHERE ae.Target IS NULL
	ORDER BY ae.[Id]

	IF @@ROWCOUNT != 0 BEGIN
		EXEC @nError = DeleteObj$ @nGonnerId
		SET @fMoreToDelete = 1
		GOTO Finish
	END

	--== Delete stale evaluations on analyses ==--
	SELECT TOP 1 @nGonnerId = ae.[Id]
	FROM CmAgentEvaluation ae
	JOIN CmObject objae
		ON objae.[Id] = ae.[Id] AND objae.Owner$ = @nAgentId
	JOIN CmObject objanalysis
		ON objanalysis.[Id] = ae.Target
		AND objanalysis.Class$ = 5059 -- WfiAnalysis objects
		AND objanalysis.Owner$ = @nWfiWordFormID
	WHERE ae.DateCreated < @dtEval
	ORDER BY ae.[Id]

	IF @@ROWCOUNT != 0 BEGIN
		EXEC @nError = DeleteObj$ @nGonnerId
		SET @fMoreToDelete = 1
		GOTO Finish
	END

	--== Make sure all analyses have human evaluations, if they, or glosses they own, are referred to by a WIC annotation. ==--
	DECLARE @adID INT, @analId INT, @humanAgentId INT, @rowcount INT, @rowcount2 INT, @evalId INT

	-- Get the ID of the CmAnnotationDefn that is the WIC type.
	SELECT @adID=Id
	FROM CmObject
	WHERE Guid$='eb92e50f-ba96-4d1d-b632-057b5c274132'

	-- Get Id of the first 'default user' human agent
	SELECT TOP 1 @humanAgentId = a.Id
	FROM CmAgent a
	JOIN CmAgent_Name nme
		ON a.Id = nme.Obj
	WHERE a.Human = 1 AND nme.Txt = 'default user'

	SELECT TOP 1 @analId = wa.[Id]
	FROM WfiAnalysis_ wa
	left outer JOIN WfiGloss_ gloss
		ON gloss.Owner$ = wa.Id
	JOIN CmAnnotation ann
		ON ann.InstanceOf = wa.[Id] OR ann.[InstanceOf] = gloss.[Id]
	JOIN CmObject ad
		ON ann.AnnotationType = ad.Id AND ad.Id = @adID
	WHERE wa.[Owner$] = @nWfiWordFormID
	ORDER BY wa.[Id]

	WHILE @@ROWCOUNT != 0 BEGIN
		SELECT @evalId=Id
		FROM cmAgentEvaluation_ cae
		WHERE Target = @analId AND Owner$ = @humanAgentId

		IF @@ROWCOUNT = 0
		BEGIN
			EXEC @nError = SetAgentEval
				@humanAgentId,
				@analId,
				1,
				'Set by RemoveUnusedAnalyses$',
				@dtEval
			SET @fMoreToDelete = 1
			GOTO Finish
		END

		SELECT TOP 1 @analId = wa.[Id]
		FROM WfiAnalysis_ wa
		left outer JOIN WfiGloss_ gloss
			ON gloss.Owner$ = wa.Id
		JOIN CmAnnotation ann
			ON ann.InstanceOf = wa.[Id] OR ann.[InstanceOf] = gloss.[Id]
		JOIN CmObject ad
			ON ann.AnnotationType = ad.Id AND ad.Id = @adID
		WHERE wa.[Id] > @analId AND wa.[Owner$] = @nWfiWordFormID
		ORDER BY wa.[Id]
	END

	--== Delete orphan analyses, which have no evaluations ==--
	SELECT TOP 1 @nGonnerId = analysis.[Id]
	FROM CmObject analysis
	LEFT OUTER JOIN cmAgentEvaluation cae
		ON cae.Target = analysis.[Id]
	WHERE cae.Target IS NULL
		AND analysis.OwnFlid$ = 5062002		-- 5062002
		AND analysis.Owner$ = @nWfiWordFormID
	ORDER BY analysis.[Id]

	WHILE @@ROWCOUNT != 0 BEGIN
		EXEC @nError = DeleteObj$ @nGonnerId
		SET @fMoreToDelete = 1
		GOTO Finish
	END

Finish:
	IF @nIsNocountOn = 0 SET NOCOUNT OFF
	SELECT @fMoreToDelete AS MoreToDelete
	RETURN @nError
GO

if object_id('WasParsingDataModified') is not null begin
	print 'removing proc WasParsingDataModified'
	drop proc WasParsingDataModified
end
print 'creating proc WasParsingDataModified'
go
CREATE PROC [dbo].[WasParsingDataModified]
			@stampCompare TIMESTAMP
AS
	SELECT TOP 1 Id
	FROM CmObject co
	WHERE co.UpdStmp > @stampCompare
		AND (co.Class$ BETWEEN 5026 AND 5045
			OR co.Class$ IN
			(4, -- FsComplexFeature 4
			49, -- FsFeatureSystem 49
			50, -- FsClosedFeature 50
			51, -- FsClosedValue 51
			53, -- FsComplexValue 53
			57, -- FsFeatureStructure 57
			59, -- FsFeatureStructureType 59
			65, -- FsSymbolicFeatureValue 65
			5001, -- MoStemMsa 5001
			5002, -- LexEntry 5002
			5005, -- LexicalDatabase 5005
			5049, -- PartOfSpeech 5049
			5092, -- PhPhoneme 5092
			5095, -- PhNCSegments 5095
			5097, -- PhEnvironment 5097
			5098, -- PhCode 5098
			5099, -- PhPhonologicalData 5099
			5101, -- MoAllomorphAdhocCoProhibition 5101
			5102, -- MoMorphemeAdhocCoProhibition 5102
			5110, -- MoAdhocCoProhibitionGroup 5110
			5117 -- MoUnclassifiedAffixMsa 5117
			))
GO

if object_id('WasParsingGrammarDataModified') is not null begin
	print 'removing proc WasParsingGrammarDataModified'
	drop proc WasParsingGrammarDataModified
end
print 'creating proc WasParsingGrammarDataModified'
go
CREATE PROC [dbo].[WasParsingGrammarDataModified]
			@stampCompare TIMESTAMP
AS
	SELECT TOP 1 Id
	FROM CmObject co
	WHERE co.UpdStmp > @stampCompare
		AND (co.Class$ IN
			(4, -- FsComplexFeature 4
			49, -- FsFeatureSystem 49
			50, -- FsClosedFeature 50
			51, -- FsClosedValue 51
			53, -- FsComplexValue 53
			57, -- FsFeatureStructure 57
			59, -- FsFeatureStructureType 59
			65, -- FsSymbolicFeatureValue 65
			5001, -- MoStemMsa 5001
			5026, -- MoAdhocCoProhibition 5026
			5027, -- MoAffixAllomorph 5027 (Actually only want MoAffixForm, but it doesn't work)
			--5028, -- MoAffixForm 5028
			5030, -- MoCompoundRule 5030
			5031, -- MoDerivationalAffixMsa 5031
			5033, -- MoEndocentricCompound 5033
			5034, -- MoExocentricCompound 5034
			5036, -- MoInflAffixSlot 5036
			5037, -- MoInflAffixTemplate 5037
			5038, -- MoInflectionalAffixMsa 5038
			5039, -- MoInflectionClass 5039
			5040, -- MoMorphologicalData 5040
			5041, -- MoMorphoSyntaxAnalysis 5041
			5042, -- MoMorphType 5042
			5049, -- PartOfSpeech 5049
			5092, -- PhPhoneme 5092
			5095, -- PhNCSegments 5095
			5097, -- PhEnvironment 5097
			5098, -- PhCode 5098
			5099, -- PhPhonologicalData 5099
			5101, -- MoAllomorphAdhocCoProhibition 5101
			5102, -- MoMorphemeAdhocCoProhibition 5102
			5110, -- MoAdhocCoProhibitionGroup 5110
			5117 -- MoUnclassifiedAffixMsa 5117
			))
GO

if object_id('WasParsingLexiconDataModified') is not null begin
	print 'removing proc WasParsingLexiconDataModified'
	drop proc WasParsingLexiconDataModified
end
print 'creating proc WasParsingLexiconDataModified'
go
CREATE PROC [dbo].[WasParsingLexiconDataModified]
			@stampCompare TIMESTAMP
AS
	SELECT TOP 1 Id
	FROM CmObject co
	WHERE co.UpdStmp > @stampCompare
		AND (co.Class$ IN
			(
			51, -- FsClosedValue 51
			53, -- FsComplexValue 53
			57, -- FsFeatureStructure 57
			59, -- FsFeatureStructureType 59
			5001, -- MoStemMsa 5001
			5002, -- LexEntry 5002
			5005, -- LexicalDatabase 5005
			5027, -- MoAffixAllomorph 5027
			5028, -- MoAffixForm 5028
			5031, -- MoDerivationalAffixMsa 5031
			5035, -- MoForm 5035
			5038, -- MoInflectionalAffixMsa 5038
			5045, -- MoStemAllomorph 5045
			5117 -- MoUnclassifiedAffixMsa 5117
			))
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200165
begin
	UPDATE Version$ SET DbVer = 200166
	COMMIT TRANSACTION
	print 'database updated to version 200166'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200165 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
