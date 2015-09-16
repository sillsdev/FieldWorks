// Copyright (c) 2003-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The argument used when we broadcast OnRecordNavigation.
	/// </summary>
	public class RecordNavigationInfo : IComparable
	{
		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="clerk">The clerk.</param>
		/// <param name="suppressSaveOnChangeRecord"></param>
		/// <param name="skipShowRecord"></param>
		/// <param name="suppressFocusChange"></param>
		public RecordNavigationInfo(RecordClerk clerk, bool suppressSaveOnChangeRecord, bool skipShowRecord, bool suppressFocusChange)
		{
			Clerk = clerk;
			HvoOfCurrentObjAtTimeOfNavigation = Clerk != null && Clerk.CurrentObjectHvo != 0 ? Clerk.CurrentObjectHvo : 0;
			SuppressSaveOnChangeRecord = suppressSaveOnChangeRecord;
			SkipShowRecord = skipShowRecord;
			SuppressFocusChange = suppressFocusChange;
		}

		/// <summary>
		///  The clerk that broadcast the change.
		/// </summary>
		public RecordClerk Clerk
		{
			get; private set;
		}

		/// <summary>
		/// Whether a change of record should result in a save (and discard of undo items).
		/// This is suppressed if the change is caused by creating or deleting a record.
		/// </summary>
		public bool SuppressSaveOnChangeRecord
		{
			get; private set;
		}

		/// <summary>
		/// Indicates whether the this action should skip ShowRecord
		/// (e.g. to avoid losing the context/pane where the user may be editing.)
		/// </summary>
		public bool SkipShowRecord
		{
			get; private set;
		}

		/// <summary>
		/// HvoOfClerkAtTimeOfNavigation is needed in Equals() for determining whether or not
		/// RecordNavigationInfo has changed in the property table.
		/// </summary>
		public int HvoOfCurrentObjAtTimeOfNavigation
		{
			get; private set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether to suppress focus changes.
		/// </summary>
		/// <value><c>true</c> if focus changes will be suppressed; otherwise, <c>false</c>.</value>
		public bool SuppressFocusChange
		{
			get; private set;
		}

		/// <summary>
		/// Given an argument from OnRecordNavigation, expected to be a RecordNavigationInfo,
		/// if it really is return it's clerk. Otherwise return null.
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public static RecordClerk GetSendingClerk(object argument)
		{
			var info = argument as RecordNavigationInfo;
			if (info == null)
				return null;
			return info.Clerk;
		}

		#region IComparable Members

		/// <summary>
		/// RecordNavigation info can be considered equivalent if
		/// the CurrentObject hasn't changed.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			return CompareTo(obj) == 0;
		}

		public override int GetHashCode()
		{
			return Clerk.VirtualFlid & HvoOfCurrentObjAtTimeOfNavigation;
		}

		public int CompareTo(object obj)
		{
			return ReflectionHelper.HaveSamePropertyValues(this, obj) ? 0 : -1;
		}

		#endregion
	}
}