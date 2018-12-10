// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.SfmToXml;
using SIL.LCModel;

namespace LanguageExplorer.Controls.LexText.DataNotebook
{
	/// <summary>
	/// Stores the information needed to make a link later, after all the records have
	/// been created.
	/// </summary>
	internal class PendingLink
	{
		public RnSfMarker Marker { get; set; }
		public SfmField Field { get; set; }
		public IRnGenericRec Record { get; set; }
	}
}