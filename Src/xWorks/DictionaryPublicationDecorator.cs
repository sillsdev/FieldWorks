// Copyright (c) 2012-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// DictionaryPublicationDecorator supports limiting lexical data in the Dictionary view by omitting
	/// items that should not be published in the selected publication. It builds a list of objects that
	/// should be omitted from the current publication, which currently can be LexEntries, LexSenses,
	/// or LexExampleSentences. It intercepts requests from the view for these properties and gives suitably
	/// modified answers. It implements IVwPropChanged and clears its cache when relevant properties change,
	/// though currently we do not try to generate automatic propchanges on additional affected properties.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Cache is a reference")]
	public class DictionaryPublicationDecorator : DomainDataByFlidDecoratorBase
	{
		// a set of HVOs of entries, senses, and examples that should not be displayed in the publication.
		readonly HashSet<int> m_excludedItems = new HashSet<int>();
		readonly HashSet<int> m_excludeAsMainEntry = new HashSet<int>();

		private FdoCache Cache { get; set; }

		private int m_LexDbEntriesFlid; // similar to m_mainFlid in the XmlVc it decorates

		private ILexEntryRepository m_entryRepo;
		private ILexReferenceRepository m_lexRefRepo;
		private ILexSenseRepository m_senseRepo;
		private ILexEntryRefRepository m_lerRepo;

		private int m_headwordFlid;
		private int m_mlHeadwordFlid;
		private int m_picsOfSensesFlid;
		private int m_senseOutlineFlid;
		private int m_mlOwnerOutlineFlid;
		private int m_publishAsMinorEntryFlid;
		private int m_headwordRefFlid;
		private int m_headwordReversalFlid;
		private int m_reversalNameFlid;

		// Map from HVO (of LexEntry) to homograph number we should publish.
		private Dictionary<int, int> m_homographNumbers = new Dictionary<int, int>();

		// a set of flids for properties that return LexEntries, LexSenses, or LexExampleSentences
		HashSet<int> m_fieldsToFilter = new HashSet<int>();

		HashSet<int> m_lexRefFieldsToFilter = new HashSet<int>();
		HashSet<int> m_lexEntryRefFieldsToFilter = new HashSet<int>();

		private List<IVwNotifyChange> m_notifees = new List<IVwNotifyChange>(); // the things we have to notify of PropChanges.

		/// <summary>
		/// Make one. By default we filter to the main dictionary.
		/// </summary>
		public DictionaryPublicationDecorator(FdoCache cache, ISilDataAccessManaged domainDataByFlid, int mainFlid)
			: this(cache, domainDataByFlid, mainFlid, cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS[0])
		{}

		/// <summary>
		/// Create one. The SDA passed MAY be the DomainDataByFlid of the cache, but it is usually another
		/// decorator.
		/// </summary>
		public DictionaryPublicationDecorator(FdoCache cache, ISilDataAccessManaged domainDataByFlid, int mainFlid, ICmPossibility publication)
			: base(domainDataByFlid)
		{
			Cache = cache;
			m_entryRepo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			m_lexRefRepo = Cache.ServiceLocator.GetInstance<ILexReferenceRepository>();
			m_senseRepo = Cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			m_lerRepo = Cache.ServiceLocator.GetInstance<ILexEntryRefRepository>();
			m_LexDbEntriesFlid = mainFlid;
			m_headwordFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "HeadWord", false);
			m_mlHeadwordFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "MLHeadWord", false);
			m_picsOfSensesFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "PicturesOfSenses", false);
			m_senseOutlineFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexSenseTags.kClassId, "LexSenseOutline", false);
			m_mlOwnerOutlineFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexSenseTags.kClassId, "MLOwnerOutlineName", false);
			m_publishAsMinorEntryFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "PublishAsMinorEntry", false);
			m_headwordRefFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "HeadWordRef", false);
			m_headwordReversalFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "HeadWordReversal", false);
			m_reversalNameFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexSenseTags.kClassId, "ReversalName", false);
			Publication = publication;
			BuildExcludedObjects();
			BuildFieldsToFilter();
			BuildHomographInfo();
		}

		/// <summary>
		/// We want to intercept notifications. So instead of registering the root box with the wrapped
		/// SDA, we register ourself.
		/// </summary>
		/// <param name="nchng"></param>
		public override void AddNotification(IVwNotifyChange nchng)
		{
			base.AddNotification(this);
			m_notifees.Add(nchng);
		}

		public override void RemoveNotification(IVwNotifyChange nchng)
		{
			m_notifees.Remove(nchng);
			base.RemoveNotification(this);
		}

		/// <summary>
		/// When we get a PropChanged, it is really meant for our m_notifiee. However, it may have invalid
		/// arguments for ivMin etc, because the item(s) inserted or deleted in the real property may be
		/// in a different (or no) place in the filtered property. Rather than trying to figure out what
		/// really changed, we issue a PropChanged which is interpreted as changing the whole property.
		/// </summary>
		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
			if (m_fieldsToFilter.Contains(tag) || m_lexRefFieldsToFilter.Contains(tag) || m_lexEntryRefFieldsToFilter.Contains(tag))
			{
				foreach (var notifee in m_notifees)
					notifee.PropChanged(hvo, tag, 0, get_VecSize(hvo, tag), 0);
			}
			else
			{
				foreach (var notifee in m_notifees)
					notifee.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
			}
		}

		private void BuildHomographInfo()
		{
			m_homographNumbers.Clear();
			var homographs = new Dictionary<string, SortedList<int, ILexEntry>>();
			var repo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			foreach (var entry in Cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances())
			{
				if (m_excludedItems.Contains(entry.Hvo))
					continue;
				var key = entry.HomographForm + repo.HomographMorphOrder(Cache, entry.PrimaryMorphType);
				SortedList<int, ILexEntry> collection;
				if (!homographs.TryGetValue(key, out collection))
				{
					collection = new SortedList<int, ILexEntry>();
					homographs[key] = collection;
				}
				if (collection.ContainsKey(entry.HomographNumber))
				{
					// Bother! duplicate. Force it in somehow: move all the entries with bigger numbers up.
					foreach (var fixKey in collection.Keys.Reverse().ToArray())
					{
						var val = collection[fixKey];
						if (val.HomographNumber <= entry.HomographNumber)
						{
							// we can insert the new entry just after val, since any entries with higher keys have been
							// moved up one.
							collection[fixKey + 1] = entry;
							break;
						}
						collection[fixKey + 1] = val;
					}
				}
				else
				{
					collection.Add(entry.HomographNumber, entry);
				}
			}
			foreach (var list in homographs.Values)
			{
				// If at most one item is shown as a headword, all items should have homograph number zero
				// (including the one and only one shown, if any).
				if ((from item in list where !m_excludeAsMainEntry.Contains(item.Value.Hvo) select item).Count() <= 1)
				{
					foreach(var item in list)
						m_homographNumbers[item.Value.Hvo] = 0;
					continue;
				}
				// otherwise number them sequentially, starting at 1, but any that are not headwords are numbered zero
				// and do not count.
				int index = 1;
				foreach (var item in list.Values)
					m_homographNumbers[item.Hvo] = (m_excludeAsMainEntry.Contains(item.Hvo) ? 0 : index++);
			}
		}

		public override bool get_BooleanProp(int hvo, int tag)
		{
			if (tag == m_publishAsMinorEntryFlid)
			{
				return VecProp(hvo, LexEntryTags.kflidEntryRefs).Select(hvoLer => m_lerRepo.GetObject(hvoLer)).Any(ler => ler.HideMinorEntry == 0);
			}
			return base.get_BooleanProp(hvo, tag);
		}

		public override int get_IntProp(int hvo, int tag)
		{
			if (tag == LexEntryTags.kflidHomographNumber)
			{
				int result;
				if (m_homographNumbers.TryGetValue(hvo, out result))
					return result;
				// In case it's one we somehow don't know about, we'll let the base method try to get the real HN.
			}
			return base.get_IntProp(hvo, tag);
		}

		public override ITsString get_StringProp(int hvo, int tag)
		{
			if (tag == m_headwordFlid)
			{
				int hn;
				if (m_homographNumbers.TryGetValue(hvo, out hn))
				{
					var entry = m_entryRepo.GetObject(hvo);
					return StringServices.HeadWordForWsAndHn(entry, Cache.DefaultVernWs, hn);
				}
				// In case it's one we somehow don't know about, we'll let the base method try to get the real HN.
			}
			else if (tag == m_senseOutlineFlid)
			{
				var sense = m_senseRepo.GetObject(hvo);
				return GetSenseNumberTss(sense);
			}
			return base.get_StringProp(hvo, tag);
		}

		private ITsString GetSenseNumberTss(ILexSense sense)
		{
			return Cache.TsStrFactory.MakeString(GetSenseNumber(sense),
				Cache.DefaultUserWs);
		}

		private string GetSenseNumber(ILexSense sense)
		{
			return Cache.GetOutlineNumber(sense, LexSenseTags.kflidSenses, false, true, this);
		}

		public override ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			if (tag == m_mlHeadwordFlid)
			{
				int hn;
				if (m_homographNumbers.TryGetValue(hvo, out hn))
				{
					var entry = m_entryRepo.GetObject(hvo);
					return StringServices.HeadWordForWsAndHn(entry, ws, hn, "",
						HomographConfiguration.HeadwordVariant.Main);
				}
				// In case it's one we somehow don't know about, we'll let the base method try to get the real HN.
			}
			else if (tag == m_headwordRefFlid)
			{
				int hn;
				if (m_homographNumbers.TryGetValue(hvo, out hn))
				{
					var entry = m_entryRepo.GetObject(hvo);
					return StringServices.HeadWordForWsAndHn(entry, ws, hn, "",
						HomographConfiguration.HeadwordVariant.DictionaryCrossRef);
				}
				// In case it's one we somehow don't know about, we'll let the base method try to get the real HN.
			}
			else if (tag == m_headwordReversalFlid)
			{
				int hn;
				if (m_homographNumbers.TryGetValue(hvo, out hn))
				{
					var entry = m_entryRepo.GetObject(hvo);
					return StringServices.HeadWordForWsAndHn(entry, ws, hn, "",
						HomographConfiguration.HeadwordVariant.ReversalCrossRef);
				}
				// In case it's one we somehow don't know about, we'll let the base method try to get the real HN.
			}
			else if (tag == m_mlOwnerOutlineFlid)
			{
				// This adapts the logic of LexSense.OwnerOutlineNameForWs
				var sense = m_senseRepo.GetObject(hvo);
				return OwnerOutlineNameForWs(sense, ws, HomographConfiguration.HeadwordVariant.DictionaryCrossRef);
			}
			else if (tag == m_reversalNameFlid)
			{
				// This adapts the logic of LexSense.OwnerOutlineNameForWs
				var sense = m_senseRepo.GetObject(hvo);
				return OwnerOutlineNameForWs(sense, ws, HomographConfiguration.HeadwordVariant.ReversalCrossRef);
			}
			return base.get_MultiStringAlt(hvo, tag, ws);
		}

		/// <summary>
		/// Returns a TsString with the entry headword and a sense number if there
		/// are more than one senses.
		/// </summary>
		public ITsString OwnerOutlineNameForWs(ILexSense sense, int wsVern, HomographConfiguration.HeadwordVariant hv)
		{
			var entry = sense.Entry;
			int hn;
			if (!m_homographNumbers.TryGetValue(entry.Hvo, out hn))
				hn = entry.HomographNumber; // unknown entry, use its own HN instead of our override
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			tisb.AppendTsString(StringServices.HeadWordForWsAndHn(entry, wsVern, hn, "", hv));
			var hc = sense.Services.GetInstance<HomographConfiguration>();
			if (hc.ShowSenseNumber(hv) && HasMoreThanOneSense(entry))
			{
				// These int props may not be needed, but they're safe.
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
									  Cache.DefaultAnalWs);
				tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
									  HomographConfiguration.ksSenseReferenceNumberStyle);
				tisb.Append(" ");
				tisb.Append(GetSenseNumber(sense));
			}
			return tisb.GetString();
		}

		bool HasMoreThanOneSense(ILexEntry entry)
		{
			// We want
			//     return SensesOS.Count > 1
			//		|| (SensesOS.Count == 1 && SensesOS[0].SensesOS.Count > 0);
			// but must go through our own cache because some of them may be suppressed.
			var senseCount = get_VecSize(entry.Hvo, LexEntryTags.kflidSenses);
			if (senseCount > 1)
				return true;
			if (senseCount == 0)
				return false;
			int hvoSense = get_VecItem(entry.Hvo, LexEntryTags.kflidSenses, 0);
			return get_VecSize(hvoSense, LexSenseTags.kflidSenses) > 0;

		}

		private void BuildFieldsToFilter()
		{
			m_fieldsToFilter.Clear();
			var mdc = ((IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor);
			// Filter EVERY field in the entire model that stores entries, senses, examples, and (slightly differently)
			// LexReferences or LexEntryRefs in vector properties.
			foreach (var flid in mdc.GetFieldIds())
			{
				//This class currently can not handle filtering atomic values, if we need to do so a refactor is required,
				// At least override get_ObjectProp, also enhance PropChanged to not assume the property is a vector one.
				// PropChanged also needs work if for any reason we put non-object properties in any of these field collections
				if (mdc.GetFieldType(flid) != (int)CellarPropertyType.OwningAtomic && mdc.GetFieldType(flid) != (int)CellarPropertyType.ReferenceAtomic)
				{
					var dstCls = mdc.GetDstClsId(flid);
					if (dstCls == LexEntryTags.kClassId || dstCls == LexSenseTags.kClassId || dstCls == LexExampleSentenceTags.kClassId)
						m_fieldsToFilter.Add(flid);
					else if (dstCls == LexReferenceTags.kClassId)
						m_lexRefFieldsToFilter.Add(flid);
					else if (dstCls == LexEntryRefTags.kClassId)
						m_lexEntryRefFieldsToFilter.Add(flid);
				}
			}
			m_fieldsToFilter.Add(LexEntryRefTags.kflidComponentLexemes);
			m_fieldsToFilter.Add(LexEntryRefTags.kflidPrimaryLexemes);
			m_fieldsToFilter.Add(LexReferenceTags.kflidTargets);
			m_fieldsToFilter.Add(Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "PrimaryComponentLexemes", false));
			m_fieldsToFilter.Add(m_LexDbEntriesFlid);
		}

		/// <summary>
		/// Build the set of objects we want to hide: all the ones that refer to the Publication.
		/// Perhaps strictly we should exclude the ones that refer to it in DoNotPublishIn, but those are
		/// the only properties that refer to this target.
		/// </summary>
		private void BuildExcludedObjects()
		{
			m_excludedItems.Clear();
			m_excludeAsMainEntry.Clear();
			if (Publication != null)
			{
				foreach (var obj in Publication.ReferringObjects)
				{
					var entry = obj as ILexEntry;
					if (entry == null || entry.DoNotPublishInRC.Contains(Publication))
					{
						m_excludedItems.Add(obj.Hvo);
						if (obj is ILexEntry)
							foreach (var sense in ((ILexEntry)obj).SensesOS)
								ExcludeSense(sense);
						if (obj is ILexSense)
							ExcludeSense((ILexSense)obj);
					}
					else
					{
						// It's an entry, and the only other option is that it refers in DoNotShowAsMainEntry
						Debug.Assert(entry.DoNotShowMainEntryInRC.Contains(Publication));
						m_excludeAsMainEntry.Add(entry.Hvo);
					}
				}
			}
		}

		private void ExcludeSense(ILexSense sense)
		{
			m_excludedItems.Add(sense.Hvo);
			foreach (var subsense in sense.SensesOS)
				ExcludeSense(subsense);
			// It is probably not necessary to exclude examples, since there is no access path to
			// an example whose sense has been excluded.
		}

		/// <summary>
		/// The publication on which everything is based.
		/// When updating this after the initial construction of the decorator, the caller should RefreshDisplay
		/// on any views using the decorator, which will call Refresh on this, or if no view is using it,
		/// should call Refresh() directly.
		/// </summary>
		internal ICmPossibility Publication { get; set; }

		/// <summary>Returns HVO's of the entries to publish. If there are none, returns an empty array.</summary>
		public IEnumerable<int> GetEntriesToPublish(PropertyTable propertyTable, RecordClerk clerk)
		{
			switch(DictionaryConfigurationListener.GetDictionaryConfigurationType(propertyTable))
			{
				case "Dictionary":
					return VecProp(Cache.LangProject.LexDbOA.Hvo, clerk.VirtualFlid);
				case "Reversal Index":
				{
					var reversalIndexGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(propertyTable, "ReversalIndexGuid");
					if(reversalIndexGuid != Guid.Empty)
					{
						var currentReversalIndex = Cache.ServiceLocator.GetObject(reversalIndexGuid) as IReversalIndex;
						if (currentReversalIndex != null)
						{
							return currentReversalIndex.AllEntries.Select(indexEntry => indexEntry.Hvo);
						}
					}
					break;
				}
			}
			return new int[] { };
		}

		public override int[] VecProp(int hvo, int tag)
		{
			var result = base.VecProp(hvo, tag);
			if (result.Length == 0)
				return result; // Not worth messing about if it's already empty!
			if (tag == m_LexDbEntriesFlid)
			{
				return result.Where(hvoDest => !m_excludedItems.Contains(hvoDest) && !m_excludeAsMainEntry.Contains(hvoDest)).ToArray();
			}
			if (m_fieldsToFilter.Contains(tag))
			{
				// Enhance JohnT: possibly we should cache this?
				return result.Where(hvoDest => !m_excludedItems.Contains(hvoDest)).ToArray();
			}
			if (m_lexRefFieldsToFilter.Contains(tag))
			{
				return result.Where(IsPublishableLexRef).ToArray();
			}
			if (m_lexEntryRefFieldsToFilter.Contains(tag))
			{
				return result.Where(hvoRef => IsPublishableLexEntryRef(hvo, hvoRef)).ToArray();
			}
			if (tag == m_picsOfSensesFlid)
			{
				return result.Where(IsPublishablePicture).ToArray();
			}
			return result;
		}

		/// <summary>
		/// Enhance JohnT: there is some evidence (see LexReference.ExtractMinimalLexReferences)
		/// that LexReferences of the three sequence types should be allowed to show up if only
		/// ONE item is present. However, such a 'relation' doesn't seem very useful, and a longer
		/// one that is reduced to a single item by filtering is even less likely to be what the user wants.
		/// </summary>
		/// <param name="hvoRef"></param>
		/// <returns></returns>
		private bool IsPublishableLexRef(int hvoRef)
		{
			var publishableItems = VecProp(hvoRef, LexReferenceTags.kflidTargets);
			int originalItemCount = BaseSda.get_VecSize(hvoRef, LexReferenceTags.kflidTargets);
			if (originalItemCount == publishableItems.Length)
				return true; // If filtering didn't change anything don't mess with it.
			if (publishableItems.Length < 2)
				return false;
			// If at least two are publishable, it depends on the type.
			// It can't be published if the first item, which represents the root of the tree, is not publishable.
			var lexRef = m_lexRefRepo.GetObject(hvoRef);
			switch(((ILexRefType)lexRef.Owner).MappingType)
			{
				case (int)LexRefTypeTags.MappingTypes.kmtEntryTree:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseTree:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
					return !m_excludedItems.Contains(lexRef.TargetsRS[0].Hvo);
			}
			return true;
		}

		private bool IsPublishableLexEntryRef(int hvoSource, int hvoRef)
		{
			ILexEntryRef ler;
			ILexEntry refOwner = null;
			if(m_lerRepo.TryGetObject(hvoRef, out ler))
				refOwner = ler.Owner as ILexEntry;
			if (refOwner == null || refOwner.Hvo == hvoSource)
				return VecProp(hvoRef, LexEntryRefTags.kflidComponentLexemes).Length > 0;
			return !refOwner.DoNotPublishInRC.Contains(Publication);
		}

		// A picture is publishable if the sense it belongs to is not excluded.
		private bool IsPublishablePicture(int hvo)
		{
			var sense = Cache.ServiceLocator.GetObject(hvo).Owner as ILexSense;
			return sense != null && !m_excludedItems.Contains(sense.Hvo);
		}


		public override int get_VecSize(int hvo, int tag)
		{
			// Enhance JohnT: might be more efficient to call base if not a modified property?
			return VecProp(hvo, tag).Length;
		}

		public override int get_VecItem(int hvo, int tag, int index)
		{
			// Enhance JohnT: might be more efficient to call base if not a modified property?
			// Enhance JohnT: Sstop the enumeration filter when we get the one we need.
			return VecProp(hvo, tag)[index];
		}

		public override int GetObjIndex(int hvoOwn, int flid, int hvo)
		{
			return VecProp(hvoOwn, flid).ToList().IndexOf(hvo);
		}

		/// <summary>
		/// Update whatever needs it.
		/// </summary>
		public override void Refresh()
		{
			BuildExcludedObjects();
			// It probably isn't necessary to rebuild the fields, but one day we might be able to add a relevant custom field??
			BuildFieldsToFilter();
			BuildHomographInfo();
		}
	}
}
