-- Update database from version 200059 to 200060
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

UPDATE PartOfSpeech
SET CatalogSourceId=null
FROM PartOfSpeech
WHERE CatalogSourceId='Adjectivalization'

UPDATE PartOfSpeech
SET CatalogSourceId=null
FROM PartOfSpeech
WHERE CatalogSourceId='participle'

UPDATE PartOfSpeech
SET CatalogSourceId='Adjective'
FROM PartOfSpeech
WHERE CatalogSourceId='adjective'

UPDATE PartOfSpeech
SET CatalogSourceId='Adverb'
FROM PartOfSpeech
WHERE CatalogSourceId='adverb'

UPDATE PartOfSpeech
SET CatalogSourceId='Adverbializer'
FROM PartOfSpeech
WHERE CatalogSourceId='adverbializer'

UPDATE PartOfSpeech
SET CatalogSourceId='CardinalNumeral'
FROM PartOfSpeech
WHERE CatalogSourceId='cardinalNumeral'

UPDATE PartOfSpeech
SET CatalogSourceId='Classifier'
FROM PartOfSpeech
WHERE CatalogSourceId='classifier'

UPDATE PartOfSpeech
SET CatalogSourceId='Complementizer'
FROM PartOfSpeech
WHERE CatalogSourceId='complementizer'

UPDATE PartOfSpeech
SET CatalogSourceId='CorrelativeConnective'
FROM PartOfSpeech
WHERE CatalogSourceId='correlativeConnective'

UPDATE PartOfSpeech
SET CatalogSourceId='DefiniteArticle'
FROM PartOfSpeech
WHERE CatalogSourceId='definiteArticle'

UPDATE PartOfSpeech
SET CatalogSourceId='Demonstrative'
FROM PartOfSpeech
WHERE CatalogSourceId='demonstrative'

UPDATE PartOfSpeech
SET CatalogSourceId='DistributiveNumeral'
FROM PartOfSpeech
WHERE CatalogSourceId='distributiveNumeral'

UPDATE PartOfSpeech
SET CatalogSourceId='DitransitiveVerb'
FROM PartOfSpeech
WHERE CatalogSourceId='ditransitiveVerb'

UPDATE PartOfSpeech
SET CatalogSourceId='EmphaticPronoun'
FROM PartOfSpeech
WHERE CatalogSourceId='emphaticPronoun'

UPDATE PartOfSpeech
SET CatalogSourceId='ExistentialMarker'
FROM PartOfSpeech
WHERE CatalogSourceId='existentialMarker'

UPDATE PartOfSpeech
SET CatalogSourceId='Expletive'
FROM PartOfSpeech
WHERE CatalogSourceId='expletive'

UPDATE PartOfSpeech
SET CatalogSourceId='Gerund'
FROM PartOfSpeech
WHERE CatalogSourceId='gerund'

UPDATE PartOfSpeech
SET CatalogSourceId='IndefiniteArticle'
FROM PartOfSpeech
WHERE CatalogSourceId='indefiniteArticle'

UPDATE PartOfSpeech
SET CatalogSourceId='IndefinitePronoun'
FROM PartOfSpeech
WHERE CatalogSourceId='indefinitePronoun'

UPDATE PartOfSpeech
SET CatalogSourceId='Interjection'
FROM PartOfSpeech
WHERE CatalogSourceId='interjection'

UPDATE PartOfSpeech
SET CatalogSourceId='InterrogativeProform'
FROM PartOfSpeech
WHERE CatalogSourceId='interrogativeProForm'

UPDATE PartOfSpeech
SET CatalogSourceId='IntransitiveVerb'
FROM PartOfSpeech
WHERE CatalogSourceId='intransitiveVerb'

UPDATE PartOfSpeech
SET CatalogSourceId='MultiplicativeNumeral'
FROM PartOfSpeech
WHERE CatalogSourceId='multiplicativeNumeral'

UPDATE PartOfSpeech
SET CatalogSourceId='OrdinalNumeral'
FROM PartOfSpeech
WHERE CatalogSourceId='ordinalNumeral'

UPDATE PartOfSpeech
SET CatalogSourceId='PartitiveNumeral'
FROM PartOfSpeech
WHERE CatalogSourceId='partitiveNumeral'

UPDATE PartOfSpeech
SET CatalogSourceId='PossessivePronoun'
FROM PartOfSpeech
WHERE CatalogSourceId='possessivePronoun'

UPDATE PartOfSpeech
SET CatalogSourceId='Postposition'
FROM PartOfSpeech
WHERE CatalogSourceId='postposition'

UPDATE PartOfSpeech
SET CatalogSourceId='Prenoun'
FROM PartOfSpeech
WHERE CatalogSourceId='prenoun'

UPDATE PartOfSpeech
SET CatalogSourceId='Preposition'
FROM PartOfSpeech
WHERE CatalogSourceId='preposition'

UPDATE PartOfSpeech
SET CatalogSourceId='Preverb'
FROM PartOfSpeech
WHERE CatalogSourceId='preverb'

UPDATE PartOfSpeech
SET CatalogSourceId='Proadjective'
FROM PartOfSpeech
WHERE CatalogSourceId='proAdjective'

UPDATE PartOfSpeech
SET CatalogSourceId='Proadverb'
FROM PartOfSpeech
WHERE CatalogSourceId='proAdverb'

UPDATE PartOfSpeech
SET CatalogSourceId='QuestionParticle'
FROM PartOfSpeech
WHERE CatalogSourceId='questionParticle'

UPDATE PartOfSpeech
SET CatalogSourceId='ReciprocalPronoun'
FROM PartOfSpeech
WHERE CatalogSourceId='reciprocalPronoun'

UPDATE PartOfSpeech
SET CatalogSourceId='ReflexivePronoun'
FROM PartOfSpeech
WHERE CatalogSourceId='reflexivePronoun'

UPDATE PartOfSpeech
SET CatalogSourceId='RelativePronoun'
FROM PartOfSpeech
WHERE CatalogSourceId='relativePronoun'

UPDATE PartOfSpeech
SET CatalogSourceId='Relativizer'
FROM PartOfSpeech
WHERE CatalogSourceId='relativizer'

UPDATE PartOfSpeech
SET CatalogSourceId='Substantive'
FROM PartOfSpeech
WHERE CatalogSourceId='substantive'

UPDATE PartOfSpeech
SET CatalogSourceId='TransitiveVerb'
FROM PartOfSpeech
WHERE CatalogSourceId='transitiveVerb'

UPDATE PartOfSpeech
SET CatalogSourceId='VerbalParticle'
FROM PartOfSpeech
WHERE CatalogSourceId='verbalParticle'

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200059
begin
	update Version$ set DbVer = 200060
	COMMIT TRANSACTION
	print 'database updated to version 200060'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200059 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
