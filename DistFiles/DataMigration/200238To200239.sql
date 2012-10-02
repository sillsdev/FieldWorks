-- Update database from version 200238 to 200239
BEGIN TRANSACTION  --( will be rolled back if wrong version #)

-------------------------------------------------------------------------------
-- February 3, 2009 Ann Bush, Parser Model Changes per FWM-156
--LangProject - Add PhFeatureSystem: owning atomic to FsFeature System.
--PhPhonData - Add PhonRules: owning sequence to PhSegmentRule
--                 PhonRuleFeats: Owning atomic to CmPossibilityList
--                 FeatConstraints: Owning  sequence to PhFeatureConstraint
--PhPhoneme - Add Property BasicIPASymbol: String
--                Features: owning Atomic to FsFeatStruc
--New Class PhSegmentRule - Add property Description: MultiString
--                             Name: MultiUnicode
--                             Direction: Integer
--                ReferenceAttribute InitialStratum atomic to MoStratum
--                ReferenceAttribute FinalStratum atomic to MoStratum
--New Class PhRegularRule - Add OwningAttribute RightHandSides Sequence to PhSegRuleRHS
--                Add OwningAttribute StrucDesc Sequence to PhSimpleContext
--New Class PhMetathesisRule< - Add Property Description: MultiString
--                                 Name: MultiUnicode
--                                 StrucChange: String
--                Add OwningAttribute StrucDesc Sequence to PhSimpleContext<
--New Class PhSegRuleRHS - Add OwningAttribute LeftContext atomic to PhPhonContext
--               Add OwningAttribute RightContext atomic to PhPhonContext
--               Add OwningAttribute StrucChange sequence to PhSimpleContext
--               Add ReferenceAttribute InputPOSes collection to PartOfSpeech
--               Add ReferenceAttribute ExclRuleFeats collection to PhPhonRuleFeat
--               Add ReferenceAttribute ReqRuleFeats collection to PhPhonRuleFeat
--New Class PhPhonRuleFeat - Add ReferenceAttribute Item atomic to CmObject
-------------------------------------------------------------------------------
--==( PhSegmentRule )==--

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(5128, 5, 0, 0, 'PhSegmentRule')

go
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5128001, 14, 5128,
		null, 'Description',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5128002, 16, 5128,
		null, 'Name',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5128003, 2, 5128,
		null, 'Direction',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5128004, 24, 5128,
		5048, 'InitialStratum',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5128005, 24, 5128,
		5048, 'FinalStratum',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5128006, 27, 5128,
		5084, 'StrucDesc',0,Null, null, null, null)
go

--==( PhPhonRuleFeat )==--

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(5132, 5, 7, 0, 'PhPhonRuleFeat')

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5132001, 24, 5132,
		0, 'Item',0,Null, null, null, null)
go

--==( PhSegRuleRHS )==--

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(5131, 5, 0, 0, 'PhSegRuleRHS')

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5131001, 23, 5131,
		5081, 'LeftContext',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5131002, 23, 5131,
		5081, 'RightContext',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5131003, 27, 5131,
		5084, 'StrucChange',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5131004, 26, 5131,
		5049, 'InputPOSes',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5131005, 26, 5131,
		5132, 'ExclRuleFeats',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5131006, 26, 5131,
		5132, 'ReqRuleFeats',0,Null, null, null, null)
go

--==( PhMetathesisRule )==--

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(5130, 5, 5128, 0, 'PhMetathesisRule')

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5130003, 13, 5130,
		null, 'StrucChange',0,Null, null, null, null)
go

--==( PhRegularRule )==--

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(5129, 5, 5128, 0, 'PhRegularRule')

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5129001, 27, 5129,
		5131, 'RightHandSides',0,Null, null, null, null)
go

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5099005, 27, 5099,
		5096, 'FeatConstraints',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5099006, 23, 5099,
		8, 'PhonRuleFeats',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5099007, 27, 5099,
		5128, 'PhonRules',0,Null, null, null, null)

go

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5092001, 13, 5092,
		null, 'BasicIPASymbol',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5092002, 23, 5092,
		57, 'Features',0,Null, null, null, null)
go

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(6001154, 23, 6001,
		49, 'PhFeatureSystem',0,Null, null, null, null)
go

-----------------------------------------------------------------------------------
------ Finish or roll back transaction as applicable
-----------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200238
BEGIN
	UPDATE Version$ SET DbVer = 200239
	COMMIT TRANSACTION
	PRINT 'database updated to version 200239'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200238 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
