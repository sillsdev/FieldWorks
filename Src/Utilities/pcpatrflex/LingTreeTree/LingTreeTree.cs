using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Xml;
using System.Text;

namespace LingTree
{
	/// <summary>
	/// Summary description for LingTreeTree.
	/// </summary>
	[Serializable]
	public class LingTreeTree : Control //Object
	{
		const int kMaxLevels = 100;
		string m_sVersion;
		string m_sDescription; // parenthesized form of the tree
		LingTreeNode m_Root; // root node of the tree
		int m_iVerticalGap; // extra gap between levels
		int m_iHorizontalGap; // extra gap between terminal nodes
		int m_iInitialXCoord; // initial, leftmost X coordinate
		int m_iInitialYCoord; // initial, topmost Y coordinate
		int m_iHorizontalOffset; // current XCoord of last terminal node processed
		int m_iXSize; // total width of tree
		int m_iYSize; // total height of tree
		int m_iGlossBottomYCoord; // lowest Gloss Y Coord (for "flat" view)
		int m_iLexBottomYCoord; // lowest Lex   Y Coord (for "flat" view)
		int m_iLexBottomYUpperMid; // lowest Lex   Y upper mid (for "flat" view)
		int m_iLexGlossGapAdjustment; // extra gap adjustment between lex and gloss
		int[] m_aiMaxLevelHeights; // array of levels, used for storing maximum height of a level
		bool m_bUseRightToLeft;
		bool m_bTrySmoothing;
		bool m_bTryPixelOffset;
		bool m_bShowFlatView;
		Font m_GlossFont;
		Font m_LexFont;
		Font m_NTFont;
		Color m_GlossColor;
		Color m_LexColor;
		Color m_NTColor;
		Color m_BackgroundColor;
		Color m_LinesColor;
		string m_sGlossFontFace;
		float m_fGlossFontSize;
		FontStyle m_GlossFontStyle;
		string m_sLexFontFace;
		float m_fLexFontSize;
		FontStyle m_LexFontStyle;
		string m_sNTFontFace;
		float m_fNTFontSize;
		FontStyle m_NTFontStyle;
		int m_iGlossColorArgb;
		int m_iLexColorArgb;
		int m_iNTColorArgb;
		int m_iBackgroundColorArgb;
		int m_iLinesColorArgb;
		int[] m_aiCustomColors = new int[16];
		double m_dLineWidth;
		string m_sMessageText;
		public event LingTreeNodeClickedEventHandler m_LingTreeNodeClickedEvent;

		public LingTreeTree()
			: this("default") { }

		public LingTreeTree(string sVersion)
			: this(
				100,
				100,
				300,
				300,
				0,
				new Font("Courier New", 12),
				new Font("Courier New", 12),
				new Font("Courier New", 12),
				Color.Green,
				Color.Blue,
				Color.Black,
				Color.Black,
				10.0,
				sVersion
			)
		{ }

		public LingTreeTree(
			int iInitialXCoord,
			int iInitialYCoord,
			int iHorizontalGap,
			int iVerticalGap,
			int iLexGlossGapAdjustment,
			Font fntGlossFont,
			Font fntLexFont,
			Font fntNTFont,
			Color clrGlossColor,
			Color clrLexColor,
			Color clrNTColor,
			Color clrLines,
			double dLineWidth,
			string sVersion
		)
		{
			m_aiMaxLevelHeights = new int[kMaxLevels];
			InitialXCoord = iInitialXCoord;
			InitialYCoord = iInitialYCoord;
			HorizontalGap = iHorizontalGap;
			VerticalGap = iVerticalGap;
			LexGlossGapAdjustment = iLexGlossGapAdjustment;
			GlossFont = fntGlossFont;
			LexFont = fntLexFont;
			NTFont = fntNTFont;
			GlossColor = clrGlossColor;
			LexColor = clrLexColor;
			NTColor = clrNTColor;
			LinesColor = clrLines;
			LineWidth = dLineWidth;
			Version = sVersion;
			m_Root = null;
			this.Paint += new PaintEventHandler(OnPaint);
			this.MouseUp += new MouseEventHandler(LingTreeTree_MouseUp);
			this.m_LingTreeNodeClickedEvent = new LingTreeNodeClickedEventHandler(OnNodeClicked);
		}

		public void OnNodeClicked(object sender, LingTreeNodeClickedEventArgs ltne)
		{
			// do nothing here
		}

