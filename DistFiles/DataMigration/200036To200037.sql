-- update database FROM version 200036 to 200037
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- Add footnote separator width attribute to Publications (FWM-47)
-------------------------------------------------------------------------------

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (42009, 2, 42, NULL, 'FootnoteSepWidth', 0, NULL, NULL)
GO

exec UpdateClassView$ 42, 1
GO

-------------------------------------------------------------------------------
-- Change CmAnnotation to abstract (FWM-27)
-- Change CmAnnotation attribute: rename LastModified to DateModified (FWM-67)
-------------------------------------------------------------------------------

-- make CmAnnotation abstract
UPDATE Class$
SET Abstract = 1
WHERE id = 34
GO

-- Rename LastModified to DateModified
-- Delete existing definition and re-add. This will lose any data, but there
-- shouldn't be any at this time and this data is not critical

DELETE FROM Field$
WHERE id = 34010
GO

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES(34010, 5, 34, null, 'DateModified', 0, null, null)
GO

exec UpdateClassView$ 34, 1
GO

-------------------------------------------------------------------------------
-- Add classes, attributes and references for CheckLists (FWM-68)
-------------------------------------------------------------------------------
INSERT INTO Class$ (Id, Mod, Base, Abstract, Name)
VALUES(5116, 5, 0, 0, 'ChkRef')
GO
INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (5116001, 2, 5116, null, 'Ref', 0, null, null)
INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (5116002, 13, 5116, null, 'KeyWord', 0, null, null)
INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (5116003, 14, 5116, null, 'Comment', 0, null, null)
INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (5116004, 2, 5116, null, 'Status', 0, null, null)
INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (5116005, 24, 5116, 5062, 'Rendering', 0, null, null)
go

exec DefineCreateProc$ 5116
GO
exec UpdateClassView$ 5116, 1
GO

INSERT INTO Class$ (Id, Mod, Base, Abstract, Name)
VALUES (5115, 5, 7, 0, 'ChkItem')
GO

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (5115001, 27, 5115, 5116, 'Occurrences', 0, null, null)
go

exec DefineCreateProc$ 5115
GO
exec UpdateClassView$ 5115, 1
GO

-------------------------------------------------------------------------------
-- Add chapter label attribute to ScrBook (FWM-70)
-------------------------------------------------------------------------------

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (3002012, 1, 3002, NULL, 'UseChapterNumHeading', 0, NULL, NULL)
GO

exec UpdateClassView$ 3002, 1
GO

-------------------------------------------------------------------------------
-- Add chapter label attributes to Scripture (FWM-70)
-- Remove Scripture.BookRevisions and any associatied data (FWM-71)
-------------------------------------------------------------------------------

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (3001022, 13, 3001, NULL, 'ChapterLabel', 0, NULL, NULL)
INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (3001023, 13, 3001, NULL, 'PsalmLabel', 0, NULL, NULL)
GO

DELETE FROM Field$ WHERE Id = 3001008 --( Scripture.BookRevisions
GO

exec UpdateClassView$ 3001, 1
GO

-------------------------------------------------------------------------------
-- Add CheckLists reference to LanguageProject (FWM-68)
-------------------------------------------------------------------------------

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES(6001050, 23, 6001, 8, 'CheckLists', 0, null, null)
GO

exec UpdateClassView$ 6001, 1
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200036
begin
	UPDATE Version$ SET DbVer = 200037
	COMMIT TRANSACTION
	print 'database updated to version 200037'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200036 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
