// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using SIL.LCModel;
using SIL.FieldWorks.Common.FwUtils;
using static SIL.FieldWorks.Common.FwUtils.FwUtils;
using SIL.FieldWorks.XWorks;
using XCore;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Answers EventConstants.ExportTextsAsFlexText: writes one .flextext file per
	/// requested text into the given folder. Lives in ITextDll rather than xWorks
	/// because InterlinVc/InterlinearExporter are only reachable from this project --
	/// a reference the other direction would be a build-breaking cycle. Registered
	/// globally in Main.xml's &lt;listeners&gt; section, so it answers regardless of
	/// which area is active.
	/// </summary>
	public class FlexTextAIExportListener : IxCoreColleague, IDisposable
	{
		private LcmCache m_cache;
		private PropertyTable m_propertyTable;
		private bool m_isDisposed;

		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			m_propertyTable = propertyTable;
			m_cache = propertyTable.GetValue<LcmCache>("cache");
			mediator.AddColleague(this);
			Subscriber.Subscribe(EventConstants.ExportTextsAsFlexText, OnExportTextsAsFlexText, m_propertyTable.GetWindow());
		}

		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[] { this };
		}

		public bool ShouldNotCall => false;

		public int Priority => (int)ColleaguePriority.Medium;

		private void OnExportTextsAsFlexText(object parameterObj)
		{
			if (!(parameterObj is ExportTextsAsFlexTextRequest request))
				return;
			ExportTextsAsFlexTextForTests(m_cache, request);
		}

		/// <summary>The export logic itself, separate from the pub/sub handler.</summary>
		internal void ExportTextsAsFlexTextForTests(LcmCache cache, ExportTextsAsFlexTextRequest request)
		{
			request.Handled = true;
			var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "HCGrammar" };
			foreach (var stText in request.TextsToExport)
			{
				var name = GrammarTextsAIExportHelpers.GetTextDisplayName(stText);
				try
				{
					var fileName = GrammarTextsAIExportHelpers.MakeSafeFileName(name, usedNames);
					var filePath = Path.Combine(request.OutputFolder, fileName + ".flextext");
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
					request.Failures.Add($"{name}: {e.Message}");
				}
			}
		}

		public void Dispose()
		{
			if (m_isDisposed)
				return;
			Subscriber.Unsubscribe(EventConstants.ExportTextsAsFlexText, OnExportTextsAsFlexText);
			m_isDisposed = true;
		}
	}
}
