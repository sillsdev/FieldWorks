// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Estimates the longest width (in pixels) of string contents in a specified column in a table.
	/// Currently only calculates for cells spanning one column in a views table.
	/// </summary>
	internal class MaxStringWidthForColumnEnv : StringMeasureEnv
	{
		IDictionary<int, Font> m_wsToFont = new Dictionary<int, Font>();
		int m_fontWs = 0;
		IVwStylesheet m_styleSheet = null;
		ILgWritingSystemFactory m_wsf = null;

		/// <summary>Index of the column being examined.</summary>
		protected int m_icolToWatch = -1;
		/// <summary>Index of the current column.</summary>
		protected int m_icolCurr = -1;
		/// <summary>Index of the current row.</summary>
		int m_irowCurr = -1;
		/// <summary>Number of columns spanned by the current cell.</summary>
		protected int m_nColSpanCurr = 0;

		/// <summary />
		public MaxStringWidthForColumnEnv(IVwStylesheet stylesheet, ISilDataAccess sda, int hvoRoot, Graphics graphics, int icolumn)
			: base(null, sda, hvoRoot, graphics, null)
		{
			m_icolToWatch = icolumn;
			m_wsf = sda.WritingSystemFactory;
			m_styleSheet = stylesheet;
		}

		/// <summary />
		public override void OpenTableRow()
		{
			m_irowCurr++;
			m_icolCurr = 0;
			m_nColSpanCurr = 0;
			base.OpenTableRow();
		}

		/// <summary />
		public override void OpenTableCell(int nRowSpan, int nColSpan)
		{
			// reset the width
			Width = 0;
			// add the col span of the previous cell
			m_icolCurr += m_nColSpanCurr;
			// then log the current cell span.
			m_nColSpanCurr = nColSpan;
			base.OpenTableCell(nRowSpan, nColSpan);
		}

		bool m_fInPara;

		/// <summary />
		public override void OpenParagraph()
		{
			// new paragraph means new line, so reset our width.
			m_fInPara = true;
			Width = 0;
			base.OpenParagraph();
		}

		/// <summary />
		protected override void CloseTheObject()
		{
			base.CloseTheObject();

			if (!m_fInPara)
			{
				// we didn't open the object in the context of a paragraph
				// so assume these are getting added to their
				// own paragraph boxes.
				UpdateMaxStringWidth();
			}
		}

		/// <summary>
		/// update max string width info, if we haven't already done so.
		/// </summary>
		public override void CloseParagraph()
		{
			base.CloseParagraph();
			// update max string width info
			UpdateMaxStringWidth();
			m_fInPara = false;
		}

		/// <summary>
		/// update max string width info, if we haven't already done so.
		/// </summary>
		public override void CloseTableCell()
		{
			base.CloseTableCell();
			UpdateMaxStringWidth();
		}

		/// <summary>
		/// Updates the column width counter for auto-resizing Views columns.
		/// </summary>
		protected virtual void UpdateMaxStringWidth()
		{
			if (Width > MaxStringWidth)
			{
				MaxStringWidth = Width;
				RowIndexOfMaxStringWidth = m_irowCurr;
			}
			Width = 0;
		}

		/// <summary>
		/// return the maximum string pixel width in the display of a given column that
		/// can be used to provide the width of the column.
		/// </summary>
		public int MaxStringWidth { get; private set; }

		/// <summary>
		/// index of row containing string of longest width
		/// </summary>
		public int RowIndexOfMaxStringWidth { get; private set; } = -1;

		/// <summary />
		public override void AddTsString(ITsString tss)
		{
			// get the first ws from the tss to determine whether we need to use a different font
			// assume if there are multiple embedded wss, they will not throw off the basic width of this text.
			var wsTss = TsStringUtils.GetWsAtOffset(tss, 0);
			Debug.Assert(wsTss > 0, $"Invalid ws({wsTss}) embedded in run in string '{tss.Text}'.");
			if (wsTss != m_fontWs && wsTss > 0)
			{
				m_fontWs = wsTss;
				SetFontToCurrentWs();
			}
			base.AddTsString(tss);
		}

		private void SetFontToCurrentWs()
		{
			m_font = GetFontFromWs(m_fontWs);
		}

		private Font GetFontFromWs(int ws)
		{
			Font font;
			if (ws == 0)
			{
				// create a font ex-nihilo
				var fontName = MiscUtils.StandardSansSerif;
				const int fontSize = 14;
				font = new Font(fontName, fontSize);
				m_wsToFont.Add(0, font);
			}
			else if (!m_wsToFont.TryGetValue(ws, out font))
			{
				// get font from stylesheet.
				font = FontHeightAdjuster.GetFontForNormalStyle(ws, m_styleSheet, m_wsf);
				m_wsToFont.Add(ws, font);
			}
			return font;
		}

		/// <summary />
		public override void AddResultString(string s)
		{
			// if we haven't already enabled a font, do so now.
			if (m_font == null)
				SetFontToCurrentWs();
			base.AddResultString(s);
		}

		public override void AddResultString(string s, int ws)
		{
			if (m_fontWs != ws && ws > 0)
			{
				m_fontWs = ws;
			}
			base.AddResultString(s, ws);
		}

		/// <summary>
		/// only update string width if we're in the column we're interested in.
		/// </summary>
		protected override void AddStringWidth(string s)
		{
			if (m_icolCurr != m_icolToWatch || m_nColSpanCurr != 1)
			{
				return;
			}
			base.AddStringWidth(s);
		}
	}
}