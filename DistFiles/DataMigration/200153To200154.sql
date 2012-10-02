-- update database FROM version 200153 to 200154
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- Comment after final GO was crashing trigger. Refactored the code while I
-- was here.
-------------------------------------------------------------------------------

IF object_id('TR_StText_Owner_UpdDel') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_StText_Owner_UpdDel'
	DROP TRIGGER [TR_StText_Owner_UpdDel]
END
GO
PRINT 'creating trigger TR_StText_Owner_UpdDel'
GO

CREATE TRIGGER [TR_StText_Owner_UpdDel] on [StText] FOR UPDATE, DELETE
AS
	DECLARE @iErr INT, @fIsNocountOn INT

	SET @fIsNocountOn = @@options & 512
	IF @fIsNocountOn = 0 SET NOCOUNT ON

	UPDATE owner
		SET [UpdDttm] = getdate()
		FROM deleted del
		JOIN [CmObject] AS owned ON owned.[Id] = del.[Id]
		JOIN [CmObject] AS owner ON owner.[Id] = owned.Owner$

	SET @iErr = @@error
	IF @fIsNocountOn = 0 SET NOCOUNT OFF
	IF @iErr != 0 BEGIN
		Raiserror ('TR_StText_Owner_UpdDel: SQL Error %d; Unable to insert rows into the StText.',
			16, 1, @iErr)
		ROLLBACK TRANSACTION
	END
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200153
BEGIN
	UPDATE [Version$] SET [DbVer] = 200154
	COMMIT TRANSACTION
	PRINT 'database updated to version 200154'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200153 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
