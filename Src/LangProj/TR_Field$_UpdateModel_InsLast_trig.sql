/***********************************************************************************************
 * Trigger: TR_Field$_UpdateModel_InsLast
 *
 * Description:
 *	TR_Field$_UpdateModel_Ins takes care of most of the work of making sure the database
 *	is OK when adding a new field to a table. Unfortunately, it didn't rebuild the associated
 *	view in most cases, using UpdateClassView$. If we put did put the following code into
 *	that trigger, UpdateClassView$ would be called twice for each field when creating a new
 *	database, once by the trigger, and once by LangProjSP.sql. LangProjSP.sql makes sure the
 *	views are created with the proper class hierarchy, whereas UpdateClassView$ does not.
 *	This trigger was written to take care of that problem. In addition, the MakeObj_*
 *	stored procedure was not being regenerated, and this trigger takes tare of that, too.
 *	Finally, some of the delete object functionality was moved here.
 *
 *	The trigger is intended to fire after all other insert triggers on the Field$ table.
 *
 * Type: 	Insert
 * Table:	Field$
 *
 * Notes:
 *	To make sure this trigger fires last, execute the following after the trigger is created:
 *
 *		EXEC sp_settriggerorder 'TR_Field$_UpdateModel_InsLast', 'last', 'INSERT'
 **********************************************************************************************/

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
