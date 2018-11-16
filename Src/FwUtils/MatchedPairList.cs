// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Encapsulates a list of MatchedPair objects.
	/// </summary>
	[XmlType("MatchedPairs")]
	public class MatchedPairList : List<MatchedPair>
	{
		private static bool s_sortAscending = true;

		/// <summary>
		/// Creates a MatchedPairList from the specified XML string.
		/// </summary>
		/// <param name="xmlSrc">The XML source string to load.</param>
		/// <param name="wsName">Name of the writing system (used for error reporting).</param>
		public static MatchedPairList Load(string xmlSrc, string wsName)
		{
			Exception e;
			var list = XmlSerializationHelper.DeserializeFromString<MatchedPairList>(xmlSrc, out e);
			if (e != null)
			{
				throw new ContinuableErrorException($"Invalid MatchedPairs field while loading the {wsName} writing system.", e);
			}

			return (list ?? new MatchedPairList());
		}

		/// <summary>
		/// Gets an XML string representing the list of matched pairs.
		/// </summary>
		public string XmlString => XmlSerializationHelper.SerializeToString(this);

		/// <summary>
		/// Determines whether or not the specified string is an opening or closing part of a
		/// matched pair.
		/// </summary>
		public bool BelongsToPair(string pairPart)
		{
			return (GetPairForOpen(pairPart) != null || GetPairForClose(pairPart) != null);
		}

		/// <summary>
		/// Determines whether or not the two specified strings are a matched pair.
		/// </summary>
		public bool IsMatchedPair(string open, string close)
		{
			var pair = GetPairForOpen(open);
			return (pair != null && pair.Close == close);
		}

		/// <summary>
		/// Gets the matched pair object for the specified opening part of a pair.
		/// </summary>
		public MatchedPair GetPairForOpen(string open)
		{
			foreach (var pair in this)
			{
				if (pair.Open == open)
				{
					return pair;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the matched pair object for the specified closing part of a pair.
		/// </summary>
		public MatchedPair GetPairForClose(string close)
		{
			foreach (var pair in this)
			{
				if (pair.Close == close)
				{
					return pair;
				}
			}

			return null;
		}

		/// <summary>
		/// Determines whether or not the specified string is the opening part of a pair.
		/// </summary>
		public bool IsOpen(string open)
		{
			return (GetPairForOpen(open) != null);
		}

		/// <summary>
		/// Determines whether or not the specified string is the opening part of a pair.
		/// </summary>
		public bool IsClose(string close)
		{
			return (GetPairForClose(close) != null);
		}

		/// <summary>
		/// Finds the specified pattern in the list and returns the associated PuncPattern
		/// object.
		/// </summary>
		public new MatchedPair this[int index] => index >= 0 && index < Count ? base[index] : null;

		#region Matched pairs sorting methods

		/// <summary>
		/// Sort the list of matched pairs using the specified order and comparer
		/// </summary>
		public void Sort(bool sortAscending, Comparison<MatchedPair> comparer)
		{
			s_sortAscending = sortAscending;
			Sort(comparer);
		}

		/// <summary>
		/// Compares matched pairs opening characters.
		/// </summary>
		public static int OpenComparer(MatchedPair x, MatchedPair y)
		{
			if ((x == null && y == null) || (string.IsNullOrEmpty(x.Open) && string.IsNullOrEmpty(y.Open)))
			{
				return 0;
			}
			if (string.IsNullOrEmpty(x.Open))
			{
				return (s_sortAscending ? -1 : 1);
			}
			if (y == null || string.IsNullOrEmpty(y.Open))
			{
				return (s_sortAscending ? 1 : -1);
			}
			return s_sortAscending ? x.Open.CompareTo(y.Open) : y.Open.CompareTo(x.Open);
		}

		/// <summary>
		/// Compares matched pairs closing characters.
		/// </summary>
		public static int CloseComparer(MatchedPair x, MatchedPair y)
		{
			if ((x == null && y == null) || (string.IsNullOrEmpty(x.Close) && string.IsNullOrEmpty(y.Close)))
			{
				return 0;
			}
			if (string.IsNullOrEmpty(x.Close))
			{
				return (s_sortAscending ? -1 : 1);
			}
			if (y == null || string.IsNullOrEmpty(y.Close))
			{
				return (s_sortAscending ? 1 : -1);
			}
			return s_sortAscending ? x.Close.CompareTo(y.Close) : y.Close.CompareTo(x.Close);
		}

		/// <summary>
		/// Compares matched pairs open character's codepoint.
		/// </summary>
		public static int OpenCodeComparer(MatchedPair x, MatchedPair y)
		{
			if (x == null && y == null || string.IsNullOrEmpty(x.Open) && string.IsNullOrEmpty(y.Open))
			{
				return 0;
			}
			if (string.IsNullOrEmpty(x.Open))
			{
				return (s_sortAscending ? -1 : 1);
			}
			if (y == null || string.IsNullOrEmpty(y.Open))
			{
				return (s_sortAscending ? 1 : -1);
			}
			return s_sortAscending ? x.Open[0].CompareTo(y.Open[0]) : y.Open[0].CompareTo(x.Open[0]);
		}

		/// <summary>
		/// Compares matched pairs close character's codepoint.
		/// </summary>
		public static int CloseCodeComparer(MatchedPair x, MatchedPair y)
		{
			if ((x == null && y == null) || (string.IsNullOrEmpty(x.Close) && string.IsNullOrEmpty(y.Close)))
			{
				return 0;
			}
			if (string.IsNullOrEmpty(x.Close))
			{
				return (s_sortAscending ? -1 : 1);
			}
			if (y == null || string.IsNullOrEmpty(y.Close))
			{
				return (s_sortAscending ? 1 : -1);
			}
			return s_sortAscending ? x.Close[0].CompareTo(y.Close[0]) : y.Close[0].CompareTo(x.Close[0]);
		}

		#endregion
	}
}