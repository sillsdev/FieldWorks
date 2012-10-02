/*************************************************************************
 * Function: fnGetOwnedIds
 *
 * Description:
 *	Returns the IDs of a list, not caring about hierarchy or order. This
 *	allows us to optimize performance.
 *
 * Parameters:
 *	@nOwner = The owinging ID, such as a Possibility List
 * 	@nTopFlid = The owning flid (field ID) of the first level of owned
 *				objects, such as 8008 (Possibility). If null, 8008 is
 *				used.
 *	@nSubFlid = The flid of secondary flid (field ID) of subsequent
 *				levels, such as 7004 (Subpossibility). If null, 7004
 *				is used.
 *
 * Returns:
 *	Table containing the Ids:
 *		[Id]		INT,
 *		[Level]		INT
 *	Level is a utility field for the function, and not necessary to the
 *	results of the function. It can be dropped in a calling query.
 *
 * Example:
 *	SELECT [Id] FROM dbo.fnGetOwnedIds (2, 8008, 7004)
 ************************************************************************/

IF OBJECT_ID('fnGetOwnedIds') IS NOT NULL BEGIN
	PRINT 'removing function fnGetOwnedIds'
	DROP FUNCTION fnGetOwnedIds
END
GO
PRINT 'creating function fnGetOwnedIds'
GO

CREATE FUNCTION fnGetOwnedIds (
	@nOwner INT,
	@nTopFlid INT,
	@nSubFlid INT)
RETURNS @tblObjects TABLE (
	[Id] INT,
	Guid$ UNIQUEIDENTIFIER,
	Class$ INT ,
	Owner$ INT,
	OwnFlid$ INT,
	OwnOrd$ INT,
	UpdStmp BINARY(8),
	UpdDttm SMALLDATETIME,
	[Level] INT)
AS
BEGIN
	DECLARE
		@nLevel INT,
		@nRowCount INT

	IF @nTopFlid IS NULL
		SET @nTopFlid = 8008 --( Possibility
	IF @nSubFlid IS NULL
		SET @nSubFlid = 7004 --( Subpossibility

	--( Get the first level of owned objects
	SET @nLevel = 1

	INSERT INTO @tblObjects
	SELECT
		[Id],
		Guid$,
		Class$,
		Owner$,
		OwnFlid$,
		OwnOrd$,
		UpdStmp,
		UpdDttm,
		@nLevel
	FROM CmObject
	WHERE Owner$ = @nOwner AND OwnFlid$ = @nTopFlid --( e.g. possibility, 8008

	SET @nRowCount = @@ROWCOUNT --( Using @@ROWCOUNT alone was flakey in the loop.

	--( Get the sublevels of owned objects
	WHILE @nRowCount != 0 BEGIN

		INSERT INTO @tblObjects
		SELECT
			o.[Id],
			o.Guid$,
			o.Class$,
			o.Owner$,
			o.OwnFlid$,
			o.OwnOrd$,
			o.UpdStmp,
			o.UpdDttm,
			(@nLevel + 1)
		FROM @tblObjects obj
		JOIN CmObject o ON o.Owner$ = obj.[Id]
			AND  o.OwnFlid$ = @nSubFlid --( e.g. subpossibility, 7004
		WHERE obj.[Level] = @nLevel

		SET @nRowCount = @@ROWCOUNT
		SET @nLevel = @nLevel + 1
	END

	RETURN
END
GO
