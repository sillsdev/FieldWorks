// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Filters;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// This one is used for concordances. Currently a concordance never controls the record bar, and indicating this
	/// prevents a variety of activity that undesirably calls CurrentObject, which causes problems because in a concordance
	/// list the HVOs don't correspond to real FDO objects.
	/// </summary>
	/// <remarks>
	/// Only used by LanguageExplorer.Areas.Lexicon.Tools.Edit.FindExampleSentenceDlg.
	/// </remarks>
	internal sealed class ConcRecordList : TemporaryRecordList
	{
		/// <summary />
		internal ConcRecordList(string id, StatusBar statusBar, RecordSorter defaultSorter, string defaultSortLabel, RecordFilter defaultFilter, bool allowDeletions, bool shouldHandleDeletion)
			: base(id, statusBar, defaultSorter, defaultSortLabel, defaultFilter, allowDeletions, shouldHandleDeletion)
		{
		}

		#region Overrides of RecordList/TemporaryRecordList

		public override bool IsControllingTheRecordTreeBar
		{
			get
			{
				return false;
			}
			set
			{
				// Do not do anything here, unless you want to manage the "RecordClerk.RecordClerkRepository.ActiveRecordClerk" property.
			}
		}

		#endregion Overrides of RecordList/TemporaryRecordList
	}
}
