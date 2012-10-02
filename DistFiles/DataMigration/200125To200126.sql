-- update database FROM version 200125 to 200126
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- For projects created since 200111, the owner of the Translator Notes
-- possibility needs to be updated to the Scripture notes possibility.
-- (NewLangProj.xml had Translator Notes listed as a possibility of the main
-- Annotation Definitions list rather than as a subpossibility of Scripture
-- notes.)
-------------------------------------------------------------------------------

DECLARE @hvoNotesAnnDefn int	-- hvo of Scripture notes definition possibility

SELECT @hvoNotesAnnDefn=[id] FROM CmObject WHERE Guid$ = '7FFC4EAB-856A-43CC-BC11-0DB55738C15B'

-- If the owner of the Translator Notes subpossibility is not owned by the Scripture Note possibility,
-- update it.
UPDATE CmObject_
SET [Owner$] = @hvoNotesAnnDefn, [OwnFlid$] = 7004
WHERE [Guid$] = '80AE5729-9CD8-424D-8E71-96C1A8FD5821' AND [Owner$] != @hvoNotesAnnDefn

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200125
begin
	UPDATE Version$ SET DbVer = 200126
	COMMIT TRANSACTION
	print 'database updated to version 200126'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200125 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
