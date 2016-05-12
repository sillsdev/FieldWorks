// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	public class DictionaryExportService
	{
		private readonly Mediator m_mediator;
		private readonly FdoCache m_cache;

		private const string DictionaryType = "Dictionary";
		private const string ReversalType = "Reversal Index";

		public DictionaryExportService(Mediator mediator)
		{
			m_mediator = mediator;
			m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
		}

		public int CountDictionaryEntries(DictionaryConfigurationModel config)
		{
			int[] entries;
			using(ClerkActivator.ActivateClerkMatchingExportType(DictionaryType, m_mediator))
				ConfiguredXHTMLGenerator.GetPublicationDecoratorAndEntries(m_mediator, out entries, DictionaryType);
			return entries.Sum(e => CountTimesGenerated(m_cache, config, e));
		}

		/// <summary>
		/// Determines how many times the entry with the given HVO is generated for the given config (usually 0 or 1,
		/// but can be more if the entry matches more than one Minor Entry node)
		/// </summary>
		internal static int CountTimesGenerated(FdoCache cache, DictionaryConfigurationModel config, int hvo)
		{
			var entry = (ILexEntry)cache.ServiceLocator.GetObject(hvo);
			if (!ConfiguredXHTMLGenerator.IsMinorEntry(entry))
				return config.Parts[0].IsEnabled ? 1 : 0;
			if (!entry.PublishAsMinorEntry)
				return 0;
			var matchingMinorParts = 0;
			for (var i = 1; i < config.Parts.Count; i++)
			{
				var part = config.Parts[i];
				if (part.IsEnabled && ConfiguredXHTMLGenerator.IsListItemSelectedForExport(part, entry, null))
					matchingMinorParts++;
			}
			return matchingMinorParts;
		}

		/// <summary>
		/// Produce a table of reversal index ShortNames and the count of the entries in each of them.
		/// The reversal indexes included will be limited to those ShortNames specified in selectedReversalIndexes.
		/// </summary>
		internal static SortedDictionary<string,int> GetCountsOfReversalIndexes(FdoCache cache, IEnumerable<string> selectedReversalIndexes)
		{
			var relevantReversalIndexesAndTheirCounts = cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances()
				.Select(repo => cache.ServiceLocator.GetObject(repo.Guid) as IReversalIndex)
				.Where(reversalindex => reversalindex != null && selectedReversalIndexes.Contains(reversalindex.ShortName))
				.ToDictionary(reversalIndex => reversalIndex.ShortName, reversalIndex => reversalIndex.EntriesOC.Count);

			return new SortedDictionary<string,int> (relevantReversalIndexesAndTheirCounts);
		}

		public void ExportDictionaryContent(string xhtmlPath, DictionaryConfigurationModel configuration = null, IThreadedProgress progress = null)
		{
			using (ClerkActivator.ActivateClerkMatchingExportType(DictionaryType, m_mediator))
			{
				configuration = configuration ?? new DictionaryConfigurationModel(
					DictionaryConfigurationListener.GetCurrentConfiguration(m_mediator, "Dictionary"), m_cache);
				ExportConfiguredXhtml(xhtmlPath, configuration, DictionaryType, progress);
			}
		}

		public void ExportReversalContent(string xhtmlPath, string reversalName = null, DictionaryConfigurationModel configuration = null,
			IThreadedProgress progress = null)
		{
			using (ClerkActivator.ActivateClerkMatchingExportType(ReversalType, m_mediator))
			{
				var originalReversalIndexGuid = m_mediator.PropertyTable.GetStringProperty("ReversalIndexGuid", null);
				var clerk = m_mediator.PropertyTable.GetValue("ActiveClerk", null) as RecordClerk;
				if (reversalName != null)
				{
					// Set the reversal index guid property so that the right guid is found down in DictionaryPublicationDecorater.GetEntriesToPublish,
					// and manually call OnPropertyChanged to cause LexEdDll ReversalClerk.ChangeOwningObject(guid) to be called. This causes the
					// right reversal content to be exported, fixing LT-17011.
					var reversalIndex = m_cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances()
						.First(repo => repo.ShortName == reversalName);
					m_mediator.PropertyTable.SetProperty("ReversalIndexGuid", reversalIndex.Guid.ToString());
					if (clerk != null)
						clerk.OnPropertyChanged("ReversalIndexGuid");
				}

				configuration = configuration ?? new DictionaryConfigurationModel(
					DictionaryConfigurationListener.GetCurrentConfiguration(m_mediator, "ReversalIndex"), m_cache);
				ExportConfiguredXhtml(xhtmlPath, configuration, ReversalType, progress);

				if (originalReversalIndexGuid != null && originalReversalIndexGuid != m_mediator.PropertyTable.GetStringProperty("ReversalIndexGuid", null))
				{
					m_mediator.PropertyTable.SetProperty("ReversalIndexGuid", originalReversalIndexGuid.ToString());
					if (clerk != null)
						clerk.OnPropertyChanged("ReversalIndexGuid");
				}
			}
		}

		private void ExportConfiguredXhtml(string xhtmlPath, DictionaryConfigurationModel configuration, string exportType, IThreadedProgress progress)
		{
			int[] entriesToSave;
			var publicationDecorator = ConfiguredXHTMLGenerator.GetPublicationDecoratorAndEntries(m_mediator, out entriesToSave, exportType);
			if (progress != null)
				progress.Maximum = entriesToSave.Length;
			ConfiguredXHTMLGenerator.SavePublishedHtmlWithStyles(entriesToSave, publicationDecorator, int.MaxValue, configuration, m_mediator, xhtmlPath, progress);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule",
			Justification = "m_currentClerk is a reference that had *better not* be disposed when ClerkActivator is disposed")]
		private sealed class ClerkActivator : IDisposable
		{
			private static RecordClerk s_dictionaryClerk;
			private static RecordClerk s_reversalIndexClerk;

			private readonly RecordClerk m_currentClerk;

			private ClerkActivator(RecordClerk currentClerk)
			{
				m_currentClerk = currentClerk;
			}

			public void Dispose()
			{
				if (m_currentClerk != null && !m_currentClerk.IsDisposed)
				{
					m_currentClerk.ActivateUI(true);
				}
			}

			private static void CacheClerk(string clerkType, RecordClerk clerk)
			{
				switch (clerkType)
				{
					case DictionaryType:
						s_dictionaryClerk = clerk;
						break;
					case ReversalType:
						s_reversalIndexClerk = clerk;
						break;
				}
			}

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "tempClerk must be active when this function returns")]
			public static ClerkActivator ActivateClerkMatchingExportType(string exportType, Mediator mediator)
			{
				var isDictionary = exportType == DictionaryType;
				const string area = "lexicon";
				var tool = isDictionary ? "lexiconDictionary" : "reversalToolEditComplete";
				var collector = new XmlNode[1];
				var parameter = new Tuple<string, string, XmlNode[]>(area, tool, collector);
				mediator.SendMessage("GetContentControlParameters", parameter);
				var parameters = collector[0].SelectSingleNode(".//parameters[@clerk]");
				var currentClerk = mediator.PropertyTable.GetValue("ActiveClerk", null) as RecordClerk;
				if (DoesClerkMatchParams(currentClerk, parameters))
					return null; // No need to juggle clerks if the one we want is already active

				var tempClerk = isDictionary ? s_dictionaryClerk : s_reversalIndexClerk;
				if (tempClerk == null || tempClerk.IsDisposed)
				{
					tempClerk = RecordClerk.FindClerk(mediator, isDictionary ? "entries" : "AllReversalEntries");
					if (tempClerk == null || tempClerk.IsDisposed)
						tempClerk = RecordClerkFactory.CreateClerk(mediator, parameters, true);
					CacheClerk(exportType, tempClerk);
				}
				tempClerk.ActivateUI(true, false);
				tempClerk.UpdateList(true, true);
				return new ClerkActivator(currentClerk); // ensure the current active clerk is reactivated after we use the temporary clerk.
			}

			private static bool DoesClerkMatchParams(RecordClerk clerk, XmlNode parameters)
			{
				if (clerk == null)
					return false;
				var atts = parameters.Attributes;
				if (atts == null)
					return false;
				var id = atts["clerk"].Value;
				return id == clerk.Id;
			}
		}
	}
}
