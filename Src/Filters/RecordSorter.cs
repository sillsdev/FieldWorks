// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.FieldWorks.Language;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Utils;
using SIL.WritingSystems;
using SIL.Xml;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// Just a shell class for containing runtime Switches for controling the diagnostic output.
	/// </summary>
	public class RuntimeSwitches
	{
		/// Tracing variable - used to control when and what is output to the debug and trace listeners
		public static TraceSwitch RecordTimingSwitch = new TraceSwitch("FilterRecordTiming", "Used for diagnostic timing output", "Off");
	}

	/// <summary>
	/// sort (in memory) based on in LCM property
	/// </summary>
	public class PropertyRecordSorter : RecordSorter
	{
		private IComparer m_comp;

		private LcmCache m_cache;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:PropertyRecordSorter"/> class.
		/// </summary>
		public PropertyRecordSorter(string propertyName)
		{
			Init(propertyName);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:PropertyRecordSorter"/> class.
		/// </summary>
		public PropertyRecordSorter()
		{
		}

		/// <summary>
		/// Inits the specified property name.
		/// </summary>
		protected void Init(string propertyName)
		{
			PropertyName = propertyName;
		}

		/// <summary>
		/// Add to the specified XML node information required to create a new
		/// record sorter equivalent to yourself.
		/// In this case we just need to add the sortProperty attribute.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			node.Add(new XAttribute("sortProperty", PropertyName));
		}

		/// <summary>
		/// Initialize an instance into the state indicated by the node, which was
		/// created by a call to PersistAsXml.
		/// </summary>
		public override void InitXml(XElement node)
		{
			Init(node);
		}

		public override LcmCache Cache
		{
			set
			{
				m_cache = value;
				base.Cache = value;
			}
		}

		/// <summary>
		/// Gets the name of the property.
		/// </summary>
		public string PropertyName { get; protected set; }

		/// <summary>
		/// Inits the specified configuration.
		/// </summary>
		protected void Init(XElement configuration)
		{
			PropertyName = XmlUtils.GetMandatoryAttributeValue(configuration, "sortProperty");
		}

		/// <summary>
		/// Get the object that does the comparisons.
		/// </summary>
		protected internal override IComparer getComparer()
		{
			return new LcmCompare(PropertyName, m_cache);
		}

		/// <summary>
		/// Sorts the specified records.
		/// </summary>
		public override void Sort(ArrayList records)
		{
#if DEBUG
			var dt1 = DateTime.Now;
			var tc1 = Environment.TickCount;
#endif
			m_comp = getComparer();
			if (m_comp is LcmCompare)
			{
				(m_comp as LcmCompare).ComparisonNoter = this;
				m_comparisonsDone = 0;
				m_percentDone = 0;
				// Make sure at least 1 so we don't divide by zero.
				m_comparisonsEstimated = Math.Max(records.Count * (int)Math.Ceiling(Math.Log(records.Count, 2.0)), 1);
			}

			records.Sort(m_comp);

#if DEBUG
			// only do this if the timing switch is info or verbose
			if (RuntimeSwitches.RecordTimingSwitch.TraceInfo)
			{
				var tc2 = Environment.TickCount;
				var ts1 = DateTime.Now - dt1;
				var s = "PropertyRecordSorter: Sorting " + records.Count + " records took " +
						   (tc2 - tc1) + " ticks," + " or " + ts1.Minutes + ":" + ts1.Seconds + "." +
						   ts1.Milliseconds.ToString("d3") + " min:sec.";
				Debug.WriteLine(s, RuntimeSwitches.RecordTimingSwitch.DisplayName);
			}
#endif
		}

		/// <summary>
		/// Merges the into.
		/// </summary>
		public override void MergeInto(/*ref*/ ArrayList records, ArrayList newRecords)
		{
			var fc = new LcmCompare(PropertyName, m_cache);
			MergeInto(records, newRecords, (IComparer) fc);
			fc.CloseCollatingEngine(); // Release the ICU data file.
		}

		/// <summary>
		/// a factory method for property sorders
		/// </summary>
		public static PropertyRecordSorter Create(LcmCache cache, XElement configuration)
		{
			var sorter = (PropertyRecordSorter)DynamicLoader.CreateObject(configuration);
			sorter.Init(configuration);
			return sorter;
		}
	}

	/// <summary>
	/// Abstract RecordSorter base class.
	/// </summary>
	public abstract class RecordSorter : IPersistAsXml, IStoresLcmCache, IReportsSortProgress, INoteComparision
	{
		/// <summary>
		/// Add to the specified XML node information required to create a new
		/// record sorter equivalent to yourself.
		/// The default is not to add any information.
		/// It may be assumed that the node already contains the assembly and class
		/// information required to create an instance of the sorter in a default state.
		/// An equivalent XmlNode will be passed to the InitXml method of the
		/// re-created sorter.
		/// This default implementation does nothing.
		/// </summary>
		/// <param name="node"></param>
		public virtual void PersistAsXml(XElement node)
		{
		}

		/// <summary>
		/// Initialize an instance into the state indicated by the node, which was
		/// created by a call to PersistAsXml.
		/// </summary>
		public virtual void InitXml(XElement node)
		{
		}

		#region IStoresLcmCache
		/// <summary>
		/// Set an LcmCache for anything that needs to know.
		/// </summary>
		public virtual LcmCache Cache
		{
			set
			{
				// do nothing by default.
			}
		}

		/// <summary>
		/// Set the data access to use in interpreting properties of objects.
		/// Call this AFTER setting Cache, otherwise, it may be overridden.
		/// </summary>
		public virtual ISilDataAccess DataAccess
		{
			set { }// do nothing by default.
		}
		#endregion IStoresLcmCache

		/// <summary>
		/// Method to retrieve the IComparer used by this sorter
		/// </summary>
		protected internal abstract IComparer getComparer();

		/// <summary>
		/// Sorts the specified records.
		/// </summary>
		public abstract void Sort(ArrayList records);

		/// <summary>
		/// Merges the into.
		/// </summary>
		public abstract void MergeInto(ArrayList records, ArrayList newRecords);

		/// <summary>
		/// Merge the new records into the original list using the specified comparer
		/// </summary>
		protected void MergeInto(ArrayList records, ArrayList newRecords, IComparer comparer)
		{
			for (int i = 0, j = 0; j < newRecords.Count; i++)
			{
				if (i >= records.Count || comparer.Compare(records[i], newRecords[j]) > 0)
				{
					// object at i is greater than current object to insert, or no more obejcts in records;
					// insert next object here.
					records.Insert(i, newRecords[j]);
					j++;
					// i gets incremented as usual past the inserted object. It will index the same
					// object next iteration as it did this one.
				}
			}
		}

		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		public virtual void CollectItems(int hvo, ArrayList collector)
		{
			collector.Add(new ManyOnePathSortItem(hvo, null, null));
		}

		/// <summary>
		/// May be implemented to preload before sorting large collections (typically most of the instances).
		/// Currently will not be called if the column is also filtered; typically the same Preload() would end
		/// up being done.
		/// </summary>
		public virtual void Preload(object rootObj)
		{
		}

		/// <summary>
		/// Return true if the other sorter is 'compatible' with this, in the sense that
		/// either they produce the same sort sequence, or one derived from it (e.g., by reversing).
		/// </summary>
		public virtual bool CompatibleSorter(RecordSorter other)
		{
			return false;
		}

		/// <summary />
		protected class LcmCompare : IComparer, IPersistAsXml
		{
			/// <summary />
			protected ICollator m_collater;
			/// <summary />
			protected bool m_fUseKeys;
			internal INoteComparision ComparisonNoter { get; set; }
			private Dictionary<object, string> m_sortKeyCache;

			private LcmCache m_cache;

			/// <summary>
			/// Initializes a new instance of the <see cref="T:LcmCompare"/> class.
			/// </summary>
			public LcmCompare(string propertyName, LcmCache cache)
			{
				m_cache = cache;
				Init();
				PropertyName= propertyName;
				m_fUseKeys = propertyName == "ShortName";
				m_sortKeyCache = new Dictionary<object, string>();
			}

			/// <summary>
			/// This constructor is intended to be used for persistence with IPersistAsXml
			/// </summary>
			public LcmCompare()
			{
				Init();
			}

			private void Init()
			{
				m_collater = null;
			}

			/// <summary>
			/// Gets the name of the property.
			/// </summary>
			public string PropertyName { get; protected set; }

			/// <summary>
			/// Add to the specified XML node information required to create a new
			/// object equivalent to yourself. The node already contains information
			/// sufficient to create an instance of the proper class.
			/// </summary>
			public void PersistAsXml(XElement node)
			{
				XmlUtils.SetAttribute(node, "property", PropertyName);
			}

			/// <summary>
			/// Initialize an instance into the state indicated by the node, which was
			/// created by a call to PersistAsXml.
			/// </summary>
			public void InitXml(XElement node)
			{
				PropertyName = XmlUtils.GetMandatoryAttributeValue(node, "property");
			}

			/// <summary>
			/// Gets the property.
			/// </summary>
			protected object GetProperty(ICmObject target, string property)
			{
				var type = target.GetType();
				var info = type.GetProperty(property,
					BindingFlags.Instance |
					BindingFlags.Public |
					BindingFlags.FlattenHierarchy);
				if (info == null)
				{
					throw new ArgumentException($"There is no public property named '{property}' in {type}. Remember, properties often end in a multi-character suffix such as OA, OS, RA, RS, or Accessor.");
				}

				return info.GetValue(target,null);
			}

			/// <summary>
			/// Opens the collating engine.
			/// </summary>
			public void OpenCollatingEngine(string sWs)
			{
				m_collater = m_cache.ServiceLocator.WritingSystemManager.Get(sWs).DefaultCollation.Collator;
			}

			/// <summary>
			/// Closes the collating engine.
			/// </summary>
			public void CloseCollatingEngine()
			{
			}

			private ICmObject GetObjFromItem(object x)
			{
				var itemX = x as IManyOnePathSortItem;
				// This is slightly clumsy but currently it's the only way we have to get a cache.
				return itemX.KeyObjectUsing(m_cache);
			}

			// Compare two objects (expected to be ManyOnePathSortItems).
			/// <summary>
			/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
			/// </summary>
			/// <param name="x">The first object to compare.</param>
			/// <param name="y">The second object to compare.</param>
			/// <returns>
			/// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
			/// </returns>
			/// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
			public int Compare(object x, object y)
			{
				ComparisonNoter?.ComparisonOccurred(); // for progress reporting.

				if (x == y)
				{
					return 0;
				}
				if (x == null)
				{
					return -1;
				}
				if (y == null)
				{
					return 1;
				}

				// One time overhead
				if (m_collater == null)
				{
					OpenCollatingEngine((GetObjFromItem(x)).SortKeyWs);
				}

				return m_collater.Compare(GetObjFromCacheOrItem(x), GetObjFromCacheOrItem(y));
			}

			private string GetObjFromCacheOrItem(object item)
			{
				var item1 = (ManyOnePathSortItem)item;
				string cachedKey;
				if (!m_sortKeyCache.TryGetValue(item1, out cachedKey))
				{
					if (m_fUseKeys)
					{
						cachedKey = GetObjFromItem(item).SortKey;
					}
					else
					{
						cachedKey = (string)GetProperty(GetObjFromItem(item), PropertyName);
					}
					m_sortKeyCache.Add(item1, cachedKey);
				}
				return cachedKey;
			}

			/// <summary />
			public override bool Equals(object obj)
			{
				if (obj == null)
				{
					return false;
				}
				// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
				// is marked with [MonoTODO] and might not work as expected in 4.0.
				if (GetType() != obj.GetType())
				{
					return false;
				}
				var that = (LcmCompare)obj;
				return m_fUseKeys == that.m_fUseKeys && (m_collater == that.m_collater && PropertyName == that.PropertyName);
			}

			/// <summary />
			public override int GetHashCode()
			{
				var hash = GetType().GetHashCode();
				if (m_fUseKeys)
				{
					hash *= 3;
				}
				if (m_collater != null)
				{
					hash += m_collater.GetHashCode();
				}
				if (PropertyName != null)
				{
					hash *= PropertyName.GetHashCode();
				}
				return hash;
			}
		}

		public Action<int> SetPercentDone
		{
			get; set;
		}

		internal int m_comparisonsDone;
		internal int m_comparisonsEstimated;
		internal int m_percentDone;
		public void ComparisonOccurred()
		{
			if (SetPercentDone == null)
			{
				return;
			}
			m_comparisonsDone++;
			var newPercentDone = m_comparisonsDone*100/m_comparisonsEstimated;
			if (newPercentDone == m_percentDone)
			{
				return;
			}
			m_percentDone = newPercentDone;
			SetPercentDone(Math.Min(m_percentDone, 100)); // just in case we do more than the estimated number.
		}
	}

	public class AndSorter : RecordSorter
	{
		private sealed class AndSorterComparer : IComparer, ICloneable
		{
			private ArrayList m_sorters;
			private ArrayList m_comps;

			/// <summary>
			/// Creates a new AndSortComparer
			/// </summary>
			public AndSorterComparer(ArrayList sorters)
			{
				m_sorters = sorters;
				m_comps = new ArrayList();
				foreach (RecordSorter rs in m_sorters)
				{
					var comp = rs.getComparer();
					(comp as StringFinderCompare)?.Init();
					m_comps.Add(comp);
				}
			}

			#region IComparer Members

			public int Compare(object x, object y)
			{
				var ret = 0;
				for (var i = 0; i < m_sorters.Count; i++)
				{
					ret = ((IComparer)m_comps[i]).Compare(x, y);
					if (ret != 0)
					{
						break;
					}
				}

				return ret;
			}

			#endregion

			#region ICloneable Members

			/// <summary>
			/// Clones the current object
			/// </summary>
			public object Clone()
			{
				return new AndSorterComparer(m_sorters);
			}

			#endregion

			/// <summary />
			public override bool Equals(object obj)
			{
				if (obj == null)
				{
					return false;
				}
				// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
				// is marked with [MonoTODO] and might not work as expected in 4.0.
				if (GetType() != obj.GetType())
				{
					return false;
				}
				var that = (AndSorterComparer)obj;
				if (m_comps == null)
				{
					if (that.m_comps != null)
					{
						return false;
					}
				}
				else
				{
					if (m_comps.Count != that.m_comps?.Count)
					{
						return false;
					}
					if (m_comps.Cast<object>().Where((t, i) => m_comps[i] != that.m_comps[i]).Any())
					{
						return false;
					}
				}
				if (m_sorters == null)
				{
					if (that.m_sorters != null)
					{
						return false;
					}
				}
				else
				{
					if (m_sorters.Count != that.m_sorters?.Count)
					{
						return false;
					}
					return !m_sorters.Cast<object>().Where((t, i) => m_sorters[i] != that.m_sorters[i]).Any();
				}
				return true;
			}

			/// <summary />
			public override int GetHashCode()
			{
				var hash = GetType().GetHashCode();
				if (m_comps != null)
				{
					hash += m_comps.Count * 3;
				}
				if (m_sorters != null)
				{
					hash += m_sorters.Count * 17;
				}
				return hash;
			}
		}

		public AndSorter() { }

		public AndSorter(ArrayList sorters): this()
		{
			foreach (RecordSorter rs in sorters)
			{
				Add(rs);
			}
		}

		/// <summary>
		/// Adds a sorter
		/// </summary>
		public void Add(RecordSorter sorter)
		{
			Sorters.Add(sorter);
		}

		/// <summary>
		/// Replaces a sorter
		/// </summary>
		/// <param name="index">The index where we want to replace the sorter</param>
		/// <param name="oldSorter">A reference to the old sorter</param>
		/// <param name="newSorter">A reference to the new sorter</param>
		public void ReplaceAt(int index, RecordSorter oldSorter, RecordSorter newSorter)
		{
			if (index < 0 || index >= Sorters.Count)
			{
				throw new IndexOutOfRangeException();
			}
			Sorters[index] = newSorter;
		}

		/// <summary>
		/// Gets the list of sorters.
		/// </summary>
		public ArrayList Sorters { get; private set; } = new ArrayList();

		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		public override void CollectItems(int hvo, ArrayList collector)
		{
			if (Sorters.Count > 0)
			{
				((RecordSorter)Sorters[0]).CollectItems(hvo, collector);
			}
			else
			{
				base.CollectItems(hvo, collector);
			}
		}

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node);
			foreach (RecordSorter rs in Sorters)
			{
				DynamicLoader.PersistObject(rs, node, "sorter");
			}
		}

		// This will probably never be used, but for the sake of completeness, here it is
		protected internal override IComparer getComparer()
		{
			IComparer comp = new AndSorterComparer(Sorters);
			return comp;
		}

		public override void Sort(/*ref*/ ArrayList records)
		{
#if DEBUG
			var dt1 = DateTime.Now;
			var tc1 = Environment.TickCount;
#endif
			var comp = new AndSorterComparer(Sorters);
			MergeSort.Sort(ref records, comp);

#if DEBUG
			// only do this if the timing switch is info or verbose
			if (RuntimeSwitches.RecordTimingSwitch.TraceInfo)
			{
				var tc2 = Environment.TickCount;
				var ts1 = DateTime.Now - dt1;
				var s = $"AndSorter:  Sorting {records.Count} records took {tc2 - tc1} ticks, or {ts1.Minutes}:{ts1.Seconds}.{ts1.Milliseconds:d3} min:sec.";
				Debug.WriteLine(s, RuntimeSwitches.RecordTimingSwitch.DisplayName);
			}
#endif
		}

		public override void MergeInto(ArrayList records, ArrayList newRecords)
		{
			var comp = new AndSorterComparer(Sorters);
			MergeInto(records, newRecords, comp);
		}

		public override bool CompatibleSorter(RecordSorter other)
		{
			foreach (RecordSorter rs in Sorters)
			{
				if (rs.CompatibleSorter(other))
				{
					return true;
				}
			}

			return false;
		}

		public int CompatibleSorterIndex(RecordSorter other)
		{
			for (var i = 0; i < Sorters.Count; i++)
			{
				if (((RecordSorter)Sorters[i]).CompatibleSorter(other))
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml (node);
			Sorters = new ArrayList(node.Elements().Count());
			foreach (var child in node.Elements())
			{
				var obj = DynamicLoader.RestoreFromChild(child, ".");
				Sorters.Add(obj);
			}
		}

		public override LcmCache Cache
		{
			set
			{
				foreach (RecordSorter rs in Sorters)
				{
					rs.Cache = value;
				}
			}
		}
	}

	/// <summary>
	/// A very general record sorter class, based on an arbitrary implementation of IComparer
	/// that can compare two LCM objects.
	/// </summary>
	public class GenRecordSorter : RecordSorter
	{
		/// <summary>
		/// Normal constructor.
		/// </summary>
		public GenRecordSorter(IComparer comp): this()
		{
			Comparer = comp;
		}

		/// <summary>
		/// Default constructor for IPersistAsXml
		/// </summary>
		public GenRecordSorter()
		{
		}

		protected internal override IComparer getComparer()
		{
			return Comparer;
		}

		/// <summary>
		/// See whether the comparer can preload. Currently we only know about one kind that can.
		/// </summary>
		public override void Preload(object rootObj)
		{
			base.Preload(rootObj);
			if (Comparer is StringFinderCompare)
			{
				((StringFinderCompare)Comparer).Preload(rootObj);
			}
		}

		/// <summary>
		/// Override to pass it on to the comparer, if relevant.
		/// </summary>
		public override ISilDataAccess DataAccess
		{
			set
			{
				base.DataAccess = value;
				if (Comparer is StringFinderCompare)
				{
					((StringFinderCompare)Comparer).DataAccess = value;
				}
			}
		}

		/// <summary>
		/// Return true if the other sorter is 'compatible' with this, in the sense that
		/// either they produce the same sort sequence, or one derived from it (e.g., by reversing).
		/// </summary>
		public override bool CompatibleSorter(RecordSorter other)
		{
			var grsOther = other as GenRecordSorter;
			if (grsOther == null)
			{
				return false;
			}

			if (CompatibleComparers(Comparer, grsOther.Comparer))
			{
				return true;
			}
			// Currently the only other kind of compatibility we know how to detect
			// is StringFinderCompares that do more-or-less the same thing.
			var sfcThis = Comparer as StringFinderCompare;
			var sfcOther = grsOther.Comparer as StringFinderCompare;
			if (sfcThis == null || sfcOther == null)
			{
				return false;
			}
			if (!sfcThis.Finder.SameFinder(sfcOther.Finder))
			{
				return false;
			}
			// We deliberately don't care if one has a ReverseCompare and the other
			// doesn't. That's handled by a different icon.
			var subCompOther = UnpackReverseCompare(sfcOther);
			var subCompThis = UnpackReverseCompare(sfcThis);
			return CompatibleComparers(subCompThis, subCompOther);
		}

		private IComparer UnpackReverseCompare(StringFinderCompare sfc)
		{
			var subComp = sfc.SubComparer;
			if (subComp is ReverseComparer)
			{
				subComp = (subComp as ReverseComparer).SubComp;
			}
			return subComp;
		}

		/// <summary>
		/// Return true if the two comparers will give the same result. Ideally this would
		/// be an interface method on ICompare, but that interface is defined by .NET so we
		/// can't enhance it. This knows about a few interesting cases.
		/// </summary>
		private static bool CompatibleComparers(IComparer first, IComparer second)
		{
			// identity
			if (first == second)
			{
				return true;
			}

			// IcuComparers on same Ws?
			var firstIcu = first as IcuComparer;
			var secondIcu = second as IcuComparer;
			if (firstIcu != null && secondIcu != null && firstIcu.WsCode == secondIcu.WsCode)
			{
				return true;
			}

			// WritingSystemComparers on same Ws?
			var firstWs = first as WritingSystemComparer;
			var secondWs = second as WritingSystemComparer;
			if (firstWs != null && secondWs != null && firstWs.WsId == secondWs.WsId)
			{
				return true;
			}

			// Both IntStringComparers?
			if (first is IntStringComparer && second is IntStringComparer)
			{
				return true;
			}

			// LcmComparers on the same property?
			var firstLcm = first as LcmCompare;
			var secondLcm = second as LcmCompare;
			return firstLcm != null && secondLcm != null && firstLcm.PropertyName == secondLcm.PropertyName;
		}

		/// <summary>
		/// Gets or sets the comparer.
		/// </summary>
		public IComparer Comparer { get; set; }

		/// <summary>
		/// Add to the specified XML node information required to create a new
		/// record sorter equivalent to yourself.
		/// The default is not to add any information.
		/// It may be assumed that the node already contains the assembly and class
		/// information required to create an instance of the sorter in a default state.
		/// An equivalent XmlNode will be passed to the InitXml method of the
		/// re-created sorter.
		/// This default implementation does nothing.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node); // does nothing, but in case needed later...
			var persistComparer = Comparer as IPersistAsXml;
			if (persistComparer == null)
			{
				throw new Exception($"cannot persist GenRecSorter with comparer class {Comparer.GetType().AssemblyQualifiedName}");
			}
			DynamicLoader.PersistObject(persistComparer, node, "comparer");
		}

		/// <summary>
		/// Initialize an instance into the state indicated by the node, which was
		/// created by a call to PersistAsXml.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml (node);
			var compNode = node.Elements().First();
			if (compNode.Name != "comparer")
			{
				throw new Exception("persist info for GenRecordSorter must have comparer child element");
			}
			Comparer = DynamicLoader.RestoreObject(compNode) as IComparer;
			if (Comparer == null)
			{
				throw new Exception("restoring sorter failed...comparer does not implement IComparer");
			}
		}

		/// <summary>
		/// Set an LcmCache for anything that needs to know.
		/// </summary>
		public override LcmCache Cache
		{
			set
			{
				if (Comparer is IStoresLcmCache)
				{
					((IStoresLcmCache)Comparer).Cache = value;
				}
			}
		}

		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		public override void CollectItems(int hvo, ArrayList collector)
		{
			if (Comparer is StringFinderCompare)
			{
				((StringFinderCompare)Comparer).CollectItems(hvo, collector);
			}
			else
				base.CollectItems(hvo, collector);
		}

		/// <summary>
		/// Sorts the specified records.
		/// </summary>
		public override void Sort(/*ref*/ ArrayList records)
		{
#if DEBUG
			var dt1 = DateTime.Now;
			var tc1 = Environment.TickCount;
#endif
			if (Comparer is StringFinderCompare)
			{
				((StringFinderCompare)Comparer).Init();
				((StringFinderCompare)Comparer).ComparisonNoter = this;
				m_comparisonsDone = 0;
				m_percentDone = 0;
				// Make sure at least 1 so we don't divide by zero.
				m_comparisonsEstimated = Math.Max(records.Count*(int)Math.Ceiling(Math.Log(records.Count, 2.0)), 1);
			}

			//records.Sort(m_comp);
			MergeSort.Sort(ref records, Comparer);

			if (Comparer is StringFinderCompare)
			{
				((StringFinderCompare)Comparer).Cleanup();
			}
#if DEBUG
			// only do this if the timing switch is info or verbose
			if (RuntimeSwitches.RecordTimingSwitch.TraceInfo)
			{
				var tc2 = Environment.TickCount;
				var ts1 = DateTime.Now - dt1;
				var s = $"GenRecordSorter:  Sorting {records.Count} records took {tc2 - tc1} ticks, or {ts1.Minutes}:{ts1.Seconds}.{ts1.Milliseconds:d3} min:sec.";
				Debug.WriteLine(s, RuntimeSwitches.RecordTimingSwitch.DisplayName);
			}
#endif
		}
		/// <summary>
		/// Required implementation.
		/// </summary>
		public override void MergeInto(ArrayList records, ArrayList newRecords)
		{
			if (Comparer is StringFinderCompare)
			{
				((StringFinderCompare)Comparer).Init();
			}

			MergeInto(records, newRecords, Comparer);

			if (Comparer is StringFinderCompare)
			{
				((StringFinderCompare)Comparer).Cleanup();
			}
		}

		/// <summary>
		/// Check whether this GenRecordSorter is equal to another object.
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			if (GetType() != obj.GetType())
			{
				return false;
			}
			var that = (GenRecordSorter)obj;
			if (Comparer == null)
			{
				if (that.Comparer != null)
				{
					return false;
				}
			}
			else
			{
				if (that.Comparer == null)
				{
					return false;
				}
				// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
				// is marked with [MonoTODO] and might not work as expected in 4.0.
				return Comparer.GetType() == that.Comparer.GetType() && Comparer.Equals(that.Comparer);
			}
			return true;
		}

		/// <summary />
		public override int GetHashCode()
		{
			var hash = GetType().GetHashCode();
			if (Comparer != null)
			{
				hash *= Comparer.GetHashCode();
			}
			return hash;
		}
	}

	/// <summary>
	/// This class compares two ManyOnePathSortItems by making use of the ability of a StringFinder
	/// object to obtain strings from the KeyObject hvo, then
	/// a (simpler) IComparer to compare the strings.
	/// </summary>
	public class StringFinderCompare : IComparer, IPersistAsXml, IStoresLcmCache, IStoresDataAccess, ICloneable
	{
		/// <summary>This is used, during a single sort, to cache keys.</summary>
		protected Hashtable m_objToKey = new Hashtable();
		internal INoteComparision ComparisonNoter { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:StringFinderCompare"/> class.
		/// </summary>
		public StringFinderCompare(IStringFinder finder, IComparer subComp)
		{
			Finder = finder;
			SubComparer = subComp;
			SortedFromEnd = false;
			SortedByLength = false;
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		public StringFinderCompare(): this(null, null)
		{
		}

		/// <summary>
		/// Gets the finder.
		/// </summary>
		public IStringFinder Finder { get; protected set; }

		public ISilDataAccess DataAccess
		{
			set
			{
				if (Finder != null)
				{
					Finder.DataAccess = value;
				}
			}
		}

		/// <summary>
		/// May be implemented to preload before sorting large collections (typically most of the instances).
		/// Currently will not be called if the column is also filtered; typically the same Preload() would end
		/// up being done.
		/// </summary>
		public virtual void Preload(object rootObj)
		{
			Finder.Preload(rootObj);
		}

		/// <summary>
		/// Gets the sub comparer.
		/// </summary>
		public IComparer SubComparer { get; protected set; }

		/// <summary>
		/// Copy our comparer's SubComparer and SortedFromEnd to another comparer.
		/// </summary>
		public void CopyTo(StringFinderCompare copyComparer)
		{
			copyComparer.SubComparer = SubComparer is ICloneable ? ((ICloneable)SubComparer).Clone() as IComparer : SubComparer;
			copyComparer.SortedFromEnd = SortedFromEnd;
			copyComparer.SortedByLength = SortedByLength;
		}

		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		public void CollectItems(int hvo, ArrayList collector)
		{
			Finder.CollectItems(hvo, collector);
		}

		/// <summary>
		/// Give the sort order. Considered to be ascending unless it has been reversed.
		/// </summary>
		public SortOrder Order => SubComparer is ReverseComparer ? SortOrder.Descending : SortOrder.Ascending;

		/// <summary>
		/// Flag whether to sort normally from the beginnings of words, or to sort from the
		/// ends of words.  This is useful for grouping words by suffix.
		/// </summary>
		public bool SortedFromEnd { get; set; }

		/// <summary>
		/// Flag whether to sort normally from the beginnings of words, or to sort from the
		/// ends of words.  This is useful for grouping words by suffix.
		/// </summary>
		public bool SortedByLength { get; set; }

		/// <summary>
		/// Inits this instance.
		/// </summary>
		public void Init()
		{
			m_objToKey.Clear();
			if (SubComparer is IcuComparer)
			{
				((IcuComparer)SubComparer).OpenCollatingEngine();
			}
			else if (SubComparer is ReverseComparer)
			{
				var ct = ReverseComparer.Reverse(SubComparer);
				if (ct is IcuComparer)
				{
					((IcuComparer)ct).OpenCollatingEngine();
				}
			}
		}

		/// <summary>
		/// Cleanups this instance. Note that clients are not obliged to call this; it just
		/// helps a little with garbage collection.
		/// </summary>
		public void Cleanup()
		{
			m_objToKey.Clear(); // redundant, but may help free memory.
			if (SubComparer is IcuComparer)
			{
				((IcuComparer)SubComparer).CloseCollatingEngine();
			}
			else if (SubComparer is ReverseComparer)
			{
				var ct = ReverseComparer.Reverse(SubComparer);
				if (ct is IcuComparer)
				{
					((IcuComparer)ct).CloseCollatingEngine();
				}
			}
		}

		/// <summary>
		/// Reverse the order of the sort.
		/// </summary>
		public void Reverse()
		{
			SubComparer = ReverseComparer.Reverse(SubComparer);
		}

		protected internal string[] GetValue(object key, bool sortedFromEnd)
		{
			try
			{
				var result = m_objToKey[key] as string[];
				if (result != null)
				{
					return result;
				}
				var item = key as IManyOnePathSortItem;
				result =  Finder.SortStrings(item, sortedFromEnd);
				m_objToKey[key] = result;
				return result;
			}
			catch (Exception e)
			{
				throw new Exception("StringFinderCompare could not get key for " + key, e);
			}
		}

		#region IComparer Members

		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal
		/// to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero
		/// x is greater than y.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">Neither x nor y implements the
		/// <see cref="T:System.IComparable"></see> interface.-or- x and y are of different
		/// types and neither one can handle comparisons with the other. </exception>
		public int Compare(object x,object y)
		{
			ComparisonNoter?.ComparisonOccurred(); // for progress reporting.

			try
			{
				if (x == y)
				{
					return 0;
				}
				if (x == null)
				{
					return SubComparer is ReverseComparer ? 1 : -1;
				}
				if (y == null)
				{
					return SubComparer is ReverseComparer ? -1 : 1;
				}

				// We pass GetValue m_fSortedFromEnd, and it is responsible for taking care of flipping the string if that's needed.
				// The reason to do it there is because sometimes a sort key will have a homograph number appeneded to the end.
				// If we do the flipping here, the resulting string will be a backwards homograph number followed by the backwards
				// sort key itself.  GetValue should know enough to return the flipped string followed by a regular homograph number.
				var keysA = GetValue(x, SortedFromEnd);
				var keysB = GetValue(y, SortedFromEnd);

				// There will usually only be one element in the array, but just in case...
				if (SortedFromEnd)
				{
					Array.Reverse(keysA);
					Array.Reverse(keysB);
				}

				var cstrings = Math.Min(keysA.Length, keysB.Length);
				for (var i = 0; i < cstrings; i++)
				{
					// Sorted by length (if enabled) will be the primary sorting factor
					if (SortedByLength)
					{
						var cchA = OrthographicLength(keysA[i]);
						var cchB = OrthographicLength(keysB[i]);
						if (cchA < cchB)
						{
							return SubComparer is ReverseComparer ? 1 : -1;
						}
						if (cchB < cchA)
						{
							return SubComparer is ReverseComparer ? -1 : 1;
						}
					}
					// However, if there's no difference in length, we continue with the
					// rest of the sort
					var result = SubComparer.Compare(keysA[i], keysB[i]);
					if (result != 0)
					{
						return result;
					}
				}
				// All corresponding strings are equal according to the comparer, so sort based on the number of strings
				if (keysA.Length < keysB.Length)
				{
					return SubComparer is ReverseComparer ? 1 : -1;
				}
				if (keysA.Length > keysB.Length)
				{
					return SubComparer is ReverseComparer ? -1 : 1;
				}
				return 0;
			}
			catch (Exception error)
			{
				throw new Exception("Comparing objects failed", error);
			}
		}

		/// <summary>
		/// Count the number of orthographic characters (word-forming, nondiacritic) in the
		/// key.
		/// </summary>
		private int OrthographicLength(string key)
		{
			var cchOrtho = 0;
			var rgch = key.ToCharArray();
			for (var i = 0; i < rgch.Length; ++i)
			{
				// Handle surrogate pairs carefully!
				int ch;
				var ch1 = rgch[i];
				if (Surrogates.IsLeadSurrogate(ch1))
				{
					var ch2 = rgch[++i];
					ch = Surrogates.Int32FromSurrogates(ch1, ch2);
				}
				else
				{
					ch = ch1;
				}
				if (Icu.IsAlphabetic(ch))
				{
					++cchOrtho;		// Seems not to include UCHAR_DIACRITIC.
				}
				else
				{
					if (Icu.IsIdeographic(ch))
					{
						++cchOrtho;
					}
				}
			}
			return cchOrtho;
		}
		#endregion

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public void PersistAsXml(XElement node)
		{
			DynamicLoader.PersistObject(Finder, node, "finder");
			DynamicLoader.PersistObject(SubComparer, node, "comparer");
			if (SortedFromEnd)
			{
				XmlUtils.SetAttribute(node, "sortFromEnd", "true");
			}
			if (SortedByLength)
			{
				XmlUtils.SetAttribute(node, "sortByLength", "true");
			}
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public void InitXml(XElement node)
		{
			Finder = DynamicLoader.RestoreFromChild(node, "finder") as IStringFinder;
			SubComparer = DynamicLoader.RestoreFromChild(node, "comparer") as IComparer;
			SortedFromEnd = XmlUtils.GetOptionalBooleanAttributeValue(node, "sortFromEnd", false);
			SortedByLength = XmlUtils.GetOptionalBooleanAttributeValue(node, "sortByLength", false);
		}

		#endregion

		#region IStoresLcmCache members

		/// <summary>
		/// Given a cache, see whether your finder wants to know about it.
		/// </summary>
		public LcmCache Cache
		{
			set
			{
				if (Finder is IStoresLcmCache)
				{
					((IStoresLcmCache)Finder).Cache = value;
				}
				if (SubComparer is IStoresLcmCache)
				{
					((IStoresLcmCache)SubComparer).Cache = value;
				}
			}
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Clones the current object
		/// </summary>
		public object Clone()
		{
			return new StringFinderCompare
				{
					Finder = Finder,
					SortedByLength = SortedByLength,
					SortedFromEnd = SortedFromEnd,
					m_objToKey = m_objToKey.Clone() as Hashtable,
					SubComparer = SubComparer is ICloneable ? ((ICloneable)SubComparer).Clone() as IComparer : SubComparer,
					ComparisonNoter = ComparisonNoter
				};
		}
		#endregion

		/// <summary />
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			if (GetType() != obj.GetType())
			{
				return false;
			}
			var that = (StringFinderCompare)obj;
			if (Finder == null)
			{
				if (that.Finder != null)
				{
					return false;
				}
			}
			else
			{
				if (that.Finder == null)
				{
					return false;
				}
				if (!Finder.SameFinder(that.Finder))
				{
					return false;
				}
			}
			if (SortedByLength != that.SortedByLength)
			{
				return false;
			}
			if (SortedFromEnd != that.SortedFromEnd)
			{
				return false;
			}
			if (m_objToKey == null)
			{
				if (that.m_objToKey != null)
				{
					return false;
				}
			}
			else
			{
				if (m_objToKey.Count != that.m_objToKey?.Count)
				{
					return false;
				}
				var ie = that.m_objToKey.GetEnumerator();
				while (ie.MoveNext())
				{
					if (!m_objToKey.ContainsKey(ie.Key) || m_objToKey[ie.Key] != ie.Value)
					{
						return false;
					}
				}
			}

			if (SubComparer == null)
			{
				return that.SubComparer == null;
			}
			return SubComparer.Equals(that.SubComparer);
		}

		/// <summary />
		public override int GetHashCode()
		{
			var hash = GetType().GetHashCode();
			if (Finder != null)
			{
				hash += Finder.GetHashCode();
			}
			if (SortedByLength)
			{
				hash *= 3;
			}
			if (SortedFromEnd)
			{
				hash *= 17;
			}
			if (m_objToKey != null)
			{
				hash += m_objToKey.Count * 53;
			}
			if (SubComparer != null)
			{
				hash += SubComparer.GetHashCode();
			}
			return hash;
		}
	}

	/// <summary>
	/// This class reverses the polarity of another IComparer.
	/// Note especially the Reverse(IComparer) static function, which creates
	/// a ReverseComparer if necessary, but can also unwrap an existing one to retrieve
	/// the original comparer.
	/// </summary>
	public class ReverseComparer : IComparer, IPersistAsXml, IStoresLcmCache, IStoresDataAccess, ICloneable
	{
		/// <summary>
		/// normal constructor
		/// </summary>
		public ReverseComparer(IComparer comp)
		{
			SubComp = comp;
		}

		/// <summary>
		/// default for persistence
		/// </summary>
		public ReverseComparer()
		{
		}

		#region IComparer Members

		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
		public int Compare(object x, object y)
		{
			return -SubComp.Compare(x, y);
		}

		#endregion

		/// <summary>
		/// Gets the sub comp.
		/// </summary>
		public IComparer SubComp { get; private set; }

		/// <summary>
		/// Return a comparer with the opposite sense of comp. If it is itself a ReverseComparer,
		/// achieve this by unwrapping and returning the original comparer; otherwise, create
		/// a ReverseComparer.
		/// </summary>
		public static IComparer Reverse(IComparer comp)
		{
			var rc = comp as ReverseComparer;
			return rc == null ? new ReverseComparer(comp) : rc.SubComp;
		}

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public void PersistAsXml(XElement node)
		{
			DynamicLoader.PersistObject(SubComp, node, "comparer");
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public void InitXml(XElement node)
		{
			SubComp = DynamicLoader.RestoreFromChild(node, "comparer") as IComparer;
		}

		#endregion

		/// <summary />
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			if (GetType() != obj.GetType())
			{
				return false;
			}
			var that = (ReverseComparer)obj;
			return SubComp == null ? that.SubComp == null : that.SubComp != null && SubComp.Equals(that.SubComp);
		}

		/// <summary />
		public override int GetHashCode()
		{
			var hash = GetType().GetHashCode();
			if (SubComp != null)
			{
				hash *= SubComp.GetHashCode();
			}
			return hash;
		}

		public LcmCache Cache
		{
			set
			{
				if (SubComp is IStoresLcmCache)
				{
					((IStoresLcmCache)SubComp).Cache = value;
				}
			}
		}

		public ISilDataAccess DataAccess
		{
			set
			{
				if (SubComp is IStoresDataAccess)
				{
					((IStoresDataAccess)SubComp).DataAccess = value;
				}
			}
		}

		#region ICloneable Members
		/// <summary>
		/// Clones the current object
		/// </summary>
		public object Clone()
		{
			return new ReverseComparer(SubComp is ICloneable ? (IComparer)((ICloneable)SubComp).Clone() : SubComp);
		}
		#endregion
	}

	/// <summary>
	/// This class compares two integers represented as strings using integer comparison.
	/// </summary>
	public class IntStringComparer : IComparer, IPersistAsXml
	{
		#region IComparer Members

		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
		public int Compare(object x, object y)
		{
			var xn = int.Parse(x.ToString());
			var yn = int.Parse(y.ToString());
			if (xn < yn)
			{
				return -1;
			}
			if (xn > yn)
			{
				return 1;
			}
			return 0;
		}

		#endregion

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public void PersistAsXml(XElement node)
		{
			// nothing to do.
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public void InitXml(XElement node)
		{
			// Nothing to do
		}

		#endregion

		/// <summary />
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			return GetType() == obj.GetType();
		}

		/// <summary />
		public override int GetHashCode()
		{
			return GetType().GetHashCode();
		}
	}

	/// <summary />
	public class IcuComparer : IComparer, IPersistAsXml
	{
		/// <summary />
		protected ILgCollatingEngine m_lce;

		/// <summary>
		/// Key for the Hashtable is a string. Value is a byte[].
		/// </summary>
		protected Hashtable m_htskey = new Hashtable();

		/// <summary>
		/// Made accessible for testing.
		/// </summary>
		public string WsCode { get; protected set; }

		#region Constructors, etc.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IcuComparer(string sWs)
		{
			WsCode = sWs;
		}

		/// <summary>
		/// Default constructor for use with IPersistAsXml
		/// </summary>
		public IcuComparer(): this(null)
		{
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Opens the collating engine.
		/// </summary>
		/// ------------------------------------------------------------------------------------------
		public void OpenCollatingEngine()
		{
			if (m_lce == null)
			{
				m_lce = new ManagedLgIcuCollator();
			}
			else
			{
				m_lce.Close();
			}
			m_lce.Open(WsCode);
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the collating engine.
		/// </summary>
		/// ------------------------------------------------------------------------------------------
		public void CloseCollatingEngine()
		{
			if (m_lce != null)
			{
				m_lce.Close();
				//Marshal.ReleaseComObject(m_lce);
				var disposable = m_lce as IDisposable;
				if (disposable != null)
					disposable.Dispose();
				m_lce = null;
			}
			if (m_htskey != null)
				m_htskey.Clear();
		}
		#endregion

		#region IComparer Members
		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
		/// ------------------------------------------------------------------------------------------
		public int Compare(object x, object y)
		{
			string a = x as string;
			string b = y as string;
			if (a == b)
				return 0;
			if (a == null)
				return 1;
			if (b == null)
				return -1;
			byte[] ka = null;
			byte[] kb = null;
			if (m_lce != null)
			{
				object kaObj = m_htskey[a];
				if (kaObj != null)
				{
					ka = (byte[])kaObj;
				}
				else
				{
					ka = (byte[])m_lce.get_SortKeyVariant(a,
						LgCollatingOptions.fcoDefault);
					m_htskey.Add(a, ka);
				}
				object kbObj = m_htskey[b];
				if (kbObj != null)
				{
					kb = (byte[])kbObj;
				}
				else
				{
					kb = (byte[])m_lce.get_SortKeyVariant(b,
						LgCollatingOptions.fcoDefault);
					m_htskey.Add(b, kb);
				}
			}
			else
			{
				OpenCollatingEngine();
				ka = (byte[])m_lce.get_SortKeyVariant(a,
					LgCollatingOptions.fcoDefault);
				kb = (byte[])m_lce.get_SortKeyVariant(b,
					LgCollatingOptions.fcoDefault);
				CloseCollatingEngine();
			}
			// This is what m_lce.CompareVariant(ka,kb,...) would do.
			// Simulate strcmp on the two NUL-terminated byte strings.
			// This avoids marshalling back and forth.
			int nVal = 0;
			if (ka.Length == 0)
				nVal = -kb.Length; // zero if equal, neg if b is longer (considered larger)
			else if (kb.Length == 0)
				nVal = 1; // ka is longer and considered larger.
			else
			{
				// Normal case, null termination should be present.
				int ib;
				for (ib = 0; ka[ib] == kb[ib] && ka[ib] != 0; ++ib)
				{
					// skip merrily along until strings differ or end.
				}
				nVal = (int)(ka[ib] - kb[ib]);
			}
			return nVal;
		}
		#endregion

		#region IPersistAsXml Members

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public void PersistAsXml(XElement node)
		{
			XmlUtils.SetAttribute(node, "ws", WsCode);
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public void InitXml(XElement node)
		{
			WsCode = XmlUtils.GetMandatoryAttributeValue(node, "ws");
		}

		#endregion

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			if (this.GetType() != obj.GetType())
				return false;
			IcuComparer that = (IcuComparer)obj;
			if (m_htskey == null)
			{
				if (that.m_htskey != null)
					return false;
			}
			else
			{
				if (that.m_htskey == null)
					return false;
				if (this.m_htskey.Count != that.m_htskey.Count)
					return false;
				IDictionaryEnumerator ie = that.m_htskey.GetEnumerator();
				while (ie.MoveNext())
				{
					if (!m_htskey.ContainsKey(ie.Key) || m_htskey[ie.Key] != ie.Value)
						return false;
				}
			}
			if (this.m_lce != that.m_lce)
				return false;
			return this.WsCode == that.WsCode;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			int hash = GetType().GetHashCode();
			if (m_htskey != null)
				hash += m_htskey.Count * 53;
			if (m_lce != null)
				hash += m_lce.GetHashCode();
			if (WsCode != null)
				hash *= WsCode.GetHashCode();
			return hash;
		}
	}

	/// <summary>
	/// A comparer which uses the writing system collator to compare strings.
	/// </summary>
	public class WritingSystemComparer : IComparer, IPersistAsXml, IStoresLcmCache
	{
		private LcmCache m_cache;
		private CoreWritingSystemDefinition m_ws;

		#region Constructors, etc.
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		public WritingSystemComparer(CoreWritingSystemDefinition ws)
		{
			m_ws = ws;
			WsId = ws.Id;
		}

		/// <summary>
		/// Default constructor for use with IPersistAsXml
		/// </summary>
		public WritingSystemComparer()
		{
		}

		public LcmCache Cache
		{
			set { m_cache = value; }
		}

		#endregion

		public string WsId { get; private set; }

		#region IComparer Members
		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
		public int Compare(object x, object y)
		{
			if (m_ws == null)
			{
				m_ws = m_cache.ServiceLocator.WritingSystemManager.Get(WsId);
			}
			return m_ws.DefaultCollation.Collator.Compare(x, y);
		}
		#endregion

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public void PersistAsXml(XElement node)
		{
			XmlUtils.SetAttribute(node, "ws", WsId);
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public void InitXml(XElement node)
		{
			WsId = XmlUtils.GetMandatoryAttributeValue(node, "ws");
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			var comparer = obj as WritingSystemComparer;
			if (comparer == null)
			{
				return false;
			}
			return WsId == comparer.WsId;
		}

		public override int GetHashCode()
		{
			return WsId.GetHashCode();
		}
	}
}
