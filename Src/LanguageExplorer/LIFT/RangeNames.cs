// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.LIFT
{
	public static class RangeNames
	{
		/// <summary />
		public const string sAffixCategoriesOA = "affix-categories";
		/// <summary />
		public const string sAnnotationDefsOA = "annotation-definitions";
		/// <summary />
		public const string sAnthroListOAold1 = "anthro_codes";
		/// <summary />
		public const string sAnthroListOA = "anthro-code";
		/// <summary />
		public const string sConfidenceLevelsOA = "confidence-levels";
		/// <summary />
		public const string sEducationOA = "education";
		/// <summary />
		public const string sGenreListOA = "genres";
		/// <summary />
		public const string sLocationsOA = "location";
		/// <summary />
		public const string sPartsOfSpeechOA = "grammatical-info";
		/// <summary />
		public const string sPartsOfSpeechOAold2 = "FromPartOfSpeech";
		/// <summary />
		public const string sPartsOfSpeechOAold1 = "from-part-of-speech";
		/// <summary />
		public const string sPeopleOA = "users";
		/// <summary />
		public const string sPositionsOA = "positions";
		/// <summary />
		public const string sRestrictionsOA = "restrictions";
		/// <summary />
		public const string sRolesOA = "roles";
		/// <summary />
		public const string sSemanticDomainListOAold1 = "semanticdomainddp4";
		/// <summary />
		public const string sSemanticDomainListOAold2 = "semantic_domain";
		/// <summary />
		public const string sSemanticDomainListOAold3 = "semantic-domain";
		/// <summary />
		public const string sSemanticDomainListOA = "semantic-domain-ddp4";
		/// <summary />
		public const string sStatusOA = "status";
		/// <summary />
		public const string sThesaurusRA = "thesaurus";
		/// <summary />
		public const string sTranslationTagsOAold1 = "translation-types";
		/// <summary />
		public const string sTranslationTagsOA = "translation-type";
		/// <summary />
		public const string sProdRestrictOA = "exception-feature";
		/// <summary />
		public const string sProdRestrictOAfrom = "from-exception-feature";
		/// <summary />
		public const string sDbComplexEntryTypesOA = "complex-form-types";
		/// <summary />
		public const string sDbDialectLabelsOA = "dialect-labels";
		/// <summary />
		public const string sDbDomainTypesOA = "domain-type";
		/// <summary />
		public const string sDbDomainTypesOAold1 = "domaintype";
		/// <summary />
		public const string sDbLanguagesOA = "languages";
		/// <summary />
		public const string sDbMorphTypesOAold = "MorphType";
		/// <summary />
		public const string sDbMorphTypesOA = "morph-type";
		/// <summary />
		public const string sDbPublicationTypesOA = "do-not-publish-in";
		/// <summary />
		public const string sDbPublicationTypesOAold = "publishin";
		/// <summary />
		public const string sDbReferencesOAold = "lexical-relations";
		/// <summary />
		public const string sDbReferencesOA = "lexical-relation";
		/// <summary />
		public const string sDbSenseTypesOA = "sense-type";
		/// <summary />
		public const string sDbSenseTypesOAold1 = "sensetype";
		/// <summary />
		public const string sDbUsageTypesOAold = "usagetype";
		/// <summary />
		public const string sDbUsageTypesOA = "usage-type";
		/// <summary />
		public const string sDbVariantEntryTypesOA = "variant-types";
		/// <summary />
		public const string sMSAinflectionFeature = "inflection-feature";
		/// <summary />
		public const string sMSAfromInflectionFeature = "from-inflection-feature";
		/// <summary />
		public const string sMSAinflectionFeatureType = "inflection-feature-type";
		/// <summary />
		public const string sReversalType = "reversal-type";

		/// <summary>
		/// Return the LIFT range name for a given cmPossibilityList. Get the fieldName
		/// of the owning field and use that to get the range name.
		/// </summary>
		public static string GetRangeNameForLiftExport(IFwMetaDataCacheManaged mdc, ICmPossibilityList list)
		{
			string rangeName;
			if (list.OwningFlid == 0)
			{
				rangeName = list.Name.BestAnalysisVernacularAlternative.Text;
			}
			else
			{
				var fieldName = mdc.GetFieldName(list.OwningFlid);
				rangeName = GetRangeName(fieldName);
			}
			return rangeName;
		}

		/// <summary>
		/// Return the LIFT range name for a given fieldName in Flex
		/// </summary>
		public static string GetRangeName(string fieldName)
		{
			string rangeName;
			switch (fieldName)
			{
				case "AffixCategories": rangeName = sAffixCategoriesOA; break;
				case "AnnotationDefs": rangeName = sAnnotationDefsOA; break;
				case "AnthroList": rangeName = sAnthroListOA; break;
				case "ConfidenceLevels": rangeName = sConfidenceLevelsOA; break;
				case "Education": rangeName = sEducationOA; break;
				case "GenreList": rangeName = sGenreListOA; break;
				case "Locations": rangeName = sLocationsOA; break;
				case "PartsOfSpeech": rangeName = sPartsOfSpeechOA; break;
				case "People": rangeName = sPeopleOA; break;
				case "Positions": rangeName = sPositionsOA; break;
				case "Restrictions": rangeName = sRestrictionsOA; break;
				case "Roles": rangeName = sRolesOA; break;
				case "SemanticDomainList": rangeName = sSemanticDomainListOA; break;
				case "Status": rangeName = sStatusOA; break;
				case "Thesaurus": rangeName = sThesaurusRA; break;
				case "TranslationTags": rangeName = sTranslationTagsOA; break;
				case "ComplexEntryTypes": rangeName = sDbComplexEntryTypesOA; break;
				case "DialectLabels": rangeName = sDbDialectLabelsOA; break;
				case "DomainTypes": rangeName = sDbDomainTypesOA; break;
				case "Languages": rangeName = sDbLanguagesOA; break;
				case "MorphTypes": rangeName = sDbMorphTypesOA; break;
				case "PublicationTypes": rangeName = sDbPublicationTypesOA; break;
				case "References": rangeName = sDbReferencesOA; break;
				case "SenseTypes": rangeName = sDbSenseTypesOA; break;
				case "UsageTypes": rangeName = sDbUsageTypesOA; break;
				case "VariantEntryTypes": rangeName = sDbVariantEntryTypesOA; break;
				default:
					rangeName = fieldName.ToLowerInvariant();
					break;
			}
			return rangeName;
		}

		public static bool RangeNameIsCustomList(string range)
		{
			switch (range)
			{
				case sAffixCategoriesOA:
				case sAnnotationDefsOA:
				case sAnthroListOAold1:
				case sAnthroListOA:
				case sConfidenceLevelsOA:
				case sEducationOA:
				case sGenreListOA:
				case sLocationsOA:
				case sPartsOfSpeechOA:
				case sPartsOfSpeechOAold2:
				case sPartsOfSpeechOAold1:
				case sPeopleOA:
				case sPositionsOA:
				case sRestrictionsOA:
				case sRolesOA:
				case sSemanticDomainListOAold1:
				case sSemanticDomainListOAold2:
				case sSemanticDomainListOAold3:
				case sSemanticDomainListOA:
				case sStatusOA:
				case sThesaurusRA:
				case sTranslationTagsOAold1:
				case sTranslationTagsOA:
				case sProdRestrictOA:
				case sProdRestrictOAfrom:
				case sDbComplexEntryTypesOA:
				case sDbDialectLabelsOA:
				case sDbDomainTypesOA:
				case sDbDomainTypesOAold1:
				case sDbLanguagesOA:
				case sDbMorphTypesOAold:
				case sDbMorphTypesOA:
				case sDbPublicationTypesOA:
				case sDbPublicationTypesOAold:
				case sDbReferencesOAold:
				case sDbReferencesOA:
				case sDbSenseTypesOA:
				case sDbSenseTypesOAold1:
				case sDbUsageTypesOAold:
				case sDbUsageTypesOA:
				case sDbVariantEntryTypesOA:
				case sMSAinflectionFeature:
				case sMSAfromInflectionFeature:
				case sMSAinflectionFeatureType:
				case sReversalType:
				case "dialect":
				case "etymology":
				case "note-type":
				case "paradigm":
				case "Publications":
					return false;
				default:
					if (range.EndsWith("-slot") || range.EndsWith("-Slots") ||
						range.EndsWith("-infl-class") || range.EndsWith("-InflClasses") ||
						range.EndsWith("-feature-value") || range.EndsWith("-stem-name"))
					{
						return false;
					}
					return true;
			}
		}
	}
}