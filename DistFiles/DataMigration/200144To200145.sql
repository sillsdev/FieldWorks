-- update database FROM version 200144 to 200145
BEGIN TRANSACTION  --( will be rolled back if wrong version#)
-- Fix a few illegal characters in CmPossibility names

update CmPossibility_Name
set txt = N'Traduction litt' + nchar(233) + N'rale'
where txt = N'Traduction litt' + nchar(65475) + nchar(65449) + N'rale'

update CmPossibility_Name
set txt = N'Traducci' + nchar(243) + N'n libre'
where txt = N'Traducci' + nchar(65475) + nchar(65459) + N'n libre'

update CmPossibility_Name
set txt = N'Traducci' + nchar(243) + N'n literal'
where txt = N'Traducci' + nchar(65475) + nchar(65459) + N'n lliteral'

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200144
BEGIN
	UPDATE [Version$] SET [DbVer] = 200145
	COMMIT TRANSACTION
	PRINT 'database updated to version 200145'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200144 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
