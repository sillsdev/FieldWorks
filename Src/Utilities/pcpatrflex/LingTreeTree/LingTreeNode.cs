using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text;

namespace LingTree
{
	/// <summary>
	/// Summary description for LingTreeNode.
	/// </summary>
	[Serializable]
	public class LingTreeNode : Control
	{
		public enum NodeType
		{
			NonTerminal = 0,
			Lex,
			Gloss,
		}

		string m_sContent; // content of the node
		string m_sId; // id of the node
		NodeType m_Type; // node type (non-terminal, lex, gloss)
		bool m_bTriangle; // draw triangle instead of line
		bool m_bOmitLine; // no line above node
		int m_iIndex; // index of node within its tree
		int m_iXCoord; // left horizontal position of the node
		int m_iXMid; // mid horizontal position of the node
		int m_iYCoord; // upper vertical position of the node
		int m_iYLowerMid; // lower mid position of the node for drawing a line below the node
		int m_iYUpperMid; // upper mid position of the node for drawing a line above the node
		int m_iHeight; // height of the node
		int m_iWidth; // width of the node
		int m_iLevel; // level (or depth) of the node within the tree
		const int m_iYCoordAdjustment = 40; // adjustment value
		LingTreeNode m_Daughter; // leftmost daughter of this node in the tree
		LingTreeNode m_Sister; // immediate sister to the right of this node in the tree
		LingTreeNode m_Mother; // mother of this node in the tree
		const int m_iTriangleOffset = 300;

		public LingTreeNode(
			int iLevel,
			int iIndex,
			string sContent,
			NodeType eNodeType,
			LingTreeNode ndeMother,
			LingTreeNode ndeDaughter,
			LingTreeNode ndeSister
		)
		{
			Level = iLevel;
			Index = iIndex;
			Content = sContent;
			Type = eNodeType;
			Mother = ndeMother;
			Daughter = ndeDaughter;
			Sister = ndeSister;
			Triangle = false;
			OmitLine = false;
			Height = 0;
			Width = 0;
			XCoord = 0;
			YCoord = 0;
			Id = "";
		}

		public LingTreeNode()
			: this(
				0,
				0,
				"",
				NodeType.NonTerminal,
				(LingTreeNode)null,
				(LingTreeNode)null,
				(LingTreeNode)null
			)
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public static double SVGConvert(int i)
		{
			return ((Convert.ToDouble(i)) / 100.0);
		}

		/// <summary>
		/// Gets/sets Content.
		/// </summary>
		public string Content
		{
			get { return m_sContent; }
			set { m_sContent = value; }
		}

		/// <summary>
		/// Gets/sets Id.
		/// </summary>
		public string Id
		{
			get { return m_sId; }
			set { m_sId = value; }
		}

		/// <summary>
		/// Gets/sets Type.
		/// </summary>
		public NodeType Type
		{
			get { return m_Type; }
			set { m_Type = value; }
		}

		/// <summary>
		/// Gets/sets Triangle.
		/// </summary>
		public bool Triangle
		{
			get { return m_bTriangle; }
			set { m_bTriangle = value; }
		}

		/// <summary>
		/// Gets/sets OmitLine.
		/// </summary>
		public bool OmitLine
		{
			get { return m_bOmitLine; }
			set { m_bOmitLine = value; }
		}

		/// <summary>
		/// Gets/sets Index.
		/// </summary>
		public int Index
		{
			get { return m_iIndex; }
			set { m_iIndex = value; }
		}

		/// <summary>
		/// Gets/sets XCoord.
		/// </summary>
		public int XCoord
		{
			get { return m_iXCoord; }
			set { m_iXCoord = value; }
		}

		/// <summary>
		/// Gets/sets XMid.
		/// </summary>
		public int XMid
		{
			get { return m_iXMid; }
			set { m_iXMid = value; }
		}

		/// <summary>
		/// Gets/sets YCoord.
		/// </summary>
		public int YCoord
		{
			get { return m_iYCoord; }
			set { m_iYCoord = value; }
		}

		/// <summary>
		/// Gets/sets YLowerMid.
		/// </summary>
		public int YLowerMid
		{
			get { return m_iYLowerMid; }
			set { m_iYLowerMid = value; }
		}

