-- update database from version 200021 to 200022
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------
-- Fix possible data corruption from previous bug
--    (CmAgentEvaluations pointing to WfiGloss are changed to point to WfiAnalysis
-------------------------------------------------------------

update CmAgentEvaluation
set CmAgentEvaluation.target = co.owner$
from CmAgentEvaluation cae (readuncommitted)
join CmObject co (readuncommitted) on co.id = cae.target
where co.class$ = 5060

-------------------------------------------------------------------------------

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200021
begin
	update Version$ set DbVer = 200022
	COMMIT TRANSACTION
	print 'database updated to version 200022'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200021 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO