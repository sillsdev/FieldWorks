-- Update database from version 200201 to 200202
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Changing the basic morph type of a LexEntry should also change the MSA subclass if the change
-- is from a stem type to an affix type, or vice versa.  This was not happening in our program
-- code until recently, so we need to ensure that older databases are fixed up properly.
-- (See comments on LT-7304.)

DECLARE @tblBadMsaInfo TABLE (
	EntryId INT,
	MorphClass INT,
	MsaId INT,
	PartOfSpeech INT
	)
INSERT INTO @tblBadMsaInfo
SELECT DISTINCT le.Id, mf.Class$, msa.Id, ISNULL(stem.PartOfSpeech, ISNULL(infl.PartOfSpeech, ISNULL(step.PartOfSpeech, ISNULL(derv.ToPartOfSpeech, uncl.PartOfSpeech))))
FROM LexEntry le
JOIN LexEntry_LexemeForm lelf ON lelf.Src=le.Id
JOIN MoForm_ mf ON mf.Id=lelf.Dst
JOIN LexEntry_MorphoSyntaxAnalyses lemsa ON lemsa.Src=le.Id
JOIN MoMorphoSyntaxAnalysis_ msa ON msa.Id=lemsa.Dst
JOIN LexSense ls ON ls.MorphoSyntaxAnalysis=lemsa.Dst
LEFT OUTER JOIN MoStemMsa stem ON stem.Id=lemsa.Dst
LEFT OUTER JOIN MoInflectionalAffixMsa infl ON infl.Id=lemsa.Dst
LEFT OUTER JOIN MoDerivationalStepMsa step ON step.Id=lemsa.Dst
LEFT OUTER JOIN MoDerivationalAffixMsa derv ON derv.Id=lemsa.Dst
LEFT OUTER JOIN MoUnclassifiedAffixMsa uncl ON uncl.Id=lemsa.Dst
WHERE (mf.Class$ = 5045 AND msa.Class$ != 5001) OR (msa.Class$ = 5001 AND mf.Class$ != 5045)

DECLARE @entryId INT, @morphClass INT, @msaId INT, @partOfSpeech INT
DECLARE @newMsaClass INT, @newMsaId INT, @newMsaGuid UNIQUEIDENTIFIER

DECLARE @cur CURSOR
SET @cur = CURSOR FAST_FORWARD FOR
	SELECT EntryId, MorphClass, MsaId, PartOfSpeech FROM @tblBadMsaInfo
OPEN @cur
FETCH NEXT FROM @cur INTO @entryId, @morphClass, @msaId, @partOfSpeech
WHILE @@FETCH_STATUS = 0
BEGIN
	-- Set the desired MSA class, and try to find an appropriate MSA that already exists.
	IF @morphClass = 5045			-- MoStemAllomorph
	BEGIN
		SET @newMsaClass = 5001		-- MoStemMsa
		if @partOfSpeech IS NULL
		BEGIN
			SELECT @newMsaId = msa.Id
			FROM LexEntry_MorphoSyntaxAnalyses lems
			JOIN MoStemMsa msa ON msa.Id=lems.Dst AND msa.PartOfSpeech IS NULL AND msa.InflectionClass IS NULL AND msa.Stratum IS NULL
			LEFT OUTER JOIN MoStemMsa_FromPartsOfSpeech fpos ON fpos.Src=msa.Id
			LEFT OUTER JOIN MoStemMsa_ProductivityRestrictions pres ON pres.Src=msa.Id
			LEFT OUTER JOIN MoStemMsa_MsFeatures msf ON msf.Src=msa.Id
			WHERE lems.Src=@entryId AND fpos.Dst IS NULL AND pres.Dst IS NULL AND msf.Dst IS NULL
		END
		ELSE
		BEGIN
			SELECT @newMsaId = msa.Id
			FROM LexEntry_MorphoSyntaxAnalyses lems
			JOIN MoStemMsa msa ON msa.Id=lems.Dst AND msa.PartOfSpeech=@partOfSpeech AND msa.InflectionClass IS NULL AND msa.Stratum IS NULL
			LEFT OUTER JOIN MoStemMsa_FromPartsOfSpeech fpos ON fpos.Src=msa.Id
			LEFT OUTER JOIN MoStemMsa_ProductivityRestrictions pres ON pres.Src=msa.Id
			LEFT OUTER JOIN MoStemMsa_MsFeatures msf ON msf.Src=msa.Id
			WHERE lems.Src=@entryId AND fpos.Dst IS NULL AND pres.Dst IS NULL AND msf.Dst IS NULL
		END
	END
	ELSE
	BEGIN
		SET @newMsaClass = 5117		-- MoUnclassifiedAffixMsa
		if @partOfSpeech IS NULL
		BEGIN
			SELECT @newMsaId = msa.Id
			FROM LexEntry_MorphoSyntaxAnalyses lems
			JOIN MoUnclassifiedAffixMsa msa ON msa.Id=lems.Dst AND msa.PartOfSpeech IS NULL
			WHERE lems.Src=@entryId
		END
		ELSE
		BEGIN
			SELECT @newMsaId = msa.Id
			FROM LexEntry_MorphoSyntaxAnalyses lems
			JOIN MoUnclassifiedAffixMsa msa ON msa.Id=lems.Dst AND msa.PartOfSpeech=@partOfSpeech
			WHERE lems.Src=@entryId
		END
	END

	IF @newMsaId IS NULL
	BEGIN
		SET @newMsaGuid = null
		EXEC CreateOwnedObject$
			@newMsaClass,		-- clid
			@newMsaId OUTPUT,	-- id
			null,				-- guid
			@entryId,			-- owner
			5002009,			-- ownFlid (LexEntry_MorphoSyntaxAnalyses)
			25,					-- type (owning collection)
			null,
			0,
			1,
			@newMsaGuid OUTPUT
		IF @newMsaClass = 5117
			UPDATE MoUnclassifiedAffixMsa SET PartOfSpeech=@partOfSpeech WHERE Id=@newMsaId
		ELSE
			UPDATE MoStemMsa SET PartOfSpeech=@partOfSpeech WHERE Id=@newMsaId
	END

	UPDATE LexSense SET MorphoSyntaxAnalysis=@newMsaId WHERE MorphoSyntaxAnalysis=@msaId
	UPDATE WfiMorphBundle SET Msa=@newMsaId WHERE Msa=@msaId
	UPDATE MoMorphemeAdhocCoProhibition SET FirstMorpheme=@newMsaId WHERE FirstMorpheme=@msaId
	UPDATE MoMorphemeAdhocCoProhibition_Morphemes SET Dst=@newMsaId WHERE Dst=@msaId
	UPDATE MoMorphemeAdhocCoProhibition_RestOfMorphemes SET Dst=@newMsaId WHERE Dst=@msaId
	UPDATE MoMorphoSyntaxAnalysis_Components SET Dst=@newMsaId WHERE Dst=@msaId
	EXEC DeleteObj$ @msaId, null

	FETCH NEXT FROM @cur INTO @entryId, @morphClass, @msaId, @partOfSpeech
END
CLOSE @cur
DEALLOCATE @cur
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200201
BEGIN
	UPDATE Version$ SET DbVer = 200202
	COMMIT TRANSACTION
	PRINT 'database updated to version 200202'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200201 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
