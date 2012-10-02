-- Update database from version 200199 to 200200
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- Clean up default constituent chart template
-------------------------------------------------------------------------------
declare @defaultTemplate int, @oldPostNuc int, @inner int,
	@outer int, @nucleus int, @oc int, @ws int, @newPostNuc int,
	@emp int, @now datetime, @prenuc int

select @now = getdate()

set @ws = (select id from LgWritingSystem where ICULocale = 'en')

set @defaultTemplate = (select cpr.id from CmPossibility_ cpr
	join CmPossibilityList_ cpl on cpr.owner$ = cpl.id
	join CmPossibility_Name cpn on cpn.Obj = cpr.id and cpn.Txt = 'Default' and cpn.Ws = @ws
	join Field$ f on f.id = cpl.ownFlid$ and f.Name = 'ConstChartTemplates')

set @oldPostNuc = (select cpr.id from CmPossibility_ cpr
	join CmPossibility_Name cpn on cpn.Obj = cpr.id and cpn.Txt = 'post-nuclear' and cpn.Ws = @ws
		and cpr.Owner$ = @defaultTemplate)

set @nucleus = (select cpr.id from CmPossibility_ cpr
	join CmPossibility_Name cpn on cpn.Obj = cpr.id and cpn.Txt = 'nucleus' and cpn.Ws = @ws
		and cpr.Owner$ = @defaultTemplate)

set @prenuc = (select cpr.id from CmPossibility_ cpr
	join CmPossibility_Name cpn on cpn.Obj = cpr.id and cpn.Txt = 'Pre-nuclear' and cpn.Ws = @ws
		and cpr.Owner$ = @defaultTemplate)

set @oc = (select cpr.id from CmPossibility_ cpr
	join CmPossibility_Name cpn on cpn.Obj = cpr.id and cpn.Txt = 'O/[C]' and cpn.Ws = @ws
		and cpr.Owner$ = @nucleus)

set @emp = (select cpr.id from CmPossibility_ cpr
	join CmPossibility_Name cpn on cpn.Obj = cpr.id and cpn.Txt = 'EMP' and cpn.Ws = @ws
		and cpr.Owner$ = @prenuc)

update CmPossibility_Name set Txt = 'O/C' where obj = @oc and Ws = @ws
update CmPossibility_Name set Txt = 'Nucleus' where obj = @nucleus and Ws = @ws
update CmPossibility_Name set Txt = 'Inner' where obj = @oldPostNuc and Ws = @ws
update CmPossibility_Abbreviation set Txt = 'in' where obj = @oldPostNuc and Ws = @ws

exec CreateObject_CmPossibility
	@ws,		--( English Writing System for Name
	N'Post-nuclear', --( Name
	@ws,		--( English Writing System for Abbreviation
	'postnuc',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@defaultTemplate,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	@newPostNuc output, NULL

-- Move the old post-nuc column (now Inner) to the start of the
update CmObject set Owner$ = @newPostNuc where id = @oldPostNuc
update CmObject set OwnOrd$ = 1 where id = @oldPostNuc

exec CreateObject_CmPossibility
	@ws,		--( English Writing System for Name
	N'Outer', --( Name
	@ws,		--( English Writing System for Abbreviation
	'out',		--( Abbreviation
	null,		--( English Writing System for Description
	null,
	null,		--( Description format
	0,			--( Sort spec
	@now,		--( Date Created
	@now,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@newPostNuc,--( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities)
	NULL,		--( StartObj
	NULL, NULL

declare @gonner nvarchar(20)
set @gonner = CONVERT(nvarchar(20), @emp)

exec DeleteObjects @gonner

GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200199
BEGIN
	UPDATE Version$ SET DbVer = 200200
	COMMIT TRANSACTION
	PRINT 'database updated to version 200200'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200199 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
