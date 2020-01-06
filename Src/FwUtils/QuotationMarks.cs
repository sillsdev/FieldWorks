// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml.Serialization;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary />
	[XmlType("Pair")]
	public class QuotationMarks
	{
		/// <summary />
		[XmlAttribute]
		public string Opening = string.Empty;
		/// <summary />
		[XmlAttribute]
		public string Closing = string.Empty;

		/// <summary>
		/// Gets a value indicating whether or not both opening and closing are empty.
		/// </summary>
		public bool IsEmpty => (string.IsNullOrEmpty(Opening.Trim()) && string.IsNullOrEmpty(Closing.Trim()));

		/// <summary>
		/// Gets a value indicating whether or not one or the other of the quotation marks
		/// exists but not both.
		/// </summary>
		public bool IsComplete => (!string.IsNullOrEmpty(Opening.Trim()) && !string.IsNullOrEmpty(Closing.Trim()));

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		public override string ToString() => $"{Opening}...{Closing}";

		/// <summary>
		/// Returns a value indicating whether or not the specified QuotationMarks object is
		/// equal to this one.
		/// </summary>
		public bool Equals(QuotationMarks qmark)
		{
			return (Opening.Equals(qmark.Opening, StringComparison.Ordinal) && Closing.Equals(qmark.Closing, StringComparison.Ordinal));
		}

		/// <summary>
		/// Gets a value indicating whether this quote marks has identical opening and closing
		/// marks.
		/// </summary>
		public bool HasIdenticalOpenerAndCloser => Opening.Equals(Closing, StringComparison.Ordinal);
	}
}