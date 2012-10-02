-- Update database from version 200215 to 200216
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FWC-33: Shorten WfiWordForm_Form.Txt and MoForm_Form.Txt to 300 bytes
--         Modify the collation sequence
-------------------------------------------------------------------------------


	DECLARE @Sql NVARCHAR(300);

	IF EXISTS(SELECT * FROM sys.indexes WHERE NAME = 'IND_MoForm_Form_Txt') BEGIN
		SET @Sql = N'DROP INDEX MoForm_Form.IND_MoForm_Form_Txt;'
		EXEC sp_executesql @Sql
	END

	SET @Sql = N'ALTER TABLE MoForm_Form ALTER COLUMN Txt NVARCHAR(300) COLLATE Latin1_General_BIN;'
	EXEC sp_executesql @Sql

	SET @Sql = N'CREATE NONCLUSTERED INDEX IND_MoForm_Form_Txt ON MoForm_Form ' +
			N'(Txt ASC) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ' +
			N'SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ' +
			N'ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY];'
	EXEC sp_executesql @Sql

	IF EXISTS(SELECT * FROM sys.indexes WHERE NAME = 'IND_WfiWordform_Form_Txt') BEGIN
		SET @Sql = N'DROP INDEX WfiWordform_Form.IND_WfiWordform_Form_Txt;'
		EXEC sp_executesql @Sql
	END

	SET @Sql = N'ALTER TABLE WfiWordform_Form ALTER COLUMN Txt NVARCHAR(300) COLLATE Latin1_General_BIN;'
	EXEC sp_executesql @Sql

	SET @Sql = N'CREATE NONCLUSTERED INDEX IND_WfiWordform_Form_Txt ON WfiWordform_Form ' +
			N'(Txt ASC) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, ' +
			N'SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ' +
			N'ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY];'

	EXEC sp_executesql @Sql



---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200215
BEGIN
	UPDATE Version$ SET DbVer = 200216
	COMMIT TRANSACTION
	PRINT 'database updated to version 200216'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200215 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