		public void OnPaint(object obj, PaintEventArgs pea)
		{
			Graphics grfx = pea.Graphics;
			CalculateCoordinates(grfx);
			// Adjust size
			Point[] atpt = { new Point(XSize, YSize) };
			grfx.TransformPoints(CoordinateSpace.Device, CoordinateSpace.Page, atpt);
			// guarantee there is something, so it will show next time (when starting
			// without any tree, the values will be zero and it never shows).
			atpt[0].X = Math.Max(10, atpt[0].X);
			atpt[0].Y = Math.Max(10, atpt[0].Y);
			Size = new Size(atpt[0]);
			BackColor = BackgroundColor;
			Draw(grfx, LinesColor);
			foreach (LingTreeNode node in this.Controls)
			{
				node.Location = new Point(node.XCoord, node.YCoord);
				node.Size = new Size(node.Width, node.Height);
				node.BringToFront();
			}
		}

		///////////////////////////////////////////////////////////////////////////////
		// NAME
		//    BeginASubTree
		// ARGUMENTS
		//    pMother  - pointer to mother node of new sub-tree
		//    iLevel - level (or depth) within the tree
		//    iIndex - index number of node in the tree
		// DESCRIPTION
		//    Create a new node which is the root of a subtree during the parsing of a tree description
		// RETURN VALUE
		//    Pointer to newly created node
		//
		LingTreeNode BeginASubTree(LingTreeNode Mother, int iLevel, int iIndex)
		{
			LingTreeNode Node = new LingTreeNode(
				iLevel,
				iIndex,
				null,
				LingTreeNode.NodeType.NonTerminal,
				Mother,
				null,
				null
			);
			if (Mother != null)
			{
				LingTreeNode Sister = Mother.Daughter;
				if (Sister == null)
					Mother.Daughter = Node; // new node is the daughter
				else
				{ // there's a daughter already
					while (Sister.Sister != null)
						Sister = Sister.Sister; // skip to rightmost sister
					Sister.Sister = Node;
				}
			}
			return Node;
		}

		///////////////////////////////////////////////////////////////////////////////
		// NAME
		//    CalculateCoordinates
		// ARGUMENTS
		//    grfx     - pointer to Device Context
		// DESCRIPTION
		//    Caluclate the coordinates for the entire tree
		// RETURN VALUE
		//    none
		//
		public void CalculateCoordinates(Graphics grfx)
		{
			InitCoordinates();
			if (m_Root == null)
			{
				return; // nothing to calculate
			}
			// adjust graphics
			grfx.PageUnit = GraphicsUnit.Millimeter;
			grfx.PageScale = .01f;

			// Calculate height for each level
			m_Root.CalculateMaxHeight(this, grfx);
			// Calculate vertical position for root and all daughters
			m_Root.CalculateYCoordinate(InitialYCoord, this, grfx);
			// Calculate horizontal position for root and all daughters
			m_iHorizontalOffset = InitialXCoord;
			m_Root.CalculateXCoordinate(this, grfx, 0);
			if (UseRightToLeft)
				UseRightToLeftOrientation();
		}

		private void InitCoordinates()
		{
			XSize = 0; // initialize
			YSize = 0;
			LexBottomYCoord = 0;
			LexBottomYUpperMid = 0;
			GlossBottomYCoord = 0;
			for (int i = 0; i < kMaxLevels; i++)
				SetMaxLevelHeight(i, 0);
		}

		public void UseRightToLeftOrientation()
		{
			int iAdjust = m_iXSize + InitialXCoord;
			foreach (LingTreeNode node in Controls)
			{
				node.XCoord = (iAdjust - node.Width) - node.XCoord;
				node.XMid = iAdjust - node.XMid;
			}
		}

		public void Draw(Graphics grfx, Color color)
		{
			if (m_Root == null)
			{
				return;
			}
			m_Root.Draw(this, grfx, color, LineWidth);
			if (MessageText != null && MessageText.Length > 1)
			{
				grfx.DrawString(
					MessageText,
					new Font("Times New Roman", 9.0f),
					new SolidBrush(SystemColors.WindowText),
					2.0f,
					2.0f
				);
			}
		}

		public string CreateSVG(XmlDocument doc, Graphics grfx)
		{
			// parse description from xml doc
			XmlNode topNode = doc.SelectSingleNode("//TreeDescription/node");
			ParseXmlTreeDescription(topNode);
			if (m_Root == null)
			{
				return "";
			}
			return CreateSVG(grfx, LinesColor.Name);
		}

		public string CreateSVG(Graphics grfx, string sLineColor)
		{
			if (m_Root == null)
			{
				return "";
			}
			CalculateCoordinates(grfx);

			//			string sFrontMatter = @"<?xml version='1.0' standalone='no'?><!DOCTYPE svg PUBLIC '-//W3C//DTD SVG 1.1//EN'
			//'http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd'>";
			StringBuilder sb = new StringBuilder();
			sb.Append("<?xml version='1.0' standalone='no'?>\r\n");
			string sSVGElement =
				"<svg width='{0}mm' height='{1}mm' version='1.1' xmlns='http://www.w3.org/2000/svg' contentScriptType='text/javascript'>\r\n";
			sb.AppendFormat(
				sSVGElement,
				LingTreeNode.SVGConvert(XSize),
				LingTreeNode.SVGConvert(YSize)
			);
			sb.Append(
				"<script  id=\"clientEventHandlersJS\">\r\nfunction OnClickLingTreeNode(node){}\r\n</script>\r\n"
			);
			m_Root.SVGCreate(this, sb, sLineColor, LineWidth);
			sb.Append("</svg>");
			return sb.ToString();
		}

