using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Windows.Forms;
using SIL.Utils;

namespace SILUBS.SharedScrUtils
{
	#region MatchedPairList class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Encapsulates a list of MatchedPair objects.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("MatchedPairs")]
	public class MatchedPairList : List<MatchedPair>
	{
		private static SortOrder s_currSortOrder = SortOrder.Ascending;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a MatchedPairList from the specified XML string.
		/// </summary>
		/// <param name="xmlSrc">The XML source string to load.</param>
		/// <param name="wsName">Name of the writing system (used for error reporting).</param>
		/// ------------------------------------------------------------------------------------
		public static MatchedPairList Load(string xmlSrc, string wsName)
		{
			Exception e;
			MatchedPairList list = XmlSerializationHelper.DeserializeFromString<MatchedPairList>(xmlSrc, out e);
			if (e != null)
				throw new ContinuableErrorException("Invalid MatchedPairs field while loading the " +
					wsName + " writing system.", e);
			return (list ?? new MatchedPairList());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an XML string representing the list of matched pairs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string XmlString
		{
			get { return XmlSerializationHelper.SerializeToString<MatchedPairList>(this); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified string is an opening or closing part of a
		/// matched pair.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool BelongsToPair(string pairPart)
		{
			return (GetPairForOpen(pairPart) != null || GetPairForClose(pairPart) != null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the two specified strings are a matched pair.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsMatchedPair(string open, string close)
		{
			MatchedPair pair = GetPairForOpen(open);
			return (pair != null && pair.Close == close);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the matched pair object for the specified opening part of a pair.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MatchedPair GetPairForOpen(string open)
		{
			foreach (MatchedPair pair in this)
			{
				if (pair.Open == open)
					return pair;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the matched pair object for the specified closing part of a pair.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MatchedPair GetPairForClose(string close)
		{
			foreach (MatchedPair pair in this)
			{
				if (pair.Close == close)
					return pair;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified string is the opening part of a pair.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsOpen(string open)
		{
			return (GetPairForOpen(open) != null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified string is the opening part of a pair.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsClose(string close)
		{
			return (GetPairForClose(close) != null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the specified pattern in the list and returns the associated PuncPattern
		/// object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new MatchedPair this[int index]
		{
			get { return (index >= 0 && index < Count) ? base[index] : null; }
		}

		#region Matched pairs sorting methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Sort(SortOrder sortOrder, Comparison<MatchedPair> comparer)
		{
			s_currSortOrder = sortOrder;
			Sort(comparer);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares matched pairs opening characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int OpenComparer(MatchedPair x, MatchedPair y)
		{
			if ((x == null && y == null) ||
				(string.IsNullOrEmpty(x.Open) && string.IsNullOrEmpty(y.Open)))
			{
				return 0;
			}

			if (x == null || string.IsNullOrEmpty(x.Open))
				return (s_currSortOrder == SortOrder.Ascending ? -1 : 1);

			if (y == null || string.IsNullOrEmpty(y.Open))
				return (s_currSortOrder == SortOrder.Ascending ? 1 : -1);

			return (s_currSortOrder == SortOrder.Ascending ?
				x.Open.CompareTo(y.Open) :
				y.Open.CompareTo(x.Open));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares matched pairs closing characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int CloseComparer(MatchedPair x, MatchedPair y)
		{
			if ((x == null && y == null) ||
				(string.IsNullOrEmpty(x.Close) && string.IsNullOrEmpty(y.Close)))
			{
				return 0;
			}

			if (x == null || string.IsNullOrEmpty(x.Close))
				return (s_currSortOrder == SortOrder.Ascending ? -1 : 1);

			if (y == null || string.IsNullOrEmpty(y.Close))
				return (s_currSortOrder == SortOrder.Ascending ? 1 : -1);

			return (s_currSortOrder == SortOrder.Ascending ?
				x.Close.CompareTo(y.Close) :
				y.Close.CompareTo(x.Close));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares matched pairs open character's codepoint.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int OpenCodeComparer(MatchedPair x, MatchedPair y)
		{
			if ((x == null && y == null) ||
				(string.IsNullOrEmpty(x.Open) && string.IsNullOrEmpty(y.Open)))
			{
				return 0;
			}

			if (x == null || string.IsNullOrEmpty(x.Open))
				return (s_currSortOrder == SortOrder.Ascending ? -1 : 1);

			if (y == null || string.IsNullOrEmpty(y.Open))
				return (s_currSortOrder == SortOrder.Ascending ? 1 : -1);

			return (s_currSortOrder == SortOrder.Ascending ?
				x.Open[0].CompareTo(y.Open[0]) :
				y.Open[0].CompareTo(x.Open[0]));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares matched pairs close character's codepoint.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int CloseCodeComparer(MatchedPair x, MatchedPair y)
		{
			if ((x == null && y == null) ||
				(string.IsNullOrEmpty(x.Close) && string.IsNullOrEmpty(y.Close)))
			{
				return 0;
			}

			if (x == null || string.IsNullOrEmpty(x.Close))
				return (s_currSortOrder == SortOrder.Ascending ? -1 : 1);

			if (y == null || string.IsNullOrEmpty(y.Close))
				return (s_currSortOrder == SortOrder.Ascending ? 1 : -1);

			return (s_currSortOrder == SortOrder.Ascending ?
				x.Close[0].CompareTo(y.Close[0]) :
				y.Close[0].CompareTo(x.Close[0]));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares matched pairs "ClosedByPara" fields.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int ClosedByParaComparer(MatchedPair x, MatchedPair y)
		{
			if (x == null && y == null)
				return 0;

			if (x == null)
				return (s_currSortOrder == SortOrder.Ascending ? -1 : 1);

			if (y == null)
				return (s_currSortOrder == SortOrder.Ascending ? 1 : -1);

			return (s_currSortOrder == SortOrder.Ascending ?
				x.PermitParaSpanning.CompareTo(y.PermitParaSpanning) :
				y.PermitParaSpanning.CompareTo(x.PermitParaSpanning));
		}

		#endregion
	}

	#endregion

	#region MatchedPair class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Stores a single pair of matching characters and a value indicating whether or not an
	/// opening of the matched pairs is automatically closed by the end of a paragraph.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("pair")]
	public class MatchedPair
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("open")]
		public string Open;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("close")]
		public string Close;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("permitParaSpanning")]
		public bool PermitParaSpanning = false;
	}

	#endregion
}
