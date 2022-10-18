// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using LanguageExplorer.DictionaryConfiguration;
using Newtonsoft.Json.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer
{
	internal sealed class PublicationActivator : IDisposable
	{
		private readonly string _currentPublication;
		private readonly IPropertyTable _propertyTable;
		private bool _isDisposed;

		internal PublicationActivator(IPropertyTable propertyTable)
		{
			_currentPublication = propertyTable.GetValue<string>(LanguageExplorerConstants.SelectedPublication, null);
			_propertyTable = propertyTable;
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
				if (!string.IsNullOrEmpty(_currentPublication))
				{
					_propertyTable.SetProperty(LanguageExplorerConstants.SelectedPublication, _currentPublication, doBroadcastIfChanged: true);
				}
			}

			_isDisposed = true;
		}

		~PublicationActivator()
		{
			Dispose(false);
		}
		#endregion disposal

		internal void ActivatePublication(string publication)
		{
			// Don't publish the property change: doing so may refresh the Dictionary (or Reversal) preview in the main window;
			// we want to activate the Publication for export purposes only.
			_propertyTable.SetProperty(LanguageExplorerConstants.SelectedPublication, publication, doBroadcastIfChanged: true);
		}

		internal JObject ExportDictionaryContentJson(string siteName,
			IEnumerable<string> templateFileNames,
			IEnumerable<DictionaryConfigurationModel> reversals,
			string configPath = null, string exportPath = null)
		{
			using (RecordListActivator.ActivateRecordListMatchingExportType(LanguageExplorerConstants.DictionaryType, _propertyTable.GetValue<MajorFlexComponentParameters>(LanguageExplorerConstants.MajorFlexComponentParameters).StatusBar, _propertyTable))
			{
				var cache = _propertyTable.GetValue<LcmCache>(FwUtilsConstants.cache);
				ConfiguredLcmGenerator.GetPublicationDecoratorAndEntries(_propertyTable, out var entriesToSave, LanguageExplorerConstants.DictionaryType, cache, _propertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList);
				return LcmJsonGenerator.GenerateDictionaryMetaData(siteName, templateFileNames, reversals, entriesToSave, configPath, exportPath, cache, _propertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList);
			}
		}
	}
}