		///////////////////////////////////////////////////////////////////////////////
		// NAME
		//    EndASubTree
		// ARGUMENTS
		//    pNode - node which is ending
		// DESCRIPTION
		//    Finish processing a node during the parsing of a tree description
		// RETURN VALUE
		//
		//
		LingTreeNode EndASubTree(LingTreeNode Node)
		{
			// cleanup content
			if (Node.Content == null)
				Node.Content = "";
			else
				Node.Content = Node.Content.Trim();
#if !Orig
			if (!Contains(Node))
			{
				this.Controls.Add(Node);
			}
#endif
			return Node.Mother;
		}

		/// <summary>
		/// Throw exception when find unmatched parens
		/// </summary>
		/// <param name="sIllFormed">Illformed tree message portion</param>
		/// <param name="sDescription">the description</param>
		/// <param name="iPos">position within the description where error was detected</param>
		void ThrowUnmatchedClosingParenException(string sIllFormed, string sDescription, int iPos)
		{
			string sMsg = sIllFormed;
			int iPos2 = Math.Max(0, iPos - 1);
			sMsg += "Unmatched closing parenthesis:\n ";
			sMsg += sDescription.Substring(0, iPos2);
			sMsg += " <ERROR DETECTED HERE> ";
			sMsg += sDescription.Substring(iPos2);
			throw (new LingTreeDescriptionException(sMsg));
		}

		///////////////////////////////////////////////////////////////////////////////
		// NAME
		//    ParseTreeDescription
		// ARGUMENTS
		//    none
		// DESCRIPTION
		//    create tree nodes based on the tree description
		// RETURN VALUE
		//    none
		//
		public bool ParseTreeDescription()
		{
			const string sIllFormed = "Ill-formed tree description!\n";
			LingTreeNode Node = null;
			int iLevel = 0; // level (or depth) of a node within the tree
			int iIndex = 1; // unique index for nodes in the tree
			bool bSeenFirstOpenParen = false;

			if (m_Root != null)
			{
				m_Root = null; // remove any existing tree
			}
			Controls.Clear(); // clear all extant (node) controls
			m_sDescription = m_sDescription.Trim();
			for (int i = 0; i < m_sDescription.Length; i++)
			{
				switch (m_sDescription[i])
				{
					case '(':
						if (bSeenFirstOpenParen && (iLevel == 0))
						{ // Ill-formed tree: final close has been reached, yet there is more
							string sMsg = sIllFormed;
							int iPos2 = Math.Max(0, i);
							sMsg +=
								"End of well-formed tree already reached. Start of a new tree discovered:\n ";
							sMsg += m_sDescription.Substring(0, iPos2);
							sMsg += " <ERROR DETECTED HERE> ";
							sMsg += m_sDescription.Substring(iPos2);
							sMsg += "\nRest of the description will be ignored";
							throw (new LingTreeDescriptionException(sMsg));
						}
						Node = BeginASubTree(Node, iLevel++, iIndex++);
						if (m_Root == null)
							m_Root = Node;
						bSeenFirstOpenParen = true;
						break;
					case ')':
						if (Node == null)
						{
							ThrowUnmatchedClosingParenException(sIllFormed, m_sDescription, i);
							return false;
						}
						Node = EndASubTree(Node);
						iLevel--; // decrement level
						break;
					case '\\':
						if (Node == null)
						{
							ThrowUnmatchedClosingParenException(sIllFormed, m_sDescription, i);
							return false;
						}
						if (
							m_sDescription[i + 1] == ')'
							|| // check for quoted parens
							m_sDescription[i + 1] == '('
						)
							Node.Content = Node.Content + m_sDescription[++i];
#if Orig
						else if (m_sDescription[i + 1] == 'L' && m_sDescription[i + 2] == ' ')
						{
							Node.Type = LingTreeNode.NodeType.Lex;
							i += 2;
						}
						else if (m_sDescription[i + 1] == 'G' && m_sDescription[i + 2] == ' ')
						{
							Node.Type = LingTreeNode.NodeType.Gloss;
							i += 2;
						}
						else if (m_sDescription[i + 1] == 'T' && m_sDescription[i + 2] == ' ')
						{
							Node.Triangle = true;
							i += 2;
						}
#else
						else if (m_sDescription[i + 1] == 'L')
						{
							Node.Type = LingTreeNode.NodeType.Lex;
							i++;
						}
						else if (m_sDescription[i + 1] == 'G')
						{
							Node.Type = LingTreeNode.NodeType.Gloss;
							i++;
						}
						else if (m_sDescription[i + 1] == 'T')
						{
							Node.Triangle = true;
							i++;
						}
						else if (m_sDescription[i + 1] == 'O')
						{
							Node.OmitLine = true;
							i++;
						}
#endif
						break;
					default:
						if (Node == null)
						{
							ThrowUnmatchedClosingParenException(sIllFormed, m_sDescription, i);
							return false;
						}
						Node.Content = Node.Content + m_sDescription[i];
						break;
				}
			}
			if (iLevel == 0)
				return true;
			else
			{
				string sMsg = sIllFormed;
				sMsg += iLevel;
				sMsg += " unmatched opening parenthes";
				if (iLevel == 1)
					sMsg += "i";
				else
					sMsg += "e";
				sMsg += "s discovered at the end of the description.";
				throw (new LingTreeDescriptionException(sMsg));
			}
		}

