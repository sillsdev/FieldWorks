// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Areas.TextsAndWords;
using SIL.Code;

namespace LanguageExplorer
{
	/// <summary>
	/// Helper class for setting values in various panels in the main status bar on the main window.
	/// </summary>
	internal static class RecordListServices
	{
		private static DataNavigationManager _dataNavigationManager;
		private static ParserMenuManager _parserMenuManager;
		private static IRecordListRepositoryForTools _recordListRepositoryForTools;

		internal static void Setup(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));

			_dataNavigationManager = majorFlexComponentParameters.DataNavigationManager;
			_parserMenuManager = majorFlexComponentParameters.ParserMenuManager;
			_recordListRepositoryForTools = majorFlexComponentParameters.RecordListRepositoryForTools;
		}

		internal static void TearDown(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));

			_dataNavigationManager = null;
			_parserMenuManager = null;
			_recordListRepositoryForTools = null;
		}

		internal static void SetRecordList(IRecordList recordList)
		{
			_dataNavigationManager.RecordList = recordList;
			_parserMenuManager.MyRecordList = recordList;
			_recordListRepositoryForTools.ActiveRecordList = recordList;
		}
	}
}