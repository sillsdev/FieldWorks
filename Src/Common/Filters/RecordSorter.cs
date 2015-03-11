// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RecordSorter.cs
// History: John Hatton, created
// Last reviewed:
//
// <remarks>
//	At the moment (April 2004), the only concrete instance of this class (PropertyRecordSorter)
//		does sorting in memory,	based on a FDO property.
//	This does not imply that all sorting will always be done in memory, only that we haven't
//	yet designed or implemented a way to do the sorting while querying.
// </remarks>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Language;
using SIL.Utils;
using SIL.WritingSystems;

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
	/// sort (in memory) based on in FDO property
	/// </summary>
	public class PropertyRecordSorter : RecordSorter
	{
		private IComparer m_comp;
		/// <summary></summary>
		protected string m_propertyName;

		private FdoCache m_cache;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PropertyRecordSorter"/> class.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// ------------------------------------------------------------------------------------
		public PropertyRecordSorter(string propertyName)
		{
			Init(propertyName);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PropertyRecordSorter"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PropertyRecordSorter()
		{

		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the specified property name.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// ------------------------------------------------------------------------------------
		protected void Init(string propertyName)
		{
			m_propertyName = propertyName;
		}

		/// <summary>
		/// Add to the specified XML node information required to create a new
		/// record sorter equivalent to yourself.
		/// In this case we just need to add the sortProperty attribute.
		/// </summary>
		/// <param name="node"></param>
		public override void PersistAsXml(XmlNode node)
		{
			XmlAttribute xaSort = node.OwnerDocument.CreateAttribute("sortProperty");
			xaSort.Value = m_propertyName;
			node.Attributes.Append(xaSort);
		}

		/// <summary>
		/// Initialize an instance into the state indicated by the node, which was
		/// created by a call to PersistAsXml.
		/// </summary>
		/// <param name="node"></param>
		public override void InitXml(XmlNode node)
		{
			Init(node);
		}

		public override FdoCache Cache
		{
			set
			{
				m_cache = value;
				base.Cache = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the property.
		/// </summary>
		/// <value>The name of the property.</value>
		/// ------------------------------------------------------------------------------------
		public string PropertyName
		{
			get { return m_propertyName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the specified configuration.
		/// </summary>
		/// <param name="configuration">The configuration.</param>
		/// ------------------------------------------------------------------------------------
		protected void Init(XmlNode configuration)
		{
			m_propertyName =XmlUtils.GetManditoryAttributeValue(configuration, "sortProperty");
		}

		/// <summary>
		/// Get the object that does the comparisons.
		/// </summary>
		/// <returns></returns>
		protected internal override IComparer getComparer()
		{
			var fc = new FdoCompare(m_propertyName, m_cache);
			return fc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the specified records.
		/// </summary>
		/// <param name="records">The records.</param>
		/// ------------------------------------------------------------------------------------
		public override void Sort(/*ref*/ ArrayList records)
		{
#if DEBUG
			DateTime dt1 = DateTime.Now;
			int tc1 = Environment.TickCount;
#endif
			m_comp = getComparer();
			if (m_comp is FdoCompare)
			{
				//(m_comp as FdoCompare).Init();
				(m_comp as FdoCompare).ComparisonNoter = this;
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
			int tc2 = Environment.TickCount;
			TimeSpan ts1 = DateTime.Now - dt1;
			string s = "PropertyRecordSorter: Sorting " + records.Count + " records took " +
				(tc2 - tc1) + " ticks," + " or " + ts1.Minutes + ":" + ts1.Seconds + "." +
				ts1.Milliseconds.ToString("d3") + " min:sec.";
				Debug.WriteLine(s, RuntimeSwitches.RecordTimingSwitch.DisplayName);
			}
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the into.
		/// </summary>
		/// <param name="records">The records.</param>
		/// <param name="newRecords">The new records.</param>
		/// ------------------------------------------------------------------------------------
		public override void MergeInto(/*ref*/ ArrayList records, ArrayList newRecords)
		{
			RecordSorter.FdoCompare fc = (RecordSorter.FdoCompare) new RecordSorter.FdoCompare(m_propertyName, m_cache);
			MergeInto(records, newRecords, (IComparer) fc);
			fc.CloseCollatingEngine(); // Release the ICU data file.
		}

		/// <summary>
		/// a factory method for property sorders
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="configuration"></param>
		/// <returns></returns>
		static public PropertyRecordSorter Create(FdoCache cache, XmlNode configuration)
		{
			PropertyRecordSorter sorter = (PropertyRecordSorter)DynamicLoader.CreateObject(configuration);
			sorter. Init (configuration);
			return sorter;
		}
	}

	/// <summary>
	/// Abstract RecordSorter base class.
	/// </summary>
	public abstract class RecordSorter : IPersistAsXml, IStoresFdoCache, IAcceptsStringTable,
		IReportsSortProgress, INoteComparision
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
		public virtual void PersistAsXml(XmlNode node)
		{
		}

		/// <summary>
		/// Initialize an instance into the state indicated by the node, which was
		/// created by a call to PersistAsXml.
		/// </summary>
		/// <param name="node"></param>
		public virtual void InitXml(XmlNode node)
		{
		}

		#region IStoresFdoCache
		/// <summary>
		/// Set an FdoCache for anything that needs to know.
		/// </summary>
		public virtual FdoCache Cache
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
		#endregion IStoresFdoCache

		#region IAcceptsStringTable Members

		public virtual StringTable StringTable
		{
			set
			{
				// do nothing by default
			}
		}

		#endregion


		/// <summary>
		/// Method to retrieve the IComparer used by this sorter
		/// </summary>
		/// <returns>The IComparer</returns>
		protected internal abstract IComparer getComparer();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the specified records.
		/// </summary>
		/// <param name="records">The records.</param>
		/// ------------------------------------------------------------------------------------
		public abstract void Sort(/*ref*/ ArrayList records);
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the into.
		/// </summary>
		/// <param name="records">The records.</param>
		/// <param name="newRecords">The new records.</param>
		/// ------------------------------------------------------------------------------------
		public abstract void MergeInto(/*ref*/ ArrayList records, ArrayList newRecords);

		/// <summary>
		/// Merge the new records into the original list using the specified comparer
		/// </summary>
		/// <param name="records"></param>
		/// <param name="newRecords"></param>
		/// <param name="comparer"></param>
		protected void MergeInto(/*ref*/ ArrayList records, ArrayList newRecords, IComparer comparer)
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
		/// <param name="obj"></param>
		/// <param name="collector"></param>
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
		/// <param name="other"></param>
		/// <returns></returns>
		public virtual bool CompatibleSorter(RecordSorter other)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		///
		/// ------------------------------------------------------------------------------------
		protected class FdoCompare : IComparer, IPersistAsXml
		{
			/// <summary></summary>
			protected string m_propertyName;
			/// <summary></summary>
			protected ICollator m_collater;
			/// <summary></summary>
			protected bool m_fUseKeys = false;
			internal INoteComparision ComparisonNoter { get; set; }
			private Dictionary<object, string> m_sortKeyCache;

			private FdoCache m_cache;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:FdoCompare"/> class.
			/// </summary>
			/// <param name="propertyName">Name of the property.</param>
			/// --------------------------------------------------------------------------------
			public FdoCompare(string propertyName, FdoCache cache)
			{
				m_cache = cache;
				Init();
				m_propertyName= propertyName;
				m_fUseKeys = propertyName == "ShortName";
				m_sortKeyCache = new Dictionary<object, string>();
			}

			/// <summary>
			/// This constructor is intended to be used for persistence with IPersistAsXml
			/// </summary>
			public FdoCompare()
			{
				Init();
			}

			private void Init()
			{
				m_collater = null;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the name of the property.
			/// </summary>
			/// <value>The name of the property.</value>
			/// --------------------------------------------------------------------------------
			public string PropertyName
			{
				get
				{
					return m_propertyName;
				}
			}
			/// <summary>
			/// Add to the specified XML node information required to create a new
			/// object equivalent to yourself. The node already contains information
			/// sufficient to create an instance of the proper class.
			/// </summary>
			/// <param name="node"></param>
			public void PersistAsXml(XmlNode node)
			{
				XmlUtils.AppendAttribute(node, "property", m_propertyName);
			}

			/// <summary>
			/// Initialize an instance into the state indicated by the node, which was
			/// created by a call to PersistAsXml.
			/// </summary>
			/// <param name="node"></param>
			public void InitXml(XmlNode node)
			{
				m_propertyName = XmlUtils.GetManditoryAttributeValue(node, "property");
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the property.
			/// </summary>
			/// <param name="target">The target.</param>
			/// <param name="property">The property.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			protected object GetProperty(ICmObject target, string property)
			{
				Type type = target.GetType();
				System.Reflection.PropertyInfo info = type.GetProperty(property,
					System.Reflection.BindingFlags.Instance |
					System.Reflection.BindingFlags.Public |
					System.Reflection.BindingFlags.FlattenHierarchy );
				if (info == null)
					throw new ArgumentException("There is no public property named '"
						+ property + "' in " + type.ToString()
						+ ". Remember, properties often end in a multi-character suffix such as OA, OS, RA, RS, or Accessor.");

				return info.GetValue(target,null);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Opens the collating engine.
			/// </summary>
			/// <param name="sWs">The s ws.</param>
			/// --------------------------------------------------------------------------------
			public void OpenCollatingEngine(string sWs)
			{
				m_collater = m_cache.ServiceLocator.WritingSystemManager.Get(sWs).DefaultCollation.Collator;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Closes the collating engine.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void CloseCollatingEngine()
			{
			}

			ICmObject GetObjFromItem(object x)
			{
				IManyOnePathSortItem itemX = x as IManyOnePathSortItem;
				// This is slightly clumsy but currently it's the only way we have to get a cache.
				return itemX.KeyObjectUsing(m_cache);
			}

			// Compare two objects (expected to be ManyOnePathSortItems).
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
			/// </summary>
			/// <param name="x">The first object to compare.</param>
			/// <param name="y">The second object to compare.</param>
			/// <returns>
			/// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
			/// </returns>
			/// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
			/// --------------------------------------------------------------------------------
			public int Compare(object x, object y)
			{
				if (ComparisonNoter != null)
					ComparisonNoter.ComparisonOccurred(); // for progress reporting.

				if (x == y)
					return 0;
				if (x == null)
					return -1;
				if (y == null)
					return 1;

				// One time overhead
				if (m_collater == null)
					OpenCollatingEngine((GetObjFromItem(x)).SortKeyWs);

				//	string property = "ShortName";
				var a = GetObjFromCacheOrItem(x);
				var b = GetObjFromCacheOrItem(y);

				return m_collater.Compare(a, b);
			}

			private string GetObjFromCacheOrItem(object item)
			{
				var item1 = (ManyOnePathSortItem) item;
				string cachedKey;
				if (!m_sortKeyCache.TryGetValue(item1, out cachedKey))
				{
					if (m_fUseKeys)
						cachedKey = GetObjFromItem(item).SortKey;
					else
						cachedKey = (string) GetProperty(GetObjFromItem(item), m_propertyName);
					m_sortKeyCache.Add(item1, cachedKey);
				}
				return cachedKey;
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="obj"></param>
			/// <returns></returns>
			[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
				Justification="See TODO-Linux comment")]
			public override bool Equals(object obj)
			{
				if (obj == null)
					return false;
				// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
				// is marked with [MonoTODO] and might not work as expected in 4.0.
				if (this.GetType() != obj.GetType())
					return false;
				FdoCompare that = (FdoCompare)obj;
				if (this.m_fUseKeys != that.m_fUseKeys)
					return false;
				if (this.m_collater != that.m_collater)
					return false;
				if (this.m_propertyName != that.m_propertyName)
					return false;
				return true;
			}

			/// <summary>
			///
			/// </summary>
			/// <returns></returns>
			public override int GetHashCode()
			{
				int hash = GetType().GetHashCode();
				if (m_fUseKeys)
					hash *= 3;
				if (m_collater != null)
					hash += m_collater.GetHashCode();
				if (m_propertyName != null)
					hash *= m_propertyName.GetHashCode();
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
				return;
			m_comparisonsDone++;
			int newPercentDone = m_comparisonsDone*100/m_comparisonsEstimated;
			if (newPercentDone == m_percentDone)
				return;
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
			/// <param name="sorters">Reference to list of sorters</param>
			public AndSorterComparer(ArrayList sorters)
			{
				m_sorters = sorters;
				m_comps = new ArrayList();
				foreach (RecordSorter rs in m_sorters)
				{
					IComparer comp = rs.getComparer();
					if (comp is StringFinderCompare)
						(comp as StringFinderCompare).Init();
					m_comps.Add(comp);
				}
			}

			#region IComparer Members

			public int Compare(object x, object y)
			{
				int ret = 0;
				for (int i = 0; i < m_sorters.Count; i++)
				{
					ret = ((IComparer)m_comps[i]).Compare(x, y);
					if (ret != 0)
						break;
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
			/// <summary>
			///
			/// </summary>
			/// <param name="obj"></param>
			/// <returns></returns>
			[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
				Justification="See TODO-Linux comment")]
			public override bool Equals(object obj)
			{
				if (obj == null)
					return false;
				// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
				// is marked with [MonoTODO] and might not work as expected in 4.0.
				if (this.GetType() != obj.GetType())
					return false;
				var that = (AndSorterComparer)obj;
				if (m_comps == null)
				{
					if (that.m_comps != null)
						return false;
				}
				else
				{
					if (that.m_comps == null)
						return false;
					if (this.m_comps.Count != that.m_comps.Count)
						return false;
					for (int i = 0; i < m_comps.Count; ++i)
					{
						if (this.m_comps[i] != that.m_comps[i])
							return false;
					}
				}
				if (m_sorters == null)
				{
					if (that.m_sorters != null)
						return false;
				}
				else
				{
					if (that.m_sorters == null)
						return false;
					if (this.m_sorters.Count != that.m_sorters.Count)
						return false;
					for (int i = 0; i < m_sorters.Count; ++i)
					{
						if (this.m_sorters[i] != that.m_sorters[i])
							return false;
					}
				}
				return true;
			}

			/// <summary>
			///
			/// </summary>
			/// <returns></returns>
			public override int GetHashCode()
			{
				int hash = GetType().GetHashCode();
				if (m_comps != null)
					hash += m_comps.Count * 3;
				if (m_sorters != null)
					hash += m_sorters.Count * 17;
				return hash;
			}
		}

		/// <summary>
		/// references to sorters
		/// </summary>
		private ArrayList m_sorters = new ArrayList();

		public AndSorter() { }

		public AndSorter(ArrayList sorters): this()
		{
			foreach (RecordSorter rs in sorters)
				Add(rs);
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a sorter
		/// </summary>
		/// <param name="sorter">A reference to a sorter</param>
		/// ------------------------------------------------------------------------------------------
		public void Add(RecordSorter sorter)
		{
			m_sorters.Add(sorter);
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Replaces a sorter
		/// </summary>
		/// <param name="index">The index where we want to replace the sorter</param>
		/// <param name="oldSorter">A reference to the old sorter</param>
		/// <param name="newSorter">A reference to the new sorter</param>
		/// ------------------------------------------------------------------------------------------
		public void ReplaceAt(int index, RecordSorter oldSorter, RecordSorter newSorter)
		{
			if (index < 0 || index >= m_sorters.Count)
				throw new IndexOutOfRangeException();
			m_sorters[index] = newSorter;
		}

		/// <summary>
		/// Gets the list of sorters.
		/// </summary>
		public ArrayList Sorters
		{
			get
			{
				return m_sorters;
			}
		}

		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		public override void CollectItems(int hvo, ArrayList collector)
		{
			if (m_sorters.Count > 0)
				((RecordSorter)m_sorters[0]).CollectItems(hvo, collector);
			else
				base.CollectItems(hvo, collector);
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public override void PersistAsXml(XmlNode node)
		{
			base.PersistAsXml(node);
			foreach(RecordSorter rs in m_sorters)
				DynamicLoader.PersistObject(rs, node, "sorter");
		}

		// This will probably never be used, but for the sake of completeness, here it is
		protected internal override IComparer getComparer()
		{
			IComparer comp = new AndSorterComparer(m_sorters);
			return comp;
		}

		public override void Sort(/*ref*/ ArrayList records)
		{
#if DEBUG
			DateTime dt1 = DateTime.Now;
			int tc1 = Environment.TickCount;
#endif
			var comp = new AndSorterComparer(m_sorters);
			MergeSort.Sort(ref records, comp);

#if DEBUG
			// only do this if the timing switch is info or verbose
			if (RuntimeSwitches.RecordTimingSwitch.TraceInfo)
			{
			int tc2 = Environment.TickCount;
			TimeSpan ts1 = DateTime.Now - dt1;
			string s = "AndSorter:  Sorting " + records.Count + " records took " +
				(tc2 - tc1) + " ticks," + " or " + ts1.Minutes + ":" + ts1.Seconds + "." +
				ts1.Milliseconds.ToString("d3") + " min:sec.";
				Debug.WriteLine(s, RuntimeSwitches.RecordTimingSwitch.DisplayName);
			}
#endif
		}

		public override void MergeInto(ArrayList records, ArrayList newRecords)
		{
			var comp = new AndSorterComparer(m_sorters);
			MergeInto(records, newRecords, comp);
		}

		public override bool CompatibleSorter(RecordSorter other)
		{
			foreach (RecordSorter rs in m_sorters)
				if(rs.CompatibleSorter(other))
					return true;

			return false;
		}

		public int CompatibleSorterIndex(RecordSorter other)
		{
			for (int i = 0; i < m_sorters.Count; i++)
				if (((RecordSorter)m_sorters[i]).CompatibleSorter(other))
					return i;

			return -1;
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public override void InitXml(XmlNode node)
		{
			base.InitXml (node);
			m_sorters = new ArrayList(node.ChildNodes.Count);
			foreach (XmlNode child in node.ChildNodes)
			{
				var obj = DynamicLoader.RestoreFromChild(child, ".");
				m_sorters.Add(obj);
			}
		}

		public override FdoCache Cache
		{
			set
			{
				foreach (RecordSorter rs in m_sorters)
					rs.Cache = value;
			}
		}
	}

	/// <summary>
	/// A very general record sorter class, based on an arbitrary implementation of IComparer
	/// that can compare two FDO objects.
	/// </summary>
	public class GenRecordSorter : RecordSorter
	{
		private IComparer m_comp;

		/// <summary>
		/// Normal constructor.
		/// </summary>
		public GenRecordSorter(IComparer comp): this()
		{
			m_comp = comp;
		}

		/// <summary>
		/// Default constructor for IPersistAsXml
		/// </summary>
		public GenRecordSorter()
		{
		}

		protected internal override IComparer getComparer()
		{
			return m_comp;
		}

		/// <summary>
		/// See whether the comparer can preload. Currently we only know about one kind that can.
		/// </summary>
		public override void Preload(object rootObj)
		{
			base.Preload(rootObj);
			if (m_comp is StringFinderCompare)
				(m_comp as StringFinderCompare).Preload(rootObj);
		}

		/// <summary>
		/// Override to pass it on to the comparer, if relevant.
		/// </summary>
		public override ISilDataAccess DataAccess
		{
			set
			{
				base.DataAccess = value;
				if (m_comp is StringFinderCompare)
					((StringFinderCompare) m_comp).DataAccess = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if the other sorter is 'compatible' with this, in the sense that
		/// either they produce the same sort sequence, or one derived from it (e.g., by reversing).
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool CompatibleSorter(RecordSorter other)
		{
			GenRecordSorter grsOther = other as GenRecordSorter;
			if (grsOther == null)
				return false;
			if (CompatibleComparers(m_comp, grsOther.m_comp))
				return true;
			// Currently the only other kind of compatibility we know how to detect
			// is StringFinderCompares that do more-or-less the same thing.
			StringFinderCompare sfcThis = m_comp as StringFinderCompare;
			StringFinderCompare sfcOther = grsOther.Comparer as StringFinderCompare;
			if (sfcThis == null || sfcOther == null)
				return false;
			if (!sfcThis.Finder.SameFinder(sfcOther.Finder))
				return false;
			// We deliberately don't care if one has a ReverseCompare and the other
			// doesn't. That's handled by a different icon.
			IComparer subCompOther = UnpackReverseCompare(sfcOther);
			IComparer subCompThis = UnpackReverseCompare(sfcThis);
			return CompatibleComparers(subCompThis, subCompOther);
		}

		private IComparer UnpackReverseCompare(StringFinderCompare sfc)
		{
			IComparer subComp = sfc.SubComparer;
			if (subComp is ReverseComparer)
				subComp = (subComp as ReverseComparer).SubComp;
			return subComp;
		}

		/// <summary>
		/// Return true if the two comparers will give the same result. Ideally this would
		/// be an interface method on ICompare, but that interface is defined by .NET so we
		/// can't enhance it. This knows about a few interesting cases.
		/// </summary>
		/// <param name="first"></param>
		/// <param name="second"></param>
		/// <returns></returns>
		bool CompatibleComparers(IComparer first, IComparer second)
		{
			// identity
			if (first == second)
				return true;

			// IcuComparers on same Ws?
			IcuComparer firstIcu = first as IcuComparer;
			IcuComparer secondIcu = second as IcuComparer;
			if (firstIcu != null && secondIcu != null && firstIcu.WsCode == secondIcu.WsCode)
				return true;

			// WritingSystemComparers on same Ws?
			var firstWs = first as WritingSystemComparer;
			var secondWs = second as WritingSystemComparer;
			if (firstWs != null && secondWs != null && firstWs.WsId == secondWs.WsId)
				return true;

			// Both IntStringComparers?
			if (first is IntStringComparer && second is IntStringComparer)
				return true;

			// FdoComparers on the same property?
			FdoCompare firstFdo = first as FdoCompare;
			FdoCompare secondFdo = second as FdoCompare;
			if (firstFdo != null && secondFdo != null && firstFdo.PropertyName == secondFdo.PropertyName)
				return true;

			// not the same any way we know about
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the comparer.
		/// </summary>
		/// <value>The comparer.</value>
		/// ------------------------------------------------------------------------------------
		public IComparer Comparer
		{
			get
			{
				return m_comp;
			}
			set
			{
				m_comp = value;
			}
		}

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		public override void PersistAsXml(XmlNode node)
		{
			base.PersistAsXml (node); // does nothing, but in case needed later...
			IPersistAsXml persistComparer = m_comp as IPersistAsXml;
			if (persistComparer == null)
				throw new Exception("cannot persist GenRecSorter with comparer class " + m_comp.GetType().AssemblyQualifiedName);
			DynamicLoader.PersistObject(persistComparer, node, "comparer");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize an instance into the state indicated by the node, which was
		/// created by a call to PersistAsXml.
		/// </summary>
		/// <param name="node"></param>
		/// ------------------------------------------------------------------------------------
		public override void InitXml(XmlNode node)
		{
			base.InitXml (node);
			XmlNode compNode = node.ChildNodes[0];
			if (compNode.Name != "comparer")
				throw new Exception("persist info for GenRecordSorter must have comparer child element");
			m_comp = DynamicLoader.RestoreObject(compNode) as IComparer;
			if (m_comp == null)
				throw new Exception("restoring sorter failed...comparer does not implement IComparer");
		}

		/// <summary>
		/// Set an FdoCache for anything that needs to know.
		/// </summary>
		public override FdoCache Cache
		{
			set
			{
				if (m_comp is IStoresFdoCache)
					(m_comp as IStoresFdoCache).Cache = value;
			}
		}

		public override StringTable StringTable
		{
			set
			{
				if (m_comp is IAcceptsStringTable)
					(m_comp as IAcceptsStringTable).StringTable = value;
			}
		}

		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="collector"></param>
		public override void CollectItems(int hvo, ArrayList collector)
		{
			if (m_comp is StringFinderCompare)
			{
				(m_comp as StringFinderCompare).CollectItems(hvo, collector);
			}
			else
				base.CollectItems(hvo, collector);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the specified records.
		/// </summary>
		/// <param name="records">The records.</param>
		/// ------------------------------------------------------------------------------------
		public override void Sort(/*ref*/ ArrayList records)
		{
#if DEBUG
			DateTime dt1 = DateTime.Now;
			int tc1 = Environment.TickCount;
#endif
			if (m_comp is StringFinderCompare)
			{
				(m_comp as StringFinderCompare).Init();
				(m_comp as StringFinderCompare).ComparisonNoter = this;
				m_comparisonsDone = 0;
				m_percentDone = 0;
				// Make sure at least 1 so we don't divide by zero.
				m_comparisonsEstimated = Math.Max(records.Count*(int)Math.Ceiling(Math.Log(records.Count, 2.0)), 1);
			}

			//records.Sort(m_comp);
			MergeSort.Sort(ref records, m_comp);

			if (m_comp is StringFinderCompare)
				(m_comp as StringFinderCompare).Cleanup();
#if DEBUG
			// only do this if the timing switch is info or verbose
			if (RuntimeSwitches.RecordTimingSwitch.TraceInfo)
			{
			int tc2 = Environment.TickCount;
			TimeSpan ts1 = DateTime.Now - dt1;
			string s = "GenRecordSorter:  Sorting " + records.Count + " records took " +
				(tc2 - tc1) + " ticks," + " or " + ts1.Minutes + ":" + ts1.Seconds + "." +
				ts1.Milliseconds.ToString("d3") + " min:sec.";
				Debug.WriteLine(s, RuntimeSwitches.RecordTimingSwitch.DisplayName);
			}
#endif
		}
		/// <summary>
		/// Required implementation.
		/// </summary>
		/// <param name="records"></param>
		/// <param name="newRecords"></param>
		public override void MergeInto(/*ref*/ ArrayList records, ArrayList newRecords)
		{
			if (m_comp is StringFinderCompare)
				(m_comp as StringFinderCompare).Init();

			//records.Sort(m_comp);
			MergeInto(records, newRecords, m_comp);

			if (m_comp is StringFinderCompare)
				(m_comp as StringFinderCompare).Cleanup();
		}

		/// <summary>
		/// Check whether this GenRecordSorter is equal to another object.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			if (this.GetType() != obj.GetType())
				return false;
			GenRecordSorter that = (GenRecordSorter)obj;
			if (m_comp == null)
			{
				if (that.m_comp != null)
					return false;
			}
			else
			{
				if (that.m_comp == null)
					return false;
				// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
				// is marked with [MonoTODO] and might not work as expected in 4.0.
				if (this.m_comp.GetType() != that.m_comp.GetType())
					return false;
				else
					return this.m_comp.Equals(that.m_comp);
			}
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			int hash = GetType().GetHashCode();
			if (m_comp != null)
				hash *= m_comp.GetHashCode();
			return hash;
		}
	}

	/// <summary>
	/// This class compares two ManyOnePathSortItems by making use of the ability of a StringFinder
	/// object to obtain strings from the KeyObject hvo, then
	/// a (simpler) IComparer to compare the strings.
	/// </summary>
	public class StringFinderCompare : IComparer, IPersistAsXml, IStoresFdoCache, IStoresDataAccess,
		IAcceptsStringTable, ICloneable
	{
		/// <summary></summary>
		protected IComparer m_subComp;
		/// <summary></summary>
		protected IStringFinder m_finder;
		/// <summary></summary>
		protected bool m_fSortedFromEnd;
		/// <summary></summary>
		protected bool m_fSortedByLength;
		// This is used, during a single sort, to cache keys.
		/// <summary></summary>
		protected Hashtable m_objToKey = new Hashtable();
		internal INoteComparision ComparisonNoter { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:StringFinderCompare"/> class.
		/// </summary>
		/// <param name="finder">The finder.</param>
		/// <param name="subComp">The sub comp.</param>
		/// ------------------------------------------------------------------------------------
		public StringFinderCompare(IStringFinder finder, IComparer subComp)
		{
			m_finder = finder;
			m_subComp = subComp;
			m_fSortedFromEnd = false;
			m_fSortedByLength = false;
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		public StringFinderCompare(): this(null, null)
		{
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the finder.
		/// </summary>
		/// <value>The finder.</value>
		/// ------------------------------------------------------------------------------------
		public IStringFinder Finder
		{
			get
			{
				return m_finder;
			}
		}

		public ISilDataAccess DataAccess
		{
			set
			{
				if (m_finder != null)
					m_finder.DataAccess = value;
			}
		}

		/// <summary>
		/// May be implemented to preload before sorting large collections (typically most of the instances).
		/// Currently will not be called if the column is also filtered; typically the same Preload() would end
		/// up being done.
		/// </summary>
		/// <param name="rootObj">Typically a CmObject, the root object of the whole view we are sorting for</param>
		public virtual void Preload(object rootObj)
		{
			m_finder.Preload(rootObj);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sub comparer.
		/// </summary>
		/// <value>The sub comparer.</value>
		/// ------------------------------------------------------------------------------------
		public IComparer SubComparer
		{
			get
			{
				return m_subComp;
			}
		}

		/// <summary>
		/// Copy our comparer's SubComparer and SortedFromEnd to another comparer.
		/// </summary>
		/// <param name="copyComparer"></param>
		public void CopyTo(StringFinderCompare copyComparer)
		{
			copyComparer.m_subComp = this.m_subComp is ICloneable ? ((ICloneable)m_subComp).Clone() as IComparer : m_subComp;
			copyComparer.m_fSortedFromEnd = this.m_fSortedFromEnd;
			copyComparer.m_fSortedByLength = this.m_fSortedByLength;
		}

		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="collector"></param>
		public void CollectItems(int hvo, ArrayList collector)
		{
			m_finder.CollectItems(hvo, collector);
		}

		/// <summary>
		/// Give the sort order. Considered to be ascending unless it has been reversed.
		/// </summary>
		public SortOrder Order
		{
			get
			{
				return m_subComp is ReverseComparer ?
					SortOrder.Descending : SortOrder.Ascending;
			}
		}

		/// <summary>
		/// Flag whether to sort normally from the beginnings of words, or to sort from the
		/// ends of words.  This is useful for grouping words by suffix.
		/// </summary>
		public bool SortedFromEnd
		{
			get
			{
				return m_fSortedFromEnd;
			}
			set
			{
				m_fSortedFromEnd = value;
			}
		}

		/// <summary>
		/// Flag whether to sort normally from the beginnings of words, or to sort from the
		/// ends of words.  This is useful for grouping words by suffix.
		/// </summary>
		public bool SortedByLength
		{
			get
			{
				return m_fSortedByLength;
			}
			set
			{
				m_fSortedByLength = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Init()
		{
			m_objToKey.Clear();
			if (m_subComp is IcuComparer)
			{
				(m_subComp as IcuComparer).OpenCollatingEngine();
			}
			else if (m_subComp is ReverseComparer)
			{
				IComparer ct = ReverseComparer.Reverse(m_subComp);
				if (ct is IcuComparer)
					(ct as IcuComparer).OpenCollatingEngine();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleanups this instance. Note that clients are not obliged to call this; it just
		/// helps a little with garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Cleanup()
		{
			m_objToKey.Clear(); // redundant, but may help free memory.
			if (m_subComp is IcuComparer)
			{
				(m_subComp as IcuComparer).CloseCollatingEngine();
			}
			else if (m_subComp is ReverseComparer)
			{
				IComparer ct = ReverseComparer.Reverse(m_subComp);
				if (ct is IcuComparer)
					(ct as IcuComparer).CloseCollatingEngine();
			}
		}

		/// <summary>
		/// Reverse the order of the sort.
		/// </summary>
		public void Reverse()
		{
			m_subComp = ReverseComparer.Reverse(m_subComp);
		}

		string[] GetValue(object key, bool sortedFromEnd)
		{
			try
			{
				string[] result = m_objToKey[key] as string[];
				if (result != null)
					return result;
				var item = key as IManyOnePathSortItem;

				result =  m_finder.SortStrings(item, sortedFromEnd);
				m_objToKey[key] = result;
				return result;
			}
			catch (Exception e)
			{
				throw new Exception("StringFinderCompare could not get key for " + key, e);
			}
		}

		#region IComparer Members

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		public int Compare(object x,object y)
		{
			if (ComparisonNoter != null)
				ComparisonNoter.ComparisonOccurred(); // for progress reporting.

			try
			{
				if (x == y)
					return 0;
				if (x == null)
					return m_subComp is ReverseComparer ? 1 : -1;
				if (y == null)
					return m_subComp is ReverseComparer ? -1 : 1;

				// We pass GetValue m_fSortedFromEnd, and it is responsible for taking care of flipping the string if that's needed.
				// The reason to do it there is because sometimes a sort key will have a homograph number appeneded to the end.
				// If we do the flipping here, the resulting string will be a backwards homograph number followed by the backwards
				// sort key itself.  GetValue should know enough to return the flipped string followed by a regular homograph number.
				string[] keysA = GetValue(x, m_fSortedFromEnd);
				string[] keysB = GetValue(y, m_fSortedFromEnd);

				// There will usually only be one element in the array, but just in case...
				if (m_fSortedFromEnd)
				{
					Array.Reverse(keysA);
					Array.Reverse(keysB);
				}

				int cstrings = Math.Min(keysA.Length, keysB.Length);
				for (int i = 0; i < cstrings; i++)
				{
					// Sorted by length (if enabled) will be the primary sorting factor
					if (m_fSortedByLength)
					{
						int cchA = OrthographicLength(keysA[i]);
						int cchB = OrthographicLength(keysB[i]);
						if (cchA < cchB)
							return m_subComp is ReverseComparer ? 1 : -1;
						else if (cchB < cchA)
							return m_subComp is ReverseComparer ? -1 : 1;
					}
					// However, if there's no difference in length, we continue with the
					// rest of the sort
					int result = m_subComp.Compare(keysA[i], keysB[i]);
					if (result != 0)
						return result;
				}
				// All corresponding strings are equal according to the comparer, so sort based on the number of strings
				if (keysA.Length < keysB.Length)
					return m_subComp is ReverseComparer ? 1 : -1;
				else if (keysA.Length > keysB.Length)
					return m_subComp is ReverseComparer ? -1 : 1;
				else return 0;
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
		/// <param name="key"></param>
		/// <returns></returns>
		private int OrthographicLength(string key)
		{
			int cchOrtho = 0;
			char[] rgch = key.ToCharArray();
			for (int i = 0; i < rgch.Length; ++i)
			{
				// Handle surrogate pairs carefully!
				int ch;
				char ch1 = rgch[i];
				if (Surrogates.IsLeadSurrogate(ch1))
				{
					char ch2 = rgch[++i];
					ch = Surrogates.Int32FromSurrogates(ch1, ch2);
				}
				else
				{
					ch = (int)ch1;
				}
				if (Icu.IsAlphabetic(ch))
				{
					++cchOrtho;		// Seems not to include UCHAR_DIACRITIC.
				}
				else
				{
					if (Icu.IsIdeographic(ch))
						++cchOrtho;
				}
			}
			return cchOrtho;
		}
		#endregion

		#region IPersistAsXml Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public void PersistAsXml(XmlNode node)
		{
			DynamicLoader.PersistObject(m_finder, node, "finder");
			DynamicLoader.PersistObject(m_subComp, node, "comparer");
			if (m_fSortedFromEnd)
				XmlUtils.AppendAttribute(node, "sortFromEnd", "true");
			if (m_fSortedByLength)
				XmlUtils.AppendAttribute(node, "sortByLength", "true");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public void InitXml(XmlNode node)
		{
			m_finder = DynamicLoader.RestoreFromChild(node, "finder") as IStringFinder;
			m_subComp = DynamicLoader.RestoreFromChild(node, "comparer") as IComparer;
			m_fSortedFromEnd = XmlUtils.GetOptionalBooleanAttributeValue(node, "sortFromEnd", false);
			m_fSortedByLength = XmlUtils.GetOptionalBooleanAttributeValue(node, "sortByLength", false);
		}

		#endregion

		#region IStoresFdoCache members

		/// <summary>
		/// Given a cache, see whether your finder wants to know about it.
		/// </summary>
		public FdoCache Cache
		{
			set
			{
				if (m_finder is IStoresFdoCache)
					((IStoresFdoCache) m_finder).Cache = value;
				if (m_subComp is IStoresFdoCache)
					((IStoresFdoCache) m_subComp).Cache = value;
			}
		}
		#endregion

		#region IAcceptsStringTable

		public StringTable StringTable
		{
			set
			{
				if (m_finder is IAcceptsStringTable)
					((IAcceptsStringTable) m_finder).StringTable = value;
				if (m_subComp is IAcceptsStringTable)
					((IAcceptsStringTable) m_subComp).StringTable = value;
			}
		}

		#endregion IAcceptsStringTable

		#region ICloneable Members
		/// <summary>
		/// Clones the current object
		/// </summary>
		public object Clone()
		{
			return new StringFinderCompare
				{
					m_finder = this.m_finder,
					m_fSortedByLength = this.m_fSortedByLength,
					m_fSortedFromEnd = this.m_fSortedFromEnd,
					m_objToKey = this.m_objToKey.Clone() as Hashtable,
					m_subComp = this.m_subComp is ICloneable ? ((ICloneable)m_subComp).Clone() as IComparer : m_subComp,
					ComparisonNoter = this.ComparisonNoter
				};
		}
		#endregion

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			if (this.GetType() != obj.GetType())
				return false;
			StringFinderCompare that = (StringFinderCompare)obj;
			if (m_finder == null)
			{
				if (that.m_finder != null)
					return false;
			}
			else
			{
				if (that.m_finder == null)
					return false;
				if (!this.m_finder.SameFinder(that.m_finder))
					return false;
			}
			if (this.m_fSortedByLength != that.m_fSortedByLength)
				return false;
			if (this.m_fSortedFromEnd != that.m_fSortedFromEnd)
				return false;
			if (m_objToKey == null)
			{
				if (that.m_objToKey != null)
					return false;
			}
			else
			{
				if (that.m_objToKey == null)
					return false;
				if (this.m_objToKey.Count != that.m_objToKey.Count)
					return false;
				IDictionaryEnumerator ie = that.m_objToKey.GetEnumerator();
				while (ie.MoveNext())
				{
					if (!m_objToKey.ContainsKey(ie.Key) || m_objToKey[ie.Key] != ie.Value)
						return false;
				}
			}
			if (m_subComp == null)
				return that.m_subComp == null;
			else
				return this.m_subComp.Equals(that.m_subComp);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			int hash = GetType().GetHashCode();
			if (m_finder != null)
				hash += m_finder.GetHashCode();
			if (m_fSortedByLength)
				hash *= 3;
			if (m_fSortedFromEnd)
				hash *= 17;
			if (m_objToKey != null)
				hash += m_objToKey.Count * 53;
			if (m_subComp != null)
				hash += m_subComp.GetHashCode();
			return hash;
		}
	}

	/// <summary>
	/// This class reverses the polarity of another IComparer.
	/// Note especially the Reverse(IComparer) static function, which creates
	/// a ReverseComparer if necessary, but can also unwrap an existing one to retrieve
	/// the original comparer.
	/// </summary>
	public class ReverseComparer : IComparer, IPersistAsXml, IStoresFdoCache, IStoresDataAccess, ICloneable
	{
		/// <summary>
		/// Reference to comparer
		/// </summary>
		private IComparer m_comp;

		/// <summary>
		/// normal constructor
		/// </summary>
		/// <param name="comp">Reference to a comparer</param>
		public ReverseComparer(IComparer comp)
		{
			m_comp = comp;
		}

		/// <summary>
		/// default for persistence
		/// </summary>
		public ReverseComparer()
		{
		}

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
			return -m_comp.Compare(x, y);
		}

		#endregion

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sub comp.
		/// </summary>
		/// <value>The sub comp.</value>
		/// ------------------------------------------------------------------------------------------
		public IComparer SubComp
		{
			get
			{
				return m_comp;
			}
		}

		/// <summary>
		/// Return a comparer with the opposite sense of comp. If it is itself a ReverseComparer,
		/// achieve this by unwrapping and returning the original comparer; otherwise, create
		/// a ReverseComparer.
		/// </summary>
		/// <param name="comp"></param>
		/// <returns></returns>
		public static IComparer Reverse(IComparer comp)
		{
			var rc = comp as ReverseComparer;
			return rc == null ? new ReverseComparer(comp) : rc.SubComp;
		}

		#region IPersistAsXml Members

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public void PersistAsXml(XmlNode node)
		{
			DynamicLoader.PersistObject(m_comp, node, "comparer");
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public void InitXml(XmlNode node)
		{
			m_comp = DynamicLoader.RestoreFromChild(node, "comparer") as IComparer;
		}

		#endregion

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			if (this.GetType() != obj.GetType())
				return false;
			var that = (ReverseComparer)obj;
			if (m_comp == null)
				return that.m_comp == null;
			if (that.m_comp == null)
				return false;
			return m_comp.Equals(that.m_comp);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			int hash = GetType().GetHashCode();
			if (m_comp != null)
				hash *= m_comp.GetHashCode();
			return hash;
		}

		public FdoCache Cache
		{
			set
			{
				if (m_comp is IStoresFdoCache)
					((IStoresFdoCache) m_comp).Cache = value;
			}
		}

		public ISilDataAccess DataAccess
		{
			set
			{
				if (m_comp is IStoresDataAccess)
					((IStoresDataAccess)m_comp).DataAccess = value;
			}
		}

		#region ICloneable Members
		/// <summary>
		/// Clones the current object
		/// </summary>
		public object Clone()
		{
			var comp = m_comp is ICloneable ? (IComparer)((ICloneable)m_comp).Clone() : m_comp;
			return new ReverseComparer(comp);
		}
		#endregion
	}

	/// <summary>
	/// This class compares two integers represented as strings using integer comparison.
	/// </summary>
	public class IntStringComparer : IComparer, IPersistAsXml
	{
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
			int xn = Int32.Parse(x.ToString());
			int yn = Int32.Parse(y.ToString());
			if (xn < yn)
				return -1;
			else if (xn > yn)
				return 1;
			else
				return 0;
		}

		#endregion

		#region IPersistAsXml Members

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public void PersistAsXml(XmlNode node)
		{
			// nothing to do.
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public void InitXml(XmlNode node)
		{
			// Nothing to do
		}

		#endregion

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			return this.GetType() == obj.GetType();
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return GetType().GetHashCode();
		}
	}

	/// -------------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// -------------------------------------------------------------------------------------------
	public class IcuComparer : IComparer, IPersistAsXml
	{
		/// <summary></summary>
		protected ILgCollatingEngine m_lce;
		/// <summary></summary>
		protected string m_sWs;
		// Key for the Hashtable is a string.
		// Value is a byte[].
		/// <summary></summary>
		protected Hashtable m_htskey = new Hashtable();

		/// <summary>
		/// Made accessible for testing.
		/// </summary>
		public string WsCode
		{
			get { return m_sWs; }
		}

		#region Constructors, etc.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IcuComparer(string sWs)
		{
			m_sWs = sWs;
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
			m_lce.Open(m_sWs);
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
		public void PersistAsXml(XmlNode node)
		{
			XmlUtils.AppendAttribute(node, "ws", m_sWs);
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------------
		public void InitXml(XmlNode node)
		{
			m_sWs = XmlUtils.GetManditoryAttributeValue(node, "ws");
		}

		#endregion

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
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
			return this.m_sWs == that.m_sWs;
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
			if (m_sWs != null)
				hash *= m_sWs.GetHashCode();
			return hash;
		}
	}

	/// <summary>
	/// A comparer which uses the writing system collator to compare strings.
	/// </summary>
	public class WritingSystemComparer : IComparer, IPersistAsXml, IStoresFdoCache
	{
		private string m_wsId;
		private FdoCache m_cache;
		private CoreWritingSystemDefinition m_ws;

		#region Constructors, etc.
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		public WritingSystemComparer(CoreWritingSystemDefinition ws)
		{
			m_ws = ws;
			m_wsId = ws.Id;
		}

		/// <summary>
		/// Default constructor for use with IPersistAsXml
		/// </summary>
		public WritingSystemComparer()
		{
		}

		public FdoCache Cache
		{
			set { m_cache = value; }
		}

		#endregion

		public string WsId
		{
			get
			{
				return m_wsId;
			}
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
			if (m_ws == null)
				m_ws = m_cache.ServiceLocator.WritingSystemManager.Get(m_wsId);
			return m_ws.DefaultCollation.Collator.Compare(x, y);
		}
		#endregion

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		public void PersistAsXml(XmlNode node)
		{
			XmlUtils.AppendAttribute(node, "ws", m_wsId);
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		public void InitXml(XmlNode node)
		{
			m_wsId = XmlUtils.GetManditoryAttributeValue(node, "ws");
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");
			var comparer = obj as WritingSystemComparer;
			if (comparer == null)
				return false;
			return m_wsId == comparer.m_wsId;
		}

		public override int GetHashCode()
		{
			return m_wsId.GetHashCode();
		}
	}
}
