-- update database FROM version 200178 to 200179
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- The Field$ insert last trigger was generating a new delete trigger for the
-- class it was a part of. It was not generating a new delete trigger for any
-- destination class.
-------------------------------------------------------------------------------

-------------------------------------------------------------------------------
-- Fix the Field$ insert trigger
-------------------------------------------------------------------------------

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
	--( gets Class$.Abstract for updating the CreateObject_*
	--( stored procedure.

	INSERT INTO @tblSubclasses
	SELECT @nClassId, c.Abstract, @nLoopLevel
	FROM Class$ c
	WHERE c.Id = @nClassId

	--( Rebuild the delete trigger

	EXEC @nErr = CreateDeleteObj @nClassId
	IF @nErr <> 0 GOTO LFail

	--( Rebuild CreateObject_*

	SELECT @nAbstract = Abstract FROM @tblSubClasses
	IF @nAbstract != 1 BEGIN
		EXEC @nErr = DefineCreateProc$ @nClassId
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

-------------------------------------------------------------------------------
-- Make sure the proper stored procedures have been generated for each
-- class. This is probably overkill, but makes sure all is well.
-------------------------------------------------------------------------------

DECLARE
	@nErr INT,
	@nClassid INT,
	@nAbstract BIT

DECLARE curClasses CURSOR FOR SELECT Id, Abstract FROM Class$ ORDER BY Id
OPEN curClasses
FETCH NEXT FROM curClasses INTO @nClassId, @nAbstract
WHILE @@FETCH_STATUS = 0 BEGIN

	--( Build the CreateObject_ stored procedure
	IF @nAbstract = 0 BEGIN
		EXEC @nErr = DefineCreateProc$ @nClassId
		--IF @nErr <> 0 GOTO LFail
	END

	--( Build the delete trigger
	EXEC @nErr = CreateDeleteObj @nClassId
	--IF @nErr <> 0 GOTO LFail

	FETCH NEXT FROM curClasses INTO @nClassId, @nAbstract
END

--( Rebuild the stored function fnGetRefsToObj
EXEC @nErr = CreateGetRefsToObj
--IF @nErr <> 0 GOTO LFail

CLOSE curClasses
DEALLOCATE curClasses

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200178
BEGIN
	UPDATE [Version$] SET [DbVer] = 200179
	COMMIT TRANSACTION
	PRINT 'database updated to version 200179'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200178 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
