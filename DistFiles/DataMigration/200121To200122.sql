-- update database FROM version 200121 to 200122
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-- Changes the Guid of the CmAnnotationDefn to a fixed Guid.
-- (The stored procedure that creates CmAnnotationDefn objects
--  does not handle fixed Guids--a new guid is created each time).
declare @hvo1 int, @hvo2 int
select @hvo1=[Obj] from CmPossibility_Name where Txt = 'Translator Note'
select @hvo2=[Obj] from CmPossibility_Abbreviation where Txt = 'TransNt'

IF (@hvo1 = @hvo2)
BEGIN
	UPDATE CmObject
	SET [Guid$]='80AE5729-9CD8-424D-8E71-96C1A8FD5821'
	WHERE id=@hvo1 AND NOT Guid$='80AE5729-9CD8-424D-8E71-96C1A8FD5821'
END

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200121
begin
	UPDATE Version$ SET DbVer = 200122
	COMMIT TRANSACTION
	print 'database updated to version 200122'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200121 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO

-- What about updating the date created and modified?
