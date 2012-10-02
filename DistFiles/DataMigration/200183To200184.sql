-- update database FROM version 200183 to 200184
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- DN-641 "Inability to delete sort methods"
-------------------------------------------------------------------------------

IF OBJECT_ID('TR_CmSortSpec_Del') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_CmSortSpec_Del'
	DROP TRIGGER [TR_CmSortSpec_Del]
END
GO
PRINT 'creating trigger TR_CmSortSpec_Del'
GO
CREATE TRIGGER [TR_CmSortSpec_Del] ON CmSortSpec FOR DELETE
AS

	DECLARE
		@nvcPrimary NVARCHAR(4000),
		@nvcSecondary NVARCHAR(4000),
		@nvcTertiary NVARCHAR(4000),
		@nID INT,
		@nFlid INT

	DECLARE curCmSortSpecDel CURSOR LOCAL STATIC FORWARD_ONLY READ_ONLY FOR
		SELECT [PrimaryField], [SecondaryField], [TertiaryField] FROM Deleted

	OPEN curCmSortSpecDel
	FETCH curCmSortSpecDel INTO @nvcPrimary, @nvcSecondary, @nvcTertiary
	WHILE @@FETCH_STATUS = 0 BEGIN

		-- this needs to work
		SELECT TOP 1 [PrimaryWs] FROM [CmSortSpec] WHERE [PrimaryWs] = @nFlid
		UNION
		SELECT TOP 1 [SecondaryWs] FROM [CmSortSpec] WHERE [SecondaryWs] = @nFlid
		UNION
		SELECT TOP 1 [TertiaryWs] FROM [CmSortSpec] WHERE [TertiaryWs] = @nFlid

		IF @@ROWCOUNT = 0
		SET @nID = dbo.fnGetLastCommaDelimID$(@nvcPrimary)
		EXEC DeleteFlidCollations NULL, NULL, @nID

		FETCH curCmSortSpecDel INTO @nvcPrimary, @nvcSecondary, @nvcTertiary
	END
	CLOSE curCmSortSpecDel
	DEALLOCATE curCmSortSpecDel

GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200183
BEGIN
	UPDATE [Version$] SET [DbVer] = 200184
	COMMIT TRANSACTION
	PRINT 'database updated to version 200184'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200183 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
