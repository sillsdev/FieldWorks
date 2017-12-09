// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer
{
	/// <summary>
	/// Helper class for setting values in various panels in the main status bar on the main window.
	/// </summary>
	internal static class RecordListServices
	{
		internal static void SetRecordList(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
		{
			majorFlexComponentParameters.DataNavigationManager.RecordList = recordList;
			majorFlexComponentParameters.ParserMenuManager.MyRecordList = recordList;
			majorFlexComponentParameters.RecordListRepositoryForTools.ActiveRecordList = recordList;
		}

		internal static void ClearRecordList(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			majorFlexComponentParameters.DataNavigationManager.RecordList = null;
			majorFlexComponentParameters.ParserMenuManager.MyRecordList = null;
			majorFlexComponentParameters.RecordListRepositoryForTools.ActiveRecordList = null;
		}
	}
}