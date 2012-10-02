/***********************************************************************************************
 * Trigger: TR_Class$_InsLast
 *
 * Description:
 *	TR_Class$_Ins takes care of most of the work of making sure the database is OK when adding
 *	a new class to a table. Unfortunately, it didn't rebuild the associated MakeObj_*
 *	stored procedure, using GenMakeObjProc. If we put did put the following code into
 *	that trigger, GenMakeObjProc would be called twice for each class when creating a new
 *	database, once by the trigger, and once by LangProjSP.sql. This procedure is created after
 *	the views created in LangProjSP.sql. It is intended to fire after all other insert triggers
 *	on the Field$ table.
 *
 * Type: 	Insert
 * Table:	Class$
 *
 * Notes:
 *	To make sure this trigger fires last, execute the following after the trigger is created:
 *
 *		EXEC sp_settriggerorder 'TR_Class$_InsLast', 'last', 'INSERT'
 **********************************************************************************************/

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
