// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using SIL.LCModel;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Published with EventConstants.ExportTextsAsFlexText and answered synchronously by a
	/// globally-registered listener: it writes one .flextext file per text in
	/// TextsToExport into OutputFolder, sets Handled to true, and appends a
	/// "&lt;name&gt;: &lt;message&gt;" entry to Failures for any text it could not export.
	/// </summary>
	public sealed class ExportTextsAsFlexTextRequest
	{
		public ExportTextsAsFlexTextRequest(IEnumerable<IStText> textsToExport, string outputFolder)
		{
			TextsToExport = textsToExport;
			OutputFolder = outputFolder;
		}

		public IEnumerable<IStText> TextsToExport { get; }

		public string OutputFolder { get; }

		/// <summary>Set true by the subscriber that handled this request.</summary>
		public bool Handled { get; set; }

		/// <summary>One entry per text that failed to export, formatted "&lt;name&gt;: &lt;message&gt;".</summary>
		public List<string> Failures { get; } = new List<string>();
	}
}
