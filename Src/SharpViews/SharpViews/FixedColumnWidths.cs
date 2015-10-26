// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
