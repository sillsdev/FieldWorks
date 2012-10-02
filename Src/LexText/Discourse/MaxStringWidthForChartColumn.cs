// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MaxStringWidthForChartColumn.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Discourse
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class MaxStringWidthForChartColumn : MaxStringWidthForColumnEnv
	{
		private int m_cLines; // number of interlinear lines displayed
		private ConstChartVc m_vc;
		/// <summary>Collects max. widths of each line choice within an InnerPile until we know which is longest.</summary>
		private int[] m_paraWidths;

		public MaxStringWidthForChartColumn(ConstChartVc vc, IVwStylesheet stylesheet, ISilDataAccess sda, int hvoRoot,
			System.Drawing.Graphics graphics, int icolumn)
			: base(stylesheet, sda, hvoRoot, graphics, icolumn)
		{
			m_vc = vc;
			m_cLines = m_vc.LineChoices.Count;
			m_paraWidths = new int[m_cLines];
		}

		/// <summary>
		/// Updates the column width counter for auto-resizing Views columns.
		/// </summary>
		public void UpdateMaxBundleWidth()
		{
			int maxWidth = 0;
			// Find widest line width in this bundle
			for (int i=0 ; i < m_cLines; i++)
			{
				int curWidth = m_paraWidths[i];
				if (curWidth > maxWidth)
					maxWidth = curWidth;
				m_paraWidths[i] = 0;
			}
			// Update member variable with widest line's width in current bundle
			m_width += maxWidth;

		}

		/// <summary>
		/// Accumulate this string's width with what is already stored for the current
		/// interlinear line.
		/// </summary>
		/// <param name="s"></param>
		protected override void AddStringWidth(string s)
		{
			if (m_icolCurr != m_icolToWatch || m_nColSpanCurr != 1)
				return;
			int pixWidth = Convert.ToInt32(GraphicsObj.MeasureString(s, m_font).Width);

			m_paraWidths[m_vc.CurrentLine] += pixWidth;
		}

		public override void CloseInnerPile()
		{
			base.CloseInnerPile();
			// update max string width info
			UpdateMaxBundleWidth();
		}

		public override void CloseParagraph()
		{
			if (m_paraWidths[0] > 0)
				UpdateMaxBundleWidth(); // Take care of non-bundle strings.
			base.CloseParagraph();
		}
	}
}
