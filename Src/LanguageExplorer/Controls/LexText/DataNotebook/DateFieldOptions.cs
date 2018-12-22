// Copyright (c) 2010-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Controls.LexText.DataNotebook
{
	/// <summary />
	public partial class DateFieldOptions : UserControl
	{
		private IHelpTopicProvider m_helpTopicProvider;
		// This example DateTime value must match that found in ImportDateFormatDlg.cs!
		private DateTime m_dtExample = new DateTime(1999, 3, 29, 15, 30, 45);
		private bool m_fGenDate;

		/// <summary />
		public DateFieldOptions()
		{
			InitializeComponent();
		}

		private void m_btnAddFormat_Click(object sender, EventArgs e)
		{
			using (var dlg = new ImportDateFormatDlg())
			{
				dlg.Initialize(string.Empty, m_helpTopicProvider, m_fGenDate);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					var sFmt = dlg.Format;
					var lvi = new ListViewItem(new[]
					{
						sFmt,
						m_dtExample.ToString(sFmt)
					});
					m_lvDateFormats.Items.Add(lvi);
				}
			}
		}

		private void m_btnModifyFormat_Click(object sender, EventArgs e)
		{
			if (m_lvDateFormats.SelectedItems.Count > 0)
			{
				var lvi = m_lvDateFormats.SelectedItems[0];
				using (var dlg = new ImportDateFormatDlg())
				{
					dlg.Initialize(lvi.SubItems[0].Text, m_helpTopicProvider, m_fGenDate);
					if (dlg.ShowDialog(this) == DialogResult.OK)
					{
						var sFmt = dlg.Format;
						lvi.SubItems[0].Text = sFmt;
						lvi.SubItems[1].Text = m_dtExample.ToString(sFmt);
					}
				}

			}
		}

		private void m_btnDeleteFormat_Click(object sender, EventArgs e)
		{
			if (m_lvDateFormats.SelectedIndices.Count > 0)
			{
				var idx = m_lvDateFormats.SelectedIndices[0];
				m_lvDateFormats.Items.RemoveAt(idx);
			}
		}


		internal void Initialize(LcmCache cache, IHelpTopicProvider helpTopicProvider, RnSfMarker rsfm, bool fGenDate)
		{
			m_helpTopicProvider = helpTopicProvider;
			m_fGenDate = fGenDate;
			m_lvDateFormats.Items.Clear();
			foreach (var sFmt in rsfm.m_dto.m_rgsFmt)
			{
				var lvi = new ListViewItem(new[] { sFmt, m_dtExample.ToString(sFmt) });
				m_lvDateFormats.Items.Add(lvi);
			}
		}

		public List<string> Formats
		{
			get
			{
				var rgsFmt = new List<string>();
				for (var i = 0; i < m_lvDateFormats.Items.Count; ++i)
				{
					var lvi = m_lvDateFormats.Items[i];
					rgsFmt.Add(lvi.SubItems[0].Text);
				}
				return rgsFmt;
			}
		}
	}
}