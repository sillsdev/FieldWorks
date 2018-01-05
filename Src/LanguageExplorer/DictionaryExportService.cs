// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;
using LanguageExplorer.Areas;
using LanguageExplorer.Areas.Lexicon;
using LanguageExplorer.Dumpster;
using LanguageExplorer.DictionaryConfiguration;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace LanguageExplorer
{
	internal class DictionaryExportService
	{
		private readonly IPropertyTable m_propertyTable;
		private readonly IPublisher m_publisher;
		private LcmCache Cache { get; }
		private IRecordList MyRecordList { get; }
		private StatusBar _statusBar;

		private const string DictionaryType = "Dictionary";
		private const string ReversalType = "Reversal Index";

		public DictionaryExportService(LcmCache cache, IRecordList activeRecordList, IPropertyTable propertyTable, IPublisher publisher, StatusBar statusBar)
		{
			Cache = cache;
			MyRecordList = activeRecordList;
			m_propertyTable = propertyTable;
			m_publisher = publisher;
			_statusBar = statusBar;
		}

		public int CountDictionaryEntries(DictionaryConfigurationModel config)
		{
			int[] entries;
			using (RecordListActivator.ActivateRecordListMatchingExportType(DictionaryType, _statusBar))
				ConfiguredXHTMLGenerator.GetPublicationDecoratorAndEntries(m_propertyTable, out entries, DictionaryType, Cache, MyRecordList);
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
			using (RecordListActivator.ActivateRecordListMatchingExportType(ReversalType, _statusBar))
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
			using (ReversalIndexActivator.ActivateReversalIndex(ri.Guid, m_propertyTable, MyRecordList))
				ConfiguredXHTMLGenerator.GetPublicationDecoratorAndEntries(m_propertyTable, out entries, ReversalType, Cache, MyRecordList);
			return entries.Length;
		}

		public void ExportDictionaryContent(string xhtmlPath, DictionaryConfigurationModel configuration = null, IThreadedProgress progress = null)
		{
			using (RecordListActivator.ActivateRecordListMatchingExportType(DictionaryType, _statusBar))
			{
				configuration = configuration ?? new DictionaryConfigurationModel(
					DictionaryConfigurationServices.GetCurrentConfiguration(m_propertyTable, "Dictionary"), Cache);
				ExportConfiguredXhtml(xhtmlPath, configuration, DictionaryType, progress);
			}
		}

		public void ExportReversalContent(string xhtmlPath, string reversalWs = null, DictionaryConfigurationModel configuration = null,
			IThreadedProgress progress = null)
		{
			using (RecordListActivator.ActivateRecordListMatchingExportType(ReversalType, _statusBar))
			using (ReversalIndexActivator.ActivateReversalIndex(reversalWs, m_propertyTable, Cache, MyRecordList))
			{
				configuration = configuration ?? new DictionaryConfigurationModel(DictionaryConfigurationServices.GetCurrentConfiguration(m_propertyTable, "ReversalIndex"), Cache);
				ExportConfiguredXhtml(xhtmlPath, configuration, ReversalType, progress);
			}
		}

		private void ExportConfiguredXhtml(string xhtmlPath, DictionaryConfigurationModel configuration, string exportType, IThreadedProgress progress)
		{
			int[] entriesToSave;
			var publicationDecorator = ConfiguredXHTMLGenerator.GetPublicationDecoratorAndEntries(m_propertyTable, out entriesToSave, exportType, Cache, MyRecordList);
			if (progress != null)
				progress.Maximum = entriesToSave.Length;
			ConfiguredXHTMLGenerator.SavePublishedHtmlWithStyles(entriesToSave, publicationDecorator, int.MaxValue, configuration, m_propertyTable, Cache, MyRecordList, xhtmlPath, progress);
		}

		private sealed class RecordListActivator : IDisposable
		{
			private static IRecordList s_dictionaryRecordList;
			private static IRecordList s_reversalIndexRecordList;
			private IRecordList m_currentRecordList;

			private RecordListActivator(IRecordList currentRecordList)
			{
				RecordList.ActiveRecordListRepository.ActiveRecordList = null;
				m_currentRecordList = currentRecordList;
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
					s_dictionaryRecordList?.BecomeInactive();
					s_reversalIndexRecordList?.BecomeInactive();
					RecordList.ActiveRecordListRepository.ActiveRecordList = m_currentRecordList;
				}
				m_currentRecordList = null;
			}

			~RecordListActivator()
			{
				Dispose(false);
			}
			#endregion disposal

			private static void CacheRecordList(string recordListType, IRecordList recordList)
			{
				switch (recordListType)
				{
					case DictionaryType:
						s_dictionaryRecordList = recordList;
						break;
					case ReversalType:
						s_reversalIndexRecordList = recordList;
						break;
				}
			}

			public static RecordListActivator ActivateRecordListMatchingExportType(string exportType, StatusBar statusBar)
			{
				var isDictionary = exportType == DictionaryType;
				const string area = AreaServices.InitialAreaMachineName;
				var tool = isDictionary ? AreaServices.LexiconDictionaryMachineName : AreaServices.ReversalEditCompleteMachineName;
				var controlElement = AreaListener.GetContentControlParameters(null, area, tool);
				Debug.Assert(controlElement != null, "Prepare to be disappointed, since it will be null.");
				var parameters = controlElement.XPathSelectElement(".//parameters[@clerk]");
				var activeRecordList = RecordList.ActiveRecordListRepository.ActiveRecordList;
				if (DoesRecordListMatchParams(activeRecordList, parameters))
					return null; // No need to juggle record lists if the one we want is already active

				var tempRecordList = isDictionary ? s_dictionaryRecordList : s_reversalIndexRecordList;
				if (tempRecordList == null)
				{
					tempRecordList = isDictionary ? ((IRecordListRepositoryForTools)RecordList.ActiveRecordListRepository).GetRecordList(LexiconArea.Entries, statusBar, LexiconArea.EntriesFactoryMethod) : ((IRecordListRepositoryForTools)RecordList.ActiveRecordListRepository).GetRecordList(LexiconArea.AllReversalEntries, statusBar, LexiconArea.AllReversalEntriesFactoryMethod);
					CacheRecordList(exportType, tempRecordList);
				}

				var retval = new RecordListActivator(activeRecordList);
				tempRecordList.ActivateUI(false);
				tempRecordList.UpdateList(true, true);
				return retval; // ensure the current active record list is reactivated after we use another record list temporarily.
			}

			private static bool DoesRecordListMatchParams(IRecordList recordList, XElement parameters)
			{
				if (recordList == null || parameters == null)
					return false;
				var clerkAttr = parameters.Attribute("clerk");
				return clerkAttr != null && clerkAttr.Value == recordList.Id;
			}
		}
		private sealed class ReversalIndexActivator : IDisposable
		{
			private readonly string m_sCurrentRevIdxGuid;
			private readonly IPropertyTable m_propertyTable;
			private readonly IRecordList m_recordList;

			private ReversalIndexActivator(string currentRevIdxGuid, IPropertyTable propertyTable, IRecordList recordList)
			{
				m_sCurrentRevIdxGuid = currentRevIdxGuid;
				m_propertyTable = propertyTable;
				m_recordList = recordList;
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
					ActivateReversalIndexIfNeeded(m_sCurrentRevIdxGuid, m_propertyTable, m_recordList, out dummy);
			}

			~ReversalIndexActivator()
			{
				Dispose(false);
			}
#endregion disposal

			public static ReversalIndexActivator ActivateReversalIndex(string reversalWs, IPropertyTable propertyTable, LcmCache cache, IRecordList activeRecordList)
			{
				if (reversalWs == null)
					return null;
				var reversalGuid = cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances().First(revIdx => revIdx.WritingSystem == reversalWs).Guid;
				return ActivateReversalIndex(reversalGuid, propertyTable, activeRecordList);
			}

			public static ReversalIndexActivator ActivateReversalIndex(Guid reversalGuid, IPropertyTable propertyTable, IRecordList activeRecordList)
			{
				string originalReversalIndexGuid;
				return ActivateReversalIndexIfNeeded(reversalGuid.ToString(), propertyTable, activeRecordList, out originalReversalIndexGuid)
					? new ReversalIndexActivator(originalReversalIndexGuid, propertyTable, activeRecordList)
					: null;
			}

			/// <returns>true iff activation was needed (the requested Reversal Index was not already active)</returns>
			private static bool ActivateReversalIndexIfNeeded(string newReversalGuid, IPropertyTable propertyTable, IRecordList recordList, out string oldReversalGuid)
			{
				oldReversalGuid = propertyTable.GetValue<string>("ReversalIndexGuid", null);
				if (newReversalGuid == null || newReversalGuid == oldReversalGuid)
					return false;
				// Set the reversal index guid property so that the right guid is found down in DictionaryPublicationDecorater.GetEntriesToPublish,
				// and manually call OnPropertyChanged to cause LexEdDll ReversalListBase.ChangeOwningObject(guid) to be called. This causes the
				// right reversal content to be exported, fixing LT-17011.
				// RBR comment: Needing to do both is an indication of a pathological state. Setting the property calls OnPropertyChanged to all Mediator clients (now Pub/Sub subscibers).
				// If 'recordList is not getting that, as a result, that shows it is not a current player. The questions then become:
				//		1. why is it not getting that broadcast? and
				//		2. What needs to change, so 'recordList' is able to get the message?
				// In short, this code is programming to some other bug.
				propertyTable.SetProperty("ReversalIndexGuid", newReversalGuid, true, true);
				recordList?.OnPropertyChanged("ReversalIndexGuid");
				return true;
			}
		}
	}
}
