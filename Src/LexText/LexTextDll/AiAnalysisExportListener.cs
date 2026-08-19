// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using static SIL.FieldWorks.Common.FwUtils.FwUtils;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.LCModel;
using SIL.Machine.Morphology.HermitCrab;
using XCore;

namespace SIL.FieldWorks.XWorks.LexText
{
	/// <summary>
	/// Answers EventConstants.ExportForAiAnalysis: writes the HermitCrab grammar and the
	/// requested texts into the export folder. Lives in LexTextDll because it is the one
	/// assembly that can reach both the parser's grammar loader and the interlinear
	/// exporter. Registered globally in Main.xml's &lt;listeners&gt; section, so it answers
	/// regardless of which area is active.
	/// </summary>
	public class AiAnalysisExportListener : IxCoreColleague, IDisposable
	{
		/// <summary>File name the grammar always gets, reserved so no text can take it.</summary>
		private const string ksGrammarFileBaseName = "HCGrammar";

		private LcmCache m_cache;
		private PropertyTable m_propertyTable;
		private bool m_isDisposed;

		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			m_propertyTable = propertyTable;
			m_cache = propertyTable.GetValue<LcmCache>("cache");
			mediator.AddColleague(this);
			Subscriber.Subscribe(EventConstants.ExportForAiAnalysis, OnExportForAiAnalysis, m_propertyTable.GetWindow());
		}

		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[] { this };
		}

		public bool ShouldNotCall => false;

		public int Priority => (int)ColleaguePriority.Medium;

		private void OnExportForAiAnalysis(object parameterObj)
		{
			if (!(parameterObj is AiAnalysisExportRequest request))
				return;
			Export(m_cache, request);
		}

		/// <summary>
		/// Writes the grammar first, then the texts. An unloadable grammar throws out of here so
		/// the whole export aborts; per-item grammar problems and per-text failures land in the
		/// request's Messages instead, since the export is still worth keeping without them.
		/// </summary>
		internal static void Export(LcmCache cache, AiAnalysisExportRequest request)
		{
			request.Handled = true;
			var logger = new GrammarExportLoadLogger(request.Messages);
			var language = HCLoader.Load(cache, logger);
			XmlLanguageWriter.Save(language, request.GrammarPath);

			FlexTextExporter.ExportTexts(cache, request.TextsToExport, request.OutputFolder,
				new[] { ksGrammarFileBaseName }, request.Messages);
		}

		public void Dispose()
		{
			if (m_isDisposed)
				return;
			Subscriber.Unsubscribe(EventConstants.ExportForAiAnalysis, OnExportForAiAnalysis);
			m_isDisposed = true;
		}
	}
}
