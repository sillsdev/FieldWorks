-- Update database from version 200240 to 200241
BEGIN TRANSACTION  --( will be rolled back if wrong version #)

-------------------------------------------------------------------------------
-- February 12, 2009 Ann Bush,  FWM-159 Change for Syntactic Markup
-- LangProject - Add TextMarkupTags: referencing atomic to CmPossibilityList.
-- Add new list, with 4 lists containing subpossibilities, add CmAnnotationDefn
-- LT-7727 add-on Create LiftResidue Property in LexEntryRef
-- FWM-156 add-on create FsFeatureSystem object pointed to by PhFeatureSystem (OA)
-------------------------------------------------------------------------------
--==( PhSegmentRule )==--
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(6001055, 23, 6001,
		8, 'TextMarkupTags',0,Null, null, null, null)

-- LT-7727 add-on Create LiftResidue Property in LexEntryRef
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5127007, 19, 5127,
		null, 'LiftResidue',0,Null, null, null, null)
go

-- Create a list of TextMarkupTags and create Possibilities and SubPossibilities with values.

DECLARE @hvoLangProj int,
	@CmMajorObject_Name_txt nvarchar(max),
	@today DATETIME,
	@en int,
	@NewObjId int,
	@NewObjGuid uniqueidentifier,
	@NewObjectId int,
	@NewObjectGuid uniqueidentifier,
	@NewSubObjectId int,
	@NewSubObjectGuid uniqueidentifier,
	@Owner INT,
	@Fmt VARBINARY(8000)

