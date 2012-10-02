-- Update database from version 200233 to 200234
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- LT-8369: Correct Chart Templates
-------------------------------------------------------------------------------
	IF not exists (select * from dsConstChart_Rows)
		-- Chart template is bad. Inner and Outer shouldn't be under Default
		Update CmObject
		   Set owner$ = (select Obj from CmPossibility_Name where Txt = 'Post-nuclear')
		 Where id in (select Obj from CmPossibility_Name where Txt in ('Inner', 'Outer'))
		  and owner$ = (select Obj from CmPossibility_Name where Txt = 'Default')
	Else
		Print' This database contains discourse charts'

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200233
BEGIN
	UPDATE Version$ SET DbVer = 200234
	COMMIT TRANSACTION
	PRINT 'database updated to version 200234'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200233 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