		/// <summary>
		/// Gets/sets YUpperMid.
		/// </summary>
		public int YUpperMid
		{
			get { return m_iYUpperMid; }
			set { m_iYUpperMid = value; }
		}

		/// <summary>
		/// Gets/sets Height.
		/// </summary>
		///
		public new int Height
		{
			get { return m_iHeight; }
			set { m_iHeight = value; }
		}

		/// <summary>
		/// Gets/sets Width.
		/// </summary>
		public new int Width
		{
			get { return m_iWidth; }
			set { m_iWidth = value; }
		}

		/// <summary>
		/// Gets/sets Level.
		/// </summary>
		public int Level
		{
			get { return m_iLevel; }
			set { m_iLevel = value; }
		}

		/// <summary>
		/// Gets/sets Daughter.
		/// </summary>
		public LingTreeNode Daughter
		{
			get { return m_Daughter; }
			set { m_Daughter = value; }
		}

		/// <summary>
		/// Gets/sets Sister.
		/// </summary>
		public LingTreeNode Sister
		{
			get { return m_Sister; }
			set { m_Sister = value; }
		}

		/// <summary>
		/// Gets/sets Mother.
		/// </summary>
		public LingTreeNode Mother
		{
			get { return m_Mother; }
			set { m_Mother = value; }
		}

		///////////////////////////////////////////////////////////////////////////////
		// NAME
		//    CalculateHeight
		// ARGUMENTS
		//    pTree - pointer to tree this node is in
		//    pDC - pointer to the device context
		// DESCRIPTION
		//    Calculate the height of this node
		// RETURN VALUE
		//    none
		//
		public void CalculateHeight(LingTreeTree Tree, Graphics grfx)
		{
			Font font = GetTextFont(Tree);
			Height = (int)Math.Ceiling(grfx.MeasureString(Content, font).Height);
		}

		///////////////////////////////////////////////////////////////////////////////
		// NAME
		//    CalculateMaxHeight
		// ARGUMENTS
		//    pTree - pointer to tree this node is in
		//    pDC - pointer to the device context
		// DESCRIPTION
		//    Determine the maximum height at each level in the tree
		//    It assumes that the height of this and all other nodes hav already been established
		// RETURN VALUE
		//    none
		//
		public void CalculateMaxHeight(LingTreeTree Tree, Graphics grfx)
		{
			LingTreeNode Node;
			// Determine the height for this node
			CalculateHeight(Tree, grfx);
			// calculate max height of all daughters
			Node = Daughter;
			while (Node != null)
			{
				Node.CalculateMaxHeight(Tree, grfx);
				Node = Node.Sister;
			}
			// set tree's maximum height at this node's level
			if (Tree.GetMaxLevelHeight(Level) < Height)
				Tree.SetMaxLevelHeight(Level, Height);
		}

		///////////////////////////////////////////////////////////////////////////////
		// NAME
		//    CalculateWidth
		// ARGUMENTS
		//    pTree - pointer to tree this node is in
		//    pDC - pointer to the device context
		// DESCRIPTION
		//    Calculate the width of this node
		// RETURN VALUE
		//    none
		//
		public void CalculateWidth(LingTreeTree Tree, Graphics grfx)
		{
			Font font = GetTextFont(Tree);
			int i = Content.IndexOf("/s");
			if (i != -1)
			{ // have a regular subscript
				Width = CalculateWidthWithSuperOrSubScript(i, font, grfx, false);
			}
			else
			{
				i = Content.IndexOf("/_");
				if (i != -1)
				{ // have an italic subscript
					Width = CalculateWidthWithSuperOrSubScript(i, font, grfx, true);
				}
				else
				{
					i = Content.IndexOf("/S");
					if (i != -1)
					{ // have a regular superscript
						Width = CalculateWidthWithSuperOrSubScript(i, font, grfx, false);
					}
					else
					{
						i = Content.IndexOf("/^");
						if (i != -1)
						{ // have an italic superscript
							Width = CalculateWidthWithSuperOrSubScript(i, font, grfx, true);
						}
						else
							Width = (int)Math.Ceiling(grfx.MeasureString(Content, font).Width);
					}
				}
			}
		}

