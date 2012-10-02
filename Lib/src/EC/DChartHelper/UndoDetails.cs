using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace DChartHelper
{
	public class UndoDetails
	{
		List<string> m_astrOldValues = new List<string>();
		List<string> m_astrNewValues = new List<string>();
		List<DataGridViewCell> m_aCells = new List<DataGridViewCell>();

		public void GoForward(DataGridViewCell aCell, string strNewValue, Font fontSource, Font fontTarget)
		{
			if (strNewValue == null)
				return;
			m_aCells.Add(aCell);
			m_astrNewValues.Add(strNewValue);

			string strOldValue = (string)aCell.Value;
			m_astrOldValues.Add(strOldValue);

			// first check for ambiguities
			int nIndex;
			if( (nIndex = strNewValue.IndexOf('%')) != -1 )
			{
				PickAmbiguity aPicker = new PickAmbiguity(strNewValue, fontSource, fontTarget);
				if (aPicker.ShowDialog() == DialogResult.OK)
					strNewValue = aPicker.SelectedWord;
			}

			if (strOldValue != null)
				strNewValue = strOldValue + ' ' + strNewValue;
			aCell.Value = strNewValue;
		}

		public void Undo(out string strNewValue)
		{
			int nLastItem = m_aCells.Count - 1;
			if (nLastItem >= 0)
			{
				DataGridViewCell aCell = m_aCells[nLastItem];
				string strValue = m_astrOldValues[nLastItem];
				aCell.Value = strValue;
				// aCell.ToolTipText = DiscourseChartForm.GetTooltip(strValue);
				strNewValue = m_astrNewValues[nLastItem];
				m_aCells.RemoveAt(nLastItem);
				m_astrOldValues.RemoveAt(nLastItem);
				m_astrNewValues.RemoveAt(nLastItem);
			}
			else
				strNewValue = null;
		}

		public void Clear()
		{
			m_astrOldValues.Clear();
			m_astrNewValues.Clear();
			m_aCells.Clear();
		}
	}
}
