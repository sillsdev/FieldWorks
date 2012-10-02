-- Update database FROM version 200161 to 200162

BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-----------------------------------------------------------------------
-- Implement Tuning Advisor recommendations.
-----------------------------------------------------------------------

-- REVIEW (SteveMiller) I am uncertain why the Tuning Advisor
-- recommended this one, because the PK is alread on ID. Might
-- be because of the INCLUDE statement.

DROP INDEX Ind_CmObject_ID ON CmObject
CREATE INDEX Ind_CmObject_ID ON CmObject (Id ASC) INCLUDE (Class$, Owner$)
GO

CREATE INDEX Ind_CmObject_OwnFlid$ ON CmObject (OwnFlid$ ASC) INCLUDE (Id, Owner$, OwnOrd$)
GO

CREATE INDEX Ind_CmObject_Class$ ON CmObject (Class$ ASC) INCLUDE (Id, Owner$, OwnFlid$)
GO


-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200161
BEGIN
	UPDATE [Version$] SET [DbVer] = 200162
	COMMIT TRANSACTION
	PRINT 'database updated to version 200162'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200161 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