		public void ParseXmlTreeDescription(XmlNode treeDescriptionTopNode)
		{
			if (m_Root != null)
			{
				m_Root = null; // remove any existing tree
			}
			Controls.Clear(); // clear all extant (node) controls
							  // process top node and all daughters
			int iIndex = 1;
			ProcessXmlTreeNode(treeDescriptionTopNode, null, 0, ref iIndex);
		}

		/// <summary>
		/// Create LingTreeNode objects based on XML tree description
		/// </summary>
		/// <param name="xmlNode">current xml node in the description</param>
		/// <param name="Node">parent LingTreeNode</param>
		/// <param name="iLevel">level (or depth) of a node within the tree</param>
		/// <param name="iIndex">unique index for nodes in the tree</param>
		protected void ProcessXmlTreeNode(
			XmlNode xmlNode,
			LingTreeNode Node,
			int iLevel,
			ref int iIndex
		)
		{
			Node = BeginASubTree(Node, iLevel++, iIndex++);
			if (m_Root == null)
				m_Root = Node;
			XmlNode attr;
			attr = xmlNode.SelectSingleNode("@type");
			if (attr != null)
			{
				switch (attr.InnerText)
				{
					case "lex":
						Node.Type = LingTreeNode.NodeType.Lex;
						break;
					case "gloss":
						Node.Type = LingTreeNode.NodeType.Gloss;
						break;
					default:
						Node.Type = LingTreeNode.NodeType.NonTerminal;
						break;
				}
			}
			attr = xmlNode.SelectSingleNode("@id");
			if (attr != null)
			{
				Node.Id = attr.InnerText;
			}
			attr = xmlNode.SelectSingleNode("@triangleover");
			if (attr != null)
			{
				if (attr.InnerText == "yes")
					Node.Triangle = true;
			}
			attr = xmlNode.SelectSingleNode("@omitlineover");
			if (attr != null)
			{
				if (attr.InnerText == "yes")
					Node.OmitLine = true;
			}

			ProcessNodeContentFromXml(xmlNode, Node);

			XmlNodeList daughterNodes = xmlNode.SelectNodes("node");
			foreach (XmlNode daughterNode in daughterNodes)
			{
				ProcessXmlTreeNode(daughterNode, Node, iLevel, ref iIndex);
			}
			Node = EndASubTree(Node);
			iLevel--; // decrement level
		}

		private void ProcessNodeContentFromXml(XmlNode xmlNode, LingTreeNode Node)
		{
			XmlNode xmlTemp = xmlNode.SelectSingleNode("label");
			if (xmlTemp != null)
				Node.Content = xmlTemp.InnerXml;
			AppendXScript(xmlNode, Node, "superscript");
			AppendXScript(xmlNode, Node, "subscript");
		}

		private void AppendXScript(XmlNode xmlNode, LingTreeNode Node, string sType)
		{
			XmlNode element = xmlNode.SelectSingleNode(sType);
			if (element != null)
			{
				string sText;
				XmlNode attr = xmlNode.SelectSingleNode(sType + "/@italic");
				switch (sType)
				{
					case "superscript":
						if (attr != null && attr.InnerText == "yes")
							sText = "/^";
						else
							sText = "/S";
						break;
					case "subscript":
						if (attr != null && attr.InnerText == "yes")
							sText = "/_";
						else
							sText = "/s";
						break;
					default:
						sText = "";
						break;
				}
				Node.Content = Node.Content + sText + element.InnerXml;
			}
		}

		/// <summary>
		/// Gets/sets Version.
		/// </summary>
		public string Version
		{
			get { return m_sVersion; }
			set { m_sVersion = value; }
		}

