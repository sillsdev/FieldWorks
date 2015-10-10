// Copyright (c) 2005-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Filters;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary />
	internal sealed class RespellerTemporaryRecordClerk : TemporaryRecordClerk
	{
		/// <summary>
		/// Contructor.
		/// </summary>
		/// <param name="id">Clerk id/name.</param>
		/// <param name="recordList">Record list for the clerk.</param>
		/// <param name="defaultSorter">The default record sorter.</param>
		/// <param name="defaultSortLabel"></param>
		/// <param name="defaultFilter">The default filter to use.</param>
		/// <param name="allowDeletions"></param>
		/// <param name="shouldHandleDeletion"></param>
		internal RespellerTemporaryRecordClerk(string id, RecordList recordList, RecordSorter defaultSorter, string defaultSortLabel, RecordFilter defaultFilter, bool allowDeletions, bool shouldHandleDeletion)
			: base(id, recordList, defaultSorter, defaultSortLabel, defaultFilter, allowDeletions, shouldHandleDeletion)
		{
		}

		/// <summary />
		public override bool IsControllingTheRecordTreeBar
		{
			get
			{
				return false; // assume this will be false.
			}
			set
			{
				// do not do anything here, unless you want to manage the "ActiveClerk" property.
			}
		}
	}
}