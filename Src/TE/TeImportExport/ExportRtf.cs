// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ExportRtf.cs
// Responsibility: TeTeam

using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	#region RtfStyle Class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to represent an RTF style
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RtfStyle : BaseStyleInfo
	{
		#region Data members
		/// <summary>Table of font names, indexed by writing system ID</summary>
		private int m_defaultWs;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:RtfStyle"/> class.
		/// NOTE: This constructor should only be used in tests! All other RtfStyles should be
		/// based on a real style in the DB.
		/// </summary>
		/// <param name="defaultWs">The default ws.</param>
		/// ------------------------------------------------------------------------------------
		protected RtfStyle(int defaultWs) : base()
		{
			m_defaultWs = defaultWs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:RtfStyle"/> class based on a FW
		/// style.
		/// </summary>
		/// <param name="style">An StStyle.</param>
		/// <param name="defaultWs">HVO of the default Writing System</param>
		/// ------------------------------------------------------------------------------------
		public RtfStyle(IStStyle style, int defaultWs)
			: base(style)
		{
			m_defaultWs = defaultWs;
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Format the style information as an RTF style string with composed Unicode characters.
		/// </summary>
		/// <param name="styleName">name of the style to export</param>
		/// <param name="styleTable">true to write data for the RTF style table, false
		/// for a usage instance in data</param>
		/// ------------------------------------------------------------------------------------
		public string ToString(string styleName, bool styleTable)
		{
			return TsStringUtils.Compose(ToString(styleName, styleTable, m_defaultWs));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Format the style information as an RTF style string with composed Unicode characters.
		/// </summary>
		/// <param name="styleName">name of the style to export</param>
		/// <param name="styleTable"><c>true</c> to write data for the RTF style table,
		/// <c>false</c> for a usage instance in data</param>
		/// <param name="ws">The writing system to use in case WS-specific overrides exist
		/// for the font information for the requested style</param>
		/// ------------------------------------------------------------------------------------
		public string ToString(string styleName, bool styleTable, int ws)
		{
			if (IsParagraphStyle)
				return TsStringUtils.Compose(ParaStyleToString(styleName, styleTable, ws));
			else
				return TsStringUtils.Compose(CharStyleToString(styleName, styleTable, ws));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all colors associated with this RTF style entry.
		/// </summary>
		/// <returns>Enumerator to a unique list of colors</returns>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<Color> GetAllColors()
		{
			List<Color> colors = new List<Color>();
			AddFontInfoColors(colors, m_defaultFontInfo);
			if (m_fontInfoOverrides != null)
			{
				foreach (FontInfo fontInfo in m_fontInfoOverrides.Values)
					AddFontInfoColors(colors, fontInfo);
			}
			if (!m_borderColor.IsInherited && !colors.Contains(m_borderColor.Value))
				colors.Add(m_borderColor.Value);
			return colors;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all explicit font names associated with this RTF style entry.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<string> GetAllExplicitFontNames()
		{
			List<string> fontNames = new List<string>();
			AddFontInfoName(fontNames, m_defaultFontInfo);
			if (m_fontInfoOverrides != null)
			{
				foreach (FontInfo fontInfo in m_fontInfoOverrides.Values)
					AddFontInfoName(fontNames, fontInfo);
			}
			return fontNames;
		}
		#endregion

		#region private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the font info colors.
		/// </summary>
		/// <param name="colors">The list of colors (RGB values).</param>
		/// <param name="fontInfo">The font info.</param>
		/// ------------------------------------------------------------------------------------
		private void AddFontInfoColors(List<Color> colors, FontInfo fontInfo)
		{
			if (!fontInfo.m_fontColor.IsInherited && !colors.Contains(fontInfo.m_fontColor.Value))
				colors.Add(fontInfo.m_fontColor.Value);
			if (!fontInfo.m_backColor.IsInherited && !colors.Contains(fontInfo.m_backColor.Value))
				colors.Add(fontInfo.m_backColor.Value);
			if (!fontInfo.m_underlineColor.IsInherited && !colors.Contains(fontInfo.m_underlineColor.Value))
				colors.Add(fontInfo.m_underlineColor.Value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the font name to the list if it is a real font name (not magic).
		/// </summary>
		/// <param name="fontNames">The list of font names.</param>
		/// <param name="fontInfo">The font info.</param>
		/// ------------------------------------------------------------------------------------
		private void AddFontInfoName(List<string> fontNames, FontInfo fontInfo)
		{
			if (fontInfo.m_fontName.IsInherited)
				return;
			string fontName = fontInfo.m_fontName.Value;
			if (!fontNames.Contains(fontName) && !StyleServices.IsMagicFontName(fontName))
				fontNames.Add(fontName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The base class stores all measurement values in millipoints (72000 mp = 1 inch).
		/// RTF requires units to be twips (20th of a point).
		/// </summary>
		/// <param name="mpValue">Measurement value in millipoints</param>
		/// ------------------------------------------------------------------------------------
		private static int ConvertMillipointsToTwips(int mpValue)
		{
			return mpValue / 50;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a font size tag for a value given in millipoints
		/// </summary>
		/// <remarks>
		/// The base class stores all measurement values in millipoints (72000 mp = 1 inch).
		/// RTF requires font units to be double the size in points (this allows for half-point
		/// sizes).
		/// </remarks>
		/// <param name="mpValue">Measurement value in millipoints</param>
		/// ------------------------------------------------------------------------------------
		private string FontSizeTag(int mpValue)
		{
			return IntegerWithTag(mpValue /500, @"\fs");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the superscript/subscript setting into a string representation
		/// </summary>
		/// <param name="fontInfo">The font info.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string SuperSubString(FontInfo fontInfo)
		{
			if (fontInfo.m_superSub.ValueIsSet)
			{
				switch (fontInfo.m_superSub.Value)
				{
					case FwSuperscriptVal.kssvOff:
						if (fontInfo.m_superSub.IsInherited)
							return string.Empty;
						else
							return @"\nosupersub";
					case FwSuperscriptVal.kssvSuper: return @"\super";
					case FwSuperscriptVal.kssvSub: return @"\sub";
				}
			}
			return string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Format a paragraph style as an RTF style string
		/// </summary>
		/// <param name="styleName">Name of the style.</param>
		/// <param name="styleTable"><c>true</c> to write data for the RTF style table,
		/// <c>false</c> for a usage instance in data</param>
		/// <param name="ws">The writing system to use in case WS-specific overrides exist
		/// for the font information for the requested style</param>
		/// ------------------------------------------------------------------------------------
		private string ParaStyleToString(string styleName, bool styleTable, int ws)
		{
			string directionString = string.Empty;
			if (DirectionIsRightToLeft == TriStateBool.triTrue)
				directionString = @"\rtlpar";
			else if (!m_rtl.IsInherited && DirectionIsRightToLeft == TriStateBool.triFalse)
				directionString = @"\ltrpar";

			string alignmentString = string.Empty;
			if (!m_alignment.IsInherited ||
				m_alignment.Value != FwTextAlign.ktalLeading ||
				DirectionIsRightToLeft == TriStateBool.triTrue)
			{
				switch (m_alignment.Value)
				{
					case FwTextAlign.ktalLeft:
						alignmentString = @"\ql";
						break;
					case FwTextAlign.ktalCenter:
						alignmentString = @"\qc";
						break;
					case FwTextAlign.ktalTrailing:
						alignmentString = (DirectionIsRightToLeft == TriStateBool.triTrue) ? @"\ql" : @"\qr";
						break;
					case FwTextAlign.ktalLeading:
						alignmentString = (DirectionIsRightToLeft == TriStateBool.triTrue) ? @"\qr" : @"\ql";
						break;
					case FwTextAlign.ktalRight:
						alignmentString = @"\qr";
						break;
					case FwTextAlign.ktalJustify:
						alignmentString = @"\qj";
						break;
				}
			}

			FontInfo fontInfo = FontInfoForWs(ws);

			string colorString = (fontInfo.m_fontColor.Value != Color.Black) ?
				@"\cf" + OwningTable.LookupColorIndex(fontInfo.m_fontColor.Value) : string.Empty;

			// If a negative first line indent was given then it means "hanging" indent. To work
			// properly, it needs to be compensated for in the left indent.
			int mpLeadingIndent = m_leadingIndent.Value;
			if (m_firstLineIndent.Value < 0)
				mpLeadingIndent -= m_firstLineIndent.Value;

			string basePortion = @"\s" + StyleNumber.ToString()
				+ directionString
				+ alignmentString
				+ IntegerWithTag(ConvertMillipointsToTwips(m_firstLineIndent.Value), @"\fi")
				+ IntegerWithTag(ConvertMillipointsToTwips(mpLeadingIndent), @"\lin")
				+ IntegerWithTag(ConvertMillipointsToTwips(m_trailingIndent.Value), @"\rin")
				+ IntegerWithTag(ConvertMillipointsToTwips(m_spaceBefore.Value), @"\sb")
				+ IntegerWithTag(ConvertMillipointsToTwips(m_spaceAfter.Value), @"\sa")
				+ LineSpacingAsString
				+ FontTagForWs(ws)
				+ FontSizeTag(fontInfo.m_fontSize.Value)
				+ colorString
				+ BorderAsString
				+ ((fontInfo.m_bold.Value) ? @"\b" : string.Empty)
				+ ((fontInfo.m_italic.Value) ? @"\i" : string.Empty)
				+ SuperSubString(fontInfo);

			if (styleTable)
			{
				return basePortion
					+ IntegerWithTag(BasedOnStyleNumber, @"\sbasedon")
					+ IntegerWithTag(NextStyleNumber, @"\snext")
					+ " " + ExportRtf.ConvertString(styleName);
			}
			else
				return basePortion;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a string representing the line spacing setting. This setting is optional
		/// and requires an additional tag based on its value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected string LineSpacingAsString
		{
			get
			{
				int height = m_lineSpacing.Value.m_lineHeight;
				// If the line spacing is not specified, then do not put anything into the style
				if (height == 0)
					return string.Empty;

				if (m_lineSpacing.Value.m_relative)
				{
					switch (height)
					{
						case 10000:
							return string.Empty;
						case 15000:
							return @"\sl360\slmult1";
						case 20000:
							return @"\sl480\slmult1";
						default:
							Debug.Assert(false);
							return string.Empty;
					}
				}
				else
				{

					int lineSpacingInTwips = ConvertMillipointsToTwips(height);
					// Negative line spacing is interpreted as "exact" and requires an \slmult0 tag
					// following it.
					if (lineSpacingInTwips < 0)
						return @"\sl" + lineSpacingInTwips + @"\slmult0";
					else
						return @"\sl" + lineSpacingInTwips;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a string representing the border settings. These settings are optional
		/// and require an additional tag based on the values given
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string BorderAsString
		{
			get
			{
				// Assume that there is no border unless proven otherwise
				string borderString = String.Empty;
				BorderThicknesses border = m_border.Value;
				// If any border exists, modify the return string to include it
				if (!(border.Top == 0 && border.Bottom == 0 &&
					border.Leading == 0 && border.Trailing == 0))
				{
					string borderColorString = (m_borderColor.Value == Color.Empty) ? string.Empty : @"\brdrcf" +
						OwningTable.LookupColorIndex(m_borderColor.Value);

					if (border.Top > 0)
					{
						borderString += @"\brdrt\brdrs\brdrw" + ConvertMillipointsToTwips(border.Top)
							+ @"\brsp20" + borderColorString;
					}
					if (border.Bottom > 0)
					{
						borderString += @"\brdrb\brdrs\brdrw" + ConvertMillipointsToTwips(border.Bottom)
							+ @"\brsp20" + borderColorString;
					}
					if (border.Leading > 0)
					{
						borderString += @"\brdrl\brdrs\brdrw" + ConvertMillipointsToTwips(border.Leading)
							+ @"\brsp80" + borderColorString;
					}
					if (border.Trailing > 0)
					{
						borderString += @"\brdrr\brdrs\brdrw" + ConvertMillipointsToTwips(border.Trailing)
							+ @"\brsp80" + borderColorString;
					}
				}

				return borderString;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Formats a character style as an RTF style string
		/// </summary>
		/// <param name="styleName">Name of the style.</param>
		/// <param name="styleTable"><c>true</c> to write data for the RTF style table,
		/// <c>false</c> for a usage instance in data</param>
		/// <param name="ws">The writing system to use in case WS-specific overrides exist
		/// for the font information for the requested style</param>
		/// ------------------------------------------------------------------------------------
		private string CharStyleToString(string styleName, bool styleTable, int ws)
		{
			FontInfo fontInfo = FontInfoForWs(ws);

			string basePortion = @"\*\cs" + StyleNumber
				+ ((fontInfo.m_bold.ValueIsSet && fontInfo.m_bold.Value) ? @"\b" : string.Empty)
				+ ((fontInfo.m_italic.ValueIsSet && fontInfo.m_italic.Value) ? @"\i" : string.Empty)
				+ FontTagForWs(ws)
				+ ((fontInfo.m_fontSize.ValueIsSet) ? FontSizeTag(fontInfo.m_fontSize.Value) : string.Empty)
				+ SuperSubString(fontInfo);

			if (styleTable)
			{
				// REVIEW(DaveE): Why do we output a "Next" style for character styles? Character styles in TE shouldn't ever have this set.
				Debug.Assert(NextStyleNumber == 0);
				return basePortion +
					@"\additive" +
					IntegerWithTag(NextStyleNumber, @"\snext") +
					" " + ExportRtf.ConvertString(styleName);
			}
			else
				return basePortion;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a boolean expression to a string representation
		/// </summary>
		/// <param name="expr">expression to evaluate</param>
		/// <param name="trueString">string for true condition</param>
		/// <param name="falseString">string for false condition</param>
		/// <returns>the true or false string based on the value of the expression</returns>
		/// ------------------------------------------------------------------------------------
		private string ExpressionToString(bool expr, string trueString, string falseString)
		{
			return expr ? trueString : falseString;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a string tag for an integer value if the value is not zero
		/// </summary>
		/// <param name="intValue"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string IntegerWithTag(int intValue, string tag)
		{
			if (intValue == 0)
				return string.Empty;
			return tag + intValue.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the font tag for the given writing system.
		/// </summary>
		/// <param name="ws">The writing system ID</param>
		/// ------------------------------------------------------------------------------------
		private string FontTagForWs(int ws)
		{
			string sFontName = RealFontNameForWs(ws);
			return sFontName == null ? string.Empty :
				IntegerWithTag(OwningTable.LookupFontId(sFontName), @"\f");
		}
		#endregion

		#region private properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the owning RtfStyleInfoTable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private RtfStyleInfoTable OwningTable
		{
			get { return (RtfStyleInfoTable)m_owningTable; }
		}
		#endregion
	}
	#endregion

	#region class RtfStyleInfoTable
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// RtfStyleInfoTable is a Dictionary of style information, indexed by the TE style name
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RtfStyleInfoTable : StyleInfoTable
	{
		#region Member Data
		private FdoCache m_cache;

		// Dictionary of the fonts - indexed by the font name
		private Dictionary<string, int> m_fontTable = new Dictionary<string, int>();
		private int m_nextFontID = 1;

		// Array of colors
		private List<Color> m_colorTable = new List<Color>();

		private bool m_defaultRightToLeft;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:RtfStyleInfoTable"/> class.
		/// </summary>
		/// <param name="cache">The FDO cache.</param>
		/// <param name="defaultRightToLeft">if set to <c>true</c> [default right to left].</param>
		/// ------------------------------------------------------------------------------------
		public RtfStyleInfoTable(FdoCache cache, bool defaultRightToLeft) :
			base( ScrStyleNames.Normal, (cache == null) ? null : cache.ServiceLocator.WritingSystemManager)
		{
			m_cache = cache;
			m_defaultRightToLeft = defaultRightToLeft;

			// Get the fonts from the various writing systems.
			if (m_cache != null) // Can only be null for testing
			{
				foreach (WritingSystem ws in m_cache.ServiceLocator.WritingSystems.AllWritingSystems)
					AddFontName(ws.DefaultFontName);
			}

			// Add black and white to the color table
			AddColorToTable(Color.FromKnownColor(KnownColor.Black));
			AddColorToTable(Color.FromKnownColor(KnownColor.White));
		}
		#endregion

		#region Font table-related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a font name to the font table if it does not yet exist.
		/// </summary>
		/// <param name="fontName">Name of the font</param>
		/// <returns>The id of the newly added font, or 0 if it was not added</returns>
		/// ------------------------------------------------------------------------------------
		private void AddFontName(string fontName)
		{
			if (!string.IsNullOrEmpty(fontName) && fontName[0] != '<' && !m_fontTable.ContainsKey(fontName))
			{
				m_fontTable.Add(fontName, m_nextFontID++);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lookup a font name and get its ID in the RTF style table.
		/// </summary>
		/// <param name="fontName">Name of the font to look up</param>
		/// <returns>The ID of the font</returns>
		/// ------------------------------------------------------------------------------------
		public int LookupFontId(string fontName)
		{
			return m_fontTable[fontName];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export the font table
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportFontTable(TextWriter writer)
		{
			writer.WriteLine(@"{\fonttbl");

			foreach (KeyValuePair<string, int> fontEntry in m_fontTable)
				writer.WriteLine(@"{\f" + fontEntry.Value + @"\fnil\fprq2 " + fontEntry.Key + @";}");

			writer.WriteLine(@"}");
		}
		#endregion

		#region Color table-related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a color to the color table
		/// </summary>
		/// <param name="color">Color to add to the table</param>
		/// <returns>The index of the newly added color</returns>
		/// ------------------------------------------------------------------------------------
		private int AddColorToTable(Color color)
		{
			m_colorTable.Add(color);
			return m_colorTable.Count - 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks up the index of the color.
		/// </summary>
		/// <param name="color">Color to look up</param>
		/// <returns>The index of the color</returns>
		/// ------------------------------------------------------------------------------------
		public int LookupColorIndex(Color color)
		{
			for (int i = 0; i < m_colorTable.Count; i++)
				if (m_colorTable[i] == color)
					return i + 1; // color indices are 1-based and List entries are 0-based
			throw new ArgumentException("Failed to look up index for unexpected color in RTF export", "color");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export the color table
		/// </summary>
		/// <param name="writer">The stream writer.</param>
		/// ------------------------------------------------------------------------------------
		private void ExportColorTable(TextWriter writer)
		{
			writer.WriteLine(@"{\colortbl;");
			foreach (Color colorEntry in m_colorTable)
			{
				writer.WriteLine(@"\red" + colorEntry.R +
					@"\green" + colorEntry.G +
					@"\blue" + colorEntry.B + ";");
			}
			writer.WriteLine(@"}");
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the underyling right-to-left value based on the context.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override TriStateBool DefaultRightToLeft
		{
			get { return m_defaultRightToLeft ? TriStateBool.triTrue : TriStateBool.triFalse; }
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the given RTF style entry to the table.
		/// </summary>
		/// <param name="key">The TE Stylename corresponding to this entry</param>
		/// <param name="value">The value of the element to add (must not be null)</param>
		/// <exception cref="T:System.ArgumentException">An element with the same key already
		/// exists in the <see cref="T:System.Collections.Generic.Dictionary`2"></see>.</exception>
		/// <exception cref="T:System.ArgumentNullException">key or value is null.</exception>
		/// ------------------------------------------------------------------------------------
		public void Add(string key, RtfStyle value)
		{
			base.Add(key, value);
			foreach (string fontName in value.GetAllExplicitFontNames())
			{
				if (!m_fontTable.ContainsKey(fontName))
					AddFontName(fontName);
			}

			foreach (Color color in value.GetAllColors())
			{
				if (!m_colorTable.Contains(color))
					AddColorToTable(color);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exports the style table and supporting tables using the specified writer.
		/// </summary>
		/// <param name="writer">The stream writer.</param>
		/// ------------------------------------------------------------------------------------
		internal void Export(TextWriter writer)
		{
			ConnectStyles();
			ExportFontTable(writer);
			ExportColorTable(writer);
			ExportStyleTable(writer);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export the style table
		/// </summary>
		/// <param name="writer">The stream writer.</param>
		/// ------------------------------------------------------------------------------------
		private void ExportStyleTable(TextWriter writer)
		{
			writer.WriteLine(@"{\stylesheet");

			// output the paragraph styles
			foreach (string styleName in Keys)
			{
				RtfStyle style = (RtfStyle)this[styleName];
				if (style.IsParagraphStyle)
					writer.WriteLine("{" + style.ToString(styleName, true) + ";}");
			}

			// output the character styles
			foreach (string styleName in Keys)
			{
				RtfStyle style = (RtfStyle)this[styleName];
				if (style.IsCharacterStyle)
					writer.WriteLine("{" + style.ToString(styleName, true) + ";}");
			}

			writer.WriteLine(@"}");
		}
	}
	#endregion

	#region class KeyTermDictionary
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A dictionary containing a key term name and a list of active references for that term.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class KeyTermDictionary : SortedDictionary<string, List<IChkRef>>
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="KeyTermDictionary"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public KeyTermDictionary()
			: base(StringComparer.InvariantCulture)
		{
		}


	}

	#endregion

	#region ExportRtf class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides export of data to an RTF format file
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ExportRtf
	{
		#region Member data
		/// <summary></summary>
		protected TextWriter m_writer;
		private FdoCache m_cache;
		private IApp m_app;
		private FilteredScrBooks m_bookFilter;
		private IScripture m_scr;
		private FwStyleSheet m_styleSheet;
		private string m_fileName;
		private bool m_firstParagraph = true;
		private bool m_defaultWsIsRTL;
		private ExportContent m_content;
		private int m_btWS;
		private IThreadedProgress m_progressDlg;
		private IScrFootnoteRepository m_footnoteRepo;
		private ICmPictureRepository m_pictureRepo;

		/// <summary>Table of style information - indexed by the style name</summary>
		protected RtfStyleInfoTable m_rtfStyleTable;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export the requested content to an RTF file
		/// </summary>
		/// <param name="fileName">output file name for the RTF exported data</param>
		/// <param name="cache">cache to read from</param>
		/// <param name="filter">book filter to determine which books to export</param>
		/// <param name="content">Determines the content to export</param>
		/// <param name="ws">writing system for the desired back translation</param>
		/// <param name="styleSheet">style sheet to get style information from</param>
		/// <param name="app">The application</param>
		public ExportRtf(string fileName, FdoCache cache, FilteredScrBooks filter,
			ExportContent content, int ws, FwStyleSheet styleSheet, IApp app)
		{
			m_fileName = fileName;
			m_cache = cache;
			m_app = app;
			m_bookFilter = filter;
			m_content = content;
			m_scr = cache.LangProject.TranslatedScriptureOA;
			m_btWS = ws;
			m_styleSheet = styleSheet;
			m_footnoteRepo = m_cache.ServiceLocator.GetInstance<IScrFootnoteRepository>();
			m_pictureRepo = m_cache.ServiceLocator.GetInstance<ICmPictureRepository>();
		}
		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Run the export
		/// </summary>
		/// <returns><c>true</c>if export ran without throwing an exception; otherwise <c>false</c>
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool Run(Form dialogOwner)
		{
			// Check whether we're about to overwrite an existing file.
			if (FileUtils.IsFileReadableAndWritable(m_fileName))
			{
				string sFmt = DlgResources.ResourceString("kstidAlreadyExists");
				string sMsg = String.Format(sFmt, m_fileName);
				string sCaption = m_app.ApplicationName;
				if (MessageBoxUtils.Show(dialogOwner, sMsg, sCaption, MessageBoxButtons.YesNo,
					MessageBoxIcon.Warning) == DialogResult.No)
				{
					return false;
				}
			}
			try
			{
				m_writer = FileUtils.OpenFileForWrite(m_fileName, Encoding.ASCII);
			}
			catch (Exception e)
			{
				MessageBoxUtils.Show(dialogOwner, e.Message, m_app.ApplicationName,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
				return false;
			}

			try
			{
				if (m_content == ExportContent.KeyTermRenderings)
					ExportKeyTerms(dialogOwner);
				else
					ExportTE(dialogOwner);
			}
			catch (Exception e)
			{
				Exception inner = e.InnerException ?? e;
				if (inner is IOException)
				{
					MessageBoxUtils.Show(dialogOwner, inner.Message, m_app.ApplicationName,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
					return false;
				}
				else
					throw;
			}
			finally
			{
				if (m_writer != null)
				{
					try
					{
						m_writer.Close();
					}
					catch
					{
						// ignore errors on close
					}
				}
				m_writer = null;
			}

			return true;
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export key terms information
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportKeyTerms(Form dialogOwner)
		{
			using (var progressDlg = new ProgressDialogWithTask(dialogOwner))
			{
				progressDlg.Title = DlgResources.ResourceString("kstidExportRtfKeyTerms");
				progressDlg.Minimum = 1;
				progressDlg.Maximum = m_cache.LangProject.KeyTermsList.PossibilitiesOS.Count;
				progressDlg.Position = 1;
				progressDlg.AllowCancel = true;

				progressDlg.RunTask(ExportKeyTerms);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exports the key terms.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns>Always null.</returns>
		/// ------------------------------------------------------------------------------------
		private object ExportKeyTerms(IThreadedProgress progressDlg, params object[] parameters)
		{
			m_progressDlg = progressDlg;
			ExportHeader();
			ExportKTFontTable();
			ExportKTStyleTable();
			ExportKeyTermsData();
			ExportFooter();
			m_progressDlg = null;

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export all of the data related to TE.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportTE(Form dialogOwner)
		{
			// count up the number of sections in the filtered books so the progress bar
			// can increment once for each section
			int sectionCount = 0;
			for (int i = 0; i < m_bookFilter.BookCount; i++)
				sectionCount += m_bookFilter.GetBook(i).SectionsOS.Count;
			using (ProgressDialogWithTask progressDlg = new ProgressDialogWithTask(dialogOwner))
			{
				progressDlg.Minimum = 0;
				progressDlg.Maximum = sectionCount;
				progressDlg.Title = DlgResources.ResourceString("kstidExportRtfProgress");
				progressDlg.AllowCancel = true;

				progressDlg.RunTask(ExportTE);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exports the TE.
		/// </summary>
		/// <param name="progressDialog">The progress dialog.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private object ExportTE(IThreadedProgress progressDialog, object[] parameters)
		{
			m_progressDlg = progressDialog;
			BuildStyleTable();

			ExportHeader();
			m_rtfStyleTable.Export(m_writer);
			ExportScripture();
			ExportFooter();

			m_progressDlg = null;

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write out the RTF header information
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportHeader()
		{
			m_writer.WriteLine(@"{\rtf1\ansi\ansicpg1252\ftnbj \deff0\deflang1033\delangfe1033\margl1440\margr1440\margt1440\margb1440");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export the closing material
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportFooter()
		{
			m_writer.WriteLine(@"}");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export the key terms font table
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportKTFontTable()
		{
			m_writer.WriteLine(@"{\fonttbl");

			// Write out the fonts
			m_writer.WriteLine(@"{\f" + "1" + @"\fnil\fprq2 " + "Arial" + @";}");

			m_writer.WriteLine(@"}");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export the key terms style table
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportKTStyleTable()
		{
			m_writer.WriteLine(@"{\stylesheet");

			m_writer.WriteLine(@"{\s100\ql\fi0\f1\fs20\tqr\tx9360 Heading;}");
			m_writer.WriteLine(@"{\s101\ql\sb240\fi0\f1\fs20\b KeyTerm;}");
			m_writer.WriteLine(@"{\s102\ql\li770\fi0\f1\fs20 Rendering;}");
			m_writer.WriteLine(@"{\s103\ql\li770\fi0\f1\fs20\i Rendering Special;}");
			m_writer.WriteLine(@"{\s104\ql\li1440\fi0\f1\fs20 References;}");
			m_writer.WriteLine(@"{\s105\ql\sb240\fi0\f1\fs20\b Category;}");

			m_writer.WriteLine(@"}");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build a table of all of the TE styles. This table holds information about
		/// the styles that is relevant to RTF.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void BuildStyleTable()
		{
			int defaultWs = m_content == ExportContent.BackTranslation ? m_btWS : m_cache.DefaultVernWs;
			ILgWritingSystemFactory wsf = m_cache.LanguageWritingSystemFactoryAccessor;
			WritingSystem ws = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			m_defaultWsIsRTL = ws.RightToLeftScript;
			m_rtfStyleTable = new RtfStyleInfoTable(m_cache, m_defaultWsIsRTL);

			// Add all of the styles from the style sheet to the RTF style table
			foreach (BaseStyleInfo style in m_styleSheet.Styles)
			{
				string fontName = m_styleSheet.GetFaceNameFromStyle(style.Name, defaultWs, wsf);
				m_rtfStyleTable.Add(style.Name, new RtfStyle(style.RealStyle, defaultWs));
			}
			m_rtfStyleTable.ConnectStyles();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export scripture paragraphs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportScripture()
		{
			// Export each of the books in the book filter.
			for (int bookIndex = 0; bookIndex < m_bookFilter.BookCount && !m_progressDlg.Canceled; bookIndex++)
			{
				IScrBook book = m_bookFilter.GetBook(bookIndex);
				m_progressDlg.Message = string.Format(DlgResources.ResourceString(
					"kstidExportBookStatus"), book.Name.UserDefaultWritingSystem.Text);
				ExportBook(book);
				m_progressDlg.Step(0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a scripture book
		/// </summary>
		/// <param name="book"></param>
		/// ------------------------------------------------------------------------------------
		private void ExportBook(IScrBook book)
		{
			// Export the title paragraphs
			foreach (IStTxtPara titlePara in book.TitleOA.ParagraphsOS)
				ExportParagraph(titlePara);

			// Export the sections
			foreach (IScrSection section in book.SectionsOS)
				ExportBookSection(section);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a scripture book section
		/// </summary>
		/// <param name="section"></param>
		/// ------------------------------------------------------------------------------------
		private void ExportBookSection(IScrSection section)
		{
			// count the section for progress
			m_progressDlg.Step(0);

			// Export the header paragraphs
			foreach (IStTxtPara headerPara in section.HeadingOA.ParagraphsOS)
				ExportParagraph(headerPara);

			// Export the content paragraphs
			foreach (IStTxtPara contentPara in section.ContentOA.ParagraphsOS)
				ExportParagraph(contentPara);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the correct back translation text for a given paragraph
		/// </summary>
		/// <param name="para"></param>
		/// ------------------------------------------------------------------------------------
		private ITsString GetBackTranslationText(IStTxtPara para)
		{
			ICmTranslation trans = para.GetOrCreateBT();

			// return the back translation text, if it exists
			ITsString btTss = trans.Translation.get_String(m_btWS);
			if (btTss != null && btTss.Length != 0)
				return btTss;

			// The BT is empty for this paragraph, so return a string to indicate that the
			// BT is missing.
			// TODO: keep the message in a static, and reuse it
			ITsStrBldr emptyMessage = TsStrBldrClass.Create();
			string message = TeResourceHelper.GetResourceString("kstidMissingBackTranslationText");
			emptyMessage.Replace(0, 0, message, StyleUtils.CharStyleTextProps(null, m_btWS));
			return emptyMessage.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a single paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void ExportParagraph(IStTxtPara para)
		{
			string paraStyleName = null;
			RtfStyle paraStyle = null;
			if (para.StyleRules != null)
			{
				paraStyleName = para.StyleRules.GetStrPropValue(
					(int)FwTextPropType.ktptNamedStyle);
				paraStyle = (RtfStyle)m_rtfStyleTable[paraStyleName];
				Debug.Assert(paraStyle != null);
			}
			else
			{
				Debug.Fail("para.StyleRules should never be null");
			}

			// Write out the paragraph attributes
			if (!m_firstParagraph)
				m_writer.Write(@"\par");
			m_firstParagraph = false;
			m_writer.Write(@"\pard\plain");

			// There should never not be a paragraph style. We found a case where stylenames were
			// getting created with non-Unicode characters (TE-3770), which caused them not to be
			// found when comparing strings. In case something like this ever happens again, we'll
			// play it safe and skip trying to write out style information.
			if (paraStyle != null)
				m_writer.Write(paraStyle.ToString(paraStyleName, false));

			// Write out each text run of the paragraph.
			ITsString tss;
			if (m_content == ExportContent.Scripture)
				tss = para.Contents;
			else
				tss = GetBackTranslationText(para);
			for (int run = 0; run < tss.RunCount; run++)
			{
				string runText = tss.get_RunText(run);
				if (runText == null)
					continue;

				ITsTextProps runProps = tss.get_Properties(run);
				m_writer.Write("{");
				// Look for footnotes
				if (runText.Length == 1 && runText[0] == StringUtils.kChObject)
				{
					string objData = runProps.GetStrPropValue((int)FwTextPropType.ktptObjData);
					// if ORC doesn't have properties, continue with next run.
					if (objData != null && objData[0] == (char)FwObjDataTypes.kodtOwnNameGuidHot ||
						objData[0] == (char)FwObjDataTypes.kodtNameGuidHot)
					{
						Guid footnoteGuid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
						IScrFootnote footnote;
						if (m_footnoteRepo.TryGetFootnote(footnoteGuid, out footnote))
						{
							if (m_content == ExportContent.Scripture)
								ExportFootnote(footnote);
							else
								ExportFootnote(footnote, m_btWS);
						}
					}
				}
				else
				{
					// If the run has a character style then spew out the style information
					string charStyleName = runProps.GetStrPropValue(
						(int)FwTextPropType.ktptNamedStyle);
					if (!string.IsNullOrEmpty(charStyleName) && m_rtfStyleTable.ContainsKey(charStyleName))
					{
						RtfStyle charStyle = (RtfStyle) m_rtfStyleTable[charStyleName];
						if (charStyle != null)
							m_writer.Write(charStyle.ToString(charStyleName, false) + " ");
					}

					ExportRun(runText);
					if (m_defaultWsIsRTL && charStyleName == ScrStyleNames.ChapterNumber)
						m_writer.Write(@"\uc0\u8207");
				}

				m_writer.WriteLine("}");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a run of text. This handles unicode conversion and escaping of characters
		/// when needed. Unicode characters will be composed.
		/// </summary>
		/// <param name="text"></param>
		/// ------------------------------------------------------------------------------------
		protected void ExportRun(string text)
		{
			m_writer.Write(TsStringUtils.Compose(ConvertString(text)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts the string for use in RTF.
		/// </summary>
		/// <param name="text">The text to convert.</param>
		/// <returns>The converted string</returns>
		/// ------------------------------------------------------------------------------------
		public static string ConvertString(String text)
		{
			if (text == null)
				text = string.Empty;

			// Build a string to write. For RTF, unicode characters need to be written
			// in the form "\uc0\uX " where X is the decimal unicode character value.
			StringBuilder bldr = new StringBuilder(1000);

			// RTF uses signed 16-bit values so any unicode characters greater than 32,767 will
			// be expressed as negative numbers.
			short chValue;
			foreach (char ch in text)
			{
				chValue = (short)ch;
				if (ch == '\\')
					bldr.Append("\\\\");
				else if (ch == '{' || ch == '}')
				{
					bldr.Append(@"\");
					bldr.Append(ch);
				}
				else if (chValue >= 0 && ch <= 127)
					bldr.Append(ch);
				else if (chValue == StringUtils.kChHardLB)
					bldr.Append(@"\line ");
				else
					bldr.Append(@"\uc0\u" + chValue.ToString() + " ");
			}
			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exports the vernacular footnote.
		/// </summary>
		/// <param name="footnote">The footnote to export.</param>
		/// ------------------------------------------------------------------------------------
		protected void ExportFootnote(IScrFootnote footnote)
		{
			ExportFootnote(footnote, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a footnote object.
		/// </summary>
		/// <param name="footnote">The footnote to be exported.</param>
		/// <param name="wsAlt">The writing system alternative. If 0, the vernacular footnote
		/// will be exported. Otherwise, the wsAlt indicates which back translation writing
		/// system to export.</param>
		/// ------------------------------------------------------------------------------------
		protected void ExportFootnote(IScrFootnote footnote, int wsAlt)
		{
			// Get the ITsString from the footnote to export.
			IStTxtPara footnotePara = (IStTxtPara)footnote.ParagraphsOS[0];
			ITsString tss = null;
			if (wsAlt == 0) // Get the vernacular text from the footnote
			{
				tss = footnotePara.Contents;
			}
			else
			{
				// Get the back translation for the specified writing system.
				ICmTranslation trans = footnotePara.GetBT();
				if (trans != null)
					tss = trans.Translation.get_String(wsAlt);
			}

			if (tss == null)
				return;

			// emit the marker into the body text
			ITsString tssMarker = footnote.FootnoteMarker;
			string footnoteMarker = tssMarker.Text;
			ITsTextProps markerProps = tssMarker.get_Properties(0);

			ExportCharStyle(markerProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			if (footnoteMarker != null)
			{
				// for RTL languages, RTF needs an RTL mark on either side of the footnote marker
				if (m_defaultWsIsRTL)
					footnoteMarker = "\u200f" + footnoteMarker + "\u200f";
				ExportRun(footnoteMarker);
			}
			else
				m_writer.Write(" ");
			m_writer.Write("}");
			m_writer.WriteLine();
			m_writer.Write("{");


			m_writer.Write(@"\footnote \pard\plain ");

			// output the paragraph style information for the footnote
			ITsTextProps paraProps = ((IStTxtPara)footnote.ParagraphsOS[0]).StyleRules;
			Debug.Assert(paraProps != null, "Footnote para StyleProps should never be null.");
			// Just to be safe, if StyleRules is null, get the default paragraph style
			string paraStyleName = paraProps != null ?
				paraProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle) :
				ScrStyleNames.NormalFootnoteParagraph;
			RtfStyle paraStyle = (RtfStyle)m_rtfStyleTable[paraStyleName];
			if (paraStyle != null)
				m_writer.Write(paraStyle.ToString(paraStyleName, false));

			// Output the footnote marker in the footnote itself if needed
			if (footnote.DisplayFootnoteMarker && footnoteMarker != null)
			{
				m_writer.Write("{");
				ExportCharStyle(ScrStyleNames.FootnoteMarker);
				ExportRun(footnoteMarker);
				m_writer.Write("}");
			}
			else
				m_writer.Write("{ }");

			// Output the chapter/verse reference if the footnote displays it.
			// But never output the ref for footnotes in a title or section heading.
			// And never output the ref for footnotes in an intro paragraph (which will
			// be the case when the verse number is zero).
			string reference = footnote.RefAsString;
			if (footnote.DisplayFootnoteReference && reference != null && reference != string.Empty)
			{
				m_writer.Write("{");
				ExportCharStyle(ScrStyleNames.FootnoteTargetRef);
				ExportRun(footnote.RefAsString);
				m_writer.Write("}");
			}
			else
				m_writer.Write("{ }");

			// Handle all of the runs in the footnote string. Footnote references occur out of
			// order so accumulate everything except footnote reference runs, then output the
			// accumulated stuff at the end.
			for (int iRun = 0; iRun < tss.RunCount; iRun++)
			{
				ITsTextProps runProps = tss.get_Properties(iRun);

				m_writer.Write("{");
				ExportCharStyle(runProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
				ExportRun(tss.get_RunText(iRun));
				m_writer.Write("}");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export character style information.
		/// </summary>
		/// <param name="styleName"></param>
		/// ------------------------------------------------------------------------------------
		private void ExportCharStyle(string styleName)
		{
			if (styleName != null)
			{
				RtfStyle charStyle = (RtfStyle)m_rtfStyleTable[styleName];
				if (charStyle != null)
					m_writer.Write(charStyle.ToString(styleName, false) + " ");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export Key Terms data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportKeyTermsData()
		{
			// Determine how to display the database name and project name
			string projectDisplay = m_cache.ProjectId.Name;

			// Export a page header
			m_writer.WriteLine(@"{\header \s100\ql\fi0\f1\fs20\tqr\tx9360 " +
				TeResourceHelper.GetResourceString("kstidExportRtfKtHeader") +
				@"\tab " + DateTime.Now.ToString() + @"\line " +
				projectDisplay +
				@"\tab Page \chpgn}");
			// go through all of the key terms looking for leaf nodes.
			ICmPossibilityList keyTermsList = m_cache.LangProject.KeyTermsList;
			// Set message to show that key terms are being selected
			m_progressDlg.Message = TeResourceHelper.GetResourceString("kstidSelectingMessage");
			SortedDictionary<string, KeyTermDictionary> selectedTerms = SelectKeyTerms(keyTermsList);
			// reset progress bar for export.
			m_progressDlg.Position = 1;
			m_progressDlg.Minimum = 1;
			m_progressDlg.Maximum = selectedTerms.Count;
			foreach (string categoryName in selectedTerms.Keys)
			{
				if (m_progressDlg.Canceled)
					break;
				// Set message to show category being exported
				m_progressDlg.Message = String.Format(TeResourceHelper.GetResourceString("kstidExportingMessage"), categoryName);
				WriteKTPara(categoryName, "Category");
				ExportKeyTermsForCategory(categoryName, selectedTerms[categoryName]);
				m_progressDlg.Step(1);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects the key terms that match the current book filter.
		/// </summary>
		/// <param name="keyTermsList">The key terms list.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private SortedDictionary<string, KeyTermDictionary> SelectKeyTerms(ICmPossibilityList keyTermsList)
		{
			SortedDictionary<string, KeyTermDictionary> result = new SortedDictionary<string, KeyTermDictionary>();
			foreach (ICmPossibility keyTermCategory in keyTermsList.PossibilitiesOS)
			{
				string name = TsStringUtils.Compose(keyTermCategory.ToString());
				KeyTermDictionary selectedTerms = SelectKeyTermsForCategory(keyTermCategory);
				if (selectedTerms.Count != 0)
					result.Add(name, selectedTerms);
				m_progressDlg.Step(1);
			}
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects the key terms for category that match the current book filter.
		/// </summary>
		/// <param name="keyTermCategory">The key term category.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private KeyTermDictionary SelectKeyTermsForCategory(ICmPossibility keyTermCategory)
		{
			KeyTermDictionary result = new KeyTermDictionary();
			foreach (IChkTerm keyTerm in keyTermCategory.SubPossibilitiesOS)
			{
				string name = TsStringUtils.Compose(keyTerm.ToString());
				List<IChkRef> selectedRefs = SelectReferencesForTerm(keyTerm);
				if (selectedRefs.Count != 0)
				{
					if (!result.ContainsKey(name))
						result.Add(name, selectedRefs);
					else
					{
						List<IChkRef> existingList = result[name];
						existingList.AddRange(selectedRefs);
					}
				}
			}
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects the references for term that match the current book filter.
		/// </summary>
		/// <param name="keyTerm">The key term.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private List<IChkRef> SelectReferencesForTerm(IChkTerm keyTerm)
		{
			List<IChkRef> result = new List<IChkRef>();
			List<int> activeBooks = m_bookFilter.BookIds;
			foreach (IChkRef checkRef in keyTerm.OccurrencesOS)
			{
				int bookId = BCVRef.GetBookFromBcv(checkRef.Ref);
				if (activeBooks.Contains(bookId))
					result.Add(checkRef);
			}
			return result;
		}

		/// <summary>
		/// Exports the key terms for a category.
		/// </summary>
		/// <param name="categoryName">Name of the category.</param>
		/// <param name="categoryTerms">The category terms.</param>
		/// ------------------------------------------------------------------------------------
		/// ------------------------------------------------------------------------------------
		private void ExportKeyTermsForCategory(string categoryName, KeyTermDictionary categoryTerms)
		{
			foreach (string keyTerm in categoryTerms.Keys)
			{
				if (m_progressDlg.Canceled)
					break;
				ExportSingleKeyTerm(keyTerm, categoryTerms[keyTerm]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a single key term with its references
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportSingleKeyTerm(string keyTerm, List<IChkRef> termRefs)
		{
			// This key term does not have any children, so export it now
			// export a paragraph for the name
			WriteKTPara(keyTerm, "KeyTerm");

			Dictionary<string, List<int>> renderingRefList = new Dictionary<string, List<int>>();
			List<int> missingRefList = new List<int>();
			List<int> ignoredRefList = new List<int>();

			// build a list of the renderings with references
			foreach (IChkRef checkRef in termRefs)
			{
				IWfiWordform wordForm = checkRef.RenderingRA;
				List<int> referenceList;
				string rendering;
				if (checkRef.Status == KeyTermRenderingStatus.Unassigned)
					referenceList = missingRefList;
				else if (checkRef.Status == KeyTermRenderingStatus.Ignored)
					referenceList = ignoredRefList;
				else if (wordForm != null)
				{
					rendering = wordForm.Form.VernacularDefaultWritingSystem.Text;

					if (!renderingRefList.ContainsKey(rendering))
					{
						// Make a new list of references
						referenceList = new List<int>();
						renderingRefList[rendering] = referenceList;
					}
					referenceList = renderingRefList[rendering];
				}
				else
				{
					// TE-8727: word form may be null when it shouldn't be, include
					// these in the missing list
					referenceList = missingRefList;
				}

				// Add the Scripture reference to the list
				referenceList.Add(checkRef.Ref);
			}

			// for each rendering, export the rendering info para and the references para
			foreach (string rendering in renderingRefList.Keys)
			{
				// TODO: When the literal translation and explanation are in the database
				// then get the text instead of the fill-ins here
				ExportRendering(rendering + " - literal translation - explanation",
					renderingRefList[rendering], "Rendering");
			}

			// export the missing renderings, and the intentionally not rendered list
			ExportRendering(TeResourceHelper.GetResourceString("kstidExportRtfKtMissingRendering"),
				missingRefList, "Rendering Special");
			ExportRendering(TeResourceHelper.GetResourceString("kstidExportRtfKtIntentionallyNotRendered"),
				ignoredRefList, "Rendering Special");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export one rendering section for a key term
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportRendering(string text, List<int> refList, string style)
		{
			// skip it if there are no references
			if (refList.Count == 0)
				return;

			// output a paragraph with the rendering, literal translation, and explanation
			WriteKTPara(text, style);

			// Output a paragraph with the list of references
			StringBuilder refListText = new StringBuilder();
			foreach (int intRef in refList)
			{
				if (refListText.Length != 0)
					refListText.Append(", ");
				refListText.Append(ScrReference.ToString(intRef));
			}
			WriteKTPara(refListText.ToString(), "References");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write out a key terms paragraph with a paragraph style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void WriteKTPara(string text, string paraStyle)
		{
			if (!m_firstParagraph)
				m_writer.Write(@"\par");
			m_firstParagraph = false;
			m_writer.Write(@"\pard\plain");
			switch (paraStyle)
			{
				case "Category":
					m_writer.Write(@"\s105\ql\sb240\fi0\f1\fs20\b");
					break;
				case "KeyTerm":
					m_writer.Write(@"\s101\ql\sb240\fi0\f1\fs20\b");
					break;
				case "Rendering":
					m_writer.Write(@"\s102\ql\li770\fi0\f1\fs20");
					break;
				case "Rendering Special":
					m_writer.Write(@"\s103\ql\li770\fi0\f1\fs20\i");
					break;
				case "References":
					m_writer.Write(@"\s104\ql\li1440\fi0\f1\fs20");
					break;
			}

			m_writer.Write("{");
			ExportRun(text);
			m_writer.WriteLine("}");
		}
		#endregion
	}
	#endregion
}
