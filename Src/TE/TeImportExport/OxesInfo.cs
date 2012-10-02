// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: OxesInfo.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This enumeration lists the specific contexts within which specific styles are valid.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum OxesContext
	{
		/// <summary>placeholder when context doesn't really matter</summary>
		Default,
		/// <summary>inside a &lt;section type="major"&gt; element</summary>
		MajorSection,
		/// <summary>inside a &lt;section&gt; element</summary>
		NormalSection,
		/// <summary>inside a &lt;section type="minor"&gt; element</summary>
		MinorSection,
		/// <summary>inside a &lt;section type="series"&gt; element</summary>
		SeriesSection,
		/// <summary>inside a &lt;section type="variant"&gt; element</summary>
		VariantSection,
		/// <summary>inside a &lt;sectionHead&gt; element within an &lt;introduction&gt; element</summary>
		IntroSection,
		/// <summary>inside an &lt;introduction&gt; element, but not in a &lt;sectionHead&gt; element</summary>
		Introduction,
		/// <summary>inside an &lt;embedded&gt; element</summary>
		Embedded,
		/// <summary>inside a &lt;speech&gt; element</summary>
		Speech,
		/// <summary>inside a &lt;table&gt; element</summary>
		Table,
		/// <summary>inside a &lt;row&gt; element</summary>
		Row,
		/// <summary>inside an &lt;item level="1"&gt; element</summary>
		ListItem1,
		/// <summary>inside an &lt;item level="2"&gt; element</summary>
		ListItem2,
		/// <summary>inside a &lt;title&gt; element</summary>
		Title,
		/// <summary>inside a &lt;note type="general" published="true" ...&gt; element</summary>
		Footnote,
		/// <summary>inside a &lt;book&gt; element</summary>
		Book
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The information stored in this class is used for both OXES export and import.  Since
	/// OXES import is part of TeDll, and since TeDll already uses TeExport, I'm putting this
	/// class definition in TeExport so that both DLLs can access it.  It's too TE specific to
	/// place in the OxesIO assembly.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class OxesInfo
	{
		#region data members
		/// <summary>
		/// The name of the TE style.
		/// </summary>
		private string m_sStyleName;
		/// <summary>
		/// True if this is a character style, false if it's a paragraph style.
		/// </summary>
		private bool m_fIsCharStyle;
		/// <summary>
		/// An outer XML tag which must be enclose the data for this specific paragraph style.
		/// </summary>
		private OxesContext m_context;
		/// <summary>
		/// The primary XML tag associated with this style.
		/// </summary>
		private string m_sXmlTag;
		/// <summary>
		/// The first attribute associated with the XML tag, if any.
		/// </summary>
		private string m_sAttrName;
		/// <summary>
		/// The value of the first attribute if it exists, and is fixed for this style.
		/// </summary>
		private string m_sAttrValue;
		/// <summary>
		/// The second attribute associated with the XML tag, if any.
		/// </summary>
		private string m_sAttrName2;
		/// <summary>
		/// The value of the second attribute if it exists, and is fixed for this style.
		/// </summary>
		private string m_sAttrValue2;
		/// <summary>
		/// True iff this character style can be embedded inside another character style.
		/// (somewhat experimental...)
		/// </summary>
		private bool m_fCanEmbed;
		/// <summary>
		/// True iff this paragraph style is applicable to headings instead of content.
		/// </summary>
		private bool m_fIsHeading;
		#endregion data members

		#region constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="fChar"></param>
		/// <param name="styleName"></param>
		/// <param name="context"></param>
		/// <param name="sTag"></param>
		/// <param name="sAttrName"></param>
		/// <param name="sAttrValue"></param>
		/// <param name="sAttrName2"></param>
		/// <param name="sAttrValue2"></param>
		/// <param name="fCanEmbed"></param>
		/// <param name="fIsHeading"></param>
		/// ------------------------------------------------------------------------------------
		private OxesInfo(bool fChar, string styleName, OxesContext context, string sTag,
			string sAttrName, string sAttrValue, string sAttrName2, string sAttrValue2,
			bool fCanEmbed, bool fIsHeading)
		{
			m_fIsCharStyle = fChar;
			m_sStyleName = styleName;
			m_context = context;
			m_sXmlTag = sTag;
			m_sAttrName = sAttrName;
			m_sAttrValue = sAttrValue;
			m_sAttrName2 = sAttrName2;
			m_sAttrValue2 = sAttrValue2;
			m_fCanEmbed = fCanEmbed;
			m_fIsHeading = fIsHeading;
		}
		#endregion constructors

		#region public accessors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// the OXES XML tag
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string XmlTag
		{
			get { return m_sXmlTag; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// the first OXES attribute for the XML tag, if any
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string AttrName
		{
			get { return m_sAttrName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// the first OXES attribute value for the XML tag, if any (and if fixed)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string AttrValue
		{
			get { return m_sAttrValue; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// the second OXES attribute for the XML tag, if any
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string AttrName2
		{
			get { return m_sAttrName2; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// the second OXES attribute value for the XML tag, if any (and if fixed)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string AttrValue2
		{
			get { return m_sAttrValue2; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// the context for this style and XML tag
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public OxesContext Context
		{
			get { return m_context; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// the TE style name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string StyleName
		{
			get { return m_sStyleName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// whether this paragraph style belongs in a heading, as opposed to the main content
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsHeading
		{
			get { return m_fIsHeading; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// whether this character style can be embedded inside another (somewhat experimental...)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool CanEmbed
		{
			get { return m_fCanEmbed; }
		}
		#endregion

		#region private static data and methods
		/// <summary>
		/// This array contains all of the standard OXES information for each standard TE style.
		/// </summary>
		/// <remarks>
		/// Note that some mappings between TE styles and OXES markup aren't fully described by
		/// this data structure.
		/// </remarks>
		static private OxesInfo[] g_rgOxesInfo = new OxesInfo[] {
//          IsChar	 StyleName									Context						XmlTag					AttrName	AttrValue		AttrName2	AttrValue2	CanEmbed IsHeader
//			-----	 ---------									-----------					------					--------	---------		---------	----------	-------- --------
new OxesInfo(true,   ScrStyleNames.Abbreviation,				OxesContext.Default,		"abbreviation",			"expansion",null,			null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.AlludedText,					OxesContext.Default,		"alludedText",			null,		null,			null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.AlternateReading,			OxesContext.Default,		"alternateReading",		null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.Attribution,					OxesContext.Default,		"l",					"type",		"attribution",	null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.BookTitleInText,				OxesContext.Default,		"bookTitleInText",		null,		null,			null,		null,		false,	 false),
//new OxesInfo(true, ScrStyleNames.CanonicalRef,				OxesContext.Default,		"reference",			"oxesID",	null,			null,		null,		false,	 false),
//new OxesInfo(false,"Caption",									OxesContext.Default,		"caption",				null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.ChapterHead,					OxesContext.NormalSection,	"chapterHead",			null,		null,			null,		null,		false,	 true),
new OxesInfo(true,   ScrStyleNames.ChapterNumber,				OxesContext.Default,		"chapterStart",			"ID",		null,			null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.ChapterNumberAlternate,		OxesContext.Default,		"chapterStart",			"ID",		null,			"aID",		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.CitationLine1,				OxesContext.Default,		"l",					"level",	"1",			"type",		"citation",	false,	 false),
new OxesInfo(false,  ScrStyleNames.CitationLine2,				OxesContext.Default,		"l",					"level",	"2",			"type",		"citation",	false,	 false),
new OxesInfo(false,  ScrStyleNames.CitationParagraph,			OxesContext.Default,		"p",					"type",		"citation",		null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.Closing,						OxesContext.Default,		"closing",				null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.CongregationalResponse,		OxesContext.Default,		"p",					"type",		"congregationalResponse",		null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.CrossReference,				OxesContext.Default,		"crossReference",		null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.Doxology,					OxesContext.Default,		"l",					"type",		"doxology",		null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.EmbeddedTextClosing,			OxesContext.Embedded,		"p",					"type",		"closing",		null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.EmbeddedTextLine1,			OxesContext.Embedded,		"l",					"level",	"1",			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.EmbeddedTextLine2,			OxesContext.Embedded,		"l",					"level",	"2",			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.EmbeddedTextLine3,			OxesContext.Embedded,		"l",					"level",	"3",			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.EmbeddedTextOpening,			OxesContext.Embedded,		"p",					"type",		"salute",		null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.EmbeddedTextParagraph,		OxesContext.Embedded,		"p",					null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.EmbeddedTextParagraphContinuation,OxesContext.Embedded,	"p",					"type",		"continuation",	null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.EmbeddedTextRefrain,			OxesContext.Embedded,		"p",					"type",		"refrain",		null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.Emphasis,					OxesContext.Default,		"emphasis",				null,		null,			null,		null,		true,	 false),
new OxesInfo(true,   ScrStyleNames.Foreign,						OxesContext.Default,		"foreign",				null,		null,			null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.Gloss,						OxesContext.Default,		"gloss",				null,		null,			null,		null,		false,	 false),
//new OxesInfo(false,"Glossary Definition",						OxesContext.Default,		"definition",			null,		null,			null,		null,		false,	 false),
//new OxesInfo(false,"Glossary Entry Main",						OxesContext.Default,		"entryMain",			null,		null,			null,		null,		false,	 false),
//new OxesInfo(false,"Glossary Entry Secondary",				OxesContext.Default,		"entrySecondary",		null,		null,			null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.Hand,						OxesContext.Default,		"hand",					null,		null,			null,		null,		false,	 false),
//new OxesInfo(true, ScrStyleNames.Header,						OxesContext.Default,		"title",				"short",	null,			null,		null,		false,	 true),
new OxesInfo(false,  ScrStyleNames.HebrewTitle,					OxesContext.Default,		"sectionHead",			"type",		"psalm",		null,		null,		false,	 true),
//new OxesInfo(true, StStyle.Hyperlink,					OxesContext.Default,		"a",					"href",		null,			null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.Inscription,					OxesContext.Default,		"inscription",			null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.InscriptionParagraph,		OxesContext.Default,		"p",					"type",		"inscription",	null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.Interlude,					OxesContext.Default,		"l",					"type",		"selah",		null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.IntroCitationLine1,			OxesContext.Introduction,	"l",					"level",	"1",			"type",		"citation",	false,	 false),
new OxesInfo(false,  ScrStyleNames.IntroCitationLine2,			OxesContext.Introduction,	"l",					"level",	"2",			"type",		"citation",	false,	 false),
new OxesInfo(false,  ScrStyleNames.IntroCitationParagraph,		OxesContext.Introduction,	"p",					"type",		"citation",		null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.IntroCrossReference,			OxesContext.Introduction,	"p",					"type",		"cross-reference",null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.IntroListItem1,				OxesContext.Introduction,	"item",					"level",	"1",			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.IntroListItem2,				OxesContext.Introduction,	"item",					"level",	"2",			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.IntroListItem3,				OxesContext.Introduction,	"item",					"level",	"3",			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.IntroParagraph,				OxesContext.Introduction,	"p",					null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.IntroSectionHead,			OxesContext.IntroSection,	"sectionHead",			null,		null,			null,		null,		false,	 true),
//new OxesInfo(false,"Intro Title Main",						OxesContext.Introduction,	"title",				"type",		"main",			null,		null,		false,	 true),
//new OxesInfo(true,   ScrStyleNames.IntroTitleSecondary,			OxesContext.Introduction,	"title",				"type",		"secondary",	null,		null,		false,	 true),
//new OxesInfo(true,   ScrStyleNames.IntroTitleTertiary,			OxesContext.Introduction,	"title",				"type",		"tertiary",		null,		null,		false,	 true),
new OxesInfo(true,   ScrStyleNames.KeyWord,						OxesContext.Default,		"keyWord",				null,		null,			null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.Label,						OxesContext.Default,		"label",				null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.Line1,						OxesContext.Default,		"l",					"level",	"1",			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.Line2,						OxesContext.Default,		"l",					"level",	"2",			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.Line3,						OxesContext.Default,		"l",					"level",	"3",			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.ListItem1,					OxesContext.Default,		"item",					"level",	"1",			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.ListItem1Additional,			OxesContext.ListItem1,		"p",					null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.ListItem2,					OxesContext.Default,		"item",					"level",	"2",			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.ListItem2Additional,			OxesContext.ListItem2,		"p",					null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.ListItem3,					OxesContext.Default,		"item",					"level",	"3",			null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.Mentioned,					OxesContext.Default,		"mentioned",			null,		null,			null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.NameOfGod,					OxesContext.Default,		"nameOfGod",			null,		null,			null,		null,		true,	 false),
new OxesInfo(false,  ScrStyleNames.CrossRefFootnoteParagraph,	OxesContext.Default,		"note",					"type",		"crossReference","canonical",null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.NormalFootnoteParagraph,		OxesContext.Default,		"note",					"type",		"general",		"canonical",null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.FootnoteTargetRef,			OxesContext.Footnote,		"reference",			null,		null,			null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.OrdinalNumberEnding,			OxesContext.Default,		"ordinalNumberEnding",	null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.NormalParagraph,				OxesContext.Default,		"p",					null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.ParagraphContinuation,		OxesContext.Default,		"p",					"type",		"continuation",	null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.ParallelPassageReference,	OxesContext.Default,		"reference",			"type",		"parallel",		null,		null,		false,	 true),
new OxesInfo(true,   ScrStyleNames.QuotedText,					OxesContext.Default,		"otPassage",			null,		null,			null,		null,		true,	 false),
new OxesInfo(true,   ScrStyleNames.ReferencedText,				OxesContext.Default,		"referencedText",		null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.Refrain,						OxesContext.Default,		"l",					"type",		"refrain",		null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.Remark,						OxesContext.Default,		"comment",				null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.SectionHead,					OxesContext.NormalSection,	"sectionHead",			null,		null,			null,		null,		false,	 true),
new OxesInfo(false,  ScrStyleNames.SectionHeadMajor,			OxesContext.MajorSection,	"sectionHead",			null,		null,			null,		null,		false,	 true),
new OxesInfo(false,  ScrStyleNames.SectionHeadMinor,			OxesContext.MinorSection,	"sectionHead",			null,		null,			null,		null,		false,	 true),
new OxesInfo(false,  ScrStyleNames.SectionHeadSeries,			OxesContext.SeriesSection,	"sectionHead",			null,		null,			null,		null,		false,	 true),
new OxesInfo(false,  ScrStyleNames.SectionRangeParagraph,		OxesContext.Default,		"sectionHead",			"type",		"range",		null,		null,		false,	 true),
new OxesInfo(true,   ScrStyleNames.SeeInGlossary,				OxesContext.Default,		"seeInGlossary",		null,		null,			null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.SoCalled,					OxesContext.Default,		"soCalled",				null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.SpeechLine1,					OxesContext.Speech,			"l",					"level",	"1",			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.SpeechLine2,					OxesContext.Speech,			"l",					"level",	"2",			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.SpeechSpeaker,				OxesContext.Speech,			"speaker",				null,		null,			null,		null,		false,	 true),
new OxesInfo(false,  ScrStyleNames.StanzaBreak,					OxesContext.Default,		"stanzaBreak",			null,		null,			null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.Supplied,					OxesContext.Default,		"supplied",				null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.TableCell,					OxesContext.Row,			"cell",					null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.TableCellHead,				OxesContext.Row,			"head",					null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.TableCellHeadLast,			OxesContext.Row,			"head",					"align",	null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.TableCellLast,				OxesContext.Row,			"cell",					"align",	null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.TableRow,					OxesContext.Table,			"row",					null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.MainBookTitle,				OxesContext.Book,			"title",				"type",		"main",			null,		null,		false,	 true),
new OxesInfo(true,   ScrStyleNames.SecondaryBookTitle,			OxesContext.Book,			"title",				"type",		"secondary",	null,		null,		false,	 true),
new OxesInfo(true,   ScrStyleNames.TertiaryBookTitle,			OxesContext.Book,			"title",				"type",		"tertiary",		null,		null,		false,	 true),
new OxesInfo(true,   ScrStyleNames.UntranslatedWord,			OxesContext.Default,		"untranslatedWord",		null,		null,			null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.Variant,						OxesContext.Default,		"variant",				null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.VariantParagraph,			OxesContext.VariantSection,	"p",					null,		null,			null,		null,		false,	 false),
new OxesInfo(false,  ScrStyleNames.VariantSectionHead,			OxesContext.VariantSection,	"sectionHead",			null,		null,			null,		null,		false,	 true),
new OxesInfo(false,  ScrStyleNames.VariantSectionTail,			OxesContext.VariantSection,	"p",					"type",		"tail",			null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.VerseNumber,					OxesContext.Default,		"verseStart",			"ID",		null,			null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.VerseNumberAlternate,		OxesContext.Default,		"verseStart",			"ID",		null,			"aID",		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.VerseNumberInNote,			OxesContext.Footnote,		"verseStart",			"ID",		null,			null,		null,		false,	 false),
new OxesInfo(true,   ScrStyleNames.WordsOfChrist,				OxesContext.Default,		"wordsOfJesus",			null,		null,			null,		null,		false,	 false),
			};
		/// <summary>
		/// Information for a paragraph without an assigned style.
		/// </summary>
		static private OxesInfo m_emptyPara = new OxesInfo(false, null, OxesContext.Default, "p",  null, null, null, null, false, false);
		/// <summary>
		/// Information for a character run without an assigned style.
		/// </summary>
		static private OxesInfo m_emptyChar = new OxesInfo(true,  null, OxesContext.Default, null, null, null, null, null, false, false);
		/// <summary>
		/// map from a TE style to the corresponding OxesInfo object
		/// </summary>
		static private Dictionary<string, OxesInfo> g_mapStyleInfo = null;
		/// <summary>
		/// map from an OXES XML tag to a list of corresponding OxesInfo objects
		/// </summary>
		static private Dictionary<string, List<OxesInfo>> g_mapTagInfo = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a map from a style name to the complete OXES information about the style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static private Dictionary<string, OxesInfo> StyleMap
		{
			get
			{
				if (g_mapStyleInfo == null)
				{
					g_mapStyleInfo = new Dictionary<string, OxesInfo>(g_rgOxesInfo.Length);
					for (int i = 0; i < g_rgOxesInfo.Length; ++i)
						g_mapStyleInfo.Add(g_rgOxesInfo[i].m_sStyleName, g_rgOxesInfo[i]);
				}
				return g_mapStyleInfo;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a map from an XML tag to a list of matching OxesInfo objects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static private Dictionary<string, List<OxesInfo>> TagMap
		{
			get
			{
				if (g_mapTagInfo == null)
				{
					g_mapTagInfo = new Dictionary<string, List<OxesInfo>>();
					for (int i = 0; i < g_rgOxesInfo.Length; ++i)
					{
						OxesInfo xinfo = g_rgOxesInfo[i];
						if (xinfo.XmlTag == null)
							continue;
						List<OxesInfo> rgoxes;
						if (g_mapTagInfo.TryGetValue(xinfo.XmlTag, out rgoxes))
						{
							rgoxes.Add(xinfo);
						}
						else
						{
							rgoxes = new List<OxesInfo>();
							rgoxes.Add(xinfo);
							g_mapTagInfo.Add(xinfo.XmlTag, rgoxes);
						}
					}
				}
				return g_mapTagInfo;
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test whether attributes listed in the OxesInfo are compatible with those in the
		/// XmlNode.
		/// </summary>
		/// <param name="xinfo"></param>
		/// <param name="node"></param>
		/// <returns>true if the two objects have matching attributes, otherwise false</returns>
		/// ------------------------------------------------------------------------------------
		private static bool AttributesMatch(OxesInfo xinfo, XmlNode node)
		{
			// First, check that everything in the OxesInfo is in the XmlNode.
			if (xinfo.AttrName != null)
			{
				string sValue = XmlUtils.GetOptionalAttributeValue(node, xinfo.AttrName);
				// If the node is "abbreviation", we need a special case here because another
				// abbreviation node has an expansion attribute which is not required for
				// the character style.
				if (node.Name == "abbreviation")
					return true;
				if (xinfo.AttrValue == null && sValue == null)
					return false;		// we need a value, we don't care what it is.
				if (xinfo.AttrValue != null && sValue != xinfo.AttrValue)
					return false;		// we need a specific value.
			}
			if (xinfo.AttrName2 != null)
			{
				string sValue = XmlUtils.GetOptionalAttributeValue(node, xinfo.AttrName2);
				if (xinfo.AttrValue2 == null && sValue == null)
					return false;		// we need a value, we don't care what it is.
				else if (xinfo.AttrValue2 != null && sValue != xinfo.AttrValue2)
					return false;		// we need a specific value.
			}
			// Now, check that attributes in the XmlNode are in the OxesInfo.
			foreach (XmlAttribute attr in node.Attributes)
			{
				if (attr.Name == xinfo.AttrName)
				{
					if (xinfo.AttrValue == null)
						continue;			// any value is okay.
					else if (attr.Value != xinfo.AttrValue)
						return false;
				}
				else if (attr.Name == xinfo.AttrName2)
				{
					if (xinfo.AttrValue2 == null)
						continue;			// any value is okay
					else if (attr.Value != xinfo.AttrValue2)
						return false;
				}
				// oxesRef is always the third attribute (not listed),
				// and xmlns is auto-inserted by the .Net framework code.
				else if (attr.Name != "oxesRef" && attr.Name != "xmlns")
				{
					return false;
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check whether the OxesInfo object is compatible with the given context information.
		/// </summary>
		/// <param name="xinfo"></param>
		/// <param name="fInIntroSection"></param>
		/// <param name="sSectionType"></param>
		/// <param name="sXmlParentTag"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static bool IsProperContextForStyle(OxesInfo xinfo, bool fInIntroSection,
			string sSectionType, string sXmlParentTag)
		{
			// If there's no context needed or provided, return true.
			if (xinfo.Context == OxesContext.Default && sXmlParentTag == null && !fInIntroSection)
				return true;
			switch (xinfo.Context)
			{
				case OxesContext.Introduction:
				case OxesContext.IntroSection:
					return fInIntroSection;
				case OxesContext.Embedded:
					return sXmlParentTag == "embedded";
				case OxesContext.Speech:
					return sXmlParentTag == "speech";
				case OxesContext.Table:
					return sXmlParentTag == "table";
				case OxesContext.Row:
					return sXmlParentTag == "row";
				case OxesContext.MajorSection:
					return sSectionType == "major";
				case OxesContext.NormalSection:
					return sSectionType == "";
				case OxesContext.MinorSection:
					return sSectionType == "minor";
				case OxesContext.SeriesSection:
					return sSectionType == "series";
				case OxesContext.VariantSection:
					return sSectionType == "variant";
				case OxesContext.ListItem1:
					return sXmlParentTag == ScrStyleNames.ListItem1;
				case OxesContext.ListItem2:
					return sXmlParentTag == ScrStyleNames.ListItem2;
				case OxesContext.Book:
				case OxesContext.Footnote:
				case OxesContext.Title:
					break;			// these don't really distinguish as far as I can tell
			}
			switch (sXmlParentTag)
			{
				case "embedded":
					return xinfo.Context == OxesContext.Embedded;
				case "speech":
					return xinfo.Context == OxesContext.Speech;
				case "table":
					return xinfo.Context == OxesContext.Table;
				case "row":
					return xinfo.Context == OxesContext.Row;
				case ScrStyleNames.ListItem1:
					return xinfo.Context == OxesContext.ListItem1;
				case ScrStyleNames.ListItem2:
					return xinfo.Context == OxesContext.ListItem2;
				case "title":
				case "note":
					break;			// I don't know if these even occur.
			}
			return true;
		}
		#endregion private static data and methods

		#region public static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the OXES information for the given character style, creating a temporary userCS
		/// object if needed.
		/// </summary>
		/// <param name="style"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public OxesInfo GetOxesInfoForCharStyle(string style)
		{
			OxesInfo oxes;
			if (String.IsNullOrEmpty(style))
				return m_emptyChar;
			else if (StyleMap.TryGetValue(style, out oxes) && oxes.m_fIsCharStyle)
				return oxes;
			else
				return new OxesInfo(true, style, OxesContext.Default, "userCS", "type", style, null, null, false, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the OXES information for the given paragraph style, creating a temporary userPS
		/// object if needed.
		/// </summary>
		/// <param name="style">The style.</param>
		/// <param name="fSectionHead">if set to <c>true</c> this is a section head style (used
		/// only for non-standard styles).</param>
		/// ------------------------------------------------------------------------------------
		static public OxesInfo GetOxesInfoForParaStyle(string style, bool fSectionHead)
		{
			OxesInfo oxes;
			if (String.IsNullOrEmpty(style))
				return m_emptyPara;
			else if (StyleMap.TryGetValue(style, out oxes) && !oxes.m_fIsCharStyle)
				return oxes;
			else if (fSectionHead)
				return new OxesInfo(false, style, OxesContext.NormalSection, null, null, null, null, null, false, true);
			else
				return new OxesInfo(false, style, OxesContext.Default, "p", "type", "userPS", "subType", style, false, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the OXES information that best matches the given XML node.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public OxesInfo GetOxesInfoForCharNode(XmlNode node)
		{
			List<OxesInfo> rgoxes;
			if (TagMap.TryGetValue(node.Name, out rgoxes))
			{
				foreach (OxesInfo xinfo in rgoxes)
				{
					if (xinfo.m_fIsCharStyle && AttributesMatch(xinfo, node))
						return xinfo;
				}
				return m_emptyChar;
			}
			else if (node.Name == "userCS")
			{
				string sType = XmlUtils.GetAttributeValue(node, "type");
				if (!String.IsNullOrEmpty(sType))
					return new OxesInfo(true, sType, OxesContext.Default, "userCS", "type", sType, null, null, false, false);
			}
			return m_emptyChar;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the OXES information that best matches the given XML node in the given context.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="fInIntroSection"></param>
		/// <param name="sSectionType"></param>
		/// <param name="sXmlParentTag"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public OxesInfo GetOxesInfoForParaNode(XmlNode node, bool fInIntroSection,
			string sSectionType, string sXmlParentTag)
		{
			List<OxesInfo> rgoxes;
			if (TagMap.TryGetValue(node.Name, out rgoxes))
			{
				OxesInfo xinfoMatch = null;
				OxesInfo xinfoSemiMatch = null;
				foreach (OxesInfo xinfo in rgoxes)
				{
					if (xinfo.m_fIsCharStyle)
						continue;
					if (!IsProperContextForStyle(xinfo, fInIntroSection, sSectionType, sXmlParentTag))
						continue;
					if (!AttributesMatch(xinfo, node))
						continue;
					if (fInIntroSection != (xinfo.Context == OxesContext.Introduction || xinfo.Context == OxesContext.IntroSection))
					{
						xinfoSemiMatch = xinfo;
						continue;
					}
					Debug.Assert(xinfoMatch == null || xinfoMatch.Context == OxesContext.Default,
						"We should only get another match if its more specific");
					xinfoMatch = xinfo;
				}
				if (xinfoMatch == null && xinfoSemiMatch != null)
				{
					// TODO: Log error message about misplaced style?
					xinfoMatch = xinfoSemiMatch;
				}
				if (xinfoMatch != null)
					return xinfoMatch;
				bool isSectionHead = (node.Name == "sectionHead");
				if ((node.Name == "p" && XmlUtils.GetOptionalAttributeValue(node, "type") == "userPS") ||
					(isSectionHead && XmlUtils.GetOptionalAttributeValue(node, "type") == "userDefined"))
				{
					string sStyle = XmlUtils.GetOptionalAttributeValue(node, "subType");
					if (!String.IsNullOrEmpty(sStyle))
					{
						OxesContext context;
						if (isSectionHead)
						{
							context = (fInIntroSection) ? OxesContext.IntroSection : OxesContext.NormalSection;
						}
						else
						{
							context = (fInIntroSection) ?  OxesContext.Introduction : OxesContext.Default;
						}
						return new OxesInfo(false, sStyle,
							context, node.Name, "type", "userPS", "subType", sStyle, false, isSectionHead);
					}
				}
			}
			// Handle oddball entries.
			if (node.Name == "title")
			{
				string sType = XmlUtils.GetOptionalAttributeValue(node, "type");
				if (sType == "parallelPassage")
				{
					foreach (XmlNode xn in node.ChildNodes)
					{
						if (xn.Name == "reference")
							return GetOxesInfoForParaNode(xn, fInIntroSection, sSectionType, sXmlParentTag);
					}
				}
			}
			return m_emptyPara;
		}
		#endregion public static methods

		#region overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check whether this object is equal to another.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			OxesInfo that = obj as OxesInfo;
			if (that == null)
				return false;
			else
				return this.StyleName == that.StyleName && this.m_fIsCharStyle == that.m_fIsCharStyle &&
					this.Context == that.Context && this.XmlTag == that.XmlTag &&
					this.AttrName == that.AttrName && this.AttrValue == that.AttrValue &&
					this.AttrName2 == that.AttrName2 && this.AttrValue2 == that.AttrValue2 &&
					this.CanEmbed == that.CanEmbed && this.IsHeading == that.IsHeading;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compute a hash code for this object.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return (StyleName == null ? 0 : StyleName.GetHashCode()) +
				Context.GetHashCode() +
				(XmlTag == null ? 0 : XmlTag.GetHashCode()) +
				(AttrName == null ? 0 : AttrName.GetHashCode()) +
				(AttrValue == null ? 0 : AttrValue.GetHashCode()) +
				(AttrName2 == null ? 0 : AttrName2.GetHashCode()) +
				(AttrValue2 == null ? 0 : AttrValue2.GetHashCode()) +
				m_fIsCharStyle.GetHashCode() +
				IsHeading.GetHashCode() +
				CanEmbed.GetHashCode();
		}
		#endregion
	}
}