		/// <summary>
		/// Gets/sets Description.
		/// </summary>
		public string Description
		{
			get { return m_sDescription; }
			set { m_sDescription = value; }
		}

		/// <summary>
		/// Gets/set Root.
		/// </summary>
		[XmlIgnore]
		public LingTreeNode Root
		{
			get { return m_Root; }
			set { m_Root = value; }
		}

		/// <summary>
		/// Gets/sets VerticalGap.
		/// </summary>
		public int VerticalGap
		{
			get { return m_iVerticalGap; }
			set { m_iVerticalGap = value; }
		}

		/// <summary>
		/// Gets/sets HorizontalGap.
		/// </summary>
		public int HorizontalGap
		{
			get { return m_iHorizontalGap; }
			set { m_iHorizontalGap = value; }
		}

		/// <summary>
		/// Gets/sets InitialXCoord.
		/// </summary>
		public int InitialXCoord
		{
			get { return m_iInitialXCoord; }
			set { m_iInitialXCoord = value; }
		}

		/// <summary>
		/// Gets/sets InitialYCoord.
		/// </summary>
		public int InitialYCoord
		{
			get { return m_iInitialYCoord; }
			set { m_iInitialYCoord = value; }
		}

		/// <summary>
		/// Gets/sets HorizontalOffset.
		/// </summary>
		public int HorizontalOffset
		{
			get { return m_iHorizontalOffset; }
			set { m_iHorizontalOffset = value; }
		}

		/// <summary>
		/// Gets/sets XSize.
		/// </summary>
		[XmlIgnore]
		public int XSize
		{
			get { return m_iXSize; }
			set { m_iXSize = value; }
		}

		/// <summary>
		/// Gets/sets YSize.
		/// </summary>
		[XmlIgnore]
		public int YSize
		{
			get { return m_iYSize; }
			set { m_iYSize = value; }
		}

		/// <summary>
		/// Gets/sets GlossBottomYCoord.
		/// </summary>
		[XmlIgnore]
		public int GlossBottomYCoord
		{
			get { return m_iGlossBottomYCoord; }
			set { m_iGlossBottomYCoord = value; }
		}

		/// <summary>
		/// Gets/sets LexBottomYCoord.
		/// </summary>
		[XmlIgnore]
		public int LexBottomYCoord
		{
			get { return m_iLexBottomYCoord; }
			set { m_iLexBottomYCoord = value; }
		}

		/// <summary>
		/// Gets/sets LexBottomYUpperMid.
		/// </summary>
		[XmlIgnore]
		public int LexBottomYUpperMid
		{
			get { return m_iLexBottomYUpperMid; }
			set { m_iLexBottomYUpperMid = value; }
		}

		/// <summary>
		/// Gets/sets LexGlossGapAdjustment.
		/// </summary>
		public int LexGlossGapAdjustment
		{
			get { return m_iLexGlossGapAdjustment; }
			set { m_iLexGlossGapAdjustment = value; }
		}

		/// <summary>
		/// Gets/sets UseRightToLeft.
		/// </summary>
		public bool UseRightToLeft
		{
			get { return m_bUseRightToLeft; }
			set { m_bUseRightToLeft = value; }
		}

		/// <summary>
		/// Gets/sets TrySmoothing.
		/// </summary>
		public bool TrySmoothing
		{
			get { return m_bTrySmoothing; }
			set { m_bTrySmoothing = value; }
		}

		/// <summary>
		/// Gets/sets TryPixelOffset.
		/// </summary>
		public bool TryPixelOffset
		{
			get { return m_bTryPixelOffset; }
			set { m_bTryPixelOffset = value; }
		}

		/// <summary>
		/// Gets/sets ShowFlatView.
		/// </summary>
		public bool ShowFlatView
		{
			get { return m_bShowFlatView; }
			set { m_bShowFlatView = value; }
		}

		/// <summary>
		/// Gets/sets Gloss Font.
		/// </summary>
		[XmlIgnore]
		public Font GlossFont
		{
			get { return m_GlossFont; }
			set
			{
				m_GlossFont = value;
				GlossFontFace = m_GlossFont.Name;
				GlossFontSize = m_GlossFont.Size;
				GlossFontStyle = m_GlossFont.Style;
			}
		}

		/// <summary>
		/// Gets/sets Gloss Font Face. (Do not use this; use GlossFont instead.)
		/// </summary>
		/// For XML Serialization
		public string GlossFontFace
		{
			get { return m_sGlossFontFace; }
			set { m_sGlossFontFace = value; }
		}

		/// <summary>
		/// Gets/sets Gloss Font Size. (Do not use this; use GlossFont instead.)
		/// </summary>
		/// For XML Serialization
		public float GlossFontSize
		{
			get { return m_fGlossFontSize; }
			set { m_fGlossFontSize = value; }
		}

