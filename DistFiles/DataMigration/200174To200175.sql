-- update database FROM version 200174 to 200175
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

--( Completely rewrote the process for parsing comma delimited strings

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

	DECLARE @nId INT

	--( IDs from comma delimited string
	IF SUBSTRING(@ntIds, 1, 1) != '<' BEGIN
		WHILE DATALENGTH(@ntIds) > 0 BEGIN

			--( The CHARINDEX gets the comma location. The comma location minus 1
			--( gets the length of the ID. If there is no comma, CHARINDEX returs
			--( 0. 0 minus 1 results in a -1, and the NULLIF turns the result into
			--( NULL. The ISNULL sees the NULL and returns the length of the ID.
			--( This is a convoluted way of approaching life, but possibly the
			--( fastest way to approach the string with SQL.

			SET @nId = CONVERT(INT, SUBSTRING(@ntIds, 1,
				ISNULL(NULLIF(CHARINDEX(',', @ntIds) - 1, -1), DATALENGTH(@ntIds))))

			INSERT INTO @tabIds
				SELECT @nId, c.Name
				FROM CmObject o
				JOIN Class$ c ON c.ID = o.Class$
				WHERE o.ID = @nId

			--( If no comma, CHARINDEX returns a 0. The NULLIF turns the 0 into a
			--( NULL. The ISNULL turns the NULL into the length of the string plus 1.
			--( This number is fed into the SUBSTRING as the start point for the
			--( SUBSTRING, but since it is longer than the actual length, returns
			--( nothing. That is precisely what we want if there is no comma.

			SET @ntIds = SUBSTRING(@ntIds,
				ISNULL(NULLIF(CHARINDEX(',', @ntIds), 0),
				DATALENGTH(@ntIds)) + 1, DATALENGTH(@ntIds))

		END
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
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200174
BEGIN
	UPDATE [Version$] SET [DbVer] = 200175
	COMMIT TRANSACTION
	PRINT 'database updated to version 200175'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200174 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