		private int CalculateWidthWithSuperOrSubScript(
			int i,
			Font font,
			Graphics grfx,
			bool bIsItalic
		)
		{
			string sBefore = Content.Substring(0, i);
			string sAfter = Content.Substring(i + 2);
			float fSize = font.Size * .6667f;
			Font scriptFont;
			if (bIsItalic)
			{
				scriptFont = new Font(font.Name, fSize, System.Drawing.FontStyle.Italic);
			}
			else
			{
				scriptFont = new Font(font.Name, fSize, System.Drawing.FontStyle.Regular);
			}
			return (int)
				Math.Ceiling(
					grfx.MeasureString(sBefore, font).Width
						+ grfx.MeasureString(sAfter, scriptFont).Width
						- 9f * font.Size
				);
		}

		///////////////////////////////////////////////////////////////////////////////
		// NAME
		//    CalculateXCoordinate
		// ARGUMENTS
		//    pTree - pointer to tree this node is in
		//    pDC - pointer to the device context
		//    iMaxColWidth - maximum width of the current column
		// DESCRIPTION
		//    Determine the X-axis coordinate for this node
		//    It assumes that the width of this and all other nodes have
		//      already been established.
		//    It also assumes that higher branching nodes are not wider than the
		//      total width of their daughters (which may not always be true...)
		// RETURN VALUE
		//    The X-axis coordinate at the mid-point of this node
		//
		public int CalculateXCoordinate(LingTreeTree Tree, Graphics grfx, int iMaxColWidth)
		{
			XMid = 0;

			// Determine the width for this node
			CalculateWidth(Tree, grfx);
			if (Daughter != null)
			{ // is a non-terminal node
				LingTreeNode NextDaughter = Daughter.Sister;
				if (NextDaughter != null)
				{ // node branches
					if (iMaxColWidth < m_iWidth)
						iMaxColWidth = m_iWidth; // remember widest node in the column
				}
				else
				{ // only one daughter, so could be in a column
					if (iMaxColWidth < Width)
						iMaxColWidth = Width; // remember widest node in the column
				}
				int iLeftMost = Daughter.CalculateXCoordinate(Tree, grfx, iMaxColWidth);
				int iRightMost = iLeftMost;
				while (NextDaughter != null)
				{ // calculate coordinates for other daughters
					iRightMost = NextDaughter.CalculateXCoordinate(Tree, grfx, iMaxColWidth);
					NextDaughter = NextDaughter.Sister;
				}
				// take mid point between first & last daughter
				XMid = (iLeftMost + iRightMost) / 2;
				if (iRightMost > iLeftMost)
				{
					if (m_iWidth > (iRightMost - iLeftMost))
					{
						int iAdjust = (m_iWidth - (iRightMost - iLeftMost)) / 2;
						m_iXMid += iAdjust;

						NextDaughter = Daughter;
						while (NextDaughter != null)
						{
							NextDaughter.AdjustXValues(Tree, iAdjust);
							NextDaughter = NextDaughter.Sister;
						}
					}
				}
			}
			else
			{ // is a terminal node
				if (iMaxColWidth < Width)
					iMaxColWidth = Width; // this might be the widest node in the column
				XMid =
					(int)Tree.HorizontalOffset
					+ // Offset from last terminal node plus
					iMaxColWidth / 2; // half the width of this column
				Tree.HorizontalOffset =
					XMid
					+ // have a new offset for next terminal node
					Tree.HorizontalGap
					+ // gap between terminal nodes plus
					iMaxColWidth / 2; // half the width of the widest node in this column
			}
			XCoord = XMid - Width / 2; // adjust for width of this node
			int iEnd = XCoord + Width;
			if (Triangle)
			{
				iEnd += Tree.HorizontalGap / 2;
			}
			if (iEnd > Tree.XSize)
				Tree.XSize = iEnd; // Keep track of total width for scrolling
			return XMid;
		}

