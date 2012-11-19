using System;
using System.Collections.Generic;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using HvoFlidKey = SIL.FieldWorks.FDO.Application.HvoFlidKey;
using HvoFlidWSKey = SIL.FieldWorks.FDO.Application.HvoFlidWSKey;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// A 'Decorator' class for the FDO ISilDataAccess implementation.
	/// This class allows for caching the 'fake' flids used in a browse view
	/// (i.e., flides used for the check box and selection, etc.).
	/// </summary>
	public class XMLViewsDataCache : DomainDataByFlidDecoratorBase
	{
		// Fake flids for this cache should be in the range 90000000 to 99999999.
		// NB: For any fke flid added and used, be sure the override methods are updated.
		// For instance, the "get_IsPropInCache" method needs to knwo about all of them,
		// but other methods can be selective, based on the type of data is ion the flid.

		/// <summary>
		/// This item controls the status of the check box in the bulk edit view, and thus, whether an item is included
		/// in any bulk edit operations. HOWEVER, we don't cache this value for all objects in the list. The VC has a default
		/// state, DefaultChecked, which controls whether an item is selected if it has no value cached for ktagItemSelected.
		/// The value of this property should therefore always be accessed through BrowseViewer.GetCurrentCheckedValue().
		/// The one exception is when the check box is actually displayed by the VC; this uses the simple value in the cache,
		/// which is OK because a line in LoadDataFor makes sure there is a value cached for any item that is visible.
		/// </summary>
		public const int ktagItemSelected = 90000000;
		internal const int ktagItemEnabled = 90000001;
		// if ktagActiveColumn has a value for m_hvoRoot, then if the check box is on
		// for a given row, the indicated cell is highlighted with a pale blue
		// background, and the ktagAlternateValue property is displayed for those cells.
		// So that no action is required for default behavior, ktagActiveColumn uses
		// 1-based indexing, so zero means no active column.
		internal const int ktagActiveColumn = 90000002;
		internal const int ktagAlternateValue = 90000003;
		internal const int ktagTagMe = 90000004;

		// This group support Rapid Data Entry views (XmlBrowseRDEView).
		internal const int ktagEditColumnBase = 91000000;
		internal const int ktagEditColumnLim = 91000100;  // arbitrary max

		// Stores override values (when value is different from DefaultSelected) for ktagItemSelected.
		private readonly Dictionary<int, int> m_selectedCache;

		private readonly Dictionary<HvoFlidKey, int> m_integerCache = new Dictionary<HvoFlidKey, int>();
		private readonly Dictionary<HvoFlidWSKey, ITsString> m_mlStringCache = new Dictionary<HvoFlidWSKey, ITsString>();
		private readonly Dictionary<HvoFlidKey, ITsString> m_stringCache = new Dictionary<HvoFlidKey, ITsString>();

		/// <summary>
		/// This virtual flid needs special treatment, as we need to maintain any separators
		/// (semicolons) typed by the user, but also need to feed the string through to the
		/// virtual property handler for storing/creating new reversal index entries.
		/// See FWR-376.
		/// </summary>
		internal int m_tagReversalEntriesBulkText;

		/// <summary>
		/// The main constructor.
		/// </summary>
		public XMLViewsDataCache(ISilDataAccessManaged domainDataByFlid, bool defaultSelected, Dictionary<int, int> selectedItems)
			: base(domainDataByFlid)
		{
			DefaultSelected = defaultSelected;
			m_selectedCache = selectedItems;
			SetOverrideMdc(new XmlViewsMdc(domainDataByFlid.MetaDataCache as IFwMetaDataCacheManaged));
			m_tagReversalEntriesBulkText = domainDataByFlid.MetaDataCache.GetFieldId("LexSense", "ReversalEntriesBulkText", false);
		}

		/// <summary>
		/// Simpler constructor supplies default args
		/// </summary>
		internal XMLViewsDataCache(ISilDataAccessManaged domainDataByFlid, XmlNode nodeSpec)
			: this(domainDataByFlid, XmlUtils.GetOptionalBooleanAttributeValue(nodeSpec, "defaultChecked", true),
			new Dictionary<int, int>())
		{
		}

		/// <summary>
		/// True if ktagSelected should return 1 for items not previously set; false to return 0.
		/// </summary>
		internal bool DefaultSelected { get; set; }

		// The cache that controls ktagSelected along with DefaultSelected. Should only be used to save
		// for creating a new instance later.
		internal Dictionary<int, int> SelectedCache { get { return m_selectedCache; } }

		/// <summary>
		/// Override to support fake integer properties.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public override int get_IntProp(int hvo, int tag)
		{
			switch (tag)
			{
				default:
					return base.get_IntProp(hvo, tag);
				case ktagActiveColumn: // Fall through
				case ktagItemEnabled: // Fall through
					int result;
					if (m_integerCache.TryGetValue(new HvoFlidKey(hvo,  tag), out result))
						return result;
					return 0;
				case ktagItemSelected:
					return GetItemSelectedValue(hvo);
			}
		}

		private int GetItemSelectedValue(int hvo)
		{
			int sel;
			if (m_selectedCache.TryGetValue(hvo, out sel))
				return sel;
			return DefaultSelected ? 1 : 0;
		}

		/// <summary>
		/// Override to work with fake flids.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public override int get_ObjectProp(int hvo, int tag)
		{
			switch (tag)
			{
				default:
					return base.get_ObjectProp(hvo, tag);
				case ktagTagMe:
					return hvo;
			}
		}

		/// <summary>
		/// Override to work with fake flid.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public override int get_VecSize(int hvo, int tag)
		{
			return tag == ktagTagMe ? -1 : base.get_VecSize(hvo, tag);
		}

		/// <summary>
		/// Override to support fake integer properties.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="n"></param>
		public override void SetInt(int hvo, int tag, int n)
		{
			switch (tag)
			{
				default:
					base.SetInt(hvo, tag, n);
					break;
				case ktagActiveColumn:
				case ktagItemEnabled:
					{
						var key = new HvoFlidKey(hvo,  tag);
						int oldVal;
						if (m_integerCache.TryGetValue(key, out oldVal) && oldVal == n)
							return; // unchanged, avoid especially PropChanged.
						m_integerCache[key] = n;
						SendPropChanged(hvo, tag, 0, 0, 0);
					}
					break;
				case ktagItemSelected:
					{
						if (GetItemSelectedValue(hvo) == n)
							return; // unchanged, avoid especially PropChanged.
						m_selectedCache[hvo] = n;
						SendPropChanged(hvo, tag, 0, 0, 0);
					}
					break;
			}
		}

		/// <summary>
		/// Override to support some fake flids.
		/// </summary>
		public override bool get_IsPropInCache(int hvo, int tag, int cpt, int ws)
		{
			switch (tag)
			{
				default:
					if (tag == m_tagReversalEntriesBulkText &&
						m_mlStringCache.ContainsKey(new HvoFlidWSKey(hvo, tag, ws)))
					{
							return true;
					}
					return base.get_IsPropInCache(hvo, tag, cpt, ws);
				case ktagTagMe:
					return true; // hvo can always be itself.
				case ktagAlternateValue:
					return m_stringCache.ContainsKey(new HvoFlidKey(hvo,  tag));
				case ktagActiveColumn: // Fall through
				case ktagItemEnabled: // Fall through
					return m_integerCache.ContainsKey(new HvoFlidKey(hvo, tag));
				case ktagItemSelected:
					return true; // we have a default, this is effectively always in the cache.
			}
		}

		/// <summary>
		/// We understand how to get a multistring for any hvo in the tagEditColumn range.
		/// We also handle the virtual property LexEntry.ReversalEntriesBulkText in a
		/// special way.  (See FWR-376.)
		/// </summary>
		public override ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			ITsString result1 = null;
			if (tag < ktagEditColumnBase || tag > ktagEditColumnLim)
			{
				result1 = base.get_MultiStringAlt(hvo, tag, ws);
				if (tag != m_tagReversalEntriesBulkText)
					return result1;
			}
			ITsString result;
			if (m_mlStringCache.TryGetValue(new HvoFlidWSKey(hvo, tag, ws), out result))
				return result;
			if (tag == m_tagReversalEntriesBulkText && result1 != null)
				return result1;
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			return tsf.MakeString("", ws);
		}

		/// <summary>
		/// Store a value, without generating PropChanged calls.
		/// </summary>
		public void CacheMultiString(int hvo, int tag, int ws, ITsString val)
		{
			m_mlStringCache[new HvoFlidWSKey(hvo, tag, ws)] = val;
		}

		/// <summary>
		/// Override to handle props in the tagEditColumn range.  We also handle the virtual
		/// property LexEntry.ReversalEntriesBulkText in a special way.  (See FWR-376.)
		/// </summary>
		public override void SetMultiStringAlt(int hvo, int tag, int ws, ITsString tss)
		{
			if (tag < ktagEditColumnBase || tag > ktagEditColumnLim)
			{
				base.SetMultiStringAlt(hvo, tag, ws, tss);
				// Keep a local copy.
				if (tag == m_tagReversalEntriesBulkText)
					CacheMultiString(hvo, tag, ws, tss);
				return;
			}
			CacheMultiString(hvo, tag, ws, tss);
			SendPropChanged(hvo, tag, ws, 0, 0);
		}

		/// <summary>
		/// Override to handle ktagAlternateValue.
		/// </summary>
		public override ITsString get_StringProp(int hvo, int tag)
		{
			if (tag == ktagAlternateValue)
			{
				ITsString result;
				if (m_stringCache.TryGetValue(new HvoFlidKey(hvo, tag), out result))
					return result;
				// Try to find a sensible WS from existing data, avoiding a crash if possible.
				// See FWR-3598.
				ITsString tss = null;
				foreach (var x in m_stringCache.Keys)
				{
					tss = m_stringCache[x];
					if (x.Flid == tag)
						break;
				}
				if (tss == null)
				{
					foreach (var x in m_mlStringCache.Keys)
					{
						ITsStrFactory tsf = TsStrFactoryClass.Create();
						return tsf.EmptyString(x.Ws);
					}
				}
				if (tss != null)
				{
					ITsStrFactory tsf = TsStrFactoryClass.Create();
					var ws = TsStringUtils.GetWsOfRun(tss, 0);
					return tsf.EmptyString(ws);
				}
				// Enhance JohnT: might be desirable to return empty string rather than crashing,
				// but as things stand, we don't know what would be a sensible WS.
				throw new InvalidOperationException("trying to read a preview value not previously cached");
			}
			return base.get_StringProp(hvo, tag);
		}

		/// <summary>
		/// Override to handle ktagAlternateValue.
		/// </summary>
		public override void SetString(int hvo, int tag, ITsString _tss)
		{
			if (tag == ktagAlternateValue)
			{
				int oldLen = 0;
				ITsString oldVal;
				if (m_stringCache.TryGetValue(new HvoFlidKey(hvo, tag), out oldVal))
					oldLen = oldVal.Length;
				m_stringCache[new HvoFlidKey(hvo, tag)] = _tss;
				SendPropChanged(hvo, tag, 0, _tss.Length, oldLen);
				return;
			}
			base.SetString(hvo, tag, _tss);
		}

	}

	class XmlViewsMdc : FdoMetaDataCacheDecoratorBase
	{
		public XmlViewsMdc(IFwMetaDataCacheManaged metaDataCache) : base(metaDataCache)
		{
		}

		public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
		{
			throw new NotImplementedException();
		}

		// So far, this is the only query that needs to know about the virtual props.
		// It may not even need to know about all of these.
		public override string GetFieldName(int flid)
		{
			switch (flid)
			{
				case XMLViewsDataCache.ktagTagMe: return "Me";
				case XMLViewsDataCache.ktagActiveColumn: return "ActiveColumn";
				case XMLViewsDataCache.ktagAlternateValue: return "AlternateValue";
				case XMLViewsDataCache.ktagItemEnabled: return "ItemEnabled";
				case XMLViewsDataCache.ktagItemSelected: return "ItemSelected";
			}
			// Paste operations currently require the column to have some name.
			if (flid >= XMLViewsDataCache.ktagEditColumnBase && flid < XMLViewsDataCache.ktagEditColumnLim)
				return "RdeColumn" + (flid - XMLViewsDataCache.ktagEditColumnBase);
			return base.GetFieldName(flid);
		}

		public override int GetFieldType(int luFlid)
		{
			// This is a bit arbitrary. Technically, the form column isn't formattable, while the one shadowing
			// Definition could be. But pretending all are Unicode just means Collect Words can't do formatting
			// of definitions, while allowing it in the Form could lead to crashes when we copy to the real field.
			if (luFlid >= XMLViewsDataCache.ktagEditColumnBase && luFlid < XMLViewsDataCache.ktagEditColumnLim)
				return (int)CellarPropertyType.Unicode;
			return base.GetFieldType(luFlid);
		}
	}
}
