// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// sort (in memory) based on in LCM property
	/// </summary>
	internal sealed class PropertyRecordSorter : RecordSorter
	{
		private IComparer _comparer;
		private LcmCache _cache;

		/// <summary />
		internal PropertyRecordSorter(string propertyName)
		{
			PropertyName = propertyName;
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		internal PropertyRecordSorter(XElement element)
			: this(XmlUtils.GetMandatoryAttributeValue(element, "sortProperty"))
		{
		}

		/// <summary>
		/// Add to the specified XML node information required to create a new
		/// record sorter equivalent to yourself.
		/// In this case we just need to add the sortProperty attribute.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			element.Add(new XAttribute("sortProperty", PropertyName));
		}

		public override LcmCache Cache
		{
			set
			{
				_cache = value;
				base.Cache = value;
			}
		}

		/// <summary>
		/// Gets the name of the property.
		/// </summary>
		internal string PropertyName { get; }

		/// <summary>
		/// Get the object that does the comparisons.
		/// </summary>
		public override IComparer Comparer => _comparer ?? (_comparer = new LcmCompare(PropertyName, _cache));

		/// <summary>
		/// Sorts the specified records.
		/// </summary>
		public override void Sort(List<IManyOnePathSortItem> records)
		{
#if DEBUG
			var dt1 = DateTime.Now;
			var tc1 = Environment.TickCount;
#endif
			if (Comparer is LcmCompare lcmCompare)
			{
				lcmCompare.ComparisonNoter = this;
				m_comparisonsDone = 0;
				m_percentDone = 0;
				// Make sure at least 1 so we don't divide by zero.
				m_comparisonsEstimated = Math.Max(records.Count * (int)Math.Ceiling(Math.Log(records.Count, 2.0)), 1);
			}
			records.Sort(_comparer);
#if DEBUG
			// only do this if the timing switch is info or verbose
			if (RuntimeSwitches.RecordTimingSwitch.TraceInfo)
			{
				var ts1 = DateTime.Now - dt1;
				Debug.WriteLine($"PropertyRecordSorter: Sorting {records.Count} records took {Environment.TickCount - tc1} ticks, or {ts1.Minutes}:{ts1.Seconds}.{ts1.Milliseconds:d3} min:sec.", RuntimeSwitches.RecordTimingSwitch.DisplayName);
			}
#endif
		}

		/// <summary>
		/// Merges the into.
		/// </summary>
		public override void MergeInto(/*ref*/ List<IManyOnePathSortItem> records, List<IManyOnePathSortItem> newRecords)
		{
			var fc = new LcmCompare(PropertyName, _cache);
			MergeInto(records, newRecords, fc);
			fc.CloseCollatingEngine(); // Release the ICU data file.
		}
	}
}