-- update database FROM version 200149 to 200150
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- Remove FwSqlExtend.dll, which was an extended stored procedure. xp_IsMatch
-- registered the .dll. (It was also the name of the project.) fnIsMatch was a
-- wrapper call to the extended stored procedure.
-------------------------------------------------------------------------------

--( Remove xp_IsMatch (script based on script generated from Query Analyzer).

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[xp_IsMatch]')
	and OBJECTPROPERTY(id, N'IsExtendedProc') = 1) BEGIN

	DBCC FwSqlExtend (FREE)
	exec master..sp_dropextendedproc N'[dbo].[xp_IsMatch]'
END
GO
--( Remove fnIsMatch$

IF OBJECT_ID('fnIsMatch$') IS NOT NULL
	DROP FUNCTION fnIsMatch$
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200149
BEGIN
	UPDATE [Version$] SET [DbVer] = 200150
	COMMIT TRANSACTION
	PRINT 'database updated to version 200149'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200149 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
