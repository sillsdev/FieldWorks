// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// this dialog, dumbs down InsertEntryDlg, to use its states and logic for
	/// creating a new entry immediately without trying to do matching Entries.
	/// </summary>
	internal class InsertEntryNow : InsertEntryDlg
	{
		internal static InsertEntryDlg CreateInsertEntryDlg(bool fCreateEntryNow)
		{
			return fCreateEntryNow ? new InsertEntryNow() : new InsertEntryDlg();
		}

		public InsertEntryNow()
		{
			m_matchingEntriesGroupBox.Visible = false;
		}

		/// <summary>
		/// skip updating matches, since this dialog is just for inserting a new entry.
		/// </summary>
		protected override void UpdateMatches()
		{
			// skip matchingEntries.ResetSearch
		}
	}
}