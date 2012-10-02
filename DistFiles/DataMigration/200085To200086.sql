-- Update database from version 200085 to 200086
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

DECLARE @wsEn int, @hvoEntryType int, @objId int, @fmtEn varbinary(4000)
DECLARE @hvoST int, @guid uniqueidentifier, @hvoPara int, @txt nvarchar(4000)
DECLARE @cPara int
DECLARE @Err int
SET @Err = 0

-- 200083To200084.sql properly handled adding the Description information for the Entry Types.
-- However, the discussion was not added if an empty (probably NULL) paragraph already
-- existed for the Discussion field.  We detect that condition here, and add the desired
-- information to the null paragraph, and create additional paragraphs if the discussion
-- so requires.

SELECT @wsEn=Id FROM LgWritingSystem WHERE ICULocale=N'en'
SELECT TOP 1 @fmtEn=Fmt FROM MultiStr$ WHERE Ws=@wsEn ORDER BY Fmt
IF @wsEn is null OR @fmtEn is null goto LFail

-- Compound
SELECT @hvoEntryType=Id FROM CmObject WHERE Guid$='1F6AE209-141A-40DB-983C-BEE93AF0CA3C'
IF @hvoEntryType is not null BEGIN
	SELECT TOP 1 @hvoST=Dst FROM CmPossibility_Discussion WHERE Src=@hvoEntryType
	SELECT @cPara=COUNT(Dst) FROM StText_Paragraphs WHERE Src = @hvoST
	IF @cPara = 1 BEGIN
		SELECT TOP 1 @hvoPara=Dst FROM StText_Paragraphs WHERE Src = @hvoST ORDER BY Ord
		SELECT @txt=p.Contents
		FROM StText_Paragraphs paras
		JOIN StTxtPara p ON p.Id=paras.Dst
		WHERE paras.Src=@hvoST
		IF @txt is null OR @txt = N'' BEGIN
			UPDATE StTxtPara
			SET Contents=N'Example (English)', Contents_Fmt=@fmtEn
			WHERE Id=@hvoPara
			SET @txt=N'Blackboard contains a stem that refers to "a large, smooth, usually dark surface on which to write or draw with chalk". However, the stem is made up of two roots, black and board.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
		END
	END
	SET @hvoST=null		-- These need to be cleared for some reason.  Otherwise, later discussions are not created.
	SET @hvoPara=null
END


-- Derivation
SELECT @hvoEntryType=Id FROM CmObject WHERE Guid$='98C273C4-F723-4FB0-80DF-EEDE2204DFCA'
IF @hvoEntryType is not null BEGIN
	SELECT TOP 1 @hvoST=Dst FROM CmPossibility_Discussion WHERE Src=@hvoEntryType
	SELECT @cPara=COUNT(Dst) FROM StText_Paragraphs WHERE Src = @hvoST
	IF @cPara = 1 BEGIN
		SELECT TOP 1 @hvoPara=Dst FROM StText_Paragraphs WHERE Src = @hvoST ORDER BY Ord
		SELECT @txt=p.Contents
		FROM StText_Paragraphs paras
		JOIN StTxtPara p ON p.Id=paras.Dst
		WHERE paras.Src=@hvoST
		IF @txt is null OR @txt = N'' BEGIN
			UPDATE StTxtPara
			SET Contents=N'The derived word is often of a different word class from the original.  It may thus take the inflectional affixes of the new word class.  In contrast to inflection, derivation', Contents_Fmt=@fmtEn
			WHERE Id=@hvoPara
			SET @txt=N'        is not obligatory'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        typically produces a greater change of meaning from the original form'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        is more likely to result in a form which has a somewhat idiosyncratic meaning, and'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        often changes the grammatical category of a root.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'Derivational operations'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        tend to be idiosyncratic and non-productive'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        do not occur in well-defined ''paradigms,'' and'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        are ''optional'' insofar as they'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                shape the basic semantic content of roots and'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                are not governed by some other syntactic operation or element.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'Examples (English)'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        Kindness is derived from kind.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        Joyful is derived from joy.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        Amazement is derived from amaze.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        Speaker is derived from speak.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        National is derived from nation.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'Kinds'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'Here are some kinds of derivational operations:'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        Operations that change the grammatical category of a root'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                Example: Nominalization (English)'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                Verbs and adjectives can be turned into nouns: amaze > amazement, speak > speaker, perform > performance, soft > softness, warm > warmth'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        Operations that change the valence (transitivity) of a root'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                Example: Causation (Swahili)'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                kula ''to eat'' > kulisha, ''to feed'''
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
		END
	END
	-- We had one other problem with 200083To200084.sql: I forgot the final
	-- EXEC CreateObject_StTxtPara after the final SET @txt.  I fixed it, but
	-- some databases may have slipped through.
	ELSE IF @cPara = 24 BEGIN
		SELECT TOP 1 @hvoPara=Dst FROM StText_Paragraphs WHERE Src = @hvoST ORDER BY Ord DESC
		SELECT @txt=p.Contents
		FROM StText_Paragraphs paras
		JOIN StTxtPara p ON p.Id=paras.Dst
		WHERE paras.Src=@hvoST
		IF @txt = N'                Example: Causation (Swahili)' BEGIN
			SET @txt=N'                kula ''to eat'' > kulisha, ''to feed'''
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
		END
	END
	SET @hvoST=null		-- These need to be cleared for some reason.  Otherwise, later discussions are not created.
	SET @hvoPara=null
