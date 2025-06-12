// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json.Linq;
using SIL.Code;
using SIL.LCModel;
using SIL.LCModel.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	public class DictionaryExportService
	{
		private readonly PropertyTable m_propertyTable;
		private readonly Mediator m_mediator;
		private readonly LcmCache m_cache;

		private const string DictionaryType = "Dictionary";
		private const string ReversalType = "Reversal Index";
		private const int BatchSize = 50; // number of entries to send to Webonary in a single post

		public DictionaryExportService(PropertyTable propertyTable, Mediator mediator)
		{
			m_propertyTable = propertyTable;
			m_mediator = mediator;
			m_cache = propertyTable.GetValue<LcmCache>("cache");
		}

		public int CountDictionaryEntries(DictionaryConfigurationModel config)
		{
			int[] entries;
			using(ClerkActivator.ActivateClerkMatchingExportType(DictionaryType, m_propertyTable, m_mediator))
				ConfiguredLcmGenerator.GetPublicationDecoratorAndEntries(m_propertyTable, out entries, DictionaryType);
			return entries.Count(e => IsGenerated(m_cache, config, e));
		}

		/// <summary>
		/// Determines how many times the entry with the given HVO is generated for the given config (usually 0 or 1,
		/// but can be more if the entry matches more than one Minor Entry node)
		/// </summary>
		internal static bool IsGenerated(LcmCache cache, DictionaryConfigurationModel config, int hvo)
		{
			var entry = (ILexEntry)cache.ServiceLocator.GetObject(hvo);
			if (ConfiguredLcmGenerator.IsMainEntry(entry, config))
				return config.Parts[0].IsEnabled && (!entry.ComplexFormEntryRefs.Any() || ConfiguredLcmGenerator.IsListItemSelectedForExport(config.Parts[0], entry));
			return entry.PublishAsMinorEntry && config.Parts.Skip(1).Any(part => ConfiguredLcmGenerator.IsListItemSelectedForExport(part, entry));
		}

		/// <summary>
		/// Produce a table of reversal index ShortNames and the count of the entries in each of them.
		/// The reversal indexes included will be limited to those ShortNames specified in selectedReversalIndexes.
		/// </summary>
		public SortedDictionary<string,int> GetCountsOfReversalIndexes(IEnumerable<string> selectedReversalIndexes)
		{
			using (ClerkActivator.ActivateClerkMatchingExportType(ReversalType, m_propertyTable, m_mediator))
			{
				var relevantReversalIndexesAndTheirCounts = m_cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances()
					.Select(repo => m_cache.ServiceLocator.GetObject(repo.Guid) as IReversalIndex)
					.Where(ri => ri != null && selectedReversalIndexes.Any(s => s.Contains(ri.ShortName)))
					.ToDictionary(ri => ri.ShortName, CountReversalIndexEntries);

				return new SortedDictionary<string,int> (relevantReversalIndexesAndTheirCounts);
			}
		}

		internal int CountReversalIndexEntries(IReversalIndex ri)
		{
			int[] entries;
			using (ReversalIndexActivator.ActivateReversalIndex(ri.Guid, m_propertyTable))
				ConfiguredLcmGenerator.GetPublicationDecoratorAndEntries(m_propertyTable, out entries, ReversalType);
			return entries.Length;
		}

		public void ExportDictionaryForWord(string filePath, DictionaryConfigurationModel configuration = null, IThreadedProgress progress = null)
		{
			using (ClerkActivator.ActivateClerkMatchingExportType(DictionaryType, m_propertyTable, m_mediator))
			{
			  configuration = configuration ?? new DictionaryConfigurationModel(DictionaryConfigurationListener.GetCurrentConfiguration(m_propertyTable, "Dictionary"), m_cache);
			  var publicationDecorator = ConfiguredLcmGenerator.GetPublicationDecoratorAndEntries(m_propertyTable, out var entriesToSave, DictionaryType);
			  if (progress != null)
				  progress.Maximum = entriesToSave.Length;
			  // TODO: Create and add call to our Word content generator LcmWordGenerator(entriesToSave, publication, configuration, filePath, progress);
			}
		}

		public void ExportReversalForWord(string filePath, string reversalWs, DictionaryConfigurationModel configuration = null, IThreadedProgress progress = null)
		{
			Guard.AgainstNullOrEmptyString(reversalWs, nameof(reversalWs));
			using (ClerkActivator.ActivateClerkMatchingExportType(ReversalType, m_propertyTable, m_mediator))
			using (ReversalIndexActivator.ActivateReversalIndex(reversalWs, m_propertyTable, m_cache))
			{
				configuration = configuration ?? new DictionaryConfigurationModel(
					DictionaryConfigurationListener.GetCurrentConfiguration(m_propertyTable, "ReversalIndex"), m_cache);
				var publicationDecorator = ConfiguredLcmGenerator.GetPublicationDecoratorAndEntries(m_propertyTable, out var entriesToSave, ReversalType);
				if (progress != null)
				  progress.Maximum = entriesToSave.Length;

				// TODO: Create and add call to our Word content generator LcmWordGenerator(entriesToSave, publication, configuration, filePath, progress);
			}
		}

	  public void ExportDictionaryContent(string xhtmlPath, DictionaryConfigurationModel configuration = null, IThreadedProgress progress = null)
		{
			using (ClerkActivator.ActivateClerkMatchingExportType(DictionaryType, m_propertyTable, m_mediator))
			{
				configuration = configuration ?? new DictionaryConfigurationModel(
					DictionaryConfigurationListener.GetCurrentConfiguration(m_propertyTable, "Dictionary"), m_cache);
				ExportConfiguredXhtml(xhtmlPath, configuration, DictionaryType, progress);
			}
		}

		public void ExportReversalContent(string xhtmlPath, string reversalWs = null, DictionaryConfigurationModel configuration = null,
			IThreadedProgress progress = null)
		{
			using (ClerkActivator.ActivateClerkMatchingExportType(ReversalType, m_propertyTable, m_mediator))
			using (ReversalIndexActivator.ActivateReversalIndex(reversalWs, m_propertyTable, m_cache))
			{
				configuration = configuration ?? new DictionaryConfigurationModel(
					DictionaryConfigurationListener.GetCurrentConfiguration(m_propertyTable, "ReversalIndex"), m_cache);
				ExportConfiguredXhtml(xhtmlPath, configuration, ReversalType, progress);
			}
		}

		private void ExportConfiguredXhtml(string xhtmlPath, DictionaryConfigurationModel configuration, string exportType, IThreadedProgress progress)
		{
			var publicationDecorator = ConfiguredLcmGenerator.GetPublicationDecoratorAndEntries(m_propertyTable, out var entriesToSave, exportType);
			if (progress != null)
				progress.Maximum = entriesToSave.Length;
			LcmXhtmlGenerator.SavePublishedHtmlWithStyles(entriesToSave, publicationDecorator, int.MaxValue, configuration, m_propertyTable, xhtmlPath, progress);
		}

		public List<JArray> ExportConfiguredJson(string folderPath, DictionaryConfigurationModel configuration, out int[] entryIds)
		{
			using (ClerkActivator.ActivateClerkMatchingExportType(DictionaryType, m_propertyTable, m_mediator))
			{
				var publicationDecorator = ConfiguredLcmGenerator.GetPublicationDecoratorAndEntries(m_propertyTable,
					out var entriesToSave, DictionaryType);
				return LcmJsonGenerator.SavePublishedJsonWithStyles(entriesToSave, publicationDecorator, BatchSize, configuration, m_propertyTable,
					Path.Combine(folderPath, "configured.json"), null, out entryIds);
			}
		}

		public List<JArray> ExportConfiguredReversalJson(string folderPath, string reversalWs, out int[] entryIds,
			DictionaryConfigurationModel configuration = null, IThreadedProgress progress = null)
		{
			Guard.AgainstNull(reversalWs, nameof(reversalWs));
			using (ClerkActivator.ActivateClerkMatchingExportType(ReversalType, m_propertyTable, m_mediator))
			using (ReversalIndexActivator.ActivateReversalIndex(reversalWs, m_propertyTable, m_cache))
			{
				var publicationDecorator = ConfiguredLcmGenerator.GetPublicationDecoratorAndEntries(m_propertyTable,
						out var entriesToSave, ReversalType);
				return LcmJsonGenerator.SavePublishedJsonWithStyles(entriesToSave, publicationDecorator, BatchSize,
					configuration, m_propertyTable, Path.Combine(folderPath, $"reversal_{reversalWs}.json"), null, out entryIds);
			}
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
				if (disposing && m_currentClerk != null && !m_currentClerk.IsDisposed)
					m_currentClerk.ActivateUI(true);
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

			public static ClerkActivator ActivateClerkMatchingExportType(string exportType, PropertyTable  propertyTable, Mediator mediator)
			{
				var isDictionary = exportType == DictionaryType;
				const string area = "lexicon";
				var tool = isDictionary ? "lexiconDictionary" : "reversalToolEditComplete";
				var collector = new XmlNode[1];
				var parameter = new Tuple<string, string, XmlNode[]>(area, tool, collector);
				mediator.SendMessage("GetContentControlParameters", parameter);
				var parameters = collector[0].SelectSingleNode(".//parameters[@clerk]");
				var currentClerk = propertyTable.GetValue<RecordClerk>("ActiveClerk", null);
				if (DoesClerkMatchParams(currentClerk, parameters))
					return null; // No need to juggle clerks if the one we want is already active

				var tempClerk = isDictionary ? s_dictionaryClerk : s_reversalIndexClerk;
				if (tempClerk == null || tempClerk.IsDisposed)
				{
					tempClerk = RecordClerk.FindClerk(propertyTable, isDictionary ? "entries" : "AllReversalEntries");
					if (tempClerk == null || tempClerk.IsDisposed)
						tempClerk = RecordClerkFactory.CreateClerk(mediator, propertyTable, parameters, true);
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
		private sealed class ReversalIndexActivator : IDisposable
		{
			private readonly string m_sCurrentRevIdxGuid;
			private readonly PropertyTable m_propertyTable;
			private readonly RecordClerk m_clerk;

			private ReversalIndexActivator(string currentRevIdxGuid, PropertyTable propertyTable, RecordClerk clerk)
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

			public static ReversalIndexActivator ActivateReversalIndex(string reversalWs, PropertyTable propertyTable, LcmCache cache)
			{
				if (reversalWs == null)
					return null;
				var reversalGuid = cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances()
					.First(revIdx => revIdx.WritingSystem == reversalWs).Guid;
				return ActivateReversalIndex(reversalGuid, propertyTable);
			}

			public static ReversalIndexActivator ActivateReversalIndex(Guid reversalGuid, PropertyTable propertyTable)
			{
				var clerk = propertyTable.GetValue<RecordClerk>("ActiveClerk", null);
				string originalReversalIndexGuid;
				return ActivateReversalIndexIfNeeded(reversalGuid.ToString(), propertyTable, clerk, out originalReversalIndexGuid)
					? new ReversalIndexActivator(originalReversalIndexGuid, propertyTable, clerk)
					: null;
			}

			/// <returns>true iff activation was needed (the requested Reversal Index was not already active)</returns>
			private static bool ActivateReversalIndexIfNeeded(string newReversalGuid, PropertyTable propertyTable, RecordClerk clerk, out string oldReversalGuid)
			{
				oldReversalGuid = propertyTable.GetStringProperty("ReversalIndexGuid", null);
				if (newReversalGuid == null || newReversalGuid == oldReversalGuid)
					return false;
				// Set the reversal index guid property so that the right guid is found down in DictionaryPublicationDecorater.GetEntriesToPublish,
				// and manually call OnPropertyChanged to cause LexEdDll ReversalClerk.ChangeOwningObject(guid) to be called. This causes the
				// right reversal content to be exported, fixing LT-17011.
				propertyTable.SetProperty("ReversalIndexGuid", newReversalGuid, true);
				if (clerk != null)
					clerk.OnPropertyChanged("ReversalIndexGuid");
				return true;
			}
		}
		internal sealed class PublicationActivator : IDisposable
		{
			private readonly string m_currentPublication;
			private readonly PropertyTable m_propertyTable;

			public PublicationActivator(PropertyTable propertyTable)
			{
				m_currentPublication = propertyTable.GetStringProperty("SelectedPublication", null);
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
					m_propertyTable.SetProperty("SelectedPublication", m_currentPublication, false);
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
				m_propertyTable.SetProperty("SelectedPublication", publication, false);
			}
		}

		public JObject ExportDictionaryContentJson(string siteName,
			IEnumerable<string> templateFileNames,
			IEnumerable<DictionaryConfigurationModel> reversals,
			int[] entryIds,
			string exportPath = null)
		{
			using (ClerkActivator.ActivateClerkMatchingExportType(DictionaryType, m_propertyTable, m_mediator))
			{
				var clerk = m_propertyTable.GetValue<RecordClerk>("ActiveClerk", null);
				return LcmJsonGenerator.GenerateDictionaryMetaData(siteName, templateFileNames, reversals, entryIds, exportPath, m_cache, clerk);
			}
		}
	}
}