-- Create the TextMarkupTags possibility list
--( to find owner of new list, which will be LangProject.  There should only be one obj
SELECT TOP 1 @hvoLangProj= id from cmObject where class$=6001
SELECT @en = id from LgWritingSystem WHERE ICULocale=N'en'

SELECT TOP 1 @fmt=Fmt FROM MultiStr$ WHERE Ws=@en ORDER BY LEN(Fmt)
	--( The shortest string will most likely not have any embedding in it.
SET @CmMajorObject_Name_txt = N'Text Markup Tags'
SET @today = CURRENT_TIMESTAMP

EXEC MakeObj_CmPossibilityList
	@en,
	@CmMajorObject_Name_txt,
	@today, --( Date created
	@today, --( Date Modified
	@en,
	N'Text Markup Tags',
	@fmt, -- Description_fmt
	127, --( Depth
	0, --( PreventChoiceAboveLevel
	1, --( Ordered
	0, --( IsClosed
	1, --( PreventDuplicates
	0, --( PreventNodeChoices
	@en, --( Abbreviation_WS
	N'TxtTags',
	NULL, --( HelpFile
	0, --( UseExtendedFields
	0, --(DisplayOption
	7, --( ItemClsId, in this case, the class for CmPossibility
	0, --( IsVernacular,
	-3, --( WsSelector, in this case, All analysis writing system
	NULL, --( ListVersion
	@hvoLangProj, --( owner
	6001055, -- Owning Flid
	NULL, --( StartObj
	@NewObjId OUTPUT,
	@NewObjGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 1. (Possibility owned by TextMarkupTags)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Grammatical Relations-Functional', --( Name
	@en,		--( English Writing System for Abbreviation
	'GrRel-Func',		--( Abbreviation
	@en,		--( English Writing System for Description
	'A list of Role tags based on the book "Describing Morphosyntax" by Thomas E. Payne(p. 129).',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjId,  --( Owner
	8008,		--( OwnFlid (PossibilityList_Possibilities))
	NULL,		--( StartObj
	@NewObjectId OUTPUT,
	@NewObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 2. (SubPossibility of Grammatical Relations-Functional)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'A',				--( Name
	@en,		--( English Writing System for Abbreviation
	'A',				--( Abbreviation
	@en,		--( English Writing System for Description
	'The most Agent-like argument of a multi-argument clause (transitive).',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 3. (SubPossibility of Grammatical Relations-Functional)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'P',				 --( Name
	@en,		--( English Writing System for Abbreviation
	'P',				--( Abbreviation
	@en,		--( English Writing System for Description
	'The most Patient-like argument of a multi-argument clause (transitive).',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 4. (SubPossibility of Grammatical Relations-Functional)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'S',				--( Name
	@en,		--( English Writing System for Abbreviation
	'S',				--( Abbreviation
	@en,		--( English Writing System for Description
	'The only nominal argument of a single argument clause (intransitive).',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 5. (SubPossibility of Grammatical Relations-Functional)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Subject',			--( Name
	@en,		--( English Writing System for Abbreviation
	'Subj',				--( Abbreviation
	@en,		--( English Writing System for Description
	'The S or A argument in a Nominative/Accusative system of grammatical relations.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 6. (SubPossibility of Grammatical Relations-Functional)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Direct Object',	--( Name
	@en,		--( English Writing System for Abbreviation
	'Obj',				--( Abbreviation
	@en,		--( English Writing System for Description
	'Usually the P argument in a Nominative/Accusative system. The object most closely tied to the Verb syntactically.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 7. (SubPossibility of Grammatical Relations-Functional)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Indirect Object',	--( Name
	@en,		--( English Writing System for Abbreviation
	'IO',				--( Abbreviation
	@en,		--( English Writing System for Description
	'An object less closely tied to the Verb syntactically than the Direct Object.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 8. (SubPossibility of Grammatical Relations-Functional)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Ergative',		--( Name
	@en,		--( English Writing System for Abbreviation
	'Erg',				--( Abbreviation
	@en,		--( English Writing System for Description
	'The A argument in an Ergative/Absolutive system of grammatical relations.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 9. (SubPossibility of Grammatical Relations-Functional)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Absolutive',	--( Name
	@en,		--( English Writing System for Abbreviation
	'Abs',				--( Abbreviation
	@en,		--( English Writing System for Description
	'The S or P argument in an Ergative/Absolutive system of grammatical relations.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp


--  Create entriew for the list 10. (SubPossibility of Grammatical Relations-Functional)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Oblique',			--( Name
	@en,		--( English Writing System for Abbreviation
	'Obl',				--( Abbreviation
	@en,		--( English Writing System for Description
	'A nominal with at best secondary relation to the Verb in a Clause.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp


--  Create entriew for the list 11. (Possibility owned by TextMarkupTags)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Syntax-Descriptive',	--( Name
	@en,		--( English Writing System for Abbreviation
	'Syn-Desc',			--( Abbreviation
	@en,		--( English Writing System for Description
	'A list of basic descriptive Syntax tags.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjId,  --( Owner
	8008,		--( OwnFlid (PossibilityList_Possibilities))
	NULL,		--( StartObj
	@NewObjectId OUTPUT,
	@NewObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 12. (SubPossibility of Syntax-Descriptive)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Adjective Phrase',	--( Name
	@en,		--( English Writing System for Abbreviation
	'AdjP',				--( Abbreviation
	@en,		--( English Writing System for Description
	'A phrase consisting of an Adjective and its modifiers.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 13. (SubPossibility of Syntax-Descriptive)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Adverb Phrase',	--( Name
	@en,		--( English Writing System for Abbreviation
	'AdvP',				--( Abbreviation
	@en,		--( English Writing System for Description
	'A phrase consisting of an Adverb and its modifiers.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 14. (SubPossibility of Syntax-Descriptive)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Clause',	--( Name
	@en,		--( English Writing System for Abbreviation
	'Cl',				--( Abbreviation
	@en,		--( English Writing System for Description
	'A group of words with a single central Verb Phrase.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 15. (SubPossibility of Syntax-Descriptive)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Clause Chain',	--( Name
	@en,		--( English Writing System for Abbreviation
	'Chn',				--( Abbreviation
	@en,		--( English Writing System for Description
	'A chain of Clauses.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 16. (SubPossibility of Syntax-Descriptive)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Noun Phrase',	--( Name
	@en,		--( English Writing System for Abbreviation
	'NP',				--( Abbreviation
	@en,		--( English Writing System for Description
	'A phrase consisting of a Noun and its modifiers.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 17. (SubPossibility of Syntax-Descriptive)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Possessive Phrase',	--( Name
	@en,		--( English Writing System for Abbreviation
	'PossP',				--( Abbreviation
	@en,		--( English Writing System for Description
	'',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 18. (SubPossibility of Syntax-Descriptive)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Postpositional Phrase',	--( Name
	@en,		--( English Writing System for Abbreviation
	'PostP',				--( Abbreviation
	@en,		--( English Writing System for Description
	'An Adpositional Phrase where the central constituent follows the Nominal.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 19. (SubPossibility of Syntax-Descriptive)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Prepositional Phrase',	--( Name
	@en,		--( English Writing System for Abbreviation
	'PreP',				--( Abbreviation
	@en,		--( English Writing System for Description
	'An Adpositional Phrase where the central constituent precedes the Nominal.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 20. (SubPossibility of Syntax-Descriptive)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Relative Clause',	--( Name
	@en,		--( English Writing System for Abbreviation
	'RelCl',				--( Abbreviation
	@en,		--( English Writing System for Description
	'A clause that serves as a noun modifier.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 21. (SubPossibility of Syntax-Descriptive)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Sentence',	--( Name
	@en,		--( English Writing System for Abbreviation
	'Sent',				--( Abbreviation
	@en,		--( English Writing System for Description
	'A complete grouping of one or more Clauses.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 22. (SubPossibility of Syntax-Descriptive)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Inflectional Phrase',	--( Name
	@en,		--( English Writing System for Abbreviation
	'IP',				--( Abbreviation
	@en,		--( English Writing System for Description
	'A grouping of (usually) a Verb Phrase and its inflectional modifiers.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 23. (SubPossibility of Syntax-Descriptive)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Verb Phrase',	--( Name
	@en,		--( English Writing System for Abbreviation
	'VP',				--( Abbreviation
	@en,		--( English Writing System for Description
	'A phrase consisting of a Verb and its direct modifiers.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 24. (Possibility owned by TextMarkupTags)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Semantics-RRG',	--( Name
	@en,		--( English Writing System for Abbreviation
	'Sem-RRG',			--( Abbreviation
	@en,		--( English Writing System for Description
	'A list of Semantic tags based on an email from John Roberts about RRG. This will need further work when crossing trees are possible in FLEx.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjId,  --( Owner
	8008,		--( OwnFlid (PossibilityList_Possibilities))
	NULL,		--( StartObj
	@NewObjectId OUTPUT,
	@NewObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 25. (SubPossibility of Semantics-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'ACTOR',	--( Name
	@en,		--( English Writing System for Abbreviation
	'ACTOR',				--( Abbreviation
	@en,		--( English Writing System for Description
	'An RRG macrorole, most agentive.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 26. (SubPossibility of Semantics-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'UNDERGOER',	--( Name
	@en,		--( English Writing System for Abbreviation
	'UNDER',				--( Abbreviation
	@en,		--( English Writing System for Description
	'An RRG macrorole, least agentive.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 26a. (SubPossibility of Semantics-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'NON-MACROROLE',	--( Name
	@en,		--( English Writing System for Abbreviation
	'NON-MR',			--( Abbreviation
	@en,		--( English Writing System for Description
	'Non-Macrorole argument',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 27. (Possibility owned by TextMarkupTags)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Syntax-RRG',		--( Name
	@en,		--( English Writing System for Abbreviation
	'Syn-RRG',			--( Abbreviation
	@en,		--( English Writing System for Description
	'A list of Syntax tags based on an email from John Roberts about RRG.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjId,  --( Owner
	8008,		--( OwnFlid (PossibilityList_Possibilities))
	NULL,		--( StartObj
	@NewObjectId OUTPUT,
	@NewObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 28. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'SENTENCE',		--( Name
	@en,		--( English Writing System for Abbreviation
	'SENT',				--( Abbreviation
	@en,		--( English Writing System for Description
	'',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 29. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'CLAUSE',	--( Name
	@en,		--( English Writing System for Abbreviation
	'CL',				--( Abbreviation
	@en,		--( English Writing System for Description
	'',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 30. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'CORE',	--( Name
	@en,		--( English Writing System for Abbreviation
	'CORE',				--( Abbreviation
	@en,		--( English Writing System for Description
	'',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 31. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'NUCLEUS',			--( Name
	@en,		--( English Writing System for Abbreviation
	'NUC',				--( Abbreviation
	@en,		--( English Writing System for Description
	'',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 32. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'PREDICATE',		--( Name
	@en,		--( English Writing System for Abbreviation
	'PRED',				--( Abbreviation
	@en,		--( English Writing System for Description
	'',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 33. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Left Detached Position',	--( Name
	@en,		--( English Writing System for Abbreviation
	'LDP',				--( Abbreviation
	@en,		--( English Writing System for Description
	'',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 34. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Right Detached Position',	--( Name
	@en,		--( English Writing System for Abbreviation
	'RDP',				--( Abbreviation
	@en,		--( English Writing System for Description
	'',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 35. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Pre-core slot',	--( Name
	@en,		--( English Writing System for Abbreviation
	'PrCS',				--( Abbreviation
	@en,		--( English Writing System for Description
	'',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 36. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Post-core slot',	--( Name
	@en,		--( English Writing System for Abbreviation
	'PoCS',				--( Abbreviation
	@en,		--( English Writing System for Description
	'',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 37. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'PERIPHERY',	--( Name
	@en,		--( English Writing System for Abbreviation
	'PERI',				--( Abbreviation
	@en,		--( English Writing System for Description
	'',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 38. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Reference Phrase',	--( Name
	@en,		--( English Writing System for Abbreviation
	'RP',				--( Abbreviation
	@en,		--( English Writing System for Description
	'Referential Phrase replaces the Noun Phrase(NP) in earlier RRG models (Van Valin 2006).',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 38a. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Pronominal Argument',	--( Name
	@en,		--( English Writing System for Abbreviation
	'PRO',				--( Abbreviation
	@en,		--( English Writing System for Description
	'Used for arguments represented by bound morphemes.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 38b. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Auxiliary',	--( Name
	@en,		--( English Writing System for Abbreviation
	'AUX',				--( Abbreviation
	@en,		--( English Writing System for Description
	'Non-predicative verbs required for the formation of particular syntactic constructions.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 39. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'CORE-R',	--( Name
	@en,		--( English Writing System for Abbreviation
	'CORE-R',				--( Abbreviation
	@en,		--( English Writing System for Description
	'The CORE of a Referential Phrase.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 40. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'NUCLEUS-R',	--( Name
	@en,		--( English Writing System for Abbreviation
	'NUC-R',				--( Abbreviation
	@en,		--( English Writing System for Description
	'The NUCLEUS of a Referential Phrase.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 41. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Referential Phrase Initial Position',	--( Name
	@en,		--( English Writing System for Abbreviation
	'RPIP',				--( Abbreviation
	@en,		--( English Writing System for Description
	'',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 42. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Referential Phrase Final Position',	--( Name
	@en,		--( English Writing System for Abbreviation
	'RPFP',				--( Abbreviation
	@en,		--( English Writing System for Description
	'',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 43. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'PERIPHERY-R',	--( Name
	@en,		--( English Writing System for Abbreviation
	'PERI-R',				--( Abbreviation
	@en,		--( English Writing System for Description
	'The PERIPHERY of a Referential Phrase.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 44. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Modifier Phrase',	--( Name
	@en,		--( English Writing System for Abbreviation
	'MP',				--( Abbreviation
	@en,		--( English Writing System for Description
	'',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 45. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'CORE-M',	--( Name
	@en,		--( English Writing System for Abbreviation
	'CORE-M',				--( Abbreviation
	@en,		--( English Writing System for Description
	'The CORE of a Modifier Phrase.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 46. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'NUCLEUS-M',	--( Name
	@en,		--( English Writing System for Abbreviation
	'NUC-M',				--( Abbreviation
	@en,		--( English Writing System for Description
	'The NUCLEUS of a Modifier Phrase.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 47. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'Adpositional Phrase',	--( Name
	@en,		--( English Writing System for Abbreviation
	'PP',				--( Abbreviation
	@en,		--( English Writing System for Description
	'PP is either Prepositional Phrase or Postpositional Phrase depending on the typology of the language.',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 48. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'CORE-P',	--( Name
	@en,		--( English Writing System for Abbreviation
	'CORE-P',				--( Abbreviation
	@en,		--( English Writing System for Description
	'The CORE of an Adpositional Phrase(PP).',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

--  Create entriew for the list 49. (SubPossibility of Syntax-RRG)
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmPossibility
	@en,		--( English Writing System for Name
	N'NUCLEUS-P',	--( Name
	@en,		--( English Writing System for Abbreviation
	'NUC-P',				--( Abbreviation
	@en,		--( English Writing System for Description
	'The NUCLEUS of an Adpositional Phrase(PP).',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 0,	--( UnderStyle, Hidden, Protected
	@NewObjectId,  --( Owner
	7004,		--( OwnFlid (Possibility_SubPossibilities))
	NULL,		--( StartObj
	@NewSubObjectId OUTPUT,
	@NewSubObjectGuid OUTPUT,
	NULL, --( ReturnTimeStamp
	NULL --( NewObjTimeStamp

-- Create the TextMarkupTags possibility list
select @owner = id from cmObject where guid$ = '8D4CBD80-0DCA-4A83-8A1F-9DB3AA4CFF54'
SET @today = CURRENT_TIMESTAMP
exec MakeObj_CmAnnotationDefn
	@en,		--( English Writing System for Name
	N'Text Tag', --( Name
	@en,		--( English Writing System for Abbreviation
	'TTag',		--( Abbreviation
	@en,		--( English Writing System for Description
	'Used in interlinear text tagging for syntax, etc',
	@fmt,		--( Description format
	0,			--( Sort spec
	@today,		--( Date Created
	@today,		--( Date Modified
	NULL,		--( Help Id
	-1073741824, --( ForeColor
	-1073741824, --( BackColor
	-1073741824, --( UnderColor
	0, 0, 1,	 --( UnderStyle, Hidden, Protected
	1, 0, 0,     --( AllowsComment, AllowsFeatureStructure, AllowsInstanceOf
	0, 0, 0,     --( InstanceOfSignature, UserCanCreate, CanCreateOrphan
	0, 1, 0,     --( PromptUser. CopyCutPastable, ZeroWidth
	1, 0, 0,	--( Multi, Severity, MaxDupOccur
	@owner,		--( Owner
	7004,		--( OwnFlid
	NULL,		--( StartObj
	@NewObjId output,
	@NewObjGuid output,
	NULL,		--( ReturnTimeStamp
	NULL		--( NewObjTimeStamp
update CmObject set Guid$ = '084A3AFE-0D00-41da-BFCF-5D8DEAFA0296' where id = @NewObjId

-- FWM-156 add-on create FsFeatureSystem object pointed to by PhFeatureSystem (OA)

Select TOP 1 @owner = id from cmObject where class$=6001 -- LangProject

exec MakeObj_FsFeatureSystem
	@Owner,    -- Owner
	6001154,   -- OwningFlid,
	Null,      -- StartObj
	NULL,      -- NewObjId output
	NULL,      -- NewObjGuid output
	NULL,      -- ReturnTimestamp
	NULL       -- NewObjTimestamp

-----------------------------------------------------------------------------------
------ Finish or roll back transaction as applicable
-----------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200240
BEGIN
	UPDATE Version$ SET DbVer = 200241
	COMMIT TRANSACTION
	PRINT 'database updated to version 200241'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200240 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
