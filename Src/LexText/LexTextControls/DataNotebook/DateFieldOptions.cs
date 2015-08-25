// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DateFieldOptions.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class DateFieldOptions : UserControl
	{
		FdoCache m_cache;
		IHelpTopicProvider m_helpTopicProvider;
		// This example DateTime value must match that found in ImportDateFormatDlg.cs!
		DateTime m_dtExample = new DateTime(1999, 3, 29, 15, 30, 45);
		bool m_fGenDate = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DateFieldOptions"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DateFieldOptions()
		{
			InitializeComponent();
		}

		private void m_btnAddFormat_Click(object sender, EventArgs e)
		{
			using (ImportDateFormatDlg dlg = new ImportDateFormatDlg())
			{
				dlg.Initialize(String.Empty, m_helpTopicProvider, m_fGenDate);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					string sFmt = dlg.Format;
					string sDate = m_dtExample.ToString(sFmt);
					ListViewItem lvi = new ListViewItem(new string[] { sFmt, sDate });
					m_lvDateFormats.Items.Add(lvi);
				}
			}
		}

		private void m_btnModifyFormat_Click(object sender, EventArgs e)
		{
			if (m_lvDateFormats.SelectedItems.Count > 0)
			{
				ListViewItem lvi = m_lvDateFormats.SelectedItems[0];
				using (ImportDateFormatDlg dlg = new ImportDateFormatDlg())
				{
					dlg.Initialize(lvi.SubItems[0].Text, m_helpTopicProvider, m_fGenDate);
					if (dlg.ShowDialog(this) == DialogResult.OK)
					{
						string sFmt = dlg.Format;
						string sDate = m_dtExample.ToString(sFmt);
						lvi.SubItems[0].Text = sFmt;
						lvi.SubItems[1].Text = sDate;
					}
				}

			}
		}

		private void m_btnDeleteFormat_Click(object sender, EventArgs e)
		{
			if (m_lvDateFormats.SelectedIndices.Count > 0)
			{
				int idx = m_lvDateFormats.SelectedIndices[0];
				m_lvDateFormats.Items.RemoveAt(idx);
			}
		}


		internal void Initialize(FdoCache cache, IHelpTopicProvider helpTopicProvider,
			DataNotebook.NotebookImportWiz.RnSfMarker rsfm, bool fGenDate)
		{
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_fGenDate = fGenDate;

			m_lvDateFormats.Items.Clear();
			for (int i = 0; i < rsfm.m_dto.m_rgsFmt.Count; ++i)
			{
				string sFmt = rsfm.m_dto.m_rgsFmt[i];
				string sDate = m_dtExample.ToString(sFmt);
				ListViewItem lvi = new ListViewItem(new string[] { sFmt, sDate });
				m_lvDateFormats.Items.Add(lvi);
			}
		}

		public List<string> Formats
		{
			get
			{
				List<string> rgsFmt = new List<string>();
				for (int i = 0; i < m_lvDateFormats.Items.Count; ++i)
				{
					ListViewItem lvi = m_lvDateFormats.Items[i];
					string sFmt = lvi.SubItems[0].Text;
					rgsFmt.Add(sFmt);
				}
				return rgsFmt;
			}
		}
	}
}