		/// <summary>
		/// Determine the Y-axis coordinate for this node
		/// </summary>
		/// <param name="iVerticalOffset">vertical offset up to this node</param>
		/// <param name="Tree">the tree itself</param>
		/// <param name="grfx">grpahics info</param>
		public void CalculateYCoordinate(int iVerticalOffset, LingTreeTree Tree, Graphics grfx)
		{
			LingTreeNode Node;
			// Determine Y-axis coordinates for this node
			YCoord = iVerticalOffset;
			YLowerMid = YCoord + Height + m_iYCoordAdjustment;
			YUpperMid = YCoord - m_iYCoordAdjustment;
			if (YLowerMid > Tree.YSize)
				Tree.YSize = YLowerMid; // Keep track of total height for scrolling
			if (Type == NodeType.Lex)
			{ // keep track of lowest for "flat" view
				if (YCoord > Tree.LexBottomYCoord)
					Tree.LexBottomYCoord = YCoord;
				if (YUpperMid > Tree.LexBottomYUpperMid)
					Tree.LexBottomYUpperMid = YUpperMid;
			}
			if (Type == NodeType.Gloss)
			{ // keep track of lowest for "flat" view
				if (YCoord > Tree.GlossBottomYCoord)
					Tree.GlossBottomYCoord = YCoord;
			}
			// Determine Y-axis coordinate for any daughters
			Node = Daughter;
			while (Node != null)
			{
				int iDaughterYCoordinate = YCoord + Tree.GetMaxLevelHeight(Level);
				if (Node.Type != NodeType.Gloss)
					iDaughterYCoordinate += Tree.VerticalGap;
				else
					iDaughterYCoordinate += Tree.LexGlossGapAdjustment;
				Node.CalculateYCoordinate(iDaughterYCoordinate, Tree, grfx);
				Node = Node.Sister;
			}
		}

		///////////////////////////////////////////////////////////////////////////////
		// NAME
		//    Draw
		// ARGUMENTS
		//    pTree - pointer to tree this node is in
		//    pDC - pointer to a device context
		// DESCRIPTION
		//    Draw this node and all subnodes
		// RETURN VALUE
		//    none
		//
		public void Draw(LingTreeTree Tree, Graphics grfx, Color color, double dLineWidth)
		{
			LingTreeNode Node;
			Pen pen = new Pen(color, (float)dLineWidth);
			Font font = GetTextFont(Tree);
			Color clr = GetTextColor(Tree);
			SolidBrush brush = new SolidBrush(clr);
			// Draw this node
			if (Tree.ShowFlatView)
			{ // adjust lex and gloss Y coordinates
				if (Type == NodeType.Lex)
				{
					YCoord = Tree.LexBottomYCoord;
					YUpperMid = Tree.LexBottomYUpperMid;
				}
				if (Type == NodeType.Gloss)
				{
					YCoord = Tree.GlossBottomYCoord;
				}
			}
			DrawString(Content, grfx, font, brush);
			// Draw daughter nodes
			Node = Daughter;
			while (Node != null)
			{
				Node.Draw(Tree, grfx, color, dLineWidth);
				if (Type == NodeType.NonTerminal)
				{
					if (
						Tree.TrySmoothing
						&& // when smoothing, set smoothing values
						(
							(
								(Node.XMid != XMid)
								&& //   when have slanted lines or
								(Node.YUpperMid != YLowerMid)
							) || Node.Triangle
						)
					) //   when are doing a triangle (which will have slanted lines)
					{
						grfx.SmoothingMode = SmoothingMode.HighQuality;
						if (Tree.TryPixelOffset)
							grfx.PixelOffsetMode = PixelOffsetMode.HighQuality;
						else
							grfx.PixelOffsetMode = PixelOffsetMode.None;
					}
					else
					{
						grfx.SmoothingMode = SmoothingMode.None;
						grfx.PixelOffsetMode = PixelOffsetMode.None;
					}
					if (!Node.Triangle)
					{
						if (!Node.OmitLine)
						{
							grfx.DrawLine(pen, Node.XMid, Node.YUpperMid, XMid, YLowerMid);
						}
					}
					else
					{
						int ix = Node.XCoord + m_iTriangleOffset;
						Point[] apt =
						{
							new Point(ix, Node.YCoord),
							new Point(XMid, YLowerMid),
							new Point(ix + (Node.Width - 2 * m_iTriangleOffset), Node.YCoord),
							new Point(ix, Node.YCoord)
						};
						grfx.DrawLines(pen, apt);
					}
				}
				Node = Node.Sister;
			}
			pen.Dispose();
			brush.Dispose();
		}

