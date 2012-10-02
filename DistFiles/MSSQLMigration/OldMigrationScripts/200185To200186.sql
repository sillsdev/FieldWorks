-- Update database from version 200185 to 200186
-- This fixes a botch in migrating from 200000 to 200006 which may affect only a handful of DBs.
-- The code in V200toV206.sql should have worked, but it apparently doesn't.

BEGIN TRANSACTION  --( will be rolled back if wrong version#)

UPDATE CmObject SET Class$ = 49 WHERE Class$ = 2001
UPDATE CmObject SET Class$ = 50 WHERE Class$ = 2002
UPDATE CmObject SET Class$ = 51 WHERE Class$ = 2003
UPDATE CmObject SET Class$ = 53 WHERE Class$ = 2005
UPDATE CmObject SET Class$ = 54 WHERE Class$ = 2006
UPDATE CmObject SET Class$ = 55 WHERE Class$ = 2007
UPDATE CmObject SET Class$ = 56 WHERE Class$ = 2008
UPDATE CmObject SET Class$ = 57 WHERE Class$ = 2009
UPDATE CmObject SET Class$ = 58 WHERE Class$ = 2010
UPDATE CmObject SET Class$ = 59 WHERE Class$ = 2011
UPDATE CmObject SET Class$ = 60 WHERE Class$ = 2012
UPDATE CmObject SET Class$ = 61 WHERE Class$ = 2013
UPDATE CmObject SET Class$ = 62 WHERE Class$ = 2014
UPDATE CmObject SET Class$ = 63 WHERE Class$ = 2015
UPDATE CmObject SET Class$ = 64 WHERE Class$ = 2016
UPDATE CmObject SET Class$ = 65 WHERE Class$ = 2017

UPDATE CmObject SET OwnFlid$ = 49002 WHERE OwnFlid$ = 2001002
UPDATE CmObject SET OwnFlid$ = 50001 WHERE OwnFlid$ = 2002001
UPDATE CmObject SET OwnFlid$ = 51001 WHERE OwnFlid$ = 2003001
UPDATE CmObject SET OwnFlid$ = 53001 WHERE OwnFlid$ = 2005001
UPDATE CmObject SET OwnFlid$ = 54001 WHERE OwnFlid$ = 2006001
UPDATE CmObject SET OwnFlid$ = 55001 WHERE OwnFlid$ = 2007001
UPDATE CmObject SET OwnFlid$ = 55002 WHERE OwnFlid$ = 2007002
UPDATE CmObject SET OwnFlid$ = 55003 WHERE OwnFlid$ = 2007003
UPDATE CmObject SET OwnFlid$ = 55004 WHERE OwnFlid$ = 2007004
UPDATE CmObject SET OwnFlid$ = 55005 WHERE OwnFlid$ = 2007005
UPDATE CmObject SET OwnFlid$ = 55006 WHERE OwnFlid$ = 2007006
UPDATE CmObject SET OwnFlid$ = 55007 WHERE OwnFlid$ = 2007007
UPDATE CmObject SET OwnFlid$ = 55008 WHERE OwnFlid$ = 2007008
UPDATE CmObject SET OwnFlid$ = 56001 WHERE OwnFlid$ = 2008001
UPDATE CmObject SET OwnFlid$ = 56002 WHERE OwnFlid$ = 2008002
UPDATE CmObject SET OwnFlid$ = 56003 WHERE OwnFlid$ = 2008003
UPDATE CmObject SET OwnFlid$ = 57001 WHERE OwnFlid$ = 2009001
UPDATE CmObject SET OwnFlid$ = 57002 WHERE OwnFlid$ = 2009002
UPDATE CmObject SET OwnFlid$ = 57003 WHERE OwnFlid$ = 2009003
UPDATE CmObject SET OwnFlid$ = 58001 WHERE OwnFlid$ = 2010001
UPDATE CmObject SET OwnFlid$ = 59001 WHERE OwnFlid$ = 2011001
UPDATE CmObject SET OwnFlid$ = 59002 WHERE OwnFlid$ = 2011002
UPDATE CmObject SET OwnFlid$ = 59003 WHERE OwnFlid$ = 2011003
UPDATE CmObject SET OwnFlid$ = 59004 WHERE OwnFlid$ = 2011004
UPDATE CmObject SET OwnFlid$ = 61001 WHERE OwnFlid$ = 2013001
UPDATE CmObject SET OwnFlid$ = 62002 WHERE OwnFlid$ = 2014002
UPDATE CmObject SET OwnFlid$ = 62003 WHERE OwnFlid$ = 2014003
UPDATE CmObject SET OwnFlid$ = 63001 WHERE OwnFlid$ = 2015001
UPDATE CmObject SET OwnFlid$ = 64001 WHERE OwnFlid$ = 2016001
UPDATE CmObject SET OwnFlid$ = 65001 WHERE OwnFlid$ = 2017001
UPDATE CmObject SET OwnFlid$ = 65002 WHERE OwnFlid$ = 2017002
UPDATE CmObject SET OwnFlid$ = 65003 WHERE OwnFlid$ = 2017003
UPDATE CmObject SET OwnFlid$ = 65004 WHERE OwnFlid$ = 2017004
UPDATE CmObject SET OwnFlid$ = 65005 WHERE OwnFlid$ = 2017005
UPDATE CmObject SET OwnFlid$ = 65006 WHERE OwnFlid$ = 2017006

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200185
BEGIN
	UPDATE Version$ SET DbVer = 200186
	COMMIT TRANSACTION
	PRINT 'database updated to version 200186'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200185 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
