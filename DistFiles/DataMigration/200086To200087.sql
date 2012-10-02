-- Update database from version 200086 to 200087
BEGIN TRANSACTION  --( will be rolled back if wrong version#)
DECLARE @Err int
SET @Err = 0

-- 200072To200073.sql fixed the update trigger for Field$ to properly recreate all the
-- affected CreateObject_xxx stored procedures as needed.  Unfortunately, earlier migration
-- scripts updated Field$ with abandon without ever checking whether subclasses needed to be
-- updated.  Just to be safe, we're running through all classes which have had fields of
-- their parents updated to recreate the affected CreateObject_xxx stored procedures.
-- Note that CreateObject_MoStemAllomorph which is used below is one of the affected
-- stored procedures.

DECLARE @clid int, @nvcName nvarchar(1000)
DECLARE cur CURSOR local static forward_only read_only FOR
	SELECT DISTINCT c.Id, c.Name
	FROM ClassPar$ p
	JOIN Class$ c on c.Id = p.Src AND c.Id <> p.Dst AND c.Abstract = 0
	WHERE p.Dst IN (1,17,25,34,42,49,55,59,65,2007,2017,3001,3002,3004,3005,3010,5002,5005,5007,
			5008,5009,5016,5031,5035,5038,5045,5049,5053,5117,5118,5119,5120,6001)

OPEN cur
FETCH NEXT FROM cur INTO @clid, @nvcName
WHILE @@FETCH_STATUS = 0
BEGIN
	print 'redefining CreateObject_' + @nvcName
	EXEC DefineCreateProc$ @clid

	FETCH NEXT FROM cur INTO @clid, @nvcName
END
CLOSE cur
DEALLOCATE cur

-- 200072To200073.sql properly handled converting either the UnderlyingForm MoForm or the final
-- MoForm from the Allomorphs field to the LexemeForm.  However, it did not handle the case
-- where both the UnderlyingForm and the Allomorphs fields were empty.  This results in an
-- empty LexemeForm field, which should never be.  We detect that condition here, and create
-- LexemeForm fields where needed, moving data from the CitationForm field as appropriate.

DECLARE @hvoEntry int, @hvoNewAllo int, @hvoStemType int
SELECT TOP 1 @hvoStemType=Id FROM CmObject WHERE Guid$='D7F713E8-E8CF-11D3-9764-00C04F186933'

DECLARE cur CURSOR local static forward_only read_only FOR
	SELECT le.Id
	FROM LexEntry le
	WHERE le.Id NOT IN (SELECT Src FROM LexEntry_LexemeForm)

OPEN cur
FETCH NEXT FROM cur INTO @hvoEntry
WHILE @@FETCH_STATUS = 0
BEGIN
	-- Our best guess for an entry without any LexemeForm or AlternateForms, but with a
	-- CitationForm (presumably) is that it is a stem.

	EXEC CreateObject_MoStemAllomorph null, null, 0, @hvoEntry, 5002029, null,
		@hvoNewAllo output, null, 0, null

	UPDATE MoForm SET MorphType=@hvoStemType WHERE Id=@hvoNewAllo

	INSERT INTO MoForm_Form (Obj,Ws,Txt)
	SELECT @hvoNewAllo,Ws,Txt FROM LexEntry_CitationForm cf
	WHERE cf.Obj = @hvoEntry

	DELETE FROM LexEntry_CitationForm WHERE Obj = @hvoEntry

	FETCH NEXT FROM cur INTO @hvoEntry
END
CLOSE cur
DEALLOCATE cur

GOTO LDone

LFail:
SET @Err = 1

LDone:

IF @Err = 1 BEGIN
	ROLLBACK TRANSACTION
	print 'Update aborted because an error occurred'
END
ELSE BEGIN
	declare @dbVersion int
	select @dbVersion = DbVer from Version$
	if @Err <> 1 AND @dbVersion = 200086
	begin
		update Version$ set DbVer = 200087
		COMMIT TRANSACTION
		print 'database updated to version 200087'
	end
	else
	begin
		ROLLBACK TRANSACTION
		print 'Update aborted: this works only if DbVer = 200086 (DbVer = ' +
				convert(varchar, @dbVersion) + ')'
	end
END
GO