		///////////////////////////////////////////////////////////////////////////////
		// NAME
		//    DrawString
		// ARGUMENTS
		//    pTree - pointer to tree this node is in
		//    pDC - pointer to a device context
		// DESCRIPTION
		//    Draw this node and all subnodes
		// RETURN VALUE
		//    none
		//
		protected void DrawString(string sRest, Graphics grfx, Font font, SolidBrush brush)
		{
			DrawBackground(grfx);
			int i = sRest.IndexOf("/s");
			if (i != -1)
			{ // have a regular subscript
				ProcessSuperOrSubScript(grfx, sRest, i, font, brush, true, false);
			}
			else
			{
				i = sRest.IndexOf("/_");
				if (i != -1)
				{ // have an italic subscript
					ProcessSuperOrSubScript(grfx, sRest, i, font, brush, true, true);
				}
				else
				{
					i = sRest.IndexOf("/S");
					if (i != -1)
					{ // have a regular superscript
						ProcessSuperOrSubScript(grfx, sRest, i, font, brush, false, false);
					}
					else
					{
						i = sRest.IndexOf("/^");
						if (i != -1)
						{ // have an italic superscript
							ProcessSuperOrSubScript(grfx, sRest, i, font, brush, false, true);
						}
						else
						{
							grfx.DrawString(sRest, font, brush, (float)XCoord, (float)YCoord);
						}
					}
				}
			}
		}

		/// <summary>
		/// Draws background of node, so can show selected items, e.g.
		/// </summary>
		/// <param name="grfx"></param>
		private void DrawBackground(Graphics grfx)
		{
			Region reg = new Region(
				new Rectangle(this.XCoord, this.YCoord, this.Width, this.Height)
			);
			grfx.SetClip(reg, System.Drawing.Drawing2D.CombineMode.Replace);
			grfx.Clear(BackColor);
			grfx.ResetClip();
		}

		public static string SVGConvertColorToSVGColorString(Color clr)
		{
			int i = clr.ToArgb();
			string sTemp = i.ToString("X");
			string sSVGColor = "#" + sTemp.Substring(2);
			return sSVGColor;
		}

		public void SVGCreate(
			LingTreeTree Tree,
			StringBuilder sb,
			string sLineColor,
			double dLineWidth
		)
		{
			LingTreeNode Node;
			Font font = GetTextFont(Tree);
			Color clr = GetTextColor(Tree);
			// Draw this node
			if (Tree.ShowFlatView)
			{ // adjust lex and gloss Y coordinates
				if (Type == NodeType.Lex)
				{
					YCoord = Tree.LexBottomYCoord;
					YUpperMid = Tree.LexBottomYUpperMid;
				}
				if (Type == NodeType.Gloss)
				{
					YCoord = Tree.GlossBottomYCoord;
				}
			}
			string sClrName = LingTreeNode.SVGConvertColorToSVGColorString(clr);
			SVGDrawString(Content, sb, font, sClrName);
			// Draw daughter nodes
			Node = Daughter;
			while (Node != null)
			{
				Node.SVGCreate(Tree, sb, sLineColor, dLineWidth);
				if (Type == NodeType.NonTerminal)
				{
					if (!Node.Triangle)
					{
						if (!Node.OmitLine)
						{
							SVGDrawLine(
								sb,
								sLineColor,
								dLineWidth,
								Node.XMid,
								Node.YUpperMid,
								XMid,
								YLowerMid
							);
						}
					}
					else
					{
						int ix = Node.XCoord + m_iTriangleOffset;
						Point[] apt =
						{
							new Point(ix, Node.YCoord),
							new Point(XMid, YLowerMid),
							new Point(ix + (Node.Width - 2 * m_iTriangleOffset), Node.YCoord),
							new Point(ix, Node.YCoord)
						};
						SVGDrawLines(sb, sLineColor, dLineWidth, apt);
					}
				}
				Node = Node.Sister;
			}
		}

		protected void SVGDrawString(string sContent, StringBuilder sb, Font font, string sColor)
		{
			sb.Append("<text");
			sb.AppendFormat(" x=\"{0}mm\"", SVGConvert(XCoord + (Width / 6))); // I don't know why this seems to work, either, but it does... very odd
			sb.AppendFormat(" y=\"{0}mm\"", SVGConvert(YCoord + (Height / 2))); // I don't know why this seems to work - the documentation makes it seem like shouldn't need this
			sb.AppendFormat(" font-family=\"{0}\"", font.FontFamily.Name);
			sb.AppendFormat(" font-size=\"{0}\"", font.Size);
			sb.AppendFormat(" fill=\"{0}\"", sColor);
			if (font.Style == FontStyle.Bold)
				sb.Append(" font-weight=\"bold\"");
			bool bFontIsItalic = font.Style == FontStyle.Italic;
			if (bFontIsItalic)
				sb.Append(" font-style=\"italic\"");
			if (m_sId.Length > 0)
				sb.AppendFormat(" onclick=\"OnClickLingTreeNode(&quot;{0}&quot;)\"", m_sId);
			sb.Append(">");
			SVGRefineContent(sb, sContent, bFontIsItalic);
			sb.Append("\r\n");
		}

