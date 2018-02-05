// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.Utils;

namespace LanguageExplorer
{
	/// <summary>
	/// The argument used when we broadcast OnRecordNavigation.
	/// </summary>
	internal class RecordNavigationInfo : IComparable
	{
		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="myRecordList">The list.</param>
		/// <param name="suppressSaveOnChangeRecord"></param>
		/// <param name="skipShowRecord"></param>
		/// <param name="suppressFocusChange"></param>
		public RecordNavigationInfo(IRecordList myRecordList, bool suppressSaveOnChangeRecord, bool skipShowRecord, bool suppressFocusChange)
		{
			MyRecordList = myRecordList;
			HvoOfCurrentObjAtTimeOfNavigation = MyRecordList != null && MyRecordList.CurrentObjectHvo != 0 ? MyRecordList.CurrentObjectHvo : 0;
			SuppressSaveOnChangeRecord = suppressSaveOnChangeRecord;
			SkipShowRecord = skipShowRecord;
			SuppressFocusChange = suppressFocusChange;
		}

		/// <summary>
		///  The list that broadcast the change.
		/// </summary>
		public IRecordList MyRecordList { get; }

		/// <summary>
		/// Whether a change of record should result in a save (and discard of undo items).
		/// This is suppressed if the change is caused by creating or deleting a record.
		/// </summary>
		public bool SuppressSaveOnChangeRecord { get; }

		/// <summary>
		/// Indicates whether the this action should skip ShowRecord
		/// (e.g. to avoid losing the context/pane where the user may be editing.)
		/// </summary>
		public bool SkipShowRecord { get; }

		/// <summary>
		/// HvoOfCurrentObjAtTimeOfNavigation is needed in Equals() for determining whether or not
		/// RecordNavigationInfo has changed in the property table.
		/// </summary>
		public int HvoOfCurrentObjAtTimeOfNavigation { get; }

		/// <summary>
		/// Gets or sets a value indicating whether to suppress focus changes.
		/// </summary>
		/// <value><c>true</c> if focus changes will be suppressed; otherwise, <c>false</c>.</value>
		public bool SuppressFocusChange { get; }

		/// <summary>
		/// Given an argument from OnRecordNavigation, expected to be a RecordNavigationInfo,
		/// if it really is return it's record list. Otherwise return null.
		/// </summary>
		public static IRecordList GetSendingList(object argument)
		{
			var info = argument as RecordNavigationInfo;
			return info?.MyRecordList;
		}

		#region IComparable Members

		/// <summary>
		/// RecordNavigation info can be considered equivalent if
		/// the CurrentObject hasn't changed.
		/// </summary>
		public override bool Equals(object obj)
		{
			return CompareTo(obj) == 0;
		}

		public override int GetHashCode()
		{
			return MyRecordList.VirtualFlid & HvoOfCurrentObjAtTimeOfNavigation;
		}

		public int CompareTo(object obj)
		{
			return ReflectionHelper.HaveSamePropertyValues(this, obj) ? 0 : -1;
		}

		#endregion
	}
}