-- update database FROM version 200141 to 200142
BEGIN TRANSACTION  --( will be rolled back if wrong version#

------------------------------------------------------------
-- 1) FWM-118 : Migrate data to ReversalIndexEntry.ReversalForm -- 5053006
-- 2) Remove Bad MultiUnicode
------------------------------------------------------------

------------------------------------------------------------
-- 1) FWM-118 : Migrate data to ReversalIndexEntry.ReversalForm -- 5053006
------------------------------------------------------------

-- Move the data from the obsolete Integer and Unicode fields to the new
-- multi-WS unicode field.

INSERT INTO ReversalIndexEntry_ReversalForm (Obj, Ws, Txt)
	SELECT Id, WritingSystem, Form
	FROM ReversalIndexEntry
	WHERE Form is not null AND WritingSystem is not null

-- Delete ReversalIndexEntry.Form (5053004)
-- Delete ReversalIndexEntry.WritingSystem (5053005)

DELETE FROM Field$ WHERE Id in (5053004, 5053005)
GO
exec UpdateClassView$ 5053
GO

------------------------------------------------------------
-- 2) Remove Bad MultiUnicode
------------------------------------------------------------

DECLARE multiTxtTables CURSOR FAST_FORWARD FOR
	SELECT c.name, f.Name
	FROM Field$ f
	JOIN Class$ c ON c.Id=f.Class
	WHERE f.Type=16
	ORDER BY c.name, f.Name

DECLARE @nvcClass nvarchar(100), @nvcField nvarchar(100), @nvcQuery nvarchar(4000)
DECLARE @cBadRows int

OPEN multiTxtTables
FETCH multiTxtTables INTO @nvcClass, @nvcField
WHILE @@FETCH_STATUS = 0 BEGIN

	SET @nvcQuery = N'SELECT @cBadRows=COUNT(*) FROM ' + @nvcClass+'_'+@nvcField +
		' mt LEFT OUTER JOIN ' + @nvcClass + ' c ON c.Id=mt.Obj WHERE c.Id IS NULL'
	EXEC sp_executesql @nvcQuery, N'@cBadRows INT OUTPUT', @cBadRows OUTPUT
	IF @cBadRows <> 0 BEGIN
		SET @nvcQuery = N'DELETE ' + @nvcClass+'_'+@nvcField +
			' FROM ' + @nvcClass+'_'+@nvcField + ' mt LEFT OUTER JOIN ' + @nvcClass +
			' c ON c.Id=mt.Obj WHERE c.id IS NULL'
		EXEC sp_executesql @nvcQuery
		PRINT @nvcClass + '_' + @nvcField + ' had ' + CONVERT(nvarchar(20), @cBadRows) + ' bad rows.'
	END

	FETCH multiTxtTables INTO @nvcClass, @nvcField
END
CLOSE multiTxtTables
DEALLOCATE multiTxtTables

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200141
begin
	UPDATE Version$ SET DbVer = 200142
	COMMIT TRANSACTION
	print 'database updated to version 200142'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200141 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO