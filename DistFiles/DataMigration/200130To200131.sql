-- update database FROM version 200130 to 200131
BEGIN TRANSACTION  --( will be rolled back if wrong version#
/*
 * Added COLLATE specification for fn_varbintohexstr. Without it, we can get a crash
 * when the server has a different default collation from the database.
 */

BEGIN
-------------------------------------------------------------------------------
-- Set the annotation type to Consultant Note for any Scripture Note that
-- doesn't have this set.
-------------------------------------------------------------------------------
	DECLARE  @idConsultantNote int
	SELECT @idConsultantNote =
		(SELECT id
		 FROM CmAnnotationDefn_
		 WHERE Guid$ = '56DE9B1A-1CE7-42A1-AA76-512EBEFF0DDA')

	UPDATE ScrScriptureNote_
	SET AnnotationType = @idConsultantNote
	where AnnotationType is null

-------------------------------------------------------------------------------
-- Update the style rules for any Scripture Note response that doesn't have the
-- Remark paragraph style set.
-------------------------------------------------------------------------------
	update sttxtpara_
	set StyleRules = 0x0001850206520065006D00610072006B00
	where owner$ in (select id from stjournaltext_ where ownflid$ = 3018007)
	and StyleRules <> 0x0001850206520065006D00610072006B00
END

GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200130
begin
	UPDATE Version$ SET DbVer = 200131
	COMMIT TRANSACTION
	print 'database updated to version 200131'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200130 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO