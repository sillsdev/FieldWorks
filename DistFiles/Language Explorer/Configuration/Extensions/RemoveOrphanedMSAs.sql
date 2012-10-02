----Name: Delete unused Grammatical Info
----What: Occasionally, extra Grammatical Info is left behind in an entry even though no sense in the entry is using it. If this happens you will see extra, unused items in the Grammatical Info Details section of some entries. These unused Grammatical Info items can cause the parser to generate many extra analyses of words. This utility deletes these unused Grammatical Info items, and deletes any parser-generated analyses which use them.
----When: Use this utility if you see unused Grammatical Info items in lexical entries. (Unused Grammatical Info items are not used by any sense in the entry.)
----Caution: Back up your project before running this utility. You cannot use Undo to undo the effects of this utility. You should restart Language Explorer when the utility has finished running. In some rare cases when the project contains a huge number of these unused items, the process may take an hour or longer to run. There will be no indication of progress while the process is running.
BEGIN TRANSACTION
-- Remove orphaned MSAs (MSAs owned in an entry without any senses referring to them) and
-- fix or remove associated objects based on this algorithm.
	-- Delete CmAgentEvaluations referencing WfiAnalysis we're going to delete.
	-- Delete WfiAnalysis if there is no human opinion (no Human CmAgentEvaluations)
	-- and it uses an orphaned MSA
	-- Delete any remaining WfiMorphBundles referencing an orphaned MSA if sense is missing
	-- For any remaining WfiMorphBundles referencing an orphaned MSA,
	-- set the Msa to the Msa of the sense
	-- Delete any MoMorphAdhocProhib referring to the orphaned MSA
	-- Delete all orphaned MSAs
-- Note this could take a long time (half hour or more) in some situations, so wait patiently.

-- table variable to hold orphaned MSAs
declare @orphans table (
	id int primary key
	)
insert into @orphans (id)
	select msa.id from MoMorphSynAnalysis_ msa
		left outer join LexSense sen on sen.MorphoSyntaxAnalysis = msa.id
		where sen.MorphoSyntaxAnalysis is null and msa.OwnFlid$ = 5002009

declare @count int, @id int, @StrId varchar(20)
select @count = count(id) from @orphans

if @count > 0 begin

	declare @human int
	select @human = id from CmAgent_ where Guid$ = '9303883A-AD5C-4CCF-97A5-4ADD391F8DCB'

	-- table variable to hold WfiAnalysis if there is no human opinion (no Human CmAgentEvaluations)
	-- and it uses an orphaned MSA
	declare @analysesToDelete table (
		id int primary key
		)
	insert into @analysesToDelete (id)
		select distinct wa.id from WfiMorphBundle_ mb
			join WfiAnalysis wa on wa.id = mb.owner$
			left outer join CmAgentEvaluation_ aeh on aeh.target = wa.id and aeh.owner$ = @human
			join @orphans os on os.id = mb.Msa
			where aeh.id is null

	-- Delete CmAgentEvaluations referencing WfiAnalysis we're going to delete.
	DECLARE deleteEvaluations CURSOR FOR
	select distinct ae.id from CmAgentEvaluation ae
		join @analysesToDelete ad on ad.id = ae.target
	OPEN deleteEvaluations
	FETCH NEXT FROM deleteEvaluations INTO @id
	WHILE @@FETCH_STATUS = 0
	BEGIN
		SET @StrId = CONVERT(NVARCHAR(20), @id);
		EXEC DeleteObjects @StrId
		FETCH NEXT FROM deleteEvaluations INTO @id
	END
	CLOSE deleteEvaluations
	DEALLOCATE deleteEvaluations

	-- Delete WfiAnalysis if there is no human opinion (no Human CmAgentEvaluations)
	-- and it uses an orphaned MSA
	DECLARE deleteAnalyses CURSOR FOR
		select id from @analysesToDelete
	OPEN deleteAnalyses
	FETCH NEXT FROM deleteAnalyses INTO @id
	WHILE @@FETCH_STATUS = 0
	BEGIN
		SET @StrId = CONVERT(NVARCHAR(20), @id);
		EXEC DeleteObjects @StrId
		FETCH NEXT FROM deleteAnalyses INTO @id
	END
	CLOSE deleteAnalyses
	DEALLOCATE deleteAnalyses

	-- Delete any remaining WfiMorphBundles referencing an orphaned MSA if sense is missing
	DECLARE deleteBundles CURSOR FOR
	select distinct mb.id from WfiMorphBundle mb
		join @orphans os on os.id = mb.msa
		where mb.sense is null
	OPEN deleteBundles
	FETCH NEXT FROM deleteBundles INTO @id
	WHILE @@FETCH_STATUS = 0
	BEGIN
		SET @StrId = CONVERT(NVARCHAR(20), @id);
		EXEC DeleteObjects @StrId
		FETCH NEXT FROM deleteBundles INTO @id
	END
	CLOSE deleteBundles
	DEALLOCATE deleteBundles

	-- For any remaining WfiMorphBundles referencing an orphaned MSA,
	-- set the Msa to the Msa of the sense
	update WfiMorphBundle set msa = sen.MorphoSyntaxAnalysis
		from WfiMorphBundle mb
		join @orphans os on os.id = mb.msa
		join LexSense sen on sen.id = mb.sense

	-- Delete any MoMorphAdhocProhib referring to the orphaned MSA
	DECLARE deleteProhibs CURSOR FOR
	select distinct map.id from MoMorphAdhocProhib map
		left outer join MoMorphAdhocProhib_Morphemes mapm on mapm.src = map.id
		join @orphans os on os.id = mapm.dst
	OPEN deleteProhibs
	FETCH NEXT FROM deleteProhibs INTO @id
	WHILE @@FETCH_STATUS = 0
	BEGIN
		SET @StrId = CONVERT(NVARCHAR(20), @id);
		EXEC DeleteObjects @StrId
		FETCH NEXT FROM deleteProhibs INTO @id
	END
	CLOSE deleteProhibs
	DEALLOCATE deleteProhibs

	-- Delete all orphaned MSAs
	DECLARE deleteMSAs CURSOR FOR
	select id from @orphans
	OPEN deleteMSAs
	FETCH NEXT FROM deleteMSAs INTO @id
	WHILE @@FETCH_STATUS = 0
	BEGIN
		SET @StrId = CONVERT(NVARCHAR(20), @id);
		EXEC DeleteObjects @StrId
		FETCH NEXT FROM deleteMSAs INTO @id
	END
	CLOSE deleteMSAs
	DEALLOCATE deleteMSAs

end

COMMIT
