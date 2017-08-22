// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Works
{
	public class DictionaryExportService
	{
		private readonly IPropertyTable m_propertyTable;
		private readonly IPublisher m_publisher;
		private LcmCache Cache { get; }
		private RecordClerk Clerk { get; }

		private const string DictionaryType = "Dictionary";
		private const string ReversalType = "Reversal Index";

		public DictionaryExportService(LcmCache cache, RecordClerk activeClerk, IPropertyTable propertyTable, IPublisher publisher)
		{
			Cache = cache;
			Clerk = activeClerk;
			m_propertyTable = propertyTable;
			m_publisher = publisher;
		}

		public int CountDictionaryEntries(DictionaryConfigurationModel config)
		{
			int[] entries;
			using (ClerkActivator.ActivateClerkMatchingExportType(DictionaryType, m_publisher))
				ConfiguredXHTMLGenerator.GetPublicationDecoratorAndEntries(m_propertyTable, out entries, DictionaryType, Cache, Clerk);
			return entries.Count(e => IsGenerated(Cache, config, e));
		}

		/// <summary>
		/// Determines how many times the entry with the given HVO is generated for the given config (usually 0 or 1,
		/// but can be more if the entry matches more than one Minor Entry node)
		/// </summary>
		internal static bool IsGenerated(LcmCache cache, DictionaryConfigurationModel config, int hvo)
		{
			var entry = (ILexEntry)cache.ServiceLocator.GetObject(hvo);
			if (ConfiguredXHTMLGenerator.IsMainEntry(entry, config))
				return config.Parts[0].IsEnabled && (!entry.ComplexFormEntryRefs.Any() || ConfiguredXHTMLGenerator.IsListItemSelectedForExport(config.Parts[0], entry));
			return entry.PublishAsMinorEntry && config.Parts.Skip(1).Any(part => ConfiguredXHTMLGenerator.IsListItemSelectedForExport(part, entry));
		}

		/// <summary>
		/// Produce a table of reversal index ShortNames and the count of the entries in each of them.
		/// The reversal indexes included will be limited to those ShortNames specified in selectedReversalIndexes.
		/// </summary>
		public SortedDictionary<string,int> GetCountsOfReversalIndexes(IEnumerable<string> selectedReversalIndexes)
		{
			using (ClerkActivator.ActivateClerkMatchingExportType(ReversalType, m_publisher))
			{
				var relevantReversalIndexesAndTheirCounts = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances()
					.Select(repo => Cache.ServiceLocator.GetObject(repo.Guid) as IReversalIndex)
					.Where(ri => ri != null && selectedReversalIndexes.Contains(ri.ShortName))
					.ToDictionary(ri => ri.ShortName, CountReversalIndexEntries);

				return new SortedDictionary<string,int> (relevantReversalIndexesAndTheirCounts);
			}
		}

		internal int CountReversalIndexEntries(IReversalIndex ri)
		{
			int[] entries;
			using (ReversalIndexActivator.ActivateReversalIndex(ri.Guid, m_propertyTable, Clerk))
				ConfiguredXHTMLGenerator.GetPublicationDecoratorAndEntries(m_propertyTable, out entries, ReversalType, Cache, Clerk);
			return entries.Length;
		}

		public void ExportDictionaryContent(string xhtmlPath, DictionaryConfigurationModel configuration = null, IThreadedProgress progress = null)
		{
			using (ClerkActivator.ActivateClerkMatchingExportType(DictionaryType, m_publisher))
			{
				configuration = configuration ?? new DictionaryConfigurationModel(
					DictionaryConfigurationListener.GetCurrentConfiguration(m_propertyTable, "Dictionary"), Cache);
				ExportConfiguredXhtml(xhtmlPath, configuration, DictionaryType, progress);
			}
		}

		public void ExportReversalContent(string xhtmlPath, string reversalWs = null, DictionaryConfigurationModel configuration = null,
			IThreadedProgress progress = null)
		{
			using (ClerkActivator.ActivateClerkMatchingExportType(ReversalType, m_publisher))
			using (ReversalIndexActivator.ActivateReversalIndex(reversalWs, m_propertyTable, Cache, Clerk))
			{
				configuration = configuration ?? new DictionaryConfigurationModel(
					DictionaryConfigurationListener.GetCurrentConfiguration(m_propertyTable, "ReversalIndex"), Cache);
				ExportConfiguredXhtml(xhtmlPath, configuration, ReversalType, progress);
			}
		}

		private void ExportConfiguredXhtml(string xhtmlPath, DictionaryConfigurationModel configuration, string exportType, IThreadedProgress progress)
		{
			int[] entriesToSave;
			var publicationDecorator = ConfiguredXHTMLGenerator.GetPublicationDecoratorAndEntries(m_propertyTable, out entriesToSave, exportType, Cache, Clerk);
			if (progress != null)
				progress.Maximum = entriesToSave.Length;
			ConfiguredXHTMLGenerator.SavePublishedHtmlWithStyles(entriesToSave, publicationDecorator, int.MaxValue, configuration, m_propertyTable, Cache, Clerk, xhtmlPath, progress);
		}

		private sealed class ClerkActivator : IDisposable
		{
			private static RecordClerk s_dictionaryClerk;
			private static RecordClerk s_reversalIndexClerk;

			private readonly RecordClerk m_currentClerk;

			private ClerkActivator(RecordClerk currentClerk)
			{
				m_currentClerk = currentClerk;
			}

			#region disposal
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");

				if (disposing)
				{
					s_dictionaryClerk?.BecomeInactive();
					s_reversalIndexClerk?.BecomeInactive();
					m_currentClerk?.ActivateUI(true);
				}
			}

			~ClerkActivator()
			{
				Dispose(false);
			}
			#endregion disposal

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

			public static ClerkActivator ActivateClerkMatchingExportType(string exportType, IPublisher publisher)
			{
				var isDictionary = exportType == DictionaryType;
				const string area = "lexicon";
				var tool = isDictionary ? "lexiconDictionary" : "reversalToolEditComplete";
				var collector = new XmlNode[1];
				var parameter = new Tuple<string, string, XmlNode[]>(area, tool, collector);
				publisher.Publish("GetContentControlParameters", parameter);
				var parameters = collector[0].SelectSingleNode(".//parameters[@clerk]");
				var currentClerk = RecordClerk.ActiveRecordClerkRepository.ActiveRecordClerk;
				if (DoesClerkMatchParams(currentClerk, parameters))
					return null; // No need to juggle clerks if the one we want is already active

				var tempClerk = isDictionary ? s_dictionaryClerk : s_reversalIndexClerk;
				if (tempClerk == null || tempClerk.IsDisposed)
				{
#if RANDYTODO
					// TODO: "GetRecordClerk" will only work if one or both clerks are now in the repository.
					// TODO: When xWorks is assimilated into Language Explorer, then this call can use the factory method overload of the method.
#endif
					tempClerk = RecordClerk.ActiveRecordClerkRepository.GetRecordClerk(isDictionary ? "entries" : "AllReversalEntries");
					CacheClerk(exportType, tempClerk);
				}
#if RANDYTODO
				// TODO: Jason, Does Flex support having multiple main clerks be active at the same time?
				// TODO: Making this temp clerk active, means it and 'currentClerk' are both active at the same time.
				// TODO: It also seems like tempClerk is never deactivated. A: I set the Dispose call on ClerkActivator to deactivate both of those static clerks, if present.
#endif
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
		private sealed class ReversalIndexActivator : IDisposable
		{
			private readonly string m_sCurrentRevIdxGuid;
			private readonly IPropertyTable m_propertyTable;
			private readonly RecordClerk m_clerk;

			private ReversalIndexActivator(string currentRevIdxGuid, IPropertyTable propertyTable, RecordClerk clerk)
			{
				m_sCurrentRevIdxGuid = currentRevIdxGuid;
				m_propertyTable = propertyTable;
				m_clerk = clerk;
			}

#region disposal
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
				string dummy;
				if(disposing)
					ActivateReversalIndexIfNeeded(m_sCurrentRevIdxGuid, m_propertyTable, m_clerk, out dummy);
			}

			~ReversalIndexActivator()
			{
				Dispose(false);
			}
#endregion disposal

			public static ReversalIndexActivator ActivateReversalIndex(string reversalWs, IPropertyTable propertyTable, LcmCache cache, RecordClerk activeRecordClerk)
			{
				if (reversalWs == null)
					return null;
				var reversalGuid = cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances()
					.First(revIdx => revIdx.WritingSystem == reversalWs).Guid;
				return ActivateReversalIndex(reversalGuid, propertyTable, activeRecordClerk);
			}

			public static ReversalIndexActivator ActivateReversalIndex(Guid reversalGuid, IPropertyTable propertyTable, RecordClerk activeRecordClerk)
			{
				string originalReversalIndexGuid;
				return ActivateReversalIndexIfNeeded(reversalGuid.ToString(), propertyTable, activeRecordClerk, out originalReversalIndexGuid)
					? new ReversalIndexActivator(originalReversalIndexGuid, propertyTable, activeRecordClerk)
					: null;
			}

			/// <returns>true iff activation was needed (the requested Reversal Index was not already active)</returns>
			private static bool ActivateReversalIndexIfNeeded(string newReversalGuid, IPropertyTable propertyTable, RecordClerk clerk, out string oldReversalGuid)
			{
				oldReversalGuid = propertyTable.GetValue<string>("ReversalIndexGuid", null);
				if (newReversalGuid == null || newReversalGuid == oldReversalGuid)
					return false;
				// Set the reversal index guid property so that the right guid is found down in DictionaryPublicationDecorater.GetEntriesToPublish,
				// and manually call OnPropertyChanged to cause LexEdDll ReversalClerk.ChangeOwningObject(guid) to be called. This causes the
				// right reversal content to be exported, fixing LT-17011.
				// RBR comment: Needing to do both is an indication of a pathological state. Setting the property calls OnPropertyChanged to all Mediator clients (now Pub/Sub subscibers).
				// If 'clerk is not getting that, as a result, that shows it is not a current player. The questions then become:
				//		1. why is it not getting that broadcast? and
				//		2. What needs to change, so 'clerk' is able to get the message?
				// In short, this code is programming to some other bug.
				propertyTable.SetProperty("ReversalIndexGuid", newReversalGuid, true, true);
				if (clerk != null)
					clerk.OnPropertyChanged("ReversalIndexGuid");
				return true;
			}
		}
		internal sealed class PublicationActivator : IDisposable
		{
			private readonly string m_currentPublication;
			private readonly IPropertyTable m_propertyTable;

			public PublicationActivator(IPropertyTable propertyTable)
			{
				m_currentPublication = propertyTable.GetValue<string>("SelectedPublication", null);
				m_propertyTable = propertyTable;
			}

#region disposal
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
				if (disposing && !string.IsNullOrEmpty(m_currentPublication))
					m_propertyTable.SetProperty("SelectedPublication", m_currentPublication, false, true);
			}

			~PublicationActivator()
			{
				Dispose(false);
			}
#endregion disposal

			public void ActivatePublication(string publication)
			{
				// Don't publish the property change: doing so may refresh the Dictionary (or Reversal) preview in the main window;
				// we want to activate the Publication for export purposes only.
				m_propertyTable.SetProperty("SelectedPublication", publication, false, true);
			}
		}
	}
}
