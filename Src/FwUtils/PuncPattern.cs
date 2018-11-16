// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Serialization;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Stores a single punctuation pattern and a value indicating whether or not the pattern
	/// is considered to be valid in the language.
	/// </summary>
	[XmlType("pattern")]
	public class PuncPattern
	{
		/// <summary>
		/// Status (valid, invalid, or unknown)
		/// </summary>
		[XmlIgnore]
		public PuncPatternStatus Status = PuncPatternStatus.Unknown;

		/// <summary />
		[XmlAttribute("value")]
		public string Pattern;

		/// <summary>
		/// Indicates where this punctuation pattern occurs with respect to its context.
		/// </summary>
		[XmlAttribute("context")]
		public ContextPosition ContextPos;

		/// <summary>
		/// Use this property only for serialization and deserialization. Use Status when the
		/// user modifies this pattern in the UI.
		/// </summary>
		[XmlAttribute("valid")]
		public bool Valid
		{
			get { return (Status == PuncPatternStatus.Valid); }
			set { Status = (value ? PuncPatternStatus.Valid : PuncPatternStatus.Invalid); }
		}

		/// <summary />
		[XmlIgnore]
		public int Count = 0;

		/// <summary />
		public PuncPattern()
		{
		}

		/// <summary>
		/// Constructor to build a fully specified PuncPattern object (used in tests)
		/// </summary>
		public PuncPattern(string pattern, ContextPosition context, PuncPatternStatus status)
		{
			Pattern = pattern;
			ContextPos = context;
			Status = status;
		}

		/// <summary>
		/// Returns a clone of the punctuation pattern.
		/// </summary>
		public PuncPattern Clone()
		{
			return new PuncPattern
			{
				Status = Status,
				Count = Count,
				Pattern = Pattern,
				Valid = Valid,
				ContextPos = ContextPos
			};
		}
	}
}
