// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using LanguageExplorer.Areas;
using SIL.Lift;
using SIL.Lift.Parsing;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using SIL.WritingSystems;
using SIL.WritingSystems.Migration;
using SIL.Xml;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This class is called by the LiftParser, as it encounters each element of a lift file.
	/// There is at least one other ILexiconMerger implementation, used in WeSay.
	/// </summary>
	public class FlexLiftMerger : ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>
	{
		readonly LcmCache m_cache;
		ITsString m_tssEmpty;
		public const string LiftDateTimeFormat = "yyyy-MM-ddTHH:mm:ssK";	// wants UTC, but works with Local
		private readonly WritingSystemManager m_wsManager;
		readonly Regex m_regexGuid = new Regex("[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private ConflictingData m_cdConflict;
		private List<ConflictingData> m_rgcdConflicts = new List<ConflictingData>();
		private List<EticCategory> m_rgcats = new List<EticCategory>();
		private string m_sLiftFile;
		// TODO WS: how should this be used in the new world?
		private string m_sLiftDir;
		private string m_sLiftProducer;		// the producer attribute in the lift element.
		private DateTime m_defaultDateTime = default(DateTime);
		private bool m_fCreatingNewEntry;
		private bool m_fCreatingNewSense;

		// save field specification information from the header.
		// LiftMultiTexts are in Xml-safe format (See MakeSafeLiftMultiText)
		readonly Dictionary<string, LiftMultiText> m_dictFieldDef = new Dictionary<string, LiftMultiText>();

		readonly Dictionary<string, IFsFeatDefn> m_mapIdFeatDefn = new Dictionary<string, IFsFeatDefn>();
		readonly Dictionary<string, IFsFeatStrucType> m_mapIdFeatStrucType = new Dictionary<string, IFsFeatStrucType>();
		readonly Dictionary<string, IFsSymFeatVal> m_mapIdAbbrSymFeatVal = new Dictionary<string, IFsSymFeatVal>();
		readonly Dictionary<Guid, IFsFeatDefn> m_mapLiftGuidFeatDefn = new Dictionary<Guid, IFsFeatDefn>();
		readonly Dictionary<IFsComplexFeature, string> m_mapComplexFeatMissingTypeAbbr = new Dictionary<IFsComplexFeature, string>();
		readonly Dictionary<IFsClosedFeature, List<string>> m_mapClosedFeatMissingValueAbbrs = new Dictionary<IFsClosedFeature, List<string>>();
		readonly Dictionary<IFsFeatStrucType, List<string>> m_mapFeatStrucTypeMissingFeatureAbbrs = new Dictionary<IFsFeatStrucType, List<string>>();
		readonly Dictionary<string, IMoStemName> m_dictStemName = new Dictionary<string, IMoStemName>();

		readonly Dictionary<string, int> m_mapMorphTypeUnknownCount = new Dictionary<string, int>();

		// map from id strings to database objects (for entries and senses).
		readonly Dictionary<string, ICmObject> m_mapIdObject = new Dictionary<string, ICmObject>();
		// list of errors encountered
		readonly List<string> m_rgErrorMsgs = new List<string>();

		// map from custom field tags to flids (for custom fields)
		readonly Dictionary<string, int> m_dictCustomFlid = new Dictionary<string, int>();

		// map from slot range name to slot map.
		readonly Dictionary<string, Dictionary<string, IMoInflClass>> m_dictDictSlots = new Dictionary<string, Dictionary<string, IMoInflClass>>();

		// map from (reversal's) writing system to reversal PartOfSpeech map.
		readonly Dictionary<int, Dictionary<string, ICmPossibility>> m_dictWsReversalPos = new Dictionary<int, Dictionary<string, ICmPossibility>>();

		// Remember the guids of deleted objects so that we don't try to reuse them.
		HashSet<Guid> m_deletedGuids = new HashSet<Guid>();
		readonly Dictionary<object, Dictionary<MuElement, List<IReversalIndexEntry>>> m_mapToMapToRie = new Dictionary<object, Dictionary<MuElement, List<IReversalIndexEntry>>>();

		/// <summary>Set of guids for elements/senses that were found in the LIFT file.</summary>
		private readonly HashSet<Guid> m_setUnchangedEntry = new HashSet<Guid>();

		private readonly HashSet<Guid> m_setChangedEntry = new HashSet<Guid>();
		private readonly HashSet<int> m_deletedObjects = new HashSet<int>();
		MergeStyle m_msImport = MergeStyle.MsKeepOld;

		bool m_fTrustModTimes;

		readonly List<CoreWritingSystemDefinition> m_addedWss = new List<CoreWritingSystemDefinition>();

		// Repositories and factories for interacting with the project.

		ICmObjectRepository m_repoCmObject;
		IMoMorphTypeRepository m_repoMoMorphType;

		IFsFeatDefnRepository m_repoFsFeatDefn;
		IFsFeatStrucTypeRepository m_repoFsFeatStrucType;
		IFsSymFeatValRepository m_repoFsSymFeatVal;

		ICmAnthroItemFactory m_factCmAnthroItem;
		IMoStemAllomorphFactory m_factMoStemAllomorph;
		IMoAffixAllomorphFactory m_factMoAffixAllomorph;
		ILexPronunciationFactory m_factLexPronunciation;
		ICmMediaFactory m_factCmMedia;
		ILexEtymologyFactory m_factLexEtymology;
		ILexSenseFactory m_factLexSense;
		ILexEntryFactory m_factLexEntry;
		IMoInflClassFactory m_factMoInflClass;
		IMoInflAffixSlotFactory m_factMoInflAffixSlot;
		ILexExampleSentenceFactory m_factLexExampleSentence;
		ICmTranslationFactory m_factCmTranslation;
		ILexEntryTypeFactory m_factLexEntryType;
		ILexRefTypeFactory m_factLexRefType;
		ICmSemanticDomainFactory m_factCmSemanticDomain;
		ICmPossibilityFactory m_factCmPossibility;
		ICmLocationFactory m_factCmLocation;
		IMoStemMsaFactory m_factMoStemMsa;
		IMoUnclassifiedAffixMsaFactory m_factMoUnclassifiedAffixMsa;
		IMoDerivStepMsaFactory m_factMoDerivStepMsa;
		IMoDerivAffMsaFactory m_factMoDerivAffMsa;
		IMoInflAffMsaFactory m_factMoInflAffMsa;
		ICmPictureFactory m_factCmPicture;
		IReversalIndexEntryFactory m_factReversalIndexEntry;
		IReversalIndexRepository m_repoReversalIndex;
		IPartOfSpeechFactory m_factPartOfSpeech;
		IMoMorphTypeFactory m_factMoMorphType;
		IPhEnvironmentFactory m_factPhEnvironment;
		ILexReferenceFactory m_factLexReference;
		ILexEntryRefFactory m_factLexEntryRef;
		ICmPersonFactory m_factCmPerson;

		IFsComplexFeatureFactory m_factFsComplexFeature;
		IFsOpenFeatureFactory m_factFsOpenFeature;
		IFsClosedFeatureFactory m_factFsClosedFeature;
		IFsFeatStrucTypeFactory m_factFsFeatStrucType;
		IFsSymFeatValFactory m_factFsSymFeatVal;
		IFsFeatStrucFactory m_factFsFeatStruc;
		IFsClosedValueFactory m_factFsClosedValue;
		IFsComplexValueFactory m_factFsComplexValue;
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
		readonly List<ICmPossibility> m_rgnewLocation = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewPerson = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewLanguage = new List<ICmPossibility>();
		readonly List<ICmPossibility> m_rgnewDialects = new List<ICmPossibility>();
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
		readonly Dictionary<string, ICmPossibility> m_dictLanguage = new Dictionary<string, ICmPossibility>();
		readonly Dictionary<string, ICmPossibility> m_dictDialect = new Dictionary<string, ICmPossibility>();

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

		readonly List<PendingFeatureValue> m_rgPendingSymFeatVal = new List<PendingFeatureValue>();
		readonly List<PendingModifyTime> m_rgPendingModifyTimes = new List<PendingModifyTime>();

		private int m_cEntriesAdded;
		private int m_cSensesAdded;
		private int m_cEntriesDeleted;
		private DateTime m_dtStart;     // when import started
		List<InvalidRelation> m_rgInvalidRelation = new List<InvalidRelation>();
		List<InvalidData> m_rgInvalidData = new List<InvalidData>();
		List<CombinedCollection> m_combinedCollections = new List<CombinedCollection>();
		readonly List<PendingRelation> m_rgPendingRelation = new List<PendingRelation>();
		readonly List<PendingRelation> m_rgPendingTreeTargets = new List<PendingRelation>();
		readonly LinkedList<PendingRelation> m_rgPendingCollectionRelations = new LinkedList<PendingRelation>();
		readonly List<PendingLexEntryRef> m_rgPendingLexEntryRefs = new List<PendingLexEntryRef>();
		readonly Dictionary<int, LiftResidue> m_dictResidue = new Dictionary<int, LiftResidue>();

		#region Constructors and other initialization methods
		public FlexLiftMerger(LcmCache cache, MergeStyle msImport, bool fTrustModTimes)
		{
			m_cSensesAdded = 0;
			m_cache = cache;
			m_tssEmpty = TsStringUtils.EmptyString(cache.DefaultUserWs);
			m_msImport = msImport;
			m_fTrustModTimes = fTrustModTimes;
			m_wsManager = cache.ServiceLocator.WritingSystemManager;

			// remember initial conditions.
			m_dtStart = DateTime.Now;

			InitializePossibilityMaps();
			InitializeReverseLexRefTypesMap();
			InitializeStemNameMap();
			InitializeReversalMaps();
			InitializeReversalPOSMaps();
			LoadCategoryCatalog();
		}

		/// <summary>
		/// Get or set the LIFT file being imported (merged from).
		/// </summary>
		public string LiftFile
		{
			get { return m_sLiftFile; }
			set
			{
				m_sLiftFile = value;
				if (!string.IsNullOrEmpty(m_sLiftFile))
				{
					m_sLiftDir = Path.GetDirectoryName(m_sLiftFile);
					m_defaultDateTime = File.GetLastWriteTimeUtc(m_sLiftFile);
					StoreLiftProducer();
				}
			}
		}

		private void StoreLiftProducer()
		{
			var readerSettings = new XmlReaderSettings
			{
				ValidationType = ValidationType.None,
				IgnoreComments = true
			};
			using (var reader = XmlReader.Create(m_sLiftFile, readerSettings))
			{
				if (reader.IsStartElement("lift"))
				{
					m_sLiftProducer = reader.GetAttribute("producer");
				}
			}
		}

		private void InitializeMorphTypes()
		{
			foreach (var poss in m_cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS)
			{
				for (var i = 0; i < poss.Name.StringCount; ++i)
				{
					int ws;
					var s = poss.Name.GetStringFromIndex(i, out ws).Text;
					if (string.IsNullOrEmpty(s) || m_dictMmt.ContainsKey(s))
					{
						continue;
					}
					m_dictMmt.Add(s, poss);
				}
			}
		}

		private void InitializeStemNameMap()
		{
			m_dictStemName.Clear();
			var posNames = new List<string>();
			foreach (var pos in m_cache.LangProject.AllPartsOfSpeech)
			{
				posNames.Clear();
				int ws;
				for (var i = 0; i < pos.Name.StringCount; ++i)
				{
					var name = pos.Name.GetStringFromIndex(i, out ws).Text;
					if (!string.IsNullOrEmpty(name))
					{
						posNames.Add(name);
					}
				}
				if (posNames.Count == 0)
				{
					posNames.Add(string.Empty);		// should never happen!
				}
				foreach (var stem in pos.AllStemNames)
				{
					for (var i = 0; i < stem.Name.StringCount; ++i)
					{
						var name = stem.Name.GetStringFromIndex(i, out ws).Text;
						if (string.IsNullOrEmpty(name))
						{
							continue;
						}
						foreach (var posName in posNames)
						{
							var key = $"{posName}:{name}";
							if (!m_dictStemName.ContainsKey(key))
							{
								m_dictStemName.Add(key, stem);
							}
						}
					}
				}
			}
		}

		private void InitializeReversalMaps()
		{
			foreach (var ri in m_cache.LangProject.LexDbOA.ReversalIndexesOC)
			{
				var mapToRIEs = new Dictionary<MuElement, List<IReversalIndexEntry>>();
				m_mapToMapToRie.Add(ri, mapToRIEs);
				InitializeReversalMap(ri.EntriesOC, mapToRIEs);
			}
		}

		/// <summary />
		/// <param name="entries">This is IEnumerable to capture similarity of ILcmOwningCollection and ILcmOwningSequence.
		/// It is ILcmOwningCollection for entries owned by ReversalIndex and
		/// ILcmOwningSequence for entries owned by Subentries of a ReversalIndexEntry</param>
		/// <param name="mapToRIEs"></param>
		private void InitializeReversalMap(IEnumerable<IReversalIndexEntry> entries, Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs)
		{
			foreach (var rie in entries)
			{
				for (var i = 0; i < rie.ReversalForm.StringCount; ++i)
				{
					int ws;
					var tss = rie.ReversalForm.GetStringFromIndex(i, out ws);
					if (tss.Length > 0)
					{
						var mue = new MuElement(ws, tss.Text);
						AddToReversalMap(mue, rie, mapToRIEs);
					}
				}
				if (rie.SubentriesOS.Count > 0)
				{
					var submapToRIEs = new Dictionary<MuElement, List<IReversalIndexEntry>>();
					m_mapToMapToRie.Add(rie, submapToRIEs);
					InitializeReversalMap(rie.SubentriesOS, submapToRIEs);
				}
			}
		}

		private static void AddToReversalMap(MuElement mue, IReversalIndexEntry rie, Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs)
		{
			List<IReversalIndexEntry> rgrie;
			if (!mapToRIEs.TryGetValue(mue, out rgrie))
			{
				rgrie = new List<IReversalIndexEntry>();
				mapToRIEs.Add(mue, rgrie);
			}
			if (!rgrie.Contains(rie))
			{
				rgrie.Add(rie);
			}
		}

		private void InitializeReverseLexRefTypesMap()
		{
			if (m_cache.LangProject.LexDbOA.ReferencesOA == null)
			{
				return;
			}
			foreach (ILexRefType lrt in m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS)
			{
				int ws;
				for (var i = 0; i < lrt.ReverseAbbreviation.StringCount; ++i)
				{
					var tss = lrt.ReverseAbbreviation.GetStringFromIndex(i, out ws);
					AddToReverseLexRefTypesMap(tss, lrt);
				}
				for (var i = 0; i < lrt.ReverseName.StringCount; ++i)
				{
					var tss = lrt.ReverseName.GetStringFromIndex(i, out ws);
					AddToReverseLexRefTypesMap(tss, lrt);
				}
			}
		}

		private void AddToReverseLexRefTypesMap(ITsString tss, ILexRefType lrt)
		{
			if (tss.Length > 0)
			{
				var s = tss.Text;
				if(!m_dictRevLexRefTypes.ContainsKey(s.ToLowerInvariant()))
				{
					m_dictRevLexRefTypes.Add(s.ToLowerInvariant(), lrt);
				}
			}
		}

		private void LoadCategoryCatalog()
		{
			var sPath = Path.Combine(FwDirectoryFinder.CodeDirectory, "Templates/GOLDEtic.xml");
			var xd = new XmlDocument();
			xd.Load(sPath);
			var xnTop = xd.SelectSingleNode("eticPOSList");
			if (xnTop?.ChildNodes != null)
			{
				foreach (XmlNode node in xnTop.SelectNodes("item"))
				{
					var sType = XmlUtils.GetOptionalAttributeValue(node, "type");
					var sId = XmlUtils.GetOptionalAttributeValue(node, "id");
					if (sType == "category" && !string.IsNullOrEmpty(sId))
					{
						LoadCategoryNode(node, sId, null);
					}
				}
			}
		}

		private void LoadCategoryNode(XmlNode node, string id, string parent)
		{
			var cat = new EticCategory {Id = id, ParentId = parent};
			if (node != null)
			{
				foreach (XmlNode xn in node.SelectNodes("abbrev"))
				{
					var sWs = XmlUtils.GetOptionalAttributeValue(xn, "ws");
					var sAbbrev = xn.InnerText;
					if (!string.IsNullOrEmpty(sWs) && !string.IsNullOrEmpty(sAbbrev))
					{
						cat.SetAbbrev(sWs, sAbbrev);
					}
				}
				foreach (XmlNode xn in node.SelectNodes("term"))
				{
					var sWs = XmlUtils.GetOptionalAttributeValue(xn, "ws");
					var sName = xn.InnerText;
					if (!string.IsNullOrEmpty(sWs) && !string.IsNullOrEmpty(sName))
					{
						cat.SetName(sWs, sName);
					}
				}
				foreach (XmlNode xn in node.SelectNodes("def"))
				{
					var sWs = XmlUtils.GetOptionalAttributeValue(xn, "ws");
					var sDesc = xn.InnerText;
					if (!string.IsNullOrEmpty(sWs) && !string.IsNullOrEmpty(sDesc))
					{
						cat.SetDesc(sWs, sDesc);
					}
				}
			}
			m_rgcats.Add(cat);
			if (node != null)
			{
				foreach (XmlNode xn in node.SelectNodes("item"))
				{
					var sType = XmlUtils.GetOptionalAttributeValue(xn, "type");
					var sChildId = XmlUtils.GetOptionalAttributeValue(xn, "id");
					if (sType == "category" && !string.IsNullOrEmpty(sChildId))
					{
						LoadCategoryNode(xn, sChildId, id);
					}
				}
			}
		}
		private void InitializePossibilityMaps()
		{
			if (m_cache.LangProject.PartsOfSpeechOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS, m_dictPos);
			}
			InitializeMorphTypes();
			if (m_cache.LangProject.LexDbOA.ComplexEntryTypesOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS, m_dictComplexFormType);
			}
			if (m_cache.LangProject.LexDbOA.VariantEntryTypesOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS, m_dictVariantType);
			}
			if (m_cache.LangProject.SemanticDomainListOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS, m_dictSemDom);
				EnhancePossibilityMapForWeSay(m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS, m_dictSemDom);
			}
			if (m_cache.LangProject.TranslationTagsOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.TranslationTagsOA.PossibilitiesOS, m_dictTransType);
			}
			if (m_cache.LangProject.AnthroListOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.AnthroListOA.PossibilitiesOS, m_dictAnthroCode);
			}
			if (m_cache.LangProject.MorphologicalDataOA?.ProdRestrictOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS, m_dictExceptFeats);
			}

			if (m_cache.LangProject.LexDbOA.DialectLabelsOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.DialectLabelsOA.PossibilitiesOS, m_dictDialect);
			}
			if (m_cache.LangProject.LexDbOA.DomainTypesOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.DomainTypesOA.PossibilitiesOS, m_dictDomainType);
			}
			if (m_cache.LangProject.LexDbOA.PublicationTypesOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS, m_dictPublicationTypes);
			}
			if (m_cache.LangProject.LexDbOA.SenseTypesOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.SenseTypesOA.PossibilitiesOS, m_dictSenseType);
			}
			if (m_cache.LangProject.StatusOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.StatusOA.PossibilitiesOS, m_dictStatus);
			}
			if (m_cache.LangProject.LexDbOA.UsageTypesOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.UsageTypesOA.PossibilitiesOS, m_dictUsageType);
			}
			if (m_cache.LangProject.LocationsOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.LocationsOA.PossibilitiesOS, m_dictLocation);
			}
			if (m_cache.LangProject.LexDbOA.LanguagesOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.LanguagesOA.PossibilitiesOS, m_dictLanguage);
			}
			if (m_cache.LangProject.PhonologicalDataOA != null)
			{
				foreach (var env in m_cache.LangProject.PhonologicalDataOA.EnvironmentsOS)
				{
					// More than one environment may have the same string representation.  This
					// is unfortunate, but it does happen.
					var s = env.StringRepresentation.Text;
					if (!string.IsNullOrEmpty(s))
					{
						List<IPhEnvironment> rgenv;
						if (m_dictEnvirons.TryGetValue(s, out rgenv))
						{
							rgenv.Add(env);
						}
						else
						{
							rgenv = new List<IPhEnvironment>();
							rgenv.Add(env);
							m_dictEnvirons.Add(s, rgenv);
						}
					}
				}
			}

			if (m_cache.LangProject.LexDbOA.ReferencesOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS, m_dictLexRefTypes);
			}
		}

		private static void InitializePossibilityMap(ILcmOwningSequence<ICmPossibility> possibilities, Dictionary<string, ICmPossibility> dict)
		{
			if (possibilities == null)
			{
				return;
			}
			foreach (var poss in possibilities)
			{
				int ws;
				for (var i = 0; i < poss.Abbreviation.StringCount; ++i)
				{
					var tss = poss.Abbreviation.GetStringFromIndex(i, out ws);
					AddToPossibilityMap(tss, poss, dict);
				}
				for (var i = 0; i < poss.Name.StringCount; ++i)
				{
					var tss = poss.Name.GetStringFromIndex(i, out ws);
					AddToPossibilityMap(tss, poss, dict);
				}
				InitializePossibilityMap(poss.SubPossibilitiesOS, dict);
			}
		}

		private static void AddToPossibilityMap(ITsString tss, ICmPossibility poss, Dictionary<string, ICmPossibility> dict)
		{
			if (tss.Length > 0)
			{
				var s = tss.Text.Normalize();
				if (!dict.ContainsKey(s))
				{
					dict.Add(s, poss);
				}
				s = s.ToLowerInvariant();
				if (!dict.ContainsKey(s))
				{
					dict.Add(s, poss);
				}
			}
		}

		private void InitializeReversalPOSMaps()
		{
			foreach (var ri in m_cache.LangProject.LexDbOA.ReversalIndexesOC)
			{
				var dict = new Dictionary<string, ICmPossibility>();
				if (ri.PartsOfSpeechOA != null)
				{
					InitializePossibilityMap(ri.PartsOfSpeechOA.PossibilitiesOS, dict);
				}
				Debug.Assert(!string.IsNullOrEmpty(ri.WritingSystem));
				var handle = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(ri.WritingSystem);
				if (m_dictWsReversalPos.ContainsKey(handle))
				{
					// REVIEW: SHOULD WE LOG A WARNING HERE?  THIS SHOULD NEVER HAPPEN!
					// (BUT IT HAS AT LEAST ONCE IN A 5.4.1 PROJECT)
				}
				else
				{
					m_dictWsReversalPos.Add(handle, dict);
				}
			}
		}

		/// <summary>
		/// WeSay stores Semantic Domain values as "abbr name", so fill in keys like that
		/// for lookup during import.
		/// </summary>
		private static void EnhancePossibilityMapForWeSay(ILcmOwningSequence<ICmPossibility> possibilities, Dictionary<string, ICmPossibility> dict)
		{
			foreach (var poss in possibilities)
			{
				for (var i = 0; i < poss.Abbreviation.StringCount; ++i)
				{
					int ws;
					var tssAbbr = poss.Abbreviation.GetStringFromIndex(i, out ws);
					if (tssAbbr.Length > 0)
					{
						var tssName = poss.Name.get_String(ws);
						if (tssName.Length > 0)
						{
							var sAbbr = tssAbbr.Text;
							var sName = tssName.Text;
							var sKey = $"{sAbbr} {sName}";
							if (!dict.ContainsKey(sKey))
							{
								dict.Add(sKey, poss);
							}
							sKey = sKey.ToLowerInvariant();
							if (!dict.ContainsKey(sKey))
							{
								dict.Add(sKey, poss);
							}
						}
					}
				}
				EnhancePossibilityMapForWeSay(poss.SubPossibilitiesOS, dict);
			}
		}

		#endregion // Constructors and other initialization methods

		private bool IsVoiceWritingSystem(int wsString)
		{
			var wsEngine = (CoreWritingSystemDefinition) m_wsManager.get_EngineOrNull(wsString);
			return wsEngine.IsVoice;
		}

		#region ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample> Members

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.EntryWasDeleted(
			Extensible info, DateTime dateDeleted)
		{
			var guid = info.Guid;
			if (guid == Guid.Empty)
			{
				return;
			}
			var cmo = GetObjectForGuid(guid);
			if (cmo is ILexEntry)	// make sure it's a LexEntry!
			{
				// We need to collect the deleted objects' guids so that they won't be
				// reused.  See FWR-3290 for what can happen if we don't do this.
				CollectGuidsFromDeletedEntry(cmo as ILexEntry);
				// TODO: Compare mod times? or our mod time against import's delete time?
				cmo.Delete();
				++m_cEntriesDeleted;
			}
		}

		private void CollectGuidsFromDeletedEntry(ILexEntry le)
		{
			m_deletedGuids.Add(le.Guid);
			if (le.LexemeFormOA != null)
			{
				m_deletedGuids.Add(le.LexemeFormOA.Guid);
			}
			foreach (var ety in le.EtymologyOS)
			{
				m_deletedGuids.Add(ety.Guid);
			}
			foreach (var msa in le.MorphoSyntaxAnalysesOC)
			{
				m_deletedGuids.Add(msa.Guid);
			}
			foreach (var er in le.EntryRefsOS)
			{
				m_deletedGuids.Add(er.Guid);
			}
			foreach (var form in le.AlternateFormsOS)
			{
				m_deletedGuids.Add(form.Guid);
			}
			foreach (var pron in le.PronunciationsOS)
			{
				m_deletedGuids.Add(pron.Guid);
			}
			CollectGuidsFromDeletedSenses(le.SensesOS);
		}

		private void CollectGuidsFromDeletedSenses(ILcmOwningSequence<ILexSense> senses)
		{
			foreach (var ls in senses)
			{
				m_deletedGuids.Add(ls.Guid);
				foreach (var pict in ls.PicturesOS)
				{
					m_deletedGuids.Add(pict.Guid);
				}
				foreach (var ex in ls.ExamplesOS)
				{
					m_deletedGuids.Add(ex.Guid);
				}
				CollectGuidsFromDeletedSenses(ls.SensesOS);
			}
		}

		/// <summary>
		/// This method does all the real work of importing an entry into the lexicon.  Up to
		/// this point, we've just been building up a memory structure based on the LIFT data.
		/// </summary>
		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.FinishEntry(CmLiftEntry entry)
		{
			var fCreateNew = entry.CmObject == null;
			if (!fCreateNew && m_msImport == MergeStyle.MsKeepBoth)
			{
				fCreateNew = EntryHasConflictingData(entry);
			}
			if (fCreateNew)
			{
				CreateNewEntry(entry);
			}
			else
			{
				MergeIntoExistingEntry(entry);
			}
		}

		CmLiftEntry ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.GetOrMakeEntry(Extensible info, int order)
		{
			var guid = GetGuidInExtensible(info);
			if (m_fTrustModTimes && SameEntryModTimes(info))
			{
				// If we're keeping only the imported data, remember this was imported!
				if (m_msImport == MergeStyle.MsKeepOnlyNew)
				{
					m_setUnchangedEntry.Add(guid);
				}
				return null;	// assume nothing has changed.
			}
			var entry = new CmLiftEntry(info, guid, order, this);
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				m_setChangedEntry.Add(entry.Guid);
			}
			return entry;
		}

		CmLiftExample ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.GetOrMakeExample(CmLiftSense sense, Extensible info)
		{
			var guid = GetGuidInExtensible(info);
			var example = new CmLiftExample
			{
				Id = info.Id,
				Guid = guid,
				CmObject = GetObjectForGuid(guid),
				DateCreated = info.CreationTime,
				DateModified = info.ModificationTime
			};
			sense.Examples.Add(example);
			return example;
		}

		CmLiftSense ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.GetOrMakeSense(CmLiftEntry entry, Extensible info, string rawXml)
		{
			var sense = CreateLiftSenseFromInfo(info, entry);
			entry.Senses.Add(sense);
			return sense;
		}

		CmLiftSense ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.GetOrMakeSubsense(CmLiftSense sense, Extensible info, string rawXml)
		{
			var sub = CreateLiftSenseFromInfo(info, sense);
			sense.Subsenses.Add(sub);
			return sub;
		}

		/// <summary>
		/// contents must be in non-Xml-safe format
		/// </summary>
		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInCitationForm(CmLiftEntry entry, LiftMultiText contents)
		{
			var newContents = MakeSafeLiftMultiText(contents);
			if (entry.CitationForm == null)
			{
				entry.CitationForm = newContents;
			}
			else
			{
				MergeLiftMultiTexts(entry.CitationForm, newContents);
			}
		}

		/// <summary>
		/// Called from Chorus?
		/// </summary>
		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInDefinition(CmLiftSense sense, LiftMultiText contents)
		{
			var newContents = MakeSafeLiftMultiText(contents);
			if (sense.Definition == null)
			{
				sense.Definition = newContents;
			}
			else
			{
				MergeLiftMultiTexts(sense.Definition, newContents);
			}
		}

		/// <summary />
		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInExampleForm(CmLiftExample example, LiftMultiText contents)
		{
			var newContents = MakeSafeLiftMultiText(contents);
			if (example.Content == null)
			{
				example.Content = newContents;
			}
			else
			{
				MergeLiftMultiTexts(example.Content, newContents);
			}
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInField(
			LiftObject extensible, string tagAttribute, DateTime dateCreated, DateTime dateModified,
			LiftMultiText contents, List<Trait> traits)
		{
			var field = new LiftField
			{
				Type = tagAttribute,
				DateCreated = dateCreated,
				DateModified = dateModified,
				Content = MakeSafeLiftMultiText(contents)
			};
			foreach (var t in traits)
			{
				var lt = new LiftTrait
				{
					Name = t.Name,
					Value = t.Value
				};
				field.Traits.Add(lt);
			}
			extensible.Fields.Add(field);
		}

		/// <summary />
		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInGloss(CmLiftSense sense, LiftMultiText contents)
		{
			var newContents = MakeSafeLiftMultiText(contents);
			if (sense.Gloss == null)
			{
				sense.Gloss = newContents;
			}
			else
			{
				MergeLiftMultiTexts(sense.Gloss, newContents);
			}
		}

		/// <summary>
		/// Only Sense and Reversal have grammatical information.
		/// </summary>
		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInGrammaticalInfo(LiftObject obj, string val, List<Trait> traits)
		{
			var graminfo = new LiftGrammaticalInfo { Value = val };
			foreach (var t in traits)
			{
				var lt = new LiftTrait
				{
					Name = t.Name,
					Value = t.Value
				};
				graminfo.Traits.Add(lt);
			}

			if (obj is CmLiftSense)
			{
				((CmLiftSense)obj).GramInfo = graminfo;
			}
			else if (obj is CmLiftReversal)
			{
				((CmLiftReversal)obj).GramInfo = graminfo;
			}
		}

		/// <summary>
		/// Invoked from Chorus?
		/// </summary>
		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInLexemeForm(CmLiftEntry entry, LiftMultiText contents)
		{
			var newContents = MakeSafeLiftMultiText(contents);
			if (entry.LexicalForm == null)
			{
				entry.LexicalForm = newContents;
			}
			else
			{
				MergeLiftMultiTexts(entry.LexicalForm, newContents);
			}
		}

		/// <summary />
		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInNote(
			LiftObject extensible, string type, LiftMultiText contents, string rawXml)
		{
			AddNewWsToAnalysis();
			var newContents = MakeSafeLiftMultiText(contents);
			var note = new CmLiftNote(type, newContents);
			// There may be <trait>, <field>, or <annotation> elements hidden in the
			// raw XML string.  Perhaps these should be arguments, but they aren't.
			FillInExtensibleElementsFromRawXml(note, rawXml);
			if (extensible is CmLiftEntry)
			{
				((CmLiftEntry)extensible).Notes.Add(note);
			}
			else if (extensible is CmLiftSense)
			{
				((CmLiftSense)extensible).Notes.Add(note);
			}
			else if (extensible is CmLiftExample)
			{
				((CmLiftExample)extensible).Notes.Add(note);
			}
			else
			{
				IgnoreNewWs();
				Debug.WriteLine("<note type='{1}'> (first content = '{2}') found in bad context: {0}", extensible.GetType().Name, type, GetFirstLiftTsString(newContents) == null ? "<null>" : GetFirstLiftTsString(newContents).Text);
			}
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInPicture(CmLiftSense sense, string href, LiftMultiText caption)
		{
			var pict = new LiftUrlRef
			{
				Url = href,
				Label = MakeSafeLiftMultiText(caption)
			};
			sense.Illustrations.Add(pict);
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInMedia(LiftObject obj, string href, LiftMultiText caption)
		{
			var phon = obj as CmLiftPhonetic;
			if (phon != null)
			{
				var url = new LiftUrlRef
				{
					Url = href,
					Label = MakeSafeLiftMultiText(caption)
				};
				phon.Media.Add(url);
			}
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInRelation(LiftObject extensible, string relationTypeName, string targetId, string rawXml)
		{
			var rel = new CmLiftRelation
			{
				Type = relationTypeName,
				Ref = targetId.Normalize()
			};
			// I've seen data with this as NFD!
			// order should be an argument of this method, but since it isn't (yet),
			// calculate the order whenever it appears to be relevant.
			// There may also be <trait>, <field>, or <annotation> elements hidden in the
			// raw XML string.  These should also be arguments, but aren't.
			FillInExtensibleElementsFromRawXml(rel, rawXml.Normalize());
			if (extensible is CmLiftEntry)
			{
				((CmLiftEntry)extensible).Relations.Add(rel);
			}
			else if (extensible is CmLiftSense)
			{
				((CmLiftSense)extensible).Relations.Add(rel);
			}
			else if (extensible is CmLiftVariant)
			{
				((CmLiftVariant)extensible).Relations.Add(rel);
			}
			else
			{
				Debug.WriteLine($"<relation type='{relationTypeName}' ref='{targetId}> found in bad context: {extensible.GetType().Name}");
			}
		}

		private static void FillInExtensibleElementsFromRawXml(LiftObject obj, string rawXml)
		{
			if (rawXml.IndexOf("<trait") > 0 ||
				rawXml.IndexOf("<field") > 0 ||
				rawXml.IndexOf("<annotation") > 0 ||
				(obj is CmLiftRelation && rawXml.IndexOf("order=") > 0))
			{
				var xdoc = new XmlDocument();
				xdoc.LoadXml(rawXml);
				var node = xdoc.FirstChild;
				var rel = obj as CmLiftRelation;
				if (rel != null)
				{
					var sOrder = XmlUtils.GetOptionalAttributeValue(node, "order", null);
					if (!string.IsNullOrEmpty(sOrder))
					{
						int order;
						rel.Order = int.TryParse(sOrder, out order) ? order : 0;
					}
				}
				foreach (XmlNode xn in node.SelectNodes("field"))
				{
					var field = CreateLiftFieldFromXml(xn);
					obj.Fields.Add(field);
				}
				foreach (XmlNode xn in node.SelectNodes("trait"))
				{
					var trait = CreateLiftTraitFromXml(xn);
					obj.Traits.Add(trait);
				}
				foreach (XmlNode xn in node.SelectNodes("annotation"))
				{
					var ann = CreateLiftAnnotationFromXml(xn);
					obj.Annotations.Add(ann);
				}
			}
		}

		/// <summary>
		/// Adapted from LiftParser.ReadExtensibleElementDetails()
		/// </summary>
		private static LiftField CreateLiftFieldFromXml(XmlNode node)
		{
			var fieldType = XmlUtils.GetMandatoryAttributeValue(node, "type");
			var priorFieldWithSameTag = $"preceding-sibling::field[@type='{fieldType}']";
			if (node.SelectSingleNode(priorFieldWithSameTag) != null)
			{
				// a fatal error
				throw new LiftFormatException($"Field with same type ({fieldType}) as sibling not allowed. Context:{node.ParentNode.OuterXml}");
			}

			var field = new LiftField
			{
				Type = fieldType,
				DateCreated = GetOptionalDateTime(node, "dateCreated"),
				DateModified = GetOptionalDateTime(node, "dateModified"),
				Content = CreateLiftMultiTextFromXml(node)
			};
			foreach (XmlNode xn in node.SelectNodes("trait"))
			{
				var trait = CreateLiftTraitFromXml(xn);
				field.Traits.Add(trait);
			}
			foreach (XmlNode xn in node.SelectNodes("annotation"))
			{
				var ann = CreateLiftAnnotationFromXml(xn);
				field.Annotations.Add(ann);
			}
			return field;
		}

		/// <summary>
		/// Adapted from LiftParser.ReadFormNodes()
		/// </summary>
		private static LiftMultiText CreateLiftMultiTextFromXml(XmlNode node)
		{
			var text = new LiftMultiText();
			foreach (XmlNode xnForm in node.SelectNodes("form"))
			{
				try
				{
					var lang = XmlUtils.GetOptionalAttributeValue(xnForm, "lang");
					var xnText = xnForm.SelectSingleNode("text");
					if (xnText != null)
					{
						// Add the separator if we need it.
						if (xnText.InnerText.Length > 0)
						{
							text.AddOrAppend(lang, string.Empty, "; ");
						}
						foreach (XmlNode xn in xnText.ChildNodes)
						{
							if (xn.Name == "span")
							{
								text.AddSpan(lang, XmlUtils.GetOptionalAttributeValue(xn, "lang"), XmlUtils.GetOptionalAttributeValue(xn, "class"), XmlUtils.GetOptionalAttributeValue(xn, "href"), xn.InnerText.Length);
							}
							text.AddOrAppend(lang, xn.InnerText, "");
						}
					}
					// Skip annotations for now...
				}
				catch (Exception)
				{
				}
			}
			return text;
		}

		/// <summary>
		/// Adapted from LiftParser.GetTrait()
		/// </summary>
		private static LiftTrait CreateLiftTraitFromXml(XmlNode node)
		{
			var trait = new LiftTrait
			{
				Name = XmlUtils.GetOptionalAttributeValue(node, "name"),
				Value = XmlUtils.GetOptionalAttributeValue(node, "value")
			};
			foreach (XmlNode n in node.SelectNodes("annotation"))
			{
				var ann = CreateLiftAnnotationFromXml(n);
				trait.Annotations.Add(ann);
			}
			return trait;
		}

		/// <summary>
		/// Adapted from LiftParser.GetAnnotation()
		/// </summary>
		private static LiftAnnotation CreateLiftAnnotationFromXml(XmlNode node)
		{
			var ann = new LiftAnnotation
			{
				Name = XmlUtils.GetOptionalAttributeValue(node, "name"),
				Value = XmlUtils.GetOptionalAttributeValue(node, "value"),
				When = GetOptionalDateTime(node, "when"),
				Who = XmlUtils.GetOptionalAttributeValue(node, "who"),
				Comment = CreateLiftMultiTextFromXml(node)
			};
			return ann;
		}

		/// <summary>
		/// Adapted from LiftParser.GetOptionalDate()
		/// </summary>
		private static DateTime GetOptionalDateTime(XmlNode node, string tag)
		{
			var sWhen = XmlUtils.GetOptionalAttributeValue(node, tag);
			if (string.IsNullOrEmpty(sWhen))
			{
				return default(DateTime);
			}
			try
			{
				return Extensible.ParseDateTimeCorrectly(sWhen);
			}
			catch (FormatException)
			{
				return default(DateTime);
			}
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInSource(CmLiftExample example, string source)
		{
			example.Source = source;
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInTrait(LiftObject extensible, Trait trait)
		{
			var lt = new LiftTrait
			{
				Value = trait.Value,
				Name = trait.Name
			};
			foreach (var t in trait.Annotations)
			{
				var ann = new LiftAnnotation
				{
					Name = t.Name,
					Value = t.Value,
					When = t.When,
					Who = t.Who
				};
				lt.Annotations.Add(ann);
			}
			extensible.Traits.Add(lt);
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInTranslationForm(CmLiftExample example, string type, LiftMultiText contents, string rawXml)
		{
			var trans = new LiftTranslation
			{
				Type = type,
				Content = MakeSafeLiftMultiText(contents)
			};
			example.Translations.Add(trans);
		}

		LiftObject ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInPronunciation(CmLiftEntry entry, LiftMultiText contents, string rawXml)
		{
			var phon = new CmLiftPhonetic { Form = MakeSafeLiftMultiText(contents) };
			entry.Pronunciations.Add(phon);
			return phon;
		}

		LiftObject ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInVariant(CmLiftEntry entry, LiftMultiText contents, string rawXml)
		{
			var var = new CmLiftVariant
			{
				Form = MakeSafeLiftMultiText(contents),
				RawXml = rawXml
			};
			// LiftIO handles "extensible", but not <pronunciation> or <relation>, so store the
			// raw XML for now.
			entry.Variants.Add(var);
			return var;
		}

		/// <summary>
		/// Convert a multitext into 'safe' format suitable for storing in XML files.
		/// </summary>
		/// <param name="multiText"></param>
		/// <remarks>JohnT: this is slightly bizarre, since LiftMerger is an IMPORT function and for the most part,
		/// we are not creating XML files. Most of the places we want to put this text, in LCM objects, we end up
		/// Decoding it again. Worth considering refactoring so that this method (renamed) just deals with characters
		/// we don't want in LCM objects, like tab and newline, and leaves the XML reserved characters alone. Then
		/// we could get rid of a lot of Decode statements also.
		/// Steve says one place we do need to make encoded XML is in the content of Residue fields.</remarks>
		/// <returns></returns>
		private static LiftMultiText MakeSafeLiftMultiText(LiftMultiText multiText)
		{
			if (multiText == null)
			{
				return null;
			}

			foreach (var lg in multiText.Keys)
			{
				multiText[lg].Text = ConvertToSafeFieldXmlContent(multiText[lg].Text);
			}
			return multiText;
		}

		/// <summary />
		LiftObject ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.GetOrMakeParentReversal(LiftObject parent, LiftMultiText contents, string type)
		{
			var rev = new CmLiftReversal
			{
				Type = type,
				Form = MakeSafeLiftMultiText(contents),
				Main = (CmLiftReversal)parent
			};
			return rev;
		}

		/// <summary />
		LiftObject ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInReversal(CmLiftSense sense, LiftObject parent, LiftMultiText contents, string type, string rawXml)
		{
			var rev = new CmLiftReversal
			{
				Type = type,
				Form = MakeSafeLiftMultiText(contents),
				Main = (CmLiftReversal)parent
			};
			sense.Reversals.Add(rev);
			return rev;
		}

		/// <summary />
		LiftObject ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInEtymology(
			CmLiftEntry entry, string source, string type, LiftMultiText form, LiftMultiText gloss, string rawXml)
		{
			var ety = new CmLiftEtymology
			{
				Source = source,
				Type = type,
				Form = MakeSafeLiftMultiText(form),
				Gloss = MakeSafeLiftMultiText(gloss)
			};
			entry.Etymologies.Add(ety);
			return ety;
		}

		/// <summary />
		private void ProcessFeatureDefinition(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev, string rawXml, IFsFeatureSystem featSystem)
		{
			IFsFeatDefn feat;
			var guid = ConvertStringToGuid(guidAttr);
			// For some reason we are processing the list twice: once when actually processing the range file
			// and once when proessing the LIFT file. During the second pass this prevents adding the same guid twice
			// which causes a crash.
			if (m_mapLiftGuidFeatDefn.TryGetValue(guid, out feat))
			{
				return;
			}

			if (m_factFsComplexFeature == null)
			{
				m_factFsComplexFeature = m_cache.ServiceLocator.GetInstance<IFsComplexFeatureFactory>();
			}
			if (m_factFsOpenFeature == null)
			{
				m_factFsOpenFeature = m_cache.ServiceLocator.GetInstance<IFsOpenFeatureFactory>();
			}
			if (m_factFsClosedFeature == null)
			{
				m_factFsClosedFeature = m_cache.ServiceLocator.GetInstance <IFsClosedFeatureFactory>();
			}
			if (m_repoFsFeatDefn == null)
			{
				m_repoFsFeatDefn = m_cache.ServiceLocator.GetInstance<IFsFeatDefnRepository>();
			}
			FillFeatureMapsIfNeeded();
			var xdoc = new XmlDocument();
			xdoc.PreserveWhitespace = true;
			xdoc.LoadXml(rawXml);
			var traits = xdoc.FirstChild.SelectNodes("trait");
			var fields = xdoc.FirstChild.SelectNodes("field");
			string sCatalogId = null;
			var fDisplayToRight = false;
			var fShowInGloss = false;
			string sSubclassType = null;
			string sComplexType = null;
			var nWsSelector = 0;
			string sWs = null;
			var rgsValues = new List<string>();
			XmlNode xnGlossAbbrev = null;
			XmlNode xnRightGlossSep = null;
			foreach (XmlNode xn in traits)
			{
				var name = XmlUtils.GetOptionalAttributeValue(xn, "name");
				switch (name)
				{
					case "catalog-source-id":
						sCatalogId = XmlUtils.GetOptionalAttributeValue(xn, "value");
						break;
					case "display-to-right":
						fDisplayToRight = XmlUtils.GetBooleanAttributeValue(xn, "value");
						break;
					case "show-in-gloss":
						fShowInGloss = XmlUtils.GetBooleanAttributeValue(xn, "value");
						break;
					case "feature-definition-type":
						sSubclassType = XmlUtils.GetOptionalAttributeValue(xn, "value");
						break;
					case "type":
						sComplexType = XmlUtils.GetOptionalAttributeValue(xn, "value");
						break;
					case "ws-selector":
						nWsSelector = XmlUtils.GetMandatoryIntegerAttributeValue(xn, "value");
						break;
					case "writing-system":
						sWs = XmlUtils.GetOptionalAttributeValue(xn, "value");
						break;
					default:
						if (name.EndsWith("-feature-value"))
						{
							var sVal = XmlUtils.GetOptionalAttributeValue(xn, "value");
							if (!string.IsNullOrEmpty(sVal))
							{
								rgsValues.Add(sVal);
							}
						}
						break;
				}
			}
			foreach (XmlNode xn in fields)
			{
				var type = XmlUtils.GetOptionalAttributeValue(xn, "type");
				switch (type)
				{
					case "gloss-abbrev":
						xnGlossAbbrev = xn;
						break;
					case "right-gloss-sep":
						xnRightGlossSep = xn;
						break;
				}
			}
			var fNew = false;
			if (m_mapIdFeatDefn.TryGetValue(id, out feat))
			{
				feat = ValidateFeatDefnType(sSubclassType, feat);
			}
			if (feat == null)
			{
				feat = CreateDesiredFeatDefn(sSubclassType, feat);
				if (feat == null)
				{
					return;
				}
				m_rgnewFeatDefn.Add(feat);
				m_mapIdFeatDefn[id] = feat;
				fNew = true;
			}
			AddNewWsToAnalysis();
			MergeInMultiUnicode(feat.Abbreviation, FsFeatDefnTags.kflidAbbreviation, abbrev, feat.Guid);
			MergeInMultiUnicode(feat.Name, FsFeatDefnTags.kflidName, label, feat.Guid);
			MergeInMultiString(feat.Description, FsFeatDefnTags.kflidDescription, description, feat.Guid);
			if (fNew || m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				if (!string.IsNullOrEmpty(sCatalogId))
				{
					feat.CatalogSourceId = sCatalogId;
				}
				if (fDisplayToRight)
				{
					feat.DisplayToRightOfValues = fDisplayToRight;
				}
				if (fShowInGloss)
				{
					feat.ShowInGloss = fShowInGloss;
				}
			}
			if (xnGlossAbbrev != null)
			{
				MergeInMultiUnicode(feat.GlossAbbreviation, xnGlossAbbrev);
			}
			if (xnRightGlossSep != null)
			{
				MergeInMultiUnicode(feat.RightGlossSep, xnRightGlossSep);
			}
			switch (sSubclassType)
			{
				case "complex":
					FinishMergingComplexFeatDefn(feat as IFsComplexFeature, sComplexType);
					break;
				case "open":
					FinishMergingOpenFeatDefn(feat as IFsOpenFeature, nWsSelector, sWs);
					break;
				case "closed":
					FinishMergingClosedFeatDefn(feat as IFsClosedFeature, rgsValues, id);
					break;
			}
			if (guid != Guid.Empty)
			{
				m_mapLiftGuidFeatDefn.Add(guid, feat);
			}
		}

		/// <summary>
		/// Either set the Type for the complex feature, or remember it to set later after the
		/// appropriate type has been defined.
		/// </summary>
		private void FinishMergingComplexFeatDefn(IFsComplexFeature featComplex, string sComplexType)
		{
			if (sComplexType == null)
			{
				return;		// The user didn't give a type -- see LT-15112.
			}
			if (featComplex.TypeRA == null || m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				IFsFeatStrucType featType;
				if (m_mapIdFeatStrucType.TryGetValue(sComplexType, out featType))
				{
					featComplex.TypeRA = featType;
				}
				else
				{
					m_mapComplexFeatMissingTypeAbbr.Add(featComplex, sComplexType);
				}
			}
		}

		/// <summary>
		/// Set the WsSelector and WritingSystem for the open feature.
		/// </summary>
		private void FinishMergingOpenFeatDefn(IFsOpenFeature featOpen, int nWsSelector, string sWs)
		{
			if (featOpen.WsSelector == 0 || m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				if (nWsSelector != 0)
				{
					featOpen.WsSelector = nWsSelector;
				}
			}
			if (string.IsNullOrEmpty(featOpen.WritingSystem) || m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				if (!string.IsNullOrEmpty(sWs))
				{
					featOpen.WritingSystem = sWs;
				}
			}
		}

		/// <summary>
		/// Either set the Values for the closed feature, or remember them to set later after
		/// they have been defined.
		/// </summary>
		private void FinishMergingClosedFeatDefn(IFsClosedFeature featClosed, List<string> rgsValues, string id)
		{
			if (featClosed.ValuesOC.Count == 0 || m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				var rgsMissing = new List<string>(rgsValues.Count);
				foreach (var sAbbr in rgsValues)
				{
					var key = $"{id}:{sAbbr}";
					IFsSymFeatVal featValue;
					if (m_mapIdAbbrSymFeatVal.TryGetValue(key, out featValue))
					{
						featClosed.ValuesOC.Add(featValue);
						continue;
					}
					if (m_rgPendingSymFeatVal.Count > 0)
					{
						PendingFeatureValue val = null;
						foreach (PendingFeatureValue pfv in m_rgPendingSymFeatVal)
						{
							if (pfv.FeatureId == id && pfv.Id == sAbbr)
							{
								val = pfv;
								break;
							}
						}
						if (val != null)
						{
							StoreSymFeatValInClosedFeature(val.Id, val.Description, val.Label, val.Abbrev, val.CatalogId, val.ShowInGloss, featClosed, val.FeatureId);
							m_rgPendingSymFeatVal.Remove(val);
							continue;
						}
					}
					rgsMissing.Add(sAbbr);
				}
				if (rgsMissing.Count > 0)
				{
					List<string> missingItems;
					if(!m_mapClosedFeatMissingValueAbbrs.TryGetValue(featClosed, out missingItems))
					{
						m_mapClosedFeatMissingValueAbbrs.Add(featClosed, rgsMissing);
					}
				}
			}
		}

		private void MergeInMultiUnicode(IMultiUnicode mu, XmlNode xnField)
		{
			foreach (XmlNode xn in xnField.SelectNodes("form"))
			{
				var sLang = XmlUtils.GetMandatoryAttributeValue(xn, "lang");
				var ws = GetWsFromLiftLang(sLang);
				var xnText = xnField.SelectSingleNode("text");
				var val = xnText?.InnerText;
				if (!string.IsNullOrEmpty(val))
				{
					var tssOld = mu.get_String(ws);
					if (tssOld.Length == 0 || m_msImport != MergeStyle.MsKeepOld)
					{
						mu.set_String(ws, val);
					}
				}
			}
		}

		private IFsFeatDefn CreateDesiredFeatDefn(string sSubclassType, IFsFeatDefn feat)
		{
			switch (sSubclassType)
			{
				case "complex":
					feat = m_factFsComplexFeature.Create();
					break;
				case "open":
					feat = m_factFsOpenFeature.Create();
					break;
				case "closed":
					feat = m_factFsClosedFeature.Create();
					break;
				default:
					feat = null;
					break;
			}
			if (feat != null)
			{
				m_cache.LangProject.MsFeatureSystemOA.FeaturesOC.Add(feat);
			}
			return feat;
		}

		private static IFsFeatDefn ValidateFeatDefnType(string sSubclassType, IFsFeatDefn feat)
		{
			if (feat != null)
			{
				switch (sSubclassType)
				{
					case "complex":
						if (feat.ClassID != FsComplexFeatureTags.kClassId)
							feat = null;
						break;
					case "open":
						if (feat.ClassID != FsOpenFeatureTags.kClassId)
							feat = null;
						break;
					case "closed":
						if (feat.ClassID != FsClosedFeatureTags.kClassId)
							feat = null;
						break;
					default:
						feat = null;
						break;
				}
			}
			return feat;
		}

		private static Guid ConvertStringToGuid(string guidAttr)
		{
			if (!string.IsNullOrEmpty(guidAttr))
			{
				try
				{
					return new Guid(guidAttr);
				}
				catch
				{
				}
			}
			return Guid.Empty;
		}

		/// <summary />
		private void ProcessFeatureStrucType(string id, string guidAttr, string parent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev, string rawXml, IFsFeatureSystem featSystem)
		{
			if (m_factFsFeatStrucType == null)
			{
				m_factFsFeatStrucType = m_cache.ServiceLocator.GetInstance<IFsFeatStrucTypeFactory>();
			}
			if (m_repoFsFeatStrucType == null)
			{
				m_repoFsFeatStrucType = m_cache.ServiceLocator.GetInstance<IFsFeatStrucTypeRepository>();
			}
			FillFeatureMapsIfNeeded();
			var xdoc = new XmlDocument { PreserveWhitespace = true };
			xdoc.LoadXml(rawXml);
			var traits = xdoc.FirstChild.SelectNodes("trait");
			string sCatalogId = null;
			var rgsFeatures = new List<string>();
			foreach (XmlNode xn in traits)
			{
				var name = XmlUtils.GetOptionalAttributeValue(xn, "name");
				switch (name)
				{
					case "catalog-source-id":
						sCatalogId = XmlUtils.GetMandatoryAttributeValue(xn, "value");
						break;
					case "feature":
						rgsFeatures.Add(XmlUtils.GetMandatoryAttributeValue(xn, "value"));
						break;
				}
			}
			IFsFeatStrucType featType;
			if (!m_mapIdFeatStrucType.TryGetValue(id, out featType))
			{
				featType = m_factFsFeatStrucType.Create();
				m_cache.LangProject.MsFeatureSystemOA.TypesOC.Add(featType);
				m_mapIdFeatStrucType.Add(id, featType);
				m_rgnewFeatStrucType.Add(featType);
			}
			AddNewWsToAnalysis();
			MergeInMultiUnicode(featType.Abbreviation, FsFeatDefnTags.kflidAbbreviation, abbrev, featType.Guid);
			MergeInMultiUnicode(featType.Name, FsFeatDefnTags.kflidName, label, featType.Guid);
			MergeInMultiString(featType.Description, FsFeatDefnTags.kflidDescription, description, featType.Guid);
			if (featType.CatalogSourceId == null || m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				if (!string.IsNullOrEmpty(sCatalogId))
				{
					featType.CatalogSourceId = sCatalogId;
				}
			}
			if (featType.FeaturesRS.Count == 0 || m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				featType.FeaturesRS.Clear();
				foreach (var sVal in rgsFeatures)
				{
					IFsFeatDefn feat;
					if (m_mapIdFeatDefn.TryGetValue(sVal, out feat))
					{
						featType.FeaturesRS.Add(feat);
					}
				}
				if (rgsFeatures.Count != featType.FeaturesRS.Count)
				{
					featType.FeaturesRS.Clear();
					m_mapFeatStrucTypeMissingFeatureAbbrs.Add(featType, rgsFeatures);
				}
			}
			// Now try to link up with missing type references.  Note that more than one complex
			// feature may be linked to the same type.
			var rgfeatHandled = new List<IFsComplexFeature>();
			foreach (var kv in m_mapComplexFeatMissingTypeAbbr)
			{
				if (kv.Value == id)
				{
					rgfeatHandled.Add(kv.Key);
					break;
				}
			}
			foreach (var feat in rgfeatHandled)
			{
				feat.TypeRA = featType;
				m_mapComplexFeatMissingTypeAbbr.Remove(feat);
			}
		}

		/// <summary />
		private void ProcessFeatureValue(string range, string id, string guidAttr, string parent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev, string rawXml)
		{
			if (m_repoFsFeatDefn == null)
			{
				m_repoFsFeatDefn = m_cache.ServiceLocator.GetInstance<IFsFeatDefnRepository>();
			}
			if (m_factFsSymFeatVal == null)
			{
				m_factFsSymFeatVal = m_cache.ServiceLocator.GetInstance<IFsSymFeatValFactory>();
			}
			if (m_repoFsSymFeatVal == null)
			{
				m_repoFsSymFeatVal = m_cache.ServiceLocator.GetInstance<IFsSymFeatValRepository>();
			}
			FillFeatureMapsIfNeeded();
			var xdoc = new XmlDocument { PreserveWhitespace = true };
			xdoc.LoadXml(rawXml);
			var traits = xdoc.FirstChild.SelectNodes("trait");
			string sCatalogId = null;
			var fShowInGloss = false;
			foreach (XmlNode xn in traits)
			{
				var name = XmlUtils.GetOptionalAttributeValue(xn, "name");
				switch (name)
				{
					case "catalog-source-id":
						sCatalogId = XmlUtils.GetOptionalAttributeValue(xn, "value");
						break;
					case "show-in-gloss":
						fShowInGloss = XmlUtils.GetBooleanAttributeValue(xn, "value");
						break;
				}
			}
			string sFeatId = null;
			var idxSuffix = range.IndexOf("-feature-value");
			if (idxSuffix > 0)
			{
				sFeatId = range.Substring(0, idxSuffix);
			}
			var guid = ConvertStringToGuid(guidAttr);
			var featClosed = FindRelevantClosedFeature(sFeatId, id, guid);
			if (featClosed == null)
			{
				// Save the information for later in hopes something comes up.
				var pfv = new PendingFeatureValue(sFeatId, id, description, label, abbrev, sCatalogId, fShowInGloss, guid);
				m_rgPendingSymFeatVal.Add(pfv);
				return;
			}
			StoreSymFeatValInClosedFeature(id, description, label, abbrev, sCatalogId, fShowInGloss, featClosed, sFeatId);
		}

		/// <summary />
		private void StoreSymFeatValInClosedFeature(string id, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev, string sCatalogId, bool fShowInGloss, IFsClosedFeature featClosed, string featId)
		{
			var fNew = false;
			var val = FindMatchingFeatValue(featClosed, id);
			if (val == null)
			{
				val = m_factFsSymFeatVal.Create();
				featClosed.ValuesOC.Add(val);
				fNew = true;
			}
			AddNewWsToAnalysis();
			MergeInMultiUnicode(val.Abbreviation, FsSymFeatValTags.kflidAbbreviation, abbrev, val.Guid);
			MergeInMultiUnicode(val.Name, FsSymFeatValTags.kflidName, label, val.Guid);
			MergeInMultiString(val.Description, FsSymFeatValTags.kflidDescription, description, val.Guid);
			if (fNew || m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				if (!string.IsNullOrEmpty(sCatalogId))
				{
					val.CatalogSourceId = sCatalogId;
				}
				if (fShowInGloss)
				{
					val.ShowInGloss = fShowInGloss;
				}
			}
			// update the map to find this later.
			m_mapIdAbbrSymFeatVal[$"{featId}:{id}"] = val;
		}

		private IFsSymFeatVal FindMatchingFeatValue(IFsClosedFeature featClosed, string id)
		{
			foreach (var sfv in featClosed.ValuesOC)
			{
				for (var i = 0; i < sfv.Abbreviation.StringCount; ++i)
				{
					int ws;
					var tss = sfv.Abbreviation.GetStringFromIndex(i, out ws);
					if (tss.Text == id)
					{
						return sfv;
					}
				}
			}
			return null;
		}

		private IFsClosedFeature FindRelevantClosedFeature(string sFeatId, string id, Guid guid)
		{
			IFsClosedFeature featClosed = null;
			if (guid != Guid.Empty)
			{
				IFsFeatDefn feat;
				if (m_mapLiftGuidFeatDefn.TryGetValue(guid, out feat))
				{
					featClosed = feat as IFsClosedFeature;
				}
			}
			if (featClosed == null && !string.IsNullOrEmpty(sFeatId))
			{
				IFsFeatDefn feat;
				if (m_mapIdFeatDefn.TryGetValue(sFeatId, out feat))
				{
					featClosed = feat as IFsClosedFeature;
				}
			}
			if (featClosed == null)
			{
				foreach (var kv in m_mapClosedFeatMissingValueAbbrs)
				{
					if (kv.Value.Contains(id))
					{
						kv.Value.Remove(id);
						if (kv.Value.Count == 0)
						{
							m_mapClosedFeatMissingValueAbbrs.Remove(kv.Key);
						}
						return kv.Key;
					}
				}
			}
			return featClosed;
		}

		private void FillFeatureMapsIfNeeded()
		{
			if (m_mapIdFeatDefn.Count == 0 && m_mapIdFeatStrucType.Count == 0 && m_mapIdAbbrSymFeatVal.Count == 0)
			{
				FillIdFeatDefnMap();
				FillIdFeatStrucTypeMap();
				FillIdAbbrSymFeatValMap();
			}
		}

		private void FillIdFeatDefnMap()
		{
			foreach (var feat in m_cache.LangProject.MsFeatureSystemOA.FeaturesOC)
			{
				for (var i = 0; i < feat.Abbreviation.StringCount; ++i)
				{
					int ws;
					var tssAbbr = feat.Abbreviation.GetStringFromIndex(i, out ws);
					var sAbbr = tssAbbr.Text;
					if (!string.IsNullOrEmpty(sAbbr) && !m_mapIdFeatDefn.ContainsKey(sAbbr))
					{
						m_mapIdFeatDefn.Add(sAbbr, feat);
					}
				}
			}
		}

		private void FillIdFeatStrucTypeMap()
		{
			foreach (var featType in m_cache.LangProject.MsFeatureSystemOA.TypesOC)
			{
				for (var i = 0; i < featType.Abbreviation.StringCount; ++i)
				{
					int ws;
					var tssAbbr = featType.Abbreviation.GetStringFromIndex(i, out ws);
					var sAbbr = tssAbbr.Text;
					if (!string.IsNullOrEmpty(sAbbr) && !m_mapIdFeatStrucType.ContainsKey(sAbbr))
					{
						m_mapIdFeatStrucType.Add(sAbbr, featType);
					}
				}
			}
		}

		private void FillIdAbbrSymFeatValMap()
		{
			foreach (var feat in m_cache.LangProject.MsFeatureSystemOA.FeaturesOC)
			{
				var featClosed = feat as IFsClosedFeature;
				if (featClosed != null)
				{
					var setIds = new HashSet<string>();
					for (var i = 0; i < featClosed.Abbreviation.StringCount; ++i)
					{
						int ws;
						var tssAbbr = featClosed.Abbreviation.GetStringFromIndex(i, out ws);
						var sAbbr = tssAbbr.Text;
						if (!string.IsNullOrEmpty(sAbbr))
						{
							setIds.Add(sAbbr);
						}
					}
					foreach (var featVal in featClosed.ValuesOC)
					{
						for (var i = 0; i < featVal.Abbreviation.StringCount; ++i)
						{
							int ws;
							var tssAbbr = featVal.Abbreviation.GetStringFromIndex(i, out ws);
							var sAbbr = tssAbbr.Text;
							if (!string.IsNullOrEmpty(sAbbr))
							{
								foreach (string sId in setIds)
								{
									var key = $"{sId}:{sAbbr}";
									if (!m_mapIdAbbrSymFeatVal.ContainsKey(key))
									{
										m_mapIdAbbrSymFeatVal.Add(key, featVal);
									}
								}
							}
						}
					}
				}
			}
		}

		/// <summary />
		private void ProcessStemName(string range, string id, string guidAttr, string parent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev, string rawXml)
		{
			var idx = range.LastIndexOf("-stem-name");
			if (idx <= 0)
			{
				return;
			}
			var sPosName = range.Substring(0, idx);
			ICmPossibility poss;
			if (!m_dictPos.TryGetValue(sPosName, out poss))
			{
				return;
			}
			var pos = poss as IPartOfSpeech;
			if (pos == null)
			{
				return;
			}
			IMoStemName stem;
			var key = $"{sPosName}:{id}";
			if (!m_dictStemName.TryGetValue(key, out stem))
			{
				stem = m_cache.ServiceLocator.GetInstance<IMoStemNameFactory>().Create();
				pos.StemNamesOC.Add(stem);
				m_dictStemName.Add(key, stem);
				m_rgnewStemName.Add(stem);
			}
			AddNewWsToAnalysis();
			MergeInMultiUnicode(stem.Abbreviation, MoStemNameTags.kflidAbbreviation, abbrev, stem.Guid);
			MergeInMultiUnicode(stem.Name, MoStemNameTags.kflidName, label, stem.Guid);
			MergeInMultiString(stem.Description, MoStemNameTags.kflidDescription, description, stem.Guid);

			var setFeats = new HashSet<string>();
			foreach (var ffs in stem.RegionsOC)
			{
				setFeats.Add(ffs.LiftName);
			}
			var xdoc = new XmlDocument { PreserveWhitespace = true };
			xdoc.LoadXml(rawXml);
			var traits = xdoc.FirstChild.SelectNodes("trait");
			foreach (XmlNode xn in traits)
			{
				var name = XmlUtils.GetOptionalAttributeValue(xn, "name");
				if (name == "feature-set")
				{
					var value = XmlUtils.GetOptionalAttributeValue(xn, "value");
					if (setFeats.Contains(value))
					{
						continue;
					}
					var ffs = ParseFeatureString(value, stem);
					if (ffs == null)
					{
						continue;
					}
					setFeats.Add(value);
					var liftName = ffs.LiftName;
					if (liftName != value)
					{
						setFeats.Add(liftName);
					}
				}
			}
		}

		/// <summary />
		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.ProcessFieldDefinition(string tag, LiftMultiText description)
		{
			var key = tag;
			//if the header field we are processing is a custom field, then create it.
			int typeFlid;
			if (IsCustomField(tag, description, out typeFlid))
			{
				Guid idk;
				FindOrCreateCustomField(tag, description, typeFlid, out idk);
				key = typeFlid + tag;
			}
			// We may need this information later, but don't do anything for now except save it.
			m_dictFieldDef.Add(key, MakeSafeLiftMultiText(description));
		}

		/// <summary>
		/// This method uses the tag and description to determine if this is a FLEx custom field. If it is, the FLID for the type is placed in
		/// the typeFlid out parameter.
		/// </summary>
		private bool IsCustomField(string tag, LiftMultiText description, out int typeFlid)
		{
			LiftString lstr;
			if(description.TryGetValue("qaa-x-spec", out lstr))
			{
				var value = lstr.Text;
				var items = value.Split(new char[]{'=',';'});
				for(var i = 0; i < items.Length; ++i)
				{
					if(items[i].Equals("Class") && items.Length >= i + 1)
					{
						try
						{
							typeFlid = m_cache.DomainDataByFlid.MetaDataCache.GetClassId(items[i + 1].Trim());
							return true;
						}
						catch(Exception e)
						{
							//we can't deal with this class, but no need to give up now
							//we won't create a custom field for it, but let's not crash (our bogus cache in the tests crashed here)
							continue;
						}
					}
				}
			}
			typeFlid = -1;
			return false;
		}

		#endregion // ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample> Members

		#region Methods for storing entry data

		/// <summary>
		/// Check whether we have an entry with the same id and the same modification time.
		/// </summary>
		private bool SameEntryModTimes(Extensible info)
		{
			var guid = GetGuidInExtensible(info);
			var obj = GetObjectForGuid(guid);
			if (obj is ILexEntry)
			{
				return AreDatesInSameSecond((obj as ILexEntry).DateModified.ToUniversalTime(), info.ModificationTime.ToUniversalTime());
			}
			return false;
		}

		private static bool AreDatesInSameSecond(DateTime objectTime, DateTime liftTime)
		{
			// Only go down to the second -- ignore any millisecond or microsecond granularity.
			return (objectTime.Date == liftTime.Date &&
					  objectTime.Hour == liftTime.Hour &&
					  objectTime.Minute == liftTime.Minute &&
					  objectTime.Second == liftTime.Second);
		}

		/// <summary>
		/// Create a new lexicon entry from the provided data.
		/// </summary>
		private void CreateNewEntry(CmLiftEntry entry)
		{
			try
			{
				m_fCreatingNewEntry = true;
				bool fNeedNewId;
				var le = CreateNewLexEntry(entry.Guid, out fNeedNewId);
				if (m_cdConflict is ConflictingEntry)
				{
					((ConflictingEntry)m_cdConflict).DupEntry = le;
					m_rgcdConflicts.Add(m_cdConflict);
					m_cdConflict = null;
				}
				StoreEntryId(le, entry);
				le.HomographNumber = entry.Order;
				CreateLexemeForm(le, entry);		// also sets CitationForm if it exists.
				if (fNeedNewId)
				{
					var xdEntryResidue = FindOrCreateResidue(le, entry.Id, LexEntryTags.kflidLiftResidue);
					var xa = xdEntryResidue.FirstChild.Attributes["id"];
					if (xa == null)
					{
						xa = xdEntryResidue.CreateAttribute("id");
						xdEntryResidue.FirstChild.Attributes.Append(xa);
					}
					xa.Value = le.LIFTid;
				}
				ProcessEntryTraits(le, entry);
				ProcessEntryNotes(le, entry);
				ProcessEntryFields(le, entry);
				CreateEntryVariants(le, entry);
				CreateEntryPronunciations(le, entry);
				CreateEntryEtymologies(le, entry);
				ProcessEntryRelations(le, entry);
				foreach (var sense in entry.Senses)
				{
					CreateEntrySense(le, sense);
				}
				if (entry.DateCreated != default(DateTime))
				{
					le.DateCreated = entry.DateCreated.ToLocalTime();
				}
				if (entry.DateModified != default(DateTime))
				{
					m_rgPendingModifyTimes.Add(new PendingModifyTime(le, entry.DateModified.ToLocalTime()));
				}
				StoreAnnotationsAndDatesInResidue(le, entry);
				FinishProcessingEntry(le);
				++m_cEntriesAdded;
			}
			finally
			{
				m_fCreatingNewEntry = false;
			}
		}

		private void StoreEntryId(ILexEntry le, CmLiftEntry entry)
		{
			if (!string.IsNullOrEmpty(entry.Id))
			{
				FindOrCreateResidue(le, entry.Id, LexEntryTags.kflidLiftResidue);
				MapIdToObject(entry.Id, le);
			}
		}

		private void MapIdToObject(string id, ICmObject cmo)
		{
			try
			{
				m_mapIdObject.Add(id, cmo);
			}
			catch (ArgumentException ex)
			{
				// presumably duplicate id.
				ICmObject cmo2;
				string msg = null;
				if (m_mapIdObject.TryGetValue(id, out cmo2))
				{
					if (cmo != cmo2)
					{
						msg = string.Format(LexTextControls.ksDuplicateIdValue, cmo.ClassName, id);
					}
				}
				if (string.IsNullOrEmpty(msg))
				{
					msg = string.Format(LexTextControls.ksProblemId, cmo.ClassName, id);
				}
				m_rgErrorMsgs.Add(msg);
			}
		}

		/// <summary>
		/// Store accumulated import residue for the entry (and its senses), and ensure
		/// that all senses have an MSA, and that duplicate MSAs are merged together.
		/// </summary>
		private void FinishProcessingEntry(ILexEntry le)
		{
			// We don't create/assign MSAs to senses if <grammatical-info> doesn't exist.
			EnsureValidMSAsForSenses(le);;
		}

		private void WriteAccumulatedResidue()
		{
			foreach (var hvo in m_dictResidue.Keys)
			{
				if (!m_cache.ServiceLocator.ObjectRepository.IsValidObjectId(hvo))
				{
					continue; // somehow the object got deleted, discard the residue
				}
				var res = m_dictResidue[hvo];
				var sLiftResidue = res.Document.OuterXml;
				var flid = res.Flid;
				if (!string.IsNullOrEmpty(sLiftResidue) && flid != 0)
				{
					m_cache.MainCacheAccessor.set_UnicodeProp(hvo, flid, sLiftResidue);
				}
			}
			m_dictResidue.Clear();
		}

		/// <summary>
		/// Check whether an existing entry has data that conflicts with an imported entry that
		/// has the same identity (guid).  Senses are not checked, since they can be added to
		/// the existing entry instead of creating an entirely new entry.
		/// </summary>
		private bool EntryHasConflictingData(CmLiftEntry entry)
		{
			m_cdConflict = null;
			var le = entry.CmObject as ILexEntry;
			if (LexemeFormsConflict(le, entry))
			{
				return true;
			}
			if (EntryEtymologiesConflict(le, entry.Etymologies))
			{
				return true;
			}
			if (EntryFieldsConflict(le, entry.Fields))
			{
				return true;
			}
			if (EntryNotesConflict(le, entry.Notes))
			{
				return true;
			}
			if (EntryTraitsConflict(le, entry.Traits))
			{
				return true;
			}
			return EntryVariantsConflict(le, entry.Variants);
		}

		/// <summary>
		/// Add the imported data to an existing lexical entry.
		/// </summary>
		private void MergeIntoExistingEntry(CmLiftEntry entry)
		{
			var le = entry.CmObject as ILexEntry;
			StoreEntryId(le, entry);
			le.HomographNumber = entry.Order;
			MergeLexemeForm(le, entry);		// also sets CitationForm if it exists.
			ProcessEntryTraits(le, entry);
			ProcessEntryNotes(le, entry);
			ProcessEntryFields(le, entry);
			MergeEntryVariants(le, entry);
			MergeEntryPronunciations(le, entry);
			MergeEntryEtymologies(le, entry);
			ProcessEntryRelations(le, entry);
			var map = new Dictionary<CmLiftSense, ILexSense>();
			var setUsed = new HashSet<int>();
			foreach (var sense in entry.Senses)
			{
				var ls = FindExistingSense(le.SensesOS, sense);
				map.Add(sense, ls);
				if (ls != null)
				{
					setUsed.Add(ls.Hvo);
				}
			}
			// If we're keeping only the imported data, delete any unused senses.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				foreach (var hvo in le.SensesOS.ToHvoArray())
				{
					if (!setUsed.Contains(hvo))
					{
						m_deletedObjects.Add(hvo);
					}
				}
			}
			foreach (var sense in entry.Senses)
			{
				ILexSense ls;
				map.TryGetValue(sense, out ls);
				if (ls == null || (m_msImport == MergeStyle.MsKeepBoth && SenseHasConflictingData(ls, sense)))
				{
					CreateEntrySense(le, sense);
				}
				else
				{
					MergeIntoExistingSense(ls, sense);
				}
			}
			if (entry.DateCreated != default(DateTime) && !AreDatesInSameSecond(le.DateCreated.ToUniversalTime(), entry.DateCreated.ToUniversalTime()))
			{
				le.DateCreated = entry.DateCreated.ToLocalTime();
			}
			if (entry.DateModified != default(DateTime))
			{
				m_rgPendingModifyTimes.Add(new PendingModifyTime(le, entry.DateModified.ToLocalTime()));
			}
			StoreAnnotationsAndDatesInResidue(le, entry);
			FinishProcessingEntry(le);
		}

		private ILexSense FindExistingSense(ILcmOwningSequence<ILexSense> rgsenses, CmLiftSense sense)
		{
			return sense.CmObject == null ? null : rgsenses.FirstOrDefault(ls => ls.Hvo == sense.CmObject.Hvo);
		}

		private bool SenseHasConflictingData(ILexSense ls, CmLiftSense sense)
		{
			m_cdConflict = null;
			//sense.Order;
			AddNewWsToAnalysis();
			if (MultiUnicodeStringsConflict(ls.Gloss, sense.Gloss, false, Guid.Empty, 0))
			{
				m_cdConflict = new ConflictingSense("Gloss", ls, this);
				return true;
			}
			if (MultiTsStringsConflict(ls.Definition, sense.Definition))
			{
				m_cdConflict = new ConflictingSense("Definition", ls, this);
				return true;
			}
			if (SenseExamplesConflict(ls, sense.Examples))
			{
				return true;
			}
			if (SenseGramInfoConflicts(ls, sense.GramInfo))
			{
				return true;
			}
			if (SenseIllustrationsConflict(ls, sense.Illustrations))
			{
				return true;
			}
			if (SenseNotesConflict(ls, sense.Notes))
			{
				return true;
			}
			if (SenseRelationsConflict(ls, sense.Relations))
			{
				return true;
			}
			if (SenseReversalsConflict(ls, sense.Reversals))
			{
				return true;
			}
			return SenseTraitsConflict(ls, sense.Traits) || SenseFieldsConflict(ls, sense.Fields);
		}

		private void CreateLexemeForm(ILexEntry le, CmLiftEntry entry)
		{
			if (entry.LexicalForm != null && !entry.LexicalForm.IsEmpty)
			{
				IMoMorphType mmt;
				string realForm;
				AddNewWsToVernacular();
				var tssForm = GetFirstLiftTsString(entry.LexicalForm);
				var mf = CreateMoForm(entry.Traits, tssForm, out mmt, out realForm, le.Guid, LexEntryTags.kflidLexemeForm);
				le.LexemeFormOA = mf;
				FinishMoForm(mf, entry.LexicalForm, tssForm, mmt, realForm, le.Guid, LexEntryTags.kflidLexemeForm);
			}
			if (entry.CitationForm != null && !entry.CitationForm.IsEmpty)
			{
				MergeInAllomorphForms(entry.CitationForm, le.CitationForm, le.LexemeFormOA?.ClassID ?? MoStemAllomorphTags.kClassId, le.Guid, LexEntryTags.kflidCitationForm);
			}
		}

		/// <summary />
		private IMoMorphType FindMorphType(ref string form, out int clsid, Guid guidEntry, int flid)
		{
			var fullForm = form;
			try
			{
				return MorphServices.FindMorphType(m_cache, ref form, out clsid);
			}
			catch (Exception error)
			{
				var bad = new InvalidData(error.Message, guidEntry, flid, fullForm, 0, m_cache, this);
				if (!m_rgInvalidData.Contains(bad))
				{
					m_rgInvalidData.Add(bad);
				}
				form = fullForm;
				clsid = MoStemAllomorphTags.kClassId;
				return GetExistingMoMorphType(MoMorphTypeTags.kguidMorphStem);
			}
		}

		private void MergeLexemeForm(ILexEntry le, CmLiftEntry entry)
		{
			if (entry.LexicalForm != null && !entry.LexicalForm.IsEmpty)
			{
				var mf = le.LexemeFormOA;
				var clsid = 0;
				if (mf == null)
				{
					var form = Icu.Normalize(XmlUtils.DecodeXmlAttribute(entry.LexicalForm.FirstValue.Value.Text), Icu.UNormalizationMode.UNORM_NFD);
					var mmt = FindMorphType(ref form, out clsid, le.Guid, LexEntryTags.kflidLexemeForm);
					mf = mmt.IsAffixType ? (IMoForm)CreateNewMoAffixAllomorph() : CreateNewMoStemAllomorph();
					le.LexemeFormOA = mf;
					mf.MorphTypeRA = mmt;
				}
				else
				{
					clsid = mf.ClassID;
				}
				MergeInAllomorphForms(entry.LexicalForm, mf.Form, clsid, le.Guid, LexEntryTags.kflidLexemeForm);
			}
			if (entry.CitationForm != null && !entry.CitationForm.IsEmpty)
			{
				MergeInAllomorphForms(entry.CitationForm, le.CitationForm, le.LexemeFormOA?.ClassID ?? MoStemAllomorphTags.kClassId, le.Guid, LexEntryTags.kflidCitationForm);
			}
		}

		private bool LexemeFormsConflict(ILexEntry le, CmLiftEntry entry)
		{
			AddNewWsToVernacular();
			if (MultiUnicodeStringsConflict(le.CitationForm, entry.CitationForm, true, le.Guid, LexEntryTags.kflidCitationForm))
			{
				m_cdConflict = new ConflictingEntry("Citation Form", le, this);
				return true;
			}
			if (le.LexemeFormOA != null)
			{
				if (MultiUnicodeStringsConflict(le.LexemeFormOA.Form, entry.LexicalForm, true, le.Guid, LexEntryTags.kflidLexemeForm))
				{
					m_cdConflict = new ConflictingEntry("Lexeme Form", le, this);
					return true;
				}
			}
			return false;
		}

		private void ProcessEntryTraits(ILexEntry le, CmLiftEntry entry)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				le.DoNotPublishInRC.Clear();
			}
			foreach (var lt in entry.Traits)
			{
				switch (lt.Name.ToLowerInvariant())
				{
					case "entrytype":	// original FLEX export = EntryType
					case "entry-type":
						// Save this for use with a <relation type="main" ...> to create LexEntryRef later.
						entry.EntryType = lt.Value;
						break;
					case RangeNames.sDbMorphTypesOAold:	// original FLEX export = MorphType
					case RangeNames.sDbMorphTypesOA:
						ProcessEntryMorphType(le, lt.Value);
						break;
					case RangeNames.sDbPublicationTypesOAold:
					case RangeNames.sDbPublicationTypesOA:
						ProcessEntryPublicationSettings(le, lt.Value);
						break;
					case "minorentrycondition":		// original FLEX export = MinorEntryCondition
					case "minor-entry-condition":
						// Save this for use with a <relation type="main" ...> to create LexEntryRef later.
						entry.MinorEntryCondition = lt.Value;
						break;
					case "excludeasheadword":	// original FLEX export = ExcludeAsHeadword
					case "exclude-as-headword":
						entry.ExcludeAsHeadword = false; // MDL: replace when Lift interface is updated for ShowHeadwordIn
						break;
					case "donotuseforparsing":	// original FLEX export = DoNotUseForParsing
					case "do-not-use-for-parsing":
						var fDontUse = XmlUtils.GetBooleanAttributeValue(lt.Value);
						if (le.DoNotUseForParsing != fDontUse && (m_fCreatingNewEntry || m_msImport != MergeStyle.MsKeepOld))
						{
							le.DoNotUseForParsing = fDontUse;
						}
						break;
					case RangeNames.sDbDialectLabelsOA:
						ProcessEntryDialects(le, lt.Value);
						break;
					default:
						ProcessUnknownTrait(entry, lt, le);
						break;
				}
			}
		}

		private void ProcessEntryPublicationSettings(ILexEntry le, string traitValue)
		{
			// This does fine adding places to not publish an entry,
			// what about removing such exclusions?
			var publication = FindOrCreatePublicationType(traitValue);
			if (!le.DoNotPublishInRC.Contains(publication))
			{
				le.DoNotPublishInRC.Add(publication);
			}
		}

		private void ProcessEntryDialects(ILexEntry le, string traitValue)
		{
			// This does fine adding dialects to an entry,
			// it won't remove dialects that the merging entry doesn't use.
			var dialect = FindOrCreateDialect(traitValue);
			if (!le.DialectLabelsRS.Contains(dialect))
			{
				le.DialectLabelsRS.Add(dialect);
			}
		}

		private void ProcessUnknownTrait(LiftObject liftObject, LiftTrait lt, ICmObject cmo)
		{
			var clid = cmo.ClassID;
			LiftMultiText desc; // safe-XML
			var sType = lt.Name;
			if (!m_dictFieldDef.TryGetValue(m_cache.DomainDataByFlid.MetaDataCache.GetClassId(cmo.ClassName) + lt.Name, out desc))
			{
				StoreTraitAsResidue(cmo, lt);
				return;
			}
			Guid possListGuid;
			var flid = FindOrCreateCustomField(sType, desc, clid, out possListGuid);
			if (flid == 0)
			{
				StoreTraitAsResidue(cmo, lt);
			}
			else
			{
				ProcessCustomFieldTraitData(cmo.Hvo, flid, lt, possListGuid);
			}
		}

		private void ProcessCustomFieldTraitData(int hvo, int flid, LiftTrait trait, Guid possListGuid)
		{
			var type = (CellarPropertyType)m_cache.MetaDataCacheAccessor.GetFieldType(flid);
			var sda = m_cache.DomainDataByFlid as ISilDataAccessManaged;
			Debug.Assert(sda != null);
			switch (type)
			{
				case CellarPropertyType.Integer:
					sda.SetInt(hvo, flid, Convert.ToInt32(trait.Value));
					break;
				case CellarPropertyType.GenDate:
					var genDate = LiftExporter.GetGenDateFromInt(Convert.ToInt32(trait.Value));
					sda.SetGenDate(hvo, flid, genDate);
					break;
				case CellarPropertyType.OwningAtomic:
				case CellarPropertyType.ReferenceAtomic:
					//"<trait name=\"CustomFldEntry ListSingleItem\" value=\"graphology\"/>",
					var repo = m_cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
					var customList = repo.GetObject(possListGuid);
					var listItem = customList.FindOrCreatePossibility(trait.Value.ToString(), m_cache.DefaultAnalWs);
					sda.SetObjProp(hvo, flid, listItem.Hvo);
					break;
				case CellarPropertyType.OwningCollection:
				case CellarPropertyType.ReferenceCollection:
				case CellarPropertyType.OwningSequence:
				case CellarPropertyType.ReferenceSequence:
					//"<trait name=\"CustomFldEntry-ListMultiItems\" value=\"anatomy\"/>",
					//"<trait name=\"CustomFldEntry-ListMultiItems\" value=\"artificial intelligence\"/>",
					var repo2 = m_cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
					var customList2 = repo2.GetObject(possListGuid);
					var listItem2 = customList2.FindOrCreatePossibility(trait.Value.ToString(), m_cache.DefaultAnalWs);
					var listRefCollectionItems = new int[1];
					listRefCollectionItems[0] = listItem2.Hvo;
					var numberitems = sda.get_VecSize(hvo, flid);
					sda.Replace(hvo, flid, numberitems, numberitems, listRefCollectionItems, 1);
					break;
				default:
					// TODO: Warn user he's smarter than we are?
					break;
			}
		}

		private void ProcessEntryMorphType(ILexEntry le, string traitValue)
		{
			var mmt = FindMorphType(traitValue);
			if (le.LexemeFormOA == null)
			{
				if (mmt.IsAffixType)
				{
					le.LexemeFormOA = CreateNewMoAffixAllomorph();
				}
				else
				{
					le.LexemeFormOA = CreateNewMoStemAllomorph();
				}
				le.LexemeFormOA.MorphTypeRA = mmt;
			}
			else if (le.LexemeFormOA.MorphTypeRA != mmt && (m_fCreatingNewEntry || m_msImport != MergeStyle.MsKeepOld || le.LexemeFormOA.MorphTypeRA == null))
			{
				if (mmt.IsAffixType)
				{
					if (le.LexemeFormOA is IMoStemAllomorph)
					{
						ReplaceMoForm(le.LexemeFormOA, CreateNewMoAffixAllomorph());
					}
				}
				else
				{
					if (!(le.LexemeFormOA is IMoStemAllomorph))
					{
						ReplaceMoForm(le.LexemeFormOA, CreateNewMoStemAllomorph());
					}
				}
				le.LexemeFormOA.MorphTypeRA = mmt;
			}
		}

		/// <summary>
		/// Replace a form (typically changing it from stem to affix or v.v.),
		/// and if we had created residue for the old form, transfer it to the new one.
		/// </summary>
		private void ReplaceMoForm(IMoForm oldForm, IMoForm newForm)
		{
			LiftResidue residue;
			if (m_dictResidue.TryGetValue(oldForm.Hvo, out residue))
			{
				m_dictResidue.Remove(oldForm.Hvo);
			}
			((ILexEntry)oldForm.Owner).ReplaceMoForm(oldForm, newForm);
			if (residue != null)
			{
				m_dictResidue[newForm.Hvo] = residue;
			}
		}

		private bool EntryTraitsConflict(ILexEntry le, List<LiftTrait> list)
		{
			foreach (var lt in list)
			{
				switch (lt.Name.ToLowerInvariant())
				{
					case "entrytype":	// original FLEX export = EntryType
					case "entry-type":
						// This trait is no longer used by FLEX.
						break;
					case RangeNames.sDbMorphTypesOAold:	// original FLEX export = MorphType
					case RangeNames.sDbMorphTypesOA:
						if (le.LexemeFormOA != null && le.LexemeFormOA.MorphTypeRA != null)
						{
							var mmt = FindMorphType(lt.Value);
							if (le.LexemeFormOA.MorphTypeRA != mmt)
							{
								m_cdConflict = new ConflictingEntry("Morph Type", le, this);
								return true;
							}
						}
						break;
					case "minorentrycondition":		// original FLEX export = MinorEntryCondition
					case "minor-entry-condition":
						// This trait is no longer used by FLEX.
						break;
					case "excludeasheadword":	// original FLEX export = ExcludeAsHeadword
					case "exclude-as-headword":
						// MDL: Remove when Lift interface is updated for ShowHeadwordIn
						break;
					case "donotuseforparsing":	// original FLEX export = DoNotUseForParsing
					case "do-not-use-for-parsing":
						var fDontUse = XmlUtils.GetBooleanAttributeValue(lt.Value);
						if (le.DoNotUseForParsing != fDontUse)
						{
							m_cdConflict = new ConflictingEntry("Do Not Use For Parsing", le, this);
							return true;
						}
						break;
				}
			}
			return false;
		}

		private void ProcessEntryNotes(ILexEntry le, CmLiftEntry entry)
		{
			AddNewWsToAnalysis();
			foreach (var note in entry.Notes)
			{
				if (note.Type == null)
				{
					note.Type = string.Empty;
				}
				switch (note.Type.ToLowerInvariant())
				{
					case "bibliography":
						MergeInMultiString(le.Bibliography, LexEntryTags.kflidBibliography, note.Content, le.Guid);
						break;
					case "":		// WeSay uses untyped notes in entries; LIFT now exports like this.
					case "comment":	// older Flex exported LIFT files have this type value.
						MergeInMultiString(le.Comment, LexEntryTags.kflidComment, note.Content, le.Guid);
						break;
					case RangeNames.sRestrictionsOA:
						MergeInMultiUnicode(le.Restrictions, LexEntryTags.kflidRestrictions, note.Content, le.Guid);
						break;
					default:
						StoreNoteAsResidue(le, note);
						break;
				}
			}
		}

		private bool EntryNotesConflict(ILexEntry le, List<CmLiftNote> list)
		{
			AddNewWsToAnalysis();
			foreach (var note in list)
			{
				if (note.Type == null)
				{
					note.Type = string.Empty;
				}
				switch (note.Type.ToLowerInvariant())
				{
					case "bibliography":
						if (MultiTsStringsConflict(le.Bibliography, note.Content))
						{
							m_cdConflict = new ConflictingEntry("Bibliography", le, this);
							return true;
						}
						break;
					case "comment":
						if (MultiTsStringsConflict(le.Comment, note.Content))
						{
							m_cdConflict = new ConflictingEntry("Note", le, this);
							return true;
						}
						break;
					case RangeNames.sRestrictionsOA:
						if (MultiUnicodeStringsConflict(le.Restrictions, note.Content, false, Guid.Empty, 0))
						{
							m_cdConflict = new ConflictingEntry("Restrictions", le, this);
							return true;
						}
						break;
				}
			}
			return false;
		}

		private void ProcessEntryFields(ILexEntry le, CmLiftEntry entry)
		{
			foreach (var lf in entry.Fields)
			{
				AddNewWsToAnalysis();
				switch (lf.Type.ToLowerInvariant())
				{
					case "importresidue":
					case "import-residue":
						le.ImportResidue = StoreTsStringValue(m_fCreatingNewEntry, le.ImportResidue, lf.Content);
						break;
					case "literal_meaning":	// original FLEX export
					case "literal-meaning":
						MergeInMultiString(le.LiteralMeaning, LexEntryTags.kflidLiteralMeaning, lf.Content, le.Guid);
						break;
					case "summary_definition":	// original FLEX export
					case "summary-definition":
						MergeInMultiString(le.SummaryDefinition, LexEntryTags.kflidSummaryDefinition, lf.Content, le.Guid);
						break;
					default:
						ProcessUnknownField(le, entry, lf,
							"LexEntry", "custom-entry-", LexEntryTags.kClassId);
						break;
				}
			}
		}

		private bool EntryFieldsConflict(ILexEntry le, List<LiftField> list)
		{
			foreach (var lf in list)
			{
				switch (lf.Type.ToLowerInvariant())
				{
					case "importresidue":
					case "import-residue":
						if (le.ImportResidue != null && le.ImportResidue.Length != 0)
						{
							var tsb = le.ImportResidue.GetBldr();
							var idx = tsb.Text.IndexOf("<lift-residue id=");
							if (idx >= 0)
							{
								tsb.Replace(idx, tsb.Length, null, null);
							}
							IgnoreNewWs();
							if (StringsConflict(tsb.GetString(), GetFirstLiftTsString(lf.Content)))
							{
								m_cdConflict = new ConflictingEntry("Import Residue", le, this);
								return true;
							}
						}
						break;
					case "literal_meaning":	// original FLEX export
					case "literal-meaning":
						AddNewWsToAnalysis();
						if (MultiTsStringsConflict(le.LiteralMeaning, lf.Content))
						{
							m_cdConflict = new ConflictingEntry("Literal Meaning", le, this);
							return true;
						}
						break;
					case "summary_definition":	// original FLEX export
					case "summary-definition":
						AddNewWsToAnalysis();
						if (MultiTsStringsConflict(le.SummaryDefinition, lf.Content))
						{
							m_cdConflict = new ConflictingEntry("Summary Definition", le, this);
							return true;
						}
						break;
					default:
						int flid;
						if (m_dictCustomFlid.TryGetValue("LexEntry-" + lf.Type, out flid))
						{
							if (CustomFieldDataConflicts(le.Hvo, flid, lf.Content))
							{
								m_cdConflict = new ConflictingEntry($"{lf.Type} (custom field)", le, this);
								return true;
							}
						}
						break;
				}
			}
			return false;
		}

		/// <summary />
		private void ProcessCustomFieldData(int hvo, int flid, LiftMultiText contents)
		{
			var type = (CellarPropertyType)m_cache.MetaDataCacheAccessor.GetFieldType(flid);
			HandleWsSettingsForCustomField(flid);
			var cmo = GetObjectForId(hvo);
			ITsMultiString tsm;
			switch (type)
			{
				case CellarPropertyType.String:
					var tss = StoreTsStringValue(m_fCreatingNewEntry | m_fCreatingNewSense, m_cache.MainCacheAccessor.get_StringProp(hvo, flid), contents);
					m_cache.MainCacheAccessor.SetString(hvo, flid, tss);
					break;
				case CellarPropertyType.MultiString:
					tsm = m_cache.MainCacheAccessor.get_MultiStringProp(hvo, flid);
					MergeInMultiString(tsm, flid, contents, cmo.Guid);
					break;
				case CellarPropertyType.MultiUnicode:
					tsm = m_cache.MainCacheAccessor.get_MultiStringProp(hvo, flid);
					MergeInMultiUnicode(tsm, flid, contents, cmo.Guid);
					break;
				case CellarPropertyType.OwningAtomic:
					var destName = m_cache.MetaDataCacheAccessor.GetDstClsName(flid);
					if (destName == "StText")
					{
						ProcessStTextField(hvo, flid, contents);
					}
					break;
				default:
					// TODO: Warn user he's smarter than we are?
					break;
			}
		}

		/// <summary />
		private void ProcessStTextField(int hvoOwner, int flid, LiftMultiText contents)
		{
			var hvoText = m_cache.DomainDataByFlid.get_ObjectProp(hvoOwner, flid);
			var fNew = false;
			if (hvoText == 0)
			{
				hvoText = m_cache.DomainDataByFlid.MakeNewObject(StTextTags.kClassId, hvoOwner, flid, -2);
				fNew = true;
			}
			var text = m_repoCmObject.GetObject(hvoText) as IStText;
			Debug.Assert(text != null);

			var paras = ParseMultipleParagraphs(contents);
			var paraFact = m_cache.ServiceLocator.GetInstance<IStTxtParaFactory>();
			if (fNew)
			{
				InsertAllParagraphs(text, paras, paraFact);
			}
			else
			{
				var cparaOld = text.ParagraphsOS.Count;
				var cparaNew = paras.Count;
				switch (m_msImport)
				{
					case MergeStyle.MsKeepOld:
						// Add any additional paragraphs to the end of the text.
						for (var i = cparaOld; i < cparaNew; ++i)
						{
							var para = paras[i];
							var stPara = paraFact.Create();
							text.ParagraphsOS.Add(stPara);
							if (!string.IsNullOrEmpty(para.StyleName))
							{
								stPara.StyleName = para.StyleName;
							}
							stPara.Contents = para.Contents;
						}
						break;
					case MergeStyle.MsKeepNew:
						// Replace old paragraph contents with the new.  If there are extra old paragraphs,
						// keep them.  (This decision is apt to change.)
						for (var i = 0; i < cparaNew; ++i)
						{
							var para = paras[i];
							IStTxtPara stPara;
							if (i < cparaOld)
							{
								stPara = text.ParagraphsOS[i] as IStTxtPara;
								if (stPara == null)
								{
									stPara = paraFact.Create();
									text.ParagraphsOS[i] = stPara;
								}
							}
							else
							{
								stPara = paraFact.Create();
								text.ParagraphsOS.Add(stPara);
							}
							if (!string.IsNullOrEmpty(para.StyleName))
							{
								stPara.StyleName = para.StyleName;
							}
							stPara.Contents = para.Contents;
						}
						break;
					case MergeStyle.MsKeepBoth:
						// If the new and old paragraphs differ (ignoring all style and writing system information),
						// then append the new paragraph content to the old, with a Unicode line separator character
						// between them.
						var indexes = new List<int>();
						for (var i = 0; i < cparaNew; ++i)
						{
							var para = paras[i];
							IStTxtPara stPara;
							if (i < cparaOld)
							{
								stPara = text.ParagraphsOS[i] as IStTxtPara;
								if (stPara != null && para.Contents.Text == stPara.Contents.Text)
								{
									continue;
								}
								if (stPara != null)
								{
									var tisb = stPara.Contents.GetIncBldr();
									tisb.Append("\u2028");
									tisb.AppendTsString(para.Contents);
									stPara.Contents = tisb.GetString();
								}
								else
								{
									indexes.Add(i);
								}
							}
							else
							{
								stPara = paraFact.Create();
								text.ParagraphsOS.Add(stPara);
								if (!string.IsNullOrEmpty(para.StyleName))
								{
									stPara.StyleName = para.StyleName;
								}
								stPara.Contents = para.Contents;
							}
						}
						// handle incompatible paragraphs by inserting a new paragraph immediately following
						// the old one.
						for (var j = indexes.Count - 1; j >= 0; --j)
						{
							var ipara = indexes[j];
							var stPara = paraFact.Create();
							text.ParagraphsOS.Insert(ipara + 1, stPara);
							var para = paras[ipara];
							if (!string.IsNullOrEmpty(para.StyleName))
							{
								stPara.StyleName = para.StyleName;
							}
							stPara.Contents = para.Contents;
						}
						break;
					case MergeStyle.MsKeepOnlyNew:
						text.ParagraphsOS.Clear();
						Debug.Assert(text.ParagraphsOS.Count == 0);
						InsertAllParagraphs(text, paras, paraFact);
						break;
				}
			}
		}

		private static void InsertAllParagraphs(IStText text, List<ParaData> paras, IStTxtParaFactory paraFact)
		{
			foreach (var para in paras)
			{
				var stPara = paraFact.Create();
				text.ParagraphsOS.Add(stPara);
				if (!string.IsNullOrEmpty(para.StyleName))
				{
					stPara.StyleName = para.StyleName;
				}
				stPara.Contents = para.Contents;
			}
		}

		/// <summary />
		private List<ParaData> ParseMultipleParagraphs(LiftMultiText contents)
		{
			var paras = new List<ParaData>();
			if (contents.Keys.Count > 1)
			{
				// Complain vociferously??
			}
			var lang = contents.Keys.FirstOrDefault();
			if (lang == null)
			{
				return paras;
			}
			var wsText = GetWsFromLiftLang(lang);
			var text = contents[lang]; // safe XML
			if (text.Text == null)
			{
				return paras;
			}
			var ich = 0;
			ParaData para = null;
			var fNewPara = true;
			var tisb = TsStringUtils.MakeIncStrBldr();
			tisb.SetIntPropValues((int) FwTextPropType.ktptWs, 0, wsText);
			var wsCurrent = wsText;
			string styleCurrent = null;
			foreach (var span in text.Spans)
			{
				var fParaStyle = false;
				if (ich < span.Index)
				{
					// text before this span.
					var len = span.Index - ich;
					var data = text.Text.Substring(ich, len); // safeXML
					if (data.Replace("\u2028", "").Replace(" ", "").Replace("\t", "") == "\u2029")
					{
						fNewPara = true;
					}
					else
					{
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsText);
						tisb.SetStrPropValue((int) FwTextPropType.ktptNamedStyle, null);
						tisb.Append(XmlUtils.DecodeXmlAttribute(data));
					}
				}
				if (fNewPara)
				{
					if (para != null)
					{
						para.Contents = tisb.GetString();
						paras.Add(para);
					}
					para = new ParaData();
					tisb.Clear();
					tisb.ClearProps();
					fNewPara = false;
					if (!string.IsNullOrEmpty(span.Class) && string.IsNullOrEmpty(span.Lang))
					{
						if (IsParaStyle(span.Class))
						{
							para.StyleName = span.Class;
							tisb.SetIntPropValues((int) FwTextPropType.ktptWs, 0, wsText);
							fParaStyle = true;
						}
					}
				}
				if (!fParaStyle)
				{
					wsCurrent = string.IsNullOrEmpty(span.Lang) ? wsText : GetWsFromLiftLang(span.Lang);
					styleCurrent = string.IsNullOrEmpty(span.Class) ? null : span.Class;
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsCurrent);
					tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, styleCurrent);
				}
				if (span.Spans.Count > 0)
				{
					ProcessNestedSpans(text, span, wsCurrent, styleCurrent, tisb);
				}
				else
				{
					tisb.Append(XmlUtils.DecodeXmlAttribute(text.Text.Substring(span.Index, span.Length)));
				}
				ich = span.Index + span.Length;
			}
			if (ich < text.Text.Length)
			{
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsText);
				tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);
				tisb.Append(XmlUtils.DecodeXmlAttribute(text.Text.Substring(ich, text.Text.Length - ich)));
			}
			if (tisb.Text.Length > 0)
			{
				if (para == null)
				{
					para = new ParaData();
				}
				para.Contents = tisb.GetString();
			}
			if (para != null)
			{
				paras.Add(para);
			}
			return paras;
		}

		private HashSet<string> m_paraStyles;
		private bool IsParaStyle(string styleName)
		{
			if (m_paraStyles == null)
			{
				m_paraStyles = new HashSet<string>();
				var repoStyles = m_cache.ServiceLocator.GetInstance<IStStyleRepository>();
				foreach (var style in repoStyles.AllInstances())
				{
					if (style.Type == StyleType.kstParagraph)
					{
						m_paraStyles.Add(style.Name);
					}
				}
			}
			return m_paraStyles.Contains(styleName);
		}

		private void ProcessNestedSpans(LiftString text, LiftSpan span, int wsSpan, string styleSpan, ITsIncStrBldr tisb)
		{
			var ich = span.Index;
			foreach (var subspan in span.Spans)
			{
				if (ich < subspan.Index)
				{
					var len = subspan.Index - ich;
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsSpan);
					tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, styleSpan);
					tisb.Append(text.Text.Substring(ich, len));
				}
				var wsCurrent = string.IsNullOrEmpty(subspan.Lang) ? wsSpan : GetWsFromLiftLang(span.Lang);
				var styleCurrent = string.IsNullOrEmpty(subspan.Class) ? styleSpan : subspan.Class;
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsCurrent);
				tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, styleCurrent);
				if (subspan.Spans.Count > 0)
				{
					ProcessNestedSpans(text, subspan, wsCurrent, styleCurrent, tisb);
				}
				else
				{
					tisb.Append(text.Text.Substring(subspan.Index, subspan.Length));
				}
				ich = subspan.Index + subspan.Length;
			}
			if (ich < span.Index + span.Length)
			{
				var ichLim = span.Index + span.Length;
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsSpan);
				tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, styleSpan);
				tisb.Append(text.Text.Substring(ich, ichLim - ich));
			}
		}

		private void HandleWsSettingsForCustomField(int flid)
		{
			var wsFlid = m_cache.MetaDataCacheAccessor.GetFieldWs(flid);
			switch (wsFlid)
			{
				case WritingSystemServices.kwsAnal:
				case WritingSystemServices.kwsAnals:
				case WritingSystemServices.kwsFirstAnal:
					AddNewWsToAnalysis();		// if new ws, add to analysis list
					break;
				case WritingSystemServices.kwsVern:
				case WritingSystemServices.kwsVerns:
				case WritingSystemServices.kwsFirstVern:
					AddNewWsToVernacular();		// if new ws, add to vernacular list
					break;
				case WritingSystemServices.kwsAnalVerns:
				case WritingSystemServices.kwsVernAnals:
				case WritingSystemServices.kwsFirstAnalOrVern:
				case WritingSystemServices.kwsFirstVernOrAnal:
					AddNewWsToBothVernAnal();		// if new ws, add to both vernacular and analysis lists
					break;
				default:
					IgnoreNewWs();	// if new ws, don't add to either vernacular or analysis lists
					break;
			}
		}

		/// <summary />
		private bool CustomFieldDataConflicts(int hvo, int flid, LiftMultiText contents)
		{
			var type = (CellarPropertyType)m_cache.MetaDataCacheAccessor.GetFieldType(flid);
			HandleWsSettingsForCustomField(flid);
			ITsMultiString tsm;
			switch (type)
			{
				case CellarPropertyType.String:
					var tss = m_cache.MainCacheAccessor.get_StringProp(hvo, flid);
					if (StringsConflict(tss, GetFirstLiftTsString(contents)))
					{
						return true;
					}
					break;
				case CellarPropertyType.MultiString:
					tsm = m_cache.MainCacheAccessor.get_MultiStringProp(hvo, flid);
					if (MultiTsStringsConflict(tsm, contents))
					{
						return true;
					}
					break;
				case CellarPropertyType.MultiUnicode:
					tsm = m_cache.MainCacheAccessor.get_MultiStringProp(hvo, flid);
					if (MultiUnicodeStringsConflict(tsm, contents, false, Guid.Empty, 0))
					{
						return true;
					}
					break;
				default:
					break;
			}
			return false;
		}

		private void CreateEntryVariants(ILexEntry le, CmLiftEntry entry)
		{
			foreach (var lv in entry.Variants)
			{
				AddNewWsToVernacular();
				var tssForm = GetFirstLiftTsString(lv.Form);
				IMoMorphType mmt;
				string realForm;
				var mf = CreateMoForm(lv.Traits, tssForm, out mmt, out realForm, le.Guid, LexEntryTags.kflidAlternateForms);
				le.AlternateFormsOS.Add(mf);
				FinishMoForm(mf, lv.Form, tssForm, mmt, realForm, le.Guid, LexEntryTags.kflidAlternateForms);
				bool fTypeSpecified;
				ProcessMoFormTraits(mf, lv, out mf, out fTypeSpecified);
				ProcessMoFormFields(mf, lv);
				StoreResidueFromVariant(mf, lv);
				if (!fTypeSpecified)
				{
					mf.MorphTypeRA = null;
				}
			}
		}

		private IMoForm CreateMoForm(List<LiftTrait> traits, ITsString tssForm, out IMoMorphType mmt, out string realForm, Guid guidEntry, int flid)
		{
			// Try to create the proper type of allomorph form to begin with.  It takes over
			// 200ms to delete one we just created!  (See LT-9006.)
			int clsidForm;
			if (tssForm?.Text == null)
			{
				clsidForm = MoStemAllomorphTags.kClassId;
				var cmo = GetObjectForGuid(MoMorphTypeTags.kguidMorphStem);
				mmt = cmo as IMoMorphType;
				realForm = null;
			}
			else
			{
				realForm = tssForm.Text;
				mmt = FindMorphType(ref realForm, out clsidForm, guidEntry, flid);
			}
			IMoMorphType mmt2;
			var clsidForm2 = GetMoFormClassFromTraits(traits, out mmt2);
			if (clsidForm2 != 0 && mmt2 != null)
			{
				if (mmt2 != mmt)
				{
					mmt = mmt2;
				}
				clsidForm = clsidForm2;
			}
			switch (clsidForm)
			{
				case MoStemAllomorphTags.kClassId:
					return CreateNewMoStemAllomorph();
				case MoAffixAllomorphTags.kClassId:
					return CreateNewMoAffixAllomorph();
				default:
					throw new InvalidProgramException(
						"unexpected MoForm subclass returned from FindMorphType or GetMoFormClassFromTraits");
			}
		}

		/// <summary />
		private void FinishMoForm(IMoForm mf, LiftMultiText forms, ITsString tssForm, IMoMorphType mmt, string realForm, Guid guidEntry, int flid)
		{
			mf.MorphTypeRA = mmt; // Has to be done, before the next call.
			if (tssForm != null)
			{
				mf.FormMinusReservedMarkers = tssForm.Text != realForm ? TsStringUtils.MakeString(realForm, TsStringUtils.GetWsAtOffset(tssForm, 0)) : tssForm;
			}
			MergeInAllomorphForms(forms, mf.Form, mf.ClassID, guidEntry, flid);
		}

		private void MergeEntryVariants(ILexEntry le, CmLiftEntry entry)
		{
			var dictHvoVariant = new Dictionary<int, CmLiftVariant>();
			foreach (var lv in entry.Variants)
			{
				AddNewWsToVernacular();
				var mf = FindMatchingMoForm(le, dictHvoVariant, lv, le.Guid, LexEntryTags.kflidAlternateForms);
				if (mf == null)
				{
					var tssForm = GetFirstLiftTsString(lv.Form);
					if (tssForm?.Text == null)
					{
						continue;
					}
					IMoMorphType mmt;
					string realForm;
					mf = CreateMoForm(lv.Traits, tssForm, out mmt, out realForm, le.Guid, LexEntryTags.kflidAlternateForms);
					le.AlternateFormsOS.Add(mf);
					FinishMoForm(mf, lv.Form, tssForm, mmt, realForm, le.Guid, LexEntryTags.kflidAlternateForms);
					dictHvoVariant.Add(mf.Hvo, lv);
				}
				else
				{
					MergeInAllomorphForms(lv.Form, mf.Form, mf.ClassID, le.Guid, LexEntryTags.kflidAlternateForms);
				}
				bool fTypeSpecified;
				ProcessMoFormTraits(mf, lv, out mf, out fTypeSpecified);
				ProcessMoFormFields(mf, lv);
				StoreResidueFromVariant(mf, lv);
				if (!fTypeSpecified)
				{
					mf.MorphTypeRA = null;
				}
			}
		}

		private bool EntryVariantsConflict(ILexEntry le, List<CmLiftVariant> list)
		{
			if (!le.AlternateFormsOS.Any() || !list.Any())
			{
				return false;
			}
			var cCommon = 0;
			var dictHvoVariant = new Dictionary<int, CmLiftVariant>();
			AddNewWsToVernacular();
			foreach (var lv in list)
			{
				var mf = FindMatchingMoForm(le, dictHvoVariant, lv, le.Guid, LexEntryTags.kflidAlternateForms);
				if (mf != null)
				{
					if (MultiUnicodeStringsConflict(mf.Form, lv.Form, true, le.Guid, LexEntryTags.kflidAlternateForms))
					{
						m_cdConflict = new ConflictingEntry($"Alternate Form ({TsStringAsHtml(mf.Form.BestVernacularAlternative, m_cache)})", le, this);
						return true;
					}
					if (MoFormTraitsConflict(mf, lv.Traits))
					{
						m_cdConflict = new ConflictingEntry($"Alternate Form ({TsStringAsHtml(mf.Form.BestVernacularAlternative, m_cache)}) details", le, this);
						return true;
					}
					++cCommon;
				}
			}
			if (cCommon < Math.Min(le.AlternateFormsOS.Count, list.Count))
			{
				m_cdConflict = new ConflictingEntry("Alternate Forms", le, this);
				return true;
			}
			return false;
		}

		private IMoForm FindMatchingMoForm(ILexEntry le, Dictionary<int, CmLiftVariant> dictHvoVariant, CmLiftVariant lv, Guid guidEntry, int flid)
		{
			IMoForm form = null;
			var cMatches = 0;
			AddNewWsToVernacular();
			foreach (var mf in le.AlternateFormsOS)
			{
				if (dictHvoVariant.ContainsKey(mf.Hvo))
				{
					continue;
				}
				var cCurrent = MultiUnicodeStringMatches(mf.Form, lv.Form, true, guidEntry, flid);
				if (cCurrent > cMatches)
				{
					form = mf;
					cMatches = cCurrent;
				}
			}

			if (form != null)
			{
				dictHvoVariant.Add(form.Hvo, lv);
			}
			return form;
		}

		private int GetMoFormClassFromTraits(List<LiftTrait> traits, out IMoMorphType mmt)
		{
			mmt = null;
			foreach (var lt in traits)
			{
				if (lt.Name.ToLowerInvariant() == RangeNames.sDbMorphTypesOAold || lt.Name.ToLowerInvariant() == RangeNames.sDbMorphTypesOA)
				{
					mmt = FindMorphType(lt.Value);
					var fAffix = mmt.IsAffixType;
					return fAffix ? MoAffixAllomorphTags.kClassId : MoStemAllomorphTags.kClassId;
				}
			}
			return 0;	// no subclass info in the traits
		}

		private void ProcessMoFormTraits(IMoForm form, CmLiftVariant variant, out IMoForm newForm, out bool fTypeSpecified)
		{
			fTypeSpecified = false;
			foreach (var lt in variant.Traits)
			{
				switch (lt.Name.ToLowerInvariant())
				{
					case RangeNames.sDbMorphTypesOAold:	// original FLEX export = MorphType
					case RangeNames.sDbMorphTypesOA:
						if (!m_fCreatingNewEntry && m_msImport == MergeStyle.MsKeepOld && form.MorphTypeRA != null)
						{
							continue;
						}
						var mmt = FindMorphType(lt.Value);
						var fAffix = mmt.IsAffixType;
						if (fAffix && form is IMoStemAllomorph)
						{
							var stem = form as IMoStemAllomorph;
							var affix = CreateNewMoAffixAllomorph();
							var entry = form.Owner as ILexEntry;
							Debug.Assert(entry != null);
							ReplaceMoForm(stem, affix);
							form = affix;
						}
						else if (!fAffix && form is IMoAffixAllomorph)
						{
							var affix = form as IMoAffixAllomorph;
							var stem = CreateNewMoStemAllomorph();
							var entry = form.Owner as ILexEntry;
							Debug.Assert(entry != null);
							ReplaceMoForm(affix, stem);
							form = stem;
						}
						if (mmt != form.MorphTypeRA)
						{
							form.MorphTypeRA = mmt;
						}
						fTypeSpecified = true;
						break;
					case "environment":
						var rgenv = FindOrCreateEnvironment(lt.Value);
						if (form is IMoStemAllomorph)
						{
							AddEnvironmentIfNeeded(rgenv, (form as IMoStemAllomorph).PhoneEnvRC);
						}
						else if (form is IMoAffixAllomorph)
						{
							AddEnvironmentIfNeeded(rgenv, (form as IMoAffixAllomorph).PhoneEnvRC);
						}
						break;
					default:
						ProcessUnknownTrait(variant, lt, form);
						break;
				}
			}
			newForm = form;
		}

		private static void AddEnvironmentIfNeeded(List<IPhEnvironment> rgnew, ILcmReferenceCollection<IPhEnvironment> rgenv)
		{
			if (rgenv != null && rgnew != null)
			{
				var fAlready = false;
				foreach (var env in rgnew)
				{
					if (rgenv.Contains(env))
					{
						fAlready = true;
						break;
					}
				}
				if (!fAlready && rgnew.Count > 0)
				{
					rgenv.Add(rgnew[0]);
				}
			}
		}

		private bool MoFormTraitsConflict(IMoForm mf, List<LiftTrait> list)
		{
			foreach (var lt in list)
			{
				switch (lt.Name.ToLowerInvariant())
				{
					case RangeNames.sDbMorphTypesOAold:	// original FLEX export = MorphType
					case RangeNames.sDbMorphTypesOA:
						if (mf.MorphTypeRA != null)
						{
							var mmt = FindMorphType(lt.Value);
							if (mf.MorphTypeRA != mmt)
							{
								return true;
							}
						}
						break;
				}
			}
			return false;
		}

		private void ProcessMoFormFields(IMoForm mf, CmLiftVariant lv)
		{
			foreach (var field in lv.Fields)
			{
				switch (field.Type.ToLowerInvariant())
				{
					default:
						ProcessUnknownField(mf, lv, field, "MoForm", "custom-variant-", MoFormTags.kClassId);
						break;
				}
			}
		}

		private void CreateEntryPronunciations(ILexEntry le, CmLiftEntry entry)
		{
			foreach (var phon in entry.Pronunciations)
			{
				AddNewWsToVernacular();
				var pron = CreateNewLexPronunciation();
				le.PronunciationsOS.Add(pron);
				MergeInMultiUnicode(pron.Form, LexPronunciationTags.kflidForm, phon.Form, pron.Guid);
				MergePronunciationMedia(pron, phon);
				ProcessPronunciationFieldsAndTraits(pron, phon);
				StoreAnnotationsAndDatesInResidue(pron, phon);
				SavePronunciationWss(phon.Form.Keys);
			}
		}

		private void MergeEntryPronunciations(ILexEntry le, CmLiftEntry entry)
		{
			var dictHvoPhon = new Dictionary<int, CmLiftPhonetic>();
			foreach (var phon in entry.Pronunciations)
			{
				IgnoreNewWs();
				var pron = FindMatchingPronunciation(le, dictHvoPhon, phon);
				if (pron == null)
				{
					pron = CreateNewLexPronunciation();
					le.PronunciationsOS.Add(pron);
					dictHvoPhon.Add(pron.Hvo, phon);
				}
				MergeInMultiUnicode(pron.Form, LexPronunciationTags.kflidForm, phon.Form, pron.Guid);
				MergePronunciationMedia(pron, phon);
				ProcessPronunciationFieldsAndTraits(pron, phon);
				StoreAnnotationsAndDatesInResidue(pron, phon);
				SavePronunciationWss(phon.Form.Keys);
			}
		}

		private void SavePronunciationWss(Dictionary<string, LiftString>.KeyCollection langs)
		{
			AddNewWsToVernacular();
			foreach (var lang in langs)
			{
				var ws = GetWsFromLiftLang(lang);
				if (ws != 0)
				{
					var wsObj = GetExistingWritingSystem(ws);
					if (!m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems.Contains(wsObj))
					{
						m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems.Add(wsObj);
					}
				}
			}
		}

		private string CopyPronunciationFile(string sFile)
		{
			Path.GetDirectoryName(m_sLiftFile);
			// Paths to try for resolving given filename:
			// {directory of LIFT file}/audio/filename
			// {FW LinkedFilesRootDir}/filename
			// {FW LinkedFilesRootDir}/Media/filename
			// {FW DataDir}/filename
			// {FW DataDir}/Media/filename
			// give up and store relative path Pictures/filename (even though it doesn't exist)
			var sPath = Path.Combine(Path.GetDirectoryName(m_sLiftFile), "audio", sFile);
			sPath = CopyFileToLinkedFiles(sFile, sPath, LcmFileHelper.ksMediaDir);
			if (!File.Exists(sPath) && !string.IsNullOrEmpty(m_cache.LangProject.LinkedFilesRootDir))
			{
				sPath = Path.Combine(m_cache.LangProject.LinkedFilesRootDir, sFile);
				if (!File.Exists(sPath) && !string.IsNullOrEmpty(m_cache.LangProject.LinkedFilesRootDir))
				{
					sPath = Path.Combine(m_cache.LangProject.LinkedFilesRootDir, "Media", sFile);
					if (!File.Exists(sPath))
					{
						sPath = Path.Combine(FwDirectoryFinder.DataDirectory, sFile);
						if (!File.Exists(sPath))
						{
							sPath = Path.Combine(FwDirectoryFinder.DataDirectory, "Media", sFile);
						}
					}
				}
			}
			return sPath;
		}

		private void MergePronunciationMedia(ILexPronunciation pron, CmLiftPhonetic phon)
		{
			AddNewWsToBothVernAnal();
			foreach (var uref in phon.Media)
			{
				var sFile = uref.Url;
				// TODO-Linux: this looks suspicious
				sFile = sFile.Replace('/', '\\');
				int ws;
				string sLabel; // safe-XML
				if (uref.Label != null && !uref.Label.IsEmpty)
				{
					ws = GetWsFromLiftLang(uref.Label.FirstValue.Key);
					sLabel = uref.Label.FirstValue.Value.Text;
				}
				else
				{
					ws = m_cache.DefaultVernWs;
					sLabel = null;
				}
				var media = FindMatchingMedia(pron.MediaFilesOS, sFile, uref.Label);
				if (media == null)
				{
					media = CreateNewCmMedia();
					pron.MediaFilesOS.Add(media);
					var sPath = CopyPronunciationFile(sFile);
					try
					{
						if (!string.IsNullOrEmpty(sLabel))
						{
							media.Label.set_String(ws, XmlUtils.DecodeXmlAttribute(sLabel));
						}
						if (!string.IsNullOrEmpty(sPath))
						{
							ICmFolder cmfMedia = null;
							foreach (var cmf in m_cache.LangProject.MediaOC)
							{
								for (var i = 0; i < cmf.Name.StringCount; ++i)
								{
									int wsT;
									var tss = cmf.Name.GetStringFromIndex(i, out wsT);
									if (tss.Text == CmFolderTags.LocalMedia)
									{
										cmfMedia = cmf;
										break;
									}
								}

								if (cmfMedia != null)
								{
									break;
								}
							}
							if (cmfMedia == null)
							{
								var factFolder = m_cache.ServiceLocator.GetInstance<ICmFolderFactory>();
								cmfMedia = factFolder.Create();
								m_cache.LangProject.MediaOC.Add(cmfMedia);
								cmfMedia.Name.UserDefaultWritingSystem = TsStringUtils.MakeString(CmFolderTags.LocalMedia, m_cache.DefaultUserWs);
							}
							var file = cmfMedia.FilesOC.FirstOrDefault(cf => cf.AbsoluteInternalPath == sPath);
							if (file == null)
							{
								var factFile = m_cache.ServiceLocator.GetInstance<ICmFileFactory>();
								file = factFile.Create();
								cmfMedia.FilesOC.Add(file);
								file.InternalPath = sPath;
							}
							media.MediaFileRA = file;
						}
					}
					catch (ArgumentException ex)
					{
						// If sFile is empty, trying to create the CmFile for the audio/media file will throw.
						// We don't care about this error as the caption will still be set properly.
						Debug.WriteLine("Error initializing media: " + ex.Message);
					}
					if (!File.Exists(sPath))
					{
						media.MediaFileRA.InternalPath = $"Media{Path.DirectorySeparatorChar}{sFile}";
					}
				}
				else
				{
					// When doing Send/Receive we should copy existing media in case they changed.
					if (m_msImport == MergeStyle.MsKeepOnlyNew)
					{
						CopyPronunciationFile(sFile);
					}
				}
				AddNewWsToBothVernAnal();
				MergeInMultiString(media.Label, CmMediaTags.kflidLabel, uref.Label, media.Guid);
			}
		}

		/// <summary />
		private ICmMedia FindMatchingMedia(ILcmOwningSequence<ICmMedia> rgmedia, string sFile,
			LiftMultiText lmtLabel)
		{
			ICmMedia mediaMatching = null;
			var cMatches = 0;
			AddNewWsToBothVernAnal();
			foreach (var media in rgmedia)
			{
				if (media.MediaFileRA == null)
				{
					continue;	// should NEVER happen!
				}
				if (media.MediaFileRA.InternalPath == sFile || Path.GetFileName(media.MediaFileRA.InternalPath) == sFile)
				{
					var cCurrent = MultiTsStringMatches(media.Label, lmtLabel);
					if (cCurrent >= cMatches)
					{
						mediaMatching = media;
						cMatches = cCurrent;
					}
				}
			}
			return mediaMatching;

		}

		/// <summary>
		/// Find the best matching pronunciation in the lex entry (if one exists) for the imported LiftPhonetic phon.
		/// If neither has any form, then only the media filenames are compared.  If both have forms, then both forms
		/// and media filenames are compared.  At least one form must match if any forms exist on either side.
		/// If either has a media file, both must have the same number of media files, and at least one filename
		/// must match.
		/// As a side-effect, dictHvoPhon has the matching hvo keyed to the imported data (if one exists).
		/// </summary>
		/// <returns>best match, or null</returns>
		private ILexPronunciation FindMatchingPronunciation(ILexEntry le, Dictionary<int, CmLiftPhonetic> dictHvoPhon, CmLiftPhonetic phon)
		{
			ILexPronunciation lexpron = null;
			ILexPronunciation lexpronNoMedia = null;
			var cMatches = 0;
			foreach (var pron in le.PronunciationsOS)
			{
				if (dictHvoPhon.ContainsKey(pron.Hvo))
				{
					continue;
				}
				var fFormMatches = false;
				var cCurrent = 0;
				IgnoreNewWs();
				if (phon.Form.Count == 0)
				{
					var forms = GetAllUnicodeAlternatives(pron.Form);
					fFormMatches = (forms.Count == 0);
				}
				else
				{
					cCurrent = MultiUnicodeStringMatches(pron.Form, phon.Form, false, Guid.Empty, 0);
					fFormMatches = (cCurrent > cMatches);
				}
				if (fFormMatches)
				{
					cMatches = cCurrent;
					if (phon.Media.Count == pron.MediaFilesOS.Count)
					{
						var cFilesMatch = 0;
						for (var i = 0; i < phon.Media.Count; ++i)
						{
							var sURL = phon.Media[i].Url;
							if (string.IsNullOrWhiteSpace(sURL))
							{
								continue;
							}
							var sFile = Path.GetFileName(sURL);
							cFilesMatch += pron.MediaFilesOS.Select(mediaFile => pron.MediaFilesOS[i].MediaFileRA).Where(cf => cf != null).Select(cf => cf.InternalPath).Where(sPath => sPath != null).Count(sPath => sFile.ToLowerInvariant() == Path.GetFileName(sPath).ToLowerInvariant());
						}

						if (phon.Media.Count == 0 || cFilesMatch > 0)
						{
							lexpron = pron;
						}
						else
						{
							lexpronNoMedia = pron;
						}
					}
					else
					{
						lexpronNoMedia = pron;
					}
				}
			}
			if (lexpron != null)
			{
				dictHvoPhon.Add(lexpron.Hvo, phon);
				return lexpron;
			}
			if (lexpronNoMedia != null)
			{
				dictHvoPhon.Add(lexpronNoMedia.Hvo, phon);
				return lexpronNoMedia;
			}
			return null;
		}

		private Dictionary<int, string> GetAllUnicodeAlternatives(ITsMultiString tsm)
		{
			var dict = new Dictionary<int, string>();
			for (var i = 0; i < tsm.StringCount; ++i)
			{
				int ws;
				var tss = tsm.GetStringFromIndex(i, out ws);
				if (tss.Text != null && ws != 0)
				{
					dict.Add(ws, tss.Text);
				}
			}
			return dict;
		}

		private Dictionary<int, ITsString> GetAllTsStringAlternatives(ITsMultiString tsm)
		{
			var dict = new Dictionary<int, ITsString>();
			for (var i = 0; i < tsm.StringCount; ++i)
			{
				int ws;
				var tss = tsm.GetStringFromIndex(i, out ws);
				if (tss.Text != null && ws != 0)
				{
					dict.Add(ws, tss);
				}
			}
			return dict;
		}

		private void ProcessPronunciationFieldsAndTraits(ILexPronunciation pron, CmLiftPhonetic phon)
		{
			foreach (var field in phon.Fields)
			{
				switch (field.Type.ToLowerInvariant())
				{
					case "cvpattern":
					case "cv-pattern":
						pron.CVPattern = StoreTsStringValue(m_fCreatingNewEntry, pron.CVPattern, field.Content);
						break;
					case "tone":
						pron.Tone = StoreTsStringValue(m_fCreatingNewEntry, pron.Tone, field.Content);
						break;
					default:
						ProcessUnknownField(pron, phon, field, "LexPronunciation", "custom-pronunciation-", LexPronunciationTags.kClassId);
						break;
				}
			}
			foreach (LiftTrait trait in phon.Traits)
			{
				switch (trait.Name.ToLowerInvariant())
				{
					case RangeNames.sLocationsOA:
						var loc = FindOrCreateLocation(trait.Value);
						if (pron.LocationRA != loc && (m_fCreatingNewEntry || m_msImport != MergeStyle.MsKeepOld || pron.LocationRA == null))
						{
							pron.LocationRA = loc;
						}
						break;
					default:
						StoreTraitAsResidue(pron, trait);
						break;
				}
			}
		}

		private void CreateEntryEtymologies(ILexEntry le, CmLiftEntry entry)
		{
			foreach (var liftEtymology in entry.Etymologies)
			{
				AddNewWsToVernacular();
				var ety = CreateNewLexEtymology();
				le.EtymologyOS.Add(ety);
				MergeInMultiUnicode(ety.Form, LexEtymologyTags.kflidForm, liftEtymology.Form, ety.Guid);
				AddNewWsToAnalysis();
				MergeInMultiUnicode(ety.Gloss, LexEtymologyTags.kflidGloss, liftEtymology.Gloss, ety.Guid);
				ProcessEtymologyFieldsAndTraits(ety, liftEtymology);
				StoreAnnotationsAndDatesInResidue(ety, liftEtymology);
			}
		}

		private void MergeEntryEtymologies(ILexEntry le, CmLiftEntry entry)
		{
			var dictHvoLiftEtym = new Dictionary<int, CmLiftEtymology>();
			foreach (var liftEtym in entry.Etymologies)
			{
				AddNewWsToVernacular();
				var etym = FindMatchingEtymology(le, dictHvoLiftEtym, liftEtym);
				if (etym == null)
				{
					etym = CreateNewLexEtymology();
					le.EtymologyOS.Add(etym);
					dictHvoLiftEtym.Add(etym.Hvo, liftEtym);
				}
				MergeInMultiUnicode(etym.Form, LexEtymologyTags.kflidForm, liftEtym.Form, etym.Guid);
				AddNewWsToAnalysis();
				MergeInMultiUnicode(etym.Gloss, LexEtymologyTags.kflidGloss, liftEtym.Gloss, etym.Guid);
				// See LT-11765 for issues here.
				if (liftEtym.Source != null && liftEtym.Source != "UNKNOWN")
				{
					etym.LiftResidue = liftEtym.Source; // Source is no longer part of the model
				}
				ProcessEtymologyFieldsAndTraits(etym, liftEtym);
				StoreDatesInResidue(etym, liftEtym);
			}
		}

		/// <summary>
		/// Find the best matching etymology in the lex entry (if one exists) for the imported LiftEtymology let.
		/// At least one form must match if any forms exist on either side.
		/// As a side-effect, dictHvoLiftEtym has the matching hvo keyed to the imported data (if one exists).
		/// </summary>
		/// <returns>best match, or null</returns>
		private ILexEtymology FindMatchingEtymology(ILexEntry le, Dictionary<int, CmLiftEtymology> dictHvoLiftEtym, CmLiftEtymology let)
		{
			ILexEtymology lexEtym = null;
			var cMatches = 0;
			foreach (var etym in le.EtymologyOS)
			{
				if (dictHvoLiftEtym.ContainsKey(etym.Hvo))
				{
					continue;
				}
				bool fFormMatches;
				var cCurrent = 0;
				AddNewWsToVernacular();
				if (let.Form.Count == 0)
				{
					var forms = GetAllUnicodeAlternatives(etym.Form);
					fFormMatches = forms.Count == 0;
				}
				else
				{
					cCurrent = MultiUnicodeStringMatches(etym.Form, let.Form, false, Guid.Empty, 0);
					fFormMatches = cCurrent > cMatches;
				}
				if (fFormMatches)
				{
					cMatches = cCurrent;
					lexEtym = etym;
				}
			}
			if (lexEtym == null)
			{
				return null;
			}
			dictHvoLiftEtym.Add(lexEtym.Hvo, let);
			return lexEtym;
		}

		private bool EntryEtymologiesConflict(ILexEntry le, List<CmLiftEtymology> list)
		{
			if (le.EtymologyOS.Count == 0 || list.Count == 0)
			{
				return false;
			}
			var cCommon = 0;
			var dictHvoEtymology = new Dictionary<int, CmLiftEtymology>();
			foreach (var lety in list)
			{
				AddNewWsToVernacular();
				var etym = FindMatchingEtymology(le, dictHvoEtymology, lety);
				if (etym != null)
				{
					if (MultiUnicodeStringsConflict(etym.Form, lety.Form, false, Guid.Empty, 0))
					{
						m_cdConflict = new ConflictingEntry($"Form ({TsStringAsHtml(etym.Form.BestVernacularAlternative, m_cache)})", le, this);
						return true;
					}
					AddNewWsToAnalysis();
					if (MultiUnicodeStringsConflict(etym.Gloss, lety.Gloss, false, Guid.Empty, 0))
					{
						m_cdConflict = new ConflictingEntry($"Gloss ({TsStringAsHtml(etym.Form.BestAnalysisAlternative, m_cache)})", le, this);
						return true;
					}
					if (EtymologyFieldsConflict(etym, lety.Fields))
					{
						return true;
					}
					IgnoreNewWs();
					if (StringsConflict(etym.LiftResidue, lety.Source))
					{
						return true;
					}
					++cCommon;
				}
			}
			if (cCommon < Math.Min(le.EtymologyOS.Count, list.Count))
			{
				m_cdConflict = new ConflictingEntry("Etymologies", le, this);
				return true;
			}
			return false;
		}

		private void ProcessEtymologyFieldsAndTraits(ILexEtymology ety, CmLiftEtymology let)
		{
			foreach (var field in let.Fields)
			{
				AddNewWsToAnalysis();
				switch (field.Type.ToLowerInvariant())
				{
					case "comment":
						MergeInMultiString(ety.Comment, LexEtymologyTags.kflidComment, field.Content, ety.Guid);
						break;
					case "languagenotes":
						MergeInMultiString(ety.LanguageNotes, LexEtymologyTags.kflidLanguageNotes, field.Content, ety.Guid);
						break;
					case "preccomment":
						MergeInMultiString(ety.PrecComment, LexEtymologyTags.kflidPrecComment, field.Content, ety.Guid);
						break;
					case "note":
						MergeInMultiString(ety.Note, LexEtymologyTags.kflidNote, field.Content, ety.Guid);
						break;
					case "bibliography":
						MergeInMultiString(ety.Bibliography, LexEtymologyTags.kflidBibliography, field.Content, ety.Guid);
						break;
					default:
						ProcessUnknownField(ety, let, field,
							"LexEtymology", "custom-etymology-", LexEtymologyTags.kClassId);
						break;
				}
			}
			foreach (var trait in let.Traits)
			{
				switch (trait.Name.ToLowerInvariant())
				{
					case RangeNames.sDbLanguagesOA:
						var lang = FindOrCreateLanguagePossibility(trait.Value);
						if (!ety.LanguageRS.Any(l => l == lang) && (m_fCreatingNewEntry || m_msImport != MergeStyle.MsKeepOld || !ety.LanguageRS.Any()))
						{
							ety.LanguageRS.Add(lang);
						}
						break;
					default:
						StoreTraitAsResidue(ety, trait);
						break;
				}
			}
		}

		private bool EtymologyFieldsConflict(ILexEtymology lexety, List<LiftField> list)
		{
			if (lexety?.Comment == null)
			{
				return false;
			}
			AddNewWsToAnalysis();
			foreach (var field in list)
			{
				switch (field.Type.ToLowerInvariant())
				{
					case "comment":
						if (MultiTsStringsConflict(lexety.Comment, field.Content))
						{
							return true;
						}
						break;
					case "languagenotes":
						if (MultiTsStringsConflict(lexety.LanguageNotes, field.Content))
						{
							return true;
						}
						break;
					case "preccomment":
						if (MultiTsStringsConflict(lexety.PrecComment, field.Content))
						{
							return true;
						}
						break;
					case "note":
						if (MultiTsStringsConflict(lexety.Note, field.Content))
						{
							return true;
						}
						break;
					case "bibliography":
						if (MultiTsStringsConflict(lexety.Bibliography, field.Content))
						{
							return true;
						}
						break;
				}
			}
			return false;
		}

		private void ProcessEntryRelations(ILexEntry le, CmLiftEntry entry)
		{
			// Due to possible forward references, wait until the end to process relations.
			foreach (var rel in entry.Relations)
			{
				if (rel.Type != "_component-lexeme" && String.IsNullOrEmpty(rel.Ref))
				{
					var xdResidue = FindOrCreateResidue(le);
					InsertResidueContent(xdResidue, CreateXmlForRelation(rel));
				}
				else
				{
					switch (rel.Type)
					{
						case "minorentry":
						case "subentry":
							// We'll just ignore these backreferences.
							break;
						case "main":
						case "_component-lexeme":
						case "BaseForm": // special case for better WeSay compability, LT-10970
							var pend = new PendingLexEntryRef(le, rel, entry);
							CreateRelationResidue(rel);
							m_rgPendingLexEntryRefs.Add(pend);
							break;
						default:
							var sResidue = CreateRelationResidue(rel);
							m_rgPendingRelation.Add(new PendingRelation(le, rel, sResidue));
							break;
					}
				}
			}
		}

		private void CreateEntrySense(ILexEntry le, CmLiftSense sense)
		{
			try
			{
				m_fCreatingNewSense = true;
				bool fNeedNewId;
				var ls = CreateNewLexSense(sense.Guid, le, out fNeedNewId);
				FillInNewSense(ls, sense, fNeedNewId);
			}
			finally
			{
				m_fCreatingNewSense = false;
			}
		}

		private void CreateSubsense(ILexSense ls, CmLiftSense sub)
		{
			var fSavedCreatingNew = m_fCreatingNewSense;
			try
			{
				m_fCreatingNewSense = true;
				bool fNeedNewId;
				var lsSub = CreateNewLexSense(sub.Guid, ls, out fNeedNewId);
				FillInNewSense(lsSub, sub, fNeedNewId);
			}
			finally
			{
				m_fCreatingNewSense = fSavedCreatingNew;
			}
		}

		private void FillInNewSense(ILexSense ls, CmLiftSense sense, bool fNeedNewId)
		{
			if (m_cdConflict is ConflictingSense)
			{
				((ConflictingSense)m_cdConflict).DupSense = ls;
				m_rgcdConflicts.Add(m_cdConflict);
				m_cdConflict = null;
			}
			//sense.Order;
			StoreSenseId(ls, sense.Id);
			AddNewWsToAnalysis();
			MergeInMultiUnicode(ls.Gloss, LexSenseTags.kflidGloss, sense.Gloss, ls.Guid);
			MergeInMultiString(ls.Definition, LexSenseTags.kflidDefinition, sense.Definition, ls.Guid);
			if (fNeedNewId)
			{
				var xd = FindOrCreateResidue(ls, sense.Id, LexSenseTags.kflidLiftResidue);
				var xa = xd.FirstChild.Attributes["id"];
				if (xa == null)
				{
					xa = xd.CreateAttribute("id");
					xd.FirstChild.Attributes.Append(xa);
				}
				xa.Value = ls.LIFTid;
			}
			CreateSenseExamples(ls, sense);
			ProcessSenseGramInfo(ls, sense);
			CreateSenseIllustrations(ls, sense);
			ProcessSenseRelations(ls, sense);
			ProcessSenseReversals(ls, sense);
			ProcessSenseNotes(ls, sense);
			ProcessSenseFields(ls, sense);
			ProcessSenseTraits(ls, sense);
			foreach (var sub in sense.Subsenses)
			{
				CreateSubsense(ls, sub);
			}
			StoreAnnotationsAndDatesInResidue(ls, sense);
			++m_cSensesAdded;
		}

		private void MergeIntoExistingSense(ILexSense ls, CmLiftSense sense)
		{
			//sense.Order;
			StoreSenseId(ls, sense.Id);
			AddNewWsToAnalysis();
			MergeInMultiUnicode(ls.Gloss, LexSenseTags.kflidGloss, sense.Gloss, ls.Guid);
			MergeInMultiString(ls.Definition, LexSenseTags.kflidDefinition, sense.Definition, ls.Guid);
			MergeSenseExamples(ls, sense);
			ProcessSenseGramInfo(ls, sense);
			MergeSenseIllustrations(ls, sense);
			ProcessSenseRelations(ls, sense);
			ProcessSenseReversals(ls, sense);
			ProcessSenseNotes(ls, sense);
			ProcessSenseFields(ls, sense);
			ProcessSenseTraits(ls, sense);

			var map = new Dictionary<CmLiftSense, ILexSense>();
			var setUsed = new HashSet<int>();
			foreach (var sub in sense.Subsenses)
			{
				var lsSub = FindExistingSense(ls.SensesOS, sub);
				map.Add(sub, lsSub);
				if (lsSub != null)
				{
					setUsed.Add(lsSub.Hvo);
				}
			}
			// If we're keeping only the imported data, delete any unused subsense.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				foreach (var hvo in ls.SensesOS.ToHvoArray())
				{
					if (!setUsed.Contains(hvo))
					{
						m_deletedObjects.Add(hvo);
					}
				}
			}
			foreach (var sub in sense.Subsenses)
			{
				ILexSense lsSub;
				map.TryGetValue(sub, out lsSub);
				if (lsSub == null || (m_msImport == MergeStyle.MsKeepBoth && SenseHasConflictingData(lsSub, sub)))
				{
					CreateSubsense(ls, sub);
				}
				else
				{
					MergeIntoExistingSense(lsSub, sub);
				}
			}
			StoreAnnotationsAndDatesInResidue(ls, sense);
		}

		private void StoreSenseId(ILexSense ls, string sId)
		{
			if (!string.IsNullOrEmpty(sId))
			{
				FindOrCreateResidue(ls, sId, LexSenseTags.kflidLiftResidue);
				MapIdToObject(sId, ls);
			}
		}

		private void CreateSenseExamples(ILexSense ls, CmLiftSense sense)
		{
			foreach (var expl in sense.Examples)
			{
				var les = CreateNewLexExampleSentence(expl.Guid, ls);
				if (!string.IsNullOrEmpty(expl.Id))
				{
					MapIdToObject(expl.Id, les);
				}
				AddNewWsToVernacular();
				MergeInMultiString(les.Example, LexExampleSentenceTags.kflidExample, expl.Content, les.Guid);
				CreateExampleTranslations(les, expl);
				ProcessExampleNotes(les, expl);
				if (TsStringIsNullOrEmpty(les.Reference) && !String.IsNullOrEmpty(expl.Source))
				{
					les.Reference = TsStringUtils.MakeString(expl.Source, m_cache.DefaultAnalWs);
				}
				ProcessExampleFields(les, expl);
				ProcessExampleTraits(les, expl);
				StoreExampleResidue(les, expl);
			}
		}

		private void MergeSenseExamples(ILexSense ls, CmLiftSense sense)
		{
			var map = new Dictionary<CmLiftExample, ILexExampleSentence>();
			var setUsed = new HashSet<int>();
			foreach (var expl in sense.Examples)
			{
				var les = FindingMatchingExampleSentence(ls, expl);
				map.Add(expl, les);
				if (les != null)
				{
					setUsed.Add(les.Hvo);
				}
			}
			// If we're keeping only the imported data, delete any unused example.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				foreach (var hvo in ls.ExamplesOS.ToHvoArray())
				{
					if (!setUsed.Contains(hvo))
					{
						m_deletedObjects.Add(hvo);
					}
				}
			}
			foreach (CmLiftExample expl in sense.Examples)
			{
				ILexExampleSentence les;
				map.TryGetValue(expl, out les);
				if (les == null)
				{
					les = CreateNewLexExampleSentence(expl.Guid, ls);
				}

				if (!string.IsNullOrEmpty(expl.Id))
				{
					MapIdToObject(expl.Id, les);
				}
				AddNewWsToVernacular();
				MergeInMultiString(les.Example, LexExampleSentenceTags.kflidExample, expl.Content, les.Guid);
				MergeExampleTranslations(les, expl);
				ProcessExampleNotes(les, expl);
				ProcessExampleFields(les, expl);
				ProcessExampleTraits(les, expl);
				if (TsStringIsNullOrEmpty(les.Reference) && !String.IsNullOrEmpty(expl.Source))
				{
					les.Reference = TsStringUtils.MakeString(expl.Source, m_cache.DefaultAnalWs);
				}
			}
		}

		private ILexExampleSentence FindingMatchingExampleSentence(ILexSense ls, CmLiftExample expl)
		{
			ILexExampleSentence les = null;
			if (expl.Guid != Guid.Empty)
			{
				var cmo = GetObjectForGuid(expl.Guid);
				if (cmo is ILexExampleSentence)
				{
					les = cmo as ILexExampleSentence;
					if (les.Owner != ls)
					{
						les = null;
					}
				}
			}

			return les ?? FindExampleSentence(ls.ExamplesOS, expl);
		}

		private bool SenseExamplesConflict(ILexSense ls, List<CmLiftExample> list)
		{
			if (ls.ExamplesOS.Count == 0 || list.Count == 0)
			{
				return false;
			}
			foreach (var expl in list)
			{
				ILexExampleSentence les = null;
				if (expl.Guid != Guid.Empty)
				{
					var cmo = GetObjectForGuid(expl.Guid);
					if (cmo is ILexExampleSentence)
					{
						les = (ILexExampleSentence)cmo;
						if (les.Owner.Hvo != ls.Hvo)
						{
							les = null;
						}
					}
				}
				if (les == null)
				{
					les = FindExampleSentence(ls.ExamplesOS, expl);
				}
				if (les == null)
				{
					continue;
				}
				AddNewWsToVernacular();
				MergeInMultiString(les.Example, LexExampleSentenceTags.kflidExample, expl.Content, les.Guid);
				if (MultiTsStringsConflict(les.Example, expl.Content))
				{
					m_cdConflict = new ConflictingSense($"Example ({TsStringAsHtml(les.Example.BestVernacularAlternative, m_cache)})", ls, this);
					return true;
				}
				IgnoreNewWs();
				if (StringsConflict(les.Reference.Text, expl.Source))
				{
					m_cdConflict = new ConflictingSense($"Example ({TsStringAsHtml(les.Example.BestVernacularAlternative, m_cache)}) Reference", ls, this);
					return true;
				}
				AddNewWsToAnalysis();
				if (ExampleTranslationsConflict(les, expl))
				{
					m_cdConflict = new ConflictingSense($"Example ({TsStringAsHtml(les.Example.BestVernacularAlternative, m_cache)}) Translations", ls, this);
					return true;
				}
				if (ExampleNotesConflict(les, expl))
				{
					m_cdConflict = new ConflictingSense($"Example ({TsStringAsHtml(les.Example.BestVernacularAlternative, m_cache)}) Reference", ls, this);
					return true;
				}
			}
			return false;
		}

		private ILexExampleSentence FindExampleSentence(ILcmOwningSequence<ILexExampleSentence> rgexamples, CmLiftExample expl)
		{
			var matches = new List<ILexExampleSentence>();
			var cMatches = 0;
			AddNewWsToVernacular();
			foreach (var les in rgexamples)
			{
				var cCurrent = MultiTsStringMatches(les.Example, expl.Content);
				if (cCurrent > cMatches)
				{
					matches.Clear();
					matches.Add(les);
					cMatches = cCurrent;
				}
				else if (cCurrent == cMatches && cCurrent > 0)
				{
					matches.Add(les);
				}
				else if ((expl.Content == null || expl.Content.IsEmpty) && (les.Example == null || les.Example.BestVernacularAnalysisAlternative.Equals(les.Example.NotFoundTss)))
				{
					matches.Add(les);
				}
			}

			if (matches.Count == 0)
			{
				return null;
			}
			if (matches.Count == 1)
			{
				return matches[0];
			}
			// Okay, we have more than one example sentence that match equally well in Example.
			// So let's look at the other fields.
			ILexExampleSentence lesMatch = null;
			cMatches = 0;
			foreach (var les in matches)
			{
				IgnoreNewWs();
				var fSameReference = MatchingItemInNotes(les.Reference, "reference", expl.Notes);
				AddNewWsToAnalysis();
				var cCurrent = TranslationsMatch(les.TranslationsOC, expl.Translations);
				if (fSameReference && cCurrent == expl.Translations.Count && cCurrent == les.TranslationsOC.Count)
				{
					return les;
				}
				if (cCurrent > cMatches)
				{
					lesMatch = les;
					cMatches = cCurrent;
				}
				else if (fSameReference)
				{
					lesMatch = les;
				}
			}
			return lesMatch;
		}

		private bool MatchingItemInNotes(ITsString tss, string sType, List<CmLiftNote> rgnotes)
		{
			var sItem = tss.Text;
			// Review: Should we match on the writing system inside the tss as well?
			var fTypeFound = false;
			foreach (var note in rgnotes)
			{
				if (note.Type == sType)
				{
					fTypeFound = true;
					foreach (string sWs in note.Content.Keys)
					{
						if (sItem == note.Content[sWs].Text)
						{
							return true;
						}
					}
				}
			}
			return string.IsNullOrEmpty(sItem) && !fTypeFound;
		}

		private int TranslationsMatch(ILcmOwningCollection<ICmTranslation> oldList, List<LiftTranslation> newList)
		{
			if (oldList.Count == 0 || newList.Count == 0)
			{
				return 0;
			}
			var cMatches = 0;
			foreach (var tran in newList)
			{
				var ct = FindExampleTranslation(oldList, tran);
				if (ct != null)
				{
					++cMatches;
				}
			}
			return cMatches;
		}

		private void CreateExampleTranslations(ILexExampleSentence les, CmLiftExample expl)
		{
			AddNewWsToAnalysis();
			foreach (var tran in expl.Translations)
			{
				ICmPossibility type = null;
				if (!string.IsNullOrEmpty(tran.Type))
				{
					type = FindOrCreateTranslationType(tran.Type);
				}
				var ct = CreateNewCmTranslation(les, type);
				les.TranslationsOC.Add(ct);
				MergeInMultiString(ct.Translation, CmTranslationTags.kflidTranslation, tran.Content, ct.Guid);
			}
		}

		private void MergeExampleTranslations(ILexExampleSentence les, CmLiftExample expl)
		{
			var map = new Dictionary<LiftTranslation, ICmTranslation>();
			var setUsed = new HashSet<int>();
			AddNewWsToAnalysis();
			foreach (var tran in expl.Translations)
			{
				var ct = FindExampleTranslation(les.TranslationsOC, tran);
				map.Add(tran, ct);
				if (ct != null)
				{
					setUsed.Add(ct.Hvo);
				}
			}
			// If we're keeping only the imported data, erase any unused existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				foreach (var hvo in les.TranslationsOC.ToHvoArray())
				{
					if (!setUsed.Contains(hvo))
					{
						m_deletedObjects.Add(hvo);
					}
				}
			}
			foreach (var tran in expl.Translations)
			{
				ICmTranslation ct;
				map.TryGetValue(tran, out ct);
				ICmPossibility type = null;
				if (!string.IsNullOrEmpty(tran.Type))
				{
					type = FindOrCreateTranslationType(tran.Type);
				}
				if (ct == null)
				{
					ct = CreateNewCmTranslation(les, type);
					les.TranslationsOC.Add(ct);
				}
				MergeInMultiString(ct.Translation, CmTranslationTags.kflidTranslation, tran.Content, ct.Guid);
				if (type != null && ct.TypeRA != type && (m_fCreatingNewSense || m_msImport != MergeStyle.MsKeepOld))
				{
					ct.TypeRA = type;
				}
			}
		}

		private bool ExampleTranslationsConflict(ILexExampleSentence les, CmLiftExample expl)
		{
			if (les.TranslationsOC.Count == 0 || expl.Translations.Count == 0)
			{
				return false;
			}
			AddNewWsToAnalysis();
			foreach (var tran in expl.Translations)
			{
				var ct = FindExampleTranslation(les.TranslationsOC, tran);
				if (ct == null)
				{
					continue;
				}
				if (MultiTsStringsConflict(ct.Translation, tran.Content))
				{
					return true;
				}
				if (!string.IsNullOrEmpty(tran.Type))
				{
					var type = FindOrCreateTranslationType(tran.Type);
					if (ct.TypeRA != type && ct.TypeRA != null)
					{
						return true;
					}
				}
			}
			return false;
		}

		private ICmTranslation FindExampleTranslation(ILcmOwningCollection<ICmTranslation> rgtranslations, LiftTranslation tran)
		{
			ICmTranslation ctMatch = null;
			var cMatches = 0;
			AddNewWsToAnalysis();
			foreach (var ct in rgtranslations)
			{
				var cCurrent = MultiTsStringMatches(ct.Translation, tran.Content);
				if (cCurrent > cMatches)
				{
					ctMatch = ct;
					cMatches = cCurrent;
				}
				else if ((tran.Content == null || tran.Content.IsEmpty) && (ct.Translation == null || ct.Translation.BestAnalysisVernacularAlternative.Equals(ct.Translation.NotFoundTss)))
				{
					return ct;
				}
			}
			return ctMatch;
		}

		private void ProcessExampleNotes(ILexExampleSentence les, CmLiftExample expl)
		{
			foreach (var note in expl.Notes)
			{
				if (note.Type == null)
				{
					note.Type = string.Empty;
				}
				switch (note.Type.ToLowerInvariant())
				{
					case "reference":
						les.Reference = StoreTsStringValue(m_fCreatingNewSense, les.Reference, note.Content);
						break;
					default:
						StoreNoteAsResidue(les, note);
						break;
				}
			}
		}

		private bool ExampleNotesConflict(ILexExampleSentence les, CmLiftExample expl)
		{
			if (expl.Notes.Count == 0)
			{
				return false;
			}
			IgnoreNewWs();
			foreach (var note in expl.Notes)
			{
				if (note.Type == null)
				{
					note.Type = string.Empty;
				}
				switch (note.Type.ToLowerInvariant())
				{
					case "reference":
						if (StringsConflict(les.Reference, GetFirstLiftTsString(note.Content)))
						{
							return true;
						}
						break;
				}
			}
			return false;
		}

		private void ProcessExampleFields(ILexExampleSentence les, CmLiftExample expl)
		{
			// Note: when/if LexExampleSentence.Reference is written as a <field>
			// instead of a <note>, the next loop will presumably be changed.
			foreach (var field in expl.Fields)
			{
				switch (field.Type.ToLowerInvariant())
				{
					default:
						ProcessUnknownField(les, expl, field, "LexExampleSentence", "custom-example-", LexExampleSentenceTags.kClassId);
						break;
				}
			}
		}

		private void ProcessExampleTraits(ILexExampleSentence lexExmp, CmLiftExample example)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				lexExmp.DoNotPublishInRC.Clear();
			}
			foreach (var lt in example.Traits)
			{
				switch (lt.Name.ToLowerInvariant())
				{
					case RangeNames.sDbPublicationTypesOAold:
					case RangeNames.sDbPublicationTypesOA:
						var poss = FindOrCreatePublicationType(lt.Value);
						if (!lexExmp.DoNotPublishInRC.Contains(poss))
						{
							lexExmp.DoNotPublishInRC.Add(poss);
						}
						break;
					default:
						ProcessUnknownTrait(example, lt, lexExmp);
						break;
				}
			}
		}

		private void ProcessSenseGramInfo(ILexSense ls, CmLiftSense sense)
		{
			if (sense.GramInfo == null)
			{
				return;
			}
			if (!m_fCreatingNewSense && m_msImport == MergeStyle.MsKeepOld && ls.MorphoSyntaxAnalysisRA != null)
			{
				return;
			}
			var gram = sense.GramInfo;
			var sTraitPos = gram.Value;
			IPartOfSpeech pos = null;
			if (!string.IsNullOrEmpty(sTraitPos))
			{
				pos = FindOrCreatePartOfSpeech(sTraitPos);
			}
			ls.MorphoSyntaxAnalysisRA = FindOrCreateMSA(ls.Entry, pos, gram.Traits);
		}

		/// <summary>
		/// Creating individual MSAs for every sense, and then merging identical MSAs at the
		/// end is expensive: deleting each redundant MSA takes ~360 msec, which can add up
		/// quickly even for only a few hundred duplications created here.  (See LT-9006.)
		/// </summary>
		private IMoMorphSynAnalysis FindOrCreateMSA(ILexEntry le, IPartOfSpeech pos, List<LiftTrait> traits)
		{
			string sType = null;
			string sFromPOS = null;
			var dictPosSlots = new Dictionary<string, List<string>>();
			var dictPosInflClasses = new Dictionary<string, List<string>>();
			var dictPosFromInflClasses = new Dictionary<string, List<string>>();
			var rgpossProdRestrict = new List<ICmPossibility>();
			var rgpossFromProdRestrict = new List<ICmPossibility>();
			string sInflectionFeature = null;
			string sFromInflFeature = null;
			string sFromStemName = null;
			var rgsResidue = new List<string>();
			foreach (var trait in traits)
			{
				if (trait.Name == "type")
				{
					sType = trait.Value;
				}
				else if (trait.Name == RangeNames.sPartsOfSpeechOAold1 || trait.Name == RangeNames.sPartsOfSpeechOAold2)
				{
					sFromPOS = trait.Value;
				}
				else if (trait.Name == RangeNames.sProdRestrictOA)
				{
					var poss = FindOrCreateExceptionFeature(trait.Value);
					rgpossProdRestrict.Add(poss);
				}
				else if (trait.Name == RangeNames.sProdRestrictOAfrom)
				{
					var poss = FindOrCreateExceptionFeature(trait.Value);
					rgpossFromProdRestrict.Add(poss);
				}
				else if (trait.Name == RangeNames.sMSAinflectionFeature)
				{
					sInflectionFeature = trait.Value;
				}
				else if (trait.Name == RangeNames.sMSAfromInflectionFeature)
				{
					sFromInflFeature = trait.Value;
				}
				else if (trait.Name == "from-stem-name")
				{
					sFromStemName = trait.Value;
				}
				else if (trait.Name.EndsWith("-slot") || trait.Name.EndsWith("-Slots"))
				{
					var len = trait.Name.Length - (trait.Name.EndsWith("-slot") ? 5 : 6);
					var sPos = trait.Name.Substring(0, len);
					List<string> rgsSlots;
					if (!dictPosSlots.TryGetValue(sPos, out rgsSlots))
					{
						rgsSlots = new List<string>();
						dictPosSlots.Add(sPos, rgsSlots);
					}
					rgsSlots.Add(trait.Value);
				}
				else if (trait.Name.EndsWith("-infl-class") || trait.Name.EndsWith("-InflectionClass"))
				{
					var len = trait.Name.Length - (trait.Name.EndsWith("-infl-class") ? 11 : 16);
					var sPos = trait.Name.Substring(0, len);
					if (sPos.StartsWith("from-"))
					{
						sPos = sPos.Substring(5);
						Debug.Assert(sPos.Length > 0);
						List<string> rgsInflClasses;
						if (!dictPosFromInflClasses.TryGetValue(sPos, out rgsInflClasses))
						{
							rgsInflClasses = new List<string>();
							dictPosFromInflClasses.Add(sPos, rgsInflClasses);
						}
						rgsInflClasses.Add(trait.Value);
					}
					else
					{
						List<string> rgsInflClasses;
						if (!dictPosInflClasses.TryGetValue(sPos, out rgsInflClasses))
						{
							rgsInflClasses = new List<string>();
							dictPosInflClasses.Add(sPos, rgsInflClasses);
						}
						rgsInflClasses.Add(trait.Value);
					}
				}
				else
				{
					rgsResidue.Add(CreateXmlForTrait(trait));
				}
			}
			IMoMorphSynAnalysis msaSense = null;
			bool fNew;
			switch (sType)
			{
				case "inflAffix":
					fNew = FindOrCreateInflAffixMSA(le, pos, dictPosSlots, dictPosInflClasses,
						rgpossProdRestrict, sInflectionFeature, rgsResidue, ref msaSense);
					break;
				case "derivAffix":
					fNew = FindOrCreateDerivAffixMSA(le, pos, sFromPOS, dictPosSlots,
						dictPosInflClasses, dictPosFromInflClasses,
						rgpossProdRestrict, rgpossFromProdRestrict,
						sInflectionFeature, sFromInflFeature, sFromStemName,
						rgsResidue, ref msaSense);
					break;
				case "derivStepAffix":
					fNew = FindOrCreateDerivStepAffixMSA(le, pos, dictPosSlots, dictPosInflClasses,
						rgsResidue, ref msaSense);
					break;
				case "affix":
					fNew = FindOrCreateUnclassifiedAffixMSA(le, pos, dictPosSlots, dictPosInflClasses,
						rgsResidue, ref msaSense);
					break;
				default:
					fNew = FindOrCreateStemMSA(le, pos, dictPosSlots, dictPosInflClasses,
						rgpossProdRestrict, sInflectionFeature, rgsResidue, ref msaSense);
					break;
			}
			if (fNew)
			{
				ProcessMsaSlotInformation(dictPosSlots, msaSense);
				ProcessMsaInflectionClassInfo(dictPosInflClasses, dictPosFromInflClasses, msaSense);
				StoreMsaExceptionFeatures(rgpossProdRestrict, rgpossFromProdRestrict, msaSense);
				if (!ParseFeatureString(sInflectionFeature, msaSense, false))
				{
					var flid = MoStemMsaTags.kflidMsFeatures;
					if (msaSense is IMoInflAffMsa)
					{
						flid = MoInflAffMsaTags.kflidInflFeats;
					}
					else if (msaSense is IMoDerivAffMsa)
					{
						flid = MoDerivAffMsaTags.kflidToMsFeatures;
					}
					LogInvalidFeatureString(le, sInflectionFeature, flid);
				}
				if (msaSense is IMoDerivAffMsa && !ParseFeatureString(sFromInflFeature, msaSense, true))
				{
					LogInvalidFeatureString(le, sFromInflFeature, MoDerivAffMsaTags.kflidFromMsFeatures);
				}

				if (!string.IsNullOrEmpty(sFromStemName))
				{
					ProcessMsaStemName(sFromStemName, sFromPOS, msaSense, rgsResidue);
				}
				StoreResidue(msaSense, rgsResidue);
			}
			return msaSense;
		}

		private void ProcessMsaStemName(string sFromStemName, string sFromPos, IMoMorphSynAnalysis msaSense, List<string> rgsResidue)
		{
			if (msaSense is IMoDerivAffMsa)
			{
				var key = $"{sFromPos}:{sFromStemName}";
				IMoStemName stem;
				if (m_dictStemName.TryGetValue(key, out stem))
				{
					(msaSense as IMoDerivAffMsa).FromStemNameRA = stem;
					return;
				}
				// TODO: Create new IMoStemName object?
			}
			rgsResidue.Add($"<trait name=\"from-stem-name\" value=\"{XmlUtils.MakeSafeXmlAttribute(sFromStemName)}\"/>");
		}

		private void LogInvalidFeatureString(ILexEntry le, string sInflectionFeature, int flid)
		{
			var bad = new InvalidData(LexTextControls.ksCannotParseFeature, le.Guid, flid, sInflectionFeature, 0, m_cache, this);
			if (!m_rgInvalidData.Contains(bad))
			{
				m_rgInvalidData.Add(bad);
			}
		}

		private ICmPossibility FindOrCreateExceptionFeature(string sValue)
		{
			ICmPossibility poss;
			if (!m_dictExceptFeats.TryGetValue(sValue, out poss))
			{
				EnsureProdRestrictListExists();
				if (m_factCmPossibility == null)
				{
					m_factCmPossibility = m_cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
				}
				poss = m_factCmPossibility.Create();
				m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS.Add(poss);
				var tss = TsStringUtils.MakeString(sValue, m_cache.DefaultAnalWs);
				poss.Name.AnalysisDefaultWritingSystem = tss;
				poss.Abbreviation.AnalysisDefaultWritingSystem = tss;
				m_rgnewExceptFeat.Add(poss);
				m_dictExceptFeats.Add(sValue, poss);
			}
			return poss;
		}

		/// <summary>
		/// Find or create an IMoStemMsa which matches the given values.
		/// </summary>
		/// <returns>true if the desired MSA is newly created, false if it already exists</returns>
		private bool FindOrCreateStemMSA(ILexEntry le, IPartOfSpeech pos,
			Dictionary<string, List<string>> dictPosSlots,
			Dictionary<string, List<string>> dictPosInflClasses,
			List<ICmPossibility> rgpossProdRestrict, string sInflectionFeature,
			List<string> rgsResidue,
			ref IMoMorphSynAnalysis msaSense)
		{
			foreach (var msa in le.MorphoSyntaxAnalysesOC)
			{
				var msaStem = msa as IMoStemMsa;
				if (msaStem != null &&
					msaStem.PartOfSpeechRA == pos &&
					MsaSlotInfoMatches(dictPosSlots, msaStem) &&
					MsaInflClassInfoMatches(dictPosInflClasses, null, msaStem) &&
					MsaExceptionFeatsMatch(rgpossProdRestrict, null, msaStem) &&
					MsaInflFeatureMatches(sInflectionFeature, null, msaStem))
					// Don't bother comparing residue -- it's not that important!
				{
					msaSense = msa;
					return false;
				}
			}
			msaSense = CreateNewMoStemMsa();
			le.MorphoSyntaxAnalysesOC.Add(msaSense);
			if (pos != null)
				((IMoStemMsa)msaSense).PartOfSpeechRA = pos;
			return true;
		}

		private void StoreMsaExceptionFeatures(List<ICmPossibility> rgpossProdRestrict, List<ICmPossibility> rgpossFromProdRestrict, IMoMorphSynAnalysis msaSense)
		{
			var msaStem = msaSense as IMoStemMsa;
			if (msaStem != null)
			{
				foreach (var poss in rgpossProdRestrict)
				{
					msaStem.ProdRestrictRC.Add(poss);
				}
				return;
			}
			var msaInfl = msaSense as IMoInflAffMsa;
			if (msaInfl != null)
			{
				foreach (var poss in rgpossProdRestrict)
				{
					msaInfl.FromProdRestrictRC.Add(poss);
				}
				return;
			}
			var msaDeriv = msaSense as IMoDerivAffMsa;
			if (msaDeriv != null)
			{
				foreach (var poss in rgpossProdRestrict)
				{
					msaDeriv.ToProdRestrictRC.Add(poss);
				}
				if (rgpossFromProdRestrict != null)
				{
					foreach (var poss in rgpossFromProdRestrict)
					{
						msaDeriv.FromProdRestrictRC.Add(poss);
					}
				}
			}
		}

		/// <summary>
		/// Find or create an IMoUnclassifiedAffixMsa which matches the given values.
		/// </summary>
		/// <returns>true if the desired MSA is newly created, false if it already exists</returns>
		private bool FindOrCreateUnclassifiedAffixMSA(ILexEntry le, IPartOfSpeech pos,
			Dictionary<string, List<string>> dictPosSlots,
			Dictionary<string, List<string>> dictPosInflClasses,
			List<string> rgsResidue,
			ref IMoMorphSynAnalysis msaSense)
		{
			foreach (var msa in le.MorphoSyntaxAnalysesOC)
			{
				var msaAffix = msa as IMoUnclassifiedAffixMsa;
				if (msaAffix != null &&
					msaAffix.PartOfSpeechRA == pos &&
					MsaSlotInfoMatches(dictPosSlots, msaAffix) &&
					MsaInflClassInfoMatches(dictPosInflClasses, null, msaAffix))
					// Don't bother comparing residue -- it's not that important!
				{
					msaSense = msa;
					return false;
				}
			}
			msaSense = CreateNewMoUnclassifiedAffixMsa();
			le.MorphoSyntaxAnalysesOC.Add(msaSense);
			if (pos != null)
			{
				((IMoUnclassifiedAffixMsa)msaSense).PartOfSpeechRA = pos;
			}
			return true;
		}

		/// <summary>
		/// Find or create an IMoDerivStepMsa which matches the given values.
		/// </summary>
		/// <returns>
		/// true if the desired MSA is newly created, false if it already exists
		/// </returns>
		private bool FindOrCreateDerivStepAffixMSA(ILexEntry le, IPartOfSpeech pos,
			Dictionary<string, List<string>> dictPosSlots,
			Dictionary<string, List<string>> dictPosInflClasses,
			List<string> rgsResidue,
			ref IMoMorphSynAnalysis msaSense)
		{
			foreach (var msa in le.MorphoSyntaxAnalysesOC)
			{
				var msaAffix = msa as IMoDerivStepMsa;
				if (msaAffix != null &&
					msaAffix.PartOfSpeechRA == pos &&
					MsaSlotInfoMatches(dictPosSlots, msaAffix) &&
					MsaInflClassInfoMatches(dictPosInflClasses, null, msaAffix))
					// Don't bother comparing residue -- it's not that important!
				{
					msaSense = msa;
					return false;
				}
			}
			msaSense = CreateNewMoDerivStepMsa();
			le.MorphoSyntaxAnalysesOC.Add(msaSense);
			if (pos != null)
			{
				((IMoDerivStepMsa)msaSense).PartOfSpeechRA = pos;
			}
			return true;
		}

		/// <summary>
		/// Find or create an IMoDerivAffMsa which matches the given values.
		/// </summary>
		/// <returns>true if the desired MSA is newly created, false if it already exists</returns>
		private bool FindOrCreateDerivAffixMSA(ILexEntry le, IPartOfSpeech pos, string sFromPOS,
			Dictionary<string, List<string>> dictPosSlots,
			Dictionary<string, List<string>> dictPosInflClasses,
			Dictionary<string, List<string>> dictPosFromInflClasses,
			List<ICmPossibility> rgpossProdRestrict, List<ICmPossibility> rgpossFromProdRestrict,
			string sInflectionFeature, string sFromInflFeature, string sFromStemName,
			List<string> rgsResidue, ref IMoMorphSynAnalysis msaSense)
		{
			IPartOfSpeech posFrom = null;
			if (!string.IsNullOrEmpty(sFromPOS))
			{
				posFrom = FindOrCreatePartOfSpeech(sFromPOS);
			}
			foreach (var msa in le.MorphoSyntaxAnalysesOC)
			{
				var msaAffix = msa as IMoDerivAffMsa;
				if (msaAffix != null &&
					msaAffix.ToPartOfSpeechRA == pos &&
					msaAffix.FromPartOfSpeechRA == posFrom &&
					MsaSlotInfoMatches(dictPosSlots, msaAffix) &&
					MsaInflClassInfoMatches(dictPosInflClasses, dictPosFromInflClasses, msaAffix) &&
					MsaExceptionFeatsMatch(rgpossProdRestrict, rgpossFromProdRestrict, msaAffix) &&
					MsaInflFeatureMatches(sInflectionFeature, sFromInflFeature, msaAffix) &&
					MsaStemNameMatches(sFromStemName, msaAffix))
					// Don't bother comparing residue -- it's not that important!
				{
					msaSense = msa;
					return false;
				}
			}
			msaSense = CreateNewMoDerivAffMsa();
			le.MorphoSyntaxAnalysesOC.Add(msaSense);
			if (pos != null)
			{
				((IMoDerivAffMsa)msaSense).ToPartOfSpeechRA = pos;
			}
			if (posFrom != null)
			{
				((IMoDerivAffMsa)msaSense).FromPartOfSpeechRA = posFrom;
			}
			return true;
		}

		/// <summary>
		/// Find or create an IMoInflAffMsa which matches the given values.
		/// </summary>
		/// <returns>true if the desired MSA is newly created, false if it already exists</returns>
		private bool FindOrCreateInflAffixMSA(ILexEntry le, IPartOfSpeech pos,
			Dictionary<string, List<string>> dictPosSlots,
			Dictionary<string, List<string>> dictPosInflClasses,
			List<ICmPossibility> rgpossProdRestrict, string sFeatureString,
			List<string> rgsResidue,
			ref IMoMorphSynAnalysis msaSense)
		{
			foreach (var msa in le.MorphoSyntaxAnalysesOC)
			{
				var msaAffix = msa as IMoInflAffMsa;
				if (msaAffix != null &&
					msaAffix.PartOfSpeechRA == pos &&
					MsaSlotInfoMatches(dictPosSlots, msaAffix) &&
					MsaInflClassInfoMatches(dictPosInflClasses, null, msaAffix) &&
					MsaExceptionFeatsMatch(rgpossProdRestrict, null,  msaAffix) &&
					MsaInflFeatureMatches(sFeatureString, null, msaAffix))
					// Don't bother comparing residue -- it's not that important!
				{
					msaSense = msa;
					return false;
				}
			}
			msaSense = CreateNewMoInflAffMsa();
			le.MorphoSyntaxAnalysesOC.Add(msaSense);
			if (pos != null)
			{
				((IMoInflAffMsa)msaSense).PartOfSpeechRA = pos;
			}
			return true;
		}

		private void ProcessMsaInflectionClassInfo(Dictionary<string, List<string>> dictPosInflClasses, Dictionary<string, List<string>> dictPosFromInflClasses, IMoMorphSynAnalysis msa)
		{
			if (msa is IMoInflAffMsa || msa is IMoUnclassifiedAffixMsa)
			{
				return;
			}
			var msaDeriv = msa as IMoDerivAffMsa;
			var msaStep = msa as IMoDerivStepMsa;
			var msaStem = msa as IMoStemMsa;
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				if (msaDeriv != null)
				{
					msaDeriv.ToInflectionClassRA = null;
					msaDeriv.FromInflectionClassRA = null;
				}
				else if (msaStep != null)
				{
					msaStep.InflectionClassRA = null;
				}
				else if (msaStem != null)
				{
					msaStem.InflectionClassRA = null;
				}
			}
			else if (m_msImport == MergeStyle.MsKeepOld && !m_fCreatingNewSense)
			{
				if (msaDeriv != null && (msaDeriv.ToInflectionClassRA != null || msaDeriv.FromInflectionClassRA != null))
				{
					return;
				}
				if (msaStep?.InflectionClassRA != null)
				{
					return;
				}
				if (msaStem?.InflectionClassRA != null)
				{
					return;
				}
			}
			foreach (var sPos in dictPosInflClasses.Keys)
			{
				var pos = FindOrCreatePartOfSpeech(sPos);
				var rgsInflClasses = dictPosInflClasses[sPos];
				if (rgsInflClasses.Count > 0 && pos != null)
				{
					foreach (var sInflClass in rgsInflClasses)
					{
						var incl = FindOrCreateInflectionClass(pos, sInflClass);
						if (msaDeriv != null)
						{
							msaDeriv.ToInflectionClassRA = incl;
						}
						else if (msaStep != null)
						{
							msaStep.InflectionClassRA = incl;
						}
						else if (msaStem != null)
						{
							msaStem.InflectionClassRA = incl;
						}
					}
				}
			}
			if (msaDeriv != null && dictPosFromInflClasses != null)
			{
				foreach (var sPos in dictPosFromInflClasses.Keys)
				{
					var pos = FindOrCreatePartOfSpeech(sPos);
					var rgsInflClasses = dictPosFromInflClasses[sPos];
					if (rgsInflClasses.Count > 0 && pos != null)
					{
						foreach (var sInflClass in rgsInflClasses)
						{
							var incl = FindOrCreateInflectionClass(pos, sInflClass);
							msaDeriv.FromInflectionClassRA = incl;		// last one wins...
						}
					}
				}
			}
		}

		private IMoInflClass FindOrCreateInflectionClass(IPartOfSpeech pos, string sInflClass)
		{
			var incl = pos.InflectionClassesOC.FirstOrDefault(inclT => HasMatchingUnicodeAlternative(sInflClass.ToLowerInvariant(), inclT.Abbreviation, inclT.Name));
			if (incl == null)
			{
				incl = CreateNewMoInflClass();
				pos.InflectionClassesOC.Add(incl);
				incl.Name.set_String(m_cache.DefaultAnalWs, sInflClass);
				m_rgnewInflClasses.Add(incl);
			}
			return incl;
		}

		private bool MsaInflClassInfoMatches(Dictionary<string, List<string>> dictPosInflClasses, Dictionary<string, List<string>> dictPosFromInflClasses, IMoMorphSynAnalysis msa)
		{
			if (msa is IMoInflAffMsa || msa is IMoUnclassifiedAffixMsa)
			{
				return true;
			}
			var fMatch = MsaMatchesInflClass(dictPosInflClasses, msa, false);
			if (fMatch && msa is IMoDerivAffMsa && dictPosFromInflClasses != null)
			{
				fMatch = MsaMatchesInflClass(dictPosFromInflClasses, msa, true);
			}
			return fMatch;
		}

		private bool MsaMatchesInflClass(Dictionary<string, List<string>> dictPosInflClasses, IMoMorphSynAnalysis msa, bool fFrom)
		{
			var msaDeriv = msa as IMoDerivAffMsa;
			var msaStep = msa as IMoDerivStepMsa;
			var msaStem = msa as IMoStemMsa;
			var fMatch = true;
			foreach (var sPos in dictPosInflClasses.Keys)
			{
				var pos = FindOrCreatePartOfSpeech(sPos);
				var rgsInflClasses = dictPosInflClasses[sPos];
				if (rgsInflClasses.Count > 0 && pos != null)
				{
					foreach (var sInflClass in rgsInflClasses)
					{
						var incl = pos.InflectionClassesOC.FirstOrDefault(inclT => HasMatchingUnicodeAlternative(sInflClass.ToLowerInvariant(), inclT.Abbreviation, inclT.Name));
						if (incl == null)
						{
							// Go ahead and create the new inflection class now.
							incl = CreateNewMoInflClass();
							pos.InflectionClassesOC.Add(incl);
							incl.Name.set_String(m_cache.DefaultAnalWs, sInflClass);
							m_rgnewInflClasses.Add(incl);
						}
						if (fFrom)
						{
							if (msaDeriv != null)
							{
								fMatch = msaDeriv.FromInflectionClassRA == incl;
							}
						}
						else
						{
							if (msaDeriv != null)
							{
								fMatch = msaDeriv.ToInflectionClassRA == incl;
							}
							else if (msaStep != null)
							{
								fMatch = msaStep.InflectionClassRA == incl;
							}
							else if (msaStem != null)
							{
								fMatch = msaStem.InflectionClassRA == incl;
							}
						}
					}
				}
			}
			return fMatch;
		}

		private void ProcessMsaSlotInformation(Dictionary<string, List<string>> dictPosSlots, IMoMorphSynAnalysis msa)
		{
			var msaInfl = msa as IMoInflAffMsa;
			if (msaInfl == null)
			{
				return;
			}
			foreach (var sPos in dictPosSlots.Keys)
			{
				var pos = FindOrCreatePartOfSpeech(sPos);
				var rgsSlot = dictPosSlots[sPos];
				if (rgsSlot.Any() && pos != null)
				{
					foreach (var sSlot in rgsSlot)
					{
						var slot = pos.AffixSlotsOC.FirstOrDefault(slotT => HasMatchingUnicodeAlternative(sSlot.ToLowerInvariant(), null, slotT.Name));
						if (slot == null)
						{
							slot = CreateNewMoInflAffixSlot();
							pos.AffixSlotsOC.Add(slot);
							slot.Name.set_String(m_cache.DefaultAnalWs, sSlot);
							m_rgnewSlots.Add(slot);
						}

						if (!msaInfl.SlotsRC.Contains(slot))
						{
							msaInfl.SlotsRC.Add(slot);
						}
					}
				}
			}
		}

		private bool MsaSlotInfoMatches(Dictionary<string, List<string>> dictPosSlots, IMoMorphSynAnalysis msa)
		{
			var msaInfl = msa as IMoInflAffMsa;
			if (msaInfl == null)
			{
				return true;
			}
			foreach (var sPos in dictPosSlots.Keys)
			{
				var pos = FindOrCreatePartOfSpeech(sPos);
				var rgsSlot = dictPosSlots[sPos];
				if (rgsSlot.Any() && pos != null)
				{
					foreach (var sSlot in rgsSlot)
					{
						var slot = pos.AffixSlotsOC.FirstOrDefault(slotT => HasMatchingUnicodeAlternative(sSlot.ToLowerInvariant(), null, slotT.Name));
						if (slot == null)
						{
							// Go ahead and create the new slot -- we'll need it shortly.
							slot = CreateNewMoInflAffixSlot();
							pos.AffixSlotsOC.Add(slot);
							slot.Name.set_String(m_cache.DefaultAnalWs, sSlot);
							m_rgnewSlots.Add(slot);
						}

						if (!msaInfl.SlotsRC.Contains(slot))
						{
							return false;
						}
					}
				}
			}
			return true;
		}

		private bool MsaExceptionFeatsMatch(List<ICmPossibility> rgpossProdRestrict, List<ICmPossibility> rgpossFromProdRestrict, IMoMorphSynAnalysis msa)
		{
			var msaStem = msa as IMoStemMsa;
			if (msaStem != null)
			{
				return msaStem.ProdRestrictRC.Count == rgpossProdRestrict.Count && msaStem.ProdRestrictRC.All(poss => rgpossProdRestrict.Contains(poss));
			}
			var msaInfl = msa as IMoInflAffMsa;
			if (msaInfl != null)
			{
				return msaInfl.FromProdRestrictRC.Count == rgpossProdRestrict.Count && msaInfl.FromProdRestrictRC.All(poss => rgpossProdRestrict.Contains(poss));
			}
			var msaDeriv = msa as IMoDerivAffMsa;
			if (msaDeriv != null)
			{
				if (msaDeriv.ToProdRestrictRC.Count != rgpossProdRestrict.Count)
				{
					return false;
				}
				if (rgpossFromProdRestrict == null && msaDeriv.FromProdRestrictRC.Count > 0)
				{
					return false;
				}
				if (msaDeriv.FromProdRestrictRC.Count != rgpossFromProdRestrict.Count)
				{
					return false;
				}
				if (msaDeriv.ToProdRestrictRC.Any(poss => !rgpossProdRestrict.Contains(poss)))
				{
					return false;
				}
				if (rgpossFromProdRestrict != null)
				{
					return msaDeriv.FromProdRestrictRC.All(poss => rgpossFromProdRestrict.Contains(poss));
				}
			}
			return true;
		}

		private bool MsaInflFeatureMatches(string sFeatureString, string sFromInflectionFeature, IMoMorphSynAnalysis msa)
		{
			var msaStem = msa as IMoStemMsa;
			if (msaStem != null)
			{
				if (msaStem.MsFeaturesOA == null)
				{
					return string.IsNullOrEmpty(sFeatureString);
				}

				if (string.IsNullOrEmpty(sFeatureString))
				{
					return false;
				}
				return sFeatureString == msaStem.MsFeaturesOA.LiftName;
			}
			var msaInfl = msa as IMoInflAffMsa;
			if (msaInfl != null)
			{
				if (msaInfl.InflFeatsOA == null)
				{
					return string.IsNullOrEmpty(sFeatureString);
				}

				if (string.IsNullOrEmpty(sFeatureString))
				{
					return false;
				}
				return sFeatureString == msaInfl.InflFeatsOA.LiftName;
			}
			var msaDeriv = msa as IMoDerivAffMsa;
			if (msaDeriv != null)
			{
				bool fOk;
				if (msaDeriv.ToMsFeaturesOA == null)
				{
					fOk = string.IsNullOrEmpty(sFeatureString);
				}
				else
				{
					fOk = msaDeriv.ToMsFeaturesOA.LiftName == sFeatureString;
				}
				if (fOk)
				{
					if (msaDeriv.FromMsFeaturesOA == null)
					{
						fOk = string.IsNullOrEmpty(sFromInflectionFeature);
					}
					else
					{
						fOk = msaDeriv.FromMsFeaturesOA.LiftName == sFromInflectionFeature;
					}
				}
				return fOk;
			}
			return true;
		}

		/// <summary>
		/// Parse a feature string that looks like "[nagr:[gen:f num:?]]", and store
		/// the corresponding feature structure.
		/// </summary>
		private bool ParseFeatureString(string sFeatureString, IMoMorphSynAnalysis msa, bool fFrom)
		{
			if (string.IsNullOrEmpty(sFeatureString))
			{
				return true;
			}
			sFeatureString = sFeatureString.Trim();
			if (string.IsNullOrEmpty(sFeatureString))
			{
				return true;
			}
			var msaStem = msa as IMoStemMsa;
			var msaInfl = msa as IMoInflAffMsa;
			var msaDeriv = msa as IMoDerivAffMsa;
			if (msaStem == null && msaInfl == null && msaDeriv == null)
			{
				return false;
			}
			string sType = null;
			if (sFeatureString[0] == '{')
			{
				var idx = sFeatureString.IndexOf('}');
				if (idx < 0)
				{
					return false;
				}
				sType = sFeatureString.Substring(1, idx - 1);
				sType = sType.Trim();
				sFeatureString = sFeatureString.Substring(idx + 1);
				sFeatureString = sFeatureString.Trim();
			}
			if (sFeatureString.Length == 0)
			{
				return false; // In case of bad data as in LT-13596
			}

			if (sFeatureString[0] == '[' && sFeatureString.EndsWith("]"))
			{
				// Remove the outermost bracketing
				var rgsName = new List<string>();
				var rgsValue = new List<string>();
				if (SplitFeatureString(sFeatureString.Substring(1, sFeatureString.Length - 2), rgsName, rgsValue))
				{
					if (m_factFsFeatStruc == null)
					{
						m_factFsFeatStruc = m_cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>();
					}
					var feat = m_factFsFeatStruc.Create();
					if (msaStem != null)
					{
						msaStem.MsFeaturesOA = feat;
					}
					else if (msaInfl != null)
					{
						msaInfl.InflFeatsOA = feat;
					}
					else if (msaDeriv != null)
					{
						if (fFrom)
						{
							msaDeriv.FromMsFeaturesOA = feat;
						}
						else if (msaDeriv != null)
						{
							msaDeriv.ToMsFeaturesOA = feat;
						}
					}
					if (!string.IsNullOrEmpty(sType))
					{
						IFsFeatStrucType type;
						if (m_mapIdFeatStrucType.TryGetValue(sType, out type))
						{
							feat.TypeRA = type;
						}
						else
						{
							return false;
						}
					}
					return ProcessFeatStrucData(rgsName, rgsValue, feat);
				}
				return false;
			}
			return false;
		}

		private IFsFeatStruc ParseFeatureString(string sFeatureString, IMoStemName stem)
		{
			if (string.IsNullOrEmpty(sFeatureString))
			{
				return null;
			}
			sFeatureString = sFeatureString.Trim();
			if (string.IsNullOrEmpty(sFeatureString))
			{
				return null;
			}
			if (stem == null)
			{
				return null;
			}
			IFsFeatStrucType type = null;
			if (sFeatureString[0] == '{')
			{
				var idx = sFeatureString.IndexOf('}');
				if (idx < 0)
				{
					return null;
				}
				var sType = sFeatureString.Substring(1, idx - 1);
				sType = sType.Trim();
				if (!string.IsNullOrEmpty(sType))
				{
					if (!m_mapIdFeatStrucType.TryGetValue(sType, out type))
					{
						return null;
					}
				}
				sFeatureString = sFeatureString.Substring(idx + 1);
				sFeatureString = sFeatureString.Trim();
			}
			if (sFeatureString[0] == '[' && sFeatureString.EndsWith("]"))
			{
				// Remove the outermost bracketing
				var rgsName = new List<string>();
				var rgsValue = new List<string>();
				if (SplitFeatureString(sFeatureString.Substring(1, sFeatureString.Length - 2), rgsName, rgsValue))
				{
					if (m_factFsFeatStruc == null)
					{
						m_factFsFeatStruc = m_cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>();
					}
					var ffs = m_factFsFeatStruc.Create();
					stem.RegionsOC.Add(ffs);
					if (type != null)
					{
						ffs.TypeRA = type;
					}
					if (ProcessFeatStrucData(rgsName, rgsValue, ffs))
					{
						var liftName = ffs.LiftName;
						var cffs = stem.RegionsOC.Count(fs => fs.LiftName == liftName);
						if (cffs > 1)
						{
							stem.RegionsOC.Remove(ffs);
							return null;
						}
						return ffs;
					}
					stem.RegionsOC.Remove(ffs);
					return null;
				}
			}
			return null;
		}

		/// <summary>
		/// recursively process the inner text of a feature structure.
		/// </summary>
		/// <returns>true if successful, false if a parse error occurs</returns>
		private bool ProcessFeatStrucData(List<string> rgsName, List<string> rgsValue, IFsFeatStruc ownerFeatStruc)
		{
			// TODO: figure out how (and when) to set ownerFeatStruc.TypeRA
			Debug.Assert(rgsName.Count == rgsValue.Count);
			for (var i = 0; i < rgsName.Count; ++i)
			{
				var sName = rgsName[i];
				IFsFeatDefn featDefn;
				if (!m_mapIdFeatDefn.TryGetValue(sName, out featDefn))
				{
					// REVIEW: SHOULD WE TRY TO CREATE ONE?
					return false;
				}
				var sValue = rgsValue[i];
				if (sValue[0] == '[')
				{
					if (!sValue.EndsWith("]"))
					{
						return false;
					}

					if (m_factFsComplexValue == null)
					{
						m_factFsComplexValue = m_cache.ServiceLocator.GetInstance<IFsComplexValueFactory>();
					}
					var rgsValName = new List<string>();
					var rgsValValue = new List<string>();
					if (SplitFeatureString(sValue.Substring(1, sValue.Length - 2), rgsValName, rgsValValue))
					{
						var val = m_factFsComplexValue.Create();
						ownerFeatStruc.FeatureSpecsOC.Add(val);
						val.FeatureRA = featDefn;
						var featVal = m_factFsFeatStruc.Create();
						val.ValueOA = featVal;
						if (!ProcessFeatStrucData(rgsValName, rgsValValue, featVal))
						{
							return false;
						}
					}
					else
					{
						return false;
					}
				}
				else
				{
					if (m_factFsClosedValue == null)
					{
						m_factFsClosedValue = m_cache.ServiceLocator.GetInstance<IFsClosedValueFactory>();
					}
					IFsSymFeatVal featVal;
					var valueKey = $"{sName}:{sValue}";
					if (m_mapIdAbbrSymFeatVal.TryGetValue(valueKey, out featVal))
					{
						var val = m_factFsClosedValue.Create();
						ownerFeatStruc.FeatureSpecsOC.Add(val);
						val.FeatureRA = featDefn;
						val.ValueRA = featVal;
					}
					else
					{
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Split the feature string into its parallel names and values.  It may well have only
		/// one of each.  The outermost brackets have been removed before this method is called.
		/// </summary>
		/// <returns>true if successful, false if a parse error occurs</returns>
		private static bool SplitFeatureString(string sFeat, List<string> rgsName, List<string> rgsValue)
		{
			while (!string.IsNullOrEmpty(sFeat))
			{
				var idxVal = sFeat.IndexOf(':');
				if (idxVal > 0)
				{
					var sFeatName = sFeat.Substring(0, idxVal).Trim();
					var sFeatVal = sFeat.Substring(idxVal + 1).Trim();
					if (sFeatName.Length == 0 || sFeatVal.Length == 0)
					{
						return false;
					}
					rgsName.Add(sFeatName);
					int idxSep;
					if (sFeatVal[0] == '[')
					{
						idxSep = FindMatchingCloseBracket(sFeatVal);
						if (idxSep < 0)
						{
							return false;
						}
						++idxSep;
						if (idxSep >= sFeatVal.Length)
						{
							idxSep = -1;
						}
						else if (sFeatVal[idxSep] != ' ')
						{
							return false;
						}
					}
					else
					{
						idxSep = sFeatVal.IndexOf(' ');
					}
					if (idxSep > 0)
					{
						rgsValue.Add(sFeatVal.Substring(0, idxSep));
						sFeat = sFeatVal.Substring(idxSep).Trim();
					}
					else
					{
						rgsValue.Add(sFeatVal);
						sFeat = null;
					}
				}
				else
				{
					return false;
				}
			}
			return rgsName.Count == rgsValue.Count && rgsName.Count > 0;
		}

		/// <summary>
		/// If the string starts with an open bracket ('['), find the matching close bracket
		/// (']').  There may be embedded pairs of open and close brackets inside the string!
		/// </summary>
		/// <returns>index of the matching close bracket, or a negative number if not found</returns>
		private static int FindMatchingCloseBracket(string sFeatVal)
		{
			if (sFeatVal[0] != '[')
			{
				return -1;
			}
			var rgBrackets = new[] { '[', ']' };
			var cOpen = 1;
			var idxBracket = 0;
			while (cOpen > 0)
			{
				idxBracket = sFeatVal.IndexOfAny(rgBrackets, idxBracket + 1);
				if (idxBracket < 0)
				{
					return idxBracket;
				}
				if (sFeatVal[idxBracket] == '[')
				{
					++cOpen;
				}
				else
				{
					--cOpen;
				}
			}
			return idxBracket;
		}

		private static bool MsaStemNameMatches(string sFromStemName, IMoDerivAffMsa msaAffix)
		{
			if (string.IsNullOrEmpty(sFromStemName) && msaAffix.FromStemNameRA == null)
			{
				return true;
			}
			var msn = msaAffix.FromStemNameRA;
			for (var i = 0; i < msn.Name.StringCount; ++i)
			{
				int ws;
				var tss = msn.Name.GetStringFromIndex(i, out ws);
				if (tss.Text == sFromStemName)
				{
					return true;
				}
			}
			return false;
		}

		private bool SenseGramInfoConflicts(ILexSense ls, LiftGrammaticalInfo gram)
		{
			if (ls.MorphoSyntaxAnalysisRA == null || gram == null)
			{
				return false;
			}
			var sPOS = gram.Value;
			IPartOfSpeech pos = null;
			string sType = null;
			string sFromPOS = null;
			var dictPosSlots = new Dictionary<string, List<string>>();
			var dictPosInflClasses = new Dictionary<string, List<string>>();
			foreach (var trait in gram.Traits)
			{
				if (trait.Name == "type")
				{
					sType = trait.Value;
				}
				else if (trait.Name == RangeNames.sPartsOfSpeechOAold1 || trait.Name == RangeNames.sPartsOfSpeechOAold2)
				{
					sFromPOS = trait.Value;
				}
				else if (trait.Name.EndsWith("-slot") || trait.Name.EndsWith("-Slots"))
				{
					var len = trait.Name.Length - (trait.Name.EndsWith("-slot") ? 5 : 6);
					var sTraitPos = trait.Name.Substring(0, len);
					List<string> rgsSlots;
					if (!dictPosSlots.TryGetValue(sTraitPos, out rgsSlots))
					{
						rgsSlots = new List<string>();
						dictPosSlots.Add(sTraitPos, rgsSlots);
					}
					rgsSlots.Add(trait.Value);
				}
				else if (trait.Name.EndsWith("-infl-class") || trait.Name.EndsWith("-InflectionClass"))
				{
					var len = trait.Name.Length - (trait.Name.EndsWith("-infl-class") ? 11 : 16);
					var sTraitPos = trait.Name.Substring(0, len);
					List<string> rgsInflClasses;
					if (!dictPosInflClasses.TryGetValue(sTraitPos, out rgsInflClasses))
					{
						rgsInflClasses = new List<string>();
						dictPosInflClasses.Add(sTraitPos, rgsInflClasses);
					}
					rgsInflClasses.Add(trait.Value);
				}
			}

			if (!string.IsNullOrEmpty(sPOS))
			{
				pos = FindOrCreatePartOfSpeech(sPOS);
			}
			var msa = ls.MorphoSyntaxAnalysisRA;
			int hvoPosOld;
			switch (sType)
			{
				case "inflAffix":
					var msaInfl = msa as IMoInflAffMsa;
					if (msaInfl == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls, this);
						return true;
					}
					hvoPosOld = msaInfl.PartOfSpeechRA?.Hvo ?? 0;
					break;
				case "derivAffix":
					var msaDerv = msa as IMoDerivAffMsa;
					if (msaDerv == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls, this);
						return true;
					}
					hvoPosOld = msaDerv.ToPartOfSpeechRA?.Hvo ?? 0;
					if (!string.IsNullOrEmpty(sFromPOS))
					{
						var posNewFrom = FindOrCreatePartOfSpeech(sFromPOS);
						var hvoOldFrom = msaDerv.FromPartOfSpeechRA?.Hvo ?? 0;
						if (posNewFrom != null && hvoOldFrom != 0 && posNewFrom.Hvo != hvoOldFrom)
						{
							return true;
						}
					}
					break;
				case "derivStepAffix":
					var msaStep = msa as IMoDerivStepMsa;
					if (msaStep == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls, this);
						return true;
					}
					hvoPosOld = msaStep.PartOfSpeechRA?.Hvo ?? 0;
					break;
				case "affix":
					var msaUncl = msa as IMoUnclassifiedAffixMsa;
					if (msaUncl == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls, this);
						return true;
					}
					hvoPosOld = msaUncl.PartOfSpeechRA?.Hvo ?? 0;
					break;
				default:
					var msaStem = msa as IMoStemMsa;
					if (msaStem == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls, this);
						return true;
					}
					hvoPosOld = msaStem.PartOfSpeechRA?.Hvo ?? 0;
					break;
			}
			if (hvoPosOld != 0 && pos != null && hvoPosOld != pos.Hvo)
			{
				m_cdConflict = new ConflictingSense("Grammatical Info. Part of Speech", ls, this);
				return true;
			}
			if (MsaSlotInformationConflicts(dictPosSlots, msa))
			{
				m_cdConflict = new ConflictingSense("Grammatical Info. Slot", ls, this);
				return true;
			}
			if (MsaInflectionClassInfoConflicts(dictPosInflClasses, msa))
			{
				m_cdConflict = new ConflictingSense("Grammatical Info. Inflection Class", ls, this);
				return true;
			}
			return false;
		}

		private static bool MsaSlotInformationConflicts(Dictionary<string, List<string>> dictPosSlots, IMoMorphSynAnalysis msa)
		{
			// how do we determine conflicts in a list?
			return false;
		}

		private static bool MsaInflectionClassInfoConflicts(Dictionary<string, List<string>> dictPosInflClasses, IMoMorphSynAnalysis msa)
		{
			// How do we determine conflicts in a list?
			return false;
		}

		private void CreateSenseIllustrations(ILexSense ls, CmLiftSense sense)
		{
			AddNewWsToBothVernAnal();
			foreach (var uref in sense.Illustrations)
			{
				int ws;
				if (uref.Label != null && !uref.Label.IsEmpty)
				{
					ws = GetWsFromLiftLang(uref.Label.FirstValue.Key);
				}
				else
				{
					ws = m_cache.DefaultVernWs;
				}
				CreatePicture(ls, uref, uref.Url, ws);
			}
		}

		/// <summary>
		/// Copy a picture file from the LIFT source location to the project pictures folder.
		/// Return the path to the copied picture.
		/// </summary>
		/// <param name="sFile">path of file to copy, which may be nested</param>
		private string CopyPicture(string sFile)
		{
			// Paths to try for resolving given filename:
			// {directory of LIFT file}/pictures/filename
			// {FW LinkedFilesRootDir}/filename
			// {FW LinkedFilesRootDir}/Pictures/filename
			// {FW DataDir}/filename
			// {FW DataDir}/Pictures/filename
			// give up and store relative path Pictures/filename (even though it doesn't exist)
			// Some versions of WeSay (e.g., 1.1.68) incorrectly include pictures\ in the picture file names, while
			// excluding the audio directory from the audio file names. So we need to be able to handle picture
			// file names with or without the initial pictures\ directory. We'll delete the initial directory if present.
			// Remember there may be subdirectories under pictures that must be handled.
			var ssFile = sFile;
			if (sFile.Length > 8)
			{
				var tpath = sFile.Substring(0, 9);
				if (tpath.ToLowerInvariant() == "pictures\\" || tpath.ToLowerInvariant() == "pictures/")
				{
					ssFile = sFile.Substring(9);
				}
			}
			var sPath = Path.Combine(Path.GetDirectoryName(m_sLiftFile), "pictures", ssFile);
			sPath = CopyFileToLinkedFiles(ssFile, sPath, LcmFileHelper.ksPicturesDir);
			if (!File.Exists(sPath) && !string.IsNullOrEmpty(m_cache.LangProject.LinkedFilesRootDir))
			{
				sPath = Path.Combine(m_cache.LangProject.LinkedFilesRootDir, sFile);
				if (!File.Exists(sPath) && !string.IsNullOrEmpty(m_cache.LangProject.LinkedFilesRootDir))
				{
					sPath = Path.Combine(m_cache.LangProject.LinkedFilesRootDir, "Pictures", sFile);
					if (!File.Exists(sPath))
					{
						sPath = Path.Combine(FwDirectoryFinder.DataDirectory, sFile);
						if (!File.Exists(sPath))
						{
							sPath = Path.Combine(FwDirectoryFinder.DataDirectory, "Pictures", sFile);
						}
					}
				}
			}
			return sPath;
		}

		/// <summary>
		/// Create a picture, adding it to the lex sense.  The filename is used to guess a full path,
		/// and the label from uref is used to set the caption.
		/// </summary>
		private void CreatePicture(ILexSense ls, LiftUrlRef uref, string sFile, int ws)
		{
			var sPath = CopyPicture(sFile);
			const string sFolder = CmFolderTags.LocalPictures;
			var pict = CreateNewCmPicture();
			ls.PicturesOS.Add(pict);

			AddNewWsToBothVernAnal();
			try
			{
				pict.UpdatePicture(sPath, GetFirstLiftTsString(uref.Label), sFolder, ws);
			}
			catch (ArgumentException ex)
			{
				// If sPath is empty (which it never can be), trying to create the CmFile
				// for the picture will throw. Even if this could happen, we wouldn't care,
				// as the caption will still be set properly.
				Debug.WriteLine("Error initializing picture: " + ex.Message);
			}
			if (!File.Exists(sPath))
			{
				pict.PictureFileRA.InternalPath = $"Pictures{Path.DirectorySeparatorChar}{sFile}";
			}
			MergeInMultiString(pict.Caption, CmPictureTags.kflidCaption, uref.Label, pict.Guid);
		}

		/// <summary>
		/// If the file which is represented in the LIFT file as sFile exists at the expected place, sPath,
		/// copy it to the appropriate place relative to the indicated subfolder of the fieldworks
		/// linked files directory, and return an updated path name.
		/// </summary>
		private string CopyFileToLinkedFiles(string sFile, string sPath, string fwDirectory)
		{
			if (File.Exists(sPath) && !string.IsNullOrEmpty(m_cache.LangProject.LinkedFilesRootDir))
			{
				// It exists in the expected place in the LIFT folder. Copy to the expected place in the linked files folder.
				var linkedFilesSubDir = Path.Combine(m_cache.LangProject.LinkedFilesRootDir, fwDirectory);
				var fwPath = Path.Combine(linkedFilesSubDir, sFile);
				Directory.CreateDirectory(Path.GetDirectoryName(fwPath));
				try
				{
					File.Copy(sPath, fwPath, true);
				}
				catch (IOException e)
				{
					// We will get an IOException if Flex has an open entry displaying a picture we are trying to copy.
					// Ignore the copy in this case assuming the picture probably didn't change anyway.
				}
				sPath = fwPath; // Make the linke to the copied file.
			}
			return sPath;
		}

		private void MergeSenseIllustrations(ILexSense ls, CmLiftSense sense)
		{
			var map = new Dictionary<LiftUrlRef, ICmPicture>();
			var setUsed = new HashSet<int>();
			AddNewWsToBothVernAnal();
			foreach (var uref in sense.Illustrations)
			{
				// TODO-Linux: this looks suspicious
				var sFile = uref.Url.Replace('/', '\\');
				var pict = FindPicture(ls.PicturesOS, sFile, uref.Label);
				map.Add(uref, pict);
				if (pict != null)
				{
					setUsed.Add(pict.Hvo);
				}
			}
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				foreach (var hvo in ls.PicturesOS.ToHvoArray())
				{
					if (!setUsed.Contains(hvo))
					{
						m_deletedObjects.Add(hvo);
					}
				}
			}
			foreach (var uref in sense.Illustrations)
			{
				ICmPicture pict;
				map.TryGetValue(uref, out pict);
				if (pict == null)
				{
					var ws = uref.Label != null && !uref.Label.IsEmpty ? GetWsFromLiftLang(uref.Label.FirstValue.Key) : m_cache.DefaultVernWs;
					CreatePicture(ls, uref, uref.Url, ws);
				}
				else
				{
					AddNewWsToAnalysis();
					MergeInMultiString(pict.Caption, CmPictureTags.kflidCaption, uref.Label, pict.Guid);
					// When doing Send/Receive we should copy existing pictures in case they changed.
					if (m_msImport == MergeStyle.MsKeepOnlyNew)
					{
						CopyPicture(uref.Url);
					}
				}
			}
		}

		/// <summary />
		private ICmPicture FindPicture(ILcmOwningSequence<ICmPicture> rgpictures, string sFile, LiftMultiText lmtLabel)
		{
			AddNewWsToBothVernAnal();
			if (sFile == null)
			{
				return null;
			}
			// sFile may or may not have pictures as first part of path. May also have nested subdirectories.
			// By default Lift uses "pictures" in path and Flex uses "Pictures" in path.
			if (sFile.Length > 9)
			{
				var tpath = sFile.Substring(0, 9).ToLowerInvariant();
				if (tpath == "pictures\\" || tpath == "pictures/")
					sFile = sFile.Substring(9);
			}
			ICmPicture pictMatching = null;
			var cMatches = 0;
			foreach (var pict in rgpictures)
			{
				var fpath = pict.PictureFileRA?.InternalPath;
				if (fpath == null)
				{
					continue;
				}
				if (fpath.Length > 9)
				{
					var tpath = fpath.Substring(0, 9).ToLowerInvariant();
					if (tpath == "pictures\\" || tpath == "pictures/")
					{
						fpath = fpath.Substring(9);
					}
				}
				if (sFile == fpath)
				{
					var cCurrent = MultiTsStringMatches(pict.Caption, lmtLabel);
					if (cCurrent >= cMatches)
					{
						pictMatching = pict;
						cMatches = cCurrent;
					}
				}
			}
			return pictMatching;
		}

		private bool SenseIllustrationsConflict(ILexSense ls, List<LiftUrlRef> list)
		{
			if (ls.PicturesOS.Count == 0 || list.Count == 0)
			{
				return false;
			}
			AddNewWsToBothVernAnal();
			foreach (var uref in list)
			{
				var sFile = FileUtils.StripFilePrefix(uref.Url);
				var pict = FindPicture(ls.PicturesOS, sFile, uref.Label);
				if (pict == null)
				{
					continue;
				}
				if (MultiTsStringsConflict(pict.Caption, uref.Label))
				{
					m_cdConflict = new ConflictingSense("Picture Caption", ls, this);
					return true;
				}
			}
			return false;
		}

		private void ProcessSenseRelations(ILexSense ls, CmLiftSense sense)
		{
			// Due to possible forward references, wait until the end to process relations,
			// unless the target is empty.  In which case, add the relation to the residue.
			foreach (var rel in sense.Relations)
			{
				if (string.IsNullOrEmpty(rel.Ref) && rel.Type != "_component-lexeme")
				{
					var xdResidue = FindOrCreateResidue(ls);
					InsertResidueContent(xdResidue, CreateXmlForRelation(rel));
				}
				else
				{
					switch (rel.Type)
					{
						case "minorentry":
						case "subentry":
							// We'll just ignore these backreferences.
							break;
						case "main":
						case "_component-lexeme":
							// These shouldn't happen at a sense level, but...
							var entry = sense.OwningEntry;
							var pend = new PendingLexEntryRef(ls, rel, entry);
							CreateRelationResidue(rel);
							m_rgPendingLexEntryRefs.Add(pend);
							break;
						default:
							var sResidue = CreateRelationResidue(rel);
							m_rgPendingRelation.Add(new PendingRelation(ls, rel, sResidue));
							break;
					}
				}
			}
		}

		private static bool SenseRelationsConflict(ILexSense ls, List<CmLiftRelation> list)
		{
			// TODO: how do we detect conflicts in a list?
			return false;
		}

		private void ProcessSenseReversals(ILexSense ls, CmLiftSense sense)
		{
			foreach (var rev in sense.Reversals)
			{
				var rie = ProcessReversal(rev);
				if (rie != null && rie.ReversalForm.StringCount != 0 && !ls.ReversalEntriesRC.Contains(rie))
				{
					ls.ReversalEntriesRC.Add(rie);
				}
			}
		}

		private IReversalIndexEntry ProcessReversal(CmLiftReversal rev)
		{
			AddNewWsToAnalysis();
			Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs;
			IReversalIndexEntry rie;
			// Do not import blank reversal entries
			if(rev.Form.IsEmpty)
			{
				return null;
			}
			if (rev.Main == null)
			{
				var riOwning = FindOrCreateReversalIndex(rev.Form, rev.Type);
				if (riOwning == null)
				{
					return null;
				}
				if (!m_mapToMapToRie.TryGetValue(riOwning, out mapToRIEs))
				{
					mapToRIEs = new Dictionary<MuElement, List<IReversalIndexEntry>>();
					m_mapToMapToRie.Add(riOwning, mapToRIEs);
				}
				rie = FindOrCreateMatchingReversal(rev.Form, mapToRIEs, riOwning.EntriesOC);
			}
			else
			{
				var rieOwner = ProcessReversal(rev.Main);	// recurse!
				if (!m_mapToMapToRie.TryGetValue(rieOwner, out mapToRIEs))
				{
					mapToRIEs = new Dictionary<MuElement, List<IReversalIndexEntry>>();
					m_mapToMapToRie.Add(rieOwner, mapToRIEs);
				}
				rie = FindOrCreateMatchingReversal(rev.Form, mapToRIEs, rieOwner.SubentriesOS);
			}
			MergeInMultiUnicode(rie.ReversalForm, ReversalIndexEntryTags.kflidReversalForm, rev.Form, rie.Guid);
			ProcessReversalGramInfo(rie, rev.GramInfo);
			return rie;
		}

		/// <summary />
		private IReversalIndexEntry FindOrCreateMatchingReversal(LiftMultiText form, Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs, ILcmOwningCollection<IReversalIndexEntry> entriesOS)
		{
			IReversalIndexEntry rie;
			var rgmue = FindAnyMatchingReversal(form, mapToRIEs, out rie);
			if (rie == null)
			{
				rie = CreateNewReversalIndexEntry();
				entriesOS.Add(rie);
			}
			foreach (var mue in rgmue)
			{
				AddToReversalMap(mue, rie, mapToRIEs);
			}
			return rie;
		}

		private List<MuElement> FindAnyMatchingReversal(LiftMultiText form, Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs, out IReversalIndexEntry rie)
		{
			rie = null;
			var rgmue = new List<MuElement>();
			AddNewWsToAnalysis();
			foreach (var key in form.Keys)
			{
				var ws = GetWsFromLiftLang(key);
				var sNew = form[key].Text; // safe XML
				var sNewNorm = string.IsNullOrEmpty(sNew) ? sNew : Icu.Normalize(sNew, Icu.UNormalizationMode.UNORM_NFD);
				// LiftMultiText parameter may have come in with escaped characters which need to be
				// converted to plain text before comparing with existing entries
				var mue = new MuElement(ws, Icu.Normalize(XmlUtils.DecodeXmlAttribute(sNewNorm), Icu.UNormalizationMode.UNORM_NFD));
				List<IReversalIndexEntry> rgrie;
				if (rie == null && mapToRIEs.TryGetValue(mue, out rgrie))
				{
					foreach (var rieT in rgrie)
					{
						if (SameMultiUnicodeContent(form, rieT.ReversalForm))
						{
							rie = rieT;
							break;
						}
					}
				}
				rgmue.Add(mue);
			}
			return rgmue;
		}

		/// <summary />
		private IReversalIndexEntry FindOrCreateMatchingReversal(LiftMultiText form, Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs, ILcmOwningSequence<IReversalIndexEntry> entriesOS)
		{
			IReversalIndexEntry rie;
			var rgmue = FindAnyMatchingReversal(form, mapToRIEs, out rie);
			if (rie == null)
			{
				rie = CreateNewReversalIndexEntry();
				entriesOS.Add(rie);
			}
			foreach (var mue in rgmue)
			{
				AddToReversalMap(mue, rie, mapToRIEs);
			}
			return rie;
		}

		/// <summary />
		private IReversalIndex FindOrCreateReversalIndex(LiftMultiText contents, string type)
		{
			IReversalIndex riOwning = null;
			// For now, fudge "type" as the basic writing system associated with the reversal.
			var sWs = type;
			if (string.IsNullOrEmpty(sWs))
			{
				if (contents == null || contents.Keys.Count == 0)
				{
					return null;
				}
				sWs = XmlUtils.DecodeXmlAttribute(contents.Keys.Count == 1 ? contents.FirstValue.Key : contents.FirstValue.Key.Split(new char[] { '_', '-' })[0]);
			}
			AddNewWsToAnalysis();
			var ws = GetWsFromStr(sWs);
			if (ws == 0)
			{
				ws = GetWsFromLiftLang(sWs);
				if (GetWsFromStr(sWs) == 0)
				{
					sWs = GetExistingWritingSystem(ws).Id;	// Must be old-style ICU Locale.
					// REVIEW Jason/Hasso): Nobody uses the newly reset 'sWs'. Can we remove the check code?
				}
			}
			return FindOrCreateReversalIndex(ws);
		}

		private void ProcessReversalGramInfo(IReversalIndexEntry rie, LiftGrammaticalInfo gram)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				rie.PartOfSpeechRA = null;
			}
			if (string.IsNullOrEmpty(gram?.Value))
			{
				return;
			}
			var sPOS = gram.Value;
			var ri = rie.ReversalIndex;
			Dictionary<string, ICmPossibility> dict;
			var handle = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(ri.WritingSystem);
			if (m_dictWsReversalPos.ContainsKey(handle))
			{
				dict = m_dictWsReversalPos[handle];
			}
			else
			{
				dict = new Dictionary<string, ICmPossibility>();
				m_dictWsReversalPos.Add(handle, dict);
			}
			if (dict.ContainsKey(sPOS))
			{
				if (!m_fCreatingNewSense && m_msImport == MergeStyle.MsKeepOld)
				{
					if (rie.PartOfSpeechRA == null)
					{
						rie.PartOfSpeechRA = dict[sPOS] as IPartOfSpeech;
					}
				}
				else
				{
					rie.PartOfSpeechRA = dict[sPOS] as IPartOfSpeech;
				}
			}
			else
			{
				var pos = CreateNewPartOfSpeech();
				if (ri.PartsOfSpeechOA == null)
				{
					var fact = m_cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();
					ri.PartsOfSpeechOA = fact.Create();
				}
				ri.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
				// Use the name and abbreviation from a regular PartOfSpeech if available, otherwise
				// just use the key and hope the user can sort it out later.
				if (m_dictPos.ContainsKey(sPOS))
				{
					var posMain = m_dictPos[sPOS] as IPartOfSpeech;
					pos.Abbreviation.MergeAlternatives(posMain.Abbreviation);
					pos.Name.MergeAlternatives(posMain.Name);
				}
				else
				{
					pos.Abbreviation.set_String(m_cache.DefaultAnalWs, sPOS);
					pos.Name.set_String(m_cache.DefaultAnalWs, sPOS);
				}
				if (!m_fCreatingNewSense && m_msImport == MergeStyle.MsKeepOld)
				{
					if (rie.PartOfSpeechRA == null)
					{
						rie.PartOfSpeechRA = pos;
					}
				}
				else
				{
					rie.PartOfSpeechRA = pos;
				}
				dict.Add(sPOS, pos);
			}
		}

		private bool SenseReversalsConflict(ILexSense ls, List<CmLiftReversal> list)
		{
			// how do we detect conflicts in a list?
			return false;
		}

		private void ProcessSenseNotes(ILexSense ls, CmLiftSense sense)
		{
			AddNewWsToAnalysis();
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				MergeInMultiString(ls.AnthroNote, LexSenseTags.kflidAnthroNote, null, ls.Guid);
				MergeInMultiString(ls.Bibliography, LexSenseTags.kflidBibliography, null, ls.Guid);
				MergeInMultiString(ls.DiscourseNote, LexSenseTags.kflidDiscourseNote, null, ls.Guid);
				MergeInMultiString(ls.EncyclopedicInfo, LexSenseTags.kflidEncyclopedicInfo, null, ls.Guid);
				MergeInMultiString(ls.GeneralNote, LexSenseTags.kflidGeneralNote, null, ls.Guid);
				MergeInMultiString(ls.GrammarNote, LexSenseTags.kflidGrammarNote, null, ls.Guid);
				MergeInMultiString(ls.PhonologyNote, LexSenseTags.kflidPhonologyNote, null, ls.Guid);
				MergeInMultiUnicode(ls.Restrictions, LexSenseTags.kflidRestrictions, null, ls.Guid);
				MergeInMultiString(ls.SemanticsNote, LexSenseTags.kflidSemanticsNote, null, ls.Guid);
				MergeInMultiString(ls.SocioLinguisticsNote, LexSenseTags.kflidSocioLinguisticsNote, null, ls.Guid);
				ls.Source = null;
			}
			foreach (var note in sense.Notes)
			{
				if (note.Type == null)
				{
					note.Type = string.Empty;
				}
				switch (note.Type.ToLowerInvariant())
				{
					case "anthropology":
						MergeInMultiString(ls.AnthroNote, LexSenseTags.kflidAnthroNote, note.Content, ls.Guid);
						break;
					case "bibliography":
						MergeInMultiString(ls.Bibliography, LexSenseTags.kflidBibliography, note.Content, ls.Guid);
						break;
					case "discourse":
						MergeInMultiString(ls.DiscourseNote, LexSenseTags.kflidDiscourseNote, note.Content, ls.Guid);
						break;
					case "encyclopedic":
						MergeInMultiString(ls.EncyclopedicInfo, LexSenseTags.kflidEncyclopedicInfo, note.Content, ls.Guid);
						break;
					case "":		// WeSay uses untyped notes in senses; LIFT now exports like this.
					case "general":	// older Flex exported LIFT files have this type value.
						MergeInMultiString(ls.GeneralNote, LexSenseTags.kflidGeneralNote, note.Content, ls.Guid);
						break;
					case "grammar":
						MergeInMultiString(ls.GrammarNote, LexSenseTags.kflidGrammarNote, note.Content, ls.Guid);
						break;
					case "phonology":
						MergeInMultiString(ls.PhonologyNote, LexSenseTags.kflidPhonologyNote, note.Content, ls.Guid);
						break;
					case RangeNames.sRestrictionsOA:
						MergeInMultiUnicode(ls.Restrictions, LexSenseTags.kflidRestrictions, note.Content, ls.Guid);
						break;
					case "semantics":
						MergeInMultiString(ls.SemanticsNote, LexSenseTags.kflidSemanticsNote, note.Content, ls.Guid);
						break;
					case "sociolinguistics":
						MergeInMultiString(ls.SocioLinguisticsNote, LexSenseTags.kflidSocioLinguisticsNote, note.Content, ls.Guid);
						break;
					case "source":
						ls.Source = StoreTsStringValue(m_fCreatingNewSense, ls.Source, note.Content);
						break;
					default:
						StoreNoteAsResidue(ls, note);
						break;
				}
			}
		}

		private bool SenseNotesConflict(ILexSense ls, List<CmLiftNote> list)
		{
			foreach (var note in list)
			{
				if (note.Type == null)
				{
					note.Type = string.Empty;
				}
				AddNewWsToAnalysis();
				switch (note.Type.ToLowerInvariant())
				{
					case "anthropology":
						if (MultiTsStringsConflict(ls.AnthroNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Anthropology Note", ls, this);
							return true;
						}
						break;
					case "bibliography":
						if (MultiTsStringsConflict(ls.Bibliography, note.Content))
						{
							m_cdConflict = new ConflictingSense("Bibliography", ls, this);
							return true;
						}
						break;
					case "discourse":
						if (MultiTsStringsConflict(ls.DiscourseNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Discourse Note", ls, this);
							return true;
						}
						break;
					case "encyclopedic":
						if (MultiTsStringsConflict(ls.EncyclopedicInfo, note.Content))
						{
							m_cdConflict = new ConflictingSense("Encyclopedic Info", ls, this);
							return true;
						}
						break;
					case "general":
						if (MultiTsStringsConflict(ls.GeneralNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("General Note", ls, this);
							return true;
						}
						break;
					case "grammar":
						if (MultiTsStringsConflict(ls.GrammarNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Grammar Note", ls, this);
							return true;
						}
						break;
					case "phonology":
						if (MultiTsStringsConflict(ls.PhonologyNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Phonology Note", ls, this);
							return true;
						}
						break;
					case RangeNames.sRestrictionsOA:
						if (MultiUnicodeStringsConflict(ls.Restrictions, note.Content, false, Guid.Empty, 0))
						{
							m_cdConflict = new ConflictingSense("Restrictions", ls, this);
							return true;
						}
						break;
					case "semantics":
						if (MultiTsStringsConflict(ls.SemanticsNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Semantics Note", ls, this);
							return true;
						}
						break;
					case "sociolinguistics":
						if (MultiTsStringsConflict(ls.SocioLinguisticsNote, note.Content))
						{
							m_cdConflict = new ConflictingSense("Sociolinguistics Note", ls, this);
							return true;
						}
						break;
					case "source":
						IgnoreNewWs();
						if (StringsConflict(ls.Source, GetFirstLiftTsString(note.Content)))
						{
							m_cdConflict = new ConflictingSense("Source", ls, this);
							return true;
						}
						break;
				}
			}
			return false;
		}

		private void ProcessSenseFields(ILexSense ls, CmLiftSense sense)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				ls.ImportResidue = m_tssEmpty;
				ls.ScientificName = m_tssEmpty;
				ClearCustomFields(LexSenseTags.kClassId);
			}
			foreach (var field in sense.Fields)
			{
				var sType = field.Type;
				switch (sType.ToLowerInvariant())
				{
					case "importresidue":
					case "import-residue":
						ls.ImportResidue = StoreTsStringValue(m_fCreatingNewSense, ls.ImportResidue, field.Content);
						break;
					case "scientific_name":	// original FLEX export
					case "scientific-name":
						ls.ScientificName = StoreTsStringValue(m_fCreatingNewSense, ls.ScientificName, field.Content);
						break;
					default:
						ProcessUnknownField(ls, sense, field, "LexSense", "custom-sense-", LexSenseTags.kClassId);
						break;
				}
			}
		}

		private void ClearCustomFields(int clsid)
		{
			// TODO: Implement this!
		}

		/// <summary>
		/// Try to find find (or create) a custom field to store this data in.  If all else
		/// fails, store it in the LiftResidue field.
		/// </summary>
		private void ProcessUnknownField(ICmObject co, LiftObject obj, LiftField field, string sClass, string sOldPrefix, int clid)
		{
			var sType = field.Type;
			if (sType.StartsWith(sOldPrefix))
			{
				sType = sType.Substring(sOldPrefix.Length);
			}
			Debug.Assert(sType.Length > 0);
			var sTag = $"{sClass}-{sType}";
			int flid;
			if (m_dictCustomFlid.TryGetValue(sTag, out flid))
			{
				ProcessCustomFieldData(co.Hvo, flid, field.Content);
			}
			else
			{
				LiftMultiText desc; // safe XML
				if (!m_dictFieldDef.TryGetValue(m_cache.DomainDataByFlid.MetaDataCache.GetClassId(co.ClassName) + sType, out desc))
				{
					m_dictFieldDef.TryGetValue(sOldPrefix + sType, out desc);
				}
				Guid possListGuid;
				flid = FindOrCreateCustomField(sType, desc, clid, out possListGuid);
				if (flid == 0)
				{
					if (clid == LexSenseTags.kClassId || clid == LexExampleSentenceTags.kClassId)
					{
						FindOrCreateResidue(co, obj.Id, LexSenseTags.kflidLiftResidue);
					}
					else
					{
						FindOrCreateResidue(co, obj.Id, LexEntryTags.kflidLiftResidue);
					}
					StoreFieldAsResidue(co, field);
				}
				else
				{
					ProcessCustomFieldData(co.Hvo, flid, field.Content);
				}
			}
		}

		/// <summary />
		private ITsString StoreTsStringValue(bool fCreatingNew, ITsString tssOld, LiftMultiText lmt)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				tssOld = m_tssEmpty;
			}
			IgnoreNewWs();
			var tss = GetFirstLiftTsString(lmt);
			if (TsStringIsNullOrEmpty(tss))
			{
				return tssOld;
			}
			if (m_msImport == MergeStyle.MsKeepOld && !fCreatingNew)
			{
				if (TsStringIsNullOrEmpty(tssOld))
				{
					tssOld= tss;
				}
			}
			else
			{
				tssOld = tss;
			}
			return tssOld;
		}

		private bool SenseFieldsConflict(ILexSense ls, List<LiftField> list)
		{
			foreach (var field in list)
			{
				IgnoreNewWs();
				var sType = field.Type;
				switch (sType.ToLowerInvariant())
				{
					case "importresidue":
					case "import-residue":
						if (ls.ImportResidue != null && ls.ImportResidue.Length != 0)
						{
							var tsb = ls.ImportResidue.GetBldr();
							var idx = tsb.Text.IndexOf("<lift-residue id=");
							if (idx >= 0)
							{
								tsb.Replace(idx, tsb.Length, null, null);
							}
							if (StringsConflict(tsb.GetString(), GetFirstLiftTsString(field.Content)))
							{
								m_cdConflict = new ConflictingSense("Import Residue", ls, this);
								return true;
							}
						}
						break;
					case "scientific_name":	// original FLEX export
					case "scientific-name":
						if (StringsConflict(ls.ScientificName, GetFirstLiftTsString(field.Content)))
						{
							m_cdConflict = new ConflictingSense("Scientific Name", ls, this);
							return true;
						}
						break;
					default:
						int flid;
						if (m_dictCustomFlid.TryGetValue("LexSense-" + sType, out flid))
						{
							if (CustomFieldDataConflicts(ls.Hvo, flid, field.Content))
							{
								m_cdConflict = new ConflictingSense($"{sType} (custom field)", ls, this);
								return true;
							}
						}
						break;
				}
			}
			return false;
		}

		private void ProcessSenseTraits(ILexSense ls, LiftObject sense)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				ls.AnthroCodesRC.Clear();
				ls.SemanticDomainsRC.Clear();
				ls.DomainTypesRC.Clear();
				ls.SenseTypeRA = null;
				ls.StatusRA = null;
				ls.UsageTypesRC.Clear();
				ls.DoNotPublishInRC.Clear();
				ls.DialectLabelsRS.Clear();
			}

			foreach (var lt in sense.Traits)
			{
				ICmPossibility poss;
				switch (lt.Name.ToLowerInvariant())
				{
					case RangeNames.sAnthroListOA:
						var ant = FindOrCreateAnthroCode(lt.Value);
						if (!ls.AnthroCodesRC.Contains(ant))
						{
							ls.AnthroCodesRC.Add(ant);
						}
						break;
					case RangeNames.sSemanticDomainListOAold1: // for WeSay 0.4 compatibility
					case RangeNames.sSemanticDomainListOAold2:
					case RangeNames.sSemanticDomainListOAold3:
					case RangeNames.sSemanticDomainListOA:
						var sem = FindOrCreateSemanticDomain(lt.Value);
						if (!ls.SemanticDomainsRC.Contains(sem))
						{
							ls.SemanticDomainsRC.Add(sem);
						}
						break;
					case RangeNames.sDbDialectLabelsOA:
						poss = FindOrCreateDialect(lt.Value);
						if (!ls.DialectLabelsRS.Contains(poss))
						{
							ls.DialectLabelsRS.Add(poss);
						}
						break;
					case RangeNames.sDbDomainTypesOAold1: // original FLEX export = DomainType
					case RangeNames.sDbDomainTypesOA:
						poss = FindOrCreateDomainType(lt.Value);
						if (!ls.DomainTypesRC.Contains(poss))
						{
							ls.DomainTypesRC.Add(poss);
						}
						break;
					case RangeNames.sDbPublicationTypesOAold:
					case RangeNames.sDbPublicationTypesOA:
						poss = FindOrCreatePublicationType(lt.Value);
						if (!ls.DoNotPublishInRC.Contains(poss))
						{
							ls.DoNotPublishInRC.Add(poss);
						}
						break;
					case RangeNames.sDbSenseTypesOAold1: // original FLEX export = SenseType
					case RangeNames.sDbSenseTypesOA:
						poss = FindOrCreateSenseType(lt.Value);
						if (ls.SenseTypeRA != poss && (m_fCreatingNewSense || m_msImport != MergeStyle.MsKeepOld || ls.SenseTypeRA == null))
						{
							ls.SenseTypeRA = poss;
						}
						break;
					case RangeNames.sStatusOA:
						poss = FindOrCreateStatus(lt.Value);
						if (ls.StatusRA != poss && (m_fCreatingNewSense || m_msImport != MergeStyle.MsKeepOld || ls.StatusRA == null))
						{
							ls.StatusRA = poss;
						}
						break;
					case RangeNames.sDbUsageTypesOAold: // original FLEX export = UsageType
					case RangeNames.sDbUsageTypesOA:
						poss = FindOrCreateUsageType(lt.Value);
						if (!ls.UsageTypesRC.Contains(poss))
						{
							ls.UsageTypesRC.Add(poss);
						}
						break;
					default:
						ProcessUnknownTrait(sense, lt, ls);
						break;
				}
			}
		}

		private bool SenseTraitsConflict(ILexSense ls, List<LiftTrait> list)
		{
			foreach (var lt in list)
			{
				ICmPossibility poss;
				switch (lt.Name.ToLowerInvariant())
				{
					case RangeNames.sAnthroListOA:
						FindOrCreateAnthroCode(lt.Value);
						// how do you detect conflicts in a reference list?
						break;
					case RangeNames.sSemanticDomainListOAold1:	// for WeSay 0.4 compatibility
					case RangeNames.sSemanticDomainListOAold2:
					case RangeNames.sSemanticDomainListOAold3:
					case RangeNames.sSemanticDomainListOA:
						FindOrCreateSemanticDomain(lt.Value);
						// how do you detect conflicts in a reference list?
						break;
					case RangeNames.sDbDialectLabelsOA:
						FindOrCreateDialect(lt.Value);
						break;
					case RangeNames.sDbDomainTypesOAold1:	// original FLEX export = DomainType
					case RangeNames.sDbDomainTypesOA:
						FindOrCreateDomainType(lt.Value);
						// how do you detect conflicts in a reference list?
						break;
					case RangeNames.sDbSenseTypesOAold1:	// original FLEX export = SenseType
					case RangeNames.sDbSenseTypesOA:
						poss = FindOrCreateSenseType(lt.Value);
						if (ls.SenseTypeRA != null && ls.SenseTypeRA != poss)
						{
							m_cdConflict = new ConflictingSense("Sense Type", ls, this);
							return true;
						}
						break;
					case RangeNames.sStatusOA:
						poss = FindOrCreateStatus(lt.Value);
						if (ls.StatusRA != null && ls.StatusRA != poss)
						{
							m_cdConflict = new ConflictingSense("Status", ls, this);
							return true;
						}
						break;
					case RangeNames.sDbUsageTypesOAold:	// original FLEX export = UsageType
					case RangeNames.sDbUsageTypesOA:
						FindOrCreateUsageType(lt.Value);
						// how do you detect conflicts in a reference list?
						break;
				}
			}
			return false;
		}
		#endregion // Methods for storing entry data

		private void EnsureValidMSAsForSenses(ILexEntry le)
		{
			var fIsAffix = IsAffixType(le);
			foreach (var ls in GetAllSenses(le))
			{
				if (ls.MorphoSyntaxAnalysisRA != null)
				{
					continue;
				}
				IMoMorphSynAnalysis msa;
				if (fIsAffix)
				{
					msa = FindEmptyAffixMsa(le);
					if (msa == null)
					{
						msa = CreateNewMoUnclassifiedAffixMsa();
						le.MorphoSyntaxAnalysesOC.Add(msa);
					}
				}
				else
				{
					msa = FindEmptyStemMsa(le);
					if (msa == null)
					{
						msa = CreateNewMoStemMsa();
						le.MorphoSyntaxAnalysesOC.Add(msa);
					}
				}
				ls.MorphoSyntaxAnalysisRA = msa;
			}
		}

		private IEnumerable<ILexSense> GetAllSenses(ILexEntry le)
		{
			var rgls = new List<ILexSense>();
			foreach (var ls in le.SensesOS)
			{
				rgls.Add(ls);
				GetAllSubsenses(ls, rgls);
			}
			return rgls;
		}

		private static void GetAllSubsenses(ILexSense ls, List<ILexSense> rgls)
		{
			foreach (var lsSub in ls.SensesOS)
			{
				rgls.Add(lsSub);
				GetAllSubsenses(lsSub, rgls);
			}
		}

		/// <summary>
		/// Is this entry an affix type?
		/// </summary>
		public bool IsAffixType(ILexEntry le)
		{
			var lfForm = le.LexemeFormOA;
			var cTypes = 0;
			if (lfForm != null)
			{
				var mmt = lfForm.MorphTypeRA;
				if (mmt != null)
				{
					if (mmt.IsStemType)
					{
						return false;
					}
					++cTypes;
				}
			}
			foreach (var form in le.AlternateFormsOS)
			{
				var mmt = form.MorphTypeRA;
				if (mmt != null)
				{
					if (mmt.IsStemType)
					{
						return false;
					}
					++cTypes;
				}
			}
			return cTypes > 0;		// assume stem if no type information.
		}

		private IMoMorphSynAnalysis FindEmptyAffixMsa(ILexEntry le)
		{
			foreach (var msa in le.MorphoSyntaxAnalysesOC)
			{
				var msaAffix = msa as IMoUnclassifiedAffixMsa;
				if (msaAffix != null && msaAffix.PartOfSpeechRA == null)
				{
					return msa;
				}
			}
			return null;
		}

		private IMoMorphSynAnalysis FindEmptyStemMsa(ILexEntry le)
		{
			return le.MorphoSyntaxAnalysesOC.Select(msa => msa as IMoStemMsa).FirstOrDefault(msaStem => msaStem != null
					&& msaStem.PartOfSpeechRA == null && msaStem.FromPartsOfSpeechRC.Count == 0
					&& msaStem.InflectionClassRA == null && msaStem.ProdRestrictRC.Count == 0 && msaStem.StratumRA == null && msaStem.MsFeaturesOA == null);
		}

		// Given an initial private use tag, if it ends with a part that follows the pattern duplN,
		// return one made by incrementing N.
		// Otherwise, return one made by appending dupl1.
		internal static string GetNextDuplPart(string privateUse)
		{
			if (string.IsNullOrEmpty(privateUse))
			{
				return "dupl1";
			}
			var lastPart = privateUse.Split('-').Last();
			if (Regex.IsMatch(lastPart, "dupl[0-9]+", RegexOptions.IgnoreCase))
			{
				// Replace the old lastPart with the result of incrementing the number
				var val = int.Parse(lastPart.Substring("dupl".Length));
				return privateUse.Substring(0, privateUse.Length - lastPart.Length) + ("dupl" + (val + 1));
			}
			// Append dupl1. We know privateUse is not empty.
			return privateUse + "-dupl1";
		}

		#region Methods to find or create list items
		/// <summary />
		private IPartOfSpeech FindOrCreatePartOfSpeech(string val)
		{
			ICmPossibility poss;
			if (TryGetPossibilityMatchingTrait(val, m_dictPos, out poss))
			{
				return poss as IPartOfSpeech;
			}

			var pos = CreateNewPartOfSpeech();
			m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			// Try to find this in the category catalog list, so we can add in more information.
			var cat = FindMatchingEticCategory(val);
			if (cat != null)
			{
				AddEticCategoryInfo(cat, pos);
			}
			if (pos.Name.AnalysisDefaultWritingSystem.Length == 0)
			{
				pos.Name.set_String(m_cache.DefaultAnalWs, val);
			}
			if (pos.Abbreviation.AnalysisDefaultWritingSystem.Length == 0)
			{
				pos.Abbreviation.set_String(m_cache.DefaultAnalWs, val);
			}
			m_dictPos.Add(val, pos);
			m_rgnewPos.Add(pos);
			return pos;
		}

		/// <summary />
		private EticCategory FindMatchingEticCategory(string val)
		{
			var sVal = val.ToLowerInvariant();
			foreach (var cat in m_rgcats)
			{
				foreach (var lang in cat.MultilingualName.Keys)
				{
					var sName = cat.MultilingualName[lang];
					if (sName.ToLowerInvariant() == sVal)
					{
						return cat;
					}
				}
				foreach (var lang in cat.MultilingualAbbrev.Keys)
				{
					var sAbbrev = cat.MultilingualAbbrev[lang];
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
				var envNew = CreateNewPhEnvironment();
				m_cache.LangProject.PhonologicalDataOA.EnvironmentsOS.Add(envNew);
				envNew.StringRepresentation = TsStringUtils.MakeString(sEnv, m_cache.DefaultAnalWs);
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
			if (m_dictMmt.TryGetValue(sTypeName, out mmt) || m_dictMmt.TryGetValue(sTypeName.ToLowerInvariant(), out mmt))
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

		private void FindOrCreateLexRefType(string relationTypeName, string guid, string parent, LiftMultiText desc, LiftMultiText label, LiftMultiText abbrev, LiftMultiText revName, LiftMultiText revAbbrev, int refType)
		{
			if (!string.IsNullOrEmpty(guid))
			{
				// If we got a guid our first choice is to match that object.
				var objRepo = m_cache.ServiceLocator.ObjectRepository;
				Guid realGuid;
				ICmObject match;
				if (Guid.TryParse(guid, out realGuid) && objRepo.TryGetObject(realGuid, out match))
				{
					var lrt1 = match as ILexRefType;
					if (lrt1 != null)
					{
						// For now we're doing this minimal merge. If we want to switch to using the regular merge rules
						// for entries/senses, change these calls to MergeInMultiString.
						MergeStringsFromLiftContents(desc, lrt1.Description, "Description", lrt1);
						MergeStringsFromLiftContents(abbrev, lrt1.Abbreviation, "Abbreviation", lrt1);
						MergeStringsFromLiftContents(label, lrt1.Name, "Name", lrt1);
						MergeStringsFromLiftContents(revName, lrt1.ReverseName, "ReverseName", lrt1);
						MergeStringsFromLiftContents(revAbbrev, lrt1.ReverseAbbreviation, "ReverseAbbreviation", lrt1);
						return;
					}
					// What should we do now? The guid is taken but it's the wrong type!
					// If we make a new LRT with a different guid, anything that references it in the file will point at the wrong object (of the wrong type).
					// If we make one using THIS guid the whole database will be corrupt, with two objects that have the same guid.
					throw new ApplicationException($"input file expects object {guid} to be a LexRefType, but it is a {match.GetType().Name}");
				}
			}
			ICmPossibility poss;
			var normalizedRelTypeName = relationTypeName.Normalize().ToLowerInvariant();
			if (m_dictLexRefTypes.TryGetValue(normalizedRelTypeName, out poss))
			{
				return;
			}
			if (m_dictRevLexRefTypes.TryGetValue(normalizedRelTypeName, out poss))
			{
				return;
			}
			var lrt = CreateNewLexRefType(guid, parent);
			m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(lrt);
			lrt.Name.set_String(m_cache.DefaultAnalWs, relationTypeName);
			if ((string.IsNullOrEmpty(m_sLiftProducer) || m_sLiftProducer.StartsWith("WeSay")) && relationTypeName == "BaseForm")
			{
				lrt.Abbreviation.set_String(m_cache.DefaultAnalWs, "base");
				lrt.ReverseName.set_String(m_cache.DefaultAnalWs, "Derived Forms");
				lrt.ReverseAbbreviation.set_String(m_cache.DefaultAnalWs, "deriv");
				lrt.MappingType = (int)LexRefTypeTags.MappingTypes.kmtEntryTree;
			}
			else
			{
				SetStringsFromLiftContents(desc, lrt.Description);
				SetStringsFromLiftContents(abbrev, lrt.Abbreviation);
				SetStringsFromLiftContents(label, lrt.Name);
				SetStringsFromLiftContents(revName, lrt.ReverseName);
				SetStringsFromLiftContents(revAbbrev, lrt.ReverseAbbreviation);
				lrt.MappingType = refType;
			}
			m_dictLexRefTypes.Add(normalizedRelTypeName, lrt);
			m_rgnewLexRefTypes.Add(lrt);

		}

		private ILexEntryType FindComplexFormType(string sOldEntryType)
		{
			ICmPossibility poss;
			if (m_dictComplexFormType.TryGetValue(sOldEntryType, out poss) || m_dictComplexFormType.TryGetValue(sOldEntryType.ToLowerInvariant(), out poss))
			{
				return poss as ILexEntryType;
			}
			return null;
		}

		private ILexEntryType FindVariantType(string sOldEntryType)
		{
			ICmPossibility poss;
			if (m_dictVariantType.TryGetValue(sOldEntryType, out poss) || m_dictVariantType.TryGetValue(sOldEntryType.ToLowerInvariant(), out poss))
			{
				return poss as ILexEntryType;
			}
			return null;
		}

		private ILexEntryType FindOrCreateComplexFormType(string sType)
		{
			ICmPossibility poss;
			if (TryGetPossibilityMatchingTrait(sType, m_dictComplexFormType, out poss))
			{
				return poss as ILexEntryType;
			}
			var let = CreateNewLexEntryType();
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
			if (TryGetPossibilityMatchingTrait(sType, m_dictVariantType, out poss))
			{
				return poss as ILexEntryType;
			}
			var let = CreateNewLexEntryType();
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
			var ws = m_cache.DefaultAnalWs;
			return FindOrCreatePossibility(traitValue, ws, CreateNewCmAnthroItem, m_dictAnthroCode, m_rgnewAnthroCode, m_cache.LangProject.AnthroListOA) as ICmAnthroItem;
		}

		private ICmSemanticDomain FindOrCreateSemanticDomain(string traitValue)
		{
			var ws = m_cache.DefaultAnalWs;
			return FindOrCreatePossibility(traitValue, ws, CreateNewCmSemanticDomain, m_dictSemDom, m_rgnewSemDom, m_cache.LangProject.SemanticDomainListOA) as ICmSemanticDomain;
		}

		private ICmPossibility FindOrCreateDialect(string traitValue)
		{
			var ws = m_cache.DefaultVernWs;
			return FindOrCreatePossibility(traitValue, ws, CreateNewCmPossibility, m_dictDialect, m_rgnewDialects, m_cache.LangProject.LexDbOA.DialectLabelsOA);
		}

		private static ICmPossibility FindOrCreatePossibility(string traitValue, int ws, Func<ICmPossibility> createMethod,
			IDictionary<string, ICmPossibility> dict, ICollection<ICmPossibility> rgnewPoss, ICmPossibilityList listToUpdate)
		{
			ICmPossibility poss;
			if (TryGetPossibilityMatchingTrait(traitValue, dict, out poss))
			{
				return poss;
			}
			poss = createMethod();
			listToUpdate.PossibilitiesOS.Add(poss);
			UpdatePossibilityWithTraitValue(ws, poss, traitValue, dict, rgnewPoss);
			return poss;
		}

		private static bool TryGetPossibilityMatchingTrait(string traitValue, IDictionary<string, ICmPossibility> dict, out ICmPossibility poss)
		{
			return dict.TryGetValue(traitValue, out poss) || dict.TryGetValue(traitValue.ToLowerInvariant(), out poss);
		}

		private static void UpdatePossibilityWithTraitValue(int ws, ICmPossibility poss, string traitValue, IDictionary<string, ICmPossibility> dict, ICollection<ICmPossibility> rgnewPoss)
		{
			poss.Abbreviation.set_String(ws, traitValue);
			poss.Name.set_String(ws, traitValue);
			dict.Add(traitValue, poss);
			rgnewPoss.Add(poss);
		}

		private ICmPossibility FindOrCreateDomainType(string traitValue)
		{
			var ws = m_cache.DefaultAnalWs;
			return FindOrCreatePossibility(traitValue, ws, CreateNewCmPossibility, m_dictDomainType, m_rgnewDomainType, m_cache.LangProject.LexDbOA.DomainTypesOA);
		}

		private ICmPossibility FindOrCreateSenseType(string traitValue)
		{
			var ws = m_cache.DefaultAnalWs;
			return FindOrCreatePossibility(traitValue, ws, CreateNewCmPossibility, m_dictSenseType, m_rgnewSenseType, m_cache.LangProject.LexDbOA.SenseTypesOA);
		}

		private ICmPossibility FindOrCreateStatus(string traitValue)
		{
			var ws = m_cache.DefaultAnalWs;
			return FindOrCreatePossibility(traitValue, ws, CreateNewCmPossibility, m_dictStatus, m_rgnewStatus, m_cache.LangProject.StatusOA);
		}

		private ICmPossibility FindOrCreateTranslationType(string sType)
		{
			// This method will now set the Abbreviation as well as the Name.
			// It only set the Name before.
			var ws = m_cache.DefaultAnalWs;
			return FindOrCreatePossibility(sType, ws, CreateNewCmPossibility, m_dictTransType, m_rgnewTransType, m_cache.LangProject.TranslationTagsOA);
		}

		private ICmPossibility FindOrCreateUsageType(string traitValue)
		{
			var ws = m_cache.DefaultAnalWs;
			return FindOrCreatePossibility(traitValue, ws, CreateNewCmPossibility, m_dictUsageType, m_rgnewUsageType, m_cache.LangProject.LexDbOA.UsageTypesOA);
		}

		private ICmPossibility FindOrCreateLanguagePossibility(string traitValue)
		{
			var ws = m_cache.DefaultAnalWs;
			return FindOrCreatePossibility(traitValue, ws, CreateNewCmPossibility, m_dictLanguage, m_rgnewLanguage, m_cache.LangProject.LexDbOA.LanguagesOA);
		}

		private ICmLocation FindOrCreateLocation(string traitValue)
		{
			var ws = m_cache.DefaultAnalWs;
			return FindOrCreatePossibility(traitValue, ws, CreateNewCmLocation, m_dictLocation, m_rgnewLocation, m_cache.LangProject.LocationsOA) as ICmLocation;
		}

		private ICmPossibility FindOrCreatePublicationType(string traitValue)
		{
			var ws = m_cache.DefaultAnalWs;
			return FindOrCreatePossibility(traitValue, ws, CreateNewCmPossibility, m_dictPublicationTypes, m_rgnewPublicationType, m_cache.LangProject.LexDbOA.PublicationTypesOA);
		}

		#endregion // Methods to find or create list items

		#region Methods for displaying list items created during import

		/// <summary>
		/// Summarize what we added to the known lists.
		/// </summary>
		public string DisplayNewListItems(string sLIFTFile, int cEntriesRead)
		{
			var sDir = Path.GetDirectoryName(sLIFTFile);
			var sLogFile = $"{Path.GetFileNameWithoutExtension(sLIFTFile)}-ImportLog.htm";
			var sHtmlFile = Path.Combine(sDir, sLogFile);
			var dtEnd = DateTime.Now;

			using (var writer = new StreamWriter(sHtmlFile, false, System.Text.Encoding.UTF8))
			{
				var sTitle = string.Format(LexTextControls.ksImportLogFor0, sLIFTFile);
				writer.WriteLine("<html>");
				writer.WriteLine("<head>");
				writer.WriteLine("<title>{0}</title>", sTitle);
				writer.WriteLine("</head>");
				writer.WriteLine("<body>");
				writer.WriteLine("<h2>{0}</h2>", sTitle);
				var deltaTicks = dtEnd.Ticks - m_dtStart.Ticks; // number of 100-nanosecond intervals
				var deltaMsec = (int)((deltaTicks + 5000L) / 10000L);   // round off to milliseconds
				var deltaSec = deltaMsec / 1000;
				var sDeltaTime = string.Format(LexTextControls.ksImportingTookTime, Path.GetFileName(sLIFTFile), deltaSec, deltaMsec % 1000);
				writer.WriteLine("<p>{0}</p>", sDeltaTime);
				var sEntryCounts = string.Format(LexTextControls.ksEntriesImportCounts, cEntriesRead, m_cEntriesAdded, m_cSensesAdded, m_cEntriesDeleted);
				writer.WriteLine("<p><h3>{0}</h3></p>", sEntryCounts);
				ListNewPossibilities(writer, LexTextControls.ksPartsOfSpeechAdded, m_rgnewPos);
				ListNewPossibilities(writer, LexTextControls.ksMorphTypesAdded, m_rgnewMmt);
				ListNewLexEntryTypes(writer, LexTextControls.ksComplexFormTypesAdded, m_rgnewComplexFormType);
				ListNewLexEntryTypes(writer, LexTextControls.ksVariantTypesAdded, m_rgnewVariantType);
				ListNewPossibilities(writer, LexTextControls.ksSemanticDomainsAdded, m_rgnewSemDom);
				ListNewPossibilities(writer, LexTextControls.ksTranslationTypesAdded, m_rgnewTransType);
				ListNewPossibilities(writer, LexTextControls.ksConditionsAdded, m_rgnewCondition);
				ListNewPossibilities(writer, LexTextControls.ksAnthropologyCodesAdded, m_rgnewAnthroCode);
				ListNewPossibilities(writer, LexTextControls.ksDialectsAdded, m_rgnewDialects);
				ListNewPossibilities(writer, LexTextControls.ksDomainTypesAdded, m_rgnewDomainType);
				ListNewPossibilities(writer, LexTextControls.ksSenseTypesAdded, m_rgnewSenseType);
				ListNewPossibilities(writer, LexTextControls.ksPeopleAdded, m_rgnewPerson);
				ListNewPossibilities(writer, LexTextControls.ksStatusValuesAdded, m_rgnewStatus);
				ListNewPossibilities(writer, LexTextControls.ksUsageTypesAdded, m_rgnewUsageType);
				ListNewPossibilities(writer, LexTextControls.ksLocationsAdded, m_rgnewLocation);
				ListNewPossibilities(writer, LexTextControls.ksLanguagesAdded, m_rgnewLanguage);
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

		private static void ListConflictsFound(StreamWriter writer, string sMsg, List<ConflictingData> list)
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
				foreach (var cd in list)
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

		private void ListInvalidData(StreamWriter writer)
		{
			if (m_rgInvalidData.Count == 0)
			{
				return;
			}
			writer.WriteLine("<table border=\"1\" width=\"100%\">");
			writer.WriteLine("<tbody>");
			writer.WriteLine("<caption><h3>{0}</h3></caption>", LexTextControls.ksInvalidDataImported);
			writer.WriteLine("<tr>");
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksEntry);
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksField);
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksInvalidValue);
			writer.WriteLine("<th width=\"49%\">{0}</th>", LexTextControls.ksErrorMessage);
			writer.WriteLine("</tr>");
			foreach (var bad in m_rgInvalidData)
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
			{
				return;
			}
			writer.WriteLine("<table border=\"1\" width=\"100%\">");
			writer.WriteLine("<tbody>");
			writer.WriteLine("<caption><h3>{0}</h3></caption>", LexTextControls.ksInvalidRelationsHeader);
			writer.WriteLine("<tr>");
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksEntry);
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksRelationType);
			writer.WriteLine("<th width=\"17%\">{0}</th>", LexTextControls.ksInvalidReference);
			writer.WriteLine("<th width=\"49%\">{0}</th>", LexTextControls.ksErrorMessage);
			writer.WriteLine("</tr>");
			foreach (var bad in m_rgInvalidRelation)
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
			if (m_combinedCollections.Count == 0)
			{
				return;
			}

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
			{
				return;
			}
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
				foreach (var msg in m_rgErrorMsgs)
				{
					writer.WriteLine("<li>{0}</li>", msg);
				}
				writer.WriteLine("</ul>");
			}
		}

		private static void ListNewLexEntryTypes(StreamWriter writer, string sMsg, List<ILexEntryType> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (var type in list)
				{
					writer.WriteLine("<li>{0} / {1}</li>", type.AbbrAndName, type.ReverseAbbr.BestAnalysisAlternative.Text);
				}
				writer.WriteLine("</ul>");
			}
		}

		private static void ListNewPossibilities(System.IO.StreamWriter writer, string sMsg, List<ICmPossibility> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (var poss in list)
				{
					writer.WriteLine("<li>{0}</li>", poss.AbbrAndName);
				}
				writer.WriteLine("</ul>");
			}
		}

		private static void ListNewEnvironments(StreamWriter writer, string sMsg, List<IPhEnvironment> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (var env in list)
				{
					writer.WriteLine("<li>{0}</li>", env.StringRepresentation.Text);
				}
				writer.WriteLine("</ul>");
			}
		}

		private static void ListNewWritingSystems(StreamWriter writer, string sMsg, List<CoreWritingSystemDefinition> list)
		{
			if (list.Count > 0)
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (var ws in list)
				{
					writer.WriteLine("<li>{0} ({1})</li>", ws.DisplayLabel, ws.Id);
				}
				writer.WriteLine("</ul>");
			}
		}
		private static void ListNewInflectionClasses(StreamWriter writer, string sMsg, List<IMoInflClass> list)
		{
			if (list.Any())
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (var infl in list)
				{
					var sPos = string.Empty;
					var cmo = infl.Owner;
					while (cmo != null && cmo.ClassID != PartOfSpeechTags.kClassId)
					{
						Debug.Assert(cmo.ClassID == MoInflClassTags.kClassId);
						if (cmo.ClassID == MoInflClassTags.kClassId)
						{
							var owner = cmo as IMoInflClass;
							sPos.Insert(0, $": {owner.Name.BestAnalysisVernacularAlternative.Text}");
						}
						cmo = cmo.Owner;
					}
					if (cmo != null)
					{
						var pos = cmo as IPartOfSpeech;
						sPos = sPos.Insert(0, pos.Name.BestAnalysisVernacularAlternative.Text);
					}
					writer.WriteLine("<li>{0}: {1}</li>", sPos, infl.Name.BestAnalysisVernacularAlternative.Text);
				}
				writer.WriteLine("</ul>");
			}
		}

		private static void ListNewSlots(StreamWriter writer, string sMsg, List<IMoInflAffixSlot> list)
		{
			if (list.Any())
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (var slot in list)
				{
					var sPos = string.Empty;
					var cmo = slot.Owner;
					if (cmo is IPartOfSpeech)
					{
						var pos = (IPartOfSpeech)cmo;
						sPos = pos.Name.BestAnalysisVernacularAlternative.Text;
					}
					writer.WriteLine("<li>{0}: {1}</li>", sPos, slot.Name.BestAnalysisVernacularAlternative.Text);
				}
				writer.WriteLine("</ul>");
			}
		}

		private static void ListNewInflectionalFeatures(StreamWriter writer, string sMsg, List<IFsFeatDefn> list)
		{
			if (list.Any())
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (var feat in list)
				{
					writer.WriteLine("<li>{0} - {1}</li>", feat.Abbreviation.BestAnalysisVernacularAlternative.Text, feat.Name.BestAnalysisVernacularAlternative.Text);
				}
				writer.WriteLine("</ul>");
			}
		}

		private static void ListNewFeatureTypes(StreamWriter writer, string sMsg, List<IFsFeatStrucType> list)
		{
			if (list.Any())
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (var type in list)
				{
					writer.WriteLine("<li>{0} - {1}</li>", type.Abbreviation.BestAnalysisVernacularAlternative.Text, type.Name.BestAnalysisVernacularAlternative.Text);
				}
				writer.WriteLine("</ul>");
			}
		}

		private static void ListNewStemNames(StreamWriter writer, string sMsg, List<IMoStemName> list)
		{
			if (list.Any())
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (var stem in list)
				{
					if (stem.Owner is IPartOfSpeech)
					{
						writer.WriteLine("<li>{0} ({1})</li>", stem.Name.BestAnalysisVernacularAlternative.Text, (stem.Owner as IPartOfSpeech).Name.BestAnalysisVernacularAlternative.Text);
					}
				}
				writer.WriteLine("</ul>");
			}
		}

		private void ListNewCustomFields(StreamWriter writer, string sMsg, List<FieldDescription> list)
		{
			if (list.Any())
			{
				writer.WriteLine("<p><h3>{0}</h3></p>", sMsg);
				writer.WriteLine("<ul>");
				foreach (var fd in list)
				{
					var sClass = m_cache.MetaDataCacheAccessor.GetClassName(fd.Class);
					writer.WriteLine("<li>{0}: {1}</li>", sClass, fd.Name);
				}
				writer.WriteLine("</ul>");
			}
		}

		#endregion // Methods for displaying list items created during import

		#region String matching, merging, extracting, etc.

		/// <summary>
		/// Merge in a form that may need to have morphtype markers stripped from it.
		/// forms is safe-XML
		/// </summary>
		private void MergeInAllomorphForms(LiftMultiText forms, ITsMultiString tsm, int clsidForm, Guid guidEntry, int flid)
		{
			// If we're keeping only the imported data, erase any existing data that isn't
			// overwritten by imported data.
			var multi = m_msImport == MergeStyle.MsKeepOnlyNew ? GetAllUnicodeAlternatives(tsm) : new Dictionary<int, string>();
			AddNewWsToVernacular();
			foreach (var key in forms.Keys)
			{
				var wsHvo = GetWsFromLiftLang(key);
				// LiftMultiText parameter may have come in with escaped characters which need to be
				// converted to plain text before merging with existing entries
				var form = XmlUtils.DecodeXmlAttribute(forms[key].Text);
				if (wsHvo > 0 && !string.IsNullOrEmpty(form))
				{
					multi.Remove(wsHvo);
					var fUpdate = false;
					if (!m_fCreatingNewEntry && m_msImport == MergeStyle.MsKeepOld)
					{
						var tssOld = tsm.get_String(wsHvo);
						if (tssOld == null || tssOld.Length == 0)
						{
							fUpdate = true;
						}
					}
					else
					{
						fUpdate = true;
					}
					if (fUpdate)
					{
						var sAllo = form;
						if (IsVoiceWritingSystem(wsHvo))
						{
							var sPath = Path.Combine(Path.GetDirectoryName(m_sLiftFile), "audio", form);
							CopyFileToLinkedFiles(form, sPath, LcmFileHelper.ksMediaDir);
						}
						else
						{
							sAllo = StripAlloForm(form, clsidForm, guidEntry, flid);
						}
						tsm.set_String(wsHvo, TsStringUtils.MakeString(sAllo, wsHvo));
					}
				}
			}
			foreach (var ws in multi.Keys)
				tsm.set_String(ws, null);
		}

		private string StripAlloForm(string form, int clsidForm, Guid guidEntry, int flid)
		{
			int clsid;
			// Strip any affix/clitic markers from the form before storing it.
			FindMorphType(ref form, out clsid, guidEntry, flid);
			if (clsidForm != 0 && clsid != clsidForm)
			{
				// complain about varying morph types??
			}
			return form;
		}

		/// <summary>
		/// Answer true if tsm already matches forms, in all the alternatives that would be set by MergeMultiString.
		/// </summary>
		private bool MatchMultiString(ITsMultiString tsm, LiftMultiText forms)
		{
			foreach (var key in forms.Keys)
			{
				var wsHvo = GetWsFromLiftLang(key);
				if (wsHvo <= 0)
				{
					continue;
				}
				var tssWanted = CreateTsStringFromLiftString(forms[key], wsHvo);
				var tssActual = tsm.get_String(wsHvo);
				if (!tssActual.Equals(tssWanted))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Merge in a Multi(Ts)String type value.
		/// </summary>
		private void MergeInMultiString(ITsMultiString tsm, int flid, LiftMultiText forms, Guid guidObj)
		{
			// If we're keeping only the imported data, erase any existing data that isn't
			// overwritten by imported data.
			var multi = m_msImport == MergeStyle.MsKeepOnlyNew ? GetAllTsStringAlternatives(tsm) : new Dictionary<int, ITsString>();
			if (forms?.Keys != null)
			{
				foreach (var key in forms.Keys)
				{
					var wsHvo = GetWsFromLiftLang(key);
					if (wsHvo > 0)
					{
						multi.Remove(wsHvo);
						if (!m_fCreatingNewEntry && !m_fCreatingNewSense && m_msImport == MergeStyle.MsKeepOld)
						{
							var tssOld = tsm.get_String(wsHvo);
							if (tssOld != null && tssOld.Length != 0)
							{
								continue;
							}
						}
						var tss = CreateTsStringFromLiftString(forms[key], wsHvo);
						tsm.set_String(wsHvo, tss);
						if (tss.RunCount == 1 && IsVoiceWritingSystem(wsHvo))
						{
							var sPath = Path.Combine(Path.GetDirectoryName(m_sLiftFile), "audio", tss.Text);
							CopyFileToLinkedFiles(tss.Text, sPath, LcmFileHelper.ksMediaDir);
						}
					}
				}
			}
			foreach (var ws in multi.Keys)
			{
				tsm.set_String(ws, null);
			}
		}

		/// <summary>
		/// Make a TsString from the (safe-XML) input LiftString
		/// </summary>
		private ITsString CreateTsStringFromLiftString(LiftString liftstr, int wsHvo)
		{
			var tsb = TsStringUtils.MakeStrBldr();
			// LiftString parameter may have come in with escaped characters which need to be
			// converted to plain text before comparing with existing entries
			var convertSafeXmlToText = XmlUtils.DecodeXmlAttribute(liftstr.Text);
			tsb.Replace(0, tsb.Length, convertSafeXmlToText, TsStringUtils.MakeProps(null, wsHvo));
			// TODO: handle nested spans.
			foreach (var span in liftstr.Spans)
			{
				var tpb = TsStringUtils.MakePropsBldr();
				var wsSpan = string.IsNullOrEmpty(span.Lang) ? wsHvo : GetWsFromLiftLang(span.Lang);
				tpb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsSpan);
				if (!string.IsNullOrEmpty(span.Class))
				{
					tpb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, span.Class);
				}
				if (!string.IsNullOrEmpty(span.LinkURL))
				{
					var linkPath = FileUtils.StripFilePrefix(span.LinkURL);
					if (MiscUtils.IsUnix)
					{
						linkPath = linkPath.TrimStart('/');
					}
					var sPath = Path.Combine(Path.GetDirectoryName(m_sLiftFile), linkPath);
					if (linkPath.StartsWith("others" + '/') || linkPath.StartsWith("others" + "\\") || linkPath.StartsWith("others" + Path.DirectorySeparatorChar))
					{
						linkPath = CopyFileToLinkedFiles(linkPath.Substring("others/".Length), sPath, LcmFileHelper.ksOtherLinkedFilesDir);
					}
					var chOdt = Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName);
					var sRef = chOdt + linkPath;
					tpb.SetStrPropValue((int)FwTextPropType.ktptObjData, sRef);
				}
				tsb.SetProperties(span.Index, span.Index + span.Length, tpb.GetTextProps());
			}
			return tsb.GetString();
		}

		/// <summary>
		/// Merge in a MultiUnicode type value.
		/// </summary>
		private void MergeInMultiUnicode(ITsMultiString tsm, int flid, LiftMultiText forms, Guid guidObj)
		{
			// If we're keeping only the imported data, erase any existing data that isn't
			// overwritten by imported data.
			var multi = m_msImport == MergeStyle.MsKeepOnlyNew ? GetAllUnicodeAlternatives(tsm) : new Dictionary<int, string>();
			if (forms?.Keys != null)
			{
				foreach (var key in forms.Keys)
				{
					var wsHvo = GetWsFromLiftLang(key);
					if (wsHvo > 0)
					{
						multi.Remove(wsHvo);
						// LiftMultiText parameter may have come in with escaped characters which need to be
						// converted to plain text before merging with existing entries
						var sText = XmlUtils.DecodeXmlAttribute(forms[key].Text);
						if (!m_fCreatingNewEntry && !m_fCreatingNewSense && m_msImport == MergeStyle.MsKeepOld)
						{
							var tss = tsm.get_String(wsHvo);
							if (tss == null || tss.Length == 0)
							{
								tsm.set_String(wsHvo, TsStringUtils.MakeString(sText, wsHvo));
							}
						}
						else
						{
							tsm.set_String(wsHvo, TsStringUtils.MakeString(sText, wsHvo));
						}
					}
				}
			}

			foreach (var ws in multi.Keys)
			{
				tsm.set_String(ws, null);
			}
		}

		Dictionary<string, int> m_mapLangWs = new Dictionary<string, int>();
		private bool m_fAddNewWsToVern;
		private bool m_fAddNewWsToAnal;

		private void AddNewWsToAnalysis()
		{
			m_fAddNewWsToVern = false;
			m_fAddNewWsToAnal = true;
		}

		private void AddNewWsToVernacular()
		{
			m_fAddNewWsToVern = true;
			m_fAddNewWsToAnal = false;
		}

		private void AddNewWsToBothVernAnal()
		{
			m_fAddNewWsToVern = true;
			m_fAddNewWsToAnal = true;
		}

		private void IgnoreNewWs()
		{
			m_fAddNewWsToVern = false;
			m_fAddNewWsToAnal = false;
		}

		public int GetWsFromLiftLang(string key)
		{
			int hvo;
			if (m_mapLangWs.TryGetValue(key, out hvo))
			{
				return hvo;
			}
			CoreWritingSystemDefinition ws;
			if (!WritingSystemServices.FindOrCreateSomeWritingSystem(m_cache, FwDirectoryFinder.TemplateDirectory, key, m_fAddNewWsToAnal, m_fAddNewWsToVern, out ws))
			{
				m_addedWss.Add(ws);
				// Use the LDML file if it's available.  Look in the current location first, then look
				// in the old location.
				var ldmlFile = Path.Combine(Path.Combine(m_sLiftDir, "WritingSystems"), key + ".ldml");
				if (!File.Exists(ldmlFile))
				{
					ldmlFile = Path.Combine(m_sLiftDir, key + ".ldml");
				}
				if (File.Exists(ldmlFile) && key == ws.Id)
				{
					var id = ws.Id;
					var adaptor = new LdmlDataMapper(new WritingSystemFactory());
					adaptor.Read(ldmlFile, ws);
					ws.Id = id;
					ws.ForceChanged();
				}
			}
			m_mapLangWs.Add(key, ws.Handle);
			// If FindOrCreate had to get creative, the WS ID may not match the input identifier. We want both the
			// original and actual keys in the map.
			if (!m_mapLangWs.ContainsKey(ws.Id))
			{
				m_mapLangWs.Add(ws.Id, ws.Handle);
			}
			return ws.Handle;
		}

		/// <summary>
		/// Both arguments must be in safe-XML form
		/// </summary>
		private void MergeLiftMultiTexts(LiftMultiText mtCurrent, LiftMultiText mtNew)
		{
			foreach (var key in mtNew.Keys)
			{
				if (mtCurrent.ContainsKey(key))
				{
					if (m_fCreatingNewEntry || m_fCreatingNewSense || m_msImport != MergeStyle.MsKeepOld)
					{
						mtCurrent.Add(key, mtNew[key]);
					}
				}
				else
				{
					mtCurrent.Add(key, mtNew[key]);
				}
			}
		}

		/// <summary>
		/// Make a TsString out of the first value in the (safe-XML) contents
		/// </summary>
		private ITsString GetFirstLiftTsString(LiftMultiText contents)
		{
			if (contents != null && !contents.IsEmpty)
			{
				var ws = GetWsFromLiftLang(contents.FirstValue.Key);
				return CreateTsStringFromLiftString(contents.FirstValue.Value, ws);
			}
			return null;
		}

		private void SetStringsFromLiftContents(LiftMultiText contents, ITsMultiString destination)
		{
			if (contents != null && !contents.IsEmpty)
			{
				foreach (var keyValuePair in contents)
				{
					var ws = GetWsFromLiftLang(keyValuePair.Key);
					destination.set_String(ws, CreateTsStringFromLiftString(keyValuePair.Value, ws));
				}
			}
		}

		HashSet<Tuple<string, string, string, ICmObject>> m_reportedMergeProblems = new HashSet<Tuple<string, string, string, ICmObject>>();

		/// <summary>
		/// Fill in any missing alternatives from the LIFT data.
		/// </summary>
		private void MergeStringsFromLiftContents(LiftMultiText contents, ITsMultiString destination, string attr, ICmObject obj)
		{
			if (contents != null && !contents.IsEmpty)
			{
				foreach (var keyValuePair in contents)
				{
					var ws = GetWsFromLiftLang(keyValuePair.Key);
					var liftString = CreateTsStringFromLiftString(keyValuePair.Value, ws);
					var ourString = destination.get_String(ws);
					if (string.IsNullOrEmpty(ourString.Text))
					{
						destination.set_String(ws, liftString);
					}
					else if (liftString.Text.Normalize() != ourString.Text.Normalize()) // ignore the very unlikely case of more subtle differences we can't report
					{
						// Fairly typically this is called more than once on the same object...not quite sure why. Simplest thing is not to make
						// the same report repeatedly.
						var key = Tuple.Create(liftString.Text, ourString.Text, attr, obj);
						if (!m_reportedMergeProblems.Contains(key))
						{
							m_reportedMergeProblems.Add(key);
							m_rgErrorMsgs.Add(string.Format(LexTextControls.ksNonMatchingRelation, liftString.Text, ourString.Text, attr, obj.ShortName));
						}
					}
				}
			}
		}

		public bool TsStringIsNullOrEmpty(ITsString tss)
		{
			return tss == null || tss.Length == 0;
		}

		private bool StringsConflict(string sOld, string sNew)
		{
			if (string.IsNullOrEmpty(sOld))
			{
				return false;
			}

			if (sNew == null)
			{
				return false;
			}
			var sNewNorm = Icu.Normalize(sNew, Icu.UNormalizationMode.UNORM_NFD);
			var sOldNorm = Icu.Normalize(sOld, Icu.UNormalizationMode.UNORM_NFD);
			return sNewNorm != sOldNorm;
		}

		private bool StringsConflict(ITsString tssOld, ITsString tssNew)
		{
			if (TsStringIsNullOrEmpty(tssOld))
			{
				return false;
			}

			if (tssNew == null)
			{
				return false;
			}
			var tssNewNorm = tssNew.get_NormalizedForm(FwNormalizationMode.knmNFD);
			var tssOldNorm = tssOld.get_NormalizedForm(FwNormalizationMode.knmNFD);
			return !tssOldNorm.Equals(tssNewNorm);
		}

		/// <summary />
		private bool MultiUnicodeStringsConflict(ITsMultiString tsm, LiftMultiText lmt, bool fStripMarkers, Guid guidEntry, int flid)
		{
			if (tsm == null || lmt == null || lmt.IsEmpty)
			{
				return false;
			}
			foreach (var key in lmt.Keys)
			{
				var wsHvo = GetWsFromLiftLang(key);
				if (wsHvo < 0)
				{
					continue;       // Should never happen!
				}
				var sNew = XmlUtils.DecodeXmlAttribute(lmt[key].Text);
				if (fStripMarkers)
				{
					sNew = StripAlloForm(sNew, 0, guidEntry, flid);
				}
				var tssOld = tsm.get_String(wsHvo);
				if (tssOld == null || tssOld.Length == 0)
				{
					continue;
				}
				var sOld = tssOld.Text;
				var sNewNorm = Icu.Normalize(sNew, Icu.UNormalizationMode.UNORM_NFD);
				var sOldNorm = Icu.Normalize(sOld, Icu.UNormalizationMode.UNORM_NFD);
				if (sNewNorm != sOldNorm)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary />
		private bool MultiTsStringsConflict(ITsMultiString tsm, LiftMultiText lmt)
		{
			if (tsm == null || lmt == null || lmt.IsEmpty)
			{
				return false;
			}
			foreach (var key in lmt.Keys)
			{
				var wsHvo = GetWsFromLiftLang(key);
				if (wsHvo < 0)
				{
					continue;       // Should never happen!
				}
				var tssNew = CreateTsStringFromLiftString(lmt[key], wsHvo);
				var tss = tsm.get_String(wsHvo);
				if (tss == null || tss.Length == 0)
				{
					continue;
				}
				var tssOld = tss;
				var tssOldNorm = tssOld.get_NormalizedForm(FwNormalizationMode.knmNFD);
				var tssNewNorm = tssNew.get_NormalizedForm(FwNormalizationMode.knmNFD);
				if (!tssOldNorm.Equals(tssNewNorm))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary />
		private int MultiUnicodeStringMatches(ITsMultiString tsm, LiftMultiText lmt, bool fStripMarkers, Guid guidEntry, int flid)
		{
			if (tsm == null && (lmt == null || lmt.IsEmpty))
			{
				return 1;
			}
			if (tsm == null || lmt == null || lmt.IsEmpty)
			{
				return 0;
			}
			var cMatches = 0;
			foreach (var key in lmt.Keys)
			{
				var wsHvo = GetWsFromLiftLang(key);
				if (wsHvo < 0)
				{
					continue;       // Should never happen!
				}
				var tssOld = tsm.get_String(wsHvo);
				if (tssOld == null || tssOld.Length == 0)
				{
					continue;
				}
				var sOld = tssOld.Text;
				var sNew = XmlUtils.DecodeXmlAttribute(lmt[key].Text);
				if (fStripMarkers)
				{
					sNew = StripAlloForm(sNew, 0, guidEntry, flid);
				}
				var sNewNorm = Icu.Normalize(sNew, Icu.UNormalizationMode.UNORM_NFD);
				var sOldNorm = Icu.Normalize(sOld, Icu.UNormalizationMode.UNORM_NFD);
				if (sNewNorm == sOldNorm)
				{
					++cMatches;
				}
			}
			return cMatches;
		}

		/// <summary />
		private int MultiTsStringMatches(ITsMultiString tsm, LiftMultiText lmt)
		{
			if (tsm == null && (lmt == null || lmt.IsEmpty))
			{
				return 1;
			}

			if (tsm == null || lmt == null || lmt.IsEmpty)
			{
				return 0;
			}
			var cMatches = 0;
			foreach (var key in lmt.Keys)
			{
				var wsHvo = GetWsFromLiftLang(key);
				if (wsHvo < 0)
				{
					continue;
				}
				var tss = tsm.get_String(wsHvo);
				if (tss == null || tss.Length == 0)
				{
					continue;
				}
				var tssOld = tss;
				var tssNew = CreateTsStringFromLiftString(lmt[key], wsHvo);
				var tssOldNorm = tssOld.get_NormalizedForm(FwNormalizationMode.knmNFD);
				var tssNewNorm = tssNew.get_NormalizedForm(FwNormalizationMode.knmNFD);
				if (tssOldNorm.Equals(tssNewNorm))
				{
					++cMatches;
				}
			}
			return cMatches;
		}

		/// <summary />
		private bool SameMultiUnicodeContent(LiftMultiText contents, ITsMultiString tsm)
		{
			foreach (var key in contents.Keys)
			{
				var ws = GetWsFromLiftLang(key);
				var sNew = contents[key].Text;
				var tssOld = tsm.get_String(ws);
				if (string.IsNullOrEmpty(sNew) && (tssOld == null || tssOld.Length == 0))
				{
					continue;
				}

				if (string.IsNullOrEmpty(sNew) || (tssOld == null || tssOld.Length == 0))
				{
					return false;
				}
				// LiftMultiText parameter may have come in with escaped characters which need to be
				// converted to plain text before comparing with existing entries
				var sNewNorm = XmlUtils.DecodeXmlAttribute(Icu.Normalize(sNew, Icu.UNormalizationMode.UNORM_NFD));
				var sOldNorm = Icu.Normalize(tssOld.Text, Icu.UNormalizationMode.UNORM_NFD);
				if (sNewNorm != sOldNorm)
				{
					return false;
				}
			}
			// TODO: check whether all strings in mua are found in contents?
			return true;
		}

		/// <summary>
		/// Check whether any of the given unicode values match in any of the writing
		/// systems.
		/// </summary>
		private static bool HasMatchingUnicodeAlternative(string sVal, ITsMultiString tsmAbbr, ITsMultiString tsmName)
		{
			int ws;
			if (tsmAbbr != null)
			{
				for (var i = 0; i < tsmAbbr.StringCount; ++i)
				{
					var tss = tsmAbbr.GetStringFromIndex(i, out ws);
					// TODO: try tss.Text.Equals(sVal, StringComparison.InvariantCultureIgnoreCase)
					if (tss.Length > 0 && tss.Text.ToLowerInvariant() == sVal)
					{
						return true;
					}
				}
			}
			if (tsmName != null)
			{
				for (var i = 0; i < tsmName.StringCount; ++i)
				{
					var tss = tsmName.GetStringFromIndex(i, out ws);
					// TODO: try tss.Text.Equals(sVal, StringComparison.InvariantCultureIgnoreCase)
					if (tss.Length > 0 && tss.Text.ToLowerInvariant() == sVal)
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Write the string as HTML, interpreting the string properties as best we can.
		/// </summary>
		public string TsStringAsHtml(ITsString tss, LcmCache cache)
		{
			var sb = new StringBuilder();
			var crun = tss.RunCount;
			for (var irun = 0; irun < crun; ++irun)
			{
				var iMin = tss.get_MinOfRun(irun);
				var iLim = tss.get_LimOfRun(irun);
				string sLang = null;
				string sDir = null;
				string sFont = null;
				var ttp = tss.get_Properties(irun);
				int nVar;
				var ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
				if (ws > 0)
				{
					var wsObj = GetExistingWritingSystem(ws);
					sLang = wsObj.Id;
					sDir = wsObj.RightToLeftScript ? "RTL" : "LTR";
					sFont = wsObj.DefaultFontName;
				}
				var nSuperscript = ttp.GetIntPropValues((int)FwTextPropType.ktptSuperscript, out nVar);
				switch (nSuperscript)
				{
					case (int)FwSuperscriptVal.kssvSuper:
						sb.Append("<sup");
						break;
					case (int)FwSuperscriptVal.kssvSub:
						sb.Append("<sub");
						break;
					default:
						sb.Append("<span");
						break;
				}

				if (!string.IsNullOrEmpty(sLang))
				{
					sb.AppendFormat(" lang=\"{0}\"", sLang);
				}
				if (!string.IsNullOrEmpty(sDir))
				{
					sb.AppendFormat(" dir=\"{0}\"", sDir);
				}
				if (!string.IsNullOrEmpty(sFont))
				{
					sb.AppendFormat(" style=\"font-family: '{0}', serif\"", sFont);
				}
				sb.Append(">");
				sb.Append(tss.Text.Substring(iMin, iLim - iMin));
				switch (nSuperscript)
				{
					case (int)FwSuperscriptVal.kssvSuper:
						sb.Append("</sup>");
						break;
					case (int)FwSuperscriptVal.kssvSub:
						sb.Append("</sub>");
						break;
					default:
						sb.Append("</span>");
						break;
				}
			}
			return sb.ToString();
		}

		#endregion // String matching, merging, extracting, etc.

		#region Storing LIFT import residue...

		private XmlDocument FindOrCreateResidue(ICmObject cmo, string sId, int flid)
		{
			LiftResidue res;
			if (!m_dictResidue.TryGetValue(cmo.Hvo, out res))
			{
				res = CreateLiftResidue(cmo.Hvo, flid, sId);
				m_dictResidue.Add(cmo.Hvo, res);
			}
			else if (!string.IsNullOrEmpty(sId))
			{
				EnsureIdSet(res.Document.FirstChild, sId);
			}
			return res.Document;
		}

		/// <summary>
		/// This creates a new LiftResidue object with an empty XML document (empty except for
		/// the enclosing &lt;lift-residue&gt; element, that is).
		/// As a side-effect, it moves any existing LIFT residue for LexEntry or LexSense from
		/// ImportResidue to LiftResidue.
		/// </summary>
		private LiftResidue CreateLiftResidue(int hvo, int flid, string sId)
		{
			// The next four lines move any existing LIFT residue from ImportResidue to LiftResidue.
			switch (flid)
			{
				case LexEntryTags.kflidLiftResidue:
					ExtractLIFTResidue(m_cache, hvo, LexEntryTags.kflidImportResidue, flid);
					break;
				case LexSenseTags.kflidLiftResidue:
					ExtractLIFTResidue(m_cache, hvo, LexSenseTags.kflidImportResidue, flid);
					break;
			}
			var sResidue = string.IsNullOrEmpty(sId) ? "<lift-residue></lift-residue>" : $"<lift-residue id=\"{XmlUtils.MakeSafeXmlAttribute(sId)}\"></lift-residue>";
			var xd = new XmlDocument { PreserveWhitespace = true };
			xd.LoadXml(sResidue);
			return new LiftResidue(flid, xd);
		}

		private static void EnsureIdSet(XmlNode xn, string sId)
		{
			var xa = xn.Attributes["id"];
			if (xa == null)
			{
				xa = xn.OwnerDocument.CreateAttribute("id");
				xa.Value = XmlUtils.MakeSafeXmlAttribute(sId);
				xn.Attributes.Append(xa);
			}
			else if (string.IsNullOrEmpty(xa.Value))
			{
				xa.Value = XmlUtils.MakeSafeXmlAttribute(sId);
			}
		}

		/// <summary>
		/// Scan ImportResidue for XML looking string inserted by LIFT import.  If any is found,
		/// move it from ImportResidue to LiftResidue.
		/// </summary>
		/// <returns>string containing any LIFT import residue found in ImportResidue</returns>
		private static string ExtractLIFTResidue(LcmCache cache, int hvo, int flidImportResidue, int flidLiftResidue)
		{
			Debug.Assert(flidLiftResidue != 0);
			var tssImportResidue = cache.MainCacheAccessor.get_StringProp(hvo, flidImportResidue);
			var sImportResidue = tssImportResidue == null ? null : tssImportResidue.Text;
			if (string.IsNullOrEmpty(sImportResidue))
			{
				return null;
			}
			if (sImportResidue.Length < 13)
			{
				return null;
			}
			var idx = sImportResidue.IndexOf("<lift-residue");
			if (idx >= 0)
			{
				var sLiftResidue = sImportResidue.Substring(idx);
				var idx2 = sLiftResidue.IndexOf("</lift-residue>");
				if (idx2 >= 0)
				{
					idx2 += 15;
					if (sLiftResidue.Length > idx2)
					{
						sLiftResidue = sLiftResidue.Substring(0, idx2);
					}
				}
				var cch = sLiftResidue.Length;
				cache.MainCacheAccessor.set_UnicodeProp(hvo, flidImportResidue, sImportResidue.Remove(idx, cch));
				cache.MainCacheAccessor.set_UnicodeProp(hvo, flidLiftResidue, sLiftResidue);
				return sLiftResidue;
			}
			return null;
		}

		private void StoreFieldAsResidue(ICmObject extensible, LiftField field)
		{
			var xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				var sXml = CreateXmlForField(field);
				InsertResidueContent(xdResidue, sXml);
			}
			else
			{
				Debug.WriteLine($"Need LiftResidue for {extensible.GetType().Name}: <field type='{field.Type}'>");
			}
		}

		private XmlDocument FindOrCreateResidue(ICmObject extensible)
		{
			// chaining if..else if instead of switch deals easier with matching superclasses.
			if (extensible is ILexEntry)
			{
				return FindOrCreateResidue(extensible, null, LexEntryTags.kflidLiftResidue);
			}

			if (extensible is ILexSense)
			{
				return FindOrCreateResidue(extensible, null, LexSenseTags.kflidLiftResidue);
			}

			if (extensible is ILexEtymology)
			{
				return FindOrCreateResidue(extensible, null, LexEtymologyTags.kflidLiftResidue);
			}

			if (extensible is ILexExampleSentence)
			{
				return FindOrCreateResidue(extensible, null, LexExampleSentenceTags.kflidLiftResidue);
			}

			if (extensible is ILexPronunciation)
			{
				return FindOrCreateResidue(extensible, null, LexPronunciationTags.kflidLiftResidue);
			}

			if (extensible is ILexReference)
			{
				return FindOrCreateResidue(extensible, null, LexReferenceTags.kflidLiftResidue);
			}

			if (extensible is IMoForm)
			{
				return FindOrCreateResidue(extensible, null, MoFormTags.kflidLiftResidue);
			}

			return extensible is IMoMorphSynAnalysis ? FindOrCreateResidue(extensible, null, MoMorphSynAnalysisTags.kflidLiftResidue) : null;
		}

		/// <summary />
		private string CreateXmlForField(LiftField field)
		{
			var bldr = new StringBuilder();
			bldr.AppendFormat("<field type=\"{0}\"", field.Type);
			AppendXmlDateAttributes(bldr, field.DateCreated, field.DateModified);
			bldr.AppendLine(">");
			AppendXmlForMultiText(bldr, field.Content, "form");
			foreach (var trait in field.Traits)
			{
				bldr.Append(CreateXmlForTrait(trait));
			}
			bldr.AppendLine("</field>");
			return bldr.ToString();
		}

		/// <summary>
		/// Append content to bldr
		/// </summary>
		private void AppendXmlForMultiText(StringBuilder bldr, LiftMultiText content, string tagXml)
		{
			if (content == null)
			{
				return; // probably shouldn't happen in a fully functional system, but...
			}
			foreach (string lang in content.Keys)
			{
				var str = content[lang]; // safe XML
				bldr.AppendFormat("<{0} lang=\"{1}\"><text>", tagXml, lang);
				var idxPrev = 0;
				foreach (var span in str.Spans)
				{
					if (idxPrev < span.Index)
					{
						bldr.Append(str.Text.Substring(idxPrev, span.Index - idxPrev));
					}
					// TODO: handle nested spans.
					var fSpan = AppendSpanElementIfNeeded(bldr, span, lang);
					bldr.Append(str.Text.Substring(span.Index, span.Length));
					if (fSpan)
					{
						bldr.Append("</span>");
					}
					idxPrev = span.Index + span.Length;
				}

				if (idxPrev < str.Text.Length)
				{
					bldr.Append(str.Text.Substring(idxPrev, str.Text.Length - idxPrev));
				}
				bldr.AppendFormat("</text></{0}>", tagXml);
				bldr.AppendLine();
			}
		}

		/// <summary>
		/// Lift import may contain tabs, newlines, or characters that need escaping to be valid XML.
		/// newlines are converted to \u2028, tabs to spaces.
		/// </summary>
		private static string ConvertToSafeFieldXmlContent(string input)
		{
			return XmlUtils.ConvertMultiparagraphToSafeXml(input.Replace('\t', ' '));
		}

		private static bool AppendSpanElementIfNeeded(StringBuilder bldr, LiftSpan span, string lang)
		{
			var fSpan = false;
			if (!string.IsNullOrEmpty(span.Class))
			{
				bldr.AppendFormat("<span class=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(span.Class));
				fSpan = true;
			}
			if (!string.IsNullOrEmpty(span.LinkURL))
			{
				if (!fSpan)
				{
					bldr.Append("<span");
					fSpan = true;
				}
				bldr.AppendFormat(" href=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(span.LinkURL));
			}
			if (!string.IsNullOrEmpty(span.Lang) && span.Lang != lang)
			{
				if (!fSpan)
				{
					bldr.Append("<span");
					fSpan = true;
				}
				bldr.AppendFormat(" lang=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(span.Lang));
			}

			if (fSpan)
			{
				bldr.Append(">");
			}
			return fSpan;
		}

		private void StoreNoteAsResidue(ICmObject extensible, CmLiftNote note)
		{
			var xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				var sXml = CreateXmlForNote(note);
				InsertResidueContent(xdResidue, sXml);
			}
			else
			{
				Debug.WriteLine($"Need LiftResidue for {extensible.GetType().Name}: <note type='{note.Type}'>");
			}
		}

		private string CreateXmlForNote(CmLiftNote note)
		{
			var bldr = new StringBuilder();
			bldr.Append("<note");
			if (!string.IsNullOrEmpty(note.Type))
			{
				bldr.AppendFormat(" type=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(note.Type));
			}
			AppendXmlDateAttributes(bldr, note.DateCreated, note.DateModified);
			bldr.AppendLine(">");
			AppendXmlForMultiText(bldr, note.Content, "form");
			foreach (var field in note.Fields)
			{
				bldr.Append(CreateXmlForField(field));
			}
			foreach (var trait in note.Traits)
			{
				bldr.Append(CreateXmlForTrait(trait));
			}
			bldr.AppendLine("</note>");
			return bldr.ToString();
		}

		/// <summary />
		private string CreateXmlForTrait(LiftTrait trait)
		{
			var bldr = new StringBuilder();
			bldr.AppendFormat("<trait name=\"{0}\" value=\"{1}\"", XmlUtils.MakeSafeXmlAttribute(trait.Name), XmlUtils.MakeSafeXmlAttribute(trait.Value));
			if (!string.IsNullOrEmpty(trait.Id))
			{
				bldr.AppendFormat(" id=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(trait.Id));
			}
			if (trait.Annotations != null && trait.Annotations.Count > 0)
			{
				bldr.AppendLine(">");
				foreach (var ann in trait.Annotations)
				{
					bldr.Append(CreateXmlForAnnotation(ann));
				}
				bldr.AppendLine("</trait>");
			}
			else
			{
				bldr.AppendLine("/>");
			}
			return bldr.ToString();
		}

		/// <summary>
		/// Return an XML string (content safe)
		/// </summary>
		private string CreateXmlForAnnotation(LiftAnnotation ann)
		{
			var bldr = new StringBuilder();
			bldr.AppendFormat("<annotation name=\"{0}\" value=\"{1}\"", ann.Name, ann.Value);
			if (!string.IsNullOrEmpty(ann.Who))
			{
				bldr.AppendFormat(" who=\"{0}\"", ann.Who);
			}
			var when = ann.When;
			if (IsDateSet(when))
			{
				bldr.AppendFormat(" when=\"{0}\"", when.ToUniversalTime().ToString(LiftDateTimeFormat));
			}
			bldr.AppendLine(">");
			AppendXmlForMultiText(bldr, ann.Comment, "form");
			bldr.AppendLine("</annotation>");
			return bldr.ToString();
		}

		private string CreateXmlForPhonetic(CmLiftPhonetic phon)
		{
			var bldr = new StringBuilder();
			bldr.Append("<pronunciation");
			AppendXmlDateAttributes(bldr, phon.DateCreated, phon.DateModified);
			bldr.AppendLine(">");
			AppendXmlForMultiText(bldr, phon.Form, "form");
			foreach (var url in phon.Media)
			{
				bldr.Append(CreateXmlForUrlRef(url, "media"));
			}
			foreach (var field in phon.Fields)
			{
				bldr.Append(CreateXmlForField(field));
			}
			foreach (var trait in phon.Traits)
			{
				bldr.Append(CreateXmlForTrait(trait));
			}
			foreach (var ann in phon.Annotations)
			{
				bldr.Append(CreateXmlForAnnotation(ann));
			}
			bldr.AppendLine("</pronunciation>");
			return bldr.ToString();
		}

		/// <summary />
		private void AppendXmlDateAttributes(StringBuilder bldr, DateTime created, DateTime modified)
		{
			if (IsDateSet(created))
			{
				bldr.AppendFormat(" dateCreated=\"{0}\"", ConvertToSafeFieldXmlContent(created.ToUniversalTime().ToString(LiftDateTimeFormat)));
			}
			if (IsDateSet(modified))
			{
				bldr.AppendFormat(" dateModified=\"{0}\"", ConvertToSafeFieldXmlContent(modified.ToUniversalTime().ToString(LiftDateTimeFormat)));
			}
		}

		private string CreateXmlForUrlRef(LiftUrlRef url, string tag)
		{
			var bldr = new StringBuilder();
			bldr.AppendFormat("<{0} href=\"{1}\">", tag, url.Url);
			bldr.AppendLine();
			AppendXmlForMultiText(bldr, url.Label, "form");
			bldr.AppendFormat("</{0}>", tag);
			bldr.AppendLine();
			return bldr.ToString();
		}

		private string CreateXmlForRelation(CmLiftRelation rel)
		{
			var bldr = new StringBuilder();
			bldr.AppendFormat("<relation type=\"{0}\" ref=\"{1}\"", rel.Type, rel.Ref);
			if (rel.Order >= 0)
			{
				bldr.AppendFormat(" order=\"{0}\"", rel.Order);
			}
			AppendXmlDateAttributes(bldr, rel.DateCreated, rel.DateModified);
			bldr.AppendLine(">");
			AppendXmlForMultiText(bldr, rel.Usage, "usage");
			foreach (var field in rel.Fields)
			{
				bldr.Append(CreateXmlForField(field));
			}
			foreach (var trait in rel.Traits)
			{
				bldr.Append(CreateXmlForTrait(trait));
			}
			foreach (var ann in rel.Annotations)
			{
				bldr.Append(CreateXmlForAnnotation(ann));
			}
			bldr.AppendLine("</relation>");
			return bldr.ToString();
		}

		private string CreateRelationResidue(CmLiftRelation rel)
		{
			if (rel.Usage != null || rel.Fields.Count > 0 || rel.Traits.Count > 0 || rel.Annotations.Count > 0)
			{
				var bldr = new StringBuilder();
				AppendXmlForMultiText(bldr, rel.Usage, "usage");
				foreach (var field in rel.Fields)
				{
					bldr.Append(CreateXmlForField(field));
				}
				foreach (var trait in rel.Traits)
				{
					bldr.Append(CreateXmlForTrait(trait));
				}
				foreach (var ann in rel.Annotations)
				{
					bldr.Append(CreateXmlForAnnotation(ann));
				}
				return bldr.ToString();
			}
			return null;
		}

		private void StoreTraitAsResidue(ICmObject extensible, LiftTrait trait)
		{
			var xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				var sXml = CreateXmlForTrait(trait);
				InsertResidueContent(xdResidue, sXml);
			}
			else
			{
				Debug.WriteLine($"Need LiftResidue for {extensible.GetType().Name}: <trait name='{trait.Name}' value='{trait.Value}'>");
			}
		}

		private void StoreResidue(ICmObject extensible, List<string> rgsResidue)
		{
			if (rgsResidue.Count == 0)
			{
				return;
			}
			var xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				foreach (var sXml in rgsResidue)
				{
					InsertResidueContent(xdResidue, sXml);
				}
			}
			else
			{
				Debug.WriteLine($"Need LiftResidue for {extensible.GetType().Name}: {rgsResidue[0]}...");
			}
		}

		private void StoreResidue(ICmObject extensible, string sResidueXml)
		{
			if (string.IsNullOrEmpty(sResidueXml))
			{
				return;
			}
			var xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				InsertResidueContent(xdResidue, sResidueXml);
			}
			else
			{
				Debug.WriteLine($"Need LiftResidue for {extensible.GetType().Name}: {sResidueXml}");
			}
		}

		private void StoreResidueFromVariant(ICmObject extensible, CmLiftVariant var)
		{
			var xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				// traits have already been handled.
				InsertResidueAttribute(xdResidue, "ref", var.Ref);
				StoreDatesInResidue(extensible, var);
				foreach (var ann in var.Annotations)
				{
					var sXml = CreateXmlForAnnotation(ann);
					InsertResidueContent(xdResidue, sXml);
				}
				foreach (var phon in var.Pronunciations)
				{
					var sXml = CreateXmlForPhonetic(phon);
					InsertResidueContent(xdResidue, sXml);
				}
				foreach (var rel in var.Relations)
				{
					var sXml = CreateXmlForRelation(rel);
					InsertResidueContent(xdResidue, sXml);
				}
				if (!string.IsNullOrEmpty(var.RawXml) &&
					string.IsNullOrEmpty(var.Ref) &&
					var.Pronunciations.Count == 0 &&
					var.Relations.Count == 0)
				{
					var xdoc = new XmlDocument { PreserveWhitespace = true };
					xdoc.LoadXml(var.RawXml);
					var sRef = XmlUtils.GetOptionalAttributeValue(xdoc.FirstChild, "ref");
					InsertResidueAttribute(xdResidue, "ref", sRef);
					foreach (XmlNode node in xdoc.FirstChild.SelectNodes("pronunciation"))
					{
						InsertResidueContent(xdResidue, node.OuterXml + Environment.NewLine);
					}
					foreach (XmlNode node in xdoc.FirstChild.SelectNodes("relation"))
					{
						InsertResidueContent(xdResidue, node.OuterXml + Environment.NewLine);
					}
				}
			}
			else
			{
				Debug.WriteLine($"Need LiftResidue for {extensible.GetType().Name}: <variant...>");
			}
		}

		private void StoreEtymologyAsResidue(ICmObject extensible, CmLiftEtymology ety)
		{
			var xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				var sXml = CreateXmlForEtymology(ety);
				InsertResidueContent(xdResidue, sXml);
			}
			else
			{
				Debug.WriteLine($"Need LiftResidue for {extensible.GetType().Name}: <etymology...>");
			}
		}

		private string CreateXmlForEtymology(CmLiftEtymology ety)
		{
			var bldr = new StringBuilder();
			bldr.AppendFormat("<etymology source=\"{0}\" type=\"{1}\"", ety.Source, ety.Type);
			AppendXmlDateAttributes(bldr, ety.DateCreated, ety.DateModified);
			bldr.AppendLine(">");
			Debug.Assert(ety.Form.Count < 2);
			AppendXmlForMultiText(bldr, ety.Form, "form");
			AppendXmlForMultiText(bldr, ety.Gloss, "gloss");
			foreach (var field in ety.Fields)
			{
				bldr.Append(CreateXmlForField(field));
			}
			foreach (var trait in ety.Traits)
			{
				bldr.Append(CreateXmlForTrait(trait));
			}
			foreach (var ann in ety.Annotations)
			{
				bldr.Append(CreateXmlForAnnotation(ann));
			}
			bldr.AppendLine("</etymology>");
			return bldr.ToString();
		}

		private void StoreDatesInResidue(ICmObject extensible, LiftObject obj)
		{
			if (IsDateSet(obj.DateCreated) || IsDateSet(obj.DateModified))
			{
				var xdResidue = FindOrCreateResidue(extensible);
				if (xdResidue != null)
				{
					InsertResidueDate(xdResidue, "dateCreated", obj.DateCreated);
					InsertResidueDate(xdResidue, "dateModified", obj.DateModified);
				}
				else
				{
					Debug.WriteLine($"Need LiftResidue for {extensible.GetType().Name}: <etymology...>");
				}
			}
		}

		private static void InsertResidueAttribute(XmlDocument xdResidue, string sName, string sValue)
		{
			if (!string.IsNullOrEmpty(sValue))
			{
				var xa = xdResidue.FirstChild.Attributes[sName];
				if (xa == null)
				{
					xa = xdResidue.CreateAttribute(sName);
					xdResidue.FirstChild.Attributes.Append(xa);
				}
				xa.Value = sValue;
			}
		}

		private void InsertResidueDate(XmlDocument xdResidue, string sAttrName, DateTime dt)
		{
			if (IsDateSet(dt))
			{
				InsertResidueAttribute(xdResidue, sAttrName, dt.ToUniversalTime().ToString(LiftDateTimeFormat));
			}
		}

		private static void InsertResidueContent(XmlDocument xdResidue, string sXml)
		{
			var context = new XmlParserContext(null, null, null, XmlSpace.None);
			using (XmlReader reader = new XmlTextReader(sXml, XmlNodeType.Element, context))
			{
				var xn = xdResidue.ReadNode(reader);
				if (xn != null)
				{
					xdResidue.FirstChild.AppendChild(xn);
					xn = xdResidue.ReadNode(reader); // add trailing newline
					if (xn != null)
					{
						xdResidue.FirstChild.AppendChild(xn);
					}
				}
			}
		}

		public bool IsDateSet(DateTime dt)
		{
			return dt != default(DateTime) && dt != m_defaultDateTime;
		}

		private void StoreAnnotationsAndDatesInResidue(ICmObject extensible, LiftObject obj)
		{
			// unknown fields and traits have already been stored as residue.
			if (obj.Annotations.Count > 0 || IsDateSet(obj.DateCreated) || IsDateSet(obj.DateModified))
			{
				var xdResidue = FindOrCreateResidue(extensible);
				if (xdResidue != null)
				{
					StoreDatesInResidue(extensible, obj);
					foreach (var ann in obj.Annotations)
					{
						var sXml = CreateXmlForAnnotation(ann);
						InsertResidueContent(xdResidue, sXml);
					}
				}
				else
				{
					Debug.WriteLine($"Need LiftResidue for {extensible.GetType().Name}: <{obj.XmlTag}...>");
				}
			}
		}

		private void StoreExampleResidue(ICmObject extensible, CmLiftExample expl)
		{
			// unknown notes have already been stored as residue.
			if (expl.Fields.Count + expl.Traits.Count + expl.Annotations.Count == 0)
			{
				return;
			}
			var xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				StoreDatesInResidue(extensible, expl);
				foreach (var ann in expl.Annotations)
				{
					var sXml = CreateXmlForAnnotation(ann);
					InsertResidueContent(xdResidue, sXml);
				}
			}
			else
			{
				Debug.WriteLine($"Need LiftResidue for {extensible.GetType().Name}: <example...>");
			}
		}

		#endregion // Storing LIFT import residue...

		#region Methods for processing LIFT header elements

		/// <summary />
		private int FindOrCreateCustomField(string sName, LiftMultiText lmtDesc, int clid, out Guid possListGuid)
		{
			var sClass = m_cache.MetaDataCacheAccessor.GetClassName(clid);
			var sTag = $"{sClass}-{sName}";
			var flid = 0;
			possListGuid = Guid.Empty;
			if (m_dictCustomFlid.TryGetValue(sTag, out flid))
			{
				m_CustomFieldNamesToPossibilityListGuids.TryGetValue(sTag, out possListGuid);
				return flid;
			}
			var sDesc = string.Empty; // safe-XML
			string sSpec = null; // safe-XML
			if (lmtDesc != null)
			{
				LiftString lstr; // safe-XML
				if (lmtDesc.TryGetValue("en", out lstr))
				{
					sDesc = lstr.Text;
				}

				if (lmtDesc.TryGetValue("qaa-x-spec", out lstr))
				{
					sSpec = lstr.Text;
				}
				if (string.IsNullOrEmpty(sSpec) && !string.IsNullOrEmpty(sDesc) && sDesc.StartsWith("Type=kcpt"))
				{
					sSpec = sDesc;
					sDesc = string.Empty;
				}
			}
			var type = CellarPropertyType.MultiUnicode;
			var wsSelector = WritingSystemServices.kwsAnalVerns;
			var clidDst = 0;
			if (!string.IsNullOrEmpty(sSpec))
			{
				string sDstCls;
				var rgsDef = sSpec.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				for (var i = 0; i < rgsDef.Length; i++)
				{
					var str = rgsDef[i].TrimStart(' ');
					rgsDef[i] = str;
				}
				type = GetCustomFieldType(rgsDef);
				if (type == CellarPropertyType.Nil)
				{
					type = CellarPropertyType.MultiUnicode;
				}
				wsSelector = GetCustomFieldWsSelector(rgsDef);
				clidDst = GetCustomFieldDstCls(rgsDef, out sDstCls);
				possListGuid = GetCustomFieldPossListGuid(rgsDef);
			}
			foreach (var fd in FieldDescription.FieldDescriptors(m_cache))
			{
				if (fd.Custom != 0 && fd.Name == sName && fd.Class == clid)
				{
					if (string.IsNullOrEmpty(sSpec))
					{
						// Fieldworks knows about a field with this label, but the file doesn't. Assume the project's definition of it is valid.
						m_dictCustomFlid.Add(sTag, fd.Id);
						possListGuid = fd.ListRootId;
						m_CustomFieldNamesToPossibilityListGuids.Add(sTag, possListGuid);
						return fd.Id;
					}
					// The project and the file both specify type information for this field. See whether they match (near enough).
					var fOk = CheckForCompatibleTypes(type, fd);
					if (!fOk)
					{
						// log error.
						return 0;
					}
					m_dictCustomFlid.Add(sTag, fd.Id);
					possListGuid = fd.ListRootId;
					m_CustomFieldNamesToPossibilityListGuids.Add(sTag, possListGuid);
					return fd.Id; // field with same label and type information exists already.
				}
			}
			switch (type)
			{
				case CellarPropertyType.Boolean:
				case CellarPropertyType.Integer:
				case CellarPropertyType.Numeric:
				case CellarPropertyType.Float:
				case CellarPropertyType.Time:
				case CellarPropertyType.Guid:
				case CellarPropertyType.Image:
				case CellarPropertyType.GenDate:
				case CellarPropertyType.Binary:
				case CellarPropertyType.String:
					clidDst = -1;
					break;
				case CellarPropertyType.Unicode:
				case CellarPropertyType.MultiString:
				case CellarPropertyType.MultiUnicode:
					if (wsSelector == 0)
					{
						wsSelector = WritingSystemServices.kwsAnalVerns;        // we need a WsSelector value!
					}
					clidDst = -1;
					break;
				case CellarPropertyType.OwningAtomic:
				case CellarPropertyType.ReferenceAtomic:
				case CellarPropertyType.OwningCollection:
				case CellarPropertyType.ReferenceCollection:
				case CellarPropertyType.OwningSequence:
				case CellarPropertyType.ReferenceSequence:
					break;
				default:
					type = CellarPropertyType.MultiUnicode;
					if (wsSelector == 0)
					{
						wsSelector = WritingSystemServices.kwsAnalVerns;
					}
					clidDst = -1;
					break;
			}
			var fdNew = new FieldDescription(m_cache)
			{
				Type = type,
				Class = clid,
				Name = sName,
				Userlabel = sName,
				HelpString = XmlUtils.DecodeXmlAttribute(sDesc),
				WsSelector = wsSelector,
				DstCls = clidDst,
				ListRootId = possListGuid
			};
			fdNew.UpdateCustomField();
			//Clear the data so that when the descriptions are next requested the up to date data is used
			FieldDescription.ClearDataAbout();
			m_dictCustomFlid.Add(sTag, fdNew.Id);
			m_CustomFieldNamesToPossibilityListGuids.Add(sTag, possListGuid);
			m_rgnewCustomFields.Add(fdNew);
			return fdNew.Id;
		}

		private Guid GetCustomFieldPossListGuid(IEnumerable<string> rgsDef)
		{
			var guidToReturn = Guid.Empty;
			foreach (var sDef in rgsDef)
			{
				if (sDef.StartsWith("range="))
				{
					var possListName = sDef.Substring(6);
					if (m_rangeNamesToPossibilityListGuids.TryGetValue(possListName, out guidToReturn))
					{
						return guidToReturn;
					}
				}
			}
			return guidToReturn;
		}

		private static bool CheckForCompatibleTypes(CellarPropertyType type, FieldDescription fd)
		{
			if (fd.Type == type)
			{
				return true;
			}
			if (fd.Type == CellarPropertyType.Binary && type == CellarPropertyType.Image)
			{
				return true;
			}
			if (fd.Type == CellarPropertyType.Image && type == CellarPropertyType.Binary)
			{
				return true;
			}
			if (fd.Type == CellarPropertyType.OwningCollection && type == CellarPropertyType.OwningSequence)
			{
				return true;
			}
			if (fd.Type == CellarPropertyType.OwningSequence && type == CellarPropertyType.OwningCollection)
			{
				return true;
			}
			if (fd.Type == CellarPropertyType.ReferenceCollection && type == CellarPropertyType.ReferenceSequence)
			{
				return true;
			}
			return fd.Type == CellarPropertyType.ReferenceSequence && type == CellarPropertyType.ReferenceCollection;
		}

		// arguments may be safeXML; but since they must be slightly decorated versions of enumeration members it doesn't matter.
		private CellarPropertyType GetCustomFieldType(string[] rgsDef)
		{
			foreach (var sDef in rgsDef)
			{
				if (sDef.StartsWith("Type="))
				{
					var sValue = sDef.Substring(5);
					if (sValue.StartsWith("kcpt"))
					{
						sValue = sValue.Substring(4);
					}
					return (CellarPropertyType)Enum.Parse(typeof(CellarPropertyType), sValue, true);
				}
			}
			return CellarPropertyType.Nil;
		}

		private int GetCustomFieldWsSelector(string[] rgsDef)
		{
			foreach (var sDef in rgsDef)
			{
				if (sDef.StartsWith("WsSelector="))
				{
					var sValue = sDef.Substring(11);
					// Do NOT use WritingSystemServices.GetMagicWsIdFromName...that's a different set of names (LT-12275)
					var ws = GetLiftExportMagicWsIdFromName(sValue);
					if (ws == 0)
					{
						ws = GetWsFromStr(sValue);
					}
					return ws;
				}
			}
			return 0;
		}

		/// <summary>
		/// Method MUST be consistent with LiftExporter.GetLiftExportMagicWsNameFromId.
		/// Change only with great care...this affects how we can import existing LIFT files.
		/// </summary>
		private static int GetLiftExportMagicWsIdFromName(string name)
		{
			switch (name)
			{
				case "kwsAnal":
					return WritingSystemServices.kwsAnal;
				case "kwsVern":
					return WritingSystemServices.kwsVern;
				case "kwsAnals":
					return WritingSystemServices.kwsAnals;
				case "kwsVerns":
					return WritingSystemServices.kwsVerns;
				case "kwsAnalVerns":
					return WritingSystemServices.kwsAnalVerns;
				case "kwsVernAnals":
					return WritingSystemServices.kwsVernAnals;
			}
			return 0;
		}

		private int GetCustomFieldDstCls(string[] rgsDef, out string sValue)
		{
			sValue = null;
			foreach (var sDef in rgsDef)
			{
				if (sDef.StartsWith("DstCls="))
				{
					sValue = sDef.Substring(7);
					return m_cache.MetaDataCacheAccessor.GetClassId(sValue);
				}
			}
			return 0;
		}

		/// <summary />
		private void ProcessAnthroItem(string id, string guidAttr, string parent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			var poss = FindExistingPossibility(id, guidAttr, label, abbrev, m_dictAnthroCode, m_cache.LangProject.AnthroListOA);
			if (poss == null)
			{
				var hvo = FindAbbevOrLabelInDict(abbrev, label, m_dictAnthroCode);
				if (hvo <= 0)
				{
					ICmObject caiParent;
					if (!string.IsNullOrEmpty(parent) && m_dictAnthroCode.ContainsKey(parent))
					{
						caiParent = m_dictAnthroCode[parent];
					}
					else
					{
						caiParent = m_cache.LangProject.AnthroListOA;
					}
					var cai = CreateNewCmAnthroItem(guidAttr, caiParent);
					SetNewPossibilityAttributes(id, description, label, abbrev, cai);
					m_dictAnthroCode[id] = cai;
					m_rgnewAnthroCode.Add(cai);
				}
			}
		}

		/// <summary />
		private static int FindAbbevOrLabelInDict(LiftMultiText abbrev, LiftMultiText label, Dictionary<string, ICmPossibility> dict)
		{
			if (abbrev?.Keys != null)
			{
				foreach (var key in abbrev.Keys)
				{
					var text = Icu.Normalize(XmlUtils.DecodeXmlAttribute(abbrev[key].Text), Icu.UNormalizationMode.UNORM_NFD);
					if (dict.ContainsKey(text))
					{
						return dict[text].Hvo;
					}
				}
			}
			if (label?.Keys != null)
			{
				foreach (var key in label.Keys)
				{
					var text = Icu.Normalize(XmlUtils.DecodeXmlAttribute(label[key].Text), Icu.UNormalizationMode.UNORM_NFD);
					if (dict.ContainsKey(text))
					{
						return dict[text].Hvo;
					}
				}
			}
			return 0;
		}

		/// <summary />
		private void ProcessSemanticDomain(string id, string guidAttr, string parent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			var poss = GetPossibilityForGuidIfExisting(id, guidAttr, m_dictSemDom);
			if (poss == null)
			{
				var csdParent = !string.IsNullOrEmpty(parent) && m_dictSemDom.ContainsKey(parent)
					? (ICmObject)m_dictSemDom[parent]
					: m_cache.LangProject.SemanticDomainListOA;
				var csd = CreateNewCmSemanticDomain(guidAttr, csdParent);
				SetNewPossibilityAttributes(id, description, label, abbrev, csd);
				m_dictSemDom[id] = csd;
				m_rgnewSemDom.Add(csd);
			}
		}

		/// <summary />
		private void ProcessPossibility(string id, string guidAttr, string parent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev,
			Dictionary<string, ICmPossibility> dict, List<ICmPossibility> rgNew, ICmPossibilityList list, bool isCustom = false)
		{
			var poss = FindExistingPossibility(id, guidAttr, label, abbrev, dict, list);
			if (poss == null)
			{
				var possParent = !string.IsNullOrEmpty(parent) && dict.ContainsKey(parent) ? (ICmObject)dict[parent] : list;
				poss = isCustom ? CreateNewCustomPossibility(guidAttr, possParent) : CreateNewCmPossibility(guidAttr, possParent);
				SetNewPossibilityAttributes(id, description, label, abbrev, poss);
				dict[id] = poss;
				rgNew.Add(poss);
			}
		}

		/// <summary>
		/// To Process Publications
		/// </summary>
		private void ProcessPossibilityPublications(string id, string guidAttr, string parent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev,
			Dictionary<string, ICmPossibility> dict, List<ICmPossibility> rgNew, ICmPossibilityList list, bool isCustom = false)
		{
			var poss = FindExistingPossibility(id, guidAttr, label, abbrev, dict, list);
			if (poss == null)
			{
				var possParent = !string.IsNullOrEmpty(parent) && dict.ContainsKey(parent) ? (ICmObject)dict[parent] : list;
				poss = isCustom ? CreateNewCustomPossibility(guidAttr, possParent) : CreateNewCmPossibility(guidAttr, possParent);
				SetNewPossibilityAttributes(id, description, label, abbrev, poss);
				dict[id] = poss;
				rgNew.Add(poss);
			}
			else
			{
				SetNewPossibilityAttributes(id, description, label, abbrev, poss);
			}
		}

		/// <summary />
		private void ProcessLocation(string id, string guidAttr, string parent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			var dict = m_dictLocation;
			var list = m_cache.LangProject.LocationsOA;
			var poss = (ICmLocation)FindExistingPossibility(id, guidAttr, label, abbrev, dict, list);
			if (poss == null)
			{
				var cmLocationFactory = m_cache.ServiceLocator.GetInstance<ICmLocationFactory>();
				ICmObject possParent = null;
				if (!string.IsNullOrEmpty(parent) && dict.ContainsKey(parent))
				{
					var parentLocation = (ICmLocation)dict[parent];
					if (!string.IsNullOrEmpty(guidAttr))
					{
						var guid = (Guid)GuidConv.ConvertFrom(guidAttr);
						poss = cmLocationFactory.Create(guid, parentLocation);
					}
					else
					{
						poss = cmLocationFactory.Create();
						parentLocation.SubPossibilitiesOS.Add(poss);
					}
				}
				else
				{
					if (!string.IsNullOrEmpty(guidAttr))
					{
						var guid = (Guid)GuidConv.ConvertFrom(guidAttr);
						poss = cmLocationFactory.Create(guid, list);
					}
					else
					{
						poss = cmLocationFactory.Create();
						list.PossibilitiesOS.Add(poss);
					}
				}
				SetNewPossibilityAttributes(id, description, label, abbrev, poss);
				dict[id] = poss;
				m_rgnewLocation.Add(poss);
			}
		}

		/// <summary />
		private void ProcessPerson(string id, string guidAttr, string parent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev,
			Dictionary<string, ICmPossibility> dict, List<ICmPossibility> rgNew, ICmPossibilityList list)
		{
			var person = FindExistingPossibility(id, guidAttr, label, abbrev, dict, list);
			if (person == null)
			{
				var possParent = !string.IsNullOrEmpty(parent) && dict.ContainsKey(parent) ? (ICmObject)dict[parent] : list;
				person = CreateNewCmPerson(guidAttr, possParent);
				SetNewPossibilityAttributes(id, description, label, abbrev, person);
				dict[id] = person;
				rgNew.Add(person);
			}
		}

		/// <summary />
		private void SetNewPossibilityAttributes(string id, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev, ICmPossibility poss)
		{
			IgnoreNewWs();
			if (label.Count > 0)
			{
				MergeInMultiUnicode(poss.Name, CmPossibilityTags.kflidName, label, poss.Guid);
			}
			else
			{
				poss.Name.AnalysisDefaultWritingSystem = TsStringUtils.MakeString(id, m_cache.DefaultAnalWs);
			}
			MergeInMultiUnicode(poss.Abbreviation, CmPossibilityTags.kflidAbbreviation, abbrev, poss.Guid);
			MergeInMultiString(poss.Description, CmPossibilityTags.kflidDescription, description, poss.Guid);
		}

		/// <summary />
		private void ProcessPartOfSpeech(string id, string guidAttr, string parent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			var poss = FindExistingPossibility(id, guidAttr, label, abbrev, m_dictPos, m_cache.LangProject.PartsOfSpeechOA);
			if (poss == null)
			{
				var posParent = !string.IsNullOrEmpty(parent) && m_dictPos.ContainsKey(parent)
					? (ICmObject)m_dictPos[parent]
					: m_cache.LangProject.PartsOfSpeechOA;
				var pos = CreateNewPartOfSpeech(guidAttr, posParent);
				SetNewPossibilityAttributes(id, description, label, abbrev, pos);
				m_dictPos[id] = pos;
				// Try to find this in the category catalog list, so we can add in more information.
				var cat = FindMatchingEticCategory(label);
				if (cat != null)
				{
					AddEticCategoryInfo(cat, pos);
				}
				m_rgnewPos.Add(pos);
			}
		}

		private void AddEticCategoryInfo(EticCategory cat, IPartOfSpeech pos)
		{
			if (cat != null)
			{
				pos.CatalogSourceId = cat.Id;
				foreach (var lang in cat.MultilingualName.Keys)
				{
					var ws = GetWsFromStr(lang);
					if (ws > 0)
					{
						var tssName = pos.Name.get_String(ws);
						if (tssName == null || tssName.Length == 0)
						{
							pos.Name.set_String(ws, cat.MultilingualName[lang]);
						}
					}
				}
				foreach (var lang in cat.MultilingualAbbrev.Keys)
				{
					var ws = GetWsFromStr(lang);
					if (ws > 0)
					{
						var tssAbbrev = pos.Abbreviation.get_String(ws);
						if (tssAbbrev == null || tssAbbrev.Length == 0)
						{
							pos.Abbreviation.set_String(ws, cat.MultilingualAbbrev[lang]);
						}
					}
				}
				foreach (var lang in cat.MultilingualDesc.Keys)
				{
					var ws = GetWsFromStr(lang);
					if (ws > 0)
					{
						var tss = pos.Description.get_String(ws);
						if (tss == null || tss.Length == 0)
						{
							pos.Description.set_String(ws, cat.MultilingualDesc[lang]);
						}
					}
				}
			}
		}

		/// <summary />
		private EticCategory FindMatchingEticCategory(LiftMultiText label)
		{
			foreach (var cat in m_rgcats)
			{
				var cMatch = 0;
				var cDiffer = 0;
				foreach (var lang in label.Keys)
				{
					var sName = label[lang].Text;
					string sCatName;
					if (cat.MultilingualName.TryGetValue(lang, out sCatName))
					{
						if (sName.ToLowerInvariant() == sCatName.ToLowerInvariant())
						{
							++cMatch;
						}
						else
						{
							++cDiffer;
						}
					}
				}

				if (cMatch > 0 && cDiffer == 0)
				{
					return cat;
				}
			}
			return null;
		}

		/// <summary />
		private void ProcessMorphType(string id, string guidAttr, string parent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			var poss = FindExistingPossibility(id, guidAttr, label, abbrev, m_dictMmt, m_cache.LangProject.LexDbOA.MorphTypesOA);
			if (poss == null)
			{
				var mmtParent = !string.IsNullOrEmpty(parent) && m_dictPos.ContainsKey(parent)
					? (ICmObject)m_dictMmt[parent]
					: m_cache.LangProject.LexDbOA.MorphTypesOA;
				var mmt = CreateNewMoMorphType(guidAttr, mmtParent);
				SetNewPossibilityAttributes(id, description, label, abbrev, mmt);
				m_dictMmt[id] = mmt;
				m_rgnewMmt.Add(mmt);
			}
		}

		/// <summary />
		private ICmPossibility FindExistingPossibility(string id, string guidAttr, LiftMultiText label, LiftMultiText abbrev, Dictionary<string, ICmPossibility> dict, ICmPossibilityList list)
		{
			var poss = GetPossibilityForGuidIfExisting(id, guidAttr, dict);
			if (poss == null)
			{
				poss = FindMatchingPossibility(list.PossibilitiesOS, label, abbrev);
				if (poss != null)
				{
					dict[id] = poss;
					// For the moment, we won't try to update any information in
					// existing items.
				}
			}
			return poss;
		}

		private ICmPossibility GetPossibilityForGuidIfExisting(string id, string guidAttr, Dictionary<string, ICmPossibility> dict)
		{
			ICmPossibility poss = null;
			if (!string.IsNullOrEmpty(guidAttr))
			{
				var guid = (Guid)GuidConv.ConvertFrom(guidAttr);
				var cmo = GetObjectForGuid(guid);
				if (cmo is ICmPossibility)
				{
					poss = cmo as ICmPossibility;
					dict[id] = poss;
					// For the moment, we won't try to update any information in
					// existing items.
				}
			}
			return poss;
		}

		/// <summary />
		ICmPossibility FindMatchingPossibility(ILcmOwningSequence<ICmPossibility> possibilities, LiftMultiText label, LiftMultiText abbrev)
		{
			IgnoreNewWs();
			foreach (var item in possibilities)
			{
				if (HasMatchingUnicodeAlternative(item.Name, label) && HasMatchingUnicodeAlternative(item.Abbreviation, abbrev))
				{
					return item;
				}
				var poss = FindMatchingPossibility(item.SubPossibilitiesOS, label, abbrev);
				if (poss != null)
				{
					return poss;
				}
			}
			return null;
		}

		/// <summary />
		private bool HasMatchingUnicodeAlternative(ITsMultiString tsm, LiftMultiText text)
		{
			if (text?.Keys != null)
			{
				foreach (var key in text.Keys)
				{
					var wsHvo = GetWsFromLiftLang(key);
					var sValue = Icu.Normalize(XmlUtils.DecodeXmlAttribute(text[key].Text), Icu.UNormalizationMode.UNORM_NFD);
					var tssAlt = tsm.get_String(wsHvo);
					if (string.IsNullOrEmpty(sValue) || (tssAlt == null || tssAlt.Length == 0))
					{
						continue;
					}
					if (sValue.ToLowerInvariant() == tssAlt.Text.ToLowerInvariant())
					{
						return true;
					}
				}
				return false;
			}
			return true;        // no data at all -- assume match (!!??)
		}


		/// <summary />
		private void VerifyOrCreateWritingSystem(string id, LiftMultiText label, LiftMultiText abbrev, LiftMultiText description)
		{
			// This finds or creates a writing system for the given key.
			var handle = GetWsFromLiftLang(id);
			Debug.Assert(handle >= 1);
			var ws = GetExistingWritingSystem(handle);

			if (m_msImport != MergeStyle.MsKeepOld || string.IsNullOrEmpty(ws.Abbreviation))
			{
				if (abbrev.Count > 0)
				{
					ws.Abbreviation = XmlUtils.DecodeXmlAttribute(abbrev.FirstValue.Value.Text);
				}
			}
			var languageSubtag = ws.Language;
			if (m_msImport != MergeStyle.MsKeepOld || string.IsNullOrEmpty(languageSubtag.Name))
			{
				if (label.Count > 0)
				{
					ws.Language = new LanguageSubtag(languageSubtag, XmlUtils.DecodeXmlAttribute(label.FirstValue.Value.Text));
				}
			}
		}

		/// <summary />
		private void ProcessSlotDefinition(string range, string id, string guidAttr, string parent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			IgnoreNewWs();
			var idx = range.IndexOf("-slot");
			if (idx < 0)
			{
				idx = range.IndexOf("-Slots");
			}
			var sOwner = range.Substring(0, idx);
			ICmPossibility owner = null;
			if (m_dictPos.ContainsKey(sOwner))
			{
				owner = m_dictPos[sOwner];
			}
			if (owner == null)
			{
				owner = FindMatchingPossibility(sOwner.ToLowerInvariant(), m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS, m_dictPos);
			}
			if (owner == null)
			{
				return;
			}
			var posOwner = (IPartOfSpeech)owner;
			var slot = posOwner.AffixSlotsOC.FirstOrDefault(slotT => HasMatchingUnicodeAlternative(slotT.Name, label));
			if (slot == null)
			{
				slot = CreateNewMoInflAffixSlot();
				posOwner.AffixSlotsOC.Add(slot);
				AddNewWsToAnalysis();
				MergeInMultiUnicode(slot.Name, MoInflAffixSlotTags.kflidName, label, slot.Guid);
				MergeInMultiString(slot.Description, MoInflAffixSlotTags.kflidDescription, description, slot.Guid);
				m_rgnewSlots.Add(slot);
				// TODO: How to handle "Optional" field.
			}
		}

		/// <summary />
		private void ProcessInflectionClassDefinition(string range, string id, string guidAttr, string sParent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			IgnoreNewWs();
			var idx = range.IndexOf("-infl-class");
			if (idx < 0)
			{
				idx = range.IndexOf("-InflClasses");
			}
			var sOwner = range.Substring(0, idx);
			ICmPossibility owner = null;
			if (m_dictPos.ContainsKey(sOwner))
			{
				owner = m_dictPos[sOwner];
			}
			if (owner == null)
			{
				owner = FindMatchingPossibility(sOwner.ToLowerInvariant(), m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS, m_dictPos);
			}
			if (owner == null)
			{
				return;
			}
			Dictionary<string, IMoInflClass> dictSlots;
			if (!m_dictDictSlots.TryGetValue(sOwner, out dictSlots))
			{
				dictSlots = new Dictionary<string, IMoInflClass>();
				m_dictDictSlots[sOwner] = dictSlots;
			}
			var posOwner = (IPartOfSpeech)owner;
			IMoInflClass infl = null;
			IMoInflClass inflParent = null;
			if (!string.IsNullOrEmpty(sParent))
			{
				inflParent = dictSlots.ContainsKey(sParent) ? dictSlots[sParent] : FindMatchingInflectionClass(sParent, posOwner.InflectionClassesOC, dictSlots);
			}
			else
			{
				foreach (var inflT in posOwner.InflectionClassesOC)
				{
					if (HasMatchingUnicodeAlternative(inflT.Name, label) && HasMatchingUnicodeAlternative(inflT.Abbreviation, abbrev))
					{
						infl = inflT;
						break;
					}
				}
			}
			if (infl == null)
			{
				infl = CreateNewMoInflClass();
				if (inflParent == null)
				{
					posOwner.InflectionClassesOC.Add(infl);
				}
				else
				{
					inflParent.SubclassesOC.Add(infl);
				}
				MergeInMultiUnicode(infl.Abbreviation, MoInflClassTags.kflidAbbreviation, abbrev, infl.Guid);
				MergeInMultiUnicode(infl.Name, MoInflClassTags.kflidName, label, infl.Guid);
				MergeInMultiString(infl.Description, MoInflClassTags.kflidDescription, description, infl.Guid);
				dictSlots[id] = infl;
			}
		}

		private IMoInflClass FindMatchingInflectionClass(string parent, ILcmOwningCollection<IMoInflClass> collection, Dictionary<string, IMoInflClass> dict)
		{
			foreach (var infl in collection)
			{
				if (HasMatchingUnicodeAlternative(parent.ToLowerInvariant(), infl.Abbreviation, infl.Name))
				{
					dict[parent] = infl;
					return infl;
				}
				var inflT = FindMatchingInflectionClass(parent, infl.SubclassesOC, dict);
				if (inflT != null)
				{
					return inflT;
				}
			}
			return null;
		}

		private ICmPossibility FindMatchingPossibility(string sVal, ILcmOwningSequence<ICmPossibility> possibilities, Dictionary<string, ICmPossibility> dict)
		{
			foreach (var poss in possibilities)
			{
				if (HasMatchingUnicodeAlternative(sVal, poss.Abbreviation, poss.Name))
				{
					dict?.Add(sVal, poss);
					return poss;
				}
				var possT = FindMatchingPossibility(sVal, poss.SubPossibilitiesOS, dict);
				if (possT != null)
				{
					return possT;
				}
			}
			return null;
		}

		#endregion // Methods for processing LIFT header elements

		#region Process Guids in import data

		/// <summary>
		/// As sense elements often don't have explict guid attributes in LIFT files,
		/// the parser generates new Guid values for them.  We want to always use the
		/// old guid values if we can, so we try to get a guid from the id value if
		/// one exists.  (In fact, WeSay appears to put out only the guid as the id
		/// value.  Flex puts out the default analysis gloss followed by the guid.)
		/// See LT-8840 for what happens if we depend of the Guid value provided by
		/// the parser.
		/// </summary>
		private CmLiftSense CreateLiftSenseFromInfo(Extensible info, LiftObject owner)
		{
			var guidInfo = info.Guid;
			info.Guid = Guid.Empty;
			var guid = GetGuidInExtensible(info);
			if (guid == Guid.Empty)
			{
				guid = guidInfo;
			}
			return new CmLiftSense(info, guid, owner, this);
		}

		private GuidConverter GuidConv { get; } = (GuidConverter)TypeDescriptor.GetConverter(typeof(Guid));

		private Guid GetGuidInExtensible(Extensible info)
		{
			if (info.Guid == Guid.Empty)
			{
				var sGuid = FindGuidInString(info.Id);
				if (!string.IsNullOrEmpty(sGuid))
				{
					return (Guid)GuidConv.ConvertFrom(sGuid);
				}
				return Guid.NewGuid();
			}
			return info.Guid;
		}

		/// <summary>
		/// Find and return a substring like "ebc06013-3cf8-4091-9436-35aa2c4ffc34", or null
		/// if nothing looks like a guid.
		/// </summary>
		private string FindGuidInString(string sId)
		{
			if (string.IsNullOrEmpty(sId) || sId.Length < 36)
			{
				return null;
			}
			var matchGuid = m_regexGuid.Match(sId);
			return matchGuid.Success ? sId.Substring(matchGuid.Index, matchGuid.Length) : null;
		}

		private ICmObject GetObjectFromTargetIdString(string targetId)
		{
			if (m_mapIdObject.ContainsKey(targetId))
			{
				return m_mapIdObject[targetId];
			}
			var sGuid = FindGuidInString(targetId);
			if (!string.IsNullOrEmpty(sGuid))
			{
				var guidTarget = (Guid)GuidConv.ConvertFrom(sGuid);
				return GetObjectForGuid(guidTarget);
			}
			return null;
		}

		#endregion // Process Guids in import data

		#region Methods for handling relation links

		/// <summary>
		/// This isn't really a relation link, but it needs to be done at the end of the
		/// import process.
		/// </summary>
		private void ProcessMissingFeatStrucTypeFeatures()
		{
			foreach (var type in m_mapFeatStrucTypeMissingFeatureAbbrs.Keys)
			{
				var rgsAbbr = m_mapFeatStrucTypeMissingFeatureAbbrs[type];
				var rgfeat = new List<IFsFeatDefn>(rgsAbbr.Count);
				foreach (var sAbbr in rgsAbbr)
				{
					IFsFeatDefn feat;
					if (m_mapIdFeatDefn.TryGetValue(sAbbr, out feat))
					{
						rgfeat.Add(feat);
					}
					else
					{
						break;
					}
				}
				if (rgfeat.Count == rgsAbbr.Count)
				{
					type.FeaturesRS.Clear();
					foreach (var featDefn in rgfeat)
					{
						type.FeaturesRS.Add(featDefn);
					}
				}
			}
			m_mapFeatStrucTypeMissingFeatureAbbrs.Clear();
		}

		/// <summary>
		/// After all the entries (and senses) have been imported, then the relations among
		/// them can be set since all the target ids can be resolved.
		/// This is also an opportunity to delete unwanted objects if we're keeping only
		/// the imported data.
		/// </summary>
		public void ProcessPendingRelations(IProgress progress)
		{
			var lexRefRepo = m_cache.ServiceLocator.GetInstance<ILexReferenceRepository>();
			var lexEntryRefRepo = m_cache.ServiceLocator.GetInstance<ILexEntryRefRepository>();
			// Collection of the lexical references from before the import. Items remaining here may be removed for MergeStyle.MsKeepOnlyNew
			var originalLexEntryRefs = new List<ILexEntryRef>(lexEntryRefRepo.AllInstances());
			var originalLexRefs = new List<ILexReference>(lexRefRepo.AllInstances());
			// relationMap is used to group collection relations from the lift file into a structure useful for creating
			// correct LexRefType and LexReference objects in our model.
			// The key is the relationType string(eg. Synonym), The value is a list of groups of references.
			//		in detail, the value holds a pair(tuple) containing a set of the ids(hvos) involved in the group
			//		and a set of all the PendingRelation objects which have those ids.
			var relationMap = new Dictionary<string, List<Tuple<HashSet<int>, HashSet<PendingRelation>>>>();
			if (m_mapFeatStrucTypeMissingFeatureAbbrs.Count > 0)
			{
				ProcessMissingFeatStrucTypeFeatures();
			}
			if (m_rgPendingRelation.Count > 0)
			{
				progress.Message = string.Format(LexTextControls.ksProcessingRelationLinks, m_rgPendingRelation.Count);
				progress.Position = 0;
				progress.Minimum = 0;
				progress.Maximum = m_rgPendingRelation.Count;
				// First pass, ignore "minorentry" and "subentry", since those should be
				// installed by "main".  (The first two are backreferences to the third.)
				// Also ignore reverse tree relation references on the first pass.
				// Also collect more information about collection type relations without
				// storing anything in the database yet.
				m_rgPendingTreeTargets.Clear();
				for (var i = 0; i < m_rgPendingRelation.Count;)
				{
					var relation = CollectRelationMembers(i);
					if (relation == null || relation.Count == 0)
					{
						++i;
					}
					else
					{
						i += relation.Count;
						ProcessRelation(originalLexRefs, relation, relationMap);
					}
					progress.Position = i;
				}
			}

			StorePendingCollectionRelations(originalLexRefs, progress, relationMap);
			StorePendingTreeRelations(originalLexRefs, progress);
			StorePendingLexEntryRefs(originalLexEntryRefs, progress);
			// We can now store residue everywhere since any bogus relations have been added
			// to residue.
			progress.Message = LexTextControls.ksWritingAccumulatedResidue;
			WriteAccumulatedResidue();

			// If we're keeping only the imported data, erase any unused entries or senses.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				progress.Message = LexTextControls.ksDeletingUnwantedEntries;
				GatherUnwantedObjects(originalLexEntryRefs, originalLexRefs); //at this point any LexRefs which haven't been matched are dead.
				DeleteUnwantedObjects();
			}
			// Now that the relations have all been set, it's safe to set the entry
			// modification times.
			progress.Message = LexTextControls.ksSettingEntryModificationTimes;
			foreach (var pmt in m_rgPendingModifyTimes)
			{
				pmt.SetModifyTime();
			}
		}

		private void StorePendingLexEntryRefs(List<ILexEntryRef> originalLexEntryRefs, IProgress progress)
		{
			// Now create the LexEntryRef type links.
			if (m_rgPendingLexEntryRefs.Count > 0)
			{
				progress.Message = string.Format(LexTextControls.ksStoringLexicalEntryReferences, m_rgPendingLexEntryRefs.Count);
				progress.Position = 0;
				progress.Minimum = 0;
				progress.Maximum = m_rgPendingLexEntryRefs.Count;
				var missingRefs = new List<Tuple<string, string>>();
				for (var i = 0; i < m_rgPendingLexEntryRefs.Count;)
				{
					var rgRefs = CollectLexEntryRefMembers(i);
					if (rgRefs == null || rgRefs.Count == 0)
					{
						++i;
					}
					else
					{
						ProcessLexEntryRefs(originalLexEntryRefs, rgRefs, missingRefs);
						i += rgRefs.Count;
					}
					progress.Position = i;
				}
				if (missingRefs.Count > 0)
				{
					var bldr = new StringBuilder();
					bldr.Append("The LIFT file you are importing has entries with 'Component' or 'Variant' references to lexical entries that ");
					bldr.Append("were not exported to the LIFT file. ");
					bldr.Append("Therefore, these references (components or variants) will be excluded from this import.  ");
					bldr.AppendLine();
					bldr.AppendLine();
					bldr.Append("This is probably a result of doing a Filtered Lexicon LIFT export. Instead, a Full Lexicon LIFT export should been done ");
					bldr.Append("to correct this problem, followed by another LIFT import.  ");
					bldr.AppendLine();
					bldr.AppendLine();
					bldr.AppendLine("Form\t\t:\tReference ID");
					foreach (var missingRefPair in missingRefs)
					{
						bldr.AppendFormat("{0}\t\t:\t{1}{2}", missingRefPair.Item1, missingRefPair.Item2, Environment.NewLine);
					}
					bldr.AppendLine();
					MessageBoxUtils.Show(bldr.ToString(), LexTextControls.ksProblemImporting, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
		}

		private void StorePendingTreeRelations(List<ILexReference> originalLexRefs, IProgress progress)
		{
			if (m_rgPendingTreeTargets.Count > 0)
			{
				progress.Message = string.Format(LexTextControls.ksSettingTreeRelationLinks, m_rgPendingTreeTargets.Count);
				progress.Position = 0;
				progress.Minimum = 0;
				progress.Maximum = m_rgPendingTreeTargets.Count;
				for (var i = 0; i < m_rgPendingTreeTargets.Count; ++i)
				{
					ProcessRemainingTreeRelation(originalLexRefs, m_rgPendingTreeTargets[i]);
					progress.Position = i + 1;
				}
			}
		}

		private void ProcessRemainingTreeRelation(List<ILexReference> originalLexRefs, PendingRelation rel)
		{
			Debug.Assert(rel.Target != null);
			if (rel.Target == null)
			{
				return;
			}
			var sType = rel.RelationType;
			Debug.Assert(!rel.IsSequence);
			var lrt = FindLexRefType(sType, false);
			if (!TreeRelationAlreadyExists(originalLexRefs, lrt, rel))
			{
				var lr = CreateNewLexReference();
				lrt.MembersOC.Add(lr);
				if (ObjectIsFirstInRelation(rel.RelationType, lrt))
				{
					lr.TargetsRS.Add(rel.Target);
					lr.TargetsRS.Add(rel.CmObject);
				}
				else
				{
					lr.TargetsRS.Add(rel.CmObject);
					lr.TargetsRS.Add(rel.Target);
				}
				StoreRelationResidue(lr, rel);
			}
		}

		private void ProcessRelation(List<ILexReference> originalLexRefs, List<PendingRelation> rgRelation, Dictionary<string, List<Tuple<HashSet<int>, HashSet<PendingRelation>>>> uniqueRelations)
		{
			if (rgRelation == null || !rgRelation.Any() || rgRelation[0] == null)
			{
				return;
			}
			switch (rgRelation[0].RelationType)
			{
				case "main":
				case "minorentry":
				case "subentry":
				case "_component-lexeme":
					// These should never get this far...
					Debug.Assert(rgRelation[0].RelationType == "Something else...");
					break;
				default:
					StoreLexReference(originalLexRefs, rgRelation, uniqueRelations);
					break;
			}
		}

		/// <summary>
		/// This method will process the m_rgPendingLexEntryRefs and put all the ones which belong
		/// in the same LexEntryRef into a collection. It will return when it encounters an item which
		/// belongs in a different LexEntryRef.
		/// </summary>
		/// <param name="i">The index into m_rgPendingLexEntryRefs from which to start building the like item collection</param>
		/// <returns>A List containing all the PendingLexEntryRefs which belong in the same LexEntryRef</returns>
		private List<PendingLexEntryRef> CollectLexEntryRefMembers(int i)
		{
			if (i < 0 || i >= m_rgPendingLexEntryRefs.Count)
			{
				return null;
			}
			var rgRefs = new List<PendingLexEntryRef>();
			PendingLexEntryRef prev = null;
			var hvo = m_rgPendingLexEntryRefs[i].ObjectHvo;
			var sEntryType = m_rgPendingLexEntryRefs[i].EntryType;
			var sMinorEntryCondition = m_rgPendingLexEntryRefs[i].MinorEntryCondition;
			var dateCreated = m_rgPendingLexEntryRefs[i].DateCreated;
			var dateModified = m_rgPendingLexEntryRefs[i].DateModified;
			//string sResidue = m_rgPendingLexEntryRefs[i].Residue; // cs 219
			while (i < m_rgPendingLexEntryRefs.Count)
			{
				var pend = m_rgPendingLexEntryRefs[i];
				// If the object, entry type (in an old LIFT file), or minor entry condition
				// (in an old LIFT file) has changed, we're into another LexEntryRef.
				if (pend.ObjectHvo != hvo || pend.EntryType != sEntryType ||
					pend.MinorEntryCondition != sMinorEntryCondition ||
					pend.DateCreated != dateCreated || pend.DateModified != dateModified)
				{
					break;
				}
				// The end of the components of a LexEntryRef may be marked only by a sudden
				// drop in the order value (which starts at 0 and increments by 1 steadily, or
				// is set to -1 when there's only one).
				if (prev != null && pend.Order < prev.Order)
				{
					break;
				}
				//If we have a different set of traits from the previous relation we belong in a new ref.
				if (prev != null)
				{
					if (prev.ComplexFormTypes.Count != pend.ComplexFormTypes.Count)
					{
						break;
					}

					if (!prev.ComplexFormTypes.ContainsCollection(pend.ComplexFormTypes))
					{
						break;
					}
				}
				pend.Target = GetObjectFromTargetIdString(m_rgPendingLexEntryRefs[i].TargetId);
				rgRefs.Add(pend);
				prev = pend;
				++i;
			}
			return rgRefs;
		}

		/// <summary>
		/// A LexEntryRef is matched if it is has same type, summary and hideMinorEntry value
		/// and if the collections all intersect.
		/// </summary>
		private bool MatchLexEntryRef(ILexEntryRef ler, int refType, List<ILexEntryType> complexEntryTypes, List<ILexEntryType> variantEntryTypes, LiftMultiText summary, List<ICmObject> componentLexemes, List<ICmObject> primaryLexemes)
		{
			if (ler.RefType != refType)
			{
				return false;
			}
			AddNewWsToAnalysis();
			if (summary != null && !MatchMultiString(ler.Summary, summary))
			{
				return false;
			}
			if ((complexEntryTypes.Any() || ler.ComplexEntryTypesRS.Any()) && !complexEntryTypes.Intersect(ler.ComplexEntryTypesRS).Any())
			{
				return false;
			}
			if ((variantEntryTypes.Any() || ler.VariantEntryTypesRS.Any()) && !variantEntryTypes.Intersect(ler.VariantEntryTypesRS).Any())
			{
				return false;
			}
			if ((componentLexemes.Any() || ler.ComponentLexemesRS.Any()) && !componentLexemes.Intersect(ler.ComponentLexemesRS).Any())
			{
				return false;
			}
			return !primaryLexemes.Any() && !ler.PrimaryLexemesRS.Any() || primaryLexemes.Intersect(ler.PrimaryLexemesRS).Any();
		}

		private void ProcessLexEntryRefs(List<ILexEntryRef> originalRefs, List<PendingLexEntryRef> rgRefs, List<Tuple<string, string>> missingRefs)
		{
			if (rgRefs.Count == 0)
			{
				return;
			}
			ILexEntry le = null;
			ICmObject target = null;
			//"main" is no longer used as a relationType in LIFT export but is handled here in case
			//an older LIFT file is being imported.
			//The current LIFT export will have rgRefs[0].RelationType == "_component-lexeme"
			if (rgRefs.Count == 1 && rgRefs[0].RelationType == "main")
			{
				target = rgRefs[0].CmObject;
				var sRef = rgRefs[0].TargetId;
				ICmObject cmo;
				if (!string.IsNullOrEmpty(sRef) && m_mapIdObject.TryGetValue(sRef, out cmo))
				{
					Debug.Assert(cmo is ILexEntry);
					le = (ILexEntry)cmo;
				}
				else
				{
					// log error message about invalid link in <relation type="main" ref="...">.
					var bad = new InvalidRelation(rgRefs[0], m_cache, this);
					if (!m_rgInvalidRelation.Contains(bad))
					{
						m_rgInvalidRelation.Add(bad);
					}
				}
			}
			else
			{
				Debug.Assert(rgRefs[0].CmObject is ILexEntry);
				le = (ILexEntry)rgRefs[0].CmObject;
			}
			if (le == null)
			{
				return;
			}
			// Adjust HideMinorEntry for using old LIFT file.
			if (rgRefs[0].HideMinorEntry == 0 && rgRefs[0].ExcludeAsHeadword)
			{
				rgRefs[0].HideMinorEntry = 1;
			}
			// See if we can find a matching variant that already exists.
			var complexEntryTypes = new List<ILexEntryType>();
			var variantEntryTypes = new List<ILexEntryType>();
			var refType = DetermineLexEntryTypes(rgRefs, complexEntryTypes, variantEntryTypes);
			var componentLexemes = new List<ICmObject>();
			var primaryLexemes = new List<ICmObject>();
			for (var i = 0; i < rgRefs.Count; ++i)
			{
				var pend = rgRefs[i];
				//This is handling the historical LIFT export data where relationType was "main"
				if (pend.RelationType == "main" && i == 0 && target != null)
				{
					componentLexemes.Add(target);
					primaryLexemes.Add(target);
				}
				else if (pend.Target != null)
				// pend.RelationType == "_component-lexeme" is now the default and there should be a non-null Target, however
				//when the LIFT file was produced by a partial export vs. full export the compontent/variant contained
				//in the lexEntry might be referencing another lexEntry that was not part of the export. In this case Target will be null.
				{
					componentLexemes.Add(pend.Target);
					//With compontents, for example a compound word, often one of the components is considered the
					//primary lexeme and the others are not.
					if (pend.IsPrimary || pend.RelationType == "main")
					{
						primaryLexemes.Add(pend.Target);
					}
				}
				else if (!string.IsNullOrEmpty(pend.TargetId))
				// pend.Target == null
				//If there is a partial LIFT export "Filtered Lexicon LIFT 0.13 XML" we can encounter a LexEntryRef that has a null target.
				//For example if the word 'unbelieving' has Components un- believe -ing then these three components will be included
				//in the <entry> 'unbelieving' as relations <relation type="_component-lexeme".../> when doing a LIFT export.
				//However, for a partial export where'un-' 'believe' and '-ing' are not included in the export, the reference	s to these found in
				//the lexEntry 'unbelieving' are invalid and therefore pend.Target will be null for each of these.
				//Therefore they are removed from this lexEntry on importing the LIFT file.
				//We should however warn the user that this data is not being imported and that they should do a FULL export to ensure this data
				//is imported correctly.
				// One exception: a LexEntryRef with NO components is exported as a relation with an EMPTY ref attribute.
				// This is not a problem, it just represents incomplete original data.
				{
					var form = "<empty form>";
					if (rgRefs[0].LexemeForm != null)
					{
						form = XmlUtils.DecodeXmlAttribute(rgRefs[0].LexemeForm.FirstValue.Value.Text);
					}
					missingRefs.Add(new Tuple<string, string>(form, pend.TargetId));
				}
			}
			if (!complexEntryTypes.Any() && !variantEntryTypes.Any() && rgRefs[0].RelationType == "BaseForm" && componentLexemes.Count == 1 && !primaryLexemes.Any())
			{
				// A BaseForm relation from WeSay, with none of our lexical relation stuff implemented.
				// The baseform should be considered primary.
				primaryLexemes.Add(componentLexemes[0]);
				complexEntryTypes.Add(FindOrCreateComplexFormType("BaseForm"));
				refType = LexEntryRefTags.krtComplexForm;
			}
			ILexEntryRef ler = null;
			LiftMultiText summary = null;
			if (rgRefs[0].Summary != null)
			{
				summary = rgRefs[0].Summary.Content;
			}
			foreach (var candidate in le.EntryRefsOS)
			{
				if (MatchLexEntryRef(candidate, refType, complexEntryTypes, variantEntryTypes, summary, componentLexemes, primaryLexemes))
				{
					ler = ler ?? candidate;
					originalRefs.Remove(candidate);
				}
			}

			if (ler == null)
			{
				// no match, make a new one with the required properties.
				ler = CreateNewLexEntryRef();

				le.EntryRefsOS.Add(ler);
				ler.RefType = refType;
				foreach (var item in complexEntryTypes)
				{
					ler.ComplexEntryTypesRS.Add(item);
				}
				foreach (var item in variantEntryTypes)
				{
					ler.VariantEntryTypesRS.Add(item);
				}
				try
				{
					foreach (var item in componentLexemes)
					{
						ler.ComponentLexemesRS.Add(item);
					}
					foreach (var item in primaryLexemes)
					{
						ler.PrimaryLexemesRS.Add(item);
					}
				}
				catch (Exception error)
				{
					var bldr = new StringBuilder();
					bldr.Append("Something went wrong while FieldWorks was attempting to import the LIFT file.");
					bldr.AppendLine();
					bldr.Append(error.Message);
					bldr.AppendLine();
					if (rgRefs[0].LexemeForm != null)
					{
						bldr.AppendFormat("CmLiftEntry.LexicalForm is:    {0}", XmlUtils.DecodeXmlAttribute(rgRefs[0].LexemeForm.FirstValue.Value.Text));
						bldr.AppendLine();
						bldr.AppendFormat("RelationType is:     {0}", rgRefs[0].RelationType);
					}
					MessageBox.Show(bldr.ToString(), LexTextControls.ksProblemImporting, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}

				ler.HideMinorEntry = rgRefs[0].HideMinorEntry;
				AddNewWsToAnalysis();
				if (summary != null)
				{
					MergeInMultiString(ler.Summary, LexEntryRefTags.kflidSummary, summary, ler.Guid);
				}
			}
			else // Adjust collection contents if necessary
			{
				AdjustCollectionContents(complexEntryTypes, ler.ComplexEntryTypesRS, ler);
				AdjustCollectionContents(variantEntryTypes, ler.VariantEntryTypesRS, ler);
				AdjustCollectionContents(componentLexemes, ler.ComponentLexemesRS, ler);
				AdjustCollectionContents(primaryLexemes, ler.PrimaryLexemesRS, ler);
			}

			// Create an empty sense if a complex form came in without a sense.  See LT-9153.
			if (!le.SensesOS.Any() && (ler.ComplexEntryTypesRS.Any() || ler.PrimaryLexemesRS.Any()))
			{
				bool fNeedNewId;
				CreateNewLexSense(Guid.Empty, le, out fNeedNewId);
				EnsureValidMSAsForSenses(le);
			}
		}

		/// <summary>
		/// This method will set the contents of the given ReferenceSequence(second param) to the union with the list (first param)
		/// </summary>
		private void AdjustCollectionContents<T>(List<T> complexEntryTypes, ILcmReferenceSequence<T> referenceCollection, ILexEntryRef lexEntryRef) where T : class, ICmObject
		{
			AdjustCollectionContents(complexEntryTypes, referenceCollection, !lexEntryRef.VariantEntryTypesRS.Any() ? LexTextControls.ksComplexFormType : LexTextControls.ksVariantType, lexEntryRef.Owner);
		}
		private void AdjustCollectionContents<T>(List<T> complexEntryTypes, ILcmReferenceSequence<T> referenceCollection, ILexReference lexEntryRef) where T : class, ICmObject
		{
			AdjustCollectionContents(complexEntryTypes, referenceCollection, lexEntryRef.TypeAbbreviation(m_cache.DefaultVernWs, lexEntryRef), referenceCollection.First());
		}

		private void AdjustCollectionContents<T>(List<T> complexEntryTypes, ILcmReferenceSequence<T> referenceCollection, string typeName, ICmObject owner) where T : class, ICmObject
		{
			if (referenceCollection.Count != complexEntryTypes.Count)
			{
				//add an error message for intersecting sets which do not have a subset-superset relationship.
				if (!complexEntryTypes.ContainsCollection(referenceCollection) && !referenceCollection.ContainsCollection(complexEntryTypes))
				{
					foreach (var newItem in complexEntryTypes)
					{
						var col = new CombinedCollection(owner, m_cache, this)
						{
							TypeName = typeName,
							BadValue = newItem is ILexEntry
								? ((ILexEntry)newItem).HeadWordForWs(m_cache.DefaultVernWs).Text
								: ((ILexEntry)(((ILexSense)newItem).Owner)).HeadWordForWs(m_cache.DefaultVernWs).Text
						};
						m_combinedCollections.Add(col);
					}
				}
				referenceCollection.Replace(0, referenceCollection.Count, complexEntryTypes);
			}
		}

		/// <summary>
		/// Answer the RefType that a LexEntryRef should have to match the input. Set the two lists to the required
		/// values for the indicated properties.
		/// </summary>
		private int DetermineLexEntryTypes(List<PendingLexEntryRef> rgRefs, List<ILexEntryType> complexEntryTypes, List<ILexEntryType> variantEntryTypes)
		{
			var result = LexEntryRefTags.krtVariant; // default in an unitialized LexEntryRef
			complexEntryTypes.Clear();
			variantEntryTypes.Clear();
			var rgsComplexFormTypes = rgRefs[0].ComplexFormTypes;
			var rgsVariantTypes = rgRefs[0].VariantTypes;
			var sOldEntryType = rgRefs[0].EntryType;
			var sOldCondition = rgRefs[0].MinorEntryCondition;
			// A trait name complex-form-type or variant-type can be used with an unspecified value
			// to indicate that this reference type is either complex or variant (more options in future).
			if (rgsComplexFormTypes.Count > 0)
			{
				result = LexEntryRefTags.krtComplexForm;
			}
			if (rgsVariantTypes.Count > 0)
			{
				result = LexEntryRefTags.krtVariant;
			}
			if (rgsComplexFormTypes.Count > 0 && rgsVariantTypes.Count > 0)
			{
				// TODO: Complain to the user that he's getting ahead of the programmers!
			}
			foreach (var sType in rgsComplexFormTypes)
			{
				if (!string.IsNullOrEmpty(sType))
				{
					var let = FindOrCreateComplexFormType(sType);
					complexEntryTypes.Add(let);
				}
			}
			foreach (var sType in rgsVariantTypes)
			{
				if (!string.IsNullOrEmpty(sType))
				{
					var let = FindOrCreateVariantType(sType);
					variantEntryTypes.Add(let);
				}
			}
			if (!complexEntryTypes.Any() && !variantEntryTypes.Any() && !string.IsNullOrEmpty(sOldEntryType))
			{
				if (sOldEntryType == "Derivation")
				{
					sOldEntryType = "Derivative";
				}
				else if (sOldEntryType == "derivation")
				{
					sOldEntryType = "derivative";
				}
				else if (sOldEntryType == "Inflectional Variant")
				{
					sOldEntryType = "Irregularly Inflected Form";
				}
				else if (sOldEntryType == "inflectional variant")
				{
					sOldEntryType = "irregularly inflected form";
				}
				var letComplex = FindComplexFormType(sOldEntryType);
				if (letComplex == null)
				{
					var letVar = FindVariantType(sOldEntryType);
					if (letVar == null && sOldEntryType.ToLowerInvariant() != "main entry")
					{
						if (string.IsNullOrEmpty(sOldCondition))
						{
							letComplex = FindOrCreateComplexFormType(sOldEntryType);
							complexEntryTypes.Add(letComplex);
							result = LexEntryRefTags.krtComplexForm;
						}
						else
						{
							letVar = FindOrCreateVariantType(sOldEntryType);
						}
					}
					if (letVar != null)
					{
						if (string.IsNullOrEmpty(sOldCondition))
						{
							variantEntryTypes.Add(letVar);
						}
						else
						{
							ILexEntryType subtype = null;
							foreach (var poss in letVar.SubPossibilitiesOS)
							{
								var sub = poss as ILexEntryType;
								if (sub != null &&
									(sub.Name.AnalysisDefaultWritingSystem.Text == sOldCondition ||
									 sub.Abbreviation.AnalysisDefaultWritingSystem.Text == sOldCondition ||
									 sub.ReverseAbbr.AnalysisDefaultWritingSystem.Text == sOldCondition))
								{
									subtype = sub;
									break;
								}
							}
							if (subtype == null)
							{
								subtype = CreateNewLexEntryType();
								letVar.SubPossibilitiesOS.Add(subtype);
								subtype.Name.set_String(m_cache.DefaultAnalWs, sOldCondition);
								subtype.Abbreviation.set_String(m_cache.DefaultAnalWs, sOldCondition);
								subtype.ReverseAbbr.set_String(m_cache.DefaultAnalWs, sOldCondition);
								m_rgnewVariantType.Add(subtype);
							}
							variantEntryTypes.Add(subtype);
						}
						result = LexEntryRefTags.krtVariant;
					}
				}
				else
				{
					complexEntryTypes.Add(letComplex);
					result = LexEntryRefTags.krtComplexForm;
				}
			}
			return result;
		}

		private void StoreLexReference(ICollection<ILexReference> refsAsYetUnmatched, List<PendingRelation> rgRelation,
			Dictionary<string, List<Tuple<HashSet<int>, HashSet<PendingRelation>>>> uniqueRelations)
		{
			// Store any relations with unrecognized targets in residue, removing them from the
			// list.
			foreach (var pendingRelation in rgRelation)
			{
				if (pendingRelation.Target == null)
				{
					StoreResidue(pendingRelation.CmObject, pendingRelation.AsResidueString());
				}
			}
			for (var i = rgRelation.Count - 1; i >= 0; --i)
			{
				if (rgRelation[i].Target == null)
				{
					rgRelation.RemoveAt(i);
				}
			}
			if (rgRelation.Count == 0)
			{
				return;
			}
			// Store the list of relations appropriately as a LexReference with a proper type.
			var sType = rgRelation[0].RelationType;
			var lrt = FindLexRefType(sType, rgRelation[0].Order != -1);
			switch (lrt.MappingType)
			{
				case (int)LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
					StoreAsymmetricPairRelations(refsAsYetUnmatched, lrt, rgRelation, ObjectIsFirstInRelation(rgRelation[0].RelationType, lrt));
					break;
				case (int)LexRefTypeTags.MappingTypes.kmtEntryPair:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSensePair:
				case (int)LexRefTypeTags.MappingTypes.kmtSensePair:
					StorePairRelations(refsAsYetUnmatched, lrt, rgRelation);
					break;
				case (int)LexRefTypeTags.MappingTypes.kmtEntryCollection:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseCollection:
					CollapseCollectionRelationPairs(rgRelation, uniqueRelations);
					break;
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence:
				case (int)LexRefTypeTags.MappingTypes.kmtEntrySequence:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseSequence:
					StoreSequenceRelation(refsAsYetUnmatched, lrt, rgRelation);
					break;
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryTree:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseTree:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseUnidirectional:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryUnidirectional:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseUnidirectional:
					StoreTreeRelation(refsAsYetUnmatched, lrt, rgRelation);
					break;
			}
		}

		private ILexRefType FindLexRefType(string sType, bool isSequence)
		{
			var lrt = GetRefTypeByNameOrReverseName(sType);
			if (lrt == null)
			{

				//This section is for older lift files where the type information wasn't written out well enough, new files will work better, but this is better than nothing
				var newLexRefType = CreateNewLexRefType(null, null);
				m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(newLexRefType);
				newLexRefType.Name.set_String(m_cache.DefaultAnalWs, sType);
				if ((string.IsNullOrEmpty(m_sLiftProducer) || m_sLiftProducer.StartsWith("WeSay")) && (sType == "BaseForm"))
				{
					newLexRefType.Abbreviation.set_String(m_cache.DefaultAnalWs, "base");
					newLexRefType.ReverseName.set_String(m_cache.DefaultAnalWs, "Derived Forms");
					newLexRefType.ReverseAbbreviation.set_String(m_cache.DefaultAnalWs, "deriv");
					newLexRefType.MappingType = (int)LexRefTypeTags.MappingTypes.kmtEntryTree;
				}
				else
				{
					newLexRefType.Abbreviation.set_String(m_cache.DefaultAnalWs, sType);
					if (isSequence)
					{
						newLexRefType.MappingType = (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence;
					}
					else
					{
						newLexRefType.MappingType = (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection;
					}
				}
				lrt = newLexRefType;
				m_dictLexRefTypes.Add(sType.ToLowerInvariant(), lrt);
				m_rgnewLexRefTypes.Add(lrt);
			}
			return lrt as ILexRefType;
		}

		private ICmPossibility GetRefTypeByNameOrReverseName(string sType)
		{
			ICmPossibility lrt;
			var normalizedTypeName = sType.Normalize().ToLowerInvariant();
			m_dictLexRefTypes.TryGetValue(normalizedTypeName, out lrt);
			if (lrt == null)
			{
				foreach (ILexRefType dictLexRefType in m_dictLexRefTypes.Values)
				{
					if (dictLexRefType.ReverseName.OccursInAnyAlternative(normalizedTypeName))
					{
						return dictLexRefType;
					}
				}
				lrt = m_rgnewLexRefTypes.FirstOrDefault(x => x.Name.OccursInAnyAlternative(normalizedTypeName)) ??
						m_rgnewLexRefTypes.FirstOrDefault(x => ((ILexRefType)x).ReverseName.OccursInAnyAlternative(normalizedTypeName));
			}
			return lrt;
		}

		private void StoreAsymmetricPairRelations(ICollection<ILexReference> refsAsYetUnmatched, ILexRefType lrt, List<PendingRelation> rgRelation, bool fFirst)
		{
			foreach (var pendingRelation in rgRelation)
			{
				Debug.Assert(pendingRelation.Target != null);
				if (pendingRelation.Target == null)
				{
					continue;
				}
				if (AsymmetricPairRelationAlreadyExists(refsAsYetUnmatched, lrt, pendingRelation, fFirst))
				{
					continue;
				}
				var lr = CreateNewLexReference();
				lrt.MembersOC.Add(lr);
				if (fFirst)
				{
					lr.TargetsRS.Add(pendingRelation.CmObject);
					lr.TargetsRS.Add(pendingRelation.Target);
				}
				else
				{
					lr.TargetsRS.Add(pendingRelation.Target);
					lr.TargetsRS.Add(pendingRelation.CmObject);
				}
				StoreRelationResidue(lr, pendingRelation);
			}
		}

		/// <summary>
		/// Attempts to match the pending relation to any existing AsymmetricPairRelations
		/// </summary>
		private static bool AsymmetricPairRelationAlreadyExists(ICollection<ILexReference> refsAsYetUnmatched, ILexRefType lrt, PendingRelation rel, bool isFirst)
		{
			var hvo1 = rel.CmObject == null ? 0 : rel.ObjectHvo;
			var hvo2 = rel.TargetHvo;
			foreach (var lr in lrt.MembersOC)
			{
				if (lr.TargetsRS.Count != 2)
				{
					continue; // SHOULD NEVER HAPPEN!!
				}
				var hvoA = lr.TargetsRS[0].Hvo;
				var hvoB = lr.TargetsRS[1].Hvo;
				if (isFirst)
				{
					if (hvoA == hvo1 && hvoB == hvo2)
					{
						refsAsYetUnmatched.Remove(lr);
						return true;
					}
				}
				else
				{
					if (hvoA == hvo2 && hvoB == hvo1)
					{
						refsAsYetUnmatched.Remove(lr);
						return true;
					}
				}
			}
			return false;
		}

		private void StorePairRelations(ICollection<ILexReference> refsAsYetUnmatched, ILexRefType lrt, List<PendingRelation> rgRelation)
		{
			foreach (var pendingRelation in rgRelation)
			{
				Debug.Assert(pendingRelation.Target != null);
				if (pendingRelation.Target == null)
				{
					continue;
				}
				if (PairRelationAlreadyExists(refsAsYetUnmatched, lrt, pendingRelation))
				{
					continue;
				}
				var lr = CreateNewLexReference();
				lrt.MembersOC.Add(lr);
				lr.TargetsRS.Add(pendingRelation.CmObject);
				lr.TargetsRS.Add(pendingRelation.Target);
				StoreRelationResidue(lr, pendingRelation);
			}
		}

		private static bool PairRelationAlreadyExists(ICollection<ILexReference> refsAsYetUnmatched, ILexRefType lrt, PendingRelation rel)
		{
			var hvo1 = rel.CmObject == null ? 0 : rel.ObjectHvo;
			var hvo2 = rel.TargetHvo;
			foreach (var lr in lrt.MembersOC)
			{
				if (lr.TargetsRS.Count != 2)
				{
					continue; // SHOULD NEVER HAPPEN!!
				}
				var hvoA = lr.TargetsRS[0].Hvo;
				var hvoB = lr.TargetsRS[1].Hvo;
				if ((hvoA == hvo1 && hvoB == hvo2) || (hvoA == hvo2 && hvoB == hvo1))
				{
					refsAsYetUnmatched.Remove(lr);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Removes duplicate and mirrored relations from Collections
		/// </summary>
		private static void CollapseCollectionRelationPairs(IEnumerable<PendingRelation> rgRelation,
			IDictionary<string, List<Tuple<HashSet<int>, HashSet<PendingRelation>>>> uniqueRelations)
		{
			//for every pending relation in this list
			foreach (var rel in rgRelation)
			{
				Debug.Assert(rel.Target != null);
				if (rel.Target == null)
				{
					continue;
				}
				List<Tuple<HashSet<int>, HashSet<PendingRelation>>> relationsForType;
				uniqueRelations.TryGetValue(rel.RelationType, out relationsForType);
				if (relationsForType != null)
				{
					var foundGroup = false;
					//for every group of relations identified so far (relations which share target or object ids)
					foreach (var refs in relationsForType)
					{
						var fAdd = true;
						//If this item belongs to an existing group
						if (refs.Item1.Contains(rel.ObjectHvo) || refs.Item1.Contains(rel.TargetHvo))
						{
							foundGroup = true;
							foreach (var pend in refs.Item2)
							{
								//test if we have added this relation or a mirror of it.
								if (pend.IsSameOrMirror(rel))
								{
									fAdd = false;
									break;
								}
							}
							if (fAdd) //add it into the group if it wasn't already here.
							{
								refs.Item1.Add(rel.ObjectHvo);
								refs.Item1.Add(rel.TargetHvo);
								refs.Item2.Add(rel);
							}
						}
					}
					//if this is a brand new relation for this type, build it
					if (!foundGroup)
					{
						relationsForType.Add(new Tuple<HashSet<int>, HashSet<PendingRelation>>(new HashSet<int> { rel.ObjectHvo, rel.TargetHvo }, new HashSet<PendingRelation> { rel }));
					}
				}
				else //First relation that we are processing, create the dictionary with this relation as our initial data.
				{
					var relData = new List<Tuple<HashSet<int>, HashSet<PendingRelation>>>
					{
						new Tuple<HashSet<int>, HashSet<PendingRelation>>(new HashSet<int> {rel.TargetHvo, rel.ObjectHvo}, new HashSet<PendingRelation> {rel})
					};
					uniqueRelations[rel.RelationType] = relData;
				}
			}
		}

		private void StorePendingCollectionRelations(ICollection<ILexReference> originalLexRefs, IProgress progress,
			Dictionary<string, List<Tuple<HashSet<int>, HashSet<PendingRelation>>>> relationMap)
		{
			progress.Message = string.Format(LexTextControls.ksSettingCollectionRelationLinks, m_rgPendingCollectionRelations.Count);
			progress.Minimum = 0;
			progress.Maximum = relationMap.Count;
			progress.Position = 0;
			foreach (var typeCollections in relationMap) //for each relationType
			{
				var sType = typeCollections.Key;
				foreach (var collection in typeCollections.Value) //for each grouping of relations for that type
				{
					var incomingRelationIDs = collection.Item1;
					var incomingRelations = collection.Item2;
					var lrt = FindLexRefType(sType, false);
					if (IsAnExistingCollectionAnExactMatch(originalLexRefs, lrt, incomingRelationIDs))
					{
						continue;
					}
					List<ICmObject> oldItems;
					var lr = FindExistingSequence(lrt, incomingRelations, out oldItems);
					if (lr == null)
					{
						lr = CreateNewLexReference();
						lrt.MembersOC.Add(lr);
						foreach (var hvo in incomingRelationIDs)
						{
							lr.TargetsRS.Add(GetObjectForId(hvo));
						}
						foreach (var relation in incomingRelations)
						{
							StoreRelationResidue(lr, relation);
						}
					}
					else
					{
						originalLexRefs.Remove(lr);
						ReplaceLexRefContents(lr, incomingRelationIDs);
					}
				}
				progress.Position++;
			}
		}

		/// <summary>
		/// This method is necessary to prevent duplication of sets of relations due to the fact they are replicated in
		/// all the members of the relation in the LIFT file.
		/// </summary>
		/// <param name="originalLexRefs">Collection of the lexical references from before the import. Items remaining here may be removed for MergeStyle.MsKeepOnlyNew</param>
		/// <param name="lrt">The LexRefType to inspect for duplicate collections</param>
		/// <param name="checkTargets">The set of ids to look for</param>
		private static bool IsAnExistingCollectionAnExactMatch(ICollection<ILexReference> originalLexRefs, ILexRefType lrt, HashSet<int> checkTargets)
		{
			foreach (var lr in lrt.MembersOC)
			{
				if (lr.TargetsRS.Count != checkTargets.Count)
				{
					continue;
				}
				//build a set of the hvos in the lexical relation
				var currentSet = lr.TargetsRS.Select(cmo => cmo.Hvo).ToList();
				//for every object in the target sequence of the LexReference
				if (currentSet.ContainsCollection(checkTargets))
				{
					originalLexRefs.Remove(lr);
					return true; // got an exact match. If not, keep trying.
				}
			}
			return false;
		}

		/// <summary>
		/// This method will adjust two collections (which share some content) based off new data from the lift file.
		/// </summary>
		/// <param name="lr">The ILexReference to adjust</param>
		/// <param name="setRelation">The Set of hvo's to use in the adjusted reference</param>
		private void ReplaceLexRefContents(ILexReference lr, IEnumerable<int> setRelation)
		{
			var localSetRelatuion = new HashSet<int>(setRelation);
			var currentSet = lr.TargetsRS.Select(cmo => cmo.Hvo).ToList();
			//for every object in the target sequence of the LexReference
			var intersectors = currentSet.Intersect(localSetRelatuion).ToList();
			if (!intersectors.Any()) //the two sets are unrelated, and shouldn't be merged
			{
				return;
			}
			//If the sets intersect, but did not have a subset-superset relationship then we might be doing
			//something the user did not expect or want, so log it for them.
			if (!intersectors.ContainsCollection(localSetRelatuion) && !intersectors.ContainsCollection(currentSet))
			{
				foreach (var item in currentSet)
				{
					if (!intersectors.Contains(item))
					{
						var conflictingData = new CombinedCollection(GetObjectForId(intersectors.First()), m_cache, this)
						{
							TypeName = lr.TypeAbbreviation(m_cache.DefaultUserWs, GetObjectForId(item)),
							BadValue = GetObjectForId(item).DeletionTextTSS.Text
						};
						m_combinedCollections.Add(conflictingData);
					}
				}
			}
			var otherObjects = localSetRelatuion.Select(hvo => GetObjectForId(hvo)).ToList();
			AdjustCollectionContents(otherObjects, lr.TargetsRS, lr);
		}

		private void StoreSequenceRelation(ICollection<ILexReference> refsAsYetUnmatched, ILexRefType lrt, List<PendingRelation> rgRelation)
		{
			if (IsAnExistingSequenceAnExactMatch(refsAsYetUnmatched, lrt, rgRelation))
			{
				return;
			}
			List<ICmObject> oldItems;
			var lr = FindExistingSequence(lrt, rgRelation, out oldItems);
			if (lr == null) //This is a new relation, add all the targets
			{
				lr = CreateNewLexReference();
				lrt.MembersOC.Add(lr);
				foreach (var pendingRelation in rgRelation)
				{
					lr.TargetsRS.Add(GetObjectForId(pendingRelation.TargetHvo));
				}
			}
			else //Reusing an old relation, replace the contents of the Targets sequence
			{
				var targetList = new List<ICmObject>();
				foreach (var pendingRelation in rgRelation)
				{
					targetList.Add(GetObjectForId(pendingRelation.TargetHvo));
				}
				lr.TargetsRS.Replace(0, lr.TargetsRS.Count, targetList);
				refsAsYetUnmatched.Remove(lr);
			}
			StoreRelationResidue(lr, rgRelation[0]);
		}

		/// <summary>
		/// Tests if there is an existing sequence in the LexRefType which exactly matches the pendingRelation
		/// in contents and order.
		/// </summary>
		/// <param name="refsAsYetUnmatched"></param>
		/// <param name="lrt">the ILexRefType to check</param>
		/// <param name="rgRelation">An ordered list of relations from the LIFT file</param>
		/// <returns>true if this sequence already exists in the system</returns>
		private static bool IsAnExistingSequenceAnExactMatch(ICollection<ILexReference> refsAsYetUnmatched, ILexRefType lrt, IList<PendingRelation> rgRelation)
		{
			foreach (var lr in lrt.MembersOC)
			{
				if (lr.TargetsRS.Count != rgRelation.Count)
				{
					continue;
				}
				var sequenceMatched = !rgRelation.Where((t, i) => lr.TargetsRS[i].Hvo != t.TargetHvo).Any();
				if (sequenceMatched)
				{
					refsAsYetUnmatched.Remove(lr);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// This method will find and return an ILexReference in the given LexRefType if it has any overlap with the given collection
		/// of relations
		/// </summary>
		private static ILexReference FindExistingSequence(ILexRefType lrt, IEnumerable<PendingRelation> rgRelation, out List<ICmObject> oldItems)
		{
			oldItems = new List<ICmObject>();
			foreach (var lr in lrt.MembersOC)
			{
				oldItems.AddRange(lr.TargetsRS);
				foreach (var pendingRelation in rgRelation)
				{
					if (oldItems.Contains(pendingRelation.Target))
					{
						return lr;
					}
				}
				oldItems.Clear();
			}
			oldItems = null; // old items aren't actually old items if we didn't find a match
			return null;
		}

		private void StoreTreeRelation(ICollection<ILexReference> refsAsYetUnmatched, ILexRefType lrt, List<PendingRelation> rgRelation)
		{
			if (TreeRelationAlreadyExists(refsAsYetUnmatched, lrt, rgRelation))
			{
				return;
			}
			if (ObjectIsFirstInRelation(rgRelation[0].RelationType, lrt))
			{
				var lr = CreateNewLexReference();
				lrt.MembersOC.Add(lr);
				lr.TargetsRS.Add(rgRelation[0].CmObject);
				foreach (var pendingRelation in rgRelation)
				{
					lr.TargetsRS.Add(GetObjectForId(pendingRelation.TargetHvo));
				}

				StoreRelationResidue(lr, rgRelation[0]);
			}
			else
			{
				foreach (var pendingRelation in rgRelation)
				{
					m_rgPendingTreeTargets.Add(pendingRelation);
				}
			}
		}

		private void StoreRelationResidue(ILexReference lr, PendingRelation pend)
		{
			var sResidue = pend.Residue;
			if (!string.IsNullOrEmpty(sResidue) || IsDateSet(pend.DateCreated) || IsDateSet(pend.DateModified))
			{
				var bldr = new StringBuilder();
				bldr.Append("<lift-residue");
				AppendXmlDateAttributes(bldr, pend.DateCreated, pend.DateModified);
				bldr.AppendLine(">");
				if (!string.IsNullOrEmpty(sResidue))
				{
					bldr.Append(sResidue);
				}
				bldr.Append("</lift-residue>");
				lr.LiftResidue = bldr.ToString();
			}
		}

		/// <summary>
		/// This method will test if a TreeRelation already exists in a lex reference of the given ILexRefType
		/// related to the given list of pending relations.
		/// If it does then those relations which are not yet included will be added to the matching ILexReference.
		/// </summary>
		private bool TreeRelationAlreadyExists(ICollection<ILexReference> originalLexRefs, ILexRefType lrt, List<PendingRelation> rgRelation)
		{
			foreach (var lr in lrt.MembersOC) //for every potential reference
			{
				if (lr.TargetsRS.Count == 0) //why this should be I don't know, probably due to some defect elsewhere.
				{
					continue;
				}
				var firstTargetHvo = lr.TargetsRS.First().Hvo;
				if (firstTargetHvo == rgRelation[0].ObjectHvo) //if the target of the first relation is the first item in the list
				{
					// Store the original contents of the LexReference to track removals
					var relationsBeforeAdditions = new List<ICmObject>(lr.TargetsRS.AsEnumerable());
					foreach (var pendingRelation in rgRelation)
					{
						var pendingObj = GetObjectForId(pendingRelation.TargetHvo);
						if (firstTargetHvo == pendingRelation.ObjectHvo && HasMatchingUnicodeAlternative(pendingRelation.RelationType.ToLowerInvariant(), lrt.Abbreviation, lrt.Name))
						{
							if (!lr.TargetsRS.Contains(pendingObj))
							{
								lr.TargetsRS.Add(pendingObj); // Add each item which is not yet in this list
							}
							else
							{
								relationsBeforeAdditions.Remove(pendingObj); // The object was found in the relation
							}
						}
						else
						{
							relationsBeforeAdditions.Remove(pendingObj); // The pending relation wasn't for this LexRef, so don't store pendingObj for removal
							m_rgPendingTreeTargets.Add(pendingRelation);
						}
					}
					originalLexRefs.Remove(lr);
					// Consider removing every object that wasn't matched from the tree relationship
					foreach (var unusedObject in relationsBeforeAdditions)
					{
						// Check the LexReference for validity as removing an object could trigger its deletion.
						// We will receive the relation with the head of the tree and the relation with the children in separate method calls.
						// Skipping the removal of an unusedObject when the Hvo matches the CmObject that owns the relations prevents removing the head
						// when processing the children.
						if (lr.IsValidObject && unusedObject.Hvo != rgRelation[0].CmObject.Hvo)
						{
							lr.TargetsRS.Remove(unusedObject);
						}
					}
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// This method will test if a TreeRelation already exists in a lex reference of the given ILexRefType
		/// related to the given PendingRelation.
		/// If it does then the relation will be added to the matching ILexReference if necessary.
		/// </summary>
		private bool TreeRelationAlreadyExists(ICollection<ILexReference> originalLexRefs, ILexRefType lrt, PendingRelation rel)
		{
			//The object who contains the other end of this relation, i.e. the entry with the Part
			//may not have been processed, so if we are dealing with the Whole we need to make sure that
			//the part is present.
			if (!ObjectIsFirstInRelation(rel.RelationType, lrt))
			{
				foreach (var lr in lrt.MembersOC)
				{
					if (lr.TargetsRS.Count == 0 || lr.TargetsRS[0].Hvo != rel.TargetHvo)
					{
						continue;
					}
					var pendingObj = GetObjectForId(rel.ObjectHvo);
					if (!lr.TargetsRS.Contains(pendingObj))
					{
						lr.TargetsRS.Add(pendingObj);
					}
					originalLexRefs.Remove(lr);
					return true;
				}
			}
			else
			{
				foreach (var lr in lrt.MembersOC)
				{
					if (lr.TargetsRS.Count == 0 || lr.TargetsRS[0].Hvo != rel.ObjectHvo)
					{
						continue;
					}
					var pendingObj = GetObjectForId(rel.TargetHvo);
					if (!lr.TargetsRS.Contains(pendingObj))
					{
						lr.TargetsRS.Add(pendingObj);
					}
					originalLexRefs.Remove(lr);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// This method will return true if the given type is the name, or main part of this reference type.
		/// as opposed to the ReversalName i.e. in a Part/Whole relation Part would return true, where Whole would
		/// return false.
		/// </summary>
		private bool ObjectIsFirstInRelation(string sType, ILexRefType lrt)
		{
			return HasMatchingUnicodeAlternative(sType.ToLowerInvariant(), lrt.Abbreviation, lrt.Name);
		}

		private List<PendingRelation> CollectRelationMembers(int i)
		{
			if (i < 0 || i >= m_rgPendingRelation.Count)
			{
				return null;
			}
			var relation = new List<PendingRelation>();
			PendingRelation prev = null;
			var hvo = m_rgPendingRelation[i].ObjectHvo;
			var sType = m_rgPendingRelation[i].RelationType;
			var dateCreated = m_rgPendingRelation[i].DateCreated;
			var dateModified = m_rgPendingRelation[i].DateModified;
			var sResidue = m_rgPendingRelation[i].Residue;
			var needSort = false;
			while (i < m_rgPendingRelation.Count)
			{
				var pend = m_rgPendingRelation[i];
				// If the object or relation type (or residue) has changed, we're into another
				// lexical relation.
				if (pend.ObjectHvo != hvo || pend.RelationType != sType ||
					pend.DateCreated != dateCreated || pend.DateModified != dateModified ||
					pend.Residue != sResidue)
				{
					break;
				}
				// If the sequence items are out of order we must sort them.
				if (prev != null && pend.Order < prev.Order)
				{
					needSort = true;
				}
				pend.Target = GetObjectFromTargetIdString(m_rgPendingRelation[i].TargetId);
				relation.Add(pend); // We handle missing/unrecognized targets later.
				prev = pend;
				++i;
			}
			// In the (typically unusual) case that the elements of the relation are out of order,
			// sort them. We prefer to use the Linq OrderBy method rather than the Sort method
			// of List because it is stable (that is, if some objects have the same Order we will
			// at least keep ones with the same Order value in their original order).
			// Enhance JohnT: it is possible that items appear out of order because they were exported
			// from two LexReferenceType objects with the same name (pend.RelationType) on the same
			// source object (pend.ObjectHvo). We can't currently recreate this situation on import.
			// See LT-15389.
			return needSort ? relation.OrderBy(pend => pend.Order).ToList() : relation;
		}

		private void GatherUnwantedObjects(List<ILexEntryRef> unusedLexEntryRefs, IEnumerable<ILexReference> unusedLexRefs)
		{
			foreach (var lexRef in unusedLexRefs)
			{
				m_deletedObjects.Add(lexRef.Hvo);
			}
			foreach (var lexEntryRef in unusedLexEntryRefs)
			{
				m_deletedObjects.Add(lexEntryRef.Hvo);
			}
			foreach (var le in m_cache.LangProject.LexDbOA.Entries)
			{
				if (!m_setUnchangedEntry.Contains(le.Guid) && !m_setChangedEntry.Contains(le.Guid))
				{
					m_deletedObjects.Add(le.Hvo);
				}
			}
		}

		private void DeleteUnwantedObjects()
		{
			if (m_deletedObjects.Count > 0)
			{
				DeleteObjects(m_deletedObjects);
				DeleteOrphans();
			}
		}

		/// <summary>
		/// This pretends to replace CmObject.DeleteObjects() in the old system.
		/// </summary>
		/// <param name="deletedObjects"></param>
		private void DeleteObjects(HashSet<int> deletedObjects)
		{
			foreach (var hvo in deletedObjects)
			{
				try
				{
					var cmo = GetObjectForId(hvo);
					var hvoOwner = cmo.Owner?.Hvo ?? 0;
					var flid = cmo.OwningFlid;
					m_cache.MainCacheAccessor.DeleteObjOwner(hvoOwner, hvo, flid, -1);
				}
				catch
				{
				}
			}
		}

		/// <summary>
		/// This replaces CmObject.DeleteOrphanedObjects(m_cache, false, null); in the
		/// old system, which used SQL extensively.  I'm not sure where this should go in
		/// the new system, or if it was used anywhere else.
		/// </summary>
		private void DeleteOrphans()
		{
			var orphans = new HashSet<int>();
			// Look for LexReference objects that have lost all their targets.
			var repoLR = m_cache.ServiceLocator.GetInstance<ILexReferenceRepository>();
			foreach (var lr in repoLR.AllInstances())
			{
				if (lr.TargetsRS.Count == 0)
				{
					orphans.Add(lr.Hvo);
				}
			}
			DeleteObjects(orphans);
			orphans.Clear();
			// Look for MSAs that are not used by any senses.
			var repoMsa = m_cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>();
			foreach (var msa in repoMsa.AllInstances())
			{
				var le = msa.Owner as ILexEntry;
				if (le == null)
				{
					continue;
				}
				var fUsed = false;
				foreach (var ls in le.AllSenses)
				{
					if (ls.MorphoSyntaxAnalysisRA == msa)
					{
						fUsed = true;
						break;
					}
				}
				if (!fUsed)
				{
					orphans.Add(msa.Hvo);
				}
			}
			DeleteObjects(orphans);
			orphans.Clear();
			// Look for WfiAnalysis objects that are not targeted by a human CmAgentEvaluation
			// and which do not own a WfiMorphBundle with a set Msa value.
			var repoWA = m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>();
			var cmaHuman = GetObjectForGuid(CmAgentTags.kguidAgentDefUser) as ICmAgent;
			Debug.Assert(cmaHuman != null);
			foreach (var wa in repoWA.AllInstances())
			{
				if (wa.GetAgentOpinion(cmaHuman) == Opinions.noopinion)
				{
					var fOk = wa.MorphBundlesOS.Any(wmb => wmb.MsaRA != null);
					if (!fOk)
					{
						orphans.Add(wa.Hvo);
					}
				}
			}
			DeleteObjects(orphans);
			orphans.Clear();

			// Update WfiMorphBundle.Form and WfiMorphBundle.Msa as needed.
			var repoWMB = m_cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>();
			foreach (var mb in repoWMB.AllInstances())
			{
				if (mb.Form.StringCount == 0 && mb.MorphRA == null && mb.MsaRA == null && mb.SenseRA == null)
				{
					var wa = mb.Owner as IWfiAnalysis;
					var wf = wa.Owner as IWfiWordform;
					var tssWordForm = wf.Form.get_String(m_cache.DefaultVernWs);
					if (tssWordForm != null && tssWordForm.Length > 0)
					{
						mb.Form.set_String(m_cache.DefaultVernWs, tssWordForm.Text);
					}
				}
				if (mb.MsaRA == null && mb.SenseRA != null)
				{
					mb.MsaRA = mb.SenseRA.MorphoSyntaxAnalysisRA;
				}
			}
			// Look for MoMorphAdhocProhib objects that don't have any Morphemes (MSA targets)
			var repoMAP = m_cache.ServiceLocator.GetInstance<IMoMorphAdhocProhibRepository>();
			foreach (var map in repoMAP.AllInstances())
			{
				if (map.MorphemesRS.Count == 0)
				{
					orphans.Add(map.Hvo);
				}
			}
			DeleteObjects(orphans);
			orphans.Clear();
		}
		#endregion // Methods for handling relation links

		#region Methods for getting or creating model objects

		internal ICmObject GetObjectForId(int hvo)
		{
			if (m_repoCmObject == null)
			{
				m_repoCmObject = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			}
			try
			{
				return m_repoCmObject.GetObject(hvo);
			}
			catch (KeyNotFoundException)
			{
				return null;
			}
		}

		internal ICmObject GetObjectForGuid(Guid guid)
		{
			if (m_repoCmObject == null)
			{
				m_repoCmObject = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			}
			return m_repoCmObject.IsValidObjectId(guid) ? m_repoCmObject.GetObject(guid) : null;
		}

		internal CoreWritingSystemDefinition GetExistingWritingSystem(int handle)
		{
			return m_cache.ServiceLocator.WritingSystemManager.Get(handle);
		}


		internal IMoMorphType GetExistingMoMorphType(Guid guid)
		{
			if (m_repoMoMorphType == null)
			{
				m_repoMoMorphType = m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			}
			return m_repoMoMorphType.GetObject(guid);
		}

		internal ICmAnthroItem CreateNewCmAnthroItem(string guidAttr, ICmObject owner)
		{
			if (m_factCmAnthroItem == null)
			{
				m_factCmAnthroItem = m_cache.ServiceLocator.GetInstance<ICmAnthroItemFactory>();
			}
			if (!string.IsNullOrEmpty(guidAttr))
			{
				var guid = (Guid)GuidConv.ConvertFrom(guidAttr);
				return owner is ICmAnthroItem ? m_factCmAnthroItem.Create(guid, owner as ICmAnthroItem) : m_factCmAnthroItem.Create(guid, owner as ICmPossibilityList);
			}
			var cai = m_factCmAnthroItem.Create();
			if (owner is ICmAnthroItem)
			{
				((ICmAnthroItem)owner).SubPossibilitiesOS.Add(cai);
			}
			else
			{
				((ICmPossibilityList)owner).PossibilitiesOS.Add(cai);
			}
			return cai;
		}

		internal ICmAnthroItem CreateNewCmAnthroItem()
		{
			if (m_factCmAnthroItem == null)
			{
				m_factCmAnthroItem = m_cache.ServiceLocator.GetInstance<ICmAnthroItemFactory>();
			}
			return m_factCmAnthroItem.Create();
		}

		internal ICmSemanticDomain CreateNewCmSemanticDomain()
		{
			if (m_factCmSemanticDomain == null)
			{
				m_factCmSemanticDomain = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>();
			}
			return m_factCmSemanticDomain.Create();
		}

		internal ICmSemanticDomain CreateNewCmSemanticDomain(string guidAttr, ICmObject owner)
		{
			if (m_factCmSemanticDomain == null)
			{
				m_factCmSemanticDomain = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>();
			}
			if (!string.IsNullOrEmpty(guidAttr))
			{
				var guid = (Guid)GuidConv.ConvertFrom(guidAttr);
				return owner is ICmSemanticDomain ? m_factCmSemanticDomain.Create(guid, owner as ICmSemanticDomain) : m_factCmSemanticDomain.Create(guid, owner as ICmPossibilityList);
			}
			var csd = m_factCmSemanticDomain.Create();
			if (owner is ICmSemanticDomain)
			{
				((ICmSemanticDomain)owner).SubPossibilitiesOS.Add(csd);
			}
			else
			{
				((ICmPossibilityList)owner).PossibilitiesOS.Add(csd);
			}
			return csd;
		}

		internal IMoStemAllomorph CreateNewMoStemAllomorph()
		{
			if (m_factMoStemAllomorph == null)
			{
				m_factMoStemAllomorph = m_cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			}
			return m_factMoStemAllomorph.Create();
		}

		internal IMoAffixAllomorph CreateNewMoAffixAllomorph()
		{
			if (m_factMoAffixAllomorph == null)
			{
				m_factMoAffixAllomorph = m_cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>();
			}
			return m_factMoAffixAllomorph.Create();
		}

		internal ILexPronunciation CreateNewLexPronunciation()
		{
			if (m_factLexPronunciation == null)
			{
				m_factLexPronunciation = m_cache.ServiceLocator.GetInstance<ILexPronunciationFactory>();
			}
			return m_factLexPronunciation.Create();
		}

		internal ICmMedia CreateNewCmMedia()
		{
			if (m_factCmMedia == null)
			{
				m_factCmMedia = m_cache.ServiceLocator.GetInstance<ICmMediaFactory>();
			}
			return m_factCmMedia.Create();
		}

		internal ILexEtymology CreateNewLexEtymology()
		{
			if (m_factLexEtymology == null)
			{
				m_factLexEtymology = m_cache.ServiceLocator.GetInstance<ILexEtymologyFactory>();
			}
			return m_factLexEtymology.Create();
		}

		internal ILexSense CreateNewLexSense(Guid guid, ICmObject owner, out bool fNeedNewId)
		{
			Debug.Assert(owner != null);
			Debug.Assert(owner is ILexEntry || owner is ILexSense);
			if (m_factLexSense == null)
			{
				m_factLexSense = m_cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			}
			fNeedNewId = false;
			ILexSense ls = null;
			if (guid != Guid.Empty && GuidIsNotInUse(guid))
			{
				ls = owner is ILexEntry ? m_factLexSense.Create(guid, owner as ILexEntry) : m_factLexSense.Create(guid, owner as ILexSense);
			}
			if (ls == null)
			{
				ls = m_factLexSense.Create();
				if (owner is ILexEntry)
				{
					((ILexEntry)owner).SensesOS.Add(ls);
				}
				else
				{
					((ILexSense)owner).SensesOS.Add(ls);
				}
				fNeedNewId = guid != Guid.Empty;
			}
			return ls;
		}

		private bool GuidIsNotInUse(Guid guid)
		{
			if (m_deletedGuids.Contains(guid))
			{
				return false;
			}
			if (m_repoCmObject == null)
			{
				m_repoCmObject = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			}
			return !m_repoCmObject.IsValidObjectId(guid);
		}

		private ILexEntry CreateNewLexEntry(Guid guid, out bool fNeedNewId)
		{
			if (m_factLexEntry == null)
			{
				m_factLexEntry = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			}
			fNeedNewId = false;
			ILexEntry le = null;
			if (guid != Guid.Empty && GuidIsNotInUse(guid))
			{
				le = m_factLexEntry.Create(guid, m_cache.LangProject.LexDbOA);
			}
			if (le == null)
			{
				le = m_factLexEntry.Create();
				fNeedNewId = guid != Guid.Empty;
			}
			return le;
		}

		internal IMoInflClass CreateNewMoInflClass()
		{
			if (m_factMoInflClass == null)
			{
				m_factMoInflClass = m_cache.ServiceLocator.GetInstance<IMoInflClassFactory>();
			}
			return m_factMoInflClass.Create();
		}

		internal IMoInflAffixSlot CreateNewMoInflAffixSlot()
		{
			if (m_factMoInflAffixSlot == null)
			{
				m_factMoInflAffixSlot = m_cache.ServiceLocator.GetInstance<IMoInflAffixSlotFactory>();
			}
			return m_factMoInflAffixSlot.Create();
		}

		internal ILexExampleSentence CreateNewLexExampleSentence(Guid guid, ILexSense owner)
		{
			Debug.Assert(owner != null);
			if (m_factLexExampleSentence == null)
			{
				m_factLexExampleSentence = m_cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>();
			}
			if (guid != Guid.Empty && GuidIsNotInUse(guid))
			{
				return m_factLexExampleSentence.Create(guid, owner);
			}
			var les = m_factLexExampleSentence.Create();
			owner.ExamplesOS.Add(les);
			return les;
		}

		internal ICmTranslation CreateNewCmTranslation(ILexExampleSentence les, ICmPossibility type)
		{
			if (m_factCmTranslation == null)
			{
				m_factCmTranslation = m_cache.ServiceLocator.GetInstance<ICmTranslationFactory>();
			}
			var fNoType = type == null;
			if (fNoType)
			{
				ICmObject obj;
				if (m_repoCmObject.TryGetObject(LangProjectTags.kguidTranFreeTranslation, out obj))
				{
					type = obj as ICmPossibility;
				}

				if (type == null)
				{
					type = FindOrCreateTranslationType("Free translation");
				}
			}
			var trans = m_factCmTranslation.Create(les, type);
			if (fNoType)
			{
				trans.TypeRA = null;
			}
			return trans;
		}

		internal ILexEntryType CreateNewLexEntryType()
		{
			if (m_factLexEntryType == null)
			{
				m_factLexEntryType = m_cache.ServiceLocator.GetInstance<ILexEntryTypeFactory>();
			}
			return m_factLexEntryType.Create();
		}

		/// <summary>
		/// Will create a new ref type using the guid if one is passed. The type will be owned by the owner guid if one is passed.
		/// </summary>
		internal ILexRefType CreateNewLexRefType(string guid, string owner)
		{
			if (m_factLexRefType == null)
			{
				m_factLexRefType = m_cache.ServiceLocator.GetInstance<ILexRefTypeFactory>();
			}
			var cleanOwner = owner?.ToLowerInvariant();
			return m_factLexRefType.Create(string.IsNullOrEmpty(guid) ? Guid.Empty : new Guid(guid), m_cache.ServiceLocator.GetAllInstances<ILexRefType>().FirstOrDefault(x => x.Guid.ToString().ToLowerInvariant() == cleanOwner));
		}

		internal ICmPossibility CreateNewCmPossibility()
		{
			if (m_factCmPossibility == null)
			{
				m_factCmPossibility = m_cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
			}
			return m_factCmPossibility.Create();
		}

		internal ICmPossibility CreateNewCmPossibility(string guidAttr, ICmObject owner)
		{
			if (m_factCmPossibility == null)
			{
				m_factCmPossibility = m_cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
			}
			if (!string.IsNullOrEmpty(guidAttr))
			{
				var guid = (Guid)GuidConv.ConvertFrom(guidAttr);
				return owner is ICmPossibility ? m_factCmPossibility.Create(guid, owner as ICmPossibility) : m_factCmPossibility.Create(guid, owner as ICmPossibilityList);
			}
			var csd = m_factCmPossibility.Create();
			if (owner is ICmPossibility)
			{
				((ICmPossibility)owner).SubPossibilitiesOS.Add(csd);
			}
			else
			{
				((ICmPossibilityList)owner).PossibilitiesOS.Add(csd);
			}
			return csd;
		}

		private ICmPossibility CreateNewCustomPossibility(string guidAttr, ICmObject owner)
		{
			var customPossibilityFactory = m_cache.ServiceLocator.GetInstance<ICmCustomItemFactory>();
			if (!string.IsNullOrEmpty(guidAttr))
			{
				var guid = (Guid)GuidConv.ConvertFrom(guidAttr);
				return owner is ICmPossibility ? customPossibilityFactory.Create(guid, owner as ICmCustomItem) : customPossibilityFactory.Create(guid, owner as ICmPossibilityList);
			}
			var csd = customPossibilityFactory.Create();
			var item = owner as ICmCustomItem;
			if (item != null)
			{
				item.SubPossibilitiesOS.Add(csd);
			}
			else
			{
				((ICmPossibilityList)owner).PossibilitiesOS.Add(csd);
			}
			return csd;
		}

		internal ICmLocation CreateNewCmLocation()
		{
			if (m_factCmLocation == null)
			{
				m_factCmLocation = m_cache.ServiceLocator.GetInstance<ICmLocationFactory>();
			}
			return m_factCmLocation.Create();
		}

		internal IMoStemMsa CreateNewMoStemMsa()
		{
			if (m_factMoStemMsa == null)
			{
				m_factMoStemMsa = m_cache.ServiceLocator.GetInstance<IMoStemMsaFactory>();
			}
			return m_factMoStemMsa.Create();
		}

		internal IMoUnclassifiedAffixMsa CreateNewMoUnclassifiedAffixMsa()
		{
			if (m_factMoUnclassifiedAffixMsa == null)
			{
				m_factMoUnclassifiedAffixMsa = m_cache.ServiceLocator.GetInstance<IMoUnclassifiedAffixMsaFactory>();
			}
			return m_factMoUnclassifiedAffixMsa.Create();
		}

		internal IMoDerivStepMsa CreateNewMoDerivStepMsa()
		{
			if (m_factMoDerivStepMsa == null)
			{
				m_factMoDerivStepMsa = m_cache.ServiceLocator.GetInstance<IMoDerivStepMsaFactory>();
			}
			return m_factMoDerivStepMsa.Create();
		}

		internal IMoDerivAffMsa CreateNewMoDerivAffMsa()
		{
			if (m_factMoDerivAffMsa == null)
			{
				m_factMoDerivAffMsa = m_cache.ServiceLocator.GetInstance<IMoDerivAffMsaFactory>();
			}
			return m_factMoDerivAffMsa.Create();
		}

		internal IMoInflAffMsa CreateNewMoInflAffMsa()
		{
			if (m_factMoInflAffMsa == null)
			{
				m_factMoInflAffMsa = m_cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>();
			}
			return m_factMoInflAffMsa.Create();
		}

		internal ICmPicture CreateNewCmPicture()
		{
			if (m_factCmPicture == null)
			{
				m_factCmPicture = m_cache.ServiceLocator.GetInstance<ICmPictureFactory>();
			}
			return m_factCmPicture.Create();
		}

		internal IReversalIndexEntry CreateNewReversalIndexEntry()
		{
			if (m_factReversalIndexEntry == null)
			{
				m_factReversalIndexEntry = m_cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>();
			}
			return m_factReversalIndexEntry.Create();
		}

		internal IReversalIndex FindOrCreateReversalIndex(int ws)
		{
			if (m_repoReversalIndex == null)
			{
				m_repoReversalIndex = m_cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			}
			return m_repoReversalIndex.FindOrCreateIndexForWs(ws);
		}

		internal IPartOfSpeech CreateNewPartOfSpeech()
		{
			if (m_factPartOfSpeech == null)
			{
				m_factPartOfSpeech = m_cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
			}
			return m_factPartOfSpeech.Create();
		}

		internal IPartOfSpeech CreateNewPartOfSpeech(string guidAttr, ICmObject owner)
		{
			Debug.Assert(owner != null);
			Debug.Assert(owner is ICmPossibilityList || owner is IPartOfSpeech);
			if (m_factPartOfSpeech == null)
			{
				m_factPartOfSpeech = m_cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
			}
			if (!string.IsNullOrEmpty(guidAttr))
			{
				var guid = (Guid)GuidConv.ConvertFrom(guidAttr);
				return owner is IPartOfSpeech ? m_factPartOfSpeech.Create(guid, owner as IPartOfSpeech) : m_factPartOfSpeech.Create(guid, owner as ICmPossibilityList);
			}
			var csd = m_factPartOfSpeech.Create();
			if (owner is IPartOfSpeech)
			{
				((IPartOfSpeech)owner).SubPossibilitiesOS.Add(csd);
			}
			else
			{
				((ICmPossibilityList)owner).PossibilitiesOS.Add(csd);
			}
			return csd;
		}

		internal IMoMorphType CreateNewMoMorphType(string guidAttr, ICmObject owner)
		{
			Debug.Assert(owner != null);
			Debug.Assert(owner is ICmPossibilityList || owner is IMoMorphType);
			if (m_factMoMorphType == null)
			{
				m_factMoMorphType = m_cache.ServiceLocator.GetInstance<IMoMorphTypeFactory>();
			}
			if (!string.IsNullOrEmpty(guidAttr))
			{
				var guid = (Guid)GuidConv.ConvertFrom(guidAttr);
				return owner is IMoMorphType ? m_factMoMorphType.Create(guid, owner as IMoMorphType) : m_factMoMorphType.Create(guid, owner as ICmPossibilityList);
			}
			var csd = m_factMoMorphType.Create();
			if (owner is IMoMorphType)
			{
				((IMoMorphType)owner).SubPossibilitiesOS.Add(csd);
			}
			else
			{
				((ICmPossibilityList)owner).PossibilitiesOS.Add(csd);
			}
			return csd;
		}

		private IPhEnvironment CreateNewPhEnvironment()
		{
			if (m_factPhEnvironment == null)
			{
				m_factPhEnvironment = m_cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>();
			}
			return m_factPhEnvironment.Create();
		}

		private ILexReference CreateNewLexReference()
		{
			if (m_factLexReference == null)
			{
				m_factLexReference = m_cache.ServiceLocator.GetInstance<ILexReferenceFactory>();
			}
			return m_factLexReference.Create();
		}

		private ILexEntryRef CreateNewLexEntryRef()
		{
			if (m_factLexEntryRef == null)
			{
				m_factLexEntryRef = m_cache.ServiceLocator.GetInstance<ILexEntryRefFactory>();
			}
			return m_factLexEntryRef.Create();
		}

		internal ICmPerson CreateNewCmPerson()
		{
			if (m_factCmPerson == null)
			{
				m_factCmPerson = m_cache.ServiceLocator.GetInstance<ICmPersonFactory>();
			}
			return m_factCmPerson.Create();
		}

		internal ICmPerson CreateNewCmPerson(string guidAttr, ICmObject owner)
		{
			if (!(owner is ICmPossibilityList))
			{
				throw new ArgumentException("Person should be in the People list", nameof(owner));
			}

			if (m_factCmPerson == null)
			{
				m_factCmPerson = m_cache.ServiceLocator.GetInstance<ICmPersonFactory>();
			}
			if (!string.IsNullOrEmpty(guidAttr))
			{
				var guid = (Guid)GuidConv.ConvertFrom(guidAttr);
				return m_factCmPerson.Create(guid, owner as ICmPossibilityList);
			}
			var csd = m_factCmPerson.Create();
			if (owner is ICmPossibility)
			{
				((ICmPossibility)owner).SubPossibilitiesOS.Add(csd);
			}
			else
			{
				((ICmPossibilityList)owner).PossibilitiesOS.Add(csd);
			}
			return csd;
		}

		private int GetWsFromStr(string sWs)
		{
			return m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(sWs);
		}

		#endregion // Methods for getting or creating model objects

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
				{
					m_PossibilityListGuids.Add(list.Guid);
				}
			}
			StoreStandardListsWithGuids();
			try
			{
				if (!File.Exists(sRangesFile))
				{
					return;
				}
				var xdoc = new XmlDocument();
				xdoc.Load(sRangesFile);
				foreach (XmlNode xn in xdoc.ChildNodes)
				{
					if (xn.Name != "lift-ranges")
					{
						continue;
					}
					foreach (XmlNode xnRange in xn.ChildNodes)
					{
						if (xnRange.Name != "range")
						{
							continue;
						}
						var range = XmlUtils.GetOptionalAttributeValue(xnRange, "id");
						if (RangeNames.RangeNameIsCustomList(range))
						{
							var rangeGuid = XmlUtils.GetOptionalAttributeValue(xnRange, "guid");
							//If the quick lookup dictionary and list have already been created for this custom list
							//then use them again it to account for any additional new range-elements.
							var dictCustomList = GetDictCustomList(range);
							var rgnewCustom = GetRgnewCustom(range);
							var customList = GetOrCreateCustomList(range, rangeGuid);
							Debug.Assert(customList != null);

							foreach (XmlNode xnElem in xnRange.ChildNodes)
							{
								if (xnElem.Name != "range-element")
								{
									continue;
								}
								ProcessCustomListRangeElement(xnElem, customList, dictCustomList, rgnewCustom);
							}
						}
						else
						{
							foreach (XmlNode xnElem in xnRange.ChildNodes)
							{
								if (xnElem.Name != "range-element")
								{
									continue;
								}
								ProcessRangeElement(xnElem, range);
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				m_rgErrorMsgs.Add("Error encountered processing ranges file: " + e.Message);
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

			AddListNameAndGuid(m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA, RangeNames.sProdRestrictOA);

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
				{
					return list;
				}
			}

			var ws = m_cache.DefaultUserWs; // get default ws
			var customPossibilityList = m_cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned(listGuid, customListName, ws);
			Debug.Assert(customListGuid == customPossibilityList.Guid.ToString());
			customPossibilityList.Name.set_String(m_cache.DefaultAnalWs, customListName);

			//Note: some of the following properties probably need to be set. Look in CustomListDlg.cs to see
			//where this code was originated from.  Are there some defaults that must be set?
			// Set various properties of CmPossibilityList
			customPossibilityList.DisplayOption = (int)PossNameType.kpntName;
			customPossibilityList.PreventDuplicates = true;
			customPossibilityList.WsSelector = WritingSystemServices.kwsAnalVerns;
			customPossibilityList.Depth = 127;

			//make sure we do not have a duplicate name for Custom Lists.
			m_PossibilityListGuids.Add(listGuid);
			m_rangeNamesToPossibilityListGuids.Add(customListName, listGuid);

			return customPossibilityList;
		}

		private void ProcessCustomListRangeElement(XmlNode xnElem, ICmPossibilityList possList, Dictionary<string, ICmPossibility> dictCustomList, List<ICmPossibility> rgnewCustom)
		{
			string guidAttr;
			string parent;
			LiftMultiText description; // non-safe-XML
			LiftMultiText label; // non-safe-XML
			LiftMultiText abbrev; // non-safe-XML
			var id = GetRangeElementDetails(xnElem, out guidAttr, out parent, out description, out label, out abbrev);

			ProcessPossibility(id, guidAttr, parent, MakeSafeLiftMultiText(description), MakeSafeLiftMultiText(label), MakeSafeLiftMultiText(abbrev), dictCustomList, rgnewCustom, possList, true);
		}

		private void ProcessRangeElement(XmlNode xnElem, string range)
		{
			string guidAttr;
			string parent;
			LiftMultiText description; // non-safe-XML
			LiftMultiText label; // non-safe-XML
			LiftMultiText abbrev; // non-safe-XML
			var id = GetRangeElementDetails(xnElem, out guidAttr, out parent, out description, out label, out abbrev);
			((ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>)this).ProcessRangeElement(range, id, guidAttr, parent, description, label, abbrev, xnElem.OuterXml);
		}


		/// <summary>
		/// Duplicates code in Palaso.LiftParser, but that doesn't read external range files, so...
		/// </summary>
		private string GetRangeElementDetails(XmlNode xnElem, out string guidAttr, out string parent, out LiftMultiText description, out LiftMultiText label, out LiftMultiText abbrev)
		{
			var id = XmlUtils.GetOptionalAttributeValue(xnElem, "id");
			parent = XmlUtils.GetOptionalAttributeValue(xnElem, "parent");
			guidAttr = XmlUtils.GetOptionalAttributeValue(xnElem, "guid");
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
			{
				label = ReadMultiText(xnLabel);
			}
			abbrev = null;
			if (xnAbbrev != null)
			{
				abbrev = ReadMultiText(xnAbbrev);
			}
			description = null;
			if (xnDescription != null)
			{
				description = ReadMultiText(xnDescription);
			}
			return id;
		}


		/// <summary>
		/// NOTE:  This method needs to be removed. The Palaso library (LiftParser and LiftMultiText) need to be refactored.
		/// Right now something like this does not work.... description = new LiftMultiText(xnDescription.OuterXml);
		/// because only OriginalOuterXml is assigned too.
		/// </summary>
		internal LiftMultiText ReadMultiText(XmlNode node)
		{
			var text = new LiftMultiText();
			ReadFormNodes(node.SelectNodes("form"), text);
			return text;
		}

		/// <summary>
		/// NOTE:  This method needs to be removed. The Palaso library (ListParser and LiftMultiText) need to be refactored.
		/// </summary>
		/// <param name="nodesWithForms"></param>
		/// <param name="text">text to add material to. Material is from XmlNode, hence non-safe XML (read from XmlNode without re-escaping).</param>
		private void ReadFormNodes(XmlNodeList nodesWithForms, LiftMultiText text)
		{
			foreach (XmlNode formNode in nodesWithForms)
			{
				try
				{
					var lang = Utilities.GetStringAttribute(formNode, "lang");
					var textNode = formNode.SelectSingleNode("text");
					if (textNode != null)
					{
						// Add the separator if we need it.
						if (textNode.InnerText.Length > 0)
						{
							text.AddOrAppend(lang, "", "; ");
						}
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
				}
				catch (Exception e)
				{
					// not a fatal error
				}
			}
		}



		#endregion //Import Ranges File

		/// <summary />
		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.ProcessRangeElement(string range, string id, string guidAttr, string parent,
				LiftMultiText description, LiftMultiText label, LiftMultiText abbrev, string rawXml)
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
				case "Publications":
					ProcessPossibilityPublications(id, guidAttr, parent, newDesc, newLabel, newAbbrev, m_dictPublicationTypes, m_rgnewPublicationType, m_cache.LangProject.LexDbOA.PublicationTypesOA);
					break;
				//New
				case RangeNames.sAffixCategoriesOA:
					ProcessPossibility(id, guidAttr, parent, newDesc, newLabel, newAbbrev, m_dictAffixCategories, m_rgAffixCategories, m_cache.LangProject.AffixCategoriesOA);
					break;
				//New
				case RangeNames.sAnnotationDefsOA:
					break;
				case RangeNames.sAnthroListOAold1: // original FLEX export
				case RangeNames.sAnthroListOA: // initialize map, adding to existing list if needed.
					ProcessAnthroItem(id, guidAttr, parent, newDesc, newLabel, newAbbrev);
					break;
				//New
				case RangeNames.sConfidenceLevelsOA:
				case RangeNames.sEducationOA:
				case RangeNames.sGenreListOA:
					break;
				case RangeNames.sLocationsOA:
					ProcessLocation(id, guidAttr, parent, newDesc, newLabel, newAbbrev);
					break;
				case RangeNames.sPartsOfSpeechOA: // map onto parts of speech?  extend as needed.
				case RangeNames.sPartsOfSpeechOAold2: // original FLEX export
				case RangeNames.sPartsOfSpeechOAold1: // map onto parts of speech?  extend as needed.
					ProcessPartOfSpeech(id, guidAttr, parent, newDesc, newLabel, newAbbrev);
					break;
				case RangeNames.sPeopleOA:
					ProcessPerson(id, guidAttr, parent, newDesc, newLabel, newAbbrev, m_dictPerson, m_rgnewPerson, m_cache.LangProject.PeopleOA);
					break;
				//New
				case RangeNames.sPositionsOA:
				case RangeNames.sRestrictionsOA:
				case RangeNames.sRolesOA:
					break;
				case RangeNames.sSemanticDomainListOAold1: // for WeSay 0.4 compatibility
				case RangeNames.sSemanticDomainListOAold2:
				case RangeNames.sSemanticDomainListOAold3:
				case RangeNames.sSemanticDomainListOA: // initialize map, adding to existing list if needed.
					ProcessSemanticDomain(id, guidAttr, parent, newDesc, newLabel, newAbbrev);
					break;
				case RangeNames.sStatusOA:
					ProcessPossibility(id, guidAttr, parent, newDesc, newLabel, newAbbrev, m_dictStatus, m_rgnewStatus, m_cache.LangProject.StatusOA);
					break;
				//New
				case RangeNames.sThesaurusRA:
					break;
				case RangeNames.sTranslationTagsOAold1: // original FLEX export
				case RangeNames.sTranslationTagsOA:
					ProcessPossibility(id, guidAttr, parent, newDesc, newLabel, newAbbrev, m_dictTransType, m_rgnewTransType, m_cache.LangProject.TranslationTagsOA);
					break;
				case RangeNames.sProdRestrictOA:
					EnsureProdRestrictListExists();
					ProcessPossibility(id, guidAttr, parent, newDesc, newLabel, newAbbrev, m_dictExceptFeats, m_rgnewExceptFeat, m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA);
					break;
				//New
				case RangeNames.sProdRestrictOAfrom:
					break;
				//lists under m_cache.LangProject.LexDbOA
				//New
				case RangeNames.sDbComplexEntryTypesOA:
					break;

				case RangeNames.sDbDialectLabelsOA:
					ProcessPossibility(id, guidAttr, parent, newDesc, newLabel, newAbbrev,
						m_dictDialect, m_rgnewDialects, m_cache.LangProject.LexDbOA.DialectLabelsOA);
					break;

				//New
				case RangeNames.sDbDomainTypesOA:
				case RangeNames.sDbDomainTypesOAold1:
					//handled already
					break;

				case RangeNames.sDbLanguagesOA:
					ProcessPossibility(id, guidAttr, parent, newDesc, newLabel, newAbbrev,
						m_dictLanguage, m_rgnewLanguage, m_cache.LangProject.LexDbOA.LanguagesOA);
					break;

				case RangeNames.sDbMorphTypesOAold: // original FLEX export
				case RangeNames.sDbMorphTypesOA:
					ProcessMorphType(id, guidAttr, parent, newDesc, newLabel, newAbbrev);
					break;

				//New
				case RangeNames.sDbPublicationTypesOAold:
				case RangeNames.sDbPublicationTypesOA:
					ProcessPossibility(id, guidAttr, parent, newDesc, newLabel, newAbbrev, m_dictPublicationTypes, m_rgnewPublicationType, m_cache.LangProject.LexDbOA.PublicationTypesOA);
					break;

				case RangeNames.sDbReferencesOAold: // original FLEX export
				case RangeNames.sDbReferencesOA: // lexical relation types (?)
					ProcessLexRefType(id, guidAttr, parent, newDesc, newLabel, newAbbrev, rawXml);
					break;
				//New
				case RangeNames.sDbSenseTypesOA:
				case RangeNames.sDbSenseTypesOAold1:
				case RangeNames.sDbUsageTypesOAold:
				case RangeNames.sDbUsageTypesOA:
				case RangeNames.sDbVariantEntryTypesOA:
					break;
				case RangeNames.sMSAinflectionFeature:
					if (m_cache.LangProject.MsFeatureSystemOA == null)
					{
						var fact = m_cache.ServiceLocator.GetInstance<IFsFeatureSystemFactory>();
						m_cache.LangProject.MsFeatureSystemOA = fact.Create();
					}
					ProcessFeatureDefinition(id, guidAttr, parent, newDesc, newLabel, newAbbrev, rawXml, m_cache.LangProject.MsFeatureSystemOA);
					break;

				//New
				case RangeNames.sMSAfromInflectionFeature:
					break;

				case RangeNames.sMSAinflectionFeatureType:
					if (m_cache.LangProject.MsFeatureSystemOA == null)
					{
						var fact = m_cache.ServiceLocator.GetInstance<IFsFeatureSystemFactory>();
						m_cache.LangProject.MsFeatureSystemOA = fact.Create();
					}
					ProcessFeatureStrucType(id, guidAttr, parent, newDesc, newLabel, newAbbrev, rawXml, m_cache.LangProject.MsFeatureSystemOA);
					break;

				//New
				case RangeNames.sReversalType:
					break;

				default:
					if (range.EndsWith("-slot") || range.EndsWith("-Slots"))
					{
						ProcessSlotDefinition(range, id, guidAttr, parent, newDesc, newLabel, newAbbrev);
					}
					else if (range.EndsWith("-infl-class") || range.EndsWith("-InflClasses"))
					{
						ProcessInflectionClassDefinition(range, id, guidAttr, parent, newDesc, newLabel, newAbbrev);
					}
					else if (range.EndsWith("-feature-value"))
					{
						ProcessFeatureValue(range, id, guidAttr, parent, newDesc, newLabel, newAbbrev, rawXml);
					}
					else if (range.EndsWith("-stem-name"))
					{
						ProcessStemName(range, id, guidAttr, parent, newDesc, newLabel, newAbbrev, rawXml);
					}
#if DEBUG
					else
					{
						Debug.WriteLine($"Unknown range '{range}' has element '{id}'");
					}
#endif
					break;
			}
		}

		private void ProcessLexRefType(string id, string guidAttr, string parent, LiftMultiText desc, LiftMultiText label,
												 LiftMultiText abbrev, string rawXml)
		{
			var xdoc = new XmlDocument();
			xdoc.LoadXml(rawXml);
			LiftTrait refTypeTrait = null;
			var traitNode = xdoc.FirstChild.SelectSingleNode("trait[@name='referenceType']");
			if (traitNode != null)
			{
				refTypeTrait = CreateLiftTraitFromXml(traitNode);
			}
			var reversalNameNode = xdoc.FirstChild.SelectSingleNode("field[@type='reverse-label']");
			LiftMultiText reverseName = null;
			if (reversalNameNode != null)
			{
				reverseName = CreateLiftMultiTextFromXml(reversalNameNode);
			}
			var revAbbrevNode = xdoc.FirstChild.SelectSingleNode("field[@type='reverse-abbrev']");
			LiftMultiText reverseAbbrev = null;
			if (revAbbrevNode != null)
			{
				reverseAbbrev = CreateLiftMultiTextFromXml(revAbbrevNode);
			}
			var refType = (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection;
			if (!string.IsNullOrEmpty(refTypeTrait?.Value))
			{
				refType = int.Parse(refTypeTrait.Value);
			}
			// If the guid and parent are both null this is probably a 'default' lexical-relation, we won't bother trying to create it
			// since it just uglies up our data. LT-14979
			if (guidAttr != null || parent != null)
			{
				FindOrCreateLexRefType(id, guidAttr, parent, desc, label, abbrev, reverseName, reverseAbbrev, refType);
			}
		}

		private void EnsureProdRestrictListExists()
		{
			if (m_cache.LangProject.MorphologicalDataOA == null)
			{
				var fact = m_cache.ServiceLocator.GetInstance<IMoMorphDataFactory>();
				m_cache.LangProject.MorphologicalDataOA = fact.Create();
			}
			if (m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA == null)
			{
				var fact = m_cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();
				m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA = fact.Create();
			}
		}

		Dictionary<string, string> m_writingsytemChangeMap = new Dictionary<string, string>();

		/// <summary />
		public void LdmlFilesMigration(string sLIFTfilesPath, string sLIFTdatafile, string sLIFTrangesfile)
		{
			var ldmlFolder = Path.Combine(sLIFTfilesPath, "WritingSystems");

			// TODO (WS_FIX): do we need settings data mappers here?
			var migrator = new LdmlInFolderWritingSystemRepositoryMigrator(ldmlFolder, NoteMigration);
			migrator.Migrate();

			if (File.Exists(sLIFTrangesfile))
			{
				var xdocRanges = new XmlDocument();
				xdocRanges.Load(sLIFTrangesfile);
				var dataRanges = XElement.Parse(xdocRanges.InnerXml);
				LdmlDataLangMigration(dataRanges, sLIFTrangesfile);
			}

			if (File.Exists(sLIFTdatafile))
			{
				var xdocLiftData = new XmlDocument();
				xdocLiftData.Load(sLIFTdatafile);
				var dataLiftData = XElement.Parse(xdocLiftData.InnerXml);
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
				{
					continue; // pathological, but let's try to survive
				}
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


		internal void NoteMigration(int toVersion, IEnumerable<LdmlMigrationInfo> migrationInfo)
		{
			foreach (var info in migrationInfo)
			{
				// Not sure if it ever reports unchanged ones, but we don't care about them.
				if (info.LanguageTagBeforeMigration != info.LanguageTagAfterMigration)
				{
					m_writingsytemChangeMap[RemoveMultipleX(info.LanguageTagBeforeMigration.ToLowerInvariant())] = info.LanguageTagAfterMigration;
				}
				// Due to earlier bugs, FieldWorks projects sometimes contain cmn* writing systems in zh* files,
				// and the fwdata incorrectly labels this data using a tag based on the file name rather than the
				// language tag indicated by the internal properties. We attempt to correct this by also converting the
				// file tag to the new tag for this writing system.
				if (info.FileName.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
				{
					var fileNameTag = Path.GetFileNameWithoutExtension(info.FileName);
					if (fileNameTag != info.LanguageTagBeforeMigration)
					{
						m_writingsytemChangeMap[RemoveMultipleX(fileNameTag.ToLowerInvariant())] = info.LanguageTagAfterMigration;
					}
				}
			}
		}

		private static string RemoveMultipleX(string input)
		{
			var gotX = false;
			var result = new List<string>();
			foreach (var item in input.Split('-'))
			{
				if (item == "x")
				{
					if (gotX)
					{
						continue;
					}
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
			{
				return !newTag.Equals(oldTag, StringComparison.OrdinalIgnoreCase);
			}
			var cleaner = new IetfLanguageTagCleaner(oldTag);
			cleaner.Clean();
			// FieldWorks needs to handle this special case.
			if (cleaner.Language.ToLowerInvariant() == "cmn")
			{
				var region = cleaner.Region;
				if (string.IsNullOrEmpty(region))
				{
					region = "CN";
				}
				cleaner = new IetfLanguageTagCleaner("zh", cleaner.Script, region, cleaner.Variant, cleaner.PrivateUse);
			}
			newTag = cleaner.GetCompleteTag();
			while (m_writingsytemChangeMap.Values.Contains(newTag, StringComparer.OrdinalIgnoreCase))
			{
				// We can't use this tag because it would conflict with what we are mapping something else to.
				cleaner = new IetfLanguageTagCleaner(cleaner.Language, cleaner.Script, cleaner.Region, cleaner.Variant, GetNextDuplPart(cleaner.PrivateUse));
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

		internal static string LinkRef(ILexEntry le)
		{
			FwLinkArgs link = new FwLinkArgs(AreaServices.LexiconEditMachineName, le.Guid);
			return XmlUtils.MakeSafeXmlAttribute(link.ToString());
		}

		#region Private classes/structs/enums

		/// <summary>
		/// MuElement = Multi-Unicode Element = one writing systemn and string of a multilingual
		/// unicode string.
		/// </summary>
		private struct MuElement
		{
			readonly string m_text;
			readonly int m_ws;
			public MuElement(int ws, string text)
			{
				m_text = text;
				m_ws = ws;
			}

			public override bool Equals(object obj)
			{
				if (obj is MuElement)
				{
					var that = (MuElement)obj;
					return m_text == that.m_text && m_ws == that.m_ws;
				}
				return false;
			}

			public override int GetHashCode()
			{
				return m_text.GetHashCode() + m_ws.GetHashCode();
			}
		}

		private abstract class ConflictingData
		{
			protected readonly FlexLiftMerger m_merger;

			protected ConflictingData(string sType, string sField, FlexLiftMerger merger)
			{
				ConflictType = sType;
				ConflictField = sField;
				m_merger = merger;
			}
			internal string ConflictType { get; }

			internal string ConflictField { get; }
			internal abstract string OrigHtmlReference();
			internal abstract string DupHtmlReference();

		}

		private sealed class ConflictingEntry : ConflictingData
		{
			private readonly ILexEntry m_leOrig;
			private ILexEntry m_leNew;

			internal ConflictingEntry(string sField, ILexEntry leOrig, FlexLiftMerger merger)
				: base(LexTextControls.ksEntry, sField, merger)
			{
				m_leOrig = leOrig;
			}
			internal override string OrigHtmlReference()
			{
				//"<a href=\"{0}\">{1}</a>", LinkRef(m_leOrig), HtmlString(m_leOrig.Headword)
				return $"<a href=\"{LinkRef(m_leOrig)}\">{m_merger.TsStringAsHtml(m_leOrig.HeadWord, m_leOrig.Cache)}</a>";
			}

			internal override string DupHtmlReference()
			{
				//"<a href=\"{0}\">{1}</a>", LinkRef(m_leNew), HtmlString(m_leNew.Headword)
				return $"<a href=\"{LinkRef(m_leNew)}\">{m_merger.TsStringAsHtml(m_leNew.HeadWord, m_leNew.Cache)}</a>";
			}
			internal ILexEntry DupEntry
			{
				set { m_leNew = value; }
			}
		}

		private sealed class ConflictingSense : ConflictingData
		{
			private readonly ILexSense m_lsOrig;

			internal ConflictingSense(string sField, ILexSense lsOrig, FlexLiftMerger merger)
				: base(LexTextControls.ksSense, sField, merger)
			{
				m_lsOrig = lsOrig;
			}
			internal override string OrigHtmlReference()
			{
				return $"<a href=\"{LinkRef(m_lsOrig.Entry)}\">{m_merger.TsStringAsHtml(OwnerOutlineName(m_lsOrig), m_lsOrig.Cache)}</a>";
			}

			internal override string DupHtmlReference()
			{
				return $"<a href=\"{LinkRef(DupSense.Entry)}\">{m_merger.TsStringAsHtml(OwnerOutlineName(DupSense), DupSense.Cache)}</a>";
			}
			internal ILexSense DupSense { private get; set; }

			private static ITsString OwnerOutlineName(ILexSense lsOrig)
			{
				return lsOrig.OwnerOutlineNameForWs(lsOrig.Cache.DefaultVernWs);
			}
		}

		/// <summary>
		/// This is the base class for pending error reports.
		/// </summary>
		private class PendingErrorReport
		{
			private Guid m_guid;
			private readonly int m_flid;
			readonly int m_ws;
			protected readonly LcmCache m_cache;
			private readonly FlexLiftMerger m_merger;

			internal PendingErrorReport(Guid guid, int flid, int ws, LcmCache cache, FlexLiftMerger merger)
			{
				m_guid = guid;
				m_flid = flid;
				m_ws = ws;
				m_cache = cache;
				m_merger = merger;
			}

			internal virtual string FieldName => m_cache.MetaDataCacheAccessor.GetFieldName(m_flid);

			private ILexEntry Entry()
			{
				var cmo = m_merger.GetObjectForGuid(m_guid);
				if (cmo is ILexEntry)
				{
					return cmo as ILexEntry;
				}
				return cmo.OwnerOfClass<ILexEntry>();
			}

			internal string EntryHtmlReference()
			{
				var le = Entry();
				return le == null ? string.Empty : $"<a href=\"{LinkRef(le)}\">{m_merger.TsStringAsHtml(le.HeadWord, m_cache)}</a>";
			}

			internal string WritingSystem
			{
				get
				{
					if (m_ws > 0)
					{
						var ws = m_merger.GetExistingWritingSystem(m_ws);
						return ws.DisplayLabel;
					}
					return null;
				}
			}

			public override bool Equals(object obj)
			{
				var that = obj as PendingErrorReport;
				if (that != null && m_flid == that.m_flid && m_guid == that.m_guid && m_ws == that.m_ws)
				{
					if (m_cache != null && that.m_cache != null)
					{
						return m_cache == that.m_cache;
					}
					return m_cache == null && that.m_cache == null;
				}
				return false;
			}

			public override int GetHashCode()
			{
				return m_flid + m_ws + m_guid.GetHashCode() + (m_cache == null ? 0 : m_cache.GetHashCode());
			}
		}

		/// <summary>
		/// This class stores the data needed to construct a warning message to the user
		/// that some imported data is actually invalid.
		/// </summary>
		private sealed class InvalidData : PendingErrorReport
		{
			public InvalidData(string sMsg, Guid guid, int flid, string val, int ws, LcmCache cache, FlexLiftMerger merger)
				: base(guid, flid, ws, cache, merger)
			{
				ErrorMessage = sMsg;
				BadValue = val;
			}

			internal string ErrorMessage { get; }

			internal string BadValue { get; }

			public override bool Equals(object obj)
			{
				var that = obj as InvalidData;
				return that != null && ErrorMessage == that.ErrorMessage && BadValue == that.BadValue && base.Equals(obj);
			}

			public override int GetHashCode()
			{
				return base.GetHashCode() + (ErrorMessage == null ? 0 : ErrorMessage.GetHashCode()) +
					(BadValue == null ? 0 : BadValue.GetHashCode());
			}
		}

		/// <summary>
		/// This class stores the data needed to construct a warning message to the user
		/// that a relation element in the imported file is invalid.
		/// </summary>
		private sealed class InvalidRelation : PendingErrorReport
		{
			readonly PendingLexEntryRef m_pendRef;

			public InvalidRelation(PendingLexEntryRef pend, LcmCache cache, FlexLiftMerger merger)
				: base(pend.CmObject.Guid, 0, 0, cache, merger)
			{
				m_pendRef = pend;
			}

			internal string TypeName => m_pendRef.RelationType;

			internal string BadValue => m_pendRef.TargetId;

			internal string ErrorMessage
			{
				get
				{
					if (m_pendRef.CmObject is ILexEntry)
					{
						return LexTextControls.ksEntryInvalidRef;
					}

					Debug.Assert(m_pendRef.CmObject is ILexSense);
					return string.Format(LexTextControls.ksSenseInvalidRef, ((ILexSense)m_pendRef.CmObject).OwnerOutlineNameForWs(m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle).Text);
				}
			}

			internal override string FieldName => string.Empty;
		}

		/// <summary>
		/// This class stores the data needed to construct a warning message to the user
		/// that a relation element in the imported file is invalid.
		/// </summary>
		private sealed class CombinedCollection : PendingErrorReport
		{

			private ICmObject _cmObject;
			public CombinedCollection(ICmObject owner, LcmCache cache, FlexLiftMerger merger)
				: base(owner.Guid, 0, 0, cache, merger)
			{
				_cmObject = owner;
			}

			internal string TypeName
			{
				get; set;
			}

			internal string BadValue
			{
				get; set;
			}

			internal string ErrorMessage => string.Format(LexTextControls.ksAddedToCombinedCollection, BadValue, TypeName, EntryHtmlReference());
		}

		/// <summary>
		/// This class stores the information for one range element from a *-feature-value range.
		/// This is used only if the corresponding IFsClosedFeature object cannot be found.
		/// </summary>
		private sealed class PendingFeatureValue
		{
			readonly Guid m_guidLift;

			/// <summary />
			internal PendingFeatureValue(string featId, string id, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev, string catalogId, bool fShowInGloss, Guid guidLift)
			{
				FeatureId = featId;
				Id = id;
				CatalogId = catalogId;
				ShowInGloss = fShowInGloss;
				Abbrev = abbrev;
				Label = label;
				Description = description;
				m_guidLift = guidLift;
			}
			internal string FeatureId { get; }

			internal string Id { get; }

			internal string CatalogId { get; }

			internal bool ShowInGloss { get; }

			internal LiftMultiText Abbrev // safe XML
			{
				get;
			}

			internal LiftMultiText Label // safe XML
			{
				get;
			}

			internal LiftMultiText Description // safe XML
			{
				get;
			}
		}

		/// <summary>
		/// This stores information for a relation that will be set later because the
		/// target object may not have been imported yet.
		/// </summary>
		private sealed class PendingRelation
		{
			readonly CmLiftRelation m_rel;

			public PendingRelation(ICmObject obj, CmLiftRelation rel, string sResidue)
			{
				CmObject = obj;
				Target = null;
				m_rel = rel;
				Residue = sResidue;
			}

			public ICmObject CmObject { get; private set; }

			public int ObjectHvo => CmObject?.Hvo ?? 0;

			public string RelationType => m_rel.Type;

			public string TargetId => m_rel.Ref;

			public ICmObject Target { get; set; }

			public int TargetHvo => Target?.Hvo ?? 0;

			public string Residue { get; private set; }

			public DateTime DateCreated => m_rel.DateCreated;

			public DateTime DateModified => m_rel.DateModified;

			internal bool IsSameOrMirror(PendingRelation rel)
			{
				if (rel == this)
				{
					return true;
				}
				if (rel.RelationType != RelationType)
				{
					return false;
				}
				if (rel.ObjectHvo == ObjectHvo && rel.Target == Target)
				{
					return true;
				}
				return rel.ObjectHvo == TargetHvo && rel.Target == CmObject;
			}

			internal void MarkAsProcessed()
			{
				CmObject = null;
			}

			internal bool HasBeenProcessed()
			{
				return CmObject == null;
			}

			internal bool IsSequence => m_rel.Order >= 0;

			internal int Order => m_rel.Order;

			public override string ToString()
			{
				return $"PendingRelation: type=\"{m_rel.Type}\", order={m_rel.Order}, target={Target?.Hvo ?? 0}, objHvo={CmObject?.Hvo ?? 0}";
			}

			internal string AsResidueString()
			{
				if (Residue == null)
				{
					Residue = string.Empty;
				}
				return IsSequence ? $"<relation type=\"{XmlUtils.MakeSafeXmlAttribute(m_rel.Type)}\" ref=\"{XmlUtils.MakeSafeXmlAttribute(m_rel.Ref)}\" order=\"{m_rel.Order}\"/>{Environment.NewLine}" : $"<relation type=\"{XmlUtils.MakeSafeXmlAttribute(m_rel.Type)}\" ref=\"{XmlUtils.MakeSafeXmlAttribute(m_rel.Ref)}\"/>{Environment.NewLine}";
			}
		}

		/// <summary />
		private sealed class PendingLexEntryRef
		{
			readonly CmLiftRelation m_rel;
			int m_nHideMinorEntry;

			// preserve trait values from older LIFT files based on old FieldWorks model

			public PendingLexEntryRef(ICmObject obj, CmLiftRelation rel, CmLiftEntry entry)
			{
				CmObject = obj;
				m_rel = rel;
				EntryType = null;
				MinorEntryCondition = null;
				ExcludeAsHeadword = false;
				Summary = null;
				if (entry != null)
				{
					EntryType = entry.EntryType;
					MinorEntryCondition = entry.MinorEntryCondition;
					ExcludeAsHeadword = entry.ExcludeAsHeadword;
					ProcessRelationData();
					LexemeForm = entry.LexicalForm;
				}
			}

			private void ProcessRelationData()
			{
				var knownTraits = new List<LiftTrait>();
				foreach (var trait in m_rel.Traits)
				{
					switch (trait.Name)
					{
						case "complex-form-type":
							ComplexFormTypes.Add(trait.Value);
							knownTraits.Add(trait);
							break;
						case "variant-type":
							VariantTypes.Add(trait.Value);
							knownTraits.Add(trait);
							break;
						case "hide-minor-entry":
							int.TryParse(trait.Value, out m_nHideMinorEntry);
							knownTraits.Add(trait);
							break;
						case "is-primary":
							IsPrimary = (trait.Value.ToLowerInvariant() == "true");
							ExcludeAsHeadword = IsPrimary;
							knownTraits.Add(trait);
							break;
					}
				}

				foreach (var trait in knownTraits)
				{
					m_rel.Traits.Remove(trait);
				}
				var knownFields = new List<LiftField>();
				foreach (var field in m_rel.Fields)
				{
					if (field.Type == "summary")
					{
						Summary = field; // this had better be safe-XML
						knownFields.Add(field);
					}
				}
				foreach (var field in knownFields)
				{
					m_rel.Fields.Remove(field);
				}
			}

			public ICmObject CmObject { get; }

			public int ObjectHvo => CmObject?.Hvo ?? 0;

			public string RelationType => m_rel.Type;

			public string TargetId => m_rel.Ref;

			public ICmObject Target { get; set; }

			public DateTime DateCreated => m_rel.DateCreated;

			public DateTime DateModified => m_rel.DateModified;

			internal int Order => m_rel.Order;

			public string EntryType { get; }

			public string MinorEntryCondition { get; }

			public bool ExcludeAsHeadword { get; private set; }

			public List<string> ComplexFormTypes { get; } = new List<string>();

			public List<string> VariantTypes { get; } = new List<string>();

			public bool IsPrimary { get; private set; }

			public int HideMinorEntry
			{
				get { return m_nHideMinorEntry; }
				set { m_nHideMinorEntry = value; }
			}

			/// <summary>
			/// safe-XML
			/// </summary>
			public LiftField Summary { get; private set; }

			/// <summary>
			/// This is used to better error reporting to the user when the default vernacular of the LIFT import file
			/// does not match the project doing the import.
			/// safe-XML
			/// </summary>
			public LiftMultiText LexemeForm { get; }
		}

		private sealed class PendingModifyTime
		{
			readonly ILexEntry m_le;
			readonly DateTime m_dt;

			public PendingModifyTime(ILexEntry le, DateTime dt)
			{
				m_le = le;
				m_dt = dt;
			}

			public void SetModifyTime()
			{
				if (!AreDatesInSameSecond(m_le.DateModified.ToUniversalTime(), m_dt.ToUniversalTime()))
				{
					m_le.DateModified = m_dt;
				}
			}
		}

		/// <summary>
		/// This stores the information for one object's LIFT import residue.
		/// </summary>
		private sealed class LiftResidue
		{
			public LiftResidue(int flid, XmlDocument xdoc)
			{
				Flid = flid;
				Document = xdoc;
			}

			public int Flid { get; }

			public XmlDocument Document { get; }
		}

		private sealed class EticCategory
		{
			public string Id { get; set; }

			public string ParentId { get; set; }

			/// <summary>
			/// Dictionary mapping WS ids to non-safe-XML strings.
			/// </summary>
			public Dictionary<string, string> MultilingualName { get; } = new Dictionary<string, string>();

			/// <summary />
			public void SetName(string lang, string name)
			{
				if (MultilingualName.ContainsKey(lang))
					MultilingualName[lang] = name;
				else
					MultilingualName.Add(lang, name);
			}
			public Dictionary<string, string> MultilingualAbbrev { get; } = new Dictionary<string, string>();

			/// <summary />
			public void SetAbbrev(string lang, string abbrev)
			{
				if (MultilingualAbbrev.ContainsKey(lang))
				{
					MultilingualAbbrev[lang] = abbrev;
				}
				else
				{
					MultilingualAbbrev.Add(lang, abbrev);
				}
			}

			/// <summary>
			/// Values are non-safe XML
			/// </summary>
			public Dictionary<string, string> MultilingualDesc { get; } = new Dictionary<string, string>();

			/// <summary />
			public void SetDesc(string lang, string desc)
			{
				if (MultilingualDesc.ContainsKey(lang))
				{
					MultilingualDesc[lang] = desc;
				}
				else
				{
					MultilingualDesc.Add(lang, desc);
				}
			}
		}

		#endregion Private classes/structs/enums
	}
}