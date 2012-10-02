-- update database from version 200008 to 200009
BEGIN TRANSACTION

--==( LexSubentry_MainEntriesOrSenses )==--

--( The data starts out in a view, based on CmObject,
--( and gets moved to a table.

--( Save off data from view

DECLARE @tblLexSubentry_MainEntriesOrSenses TABLE (Src INT, Dst INT, Ord INT)

INSERT INTO @tblLexSubentry_MainEntriesOrSenses
SELECT * FROM LexSubentry_MainEntriesOrSenses

--( Change Field Types

DELETE FROM Field$ WHERE Id = 5007001

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (5007001, 28, 5007, 0, 'MainEntriesOrSenses', 0, NULL, NULL)

--( Copy data to new location

INSERT INTO LexSubentry_MainEntriesOrSenses (Src, Dst, Ord)
SELECT Src, Dst, Ord FROM @tblLexSubentry_MainEntriesOrSenses

GO

--==( LexMinorEntry MainEntryOrSense )==--

--( Temporarily store moved data

CREATE TABLE #MinorEntryOrSense (Id INT, EntryOrSense INT)

INSERT INTO #MinorEntryOrSense
SELECT me.[Id], COALESCE(reos.Entry, reos.Sense)
FROM LexRefEntryOrSense reos
JOIN CmObject o ON o.[Id] = reos.[Id]
JOIN LexMinorEntry me ON me.[Id] = o.Owner$

--( Remove the data from old location

DELETE LexRefEntryOrSense
DELETE FROM CmObject WHERE Class$ = 5003 --( LexRefEntryOrSense

--( Change Field Types

DELETE FROM Field$ WHERE Id = 5009001

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
	VALUES (5009001, 24, 5009, 0, 'MainEntryOrSense', 0, NULL, NULL)
GO

--( Move LexMinorEntry.MainEntryOrSense

UPDATE LexMinorEntry
SET MainEntryOrSense = EntryOrSense
FROM #MinorEntryOrSense
WHERE #MinorEntryOrSense.[Id] = LexMinorEntry.[Id]

GO

DROP TABLE #MinorEntryOrSense

--( Remove LexRefEntryOrSense

DELETE FROM Field$ WHERE Class = 5003
DELETE FROM ClassPar$ WHERE Src = 5003
DELETE FROM Class$ WHERE Id = 5003

DROP VIEW LexRefEntryOrSense_
DROP TABLE LexRefEntryOrSense

GO

/***********************************************************************************************
	Stored procedures related to LexEntry
 **********************************************************************************************/

IF OBJECT_ID('ChangeLexSubToLexMajor') IS NOT NULL BEGIN
	if (select DbVer from Version$) = 200008
		PRINT 'removing procedure ChangeLexSubToLexMajor'
	DROP PROCEDURE ChangeLexSubToLexMajor
END
GO
if (select DbVer from Version$) = 200008
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

		--( Remove owned all Entries or Senses

		--TODO (SteveMiller): unremark when model changes.
		--DELETE FROM LexSubentry_MainEntriesOrSenses WHERE Src = @nObjId

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
	if (select DbVer from Version$) = 200008
		PRINT 'removing procedure ChangeLexMajorToLexSub'
	DROP PROCEDURE ChangeLexMajorToLexSub
END
GO
if (select DbVer from Version$) = 200008
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
 *		@nSubEntryType = Refer to model for description. Optional.
 *		@fIsBodyWithHeadword = Refer to model for description. Optional.
 *		@nWritingSystem = WS for Subentry Literal Meaning. Optional.
 *		@nvcText = Text for Subentry Literal Meaning. Optional
 *		@nvbFormat = Format for Subentry Literal Meaning Optional
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
	SELECT @nClassId = Class$
	FROM CmObject (READUNCOMMITTED)
	WHERE [Id] = @nObjId

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

-------------------------------------------------------------------------------
IF OBJECT_ID('ChangeLexMajorToLexMinor') IS NOT NULL BEGIN
	if (select DbVer from Version$) = 200008
		PRINT 'removing procedure ChangeLexMajorToLexMinor'
	DROP PROCEDURE ChangeLexMajorToLexMinor
END
GO
if (select DbVer from Version$) = 200008
	PRINT 'creating procedure ChangeLexMajorToLexMinor'
GO

/*****************************************************************************
 *	Procedure: ChangeLexMajorToLexMinor
 *
 *	Description:
 *		Changes a LexMajorEntry to a LexMinorEntry
 *
 *	Parameters:
 *      @nObjId = Id (hvo) of the object to be changed.
 *
 *	Returns:
 *		0 for success, otherwise an error code
 *****************************************************************************/

CREATE PROCEDURE ChangeLexMajorToLexMinor
	@nObjId INT
AS
	DECLARE
		@nReturn INT,
		@nClassId INT

	SET @nReturn = 0

	--( Double check class
	SELECT @nClassId = Class$
	FROM CmObject (READUNCOMMITTED)
	WHERE [Id] = @nObjId

	IF @nClassId != 5008 BEGIN --( LexMajorEntry
		RAISERROR('The object is not a LexMajorEntry', 16, 1)
		SET @nReturn = -1
	END

	ELSE BEGIN

		--( Remove the old Major Entry
		DELETE FROM LexMajorEntry WHERE [Id] = @nObjId

		--( Move LexMajorEntry.SummaryDefiniton to LexMinorEntry.Comment
		UPDATE MultiStr$
		SET Flid = 5009003
		WHERE Obj = @nObjId AND Flid = 5008003

		--( Change the class of the object
		UPDATE CmObject SET Class$ = 5009 WHERE [Id] = @nObjId

		INSERT INTO LexMinorEntry ([Id]) VALUES (@nObjId)

		-- REVIEW (SteveMiller): At some point we might want to add
		-- functionality to add Features and a Condition.

	END
	RETURN @nReturn
GO

-------------------------------------------------------------------------------
IF OBJECT_ID('ChangeLexMinorToLexMajor') IS NOT NULL BEGIN
	if (select DbVer from Version$) = 200008
		PRINT 'removing procedure ChangeLexMinorToLexMajor'
	DROP PROCEDURE ChangeLexMinorToLexMajor
END
GO
if (select DbVer from Version$) = 200008
	PRINT 'creating procedure ChangeLexMinorToLexMajor'
GO

/*****************************************************************************
 *	Procedure: ChangeLexMinorToLexMajor
 *
 *	Description:
 *		Changes a LexMinorEntry to a LexMajorEntry
 *
 *	Parameters:
 *      @nObjId = Id (hvo) of the object to be changed.
 *
 *	Returns:
 *		0 for success, otherwise an error code
 *****************************************************************************/

CREATE PROCEDURE ChangeLexMinorToLexMajor
	@nObjId INT
AS
	DECLARE
		@nReturn INT,
		@nClassId INT,
		@nId INT

	SET @nReturn = 0

	--( Double check class
	SELECT @nClassId = Class$
	FROM CmObject (READUNCOMMITTED)
	WHERE [Id] = @nObjId

	IF @nClassId != 5009 BEGIN --( LexMinorEntry
		RAISERROR('The object is not a LexMinorEntry', 16, 1)
		SET @nReturn = -1
	END

	ELSE BEGIN

		--( Remove owned atomic LexMinorEntry.Features
		SELECT @nId = Dst
		FROM LexMinorEntry_Features (READUNCOMMITTED)
		WHERE Src = @nid

		IF @@ROWCOUNT = 1 --( should be 0 or 1
			EXEC @nReturn = DeleteObj$ @nId

		--( reference atomic LexMinorEntry Condition is a column in
		--( LexMinorEntry. No need to do anything more here.

		--( Remove the Minor Entry
		DELETE FROM LexMinorEntry WHERE [Id] = @nId

		--( Move LexMinorEntry.Comment to LexMajorEntry.SummaryDefiniton
		UPDATE MultiStr$
		SET Flid = 5008003
		WHERE Obj = @nObjId AND Flid = 5009003

		--( Change the class of the object
		UPDATE CmObject SET Class$ = 5008 WHERE [Id] = @nObjId

		--( Add the new Major Entry
		INSERT INTO LexMajorEntry ([Id]) VALUES (@nObjId)

	END
	RETURN @nReturn
GO

-------------------------------------------------------------------------------
IF OBJECT_ID('ChangeLexMinorToLexSub') IS NOT NULL BEGIN
	if (select DbVer from Version$) = 200008
		PRINT 'removing procedure ChangeLexMinorToLexSub'
	DROP PROCEDURE ChangeLexMinorToLexSub
END
GO
if (select DbVer from Version$) = 200008
	PRINT 'creating procedure ChangeLexMinorToLexSub'
GO

/*****************************************************************************
 *	Procedure: ChangeLexMinorToLexSub
 *
 *	Description:
 *		Changes a LexMinorEntry to a LexSubEntry
 *
 *	Parameters:
 *      @nObjId = Id (hvo) of the object to be changed.
 *		@nSubEntryType = Refer to model for description. Optional.
 *		@fIsBodyWithHeadword = Refer to model for description. Optional.
 *		@nWritingSystem = WS for Subentry Literal Meaning. Optional.
 *		@nvcText = Text for Subentry Literal Meaning. Optional
 *		@nvbFormat = Format for Subentry Literal Meaning Optional
 *
 *	Returns:
 *		0 for success, otherwise an error code
 *****************************************************************************/

CREATE PROCEDURE ChangeLexMinorToLexSub
	@nObjId INT,
	@nSubEntryType INT = NULL,
	@fIsBodyWithHeadword BIT = 0,
	@nWritingSystem INT = NULL,
	@nvcText NVARCHAR(4000) = NULL,
	@nvbFormat VARBINARY(8000) = NULL
AS
	DECLARE	@nReturn INT
	SET @nReturn = 0

	EXEC @nReturn = ChangeLexMinorToLexMajor @nObjId
	IF @nReturn = 0
		EXEC @nReturn = ChangeLexMajorToLexSub
			@nObjId,
			@nSubEntryType,
			@fIsBodyWithHeadword,
			@nWritingSystem,
			@nvcText,
			@nvbFormat

	RETURN @nReturn
GO

-------------------------------------------------------------------------------
IF OBJECT_ID('ChangeLexSubToLexMinor') IS NOT NULL BEGIN
	if (select DbVer from Version$) = 200008
		PRINT 'removing procedure ChangeLexSubToLexMinor'
	DROP PROCEDURE ChangeLexSubToLexMinor
END
GO
if (select DbVer from Version$) = 200008
	PRINT 'creating procedure ChangeLexSubToLexMinor'
GO

/*****************************************************************************
 *	Procedure: ChangeLexSubToLexMinor
 *
 *	Description:
 *		Changes a LexSubEntry to a LexMinorEntry
 *
 *	Parameters:
 *      @nObjId = Id (hvo) of the object to be changed.
 *
 *	Returns:
 *		0 for success, otherwise an error code
 *****************************************************************************/

CREATE PROCEDURE ChangeLexSubToLexMinor
	@nObjId INT
AS
	DECLARE	@nReturn INT
	SET @nReturn = 0

	EXEC @nReturn = ChangeLexSubToLexMajor @nObjId
	IF @nReturn = 0
		EXEC @nReturn = ChangeLexMajorToLexMinor @nObjId

	RETURN @nReturn
GO


declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200008
begin
	update Version$ set DbVer = 200009
	COMMIT TRANSACTION
	print 'database updated to version 200009'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200008 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
