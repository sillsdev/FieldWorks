// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Areas.Lexicon;
using LanguageExplorer.Areas.Lexicon.Reversals;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer
{
	internal sealed class RecordListActivator : IDisposable
	{
		private static IRecordList s_dictionaryRecordList;
		private static IRecordList s_reversalIndexRecordList;
		private IRecordListRepository _recordListRepository;
		private bool _isDisposed;

		internal IRecordList ActiveRecordList { get; private set; }

		private RecordListActivator(IRecordListRepository recordListRepository, IRecordList currentRecordList)
		{
			_recordListRepository = recordListRepository;
			_recordListRepository.ActiveRecordList = null;
			ActiveRecordList = currentRecordList;
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
				s_dictionaryRecordList?.BecomeInactive();
				s_reversalIndexRecordList?.BecomeInactive();
				_recordListRepository.ActiveRecordList = ActiveRecordList;
			}
			ActiveRecordList = null;
			_recordListRepository = null;

			_isDisposed = true;
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
				case LanguageExplorerConstants.DictionaryType:
					s_dictionaryRecordList = recordList;
					break;
				case LanguageExplorerConstants.ReversalType:
					s_reversalIndexRecordList = recordList;
					break;
			}
		}

		internal static RecordListActivator ActivateRecordListMatchingExportType(string exportType, StatusBar statusBar, IPropertyTable propertyTable)
		{
			var isDictionary = exportType == LanguageExplorerConstants.DictionaryType;
			var recordListId = isDictionary ? LanguageExplorerConstants.Entries : LanguageExplorerConstants.AllReversalEntries;
			var activeRecordListRepository = propertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository);
			var activeRecordList = activeRecordListRepository.ActiveRecordList;
			if (activeRecordList != null && activeRecordList.Id == recordListId)
			{
				return null; // No need to juggle record lists if the one we want is already active
			}
			var tempRecordList = isDictionary ? s_dictionaryRecordList : s_reversalIndexRecordList;
			if (tempRecordList == null)
			{
				tempRecordList = isDictionary ? activeRecordListRepository.GetRecordList(LanguageExplorerConstants.Entries, statusBar, LexiconArea.EntriesFactoryMethod) : activeRecordListRepository.GetRecordList(LanguageExplorerConstants.AllReversalEntries, statusBar, ReversalServices.AllReversalEntriesFactoryMethod);
				CacheRecordList(exportType, tempRecordList);
			}
			var retval = new RecordListActivator(activeRecordListRepository, activeRecordList);
			if (!tempRecordList.IsSubservientRecordList)
			{
				if (propertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList != tempRecordList)
				{
					// Some tests may not have a window.
					if (propertyTable.TryGetValue<Form>(FwUtilsConstants.window, out var form))
					{
						RecordListServices.SetRecordList(form.Handle, tempRecordList);
					}
				}
				tempRecordList.ActivateUI(false);
			}
			tempRecordList.UpdateList(true, true);
			return retval; // ensure the current active record list is reactivated after we use another record list temporarily.
		}
	}
}