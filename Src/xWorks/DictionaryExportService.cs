// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
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
		private Guid m_origRevOwningObjectGuid;
		private ArrayList m_origSortedObjects;

		internal const string DictionaryType = "Dictionary";
		internal const string ReversalType = "Reversal Index";
		private const int BatchSize = 50; // number of entries to send to Webonary in a single post

		public DictionaryExportService(PropertyTable propertyTable, Mediator mediator)
		{
			m_propertyTable = propertyTable;
			m_mediator = mediator;
			m_cache = propertyTable.GetValue<LcmCache>("cache");
		}

		public int CountDictionaryEntries(DictionaryConfigurationModel config, string pubName)
		{
			GetDictionaryEntries(pubName, false, out RecordClerk clerk, out DictionaryPublicationDecorator decorator,
				out int[] entries);
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
		public SortedDictionary<string,int> GetCountsOfReversalIndexes(IEnumerable<string> selectedReversalIndexes, string pubName)
		{
			var retDict = new SortedDictionary<string, int>();
			var revClerk = GetReversalClerk();
			var decorator = ConfiguredLcmGenerator.GetDecorator(m_propertyTable, m_cache, revClerk, pubName);
			try
			{
				StoreReversalData(revClerk, false);
				foreach (var reversal in m_cache.ServiceLocator
							 .GetInstance<IReversalIndexRepository>().AllInstances())
				{
					var ri = (m_cache.ServiceLocator.GetObject(reversal.Guid) as IReversalIndex);
					if (ri != null && selectedReversalIndexes.Any(s => s.Contains(ri.ShortName)))
					{
						var revConfig = DictionaryConfigurationModel.GetReversalConfigurationModel(
								ri.WritingSystem, m_cache, m_propertyTable);

						int count = CountReversalIndexEntries(ri, revClerk, decorator, revConfig);
						retDict.Add(ri.ShortName, count);

						// If we just sorted for the original reversal, then keep it's sorted objects to restore later.
						UpdateSortedObjects(revClerk, reversal.Guid, false);
					}
				}
			}
			finally
			{
				// Restore data.
				RestoreReversalData(revClerk, false);
			}

			return retDict;
		}

		internal int CountReversalIndexEntries(IReversalIndex ri, RecordClerk revClerk, DictionaryPublicationDecorator decorator,
			DictionaryConfigurationModel revConfig)
		{
			var entries = revClerk.GetReversalFilteredAndSortedEntries(ri.Guid, decorator, revConfig);
			return entries.Length;
		}

		public void ExportWordDictionary(string filePath, int[] entriesToSave, RecordClerk clerk,
			DictionaryPublicationDecorator pubDecorator, IThreadedProgress progress)
		{
			if (progress != null)
			  progress.Maximum = entriesToSave.Length;

			var dictConfig = new DictionaryConfigurationModel(
				DictionaryConfigurationListener.GetCurrentConfiguration(m_propertyTable, "Dictionary"), m_cache);
			LcmWordGenerator.SavePublishedDocx(entriesToSave, clerk, pubDecorator, int.MaxValue, dictConfig, m_propertyTable,
				filePath, progress);
		}

		public void ExportWordReversal(string filePath, string reversalWs, int[] entriesToSave, RecordClerk revClerk,
			DictionaryPublicationDecorator pubDecorator, DictionaryConfigurationModel revConfig, IThreadedProgress progress)
		{
			Guard.AgainstNullOrEmptyString(reversalWs, nameof(reversalWs));

			if (progress != null)
			  progress.Maximum = entriesToSave.Length;

			string reversalFilePath = filePath.Split(new string[] { ".docx"}, StringSplitOptions.None)[0] + "-reversal-" + reversalWs + ".docx";
			LcmWordGenerator.SavePublishedDocx(entriesToSave, revClerk, pubDecorator, int.MaxValue, revConfig, m_propertyTable,
				reversalFilePath, progress);
		}

		public void ExportXhtmlDictionary(string xhtmlPath, RecordClerk clerk, DictionaryPublicationDecorator pubDecorator, int[] entriesToSave,
			IThreadedProgress progress)
		{
			if (progress != null)
				progress.Maximum = entriesToSave.Length;

			var dictConfig = new DictionaryConfigurationModel(
				DictionaryConfigurationListener.GetCurrentConfiguration(m_propertyTable, "Dictionary"), m_cache);
			LcmXhtmlGenerator.SavePublishedHtmlWithStyles(entriesToSave, clerk, pubDecorator, int.MaxValue, dictConfig,
				m_propertyTable, xhtmlPath, progress, true);
		}

		public void ExportXhtmlReversal(string xhtmlPath, RecordClerk revClerk, DictionaryPublicationDecorator pubDecorator, int[] entriesToSave,
			IThreadedProgress progress)
		{
			if (progress != null)
				progress.Maximum = entriesToSave.Length;

			var revConfig = new DictionaryConfigurationModel(
				DictionaryConfigurationListener.GetCurrentConfiguration(m_propertyTable, "ReversalIndex"), m_cache);
			LcmXhtmlGenerator.SavePublishedHtmlWithStyles(entriesToSave, revClerk, pubDecorator, int.MaxValue, revConfig,
				m_propertyTable, xhtmlPath, progress, true);
		}

		public List<JArray> ExportJsonDictionary(string folderPath, DictionaryConfigurationModel config, string pubName, out int[] entryIds)
		{
			GetDictionaryEntries(pubName, false, out RecordClerk clerk,
				out DictionaryPublicationDecorator decorator, out int[] entries);

			return LcmJsonGenerator.SavePublishedJsonWithStyles(entries, decorator, BatchSize, config, m_propertyTable,
				Path.Combine(folderPath, "configured.json"), null, out entryIds);
		}

		public List<JArray> ExportJsonReversal(int[] entries, DictionaryPublicationDecorator decorator, string folderPath,
			string reversalWs, out int[] entryIds, DictionaryConfigurationModel revConfig)
		{
			Guard.AgainstNull(reversalWs, nameof(reversalWs));
			return LcmJsonGenerator.SavePublishedJsonWithStyles(entries, decorator, BatchSize, revConfig, m_propertyTable,
				Path.Combine(folderPath, $"reversal_{reversalWs}.json"), null, out entryIds);
		}

		public JObject ExportDictionaryContentJson(string siteName,
			IEnumerable<string> templateFileNames,
			IEnumerable<DictionaryConfigurationModel> reversals,
			int[] entryIds,
			string exportPath = null)
		{
			var clerk = GetDictionaryClerk();
			return LcmJsonGenerator.GenerateDictionaryMetaData(siteName, templateFileNames, reversals, entryIds, exportPath, m_cache, clerk);
		}

		/// <summary>
		/// Gets or creates a dictionary clerk. Does not update the Gui, change properties, or
		/// send notifications.
		/// </summary>
		public RecordClerk GetDictionaryClerk()
		{
			var clerk = RecordClerk.FindClerk(m_propertyTable, "entries");

			// If there isn't yet a dictionary clerk then create one.
			if (clerk == null)
			{
				// Get the node for the dictionary.
				XWindow.TryGetToolNode("lexicon", "lexiconDictionary", m_propertyTable, out XmlNode node);
				node = node.SelectSingleNode("control");
				node = node.SelectSingleNode(".//parameters[@clerk]");

				clerk = RecordClerkFactory.CreateClerk(m_mediator, m_propertyTable, node, false, false);
				clerk.UpdateFiltersAndSortersIfNeeded(false);
			}
			return clerk;
		}

		/// <summary>
		/// Gets or creates a reversal clerk. Does not update the Gui, change properties, or
		/// send notifications.
		/// </summary>
		public RecordClerk GetReversalClerk()
		{
			var reversalClerk = RecordClerk.FindClerk(m_propertyTable, "AllReversalEntries");

			// If there isn't yet a reversal clerk then create one.
			if (reversalClerk == null)
			{
				// Get the node for the reversal.
				XWindow.TryGetToolNode("lexicon", "reversalToolEditComplete", m_propertyTable, out XmlNode node);
				node = node.SelectSingleNode("control/parameters");
				XmlNodeList nodes = node.SelectNodes("control");
				node = nodes[1].SelectSingleNode("parameters/control/parameters");

				reversalClerk = RecordClerkFactory.CreateClerk(m_mediator, m_propertyTable, node, false, false);
			}
			return reversalClerk;
		}

		/// <summary>
		/// Gets the filtered and sorted dictionary entries.
		/// </summary>
		/// <param name="pubName">The name of the publication to use.  If null, then use the current publication.</param>
		/// <param name="stopSuppressingListLoading">If true then after we get the entries stop suppressing list
		/// loading. If we don't then the 'Lexicon Edit' view may be blank the first time viewed.</param>
		public void GetDictionaryEntries(string pubName, bool stopSuppressingListLoading,
			out RecordClerk clerk, out DictionaryPublicationDecorator decorator, out int[] entries)
		{
			clerk = GetDictionaryClerk();
			// Use the current publication settings (the current decorator).
			if (string.IsNullOrEmpty(pubName))
			{
				decorator = ConfiguredLcmGenerator.CurrentDecorator(m_propertyTable, m_cache, clerk);
			}
			// Use the specified publication settings.
			else
			{
				decorator = ConfiguredLcmGenerator.GetDecorator(m_propertyTable, m_cache, clerk, pubName);
			}

			entries = clerk.GetDictionaryFilteredAndSortedEntries(decorator);

			// Stop suppressing list loading, or the 'Lexicon Edit' view may be blank the first time viewed.
			if (stopSuppressingListLoading)
			{
				clerk.ListLoadingSuppressed = false;
			}
		}

		/// <summary>
		/// Store some reversal data that will need to be restored after the export is complete.
		/// </summary>
		/// <param name="forTesting">If true then don't store/restore reversal data.</param>
		public void StoreReversalData(RecordClerk revClerk, bool forTesting)
		{
			if (!forTesting)
			{
				m_origRevOwningObjectGuid = revClerk.OwningObject.Guid;
				m_origSortedObjects = (revClerk.SortItemProvider as RecordList).SortedObjects;
			}
		}

		/// <summary>
		/// Restores some reversal data that was modified during the export.
		/// </summary>
		/// <param name="forTesting">If true then don't store/restore reversal data.</param>
		public void RestoreReversalData(RecordClerk revClerk, bool forTesting)
		{
			if (!forTesting)
			{
				var origRevWs = (m_cache.ServiceLocator.GetObject(m_origRevOwningObjectGuid) as IReversalIndex).WritingSystem;
				var origRevConfig = DictionaryConfigurationModel.GetReversalConfigurationModel(origRevWs, m_cache, m_propertyTable);
				revClerk.ChangeOwningObject(m_origRevOwningObjectGuid, false, origRevConfig);
				(revClerk.SortItemProvider as RecordList).SortedObjects = m_origSortedObjects;
			}
		}

		/// <summary>
		/// If we original reversal was updated, then store the updated 'sorted objects'.
		/// </summary>
		/// <param name="revGuid">The reversal that was updated in the reversal clerk.</param>
		/// <param name="forTesting">If true then don't store/restore reversal data.</param>
		public void UpdateSortedObjects(RecordClerk revClerk, Guid revGuid, bool forTesting)
		{
			if (!forTesting && revGuid == m_origRevOwningObjectGuid)
			{
				m_origSortedObjects = (revClerk.SortItemProvider as RecordList).SortedObjects;
			}
		}
	}
}
