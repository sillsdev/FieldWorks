-- Update database from version 200186 to 200187
-- This fixes a problem where the style rules were set to null.

BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Assign the style rules for Section Head when the StyleRules are null for a
-- normal Scripture section heading paragraph.
UPDATE StPara_
SET StyleRules = 0x000185020C530065006300740069006F006E0020004800650061006400
FROM sttext_ t,
	scrsection_ s
WHERE StPara_.stylerules is null
	AND t.ownflid$ = 3005001
	AND StPara_.Owner$ = t.id
	AND t.owner$ = s.id
	AND s.verserefstart % 1000 > 0

-- Assign the style rules for Paragraph when the StyleRules are null for a
-- normal Scripture content paragraph.
UPDATE StPara_
SET StyleRules = 0x0001850209500061007200610067007200610070006800
FROM sttext_ t,
	scrsection_ s
WHERE StPara_.stylerules is null
	AND t.ownflid$ = 3005002
	AND StPara_.Owner$ = t.id
	AND t.owner$ = s.id
	AND s.verserefstart % 1000 > 0

-- Assign the style rules for Intro Section Head when the StyleRules are null for a
-- introduction Scripture section heading paragraph.
UPDATE StPara_
SET StyleRules = 0x000185021249006E00740072006F002000530065006300740069006F006E0020004800650061006400
FROM sttext_ t,
	scrsection_ s
WHERE StPara_.stylerules is null
	AND t.ownflid$ = 3005001
	AND StPara_.Owner$ = t.id
	AND t.owner$ = s.id
	AND s.verserefstart % 1000 = 0

-- Assign the style rules for Intro Paragraph when the StyleRules are null for a
-- introduction Scripture content paragraph.
UPDATE StPara_
SET StyleRules = 0x000185020F49006E00740072006F002000500061007200610067007200610070006800
FROM sttext_ t,
	scrsection_ s
WHERE StPara_.stylerules is null
	AND t.ownflid$ = 3005002
	AND StPara_.Owner$ = t.id
	AND t.owner$ = s.id
	AND s.verserefstart % 1000 = 0

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200186
BEGIN
	UPDATE Version$ SET DbVer = 200187
	COMMIT TRANSACTION
	PRINT 'database updated to version 200187'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200186 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
