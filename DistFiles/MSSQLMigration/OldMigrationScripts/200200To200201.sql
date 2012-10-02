-- Update database from version 200200 to 200201
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- Clean up invalid references in ChkRef table
-- Depending on the date the DB was created and the
-- way it was created, the references could be wrong
-- in one of the following ways:
-- Could have the last valid verse for the chapter
--    where the key term was expected
-- Could have an incorrect chapter and/or verse
--    based on treating the digits of the verse bridge as
--    one large verse number (> 999 increases the
--    chapter number)
-- Could have the first part of the verse bridge, ignoring
--     the second part
-------------------------------------------------------------------------------
-- Fix the reference for Paradise in 2CO 12:4
update ChkRef set Ref = 47012004
 where KeyWord = 'paradeisos' and Ref in (47012021, 47012034, 47012003)
-- Fix the reference for Jew in Luke 23:51
update ChkRef set Ref = 42023051
 where KeyWord = 'Ioudaios, ou' and Ref in (42023056, 42028051, 42023050)
-- Fix the reference for onoma in Acts 9:29
update ChkRef set Ref = 44009029
 where KeyWord = 'onoma' and Ref in (44009043, 44011829, 44009028)
-- Fix the reference for Repentence in Hebrews 6:6
update ChkRef set Ref = 58006006
 where KeyWord = 'metanoia' and Ref in (58006020, 58006046, 58006004)
-- Fix the reference for Indeed in Galatians 3:4
update ChkRef set Ref = 48003004
 where KeyWord = 'gj' and Ref in (48003029, 48003045)

GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200200
BEGIN
	UPDATE Version$ SET DbVer = 200201
	COMMIT TRANSACTION
	PRINT 'database updated to version 200201'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200200 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