		/// <summary>
		/// Gets/sets Gloss Font Style (Do not use this; use GlossFont instead.)
		/// </summary>
		/// For XML Serialization
		public FontStyle GlossFontStyle
		{
			get { return m_GlossFontStyle; }
			set { m_GlossFontStyle = value; }
		}

		/// <summary>
		/// Gets/sets Lexical Item Font.
		/// </summary>
		[XmlIgnore]
		public Font LexFont
		{
			get { return m_LexFont; }
			set
			{
				m_LexFont = value;
				LexFontFace = m_LexFont.Name;
				LexFontSize = m_LexFont.Size;
				LexFontStyle = m_LexFont.Style;
			}
		}

		/// <summary>
		/// Gets/sets Lex Font Face. (Do not use this; use LexFont instead.)
		/// </summary>
		/// For XML Serialization
		public string LexFontFace
		{
			get { return m_sLexFontFace; }
			set { m_sLexFontFace = value; }
		}

		/// <summary>
		/// Gets/sets Lex Font Size. (Do not use this; use LexFont instead.)
		/// </summary>
		/// For XML Serialization
		public float LexFontSize
		{
			get { return m_fLexFontSize; }
			set { m_fLexFontSize = value; }
		}

		/// <summary>
		/// Gets/sets Lex Font Style (Do not use this; use LexFont instead.)
		/// </summary>
		/// For XML Serialization
		public FontStyle LexFontStyle
		{
			get { return m_LexFontStyle; }
			set { m_LexFontStyle = value; }
		}

		/// <summary>
		/// Gets/sets NonTerminal Font.
		/// </summary>
		[XmlIgnore]
		public Font NTFont
		{
			get { return m_NTFont; }
			set
			{
				m_NTFont = value;
				NTFontFace = m_NTFont.Name;
				NTFontSize = m_NTFont.Size;
				NTFontStyle = m_NTFont.Style;
			}
		}

		/// <summary>
		/// Gets/sets NT Font Face. (Do not use this; use NTFont instead.)
		/// </summary>
		/// For XML Serialization
		public string NTFontFace
		{
			get { return m_sNTFontFace; }
			set { m_sNTFontFace = value; }
		}

		/// <summary>
		/// Gets/sets NT Font Size. (Do not use this; use NTFont instead.)
		/// </summary>
		/// For XML Serialization
		public float NTFontSize
		{
			get { return m_fNTFontSize; }
			set { m_fNTFontSize = value; }
		}

		/// <summary>
		/// Gets/sets NT Font Style (Do not use this; use NTFont instead.)
		/// </summary>
		/// For XML Serialization
		public FontStyle NTFontStyle
		{
			get { return m_NTFontStyle; }
			set { m_NTFontStyle = value; }
		}

		/// <summary>
		/// Gets/sets Gloss Color.
		/// </summary>
		[XmlIgnore]
		public Color GlossColor
		{
			get { return m_GlossColor; }
			set
			{
				m_GlossColor = value;
				m_iGlossColorArgb = m_GlossColor.ToArgb();
			}
		}

		/// <summary>
		/// Gets/sets Gloss Color ARGB value.  (Do not use this; use GlossColor instead.)
		/// </summary>
		/// For XML Serialization
		public int GlossColorArgb
		{
			get { return m_iGlossColorArgb; }
			set
			{
				m_iGlossColorArgb = value;
				m_GlossColor = Color.FromArgb(m_iGlossColorArgb);
			}
		}

		/// <summary>
		/// Gets/sets Lexical Item Color.
		/// </summary>
		[XmlIgnore]
		public Color LexColor
		{
			get { return m_LexColor; }
			set
			{
				m_LexColor = value;
				m_iLexColorArgb = m_LexColor.ToArgb();
			}
		}

		/// <summary>
		/// Gets/sets Lex Color ARGB value.  (Do not use this; use LexColor instead.)
		/// </summary>
		/// For XML Serialization
		public int LexColorArgb
		{
			get { return m_iLexColorArgb; }
			set
			{
				m_iLexColorArgb = value;
				m_LexColor = Color.FromArgb(m_iLexColorArgb);
			}
		}

		/// <summary>
		/// Gets/sets NonTerminal Color.
		/// </summary>
		[XmlIgnore]
		public Color NTColor
		{
			get { return m_NTColor; }
			set
			{
				m_NTColor = value;
				m_iNTColorArgb = m_NTColor.ToArgb();
			}
		}

		/// <summary>
		/// Gets/sets NT Color ARGB value.  (Do not use this; use NTColor instead.)
		/// </summary>
		/// For XML Serialization
		public int NTColorArgb
		{
			get { return m_iNTColorArgb; }
			set
			{
				m_iNTColorArgb = value;
				m_NTColor = Color.FromArgb(m_iNTColorArgb);
			}
		}

