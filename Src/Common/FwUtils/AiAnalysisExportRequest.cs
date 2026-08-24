// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using SIL.LCModel;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Published with EventConstants.ExportForAiAnalysis and answered synchronously by a
	/// globally-registered listener: it writes the HermitCrab grammar to GrammarPath and one
	/// .flextext file per text in TextsToExport into OutputFolder, sets Handled to true, and
	/// appends a plain-language entry to Messages for anything it warned about or could not
	/// write. A failure to build the grammar throws instead, since the export is worthless
	/// without it.
	/// </summary>
	public sealed class AiAnalysisExportRequest
	{
		public AiAnalysisExportRequest(IEnumerable<IStText> textsToExport, string outputFolder, string grammarPath)
		{
			TextsToExport = textsToExport;
			OutputFolder = outputFolder;
			GrammarPath = grammarPath;
		}

		public IEnumerable<IStText> TextsToExport { get; }

		public string OutputFolder { get; }

		/// <summary>Full path of the grammar file to write, inside OutputFolder.</summary>
		public string GrammarPath { get; }

		/// <summary>Set true by the subscriber that handled this request.</summary>
		public bool Handled { get; set; }

		/// <summary>
		/// Grammar-load warnings, and one "&lt;name&gt;: &lt;message&gt;" entry per text that
		/// failed to export. Shown to the user as an export summary, so every entry has to read
		/// as a sentence on its own.
		/// </summary>
		public List<string> Messages { get; } = new List<string>();
	}
}