END

-- Dialectal Variant
SELECT @hvoEntryType=Id FROM CmObject WHERE Guid$='024B62C9-93B3-41A0-AB19-587A0030219A'
IF @hvoEntryType is not null BEGIN
	SELECT TOP 1 @hvoST=Dst FROM CmPossibility_Discussion WHERE Src=@hvoEntryType
	SELECT @cPara=COUNT(Dst) FROM StText_Paragraphs WHERE Src = @hvoST
	IF @cPara = 1 BEGIN
		SELECT TOP 1 @hvoPara=Dst FROM StText_Paragraphs WHERE Src = @hvoST ORDER BY Ord
		SELECT @txt=p.Contents
		FROM StText_Paragraphs paras
		JOIN StTxtPara p ON p.Id=paras.Dst
		WHERE paras.Src=@hvoST
		IF @txt is null OR @txt = N'' BEGIN
			UPDATE StTxtPara
			SET Contents=N'A dialectal variant is a minor entry in the lexical database that is related to a major entry. It contains minimal phonological, semantic, and grammatical information about the variant.', Contents_Fmt=@fmtEn
			WHERE Id=@hvoPara
		END
	END
	SET @hvoST=null		-- These need to be cleared for some reason.  Otherwise, later discussions are not created.
	SET @hvoPara=null
END

-- Free Variant
SELECT @hvoEntryType=Id FROM CmObject WHERE Guid$='4343B1EF-B54F-4FA4-9998-271319A6D74C'
IF @hvoEntryType is not null BEGIN
	SELECT TOP 1 @hvoST=Dst FROM CmPossibility_Discussion WHERE Src=@hvoEntryType
	SELECT @cPara=COUNT(Dst) FROM StText_Paragraphs WHERE Src = @hvoST
	IF @cPara = 1 BEGIN
		SELECT TOP 1 @hvoPara=Dst FROM StText_Paragraphs WHERE Src = @hvoST ORDER BY Ord
		SELECT @txt=p.Contents
		FROM StText_Paragraphs paras
		JOIN StTxtPara p ON p.Id=paras.Dst
		WHERE paras.Src=@hvoST
		IF @txt is null OR @txt = N'' BEGIN
			UPDATE StTxtPara
			SET Contents=N'A free variant is a minor entry in the lexical database that is related to a major entry. It contains minimal phonological, semantic, and grammatical information about the variant.', Contents_Fmt=@fmtEn
			WHERE Id=@hvoPara
		END
	END
	SET @hvoST=null		-- These need to be cleared for some reason.  Otherwise, later discussions are not created.
	SET @hvoPara=null
END

