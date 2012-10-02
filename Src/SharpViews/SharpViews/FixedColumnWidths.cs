using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.SharpViews
{
	public class FixedColumnWidths : IControlColumnWidths
	{
		private int[] m_ColumnWidths;

		public FixedColumnWidths(params int[] columnWidths)
		{
			m_ColumnWidths = columnWidths;
		}

		public int[] ColumnWidths(int ncols, LayoutInfo layoutInfo)
		{
			return m_ColumnWidths;
		}
	}
}
