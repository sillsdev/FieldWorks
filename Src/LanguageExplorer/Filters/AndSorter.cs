// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Filters
{
	public class AndSorter : RecordSorter
	{
		public AndSorter() { }

		public AndSorter(List<RecordSorter> sorters) : this()
		{
			foreach (var recordSorter in sorters)
			{
				Add(recordSorter);
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
		public List<RecordSorter> Sorters { get; private set; } = new List<RecordSorter>();

		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		public override void CollectItems(int hvo, List<IManyOnePathSortItem> collector)
		{
			if (Sorters.Count > 0)
			{
				Sorters[0].CollectItems(hvo, collector);
			}
			else
			{
				base.CollectItems(hvo, collector);
			}
		}

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			base.PersistAsXml(element);
			foreach (var rs in Sorters)
			{
				DynamicLoader.PersistObject(rs, element, "sorter");
			}
		}

		// This will probably never be used, but for the sake of completeness, here it is
		protected internal override IComparer Comparer => new AndSorterComparer(Sorters);

		public override void Sort(List<IManyOnePathSortItem> records)
		{
#if DEBUG
			var dt1 = DateTime.Now;
			var tc1 = Environment.TickCount;
#endif
			var comp = new AndSorterComparer(Sorters);
			records.Sort(comp);

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

		public override void MergeInto(List<IManyOnePathSortItem> records, List<IManyOnePathSortItem> newRecords)
		{
			var comp = new AndSorterComparer(Sorters);
			MergeInto(records, newRecords, comp);
		}

		public override bool CompatibleSorter(RecordSorter other)
		{
			foreach (var recordSorter in Sorters)
			{
				if (recordSorter.CompatibleSorter(other))
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
				if (Sorters[i].CompatibleSorter(other))
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement element)
		{
			base.InitXml(element);
			Sorters = new List<RecordSorter>(element.Elements().Count());
			foreach (var child in element.Elements())
			{
				Sorters.Add((RecordSorter)DynamicLoader.RestoreFromChild(child, "."));
			}
		}

		public override LcmCache Cache
		{
			set
			{
				foreach (var recordSorter in Sorters)
				{
					recordSorter.Cache = value;
				}
			}
		}

		private sealed class AndSorterComparer : IComparer, ICloneable
		{
			private List<RecordSorter> m_sorters;
			private List<IComparer> m_comps;

			/// <summary>
			/// Creates a new AndSortComparer
			/// </summary>
			public AndSorterComparer(List<RecordSorter> sorters)
			{
				m_sorters = sorters;
				m_comps = new List<IComparer>();
				foreach (var recordSorter in m_sorters)
				{
					var comp = recordSorter.Comparer;
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
					ret = m_comps[i].Compare(x, y);
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
	}
}