-- Idiom
SELECT @hvoEntryType=Id FROM CmObject WHERE Guid$='B2276DEC-B1A6-4D82-B121-FD114C009C59'
IF @hvoEntryType is not null BEGIN
	SELECT TOP 1 @hvoST=Dst FROM CmPossibility_Discussion WHERE Src=@hvoEntryType
	SELECT @cPara=COUNT(Dst) FROM StText_Paragraphs WHERE Src = @hvoST
	IF @cPara = 1 BEGIN
		SELECT TOP 1 @hvoPara=Dst FROM StText_Paragraphs WHERE Src = @hvoST ORDER BY Ord
		SELECT @txt=p.Contents
		FROM StText_Paragraphs paras
		JOIN StTxtPara p ON p.Id=paras.Dst
		WHERE paras.Src=@hvoST
		IF @txt is null OR @txt = N'' BEGIN
			UPDATE StTxtPara
			SET Contents=N'An idiom is a multiword expression. Individual components of an idiom can often be inflected in the same way individual words in a phrase can be inflected. This inflection usually follows the same pattern of inflection as the idiom''s literal counterpart.', Contents_Fmt=@fmtEn
			WHERE Id=@hvoPara
			SET @txt=N'        Example: have a bee in one''s bonnet'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                        He has bees in his bonnet.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'An idiom behaves as a single semantic unit.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        It tends to have some measure of internal cohesion such that it can often be replaced by a literal counterpart that is made up of a single word.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                Example: kick the bucket'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                                die'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        It resists interruption by other words whether they are semantically compatible or not.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                Example: pull one''s leg'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                                *pull hard on one''s leg'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                                *pull on one''s left leg'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        It resists reordering of its component parts.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                Example: let the cat out of the bag'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                                *the cat got let out of the bag'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'An idiom has a non-productive syntactic structure. Only single particular lexemes can collocate in an idiomatic construction. Substituting other words from the same generic lexical relation set will destroy the idiomatic meaning of the expression.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                Example: eat one''s words'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                                *eat one''s sentences'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                                ?swallow one''s words'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'An idiom often shows the following characteristics:'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        It is syntactically anomalous. It has an unusual grammatical structure.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                Example: by and large'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        It contains unique, fossilized items.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                Examples: to and fro - fro < from = away (Scottish)'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                                  cobweb - cob < cop = spider (Middle English)'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'Some linguists contend that compound words may qualify as idioms, while others maintain that an idiom must be more lexically complex.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'Idioms contrast with the following:'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        Metaphors satisfy the first requirement for an idiom, that their meaning be obscure, but not the second, that they not be productive.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                Examples: throw in the towel'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                                  throw in the sponge'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        Collocates may have restricted lexical possibilities or use archaic vocabulary such that they are not productive, but their meaning is not opaque.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                Examples: heavy drinking'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                                  mete out'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
		END
	END
	SET @hvoST=null		-- These need to be cleared for some reason.  Otherwise, later discussions are not created.
	SET @hvoPara=null
END

-- Inflectional Variant
SELECT @hvoEntryType=Id FROM CmObject WHERE Guid$='FCC61889-00E6-467B-9CF0-8C4F48B9A486'
IF @hvoEntryType is not null BEGIN
	SELECT TOP 1 @hvoST=Dst FROM CmPossibility_Discussion WHERE Src=@hvoEntryType
	SELECT @cPara=COUNT(Dst) FROM StText_Paragraphs WHERE Src = @hvoST
	IF @cPara = 1 BEGIN
		SELECT TOP 1 @hvoPara=Dst FROM StText_Paragraphs WHERE Src = @hvoST ORDER BY Ord
		SELECT @txt=p.Contents
		FROM StText_Paragraphs paras
		JOIN StTxtPara p ON p.Id=paras.Dst
		WHERE paras.Src=@hvoST
		IF @txt is null OR @txt = N'' BEGIN
			UPDATE StTxtPara
			SET Contents=N'An inflectional variant is a minor entry in the lexical database that is related to a major entry. It contains minimal phonological, semantic, and grammatical information about the variant.', Contents_Fmt=@fmtEn
			WHERE Id=@hvoPara
			SET @txt=N'In contrast to derivation, inflection'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        does not result in a change of word class, and'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        usually produces a predictable, non-idiosyncratic change of meaning.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'Inflectional operations ground the semantic content of a root according to place, time, and participant reference, without substantially affecting the basic semantic content of the root. They often specify when an event or situation took place, who or what were the participants, and sometimes where, how or whether an event or situation really took place. In other words, roots can be inflected for such things as:'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'         Agreement: person, number, and gender'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        Sequential, temporal or epistemological grounding: tense, aspect, mode'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'Inflectional operations'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        are grammatically required in certain syntactic environments'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'                Example: The main verb of an English sentence must be inflected for subject and tense.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        tend to be regular and productive, in comparison to derivational operations, and'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        tend to occur in paradigms.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'Example (English)'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        In the following English sentence, come is inflected for person and number by the suffix -s:'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        The mailman comes about noon.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'Example (Spanish)'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        In the following Spanish noun phrase, las and rojas are inflected for agreement with manzanas in grammatical gender by -a and in number by -s:'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        las manzanas rojas ''the red apples'''
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
		END
	END
	SET @hvoST=null		-- These need to be cleared for some reason.  Otherwise, later discussions are not created.
	SET @hvoPara=null
