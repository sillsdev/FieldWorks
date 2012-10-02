-- update database FROM version 200117 to 200118

BEGIN TRANSACTION  --( will be rolled back if wrong version#
-- Update two stored procedures which wrongly used "ID" instead of "Id" in one
-- of the few contexts where it makes a difference in SQL.


BEGIN
-------------------------------------------------------------------------------
-- Update the TE Notes Category filter to match including subitems
-------------------------------------------------------------------------------
	UPDATE	cmcell_
	SET	Contents = 'Matches +subitems'
	where	owner$ =	(select	[id]
				from	cmrow_
				where	owner$ =	(select	[id]
							from	cmfilter
							where	App='A7D421E1-1DD3-11D5-B720-0010A4B54856'
							and	Name = 'Category'
							and	ColumnInfo='3018,3018002'))
	AND	Contents = 'Matches'

-------------------------------------------------------------------------------
-- Update the TE Notes Categories Possibility List to fix the ItemClsid
-------------------------------------------------------------------------------
	UPDATE CmPossibilityList
	SET	[ItemClsid] = (SELECT [id] FROM Class$ where [Name] = 'CmPossibility')
	where [Id] = (SELECT [Dst] FROM Scripture_ScriptureNoteCategories)
END
GO
-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200117
begin
	UPDATE Version$ SET DbVer = 200118
	COMMIT TRANSACTION
	print 'database updated to version 200118'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200117 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
