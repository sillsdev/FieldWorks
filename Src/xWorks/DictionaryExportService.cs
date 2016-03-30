// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
		private FdoCache Cache { get { return (FdoCache)m_mediator.PropertyTable.GetValue("cache"); } }

		private const string DictionaryType = "Dictionary";
		private const string ReversalType = "Reversal Index";

		public DictionaryExportService(Mediator mediator)
		{
			m_mediator = mediator;
		}

		public int CountDictionaryEntries()
		{
			int[] entries;
			using(ClerkActivator.ActivateClerkMatchingExportType(DictionaryType, m_mediator))
				ConfiguredXHTMLGenerator.GetPublicationDecoratorAndEntries(m_mediator, out entries, DictionaryType);
			return entries.Length;
		}

		public int CountReversalIndexEntries(IEnumerable<string> selectedReversalIndexes)
		{
			// TODO: we need to add some logic to retrive reversal entry based on Selected publication in future.

			return Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances()
				.Select(repo => Cache.ServiceLocator.GetObject(repo.Guid) as IReversalIndex)
				.Where(reversalindex => reversalindex != null && selectedReversalIndexes.Contains(reversalindex.ShortName))
				.Sum(reversalindex => reversalindex.EntriesOC.Count);
		}

		public void ExportDictionaryContent(string xhtmlPath, DictionaryConfigurationModel configuration = null, IThreadedProgress progress = null)
		{
			using (ClerkActivator.ActivateClerkMatchingExportType(DictionaryType, m_mediator))
			{
				configuration = configuration ?? new DictionaryConfigurationModel(
					DictionaryConfigurationListener.GetCurrentConfiguration(m_mediator, "Dictionary"), Cache);
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
					var reversalIndex = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances()
						.FirstOrDefault(repo => repo.ShortName == reversalName);
					m_mediator.PropertyTable.SetProperty("ReversalIndexGuid", reversalIndex.Guid.ToString());
					if (clerk != null)
						clerk.OnPropertyChanged("ReversalIndexGuid");
				}

				configuration = configuration ?? new DictionaryConfigurationModel(
					DictionaryConfigurationListener.GetCurrentConfiguration(m_mediator, "ReversalIndex"), Cache);
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
