-- update database from version 200010 to 200011
BEGIN TRANSACTION

IF OBJECT_ID('GenerateCustomName') IS NOT NULL BEGIN
	if (select DbVer from Version$) = 200010
		PRINT 'removing procedure GenerateCustomName'
	DROP PROCEDURE GenerateCustomName
END
GO
if (select DbVer from Version$) = 200010
	PRINT 'creating procedure GenerateCustomName'
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


declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200010
begin
	update Version$ set DbVer = 200011
	COMMIT TRANSACTION
	print 'database updated to version 200011'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200010 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
