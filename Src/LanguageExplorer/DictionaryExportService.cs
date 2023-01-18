// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.DictionaryConfiguration;
using Newtonsoft.Json.Linq;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace LanguageExplorer
{
	internal class DictionaryExportService
	{
		private readonly IPropertyTable _propertyTable;
		private LcmCache Cache { get; }
		private IRecordList MyRecordList { get; }
		private readonly StatusBar _statusBar;
		private const int BatchSize = 50; // number of entries to send to Webonary in a single post

		public DictionaryExportService(LcmCache cache, IRecordList activeRecordList, IPropertyTable propertyTable, StatusBar statusBar)
		{
			Cache = cache;
			MyRecordList = activeRecordList;
			_propertyTable = propertyTable;
			_statusBar = statusBar;
		}

		public int CountDictionaryEntries(DictionaryConfigurationModel config)
		{
			int[] entries;
			using (RecordListActivator.ActivateRecordListMatchingExportType(LanguageExplorerConstants.DictionaryType, _statusBar, _propertyTable))
			{
				ConfiguredLcmGenerator.GetPublicationDecoratorAndEntries(_propertyTable, out entries, LanguageExplorerConstants.DictionaryType, Cache, MyRecordList);
			}
			return entries.Count(e => IsGenerated(Cache, config, e));
		}

		/// <summary>
		/// Determines how many times the entry with the given HVO is generated for the given config (usually 0 or 1,
		/// but can be more if the entry matches more than one Minor Entry node)
		/// </summary>
		internal static bool IsGenerated(LcmCache cache, DictionaryConfigurationModel config, int hvo)
		{
			var entry = (ILexEntry)cache.ServiceLocator.GetObject(hvo);
			if (ConfiguredLcmGenerator.IsMainEntry(entry, config))
			{
				return config.Parts[0].IsEnabled && (!entry.ComplexFormEntryRefs.Any() || ConfiguredLcmGenerator.IsListItemSelectedForExport(config.Parts[0], entry));
			}
			return entry.PublishAsMinorEntry && config.Parts.Skip(1).Any(part => ConfiguredLcmGenerator.IsListItemSelectedForExport(part, entry));
		}

		/// <summary>
		/// Produce a table of reversal index ShortNames and the count of the entries in each of them.
		/// The reversal indexes included will be limited to those ShortNames specified in selectedReversalIndexes.
		/// </summary>
		public SortedDictionary<string, int> GetCountsOfReversalIndexes(IEnumerable<string> selectedReversalIndexes)
		{
			using (RecordListActivator.ActivateRecordListMatchingExportType(LanguageExplorerConstants.ReversalType, _statusBar, _propertyTable))
			{
				var relevantReversalIndexesAndTheirCounts = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances()
					.Select(repo => Cache.ServiceLocator.GetObject(repo.Guid) as IReversalIndex)
					.Where(ri => ri != null && selectedReversalIndexes.Any(s => s.Contains(ri.ShortName)))
					.ToDictionary(ri => ri.ShortName, CountReversalIndexEntries);
				return new SortedDictionary<string, int>(relevantReversalIndexesAndTheirCounts);
			}
		}

		internal int CountReversalIndexEntries(IReversalIndex ri)
		{
			using (ReversalIndexActivator.ActivateReversalIndex(ri.Guid, _propertyTable, MyRecordList))
			{
				ConfiguredLcmGenerator.GetPublicationDecoratorAndEntries(_propertyTable, out var entries, LanguageExplorerConstants.ReversalType, Cache, MyRecordList);
				return entries.Length;
			}
		}

		public void ExportDictionaryContent(string xhtmlPath, DictionaryConfigurationModel configuration = null, IThreadedProgress progress = null)
		{
			using (RecordListActivator.ActivateRecordListMatchingExportType(LanguageExplorerConstants.DictionaryType, _statusBar, _propertyTable))
			{
				configuration = configuration ?? new DictionaryConfigurationModel(DictionaryConfigurationServices.GetCurrentConfiguration(_propertyTable, "Dictionary"), Cache);
				ExportConfiguredXhtml(xhtmlPath, configuration, LanguageExplorerConstants.DictionaryType, progress);
			}
		}

		public void ExportReversalContent(string xhtmlPath, string reversalWs = null, DictionaryConfigurationModel configuration = null, IThreadedProgress progress = null)
		{
			using (RecordListActivator.ActivateRecordListMatchingExportType(LanguageExplorerConstants.ReversalType, _statusBar, _propertyTable))
			using (ReversalIndexActivator.ActivateReversalIndex(reversalWs, _propertyTable, Cache, MyRecordList))
			{
				configuration = configuration ?? new DictionaryConfigurationModel(DictionaryConfigurationServices.GetCurrentConfiguration(_propertyTable, "ReversalIndex"), Cache);
				ExportConfiguredXhtml(xhtmlPath, configuration, LanguageExplorerConstants.ReversalType, progress);
			}
		}

		private void ExportConfiguredXhtml(string xhtmlPath, DictionaryConfigurationModel configuration, string exportType, IThreadedProgress progress)
		{
			var publicationDecorator = ConfiguredLcmGenerator.GetPublicationDecoratorAndEntries(_propertyTable, out var entriesToSave, exportType, Cache, MyRecordList);
			if (progress != null)
			{
				progress.Maximum = entriesToSave.Length;
			}
			LcmXhtmlGenerator.SavePublishedHtmlWithStyles(entriesToSave, publicationDecorator, int.MaxValue, configuration, _propertyTable, Cache, MyRecordList, xhtmlPath, progress);
		}

		public List<JArray> ExportConfiguredJson(string folderPath, DictionaryConfigurationModel configuration)
		{
			using (RecordListActivator.ActivateRecordListMatchingExportType(LanguageExplorerConstants.DictionaryType, _statusBar, _propertyTable))
			{
				var activeRecordListRepository = _propertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository);
				var publicationDecorator = ConfiguredLcmGenerator.GetPublicationDecoratorAndEntries(_propertyTable, out var entriesToSave,
					LanguageExplorerConstants.DictionaryType, Cache, activeRecordListRepository.ActiveRecordList);
				return LcmJsonGenerator.SavePublishedJsonWithStyles(entriesToSave, publicationDecorator, BatchSize, configuration, _propertyTable,
					Path.Combine(folderPath, "configured.json"), null);
			}
		}

		public List<JArray> ExportConfiguredReversalJson(string folderPath, string reversalWs, out int[] entryIds,
			DictionaryConfigurationModel configuration = null, IThreadedProgress progress = null)
		{
			Guard.AgainstNull(reversalWs, nameof(reversalWs));
			using (RecordListActivator.ActivateRecordListMatchingExportType(LanguageExplorerConstants.ReversalType, _statusBar, _propertyTable))
			using (ReversalIndexActivator.ActivateReversalIndex(reversalWs, _propertyTable, Cache, MyRecordList))
			{
				var activeRecordListRepository = _propertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository);
				var publicationDecorator = ConfiguredLcmGenerator.GetPublicationDecoratorAndEntries(_propertyTable, out entryIds, LanguageExplorerConstants.ReversalType, Cache, activeRecordListRepository.ActiveRecordList);
				return LcmJsonGenerator.SavePublishedJsonWithStyles(entryIds, publicationDecorator, BatchSize, configuration, _propertyTable, Path.Combine(folderPath, $"reversal_{reversalWs}.json"), null);
			}
		}

		private sealed class ReversalIndexActivator : IDisposable
		{
			private readonly string _currentRevIdxGuid;
			private readonly IPropertyTable _propertyTable;
			private readonly IRecordList _recordList;
			private bool _isDisposed;

			private ReversalIndexActivator(string currentRevIdxGuid, IPropertyTable propertyTable, IRecordList recordList)
			{
				_currentRevIdxGuid = currentRevIdxGuid;
				_propertyTable = propertyTable;
				_recordList = recordList;
			}

			#region disposal
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
				if (_isDisposed)
				{
					// No need to run it more than once.
					return;
				}

				if (disposing)
				{
					ActivateReversalIndexIfNeeded(_currentRevIdxGuid, _propertyTable, _recordList, out _);
				}

				_isDisposed = true;
			}

			~ReversalIndexActivator()
			{
				Dispose(false);
			}
			#endregion disposal

			internal static ReversalIndexActivator ActivateReversalIndex(string reversalWs, IPropertyTable propertyTable, LcmCache cache, IRecordList activeRecordList)
			{
				if (reversalWs == null)
				{
					return null;
				}
				var reversalGuid = cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances().First(revIdx => revIdx.WritingSystem == reversalWs).Guid;
				return ActivateReversalIndex(reversalGuid, propertyTable, activeRecordList);
			}

			internal static ReversalIndexActivator ActivateReversalIndex(Guid reversalGuid, IPropertyTable propertyTable, IRecordList activeRecordList)
			{
				return ActivateReversalIndexIfNeeded(reversalGuid.ToString(), propertyTable, activeRecordList, out var originalReversalIndexGuid)
					? new ReversalIndexActivator(originalReversalIndexGuid, propertyTable, activeRecordList) : null;
			}

			/// <returns>true iff activation was needed (the requested Reversal Index was not already active)</returns>
			private static bool ActivateReversalIndexIfNeeded(string newReversalGuid, IPropertyTable propertyTable, IRecordList recordList, out string oldReversalGuid)
			{
				oldReversalGuid = propertyTable.GetValue<string>(LanguageExplorerConstants.ReversalIndexGuid, null);
				if (newReversalGuid == null || newReversalGuid == oldReversalGuid)
				{
					return false;
				}
				// Set the reversal index guid property so that the right guid is found down in DictionaryPublicationDecorater.GetEntriesToPublish.
				propertyTable.SetProperty(LanguageExplorerConstants.ReversalIndexGuid, newReversalGuid, true, true, SettingsGroup.LocalSettings);
				return true;
			}
		}
	}
}