END

-- Keyterm Phrase
SELECT @hvoEntryType=Id FROM CmObject WHERE Guid$='CCE519D8-A9C5-4F28-9C7D-5370788BFBD5'
-- No discussion available.

-- Main Entry
SELECT @hvoEntryType=Id FROM CmObject WHERE Guid$='5541D063-2D43-4E49-AAAD-BBA4AE5ECCD1'
IF @hvoEntryType is not null BEGIN
	SELECT TOP 1 @hvoST=Dst FROM CmPossibility_Discussion WHERE Src=@hvoEntryType
	SELECT @cPara=COUNT(Dst) FROM StText_Paragraphs WHERE Src = @hvoST
	IF @cPara = 1 BEGIN
		SELECT TOP 1 @hvoPara=Dst FROM StText_Paragraphs WHERE Src = @hvoST ORDER BY Ord
		SELECT @txt=p.Contents
		FROM StText_Paragraphs paras
		JOIN StTxtPara p ON p.Id=paras.Dst
		WHERE paras.Src=@hvoST
		IF @txt is null OR @txt = N'' BEGIN
			UPDATE StTxtPara
			SET Contents=N'Insert the following types of lexemes as main entries:', Contents_Fmt=@fmtEn
			WHERE Id=@hvoPara
			SET @txt=N'        Morphemes'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        Letters of the alphabet'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        Loan words or phrases'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        Proper names'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
		END
	END
	SET @hvoST=null		-- These need to be cleared for some reason.  Otherwise, later discussions are not created.
	SET @hvoPara=null
END

-- Phrasal Verb
SELECT @hvoEntryType=Id FROM CmObject WHERE Guid$='35CEE792-74C8-444E-A9B7-ED0461D4D3B7'
IF @hvoEntryType is not null BEGIN
	SELECT TOP 1 @hvoST=Dst FROM CmPossibility_Discussion WHERE Src=@hvoEntryType
	SELECT @cPara=COUNT(Dst) FROM StText_Paragraphs WHERE Src = @hvoST
	IF @cPara = 1 BEGIN
		SELECT TOP 1 @hvoPara=Dst FROM StText_Paragraphs WHERE Src = @hvoST ORDER BY Ord
		SELECT @txt=p.Contents
		FROM StText_Paragraphs paras
		JOIN StTxtPara p ON p.Id=paras.Dst
		WHERE paras.Src=@hvoST
		IF @txt is null OR @txt = N'' BEGIN
			UPDATE StTxtPara
			SET Contents=N'Example (English)', Contents_Fmt=@fmtEn
			WHERE Id=@hvoPara
			SET @txt=N'         The item give up is a phrasal verb, as in the following:'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        He gave up smoking.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        He gave smoking up.'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'Example (Akan)'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt='        The item gyee ... so ''answered'' is a phrasal verb, as in the following:'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        Kofi gyee Kwame so'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt=N'        Kofi received Kwame on'
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
			SET @txt='        ''Kofi answered Kwame.'''
			EXEC CreateObject_StTxtPara null, null, null, null, @txt, @fmtEn, @hvoST, 14001, null, @hvoPara output, @guid output
		END
	END
	SET @hvoST=null		-- These need to be cleared for some reason.  Otherwise, later discussions are not created.
	SET @hvoPara=null
