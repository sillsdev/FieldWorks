using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Windows.Forms;
using SIL.Utils;

namespace SILUBS.SharedScrUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum PuncPatternStatus
	{
		Valid,
		Invalid,
		Unknown
	}

	#region enum ContextPosition
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Enumeration indicating where a punctuation pattern occurs with respect to its context
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum ContextPosition
	{
		/// <summary>Occurs at the start of a word or paragraph</summary>
		WordInitial,
		/// <summary>Occurs between two words and is word-forming (or in the middle
		/// of a compound word)</summary>
		WordMedial,
		/// <summary>Occurs between two words and is not word-forming (or in the middle
		/// of a compound word)</summary>
		WordBreaking,
		/// <summary>Occurs at the end of a word or paragraph</summary>
		WordFinal,
		/// <summary>Occurs surrounded by whitespace or alone in a paragraph</summary>
		Isolated,
		/// <summary>Undefined</summary>
		Undefined,
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Encapsulates a list of PuncPattern objects.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("PunctuationPatterns")]
	public class PuncPatternsList : List<PuncPattern>
	{
		private static SortOrder s_currSortOrder = SortOrder.Ascending;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a PuncPatternsList from the specified XML string.
		/// </summary>
		/// <param name="xmlSrc">The XML source string to load.</param>
		/// <param name="wsName">Name of the writing system (used for error reporting).</param>
		/// ------------------------------------------------------------------------------------
		public static PuncPatternsList Load(string xmlSrc, string wsName)
		{
			Exception e;
			PuncPatternsList list = XmlSerializationHelper.DeserializeFromString<PuncPatternsList>(xmlSrc, out e);
			if (e != null)
				throw new ContinuableErrorException("Invalid PunctuationPatterns field while loading the " +
					wsName + " writing system.", e);

			return (list ?? new PuncPatternsList());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the specified pattern in the list and returns the associated PuncPattern
		/// object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PuncPattern this[string puncPattern]
		{
			get
			{
				foreach (PuncPattern pattern in this)
				{
					if (pattern.Pattern == puncPattern)
						return pattern;
				}

				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the <see cref="T:SILUBS.SharedScrUtils.PuncPattern"/> with the specified index.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new PuncPattern this[int i]
		{
			get { return (i < 0 || i >= Count ? null : base[i]); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a clone of the list of punctuation patterns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PuncPatternsList Clone()
		{
			PuncPatternsList clone = new PuncPatternsList();
			foreach (PuncPattern pattern in this)
				clone.Add(pattern.Clone());

			return clone;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an XML string representing the list of puncutation patterns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string XmlString
		{
			get
			{
				// Only serialize those patterns with a valid or invalid status.
				PuncPatternsList tmpList = new PuncPatternsList();
				foreach (PuncPattern pattern in this)
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

				string xml = XmlSerializationHelper.SerializeToString<PuncPatternsList>(tmpList);

				// Replace ORCs with the hex codepoint value for single quote.
				return xml.Replace("\xFFFC", "&#x27;");
			}
		}

		#region PuncPattern sorting methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Sort(SortOrder sortOrder, Comparison<PuncPattern> comparer)
		{
			s_currSortOrder = sortOrder;
			Sort(comparer);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares punctuation patterns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int PatternComparer(PuncPattern x, PuncPattern y)
		{
			if (x == null && y == null)
				return 0;

			if (x == null)
				return -1;

			if (y == null)
				return 1;

			if (string.IsNullOrEmpty(x.Pattern) && string.IsNullOrEmpty(y.Pattern))
				return 0;

			if (string.IsNullOrEmpty(x.Pattern))
				return -1;

			if (string.IsNullOrEmpty(y.Pattern))
				return 1;

			string xPtrn = x.Pattern.Trim();
			string yPtrn = y.Pattern.Trim();

			return (s_currSortOrder == SortOrder.Ascending ?
				xPtrn.CompareTo(yPtrn) : yPtrn.CompareTo(xPtrn));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares punctuation pattern contexts.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int ContextComparer(PuncPattern x, PuncPattern y)
		{
			if (x == null && y == null)
				return 0;

			if (x == null)
				return -1;

			if (y == null)
				return 1;

			return (s_currSortOrder == SortOrder.Ascending ?
				x.ContextPos.CompareTo(y.ContextPos) :
				y.ContextPos.CompareTo(x.ContextPos));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares punctuation pattern counts.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int CountComparer(PuncPattern x, PuncPattern y)
		{
			if (x == null && y == null)
				return 0;

			if (x == null)
				return -1;

			if (y == null)
				return 1;

			return (s_currSortOrder == SortOrder.Ascending ?
				x.Count.CompareTo(y.Count) :
				y.Count.CompareTo(x.Count));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares punctuation pattern statuses.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int StatusComparer(PuncPattern x, PuncPattern y)
		{
			if (x == null && y == null)
				return 0;

			if (x == null)
				return -1;

			if (y == null)
				return 1;

			return (s_currSortOrder == SortOrder.Ascending ?
				x.Status.CompareTo(y.Status) :
				y.Status.CompareTo(x.Status));
		}

		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Stores a single punctuation pattern and a value indicating whether or not the pattern
	/// is considered to be valid in the language.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("pattern")]
	public class PuncPattern
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Status (valid, invalid, or unknown)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public PuncPatternStatus Status = PuncPatternStatus.Unknown;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("value")]
		public string Pattern;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates where this punctuation pattern occurs with respect to its context.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("context")]
		public ContextPosition ContextPos;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this property only for serialization and deserialization. Use Status when the
		/// user modifies this pattern in the UI.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("valid")]
		public bool Valid
		{
			get { return (Status == PuncPatternStatus.Valid); }
			set { Status = (value ? PuncPatternStatus.Valid : PuncPatternStatus.Invalid); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public int Count = 0;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default Constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PuncPattern()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor to build a fully specified PuncPattern object (used in tests)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PuncPattern(string pattern, ContextPosition context, PuncPatternStatus status)
		{
			Pattern = pattern;
			ContextPos = context;
			Status = status;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a clone of the punctuation pattern.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PuncPattern Clone()
		{
			PuncPattern clone = new PuncPattern();
			clone.Status = Status;
			clone.Count = Count;
			clone.Pattern = Pattern;
			clone.Valid = Valid;
			clone.ContextPos = ContextPos;

			return clone;
		}
	}
}
