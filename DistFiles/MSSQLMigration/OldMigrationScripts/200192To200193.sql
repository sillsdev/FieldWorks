-- Update database from version 200192 to 200193
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Update human and parser CmAgents to fixed Guids, if they haven't already been set.
-- There are two reasons for this: 1)The script to go from 200189 to 200190 seems to fail
-- intermittently, causing the CmAgent "Computer" GUID to not get set correctly; 2) A few
-- databases exist that were not created using NewLangProj, so they do not have the fixed
-- GUIDs for the M3Parser and Human CmAgents.
-- After running this script:
-- The computer CmAgent should have this fixed Guid: 67E9B8BF-C312-458e-89C3-6E9326E48AA0
-- The parser CmAgent should have this fixed Guid: 1257A971-FCEF-4F06-A5E2-C289DE5AAF72
-- The human CmAgent should have this fixed Guid: 9303883A-AD5C-4CCF-97A5-4ADD391F8DCB
BEGIN
	-- Set a fixed Guid for the human CmAgent
	UPDATE CmAgent_
	SET Guid$ = '9303883A-AD5C-4CCF-97A5-4ADD391F8DCB'
	WHERE Human = 1 AND Guid$ <> '9303883A-AD5C-4CCF-97A5-4ADD391F8DCB'

	-- Set a fixed Guid for the parser CmAgent
	UPDATE CmAgent_
	SET Guid$ = '1257A971-FCEF-4F06-A5E2-C289DE5AAF72'
	WHERE Human = 0 AND Guid$ <> '1257A971-FCEF-4F06-A5E2-C289DE5AAF72'
	AND id = (SELECT DISTINCT obj FROM CmAgent_Name WHERE Txt = 'M3Parser')

	-- Set a fixed Guid for the parser CmAgent
	UPDATE CmAgent_
	SET Guid$ = '67E9B8BF-C312-458E-89C3-6E9326E48AA0'
	WHERE Human = 0 AND Guid$ <> '67E9B8BF-C312-458E-89C3-6E9326E48AA0'
	AND id = (SELECT DISTINCT obj FROM CmAgent_Name WHERE Txt = 'Computer')
END
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200192
BEGIN
	UPDATE Version$ SET DbVer = 200193
	COMMIT TRANSACTION
	PRINT 'database updated to version 200193'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200192 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
