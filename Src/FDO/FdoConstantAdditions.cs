// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FdoConastantAdditions.cs
// Responsibility: Randy Regnier
// Last reviewed: never
//		Hand made additions to the generated partial constant classes go here.

using System;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.FDO
{
	public partial class LangProjectTags
	{
		/// <summary>Translation Types Possibility List</summary>
		public const string kguidTranslationTypes = "d7f71649-e8cf-11d3-9764-00c04f186933";
		/// <summary>Back Translation item in Translation Types list</summary>
		public static readonly Guid kguidTranBackTranslation = new Guid("80a0dddb-8b4b-4454-b872-88adec6f2aba");
		/// <summary>Free Translation item in Translation Types list</summary>
		public static readonly Guid kguidTranFreeTranslation = new Guid("d7f7164a-e8cf-11d3-9764-00c04f186933");
		/// <summary>Literal Translation item in Translation Types list</summary>
		public static readonly Guid kguidTranLiteralTranslation = new Guid("d7f7164b-e8cf-11d3-9764-00c04f186933");

		/// <summary>Key Terms Checking Possibility List</summary>
		public static readonly Guid kguidChkKeyTermsList = new Guid("76FB50CA-F858-469c-B1DE-A73A863E9B10");
		/// <summary>Old Key Terms Checking Possibility List</summary>
		public static readonly Guid kguidOldKeyTermsList = new Guid("04B81EC0-4850-4dc2-98C1-3FC043F71845");

		/// <summary>CmAgent representing the default M3Parser</summary>
		public static readonly Guid kguidAgentM3Parser = new Guid("1257A971-FCEF-4F06-A5E2-C289DE5AAF72");
		/// <summary>CmAgent representing the default HCParser</summary>
		public static readonly Guid kguidAgentHCParser = new Guid("5093D7D7-4F18-4AAD-8C86-88389476DF15");
		/// <summary>CmAgent representing the default User</summary>
		public static readonly Guid kguidAgentDefUser = new Guid("9303883A-AD5C-4CCF-97A5-4ADD391F8DCB");
		/// <summary>CmAgent representing the Computer (i.e., for Checking)</summary>
		public static readonly Guid kguidAgentComputer = new Guid("67E9B8BF-C312-458e-89C3-6E9326E48AA0");

		/// <summary>Phonological rule morpheme boundary</summary>
		public static readonly Guid kguidPhRuleMorphBdry = new Guid("3bde17ce-e39a-4bae-8a5c-a8d96fd4cb56");
		/// <summary>Phonological rule word boundary</summary>
		public static readonly Guid kguidPhRuleWordBdry = new Guid("7db635e0-9ef3-4167-a594-12551ed89aaa");

		/// <summary>Lex Complex Form Types Possibility List</summary>
		public const string kguidLexComplexFormTypes = "1ee09905-63dd-4c7a-a9bd-1d496743ccd6";
		/// <summary>Compound item in LexEntry Types list</summary>
		public const string kguidLexTypCompound = "1f6ae209-141a-40db-983c-bee93af0ca3c";
		/// <summary>Contraction item in LexEntry Types list</summary>
		public const string kguidLexTypContraction = "73266a3a-48e8-4bd7-8c84-91c730340b7d";
		/// <summary>Derivation item in LexEntry Types list</summary>
		public const string kguidLexTypDerivation = "98c273c4-f723-4fb0-80df-eede2204dfca";
		/// <summary>Idiom item in LexEntry Types list</summary>
		public const string kguidLexTypIdiom = "b2276dec-b1a6-4d82-b121-fd114c009c59";
		/// <summary>Phrasal Verb item in LexEntry Types list</summary>
		public const string kguidLexTypPhrasalVerb = "35cee792-74c8-444e-a9b7-ed0461d4d3b7";
		/// <summary>Saying item in LexEntry Types list</summary>
		public const string kguidLexTypSaying = "9466d126-246e-400b-8bba-0703e09bc567";

		/// <summary>Lex Variant Types Possibility List</summary>
		public const string kguidLexVariantTypes = "bb372467-5230-43ef-9cc7-4d40b053fb94";
		/// <summary>Dialectal Variant item in LexEntry Types list</summary>
		public const string kguidLexTypDialectalVar = "024b62c9-93b3-41a0-ab19-587a0030219a";
		/// <summary>Free Variant item in LexEntry Types list</summary>
		public const string kguidLexTypFreeVar = "4343b1ef-b54f-4fa4-9998-271319a6d74c";
		/// <summary>Inflectional Variant item in LexEntry Types list</summary>
		public const string kguidLexTypIrregInflectionVar = "01d4fbc1-3b0c-4f52-9163-7ab0d4f4711c";
		/// <summary>Plural Variant item in LexEntry Types list</summary>
		public const string kguidLexTypPluralVar = "a32f1d1c-4832-46a2-9732-c2276d6547e8";
		/// <summary>Past Variant item in LexEntry Types list</summary>
		public const string kguidLexTypPastVar = "837ebe72-8c1d-4864-95d9-fa313c499d78";
		/// <summary>Spelling Variant item in LexEntry Types list</summary>
		public const string kguidLexTypSpellingVar = "0c4663b3-4d9a-47af-b9a1-c8565d8112ed";
	}

	public partial class CmAgentTags
	{
		/// <summary>CmAgent representing the XAmple Parser</summary>
		public static readonly Guid kguidAgentXAmpleParser = new Guid("1257A971-FCEF-4F06-A5E2-C289DE5AAF72");
		/// <summary>CmAgent representing the Hermit Crab Parser</summary>
		public static readonly Guid kguidAgentHermitCrabParser = new Guid("5093D7D7-4F18-4AAD-8C86-88389476DF15");
		/// <summary>CmAgent representing the default User</summary>
		public static readonly Guid kguidAgentDefUser = new Guid("9303883A-AD5C-4CCF-97A5-4ADD391F8DCB");
		/// <summary>CmAgent representing the Computer (i.e., for Checking)</summary>
		public static readonly Guid kguidAgentComputer = new Guid("67E9B8BF-C312-458e-89C3-6E9326E48AA0");
	}

	public partial class CmPossibilityListTags
	{
		// Guids of CmPossibility items from the Translation Types Possibility List.
		/// <summary>Translation Types Possibility List</summary>
		public static readonly Guid kguidTranslationTypes = new Guid("d7f71649-e8cf-11d3-9764-00c04f186933");
		/// <summary>Key Terms Checking Possibility List</summary>
		public static readonly Guid kguidChkKeyTermsList = new Guid("76FB50CA-F858-469c-B1DE-A73A863E9B10");
		/// <summary>AnnotationDefn Possibility List</summary>
		public static readonly Guid kguidAnnotationDefnList = new Guid("EA346C01-022F-4F34-B938-219CE7B65B73");
		/// <summary>Lex Complex Form Types Possibility List</summary>
		public static readonly Guid kguidLexComplexFormTypes = new Guid("1ee09905-63dd-4c7a-a9bd-1d496743ccd6");
		/// <summary>Lex Variant Types Possibility List</summary>
		public static readonly Guid kguidLexVariantTypes = new Guid("bb372467-5230-43ef-9cc7-4d40b053fb94");
		/// <summary>MorphTypes Possibility List</summary>
		public const string kguidMorphTypes = "d7f713d8-e8cf-11d3-9764-00c04f186933";
		/// <summary>Publications Possibility List</summary>
		public const string kguidPublicationsList = "48a4eaed-2ae3-4c81-8c9c-f79f2f3a1455";
	}

	public partial class CmPossibilityTags
	{
		// Guids of CmPossibility items from the Translation Types Possibility List.
		/// <summary>Back Translation item in Translation Types list</summary>
		public static readonly Guid kguidTranBackTranslation = new Guid("80a0dddb-8b4b-4454-b872-88adec6f2aba");
		/// <summary>Free Translation item in Translation Types list</summary>
		public static readonly Guid kguidTranFreeTranslation = new Guid("d7f7164a-e8cf-11d3-9764-00c04f186933");
		/// <summary>Literal Translation item in Translation Types list</summary>
		public static readonly Guid kguidTranLiteralTranslation = new Guid("d7f7164b-e8cf-11d3-9764-00c04f186933");
	}

	public partial class StTxtParaTags
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This property is used when chapter or verse numbers change in the contents of
		/// a paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public const int ktagVerseNumbers = kClassId * 1000 + 998;
	}

	public partial class CmAnnotationDefnTags
	{
		/// <summary>Comment item in Annotation Definitions list</summary>
		public static readonly Guid kguidAnnComment = new Guid("f094a0b0-01b8-4621-97f1-4d775bc29ce7");
		/// <summary>Consultant Note item in Annotation Definitions list</summary>
		public static readonly Guid kguidAnnConsultantNote = new Guid("56de9b1a-1ce7-42a1-aa76-512ebeff0dda");
		/// <summary>Translator Note item in Annotation Definitions list</summary>
		public static readonly Guid kguidAnnTranslatorNote = new Guid("80ae5729-9cd8-424d-8e71-96c1a8fd5821");
		/// <summary>Errors item in Annotation Definitions list</summary>
		public static readonly Guid kguidAnnCheckingError = new Guid("82e2fd92-48d8-43c9-ba84-cc4a2a5beead");
		/// <summary>Phonological rule morpheme boundary</summary>
		public static readonly Guid kguidPhRuleMorphBdry = new Guid("3bde17ce-e39a-4bae-8a5c-a8d96fd4cb56");
		/// <summary>Phonological rule word boundary</summary>
		public static readonly Guid kguidPhRuleWordBdry = new Guid("7db635e0-9ef3-4167-a594-12551ed89aaa");

		#region Scripture check Ids
		/// <summary>Search for invalid characters item in Annotation Definitions list</summary>
		public static readonly Guid kguidCheckCharacters = new Guid("6558A579-B9C4-4EFD-8728-F994D0561293");
		/// <summary>Find Mixed Capitalization item in Annotation Definitions list</summary>
		public static readonly Guid kguidCheckMixedCapitalization = new Guid("BABCB400-F274-4498-92C5-77E99C90F75D");
		/// <summary>Search for chapter and verse errors item in Annotation Definitions list</summary>
		public static readonly Guid kguidCheckChapterVerse = new Guid("F17A054B-D21E-4298-A1A5-0D79C4AF6F0F");
		/// <summary>Search for matched pair punctuation errors item in Annotation Definitions list</summary>
		public static readonly Guid kguidCheckMatchedPairs = new Guid("DDCCB400-F274-4498-92C5-77E99C90F75B");
		/// <summary>Search for invalid punctuation item in Annotation Definitions list</summary>
		public static readonly Guid kguidCheckPunctuation = new Guid("DCC8D4D2-13B2-46E4-8FB3-29C166D189EA");
		/// <summary>Search for repeated words item in Annotation Definitions list</summary>
		public static readonly Guid kguidCheckRepeatedWords = new Guid("72ABB400-F274-4498-92C5-77E99C90F75B");
		/// <summary>Search for uncapitalized styles item in Annotation Definitions list</summary>
		public static readonly Guid kguidCheckUncapitalizedStyles = new Guid("BABCB400-F274-4498-92C5-77E99C90F75B");
		/// <summary>Sentence Final Punctuation Capitalization check ID</summary>
		public static readonly Guid kguidCheckSentenceFinalPunctuation = new Guid("BABCB400-F274-4498-92C5-77E99C90F75C");
		/// <summary>Quotations check ID</summary>
		public static readonly Guid kguidCheckQuotations = new Guid("DDCCB400-F274-4498-92C5-77E99C90F75C");
		#endregion

		/// <summary>Note item in Annotation Definitions list</summary>
		public static readonly Guid kguidAnnNote = new Guid("7ffc4eab-856a-43cc-bc11-0db55738c15b");
		/// <summary>Text item in Annotation Definitions list</summary>
		public static readonly Guid kguidAnnText = new Guid("8d4cbd80-0dca-4a83-8a1f-9db3aa4cff54");
		/// <summary>Annotation used to record when some process was last applied to the object.
		/// For example, this is used in IText to record when an StTxtPara was last parsed.
		/// BeginObject points to the object processed, and CompDetails contains a string
		/// representation of the UpdStmp of the object in question when processed</summary>
		public static readonly Guid kguidAnnProcessTime = new Guid("20cf6c1c-9389-4380-91f5-dfa057003d51");
		/// <summary>Annotation used to record when some process was last applied to the object.
		/// For example, this is used in IText to record when an StTxtPara was last parsed.
		/// BeginObject points to the object processed, and CompDetails contains a string
		/// representation of the UpdStmp of the object in question when processed</summary>
		public const string kguidAnnProcessTimeStr = "20cf6c1c-9389-4380-91f5-dfa057003d51";
	}

	public partial class CmResourceTags
	{
		/// <summary>Fixed GUID used to identify the CmResource used to indicate that orphaned
		/// footnotes have already been fixed in a FW project</summary>
		public static readonly Guid kguidFixedOrphanedFootnotes = new Guid("35E2F9E2-AF55-48c4-A8A1-4C2722386C85");
		/// <summary>Name used to identify the CmResource used to indicate that orphaned
		/// footnotes have already been fixed in a FW project</summary>
		public const string ksFixedOrphanedFootnotes = "FixedOrphanedFootnotes";

		/// <summary>Fixed GUID used to identify the CmResource used to indicate that paragraphs
		/// without BTs have already been fixed in a FW project</summary>
		public static readonly Guid kguidFixedParasWithoutBt = new Guid("BEBF4AFA-6317-4c05-BECA-47E8D0D57E5E");
		/// <summary>Name used to identify the CmResource used to indicate that paragraphs
		/// without BTs have already been fixed in a FW project</summary>
		public const string ksFixedParasWithoutBt = "FixedParasWithoutBt";

		/// <summary>Fixed GUID used to identify the CmResource used to indicate that paragraphs
		/// without segments have already been fixed in a FW project</summary>
		public static readonly Guid kguidFixedParasWithoutSegments = new Guid("E6B7CC55-E795-49ad-A1F0-DA72E3978829");
		/// <summary>Name used to identify the CmResource used to indicate that paragraphs
		/// without segments have already been fixed in a FW project</summary>
		public const string ksFixedParasWithoutSegments = "FixedParasWithoutSegments";

		/// <summary>Fixed GUID used to identify the CmResource used to indicate that leftover
		/// key term hierarchical structures have already been removed from a FW project</summary>
		public static readonly Guid kguidRemovedOldKeyTermsList = new Guid("98DBEDCE-0568-4f66-AFE3-59F032C324C6");
		/// <summary>Name used to identify the CmResource used to indicate that the old key
		/// terms list with its leftover key term hierarchical structures has already been
		/// removed from a FW project</summary>
		public const string ksRemovedOldKeyTermsList = "RemovedOldKeyTermsList";

		/// <summary>Name used to identify the CmResource used to indicate that segmenting
		/// of paragraphs has been updated for the new segmentation code.</summary>
		public const string ksResegmentedParasWithOrcs = "ResegmentedParasWithOrcs";
		/// <summary>Fixed GUID used to identify the CmResource used to indicate that segmenting
		/// of paragraphs has been updated for the new segmentation code.</summary>
		public static readonly Guid kguidResegmentedParasWithOrcs = new Guid("49F3406E-9C7D-4853-9BF1-DBB8EBAEFB5E");

		/// <summary>Name used to identify the CmResource used to indicate that styles that
		/// are referenced in ScrTxtParas have been marked InUse.</summary>
		public const string ksFixedStylesInUse = "FixedStylesInUse";
		/// <summary>Fixed GUID used to identify the CmResource used to indicate that styles that
		/// are referenced in ScrTxtParas have been marked InUse.</summary>
		public static readonly Guid kguidFixedStylesInUse = new Guid("8D4C8A53-EDC2-4e11-87CE-42FCB1A7D79C");
	}

	public partial class LexEntryRefTags
	{
		/// <summary>
		/// This value is used in LexEntryRef.RefType to indicate a variant.
		/// </summary>
		public const int krtVariant = 0;
		/// <summary>
		/// This value is used in LexEntryRef.RefType to indicate a complex form.
		/// </summary>
		public const int krtComplexForm = 1;
	}

	public abstract partial class LexRefTypeTags
	{
		/// <summary>
		///
		/// </summary>
		public enum MappingTypes
		{
			/// <summary></summary>
			kmtSenseCollection = 0,
			/// <summary></summary>
			kmtSensePair = 1,
			/// <summary>Sense Pair with different Forward/Reverse names</summary>
			kmtSenseAsymmetricPair = 2,
			/// <summary></summary>
			kmtSenseTree = 3,
			/// <summary></summary>
			kmtSenseSequence = 4,
			/// <summary></summary>
			kmtEntryCollection = 5,
			/// <summary></summary>
			kmtEntryPair = 6,
			/// <summary>Entry Pair with different Forward/Reverse names</summary>
			kmtEntryAsymmetricPair = 7,
			/// <summary></summary>
			kmtEntryTree = 8,
			/// <summary></summary>
			kmtEntrySequence = 9,
			/// <summary></summary>
			kmtEntryOrSenseCollection = 10,
			/// <summary></summary>
			kmtEntryOrSensePair = 11,
			/// <summary></summary>
			kmtEntryOrSenseAsymmetricPair = 12,
			/// <summary></summary>
			kmtEntryOrSenseTree = 13,
			/// <summary></summary>
			kmtEntryOrSenseSequence = 14
		};

	}

	public partial class LexEntryTypeTags
	{
		/// <summary>Compound item in LexEntry Types list</summary>
		public static readonly Guid kguidLexTypCompound = new Guid("1f6ae209-141a-40db-983c-bee93af0ca3c");
		/// <summary>Contraction item in LexEntry Types list</summary>
		public static readonly Guid kguidLexTypContraction = new Guid("73266a3a-48e8-4bd7-8c84-91c730340b7d");
		/// <summary>Derivation item in LexEntry Types list</summary>
		public static readonly Guid kguidLexTypDerivation = new Guid("98c273c4-f723-4fb0-80df-eede2204dfca");
		/// <summary>Idiom item in LexEntry Types list</summary>
		public static readonly Guid kguidLexTypIdiom = new Guid("b2276dec-b1a6-4d82-b121-fd114c009c59");
		/// <summary>Phrasal Verb item in LexEntry Types list</summary>
		public static readonly Guid kguidLexTypPhrasalVerb = new Guid("35cee792-74c8-444e-a9b7-ed0461d4d3b7");
		/// <summary>Saying item in LexEntry Types list</summary>
		public static readonly Guid kguidLexTypSaying = new Guid("9466d126-246e-400b-8bba-0703e09bc567");

		/// <summary>Dialectal Variant item in LexEntry Types list</summary>
		public static readonly Guid kguidLexTypDialectalVar = new Guid("024b62c9-93b3-41a0-ab19-587a0030219a");
		/// <summary>Free Variant item in LexEntry Types list</summary>
		public static readonly Guid kguidLexTypFreeVar = new Guid("4343b1ef-b54f-4fa4-9998-271319a6d74c");
		/// <summary>Inflectional Variant item in LexEntry Types list</summary>
		public static readonly Guid kguidLexTypIrregInflectionVar = new Guid("01D4FBC1-3B0C-4f52-9163-7AB0D4F4711C");
		/// <summary>Plural Variant item in LexEntry Types list</summary>
		public static readonly Guid kguidLexTypPluralVar = new Guid("a32f1d1c-4832-46a2-9732-c2276d6547e8");
		/// <summary>Past Variant item in LexEntry Types list</summary>
		public static readonly Guid kguidLexTypPastVar = new Guid("837ebe72-8c1d-4864-95d9-fa313c499d78");
		/// <summary>Spelling Variant item in LexEntry Types list</summary>
		public static readonly Guid kguidLexTypSpellingVar = new Guid("0c4663b3-4d9a-47af-b9a1-c8565d8112ed");
	}

	public partial class MoMorphTypeTags
	{

		// These are fixed GUIDS for possibility lists and items that are guaranteed to remain
		// in the database and can thus be used by application code.
		//
		// It might be better to define these as Guids instead of strings, but C# won't let us
		// do that since Guids are objects, which require a constructor and can't be const.
		// We're lucky that strings can be initialized const without an explicit constructor!
		// Also, strings can be used in case statements, but Guids cannot, so it's easier to
		// to convert the guid variable to a string first when a case statement is appropriate.
		// The hex chars A-F are given in lowercase here because C#'s Guid.ToString() method
		// is defined to produce lowercase hex digit chars.
		//
		/// <summary>Bound Root item in Morph Types list</summary>
		public const string kMorphBoundRoot = "d7f713e4-e8cf-11d3-9764-00c04f186933";
		/// <summary>Bound Root item in Morph Types list</summary>
		public static readonly Guid kguidMorphBoundRoot = new Guid(kMorphBoundRoot);

		/// <summary>Bound Stem item in Morph Types list</summary>
		public const string kMorphBoundStem = "d7f713e7-e8cf-11d3-9764-00c04f186933";
		/// <summary>Bound Stem item in Morph Types list</summary>
		public static readonly Guid kguidMorphBoundStem = new Guid(kMorphBoundStem);

		/// <summary>Circumfix item in Morph Types list</summary>
		public const string kMorphCircumfix = "d7f713df-e8cf-11d3-9764-00c04f186933";
		/// <summary>Circumfix item in Morph Types list</summary>
		public static readonly Guid kguidMorphCircumfix = new Guid(kMorphCircumfix);

		/// <summary>Clitic item in Morph Types list</summary>
		public const string kMorphClitic = "c2d140e5-7ca9-41f4-a69a-22fc7049dd2c";
		/// <summary>Clitic item in Morph Types list</summary>
		public static readonly Guid kguidMorphClitic = new Guid(kMorphClitic);

		/// <summary>Enclitic item in Morph Types list</summary>
		public const string kMorphEnclitic = "d7f713e1-e8cf-11d3-9764-00c04f186933";
		/// <summary>Enclitic item in Morph Types list</summary>
		public static readonly Guid kguidMorphEnclitic = new Guid(kMorphEnclitic);

		/// <summary>Infix item in Morph Types list</summary>
		public const string kMorphInfix = "d7f713da-e8cf-11d3-9764-00c04f186933";
		/// <summary>Infix item in Morph Types list</summary>
		public static readonly Guid kguidMorphInfix = new Guid(kMorphInfix);

		/// <summary>Particle item in Morph Types list</summary>
		public const string kMorphParticle = "56db04bf-3d58-44cc-b292-4c8aa68538f4";
		/// <summary>Particle item in Morph Types list</summary>
		public static readonly Guid kguidMorphParticle = new Guid(kMorphParticle);

		/// <summary>Prefix item in Morph Types list</summary>
		public const string kMorphPrefix = "d7f713db-e8cf-11d3-9764-00c04f186933";
		/// <summary>Prefix item in Morph Types list</summary>
		public static readonly Guid kguidMorphPrefix = new Guid(kMorphPrefix);

		/// <summary>Proclitic item in Morph Types list</summary>
		public const string kMorphProclitic = "d7f713e2-e8cf-11d3-9764-00c04f186933";
		/// <summary>Proclitic item in Morph Types list</summary>
		public static readonly Guid kguidMorphProclitic = new Guid(kMorphProclitic);

		/// <summary>Root item in Morph Types list</summary>
		public const string kMorphRoot = "d7f713e5-e8cf-11d3-9764-00c04f186933";
		/// <summary>Root item in Morph Types list</summary>
		public static readonly Guid kguidMorphRoot = new Guid(kMorphRoot);

		/// <summary>Simulfix item in Morph Types list</summary>
		public const string kMorphSimulfix = "d7f713dc-e8cf-11d3-9764-00c04f186933";
		/// <summary>Simulfix item in Morph Types list</summary>
		public static readonly Guid kguidMorphSimulfix = new Guid(kMorphSimulfix);

		/// <summary>Stem item in Morph Types list</summary>
		public const string kMorphStem = "d7f713e8-e8cf-11d3-9764-00c04f186933";
		/// <summary>Stem item in Morph Types list</summary>
		public static readonly Guid kguidMorphStem = new Guid(kMorphStem);

		/// <summary>Suffix item in Morph Types list</summary>
		public const string kMorphSuffix = "d7f713dd-e8cf-11d3-9764-00c04f186933";
		/// <summary>Suffix item in Morph Types list</summary>
		public static readonly Guid kguidMorphSuffix = new Guid(kMorphSuffix);

		/// <summary>Suprafix item in Morph Types list</summary>
		public const string kMorphSuprafix = "d7f713de-e8cf-11d3-9764-00c04f186933";
		/// <summary>Suprafix item in Morph Types list</summary>
		public static readonly Guid kguidMorphSuprafix = new Guid(kMorphSuprafix);

		/// <summary>Infixing Interfix item in Morph Types list</summary>
		public const string kMorphInfixingInterfix = "18d9b1c3-b5b6-4c07-b92c-2fe1d2281bd4";
		/// <summary>Infixing Interfix item in Morph Types list</summary>
		public static readonly Guid kguidMorphInfixingInterfix = new Guid(kMorphInfixingInterfix);

		/// <summary>Prefixing Interfix item in Morph Types list</summary>
		public const string kMorphPrefixingInterfix = "af6537b0-7175-4387-ba6a-36547d37fb13";
		/// <summary>Prefixing Interfix item in Morph Types list</summary>
		public static readonly Guid kguidMorphPrefixingInterfix = new Guid(kMorphPrefixingInterfix);

		/// <summary>Suffixing Interfix item in Morph Types list</summary>
		public const string kMorphSuffixingInterfix = "3433683d-08a9-4bae-ae53-2a7798f64068";
		/// <summary>Suffixing Interfix item in Morph Types list</summary>
		public static readonly Guid kguidMorphSuffixingInterfix = new Guid(kMorphSuffixingInterfix);

		/// <summary>Phrase item in Morph Types list</summary>
		public const string kMorphPhrase = "a23b6faa-1052-4f4d-984b-4b338bdaf95f";
		/// <summary>Phrase item in Morph Types list</summary>
		public static readonly Guid kguidMorphPhrase = new Guid(kMorphPhrase);

		/// <summary>Discontiguous phrase item in Morph Types list</summary>
		public const string kMorphDiscontiguousPhrase = "0cc8c35a-cee9-434d-be58-5d29130fba5b";
		/// <summary>Discontiguous phrase item in Morph Types list</summary>
		public static readonly Guid kguidMorphDiscontiguousPhrase = new Guid(kMorphDiscontiguousPhrase);
	}

	public partial class ScriptureTags
	{
		#region Public constants
		/// <summary>Number of books in whole Bible (without apocrypha)</summary>
		public const short kBibleLim = 66;
		/// <summary>One-based index of first book in old testament</summary>
		public const short kiOtMin = 1;
		/// <summary>One-based index of last book in old testament</summary>
		public const short kiOtMax = 39;
		/// <summary>One-based index of first book in new testament</summary>
		public const short kiNtMin = 40;
		/// <summary>Footnote marker used for initial value of auto generated
		/// footnote markers.</summary>
		public const string kDefaultAutoFootnoteMarker = "a";
		/// <summary>Footnote marker used for initial value of auto generated
		/// footnote markers.</summary>
		public const string kDefaultFootnoteMarkerSymbol = "*";

		/// <summary>Name for the default import settings</summary>
		public const string kDefaultImportSettingsName = "Default";
		/// <summary>Default name for the Paratext 5 import settings</summary>
		public const string kParatext5ImportSettingsName = "Paratext5";
		/// <summary>Default name for the Paratext 6 import settings</summary>
		public const string kParatext6ImportSettingsName = "Paratext6";
		/// <summary>Default name for the other import settings</summary>
		public const string kOtherImportSettingsName = "Other";
		#endregion
	}

	public partial class PhMetathesisRuleTags
	{
		/// <summary>
		/// Left environment
		/// </summary>
		public const int kidxLeftEnv = 0;
		/// <summary>
		/// Left switch
		/// </summary>
		public const int kidxLeftSwitch = 3;
		/// <summary>
		/// Middle
		/// </summary>
		public const int kidxMiddle = 2;
		/// <summary>
		/// Right switch
		/// </summary>
		public const int kidxRightSwitch = 1;
		/// <summary>
		/// Right environment
		/// </summary>
		public const int kidxRightEnv = 4;
	}

	public partial class PhPhonemeTags
	{
		/// <summary>
		/// file name for the BasicIPASymbol mapper file
		/// </summary>
		public const string ksBasicIPAInfoFile = "BasicIPAInfo.xml";
	}

	/// <summary>
	/// Constants used for encoding conversion
	/// </summary>
	public static class EncodingConstants
	{
		/// <summary>
		/// Code page 28591 will map all high-bit characters to the same
		/// value with a high-byte of 0 in unicode. For example, character 0x84
		/// will end up as 0x0084.
		/// </summary>
		public const int kMagicCodePage = 28591;
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Constants for WfiWordforms
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class WfiWordformTags
	{
		/// <summary>
		/// Maximum length for the text used in a WfiWordform.
		/// </summary>
		public const int kMaxWordformLength = 300;
	}

	/// <summary>
	/// Constants for RnResearchNbk
	/// </summary>
	public partial class RnResearchNbkTags
	{
		/// <summary>Record Types possibility list</summary>
		public static readonly Guid kguidRecTypesList = new Guid("D9D55B12-EA5E-11DE-95EF-0013722F8DEC");

		/// <summary>Conversation item in Record Types list</summary>
		public static readonly Guid kguidRecConversation = new Guid("B7B37B86-EA5E-11DE-80E9-0013722F8DEC");
		/// <summary>Interview item in Record Types list</summary>
		public static readonly Guid kguidRecInterview = new Guid("B7BF673E-EA5E-11DE-9C4D-0013722F8DEC");
		/// <summary>Structured Interview item in Record Types list</summary>
		public static readonly Guid kguidRecStructured = new Guid("B7C8F092-EA5E-11DE-8D7D-0013722F8DEC");
		/// <summary>Unstructured Interview item in Record Types list</summary>
		public static readonly Guid kguidRecUnstructured = new Guid("B7D4DC4A-EA5E-11DE-867C-0013722F8DEC");
		/// <summary>Literature Summary item in Record Types list</summary>
		public static readonly Guid kguidRecLiteratureSummary = new Guid("B7E0C7F8-EA5E-11DE-82CC-0013722F8DEC");
		/// <summary>Observation item in Record Types list</summary>
		public static readonly Guid kguidRecObservation = new Guid("B7EA5156-EA5E-11DE-9F9C-0013722F8DEC");
		/// <summary>Performance item in Record Types list</summary>
		public static readonly Guid kguidRecPerformance = new Guid("B7F63D0E-EA5E-11DE-9F02-0013722F8DEC");
		/// <summary>Analysis item in Record Types list</summary>
		public static readonly Guid kguidRecAnalysis = new Guid("82290763-1633-4998-8317-0EC3F5027FBD");
		/// <summary>Event item in Record Types list</summary>
		public static readonly Guid kguidRecEvent = new Guid("00951e6f-2523-4ac9-a649-ad42c659cf83");
		/// <summary>Methodology item in Record Types list</summary>
		public static readonly Guid kguidRecMethodology = new Guid("bcb1cf88-582d-463a-b96f-0023f6ac2395");
		/// <summary>Weather item in Record Types list</summary>
		public static readonly Guid kguidRecWeather = new Guid("06974d9a-ff86-4e1c-a3e5-7ce8c961dcb9");

	}

	/// <summary>
	/// Constants for CmFolder
	/// </summary>
	public partial class CmFolderTags
	{
		// We don't want to localize internal folder names for pictures and media.
		/// <summary>Returns a string for the local picture folder.</summary>
		public const string LocalPictures = "Local Pictures";
		/// <summary>Returns a string for the local media folder.</summary>
		public const string LocalMedia = "Local Media";
		/// <summary>Returns a string for the local CmFolder for externalLink's to files found in TsStrings.</summary>
		public const string LocalFilePathsInTsStrings = "File paths in TsStrings";
		/// <summary>The name of the default CmFolder for storing (non-cataloged) pictures.</summary>
		public const string DefaultPictureFolder = LocalPictures;
	}
}