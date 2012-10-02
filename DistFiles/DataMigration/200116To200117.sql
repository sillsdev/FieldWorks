-- update database FROM version 200116 to 200117

BEGIN TRANSACTION  --( will be rolled back if wrong version#
-- Update two stored procedures which wrongly used "ID" instead of "Id" in one
-- of the few contexts where it makes a difference in SQL.

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
	DECLARE @tIds TABLE (ID INT, ClassName NVARCHAR(100), Level TINYINT)

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
		SELECT f.ID, c.Name, 0
		FROM OPENXML(@nXmlIdsParam, '/root/Obj') WITH (Id INT) f
		JOIN CmObject o ON o.ID = f.ID
		JOIN Class$ c ON c.ID = o.Class$
	END
	--( If we're working with an XML string:
	ELSE IF SUBSTRING(@ntIds, 1, 1) = '<' BEGIN
		EXECUTE sp_xml_preparedocument @hXmlIds OUTPUT, @ntIds

		INSERT INTO @tIds
		SELECT f.ID, f.ClassName, 0
		FROM dbo.fnGetIdsFromString(@ntIds, @hXmlIds) AS f

		EXECUTE sp_xml_removedocument @hXmlIds
	END
	--( If we're working with a comma delimited list
	ELSE
		INSERT INTO @tIds
		SELECT f.ID, f.ClassName, 0
		FROM dbo.fnGetIdsFromString(@ntIds, NULL) AS f

	--( Now find owned objects

	SET @nLevel = 1

	INSERT INTO @tIds
	SELECT o.ID, c.Name, @nLevel
	FROM @tIds t
	JOIN CmObject o ON o.Owner$ = t.Id
	JOIN Class$ c ON c.ID = o.Class$

	SET @nRowCount = @@ROWCOUNT
	WHILE @nRowCount != 0 BEGIN
		SET @nLevel = @nLevel + 1

		INSERT INTO @tIds
		SELECT o.ID, c.Name, @nLevel
		FROM @tIds t
		JOIN CmObject o ON o.Owner$ = t.Id
		JOIN Class$ c ON c.ID = o.Class$
		WHERE t.Level = @nLevel - 1

		SET @nRowCount = @@ROWCOUNT
	END
	SET @nLevel = @nLevel - 1

	--==( Delete objects )==--

	--( We're going to start out at the leaves and work
	--( toward the trunk.

	WHILE @nLevel >= 0	BEGIN

		SELECT TOP 1 @nObjId = t.ID, @nvcClassName = t.ClassName
		FROM @tIds t
		WHERE t.Level = @nLevel
		ORDER BY t.Id

		SET @nRowCount = @@ROWCOUNT
		WHILE @nRowCount = 1 BEGIN
			SET @nvcSql = N'DELETE ' + @nvcClassName + N' WHERE Id = @nObjectID'
			EXEC sp_executesql @nvcSql, N'@nObjectID INT', @nObjectId = @nObjId
			SET @nError = @@ERROR
			IF @nError != 0
				GOTO Fail

			SELECT TOP 1 @nObjId = t.ID, @nvcClassName = t.ClassName
			FROM @tIds t
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

IF OBJECT_ID('fnGetIdsFromString') IS NOT NULL BEGIN
	PRINT 'removing function fnGetIdsFromString'
	DROP FUNCTION fnGetIdsFromString
END
GO
PRINT 'creating function fnGetIdsFromString'
GO

CREATE FUNCTION fnGetIdsFromString (@ntIds NTEXT, @hXmlIds INT)
RETURNS @tabIds TABLE (ID INT, ClassName NVARCHAR(100))
AS
BEGIN
	DECLARE
		@nId INT,
		@nEnd INT,
		@nStart INT

	--( IDs from comma delimited string

	IF SUBSTRING(@ntIds, 1, 1) != '<' BEGIN

		SET @nStart = 1
		SET @nEnd = CHARINDEX(',', @ntIds, @nStart)

		WHILE @nEnd > 1 BEGIN
			SET @nId = SUBSTRING(@ntIds, @nStart, @nEnd - @nStart)

			INSERT INTO @tabIds
			SELECT @nId, c.Name
			FROM CmObject o
			JOIN Class$ c ON c.ID = o.Class$
			WHERE o.ID = @nId

			SET @nStart = @nEnd + 1
			SET @nEnd = CHARINDEX(',', @ntIds, @nStart)
		END

		--( last one.
		SET @nId = SUBSTRING(@ntIds, @nStart, DATALENGTH(@ntIds) - @nStart)

		INSERT INTO @tabIds
		SELECT @nId, c.Name
		FROM CmObject o
		JOIN Class$ c ON c.ID = o.Class$
		WHERE o.ID = @nId
	END

	--( Load from an XML string
	ELSE BEGIN
		--( In certain conditions, a function cannot call
		--( sp_xml_preparedocument. You must set it up first
		--( in the calling program:
		--(
		--( EXECUTE sp_xml_preparedocument @hXmlIds OUTPUT, @ntIds

		--Note: (JohnT): The identifier in the With clauses of the OPENXML is CASE SENSITIVE!!
		INSERT INTO @tabIds
		SELECT i.ID, c.Name
		FROM OPENXML(@hXmlIds, '/root/Obj') WITH (Id INT) i
		JOIN CmObject o ON o.ID = i.ID
		JOIN Class$ c ON c.ID = o.CLASS$

		--( In certain conditions, a function cannot call
		--( sp_xml_removedocument. You must set it up first in
		--( the calling program:
		--(
		--( EXECUTE sp_xml_removedocument @hXmlIds
	END
	RETURN

END
GO
-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200116
begin
	UPDATE Version$ SET DbVer = 200117
	COMMIT TRANSACTION
	print 'database updated to version 200117'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200116 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
