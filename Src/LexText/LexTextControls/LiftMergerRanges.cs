using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Palaso.Lift;
using Palaso.Lift.Parsing;
using Palaso.WritingSystems.Migration;
using Palaso.WritingSystems.Migration.WritingSystemsLdmlV0To1Migration;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// This class is called by the LiftParser, as it encounters each element of a lift file.
	/// There is at least one other ILexiconMerger implementation, used in WeSay.
	/// </summary>
	public partial class FlexLiftMerger : ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>
	{
		// lists of new items added to lists
		readonly List<ICmPossibility> m_rgnewPos = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewMmt = new List<ICmPossibility>();
		readonly List<ILexEntryType> m_rgnewComplexFormType = new List<ILexEntryType>();
		readonly List<ILexEntryType> m_rgnewVariantType = new List<ILexEntryType>();
		readonly List<ICmPossibility> m_rgnewSemDom = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewTransType = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewCondition = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewAnthroCode = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewDomainType = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewPublicationType = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewSenseType = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewStatus = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewUsageType = new List<ICmPossibility>();
		readonly List<ICmLocation> m_rgnewLocation = new List<ICmLocation>();
		readonly List<ICmPossibility> m_rgnewPerson = new List<ICmPossibility>();
		readonly List<IPhEnvironment> m_rgnewEnvirons = new List<IPhEnvironment>();
		readonly List<ICmPossibility> m_rgnewLexRefTypes = new List<ICmPossibility>();
		readonly List<IMoInflClass> m_rgnewInflClasses = new List<IMoInflClass>();
		readonly List<IMoInflAffixSlot> m_rgnewSlots = new List<IMoInflAffixSlot>();
		readonly List<ICmPossibility> m_rgnewExceptFeat = new List<ICmPossibility>();
		readonly List<IMoStemName> m_rgnewStemName = new List<IMoStemName>();

		//New
		readonly List<ICmPossibility> m_rgAffixCategories = new List<ICmPossibility>();

		readonly List<IFsFeatDefn> m_rgnewFeatDefn = new List<IFsFeatDefn>();
		readonly List<IFsFeatStrucType> m_rgnewFeatStrucType = new List<IFsFeatStrucType>();

		readonly List<FieldDescription> m_rgnewCustomFields = new List<FieldDescription>();

		// Maps for quick lookup of list items. These are populated when importing the .lift-ranges file.
		// The quick lookup is used when importing data from the .lift file.
		// Keys are NFC-normalized versions of the values of possibilities in the corresponding lists (non-safe-XML).
		readonly Dictionary<string, ICmPossibility> m_dictPos = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictMmt = new Dictionary<string, ICmPossibility>(19);
		readonly Dictionary<string, ICmPossibility> m_dictComplexFormType = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictVariantType = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictSemDom = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictTransType = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictAnthroCode = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictDomainType = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictSenseType = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictStatus = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictUsageType = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictLocation = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictPerson = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, List<IPhEnvironment>> m_dictEnvirons = new Dictionary<string, List<IPhEnvironment>>();
		readonly Dictionary<string, ICmPossibility> m_dictLexRefTypes = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictRevLexRefTypes = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictExceptFeats = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictPublicationTypes = new Dictionary<string, ICmPossibility>();

		//New
		readonly Dictionary<string, ICmPossibility> m_dictAffixCategories = new Dictionary<string, ICmPossibility>();
		// maps for quick lookup of list items  for each of the custom lists.
		//m_dictCustomLists key is the CustomList name
		private Dictionary<string, Dictionary<string, ICmPossibility>> m_dictCustomLists =
													new Dictionary<string, Dictionary<string, ICmPossibility>>();
		//Lists of new items added to the Custom Lists
		private Dictionary<string, List<ICmPossibility>> m_rgnewCustoms = new Dictionary<string, List<ICmPossibility>>();

		//Guid of all CmPossibility Lists and their names.
		private List<Guid> m_PossibilityListGuids = new List<Guid>();

		//All custom CmPossibility lists names and Guids
		private Dictionary<string, Guid> m_rangeNamesToPossibilityListGuids = new Dictionary<string, Guid>();

		//map from Custom field name to PossibilityList.Guid for custom fields which contain cmPossibilityList data.
		readonly Dictionary<string, Guid> m_CustomFieldNamesToPossibilityListGuids = new Dictionary<string, Guid>();
		//============================================================================
		#region Methods to find or create list items
		/// <summary>
		///word
		/// </summary>
		/// <param name="val">non-safe-XML</param>
		/// <returns></returns>
		private IPartOfSpeech FindOrCreatePartOfSpeech(string val)
		{
			ICmPossibility poss;
			if (m_dictPos.TryGetValue(val, out poss) ||
				m_dictPos.TryGetValue(val.ToLowerInvariant(), out poss))
			{
				return poss as IPartOfSpeech;
			}
			IPartOfSpeech pos = CreateNewPartOfSpeech();
			m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			// Try to find this in the category catalog list, so we can add in more information.
			EticCategory cat = FindMatchingEticCategory(val);
			if (cat != null)
				AddEticCategoryInfo(cat, pos as IPartOfSpeech);
			if (pos.Name.AnalysisDefaultWritingSystem.Length == 0)
				pos.Name.set_String(m_cache.DefaultAnalWs, val);
			if (pos.Abbreviation.AnalysisDefaultWritingSystem.Length == 0)
				pos.Abbreviation.set_String(m_cache.DefaultAnalWs, val);
			m_dictPos.Add(val, pos);
			m_rgnewPos.Add(pos);
			return pos;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="val">non-safe-XML</param>
		/// <returns></returns>
		private EticCategory FindMatchingEticCategory(string val)
		{
			string sVal = val.ToLowerInvariant();
			foreach (EticCategory cat in m_rgcats)
			{
				foreach (string lang in cat.MultilingualName.Keys)
				{
					string sName = cat.MultilingualName[lang];
					if (sName.ToLowerInvariant() == sVal)
						return cat;
				}
				foreach (string lang in cat.MultilingualAbbrev.Keys)
				{
					string sAbbrev = cat.MultilingualAbbrev[lang];
					if (sAbbrev.ToLowerInvariant() == sVal)
						return cat;
				}
			}
			return null;
		}

		private List<IPhEnvironment> FindOrCreateEnvironment(string sEnv)
		{
			List<IPhEnvironment> rghvo;
			if (!m_dictEnvirons.TryGetValue(sEnv, out rghvo))
			{
				IPhEnvironment envNew = CreateNewPhEnvironment();
				m_cache.LangProject.PhonologicalDataOA.EnvironmentsOS.Add(envNew);
				envNew.StringRepresentation = m_cache.TsStrFactory.MakeString(sEnv, m_cache.DefaultAnalWs);
				rghvo = new List<IPhEnvironment>();
				rghvo.Add(envNew);
				m_dictEnvirons.Add(sEnv, rghvo);
				m_rgnewEnvirons.Add(envNew);
			}
			return rghvo;
		}

		private IMoMorphType FindMorphType(string sTypeName)
		{
			ICmPossibility mmt;
			if (m_dictMmt.TryGetValue(sTypeName, out mmt) ||
				m_dictMmt.TryGetValue(sTypeName.ToLowerInvariant(), out mmt))
			{
				return mmt as IMoMorphType;
			}
			// This seems the most suitable default value.  Returning null causes crashes.
			// (See FWR-3869.)
			int count;
			if (!m_mapMorphTypeUnknownCount.TryGetValue(sTypeName, out count))
			{
				count = 0;
				m_mapMorphTypeUnknownCount.Add(sTypeName, count);
			}
			++count;
			m_mapMorphTypeUnknownCount[sTypeName] = count;
			return GetExistingMoMorphType(MoMorphTypeTags.kguidMorphStem);
		}

		private ILexRefType FindOrCreateLexRefType(string relationTypeName, bool fIsSequence)
		{
			ICmPossibility poss;
			if (m_dictLexRefTypes.TryGetValue(relationTypeName, out poss) ||
				m_dictLexRefTypes.TryGetValue(relationTypeName.ToLowerInvariant(), out poss))
			{
				return poss as ILexRefType;
			}
			if (m_dictRevLexRefTypes.TryGetValue(relationTypeName, out poss) ||
				m_dictRevLexRefTypes.TryGetValue(relationTypeName.ToLowerInvariant(), out poss))
			{
				return poss as ILexRefType;
			}
			ILexRefType lrt = CreateNewLexRefType();
			m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(lrt);
			lrt.Name.set_String(m_cache.DefaultAnalWs, relationTypeName);
			if ((String.IsNullOrEmpty(m_sLiftProducer) || m_sLiftProducer.StartsWith("WeSay")) &&
				(relationTypeName == "BaseForm"))
			{
				lrt.Abbreviation.set_String(m_cache.DefaultAnalWs, "base");
				lrt.ReverseName.set_String(m_cache.DefaultAnalWs, "Derived Forms");
				lrt.ReverseAbbreviation.set_String(m_cache.DefaultAnalWs, "deriv");
				lrt.MappingType = (int)LexRefTypeTags.MappingTypes.kmtEntryTree;
			}
			else
			{
				lrt.Abbreviation.set_String(m_cache.DefaultAnalWs, relationTypeName);
				if (fIsSequence)
					lrt.MappingType = (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence;
				else
					lrt.MappingType = (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection;
			}
			m_dictLexRefTypes.Add(relationTypeName, lrt);
			m_rgnewLexRefTypes.Add(lrt);
			return lrt;
		}

		private ILexEntryType FindComplexFormType(string sOldEntryType)
		{
			ICmPossibility poss;
			if (m_dictComplexFormType.TryGetValue(sOldEntryType, out poss) ||
				m_dictComplexFormType.TryGetValue(sOldEntryType.ToLowerInvariant(), out poss))
			{
				return poss as ILexEntryType;
			}
			return null;
		}

		private ILexEntryType FindVariantType(string sOldEntryType)
		{
			ICmPossibility poss;
			if (m_dictVariantType.TryGetValue(sOldEntryType, out poss) ||
				m_dictVariantType.TryGetValue(sOldEntryType.ToLowerInvariant(), out poss))
			{
				return poss as ILexEntryType;
			}
			return null;
		}

		private ILexEntryType FindOrCreateComplexFormType(string sType)
		{
			ICmPossibility poss;
			if (m_dictComplexFormType.TryGetValue(sType, out poss) ||
				m_dictComplexFormType.TryGetValue(sType.ToLowerInvariant(), out poss))
			{
				return poss as ILexEntryType;
			}
			ILexEntryType let = CreateNewLexEntryType();
			m_cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.Add(let);
			let.Abbreviation.set_String(m_cache.DefaultAnalWs, sType);
			let.Name.set_String(m_cache.DefaultAnalWs, sType);
			let.ReverseAbbr.set_String(m_cache.DefaultAnalWs, sType);
			m_dictComplexFormType.Add(sType, let);
			m_rgnewComplexFormType.Add(let);
			return let;
		}

		private ILexEntryType FindOrCreateVariantType(string sType)
		{
			ICmPossibility poss;
			if (m_dictVariantType.TryGetValue(sType, out poss) ||
				m_dictVariantType.TryGetValue(sType.ToLowerInvariant(), out poss))
			{
				return poss as ILexEntryType;
			}
			ILexEntryType let = CreateNewLexEntryType();
			m_cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Add(let);
			let.Abbreviation.set_String(m_cache.DefaultAnalWs, sType);
			let.Name.set_String(m_cache.DefaultAnalWs, sType);
			let.ReverseAbbr.set_String(m_cache.DefaultAnalWs, sType);
			m_dictVariantType.Add(sType, let);
			m_rgnewVariantType.Add(let);
			return let;
		}

		private ICmAnthroItem FindOrCreateAnthroCode(string traitValue)
		{
			ICmPossibility poss;
			if (m_dictAnthroCode.TryGetValue(traitValue, out poss) ||
				m_dictAnthroCode.TryGetValue(traitValue.ToLowerInvariant(), out poss))
			{
				return poss as ICmAnthroItem;
			}
			ICmAnthroItem ant = CreateNewCmAnthroItem();
			m_cache.LangProject.AnthroListOA.PossibilitiesOS.Add(ant);
			ant.Abbreviation.set_String(m_cache.DefaultAnalWs, traitValue);
			ant.Name.set_String(m_cache.DefaultAnalWs, traitValue);
			m_dictAnthroCode.Add(traitValue, ant);
			m_rgnewAnthroCode.Add(ant);
			return ant;
		}

		private ICmSemanticDomain FindOrCreateSemanticDomain(string traitValue)
		{
			ICmPossibility poss;
			if (m_dictSemDom.TryGetValue(traitValue, out poss) ||
				m_dictSemDom.TryGetValue(traitValue.ToLowerInvariant(), out poss))
			{
				return poss as ICmSemanticDomain;
			}
			ICmSemanticDomain sem = CreateNewCmSemanticDomain();
			m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Add(sem);
			sem.Abbreviation.set_String(m_cache.DefaultAnalWs, traitValue);
			sem.Name.set_String(m_cache.DefaultAnalWs, traitValue);
			m_dictSemDom.Add(traitValue, sem);
			m_rgnewSemDom.Add(sem);
			return sem;
		}

		private ICmPossibility FindOrCreateDomainType(string traitValue)
		{
			ICmPossibility poss;
			if (m_dictDomainType.TryGetValue(traitValue, out poss) ||
				m_dictDomainType.TryGetValue(traitValue.ToLowerInvariant(), out poss))
			{
				return poss;
			}
			poss = CreateNewCmPossibility();
			m_cache.LangProject.LexDbOA.DomainTypesOA.PossibilitiesOS.Add(poss);
			poss.Abbreviation.set_String(m_cache.DefaultAnalWs, traitValue);
			poss.Name.set_String(m_cache.DefaultAnalWs, traitValue);
			m_dictDomainType.Add(traitValue, poss);
			m_rgnewDomainType.Add(poss);
			return poss;
		}

		private ICmPossibility FindOrCreateSenseType(string traitValue)
		{
			ICmPossibility poss;
			if (m_dictSenseType.TryGetValue(traitValue, out poss) ||
				m_dictSenseType.TryGetValue(traitValue.ToLowerInvariant(), out poss))
			{
				return poss;
			}
			poss = CreateNewCmPossibility();
			m_cache.LangProject.LexDbOA.SenseTypesOA.PossibilitiesOS.Add(poss);
			poss.Abbreviation.set_String(m_cache.DefaultAnalWs, traitValue);
			poss.Name.set_String(m_cache.DefaultAnalWs, traitValue);
			m_dictSenseType.Add(traitValue, poss);
			m_rgnewSenseType.Add(poss);
			return poss;
		}

		private ICmPossibility FindOrCreateStatus(string traitValue)
		{
			ICmPossibility poss;
			if (m_dictStatus.TryGetValue(traitValue, out poss) ||
				m_dictStatus.TryGetValue(traitValue.ToLowerInvariant(), out poss))
			{
				return poss;
			}
			poss = CreateNewCmPossibility();
			m_cache.LangProject.StatusOA.PossibilitiesOS.Add(poss);
			poss.Abbreviation.set_String(m_cache.DefaultAnalWs, traitValue);
			poss.Name.set_String(m_cache.DefaultAnalWs, traitValue);
			m_dictStatus.Add(traitValue, poss);
			m_rgnewStatus.Add(poss);
			return poss;
		}

		private ICmPossibility FindOrCreateTranslationType(string sType)
		{
			ICmPossibility poss;
			if (m_dictTransType.TryGetValue(sType, out poss) ||
				m_dictTransType.TryGetValue(sType.ToLowerInvariant(), out poss))
			{
				return poss;
			}
			poss = CreateNewCmPossibility();
			m_cache.LangProject.TranslationTagsOA.PossibilitiesOS.Add(poss);
			poss.Name.set_String(m_cache.DefaultAnalWs, sType);
			m_dictTransType.Add(sType, poss);
			m_rgnewTransType.Add(poss);
			return poss;
		}

		private ICmPossibility FindOrCreateUsageType(string traitValue)
		{
			ICmPossibility poss;
			if (m_dictUsageType.TryGetValue(traitValue, out poss) ||
				m_dictUsageType.TryGetValue(traitValue.ToLowerInvariant(), out poss))
			{
				return poss;
			}
			poss = CreateNewCmPossibility();
			m_cache.LangProject.LexDbOA.UsageTypesOA.PossibilitiesOS.Add(poss);
			poss.Abbreviation.set_String(m_cache.DefaultAnalWs, traitValue);
			poss.Name.set_String(m_cache.DefaultAnalWs, traitValue);
			m_dictUsageType.Add(traitValue, poss);
			m_rgnewUsageType.Add(poss);
			return poss;
		}

		private ICmLocation FindOrCreateLocation(string traitValue)
		{
			ICmPossibility poss;
			if (m_dictLocation.TryGetValue(traitValue, out poss) ||
				m_dictLocation.TryGetValue(traitValue.ToLowerInvariant(), out poss))
			{
				return poss as ICmLocation;
			}
			ICmLocation loc = CreateNewCmLocation();
			m_cache.LangProject.LocationsOA.PossibilitiesOS.Add(loc);
			loc.Abbreviation.set_String(m_cache.DefaultAnalWs, traitValue);
			loc.Name.set_String(m_cache.DefaultAnalWs, traitValue);
			m_dictLocation.Add(traitValue, loc);
			m_rgnewLocation.Add(loc);
			return loc;
		}

		private ICmPossibility FindOrCreatePublicationType(string traitValue)
		{
			ICmPossibility poss;
			if (m_dictPublicationTypes.TryGetValue(traitValue, out poss) ||
				m_dictPublicationTypes.TryGetValue(traitValue.ToLowerInvariant(), out poss))
			{
				return poss;
			}
			poss = CreateNewCmPossibility();
			m_cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(poss);
			poss.Abbreviation.set_String(m_cache.DefaultAnalWs, traitValue);
			poss.Name.set_String(m_cache.DefaultAnalWs, traitValue);
			m_dictPublicationTypes.Add(traitValue, poss);
			m_rgnewPublicationType.Add(poss);
			return poss;
		}

		#endregion // Methods to find or create list items

		//===========================================================================

		#region Methods for displaying list items created during import
		/// <summary>
		/// Summarize what we added to the known lists.
		/// </summary>
		public string DisplayNewListItems(string sLIFTFile, int cEntriesRead)
		{
			string sDir = System.IO.Path.GetDirectoryName(sLIFTFile);
			string sLogFile = String.Format("{0}-ImportLog.htm",
				System.IO.Path.GetFileNameWithoutExtension(sLIFTFile));
			string sHtmlFile = System.IO.Path.Combine(sDir, sLogFile);
			DateTime dtEnd = DateTime.Now;

			using (var writer = new StreamWriter(sHtmlFile, false, System.Text.Encoding.UTF8))
			{
				string sTitle = String.Format(LexTextControls.ksImportLogFor0, sLIFTFile);
				writer.WriteLine("<html>");
				writer.WriteLine("<head>");
				writer.WriteLine("<title>{0}</title>", sTitle);
				writer.WriteLine("</head>");
				writer.WriteLine("<body>");
				writer.WriteLine("<h2>{0}</h2>", sTitle);
				long deltaTicks = dtEnd.Ticks - m_dtStart.Ticks;	// number of 100-nanosecond intervals
				int deltaMsec = (int)((deltaTicks + 5000L) / 10000L);	// round off to milliseconds
				int deltaSec = deltaMsec / 1000;
				string sDeltaTime = String.Format(LexTextControls.ksImportingTookTime,
					System.IO.Path.GetFileName(sLIFTFile), deltaSec, deltaMsec % 1000);
				writer.WriteLine("<p>{0}</p>", sDeltaTime);
				string sEntryCounts = String.Format(LexTextControls.ksEntriesImportCounts,
					cEntriesRead, m_cEntriesAdded, m_cSensesAdded, m_cEntriesDeleted);
				writer.WriteLine("<p><h3>{0}</h3></p>", sEntryCounts);
				ListNewPossibilities(writer, LexTextControls.ksPartsOfSpeechAdded, m_rgnewPos);
				ListNewPossibilities(writer, LexTextControls.ksMorphTypesAdded, m_rgnewMmt);
				ListNewLexEntryTypes(writer, LexTextControls.ksComplexFormTypesAdded, m_rgnewComplexFormType);
				ListNewLexEntryTypes(writer, LexTextControls.ksVariantTypesAdded, m_rgnewVariantType);
				ListNewPossibilities(writer, LexTextControls.ksSemanticDomainsAdded, m_rgnewSemDom);
				ListNewPossibilities(writer, LexTextControls.ksTranslationTypesAdded, m_rgnewTransType);
				ListNewPossibilities(writer, LexTextControls.ksConditionsAdded, m_rgnewCondition);
				ListNewPossibilities(writer, LexTextControls.ksAnthropologyCodesAdded, m_rgnewAnthroCode);
				ListNewPossibilities(writer, LexTextControls.ksDomainTypesAdded, m_rgnewDomainType);
				ListNewPossibilities(writer, LexTextControls.ksSenseTypesAdded, m_rgnewSenseType);
				ListNewPossibilities(writer, LexTextControls.ksPeopleAdded, m_rgnewPerson);
				ListNewPossibilities(writer, LexTextControls.ksStatusValuesAdded, m_rgnewStatus);
				ListNewPossibilities(writer, LexTextControls.ksUsageTypesAdded, m_rgnewUsageType);
				ListNewEnvironments(writer, LexTextControls.ksEnvironmentsAdded, m_rgnewEnvirons);
				ListNewPossibilities(writer, LexTextControls.ksLexicalReferenceTypesAdded, m_rgnewLexRefTypes);
				ListNewWritingSystems(writer, LexTextControls.ksWritingSystemsAdded, m_addedWss);
				ListNewInflectionClasses(writer, LexTextControls.ksInflectionClassesAdded, m_rgnewInflClasses);
				ListNewSlots(writer, LexTextControls.ksInflectionalAffixSlotsAdded, m_rgnewSlots);
				ListNewPossibilities(writer, LexTextControls.ksExceptionFeaturesAdded, m_rgnewExceptFeat);
				ListNewInflectionalFeatures(writer, LexTextControls.ksInflectionFeaturesAdded, m_rgnewFeatDefn);
				ListNewFeatureTypes(writer, LexTextControls.ksFeatureTypesAdded, m_rgnewFeatStrucType);
				ListNewStemNames(writer, LexTextControls.ksStemNamesAdded, m_rgnewStemName);
				ListNewPossibilities(writer, LexTextControls.ksPublicationTypesAdded, m_rgnewPublicationType);
				ListNewCustomFields(writer, LexTextControls.ksCustomFieldsAdded, m_rgnewCustomFields);
				ListConflictsFound(writer, LexTextControls.ksConflictsResultedInDup, m_rgcdConflicts);
				ListInvalidData(writer);
				ListTruncatedData(writer);
				ListInvalidRelations(writer);
				ListCombinedCollections(writer);
				ListInvalidMorphTypes(writer);
				ListErrorMessages(writer);
				writer.WriteLine("</body>");
				writer.WriteLine("</html>");
				writer.Close();
				return sHtmlFile;
			}
		}

		private void ListConflictsFound(StreamWriter writer, string sMsg,
			List<ConflictingData> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<table border=\"1\" width=\"100%\">");
				writer.WriteLine("<tbody>");
				writer.WriteLine("<caption><h3>{0}</h3></caption>", sMsg);
				writer.WriteLine("<tr>");
				writer.WriteLine("<th width=\"16%\">{0}</th>", LexTextControls.ksType);
				writer.WriteLine("<th width=\"28%\">{0}</th>", LexTextControls.ksConflictingField);
				writer.WriteLine("<th width=\"28%\">{0}</th>", LexTextControls.ksOriginal);
				writer.WriteLine("<th width=\"28%\">{0}</th>", LexTextControls.ksNewDuplicate);
				writer.WriteLine("</tr>");
				foreach (ConflictingData cd in list)
				{
					writer.WriteLine("<tr>");
					writer.WriteLine("<td width=\"16%\">{0}</td>", cd.ConflictType);
					writer.WriteLine("<td width=\"28%\">{0}</td>", cd.ConflictField);
					writer.WriteLine("<td width=\"28%\">{0}</td>", cd.OrigHtmlReference());
					writer.WriteLine("<td width=\"28%\">{0}</td>", cd.DupHtmlReference());
					writer.WriteLine("</tr>");
				}
				writer.WriteLine("</tbody>");
				writer.WriteLine("</table>");
				writer.WriteLine("<p>{0}</p>", LexTextControls.ksClickOnHyperLinks);
			}

		}

		private void ListTruncatedData(StreamWriter writer)
		{
			if (m_rgTruncated.Count == 0)
				return;
			writer.WriteLine("<table border=\"1\" width=\"100%\">");
			writer.WriteLine("<tbody>");
			writer.WriteLine("<caption><h3>{0}</h3></caption>", LexTextControls.ksTruncatedOnImport);
			writer.WriteLine("<tr>");
			writer.WriteLine("<th width=\"15%\">{0}</th>", LexTextControls.ksTruncatedField);
			writer.WriteLine("<th width=\"10%\">{0}</th>", LexTextControls.ksStoredLength);
			writer.WriteLine("<th width=\"15%\">{0}</th>", LexTextControls.ksWritingSystem);
			writer.WriteLine("<th width=\"20%\">{0}</th>", LexTextControls.ksEntry);
			writer.WriteLine("<th width=\"40%\">{0}</th>", LexTextControls.ksOriginalValue);
			writer.WriteLine("</tr>");
			foreach (TruncatedData td in m_rgTruncated)
			{
				writer.WriteLine("<tr>");
				writer.WriteLine("<td width=\"15%\">{0}</td>", td.FieldName);
				writer.WriteLine("<td width=\"10%\">{0}</td>", td.StoredLength);
				writer.WriteLine("<td width=\"15%\">{0}</td>", td.WritingSystem);
				writer.WriteLine("<td width=\"20%\">{0}</td>", td.EntryHtmlReference());
				writer.WriteLine("<td width=\"40%\">{0}</td>", td.OriginalText);
				writer.WriteLine("</tr>");
			}
			writer.WriteLine("</tbody>");
			writer.WriteLine("</table>");
			writer.WriteLine("<p>{0}</p>", LexTextControls.ksClickOnHyperLinks);
		}

		private void ListInvalidData(StreamWriter writer)
		{
			if (m_rgInvalidData.Count == 0)
				return;
			writer.WriteLine("<table border=\"1\" width=\"100%\">");
			writer.WriteLine("<tbody>");
			writer.WriteLine("<caption><h3>{0}</h3></caption>", LexTextControls.ksInvalidDataImported);
			writer.WriteLine("<tr>");
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksEntry);
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksField);
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksInvalidValue);
			writer.WriteLine("<th width=\"49%\">{0}</th>", LexTextControls.ksErrorMessage);
			writer.WriteLine("</tr>");
			foreach (InvalidData bad in m_rgInvalidData)
			{
				writer.WriteLine("<tr>");
				writer.WriteLine("<td width=\"17%\">{0}</td>", bad.EntryHtmlReference());
				writer.WriteLine("<td width=\"17%\">{0}</td>", bad.FieldName);
				writer.WriteLine("<td width=\"17%\">{0}</td>", bad.BadValue);
				writer.WriteLine("<td width=\"49%\">{0}</td>", bad.ErrorMessage);
				writer.WriteLine("</tr>");
			}
			writer.WriteLine("</tbody>");
			writer.WriteLine("</table>");
			writer.WriteLine("<p>{0}</p>", LexTextControls.ksClickOnHyperLinks);
		}

		private void ListInvalidRelations(StreamWriter writer)
		{
			if (m_rgInvalidRelation.Count == 0)
				return;
			writer.WriteLine("<table border=\"1\" width=\"100%\">");
			writer.WriteLine("<tbody>");
			writer.WriteLine("<caption><h3>{0}</h3></caption>", LexTextControls.ksInvalidRelationsHeader);
			writer.WriteLine("<tr>");
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksEntry);
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksRelationType);
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksInvalidReference);
			writer.WriteLine("<th width=\"49%\">{0}</th>", LexTextControls.ksErrorMessage);
			writer.WriteLine("</tr>");
			foreach (InvalidRelation bad in m_rgInvalidRelation)
			{
				writer.WriteLine("<tr>");
				writer.WriteLine("<td width=\"17%\">{0}</td>", bad.EntryHtmlReference());
				writer.WriteLine("<td width=\"17%\">{0}</td>", bad.TypeName);
				writer.WriteLine("<td width=\"17%\">{0}</td>", bad.BadValue);
				writer.WriteLine("<td width=\"49%\">{0}</td>", bad.ErrorMessage);
				writer.WriteLine("</tr>");
			}
			writer.WriteLine("</tbody>");
			writer.WriteLine("</table>");
			writer.WriteLine("<p>{0}</p>", LexTextControls.ksClickOnHyperLinks);
		}

		private void ListCombinedCollections(StreamWriter writer)
		{
			if(m_combinedCollections.Count == 0)
				return;

			writer.WriteLine("<table border=\"1\" width=\"100%\">");
			writer.WriteLine("<tbody>");
			writer.WriteLine("<caption><h3>{0}</h3></caption>", LexTextControls.ksCombinedCollections);
			writer.WriteLine("<tr>");
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksEntry);
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksRelationType);
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksAddedItem); //column header for items added in combining two collections during lift merge
			writer.WriteLine("<th width=\"49%\">{0}</th>", LexTextControls.ksErrorMessage);
			writer.WriteLine("</tr>");
			foreach (var bad in m_combinedCollections)
			{
				writer.WriteLine("<tr>");
				writer.WriteLine("<td width=\"17%\">{0}</td>", bad.EntryHtmlReference());
				writer.WriteLine("<td width=\"17%\">{0}</td>", bad.TypeName);
				writer.WriteLine("<td width=\"17%\">{0}</td>", bad.BadValue);
				writer.WriteLine("<td width=\"49%\">{0}</td>", bad.ErrorMessage);
				writer.WriteLine("</tr>");
			}
			writer.WriteLine("</tbody>");
			writer.WriteLine("</table>");
		}

		private void ListInvalidMorphTypes(StreamWriter writer)
		{
			if (m_mapMorphTypeUnknownCount.Count == 0)
				return;
			writer.WriteLine("<table border=\"1\" width=\"100%\">");
			writer.WriteLine("<tbody>");
			writer.WriteLine("<caption><h3>{0}</h3></caption>", LexTextControls.ksUnknownMorphTypes);
			writer.WriteLine("<tr>");
			writer.WriteLine("<th width=\"83%\">{0}</th>", LexTextControls.ksName);
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksReferenceCount);
			writer.WriteLine("</tr>");
			foreach (var bad in m_mapMorphTypeUnknownCount)
			{
				writer.WriteLine("<tr>");
				writer.WriteLine("<td width=\"83%\">{0}</td>", bad.Key);
				writer.WriteLine("<td width=\"17%\">{0}</td>", bad.Value);
				writer.WriteLine("</tr>");
			}
			writer.WriteLine("</tbody>");
			writer.WriteLine("</table>");
		}

		private void ListErrorMessages(StreamWriter writer)
		{
			if (m_rgErrorMsgs.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", LexTextControls.ksErrorsEncounteredHeader);
				writer.WriteLine("<ul>");
				foreach (string msg in m_rgErrorMsgs)
					writer.WriteLine("<li>{0}</li>", msg);
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewLexEntryTypes(StreamWriter writer, string sMsg, List<ILexEntryType> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (ILexEntryType type in list)
					writer.WriteLine("<li>{0} / {1}</li>", type.AbbrAndName, type.ReverseAbbr.BestAnalysisAlternative.Text);
				writer.WriteLine("</ul>");
			}
		}

		private static void ListNewPossibilities(System.IO.StreamWriter writer, string sMsg,
			List<ICmPossibility> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (ICmPossibility poss in list)
					writer.WriteLine("<li>{0}</li>", poss.AbbrAndName);
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewEnvironments(System.IO.StreamWriter writer, string sMsg,
			List<IPhEnvironment> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (IPhEnvironment env in list)
					writer.WriteLine("<li>{0}</li>", env.StringRepresentation.Text);
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewWritingSystems(System.IO.StreamWriter writer, string sMsg,
			List<IWritingSystem> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (IWritingSystem ws in list)
					writer.WriteLine("<li>{0} ({1})</li>", ws.DisplayLabel, ws.Id);
				writer.WriteLine("</ul>");
			}
		}
		private void ListNewInflectionClasses(System.IO.StreamWriter writer, string sMsg, List<IMoInflClass> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (IMoInflClass infl in list)
				{
					string sPos = String.Empty;
					ICmObject cmo = infl.Owner;
					while (cmo != null && cmo.ClassID != PartOfSpeechTags.kClassId)
					{
						Debug.Assert(cmo.ClassID == MoInflClassTags.kClassId);
						if (cmo.ClassID == MoInflClassTags.kClassId)
						{
							IMoInflClass owner = cmo as IMoInflClass;
							sPos.Insert(0, String.Format(": {0}", owner.Name.BestAnalysisVernacularAlternative.Text));
						}
						cmo = cmo.Owner;
					}
					if (cmo != null)
					{
						IPartOfSpeech pos = cmo as IPartOfSpeech;
						sPos = sPos.Insert(0, pos.Name.BestAnalysisVernacularAlternative.Text);
					}
					writer.WriteLine("<li>{0}: {1}</li>", sPos, infl.Name.BestAnalysisVernacularAlternative.Text);
				}
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewSlots(System.IO.StreamWriter writer, string sMsg, List<IMoInflAffixSlot> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (IMoInflAffixSlot slot in list)
				{
					string sPos = String.Empty;
					ICmObject cmo = slot.Owner;
					if (cmo != null && cmo is IPartOfSpeech)
					{
						IPartOfSpeech pos = cmo as IPartOfSpeech;
						sPos = pos.Name.BestAnalysisVernacularAlternative.Text;
					}
					writer.WriteLine("<li>{0}: {1}</li>", sPos, slot.Name.BestAnalysisVernacularAlternative.Text);
				}
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewInflectionalFeatures(StreamWriter writer, string sMsg, List<IFsFeatDefn> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (IFsFeatDefn feat in list)
				{
					writer.WriteLine("<li>{0} - {1}</li>",
						feat.Abbreviation.BestAnalysisVernacularAlternative.Text,
						feat.Name.BestAnalysisVernacularAlternative.Text);
				}
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewFeatureTypes(StreamWriter writer, string sMsg, List<IFsFeatStrucType> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (IFsFeatStrucType type in list)
				{
					writer.WriteLine("<li>{0} - {1}</li>",
						type.Abbreviation.BestAnalysisVernacularAlternative.Text,
						type.Name.BestAnalysisVernacularAlternative.Text);
				}
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewStemNames(StreamWriter writer, string sMsg, List<IMoStemName> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (IMoStemName stem in list)
				{
					if (stem.Owner is IPartOfSpeech)
					{
						writer.WriteLine("<li>{0} ({1})</li>",
							stem.Name.BestAnalysisVernacularAlternative.Text,
							(stem.Owner as IPartOfSpeech).Name.BestAnalysisVernacularAlternative.Text);
					}
					else if (stem.Owner is IMoInflClass)
					{
						// YAGNI: This isn't (yet) supported by the UI.
					}
				}
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewCustomFields(StreamWriter writer, string sMsg, List<FieldDescription> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (FieldDescription fd in list)
				{
					string sClass = m_cache.MetaDataCacheAccessor.GetClassName(fd.Class);
					writer.WriteLine("<li>{0}: {1}</li>", sClass, fd.Name);
				}
				writer.WriteLine("</ul>");
			}
		}

		#endregion // Methods for displaying list items created during import

		//============================================================================
		#region Import Ranges File
		/// <summary>
		/// This method is a temporary (?) expedient for reading the morph-type information
		/// from a .lift-ranges file.  Someday, the LiftIO.Parsing.LiftParser (or its
		/// Palaso replacement) should handle href values in range elements so that this
		/// method will not be needed.  Without doing this, the user can export morph-type
		/// values in something other than English, and lift import blows up.  See FWR-3869.
		///
		/// Only the morph-type range is handled at present, because the other ranges do not
		/// assume successful matching.
		///
		/// ADDITION June 2001:  This method also imports custom lists from the .lift-ranges file.
		/// </summary>
		public void LoadLiftRanges(string sRangesFile)
		{
			//Store the Guids of all existing possibility lists so that we do not create duplicate custom lists.
			var repo = m_cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			foreach (var list in repo.AllInstances())
			{
				if (!m_PossibilityListGuids.Contains(list.Guid))
					m_PossibilityListGuids.Add(list.Guid);
			}
			StoreStandardListsWithGuids();
			try
			{
				if (!File.Exists(sRangesFile))
					return;
				var xdoc = new XmlDocument();
				xdoc.Load(sRangesFile);
				foreach (XmlNode xn in xdoc.ChildNodes)
				{
					if (xn.Name != "lift-ranges")
						continue;
					foreach (XmlNode xnRange in xn.ChildNodes)
					{
						if (xnRange.Name != "range")
							continue;
						var range = XmlUtils.GetAttributeValue(xnRange, "id");
						if (RangeNames.RangeNameIsCustomList(range))
						{
							var rangeGuid = XmlUtils.GetAttributeValue(xnRange, "guid");
							Dictionary<string, ICmPossibility> dictCustomList;	//used for quick lookup on importing data
							List<ICmPossibility> rgnewCustom;				//used for quick lookup on importing data
							//If the quick lookup dictionary and list have already been created for this custom list
							//then use them again it to account for any additional new range-elements.
							dictCustomList = GetDictCustomList(range);
							rgnewCustom = GetRgnewCustom(range);

							var customList = GetOrCreateCustomList(range, rangeGuid);
							Debug.Assert(customList != null);

							foreach (XmlNode xnElem in xnRange.ChildNodes)
							{
								if (xnElem.Name != "range-element")
									continue;
								ProcessCustomListRangeElement(xnElem, customList, dictCustomList, rgnewCustom);
							}
						}
						else
						{
							foreach (XmlNode xnElem in xnRange.ChildNodes)
							{
								if (xnElem.Name != "range-element")
									continue;
								ProcessRangeElement(xnElem, range);
							}
						}
					}
				}
			}
			catch (Exception)
			{
				// swallow any exception...
			}
		}


		private void StoreStandardListsWithGuids()
		{
			Guid guid;

			AddListNameAndGuid(m_cache.LanguageProject.AffixCategoriesOA, RangeNames.sAffixCategoriesOA);

			AddListNameAndGuid(m_cache.LanguageProject.AnnotationDefsOA, RangeNames.sAnnotationDefsOA);

			AddListNameAndGuid(m_cache.LanguageProject.AnthroListOA, RangeNames.sAnthroListOAold1);
			AddListNameAndGuid(m_cache.LanguageProject.AnthroListOA, RangeNames.sAnthroListOA);

			AddListNameAndGuid(m_cache.LanguageProject.ConfidenceLevelsOA, RangeNames.sConfidenceLevelsOA);

			AddListNameAndGuid(m_cache.LanguageProject.EducationOA, RangeNames.sEducationOA);

			AddListNameAndGuid(m_cache.LanguageProject.GenreListOA, RangeNames.sGenreListOA);

			AddListNameAndGuid(m_cache.LanguageProject.LocationsOA, RangeNames.sLocationsOA);

			AddListNameAndGuid(m_cache.LanguageProject.PartsOfSpeechOA, RangeNames.sPartsOfSpeechOA);
			AddListNameAndGuid(m_cache.LanguageProject.PartsOfSpeechOA, RangeNames.sPartsOfSpeechOAold2);
			AddListNameAndGuid(m_cache.LanguageProject.PartsOfSpeechOA, RangeNames.sPartsOfSpeechOAold1);

			AddListNameAndGuid(m_cache.LanguageProject.PeopleOA, RangeNames.sPeopleOA);

			AddListNameAndGuid(m_cache.LanguageProject.PositionsOA, RangeNames.sPositionsOA);

			AddListNameAndGuid(m_cache.LanguageProject.RestrictionsOA, RangeNames.sRestrictionsOA);

			AddListNameAndGuid(m_cache.LanguageProject.RolesOA, RangeNames.sRolesOA);

			AddListNameAndGuid(m_cache.LanguageProject.SemanticDomainListOA, RangeNames.sSemanticDomainListOAold1);
			AddListNameAndGuid(m_cache.LanguageProject.SemanticDomainListOA, RangeNames.sSemanticDomainListOAold2);
			AddListNameAndGuid(m_cache.LanguageProject.SemanticDomainListOA, RangeNames.sSemanticDomainListOAold3);
			AddListNameAndGuid(m_cache.LanguageProject.SemanticDomainListOA, RangeNames.sSemanticDomainListOA);

			AddListNameAndGuid(m_cache.LanguageProject.StatusOA, RangeNames.sStatusOA);

			AddListNameAndGuid(m_cache.LanguageProject.ThesaurusRA, RangeNames.sThesaurusRA);

			AddListNameAndGuid(m_cache.LanguageProject.TranslationTagsOA, RangeNames.sTranslationTagsOAold1);
			AddListNameAndGuid(m_cache.LanguageProject.TranslationTagsOA, RangeNames.sTranslationTagsOA);

			//=========================================================================================
			AddListNameAndGuid(m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA, RangeNames.sProdRestrictOA);

			//=========================================================================================
			//lists under m_cache.LangProject.LexDbOA
			AddListNameAndGuid(m_cache.LanguageProject.LexDbOA.ComplexEntryTypesOA, RangeNames.sDbComplexEntryTypesOA);

			AddListNameAndGuid(m_cache.LangProject.LexDbOA.DomainTypesOA, RangeNames.sDbDomainTypesOA);

			AddListNameAndGuid(m_cache.LangProject.LexDbOA.MorphTypesOA, RangeNames.sDbMorphTypesOAold);
			AddListNameAndGuid(m_cache.LangProject.LexDbOA.MorphTypesOA, RangeNames.sDbMorphTypesOA);

			AddListNameAndGuid(m_cache.LangProject.LexDbOA.PublicationTypesOA, RangeNames.sDbPublicationTypesOA);
			AddListNameAndGuid(m_cache.LangProject.LexDbOA.PublicationTypesOA, RangeNames.sDbPublicationTypesOAold);

			AddListNameAndGuid(m_cache.LangProject.LexDbOA.ReferencesOA, "references");

			AddListNameAndGuid(m_cache.LanguageProject.LexDbOA.ReferencesOA, RangeNames.sDbReferencesOAold);
			AddListNameAndGuid(m_cache.LanguageProject.LexDbOA.ReferencesOA, RangeNames.sDbReferencesOA);

			AddListNameAndGuid(m_cache.LangProject.LexDbOA.SenseTypesOA, RangeNames.sDbSenseTypesOA);
			AddListNameAndGuid(m_cache.LangProject.LexDbOA.SenseTypesOA, RangeNames.sDbSenseTypesOAold1);


			AddListNameAndGuid(m_cache.LangProject.LexDbOA.UsageTypesOA, RangeNames.sDbUsageTypesOA);

			AddListNameAndGuid(m_cache.LangProject.LexDbOA.VariantEntryTypesOA, RangeNames.sDbVariantEntryTypesOA);
		}

		private void AddListNameAndGuid(ICmPossibilityList possList, string listName)
		{
			if (possList != null)
			{
				m_rangeNamesToPossibilityListGuids.Add(listName, possList.Guid);
			}
		}

		private ICmPossibilityList GetOrCreateCustomList(string customListName, string customListGuid)
		{
			//If the custom list already has been created then just get the reference to it.
			var listGuid = ConvertStringToGuid(customListGuid);
			if (m_PossibilityListGuids.Contains(listGuid))
			{
				var repo = m_cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
				var list = repo.GetObject(listGuid);
				if (list != null)
					return list;
			}

			var ws = m_cache.DefaultUserWs; // get default ws
			var customPossibilityList = m_cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned(
				listGuid, customListName, ws);
			Debug.Assert(customListGuid == customPossibilityList.Guid.ToString());
			customPossibilityList.Name.set_String(m_cache.DefaultAnalWs, customListName);

			//Note: some of the following properties probably need to be set. Look in CustomListDlg.cs to see
			//where this code was originated from.  Are there some defaults that must be set?
			// Set various properties of CmPossibilityList
			customPossibilityList.DisplayOption = (int)PossNameType.kpntName;
			customPossibilityList.PreventDuplicates = true;
			//customPossibilityList.IsSorted = false;
			customPossibilityList.WsSelector = WritingSystemServices.kwsAnalVerns;
			//customPossibilityList.Depth = 1;
			//if (SupportsHierarchy)
			//customPossibilityList.Depth = 127;
			customPossibilityList.Depth = 127;

			//make sure we do not have a duplicate name for Custom Lists.
			m_PossibilityListGuids.Add(listGuid);
			m_rangeNamesToPossibilityListGuids.Add(customListName, listGuid);

			return customPossibilityList;
		}

		private void ProcessCustomListRangeElement(XmlNode xnElem, ICmPossibilityList possList,
					Dictionary<string, ICmPossibility> dictCustomList, List<ICmPossibility> rgnewCustom)
		{
			string guidAttr;
			string parent;
			LiftMultiText description; // non-safe-XML
			LiftMultiText label; // non-safe-XML
			LiftMultiText abbrev; // non-safe-XML
			string id = GetRangeElementDetails(xnElem, out guidAttr, out parent, out description, out label, out abbrev);

			ProcessPossibility(id, guidAttr, parent, MakeSafeLiftMultiText(description), MakeSafeLiftMultiText(label), MakeSafeLiftMultiText(abbrev),
							   dictCustomList, rgnewCustom, possList);
		}

		private void ProcessRangeElement(XmlNode xnElem, string range)
		{
			string guidAttr;
			string parent;
			LiftMultiText description; // non-safe-XML
			LiftMultiText label; // non-safe-XML
			LiftMultiText abbrev; // non-safe-XML
			var id = GetRangeElementDetails(xnElem, out guidAttr, out parent, out description, out label,
				out abbrev);
			(this as ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>).ProcessRangeElement(
				range, id, guidAttr, parent, description, label, abbrev, xnElem.OuterXml);
		}


		/// <summary>
		/// Duplicates code in Palaso.LiftParser, but that doesn't read external range files, so...
		/// </summary>
		/// <param name="xnElem"></param>
		/// <param name="guidAttr"></param>
		/// <param name="parent"></param>
		/// <param name="description">non-safe-XML (read from XmlNode without re-escaping)</param>
		/// <param name="label">non-safe-XML</param>
		/// <param name="abbrev">non-safe-XML</param>
		/// <returns></returns>
		private string GetRangeElementDetails(XmlNode xnElem, out string guidAttr, out string parent, out LiftMultiText description, out LiftMultiText label, out LiftMultiText abbrev)
		{
			var id = XmlUtils.GetAttributeValue(xnElem, "id");
			parent = XmlUtils.GetAttributeValue(xnElem, "parent");
			guidAttr = XmlUtils.GetAttributeValue(xnElem, "guid");
			//var rawXml = xnElem.OuterXml;
			XmlNode xnLabel = null;
			XmlNode xnAbbrev = null;
			XmlNode xnDescription = null;
			foreach (XmlNode xn in xnElem.ChildNodes)
			{
				switch (xn.Name)
				{
					case "label": xnLabel = xn; break;
					case "abbrev": xnAbbrev = xn; break;
					case "description": xnDescription = xn; break;
				}
			}
			label = null;
			if (xnLabel != null)
				label = ReadMultiText(xnLabel);
			abbrev = null;
			if (xnAbbrev != null)
				abbrev = ReadMultiText(xnAbbrev);
			description = null;
			if (xnDescription != null)
				description = ReadMultiText(xnDescription);
			return id;
		}


		/// <summary>
		/// NOTE:  This method needs to be removed. The Palaso library (LiftParser and LiftMultiText) need to be refactored.
		/// Right now something like this does not work.... description = new LiftMultiText(xnDescription.OuterXml);
		/// because only OriginalOuterXml is assigned too.
		/// </summary>
		/// <param name="node"></param>
		/// <returns>non-safe XML (read from XmlNode without re-escaping)</returns>
		internal LiftMultiText ReadMultiText(XmlNode node)
		{
			LiftMultiText text = new LiftMultiText();
			ReadFormNodes(node.SelectNodes("form"), text);
			return text;
		}

		/// <summary>
		/// NOTE:  This method needs to be removed. The Palaso library (ListParser and LiftMultiText) need to be refactored.
		/// </summary>
		/// <param name="nodesWithForms"></param>
		/// <param name="text">text to add material to. Material is from XmlNode, hence non-safe XML (read from XmlNode without re-escaping).</param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void ReadFormNodes(XmlNodeList nodesWithForms, LiftMultiText text)
		{
			foreach (XmlNode formNode in nodesWithForms)
			{
				try
				{
					string lang = Utilities.GetStringAttribute(formNode, "lang");
					XmlNode textNode = formNode.SelectSingleNode("text");
					if (textNode != null)
					{
						// Add the separator if we need it.
						if (textNode.InnerText.Length > 0)
							text.AddOrAppend(lang, "", "; ");
						foreach (XmlNode node in textNode.ChildNodes)
						{
							if (node.Name == "span")
							{
								text.AddSpan(lang,
									Utilities.GetOptionalAttributeString(node, "lang"),
									Utilities.GetOptionalAttributeString(node, "class"),
									Utilities.GetOptionalAttributeString(node, "href"),
									node.InnerText.Length);
							}
							text.AddOrAppend(lang, node.InnerText, "");
						}
					}
					var nodelist = formNode.SelectNodes("annotation");
					if (nodelist != null)
					{
						//foreach (XmlNode annotationNode in nodelist)
						//{
						//    Annotation annotation = GetAnnotation(annotationNode);
						//    annotation.LanguageHint = lang;
						//    text.Annotations.Add(annotation);
						//}
					}
				}
				catch (Exception e)
				{
					// not a fatal error
					//NotifyFormatError(e);
				}
			}
		}



		#endregion //Import Ranges File

		/// <summary>
		///
		/// </summary>
		/// <param name="range"></param>
		/// <param name="id"></param>
		/// <param name="guidAttr"></param>
		/// <param name="parent"></param>
		/// <param name="description">non-safe-XML</param>
		/// <param name="label">non-safe-XML</param>
		/// <param name="abbrev">non-safe-XML</param>
		/// <param name="rawXml"></param>
		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.ProcessRangeElement(
				string range, string id, string guidAttr, string parent,
				LiftMultiText description, LiftMultiText label, LiftMultiText abbrev,
				string rawXml)
		{
			IgnoreNewWs();
			var newDesc = MakeSafeLiftMultiText(description);
			var newLabel = MakeSafeLiftMultiText(label);
			var newAbbrev = MakeSafeLiftMultiText(abbrev);
			switch (range)
			{
				case "dialect": // translate into writing systems
					VerifyOrCreateWritingSystem(id, newLabel, newAbbrev, newDesc);
					break;
				case "etymology": // I think we can ignore these.
					break;
				case "note-type": // I think we can ignore these.
					break;
				case "paradigm": // I think we can ignore these.
					break;
					//============================================================================
					//============================================================================
					//============================================================================
					//============================================================================
					//============================================================================
					//New
				case RangeNames.sAffixCategoriesOA:
					ProcessPossibility(id, guidAttr, parent, newDesc, newLabel, newAbbrev,
									   m_dictAffixCategories, m_rgAffixCategories, m_cache.LangProject.AffixCategoriesOA);
					break;
					//New
				case RangeNames.sAnnotationDefsOA:
					break;

					//xxx============================================xxx
				case RangeNames.sAnthroListOAold1: // original FLEX export
				case RangeNames.sAnthroListOA: // initialize map, adding to existing list if needed.
					ProcessAnthroItem(id, guidAttr, parent, newDesc, newLabel, newAbbrev);
					break;
					//xxx============================================xxx

					//New
				case RangeNames.sConfidenceLevelsOA:
				case RangeNames.sEducationOA:
				case RangeNames.sGenreListOA:
				case RangeNames.sLocationsOA:
					break;

					//xxx============================================xxx
				case RangeNames.sPartsOfSpeechOA: // map onto parts of speech?  extend as needed.
				case RangeNames.sPartsOfSpeechOAold2: // original FLEX export
				case RangeNames.sPartsOfSpeechOAold1: // map onto parts of speech?  extend as needed.
					ProcessPartOfSpeech(id, guidAttr, parent, newDesc, newLabel, newAbbrev);
					break;
					//xxx============================================xxx
					//xxx============================================xxx
				case RangeNames.sPeopleOA:
					ProcessPerson(id, guidAttr, parent, newDesc, newLabel, newAbbrev, m_dictPerson, m_rgnewPerson, m_cache.LangProject.PeopleOA);
					break;
					//xxx============================================xxx

					//New
				case RangeNames.sPositionsOA:
				case RangeNames.sRestrictionsOA:
				case RangeNames.sRolesOA:
					break;

					//xxx============================================xxx
				case RangeNames.sSemanticDomainListOAold1: // for WeSay 0.4 compatibility
				case RangeNames.sSemanticDomainListOAold2:
				case RangeNames.sSemanticDomainListOAold3:
				case RangeNames.sSemanticDomainListOA: // initialize map, adding to existing list if needed.
					ProcessSemanticDomain(id, guidAttr, parent, newDesc, newLabel, newAbbrev);
					break;
					//xxx============================================xxx

					//xxx============================================xxx
				case RangeNames.sStatusOA:
					ProcessPossibility(id, guidAttr, parent, newDesc, newLabel, newAbbrev,
									   m_dictStatus, m_rgnewStatus, m_cache.LangProject.StatusOA);
					break;
					//xxx============================================xxx

					//New
				case RangeNames.sThesaurusRA:
					break;

					//xxx============================================xxx
				case RangeNames.sTranslationTagsOAold1: // original FLEX export
				case RangeNames.sTranslationTagsOA:
					ProcessPossibility(id, guidAttr, parent, newDesc, newLabel, newAbbrev,
									   m_dictTransType, m_rgnewTransType, m_cache.LangProject.TranslationTagsOA);
					break;
					//xxx============================================xxx
					//=========================================================================================

					//xxx============================================xxx
				case RangeNames.sProdRestrictOA:
					EnsureProdRestrictListExists();
					ProcessPossibility(id, guidAttr, parent, newDesc, newLabel, newAbbrev,
									   m_dictExceptFeats, m_rgnewExceptFeat,
									   m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA);
					break;
					//xxx============================================xxx

					//New
				case RangeNames.sProdRestrictOAfrom:
					break;
					//=========================================================================================
					//lists under m_cache.LangProject.LexDbOA
					//New
				case RangeNames.sDbComplexEntryTypesOA:
					break;

					//New
				case RangeNames.sDbDomainTypesOA:
				case RangeNames.sDbDomainTypesOAold1:
					//handled already
					break;


					//xxx============================================xxx
				case RangeNames.sDbMorphTypesOAold: // original FLEX export
				case RangeNames.sDbMorphTypesOA:
					ProcessMorphType(id, guidAttr, parent, newDesc, newLabel, newAbbrev);
					break;
					//xxx============================================xxx

					//New
				case RangeNames.sDbPublicationTypesOAold:
				case RangeNames.sDbPublicationTypesOA:
					ProcessPossibility(id, guidAttr, parent, newDesc, newLabel, newAbbrev,
									   m_dictPublicationTypes, m_rgnewPublicationType, m_cache.LangProject.LexDbOA.PublicationTypesOA);
					break;

					//xxx============================================xxx
				case RangeNames.sDbReferencesOAold: // original FLEX export
				case RangeNames.sDbReferencesOA: // lexical relation types (?)
					// TODO: Handle these here instead of where they're encountered in processing!
					break;
					//xxx============================================xxx
					//New
				case RangeNames.sDbSenseTypesOA:
				case RangeNames.sDbSenseTypesOAold1:
				case RangeNames.sDbUsageTypesOAold:
				case RangeNames.sDbUsageTypesOA:
				case RangeNames.sDbVariantEntryTypesOA:
					break;
					//=====================EXTRA RANGES NOT  CmPossibilityLists============================

					//xxx============================================xxx
				case RangeNames.sMSAinflectionFeature:
					if (m_cache.LangProject.MsFeatureSystemOA == null)
					{
						IFsFeatureSystemFactory fact = m_cache.ServiceLocator.GetInstance<IFsFeatureSystemFactory>();
						m_cache.LangProject.MsFeatureSystemOA = fact.Create();
					}
					ProcessFeatureDefinition(id, guidAttr, parent, newDesc, newLabel, newAbbrev, rawXml,
											 m_cache.LangProject.MsFeatureSystemOA);
					break;
					//xxx============================================xxx

					//New
				case RangeNames.sMSAfromInflectionFeature:
					break;

					//xxx============================================xxx

				case RangeNames.sMSAinflectionFeatureType:
					if (m_cache.LangProject.MsFeatureSystemOA == null)
					{
						IFsFeatureSystemFactory fact = m_cache.ServiceLocator.GetInstance<IFsFeatureSystemFactory>();
						m_cache.LangProject.MsFeatureSystemOA = fact.Create();
					}
					ProcessFeatureStrucType(id, guidAttr, parent, newDesc, newLabel, newAbbrev, rawXml,
											m_cache.LangProject.MsFeatureSystemOA);
					break;
					//xxx============================================xxx

					//New
				case RangeNames.sReversalType:
					break;

				default:
					if (range.EndsWith("-slot") || range.EndsWith("-Slots"))
						ProcessSlotDefinition(range, id, guidAttr, parent, newDesc, newLabel, newAbbrev);
					else if (range.EndsWith("-infl-class") || range.EndsWith("-InflClasses"))
						ProcessInflectionClassDefinition(range, id, guidAttr, parent, newDesc, newLabel, newAbbrev);
					else if (range.EndsWith("-feature-value"))
						ProcessFeatureValue(range, id, guidAttr, parent, newDesc, newLabel, newAbbrev, rawXml);
					else if (range.EndsWith("-stem-name"))
						ProcessStemName(range, id, guidAttr, parent, newDesc, newLabel, newAbbrev, rawXml);
#if DEBUG
					else
						Debug.WriteLine(String.Format("Unknown range '{0}' has element '{1}'", range, id));
#endif
					break;
			}
		}

		private void EnsureProdRestrictListExists()
		{
			if (m_cache.LangProject.MorphologicalDataOA == null)
			{
				IMoMorphDataFactory fact = m_cache.ServiceLocator.GetInstance<IMoMorphDataFactory>();
				m_cache.LangProject.MorphologicalDataOA = fact.Create();
			}
			if (m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA == null)
			{
				ICmPossibilityListFactory fact = m_cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();
				m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA = fact.Create();
			}
		}


		//================================================================================================================
		Dictionary<string, string> m_writingsytemChangeMap = new Dictionary<string, string>();

		/// <summary>
		///
		/// </summary>
		/// <param name="sLIFTfilesPath"></param>
		/// <param name="sLIFTdatafile"></param>
		/// <param name="sLIFTrangesfile"></param>
		public void LdmlFilesMigration(string sLIFTfilesPath, string sLIFTdatafile, string sLIFTrangesfile)
		{
			var ldmlFolder = Path.Combine(sLIFTfilesPath, "WritingSystems");

			var migrator = new LdmlInFolderWritingSystemRepositoryMigrator(ldmlFolder, NoteMigration);
			migrator.Migrate();

			if (File.Exists(sLIFTrangesfile))
			{
				var xdocRanges = new XmlDocument();
				xdocRanges.Load(sLIFTrangesfile);
				XElement dataRanges = XElement.Parse(xdocRanges.InnerXml);
				LdmlDataLangMigration(dataRanges, sLIFTrangesfile);
			}

			if (File.Exists(sLIFTdatafile))
			{
				var xdocLiftData = new XmlDocument();
				xdocLiftData.Load(sLIFTdatafile);
				XElement dataLiftData = XElement.Parse(xdocLiftData.InnerXml);
				LdmlDataLangMigration(dataLiftData, sLIFTdatafile);
			}
		}

		private void LdmlDataLangMigration(XElement data, string filename)
		{
			var changed = false;
			foreach (var elt in data.XPathSelectElements("//*[name()='form' or name()='span' or name()='gloss']"))
			{
				var attr = elt.Attribute("lang");
				if (attr == null)
					continue; // pathological, but let's try to survive
				var oldTag = attr.Value;
				string newTag;
				if (TryGetNewTag(oldTag, out newTag))
				{
					changed = true;
					attr.Value = newTag;
				}
			}
			if (changed)
			{
				data.Save(filename);
			}
		}


		internal void NoteMigration(IEnumerable<LdmlVersion0MigrationStrategy.MigrationInfo> migrationInfo)
		{
			foreach (var info in migrationInfo)
			{
				// Not sure if it ever reports unchanged ones, but we don't care about them.
				if (info.RfcTagBeforeMigration != info.RfcTagAfterMigration)
					m_writingsytemChangeMap[RemoveMultipleX(info.RfcTagBeforeMigration.ToLowerInvariant())] = info.RfcTagAfterMigration;
				// Due to earlier bugs, FieldWorks projects sometimes contain cmn* writing systems in zh* files,
				// and the fwdata incorrectly labels this data using a tag based on the file name rather than the
				// language tag indicated by the internal properties. We attempt to correct this by also converting the
				// file tag to the new tag for this writing system.
				if (info.FileName.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
				{
					var fileNameTag = Path.GetFileNameWithoutExtension(info.FileName);
					if (fileNameTag != info.RfcTagBeforeMigration)
						m_writingsytemChangeMap[RemoveMultipleX(fileNameTag.ToLowerInvariant())] = info.RfcTagAfterMigration;
				}
			}
		}

		string RemoveMultipleX(string input)
		{
			bool gotX = false;
			var result = new List<string>();
			foreach (var item in input.Split('-'))
			{
				if (item == "x")
				{
					if (gotX)
						continue;
					else
						gotX = true; // and include this first X
				}
				result.Add(item);
			}
			return string.Join("-", result.ToArray());
		}

		private bool TryGetNewTag(string oldTag, out string newTag)
		{
			var key = RemoveMultipleX(oldTag.ToLowerInvariant());
			if (m_writingsytemChangeMap.TryGetValue(key, out newTag))
				return !newTag.Equals(oldTag, StringComparison.OrdinalIgnoreCase);
			var cleaner = new Rfc5646TagCleaner(oldTag);
			cleaner.Clean();
			// FieldWorks needs to handle this special case.
			if (cleaner.Language.ToLowerInvariant() == "cmn")
			{
				var region = cleaner.Region;
				if (string.IsNullOrEmpty(region))
					region = "CN";
				cleaner = new Rfc5646TagCleaner("zh", cleaner.Script, region, cleaner.Variant, cleaner.PrivateUse);
			}
			newTag = cleaner.GetCompleteTag();
			while (m_writingsytemChangeMap.Values.Contains(newTag, StringComparer.OrdinalIgnoreCase))
			{
				// We can't use this tag because it would conflict with what we are mapping something else to.
				cleaner = new Rfc5646TagCleaner(cleaner.Language, cleaner.Script, cleaner.Region, cleaner.Variant,
					GetNextDuplPart(cleaner.PrivateUse));
				newTag = cleaner.GetCompleteTag();
			}
			m_writingsytemChangeMap[key] = newTag;
			return !newTag.Equals(oldTag, StringComparison.OrdinalIgnoreCase);
		}

		private List<ICmPossibility> GetRgnewCustom(string range)
		{
			List<ICmPossibility> rgnewCustom;
			if (!m_rgnewCustoms.TryGetValue(range, out rgnewCustom))
			{
				rgnewCustom = new List<ICmPossibility>();
				m_rgnewCustoms.Add(range, rgnewCustom);
			}
			return rgnewCustom;
		}

		private Dictionary<string, ICmPossibility> GetDictCustomList(string range)
		{
			Dictionary<string, ICmPossibility> dictCustomList;
			if (!m_dictCustomLists.TryGetValue(range, out dictCustomList))
			{
				dictCustomList = new Dictionary<string, ICmPossibility>();
				m_dictCustomLists.Add(range, dictCustomList);
			}
			return dictCustomList;
		}
	}
}
