using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using ECInterfaces;

namespace AdaptIt2Unicode
{
	public partial class FilteredFieldsForm : Form
	{
		protected const int cnColumnConvertCheckbox = 0;
		protected const int cnColumnSFMs = 1;
		protected const int cnColumnExampleData = 2;
		protected const int cnColumnExampleResult = 3;

		protected List<string> m_astrSfmsToConvert = new List<string>();
		protected IEncConverter m_aEC = null;
		protected Dictionary<string, List<string>> m_mapFilteredSfms = null;

		public FilteredFieldsForm(Dictionary<string, List<string>> mapFilteredSfms, List<string> astrSfmsToConvert,
			List<string> astrSfmsToNotConvert, Font font, Font fontConverted, IEncConverter aEC)
		{
			InitializeComponent();

			m_aEC = aEC;
			m_mapFilteredSfms = mapFilteredSfms;

			dataGridViewFilterSfms.Columns[cnColumnExampleData].DefaultCellStyle.Font = font;
			dataGridViewFilterSfms.Columns[cnColumnExampleResult].DefaultCellStyle.Font = fontConverted;

			foreach (KeyValuePair<string, List<string>> kvp in mapFilteredSfms)
			{
				if (!astrSfmsToConvert.Contains(kvp.Key) && !astrSfmsToNotConvert.Contains(kvp.Key))
				{
					System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(kvp.Value[0]));
					string strInput = kvp.Value[0];
					string strOutput = m_aEC.Convert(strInput);
					bool bOnByDefault = Properties.Settings.Default.DefaultFieldsToConvert.Contains(kvp.Key);
					object[] aoRowData = new object[4] { bOnByDefault, kvp.Key, kvp.Value[0], strOutput };
					int nIndex = dataGridViewFilterSfms.Rows.Add(aoRowData);
					DataGridViewRow theRow = dataGridViewFilterSfms.Rows[nIndex];
					theRow.Tag = 0;
				}
			}
		}

		public new DialogResult ShowDialog()
		{
			if (dataGridViewFilterSfms.Rows.Count == 0)
				return DialogResult.None;
			else
			{
				helpProvider.SetHelpString(dataGridViewFilterSfms, Properties.Resources.FilteredFieldConversionHelp);
				return base.ShowDialog();
			}
		}

		public void DivyUpSfmsToConvert(List<string> astrSfmsToConvert, List<string> astrSfmsToNotConvert)
		{
			foreach (string strSfm in m_mapFilteredSfms.Keys)
			{
				if (m_astrSfmsToConvert.Contains(strSfm))
				{
					if (!astrSfmsToConvert.Contains(strSfm))
						astrSfmsToConvert.Add(strSfm);
				}
				else
				{
					if (!astrSfmsToNotConvert.Contains(strSfm))
						astrSfmsToNotConvert.Add(strSfm);
				}
			}
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			foreach (DataGridViewRow aRow in dataGridViewFilterSfms.Rows)
			{
				DataGridViewCheckBoxCell aCheckboxCell = (DataGridViewCheckBoxCell)aRow.Cells[cnColumnConvertCheckbox];
				if ((bool)aCheckboxCell.Value)
				{
					m_astrSfmsToConvert.Add((string)aRow.Cells[cnColumnSFMs].Value);
				}
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void dataGridViewFilterSfms_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (    (e.RowIndex < 0)
				||  (e.RowIndex >= dataGridViewFilterSfms.Rows.Count)
				||  (e.ColumnIndex < 0)
				||  (e.ColumnIndex >= dataGridViewFilterSfms.Columns.Count))
				return;

			string strInput, strOutput;
			DataGridViewRow theRow = dataGridViewFilterSfms.Rows[e.RowIndex];
			DataGridViewCell theCell = theRow.Cells[e.ColumnIndex];
			switch (e.ColumnIndex)
			{
				case cnColumnConvertCheckbox:
					if (false == (bool)theCell.Value)   // check for false, which means it's becoming true
					{
						strInput = (string)theRow.Cells[cnColumnExampleData].Value;
						strOutput = m_aEC.Convert(strInput);
						theRow.Cells[cnColumnExampleResult].Value = strOutput;
					}
					break;

				case cnColumnExampleData:
					int nIndex = (int)theRow.Tag;
					string strMarker = (string)theRow.Cells[cnColumnSFMs].Value;
					List<string> astrSampleData = m_mapFilteredSfms[strMarker];
					if (++nIndex >= astrSampleData.Count)
						nIndex = 0;
					strInput = astrSampleData[nIndex];
					strOutput = m_aEC.Convert(strInput);
					theCell.Value = strInput;
					theRow.Cells[cnColumnExampleResult].Value = strOutput;
					theRow.Tag = nIndex;
					break;
			}
		}
	}
}