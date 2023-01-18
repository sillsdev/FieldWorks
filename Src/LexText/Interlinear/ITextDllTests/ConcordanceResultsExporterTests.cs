// Copyright (c) 2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.IText
{
	public class ConcordanceResultsExporterTests
	{
		[Test]
		public void CloseTableCell_EmptyCell()
		{
			using (var writer = new StringWriter())
			{
				var exporter = new ConcordanceResultsExporter(writer, new XmlBrowseViewBaseVc(), null, 1);
				exporter.AddString(new TsStrBldr().Append("Row1Cell1", 1).GetString());
				exporter.CloseTableCell();
				exporter.AddString(new TsStrBldr().Append("Row1Cell2", 1).GetString());
				exporter.CloseTableCell();
				exporter.AddString(new TsStrBldr().Append("Row1Cell3", 1).GetString());
				exporter.CloseTableCell();
				exporter.CloseTableRow();
				exporter.AddString(new TsStrBldr().Append("Row2Cell1", 1).GetString());
				exporter.CloseTableCell();
				// Intentionally don't write Cell2.
				exporter.CloseTableCell();
				exporter.AddString(new TsStrBldr().Append("Row2Cell3", 1).GetString());
				exporter.CloseTableCell();
				exporter.CloseTableRow();
				writer.Flush();

				var lines = writer.ToString().Replace("\r", string.Empty).Split('\n');
				Assert.That(lines.Length, Is.EqualTo(4), "Should contain header row, two data rows, and a trailing newline");
				// Confirm that a empty Row2Cell2 was written (ie. Row2Cell3 should be in the same column as Row1Cell3).
				Assert.That(lines[0], Is.Empty, "Header generation requires more setup than we did.");
				Assert.That(lines[1], Is.EqualTo("Row1Cell1,Row1Cell2,Row1Cell3"));
				Assert.That(lines[2], Is.EqualTo("Row2Cell1,,Row2Cell3"));
				Assert.That(lines[3], Is.Empty);
			}
		}

		[Test]
		public void CsvWorkerTests_SpecialCharacters()
		{
			using (var writer = new StringWriter())
			{
				var exporter = new ConcordanceResultsExporter(writer, new XmlBrowseViewBaseVc(), null, 1);
				exporter.AddString(new TsStrBldr().Append("ViolinCell0", 1).GetString());
				exporter.CloseTableCell();
				exporter.AddString(new TsStrBldr().Append("Cell\r\n1", 1).GetString());
				exporter.CloseTableCell();
				exporter.AddString(new TsStrBldr().Append("Cell2, with a comma", 1).GetString());
				exporter.CloseTableCell();
				exporter.AddString(new TsStrBldr().Append("Cell3 \"quote\"", 1).GetString());
				exporter.CloseTableCell();
				exporter.CloseTableRow();
				writer.Flush();

				var dataRow = writer.ToString().Trim('\r', '\n');
				// Confirm that the written csv handles commas and quotes embed in the string.
				Assert.That(dataRow, Is.EqualTo("ViolinCell0,\"Cell\r\n1\",\"Cell2, with a comma\",\"Cell3 \"\"quote\"\"\""));
			}
		}
	}
}