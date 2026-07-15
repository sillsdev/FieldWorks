// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// §19f.9: a managed CSV export of the browse's VISIBLE columns and rows — the bounded, LCModel-free half
	/// of the legacy export. The legacy shell-level <c>RecordClerk.OnExport</c> → <c>ExportDialog</c> /
	/// ConfiguredExport (XHTML/XML) path and OS print are genuinely shell/Phase-2 concerns.
	/// PARITY §19f → avalonia-end-game: OS print integration + the full legacy ConfiguredExport formats.
	///
	/// The exporter is a pure function over the headers + the materialized cell strings the view already has,
	/// so it unit-tests without a window, a cache, or a file. Quoting follows RFC 4180: a field is wrapped in
	/// double quotes when it contains a comma, a double quote, or a CR/LF, and any embedded double quote is
	/// doubled. Rows are separated by CRLF (the spreadsheet-portable line ending).
	/// </summary>
	public static class BrowseCsvExporter
	{
		/// <summary>
		/// Renders <paramref name="headers"/> as the first CSV line followed by one line per row in
		/// <paramref name="rows"/> (each an ordered list of cell strings). A null/empty header list still emits
		/// the row lines; a row shorter than the header is padded conceptually by simply emitting the cells it
		/// has. Returns CRLF-joined CSV text (no trailing newline).
		/// </summary>
		public static string ToCsv(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
		{
			var lines = new List<string>();
			if (headers != null && headers.Count > 0)
				lines.Add(Line(headers));
			if (rows != null)
				foreach (var row in rows)
					lines.Add(Line(row ?? new List<string>()));
			return string.Join("\r\n", lines);
		}

		private static string Line(IReadOnlyList<string> fields)
		{
			var sb = new StringBuilder();
			for (var i = 0; i < fields.Count; i++)
			{
				if (i > 0)
					sb.Append(',');
				sb.Append(Quote(fields[i] ?? string.Empty));
			}
			return sb.ToString();
		}

		// Wraps a field in double quotes (doubling any embedded quote) only when it needs it (RFC 4180).
		private static string Quote(string field)
		{
			var needsQuote = field.IndexOf(',') >= 0 || field.IndexOf('"') >= 0
				|| field.IndexOf('\n') >= 0 || field.IndexOf('\r') >= 0;
			if (!needsQuote)
				return field;
			return "\"" + field.Replace("\"", "\"\"") + "\"";
		}
	}
}
