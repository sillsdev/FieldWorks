-- Update database from version 200226 to 200227
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FDB-207: Performance enhancement to fnGetIdsFromString
-------------------------------------------------------------------------------

IF OBJECT_ID('fnGetIdsFromString') IS NOT NULL BEGIN
	PRINT 'removing function fnGetIdsFromString'
	DROP FUNCTION fnGetIdsFromString
END
GO
PRINT 'creating function fnGetIdsFromString'
GO

CREATE FUNCTION fnGetIdsFromString (
	@Ids NVARCHAR(MAX),
	@hXmlIds INT)
RETURNS @tabIds TABLE (ID INT)
AS
BEGIN
	--( IDs from comma delimited string
	IF SUBSTRING(@Ids, 1, 1) != N'<' BEGIN

		--( This works only if a comma is at the beginning and end of the string.
		IF SUBSTRING(@Ids, 1, 1) != N','
			SET @Ids = N',' + @Ids;
		IF SUBSTRING(@Ids, LEN(@Ids), 1) != N','
			SET @Ids = @Ids + N',';

		INSERT INTO @tabIds
		SELECT SUBSTRING(@Ids, n.N + 1, CHARINDEX(',', @Ids, n.N + 1) - n.N - 1)
		FROM Numbers n
		WHERE n.N < LEN(@Ids)
			AND SUBSTRING(@Ids, n.N, 1) = ',';  --Notice how we find the comma

		--( The following code works fine. It just is slower than using the above
		--( technique. I leave it here in case we need to return to it for the
		--( Firebird port. (It also still has the CmObject and Class$ tables
		--( joined in, which I didn't bother to take out with the performance
		--( enhancement.)

		/*
		DECLARE @nId INT
		WHILE DATALENGTH(@Ids) > 0 BEGIN

			--( The CHARINDEX gets the comma location. The comma location minus 1
			--( gets the length of the ID. If there is no comma, CHARINDEX returs
			--( 0. 0 minus 1 results in a -1, and the NULLIF turns the result into
			--( NULL. The ISNULL sees the NULL and returns the length of the ID.
			--( This is a convoluted way of approaching life, but possibly the
			--( fastest way to approach the string with SQL.

			SET @nId = CONVERT(INT, SUBSTRING(@Ids, 1,
				ISNULL(NULLIF(CHARINDEX(',', @Ids) - 1, -1), DATALENGTH(@Ids))))

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

			SET @Ids = SUBSTRING(@Ids,
				ISNULL(NULLIF(CHARINDEX(',', @ntIds), 0),
				DATALENGTH(@Ids)) + 1, DATALENGTH(@Ids))
		END
		*/
	END --( IF SUBSTRING(@Ids, 1, 1) != '<'

	--( Load from an XML string
	ELSE BEGIN
		--( In certain conditions, a function cannot call
		--( sp_xml_preparedocument. You must set it up first
		--( in the calling program:
		--(
		--( EXECUTE sp_xml_preparedocument @hXmlIds OUTPUT, @Ids

		--Note: (JohnT): The identifier in the With clauses of the OPENXML is CASE SENSITIVE!!
		INSERT INTO @tabIds
			SELECT i.ID
			FROM OPENXML(@hXmlIds, '/root/Obj') WITH (Id INT) i

		--( In certain conditions, a function cannot call
		--( sp_xml_removedocument. You must set it up first in
		--( the calling program:
		--(
		--( EXECUTE sp_xml_removedocument @hXmlIds
	END

	RETURN
END
GO

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200226
BEGIN
	UPDATE Version$ SET DbVer = 200227
	COMMIT TRANSACTION
	PRINT 'database updated to version 200227'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200226 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
