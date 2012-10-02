/***********************************************************************************************
 * GenerateCustomName
 *
 * Description:
 *	Generates a unique custom field name.
 *
 * Parameters:
 *	none
 *
 * Returns:
 *	0 if successful, 1 if an error occurs.
 *
 * Example:
 *	DECLARE @nvcName NVARCHAR(100)
 *	EXEC GenerateCustomName @nvcName OUTPUT
 *
 * Notes:
 *	See in code for problems around non-Roman field names
 **********************************************************************************************/
IF OBJECT_ID('GenerateCustomName') IS NOT NULL BEGIN
	PRINT 'removing proc GenerateCustomName'
	DROP PROCEDURE GenerateCustomName
END
GO
PRINT 'creating proc GenerateCustomName'
GO

CREATE PROCEDURE GenerateCustomName

	--( There was some discussion of making the custom field name as
	--( close as possibile to the label for the custom field. The
	--( problem came in non-Roman scripts. What would happen if the
	--( user chose a script that Microsoft doesn't have in its
	--( collation tables? This proc would fail. For now, any value
	--( that gets passed in is overwritten.

	@nvcName NVARCHAR(100) OUTPUT
AS BEGIN
	DECLARE
		@nCount INT,
		@nRowcount INT,
		@nWhatever INT

	SET @nvcName = 'custom'
	SET @nCount = 0

	--( The first custom field doesn't have any number appended to it
	--( in Data Notebook, so we'll follow that example here.
	SELECT @nWhatever = [Id] FROM Field$ WHERE [Name] = @nvcName

	SET @nRowcount = @@ROWCOUNT
	WHILE @nRowcount != 0 BEGIN
		SET @nCount = @nCount + 1

		SELECT @nWhatever = [Id]
		FROM Field$
		WHERE [Name] = @nvcName + CAST(@nCount AS VARCHAR(3))

		SET @nRowcount = @@ROWCOUNT
	END

	IF @nCount = 0
		SET @nvcName = @nvcName
	ELSE
		SET @nvcName = @nvcName + CONVERT(VARCHAR(3), @nCount)
END
GO
