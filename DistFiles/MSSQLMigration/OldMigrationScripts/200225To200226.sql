-- Update database from version 200225 to 200226
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FWM-152 and FWM-153: Add CmAnnotationDefn.MaxDupOccur, ScrDraft.Protected.
-- TE-6991: Fix crash with CheckingError
-------------------------------------------------------------------------------

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(35014, 2, 35,
		null, 'MaxDupOccur',0,Null, null, null, null)
go

-------------------------------------------------------------------------------

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3010005, 1, 3010,
		null, 'Protected',0,Null, null, null, null)
go

-------------------------------------------------------------------------------

IF OBJECT_ID('CopyObj$') IS NOT NULL BEGIN
	PRINT 'removing procedure CopyObj$'
	DROP PROC [CopyObj$]
END
GO
PRINT 'creating procedure CopyObj$'
GO
CREATE PROCEDURE [CopyObj$]
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

	-- JohnT: In the case of a sequence we insert the copy at the end, which means giving it an ord
	-- one larger than any existing item in the sequence.
	-- all other objects will be given the same ownord$ as in the original.
	DECLARE @nTopDestOwnOrd INT
	SELECT @nTopDestOwnOrd = max(ownOrd$) from CmObject where [Owner$] = @nTopDestOwnerId and [OwnFlid$] = @nTopDestOwnerFlid and [OwnOrd$] is not null
	IF @nTopDestOwnOrd is not null
		SET @nTopDestOwnOrd = @nTopDestOwnOrd + 1

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

	-- First (root) object gets OwnOrd$ computed above, others copy from destination.
		IF @nFirstOuterLoop = 1
			INSERT INTO CmObject WITH (ROWLOCK) ([Guid$], [Class$], [Owner$], [OwnFlid$], [OwnOrd$])
				SELECT @guidNew, [Class$], @nOwnerId, @nOwnerFieldId, @nTopDestOwnOrd
				FROM CmObject
				WHERE [Id] = @nSourceObjId
		ELSE
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

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200225
BEGIN
	UPDATE Version$ SET DbVer = 200226
	COMMIT TRANSACTION
	PRINT 'database updated to version 200226'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200225 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
