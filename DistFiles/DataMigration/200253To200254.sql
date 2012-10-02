-- Update database from version 200253 to 200254
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- LT-9653 Rename the 'O/C' Column to 'Object/Complement' in the Text Chart
-------------------------------------------------------------------------------
declare @defaultTemplate int, @nucleus int, @oc int, @ws int, @now datetime

select @now = getdate()

set @ws = (select id from LgWritingSystem where ICULocale = 'en')

set @defaultTemplate = (select cpr.id from CmPossibility_ cpr
	join CmPossibilityList_ cpl on cpr.owner$ = cpl.id
	join CmPossibility_Name cpn on cpn.Obj = cpr.id and cpn.Txt = 'Default' and cpn.Ws = @ws
	join Field$ f on f.id = cpl.ownFlid$ and f.Name = 'ConstChartTempl')

set @nucleus = (select cpr.id from CmPossibility_ cpr
	join CmPossibility_Name cpn on cpn.Obj = cpr.id and cpn.Txt = 'Nucleus' and cpn.Ws = @ws
		and cpr.Owner$ = @defaultTemplate)

set @oc = (select cpr.id from CmPossibility_ cpr
	join CmPossibility_Name cpn on cpn.Obj = cpr.id and cpn.Txt = 'O/C' and cpn.Ws = @ws
		and cpr.Owner$ = @nucleus)

update CmPossibility_Name set Txt = 'Object/Complement' where obj = @oc and Ws = @ws

GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200253
BEGIN
	UPDATE Version$ SET DbVer = 200254
	COMMIT TRANSACTION
	PRINT 'database updated to version 200254'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200253 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
