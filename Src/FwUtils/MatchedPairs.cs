// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Serialization;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Stores a single pair of matching characters and a value indicating whether or not an
	/// opening of the matched pairs is automatically closed by the end of a paragraph.
	/// </summary>
	[XmlType("pair")]
	public class MatchedPair
	{
		/// <summary />
		[XmlAttribute("open")]
		public string Open;

		/// <summary />
		[XmlAttribute("close")]
		public string Close;

		/// <summary />
		[XmlAttribute("permitParaSpanning")]
		public bool PermitParaSpanning;
	}
}