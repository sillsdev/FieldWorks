// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2008' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LiftMerger.cs
// Responsibility: SteveMc (original version by John Hatton as extension)
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Palaso.Lift;
using Palaso.Lift.Parsing;
using Palaso.WritingSystems;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
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
		readonly FdoCache m_cache;
		readonly ITsPropsFactory m_tpf = TsPropsFactoryClass.Create();
		ITsString m_tssEmpty;
		readonly GuidConverter m_gconv = (GuidConverter)TypeDescriptor.GetConverter(typeof(Guid));
		public const string LiftDateTimeFormat = "yyyy-MM-ddTHH:mm:ssK";	// wants UTC, but works with Local
		private readonly IWritingSystemManager m_wsManager;

		readonly Regex m_regexGuid = new Regex(
			"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private ConflictingData m_cdConflict = null;
		private List<ConflictingData> m_rgcdConflicts = new List<ConflictingData>();

		private List<EticCategory> m_rgcats = new List<EticCategory>();

		private string m_sLiftFile;
		// TODO WS: how should this be used in the new world?
		private string m_sLiftDir = null;
		private string m_sLiftProducer = null;		// the producer attribute in the lift element.
		private DateTime m_defaultDateTime = default(DateTime);
		private bool m_fCreatingNewEntry = false;
		private bool m_fCreatingNewSense = false;

		// save field specification information from the header.
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

		/// <summary>
		/// MuElement = Multi-Unicode Element = one writing systemn and string of a multilingual
		/// unicode string.
		/// </summary>
		struct MuElement
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
		readonly Dictionary<object, Dictionary<MuElement, List<IReversalIndexEntry>>> m_mapToMapToRie =
			new Dictionary<object, Dictionary<MuElement, List<IReversalIndexEntry>>>();

		/// <summary>Set of guids for elements/senses that were found in the LIFT file.</summary>
		readonly Set<Guid> m_setUnchangedEntry = new Set<Guid>();

		readonly Set<Guid> m_setChangedEntry = new Set<Guid>();
		readonly Set<int> m_deletedObjects = new Set<int>();

		public enum MergeStyle
		{
			/// <summary>When there's a conflict, keep the existing data.</summary>
			MsKeepOld = 1,
			/// <summary>When there's a conflict, keep the data in the LIFT file.</summary>
			MsKeepNew = 2,
			/// <summary>When there's a conflict, keep both the existing data and the data in the LIFT file.</summary>
			MsKeepBoth = 3,
			/// <summary>Throw away any existing entries/senses/... that are not in the LIFT file.</summary>
			MsKeepOnlyNew = 4
		}
		MergeStyle m_msImport = MergeStyle.MsKeepOld;

		bool m_fTrustModTimes;

		readonly List<IWritingSystem> m_addedWss = new List<IWritingSystem>();

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
		IReversalIndexFactory m_factReversalIndex;
		IPartOfSpeechFactory m_factPartOfSpeech;
		IMoMorphTypeFactory m_factMoMorphType;
		IPhEnvironmentFactory m_factPhEnvironment;
		ILexReferenceFactory m_factLexReference;
		ILexEntryRefFactory m_factLexEntryRef;

		IFsComplexFeatureFactory m_factFsComplexFeature;
		IFsOpenFeatureFactory m_factFsOpenFeature;
		IFsClosedFeatureFactory m_factFsClosedFeature;
		IFsFeatStrucTypeFactory m_factFsFeatStrucType;
		IFsSymFeatValFactory m_factFsSymFeatVal;
		IFsFeatStrucFactory m_factFsFeatStruc;
		IFsClosedValueFactory m_factFsClosedValue;
		IFsComplexValueFactory m_factFsComplexValue;

		#region Constructors and other initialization methods
		public FlexLiftMerger(FdoCache cache, MergeStyle msImport, bool fTrustModTimes)
		{
			m_cSensesAdded = 0;
			m_cache = cache;
			m_tssEmpty = cache.TsStrFactory.EmptyString(cache.DefaultUserWs);
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
				if (!String.IsNullOrEmpty(m_sLiftFile))
				{
					m_sLiftDir = Path.GetDirectoryName(m_sLiftFile);
					m_defaultDateTime = File.GetLastWriteTimeUtc(m_sLiftFile);
					StoreLiftProducer();
				}
			}
		}

		private void StoreLiftProducer()
		{
			XmlReaderSettings readerSettings = new XmlReaderSettings();
			readerSettings.ValidationType = ValidationType.None;
			readerSettings.IgnoreComments = true;
			using (XmlReader reader = XmlReader.Create(m_sLiftFile, readerSettings))
			{
				if (reader.IsStartElement("lift"))
					m_sLiftProducer = reader.GetAttribute("producer");
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
					if (String.IsNullOrEmpty(s) || m_dictMmt.ContainsKey(s))
						continue;
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
					string name = pos.Name.GetStringFromIndex(i, out ws).Text;
					if (!String.IsNullOrEmpty(name))
						posNames.Add(name);
				}
				if (posNames.Count == 0)
					posNames.Add(String.Empty);		// should never happen!
				foreach (var stem in pos.AllStemNames)
				{
					for (var i = 0; i < stem.Name.StringCount; ++i)
					{
						var name = stem.Name.GetStringFromIndex(i, out ws).Text;
						if (String.IsNullOrEmpty(name))
							continue;
						foreach (var posName in posNames)
						{
							var key = String.Format("{0}:{1}", posName, name);
							if (!m_dictStemName.ContainsKey(key))
								m_dictStemName.Add(key, stem);
						}
					}
				}
			}
		}

		private void InitializeReversalMaps()
		{
			foreach (IReversalIndex ri in m_cache.LangProject.LexDbOA.ReversalIndexesOC)
			{
				Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs =
					new Dictionary<MuElement, List<IReversalIndexEntry>>();
				m_mapToMapToRie.Add(ri, mapToRIEs);
				InitializeReversalMap(ri.EntriesOC, mapToRIEs);
			}
		}

		private void InitializeReversalMap(IFdoOwningCollection<IReversalIndexEntry> entries,
			Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs)
		{
			foreach (IReversalIndexEntry rie in entries)
			{
				for (int i = 0; i < rie.ReversalForm.StringCount; ++i)
				{
					int ws;
					ITsString tss = rie.ReversalForm.GetStringFromIndex(i, out ws);
					if (tss.Length > 0)
					{
						MuElement mue = new MuElement(ws, tss.Text);
						AddToReversalMap(mue, rie, mapToRIEs);
					}
				}
				if (rie.SubentriesOC.Count > 0)
				{
					Dictionary<MuElement, List<IReversalIndexEntry>> submapToRIEs =
						new Dictionary<MuElement, List<IReversalIndexEntry>>();
					m_mapToMapToRie.Add(rie, submapToRIEs);
					InitializeReversalMap(rie.SubentriesOC, submapToRIEs);
				}
			}
		}

		private void AddToReversalMap(MuElement mue, IReversalIndexEntry rie,
			Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs)
		{
			List<IReversalIndexEntry> rgrie;
			if (!mapToRIEs.TryGetValue(mue, out rgrie))
			{
				rgrie = new List<IReversalIndexEntry>();
				mapToRIEs.Add(mue, rgrie);
			}
			if (!rgrie.Contains(rie))
				rgrie.Add(rie);
		}

		private void InitializeReverseLexRefTypesMap()
		{
			int ws;
			foreach (ILexRefType lrt in m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS)
			{
				for (int i = 0; i < lrt.ReverseAbbreviation.StringCount; ++i)
				{
					ITsString tss = lrt.ReverseAbbreviation.GetStringFromIndex(i, out ws);
					AddToReverseLexRefTypesMap(tss, lrt);
				}
				for (int i = 0; i < lrt.ReverseName.StringCount; ++i)
				{
					ITsString tss = lrt.ReverseName.GetStringFromIndex(i, out ws);
					AddToReverseLexRefTypesMap(tss, lrt);
				}
			}
		}

		private void AddToReverseLexRefTypesMap(ITsString tss, ILexRefType lrt)
		{
			if (tss.Length > 0)
			{
				string s = tss.Text;
				if (!m_dictRevLexRefTypes.ContainsKey(s))
					m_dictRevLexRefTypes.Add(s, lrt);
				s = s.ToLowerInvariant();
				if (!m_dictRevLexRefTypes.ContainsKey(s))
					m_dictRevLexRefTypes.Add(s, lrt);
			}
		}

		private void LoadCategoryCatalog()
		{
			string sPath = System.IO.Path.Combine(DirectoryFinder.FWCodeDirectory,
				"Templates/GOLDEtic.xml");
			XmlDocument xd = new XmlDocument();
			xd.Load(sPath);
			XmlNode xnTop = xd.SelectSingleNode("eticPOSList");
			if (xnTop != null && xnTop.ChildNodes != null)
			{
				foreach (XmlNode node in xnTop.SelectNodes("item"))
				{
					string sType = XmlUtils.GetAttributeValue(node, "type");
					string sId = XmlUtils.GetAttributeValue(node, "id");
					if (sType == "category" && !String.IsNullOrEmpty(sId))
						LoadCategoryNode(node, sId, null);
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
					var sWs = XmlUtils.GetAttributeValue(xn, "ws");
					var sAbbrev = xn.InnerText;
					if (!String.IsNullOrEmpty(sWs) && !String.IsNullOrEmpty(sAbbrev))
						cat.SetAbbrev(sWs, sAbbrev);
				}
				foreach (XmlNode xn in node.SelectNodes("term"))
				{
					var sWs = XmlUtils.GetAttributeValue(xn, "ws");
					var sName = xn.InnerText;
					if (!String.IsNullOrEmpty(sWs) && !String.IsNullOrEmpty(sName))
						cat.SetName(sWs, sName);
				}
				foreach (XmlNode xn in node.SelectNodes("def"))
				{
					var sWs = XmlUtils.GetAttributeValue(xn, "ws");
					var sDesc = xn.InnerText;
					if (!String.IsNullOrEmpty(sWs) && !String.IsNullOrEmpty(sDesc))
						cat.SetDesc(sWs, sDesc);
				}
			}
			m_rgcats.Add(cat);
			if (node != null)
			{
				foreach (XmlNode xn in node.SelectNodes("item"))
				{
					var sType = XmlUtils.GetAttributeValue(xn, "type");
					var sChildId = XmlUtils.GetAttributeValue(xn, "id");
					if (sType == "category" && !String.IsNullOrEmpty(sChildId))
						LoadCategoryNode(xn, sChildId, id);
				}
			}
		}
		#endregion // Constructors and other initialization methods

		private bool IsVoiceWritingSystem(int wsString)
		{
			var wsEngine = m_wsManager.get_EngineOrNull(wsString);
			return wsEngine is WritingSystemDefinition && ((WritingSystemDefinition)wsEngine).IsVoice;
		}

		#region ILexiconMerger<LiftObject, LiftEntry, LiftSense, LiftExample> Members

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.EntryWasDeleted(
			Extensible info, DateTime dateDeleted)
		{
			Guid guid = info.Guid;
			if (guid == Guid.Empty)
				return;
			ICmObject cmo = GetObjectForGuid(guid);
			if (cmo == null)
				return;
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
				m_deletedGuids.Add(le.LexemeFormOA.Guid);
			if (le.EtymologyOA != null)
				m_deletedGuids.Add(le.EtymologyOA.Guid);
			foreach (var msa in le.MorphoSyntaxAnalysesOC)
				m_deletedGuids.Add(msa.Guid);
			foreach (var er in le.EntryRefsOS)
				m_deletedGuids.Add(er.Guid);
			foreach (var form in le.AlternateFormsOS)
				m_deletedGuids.Add(form.Guid);
			foreach (var pron in le.PronunciationsOS)
				m_deletedGuids.Add(pron.Guid);
			CollectGuidsFromDeletedSenses(le.SensesOS);
		}

		private void CollectGuidsFromDeletedSenses(IFdoOwningSequence<ILexSense> senses)
		{
			foreach (var ls in senses)
			{
				m_deletedGuids.Add(ls.Guid);
				foreach (var pict in ls.PicturesOS)
					m_deletedGuids.Add(pict.Guid);
				foreach (var ex in ls.ExamplesOS)
					m_deletedGuids.Add(ex.Guid);
				CollectGuidsFromDeletedSenses(ls.SensesOS);
			}
		}

		/// <summary>
		/// This method does all the real work of importing an entry into the lexicon.  Up to
		/// this point, we've just been building up a memory structure based on the LIFT data.
		/// </summary>
		/// <param name="entry"></param>
		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.FinishEntry(
			CmLiftEntry entry)
		{
			bool fCreateNew = entry.CmObject == null;
			if (!fCreateNew && m_msImport == MergeStyle.MsKeepBoth)
				fCreateNew = EntryHasConflictingData(entry);
			if (fCreateNew)
				CreateNewEntry(entry);
			else
				MergeIntoExistingEntry(entry);
		}

		CmLiftEntry ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.GetOrMakeEntry(
			Extensible info, int order)
		{
			Guid guid = GetGuidInExtensible(info);
			if (m_fTrustModTimes && SameEntryModTimes(info))
			{
				// If we're keeping only the imported data, remember this was imported!
				if (m_msImport == MergeStyle.MsKeepOnlyNew)
					m_setUnchangedEntry.Add(guid);
				return null;	// assume nothing has changed.
			}
			CmLiftEntry entry = new CmLiftEntry(info, guid, order, this);
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
				m_setChangedEntry.Add(entry.Guid);
			return entry;
		}

		CmLiftExample ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.GetOrMakeExample(
			CmLiftSense sense, Extensible info)
		{
			CmLiftExample example = new CmLiftExample();
			example.Id = info.Id;
			example.Guid = GetGuidInExtensible(info);
			example.CmObject = GetObjectForGuid(example.Guid);
			example.DateCreated = info.CreationTime;
			example.DateModified = info.ModificationTime;
			sense.Examples.Add(example);
			return example;
		}

		CmLiftSense ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.GetOrMakeSense(
			CmLiftEntry entry, Extensible info, string rawXml)
		{
			CmLiftSense sense = CreateLiftSenseFromInfo(info, entry);
			entry.Senses.Add(sense);
			return sense;
		}

		CmLiftSense ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.GetOrMakeSubsense(
			CmLiftSense sense, Extensible info, string rawXml)
		{
			CmLiftSense sub = CreateLiftSenseFromInfo(info, sense);
			sense.Subsenses.Add(sub);
			return sub;
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInCitationForm(
			CmLiftEntry entry, LiftMultiText contents)
		{
			var newContents = MakeSafeLiftMultiText(contents);
			if (entry.CitationForm == null)
				entry.CitationForm = newContents;
			else
				MergeLiftMultiTexts(entry.CitationForm, newContents);
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInDefinition(
			CmLiftSense sense, LiftMultiText contents)
		{
			var newContents = MakeSafeLiftMultiText(contents);
			if (sense.Definition == null)
				sense.Definition = newContents;
			else
				MergeLiftMultiTexts(sense.Definition, newContents);
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInExampleForm(
			CmLiftExample example, LiftMultiText contents)
		{
			var newContents = MakeSafeLiftMultiText(contents);
			if (example.Content == null)
				example.Content = newContents;
			else
				MergeLiftMultiTexts(example.Content, newContents);
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInField(
			LiftObject extensible, string tagAttribute, DateTime dateCreated, DateTime dateModified,
			LiftMultiText contents, List<Trait> traits)
		{
			LiftField field = new LiftField();
			field.Type = tagAttribute;
			field.DateCreated = dateCreated;
			field.DateModified = dateModified;
			field.Content = MakeSafeLiftMultiText(contents);
			foreach (Trait t in traits)
			{
				LiftTrait lt = new LiftTrait();
				lt.Name = t.Name;
				lt.Value = t.Value;
				field.Traits.Add(lt);
			}
			extensible.Fields.Add(field);
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInGloss(
			CmLiftSense sense, LiftMultiText contents)
		{
			var newContents = MakeSafeLiftMultiText(contents);
			if (sense.Gloss == null)
				sense.Gloss = newContents;
			else
				MergeLiftMultiTexts(sense.Gloss, newContents);
		}

		/// <summary>
		/// Only Sense and Reversal have grammatical information.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="val"></param>
		/// <param name="traits"></param>
		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInGrammaticalInfo(
			LiftObject obj, string val, List<Trait> traits)
		{
			LiftGrammaticalInfo graminfo = new LiftGrammaticalInfo();
			graminfo.Value = val;
			foreach (Trait t in traits)
			{
				LiftTrait lt = new LiftTrait();
				lt.Name = t.Name;
				lt.Value = t.Value;
				graminfo.Traits.Add(lt);
			}
			if (obj is CmLiftSense)
				(obj as CmLiftSense).GramInfo = graminfo;
			else if (obj is CmLiftReversal)
				(obj as CmLiftReversal).GramInfo = graminfo;
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInLexemeForm(
			CmLiftEntry entry, LiftMultiText contents)
		{
			var newContents = MakeSafeLiftMultiText(contents);
			if (entry.LexicalForm == null)
				entry.LexicalForm = newContents;
			else
				MergeLiftMultiTexts(entry.LexicalForm, newContents);
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInNote(
			LiftObject extensible, string type, LiftMultiText contents, string rawXml)
		{
			AddNewWsToAnalysis();
			var newContents = MakeSafeLiftMultiText(contents);
			CmLiftNote note = new CmLiftNote(type, newContents);
			// There may be <trait>, <field>, or <annotation> elements hidden in the
			// raw XML string.  Perhaps these should be arguments, but they aren't.
			FillInExtensibleElementsFromRawXml(note, rawXml);
			if (extensible is CmLiftEntry)
				(extensible as CmLiftEntry).Notes.Add(note);
			else if (extensible is CmLiftSense)
				(extensible as CmLiftSense).Notes.Add(note);
			else if (extensible is CmLiftExample)
				(extensible as CmLiftExample).Notes.Add(note);
			else
			{
				IgnoreNewWs();
				Debug.WriteLine(String.Format(
					"<note type='{1}'> (first content = '{2}') found in bad context: {0}",
					extensible.GetType().Name,
					type,
					GetFirstLiftTsString(newContents) == null ? "<null>" : GetFirstLiftTsString(newContents).Text));
			}
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInPicture(
			CmLiftSense sense, string href, LiftMultiText caption)
		{
			LiftUrlRef pict = new LiftUrlRef();
			pict.Url = href;
			pict.Label = MakeSafeLiftMultiText(caption);
			sense.Illustrations.Add(pict);
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInMedia(
			LiftObject obj, string href, LiftMultiText caption)
		{
			CmLiftPhonetic phon = obj as CmLiftPhonetic;
			if (phon != null)
			{
				LiftUrlRef url = new LiftUrlRef();
				url.Url = href;
				url.Label = MakeSafeLiftMultiText(caption);
				phon.Media.Add(url);
			}
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInRelation(
			LiftObject extensible, string relationTypeName, string targetId, string rawXml)
		{
			CmLiftRelation rel = new CmLiftRelation();
			rel.Type = relationTypeName;
			rel.Ref = targetId.Normalize();	// I've seen data with this as NFD!
			// order should be an argument of this method, but since it isn't (yet),
			// calculate the order whenever it appears to be relevant.
			// There may also be <trait>, <field>, or <annotation> elements hidden in the
			// raw XML string.  These should also be arguments, but aren't.
			FillInExtensibleElementsFromRawXml(rel, rawXml.Normalize());
			if (extensible is CmLiftEntry)
				(extensible as CmLiftEntry).Relations.Add(rel);
			else if (extensible is CmLiftSense)
				(extensible as CmLiftSense).Relations.Add(rel);
			else if (extensible is CmLiftVariant)
				(extensible as CmLiftVariant).Relations.Add(rel);
			else
				Debug.WriteLine(String.Format("<relation type='{0}' ref='{1}> found in bad context: {2}",
					relationTypeName, targetId, extensible.GetType().Name));
		}

		private static void FillInExtensibleElementsFromRawXml(LiftObject obj, string rawXml)
		{
			if (rawXml.IndexOf("<trait") > 0 ||
				rawXml.IndexOf("<field") > 0 ||
				rawXml.IndexOf("<annotation") > 0 ||
				(obj is CmLiftRelation && rawXml.IndexOf("order=") > 0))
			{
				XmlDocument xdoc = new XmlDocument();
				xdoc.LoadXml(rawXml);
				XmlNode node = xdoc.FirstChild;
				CmLiftRelation rel = obj as CmLiftRelation;
				if (rel != null)
				{
					string sOrder = XmlUtils.GetAttributeValue(node, "order", null);
					if (!String.IsNullOrEmpty(sOrder))
					{
						int order;
						if (Int32.TryParse(sOrder, out order))
							rel.Order = order;
						else
							rel.Order = 0;
					}
				}
				foreach (XmlNode xn in node.SelectNodes("field"))
				{
					LiftField field = CreateLiftFieldFromXml(xn);
					obj.Fields.Add(field);
				}
				foreach (XmlNode xn in node.SelectNodes("trait"))
				{
					LiftTrait trait = CreateLiftTraitFromXml(xn);
					obj.Traits.Add(trait);
				}
				foreach (XmlNode xn in node.SelectNodes("annotation"))
				{
					LiftAnnotation ann = CreateLiftAnnotationFromXml(xn);
					obj.Annotations.Add(ann);
				}
			}
		}

		/// <summary>
		/// Adapted from LiftParser.ReadExtensibleElementDetails()
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private static LiftField CreateLiftFieldFromXml(XmlNode node)
		{
			string fieldType = XmlUtils.GetManditoryAttributeValue(node, "type");
			string priorFieldWithSameTag = String.Format("preceding-sibling::field[@type='{0}']", fieldType);
			if (node.SelectSingleNode(priorFieldWithSameTag) != null)
			{
				// a fatal error
				throw new LiftFormatException(String.Format("Field with same type ({0}) as sibling not allowed. Context:{1}", fieldType, node.ParentNode.OuterXml));
			}
			LiftField field = new LiftField();
			field.Type = fieldType;
			field.DateCreated = GetOptionalDateTime(node, "dateCreated");
			field.DateModified = GetOptionalDateTime(node, "dateModified");
			field.Content = CreateLiftMultiTextFromXml(node);
			foreach (XmlNode xn in node.SelectNodes("trait"))
			{
				LiftTrait trait = CreateLiftTraitFromXml(xn);
				field.Traits.Add(trait);
			}
			foreach (XmlNode xn in node.SelectNodes("annotation"))
			{
				LiftAnnotation ann = CreateLiftAnnotationFromXml(xn);
				field.Annotations.Add(ann);
			}
			return field;
		}

		/// <summary>
		/// Adapted from LiftParser.ReadFormNodes()
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private static LiftMultiText CreateLiftMultiTextFromXml(XmlNode node)
		{
			LiftMultiText text = new LiftMultiText();
			foreach (XmlNode xnForm in node.SelectNodes("form"))
			{
				try
				{
					string lang = XmlUtils.GetAttributeValue(xnForm, "lang");
					XmlNode xnText = xnForm.SelectSingleNode("text");
					if (xnText != null)
					{
						// Add the separator if we need it.
						if (xnText.InnerText.Length > 0)
							text.AddOrAppend(lang, "", "; ");
						foreach (XmlNode xn in xnText.ChildNodes)
						{
							if (xn.Name == "span")
							{
								text.AddSpan(lang,
											 XmlUtils.GetOptionalAttributeValue(xn, "lang"),
											 XmlUtils.GetOptionalAttributeValue(xn, "class"),
											 XmlUtils.GetOptionalAttributeValue(xn, "href"),
											 xn.InnerText.Length);
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
		/// <param name="node"></param>
		/// <returns></returns>
		private static LiftTrait CreateLiftTraitFromXml(XmlNode node)
		{
			LiftTrait trait = new LiftTrait();
			trait.Name = XmlUtils.GetAttributeValue(node, "name");
			trait.Value = XmlUtils.GetAttributeValue(node, "value");
			foreach (XmlNode n in node.SelectNodes("annotation"))
			{
				LiftAnnotation ann = CreateLiftAnnotationFromXml(n);
				trait.Annotations.Add(ann);
			}
			return trait;
		}

		/// <summary>
		/// Adapted from LiftParser.GetAnnotation()
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private static LiftAnnotation CreateLiftAnnotationFromXml(XmlNode node)
		{
			LiftAnnotation ann = new LiftAnnotation();
			ann.Name = XmlUtils.GetOptionalAttributeValue(node, "name");
			ann.Value = XmlUtils.GetOptionalAttributeValue(node, "value");
			ann.When = GetOptionalDateTime(node, "when");
			ann.Who = XmlUtils.GetOptionalAttributeValue(node, "who");
			ann.Comment = CreateLiftMultiTextFromXml(node);
			return ann;
		}

		/// <summary>
		/// Adapted from LiftParser.GetOptionalDate()
		/// </summary>
		/// <param name="node"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		private static DateTime GetOptionalDateTime(XmlNode node, string tag)
		{
			string sWhen = XmlUtils.GetAttributeValue(node, tag);
			if (String.IsNullOrEmpty(sWhen))
			{
				return default(DateTime);
			}
			else
			{
				try
				{
					return Extensible.ParseDateTimeCorrectly(sWhen);
				}
				catch (FormatException)
				{
					return default(DateTime);
				}
			}
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInSource(
			CmLiftExample example, string source)
		{
			example.Source = source;
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInTrait(
			LiftObject extensible, Trait trait)
		{
			LiftTrait lt = new LiftTrait();
			lt.Value = trait.Value;
			lt.Name = trait.Name;
			foreach (Annotation t in trait.Annotations)
			{
				LiftAnnotation ann = new LiftAnnotation();
				ann.Name = t.Name;
				ann.Value = t.Value;
				ann.When = t.When;
				ann.Who = t.Who;
				lt.Annotations.Add(ann);
			}
			extensible.Traits.Add(lt);
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInTranslationForm(
			CmLiftExample example, string type, LiftMultiText contents, string rawXml)
		{
			LiftTranslation trans = new LiftTranslation();
			trans.Type = type;
			trans.Content = MakeSafeLiftMultiText(contents);
			example.Translations.Add(trans);
		}

		LiftObject ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInPronunciation(
			CmLiftEntry entry, LiftMultiText contents, string rawXml)
		{
			//if (contents == null || contents.IsEmpty)
			//    return null;
			CmLiftPhonetic phon = new CmLiftPhonetic();
			phon.Form = MakeSafeLiftMultiText(contents);
			entry.Pronunciations.Add(phon);
			return phon;
		}

		LiftObject ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInVariant(
			CmLiftEntry entry, LiftMultiText contents, string rawXml)
		{
			//if (contents == null || contents.IsEmpty)
			//    return null;
			CmLiftVariant var = new CmLiftVariant();
			var.Form = MakeSafeLiftMultiText(contents);
			// LiftIO handles "extensible", but not <pronunciation> or <relation>, so store the
			// raw XML for now.
			var.RawXml = rawXml;
			entry.Variants.Add(var);
			return var;
		}

		private static LiftMultiText MakeSafeLiftMultiText(LiftMultiText multiText)
		{
			if (multiText == null)
				return null;
			foreach (var lg in multiText.Keys)
				multiText[lg].Text = XmlUtils.ConvertMultiparagraphToSafeXml(multiText[lg].Text);
			return multiText;
		}

		LiftObject ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.GetOrMakeParentReversal(
			LiftObject parent, LiftMultiText contents, string type)
		{
			//if (contents == null || contents.IsEmpty)
			//    return null;
			CmLiftReversal rev = new CmLiftReversal();
			rev.Type = type;
			rev.Form = MakeSafeLiftMultiText(contents);
			rev.Main = parent as CmLiftReversal;
			return rev;
		}

		LiftObject ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInReversal(
			CmLiftSense sense, LiftObject parent, LiftMultiText contents, string type, string rawXml)
		{
			//if (contents == null || contents.IsEmpty)
			//    return null;
			CmLiftReversal rev = new CmLiftReversal();
			rev.Type = type;
			rev.Form = MakeSafeLiftMultiText(contents);
			rev.Main = parent as CmLiftReversal;
			sense.Reversals.Add(rev);
			return rev;
		}

		LiftObject ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MergeInEtymology(
			CmLiftEntry entry, string source, string type, LiftMultiText form, LiftMultiText gloss,
			string rawXml)
		{
			CmLiftEtymology ety = new CmLiftEtymology();
			ety.Source = source;
			ety.Type = type;
			ety.Form = MakeSafeLiftMultiText(form);
			ety.Gloss = MakeSafeLiftMultiText(gloss);
			entry.Etymologies.Add(ety);
			return ety;
		}

		private void ProcessFeatureDefinition(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev, string rawXml,
			IFsFeatureSystem featSystem)
		{
			IFsFeatDefn feat;
			Guid guid = ConvertStringToGuid(guidAttr);
			// For some reason we are processing the list twice: once when actually processing the range file
			// and once when proessing the LIFT file. During the second pass this prevents adding the same guid twice
			// which causes a crash.
			if (m_mapLiftGuidFeatDefn.TryGetValue(guid, out feat) == true)
					return;

			if (m_factFsComplexFeature == null)
				m_factFsComplexFeature = m_cache.ServiceLocator.GetInstance<IFsComplexFeatureFactory>();
			if (m_factFsOpenFeature == null)
				m_factFsOpenFeature = m_cache.ServiceLocator.GetInstance<IFsOpenFeatureFactory>();
			if (m_factFsClosedFeature == null)
				m_factFsClosedFeature = m_cache.ServiceLocator.GetInstance <IFsClosedFeatureFactory>();
			if (m_repoFsFeatDefn == null)
				m_repoFsFeatDefn = m_cache.ServiceLocator.GetInstance<IFsFeatDefnRepository>();
			FillFeatureMapsIfNeeded();
			XmlDocument xdoc = new XmlDocument();
			xdoc.PreserveWhitespace = true;
			xdoc.LoadXml(rawXml);
			XmlNodeList traits = xdoc.FirstChild.SelectNodes("trait");
			XmlNodeList fields = xdoc.FirstChild.SelectNodes("field");
			string sCatalogId = null;
			bool fDisplayToRight = false;
			bool fShowInGloss = false;
			string sSubclassType = null;
			string sComplexType = null;
			int nWsSelector = 0;
			string sWs = null;
			List<string> rgsValues = new List<string>();
			XmlNode xnGlossAbbrev = null;
			XmlNode xnRightGlossSep = null;
			foreach (XmlNode xn in traits)
			{
				string name = XmlUtils.GetAttributeValue(xn, "name");
				switch (name)
				{
					case "catalog-source-id":
						sCatalogId = XmlUtils.GetAttributeValue(xn, "value");
						break;
					case "display-to-right":
						fDisplayToRight = XmlUtils.GetBooleanAttributeValue(xn, "value");
						break;
					case "show-in-gloss":
						fShowInGloss = XmlUtils.GetBooleanAttributeValue(xn, "value");
						break;
					case "feature-definition-type":
						sSubclassType = XmlUtils.GetAttributeValue(xn, "value");
						break;
					case "type":
						sComplexType = XmlUtils.GetAttributeValue(xn, "value");
						break;
					case "ws-selector":
						nWsSelector = XmlUtils.GetMandatoryIntegerAttributeValue(xn, "value");
						break;
					case "writing-system":
						sWs = XmlUtils.GetAttributeValue(xn, "value");
						break;
					default:
						if (name.EndsWith("-feature-value"))
						{
							string sVal = XmlUtils.GetAttributeValue(xn, "value");
							if (!String.IsNullOrEmpty(sVal))
								rgsValues.Add(sVal);
						}
						break;
				}
			}
			foreach (XmlNode xn in fields)
			{
				string type = XmlUtils.GetAttributeValue(xn, "type");
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
			bool fNew = false;
			if (m_mapIdFeatDefn.TryGetValue(id, out feat))
				feat = ValidateFeatDefnType(sSubclassType, feat);
			if (feat == null)
			{
				feat = CreateDesiredFeatDefn(sSubclassType, feat);
				if (feat == null)
					return;
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
				if (!String.IsNullOrEmpty(sCatalogId))
					feat.CatalogSourceId = sCatalogId;
				if (fDisplayToRight)
					feat.DisplayToRightOfValues = fDisplayToRight;
				if (fShowInGloss)
					feat.ShowInGloss = fShowInGloss;
			}
			if (xnGlossAbbrev != null)
				MergeInMultiUnicode(feat.GlossAbbreviation, xnGlossAbbrev);
			if (xnRightGlossSep != null)
				MergeInMultiUnicode(feat.RightGlossSep, xnRightGlossSep);
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
				m_mapLiftGuidFeatDefn.Add(guid, feat);
		}

		/// <summary>
		/// Either set the Type for the complex feature, or remember it to set later after the
		/// appropriate type has been defined.
		/// </summary>
		private void FinishMergingComplexFeatDefn(IFsComplexFeature featComplex, string sComplexType)
		{
			if (featComplex.TypeRA == null ||
				m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
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
			if (featOpen.WsSelector == 0 ||
				m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				if (nWsSelector != 0)
					featOpen.WsSelector = nWsSelector;
			}
			if (string.IsNullOrEmpty(featOpen.WritingSystem) ||
				m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				if (!string.IsNullOrEmpty(sWs))
					featOpen.WritingSystem = sWs;
			}
		}

		/// <summary>
		/// Either set the Values for the closed feature, or remember them to set later after
		/// they have been defined.
		/// </summary>
		private void FinishMergingClosedFeatDefn(IFsClosedFeature featClosed, List<string> rgsValues,
			string id)
		{
			if (featClosed.ValuesOC.Count == 0 ||
				m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				IFsSymFeatVal featValue;
				List<string> rgsMissing = new List<string>(rgsValues.Count);
				foreach (string sAbbr in rgsValues)
				{
					string key = String.Format("{0}:{1}", id, sAbbr);
					if (m_mapIdAbbrSymFeatVal.TryGetValue(key, out featValue))
					{
						featClosed.ValuesOC.Add(featValue);
						continue;
					}
					else if (m_rgPendingSymFeatVal.Count > 0)
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
							StoreSymFeatValInClosedFeature(val.Id, val.Description, val.Label,
								val.Abbrev, val.CatalogId, val.ShowInGloss, featClosed, val.FeatureId);
							m_rgPendingSymFeatVal.Remove(val);
							continue;
						}
					}
					rgsMissing.Add(sAbbr);
				}
				if (rgsMissing.Count > 0)
					m_mapClosedFeatMissingValueAbbrs.Add(featClosed, rgsMissing);
			}
		}

		private void MergeInMultiUnicode(IMultiUnicode mu, XmlNode xnField)
		{
			int ws = 0;
			string val = null;
			foreach (XmlNode xn in xnField.SelectNodes("form"))
			{
				string sLang = XmlUtils.GetManditoryAttributeValue(xn, "lang");
				ws = GetWsFromLiftLang(sLang);
				XmlNode xnText = xnField.SelectSingleNode("text");
				if (xnText != null)
				{
					val = xnText.InnerText;
					if (!String.IsNullOrEmpty(val))
					{
						ITsString tssOld = mu.get_String(ws);
						if (tssOld.Length == 0 || m_msImport != MergeStyle.MsKeepOld)
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
				m_cache.LangProject.MsFeatureSystemOA.FeaturesOC.Add(feat);
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
			if (!String.IsNullOrEmpty(guidAttr))
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

		private void ProcessFeatureStrucType(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev, string rawXml,
			IFsFeatureSystem featSystem)
		{
			if (m_factFsFeatStrucType == null)
				m_factFsFeatStrucType = m_cache.ServiceLocator.GetInstance<IFsFeatStrucTypeFactory>();
			if (m_repoFsFeatStrucType == null)
				m_repoFsFeatStrucType = m_cache.ServiceLocator.GetInstance<IFsFeatStrucTypeRepository>();
			FillFeatureMapsIfNeeded();
			XmlDocument xdoc = new XmlDocument();
			xdoc.PreserveWhitespace = true;
			xdoc.LoadXml(rawXml);
			XmlNodeList traits = xdoc.FirstChild.SelectNodes("trait");
			string sCatalogId = null;
			List<string> rgsFeatures = new List<string>();
			foreach (XmlNode xn in traits)
			{
				string name = XmlUtils.GetAttributeValue(xn, "name");
				switch (name)
				{
					case "catalog-source-id":
						sCatalogId = XmlUtils.GetManditoryAttributeValue(xn, "value");
						break;
					case "feature":
						rgsFeatures.Add(XmlUtils.GetManditoryAttributeValue(xn, "value"));
						break;
				}
			}
			IFsFeatStrucType featType = null;
			if (!m_mapIdFeatStrucType.TryGetValue(id, out featType))
			{
				featType = m_factFsFeatStrucType.Create();
				m_cache.LangProject.MsFeatureSystemOA.TypesOC.Add(featType);
				m_mapIdFeatStrucType.Add(id, featType);
				m_rgnewFeatStrucType.Add(featType);
			}
			Guid guid = ConvertStringToGuid(guidAttr);
			AddNewWsToAnalysis();
			MergeInMultiUnicode(featType.Abbreviation, FsFeatDefnTags.kflidAbbreviation, abbrev, featType.Guid);
			MergeInMultiUnicode(featType.Name, FsFeatDefnTags.kflidName, label, featType.Guid);
			MergeInMultiString(featType.Description, FsFeatDefnTags.kflidDescription, description, featType.Guid);
			if (featType.CatalogSourceId == null ||
				m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				if (!String.IsNullOrEmpty(sCatalogId))
					featType.CatalogSourceId = sCatalogId;
			}
			if (featType.FeaturesRS.Count == 0 ||
				m_msImport == MergeStyle.MsKeepNew || m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				IFsFeatDefn feat;
				featType.FeaturesRS.Clear();
				foreach (string sVal in rgsFeatures)
				{
					if (m_mapIdFeatDefn.TryGetValue(sVal, out feat))
						featType.FeaturesRS.Add(feat);
				}
				if (rgsFeatures.Count != featType.FeaturesRS.Count)
				{
					featType.FeaturesRS.Clear();
					m_mapFeatStrucTypeMissingFeatureAbbrs.Add(featType, rgsFeatures);
				}
			}
			// Now try to link up with missing type references.  Note that more than one complex
			// feature may be linked to the same type.
			List<IFsComplexFeature> rgfeatHandled = new List<IFsComplexFeature>();
			foreach (KeyValuePair<IFsComplexFeature, string> kv in m_mapComplexFeatMissingTypeAbbr)
			{
				if (kv.Value == id)
				{
					rgfeatHandled.Add(kv.Key);
					break;
				}
			}
			foreach (IFsComplexFeature feat in rgfeatHandled)
			{
				feat.TypeRA = featType;
				m_mapComplexFeatMissingTypeAbbr.Remove(feat);
			}
		}

		private void ProcessFeatureValue(string range, string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev, string rawXml)
		{
			if (m_repoFsFeatDefn == null)
				m_repoFsFeatDefn = m_cache.ServiceLocator.GetInstance<IFsFeatDefnRepository>();
			if (m_factFsSymFeatVal == null)
				m_factFsSymFeatVal = m_cache.ServiceLocator.GetInstance<IFsSymFeatValFactory>();
			if (m_repoFsSymFeatVal == null)
				m_repoFsSymFeatVal = m_cache.ServiceLocator.GetInstance<IFsSymFeatValRepository>();
			FillFeatureMapsIfNeeded();
			XmlDocument xdoc = new XmlDocument();
			xdoc.PreserveWhitespace = true;
			xdoc.LoadXml(rawXml);
			XmlNodeList traits = xdoc.FirstChild.SelectNodes("trait");
			string sCatalogId = null;
			bool fShowInGloss = false;
			foreach (XmlNode xn in traits)
			{
				string name = XmlUtils.GetAttributeValue(xn, "name");
				switch (name)
				{
					case "catalog-source-id":
						sCatalogId = XmlUtils.GetAttributeValue(xn, "value");
						break;
					case "show-in-gloss":
						fShowInGloss = XmlUtils.GetBooleanAttributeValue(xn, "value");
						break;
				}
			}
			string sFeatId = null;
			int idxSuffix = range.IndexOf("-feature-value");
			if (idxSuffix > 0)
				sFeatId = range.Substring(0, idxSuffix);
			Guid guid = ConvertStringToGuid(guidAttr);
			IFsClosedFeature featClosed = FindRelevantClosedFeature(sFeatId, id, guid);
			if (featClosed == null)
			{
				// Save the information for later in hopes something comes up.
				PendingFeatureValue pfv = new PendingFeatureValue(sFeatId, id, description,
					label, abbrev, sCatalogId, fShowInGloss, guid);
				m_rgPendingSymFeatVal.Add(pfv);
				return;
			}
			StoreSymFeatValInClosedFeature(id, description, label, abbrev,
				sCatalogId, fShowInGloss, featClosed, sFeatId);
		}

		private void StoreSymFeatValInClosedFeature(string id, LiftMultiText description,
			LiftMultiText label, LiftMultiText abbrev, string sCatalogId, bool fShowInGloss,
			IFsClosedFeature featClosed, string featId)
		{
			bool fNew = false;
			IFsSymFeatVal val = FindMatchingFeatValue(featClosed, id);
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
				if (!String.IsNullOrEmpty(sCatalogId))
					val.CatalogSourceId = sCatalogId;
				if (fShowInGloss)
					val.ShowInGloss = fShowInGloss;
			}
			// update the map to find this later.
			string key = String.Format("{0}:{1}", featId, id);
			m_mapIdAbbrSymFeatVal[key] = val;
		}

		private IFsSymFeatVal FindMatchingFeatValue(IFsClosedFeature featClosed, string id)
		{
			IFsSymFeatVal val = null;
			foreach (IFsSymFeatVal sfv in featClosed.ValuesOC)
			{
				for (int i = 0; i < sfv.Abbreviation.StringCount; ++i)
				{
					int ws;
					ITsString tss = sfv.Abbreviation.GetStringFromIndex(i, out ws);
					if (tss.Text == id)
						return sfv;
				}
			}
			return val;
		}

		private IFsClosedFeature FindRelevantClosedFeature(string sFeatId, string id, Guid guid)
		{
			IFsClosedFeature featClosed = null;
			if (guid != Guid.Empty)
			{
				IFsFeatDefn feat;
				if (m_mapLiftGuidFeatDefn.TryGetValue(guid, out feat))
					featClosed = feat as IFsClosedFeature;
			}
			if (featClosed == null && !String.IsNullOrEmpty(sFeatId))
			{
				IFsFeatDefn feat;
				if (m_mapIdFeatDefn.TryGetValue(sFeatId, out feat))
					featClosed = feat as IFsClosedFeature;
			}
			if (featClosed == null)
			{
				foreach (KeyValuePair<IFsClosedFeature, List<string>> kv in m_mapClosedFeatMissingValueAbbrs)
				{
					if (kv.Value.Contains(id))
					{
						kv.Value.Remove(id);
						if (kv.Value.Count == 0)
							m_mapClosedFeatMissingValueAbbrs.Remove(kv.Key);
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
			foreach (IFsFeatDefn feat in m_cache.LangProject.MsFeatureSystemOA.FeaturesOC)
			{
				for (int i = 0; i < feat.Abbreviation.StringCount; ++i)
				{
					int ws;
					ITsString tssAbbr = feat.Abbreviation.GetStringFromIndex(i, out ws);
					string sAbbr = tssAbbr.Text;
					if (!String.IsNullOrEmpty(sAbbr) && !m_mapIdFeatDefn.ContainsKey(sAbbr))
						m_mapIdFeatDefn.Add(sAbbr, feat);
				}
			}
		}

		private void FillIdFeatStrucTypeMap()
		{
			foreach (IFsFeatStrucType featType in m_cache.LangProject.MsFeatureSystemOA.TypesOC)
			{
				for (int i = 0; i < featType.Abbreviation.StringCount; ++i)
				{
					int ws;
					ITsString tssAbbr = featType.Abbreviation.GetStringFromIndex(i, out ws);
					string sAbbr = tssAbbr.Text;
					if (!String.IsNullOrEmpty(sAbbr) && !m_mapIdFeatStrucType.ContainsKey(sAbbr))
						m_mapIdFeatStrucType.Add(sAbbr, featType);
				}
			}
		}

		private void FillIdAbbrSymFeatValMap()
		{
			foreach (IFsFeatDefn feat in m_cache.LangProject.MsFeatureSystemOA.FeaturesOC)
			{
				IFsClosedFeature featClosed = feat as IFsClosedFeature;
				if (featClosed != null)
				{
					Set<string> setIds = new Set<string>();
					for (int i = 0; i < featClosed.Abbreviation.StringCount; ++i)
					{
						int ws;
						ITsString tssAbbr = featClosed.Abbreviation.GetStringFromIndex(i, out ws);
						string sAbbr = tssAbbr.Text;
						if (!String.IsNullOrEmpty(sAbbr))
							setIds.Add(sAbbr);
					}
					foreach (IFsSymFeatVal featVal in featClosed.ValuesOC)
					{
						for (int i = 0; i < featVal.Abbreviation.StringCount; ++i)
						{
							int ws;
							ITsString tssAbbr = featVal.Abbreviation.GetStringFromIndex(i, out ws);
							string sAbbr = tssAbbr.Text;
							if (!String.IsNullOrEmpty(sAbbr))
							{
								foreach (string sId in setIds)
								{
									string key = String.Format("{0}:{1}", sId, sAbbr);
									if (!m_mapIdAbbrSymFeatVal.ContainsKey(key))
										m_mapIdAbbrSymFeatVal.Add(key, featVal);
								}
							}
						}
					}
				}
			}
		}

		private void ProcessStemName(string range, string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev, string rawXml)
		{
			var idx = range.LastIndexOf("-stem-name");
			if (idx <= 0)
				return;
			string sPosName = range.Substring(0, idx);
			ICmPossibility poss;
			if (!m_dictPos.TryGetValue(sPosName, out poss))
				return;
			IPartOfSpeech pos = poss as IPartOfSpeech;
			if (pos == null)
				return;
			IMoStemName stem;
			var key = String.Format("{0}:{1}", sPosName, id);
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

			HashSet<string> setFeats = new HashSet<string>();
			foreach (IFsFeatStruc ffs in stem.RegionsOC)
				setFeats.Add(ffs.LiftName);
			XmlDocument xdoc = new XmlDocument();
			xdoc.PreserveWhitespace = true;
			xdoc.LoadXml(rawXml);
			XmlNodeList traits = xdoc.FirstChild.SelectNodes("trait");
			foreach (XmlNode xn in traits)
			{
				var name = XmlUtils.GetAttributeValue(xn, "name");
				if (name == "feature-set")
				{
					var value = XmlUtils.GetAttributeValue(xn, "value");
					if (setFeats.Contains(value))
						continue;
					IFsFeatStruc ffs = ParseFeatureString(value, stem);
					if (ffs == null)
						continue;
					setFeats.Add(value);
					var liftName = ffs.LiftName;
					if (liftName != value)
						setFeats.Add(liftName);
				}
			}
		}

		void ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.ProcessFieldDefinition(
			string tag, LiftMultiText description)
		{
			string key = tag;
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
		/// <param name="tag"></param>
		/// <param name="description"></param>
		/// <param name="typeFlid"></param>
		/// <returns></returns>
		private bool IsCustomField(string tag, LiftMultiText description, out int typeFlid)
		{
			LiftString lstr;
			if(description.TryGetValue("qaa-x-spec", out lstr))
			{
				string value = lstr.Text;
				string[] items = value.Split(new char[]{'=',';'});
				for(int i = 0; i < items.Length; ++i)
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
		/// <param name="info"></param>
		/// <returns></returns>
		private bool SameEntryModTimes(Extensible info)
		{
			Guid guid = GetGuidInExtensible(info);
			ICmObject obj = GetObjectForGuid(guid);
			if (obj != null && obj is ILexEntry)
			{
				DateTime dtMod = (obj as ILexEntry).DateModified;
				DateTime dtMod2 = dtMod.ToUniversalTime();
				DateTime dtModNew = info.ModificationTime.ToUniversalTime();
				// Only go down to the second -- ignore any millisecond or microsecond granularity.
				return (dtMod2.Date == dtModNew.Date &&
					dtMod2.Hour == dtModNew.Hour &&
					dtMod2.Minute == dtModNew.Minute &&
					dtMod2.Second == dtModNew.Second);
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Create a new lexicon entry from the provided data.
		/// </summary>
		/// <param name="entry"></param>
		private void CreateNewEntry(CmLiftEntry entry)
		{
			try
			{
				m_fCreatingNewEntry = true;
				bool fNeedNewId;
				ILexEntry le = CreateNewLexEntry(entry.Guid, out fNeedNewId);
				if (m_cdConflict != null && m_cdConflict is ConflictingEntry)
				{
					(m_cdConflict as ConflictingEntry).DupEntry = le;
					m_rgcdConflicts.Add(m_cdConflict);
					m_cdConflict = null;
				}
				StoreEntryId(le, entry);
				le.HomographNumber = entry.Order;
				CreateLexemeForm(le, entry);		// also sets CitationForm if it exists.
				if (fNeedNewId)
				{
					XmlDocument xdEntryResidue = FindOrCreateResidue(le, entry.Id, LexEntryTags.kflidLiftResidue);
					XmlAttribute xa = xdEntryResidue.FirstChild.Attributes["id"];
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
				ProcessEntryEtymologies(le, entry);
				ProcessEntryRelations(le, entry);
				foreach (CmLiftSense sense in entry.Senses)
					CreateEntrySense(le, sense);
				if (entry.DateCreated != default(DateTime))
					le.DateCreated = entry.DateCreated.ToLocalTime();
				if (entry.DateModified != default(DateTime))
					m_rgPendingModifyTimes.Add(new PendingModifyTime(le, entry.DateModified.ToLocalTime()));
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
			if (!String.IsNullOrEmpty(entry.Id))
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
						msg = String.Format(LexTextControls.ksDuplicateIdValue,
							cmo.ClassName, id);
					}
				}
				if (String.IsNullOrEmpty(msg))
					msg = String.Format(LexTextControls.ksProblemId, cmo.ClassName, id);
				m_rgErrorMsgs.Add(msg);
			}
		}

		/// <summary>
		/// Store accumulated import residue for the entry (and its senses), and ensure
		/// that all senses have an MSA, and that duplicate MSAs are merged together.
		/// </summary>
		/// <param name="le"></param>
		private void FinishProcessingEntry(ILexEntry le)
		{
			// We don't create/assign MSAs to senses if <grammatical-info> doesn't exist.
			EnsureValidMSAsForSenses(le);
			// The next line of code is commented out in 6.0 because it may be finding and
			// fixing lots of redundancies that we didn't create.  In fact, we shouldn't be
			// creating any redundancies any more -- there's lots of code looking for matching
			// MSAs to reuse!
			//(le as LexEntry).MergeRedundantMSAs();
		}

		private void WriteAccumulatedResidue()
		{
			foreach (int hvo in m_dictResidue.Keys)
			{
				LiftResidue res = m_dictResidue[hvo];
				string sLiftResidue = res.Document.OuterXml;
				int flid = res.Flid;
				if (!String.IsNullOrEmpty(sLiftResidue) && flid != 0)
					m_cache.MainCacheAccessor.set_UnicodeProp(hvo, flid, sLiftResidue);
			}
			m_dictResidue.Clear();
		}

#if false // CS0169
		private static int StartOfLiftResidue(ITsStrBldr tsb)
		{
			int idx = tsb.Length;
			if (tsb.Text != null)
			{
				idx = tsb.Text.IndexOf("<lift-residue id=");
				if (idx < 0)
					idx = tsb.Length;
			}
			return idx;
		}
#endif
		/// <summary>
		/// Check whether an existing entry has data that conflicts with an imported entry that
		/// has the same identity (guid).  Senses are not checked, since they can be added to
		/// the existing entry instead of creating an entirely new entry.
		/// </summary>
		/// <param name="entry"></param>
		/// <returns>true if a conflict exists, otherwise false</returns>
		private bool EntryHasConflictingData(CmLiftEntry entry)
		{
			m_cdConflict = null;
			ILexEntry le = entry.CmObject as ILexEntry;
			if (LexemeFormsConflict(le, entry))
				return true;
			if (EntryEtymologiesConflict(le.EtymologyOA, entry.Etymologies))
			{
				m_cdConflict = new ConflictingEntry("Etymology", le, this);
				return true;
			}
			if (EntryFieldsConflict(le, entry.Fields))
				return true;
			if (EntryNotesConflict(le, entry.Notes))
				return true;
			if (EntryPronunciationsConflict(le, entry.Pronunciations))
				return true;
			if (EntryTraitsConflict(le, entry.Traits))
				return true;
			if (EntryVariantsConflict(le, entry.Variants))
				return true;
			//entry.DateCreated;
			//entry.DateModified;
			//entry.Order;
			//entry.Relations;
			return false;
		}

		/// <summary>
		/// Add the imported data to an existing lexical entry.
		/// </summary>
		/// <param name="entry"></param>
		private void MergeIntoExistingEntry(CmLiftEntry entry)
		{

			ILexEntry le = entry.CmObject as ILexEntry;
			StoreEntryId(le, entry);
			le.HomographNumber = entry.Order;
			MergeLexemeForm(le, entry);		// also sets CitationForm if it exists.
			ProcessEntryTraits(le, entry);
			ProcessEntryNotes(le, entry);
			ProcessEntryFields(le, entry);
			MergeEntryVariants(le, entry);
			MergeEntryPronunciations(le, entry);
			ProcessEntryEtymologies(le, entry);
			ProcessEntryRelations(le, entry);
			Dictionary<CmLiftSense, ILexSense> map = new Dictionary<CmLiftSense, ILexSense>();
			Set<int> setUsed = new Set<int>();
			foreach (CmLiftSense sense in entry.Senses)
			{
				ILexSense ls = FindExistingSense(le.SensesOS, sense);
				map.Add(sense, ls);
				if (ls != null)
					setUsed.Add(ls.Hvo);
			}
			// If we're keeping only the imported data, delete any unused senses.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				foreach (int hvo in le.SensesOS.ToHvoArray())
				{
					if (!setUsed.Contains(hvo))
						m_deletedObjects.Add(hvo);
				}
			}
			foreach (CmLiftSense sense in entry.Senses)
			{
				ILexSense ls;
				map.TryGetValue(sense, out ls);
				if (ls == null || (m_msImport == MergeStyle.MsKeepBoth && SenseHasConflictingData(ls, sense)))
					CreateEntrySense(le, sense);
				else
					MergeIntoExistingSense(ls, sense);
			}
			if (entry.DateCreated != default(DateTime))
				le.DateCreated = entry.DateCreated.ToLocalTime();
			if (entry.DateModified != default(DateTime))
				m_rgPendingModifyTimes.Add(new PendingModifyTime(le, entry.DateModified.ToLocalTime()));
			StoreAnnotationsAndDatesInResidue(le, entry);
			FinishProcessingEntry(le);
		}

		private ILexSense FindExistingSense(IFdoOwningSequence<ILexSense> rgsenses, CmLiftSense sense)
		{
			if (sense.CmObject == null)
				return null;
			foreach (ILexSense ls in rgsenses)
			{
				if (ls.Hvo == sense.CmObject.Hvo)
					return ls;
			}
			return null;
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
				return true;
			if (SenseGramInfoConflicts(ls, sense.GramInfo))
				return true;
			if (SenseIllustrationsConflict(ls, sense.Illustrations))
				return true;
			if (SenseNotesConflict(ls, sense.Notes))
				return true;
			if (SenseRelationsConflict(ls, sense.Relations))
				return true;
			if (SenseReversalsConflict(ls, sense.Reversals))
				return true;
			if (SenseTraitsConflict(ls, sense.Traits))
				return true;
			if (SenseFieldsConflict(ls, sense.Fields))
				return true;
			return false;
		}

		private void CreateLexemeForm(ILexEntry le, CmLiftEntry entry)
		{
			if (entry.LexicalForm != null && !entry.LexicalForm.IsEmpty)
			{
				IMoMorphType mmt;
				string realForm;
				AddNewWsToVernacular();
				ITsString tssForm = GetFirstLiftTsString(entry.LexicalForm);
				IMoForm mf = CreateMoForm(entry.Traits, tssForm, out mmt, out realForm,
					le.Guid, LexEntryTags.kflidLexemeForm);
				le.LexemeFormOA = mf;
				FinishMoForm(mf, entry.LexicalForm, tssForm, mmt, realForm,
					le.Guid, LexEntryTags.kflidLexemeForm);
			}
			if (entry.CitationForm != null && !entry.CitationForm.IsEmpty)
			{
				MergeInAllomorphForms(entry.CitationForm, le.CitationForm,
					le.LexemeFormOA == null ? MoStemAllomorphTags.kClassId : le.LexemeFormOA.ClassID,
					le.Guid, LexEntryTags.kflidCitationForm);
			}
		}

		private IMoMorphType FindMorphType(ref string form, out int clsid, Guid guidEntry, int flid)
		{
			string fullForm = form;
			try
			{
				return MorphServices.FindMorphType(m_cache, ref form, out clsid);
			}
			catch (Exception error)
			{
				InvalidData bad = new InvalidData(error.Message, guidEntry, flid, fullForm, 0, m_cache, this);
				if (!m_rgInvalidData.Contains(bad))
					m_rgInvalidData.Add(bad);
				form = fullForm;
				clsid = MoStemAllomorphTags.kClassId;
				return GetExistingMoMorphType(MoMorphTypeTags.kguidMorphStem);
			}
		}

		private void MergeLexemeForm(ILexEntry le, CmLiftEntry entry)
		{
			if (entry.LexicalForm != null && !entry.LexicalForm.IsEmpty)
			{
				IMoForm mf = le.LexemeFormOA;
				int clsid = 0;
				if (mf == null)
				{
					string form = entry.LexicalForm.FirstValue.Value.Text;
					IMoMorphType mmt = FindMorphType(ref form, out clsid,
						le.Guid, LexEntryTags.kflidLexemeForm);
					if (mmt.IsAffixType)
						mf = CreateNewMoAffixAllomorph();
					else
						mf = CreateNewMoStemAllomorph();
					le.LexemeFormOA = mf;
					mf.MorphTypeRA = mmt;
				}
				else
				{
					clsid = mf.ClassID;
				}
				MergeInAllomorphForms(entry.LexicalForm, mf.Form, clsid, le.Guid,
					LexEntryTags.kflidLexemeForm);
			}
			if (entry.CitationForm != null && !entry.CitationForm.IsEmpty)
			{
				MergeInAllomorphForms(entry.CitationForm, le.CitationForm,
					le.LexemeFormOA == null ? MoStemAllomorphTags.kClassId : le.LexemeFormOA.ClassID,
					le.Guid, LexEntryTags.kflidCitationForm);
			}
		}

		private bool LexemeFormsConflict(ILexEntry le, CmLiftEntry entry)
		{
			AddNewWsToVernacular();
			if (MultiUnicodeStringsConflict(le.CitationForm, entry.CitationForm, true,
				le.Guid, LexEntryTags.kflidCitationForm))
			{
				m_cdConflict = new ConflictingEntry("Citation Form", le, this);
				return true;
			}
			if (le.LexemeFormOA != null)
			{
				if (MultiUnicodeStringsConflict(le.LexemeFormOA.Form, entry.LexicalForm, true,
					le.Guid, LexEntryTags.kflidLexemeForm))
				{
					m_cdConflict = new ConflictingEntry("Lexeme Form", le, this);
					return true;
				}
			}
			return false;
		}

		private void ProcessEntryTraits(ILexEntry le, CmLiftEntry entry)
		{
			foreach (LiftTrait lt in entry.Traits)
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
						bool fDontUse = XmlUtils.GetBooleanAttributeValue(lt.Value);
						if (le.DoNotUseForParsing != fDontUse && (m_fCreatingNewEntry || m_msImport != MergeStyle.MsKeepOld))
							le.DoNotUseForParsing = fDontUse;
						break;
					default:
						ProcessUnknownTrait(entry, lt, le);
						break;
				}
			}
		}

		private void ProcessUnknownTrait(LiftObject liftObject, LiftTrait lt, ICmObject cmo)
		{
			int clid = cmo.ClassID;
			LiftMultiText desc = null;
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
					int numberitems = sda.get_VecSize(hvo, flid);
					sda.Replace(hvo, flid, numberitems, numberitems, listRefCollectionItems, 1);
					break;
				default:
					// TODO: Warn user he's smarter than we are?
					break;
			}
		}

		private void ProcessEntryMorphType(ILexEntry le, string traitValue)
		{
			IMoMorphType mmt = FindMorphType(traitValue);
			if (le.LexemeFormOA == null)
			{
				if (mmt.IsAffixType)
					le.LexemeFormOA = CreateNewMoAffixAllomorph();
				else
					le.LexemeFormOA = CreateNewMoStemAllomorph();
				le.LexemeFormOA.MorphTypeRA = mmt;
			}
			else if (le.LexemeFormOA.MorphTypeRA != mmt &&
				(m_fCreatingNewEntry || m_msImport != MergeStyle.MsKeepOld || le.LexemeFormOA.MorphTypeRA == null))
			{
				if (mmt.IsAffixType)
				{
					if (le.LexemeFormOA is IMoStemAllomorph)
						le.ReplaceMoForm(le.LexemeFormOA, CreateNewMoAffixAllomorph());
				}
				else
				{
					if (!(le.LexemeFormOA is IMoStemAllomorph))
						le.ReplaceMoForm(le.LexemeFormOA, CreateNewMoStemAllomorph());
				}
				le.LexemeFormOA.MorphTypeRA = mmt;
			}
		}

		private bool EntryTraitsConflict(ILexEntry le, List<LiftTrait> list)
		{
			foreach (LiftTrait lt in list)
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
							IMoMorphType mmt = FindMorphType(lt.Value);
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
						bool fDontUse = XmlUtils.GetBooleanAttributeValue(lt.Value);
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
			foreach (CmLiftNote note in entry.Notes)
			{
				if (note.Type == null)
					note.Type = String.Empty;
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
			foreach (CmLiftNote note in list)
			{
				if (note.Type == null)
					note.Type = String.Empty;
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
			foreach (LiftField lf in entry.Fields)
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
			foreach (LiftField lf in list)
			{
				switch (lf.Type.ToLowerInvariant())
				{
					case "importresidue":
					case "import-residue":
						if (le.ImportResidue != null && le.ImportResidue.Length != 0)
						{
							ITsStrBldr tsb = le.ImportResidue.GetBldr();
							int idx = tsb.Text.IndexOf("<lift-residue id=");
							if (idx >= 0)
								tsb.Replace(idx, tsb.Length, null, null);
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
								m_cdConflict = new ConflictingEntry(String.Format("{0} (custom field)", lf.Type), le, this);
								return true;
							}
						}
						break;
				}
			}
			return false;
		}

		private void ProcessCustomFieldData(int hvo, int flid, LiftMultiText contents)
		{
			CellarPropertyType type = (CellarPropertyType)m_cache.MetaDataCacheAccessor.GetFieldType(flid);
			HandleWsSettingsForCustomField(flid);
			ICmObject cmo = GetObjectForId(hvo);
			ITsMultiString tsm;
			switch (type)
			{
				case CellarPropertyType.String:
				case CellarPropertyType.BigString:
					ITsString tss = StoreTsStringValue(m_fCreatingNewEntry | m_fCreatingNewSense,
						m_cache.MainCacheAccessor.get_StringProp(hvo, flid), contents);
					m_cache.MainCacheAccessor.SetString(hvo, flid, tss);
					break;
				case CellarPropertyType.MultiString:
				case CellarPropertyType.MultiBigString:
					tsm = m_cache.MainCacheAccessor.get_MultiStringProp(hvo, flid);
					MergeInMultiString(tsm, flid, contents, cmo.Guid);
					break;
				case CellarPropertyType.MultiUnicode:
				case CellarPropertyType.MultiBigUnicode:
					tsm = m_cache.MainCacheAccessor.get_MultiStringProp(hvo, flid);
					MergeInMultiUnicode(tsm, flid, contents, cmo.Guid);
					break;
				case CellarPropertyType.OwningAtomic:
					var destName = m_cache.MetaDataCacheAccessor.GetDstClsName(flid);
					if (destName == "StText")
						ProcessStTextField(hvo, flid, contents);
					break;
				default:
					// TODO: Warn user he's smarter than we are?
					break;
			}
		}

		internal class ParaData
		{
			public string StyleName { get; set; }
			public ITsString Contents { get; set; }
		}

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
				var cparaLim = Math.Min(cparaOld, cparaNew);
				switch (m_msImport)
				{
					case MergeStyle.MsKeepOld:
						// Add any additional paragraphs to the end of the text.
						for (var i = cparaOld; i < cparaNew; ++i)
						{
							var para = paras[i];
							var stPara = paraFact.Create();
							text.ParagraphsOS.Add(stPara);
							if (!String.IsNullOrEmpty(para.StyleName))
								stPara.StyleName = para.StyleName;
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
							if (!String.IsNullOrEmpty(para.StyleName))
								stPara.StyleName = para.StyleName;
							stPara.Contents = para.Contents;
						}
						//if (cparaNew < cparaOld)
						//    text.ParagraphsOS.Replace(cparaNew, cparaOld - cparaNew, new List<ICmObject>());
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
									continue;
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
								if (!String.IsNullOrEmpty(para.StyleName))
									stPara.StyleName = para.StyleName;
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
							if (!String.IsNullOrEmpty(para.StyleName))
								stPara.StyleName = para.StyleName;
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
				if (!String.IsNullOrEmpty(para.StyleName))
					stPara.StyleName = para.StyleName;
				stPara.Contents = para.Contents;
			}
		}

		private List<ParaData> ParseMultipleParagraphs(LiftMultiText contents)
		{
			var paras = new List<ParaData>();
			if (contents.Keys.Count > 1)
			{
				// Complain vociferously??
			}
			var lang = contents.Keys.FirstOrDefault();
			if (lang == null)
				return paras;
			var wsText = GetWsFromLiftLang(lang);
			var text = contents[lang];
			if (text.Text == null)
				return paras;
			var ich = 0;
			ParaData para = null;
			var fNewPara = true;
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			tisb.SetIntPropValues((int) FwTextPropType.ktptWs, 0, wsText);
			var wsCurrent = wsText;
			string styleCurrent = null;
			for (var i = 0; i < text.Spans.Count; ++i)
			{
				var span = text.Spans[i];
				var fParaStyle = false;
				if (ich < span.Index)
				{
					// text before this span.
					var len = span.Index - ich;
					var data = text.Text.Substring(ich, len);
					if (data.Replace("\u2028", "").Replace(" ", "").Replace("\t", "") == "\u2029")
					{
						fNewPara = true;
					}
					else
					{
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsText);
						tisb.SetStrPropValue((int) FwTextPropType.ktptNamedStyle, null);
						tisb.Append(data);
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
					if (!String.IsNullOrEmpty(span.Class) && String.IsNullOrEmpty(span.Lang))
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
					wsCurrent = String.IsNullOrEmpty(span.Lang) ? wsText : GetWsFromLiftLang(span.Lang);
					styleCurrent = String.IsNullOrEmpty(span.Class) ? null : span.Class;
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsCurrent);
					tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, styleCurrent);
				}
				if (span.Spans.Count > 0)
					ProcessNestedSpans(text, span, wsCurrent, styleCurrent, tisb);
				else
					tisb.Append(text.Text.Substring(span.Index, span.Length));
				ich = span.Index + span.Length;
			}
			if (ich < text.Text.Length)
			{
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsText);
				tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);
				tisb.Append(text.Text.Substring(ich, text.Text.Length - ich));
			}
			if (tisb.Text.Length > 0)
			{
				if (para == null)
					para = new ParaData();
				para.Contents = tisb.GetString();
			}
			if (para != null)
				paras.Add(para);
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
						m_paraStyles.Add(style.Name);
				}
			}
			return m_paraStyles.Contains(styleName);
		}

		private void ProcessNestedSpans(LiftString text, LiftSpan span, int wsSpan, string styleSpan,
			ITsIncStrBldr tisb)
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
				var wsCurrent = String.IsNullOrEmpty(subspan.Lang) ? wsSpan : GetWsFromLiftLang(span.Lang);
				var styleCurrent = String.IsNullOrEmpty(subspan.Class) ? styleSpan : subspan.Class;
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsCurrent);
				tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, styleCurrent);
				if (subspan.Spans.Count > 0)
					ProcessNestedSpans(text, subspan, wsCurrent, styleCurrent, tisb);
				else
					tisb.Append(text.Text.Substring(subspan.Index, subspan.Length));
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

		private bool CustomFieldDataConflicts(int hvo, int flid, LiftMultiText contents)
		{
			CellarPropertyType type = (CellarPropertyType)m_cache.MetaDataCacheAccessor.GetFieldType(flid);
			HandleWsSettingsForCustomField(flid);
			ITsMultiString tsm;
			switch (type)
			{
				case CellarPropertyType.String:
				case CellarPropertyType.BigString:
					ITsString tss = m_cache.MainCacheAccessor.get_StringProp(hvo, flid);
					if (StringsConflict(tss, GetFirstLiftTsString(contents)))
						return true;
					break;
				case CellarPropertyType.MultiString:
				case CellarPropertyType.MultiBigString:
					tsm = m_cache.MainCacheAccessor.get_MultiStringProp(hvo, flid);
					if (MultiTsStringsConflict(tsm, contents))
						return true;
					break;
				case CellarPropertyType.MultiUnicode:
				case CellarPropertyType.MultiBigUnicode:
					tsm = m_cache.MainCacheAccessor.get_MultiStringProp(hvo, flid);
					if (MultiUnicodeStringsConflict(tsm, contents, false, Guid.Empty, 0))
						return true;
					break;
				default:
					break;
			}
			return false;
		}

		private void CreateEntryVariants(ILexEntry le, CmLiftEntry entry)
		{
			foreach (CmLiftVariant lv in entry.Variants)
			{
				AddNewWsToVernacular();
				ITsString tssForm = GetFirstLiftTsString(lv.Form);
				IMoMorphType mmt;
				string realForm;
				IMoForm mf = CreateMoForm(lv.Traits, tssForm, out mmt, out realForm,
					le.Guid, LexEntryTags.kflidAlternateForms);
				le.AlternateFormsOS.Add(mf);
				FinishMoForm(mf, lv.Form, tssForm, mmt, realForm,
					le.Guid, LexEntryTags.kflidAlternateForms);
				bool fTypeSpecified;
				ProcessMoFormTraits(mf, lv, out fTypeSpecified);
				ProcessMoFormFields(mf, lv);
				StoreResidueFromVariant(mf, lv);
				if (!fTypeSpecified)
					mf.MorphTypeRA = null;
			}
		}

		private IMoForm CreateMoForm(List<LiftTrait> traits, ITsString tssForm,
			out IMoMorphType mmt, out string realForm, Guid guidEntry, int flid)
		{
			// Try to create the proper type of allomorph form to begin with.  It takes over
			// 200ms to delete one we just created!  (See LT-9006.)
			int clsidForm;
			if (tssForm == null || tssForm.Text == null)
			{
				clsidForm = MoStemAllomorphTags.kClassId;
				ICmObject cmo = GetObjectForGuid(MoMorphTypeTags.kguidMorphStem);
				mmt = cmo as IMoMorphType;
				realForm = null;
			}
			else
			{
				realForm = tssForm.Text;
				mmt = FindMorphType(ref realForm, out clsidForm, guidEntry, flid);
			}
			IMoMorphType mmt2;
			int clsidForm2 = GetMoFormClassFromTraits(traits, out mmt2);
			if (clsidForm2 != 0 && mmt2 != null)
			{
				if (mmt2 != mmt)
					mmt = mmt2;
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

		private void FinishMoForm(IMoForm mf, LiftMultiText forms, ITsString tssForm, IMoMorphType mmt,
			string realForm, Guid guidEntry, int flid)
		{
			mf.MorphTypeRA = mmt; // Has to be done, before the next call.
			ITsString tssRealForm;
			if (tssForm != null)
			{
				if (tssForm.Text != realForm)
				{
					// make a new tsString with the old ws.
					tssRealForm = TsStringUtils.MakeTss(realForm,
						TsStringUtils.GetWsAtOffset(tssForm, 0));
				}
				else
				{
					tssRealForm = tssForm;
				}
				mf.FormMinusReservedMarkers = tssRealForm;
			}
			MergeInAllomorphForms(forms, mf.Form, mf.ClassID, guidEntry, flid);
		}

		private void MergeEntryVariants(ILexEntry le, CmLiftEntry entry)
		{
			Dictionary<int, CmLiftVariant> dictHvoVariant = new Dictionary<int, CmLiftVariant>();
			foreach (CmLiftVariant lv in entry.Variants)
			{
				AddNewWsToVernacular();
				IMoForm mf = FindMatchingMoForm(le, dictHvoVariant, lv,
					le.Guid, LexEntryTags.kflidAlternateForms);
				if (mf == null)
				{
					ITsString tssForm = GetFirstLiftTsString(lv.Form);
					if (tssForm == null || tssForm.Text == null)
						continue;
					IMoMorphType mmt;
					string realForm;
					mf = CreateMoForm(lv.Traits, tssForm, out mmt, out realForm,
						le.Guid, LexEntryTags.kflidAlternateForms);
					le.AlternateFormsOS.Add(mf);
					FinishMoForm(mf, lv.Form, tssForm, mmt, realForm,
						le.Guid, LexEntryTags.kflidAlternateForms);
					dictHvoVariant.Add(mf.Hvo, lv);
				}
				else
				{
					MergeInAllomorphForms(lv.Form, mf.Form, mf.ClassID,
						le.Guid, LexEntryTags.kflidAlternateForms);
				}
				bool fTypeSpecified;
				ProcessMoFormTraits(mf, lv, out fTypeSpecified);
				ProcessMoFormFields(mf, lv);
				StoreResidueFromVariant(mf, lv);
				if (!fTypeSpecified)
					mf.MorphTypeRA = null;
			}
		}

		private bool EntryVariantsConflict(ILexEntry le, List<CmLiftVariant> list)
		{
			if (le.AlternateFormsOS.Count == 0 || list.Count == 0)
				return false;
			int cCommon = 0;
			Dictionary<int, CmLiftVariant> dictHvoVariant = new Dictionary<int, CmLiftVariant>();
			AddNewWsToVernacular();
			foreach (CmLiftVariant lv in list)
			{
				IMoForm mf = FindMatchingMoForm(le, dictHvoVariant, lv,
					le.Guid, LexEntryTags.kflidAlternateForms);
				if (mf != null)
				{
					if (MultiUnicodeStringsConflict(mf.Form, lv.Form, true,
						le.Guid, LexEntryTags.kflidAlternateForms))
					{
						m_cdConflict = new ConflictingEntry(String.Format("Alternate Form ({0})",
							TsStringAsHtml(mf.Form.BestVernacularAlternative, m_cache)), le, this);
						return true;
					}
					if (MoFormTraitsConflict(mf, lv.Traits))
					{
						m_cdConflict = new ConflictingEntry(String.Format("Alternate Form ({0}) details",
							TsStringAsHtml(mf.Form.BestVernacularAlternative, m_cache)), le, this);
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
			else
			{
				return false;
			}
		}

		private IMoForm FindMatchingMoForm(ILexEntry le, Dictionary<int, CmLiftVariant> dictHvoVariant,
			CmLiftVariant lv, Guid guidEntry, int flid)
		{
			IMoForm form = null;
			int cMatches = 0;
			AddNewWsToVernacular();
			foreach (IMoForm mf in le.AlternateFormsOS)
			{
				if (dictHvoVariant.ContainsKey(mf.Hvo))
					continue;
				int cCurrent = MultiUnicodeStringMatches(mf.Form, lv.Form, true, guidEntry, flid);
				if (cCurrent > cMatches)
				{
					form = mf;
					cMatches = cCurrent;
				}
			}
			if (form != null)
				dictHvoVariant.Add(form.Hvo, lv);
			return form;
		}

		private int GetMoFormClassFromTraits(List<LiftTrait> traits, out IMoMorphType mmt)
		{
			mmt = null;
			foreach (LiftTrait lt in traits)
			{
				if (lt.Name.ToLowerInvariant() == RangeNames.sDbMorphTypesOAold ||
					lt.Name.ToLowerInvariant() == RangeNames.sDbMorphTypesOA)
				{
					mmt = FindMorphType(lt.Value);
					bool fAffix = mmt.IsAffixType;
					if (fAffix)
						return MoAffixAllomorphTags.kClassId;
					else
						return MoStemAllomorphTags.kClassId;
				}
			}
			return 0;	// no subclass info in the traits
		}

		private void ProcessMoFormTraits(IMoForm form, CmLiftVariant variant, out bool fTypeSpecified)
		{
			fTypeSpecified = false;
			foreach (LiftTrait lt in variant.Traits)
			{
				IMoMorphType mmt = null;
				switch (lt.Name.ToLowerInvariant())
				{
					case RangeNames.sDbMorphTypesOAold:	// original FLEX export = MorphType
					case RangeNames.sDbMorphTypesOA:
						if (!m_fCreatingNewEntry && m_msImport == MergeStyle.MsKeepOld && form.MorphTypeRA != null)
							continue;
						mmt = FindMorphType(lt.Value);
						bool fAffix = mmt.IsAffixType;
						if (fAffix && form is IMoStemAllomorph)
						{
							IMoStemAllomorph stem = form as IMoStemAllomorph;
							IMoAffixAllomorph affix = CreateNewMoAffixAllomorph();
							ILexEntry entry = form.Owner as ILexEntry;
							Debug.Assert(entry != null);
							entry.ReplaceMoForm(stem, affix);
							form = affix;
						}
						else if (!fAffix && form is IMoAffixAllomorph)
						{
							IMoAffixAllomorph affix = form as IMoAffixAllomorph;
							IMoStemAllomorph stem = CreateNewMoStemAllomorph();
							ILexEntry entry = form.Owner as ILexEntry;
							Debug.Assert(entry != null);
							entry.ReplaceMoForm(affix, stem);
							form = stem;
						}
						if (mmt != form.MorphTypeRA)
							form.MorphTypeRA = mmt;
						fTypeSpecified = true;
						break;
					case "environment":
						List<IPhEnvironment> rgenv = FindOrCreateEnvironment(lt.Value);
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
		}

		private static void AddEnvironmentIfNeeded(List<IPhEnvironment> rgnew, IFdoReferenceCollection<IPhEnvironment> rgenv)
		{
			if (rgenv != null && rgnew != null)
			{
				bool fAlready = false;
				foreach (IPhEnvironment env in rgnew)
				{
					if (rgenv.Contains(env))
					{
						fAlready = true;
						break;
					}
				}
				if (!fAlready && rgnew.Count > 0)
					rgenv.Add(rgnew[0]);
			}
		}

		private bool MoFormTraitsConflict(IMoForm mf, List<LiftTrait> list)
		{
			foreach (LiftTrait lt in list)
			{
				switch (lt.Name.ToLowerInvariant())
				{
					case RangeNames.sDbMorphTypesOAold:	// original FLEX export = MorphType
					case RangeNames.sDbMorphTypesOA:
						if (mf.MorphTypeRA != null)
						{
							IMoMorphType mmt = FindMorphType(lt.Value);
							if (mf.MorphTypeRA != mmt)
								return true;
						}
						break;
					case "environment":
						if (mf is IMoStemAllomorph)
						{
							if ((mf as IMoStemAllomorph).PhoneEnvRC.Count > 0)
							{
								//int hvo = FindOrCreateEnvironment(lt.Value);
							}
						}
						else if (mf is IMoAffixAllomorph)
						{
							if ((mf as IMoAffixAllomorph).PhoneEnvRC.Count > 0)
							{
								//int hvo = FindOrCreateEnvironment(lt.Value);
							}
						}
						break;
				}
			}
			return false;
		}

		private void ProcessMoFormFields(IMoForm mf, CmLiftVariant lv)
		{
			foreach (LiftField field in lv.Fields)
			{
				switch (field.Type.ToLowerInvariant())
				{
					default:
						ProcessUnknownField(mf, lv, field,
							"MoForm", "custom-variant-", MoFormTags.kClassId);
						break;
				}
			}
		}

		private void CreateEntryPronunciations(ILexEntry le, CmLiftEntry entry)
		{
			foreach (CmLiftPhonetic phon in entry.Pronunciations)
			{
				IgnoreNewWs();
				ILexPronunciation pron = CreateNewLexPronunciation();
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
			Dictionary<int, CmLiftPhonetic> dictHvoPhon = new Dictionary<int, CmLiftPhonetic>();
			foreach (CmLiftPhonetic phon in entry.Pronunciations)
			{
				IgnoreNewWs();
				ILexPronunciation pron = FindMatchingPronunciation(le, dictHvoPhon, phon);
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
			IgnoreNewWs();
			foreach (string lang in langs)
			{
				int ws = GetWsFromLiftLang(lang);
				if (ws != 0)
				{
					IWritingSystem wsObj = GetExistingWritingSystem(ws);
					if (!m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems.Contains(wsObj))
						m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems.Add(wsObj);
				}
			}
		}

		private string CopyPronunciationFile(string sFile)
		{
			/*string sLiftDir = */
			Path.GetDirectoryName(m_sLiftFile);
			// Paths to try for resolving given filename:
			// {directory of LIFT file}/audio/filename
			// {FW LinkedFilesRootDir}/filename
			// {FW LinkedFilesRootDir}/Media/filename
			// {FW DataDir}/filename
			// {FW DataDir}/Media/filename
			// give up and store relative path Pictures/filename (even though it doesn't exist)
			string sPath = Path.Combine(Path.GetDirectoryName(m_sLiftFile),
				String.Format("audio{0}{1}", Path.DirectorySeparatorChar, sFile));
			sPath = CopyFileToLinkedFiles(sFile, sPath, DirectoryFinder.ksMediaDir);
			if (!File.Exists(sPath) && !String.IsNullOrEmpty(m_cache.LangProject.LinkedFilesRootDir))
			{
				sPath = Path.Combine(m_cache.LangProject.LinkedFilesRootDir, sFile);
				if (!File.Exists(sPath) && !String.IsNullOrEmpty(m_cache.LangProject.LinkedFilesRootDir))
				{
					sPath = Path.Combine(m_cache.LangProject.LinkedFilesRootDir,
						String.Format("Media{0}{1}", Path.DirectorySeparatorChar, sFile));
					if (!File.Exists(sPath))
					{
						sPath = Path.Combine(DirectoryFinder.FWDataDirectory, sFile);
						if (!File.Exists(sPath))
							sPath = Path.Combine(DirectoryFinder.FWDataDirectory,
								String.Format("Media{0}{1}", Path.DirectorySeparatorChar, sFile));
					}
				}
			}
			return sPath;
		}

		private void MergePronunciationMedia(ILexPronunciation pron, CmLiftPhonetic phon)
		{
			AddNewWsToBothVernAnal();
			foreach (LiftUrlRef uref in phon.Media)
			{
				string sFile = uref.Url;
				// TODO-Linux: this looks suspicious
				sFile = sFile.Replace('/', '\\');
				int ws;
				string sLabel;
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
				ICmMedia media = FindMatchingMedia(pron.MediaFilesOS, sFile, uref.Label);
				if (media == null)
				{
					media = CreateNewCmMedia();
					pron.MediaFilesOS.Add(media);
					string sPath = CopyPronunciationFile(sFile);
					try
					{
						if (!String.IsNullOrEmpty(sLabel))
							media.Label.set_String(ws, sLabel);
						if (!String.IsNullOrEmpty(sPath))
						{
							ICmFolder cmfMedia = null;
							foreach (ICmFolder cmf in m_cache.LangProject.MediaOC)
							{
								for (int i = 0; i < cmf.Name.StringCount; ++i)
								{
									int wsT;
									ITsString tss = cmf.Name.GetStringFromIndex(i, out wsT);
									if (tss.Text == CmFolderTags.LocalMedia)
									{
										cmfMedia = cmf;
										break;
									}
								}
								if (cmfMedia != null)
									break;
							}
							if (cmfMedia == null)
							{
								ICmFolderFactory factFolder = m_cache.ServiceLocator.GetInstance<ICmFolderFactory>();
								cmfMedia = factFolder.Create();
								m_cache.LangProject.MediaOC.Add(cmfMedia);
								cmfMedia.Name.UserDefaultWritingSystem = m_cache.TsStrFactory.MakeString(CmFolderTags.LocalMedia, m_cache.DefaultUserWs);
							}
							ICmFile file = null;
							foreach (ICmFile cf in cmfMedia.FilesOC)
							{
								if (cf.AbsoluteInternalPath == sPath)
								{
									file = cf;
									break;
								}
							}
							if (file == null)
							{
								ICmFileFactory factFile = m_cache.ServiceLocator.GetInstance<ICmFileFactory>();
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
						media.MediaFileRA.InternalPath =
							String.Format("Media{0}{1}", Path.DirectorySeparatorChar, sFile);
					}
				}
				else
				{
					// When doing Send/Receive we should copy existing media in case they changed.
					if (m_msImport == MergeStyle.MsKeepOnlyNew)
						CopyPronunciationFile(sFile);
				}
				AddNewWsToBothVernAnal();
				MergeInMultiString(media.Label, CmMediaTags.kflidLabel, uref.Label, media.Guid);
			}
		}

		private ICmMedia FindMatchingMedia(IFdoOwningSequence<ICmMedia> rgmedia, string sFile,
			LiftMultiText lmtLabel)
		{
			ICmMedia mediaMatching = null;
			int cMatches = 0;
			AddNewWsToBothVernAnal();
			foreach (ICmMedia media in rgmedia)
			{
				if (media.MediaFileRA == null)
					continue;	// should NEVER happen!
				if (media.MediaFileRA.InternalPath == sFile ||
					Path.GetFileName(media.MediaFileRA.InternalPath) == sFile)
				{
					int cCurrent = MultiTsStringMatches(media.Label, lmtLabel);
					if (cCurrent >= cMatches)
					{
						mediaMatching = media;
						cMatches = cCurrent;
					}
				}
			}
			return mediaMatching;

		}

		private bool EntryPronunciationsConflict(ILexEntry le, List<CmLiftPhonetic> list)
		{
			if (le.PronunciationsOS.Count == 0 || list.Count == 0)
				return false;
			int cCommon = 0;
			Dictionary<int, CmLiftPhonetic> dictHvoPhon = new Dictionary<int, CmLiftPhonetic>();
			IgnoreNewWs();
			foreach (CmLiftPhonetic phon in list)
			{
				ILexPronunciation pron = FindMatchingPronunciation(le, dictHvoPhon, phon);
				if (pron != null)
				{
					if (MultiUnicodeStringsConflict(pron.Form, phon.Form, false, Guid.Empty, 0))
					{
						m_cdConflict = new ConflictingEntry(String.Format("Pronunciation ({0})",
							TsStringAsHtml(pron.Form.BestVernacularAnalysisAlternative, m_cache)), le, this);
						return true;
					}
					if (PronunciationFieldsOrTraitsConflict(pron, phon))
					{
						m_cdConflict = new ConflictingEntry(String.Format("Pronunciation ({0}) details",
							TsStringAsHtml(pron.Form.BestVernacularAnalysisAlternative, m_cache)), le, this);
						return true;
					}
					// TODO: Compare phon.Media and pron.MediaFilesOS
					++cCommon;
				}
			}
			if (cCommon < Math.Min(le.PronunciationsOS.Count, list.Count))
			{
				m_cdConflict = new ConflictingEntry("Pronunciations", le, this);
				return true;
			}
			else
			{
				return false;
			}
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
		private ILexPronunciation FindMatchingPronunciation(ILexEntry le, Dictionary<int, CmLiftPhonetic> dictHvoPhon,
			CmLiftPhonetic phon)
		{
			ILexPronunciation lexpron = null;
			ILexPronunciation lexpronNoMedia = null;
			int cMatches = 0;
			foreach (ILexPronunciation pron in le.PronunciationsOS)
			{
				if (dictHvoPhon.ContainsKey(pron.Hvo))
					continue;
				bool fFormMatches = false;
				int cCurrent = 0;
				IgnoreNewWs();
				if (phon.Form.Count == 0)
				{
					Dictionary<int, string> forms = GetAllUnicodeAlternatives(pron.Form);
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
						int cFilesMatch = 0;
						for (int i = 0; i < phon.Media.Count; ++i)
						{
							string sURL = phon.Media[i].Url;
							if (sURL == null)
								continue;
							string sFile = Path.GetFileName(sURL);
							for (int j = 0; j < pron.MediaFilesOS.Count; ++j)
							{
								ICmFile cf = pron.MediaFilesOS[i].MediaFileRA;
								if (cf != null)
								{
									string sPath = cf.InternalPath;
									if (sPath == null)
										continue;
									if (sFile.ToLowerInvariant() == Path.GetFileName(sPath).ToLowerInvariant())
										++cFilesMatch;
								}
							}
						}
						if (phon.Media.Count == 0 || cFilesMatch > 0)
							lexpron = pron;
						else
							lexpronNoMedia = pron;
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
			else if (lexpronNoMedia != null)
			{
				dictHvoPhon.Add(lexpronNoMedia.Hvo, phon);
				return lexpronNoMedia;
			}
			else
			{
				return null;
			}
		}

		private Dictionary<int, string> GetAllUnicodeAlternatives(ITsMultiString tsm)
		{
			Dictionary<int, string> dict = new Dictionary<int, string>();
			for (int i = 0; i < tsm.StringCount; ++i)
			{
				int ws;
				ITsString tss = tsm.GetStringFromIndex(i, out ws);
				if (tss.Text != null && ws != 0)
					dict.Add(ws, tss.Text);
			}
			return dict;
		}

		private Dictionary<int, ITsString> GetAllTsStringAlternatives(ITsMultiString tsm)
		{
			Dictionary<int, ITsString> dict = new Dictionary<int, ITsString>();
			for (int i = 0; i < tsm.StringCount; ++i)
			{
				int ws;
				ITsString tss = tsm.GetStringFromIndex(i, out ws);
				if (tss.Text != null && ws != 0)
					dict.Add(ws, tss);
			}
			return dict;
		}

		private void ProcessPronunciationFieldsAndTraits(ILexPronunciation pron, CmLiftPhonetic phon)
		{
			foreach (LiftField field in phon.Fields)
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
						ProcessUnknownField(pron, phon, field,
							"LexPronunciation", "custom-pronunciation-", LexPronunciationTags.kClassId);
						break;
				}
			}
			foreach (LiftTrait trait in phon.Traits)
			{
				switch (trait.Name.ToLowerInvariant())
				{
					case RangeNames.sLocationsOA:
						ICmLocation loc = FindOrCreateLocation(trait.Value);
						if (pron.LocationRA != loc && (m_fCreatingNewEntry || m_msImport != MergeStyle.MsKeepOld ||
							pron.LocationRA == null))
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

		private bool PronunciationFieldsOrTraitsConflict(ILexPronunciation pron, CmLiftPhonetic phon)
		{
			IgnoreNewWs();
			foreach (LiftField field in phon.Fields)
			{
				switch (field.Type.ToLowerInvariant())
				{
					case "cvpattern":
					case "cv-pattern":
						if (StringsConflict(pron.CVPattern, GetFirstLiftTsString(field.Content)))
							return true;
						break;
					case "tone":
						if (StringsConflict(pron.Tone, GetFirstLiftTsString(field.Content)))
							return true;
						break;
				}
			}
			foreach (LiftTrait trait in phon.Traits)
			{
				switch (trait.Name.ToLowerInvariant())
				{
					case RangeNames.sLocationsOA:
						ICmLocation loc = FindOrCreateLocation(trait.Value);
						if (pron.LocationRA != null && pron.LocationRA != loc)
							return true;
						break;
					default:
						break;
				}
			}
			return false;
		}

		private void ProcessEntryEtymologies(ILexEntry le, CmLiftEntry entry)
		{
			bool fFirst = true;
			foreach (CmLiftEtymology let in entry.Etymologies)
			{
				if (fFirst)
				{
					if (le.EtymologyOA == null)
					{
						ILexEtymology ety = CreateNewLexEtymology();
						le.EtymologyOA = ety;
					}
					AddNewWsToVernacular();
					MergeInMultiUnicode(le.EtymologyOA.Form, LexEtymologyTags.kflidForm, let.Form, le.EtymologyOA.Guid);
					AddNewWsToAnalysis();
					MergeInMultiUnicode(le.EtymologyOA.Gloss, LexEtymologyTags.kflidGloss, let.Gloss, le.EtymologyOA.Guid);
					// See LT-11765 for issues here.
					if (let.Source != null && let.Source != "UNKNOWN")
					{
						if (!m_fCreatingNewEntry && m_msImport == MergeStyle.MsKeepOld)
						{
							if (String.IsNullOrEmpty(le.EtymologyOA.Source))
								le.EtymologyOA.Source = let.Source;
						}
						else
						{
							le.EtymologyOA.Source = let.Source;
						}
					}
					ProcessEtymologyFieldsAndTraits(le.EtymologyOA, let);
					StoreDatesInResidue(le.EtymologyOA, let);
					fFirst = false;
				}
				else
				{
					StoreEtymologyAsResidue(le, let);
				}
			}
		}

		private bool EntryEtymologiesConflict(ILexEtymology lexety, List<CmLiftEtymology> list)
		{
			if (lexety == null || list.Count == 0)
				return false;
			foreach (CmLiftEtymology ety in list)
			{
				AddNewWsToVernacular();
				if (MultiUnicodeStringsConflict(lexety.Form, ety.Form, false, Guid.Empty, 0))
					return true;
				AddNewWsToAnalysis();
				if (MultiUnicodeStringsConflict(lexety.Gloss, ety.Gloss, false, Guid.Empty, 0))
					return true;
				IgnoreNewWs();
				if (StringsConflict(lexety.Source, ety.Source))
					return true;
				if (EtymologyFieldsConflict(lexety, ety.Fields))
					return true;
				break;
			}
			return false;
		}

		private void ProcessEtymologyFieldsAndTraits(ILexEtymology ety, CmLiftEtymology let)
		{
			foreach (LiftField field in let.Fields)
			{
				AddNewWsToAnalysis();
				switch (field.Type.ToLowerInvariant())
				{
					case "comment":
						MergeInMultiString(ety.Comment, LexEtymologyTags.kflidComment, field.Content, ety.Guid);
						break;
					//case "multiform":		causes problems on round-tripping
					//    MergeIn(ety.Form, field.Content, ety.Guid);
					//    break;
					default:
						ProcessUnknownField(ety, let, field,
							"LexEtymology", "custom-etymology-", LexEtymologyTags.kClassId);
						break;
				}
			}
			foreach (LiftTrait trait in let.Traits)
			{
				StoreTraitAsResidue(ety, trait);
			}
		}

		private bool EtymologyFieldsConflict(ILexEtymology lexety, List<LiftField> list)
		{
			if (lexety == null || lexety.Comment == null)
				return false;
			AddNewWsToAnalysis();
			foreach (LiftField field in list)
			{
				switch (field.Type.ToLowerInvariant())
				{
					case "comment":
						if (MultiTsStringsConflict(lexety.Comment, field.Content))
							return true;
						break;
				}
			}
			return false;
		}

		private void ProcessEntryRelations(ILexEntry le, CmLiftEntry entry)
		{
			// Due to possible forward references, wait until the end to process relations.
			foreach (CmLiftRelation rel in entry.Relations)
			{
				if (rel.Type != "_component-lexeme" && String.IsNullOrEmpty(rel.Ref))
				{
					XmlDocument xdResidue = FindOrCreateResidue(le);
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
							PendingLexEntryRef pend = new PendingLexEntryRef(le, rel, entry);
							pend.Residue = CreateRelationResidue(rel);
							m_rgPendingLexEntryRefs.Add(pend);
							break;
						default:
							string sResidue = CreateRelationResidue(rel);
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
				ILexSense ls = CreateNewLexSense(sense.Guid, le, out fNeedNewId);
				FillInNewSense(ls, sense, fNeedNewId);
			}
			finally
			{
				m_fCreatingNewSense = false;
			}
		}

		private void CreateSubsense(ILexSense ls, CmLiftSense sub)
		{
			bool fSavedCreatingNew = m_fCreatingNewSense;
			try
			{
				m_fCreatingNewSense = true;
				bool fNeedNewId;
				ILexSense lsSub = CreateNewLexSense(sub.Guid, ls, out fNeedNewId);
				FillInNewSense(lsSub, sub, fNeedNewId);
			}
			finally
			{
				m_fCreatingNewSense = fSavedCreatingNew;
			}
		}

		private void FillInNewSense(ILexSense ls, CmLiftSense sense, bool fNeedNewId)
		{
			if (m_cdConflict != null && m_cdConflict is ConflictingSense)
			{
				(m_cdConflict as ConflictingSense).DupSense = ls;
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
				XmlDocument xd = FindOrCreateResidue(ls, sense.Id, LexSenseTags.kflidLiftResidue);
				XmlAttribute xa = xd.FirstChild.Attributes["id"];
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
			foreach (CmLiftSense sub in sense.Subsenses)
				CreateSubsense(ls, sub);
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

			Dictionary<CmLiftSense, ILexSense> map = new Dictionary<CmLiftSense, ILexSense>();
			Set<int> setUsed = new Set<int>();
			foreach (CmLiftSense sub in sense.Subsenses)
			{
				ILexSense lsSub = FindExistingSense(ls.SensesOS, sub);
				map.Add(sub, lsSub);
				if (lsSub != null)
					setUsed.Add(lsSub.Hvo);
			}
			// If we're keeping only the imported data, delete any unused subsense.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				foreach (int hvo in ls.SensesOS.ToHvoArray())
				{
					if (!setUsed.Contains(hvo))
						m_deletedObjects.Add(hvo);
				}
			}
			foreach (CmLiftSense sub in sense.Subsenses)
			{
				ILexSense lsSub;
				map.TryGetValue(sub, out lsSub);
				if (lsSub == null || (m_msImport == MergeStyle.MsKeepBoth && SenseHasConflictingData(lsSub, sub)))
					CreateSubsense(ls, sub);
				else
					MergeIntoExistingSense(lsSub, sub);
			}
			StoreAnnotationsAndDatesInResidue(ls, sense);
		}

		private void StoreSenseId(ILexSense ls, string sId)
		{
			if (!String.IsNullOrEmpty(sId))
			{
				FindOrCreateResidue(ls, sId, LexSenseTags.kflidLiftResidue);
				MapIdToObject(sId, ls);
			}
		}

		private void CreateSenseExamples(ILexSense ls, CmLiftSense sense)
		{
			foreach (CmLiftExample expl in sense.Examples)
			{
				ILexExampleSentence les = CreateNewLexExampleSentence(expl.Guid, ls);
				if (!String.IsNullOrEmpty(expl.Id))
					MapIdToObject(expl.Id, les);
				AddNewWsToVernacular();
				MergeInMultiString(les.Example, LexExampleSentenceTags.kflidExample, expl.Content, les.Guid);
				CreateExampleTranslations(les, expl);
				ProcessExampleNotes(les, expl);
				if (TsStringIsNullOrEmpty(les.Reference) && !String.IsNullOrEmpty(expl.Source))
					les.Reference = m_cache.TsStrFactory.MakeString(expl.Source, m_cache.DefaultAnalWs);
				ProcessExampleFields(les, expl);
				ProcessExampleTraits(les, expl);
				StoreExampleResidue(les, expl);
			}
		}

		private void MergeSenseExamples(ILexSense ls, CmLiftSense sense)
		{
			Dictionary<CmLiftExample, ILexExampleSentence> map = new Dictionary<CmLiftExample, ILexExampleSentence>();
			Set<int> setUsed = new Set<int>();
			foreach (CmLiftExample expl in sense.Examples)
			{
				ILexExampleSentence les = FindingMatchingExampleSentence(ls, expl);
				map.Add(expl, les);
				if (les != null)
					setUsed.Add(les.Hvo);
			}
			// If we're keeping only the imported data, delete any unused example.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				foreach (int hvo in ls.ExamplesOS.ToHvoArray())
				{
					if (!setUsed.Contains(hvo))
						m_deletedObjects.Add(hvo);
				}
			}
			foreach (CmLiftExample expl in sense.Examples)
			{
				ILexExampleSentence les;
				map.TryGetValue(expl, out les);
				if (les == null)
					les = CreateNewLexExampleSentence(expl.Guid, ls);
				if (!String.IsNullOrEmpty(expl.Id))
					MapIdToObject(expl.Id, les);
				AddNewWsToVernacular();
				MergeInMultiString(les.Example, LexExampleSentenceTags.kflidExample, expl.Content, les.Guid);
				MergeExampleTranslations(les, expl);
				ProcessExampleNotes(les, expl);
				if (TsStringIsNullOrEmpty(les.Reference) && !String.IsNullOrEmpty(expl.Source))
					les.Reference = m_cache.TsStrFactory.MakeString(expl.Source, m_cache.DefaultAnalWs);
			}
		}

		private ILexExampleSentence FindingMatchingExampleSentence(ILexSense ls, CmLiftExample expl)
		{
			ILexExampleSentence les = null;
			if (expl.Guid != Guid.Empty)
			{
				ICmObject cmo = GetObjectForGuid(expl.Guid);
				if (cmo != null && cmo is ILexExampleSentence)
				{
					les = cmo as ILexExampleSentence;
					if (les.Owner != ls)
						les = null;
				}
			}
			if (les == null)
				les = FindExampleSentence(ls.ExamplesOS, expl);
			return les;
		}

		private bool SenseExamplesConflict(ILexSense ls, List<CmLiftExample> list)
		{
			if (ls.ExamplesOS.Count == 0 || list.Count == 0)
				return false;
			foreach (CmLiftExample expl in list)
			{
				ILexExampleSentence les = null;
				if (expl.Guid != Guid.Empty)
				{
					ICmObject cmo = GetObjectForGuid(expl.Guid);
					if (cmo != null && cmo is ILexExampleSentence)
					{
						les = cmo as ILexExampleSentence;
						if (les.Owner.Hvo != ls.Hvo)
							les = null;
					}
				}
				if (les == null)
					les = FindExampleSentence(ls.ExamplesOS, expl);
				if (les == null)
					continue;
				AddNewWsToVernacular();
				MergeInMultiString(les.Example, LexExampleSentenceTags.kflidExample, expl.Content, les.Guid);
				if (MultiTsStringsConflict(les.Example, expl.Content))
				{
					m_cdConflict = new ConflictingSense(String.Format("Example ({0})",
						TsStringAsHtml(les.Example.BestVernacularAlternative, m_cache)), ls, this);
					return true;
				}
				IgnoreNewWs();
				if (StringsConflict(les.Reference.Text, expl.Source))
				{
					m_cdConflict = new ConflictingSense(String.Format("Example ({0}) Reference",
						TsStringAsHtml(les.Example.BestVernacularAlternative, m_cache)), ls, this);
					return true;
				}
				AddNewWsToAnalysis();
				if (ExampleTranslationsConflict(les, expl))
				{
					m_cdConflict = new ConflictingSense(String.Format("Example ({0}) Translations",
						TsStringAsHtml(les.Example.BestVernacularAlternative, m_cache)), ls, this);
					return true;
				}
				if (ExampleNotesConflict(les, expl))
				{
					m_cdConflict = new ConflictingSense(String.Format("Example ({0}) Reference",
						TsStringAsHtml(les.Example.BestVernacularAlternative, m_cache)), ls, this);
					return true;
				}
			}
			return false;
		}

		private ILexExampleSentence FindExampleSentence(IFdoOwningSequence<ILexExampleSentence> rgexamples, CmLiftExample expl)
		{
			List<ILexExampleSentence> matches = new List<ILexExampleSentence>();
			int cMatches = 0;
			AddNewWsToVernacular();
			foreach (ILexExampleSentence les in rgexamples)
			{
				int cCurrent = MultiTsStringMatches(les.Example, expl.Content);
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
				else if ((expl.Content == null || expl.Content.IsEmpty) &&
					(les.Example == null || les.Example.BestVernacularAnalysisAlternative.Equals(les.Example.NotFoundTss)))
				{
					matches.Add(les);
				}
			}
			if (matches.Count == 0)
				return null;
			else if (matches.Count == 1)
				return matches[0];
			// Okay, we have more than one example sentence that match equally well in Example.
			// So let's look at the other fields.
			ILexExampleSentence lesMatch = null;
			cMatches = 0;
			foreach (ILexExampleSentence les in matches)
			{
				IgnoreNewWs();
				bool fSameReference = MatchingItemInNotes(les.Reference, "reference", expl.Notes);
				AddNewWsToAnalysis();
				int cCurrent = TranslationsMatch(les.TranslationsOC, expl.Translations);
				if (fSameReference &&
					cCurrent == expl.Translations.Count &&
					cCurrent == les.TranslationsOC.Count)
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
			string sItem = tss.Text;
			// Review: Should we match on the writing system inside the tss as well?
			bool fTypeFound = false;
			foreach (CmLiftNote note in rgnotes)
			{
				if (note.Type == sType)
				{
					fTypeFound = true;
					foreach (string sWs in note.Content.Keys)
					{
						if (sItem == note.Content[sWs].Text)
							return true;
					}
				}
			}
			return String.IsNullOrEmpty(sItem) && !fTypeFound;
		}

		private int TranslationsMatch(IFdoOwningCollection<ICmTranslation> oldList, List<LiftTranslation> newList)
		{
			if (oldList.Count == 0 || newList.Count == 0)
				return 0;
			int cMatches = 0;
			foreach (LiftTranslation tran in newList)
			{
				ICmTranslation ct = FindExampleTranslation(oldList, tran);
				if (ct != null)
					++cMatches;
			}
			return cMatches;
		}

//		private static bool StringsMatch(string sOld, string sNew)
//		{
//			if (String.IsNullOrEmpty(sOld) && String.IsNullOrEmpty(sNew))
//			{
//				return true;
//			}
//			else if (String.IsNullOrEmpty(sOld) || String.IsNullOrEmpty(sNew))
//			{
//				return false;
//			}
//			else
//			{
//				string sOldNorm = Icu.Normalize(sOld, Icu.UNormalizationMode.UNORM_NFD);
//				string sNewNorm = Icu.Normalize(sNew, Icu.UNormalizationMode.UNORM_NFD);
//				return sOldNorm == sNewNorm;
//			}
//		}

		private void CreateExampleTranslations(ILexExampleSentence les, CmLiftExample expl)
		{
			AddNewWsToAnalysis();
			foreach (LiftTranslation tran in expl.Translations)
			{
				ICmPossibility type = null;
				if (!String.IsNullOrEmpty(tran.Type))
					type = FindOrCreateTranslationType(tran.Type);
				ICmTranslation ct = CreateNewCmTranslation(les, type);
				les.TranslationsOC.Add(ct);
				MergeInMultiString(ct.Translation, CmTranslationTags.kflidTranslation, tran.Content, ct.Guid);
			}
		}

		private void MergeExampleTranslations(ILexExampleSentence les, CmLiftExample expl)
		{
			Dictionary<LiftTranslation, ICmTranslation> map = new Dictionary<LiftTranslation, ICmTranslation>();
			Set<int> setUsed = new Set<int>();
			AddNewWsToAnalysis();
			foreach (LiftTranslation tran in expl.Translations)
			{
				ICmTranslation ct = FindExampleTranslation(les.TranslationsOC, tran);
				map.Add(tran, ct);
				if (ct != null)
					setUsed.Add(ct.Hvo);
			}
			// If we're keeping only the imported data, erase any unused existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				foreach (int hvo in les.TranslationsOC.ToHvoArray())
				{
					if (!setUsed.Contains(hvo))
						m_deletedObjects.Add(hvo);
				}
			}
			foreach (LiftTranslation tran in expl.Translations)
			{
				ICmTranslation ct;
				map.TryGetValue(tran, out ct);
				ICmPossibility type = null;
				if (!String.IsNullOrEmpty(tran.Type))
					type = FindOrCreateTranslationType(tran.Type);
				if (ct == null)
				{
					ct = CreateNewCmTranslation(les, type);
					les.TranslationsOC.Add(ct);
				}
				MergeInMultiString(ct.Translation, CmTranslationTags.kflidTranslation, tran.Content, ct.Guid);
				if (type != null &&
					ct.TypeRA != type &&
					(m_fCreatingNewSense || m_msImport != MergeStyle.MsKeepOld))
				{
					ct.TypeRA = type;
				}
			}
		}

		private bool ExampleTranslationsConflict(ILexExampleSentence les, CmLiftExample expl)
		{
			if (les.TranslationsOC.Count == 0 || expl.Translations.Count == 0)
				return false;
			AddNewWsToAnalysis();
			foreach (LiftTranslation tran in expl.Translations)
			{
				ICmTranslation ct = FindExampleTranslation(les.TranslationsOC, tran);
				if (ct == null)
					continue;
				if (MultiTsStringsConflict(ct.Translation, tran.Content))
					return true;
				if (!String.IsNullOrEmpty(tran.Type))
				{
					ICmPossibility type = FindOrCreateTranslationType(tran.Type);
					if (ct.TypeRA != type && ct.TypeRA != null)
						return true;
				}
			}
			return false;
		}

		private ICmTranslation FindExampleTranslation(IFdoOwningCollection<ICmTranslation> rgtranslations,
			LiftTranslation tran)
		{
			ICmTranslation ctMatch = null;
			int cMatches = 0;
			AddNewWsToAnalysis();
			foreach (ICmTranslation ct in rgtranslations)
			{
				int cCurrent = MultiTsStringMatches(ct.Translation, tran.Content);
				if (cCurrent > cMatches)
				{
					ctMatch = ct;
					cMatches = cCurrent;
				}
				else if ((tran.Content == null || tran.Content.IsEmpty) &&
					(ct.Translation == null || ct.Translation.BestAnalysisVernacularAlternative.Equals(ct.Translation.NotFoundTss)))
				{
					return ct;
				}
			}
			return ctMatch;
		}

		private void ProcessExampleNotes(ILexExampleSentence les, CmLiftExample expl)
		{
			foreach (CmLiftNote note in expl.Notes)
			{
				if (note.Type == null)
					note.Type = String.Empty;
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
				return false;
			IgnoreNewWs();
			foreach (CmLiftNote note in expl.Notes)
			{
				if (note.Type == null)
					note.Type = String.Empty;
				switch (note.Type.ToLowerInvariant())
				{
					case "reference":
						if (StringsConflict(les.Reference, GetFirstLiftTsString(note.Content)))
							return true;
						break;
				}
			}
			return false;
		}

		private void ProcessExampleFields(ILexExampleSentence les, CmLiftExample expl)
		{
			// Note: when/if LexExampleSentence.Reference is written as a <field>
			// instead of a <note>, the next loop will presumably be changed.
			foreach (LiftField field in expl.Fields)
			{
				switch (field.Type.ToLowerInvariant())
				{
					default:
						ProcessUnknownField(les, expl, field,
							"LexExampleSentence", "custom-example-", LexExampleSentenceTags.kClassId);
						break;
				}
			}
		}

		private void ProcessExampleTraits(ILexExampleSentence lexExmp, CmLiftExample example)
		{
			foreach (LiftTrait lt in example.Traits)
			{
				switch (lt.Name.ToLowerInvariant())
				{
					default:
						ProcessUnknownTrait(example, lt, lexExmp);
						break;
				}
			}
		}

		private void ProcessSenseGramInfo(ILexSense ls, CmLiftSense sense)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				// except we always need a grammatical info element...
			}
			if (sense.GramInfo == null)
				return;
			if (!m_fCreatingNewSense && m_msImport == MergeStyle.MsKeepOld && ls.MorphoSyntaxAnalysisRA != null)
				return;
			LiftGrammaticalInfo gram = sense.GramInfo;
			string sTraitPos = gram.Value;
			IPartOfSpeech pos = null;
			if (!String.IsNullOrEmpty(sTraitPos))
				pos = FindOrCreatePartOfSpeech(sTraitPos);
			ls.MorphoSyntaxAnalysisRA = FindOrCreateMSA(ls.Entry, pos, gram.Traits);
		}

		/// <summary>
		/// Creating individual MSAs for every sense, and then merging identical MSAs at the
		/// end is expensive: deleting each redundant MSA takes ~360 msec, which can add up
		/// quickly even for only a few hundred duplications created here.  (See LT-9006.)
		/// </summary>
		private IMoMorphSynAnalysis FindOrCreateMSA(ILexEntry le, IPartOfSpeech pos,
			List<LiftTrait> traits)
		{
			string sType = null;
			string sFromPOS = null;
			Dictionary<string, List<string>> dictPosSlots = new Dictionary<string, List<string>>();
			Dictionary<string, List<string>> dictPosInflClasses = new Dictionary<string, List<string>>();
			Dictionary<string, List<string>> dictPosFromInflClasses = new Dictionary<string, List<string>>();
			List<ICmPossibility> rgpossProdRestrict = new List<ICmPossibility>();
			List<ICmPossibility> rgpossFromProdRestrict = new List<ICmPossibility>();
			string sInflectionFeature = null;
			string sFromInflFeature = null;
			string sFromStemName = null;
			List<string> rgsResidue = new List<string>();
			foreach (LiftTrait trait in traits)
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
					ICmPossibility poss = FindOrCreateExceptionFeature(trait.Value);
					rgpossProdRestrict.Add(poss);
				}
				else if (trait.Name == RangeNames.sProdRestrictOAfrom)
				{
					ICmPossibility poss = FindOrCreateExceptionFeature(trait.Value);
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
					int len = trait.Name.Length - (trait.Name.EndsWith("-slot") ? 5 : 6);
					string sPos = trait.Name.Substring(0, len);
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
					int len = trait.Name.Length - (trait.Name.EndsWith("-infl-class") ? 11 : 16);
					string sPos = trait.Name.Substring(0, len);
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
					int flid = MoStemMsaTags.kflidMsFeatures;
					if (msaSense is IMoInflAffMsa)
						flid = MoInflAffMsaTags.kflidInflFeats;
					else if (msaSense is IMoDerivAffMsa)
						flid = MoDerivAffMsaTags.kflidToMsFeatures;
					LogInvalidFeatureString(le, sInflectionFeature, flid);
				}
				if (msaSense is IMoDerivAffMsa && !ParseFeatureString(sFromInflFeature, msaSense, true))
				{
					LogInvalidFeatureString(le, sFromInflFeature, MoDerivAffMsaTags.kflidFromMsFeatures);
				}
				if (!String.IsNullOrEmpty(sFromStemName))
					ProcessMsaStemName(sFromStemName, sFromPOS, msaSense, rgsResidue);
				StoreResidue(msaSense, rgsResidue);
			}
			return msaSense;
		}

		private void ProcessMsaStemName(string sFromStemName, string sFromPos,
			IMoMorphSynAnalysis msaSense, List<string> rgsResidue)
		{
			if (msaSense is IMoDerivAffMsa)
			{
				var key = String.Format("{0}:{1}", sFromPos, sFromStemName);
				IMoStemName stem;
				if (m_dictStemName.TryGetValue(key, out stem))
				{
					(msaSense as IMoDerivAffMsa).FromStemNameRA = stem;
					return;
				}
				// TODO: Create new IMoStemName object?
			}
			string sResidue = String.Format("<trait name=\"from-stem-name\" value=\"{0}\"/>",
				XmlUtils.MakeSafeXmlAttribute(sFromStemName));
			rgsResidue.Add(sResidue);
		}

		private void LogInvalidFeatureString(ILexEntry le, string sInflectionFeature, int flid)
		{
			InvalidData bad = new InvalidData(LexTextControls.ksCannotParseFeature,
				le.Guid, flid, sInflectionFeature, 0, m_cache, this);
			if (!m_rgInvalidData.Contains(bad))
				m_rgInvalidData.Add(bad);
		}

		private ICmPossibility FindOrCreateExceptionFeature(string sValue)
		{
			ICmPossibility poss;
			if (!m_dictExceptFeats.TryGetValue(sValue, out poss))
			{
				EnsureProdRestrictListExists();
				if (m_factCmPossibility == null)
					m_factCmPossibility = m_cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
				poss = m_factCmPossibility.Create();
				m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS.Add(poss);
				ITsString tss = m_cache.TsStrFactory.MakeString(sValue, m_cache.DefaultAnalWs);
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
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoStemMsa msaStem = msa as IMoStemMsa;
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
				(msaSense as IMoStemMsa).PartOfSpeechRA = pos;
			return true;
		}

		private void StoreMsaExceptionFeatures(List<ICmPossibility> rgpossProdRestrict,
			List<ICmPossibility> rgpossFromProdRestrict,
			IMoMorphSynAnalysis msaSense)
		{
			IMoStemMsa msaStem = msaSense as IMoStemMsa;
			if (msaStem != null)
			{
				foreach (ICmPossibility poss in rgpossProdRestrict)
					msaStem.ProdRestrictRC.Add(poss);
				return;
			}
			IMoInflAffMsa msaInfl = msaSense as IMoInflAffMsa;
			if (msaInfl != null)
			{
				foreach (ICmPossibility poss in rgpossProdRestrict)
					msaInfl.FromProdRestrictRC.Add(poss);
				return;
			}
			IMoDerivAffMsa msaDeriv = msaSense as IMoDerivAffMsa;
			if (msaDeriv != null)
			{
				foreach (ICmPossibility poss in rgpossProdRestrict)
					msaDeriv.ToProdRestrictRC.Add(poss);
				if (rgpossFromProdRestrict != null)
				{
					foreach (ICmPossibility poss in rgpossFromProdRestrict)
						msaDeriv.FromProdRestrictRC.Add(poss);
				}
				return;
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
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoUnclassifiedAffixMsa msaAffix = msa as IMoUnclassifiedAffixMsa;
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
				(msaSense as IMoUnclassifiedAffixMsa).PartOfSpeechRA = pos;
			return true;
		}

		/// <summary>
		/// Find or create an IMoDerivStepMsa which matches the given values.
		/// </summary>
		/// <param name="le">The entry.</param>
		/// <param name="pos">The part of speech.</param>
		/// <param name="dictPosSlots">The dict pos slots.</param>
		/// <param name="dictPosInflClasses">The dict pos infl classes.</param>
		/// <param name="rgsResidue">The RGS residue.</param>
		/// <param name="msaSense">The msa sense.</param>
		/// <returns>
		/// true if the desired MSA is newly created, false if it already exists
		/// </returns>
		private bool FindOrCreateDerivStepAffixMSA(ILexEntry le, IPartOfSpeech pos,
			Dictionary<string, List<string>> dictPosSlots,
			Dictionary<string, List<string>> dictPosInflClasses,
			List<string> rgsResidue,
			ref IMoMorphSynAnalysis msaSense)
		{
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoDerivStepMsa msaAffix = msa as IMoDerivStepMsa;
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
				(msaSense as IMoDerivStepMsa).PartOfSpeechRA = pos;
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
			if (!String.IsNullOrEmpty(sFromPOS))
				posFrom = FindOrCreatePartOfSpeech(sFromPOS);
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoDerivAffMsa msaAffix = msa as IMoDerivAffMsa;
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
				(msaSense as IMoDerivAffMsa).ToPartOfSpeechRA = pos;
			if (posFrom != null)
				(msaSense as IMoDerivAffMsa).FromPartOfSpeechRA = posFrom;
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
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoInflAffMsa msaAffix = msa as IMoInflAffMsa;
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
				(msaSense as IMoInflAffMsa).PartOfSpeechRA = pos;
			return true;
		}

		//private bool MsaResidueMatches(List<string> rgsResidue, IMoMorphSynAnalysis msa)
		//{
		//    string sResidue = (msa as MoMorphSynAnalysis).LiftResidueContent;
		//    if (String.IsNullOrEmpty(sResidue))
		//        return rgsResidue.Count == 0;
		//    int cch = 0;
		//    foreach (string s in rgsResidue)
		//    {
		//        if (sResidue.IndexOf(s) < 0)
		//            return false;
		//        cch += s.Length;
		//    }
		//    return sResidue.Length == cch;
		//}

		private void ProcessMsaInflectionClassInfo(Dictionary<string, List<string>> dictPosInflClasses,
			Dictionary<string, List<string>> dictPosFromInflClasses, IMoMorphSynAnalysis msa)
		{
			if (msa is IMoInflAffMsa || msa is IMoUnclassifiedAffixMsa)
				return;
			IMoDerivAffMsa msaDeriv = msa as IMoDerivAffMsa;
			IMoDerivStepMsa msaStep = msa as IMoDerivStepMsa;
			IMoStemMsa msaStem = msa as IMoStemMsa;
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
					return;
				if (msaStep != null && msaStep.InflectionClassRA != null)
					return;
				if (msaStem != null && msaStem.InflectionClassRA != null)
					return;
			}
			foreach (string sPos in dictPosInflClasses.Keys)
			{
				IPartOfSpeech pos = FindOrCreatePartOfSpeech(sPos);
				List<string> rgsInflClasses = dictPosInflClasses[sPos];
				if (rgsInflClasses.Count > 0 && pos != null)
				{
					foreach (string sInflClass in rgsInflClasses)
					{
						IMoInflClass incl = FindOrCreateInflectionClass(pos, sInflClass);
						if (msaDeriv != null)
							msaDeriv.ToInflectionClassRA = incl;
						else if (msaStep != null)
							msaStep.InflectionClassRA = incl;
						else if (msaStem != null)
							msaStem.InflectionClassRA = incl;
					}
				}
			}
			if (msaDeriv != null && dictPosFromInflClasses != null)
			{
				foreach (string sPos in dictPosFromInflClasses.Keys)
				{
					IPartOfSpeech pos = FindOrCreatePartOfSpeech(sPos);
					List<string> rgsInflClasses = dictPosFromInflClasses[sPos];
					if (rgsInflClasses.Count > 0 && pos != null)
					{
						foreach (string sInflClass in rgsInflClasses)
						{
							IMoInflClass incl = FindOrCreateInflectionClass(pos, sInflClass);
							msaDeriv.FromInflectionClassRA = incl;		// last one wins...
						}
					}
				}
			}
		}

		private IMoInflClass FindOrCreateInflectionClass(IPartOfSpeech pos, string sInflClass)
		{
			IMoInflClass incl = null;
			foreach (IMoInflClass inclT in pos.InflectionClassesOC)
			{
				if (HasMatchingUnicodeAlternative(sInflClass.ToLowerInvariant(), inclT.Abbreviation, inclT.Name))
				{
					incl = inclT;
					break;
				}
			}
			if (incl == null)
			{
				incl = CreateNewMoInflClass();
				pos.InflectionClassesOC.Add(incl);
				incl.Name.set_String(m_cache.DefaultAnalWs, sInflClass);
				m_rgnewInflClasses.Add(incl);
			}
			return incl;
		}

		private bool MsaInflClassInfoMatches(Dictionary<string, List<string>> dictPosInflClasses,
			Dictionary<string, List<string>> dictPosFromInflClasses, IMoMorphSynAnalysis msa)
		{
			if (msa is IMoInflAffMsa || msa is IMoUnclassifiedAffixMsa)
				return true;
			bool fMatch = MsaMatchesInflClass(dictPosInflClasses, msa, false);
			if (fMatch && msa is IMoDerivAffMsa && dictPosFromInflClasses != null)
				fMatch = MsaMatchesInflClass(dictPosFromInflClasses, msa, true);
			return fMatch;
		}

		private bool MsaMatchesInflClass(Dictionary<string, List<string>> dictPosInflClasses,
			IMoMorphSynAnalysis msa, bool fFrom)
		{
			IMoDerivAffMsa msaDeriv = msa as IMoDerivAffMsa;
			IMoDerivStepMsa msaStep = msa as IMoDerivStepMsa;
			IMoStemMsa msaStem = msa as IMoStemMsa;
			bool fMatch = true;
			foreach (string sPos in dictPosInflClasses.Keys)
			{
				IPartOfSpeech pos = FindOrCreatePartOfSpeech(sPos);
				List<string> rgsInflClasses = dictPosInflClasses[sPos];
				if (rgsInflClasses.Count > 0 && pos != null)
				{
					foreach (string sInflClass in rgsInflClasses)
					{
						IMoInflClass incl = null;
						foreach (IMoInflClass inclT in pos.InflectionClassesOC)
						{
							if (HasMatchingUnicodeAlternative(sInflClass.ToLowerInvariant(), inclT.Abbreviation, inclT.Name))
							{
								incl = inclT;
								break;
							}
						}
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
								fMatch = msaDeriv.FromInflectionClassRA == incl;
						}
						else
						{
							if (msaDeriv != null)
								fMatch = msaDeriv.ToInflectionClassRA == incl;
							else if (msaStep != null)
								fMatch = msaStep.InflectionClassRA == incl;
							else if (msaStem != null)
								fMatch = msaStem.InflectionClassRA == incl;
						}

					}
				}
			}
			return fMatch;
		}

		private void ProcessMsaSlotInformation(Dictionary<string, List<string>> dictPosSlots,
			IMoMorphSynAnalysis msa)
		{
			IMoInflAffMsa msaInfl = msa as IMoInflAffMsa;
			if (msaInfl == null)
				return;
			foreach (string sPos in dictPosSlots.Keys)
			{
				IPartOfSpeech pos = FindOrCreatePartOfSpeech(sPos);
				List<string> rgsSlot = dictPosSlots[sPos];
				if (rgsSlot.Count > 0 && pos != null)
				{
					foreach (string sSlot in rgsSlot)
					{
						IMoInflAffixSlot slot = null;
						foreach (IMoInflAffixSlot slotT in pos.AffixSlotsOC)
						{
							if (HasMatchingUnicodeAlternative(sSlot.ToLowerInvariant(), null, slotT.Name))
							{
								slot = slotT;
								break;
							}
						}
						if (slot == null)
						{
							slot = CreateNewMoInflAffixSlot();
							pos.AffixSlotsOC.Add(slot);
							slot.Name.set_String(m_cache.DefaultAnalWs, sSlot);
							m_rgnewSlots.Add(slot);
						}
						if (!msaInfl.SlotsRC.Contains(slot))
							msaInfl.SlotsRC.Add(slot);
					}
				}
			}
		}

		private bool MsaSlotInfoMatches(Dictionary<string, List<string>> dictPosSlots,
			IMoMorphSynAnalysis msa)
		{
			IMoInflAffMsa msaInfl = msa as IMoInflAffMsa;
			if (msaInfl == null)
				return true;
			foreach (string sPos in dictPosSlots.Keys)
			{
				IPartOfSpeech pos = FindOrCreatePartOfSpeech(sPos);
				List<string> rgsSlot = dictPosSlots[sPos];
				if (rgsSlot.Count > 0 && pos != null)
				{
					foreach (string sSlot in rgsSlot)
					{
						IMoInflAffixSlot slot = null;
						foreach (IMoInflAffixSlot slotT in pos.AffixSlotsOC)
						{
							if (HasMatchingUnicodeAlternative(sSlot.ToLowerInvariant(), null, slotT.Name))
							{
								slot = slotT;
								break;
							}
						}
						if (slot == null)
						{
							// Go ahead and create the new slot -- we'll need it shortly.
							slot = CreateNewMoInflAffixSlot();
							pos.AffixSlotsOC.Add(slot);
							slot.Name.set_String(m_cache.DefaultAnalWs, sSlot);
							m_rgnewSlots.Add(slot);
						}
						if (!msaInfl.SlotsRC.Contains(slot))
							return false;
					}
				}
			}
			return true;
		}

		private bool MsaExceptionFeatsMatch(List<ICmPossibility> rgpossProdRestrict,
			List<ICmPossibility> rgpossFromProdRestrict, IMoMorphSynAnalysis msa)
		{
			IMoStemMsa msaStem = msa as IMoStemMsa;
			if (msaStem != null)
			{
				if (msaStem.ProdRestrictRC.Count != rgpossProdRestrict.Count)
					return false;
				foreach (ICmPossibility poss in msaStem.ProdRestrictRC)
				{
					if (!rgpossProdRestrict.Contains(poss))
						return false;
				}
				return true;
			}
			IMoInflAffMsa msaInfl = msa as IMoInflAffMsa;
			if (msaInfl != null)
			{
				if (msaInfl.FromProdRestrictRC.Count != rgpossProdRestrict.Count)
					return false;
				foreach (ICmPossibility poss in msaInfl.FromProdRestrictRC)
				{
					if (!rgpossProdRestrict.Contains(poss))
						return false;
				}
				return true;
			}
			IMoDerivAffMsa msaDeriv = msa as IMoDerivAffMsa;
			if (msaDeriv != null)
			{
				if (msaDeriv.ToProdRestrictRC.Count != rgpossProdRestrict.Count)
					return false;
				if (rgpossFromProdRestrict == null && msaDeriv.FromProdRestrictRC.Count > 0)
					return false;
				if (msaDeriv.FromProdRestrictRC.Count != rgpossFromProdRestrict.Count)
					return false;
				foreach (ICmPossibility poss in msaDeriv.ToProdRestrictRC)
				{
					if (!rgpossProdRestrict.Contains(poss))
						return false;
				}
				if (rgpossFromProdRestrict != null)
				{
					foreach (ICmPossibility poss in msaDeriv.FromProdRestrictRC)
					{
						if (!rgpossFromProdRestrict.Contains(poss))
							return false;
					}
				}
				return true;
			}
			return true;
		}

		private bool MsaInflFeatureMatches(string sFeatureString, string sFromInflectionFeature,
			IMoMorphSynAnalysis msa)
		{
			IMoStemMsa msaStem = msa as IMoStemMsa;
			if (msaStem != null)
			{
				if (msaStem.MsFeaturesOA == null)
					return String.IsNullOrEmpty(sFeatureString);
				else if (String.IsNullOrEmpty(sFeatureString))
					return false;
				else
					return sFeatureString == msaStem.MsFeaturesOA.LiftName;
			}
			IMoInflAffMsa msaInfl = msa as IMoInflAffMsa;
			if (msaInfl != null)
			{
				if (msaInfl.InflFeatsOA == null)
					return String.IsNullOrEmpty(sFeatureString);
				else if (String.IsNullOrEmpty(sFeatureString))
					return false;
				else
					return sFeatureString == msaInfl.InflFeatsOA.LiftName;
			}
			IMoDerivAffMsa msaDeriv = msa as IMoDerivAffMsa;
			if (msaDeriv != null)
			{
				bool fOk;
				if (msaDeriv.ToMsFeaturesOA == null)
					fOk = String.IsNullOrEmpty(sFeatureString);
				else
					fOk = msaDeriv.ToMsFeaturesOA.LiftName == sFeatureString;
				if (fOk)
				{
					if (msaDeriv.FromMsFeaturesOA == null)
						fOk = String.IsNullOrEmpty(sFromInflectionFeature);
					else
						fOk = msaDeriv.FromMsFeaturesOA.LiftName == sFromInflectionFeature;
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
			if (String.IsNullOrEmpty(sFeatureString))
				return true;
			sFeatureString = sFeatureString.Trim();
			if (String.IsNullOrEmpty(sFeatureString))
				return true;
			IMoStemMsa msaStem = msa as IMoStemMsa;
			IMoInflAffMsa msaInfl = msa as IMoInflAffMsa;
			IMoDerivAffMsa msaDeriv = msa as IMoDerivAffMsa;
			if (msaStem == null && msaInfl == null && msaDeriv == null)
				return false;
			string sType = null;
			if (sFeatureString[0] == '{')
			{
				int idx = sFeatureString.IndexOf('}');
				if (idx < 0)
					return false;
				sType = sFeatureString.Substring(1, idx - 1);
				sType = sType.Trim();
				sFeatureString = sFeatureString.Substring(idx + 1);
				sFeatureString = sFeatureString.Trim();
			}
			if (sFeatureString[0] == '[' && sFeatureString.EndsWith("]"))
			{
				// Remove the outermost bracketing
				List<string> rgsName = new List<string>();
				List<string> rgsValue = new List<string>();
				if (SplitFeatureString(sFeatureString.Substring(1, sFeatureString.Length - 2), rgsName, rgsValue))
				{
					if (m_factFsFeatStruc == null)
						m_factFsFeatStruc = m_cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>();
					IFsFeatStruc feat = m_factFsFeatStruc.Create();
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
							msaDeriv.FromMsFeaturesOA = feat;
						else if (msaDeriv != null)
							msaDeriv.ToMsFeaturesOA = feat;
					}
					else
					{
						return false;
					}
					if (!String.IsNullOrEmpty(sType))
					{
						IFsFeatStrucType type = null;
						if (m_mapIdFeatStrucType.TryGetValue(sType, out type))
							feat.TypeRA = type;
						else
							return false;
					}
					return ProcessFeatStrucData(rgsName, rgsValue, feat);
				}
				else
				{
					return false;
				}
			}
			return false;
		}

		private IFsFeatStruc ParseFeatureString(string sFeatureString, IMoStemName stem)
		{
			if (String.IsNullOrEmpty(sFeatureString))
				return null;
			sFeatureString = sFeatureString.Trim();
			if (String.IsNullOrEmpty(sFeatureString))
				return null;
			if (stem == null)
				return null;
			IFsFeatStrucType type = null;
			if (sFeatureString[0] == '{')
			{
				int idx = sFeatureString.IndexOf('}');
				if (idx < 0)
					return null;
				string sType = sFeatureString.Substring(1, idx - 1);
				sType = sType.Trim();
				if (!String.IsNullOrEmpty(sType))
				{
					if (!m_mapIdFeatStrucType.TryGetValue(sType, out type))
						return null;
				}
				sFeatureString = sFeatureString.Substring(idx + 1);
				sFeatureString = sFeatureString.Trim();
			}
			if (sFeatureString[0] == '[' && sFeatureString.EndsWith("]"))
			{
				// Remove the outermost bracketing
				List<string> rgsName = new List<string>();
				List<string> rgsValue = new List<string>();
				if (SplitFeatureString(sFeatureString.Substring(1, sFeatureString.Length - 2), rgsName, rgsValue))
				{
					if (m_factFsFeatStruc == null)
						m_factFsFeatStruc = m_cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>();
					IFsFeatStruc ffs = null;
					ffs = m_factFsFeatStruc.Create();
					stem.RegionsOC.Add(ffs);
					if (type != null)
						ffs.TypeRA = type;
					if (ProcessFeatStrucData(rgsName, rgsValue, ffs))
					{
						int cffs = 0;
						string liftName = ffs.LiftName;
						foreach (IFsFeatStruc fs in stem.RegionsOC)
						{
							if (fs.LiftName == liftName)
								++cffs;
						}
						if (cffs > 1)
						{
							stem.RegionsOC.Remove(ffs);
							return null;
						}
						return ffs;
					}
					else
					{
						stem.RegionsOC.Remove(ffs);
						return null;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// recursively process the inner text of a feature structure.
		/// </summary>
		/// <returns>true if successful, false if a parse error occurs</returns>
		private bool ProcessFeatStrucData(List<string> rgsName, List<string> rgsValue,
			IFsFeatStruc ownerFeatStruc)
		{
			// TODO: figure out how (and when) to set ownerFeatStruc.TypeRA
			Debug.Assert(rgsName.Count == rgsValue.Count);
			for (int i = 0; i < rgsName.Count; ++i)
			{
				string sName = rgsName[i];
				IFsFeatDefn featDefn = null;
				if (!m_mapIdFeatDefn.TryGetValue(sName, out featDefn))
				{
					// REVIEW: SHOULD WE TRY TO CREATE ONE?
					return false;
				}
				string sValue = rgsValue[i];
				if (sValue[0] == '[')
				{
					if (!sValue.EndsWith("]"))
						return false;
					if (m_factFsComplexValue == null)
						m_factFsComplexValue = m_cache.ServiceLocator.GetInstance<IFsComplexValueFactory>();
					List<string> rgsValName = new List<string>();
					List<string> rgsValValue = new List<string>();
					if (SplitFeatureString(sValue.Substring(1, sValue.Length - 2), rgsValName, rgsValValue))
					{
						IFsComplexValue val = m_factFsComplexValue.Create();
						ownerFeatStruc.FeatureSpecsOC.Add(val);
						val.FeatureRA = featDefn;
						IFsFeatStruc featVal = m_factFsFeatStruc.Create();
						val.ValueOA = featVal;
						if (!ProcessFeatStrucData(rgsValName, rgsValValue, featVal))
							return false;
					}
					else
					{
						return false;
					}
				}
				else
				{
					if (m_factFsClosedValue == null)
						m_factFsClosedValue = m_cache.ServiceLocator.GetInstance<IFsClosedValueFactory>();
					IFsSymFeatVal featVal = null;
					string valueKey = String.Format("{0}:{1}", sName, sValue);
					if (m_mapIdAbbrSymFeatVal.TryGetValue(valueKey, out featVal))
					{
						IFsClosedValue val = m_factFsClosedValue.Create();
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
			while (!String.IsNullOrEmpty(sFeat))
			{
				int idxVal = sFeat.IndexOf(':');
				if (idxVal > 0)
				{
					string sFeatName = sFeat.Substring(0, idxVal).Trim();
					string sFeatVal = sFeat.Substring(idxVal + 1).Trim();
					if (sFeatName.Length == 0 || sFeatVal.Length == 0)
						return false;
					rgsName.Add(sFeatName);
					int idxSep = -1;
					if (sFeatVal[0] == '[')
					{
						idxSep = FindMatchingCloseBracket(sFeatVal);
						if (idxSep < 0)
							return false;
						++idxSep;
						if (idxSep >= sFeatVal.Length)
							idxSep = -1;
						else if (sFeatVal[idxSep] != ' ')
							return false;
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
				return -1;
			char[] rgBrackets = new char[] { '[', ']' };
			int cOpen = 1;
			int idxBracket = 0;
			while (cOpen > 0)
			{
				idxBracket = sFeatVal.IndexOfAny(rgBrackets, idxBracket + 1);
				if (idxBracket < 0)
					return idxBracket;
				if (sFeatVal[idxBracket] == '[')
					++cOpen;
				else
					--cOpen;
			}
			return idxBracket;
		}

		private bool MsaStemNameMatches(string sFromStemName, IMoDerivAffMsa msaAffix)
		{
			if (String.IsNullOrEmpty(sFromStemName) && msaAffix.FromStemNameRA == null)
				return true;
			IMoStemName msn = msaAffix.FromStemNameRA;
			int ws;
			for (int i = 0; i < msn.Name.StringCount; ++i)
			{
				ITsString tss = msn.Name.GetStringFromIndex(i, out ws);
				if (tss.Text == sFromStemName)
					return true;
			}
			return false;
		}

		private bool SenseGramInfoConflicts(ILexSense ls, LiftGrammaticalInfo gram)
		{
			if (ls.MorphoSyntaxAnalysisRA == null || gram == null)
				return false;
			string sPOS = gram.Value;
			IPartOfSpeech pos = null;
			string sType = null;
			string sFromPOS = null;
			Dictionary<string, List<string>> dictPosSlots = new Dictionary<string, List<string>>();
			Dictionary<string, List<string>> dictPosInflClasses = new Dictionary<string, List<string>>();
			foreach (LiftTrait trait in gram.Traits)
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
					int len = trait.Name.Length - (trait.Name.EndsWith("-slot") ? 5 : 6);
					string sTraitPos = trait.Name.Substring(0, len);
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
					int len = trait.Name.Length - (trait.Name.EndsWith("-infl-class") ? 11 : 16);
					string sTraitPos = trait.Name.Substring(0, len);
					List<string> rgsInflClasses;
					if (!dictPosInflClasses.TryGetValue(sTraitPos, out rgsInflClasses))
					{
						rgsInflClasses = new List<string>();
						dictPosInflClasses.Add(sTraitPos, rgsInflClasses);
					}
					rgsInflClasses.Add(trait.Value);
				}
			}
			if (!String.IsNullOrEmpty(sPOS))
				pos = FindOrCreatePartOfSpeech(sPOS);
			IMoMorphSynAnalysis msa = ls.MorphoSyntaxAnalysisRA;
			int hvoPosOld = 0;
			switch (sType)
			{
				case "inflAffix":
					IMoInflAffMsa msaInfl = msa as IMoInflAffMsa;
					if (msaInfl == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls, this);
						return true;
					}
					hvoPosOld = msaInfl.PartOfSpeechRA == null ? 0 : msaInfl.PartOfSpeechRA.Hvo;
					break;
				case "derivAffix":
					IMoDerivAffMsa msaDerv = msa as IMoDerivAffMsa;
					if (msaDerv == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls, this);
						return true;
					}
					hvoPosOld = msaDerv.ToPartOfSpeechRA == null ? 0 : msaDerv.ToPartOfSpeechRA.Hvo;
					if (!String.IsNullOrEmpty(sFromPOS))
					{
						IPartOfSpeech posNewFrom = FindOrCreatePartOfSpeech(sFromPOS);
						int hvoOldFrom = msaDerv.FromPartOfSpeechRA == null ? 0 : msaDerv.FromPartOfSpeechRA.Hvo;
						if (posNewFrom != null && hvoOldFrom != 0 && posNewFrom.Hvo != hvoOldFrom)
							return true;
					}
					break;
				case "derivStepAffix":
					IMoDerivStepMsa msaStep = msa as IMoDerivStepMsa;
					if (msaStep == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls, this);
						return true;
					}
					hvoPosOld = msaStep.PartOfSpeechRA == null ? 0 : msaStep.PartOfSpeechRA.Hvo;
					break;
				case "affix":
					IMoUnclassifiedAffixMsa msaUncl = msa as IMoUnclassifiedAffixMsa;
					if (msaUncl == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls, this);
						return true;
					}
					hvoPosOld = msaUncl.PartOfSpeechRA == null ? 0 : msaUncl.PartOfSpeechRA.Hvo;
					break;
				default:
					IMoStemMsa msaStem = msa as IMoStemMsa;
					if (msaStem == null)
					{
						m_cdConflict = new ConflictingSense("Grammatical Info. Type", ls, this);
						return true;
					}
					hvoPosOld = msaStem.PartOfSpeechRA == null ? 0 : msaStem.PartOfSpeechRA.Hvo;
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

		private bool MsaSlotInformationConflicts(Dictionary<string, List<string>> dictPosSlots,
			IMoMorphSynAnalysis msa)
		{
			// how do we determine conflicts in a list?
			return false;
		}

		private bool MsaInflectionClassInfoConflicts(Dictionary<string, List<string>> dictPosInflClasses,
			IMoMorphSynAnalysis msa)
		{
			// How do we determine conflicts in a list?
			return false;
		}

		private void CreateSenseIllustrations(ILexSense ls, CmLiftSense sense)
		{
			AddNewWsToBothVernAnal();
			foreach (LiftUrlRef uref in sense.Illustrations)
			{
				int ws;
				if (uref.Label != null && !uref.Label.IsEmpty)
					ws = GetWsFromLiftLang(uref.Label.FirstValue.Key);
				else
					ws = m_cache.DefaultVernWs;
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
					ssFile = sFile.Substring(9);
			}
			string sPath = Path.Combine(Path.GetDirectoryName(m_sLiftFile),
				String.Format("pictures{0}{1}", Path.DirectorySeparatorChar, ssFile));
			sPath = CopyFileToLinkedFiles(ssFile, sPath, DirectoryFinder.ksPicturesDir);
			if (!File.Exists(sPath) && !String.IsNullOrEmpty(m_cache.LangProject.LinkedFilesRootDir))
			{
				sPath = Path.Combine(m_cache.LangProject.LinkedFilesRootDir, sFile);
				if (!File.Exists(sPath) && !String.IsNullOrEmpty(m_cache.LangProject.LinkedFilesRootDir))
				{
					sPath = Path.Combine(m_cache.LangProject.LinkedFilesRootDir,
						String.Format("Pictures{0}{1}", Path.DirectorySeparatorChar, sFile));
					if (!File.Exists(sPath))
					{
						sPath = Path.Combine(DirectoryFinder.FWDataDirectory, sFile);
						if (!File.Exists(sPath))
							sPath = Path.Combine(DirectoryFinder.FWDataDirectory,
								String.Format("Pictures{0}{1}", Path.DirectorySeparatorChar, sFile));
					}
				}
			}
			return sPath;
		}

		/// <summary>
		/// Create a picture, adding it to the lex sense.  The filename is used to guess a full path,
		/// and the label from uref is used to set the caption.
		/// </summary>
		/// <param name="ls"></param>
		/// <param name="uref"></param>
		/// <param name="sFile"></param>
		/// <param name="ws"></param>
		private void CreatePicture(ILexSense ls, LiftUrlRef uref, string sFile, int ws)
		{
			string sPath = CopyPicture(sFile);
			string sFolder = CmFolderTags.LocalPictures;

			ICmPicture pict = CreateNewCmPicture();
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
				pict.PictureFileRA.InternalPath =
					String.Format("Pictures{0}{1}", Path.DirectorySeparatorChar, sFile);
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
			if (File.Exists(sPath) && !String.IsNullOrEmpty(m_cache.LangProject.LinkedFilesRootDir))
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
			Dictionary<LiftUrlRef, ICmPicture> map = new Dictionary<LiftUrlRef, ICmPicture>();
			Set<int> setUsed = new Set<int>();
			AddNewWsToBothVernAnal();
			foreach (LiftUrlRef uref in sense.Illustrations)
			{
				// TODO-Linux: this looks suspicious
				string sFile = uref.Url.Replace('/', '\\');
				ICmPicture pict = FindPicture(ls.PicturesOS, sFile, uref.Label);
				map.Add(uref, pict);
				if (pict != null)
					setUsed.Add(pict.Hvo);
			}
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				foreach (int hvo in ls.PicturesOS.ToHvoArray())
				{
					if (!setUsed.Contains(hvo))
						m_deletedObjects.Add(hvo);
				}
			}
			foreach (LiftUrlRef uref in sense.Illustrations)
			{
				ICmPicture pict;
				map.TryGetValue(uref, out pict);
				if (pict == null)
				{
					int ws;
					if (uref.Label != null && !uref.Label.IsEmpty)
						ws = GetWsFromLiftLang(uref.Label.FirstValue.Key);
					else
						ws = m_cache.DefaultVernWs;
					CreatePicture(ls, uref, uref.Url, ws);
				}
				else
				{
					AddNewWsToAnalysis();
					MergeInMultiString(pict.Caption, CmPictureTags.kflidCaption, uref.Label, pict.Guid);
					// When doing Send/Receive we should copy existing pictures in case they changed.
					if (m_msImport == MergeStyle.MsKeepOnlyNew)
						CopyPicture(uref.Url);
				}
			}
		}

		private ICmPicture FindPicture(IFdoOwningSequence<ICmPicture> rgpictures, string sFile,
			LiftMultiText lmtLabel)
		{
			ICmPicture pictMatching = null;
			int cMatches = 0;
			AddNewWsToBothVernAnal();
			if (sFile == null)
				return pictMatching;
			// sFile may or may not have pictures as first part of path. May also have nested subdirectories.
			// By default Lift uses "pictures" in path and Flex uses "Pictures" in path.
			if (sFile.Length > 9)
			{
				var tpath = sFile.Substring(0, 9).ToLowerInvariant();
				if (tpath == "pictures\\" || tpath == "pictures/")
					sFile = sFile.Substring(9);
			}
			foreach (ICmPicture pict in rgpictures)
			{
				if (pict.PictureFileRA == null)
					continue;	// should NEVER happen!
				var fpath = pict.PictureFileRA.InternalPath;
				if (fpath == null)
					continue;
				if (fpath.Length > 9)
				{
					var tpath = fpath.Substring(0, 9).ToLowerInvariant();
					if (tpath == "pictures\\" || tpath == "pictures/")
						fpath = fpath.Substring(9);
				}
				if (sFile == fpath)
				{
					int cCurrent = MultiTsStringMatches(pict.Caption, lmtLabel);
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
				return false;
			AddNewWsToBothVernAnal();
			foreach (LiftUrlRef uref in list)
			{
				string sFile = FileUtils.StripFilePrefix(uref.Url);
				ICmPicture pict = FindPicture(ls.PicturesOS, sFile, uref.Label);
				if (pict == null)
					continue;
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
			foreach (CmLiftRelation rel in sense.Relations)
			{
				if (String.IsNullOrEmpty(rel.Ref) && rel.Type != "_component-lexeme")
				{
					XmlDocument xdResidue = FindOrCreateResidue(ls);
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
							CmLiftEntry entry = sense.OwningEntry;
							PendingLexEntryRef pend = new PendingLexEntryRef(ls, rel, entry);
							pend.Residue = CreateRelationResidue(rel);
							m_rgPendingLexEntryRefs.Add(pend);
							break;
						default:
							string sResidue = CreateRelationResidue(rel);
							m_rgPendingRelation.Add(new PendingRelation(ls, rel, sResidue));
							break;
					}
				}
			}
		}

		private bool SenseRelationsConflict(ILexSense ls, List<CmLiftRelation> list)
		{
			// TODO: how do we detect conflicts in a list?
			return false;
		}

		private void ProcessSenseReversals(ILexSense ls, CmLiftSense sense)
		{
			foreach (CmLiftReversal rev in sense.Reversals)
			{
				IReversalIndexEntry rie = ProcessReversal(rev);
				if (rie != null && !ls.ReversalEntriesRC.Contains(rie))
					ls.ReversalEntriesRC.Add(rie);
			}
		}

		private IReversalIndexEntry ProcessReversal(CmLiftReversal rev)
		{
			AddNewWsToAnalysis();
			Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs;
			IReversalIndexEntry rie = null;
			if (rev.Main == null)
			{
				IReversalIndex riOwning = FindOrCreateReversalIndex(rev.Form, rev.Type);
				if (riOwning == null)
					return null;
				if (!m_mapToMapToRie.TryGetValue(riOwning, out mapToRIEs))
				{
					mapToRIEs = new Dictionary<MuElement, List<IReversalIndexEntry>>();
					m_mapToMapToRie.Add(riOwning, mapToRIEs);
				}
				rie = FindOrCreateMatchingReversal(rev.Form, mapToRIEs, riOwning.EntriesOC);
			}
			else
			{
				IReversalIndexEntry rieOwner = ProcessReversal(rev.Main);	// recurse!
				if (!m_mapToMapToRie.TryGetValue(rieOwner, out mapToRIEs))
				{
					mapToRIEs = new Dictionary<MuElement, List<IReversalIndexEntry>>();
					m_mapToMapToRie.Add(rieOwner, mapToRIEs);
				}
				rie = FindOrCreateMatchingReversal(rev.Form, mapToRIEs, rieOwner.SubentriesOC);
			}
			MergeInMultiUnicode(rie.ReversalForm, ReversalIndexEntryTags.kflidReversalForm, rev.Form, rie.Guid);
			ProcessReversalGramInfo(rie, rev.GramInfo);
			return rie;
		}

		private IReversalIndexEntry FindOrCreateMatchingReversal(LiftMultiText form,
			Dictionary<MuElement, List<IReversalIndexEntry>> mapToRIEs,
			IFdoOwningCollection<IReversalIndexEntry> entriesOC)
		{
			IReversalIndexEntry rie = null;
			List<IReversalIndexEntry> rgrie;
			List<MuElement> rgmue = new List<MuElement>();
			AddNewWsToAnalysis();
			foreach (string key in form.Keys)
			{
				int ws = GetWsFromLiftLang(key);
				string sNew = form[key].Text;
				string sNewNorm;
				if (String.IsNullOrEmpty(sNew))
					sNewNorm = sNew;
				else
					sNewNorm = Icu.Normalize(sNew, Icu.UNormalizationMode.UNORM_NFD);
				MuElement mue = new MuElement(ws, sNewNorm);
				if (rie == null && mapToRIEs.TryGetValue(mue, out rgrie))
				{
					foreach (IReversalIndexEntry rieT in rgrie)
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
			if (rie == null)
			{
				rie = CreateNewReversalIndexEntry();
				entriesOC.Add(rie);
			}
			foreach (MuElement mue in rgmue)
				AddToReversalMap(mue, rie, mapToRIEs);
			return rie;
		}

		private IReversalIndex FindOrCreateReversalIndex(LiftMultiText contents, string type)
		{
			IReversalIndex riOwning = null;
			// For now, fudge "type" as the basic writing system associated with the reversal.
			string sWs = type;
			if (String.IsNullOrEmpty(sWs))
			{
				if (contents == null || contents.Keys.Count == 0)
					return null;
				if (contents.Keys.Count == 1)
					sWs = contents.FirstValue.Key;
				else
					sWs = contents.FirstValue.Key.Split(new char[] { '_', '-' })[0];
			}
			AddNewWsToAnalysis();
			int ws = GetWsFromStr(sWs);
			if (ws == 0)
			{
				ws = GetWsFromLiftLang(sWs);
				if (GetWsFromStr(sWs) == 0)
					sWs = GetExistingWritingSystem(ws).Id;	// Must be old-style ICU Locale.
			}
			// A linear search should be safe here, because we don't expect more than 2 or 3
			// reversal indexes in any given project.
			foreach (IReversalIndex ri in m_cache.LangProject.LexDbOA.ReversalIndexesOC)
			{
				if (ri.WritingSystem == sWs)
				{
					riOwning = ri;
					break;
				}
			}
			if (riOwning == null)
			{
				riOwning = CreateNewReversalIndex();
				m_cache.LangProject.LexDbOA.ReversalIndexesOC.Add(riOwning);
				riOwning.WritingSystem = GetExistingWritingSystem(ws).Id;
			}
			return riOwning;
		}

		private void ProcessReversalGramInfo(IReversalIndexEntry rie, LiftGrammaticalInfo gram)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
				rie.PartOfSpeechRA = null;
			if (gram == null || String.IsNullOrEmpty(gram.Value))
				return;
			string sPOS = gram.Value;
			IReversalIndex ri = rie.ReversalIndex;
			Dictionary<string, ICmPossibility> dict = null;
			int handle = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(ri.WritingSystem);
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
						rie.PartOfSpeechRA = dict[sPOS] as IPartOfSpeech;
				}
				else
				{
					rie.PartOfSpeechRA = dict[sPOS] as IPartOfSpeech;
				}
			}
			else
			{
				IPartOfSpeech pos = CreateNewPartOfSpeech();
				if (ri.PartsOfSpeechOA == null)
				{
					ICmPossibilityListFactory fact = m_cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();
					ri.PartsOfSpeechOA = fact.Create();
				}
				ri.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
				// Use the name and abbreviation from a regular PartOfSpeech if available, otherwise
				// just use the key and hope the user can sort it out later.
				if (m_dictPos.ContainsKey(sPOS))
				{
					IPartOfSpeech posMain = m_dictPos[sPOS] as IPartOfSpeech;
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
						rie.PartOfSpeechRA = pos;
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
			foreach (CmLiftNote note in sense.Notes)
			{
				if (note.Type == null)
					note.Type = String.Empty;
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
			foreach (CmLiftNote note in list)
			{
				if (note.Type == null)
					note.Type = String.Empty;
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
			foreach (LiftField field in sense.Fields)
			{
				string sType = field.Type;
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
						ProcessUnknownField(ls, sense, field,
							"LexSense", "custom-sense-", LexSenseTags.kClassId);
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
		/// <param name="co"></param>
		/// <param name="obj"></param>
		/// <param name="field"></param>
		/// <param name="sClass"></param>
		/// <param name="sOldPrefix"></param>
		/// <param name="clid"></param>
		private void ProcessUnknownField(ICmObject co, LiftObject obj, LiftField field,
			string sClass, string sOldPrefix, int clid)
		{
			string sType = field.Type;
			if (sType.StartsWith(sOldPrefix))
				sType = sType.Substring(sOldPrefix.Length);
			Debug.Assert(sType.Length > 0);
			string sTag = String.Format("{0}-{1}", sClass, sType);
			int flid;
			if (m_dictCustomFlid.TryGetValue(sTag, out flid))
			{
				ProcessCustomFieldData(co.Hvo, flid, field.Content);
			}
			else
			{
				LiftMultiText desc = null;
				if (!m_dictFieldDef.TryGetValue(m_cache.DomainDataByFlid.MetaDataCache.GetClassId(co.ClassName) + sType, out desc))
				{
					m_dictFieldDef.TryGetValue(sOldPrefix + sType, out desc);
				}
				Guid possListGuid;
				flid = FindOrCreateCustomField(sType, desc, clid, out possListGuid);
				if (flid == 0)
				{
					if (clid == LexSenseTags.kClassId || clid == LexExampleSentenceTags.kClassId)
						FindOrCreateResidue(co, obj.Id, LexSenseTags.kflidLiftResidue);
					else
						FindOrCreateResidue(co, obj.Id, LexEntryTags.kflidLiftResidue);
					StoreFieldAsResidue(co, field);
				}
				else
				{
					ProcessCustomFieldData(co.Hvo, flid, field.Content);
				}
			}
		}

		private ITsString StoreTsStringValue(bool fCreatingNew, ITsString tssOld, LiftMultiText lmt)
		{
			// If we're keeping only the imported data, erase any existing data first.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
				tssOld = m_tssEmpty;
			IgnoreNewWs();
			ITsString tss = GetFirstLiftTsString(lmt);
			if (TsStringIsNullOrEmpty(tss))
				return tssOld;
			if (m_msImport == MergeStyle.MsKeepOld && !fCreatingNew)
			{
				if (TsStringIsNullOrEmpty(tssOld))
					tssOld= tss;
			}
			else
			{
				tssOld = tss;
			}
			return tssOld;
		}

		private bool SenseFieldsConflict(ILexSense ls, List<LiftField> list)
		{
			foreach (LiftField field in list)
			{
				IgnoreNewWs();
				string sType = field.Type;
				switch (sType.ToLowerInvariant())
				{
					case "importresidue":
					case "import-residue":
						if (ls.ImportResidue != null && ls.ImportResidue.Length != 0)
						{
							ITsStrBldr tsb = ls.ImportResidue.GetBldr();
							int idx = tsb.Text.IndexOf("<lift-residue id=");
							if (idx >= 0)
								tsb.Replace(idx, tsb.Length, null, null);
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
								m_cdConflict = new ConflictingSense(String.Format("{0} (custom field)", sType), ls, this);
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
			}
			ICmPossibility poss;
			foreach (LiftTrait lt in sense.Traits)
			{
				switch (lt.Name.ToLowerInvariant())
				{
					case RangeNames.sAnthroListOA:
						ICmAnthroItem ant = FindOrCreateAnthroCode(lt.Value);
						if (!ls.AnthroCodesRC.Contains(ant))
							ls.AnthroCodesRC.Add(ant);
						break;
					case RangeNames.sSemanticDomainListOAold1: // for WeSay 0.4 compatibility
					case RangeNames.sSemanticDomainListOAold2:
					case RangeNames.sSemanticDomainListOAold3:
					case RangeNames.sSemanticDomainListOA:
						ICmSemanticDomain sem = FindOrCreateSemanticDomain(lt.Value);
						if (!ls.SemanticDomainsRC.Contains(sem))
							ls.SemanticDomainsRC.Add(sem);
						break;
					case RangeNames.sDbDomainTypesOAold1: // original FLEX export = DomainType
					case RangeNames.sDbDomainTypesOA:
						poss = FindOrCreateDomainType(lt.Value);
						if (!ls.DomainTypesRC.Contains(poss))
							ls.DomainTypesRC.Add(poss);
						break;
					case RangeNames.sDbSenseTypesOAold1: // original FLEX export = SenseType
					case RangeNames.sDbSenseTypesOA:
						poss = FindOrCreateSenseType(lt.Value);
						if (ls.SenseTypeRA != poss && (m_fCreatingNewSense || m_msImport != MergeStyle.MsKeepOld || ls.SenseTypeRA == null))
							ls.SenseTypeRA = poss;
						break;
					case RangeNames.sStatusOA:
						poss = FindOrCreateStatus(lt.Value);
						if (ls.StatusRA != poss && (m_fCreatingNewSense || m_msImport != MergeStyle.MsKeepOld || ls.StatusRA == null))
							ls.StatusRA = poss;
						break;
					case RangeNames.sDbUsageTypesOAold: // original FLEX export = UsageType
					case RangeNames.sDbUsageTypesOA:
						poss = FindOrCreateUsageType(lt.Value);
						if (!ls.UsageTypesRC.Contains(poss))
							ls.UsageTypesRC.Add(poss);
						break;
					default:
						ProcessUnknownTrait(sense, lt, ls);
						break;
				}
			}
		}

		private bool SenseTraitsConflict(ILexSense ls, List<LiftTrait> list)
		{
			ICmPossibility poss;
			foreach (LiftTrait lt in list)
			{
				switch (lt.Name.ToLowerInvariant())
				{
					case RangeNames.sAnthroListOA:
						ICmAnthroItem ant = FindOrCreateAnthroCode(lt.Value);
						// how do you detect conflicts in a reference list?
						break;
					case RangeNames.sSemanticDomainListOAold1:	// for WeSay 0.4 compatibility
					case RangeNames.sSemanticDomainListOAold2:
					case RangeNames.sSemanticDomainListOAold3:
					case RangeNames.sSemanticDomainListOA:
						poss = FindOrCreateSemanticDomain(lt.Value);
						// how do you detect conflicts in a reference list?
						break;
					case RangeNames.sDbDomainTypesOAold1:	// original FLEX export = DomainType
					case RangeNames.sDbDomainTypesOA:
						poss = FindOrCreateDomainType(lt.Value);
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
						poss = FindOrCreateUsageType(lt.Value);
						// how do you detect conflicts in a reference list?
						break;
				}
			}
			return false;
		}
		#endregion // Methods for storing entry data

		private void EnsureValidMSAsForSenses(ILexEntry le)
		{
			bool fIsAffix = IsAffixType(le);
			foreach (ILexSense ls in GetAllSenses(le))
			{
				if (ls.MorphoSyntaxAnalysisRA != null)
					continue;
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
			List<ILexSense> rgls = new List<ILexSense>();
			foreach (ILexSense ls in le.SensesOS)
			{
				rgls.Add(ls);
				GetAllSubsenses(ls, rgls);
			}
			return rgls;
		}

		private void GetAllSubsenses(ILexSense ls, List<ILexSense> rgls)
		{
			foreach (ILexSense lsSub in ls.SensesOS)
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
			IMoForm lfForm = le.LexemeFormOA;
			int cTypes = 0;
			if (lfForm != null)
			{
				IMoMorphType mmt = lfForm.MorphTypeRA;
				if (mmt != null)
				{
					if (mmt.IsStemType)
						return false;
					++cTypes;
				}
			}
			foreach (IMoForm form in le.AlternateFormsOS)
			{
				IMoMorphType mmt = form.MorphTypeRA;
				if (mmt != null)
				{
					if (mmt.IsStemType)
						return false;
					++cTypes;
				}
			}
			return cTypes > 0;		// assume stem if no type information.
		}

		private IMoMorphSynAnalysis FindEmptyAffixMsa(ILexEntry le)
		{
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoUnclassifiedAffixMsa msaAffix = msa as IMoUnclassifiedAffixMsa;
				if (msaAffix != null && msaAffix.PartOfSpeechRA == null)
					return msa;
			}
			return null;
		}

		private IMoMorphSynAnalysis FindEmptyStemMsa(ILexEntry le)
		{
			foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
			{
				IMoStemMsa msaStem = msa as IMoStemMsa;
				if (msaStem != null &&
					msaStem.PartOfSpeechRA == null &&
					msaStem.FromPartsOfSpeechRC.Count == 0 &&
					msaStem.InflectionClassRA == null &&
					msaStem.ProdRestrictRC.Count == 0 &&
					msaStem.StratumRA == null &&
					msaStem.MsFeaturesOA == null)
				{
					return msaStem;
				}
			}
			return null;
		}

		// Given an initial private use tag, if it ends with a part that follows the pattern duplN,
		// return one made by incrementing N.
		// Otherwise, return one made by appending dupl1.
		internal static string GetNextDuplPart(string privateUse)
		{
			if (string.IsNullOrEmpty(privateUse))
				return "dupl1";
			var lastPart = privateUse.Split('-').Last();
			if (Regex.IsMatch(lastPart, "dupl[0-9]+", RegexOptions.IgnoreCase))
			{
				// Replace the old lastPart with the result of incrementing the number
				int val = int.Parse(lastPart.Substring("dupl".Length));
				return privateUse.Substring(0, privateUse.Length - lastPart.Length) + ("dupl" + (val + 1));
			}
			// Append dupl1. We know privateUse is not empty.
			return privateUse + "-dupl1";
		}


	}
}
