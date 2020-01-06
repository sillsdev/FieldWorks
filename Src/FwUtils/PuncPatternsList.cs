// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Encapsulates a list of PuncPattern objects.
	/// </summary>
	[XmlType("PunctuationPatterns")]
	public class PuncPatternsList : List<PuncPattern>
	{
		private static bool s_sortAscending = true;

		/// <summary>
		/// Creates a PuncPatternsList from the specified XML string.
		/// </summary>
		/// <param name="xmlSrc">The XML source string to load.</param>
		/// <param name="wsName">Name of the writing system (used for error reporting).</param>
		public static PuncPatternsList Load(string xmlSrc, string wsName)
		{
			Exception e;
			var list = XmlSerializationHelper.DeserializeFromString<PuncPatternsList>(xmlSrc, out e);
			if (e != null)
			{
				throw new ContinuableErrorException($"Invalid PunctuationPatterns field while loading the {wsName} writing system.", e);
			}

			return list ?? new PuncPatternsList();
		}

		/// <summary>
		/// Finds the specified pattern in the list and returns the associated PuncPattern
		/// object.
		/// </summary>
		public PuncPattern this[string puncPattern]
		{
			get
			{
				foreach (var pattern in this)
				{
					if (pattern.Pattern == puncPattern)
					{
						return pattern;
					}
				}

				return null;
			}
		}

		/// <summary>
		/// Gets the PuncPattern with the specified index.
		/// </summary>
		public new PuncPattern this[int i] => (i < 0 || i >= Count ? null : base[i]);

		/// <summary>
		/// Returns a clone of the list of punctuation patterns.
		/// </summary>
		public PuncPatternsList Clone()
		{
			var clone = new PuncPatternsList();
			foreach (var pattern in this)
			{
				clone.Add(pattern.Clone());
			}
			return clone;
		}

		/// <summary>
		/// Gets an XML string representing the list of punctuation patterns.
		/// </summary>
		public string XmlString
		{
			get
			{
				// Only serialize those patterns with a valid or invalid status.
				var tmpList = new PuncPatternsList();
				foreach (var pattern in this)
				{
					if (pattern.Status != PuncPatternStatus.Unknown)
					{
						// Data in SQL strings should not contain a single quote.
						// Therefore, convert single quotes to ORCs, which will be
						// converted to &#x27 below.
						pattern.Pattern = pattern.Pattern.Replace("'", "\xFFFC");
						tmpList.Add(pattern);
					}
				}
				var xml = XmlSerializationHelper.SerializeToString(tmpList);
				// Replace ORCs with the hex codepoint value for single quote.
				return xml.Replace("\xFFFC", "&#x27;");
			}
		}

		#region PuncPattern sorting methods
		/// <summary>
		/// Sort the list of punctuation patterns using the specified order and comparer
		/// </summary>
		public void Sort(bool sortAscending, Comparison<PuncPattern> comparer)
		{
			s_sortAscending = sortAscending;
			Sort(comparer);
		}

		/// <summary>
		/// Compares punctuation pattern contexts.
		/// </summary>
		public static int ContextComparer(PuncPattern x, PuncPattern y)
		{
			if (x == null && y == null)
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
			return s_sortAscending ? x.ContextPos.CompareTo(y.ContextPos) : y.ContextPos.CompareTo(x.ContextPos);
		}

		/// <summary>
		/// Compares punctuation pattern counts.
		/// </summary>
		public static int CountComparer(PuncPattern x, PuncPattern y)
		{
			if (x == null && y == null)
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
			return s_sortAscending ? x.Count.CompareTo(y.Count) : y.Count.CompareTo(x.Count);
		}

		/// <summary>
		/// Compares punctuation pattern statuses.
		/// </summary>
		public static int StatusComparer(PuncPattern x, PuncPattern y)
		{
			if (x == null && y == null)
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
			return s_sortAscending ? x.Status.CompareTo(y.Status) : y.Status.CompareTo(x.Status);
		}

		#endregion
	}
}