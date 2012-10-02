-- Update database from version 200256 to 200257
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- Removes ownership of Wfics, Pfics, text segments, free translations,
-- literal translations, and (interlinear segment) note annotations
-------------------------------------------------------------------------------

update CmObject set Owner$ = null, OwnFlid$ = null, OwnOrd$ = null
from CmObject co
join CmAnnotation ca on ca.id = co.id
join CmAnnotationDefn_ cad on ca.AnnotationType = cad.id
and cad.Guid$ in ('EB92E50F-BA96-4D1D-B632-057B5C274132', 'CFECB1FE-037A-452D-A35B-59E06D15F4DF',
'B63F0702-32F7-4ABB-B005-C1D2265636AD', '9AC9637A-56B9-4F05-A0E1-4243FBFB57DB', 'B0B1BB21-724D-470A-BE94-3D9A436008B8',
'7FFC4EAB-856A-43CC-BC11-0DB55738C15B')
GO

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200256
BEGIN
	UPDATE Version$ SET DbVer = 200257
	COMMIT TRANSACTION
	PRINT 'database updated to version 200257'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200256 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
