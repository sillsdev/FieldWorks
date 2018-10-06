// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer
{
	/// <summary>
	/// Helper class for setting values in various panels in the main status bar on the main window.
	/// </summary>
	/// <remarks>
	/// Since this is a static class and has static data members, the current window calls "Setup" when activated,
	/// and "TearDown", when it is disposed and when it goes inactive. That should allow the currently active
	/// FLEx window to make use of the static data members and the "SetRecordList" method (along with the current RecordList).
	/// </remarks>
	internal static class RecordListServices
	{
		private static Dictionary<IntPtr, Tuple<DataNavigationManager, ParserMenuManager, IRecordListRepositoryForTools>> _mapping = new Dictionary<IntPtr, Tuple<DataNavigationManager, ParserMenuManager, IRecordListRepositoryForTools>>();

		internal static void Setup(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));

			var handle = ((Form)majorFlexComponentParameters.MainWindow).Handle;
			if (_mapping.ContainsKey(handle))
			{
				throw new InvalidOperationException("Do not setup the window more than once.");
			}
			_mapping.Add(handle, new Tuple<DataNavigationManager, ParserMenuManager, IRecordListRepositoryForTools>(majorFlexComponentParameters.DataNavigationManager, majorFlexComponentParameters.ParserMenuManager, majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository)));
		}

		internal static void TearDown(IntPtr handle)
		{
			_mapping.Remove(handle);
		}

		internal static void SetRecordList(IntPtr handle, IRecordList recordList)
		{
			var dataForWindow = _mapping[handle];
			dataForWindow.Item1.RecordList = recordList;
			dataForWindow.Item2.MyRecordList = recordList;
			dataForWindow.Item3.ActiveRecordList = recordList;
		}

		/// <summary>
		/// Fetches the GUID value of the given property, having checked it is a valid object.
		/// If it is not a valid object, the property is removed.
		/// </summary>
		internal static Guid GetObjectGuidIfValid(IPropertyTable propertyTable, string key)
		{
			var sGuid = propertyTable.GetValue<string>(key);
			if (string.IsNullOrEmpty(sGuid))
			{
				return Guid.Empty;
			}

			Guid guid;
			if (!Guid.TryParse(sGuid, out guid))
			{
				return Guid.Empty;
			}

			var cache = propertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache);
			if (cache.ServiceLocator.ObjectRepository.IsValidObjectId(guid))
			{
				return guid;
			}
			propertyTable.RemoveProperty(key);
			return Guid.Empty;
		}
	}
}