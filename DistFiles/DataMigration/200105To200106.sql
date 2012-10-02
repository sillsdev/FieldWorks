-- update database FROM version 200105 to 200106

BEGIN TRANSACTION  --( will be rolled back if wrong version#

if exists (select * from sysobjects where id = object_id(N'[ScrSriptureNote]') and OBJECTPROPERTY(id, N'IsUserTable') = 1) BEGIN
	-------------------------------------------------------------------------------
	-- Rename the ScrSriptureNote class to ScrScriptureNote (accidentally misnamed
	-- in migration from Version 101 to 102)
	-------------------------------------------------------------------------------

	delete [Field$] where [Id] = 3018001
	delete [Field$] where [Id] = 3018002
	delete [Field$] where [Id] = 3018003
	delete [Field$] where [Id] = 3018004
	delete [Field$] where [Id] = 3018005
	delete [Field$] where [Id] = 3018006
	delete [Field$] where [Id] = 3018007
	delete [Field$] where [Id] = 3017002

	drop table [ScrSriptureNote]

	delete from ClassPar$ where Src = 3018

	delete [Class$] where [Id] = 3018

	insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
		values(3018, 3, 37, 0, 'ScrScriptureNote')

	-------------------------------------------------------------------------------
	-- Re-add fields and relations to ScrBookAnnotations and the renamed
	-- ScrScriptureNote
	-------------------------------------------------------------------------------

	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(3017002, 27, 3017, 3018, 'Notes',0,Null, null, null, null)

	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(3018001, 2, 3018, null, 'ResolutionStatus',0,Null, null, null, null)
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(3018002, 28, 3018, 7, 'Categories',0,Null, null, null, null)
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(3018003, 23, 3018, 68, 'Quote',0,Null, null, null, null)
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(3018004, 23, 3018, 68, 'Discussion',0,Null, null, null, null)
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(3018005, 23, 3018, 68, 'Recommendation',0,Null, null, null, null)
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(3018006, 23, 3018, 68, 'Resolution',0,Null, null, null, null)
	insert into [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		values(3018007, 27, 3018, 68, 'Responses',0,Null, null, null, null)

	exec UpdateClassView$ 3018, 1
END
GO

-------------------------------------------------------------------------------
-- The rest of the migration only gets done if this DB already has Scripture!
-- Create 66 ScrBookAnnotations and move Scripture annotations from the
-- language project to the appropriate ScrBookAnnotation
-------------------------------------------------------------------------------
IF EXISTS(SELECT * FROM Scripture)
BEGIN
	-- Create ScrBookAnnotations
	DECLARE @owner int, @iBook int
	select @owner = (select id from Scripture)
	select @iBook = count(*) from ScrBookAnnotations -- typically this should be 0
	-- Set parameter values
	while (@iBook < 66)
	BEGIN
		EXEC CreateOwnedObject$
			@clid = 3017,
			@id = null,
			@guid = null,
			@owner = @owner,
			@ownFlid = 3001024,
			@type = 27
		SET @iBook = @iBook + 1
	END

	-- Convert existing CmBaseAnnotations into ScrScriptureNotes
	DECLARE @id int, @beginRef int, @hvoText int, @bookNumber int, @prevBookNumber int,
		@ord int, @responseOrd int, @dateCreated datetime, @dateModified datetime,
		@hvoResponse int

	SET @ord = 1
	SET @prevBookNumber = -1

	DECLARE ScrAnnotationsCur CURSOR FOR
	SELECT [id], BeginRef, t.Dst, DateCreated, DateModified FROM CmBaseAnnotation_
	LEFT OUTER JOIN CmAnnotation_Text t on t.Src = [id]
	where	Class$=37
	AND	BeginRef > 0
	ORDER BY BeginRef, DateCreated

	OPEN ScrAnnotationsCur

	FETCH NEXT FROM ScrAnnotationsCur
	INTO @id, @beginRef, @hvoText, @dateCreated, @dateModified

	WHILE @@FETCH_STATUS = 0
	BEGIN
		SET @bookNumber = @beginRef/1000000
		IF (@bookNumber <> @prevBookNumber)
			SELECT	@owner = (SELECT [id] FROM CmObject where Class$ = 3017 and ownord$ = @bookNumber),
				@prevBookNumber = @bookNumber

		-- Convert the CmAnnotation into a ScrScriptureNote
		UPDATE CmObject
		SET	Class$=3018,
			Owner$=@owner,
			OwnFlid$=3017002,
			OwnOrd$=@ord
		WHERE	[id] = @id
		SET @ord = @ord + 1
		INSERT INTO ScrScriptureNote([id])
		VALUES(@id)

		-- Change the existing Text (an StText) into an StJournalText and make it the Discussion
		IF @hvoText IS NOT NULL
		BEGIN
			UPDATE CmObject
			SET	Class$=68,
				OwnFlid$=3018004
			WHERE	[id] = @hvoText
			INSERT INTO StJournalText([id], DateCreated, DateModified)
			VALUES (@hvoText, @dateCreated, @dateModified)
		END
		ELSE
		BEGIN
			EXEC CreateOwnedObject$
				@clid = 68,
				@id = null,
				@guid = null,
				@owner = @id,
				@ownFlid = 3018004,
				@type = 23
		END

		-- Create the Quote, Recommendation and Resolution texts
		EXEC CreateOwnedObject$
			@clid = 68,
			@id = null,
			@guid = null,
			@owner = @id,
			@ownFlid = 3018003,
			@type = 23
		EXEC CreateOwnedObject$
			@clid = 68,
			@id = null,
			@guid = null,
			@owner = @id,
			@ownFlid = 3018005,
			@type = 23
		EXEC CreateOwnedObject$
			@clid = 68,
			@id = null,
			@guid = null,
			@owner = @id,
			@ownFlid = 3018006,
			@type = 23

		-- Deal with reponses (if text is set)
		SET @responseOrd = 1
		DECLARE ResponsesCur CURSOR FOR
		select [id], t.Dst, DateCreated, DateModified from CmIndirectAnnotation_
		JOIN CmAnnotation_Text t on t.Src = [id]
		JOIN CmIndirectAnnotation_AppliesTo a on a.Src = [id]
		WHERE a.Dst = @id
		ORDER BY DateCreated

		OPEN ResponsesCur

		FETCH NEXT FROM ResponsesCur
		INTO @hvoResponse, @hvoText, @dateCreated, @dateModified

		WHILE @@FETCH_STATUS = 0
		BEGIN
			-- Change the existing Text (an StText) into an StJournalText and append it to the Responses
			UPDATE CmObject
			SET	Class$=68,
				Owner$=@id,
				OwnFlid$=3018007,
				OwnOrd$ = @responseOrd
			WHERE	[id] = @hvoText
			INSERT INTO StJournalText([id], DateCreated, DateModified)
			VALUES (@hvoText, @dateCreated, @dateModified)
			SET @responseOrd = @responseOrd + 1

			-- Delete the CmIndirectAnnotation
			EXEC DeleteObj$ @objId = @hvoResponse

			FETCH NEXT FROM ResponsesCur
			INTO @hvoResponse, @hvoText, @dateCreated, @dateModified
		END

		CLOSE ResponsesCur
		DEALLOCATE ResponsesCur

		FETCH NEXT FROM ScrAnnotationsCur
		INTO @id, @beginRef, @hvoText, @dateCreated, @dateModified
	END

	CLOSE ScrAnnotationsCur
	DEALLOCATE ScrAnnotationsCur

	-- Finally, clean up any orphaned indirect annotations
	DECLARE OrphanCmIndirectCur CURSOR FOR
	select [id] from CmIndirectAnnotation_
	WHERE NOT EXISTS (SELECT * FROM CmIndirectAnnotation_AppliesTo WHERE Src=[id])

	OPEN OrphanCmIndirectCur

	FETCH NEXT FROM OrphanCmIndirectCur
	INTO @id

	WHILE @@FETCH_STATUS = 0
	BEGIN
		-- Delete the CmIndirectAnnotation
		EXEC DeleteObj$ @objId = @id

		FETCH NEXT FROM OrphanCmIndirectCur
		INTO @id
	END

	CLOSE OrphanCmIndirectCur
	DEALLOCATE OrphanCmIndirectCur

END
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200105
begin
	UPDATE Version$ SET DbVer = 200106
	COMMIT TRANSACTION
	print 'database updated to version 200106'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200105 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