		private void SVGRefineContent(StringBuilder sb, string sContent, bool bFontIsItalic)
		{
			int i = sContent.IndexOf("/s");
			if (i != -1)
			{ // have a regular subscript
				SVGProcessSuperOrSubScript(sb, sContent, i, true, false, bFontIsItalic);
			}
			else
			{
				i = sContent.IndexOf("/_");
				if (i != -1)
				{ // have an italic subscript
					SVGProcessSuperOrSubScript(sb, sContent, i, true, true, bFontIsItalic);
				}
				else
				{
					i = sContent.IndexOf("/S");
					if (i != -1)
					{ // have a regular superscript
						SVGProcessSuperOrSubScript(sb, sContent, i, false, false, bFontIsItalic);
					}
					else
					{
						i = sContent.IndexOf("/^");
						if (i != -1)
						{ // have an italic superscript
							SVGProcessSuperOrSubScript(sb, sContent, i, false, true, bFontIsItalic);
						}
						else
							sb.AppendFormat("{0}</text>", sContent);
					}
				}
			}
		}

		private void SVGProcessSuperOrSubScript(
			StringBuilder sb,
			string sContent,
			int i,
			bool bIsSubscript,
			bool bIsItalic,
			bool bFontIsItalic
		)
		{
			string sSubOrSuper;
			string sBefore = sContent.Substring(0, i);
			sb.Append(sBefore);
			if (bIsSubscript)
				sSubOrSuper = "sub";
			else
				sSubOrSuper = "super";
			sb.AppendFormat("<tspan baseline-shift=\"{0}\" font-size=\"67%\"", sSubOrSuper);
			if (bIsItalic)
			{
				string sItalic;
				if (bFontIsItalic)
					sItalic = "normal"; // toggle italicicity
				else
					sItalic = "italic";
				sb.AppendFormat(" font-style=\"{0}\"", sItalic);
			}
			sb.Append(">");
			sb.Append(sContent.Substring(i + 2));
			sb.Append("</tspan></text>");
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="linesColor"></param>
		/// <param name="dLineWidth"></param>
		/// <param name="iDaughterXMid"></param>
		/// <param name="iDaughterYUpperMid"></param>
		/// <param name="iXMid"></param>
		/// <param name="iYLowerMid"></param>
		private void SVGDrawLine(
			StringBuilder sb,
			string sLineColor,
			double dLineWidth,
			int iDaughterXMid,
			int iDaughterYUpperMid,
			int iXMid,
			int iYLowerMid
		)
		{
			sb.Append("<line");
			sb.AppendFormat(" x1=\"{0}mm\"", SVGConvert(iDaughterXMid));
			sb.AppendFormat(" y1=\"{0}mm\"", SVGConvert(iDaughterYUpperMid - Height / 4)); // - Height/4)); // - m_iYCoordAdjustment));
			sb.AppendFormat(" x2=\"{0}mm\"", SVGConvert(iXMid));
			sb.AppendFormat(" y2=\"{0}mm\"", SVGConvert(iYLowerMid - Height / 4)); //  - Height/4)); //  + m_iYCoordAdjustment));
			sb.AppendFormat(" stroke=\"{0}\"", sLineColor);
			sb.AppendFormat(" stroke-width=\"{0}mm\"", dLineWidth / 100.0);
			sb.Append("/>\r\n");
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="linesColor"></param>
		/// <param name="apt"></param>
		private void SVGDrawLines(
			StringBuilder sb,
			string sLineColor,
			double dLineWidth,
			Point[] apt
		)
		{
#if Orig
			sb.Append("<polygon points=\"");
			foreach (Point pt in apt)
				sb.AppendFormat("{0}mm,{1}mm ", SVGConvert(pt.X), SVGConvert(pt.Y));
			sb.AppendFormat("\" stroke=\"{0}\"", sLineColor);
			sb.AppendFormat(" stroke-width=\"{0}\"", dLineWidth); // /100.0);
			sb.Append("/>\r\n");
#else
			SVGDrawLine(sb, sLineColor, dLineWidth, apt[0].X, apt[0].Y, apt[1].X, apt[1].Y);
			SVGDrawLine(sb, sLineColor, dLineWidth, apt[1].X, apt[1].Y, apt[2].X, apt[2].Y);
			SVGDrawLine(sb, sLineColor, dLineWidth, apt[2].X, apt[2].Y, apt[3].X, apt[3].Y);
#endif
		}

		private void ProcessSuperOrSubScript(
			Graphics grfx,
			string sRest,
			int i,
			Font font,
			SolidBrush brush,
			bool bIsSubscript,
			bool bIsItalic
		)
		{
			string sBefore = sRest.Substring(0, i);
			grfx.DrawString(sBefore, font, brush, (float)XCoord, (float)YCoord);
			float fXAdjust =
				grfx.MeasureString(sBefore, font).Width + (float)XCoord - 9f * font.Size; // what should this really be?
			float fSize = font.Size * .6667f;
			float fFactor;
			Font scriptFont;
			if (bIsSubscript)
				fFactor = .5f;
			else
				fFactor = .1f;
			if (bIsItalic)
				scriptFont = new Font(
					font.Name,
					fSize,
					font.Style | System.Drawing.FontStyle.Italic
				);
			else
			{
				int iStyleCode = font.Style.GetHashCode();
				int iItalicCode = System.Drawing.FontStyle.Italic.GetHashCode();
				int iResult = iStyleCode & iItalicCode;
				if (iResult > 0)
					scriptFont = new Font(
						font.Name,
						fSize,
						font.Style ^ System.Drawing.FontStyle.Italic
					);
				else
					scriptFont = new Font(font.Name, fSize, font.Style);
			}
			float cyLineSpace = font.GetHeight(grfx);
			int iCellSpace = font.FontFamily.GetLineSpacing(font.Style);
			int iCellAscent = font.FontFamily.GetCellAscent(font.Style);
			float cyAscent = cyLineSpace * iCellAscent / iCellSpace;
			float cScript = cyAscent * fFactor;

			float cy = (float)YCoord;
			if (bIsSubscript)
				cy += cScript;
			else
				cy -= cScript;
			grfx.DrawString(sRest.Substring(i + 2), scriptFont, brush, fXAdjust, cy);
		}

		///////////////////////////////////////////////////////////////////////////////
		// NAME
		//    GetTextFont
		// ARGUMENTS
		//    Tree - tree this node is in
		// DESCRIPTION
		//    Set font for this node
		// RETURN VALUE
		//    font of the node type
		//
		Font GetTextFont(LingTreeTree Tree)
		{
			if (Type == NodeType.NonTerminal)
			{
				return Tree.NTFont;
			}
			else if (Type == NodeType.Lex)
			{
				return Tree.LexFont;
			}
			else if (Type == NodeType.Gloss)
			{
				return Tree.GlossFont;
			}
			else // should never happen, but...
			{
				return Tree.NTFont;
			}
		}

		///////////////////////////////////////////////////////////////////////////////
		// NAME
		//    GetTextColor
		// ARGUMENTS
		//    Tree - tree this node is in
		// DESCRIPTION
		//    Set color for this node
		// RETURN VALUE
		//    color of the node type
		//
		Color GetTextColor(LingTreeTree Tree)
		{
			if (Type == NodeType.NonTerminal)
			{
				return Tree.NTColor;
			}
			else if (Type == NodeType.Lex)
			{
				return Tree.LexColor;
			}
			else if (Type == NodeType.Gloss)
			{
				return Tree.GlossColor;
			}
			else // should never happen, but...
			{
				return Tree.NTColor;
			}
		}

		void AdjustXValues(LingTreeTree tree, int iAdjust)
		{
			// adjust this node
			XCoord += iAdjust;
			XMid += iAdjust;
			tree.HorizontalOffset = tree.HorizontalOffset + iAdjust;
			// adjust any daughter nodes
			LingTreeNode NextDaughter = Daughter;
			while (NextDaughter != null)
			{
				NextDaughter.AdjustXValues(tree, iAdjust);
				NextDaughter = NextDaughter.Sister;
			}
		}
	}
}