		/// <summary>
		/// Gets/sets Color of lines in trees.
		/// </summary>
		[XmlIgnore]
		public Color LinesColor
		{
			get { return m_LinesColor; }
			set
			{
				m_LinesColor = value;
				m_iLinesColorArgb = m_LinesColor.ToArgb();
			}
		}

		/// <summary>
		/// Gets/sets Lines Color ARGB value.  (Do not use this; use LinesColor instead.)
		/// </summary>
		/// For XML Serialization
		public int LinesColorArgb
		{
			get { return m_iLinesColorArgb; }
			set
			{
				m_iLinesColorArgb = value;
				m_LinesColor = Color.FromArgb(m_iLinesColorArgb);
			}
		}

		/// <summary>
		/// Gets/sets Color of Background in trees.
		/// </summary>
		[XmlIgnore]
		public Color BackgroundColor
		{
			get { return m_BackgroundColor; }
			set
			{
				m_BackgroundColor = value;
				m_iBackgroundColorArgb = m_BackgroundColor.ToArgb();
			}
		}

		/// <summary>
		/// Gets/sets Background Color ARGB value.  (Do not use this; use BackgroundColor instead.)
		/// </summary>
		/// For XML Serialization
		public int BackgroundColorArgb
		{
			get { return m_iBackgroundColorArgb; }
			set
			{
				m_iBackgroundColorArgb = value;
				m_BackgroundColor = Color.FromArgb(m_iBackgroundColorArgb);
			}
		}

		/// <summary>
		/// Gets/sets Line width.
		/// </summary>
		public double LineWidth
		{
			get { return m_dLineWidth; }
			set { m_dLineWidth = value; }
		}

		/// <summary>
		/// Gets/sets custom colors.
		/// </summary>
		public int[] CustomColors
		{
			get { return m_aiCustomColors; }
			set { m_aiCustomColors = value; }
		}

		/// <summary>
		/// Gets/sets message text that will show in upper left corner of tree.
		/// </summary>
		[XmlIgnore]
		public string MessageText
		{
			get { return m_sMessageText; }
			set { m_sMessageText = value; }
		}

		public int GetMaxLevelHeight(int iLevel)
		{
			return m_aiMaxLevelHeights[iLevel];
		}

		public void SetMaxLevelHeight(int iLevel, int iHeight)
		{
			m_aiMaxLevelHeights[iLevel] = iHeight;
		}

		public void setFontsFromXml()
		{
			NTFont = new Font(m_sNTFontFace, m_fNTFontSize, m_NTFontStyle);
			//NTColor = Color.FromArgb(NTColorArgb);
			GlossFont = new Font(m_sGlossFontFace, m_fGlossFontSize, m_GlossFontStyle);
			//GlossColor = Color.FromArgb(GlossColorArgb);
			LexFont = new Font(m_sLexFontFace, m_fLexFontSize, m_LexFontStyle);
			//LexColor = Color.FromArgb(LexColorArgb);
			//LinesColor= Color.FromArgb(LinesColorArgb);
		}

		public void SetTreeParameters(XmlDocument doc)
		{
			VerticalGap = GetXmlParameterAsInt(doc, "Layout/VerticalGap");
			HorizontalGap = GetXmlParameterAsInt(doc, "Layout/HorizontalGap");
			InitialXCoord = GetXmlParameterAsInt(doc, "Layout/InitialXCoord");
			InitialYCoord = GetXmlParameterAsInt(doc, "Layout/InitialYCoord");
			HorizontalOffset = GetXmlParameterAsInt(doc, "Layout/HorizontalOffset");
			LexGlossGapAdjustment = GetXmlParameterAsInt(doc, "Layout/LexGlossGapAdjustment");
			ShowFlatView = GetXmlParameterAsBoolean(doc, "Layout/ShowFlatView");

			GlossFontFace = GetXmlParameter(doc, "Fonts/Gloss/GlossFontFace");
			GlossFontSize = GetXmlParameterAsFloat(doc, "Fonts/Gloss/GlossFontSize");
			GlossFontStyle = GetXmlParameterAsFontStyle(doc, "Fonts/Gloss/GlossFontStyle");
			SetColorFromXmlParameter(
				doc,
				"Fonts/Gloss/GlossColorName",
				"Fonts/Gloss/GlossColorArgb",
				out m_GlossColor,
				out m_iGlossColorArgb
			);

			LexFontFace = GetXmlParameter(doc, "Fonts/Lex/LexFontFace");
			LexFontSize = GetXmlParameterAsFloat(doc, "Fonts/Lex/LexFontSize");
			LexFontStyle = GetXmlParameterAsFontStyle(doc, "Fonts/Lex/LexFontStyle");
			SetColorFromXmlParameter(
				doc,
				"Fonts/Lex/LexColorName",
				"Fonts/Lex/LexColorArgb",
				out m_LexColor,
				out m_iLexColorArgb
			);

			NTFontFace = GetXmlParameter(doc, "Fonts/NT/NTFontFace");
			NTFontSize = GetXmlParameterAsFloat(doc, "Fonts/NT/NTFontSize");
			NTFontStyle = GetXmlParameterAsFontStyle(doc, "Fonts/NT/NTFontStyle");
			SetColorFromXmlParameter(
				doc,
				"Fonts/NT/NTColorName",
				"Fonts/NT/NTColorArgb",
				out m_NTColor,
				out m_iNTColorArgb
			);

			LineWidth = GetXmlParameterAsDouble(doc, "Fonts/Lines/LineWidth");
			SetColorFromXmlParameter(
				doc,
				"Fonts/Lines/LinesColorName",
				"Fonts/Lines/LinesColorArgb",
				out m_LinesColor,
				out m_iLinesColorArgb
			);

			SetColorFromXmlParameter(
				doc,
				"Fonts/Background/BackgroundColorName",
				"Fonts/Background/BackgroundColorArgb",
				out m_BackgroundColor,
				out m_iBackgroundColorArgb
			);

			setFontsFromXml();
		}

