-- Update database from version 200227 to 200228
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- Update DeleteObjects, which should have been put in the last migration
-- for FDB-207 :-(
-------------------------------------------------------------------------------

IF OBJECT_ID('DeleteObjects') IS NOT NULL BEGIN
	PRINT 'removing procedure DeleteObjects'
	DROP PROC DeleteObjects
END
GO
PRINT 'creating procedure DeleteObjects'
GO

CREATE PROCEDURE DeleteObjects
	@ntIds NTEXT = NULL,
	@nXmlIdsParam INT = NULL
AS
	DECLARE @tIds TABLE (ID INT, Level TINYINT)

	DECLARE
		@hXmlIds INT,
		@nRowCount INT,
		@nObjId INT,
		@nLevel INT,
		@nvcClassName NVARCHAR(100),
		@nvcSql NVARCHAR(1000),
		@nError INT

	SET @nError = 0

	IF (@ntIds IS NULL AND @nXmlIdsParam IS NULL) OR (@ntIds IS NOT NULL AND @nXmlIdsParam IS NOT NULL)
		GOTO Fail

	--==( Load Ids )==--

	--( If we're working with an XML doc:
	--Note: (JohnT): The identifier in the With clauses of the OPENXML is CASE SENSITIVE!!
	IF @nXmlIdsParam IS NOT NULL BEGIN
		INSERT INTO @tIds
		SELECT f.ID, 0
		FROM OPENXML(@nXmlIdsParam, '/root/Obj') WITH (Id INT) f
	END
	--( If we're working with an XML string:
	ELSE IF SUBSTRING(@ntIds, 1, 1) = '<' BEGIN
		EXECUTE sp_xml_preparedocument @hXmlIds OUTPUT, @ntIds

		INSERT INTO @tIds
		SELECT f.ID, 0
		FROM dbo.fnGetIdsFromString(@ntIds, @hXmlIds) AS f

		EXECUTE sp_xml_removedocument @hXmlIds
	END
	--( If we're working with a comma delimited list
	ELSE
		INSERT INTO @tIds
		SELECT f.ID, 0
		FROM dbo.fnGetIdsFromString(@ntIds, NULL) AS f

	--( Now find owned objects

	SET @nLevel = 1

	INSERT INTO @tIds
	SELECT o.ID, @nLevel
	FROM @tIds t
	JOIN CmObject o ON o.Owner$ = t.Id

	SET @nRowCount = @@ROWCOUNT
	WHILE @nRowCount != 0 BEGIN
		SET @nLevel = @nLevel + 1

		INSERT INTO @tIds
		SELECT o.ID, @nLevel
		FROM @tIds t
		JOIN CmObject o ON o.Owner$ = t.Id
		WHERE t.Level = @nLevel - 1

		SET @nRowCount = @@ROWCOUNT
	END
	SET @nLevel = @nLevel - 1

	--==( Delete objects )==--

	--( We're going to start out at the leaves and work
	--( toward the trunk.

	WHILE @nLevel >= 0	BEGIN

		SELECT TOP 1 @nObjId = t.ID, @nvcClassName = c.Name
		FROM @tIds t
		JOIN CmObject o ON o.Id = t.Id
		JOIN Class$ c ON c.ID = o.Class$
		WHERE t.Level = @nLevel
		ORDER BY t.Id

		SET @nRowCount = @@ROWCOUNT
		WHILE @nRowCount = 1 BEGIN
			SET @nvcSql = N'DELETE ' + @nvcClassName + N' WHERE Id = @nObjectID'
			EXEC sp_executesql @nvcSql, N'@nObjectID INT', @nObjectId = @nObjId
			SET @nError = @@ERROR
			IF @nError != 0
				GOTO Fail

			SELECT TOP 1 @nObjId = t.ID, @nvcClassName = c.Name
			FROM @tIds t
			JOIN CmObject o ON o.Id = t.Id
			JOIN Class$ c ON c.ID = o.Class$
			WHERE t.Id > @nobjId AND t.Level = @nLevel
			ORDER BY t.ID

			SET @nRowCount = @@ROWCOUNT
		END

		SET @nLevel = @nLevel - 1
	END

	RETURN 0

Fail:
	RETURN @nError
GO

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200227
BEGIN
	UPDATE Version$ SET DbVer = 200228
	COMMIT TRANSACTION
	PRINT 'database updated to version 200228'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200227 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
