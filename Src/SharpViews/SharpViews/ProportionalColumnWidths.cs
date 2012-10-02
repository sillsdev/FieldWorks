using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.SharpViews
{
	public class ProportionalColumnWidths : IControlColumnWidths
	{
		private int[] m_ColumnWidths;

		private int Total(int[] percentages)
		{
			int total = 0;
			foreach (int percent in percentages)
			{
				total += percent;
			}
			return total;
		}

		public ProportionalColumnWidths(params int[] percentages)
		{
			int total = Total(percentages);

			for (int i = 0; i < percentages.Length; i++)
			{
				percentages[i] = (int)(percentages[i] * 100.0 / total);
			}

			m_ColumnWidths = percentages;
		}

		public int[] ColumnWidths(int ncols, LayoutInfo layoutInfo)
		{
			int[] columnWidths = new int[ncols];
			for (int i = 0; i < ncols; i++)
			{
				columnWidths[i] = (int)(m_ColumnWidths[i] * (layoutInfo.MaxWidth) / 100.0);
			}
			return columnWidths;
		}
	}
}