		private void SetColorFromXmlParameter(
			XmlDocument doc,
			string sNameParameter,
			string sArgbParameter,
			out Color color,
			out int iColorArgb
		)
		{
			string sValue = GetXmlParameter(doc, sNameParameter);
			color = Color.FromName(sValue);
			if (color.IsKnownColor)
				iColorArgb = color.ToArgb();
			else
				iColorArgb = GetXmlParameterAsInt(doc, sArgbParameter);
		}

		private bool GetXmlParameterAsBoolean(XmlDocument doc, string sParameter)
		{
			bool fResult = false;
			string sValue = GetXmlParameter(doc, sParameter);
			if (sValue.ToLower() == "true")
				fResult = true;
			return fResult;
		}

		private double GetXmlParameterAsDouble(XmlDocument doc, string sParameter)
		{
			string sValue = GetXmlParameter(doc, sParameter);
			return Convert.ToDouble(sValue);
		}

		private float GetXmlParameterAsFloat(XmlDocument doc, string sParameter)
		{
			string sValue = GetXmlParameter(doc, sParameter);
			return Convert.ToSingle(sValue);
		}

		private FontStyle GetXmlParameterAsFontStyle(XmlDocument doc, string sParameter)
		{
			FontStyle result;
			string sValue = GetXmlParameter(doc, sParameter);
			switch (sValue.ToLower())
			{
				case "bold":
					result = FontStyle.Bold;
					break;
				case "italic":
					result = FontStyle.Italic;
					break;
				case "bolditalic":
					result = FontStyle.Bold | FontStyle.Italic;
					break;
				case "regular":
					result = FontStyle.Regular;
					break;
				case "underline":
					result = FontStyle.Underline;
					break;
				case "strikeout":
					result = FontStyle.Strikeout;
					break;
				default:
					result = FontStyle.Regular;
					break;
			}
			return result;
		}

		private int GetXmlParameterAsInt(XmlDocument doc, string sParameter)
		{
			string sValue = GetXmlParameter(doc, sParameter);
			return Convert.ToInt32(sValue);
		}

		private string GetXmlParameter(XmlDocument doc, string sParameter)
		{
			string sValue;
			XmlNode node = doc.SelectSingleNode("/LingTree/Parameters/" + sParameter);
			if (node != null)
				sValue = node.InnerText;
			else
				sValue = "";
			return sValue;
		}

		private void LingTreeTree_MouseUp(object sender, MouseEventArgs e)
		{
			Graphics grfx = this.CreateGraphics();
			grfx.PageUnit = GraphicsUnit.Millimeter;
			grfx.PageScale = .01f;
			Point[] atpt = { new Point(e.X, e.Y) };
			grfx.TransformPoints(CoordinateSpace.Page, CoordinateSpace.Device, atpt);
			int iClickX = atpt[0].X;
			int iClickY = atpt[0].Y;
			foreach (LingTreeNode node in this.Controls)
			{
				if (iClickX >= node.XCoord && iClickX <= (node.XCoord + node.Width))
					if (iClickY >= node.YCoord && iClickY <= (node.YCoord + node.Height))
					{
						node.BackColor = SystemColors.Highlight;
						LingTreeNodeClickedEventArgs ltne = new LingTreeNodeClickedEventArgs(node);
						if (this.m_LingTreeNodeClickedEvent != null)
							m_LingTreeNodeClickedEvent(this, ltne);
					}
					else
						node.BackColor = BackColor;
				else
					node.BackColor = BackColor;
			}
			Invalidate();
		}
	}
}
