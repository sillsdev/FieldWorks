// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.LIFT
{
	internal static class RangeNames
	{
		/// <summary />
		internal const string sAffixCategoriesOA = "affix-categories";
		/// <summary />
		internal const string sAnnotationDefsOA = "annotation-definitions";
		/// <summary />
		internal const string sAnthroListOAold1 = "anthro_codes";
		/// <summary />
		internal const string sAnthroListOA = "anthro-code";
		/// <summary />
		internal const string sConfidenceLevelsOA = "confidence-levels";
		/// <summary />
		internal const string sEducationOA = "education";
		/// <summary />
		internal const string sGenreListOA = "genres";
		/// <summary />
		internal const string sLocationsOA = "location";
		/// <summary />
		internal const string sPartsOfSpeechOA = "grammatical-info";
		/// <summary />
		internal const string sPartsOfSpeechOAold2 = "FromPartOfSpeech";
		/// <summary />
		internal const string sPartsOfSpeechOAold1 = "from-part-of-speech";
		/// <summary />
		internal const string sPeopleOA = "users";
		/// <summary />
		internal const string sPositionsOA = "positions";
		/// <summary />
		internal const string sRestrictionsOA = "restrictions";
		/// <summary />
		internal const string sRolesOA = "roles";
		/// <summary />
		internal const string sSemanticDomainListOAold1 = "semanticdomainddp4";
		/// <summary />
		internal const string sSemanticDomainListOAold2 = "semantic_domain";
		/// <summary />
		internal const string sSemanticDomainListOAold3 = "semantic-domain";
		/// <summary />
		internal const string sSemanticDomainListOA = "semantic-domain-ddp4";
		/// <summary />
		internal const string sStatusOA = "status";
		/// <summary />
		internal const string sThesaurusRA = "thesaurus";
		/// <summary />
		internal const string sTranslationTagsOAold1 = "translation-types";
		/// <summary />
		internal const string sTranslationTagsOA = "translation-type";
		/// <summary />
		internal const string sProdRestrictOA = "exception-feature";
		/// <summary />
		internal const string sProdRestrictOAfrom = "from-exception-feature";
		/// <summary />
		internal const string sDbComplexEntryTypesOA = "complex-form-types";
		/// <summary />
		internal const string sDbDialectLabelsOA = "dialect-labels";
		/// <summary />
		internal const string sDbDomainTypesOA = "domain-type";
		/// <summary />
		internal const string sDbDomainTypesOAold1 = "domaintype";
		/// <summary />
		internal const string sDbLanguagesOA = "languages";
		/// <summary />
		internal const string sDbMorphTypesOAold = "MorphType";
		/// <summary />
		internal const string sDbMorphTypesOA = "morph-type";
		/// <summary />
		internal const string sDbPublicationTypesOA = "do-not-publish-in";
		/// <summary />
		internal const string sDbPublicationTypesOAold = "publishin";
		/// <summary />
		internal const string sDbReferencesOAold = "lexical-relations";
		/// <summary />
		internal const string sDbReferencesOA = "lexical-relation";
		/// <summary />
		internal const string sDbSenseTypesOA = "sense-type";
		/// <summary />
		internal const string sDbSenseTypesOAold1 = "sensetype";
		/// <summary />
		internal const string sDbUsageTypesOAold = "usagetype";
		/// <summary />
		internal const string sDbUsageTypesOA = "usage-type";
		/// <summary />
		internal const string sDbVariantEntryTypesOA = "variant-types";
		/// <summary />
		internal const string sMSAinflectionFeature = "inflection-feature";
		/// <summary />
		internal const string sMSAfromInflectionFeature = "from-inflection-feature";
		/// <summary />
		internal const string sMSAinflectionFeatureType = "inflection-feature-type";
		/// <summary />
		internal const string sReversalType = "reversal-type";

		/// <summary>
		/// Return the LIFT range name for a given cmPossibilityList. Get the fieldName
		/// of the owning field and use that to get the range name.
		/// </summary>
		internal static string GetRangeNameForLiftExport(IFwMetaDataCacheManaged mdc, ICmPossibilityList list)
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

		internal static bool RangeNameIsCustomList(string range)
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
					return !range.EndsWith("-slot") && !range.EndsWith("-Slots") && !range.EndsWith("-infl-class")
						   && !range.EndsWith("-InflClasses") && !range.EndsWith("-feature-value") && !range.EndsWith("-stem-name");
			}
		}

		/// <summary>
		/// Return the LIFT range name for a given fieldName in Flex
		/// </summary>
		private static string GetRangeName(string fieldName)
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
	}
}