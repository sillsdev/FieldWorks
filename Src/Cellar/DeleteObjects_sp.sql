/*************************************************************************
 * DeleteObjects
 *
 * Description:
 *	Deletes objects and their owned objects. It also removes references
 *	to the object(s).
 *
 * Parameters:
 *	@ntIds	= A Unicode string that has a comma delimited list of IDs.
 *
 * Notes:
 *	Before an object can be deleted, references to it must be deleted.
 *	But before that, the objects that this object owns must be deleted.
 *
 *	This procedure depends on delete triggers that are created by
 *	CreateDeleteObj.
 *************************************************************************/

IF OBJECT_ID('DeleteObjects') IS NOT NULL BEGIN
	PRINT 'removing procedure DeleteObjects'
	DROP PROC DeleteObjects
END
GO
PRINT 'creating procedure DeleteObjects'
GO

CREATE PROCEDURE DeleteObjects
	@ntIds NTEXT = NULL
AS
	DECLARE @tIds TABLE (ID INT, Level TINYINT)

	DECLARE
		@nRowCount INT,
		@nObjId INT,
		@nLevel INT,
		@nvcClassName NVARCHAR(100),
		@nvcSql NVARCHAR(1000),
		@nError INT

	SET @nError = 0

	--==( Load Ids )==--

	INSERT INTO @tIds
	SELECT f.ID, 0
	FROM dbo.fnGetIdsFromString(@ntIds) AS f

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
