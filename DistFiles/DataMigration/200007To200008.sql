-- update database from version 200007 to 200008
BEGIN TRANSACTION

IF OBJECT_ID('ChangeLexSubToLexMajor') IS NOT NULL BEGIN
	if (select DbVer from Version$) = 200007
		PRINT 'removing procedure ChangeLexSubToLexMajor'
	DROP PROCEDURE ChangeLexSubToLexMajor
END
GO
if (select DbVer from Version$) = 200007
	PRINT 'creating procedure ChangeLexSubToLexMajor'
GO

/*****************************************************************************
 *	Procedure: ChangeLexSubToLexMajor
 *
 *	Description:
 *		Changes a LexSubEntry to a LexMajorEntry
 *
 *	Parameters:
 *      @nObjId = Id (hvo) of the object to be changed.
 *
 *	Returns:
 *		0 for success, otherwise an error code
 *****************************************************************************/

CREATE PROCEDURE ChangeLexSubToLexMajor
	@nObjId INT
AS
	DECLARE
		@nReturn INT,
		@nClassId INT,
		@nSubId INT

	SET @nReturn = 0

	--( Double check class
	SELECT @nClassId = Class$ FROM CmObject WHERE [Id] = @nObjId
	IF @nClassId != 5007 BEGIN --( LexSubEntry
		RAISERROR('The object is not a LexSubEntry', 16, 1)
		SET @nReturn = -1
	END

	ELSE BEGIN

		--( Remove owned all LexRefEntryOrSense objects
		SELECT TOP 1 @nSubId = Dst
		FROM LexSubentry_MainEntriesOrSenses
		WHERE Src = @nObjId
		ORDER BY Dst

		WHILE @@ROWCOUNT = 1 BEGIN
			EXEC @nReturn = DeleteObj$ @nSubId

			SELECT TOP 1 @nSubId = Dst
			FROM LexSubentry_MainEntriesOrSenses
			WHERE Src = @nObjId AND Dst > @nSubId
			ORDER BY Dst
		END

		--( Remove a piece of the subclass record
		DELETE LexSubentry_LiteralMeaning WHERE Obj = @nObjId

		--( Remove the subclass record
		DELETE LexSubentry WHERE [Id] = @nObjId

		--( Change the class of the object
		UPDATE CmObject SET Class$ = 5008 WHERE [Id] = @nObjId

	END
	RETURN @nReturn
GO

-------------------------------------------------------------------------------
IF OBJECT_ID('ChangeLexMajorToLexSub') IS NOT NULL BEGIN
	if (select DbVer from Version$) = 200007
		PRINT 'removing procedure ChangeLexMajorToLexSub'
	DROP PROCEDURE ChangeLexMajorToLexSub
END
GO
if (select DbVer from Version$) = 200007
	PRINT 'creating procedure ChangeLexMajorToLexSub'
GO

/*****************************************************************************
 *	Procedure: ChangeLexMajorToLexSub
 *
 *	Description:
 *		Changes a LexMajorEntry to a LexSubEntry
 *
 *	Parameters:
 *      @nObjId = Id (hvo) of the object to be changed.
 *		@nSubEntryType = Refer to model for description. Defaults to null
 *		@fIsBodyWithHeadword = Refer to model for description. Defaults to 0.
 *
 *	Returns:
 *		0 for success, otherwise an error code
 *****************************************************************************/

CREATE PROCEDURE ChangeLexMajorToLexSub
	@nObjId INT,
	@nSubEntryType INT = NULL,
	@fIsBodyWithHeadword BIT = 0,
	@nWritingSystem INT = NULL,
	@nvcText NVARCHAR(4000) = NULL,
	@nvbFormat VARBINARY(8000) = NULL
AS
	DECLARE
		@nReturn INT,
		@nClassId INT,
		@nSubId INT

	SET @nReturn = 0

	--( Double check class
	SELECT @nClassId = Class$ FROM CmObject WHERE [Id] = @nObjId
	IF @nClassId != 5008 BEGIN --( LexMajorEntry
		RAISERROR('The object is not a LexMajorEntry', 16, 1)
		SET @nReturn = -1
	END

	ELSE BEGIN

		--( Add the LexSubentry piece.
		INSERT INTO LexSubentry (
			[Id], SubentryType, IsBodyWithHeadword)
		VALUES (
			@nObjId, @nSubEntryType, @fIsBodyWithHeadword)

		--( Change the class of the object
		UPDATE CmObject SET Class$ = 5007 WHERE [Id] = @nObjId

		--( Add a Literal meaning
		IF @nWritingSystem IS NOT NULL
			AND @nvcText IS NOT NULL
			AND @nvbFormat IS NOT NULL BEGIN

			INSERT INTO MultiStr$(
				Flid, Obj, Ws, Txt, Fmt)
			VALUES (
				5007003, @nObjId, @nWritingSystem, @nvcText, @nvbFormat)

		END

	END
	RETURN @nReturn
GO


declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200007
begin
	update Version$ set DbVer = 200008
	COMMIT TRANSACTION
	print 'database updated to version 200008'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200007 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
