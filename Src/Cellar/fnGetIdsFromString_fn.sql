/*************************************************************************
 * Function fnGetIdsFromString
 *
 * Description:
 *	Loads a table variable with object IDs from an NTEXT parameter
 *	which has IDs in a comma delimited list.
 *
 * Parameters:
 *	@Ids	= a comma delimited list. Can be a singe ID.
 *
 * Returns:
 *	@tabIds	= table variable of IDs.
 *
 * Notes:
 * To get a comma delimited list of 999 IDs for testing, execute the
 * following:
 *
		DECLARE
			@x INT,
			@Ids NVARCHAR(MAX)
		SET @x = 1
		SET	@Ids = N''
		WHILE @x < 1000 BEGIN
			SET @Ids = @Ids + CONVERT(NVARCHAR(5), @x) + N',';
			SET @x = @x + 1
		END
		PRINT @Ids

 *
 * --------------------------------------------------------------------
 * Calling Sample (Comma Delimited):
 *
 *	SET @Ids = N'1';
 *
 *	INSERT INTO @tIds
 *	SELECT f."ID"
 *	FROM dbo.fnGetIdsFromString(@Ids, NULL) AS f
 *
 * Last Modified:
 *	10 September 2008
 ************************************************************************/

IF OBJECT_ID('fnGetIdsFromString') IS NOT NULL BEGIN
	PRINT 'removing function fnGetIdsFromString'
	DROP FUNCTION fnGetIdsFromString
END
GO
PRINT 'creating function fnGetIdsFromString'
GO

CREATE FUNCTION fnGetIdsFromString (
	@Ids NVARCHAR(MAX))
RETURNS @tabIds TABLE (ID INT)
AS
BEGIN
	--( This function works only if a comma is at the beginning and end
	--( of the string.
	IF SUBSTRING(@Ids, 1, 1) != N','
		SET @Ids = N',' + @Ids;
	IF SUBSTRING(@Ids, LEN(@Ids), 1) != N','
		SET @Ids = @Ids + N',';

	INSERT INTO @tabIds
	SELECT SUBSTRING(@Ids, n.N + 1, CHARINDEX(',', @Ids, n.N + 1) - n.N - 1)
	FROM Numbers n
	WHERE n.N < LEN(@Ids)
		AND SUBSTRING(@Ids, n.N, 1) = ',';  --Notice how we find the commas

	RETURN
END
GO
