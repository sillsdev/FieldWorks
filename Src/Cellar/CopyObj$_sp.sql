/***********************************************************************************************
 *	CopyObj$
 *
 *	Description:
 *		Creates a copy of an object.
 *
 *	Parameters:
 *		@nTopSourceObjId INT,     -- the top owning object
 *		@nTopDestOwnerId INT,     -- The ID of the owner of the top object we're creating here
 *		@nTopDestOwnerFlid INT,   -- The owning field ID of the top object we're creating here
 *		@hvoDstStart INT = NULL,  -- The ID of the object before which the object will be
 *                                -- inserted. This must be NULL for fields that are not owning
 *                                -- sequences. If NULL for owning sequences, the object will
 *                                -- be appended to the list.
 *		@nTopDestObjId INT OUTPUT -- the ID for the new object we're creating here
 *
 *	TODO (SteveMiller):
 *		This version of CopyObj$ quadrupled the speed of its immediate predecessor. However,
 *		some more speed may be gained. One potential gain is putting a primary key and/or
 *		index on the temp table #SourceObjs.
 **********************************************************************************************/

IF OBJECT_ID('CopyObj$') IS NOT NULL BEGIN
	PRINT 'removing procedure CopyObj$'
	DROP PROC [CopyObj$]
END
GO
PRINT 'creating procedure CopyObj$'
GO
CREATE PROCEDURE [CopyObj$]
	@nTopSourceObjId INT,
	@nTopDestOwnerId INT,
	@nTopDestOwnerFlid INT,
	@hvoDstStart INT = NULL,
	@nTopDestObjId INT OUTPUT
AS

	DECLARE
		@nDstStartOrd INT,
		@nMinOrd INT,
		@nSpaceAvail INT,
		@nSpaceNeed INT,
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
		@nvcQuery NVARCHAR(4000),
		@nvcFieldsList NVARCHAR(4000),
		@nvcValuesList NVARCHAR(4000),
		@nColumn INT,
		@sysColumn SYSNAME,
		@nFlid INT,
		@nType INT,
		@nTopDestType INT,
		@nSourceClassId INT,
		@nDestClassId INT,
		@nvcFieldName NVARCHAR(100),
		@nvcTableName NVARCHAR(100),
		@StrId NVARCHAR(20);

	-- Remark these constants for production code. For coding and testing with Query Analyzer,
	-- unremark these constants and put an @ in front of the variables wherever they appear.
	/*
	declare @kcptOwningSequence int
	set @kcptOwningSequence = 27
	*/

	SET @nFirstOuterLoop = 1
	SET @nOwnerId = @nTopDestOwnerId
	SET @nOwnerFieldId = @nTopDestOwnerFlid

	SELECT @nTopDestType = Type FROM Field$ WHERE Id = @nTopDestOwnerFlid

	IF (@nTopDestType != 27 AND @hvoDstStart IS NOT NULL) BEGIN
		RAISERROR('hvoDstStart must be NULL for objects that are not in an owning sequence: Object ID=%d, Dest Owner ID=%d, Dest Owner Flid=%d, Dest Start Obj=%d', 16, 1,
			@nTopSourceObjId, @nTopDestOwnerId,	@nTopDestOwnerFlid, @hvoDstStart)
		GOTO LFail
	END
	IF (@nTopDestType = 27) BEGIN

		IF @hvoDstStart is null BEGIN
			SELECT	@nDstStartOrd = coalesce(max([OwnOrd$]), -1) + 1
			FROM	CmObject
			WHERE	[Owner$] = @nTopDestOwnerId
				and [OwnFlid$] = @nTopDestOwnerFlid
		END
		ELSE BEGIN
			SELECT	@nDstStartOrd = [OwnOrd$]
			FROM	CmObject
			WHERE	[Owner$] = @nTopDestOwnerId
				and [OwnFlid$] = @nTopDestOwnerFlid
				and [Id] = @hvoDstStart

			IF @nDstStartOrd is null BEGIN
				RAISERROR('If hvoDstStart is not NULL, it must be an existing object in the destination sequence: Object ID=%d, Dest Owner ID=%d, Dest Owner Flid=%d, Dest Start Obj=%d', 16, 1,
					@nTopSourceObjId, @nTopDestOwnerId,	@nTopDestOwnerFlid, @hvoDstStart)
				GOTO LFail
			END

			-- If the objects are not appended to the end of the destination list then determine if there is enough room

			-- Find the object with the largest ordinal value less than the destination start object's ordinal
			SELECT @nMinOrd = coalesce(max([OwnOrd$]), -1)
			FROM	CmObject
			WHERE	[Owner$] = @nTopDestOwnerId
				and [OwnFlid$] = @nTopDestOwnerFlid
				and [OwnOrd$] < @nDstStartOrd

			SET @nSpaceAvail = @nDstStartOrd - @nMinOrd - 1
			SET @nSpaceNeed = 1 -- Using a variable rather than hard-coding the 1 to make it more self-documenting and to prepare the way for future enhancement to copy multiple objects

			-- see if there is currently enough room for the objects under the destination object's sequence list;
			--	if there is not then make room
			IF @nSpaceAvail < @nSpaceNeed BEGIN
				UPDATE	CmObject
				SET	[OwnOrd$] = [OwnOrd$] + @nSpaceNeed - @nSpaceAvail
				WHERE	[Owner$] = @nTopDestOwnerId
					and [OwnFlid$] = @nTopDestOwnerFlid
					and [OwnOrd$] >= @nDstStartOrd
			END

			-- We can now stick the (first -- only one for now) copied object anwhere in the gap bounded
			-- (inclusively) at the bottom by (@nMinOrd + 1) and at the top by
			-- (@nMinOrd + 1 + MAX(0, (@nSpaceAvail - @nSpaceNeed))). We'll stick it at the lowest position
			-- because a) the calculation is much easier and b) it's probably slightly more likely that a
			-- subsequent copy will want to insert something else after this thing we're inserting
			SET @nDstStartOrd = @nMinOrd + 1
		END
	END

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

	--== Get the Object Tree ==--

	SET @StrId = CAST(@nTopSourceObjId AS NVARCHAR(20));

	INSERT INTO #SourceObjs
		SELECT oo.*, c.[Name], NULL
		FROM dbo.fnGetOwnedObjects$(@StrId, NULL, 1, 0, 1, null, 1) oo
		JOIN Class$ c ON c.[Id] = oo.[ObjClass]
		WHERE oo.[ObjClass] <> 0 -- Have to block CmObject rows

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
				SELECT @guidNew, [Class$], @nOwnerId, @nOwnerFieldId, @nDstStartOrd
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

		--( Tables of the former MultiTxt$ table are now set in a separate loop.

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

LFail:

GO
