// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrFootnote.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SILUBS.SharedScrUtils;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.Scripture
{
	#region Footnote hash entry struct
	internal class FootnoteHashEntry
	{
		/// <summary>Starting Scripture reference</summary>
		private BCVRef m_startRef; //
		/// <summary>Ending Scripture reference (same as start except in case of verse bridge)</summary>
		private BCVRef m_endRef;
		/// <summary>paragraph where footnote is</summary>
		private int m_hvoPara;
		/// <summary>
		/// Flag indicating whether footnote ORC is still present in the text as of the latest scan.
		/// If not, the footnote is in the process of being deleted or has somehow gotten orphaned.
		/// </summary>
		private bool m_fFound;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="startRef">The start reference at the position where the footnote ORC is
		/// in the vernacular text</param>
		/// <param name="endRef">The end reference at the position where the footnote ORC is in
		/// the vernacular text (can be different from start ref because of verse bridges).
		/// </param>
		/// <param name="hvo">The ID of the paragraph containing the ORC for the footnote. This
		/// could also be the last paragraph where it was found if it is in the process of
		/// being deleted (in which case ORCs may still exist in the corresponding back
		/// translations).</param>
		/// ------------------------------------------------------------------------------------
		public FootnoteHashEntry(BCVRef startRef, BCVRef endRef, int hvo)
		{
			m_startRef = new BCVRef(startRef);
			m_endRef = new BCVRef(endRef);
			m_hvoPara = hvo;
			m_fFound = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the start reference of the footnote has entry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal BCVRef StartRef
		{
			get { return new BCVRef(m_startRef); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the end reference of the footnote has entry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal BCVRef EndRef
		{
			get { return new BCVRef(m_endRef); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/Sets the hvo of the paragraph containing the footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal int HvoPara
		{
			get { return m_hvoPara; }
			set { m_hvoPara = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets flag indicating whether footnote ORC is still present in the text.
		/// If at the end of a complete scan of the text, this flag is <c>false</c>, the
		/// footnote is in the process of being deleted or has somehow gotten orphaned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Found
		{
			get { return m_fFound; }
			set
			{
				// ENHANCE (TE-2886): We really want to enable the following assertion, but there
				// is an edge case when copying from a saved version in the Compare and Merge
				// dialog box. In that case, a footnote ORC can temporarily be included in the
				// text of two different books and can therefore be found twice. The only
				// semi-good way we could think of to prevent this was to make a special
				// constructor for ScrBook and pass a flag down the call stack to suppress
				// rescanning of footnotes in this instance. That would have made the code more
				// confusing, so we decided not to do that. Implementing TE-2885
				// Debug.Assert(!value || m_fFound != value, "Footnote ORC should not be found twice during a scan of the text.");
				m_fFound = value;
			}
		}
	}
	#endregion

	#region ScrFootnote
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Extends the StFootnote class for methods that are specific to scripture.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrFootnote : StFootnote
	{
		#region Data members
		/// <summary></summary>
		protected IScripture m_scr;
		private bool m_fIgnoreDisplaySettings = false;
		#endregion

		#region Construction & initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used for code like this foo.blah = new  StFootnote().
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrFootnote()
			:base() {}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Normal constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrFootnote(FdoCache fcCache, int hvo)
			:base(fcCache, hvo)
		{
			Init();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this constructor where you want to have control over loading/validating objects
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrFootnote(FdoCache fcCache, int hvo, bool fCheckValidity, bool fLoadIntoCache)
			:base(fcCache, hvo, fCheckValidity, fLoadIntoCache)
		{
			Init();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize stuff
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void Init()
		{
			m_scr = Cache.LangProject.TranslatedScriptureOA;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the StFootnote in the given owning ScrBook.
		/// The caller must take care of inserting the proper ORC and the footnote's paragraph
		/// and a propchanged for the new footnote inserted into the book's collection.
		/// </summary>
		/// <param name="book">The book that the footnote is being added to</param>
		/// <param name="iInsertAt">Zero-based index of the position in the book's sequence of
		/// footnotes where the new footnote is to be inserted</param>
		/// <param name="sMarker">ref: The marker to use (more or less)</param>
		/// <param name="cache">The cache.</param>
		/// <param name="ws">The writing system for the footnote marker.</param>
		/// <returns>The newly created Footnote object</returns>
		/// ------------------------------------------------------------------------------------
		public static StFootnote CreateFootnoteInScrBook(IScrBook book, int iInsertAt,
			ref string sMarker, FdoCache cache, int ws)
		{
			// Create StFootnote object and establish it in the database
			StFootnote footnote = new StFootnote();
			book.FootnotesOS.InsertAt(footnote, iInsertAt);

			// Create a default FootnoteMarker property in the StFootnote with
			// the correct text properties
			ITsTextProps markerProps =
				StyleUtils.CharStyleTextProps(ScrStyleNames.FootnoteMarker, ws);
			ITsStrBldr tsStrBldrFootnoteMkr = TsStrBldrClass.Create();
			if (sMarker == null)
				sMarker = string.Empty;
			tsStrBldrFootnoteMkr.Replace(0, 0, sMarker, markerProps);
			footnote.FootnoteMarker.UnderlyingTsString = tsStrBldrFootnoteMkr.GetString();

			// Mark the footnote marker style as being in use.
			IScripture scr = cache.LangProject.TranslatedScriptureOA;
			IStStyle markerStyle = scr.FindStyle(ScrStyleNames.FootnoteMarker);
			markerStyle.InUse = true;

			// Copy current value of default footnote options to the new footnote.
			footnote.DisplayFootnoteReference = scr.DisplayFootnoteReference;
			footnote.DisplayFootnoteMarker = scr.DisplayFootnoteMarker;

			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the given footnote and recalculates the footnote markers of following
		/// footnotes. (Note: this does not delete the footnote marker from the text.)
		/// </summary>
		/// <param name="footnote"></param>
		/// ------------------------------------------------------------------------------------
		public static void DeleteFootnote(StFootnote footnote)
		{
			ScrBook book = new ScrBook(footnote.Cache, footnote.OwnerHVO);
			book.RemoveFootnote(footnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a footnote by inserting the footnote marker into the given string builder
		/// and creating a new <see cref="StFootnote"/> with the footnote marker set to the
		/// same marker. Note that this method does <em>not</em> insert any paragraphs into the
		/// footnote (the given style is only used to determine what kind of footnote it will
		/// be -- it is not actually applied.
		/// </summary>
		/// <param name="book">The book that the footnote is being added to</param>
		/// <param name="styleId">The styleId that tells us whether this is a general footnote
		/// or a cross-reference</param>
		/// <param name="iInsertAt">Zero-based index of the position in the sequence of
		/// footnotes where the new footnote is to be inserted</param>
		/// <param name="tsStrBldr">String builder for the paragraph being built</param>
		/// <param name="ich">Character index in paragraph where footnote is to be inserted
		/// </param>
		/// <returns>The newly created Footnote object</returns>
		/// ------------------------------------------------------------------------------------
		public static ScrFootnote InsertFootnoteAt(IScrBook book, string styleId, int iInsertAt,
			ITsStrBldr tsStrBldr, int ich)
		{
			// If the marker should be calculated, just insert an 'a' as a placeholder. It
			// will be changed in the RecalculateFootnoteMarkers() method.
			string sMarker = book.Cache.LangProject.TranslatedScriptureOA.GetFootnoteMarker(styleId);
			return InsertFootnoteAt(book, iInsertAt, tsStrBldr, ich, sMarker);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a footnote by inserting the footnote marker into the given string builder
		/// and creating a new <see cref="StFootnote"/> with the footnote marker set to the
		/// same marker. This is the real workhorse, used mainly for internal implementation,
		/// but it's public so import can use it to create footnotes more efficiently.
		/// </summary>
		/// <param name="book">The book that the footnote is being added to</param>
		/// <param name="iInsertAt">Zero-based index of the position in the sequence of
		/// footnotes where the new footnote is to be inserted</param>
		/// <param name="tsStrBldr">String builder for the paragraph being built</param>
		/// <param name="ich">Character index in paragraph where footnote is to be inserted
		/// </param>
		/// <param name="sMarker">The marker to use (more or less)</param>
		/// <returns>The newly created Footnote object</returns>
		/// ------------------------------------------------------------------------------------
		public static ScrFootnote InsertFootnoteAt(IScrBook book, int iInsertAt,
			ITsStrBldr tsStrBldr, int ich, string sMarker)
		{
			FdoCache cache = book.Cache;
			// TODO (TimS): When we have a place in the DB to store the owning para of a footnote
			// we need to pass in the para and then store it in the footnote (TE-2885)
			System.Diagnostics.Debug.Assert(book != null);
			System.Diagnostics.Debug.Assert(tsStrBldr.Length >= ich);
			IScripture scr = cache.LangProject.TranslatedScriptureOA;

			// Determine the appropriate writing system for the marker
			int ws;
			if (tsStrBldr.Length > 0)
			{
				int nDummy;
				// Use the writing system of the run where the footnote marker
				// is being inserted.
				ws = (tsStrBldr.get_PropertiesAt(ich)).GetIntPropValues(
					(int)FwTextPropType.ktptWs, out nDummy);
			}
			else
			{
				// Otherwise, use the default vernacular writing system. This will only
				// happen when a footnote is being inserted at the beginning of an empty
				// string. It's unlikely, but we deal with it.
				ws = cache.LangProject.DefaultVernacularWritingSystem;
			}

			ScrFootnote footnote = CreateScrFootnote(book, iInsertAt, ref sMarker, cache, ws);

			// Insert the owning ORC into the paragraph
			footnote.InsertOwningORCIntoPara(tsStrBldr, ich, ws);

			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the ScrFootnote in the given owning ScrBook.
		/// The caller must take care of inserting the proper ORC and the footnote's paragraph.
		/// </summary>
		/// <param name="book">The book that the footnote is being added to</param>
		/// <param name="iInsertAt">Zero-based index of the position in the book's sequence of
		/// footnotes where the new footnote is to be inserted</param>
		/// <param name="sMarker">ref: The marker to use (more or less)</param>
		/// <param name="cache">The cache.</param>
		/// <param name="ws">The writing system for the footnote marker.</param>
		/// <returns>The newly created Footnote object</returns>
		/// ------------------------------------------------------------------------------------
		private static ScrFootnote CreateScrFootnote(IScrBook book, int iInsertAt, ref string sMarker,
			FdoCache cache, int ws)
		{
			// Create StFootnote object and establish it in the database
			StFootnote foot = new StFootnote();
			book.FootnotesOS.InsertAt(foot, iInsertAt);
			// Construct a ScrFootnote so that its internal variables will get set correctly
			ScrFootnote footnote = new ScrFootnote(cache, foot.Hvo);

			// Create a default FootnoteMarker property in the StFootnote with
			// the correct text properties
			ITsTextProps markerProps =
				StyleUtils.CharStyleTextProps(ScrStyleNames.FootnoteMarker, ws);
			ITsStrBldr tsStrBldrFootnoteMkr = TsStrBldrClass.Create();

			// Mark the footnote marker style as being in use.
			IScripture scr = cache.LangProject.TranslatedScriptureOA;
			IStStyle markerStyle = scr.FindStyle(ScrStyleNames.FootnoteMarker);
			markerStyle.InUse = true;

			if (sMarker == null)
				sMarker = string.Empty;
			tsStrBldrFootnoteMkr.Replace(0, 0, sMarker, markerProps);
			footnote.FootnoteMarker = tsStrBldrFootnoteMkr.GetString();

			// Copy current value of default footnote options to the new footnote.
			footnote.DisplayFootnoteReference = scr.DisplayFootnoteReference;
			footnote.DisplayFootnoteMarker = scr.DisplayFootnoteMarker;
			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the next footnote and returns it. <paramref name="iSection"/>,
		/// <paramref name="iParagraph"/>, <paramref name="ich"/> and <paramref name="tag"/>
		/// reflect the position after the next footnote marker. If no footnote can be found
		/// <paramref name="iSection"/>, <paramref name="iParagraph"/>, <paramref name="ich"/>
		/// and <paramref name="tag"/> won't change.
		/// </summary>
		/// <param name="fcCache">FDO cache</param>
		/// <param name="book">Current book</param>
		/// <param name="iSection">Index of section to start search</param>
		/// <param name="iParagraph">Index of paragraph to start search</param>
		/// <param name="ich">Character index to start search</param>
		/// <param name="tag">Flid to start search</param>
		/// <returns>Next footnote, or <c>null</c> if there isn't a next footnote in the
		/// current book.</returns>
		/// ------------------------------------------------------------------------------------
		public static ScrFootnote FindNextFootnote(FdoCache fcCache,
			IScrBook book, ref int iSection, ref int iParagraph, ref int ich, ref int tag)
		{
			return FindNextFootnote(fcCache, book, ref iSection, ref iParagraph, ref ich,
				ref tag, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the next footnote and returns it. <paramref name="iSection"/>,
		/// <paramref name="iParagraph"/>, <paramref name="ich"/> and <paramref name="tag"/>
		/// reflect the position after the next footnote marker. If no footnote can be found
		/// <paramref name="iSection"/>, <paramref name="iParagraph"/>, <paramref name="ich"/>
		/// and <paramref name="tag"/> won't change.
		/// </summary>
		/// <param name="fcCache">FDO cache</param>
		/// <param name="book">Current book</param>
		/// <param name="iSection">Index of section to start search</param>
		/// <param name="iParagraph">Index of paragraph to start search</param>
		/// <param name="ich">Character index to start search</param>
		/// <param name="tag">Flid to start search</param>
		/// <param name="fSkipCurrentPos"><c>true</c> to start search with run after ich,
		/// <c>false</c> to start with current run.</param>
		/// <returns>Next footnote, or <c>null</c> if there isn't a next footnote in the
		/// current book.</returns>
		/// ------------------------------------------------------------------------------------
		public static ScrFootnote FindNextFootnote(FdoCache fcCache,
			IScrBook book, ref int iSection, ref int iParagraph, ref int ich, ref int tag,
			bool fSkipCurrentPos)
		{
			// Don't bother looking if book has no footnotes.
			if (book.FootnotesOS.Count == 0)
				return null;

			ScrFootnote footnote = null;

			int iSectionTmp = iSection;
			int iParagraphTmp = iParagraph;
			int ichTmp = ich;
			int tagTmp = tag;

			if (tagTmp == (int)ScrBook.ScrBookTags.kflidTitle)
			{
				footnote = FindNextFootnoteInText(book.TitleOA, ref iParagraphTmp, ref ichTmp,
					fSkipCurrentPos);
				if (footnote == null)
				{
					iSectionTmp = 0;
					iParagraphTmp = 0;
					ichTmp = 0;
					tagTmp = (int)ScrSection.ScrSectionTags.kflidHeading;
					fSkipCurrentPos = false;
				}
			}

			FdoSequence<IScrSection> sections = book.SectionsOS;
			IScrSection section = null;
			if (footnote == null)
				section = new ScrSection(fcCache, sections.HvoArray[iSectionTmp]);

			if (tagTmp == (int)ScrSection.ScrSectionTags.kflidHeading)
			{
				footnote = FindNextFootnoteInText(section.HeadingOA, ref iParagraphTmp, ref ichTmp,
					fSkipCurrentPos);
				if (footnote == null)
				{
					iParagraphTmp = 0;
					ichTmp = 0;
					tagTmp = (int)ScrSection.ScrSectionTags.kflidContent;
					fSkipCurrentPos = false;
				}
			}

			if (tagTmp == (int)ScrSection.ScrSectionTags.kflidContent)
			{
				footnote = FindNextFootnoteInText(section.ContentOA, ref iParagraphTmp, ref ichTmp,
					fSkipCurrentPos);
			}

			while (footnote == null && iSectionTmp < sections.Count - 1)
			{
				iSectionTmp++;
				section = new ScrSection(fcCache, sections.HvoArray[iSectionTmp]);
				footnote = FindFirstFootnoteInSection(section, out iParagraphTmp, out ichTmp,
					out tagTmp);
			}

			if (footnote != null)
			{
				iSection = iSectionTmp;
				iParagraph = iParagraphTmp;
				ich = ichTmp;
				tag = tagTmp;
			}
			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnote at the specified position.
		/// </summary>
		/// <param name="cache">FDO cache</param>
		/// <param name="book">The book to search for footnote</param>
		/// <param name="iSection">Index of section.</param>
		/// <param name="iParagraph">Index of paragraph.</param>
		/// <param name="ich">Character position.</param>
		/// <param name="tag">Tag</param>
		/// <returns>Footnote at specified position, or <c>null</c> if specified position is
		/// not in front of a footnote marker run.</returns>
		/// ------------------------------------------------------------------------------------
		public static ScrFootnote FindCurrentFootnote(FdoCache cache,
			IScrBook book, int iSection, int iParagraph, int ich, int tag)
		{
			// Don't bother looking if book has no footnotes.
			if (book.FootnotesOS.Count == 0)
				return null;

			IStTxtPara para = null;
			if (tag == (int)ScrBook.ScrBookTags.kflidTitle)
				para = (IStTxtPara)book.TitleOA.ParagraphsOS[iParagraph];
			else
			{
				IScrSection section = new ScrSection(cache, book.SectionsOS.HvoArray[iSection]);
				if (tag == (int)ScrSection.ScrSectionTags.kflidHeading)
					para = (StTxtPara)section.HeadingOA.ParagraphsOS[iParagraph];
				else if (tag == (int)ScrSection.ScrSectionTags.kflidContent)
					para = (StTxtPara)section.ContentOA.ParagraphsOS[iParagraph];
				else
				{
					Debug.Assert(false, "Unexpected tag");
					return null;
				}
			}

			ITsString tss = para.Contents.UnderlyingTsString;
			Debug.Assert(tss.Length >= ich);
			ITsTextProps tprops = tss.get_PropertiesAt(ich);
			int footnoteHvo = GetFootnoteFromProps(cache, tprops);

			if (footnoteHvo > 0)
				return new ScrFootnote(cache, footnoteHvo);
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the previous footnote and returns it.
		/// </summary>
		/// <param name="para">Paragraph to start search</param>
		/// <param name="ich">Character index to start search, or -1 to start at the end of
		/// the paragraph.</param>
		/// <returns>Previous footnote, or <c>null</c> if there isn't a previous footnote in the
		/// current book.</returns>
		/// ------------------------------------------------------------------------------------
		public static ScrFootnote FindPreviousFootnote(IStTxtPara para, int ich)
		{
			FdoCache cache = para.Cache;
			IScrBook book;
			IStText text = new StText(cache, para.OwnerHVO);
			int iPara = Array.IndexOf(text.ParagraphsOS.HvoArray, para.Hvo);
			int flid = text.OwningFlid;
			if (flid == (int)ScrSection.ScrSectionTags.kflidHeading ||
				flid == (int)ScrSection.ScrSectionTags.kflidContent)
			{
				ScrSection section = new ScrSection(cache, text.OwnerHVO);
				int iSection = section.IndexInBook;
				return ScrFootnote.FindPreviousFootnote(cache, section.OwningBook,
					ref iSection, ref iPara, ref ich, ref flid);
			}
			else if (flid == (int)ScrBook.ScrBookTags.kflidTitle)
			{
				book = new ScrBook(cache, text.OwnerHVO);
				return ScrFootnote.FindPreviousFootnoteInText(text, ref iPara,
					ref ich, false);
			}
			else
				throw new Exception("Can only create footnotes in Scripture Book titles, section heads, and contents");
		}

		// I added this and then ended up not needing it, but I'll keep it her commented out in
		// case it proves useful later.
		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Finds the next footnote and returns it.
		///// </summary>
		///// <param name="para">Paragraph to start search</param>
		///// <param name="ich">Character index to start search, or -1 to start at the end of
		///// the paragraph.</param>
		///// <returns>Next footnote, or <c>null</c> if there isn't a previous footnote in the
		///// current book.</returns>
		///// ------------------------------------------------------------------------------------
		//public static ScrFootnote FindNextFootnote(IStTxtPara para, int ich)
		//{
		//    FdoCache cache = para.Cache;
		//    IScrBook book;
		//    IStText text = new StText(cache, para.OwnerHVO);
		//    int iPara = Array.IndexOf(text.ParagraphsOS.HvoArray, para.Hvo);
		//    int flid = text.OwningFlid;
		//    if (flid == (int)ScrSection.ScrSectionTags.kflidHeading ||
		//        flid == (int)ScrSection.ScrSectionTags.kflidContent)
		//    {
		//        IScrSection section = new ScrSection(cache, text.OwnerHVO);
		//        book = new ScrBook(cache, section.OwnerHVO);
		//        int iSection = Array.IndexOf(book.SectionsOS.HvoArray, section.Hvo);
		//        return ScrFootnote.FindNextFootnote(cache,
		//            book, ref iSection, ref iPara, ref ich, ref flid);
		//    }
		//    else if (flid == (int)ScrBook.ScrBookTags.kflidTitle)
		//    {
		//        book = new ScrBook(cache, text.OwnerHVO);
		//        return ScrFootnote.FindNextFootnoteInText(text, ref iPara,
		//            ref ich, false);
		//    }
		//    else
		//        throw new Exception("Can only create footnotes in Scripture Book titles, section heads, and contents");
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the previous footnote and returns it. <paramref name="iSection"/>,
		/// <paramref name="iParagraph"/>, <paramref name="ich"/> and <paramref name="tag"/>
		/// reflect the position before the previous footnote marker. If no footnote can be found
		/// <paramref name="iSection"/>, <paramref name="iParagraph"/>, <paramref name="ich"/>
		/// and <paramref name="tag"/> won't change.
		/// </summary>
		/// <param name="fcCache">FDO cache</param>
		/// <param name="book">Current book</param>
		/// <param name="iSection">Index of section to start search</param>
		/// <param name="iParagraph">Index of paragraph to start search, or -1 to start search
		/// in last paragraph.</param>
		/// <param name="ich">Character index to start search, or -1 to start at the end of
		/// the paragraph.</param>
		/// <param name="tag">Flid to start search</param>
		/// <returns>Previous footnote, or <c>null</c> if there isn't a previous footnote in the
		/// current book.</returns>
		/// ------------------------------------------------------------------------------------
		public static ScrFootnote FindPreviousFootnote(FdoCache fcCache,
			IScrBook book, ref int iSection, ref int iParagraph, ref int ich, ref int tag)
		{
			return FindPreviousFootnote(fcCache, book, ref iSection, ref iParagraph, ref ich,
				ref tag, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the previous footnote and returns it. <paramref name="iSection"/>,
		/// <paramref name="iParagraph"/>, <paramref name="ich"/> and <paramref name="tag"/>
		/// reflect the position before the previous footnote marker. If no footnote can be found
		/// <paramref name="iSection"/>, <paramref name="iParagraph"/>, <paramref name="ich"/>
		/// and <paramref name="tag"/> won't change.
		/// </summary>
		/// <param name="fcCache">FDO cache</param>
		/// <param name="book">Current book</param>
		/// <param name="iSection">Index of section to start search</param>
		/// <param name="iParagraph">Index of paragraph to start search, or -1 to start search
		/// in last paragraph.</param>
		/// <param name="ich">Character index to start search, or -1 to start at the end of
		/// the paragraph.</param>
		/// <param name="tag">Flid to start search</param>
		/// <param name="fSkipFirstRun"><c>true</c> if the current run where ich is in should
		/// be skipped, otherwise <c>false</c>.</param>
		/// <returns>Previous footnote, or <c>null</c> if there isn't a previous footnote in the
		/// current book.</returns>
		/// ------------------------------------------------------------------------------------
		public static ScrFootnote FindPreviousFootnote(FdoCache fcCache,
			IScrBook book, ref int iSection, ref int iParagraph, ref int ich, ref int tag,
			bool fSkipFirstRun)
		{
			// Don't bother looking if book has no footnotes.
			if (book.FootnotesOS.Count == 0)
				return null;

			ScrFootnote footnote = null;
			FdoOwningSequence<IScrSection> sections = book.SectionsOS;
			IScrSection section = null;
			if (tag != (int)ScrBook.ScrBookTags.kflidTitle)
				section = new ScrSection(fcCache, sections.HvoArray[iSection]);

			int iSectionTmp = iSection;
			int iParagraphTmp = iParagraph;
			int ichTmp = ich;
			int tagTmp = tag;

			if (tagTmp == (int)ScrSection.ScrSectionTags.kflidContent)
			{
				footnote = FindPreviousFootnoteInText(section.ContentOA, ref iParagraphTmp,
					ref ichTmp, fSkipFirstRun);
				if (footnote == null)
				{
					tagTmp = (int)ScrSection.ScrSectionTags.kflidHeading;
					iParagraphTmp = -1;
					ichTmp = -1;
					fSkipFirstRun = false;
				}
			}

			if (tagTmp == (int)ScrSection.ScrSectionTags.kflidHeading)
			{
				footnote = FindPreviousFootnoteInText(section.HeadingOA, ref iParagraphTmp,
					ref ichTmp, fSkipFirstRun);
				while (footnote == null && iSectionTmp > 0)
				{
					iSectionTmp--;
					section = new ScrSection(fcCache, sections.HvoArray[iSectionTmp]);
					footnote = FindLastFootnoteInSection(section, out iParagraphTmp, out ichTmp, out tagTmp);
				}

				if (footnote == null)
				{
					iSectionTmp = 0;
					iParagraphTmp = -1;
					ichTmp = -1;
					fSkipFirstRun = false;
				}
			}

			if (footnote == null && book.TitleOA != null)
			{
				tagTmp = (int)ScrBook.ScrBookTags.kflidTitle;
				footnote = FindPreviousFootnoteInText(book.TitleOA, ref iParagraphTmp, ref ichTmp,
					fSkipFirstRun);
			}

			if (footnote != null)
			{
				iSection = iSectionTmp;
				iParagraph = iParagraphTmp;
				ich = ichTmp;
				tag = tagTmp;
			}
			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches for first footnote reference in the StText.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="iPara"></param>
		/// <param name="ich"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected static ScrFootnote FindFirstFootnoteInText(IStText text, out int iPara, out int ich)
		{
			iPara = ich = 0;
			FdoSequence<IStPara> paragraphs = text.ParagraphsOS;
			if (paragraphs.Count == 0)
				return null;
			if (new StTxtPara(text.Cache, paragraphs.HvoArray[iPara]).Contents.Text != null)
				return FindNextFootnoteInText(text, ref iPara, ref ich, false);
			else
				return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches for last footnote reference in the StText.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="iPara"></param>
		/// <param name="ich"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected static ScrFootnote FindLastFootnoteInText(IStText text, out int iPara, out int ich)
		{
			FdoCache cache = text.Cache;
			ich = -1;
			FdoSequence<IStPara> paragraphs = text.ParagraphsOS;
			iPara = paragraphs.Count;
			while (--iPara >= 0 &&
				new StTxtPara(cache, paragraphs.HvoArray[iPara]).Contents.Text == null)
			{
			}
			if (iPara < 0)
				return null;
			ich = new StTxtPara(cache, paragraphs.HvoArray[iPara]).Contents.Length;
			return FindPreviousFootnoteInText(text, ref iPara, ref ich, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the next footnote in the section.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="iParagraph">Index of paragraph to start search.</param>
		/// <param name="ich">Character index to start search.</param>
		/// <param name="fSkipCurrentPosition">If <c>true</c> we search forwards starting with the
		/// run after ich, otherwise we start with the current run.</param>
		/// <returns>Next footnote in string, or <c>null</c> if footnote can't be found.</returns>
		/// ------------------------------------------------------------------------------------
		public static ScrFootnote FindNextFootnoteInText(IStText text, ref int iParagraph,
			ref int ich, bool fSkipCurrentPosition)
		{
			FdoSequence<IStPara> paragraphs = text.ParagraphsOS;
			if (paragraphs.Count == 0)
				return null;
			IStTxtPara para = new StTxtPara(text.Cache, paragraphs.HvoArray[iParagraph]);
			ITsString tss = para.Contents.UnderlyingTsString;
			ScrFootnote footnote = FindFirstFootnoteInString(text.Cache, tss, ref ich,
				fSkipCurrentPosition);
			while (footnote == null && iParagraph < paragraphs.Count - 1)
			{
				iParagraph++;
				ich = 0;
				para = new StTxtPara(text.Cache, paragraphs.HvoArray[iParagraph]);
				tss = para.Contents.UnderlyingTsString;
				footnote = FindFirstFootnoteInString(text.Cache, tss, ref ich, false);
			}
			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="text"></param>
		/// <param name="iParagraph">Index of paragraph to start search, or -1 to start search
		/// in last paragraph.</param>
		/// <param name="ich">Character index to start search, or -1 to start at the end of
		/// the paragraph.</param>
		/// <param name="fSkipCurrentPosition">If <c>true</c> we search backwards starting with the
		/// run before ich, otherwise we start with the run ich is in.</param>
		/// <returns>Last footnote in string, or <c>null</c> if footnote can't be found.</returns>
		/// ------------------------------------------------------------------------------------
		public static ScrFootnote FindPreviousFootnoteInText(IStText text, ref int iParagraph,
			ref int ich, bool fSkipCurrentPosition)
		{
			FdoSequence<IStPara> paragraphs = text.ParagraphsOS;
			if (paragraphs.Count == 0)
				return null;
			if (iParagraph == -1)
				iParagraph = paragraphs.Count - 1;
			IStTxtPara para = new StTxtPara(text.Cache, paragraphs.HvoArray[iParagraph]);
			ITsString tss = para.Contents.UnderlyingTsString;
			ScrFootnote footnote = FindLastFootnoteInString(text.Cache, tss, ref ich,
				fSkipCurrentPosition);
			while (footnote == null && iParagraph > 0)
			{
				iParagraph--;
				para = new StTxtPara(text.Cache, paragraphs.HvoArray[iParagraph]);
				tss = para.Contents.UnderlyingTsString;
				ich = tss.Length;
				footnote = FindLastFootnoteInString(text.Cache, tss, ref ich, false);
			}
			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches section forwards for first footnote reference.
		/// </summary>
		/// <param name="section"></param>
		/// <param name="iPara">out: The index of the para containing the footnote.</param>
		/// <param name="ich">out: The character index where the footnote was found.</param>
		/// <param name="tag">out: whether the footnote was found in heading or contents</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ScrFootnote FindFirstFootnoteInSection(IScrSection section, out int iPara,
			out int ich, out int tag)
		{
			tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			ScrFootnote footnote = FindFirstFootnoteInText(section.HeadingOA, out iPara, out ich);
			if (footnote == null)
			{
				tag = (int)ScrSection.ScrSectionTags.kflidContent;
				footnote = FindFirstFootnoteInText(section.ContentOA, out iPara, out ich);
			}
			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches section backwards for last footnote reference.
		/// </summary>
		/// <param name="section"></param>
		/// <param name="iPara">out: The index of the para containing the footnote.</param>
		/// <param name="ich">out: The character index where the footnote was found.</param>
		/// <param name="tag">out: whether the footnote was found in heading or contents</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ScrFootnote FindLastFootnoteInSection(IScrSection section, out int iPara,
			out int ich, out int tag)
		{
			tag = (int)ScrSection.ScrSectionTags.kflidContent;
			ScrFootnote footnote = FindLastFootnoteInText(section.ContentOA, out iPara, out ich);
			if (footnote == null)
			{
				tag = (int)ScrSection.ScrSectionTags.kflidHeading;
				footnote = FindLastFootnoteInText(section.HeadingOA, out iPara, out ich);
			}
			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches section forwards for first footnote reference in a section range.
		/// </summary>
		/// <param name="sectionRange">The section range.</param>
		/// <param name="ihvoSectionStart">The index of the starting section in the section hvo array</param>
		/// <param name="ihvoSectionEnd">The index of the ending section in the section hvo array</param>
		/// <returns>
		/// first footnote in a range of sections or null if not found
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static ScrFootnote FindFirstFootnoteInSectionRange(
			FdoOwningSequence<IScrSection> sectionRange, int ihvoSectionStart, int ihvoSectionEnd)
		{
			ScrFootnote footnote;
			int iPara;
			int ich;
			int tag;
			for (int iSection = ihvoSectionStart; iSection <= ihvoSectionEnd; iSection++)
			{
				ScrSection section = (ScrSection)sectionRange[iSection];
				footnote = FindFirstFootnoteInSection(section, out iPara, out ich, out tag);
				if (footnote != null)
					return footnote;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches section backwards for last footnote reference in a section range.
		/// </summary>
		/// <param name="sectionRange">The section range.</param>
		/// <param name="ihvoSectionStart">The index of the starting section in the section hvo array</param>
		/// <param name="ihvoSectionEnd">The index of the ending section in the section hvo array</param>
		/// <returns>last footnote in a range of sections or null if not found</returns>
		/// ------------------------------------------------------------------------------------
		public static ScrFootnote FindLastFootnoteInSectionRange(
			FdoOwningSequence<IScrSection> sectionRange, int ihvoSectionStart, int ihvoSectionEnd)
		{
			ScrFootnote footnote;
			int iPara;
			int ich;
			int tag;
			for (int iSection = ihvoSectionEnd; iSection >= ihvoSectionStart; iSection--)
			{
				ScrSection section = (ScrSection)sectionRange[iSection];
				footnote = FindLastFootnoteInSection(section, out iPara, out ich, out tag);
				if (footnote != null)
					return footnote;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches TsString forwards for first footnote reference.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="tss"></param>
		/// <param name="ich">Character index to start search.</param>
		/// <param name="fSkipCurrentPosition">If <c>true</c> we search forward starting with the
		/// run after ich, otherwise we start with the current run.</param>
		/// <returns>First footnote in string after ich, or <c>null</c> if footnote can't be found.</returns>
		/// ------------------------------------------------------------------------------------
		public static ScrFootnote FindFirstFootnoteInString(FdoCache cache, ITsString tss,
			ref int ich, bool fSkipCurrentPosition)
		{
			int irun = tss.get_RunAt(ich);
			int runCount = tss.RunCount;

			ITsTextProps tprops;
			int footnoteHvo;

			if (fSkipCurrentPosition)
				irun++;

			while (irun < runCount)
			{
				tprops = tss.get_Properties(irun);
				footnoteHvo = GetFootnoteFromProps(cache, tprops);
				if (footnoteHvo > 0)
				{
					ich = tss.get_LimOfRun(irun);
					return new ScrFootnote(cache, footnoteHvo);
				}
				irun++;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches TsString backwards for last footnote reference.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="tss"></param>
		/// <param name="ich">Character index to start search, or -1 to start at the end of
		/// the string.</param>
		/// <param name="fSkipCurrentPosition">If <c>true</c> we search backwards starting with the
		/// run before ich, otherwise we start with the run ich is in.</param>
		/// <returns>Last footnote in string, or <c>null</c> if footnote can't be found.</returns>
		/// ------------------------------------------------------------------------------------
		public static ScrFootnote FindLastFootnoteInString(FdoCache cache, ITsString tss,
			ref int ich, bool fSkipCurrentPosition)
		{
			if (ich == -1)
			{
				fSkipCurrentPosition = false;
				ich = tss.Length;
			}
			ITsTextProps tprops = (fSkipCurrentPosition ? null : tss.get_PropertiesAt(ich));
			int footnoteHvo = (fSkipCurrentPosition ? 0 : GetFootnoteFromProps(cache, tprops));
			//TODO: TE-4199 if ich beyond the text, we must fix this so we do not skip the last run
			int irun = tss.get_RunAt(ich);
			while (footnoteHvo <= 0 && irun > 0)
			{
				irun--;
				tprops = tss.get_Properties(irun);
				footnoteHvo = GetFootnoteFromProps(cache, tprops);
			}
			ich = tss.get_MinOfRun(irun);
			return footnoteHvo <= 0 ? null : new ScrFootnote(cache, footnoteHvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets footnote hvo from reference in text properties
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="tprops"></param>
		/// <returns>Hvo of the footnote that was found or 0 if none was found</returns>
		/// ------------------------------------------------------------------------------------
		protected static int GetFootnoteFromProps(FdoCache cache, ITsTextProps tprops)
		{
			string footnoteRef =
				tprops.GetStrPropValue((int)FwTextPropType.ktptObjData);

			if (footnoteRef != null)
			{
				// first char. of strData is type code - GUID will follow it.
				Guid objGuid = MiscUtils.GetGuidFromObjData(footnoteRef.Substring(1));

				int hvo = cache.GetIdFromGuid(objGuid);
				if (hvo > 0 && cache.GetClassOfObject(hvo) == StFootnote.kClassId)
					return hvo;
			}
			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compute and cache the references for all footnotes in the given text.
		/// </summary>
		/// <param name="text">The text whose footnote refs are to be computed</param>
		/// <param name="footnoteRefs">Dictionary of footnote HVOs to a structure containing
		/// Scripture references and owning paragraph's HVO</param>
		/// <param name="footnoteRef">Caller should pass in the initial reference to use as the
		/// basis for any references found in the course of parsing the text. Returned value
		/// will be the final reference found, which can be used as the basis for the subsequent
		/// text</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal static void AddFootnoteRefsInText(IStText text,
			Dictionary<int, FootnoteHashEntry> footnoteRefs, ref BCVRef footnoteRef)
		{
			if (text == null)
				return;

			FdoCache cache = text.Cache;
			BCVRef startRef = new BCVRef(footnoteRef);
			BCVRef endRef = new BCVRef(footnoteRef);

			foreach (IStTxtPara para in text.ParagraphsOS)
			{
				// search each run for chapter numbers, verse numbers, or footnote markers
				ITsString tssContents = para.Contents.UnderlyingTsString;
				string sPara = tssContents.Text;
				if (sPara == null)
					continue;

				int nRun = tssContents.RunCount;
				for (int i = 0; i < nRun; i++)
				{
					TsRunInfo runInfo;
					ITsTextProps tprops = tssContents.FetchRunInfo(i, out runInfo);
					string styleName = tprops.GetStrPropValue(
						(int)FwTextPropType.ktptNamedStyle);

					// When a verse number is encountered, save the number into
					// the reference.
					if (styleName == ScrStyleNames.VerseNumber)
					{
						string sVerseNum = sPara.Substring(runInfo.ichMin,
							runInfo.ichLim - runInfo.ichMin);
						int nVerseStart, nVerseEnd;
						ScrReference.VerseToInt(sVerseNum, out nVerseStart, out nVerseEnd);
						startRef.Verse = nVerseStart;
						endRef.Verse = nVerseEnd;
					}

					// If a chapter number is encountered then save the number into
					// the reference and start the verse number back at 1.
					else if (styleName == ScrStyleNames.ChapterNumber)
					{
						try
						{
							string sChapterNum = sPara.Substring(runInfo.ichMin,
								runInfo.ichLim - runInfo.ichMin);
							startRef.Chapter = endRef.Chapter = ScrReference.ChapterToInt(sChapterNum);
							startRef.Verse = endRef.Verse = 1;
						}
						catch (ArgumentException)
						{
							// ignore runs with invalid Chapter numbers
						}
					}

					// If the run is a footnote reference then store the Scripture
					// reference with the footnote hvo in the hash table.
					else if (styleName == null)
					{
						int footnoteHvo = GetFootnoteFromProps(text.Cache, tprops);
						if (footnoteHvo > 0)
							CacheFootnoteHashEntry(footnoteRefs, footnoteHvo, startRef, endRef, para);
					}
				}
			}
			footnoteRef = endRef;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Stores or updates the information about the current footnote in the dictionary that
		/// stores paragraph and reference information about footnotes.
		/// </summary>
		/// <param name="footnoteRefs">The dictionary that stores paragraph and reference
		/// information about footnotes</param>
		/// <param name="footnoteHvo">The ID of the footnote (key to the dictionary)</param>
		/// <param name="startRef">Scripture reference for the footnote</param>
		/// <param name="endRef">Scripture reference for the footnote (can differ from the start
		/// reference in the case of a verse bridge)</param>
		/// <param name="para">Paragraph that "owns" the footnote.</param>
		/// ------------------------------------------------------------------------------------
		private static void CacheFootnoteHashEntry(Dictionary<int, FootnoteHashEntry> footnoteRefs,
			int footnoteHvo, BCVRef startRef, BCVRef endRef, IStTxtPara para)
		{
			// If footnote is in table, check to see if reference has
			// changed.
			bool doPropChange = false;
			bool insertedRef = false;

			if (footnoteRefs.ContainsKey(footnoteHvo))
			{
				FootnoteHashEntry existingEntry = footnoteRefs[footnoteHvo];
				existingEntry.Found = true;
				doPropChange = (existingEntry.StartRef != startRef ||
					existingEntry.EndRef != endRef);
				insertedRef = doPropChange;
			}
			else
				insertedRef = true;

			if (insertedRef)
			{
				footnoteRefs[footnoteHvo] = new FootnoteHashEntry(startRef, endRef, para.Hvo);
				// Reference changed - need to refresh footnote on screen.
				if (doPropChange)
					para.Cache.PropChanged(null, PropChangeType.kpctNotifyAll,
						footnoteHvo, StFootnote.ktagFootnoteOptions, 0, 1, 1);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hashtable of footnote HVO's to FootnoteHashEntry's.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private Dictionary<int, FootnoteHashEntry> FootnoteRefHashtable
		{
			get
			{
				return ((ScrBook)Owner).FootnoteRefHashtable;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the FootnoteHashEntry containing meta-info for the footnote (para HVO and
		/// Scripture refs)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private FootnoteHashEntry FootnoteRefInfo
		{
			get
			{
				// Attempt to get info for footnote
				Dictionary<int, FootnoteHashEntry> footnoteRefs = FootnoteRefHashtable;

				// Rebuild dictionary for book if reference for footnote is not found
				if (!footnoteRefs.ContainsKey(Hvo))
					new ScrBook(Cache, OwnerHVO).RefreshFootnoteRefs(footnoteRefs);

				// It is guaranteed to have it by now, as RefreshFootnoteRefs will add it, if missing.
				// However, in some cases a footnote can still exist in the database, even though it is no longer
				// found in a paragraph property (LTB-408). In that case, we'll throw an exception here.
				return footnoteRefs[Hvo];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a string representing the reference of the footnote (chapter:verse)
		/// NOTE: This is formatted for the default vernacular writing system.
		/// </summary>
		/// <returns>The reference of this footnote, or an empty string if no paragraph was found.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public string RefAsString
		{
			get { return GetReference(m_cache.DefaultVernWs); }
		}

		/// <summary>
		/// This is used to get reference information irrespective of display settings.
		/// </summary>
		public bool IgnoreDisplaySettings
		{
			get { return m_fIgnoreDisplaySettings; }
			set { m_fIgnoreDisplaySettings = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the start ref.
		/// </summary>
		/// <value>The start ref.</value>
		/// ------------------------------------------------------------------------------------
		public ScrReference StartRef
		{
			get
			{
				FootnoteHashEntry footnoteEntry = FootnoteRefInfo;
				return new ScrReference(footnoteEntry.StartRef, m_scr.Versification);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the end ref.
		/// </summary>
		/// <value>The end ref.</value>
		/// ------------------------------------------------------------------------------------
		public ScrReference EndRef
		{
			get
			{
				FootnoteHashEntry footnoteEntry = FootnoteRefInfo;
				return new ScrReference(footnoteEntry.EndRef, m_scr.Versification);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the paragraph style name of the first paragraph in the footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ParaStyleId
		{
			get
			{
				return (ParagraphsOS.Count == 0) ? ScrStyleNames.NormalFootnoteParagraph :
					new ScrTxtPara(Cache, ParagraphsOS.HvoArray[0]).StyleName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a string representing the reference of the footnote (chapter:verse)
		/// </summary>
		/// <param name="hvoWs">HVO of the writing system for which we are formatting this
		/// reference.</param>
		/// <returns>reference in string format</returns>
		/// ------------------------------------------------------------------------------------
		public string GetReference(int hvoWs)
		{
			try
			{
				if (!IgnoreDisplaySettings && !m_scr.GetDisplayFootnoteReference(ParaStyleId))
					return string.Empty;
				FootnoteHashEntry footnoteEntry = FootnoteRefInfo;
				// Don't display the reference if the reference is for an intro
				if (footnoteEntry.StartRef.Verse == 0 && footnoteEntry.EndRef.Verse == 0)
					return string.Empty;

				return ((Scripture)m_scr).ChapterVerseBridgeAsString(footnoteEntry.StartRef,
					footnoteEntry.EndRef, hvoWs) + " ";
			}
			catch
			{
				// This can happen if the paragraph already got deleted, but the footnote
				// still hangs around. The caller is expected to handle a bogus reference
				// string.
				return string.Empty;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the id of the paragraph containing the footnote.
		/// </summary>
		/// <returns>The HVO of the paragraph that contains this footnote, or 0 if no paragraph
		/// was found.</returns>
		/// ------------------------------------------------------------------------------------
		public int ContainingParagraphHvo
		{
			get
			{
				try
				{
					return FootnoteRefInfo.HvoPara;
				}
				catch
				{
					// This can happen if the paragraph already got deleted, but the footnote
					// still hangs around. The caller is expected to handle a HVO of 0.
					return 0;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a flag indicating whether this footnote's ORC is still present in the
		/// vernacular text as of the latest scan. If not, the footnote is in the process of
		/// being deleted or has somehow gotten orphaned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool HasORCInVernacular
		{
			get
			{
				try
				{
					return FootnoteRefInfo.Found;
				}
				catch (Exception e)
				{
					// This can happen if the paragraph already got deleted, but the footnote
					// still hangs around.
					// Pasting a footnote temporarily puts an orphaned footnote into the collection
					// and the PropChanged gets issued for it before the ORC in the text is created,
					// so in that case, we don't report the orphaned footnote. TE-8048
					// Restoring a footnote from a saved version has the same problem as pasting one.
					if (!Environment.StackTrace.Contains("MakeObjFromText") &&
						!Environment.StackTrace.Contains("Paste") &&
						!Environment.StackTrace.Contains("ReplaceCurrentWithRevision"))
					{
						Exception orphanedFootnote = new Exception("Orphaned footnote detected!", e);
#if DEBUG
						throw orphanedFootnote;
#else
						Logger.WriteError(orphanedFootnote);
#endif
					}
					return false;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the section that this footnote belongs to (if any).
		/// Returns 0 if it no longer is found in a paragraph text property (LTB-408),
		/// or if it doesn't belong to a section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool TryGetContainingSectionHvo(out int hvoSection)
		{
				hvoSection = 0;
				int hvoSectionText;
				if (TryGetContainingStText(out hvoSectionText))
				{
					int flidOwningStTxt = Cache.GetOwningFlidOfObject(hvoSectionText);
					if (flidOwningStTxt == (int)ScrSection.ScrSectionTags.kflidContent ||
						flidOwningStTxt == (int)ScrSection.ScrSectionTags.kflidHeading)
					{
						hvoSection = Cache.GetOwnerOfObject(hvoSectionText);
					}
				}
				return hvoSection != 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///	Return the title (StText) that this footnote belongs to (if any).
		/// Returns 0 if it no longer is found in a paragraph text property (LTB-408),
		/// or if it doesn't belong to a title.
		/// </summary>
		/// <param name="hvoTitle">StText title</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool TryGetContainingTitle(out int hvoTitle)
		{
			hvoTitle = 0;
			int hvoSectionText;
			if (TryGetContainingStText(out hvoSectionText))
			{
				int flidOwningStTxt = Cache.GetOwningFlidOfObject(hvoSectionText);
				if (flidOwningStTxt == (int)ScrBook.ScrBookTags.kflidTitle)
				{
					hvoTitle = hvoSectionText;
				}
			}
			return hvoTitle != 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to get the StText containing this footnote ORC
		/// </summary>
		/// <param name="hvoStText"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool TryGetContainingStText(out int hvoStText)
		{
			hvoStText = 0;
			int hvoPara = this.ContainingParagraphHvo;
			if (hvoPara == 0)
				return false;
			hvoStText = Cache.GetOwnerOfObject(hvoPara);
			return hvoStText != 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden because this property is dictated by the Scripture object for Scripture
		/// footnotes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool DisplayFootnoteMarker
		{
			get
			{
				return m_scr.GetDisplayFootnoteMarker(ParaStyleId);
			}
			set
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the footnote.
		/// </summary>
		/// <value>The type of the footnote.</value>
		/// ------------------------------------------------------------------------------------
		public FootnoteMarkerTypes FootnoteType
		{
			get
			{
				return m_scr.DetermineFootnoteMarkerType(ParaStyleId);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden because this property is dictated by the Scripture object for Scripture
		/// footnotes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool DisplayFootnoteReference
		{
			get
			{
				return m_scr.GetDisplayFootnoteReference(ParaStyleId);
			}
			set
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to return a TSString instead of a TSStringAccessor and also to defer
		/// some (like about 100%) of the logic to the Scripture object
		/// </summary>
		/// <remarks>The WS of the marker will be the default VERN writing system</remarks>
		/// ------------------------------------------------------------------------------------
		public new ITsString FootnoteMarker
		{
			get
			{
				ITsStrBldr bldr = MakeFootnoteMarker(m_cache.DefaultVernWs);
				return bldr.GetString();
			}
			set
			{
				// It is valid to set a non-letter footnote marker (for symbolic types)
				//Debug.Assert(char.IsLetter(value.Text[0]));
				base.FootnoteMarker.UnderlyingTsString = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the footnote marker.
		/// </summary>
		/// <param name="markerWS">The WS of the marker</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ITsStrBldr MakeFootnoteMarker(int markerWS)
		{
			// Get the marker
			string marker = string.Empty;
			if (FootnoteType == FootnoteMarkerTypes.AutoFootnoteMarker)
			{
				if (m_cache.MarkerIndexCache == null)
					m_cache.MarkerIndexCache = new FootnoteMarkerIndexCache(m_cache);
				int index = m_cache.GetFootnoteMarkerIndex(Hvo);
				marker = ((char)((int)'a' + (index % 26))).ToString();
			}
			else
				marker = m_scr.GetFootnoteMarker(ParaStyleId,
					base.FootnoteMarker.UnderlyingTsString).Text;

			// create a TsString to hold the marker
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, markerWS);
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				ScrStyleNames.FootnoteMarker);
			strBldr.Replace(0, 0, marker, propsBldr.GetTextProps());
			return strBldr;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes a footnote along with its marker in the text. This also recalculates the
		/// footnote markers of following footnotes.
		/// </summary>
		/// <param name="footnote"></param>
		/// ------------------------------------------------------------------------------------
		public static void DeleteFootnoteAndMarker(ScrFootnote footnote)
		{
			// Paragraph changes are not monitored by cache - refresh cache for book before
			// doing deletion
			IScrBook book = new ScrBook(footnote.Cache, footnote.OwnerHVO);
			// REVIEW: Probably not needed now??? ((ScrBook)book).RefreshFootnoteRefs();

			// Get paragraph containing footnote and search for run having footnote ref.
			int hvoPara = footnote.ContainingParagraphHvo;
			if (hvoPara != 0)
			{
				StTxtPara para = new StTxtPara(footnote.Cache, hvoPara);
				para.DeleteAnyBtMarkersForFootnote(footnote.Guid);
				ITsString tssContent = para.Contents.UnderlyingTsString;
				TsRunInfo runInfo = new TsRunInfo();
				int i;
				for (i = 0; i < tssContent.RunCount; i++)
				{
					ITsTextProps textProps = tssContent.FetchRunInfo(i, out runInfo);
					string strGuid =
						textProps.GetStrPropValue((int)FwTextPropType.ktptObjData);
					if (strGuid != null)
					{
						Guid guid = MiscUtils.GetGuidFromObjData(strGuid.Substring(1));
						if (footnote.Guid == guid)
							break;
					}
				}
				System.Diagnostics.Debug.Assert(i < tssContent.RunCount, "Footnote marker not found in text");

				// Remove footnote ref from paragraph - then delete the footnote
				ITsStrBldr bldr = tssContent.GetBldr();
				bldr.Replace(runInfo.ichMin, runInfo.ichLim, string.Empty, null);
				para.Contents.UnderlyingTsString = bldr.GetString();
			}
			DeleteFootnote(footnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Starts recalculation of footnotes for a book at the given footnote.
		/// </summary>
		/// <param name="footnote">footnote to where recalculation starts</param>
		/// ------------------------------------------------------------------------------------
		public static void RecalculateFootnoteMarkers(StFootnote footnote)
		{
			int index = footnote.IndexInOwner;
			Debug.Assert(index >= 0);
			RecalculateFootnoteMarkers(new ScrBook(footnote.Cache, footnote.OwnerHVO), index);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recalculates footnote markers for all footnotes in book starting at the given
		/// footnote index.
		/// </summary>
		/// <param name="book"></param>
		/// <param name="iStartAt"></param>
		/// ------------------------------------------------------------------------------------
		public static void RecalculateFootnoteMarkers(IScrBook book, int iStartAt)
		{
			FdoCache cache = book.Cache;
			IScripture scr = cache.LangProject.TranslatedScriptureOA;
			// Paragraph changes are not monitored by cache - refresh cache for book before
			// doing recalculation
			((ScrBook)book).RefreshFootnoteRefs();

			if (cache.MarkerIndexCache == null)
				cache.MarkerIndexCache = new FootnoteMarkerIndexCache(cache);

			int hvoPrevPara = 0;
			ScrFootnote footnote = null;
			FdoOwningSequence<IStFootnote> footnotes = book.FootnotesOS;
			for (int i = iStartAt; i < footnotes.Count; i++)
			{
				footnote = new ScrFootnote(cache, footnotes.HvoArray[i]);
				bool refreshFootnote =
					cache.MarkerIndexCache.GetDirtyFlagForFootnote(footnote.Hvo);

				if (refreshFootnote && footnote != null)
				{
					cache.MarkerIndexCache.ClearDirtyFlagForFootnote(footnote.Hvo);
					cache.PropChanged(null, PropChangeType.kpctNotifyAll,
						footnote.Hvo, (int)StFootnote.StFootnoteTags.kflidFootnoteMarker, 0, 1, 1);

					// Give a notification to the paragraph when all markers have been
					// updated so that it will display the updated footnote markers.
					if (footnote.HasORCInVernacular)
					{
						int hvoPara = footnote.ContainingParagraphHvo;
						if (hvoPara != hvoPrevPara && hvoPrevPara != 0)
						{
							using (new IgnorePropChanged(cache, PropChangedHandling.SuppressChangeWatcher))
							{
								cache.PropChanged(null, PropChangeType.kpctNotifyAll,
									hvoPrevPara, (int)StTxtPara.StTxtParaTags.kflidContents,
									0, 1, 1);
							}
						}
						hvoPrevPara = hvoPara;
					}
				}
			}
			// Give a notification for the last paragraph so that it will display
			// the updated footnote markers.
			if (hvoPrevPara != 0)
			{
				using (new IgnorePropChanged(cache, PropChangedHandling.SuppressChangeWatcher))
				{
					cache.PropChanged(null, PropChangeType.kpctNotifyAll,
						hvoPrevPara, (int)StTxtPara.StTxtParaTags.kflidContents, 0, 1, 1);
				}
			}
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Finds first auto generated footnote before starting footnote.
		///// </summary>
		///// <param name="scr"></param>
		///// <param name="book"></param>
		///// <param name="iStartAt">Index of added footnote</param>
		///// <returns></returns>
		///// ------------------------------------------------------------------------------------
		//public static string CalculateStartingFootnoteMarker(Scripture scr, ScrBook book,
		//    int iStartAt)
		//{
		//    int iPrevAutoFootnote = iStartAt - 1;
		//    string marker = null;
		//    FdoOwningSequence footnotes = book.FootnotesOS;
		//    while (iPrevAutoFootnote >= 0)
		//    {
		//        ScrFootnote footnote = new ScrFootnote(book.Cache, footnotes.HvoArray[iPrevAutoFootnote]);
		//        marker = footnote.FootnoteMarker.Text;
		//        if (scr.DetermineFootnoteMarkerType(footnote.ParaStyleId) ==
		//            FootnoteMarkerTypes.AutoFootnoteMarker)
		//        {
		//            break;
		//        }
		//        iPrevAutoFootnote--;
		//    }

		//    if (iPrevAutoFootnote < 0)
		//        return Scripture.kDefaultAutoFootnoteMarker;

		//    return CalculateNextFootnoteMarker(scr, marker);
		//}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// This is mainly for internal use, but it is public so Import can be more efficient.
		///// </summary>
		///// <param name="scr">Scipture object</param>
		///// <param name="marker">Marker on which to base calculation</param>
		///// <returns></returns>
		///// ------------------------------------------------------------------------------------
		//public static string CalculateNextFootnoteMarker(Scripture scr, string marker)
		//{
		//    char newChar;
		//    bool wrapped = true;

		//    if (scr.RestartFootnoteSequence)
		//    {
		//        ShouldMarkerCharWrap(marker[0], out newChar);
		//        return new string(newChar, 1);
		//    }

		//    StringBuilder bldr = new StringBuilder(marker);
		//    for (int i = marker.Length - 1; i >= 0; i--)
		//    {
		//        wrapped = ShouldMarkerCharWrap(marker[i], out newChar);
		//        bldr.Replace(marker[i], newChar, i, 1);

		//        if (!wrapped)
		//            break;
		//    }

		//    // If we got to this point and the first character in the marker string had
		//    // to wrap, then we need to append another character to the marker.
		//    if (wrapped)
		//        bldr.Append("a");

		//    return bldr.ToString();
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if a character in the footnote marker needs to wrap from 'z' to 'a'
		/// </summary>
		/// <param name="markerChar">Marker character to test.</param>
		/// <param name="nextChar">The correct marker character that should follow the one
		/// being tested.</param>
		/// <returns><c>true</c> if markerChar was wrapped from 'z' to 'a'</returns>
		/// ------------------------------------------------------------------------------------
		private static bool ShouldMarkerCharWrap(char markerChar, out char nextChar)
		{
			if (markerChar == 'z')
			{
				nextChar = 'a';
				return true;
			}

			nextChar = (char)(markerChar + 1);
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes cache for all footnotes in the StText containing the paragraph.  We only
		/// use this method when verses of a paragraph are changed - this will only happen on
		/// content paragraphs of a ScrSection.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoObj"></param>
		/// ------------------------------------------------------------------------------------
		internal static void RefreshCacheForParagraph(FdoCache cache, int hvoObj)
		{
			int hvoPara = hvoObj;
			if (cache.GetClassOfObject(hvoObj) == CmTranslation.kClassId)
				hvoPara = cache.GetOwnerOfObject(hvoObj);

			int hvoText = cache.GetOwnerOfObject(hvoPara);
			IStText text = new StText(cache, hvoText);
			ScrSection section = new ScrSection(cache, text.OwnerHVO);
			BCVRef verseRef = new BCVRef(section.VerseRefStart);

			Dictionary<int, FootnoteHashEntry> dict;
			// Don't bother on empty cache - it will be created when needed. Need to use HVO to get GUID so we
			// don't create the book and potentially cause the refresh routine to be called.
			Guid bookGuid = cache.GetGuidFromId(section.OwnerHVO);
			if (!cache.TryGetHashtable<int, FootnoteHashEntry>(bookGuid, out dict) ||
				dict.Count == 0)
			{
				return;
			}

			AddFootnoteRefsInText(text, dict, ref verseRef);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replaces all references to one paragraph in the footnote cache with another. Used to
		/// update the cache when a paragraph is being merged with another.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="oldParaHvo">id of paragraph being merged into another</param>
		/// <param name="newParaHvo">id of paragraph surviving the merge</param>
		/// ------------------------------------------------------------------------------------
		public static void ReplaceReferencesToParagraph(FdoCache cache, int oldParaHvo, int newParaHvo)
		{
			int bookHvo = cache.GetOwnerOfObjectOfClass(oldParaHvo, (int)ScrBook.kclsidScrBook);
			if (bookHvo <= 0)
			{
				Debug.Fail("Footnotes have to be contained in paragraphs owned by books.");
				return;
			}
			Guid bookGuid = cache.GetGuidFromId(bookHvo);

			Dictionary<int, FootnoteHashEntry> dict;
			// Don't bother on empty cache - it will be created when needed. Need to use HVO to get
			// GUID so we don't create the book and potentially cause the refresh routine to be called.
			if (!cache.TryGetHashtable<int, FootnoteHashEntry>(bookGuid, out dict) || dict.Count == 0)
				return;

			foreach (FootnoteHashEntry entry in dict.Values)
			{
				if (entry.HvoPara == oldParaHvo)
					entry.HvoPara = newParaHvo;
			}
		}
	}
	#endregion

	#region StFootnoteVerseChangeWatcher
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Updates the cache of references for footnotes in a paragraph when verse or chapter
	/// numbers are inserted
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StFootnoteVerseChangeWatcher : ChangeWatcher
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor (required for adding a generic ChangeWatcher on the cache)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StFootnoteVerseChangeWatcher() : base(null, 0)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		public StFootnoteVerseChangeWatcher(FdoCache cache) :
			base(cache, (int)StTxtPara.ktagVerseNumbers)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes cache for footnotes in updated text.
		/// </summary>
		/// <param name="hvoPara">The Paragraph that was changed</param>
		/// <param name="ivMin">the starting character index where the change occurred</param>
		/// <param name="cvIns">the number of characters inserted</param>
		/// <param name="cvDel">the number of characters deleted</param>
		/// ------------------------------------------------------------------------------------
		protected override void DoEffectsOfPropChange(int hvoPara, int ivMin, int cvIns, int cvDel)
		{
			ScrFootnote.RefreshCacheForParagraph(m_cache, hvoPara);
		}
	}
	#endregion
}
