-- Update database from version 200218 to 200219
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FDB-199: Improve Performance of fnGetIdsFromString()
-------------------------------------------------------------------------------

SET NOCOUNT ON;

IF OBJECT_ID('dbo.Numbers') IS NOT NULL
	DROP TABLE Numbers;

--( All we are interested in here is creating a whole lot of rows, so we
--( can get a sequential list of 11,000 numbers (numbers 1 to 11,000).

--( Much to my surprise, we don't need a CREATE TABLE statement first.
--( SQL Server is smart enough to create a table based on the SELECT command.

SELECT TOP 11000 IDENTITY(INT,1,1) AS N
INTO Numbers
FROM Master..SysColumns sc1, Master..SysColumns sc2;

ALTER TABLE Numbers
	ADD CONSTRAINT PK_Numbers_N PRIMARY KEY CLUSTERED (N) WITH FILLFACTOR = 100;
GO

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
RETURNS @tabIds TABLE (ID INT, ClassName NVARCHAR(100))
AS
BEGIN
	DECLARE @nId INT

	--( IDs from comma delimited string
	IF SUBSTRING(@Ids, 1, 1) != '<' BEGIN
		SET @Ids = N',' + @Ids + N',';

		INSERT INTO @tabIds
		SELECT SUBSTRING(@Ids, n.N + 1, CHARINDEX(',', @Ids, n.N + 1) - n.N - 1),
			c.Name
		FROM Numbers n
		JOIN CmObject o ON o.Id	=
			CAST(SUBSTRING(@Ids, n.N + 1, CHARINDEX(',', @Ids, n.N + 1) - n.N - 1) AS INT)
		JOIN Class$ c ON c.Id = o.Class$
		WHERE n.N < LEN(@Ids)
			AND SUBSTRING(@Ids, n.N, 1) = ',';  --Notice how we find the comma

		--( The following code works fine. It just is slower than using the above
		--( technique. I leave it here in case we need to return to it for the
		--( Firebird port.

		/*
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

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200218
BEGIN
	UPDATE Version$ SET DbVer = 200219
	COMMIT TRANSACTION
	PRINT 'database updated to version 200219'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200218 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
