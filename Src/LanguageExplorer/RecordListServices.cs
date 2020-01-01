// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords;
using SIL.Code;

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
		private static readonly Dictionary<IntPtr, Tuple<DataNavigationManager, ParserMenuManager, IRecordListRepositoryForTools>> Mapping = new Dictionary<IntPtr, Tuple<DataNavigationManager, ParserMenuManager, IRecordListRepositoryForTools>>();

		internal static void Setup(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));

			var handle = ((Form)majorFlexComponentParameters.MainWindow).Handle;
			if (Mapping.ContainsKey(handle))
			{
				throw new InvalidOperationException("Do not setup the window more than once.");
			}
			Mapping.Add(handle, new Tuple<DataNavigationManager, ParserMenuManager, IRecordListRepositoryForTools>(majorFlexComponentParameters.DataNavigationManager, majorFlexComponentParameters.ParserMenuManager, majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository)));
		}

		internal static void TearDown(IntPtr handle)
		{
			Mapping.Remove(handle);
		}

		internal static void SetRecordList(IntPtr handle, IRecordList recordList)
		{
			var dataForWindow = Mapping[handle];
			if (ReferenceEquals(dataForWindow.Item3.ActiveRecordList, recordList))
			{
				// already set
				return;
			}
			dataForWindow.Item1.RecordList = recordList;
			dataForWindow.Item2.MyRecordList = recordList;
			dataForWindow.Item3.ActiveRecordList = recordList;
		}

		/// <summary>
		/// Create a copy of a List containing IManyOnePathSortItem instances.
		/// </summary>
		internal static List<IManyOnePathSortItem> Clone(this IList<IManyOnePathSortItem> me)
		{
			return new List<IManyOnePathSortItem>(me);
		}

		/// <summary>
		/// Sort a List of IManyOnePathSortItem instances.
		/// </summary>
		internal static void Sort(this List<IManyOnePathSortItem> me, IComparer comparer)
		{
			me.Sort(0, me.Count - 1, comparer, me.Clone());
		}

		/// <summary>
		/// The actual implementation
		/// </summary>
		private static void Sort(this IList<IManyOnePathSortItem> me, int left, int right, IComparer comparer, List<IManyOnePathSortItem> secondaryList)
		{
			if (secondaryList.Count != me.Count)
			{
				throw new ArgumentOutOfRangeException($"Primary count '{me.Count}' does not match the secondary (primary clone) count '{secondaryList.Count}'.");
			}
			if (right > left)
			{
				var middle = (left + right) / 2;
				Sort(me, left, middle, comparer, secondaryList);
				Sort(me, middle + 1, right, comparer, secondaryList);
				int i;
				for (i = middle + 1; i > left; i--)
				{
					secondaryList[i - 1] = me[i - 1];
				}
				int j;
				for (j = middle; j < right; j++)
				{
					secondaryList[right + middle - j] = me[j + 1];
				}
				for (var k = left; k <= right; k++)
				{
					me[k] = comparer.Compare(secondaryList[i], secondaryList[j]) < 0 ? secondaryList[i++] : secondaryList[j--];
				}
			}
		}
	}
}