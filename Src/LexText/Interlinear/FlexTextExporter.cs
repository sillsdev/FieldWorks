// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using SIL.LCModel;
using SIL.FieldWorks.XWorks;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Writes texts to .flextext files. Lives in ITextDll because InterlinVc and
	/// InterlinearExporter are only reachable from this project.
	/// </summary>
	public static class FlexTextExporter
	{
		/// <summary>
		/// Writes one .flextext file per text into <paramref name="outputFolder"/>, named
		/// after the text and made unique against <paramref name="reservedNames"/> and
		/// against each other. A text that cannot be written appends a
		/// "&lt;name&gt;: &lt;message&gt;" entry to <paramref name="messages"/> and is
		/// skipped, so one bad text does not lose the rest.
		/// </summary>
		public static void ExportTexts(LcmCache cache, IEnumerable<IStText> texts, string outputFolder,
			IEnumerable<string> reservedNames, ICollection<string> messages)
		{
			var usedNames = new HashSet<string>(reservedNames, StringComparer.OrdinalIgnoreCase);
			foreach (var stText in texts)
			{
				var name = GrammarTextsAIExportHelpers.GetTextDisplayName(stText);
				try
				{
					var fileName = GrammarTextsAIExportHelpers.MakeSafeFileName(name, usedNames);
					var filePath = Path.Combine(outputFolder, fileName + ".flextext");
					var settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };
					using (var vc = new InterlinVc(cache))
					using (var writer = XmlWriter.Create(filePath, settings))
					{
						vc.LineChoices = InterlinLineChoices.DefaultChoices(cache.LangProject, cache.DefaultVernWs, cache.DefaultAnalWs);
						var exporter = InterlinearExporter.Create("xml", cache, writer, stText, vc.LineChoices, vc);
						exporter.WriteBeginDocument();
						exporter.ExportDisplay();
						exporter.WriteEndDocument();
					}
				}
				catch (Exception e)
				{
					messages.Add($"{name}: {e.Message}");
				}
			}
		}
	}
}
