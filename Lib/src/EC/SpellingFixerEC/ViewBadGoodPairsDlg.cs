#define TurnOffSF30

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SpellingFixerEC
{
	internal partial class ViewBadGoodPairsDlg : Form
	{
#if !TurnOffSF30
		internal Bad2GoodMap m_mapBad2Good = null;
		internal KnownGoodWordList m_mapWhiteList = null;
		internal bool m_bEditingWhiteList = false;

		internal CscProject m_project;
#endif

		const int cnBadSpelling = 0;
		const int cnGoodSpelling = 1;

		internal ViewBadGoodPairsDlg(DataTable myTable, Font font)
		{
			InitializeComponent();

			// for this ctor, the columns from the datatable
			this.dataGridView.Columns.Clear();

			this.dataGridView.RowsDefaultCellStyle.Font = font;
			dataGridView.RowTemplate.Height = font.Height + 6;   // 6 for padding

			this.dataGridView.DataSource = myTable;
			dataGridView.AllowUserToAddRows = false;

			dataGridView.Columns[SpellingFixerEC.strColumnCmt].Visible = false;
			helpProvider.SetHelpString(dataGridView, Properties.Resources.ViewBadGoodPairsBad2GoodHelp);
		}

#if !TurnOffSF30
		internal ViewBadGoodPairsDlg(CscProject project, ref KnownGoodWordList mapWhiteList,
			ref Bad2GoodMap mapBad2Good, Font font, bool bEditingWhiteList)
		{
			InitializeComponent();

			m_project = project;
			m_mapWhiteList = mapWhiteList;
			m_mapBad2Good = mapBad2Good;
			m_bEditingWhiteList = bEditingWhiteList;

			this.dataGridView.RowsDefaultCellStyle.Font = font;
			dataGridView.RowTemplate.Height = font.Height + 6;   // 6 for padding

			if (bEditingWhiteList)
			{
				this.Text = "Edit Dictionary";
				this.helpProvider.SetHelpString(dataGridView, Properties.Resources.ViewBadGoodPairsDictionaryHelp);
				dataGridView.Columns[cnGoodSpelling].HeaderText = "Known Good Words";
				dataGridView.Columns[cnBadSpelling].Visible = false;

				object[] ao = new object[2];
				ao[cnBadSpelling] = null;
				foreach (string strKey in mapWhiteList.Keys)
				{
					ao[cnGoodSpelling] = strKey;
					dataGridView.Rows.Add(ao);
				}
			}
			else
			{
				helpProvider.SetHelpString(dataGridView, Properties.Resources.ViewBadGoodPairsBad2GoodHelp);

				object[] ao = new object[2];
				foreach (KeyValuePair<string,string> kvp in mapBad2Good)
				{
					ao[cnBadSpelling] = kvp.Key;
					ao[cnGoodSpelling] = kvp.Value;
					dataGridView.Rows.Add(ao);
				}
			}
		}

		protected bool EditingDictionary
		{
			get { return m_bEditingWhiteList; }
		}

		protected bool EditingCscBad2GoodList
		{
			get { return (m_mapBad2Good != null) && !EditingDictionary; }
		}
#endif

		private void buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			this.Close();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void dataGridView_UserAddedRow(object sender, DataGridViewRowEventArgs e)
		{
			buttonOK.Enabled = true;
		}

		private void dataGridView_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
		{
			DeletingRow(e.Row);
		}

		protected bool DeletingRow(DataGridViewRow theRow)
		{
			buttonOK.Enabled = true;
#if !TurnOffSF30
			if (EditingCscBad2GoodList)
			{
				string strBadValue = (string)theRow.Cells[cnBadSpelling].Value;
				if (!String.IsNullOrEmpty(strBadValue) && m_mapBad2Good.ContainsKey(strBadValue))
					m_mapBad2Good.Remove(strBadValue);

				// note that we aren't removing the potentially last instance of this good value from the
				//  known good list... not sure if that's correct
			}
			else if (EditingDictionary)
			{
				string strGoodValue = (string)theRow.Cells[cnGoodSpelling].Value;
				if (!String.IsNullOrEmpty(strGoodValue))
				{
					if (RetaskBad2GoodList(strGoodValue, null) != DialogResult.No)
					{
						if (m_mapWhiteList.ContainsKey(strGoodValue))
							m_mapWhiteList.Remove(strGoodValue);
					}
					else
						return false;
				}
			}
#endif

			return true;
		}

		// we want to keep track of the changes, because it probably means changes to the Bad 2 Good
		//  table as well
		protected string m_strBadForm = null;
		protected string m_strGoodForm = null;
		protected string m_strWordBeingEdited = null;
		private void dataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
		{
			DataGridViewRow theRow = dataGridView.Rows[e.RowIndex];
			if (theRow.IsNewRow)
			{
				theRow.Selected = false;    // unselect the current row...
				theRow = dataGridView.Rows[dataGridView.Rows.Add(new object[] { null, null })];
			}

			m_strBadForm = (string)theRow.Cells[cnBadSpelling].Value;
			m_strGoodForm = (string)theRow.Cells[cnGoodSpelling].Value;
			m_strWordBeingEdited = (e.ColumnIndex == cnBadSpelling) ? m_strBadForm : m_strGoodForm;
		}

		private void dataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
		{
			DataGridViewRow theRow = dataGridView.Rows[e.RowIndex];
			string strWordEdited = (string)theRow.Cells[e.ColumnIndex].Value;
			if ((strWordEdited == m_strWordBeingEdited) || String.IsNullOrEmpty(strWordEdited))
				return;

			buttonOK.Enabled = true;

#if !TurnOffSF30
			// the word was changed... if we're editing the dictionary...
			if (EditingDictionary)
			{
				// if this is a new record...
				if (String.IsNullOrEmpty(m_strWordBeingEdited))
					if (!AddToWhiteList(strWordEdited))  // (i.e. so add)
					{
						// means we didn't add it (because it was already there)
						System.Diagnostics.Debug.Assert(m_mapWhiteList.ContainsKey(strWordEdited));

						// in that case, don't add this new value, but just select and display the existing value.
						theRow.Cells[cnGoodSpelling].Value = null;
					}

				// ... then we also have to update any entries in the bad2good list that had
				//  the original good word in them.
				DialogResult res = RetaskBad2GoodList(m_strWordBeingEdited, strWordEdited);

				// either if they said 'Yes' or there weren't others...
				if (res != DialogResult.Cancel)
				{
					// update the current value in the known good list with the new value
					RetaskWhiteListSfw(m_strWordBeingEdited, strWordEdited);
				}

				// if it happened to have been 'No', then we have to have the old value as well
				if (res == DialogResult.No)
				{
					if (AddToWhiteList(m_strWordBeingEdited))  // (i.e. so add)
						dataGridView.Rows.Add(new object[] { null, m_strWordBeingEdited });
				}

				// the update to the WordsToCheck will happen by the caller after we're totally done
			}
			else if (EditingCscBad2GoodList)
			{
				DataGridViewCell theBadCell = theRow.Cells[cnBadSpelling];
				if (e.ColumnIndex == cnGoodSpelling)
				{
					string strBadValue = (string)theBadCell.Value;
					if (String.IsNullOrEmpty(strBadValue))
						theRow.Cells[cnGoodSpelling].Value = strWordEdited;
					else
					{
						// if this is newly edited (i.e. word being edited is null), then pretend the bad value is
						//  new too (or it looks like it's already there)
						GoodValueEdited(theRow, strBadValue, strBadValue, m_strWordBeingEdited, strWordEdited);
					}
				}
				else    // edited the bad spelling cell
				{
					System.Diagnostics.Debug.Assert(e.ColumnIndex == cnBadSpelling);
					if (String.IsNullOrEmpty(m_strWordBeingEdited) && (theRow.Cells[cnGoodSpelling].Value == null))
						// means that this is the new row, so with only a bad value, we have nothing
						theRow.Cells[cnBadSpelling].Value = strWordEdited;
					else
						BadValueOnlyEdited(theBadCell, m_strWordBeingEdited, strWordEdited, (string)theRow.Cells[cnGoodSpelling].Value);
				}
			}
#endif
		}

		private void UpdateGridGoodValues(string strOldValue, string strNewValue)
		{
			foreach (DataGridViewRow aRow in dataGridView.Rows)
			{
				DataGridViewCell theGoodCell = aRow.Cells[cnGoodSpelling];
				if (strOldValue == (string)theGoodCell.Value)
					theGoodCell.Value = strNewValue;
			}
		}

#if !TurnOffSF30
		private DialogResult RetaskBad2GoodList(string strOldValue, string strNewValue)
		{
			// ... then we also have to update any entries in the bad2good list that had
			//  the original good word in them.
			DialogResult res = DialogResult.None;
			if (m_mapBad2Good.ContainsValue(strOldValue))
			{
				bool bDeleting = String.IsNullOrEmpty(strNewValue);
				string strMsg = String.Format("The word you just {1}, '{2}', is in other records in the bad-to-good list.{0}Would you like to have those record(s) {1} as well?",
					Environment.NewLine, (bDeleting) ? "deleted" : "changed", strOldValue);

				if (!bDeleting)
					strMsg += String.Format(" (i.e. to '{0}')", strNewValue);

				res = MessageBox.Show(strMsg, CscProject.cstrCaption, MessageBoxButtons.YesNoCancel);

				if (res == DialogResult.Yes)
				{
					while (m_mapBad2Good.ContainsValue(strOldValue))
					{
						foreach (KeyValuePair<string, string> kvp in m_mapBad2Good)
						{
							if (strOldValue == kvp.Value)
							{
								m_mapBad2Good.Remove(kvp.Key);
								if (!bDeleting)
									m_mapBad2Good.Add(kvp.Key, strNewValue);
								break;  // can't modify the collection without restarting the enumerator
							}
						}
					}
				}
			}

			return res;
		}

		private void RetaskWhiteListSfw(string strKey, string strNewValue)
		{
			SpellFixerWord sfw = m_mapWhiteList[strKey];
			m_mapWhiteList.Remove(strKey);
			if (!m_mapWhiteList.ContainsKey(strNewValue))
			{
				sfw.Value = strNewValue;
				sfw.InitializeNonStaticData(m_project);
				m_mapWhiteList.Add(sfw);
			}
		}

		private bool AddToWhiteList(string strValue)
		{
			if (!m_mapWhiteList.ContainsKey(strValue))
			{
				SpellFixerWord sfw = m_project.GetNewSpellFixerWord(strValue, null);
				m_mapWhiteList.Add(sfw);
				return true;
			}
			return false;
		}

		protected void GoodValueEdited(DataGridViewRow theRow, string strBadValue, string strNewBadValue,
			string strGoodValue, string strNewGoodValue)
		{
			// if the new record is already in there, then just select and display the existing value
			if (strBadValue != strNewBadValue)
				foreach (DataGridViewRow aRow in dataGridView.Rows)
					if (strNewBadValue == (string)aRow.Cells[cnBadSpelling].Value)
					{
						aRow.Selected = true;
						dataGridView.FirstDisplayedScrollingRowIndex = aRow.Index;
						System.Diagnostics.Debug.Assert(m_mapBad2Good.ContainsKey(strNewBadValue) && m_mapBad2Good[strNewBadValue] == strNewGoodValue);
						return;
					}

			// remove the one we're editing (so we don't find it below causing us to query the user
			//  about *that* one)
			if (!String.IsNullOrEmpty(strBadValue))
				m_mapBad2Good.Remove(strBadValue);

			// then see if any other records have the same 'good form' and ask the user if they'd like us
			//  to change those also
			DialogResult res = RetaskBad2GoodList(strGoodValue, strNewGoodValue);
			if (res == DialogResult.Yes)
				UpdateGridGoodValues(strGoodValue, strNewGoodValue);

			// now update the row being edited with the new data
			theRow.Cells[cnGoodSpelling].Value = strNewGoodValue;
			if (strNewBadValue != strBadValue)
				theRow.Cells[cnBadSpelling].Value = strNewBadValue;

			// now add the new data to the collection
			m_mapBad2Good.Add(strNewBadValue, strNewGoodValue);

			// also update the known good list with the new good form (if we're changing them all)
			System.Diagnostics.Debug.Assert(String.IsNullOrEmpty(strGoodValue) || m_mapWhiteList.ContainsKey(strGoodValue));
			if (!String.IsNullOrEmpty(strGoodValue)
				&& m_mapWhiteList.ContainsKey(strGoodValue)
				&& (res != DialogResult.No))   // this means the original good form is still there for other records
			{
				RetaskWhiteListSfw(strGoodValue, strNewGoodValue);
			}
			else if (!m_mapWhiteList.ContainsKey(strNewGoodValue))
				AddToWhiteList(strNewGoodValue);

			buttonOK.Enabled = true;
			// the update to the WordsToCheck will happen by the caller after we're totally done
		}

		protected void BadValueOnlyEdited(DataGridViewCell theBadCell, string strBadValue, string strNewBadValue,
			string strNewGoodValue)
		{
			// update the row value
			theBadCell.Value = strNewBadValue;

			// and update the collection
			if (!String.IsNullOrEmpty(strBadValue))
				m_mapBad2Good.Remove(strBadValue);

			m_mapBad2Good.Add(strNewBadValue, strNewGoodValue);

			// since we know this can't be a good word (or it wouldn't be the 'bad' form), remove it from the white list if present
			if (m_mapWhiteList.ContainsKey(strNewBadValue))
				m_mapWhiteList.Remove(strNewBadValue);

			buttonOK.Enabled = true;
		}

#else   // not in SF30
#endif

		private void dataGridView_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
		{
			if ((e.RowIndex < 0) || (e.RowIndex >= dataGridView.Rows.Count)
				|| (e.ColumnIndex < 0) || (e.ColumnIndex >= dataGridView.Columns.Count)
#if !TurnOffSF30
				|| (EditingDictionary)
#endif
				|| (e.Button != MouseButtons.Right))
				return;

			try
			{
				ProcessCellClick(e);
			}
			catch (Exception ex)
			{
				MessageBox.Show(String.Format("Unable to update record! Reason: {0}", ex.Message));
			}
		}

		protected void ProcessCellClick(DataGridViewCellMouseEventArgs e)
		{
			DataGridViewRow theRow = dataGridView.Rows[e.RowIndex];
			ProcessEditRow(theRow);
		}

		protected void ProcessEditRow(DataGridViewRow theRow)
		{
			DataGridViewCell theBadCell = theRow.Cells[cnBadSpelling];
			DataGridViewCell theGoodCell = theRow.Cells[cnGoodSpelling];
			string strBadValue = (string)theBadCell.Value;
			string strGoodValue = (string)theGoodCell.Value;

			QueryGoodSpelling aQuery = new QueryGoodSpelling(dataGridView.RowsDefaultCellStyle.Font);
			DialogResult res = aQuery.ShowDialog(strBadValue, strGoodValue, strBadValue, (strBadValue != null));
			if (res == DialogResult.Abort)
			{
				// this means delete
				dataGridView.Rows.Remove(theRow);

#if !TurnOffSF30
				if (!String.IsNullOrEmpty(strBadValue) && EditingCscBad2GoodList)
					m_mapBad2Good.Remove(strBadValue);
#endif
				buttonOK.Enabled = true;
			}
			else if (res == DialogResult.OK)
			{
				// try to organize the permutations:
				//    o both columns were initially null (i.e. IsNewRow = true) and now have values
				//      ->  both must now be non-null
				//      ->  new 'bad' must not already exist
				//    o both columns were non-null and now the 'good' value is changed
				//      ->  if the old 'good' value is still present elsewhere, then query to change those also
				//      ->  change the good value in the row (and bad if necessary)
				//      ->  change the good value in the white list as well
				//    o both columns were non-null and now the 'bad' value (only) is changed
				//      ->  change the bad value in the row
				//      ->  change the bad value in the bad2good list
				if (String.IsNullOrEmpty(aQuery.BadSpelling) || String.IsNullOrEmpty(aQuery.GoodSpelling))
				{
					// if either of them are null...
					throw new ApplicationException("The 'Bad' and 'Good' forms are not allowed to be nothing!");
				}
#if !TurnOffSF30
				/* I don't recall what I was thinking about this one. Sometimes having the good and bad spelling
				 * be the same is the only way to force a replacement to not happen (e.g. if a shorter string
				 * replacement rule would otherwise override a longer string and you don't want that)
				 * Oh: maybe that applied with CSC since that always only worked with whole word forms... move
				 * it into the conditional compile area...
				else if (aQuery.BadSpelling == aQuery.GoodSpelling)
				{
					// if they're the same...
					throw new ApplicationException("The new 'Bad' and 'Good' forms must be different from each other!");
				}
				*/
				else if (!EditingCscBad2GoodList)
#endif
				{
					// Legacy SpellFixer
					theBadCell.Value = aQuery.BadSpelling;
					theGoodCell.Value = aQuery.GoodSpelling;
				}
#if !TurnOffSF30
				else if (String.IsNullOrEmpty(strBadValue) && m_mapBad2Good.ContainsKey(aQuery.BadSpelling))
				{
					// if we're adding a new one (i.e. strBadValue == null) and it's already in the list...
					// ... we don't want to have added the dummy record (so delete it)
					dataGridView.Rows.Remove(theRow);

					// scroll the existing match into view.
					foreach (DataGridViewRow aRow in dataGridView.Rows)
						if (aQuery.BadSpelling == (string)aRow.Cells[cnBadSpelling].Value)
						{
							aRow.Selected = true;
							dataGridView.FirstDisplayedScrollingRowIndex = aRow.Index;
							break;
						}

					// if the value was, in fact, different, then throw an exception
					if (m_mapBad2Good[aQuery.BadSpelling] != aQuery.GoodSpelling)
					{
						// then throw an error unless it has the same result
						throw new ApplicationException(String.Format("This new 'Bad Form' (i.e. '{0}') is already associated with another Good Form, '{1}'! If you really want to do this, you should edit the other record.",
							aQuery.BadSpelling, m_mapBad2Good[aQuery.BadSpelling]));
					}
				}

				// now deal with the case where the good spelling was changed, which potentially involves updating
				//  the list of known good words. This could include changes to the bad form as well.
				else if (aQuery.GoodSpelling != strGoodValue)
				{
					GoodValueEdited(theRow, strBadValue, aQuery.BadSpelling, strGoodValue, aQuery.GoodSpelling);
				}

				// check in case they changed the bad spelling only...
				else if (aQuery.BadSpelling != strBadValue)
				{
					System.Diagnostics.Debug.Assert(aQuery.GoodSpelling == strGoodValue);
					BadValueOnlyEdited(theBadCell, strBadValue, aQuery.BadSpelling, aQuery.GoodSpelling);
				}
#endif
				buttonOK.Enabled = true;
			}
		}

		private void dataGridView_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			System.Diagnostics.Trace.WriteLine(String.Format("PreviewKeyDown: sender: {3}, KeyValue: {0}, KeyCode: {1}, KeyData: {2}",
				e.KeyValue, e.KeyCode, e.KeyData, sender.ToString()));

#if !TurnOffSF30
			if (EditingDictionary)
#endif
			{
				if (e.KeyCode == Keys.Delete)
					foreach (DataGridViewCell aCell in dataGridView.SelectedCells)
					{
						if ((aCell.RowIndex >= 0) & (aCell.RowIndex < dataGridView.Rows.Count))
						{
							DataGridViewRow theRow = dataGridView.Rows[aCell.RowIndex];
							if (DeletingRow(theRow))
								dataGridView.Rows.Remove(theRow);
						}
					}
			}
		}

		private void buttonAddCorrection_Click(object sender, EventArgs e)
		{
			DataTable myTable = (DataTable)dataGridView.DataSource;
			myTable.Rows.Add(new object[] { "incorect", "incorrect" });
			DataGridViewRow theRow = dataGridView.Rows[dataGridView.Rows.Count - 1];
			while(true)
			{
				try
				{
					ProcessEditRow(theRow);
					return; // if it doesn't throw we're done
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, SpellingFixerEC.cstrCaption);
				}
			}
		}
	}
}