-- update database from version 200041 to 200042
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
--( Set ownership of any ReversalIndex objects to the lexicon.
declare @ldbID int
SELECT  top 1 @ldbID=ID
FROM LexicalDatabase

UPDATE CmObject
SET Owner$=@ldbID, OwnFlid$=5005017
WHERE Class$=5052

GO

---------------------------------------------------------------------------------------------------------------------------------------------

-- REVIEW (SteveMiller): UpdateClassView$ really ought to
-- be called by the Field$ trigger. See FDB-87.

--( For 200038To200039
EXEC UpdateClassView$ 5117	--( MoUnclassifiedAffixMsa

--( For 200039To200040
EXEC UpdateClassView$ 6001  --( LanguageProject
EXEC UpdateClassView$ 5005  --( LexicalDatabase
EXEC UpdateClassView$ 5038	--( MoInflectionalAffixMsa
EXEC UpdateClassView$ 5053	--( ReversalIndexEntry

GO

-------------------------------------------------------------------------------

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200041
begin
	update Version$ set DbVer = 200042
	COMMIT TRANSACTION
	print 'database updated to version 200041'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200041 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
