-- update database FROM version 200143 to 200143
BEGIN TRANSACTION  --( will be rolled back if wrong version#)
-- get a table containing the senses that need fixing, their (possibly indirectly) owning lex entries.
-- also has a slot, currently null, for the MSA we will set the sense to point to.
declare @tblInfo table (ls int, le int, msa int)
insert into @tblinfo select id, owner$, null from LexSense_ where MorphoSyntaxAnalysis is null
while @@rowcount > 0 begin
	-- as long as any of the 'lex entry' values is really a lex sense, replace it with its owner.
	update @tblInfo
	set le = item.owner
	from  (select ls.id as sense, ls.owner$ as owner from @tblInfo ti join LexSense_ ls on ls.id = ti.le) as item
	where le = item.sense
end
-- get rid of any remaining rows where owning 'entry' is not an entry. Unlikely, but play safe.
delete from @tblInfo where le in (select ti.le from @tblInfo ti left outer join LexEntry le1 on le1.id = ti.le where le1.id is null)
-- Fill in MSA for entries that have an unspecified stem MSA.
update @tblInfo set msa = item.msa
from (select le.id entry, msa.id msa
	from LexEntry le
	join MoStemMsa_ msa on le.id = msa.Owner$ and msa.PartOfSpeech is null
) item
where le = item.entry
-- Fill in MSA for entries that have an unspecified affix MSA.
update @tblInfo set msa = item.msa
from (select le.id entry, msa.id msa
	from LexEntry le
	join MoUnclassifiedAffixMsa_ msa on le.id = msa.Owner$ and msa.PartOfSpeech is null
) item
where le = item.entry

-- loop over entries that still need an unspecified affix MSA.
-- that is, ones that have a LexemeForm that is an affix and no current MSA.
DECLARE curInfo CURSOR LOCAL STATIC FORWARD_ONLY FOR
SELECT ti.le from @tblInfo ti
	join LexEntry le1 on ti.le = le1.id
	join MoAffixForm_ mf on mf.owner$ = le1.id and mf.OwnFlid$ = 5002029 -- LexemeForm
	where ti.msa is null
	group by ti.le


declare @entry int, @msa int, @uid uniqueidentifier
OPEN curInfo
FETCH curInfo INTO @entry
WHILE @@FETCH_STATUS = 0 BEGIN
	set @uid = null
	set @msa = null
	exec CreateOwnedObject$
		5117, -- MoUnclassifiedAffixMsa
		@msa output,
		null,
		@entry, -- owner
		5002009, -- kflidLexEntry_MorphoSyntaxAnalyses
		25, --kcptOwningCollection
		null,
		0,
		1,
		@uid output
	update @tblInfo set msa = @msa where le = @entry -- may update multiple rows
	FETCH curInfo INTO @entry
END
CLOSE curInfo
DEALLOCATE curInfo

-- loop over entries that need a stem MSA.
-- that is, the ones that don't yet have any MSA.
DECLARE curInfo2 CURSOR LOCAL STATIC FORWARD_ONLY FOR
SELECT ti.le from @tblInfo ti
	where ti.msa is null
	group by ti.le


OPEN curInfo2
FETCH curInfo2 INTO @entry
WHILE @@FETCH_STATUS = 0 BEGIN
	set @uid = null
	set @msa = null
	exec CreateOwnedObject$
		5001, -- MoStemMsa
		@msa output,
		null,
		@entry, -- owner
		5002009, -- kflidLexEntry_MorphoSyntaxAnalyses
		25, --kcptOwningCollection
		null,
		0,
		1,
		@uid output
	update @tblInfo set msa = @msa where le = @entry -- may update several rows
	FETCH curInfo2 INTO @entry
END
CLOSE curInfo2
DEALLOCATE curInfo2

update LexSense set MorphoSyntaxAnalysis = ti.msa
	from @tblInfo ti
	where id = ti.ls

--handy to see what changed if you use this for anything else.
--select * from @tblInfo ti join LexEntry le1 on ti.le = le1.id
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200143
BEGIN
	UPDATE [Version$] SET [DbVer] = 200144
	COMMIT TRANSACTION
	PRINT 'database updated to version 200144'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200143 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
