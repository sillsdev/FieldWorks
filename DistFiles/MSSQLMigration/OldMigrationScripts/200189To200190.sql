-- Update database from version 200189 to 200190
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- TE-5712: Remove CreateNewScrBook
-------------------------------------------------------------------------------
IF OBJECT_ID('CreateNewScrBook') IS NOT NULL BEGIN
	PRINT 'removing procedure CreateNewScrBook';
	DROP PROCEDURE CreateNewScrBook;
END
GO

-------------------------------------------------------------------------------
-- TE-6097: Add a new CmAgent "Computer" as an AnalyzingAgent to the
-- LanguageProject
-- Change the Errors AnnotationDefn
-------------------------------------------------------------------------------
BEGIN
	DECLARE @lp INT, @en INT, @NewObjId INT, @NewObjGuid UNIQUEIDENTIFIER, @hvoErrorsAnnDefn INT
	SELECT top 1 @lp=id FROM LanguageProject
	SELECT @en=id FROM LgWritingSystem WHERE ICULocale = 'en'
	EXECUTE CreateObject_CmAgent @en, 'Computer', null , false, null, @lp, 6001038, null, @NewObjId OUTPUT,
		@NewObjGuid OUTPUT
	-- Assign a fixed GUID
	UPDATE CmObject SET Guid$ = '67E9B8BF-C312-458e-89C3-6E9326E48AA0' WHERE Id = @NewObjId

	SELECT @hvoErrorsAnnDefn = d.id FROM CmAnnotationDefn_ d
	JOIN CmPossibility_Name n ON d.Id = n.Obj AND n.Ws = @en AND n.Txt='Errors'
	JOIN CmPossibilityList_ p ON p.OwnFlid$ = 6001047 AND d.Owner$ = p.Id
	WHERE d.OwnFlid$ = 8008

	UPDATE CmAnnotationDefn
	SET UserCanCreate = 0, CanCreateOrphan = 0
	WHERE id = @hvoErrorsAnnDefn
END
GO

-------------------------------------------------------------------------------
-- FWM-136: Model change to support checking tool history. Add ScrCheckRun
-- class and add collection ScrBookAnnotations.ChkHistRecs to own them.
-------------------------------------------------------------------------------

	INSERT INTO Class$ ([Id], [Mod], [Base], [Abstract], [Name])
		VALUES(3019, 3, 0, 0, 'ScrCheckRun')
GO

EXEC UpdateClassView$ 3019

GO

	INSERT INTO [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		VALUES(3017003, 25, 3017,
			3019, 'ChkHistRecs',0,Null, null, null, null)
GO

EXEC UpdateClassView$ 3017
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200189
BEGIN
	UPDATE Version$ SET DbVer = 200190
	COMMIT TRANSACTION
	PRINT 'database updated to version 200190'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200189 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
