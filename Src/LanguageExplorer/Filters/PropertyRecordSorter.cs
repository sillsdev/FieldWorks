// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Diagnostics;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
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
}