END

-- Saying
SELECT @hvoEntryType=Id FROM CmObject WHERE Guid$='9466D126-246E-400B-8BBA-0703E09BC567'
-- No discussion available

-- Spelling Variant
SELECT @hvoEntryType=Id FROM CmObject WHERE Guid$='0C4663B3-4D9A-47AF-B9A1-C8565D8112ED'
IF @hvoEntryType is not null BEGIN
	SELECT TOP 1 @hvoST=Dst FROM CmPossibility_Discussion WHERE Src=@hvoEntryType
	SELECT @cPara=COUNT(Dst) FROM StText_Paragraphs WHERE Src = @hvoST
	IF @cPara = 1 BEGIN
		SELECT TOP 1 @hvoPara=Dst FROM StText_Paragraphs WHERE Src = @hvoST ORDER BY Ord
		SELECT @txt=p.Contents
		FROM StText_Paragraphs paras
		JOIN StTxtPara p ON p.Id=paras.Dst
		WHERE paras.Src=@hvoST
		IF @txt is null OR @txt = N'' BEGIN
			UPDATE StTxtPara
			SET Contents=N'A spelling variant is a minor entry in the lexical database that is related to a major entry. It contains minimal phonological, semantic, and grammatical information about the variant.', Contents_Fmt=@fmtEn
			WHERE Id=@hvoPara
		END
	END
	SET @hvoST=null		-- These need to be cleared for some reason.  Otherwise, later discussions are not created.
	SET @hvoPara=null
END

GOTO LDone

LFail:
SET @Err = 1

LDone:

-- Test output
/*
SELECT disc.Src 'ObjId', n.Txt 'Name', paras.Ord, p.Contents 'Discussion'
FROM CmPossibility_Discussion disc
JOIN StText_Paragraphs paras on paras.Src = disc.Dst
JOIN StTxtPara p on p.Id = paras.Dst
JOIN CmPossibility_Name n on n.Obj = disc.Src AND n.Ws = (SELECT Id FROM LgWritingSystem WHERE ICULocale=N'en')
WHERE disc.Src IN (SELECT Id FROM CmObject WHERE Guid$ IN (
	'1F6AE209-141A-40DB-983C-BEE93AF0CA3C',
	'98C273C4-F723-4FB0-80DF-EEDE2204DFCA',
	'024B62C9-93B3-41A0-AB19-587A0030219A',
	'4343B1EF-B54F-4FA4-9998-271319A6D74C',
	'B2276DEC-B1A6-4D82-B121-FD114C009C59',
	'FCC61889-00E6-467B-9CF0-8C4F48B9A486',
	'CCE519D8-A9C5-4F28-9C7D-5370788BFBD5',
	'5541D063-2D43-4E49-AAAD-BBA4AE5ECCD1',
	'35CEE792-74C8-444E-A9B7-ED0461D4D3B7',
	'9466D126-246E-400B-8BBA-0703E09BC567',
	'0C4663B3-4D9A-47AF-B9A1-C8565D8112ED'))
ORDER BY n.Txt,paras.Ord
*/
---------------------------------------------------------------------
IF @Err = 1 BEGIN
	ROLLBACK TRANSACTION
	print 'Update aborted because an error occurred'
END
ELSE BEGIN
	declare @dbVersion int
	select @dbVersion = DbVer from Version$
	if @Err <> 1 AND @dbVersion = 200085
	begin
		update Version$ set DbVer = 200086
		COMMIT TRANSACTION
		print 'database updated to version 200086'
	end
	else
	begin
		ROLLBACK TRANSACTION
		print 'Update aborted: this works only if DbVer = 200085 (DbVer = ' +
				convert(varchar, @dbVersion) + ')'
	end
END
GO
