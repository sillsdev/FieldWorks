-- Update database from version 200056 to 200057
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

DECLARE @Id as NVARCHAR(4000), @Term as NVARCHAR(4000), @ws as integer
DECLARE @MasterData TABLE (Id NVARCHAR(4000), Term NVARCHAR(4000))

-- Add all current items from the master list.
INSERT INTO @MasterData (Id, Term) VALUES ('PartOfSpeechValue', 'PartOfSpeechValue')
INSERT INTO @MasterData (Id, Term) VALUES ('adjective', 'adjective')
INSERT INTO @MasterData (Id, Term) VALUES ('Adjectivalization', 'Adjectivalization')
INSERT INTO @MasterData (Id, Term) VALUES ('participle', 'participle')
INSERT INTO @MasterData (Id, Term) VALUES ('prenoun', 'prenoun')
INSERT INTO @MasterData (Id, Term) VALUES ('preverb', 'preverb')
INSERT INTO @MasterData (Id, Term) VALUES ('adverb', 'Adverb')
INSERT INTO @MasterData (Id, Term) VALUES ('interjection', 'interjection')
INSERT INTO @MasterData (Id, Term) VALUES ('Adposition', 'Adposition')
INSERT INTO @MasterData (Id, Term) VALUES ('postposition', 'postposition')
INSERT INTO @MasterData (Id, Term) VALUES ('preposition', 'preposition')
INSERT INTO @MasterData (Id, Term) VALUES ('Connective', 'Connective')
INSERT INTO @MasterData (Id, Term) VALUES ('CoordinatingConnective', 'CoordinatingConnective')
INSERT INTO @MasterData (Id, Term) VALUES ('correlativeConnective', 'correlativeConnective')
INSERT INTO @MasterData (Id, Term) VALUES ('SubordinatingConnective', 'SubordinatingConnective')
INSERT INTO @MasterData (Id, Term) VALUES ('adverbializer', 'adverbializer')
INSERT INTO @MasterData (Id, Term) VALUES ('complementizer', 'complementizer')
INSERT INTO @MasterData (Id, Term) VALUES ('relativizer', 'relativizer')
INSERT INTO @MasterData (Id, Term) VALUES ('Determiner', 'Determiner')
INSERT INTO @MasterData (Id, Term) VALUES ('demonstrative', 'demonstrative')
INSERT INTO @MasterData (Id, Term) VALUES ('Article', 'article')
INSERT INTO @MasterData (Id, Term) VALUES ('definiteArticle', 'definiteArticle')
INSERT INTO @MasterData (Id, Term) VALUES ('indefiniteArticle', 'indefiniteArticle')
INSERT INTO @MasterData (Id, Term) VALUES ('Quantifier', 'Quantifier')
INSERT INTO @MasterData (Id, Term) VALUES ('Numeral', 'Numeral')
INSERT INTO @MasterData (Id, Term) VALUES ('cardinalNumeral', 'cardinalNumeral')
INSERT INTO @MasterData (Id, Term) VALUES ('distributiveNumeral', 'distributiveNumeral')
INSERT INTO @MasterData (Id, Term) VALUES ('multiplicativeNumeral', 'multiplicativeNumeral')
INSERT INTO @MasterData (Id, Term) VALUES ('ordinalNumeral', 'ordinalNumeral')
INSERT INTO @MasterData (Id, Term) VALUES ('partitiveNumeral', 'partitiveNumeral')
INSERT INTO @MasterData (Id, Term) VALUES ('Noun', 'noun')
INSERT INTO @MasterData (Id, Term) VALUES ('substantive', 'substantive')
INSERT INTO @MasterData (Id, Term) VALUES ('Nominal', 'Nominal')
INSERT INTO @MasterData (Id, Term) VALUES ('gerund', 'gerund')
INSERT INTO @MasterData (Id, Term) VALUES ('Particle', 'Particle')
INSERT INTO @MasterData (Id, Term) VALUES ('verbalParticle', 'verbalParticle')
INSERT INTO @MasterData (Id, Term) VALUES ('questionParticle', 'questionParticle')
INSERT INTO @MasterData (Id, Term) VALUES ('NominalParticle', 'NominalParticle')
INSERT INTO @MasterData (Id, Term) VALUES ('classifier', 'classifier')
INSERT INTO @MasterData (Id, Term) VALUES ('ProForm', 'ProForm')
INSERT INTO @MasterData (Id, Term) VALUES ('expletive', 'expletive')
INSERT INTO @MasterData (Id, Term) VALUES ('existentialMarker', 'existentialMarker')
INSERT INTO @MasterData (Id, Term) VALUES ('interrogativeProForm', 'interrogativeProForm')
INSERT INTO @MasterData (Id, Term) VALUES ('proAdjective', 'proAdjective')
INSERT INTO @MasterData (Id, Term) VALUES ('Pronoun', 'Pronoun')
INSERT INTO @MasterData (Id, Term) VALUES ('indefinitePronoun', 'indefinitePronoun')
INSERT INTO @MasterData (Id, Term) VALUES ('reciprocalPronoun', 'reciprocalPronoun')
INSERT INTO @MasterData (Id, Term) VALUES ('relativePronoun', 'relativePronoun')
INSERT INTO @MasterData (Id, Term) VALUES ('PersonalPronoun', 'PersonalPronoun')
INSERT INTO @MasterData (Id, Term) VALUES ('emphaticPronoun', 'emphaticPronoun')
INSERT INTO @MasterData (Id, Term) VALUES ('possessivePronoun', 'possessivePronoun')
INSERT INTO @MasterData (Id, Term) VALUES ('reflexivePronoun', 'reflexivePronoun')
INSERT INTO @MasterData (Id, Term) VALUES ('Verb', 'Verb')
INSERT INTO @MasterData (Id, Term) VALUES ('ditransitiveVerb', 'ditransitiveVerb')
INSERT INTO @MasterData (Id, Term) VALUES ('intransitiveVerb', 'intransitiveVerb')
INSERT INTO @MasterData (Id, Term) VALUES ('transitiveVerb', 'transitiveVerb')

SELECT @ws=id
FROM LgWritingSystem
WHERE ICULocale='en'

DECLARE grpCursor CURSOR local static forward_only read_only FOR
	SELECT Id, Term
	FROM @MasterData

OPEN grpCursor
FETCH NEXT FROM grpCursor INTO @Id, @Term
WHILE @@FETCH_STATUS = 0
BEGIN
	DECLARE @posID int, @Txt as NVARCHAR(4000)

	SELECT TOP 1 @posID=pos.Id, @Txt=nme.Txt
	FROM PartOfSpeech pos
	JOIN CmPossibility_Name nme
		ON nme.Obj=pos.Id
	WHERE CatalogSourceId is null
		AND nme.Ws=@ws
		AND LOWER(REPLACE(RTRIM(LTRIM(nme.Txt)),' ', '')) = LOWER(REPLACE(RTRIM(LTRIM(@Term)),' ', ''))

	IF @posID is not null BEGIN
		UPDATE PartOfSpeech
		SET CatalogSourceId=@Id
		WHERE Id=@posID
	END

	-- Must reset them, or following calls will use old id and text.
	SET @posID = null
	SET @Txt = null

	FETCH NEXT FROM grpCursor INTO @Id, @Term
END
CLOSE grpCursor
DEALLOCATE grpCursor

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200056
begin
	update Version$ set DbVer = 200057
	COMMIT TRANSACTION
	print 'database updated to version 200057'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200056 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO