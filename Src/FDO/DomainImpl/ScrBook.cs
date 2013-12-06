// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2003' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrBook.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;
using SIL.Utils;
using System.Text;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ScrBook represents a single book in a Scripture vernacular translation.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class ScrBook
	{
		#region MissingVerseRange class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Class representing a simple range of Scripture verses.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal class MissingVerseRange
		{
			/// <summary>The first verse in the range (i.e., the "min")</summary>
			private ScrReference m_rangeStart;
			/// <summary>The last verse in the range</summary>
			private ScrReference m_rangeEnd;

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:MissingVerseRange"/> class.
			/// </summary>
			/// <param name="rangeMin">The first verse in the range.</param>
			/// <param name="rangeLast">The last verse in the range.</param>
			/// ------------------------------------------------------------------------------------
			internal MissingVerseRange(ScrReference rangeMin, ScrReference rangeLast)
			{
				m_rangeStart = new ScrReference(rangeMin);
				m_rangeEnd = new ScrReference(rangeLast);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Creates a new instance of the <see cref="T:MissingVerseRange"/> class.
			/// </summary>
			/// <param name="rangeMin">The first verse in the range.</param>
			/// <param name="rangeLim">The verse immediately following the range (i.e. the "lim").
			/// </param>
			/// ------------------------------------------------------------------------------------
			internal static MissingVerseRange CreateFromMinAndLim(ScrReference rangeMin, ScrReference rangeLim)
			{
				return new MissingVerseRange(rangeMin, rangeLim.PrevVerse);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:MissingVerseRange"/> class.
			/// </summary>
			/// <param name="versePrev">The verse immediately preceding the range.</param>
			/// <param name="rangeLim">The verse immediately following the range (i.e. the "lim").
			/// </param>
			/// ------------------------------------------------------------------------------------
			internal static MissingVerseRange CreateFromPrevAndLim(ScrReference versePrev, ScrReference rangeLim)
			{
				return CreateFromMinAndLim(versePrev.NextVerse, rangeLim);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:MissingVerseRange"/> class.
			/// </summary>
			/// <param name="versePrev">The verse immediately preceding the range.</param>
			/// <param name="rangeLast">The last verse in the range.</param>
			/// ------------------------------------------------------------------------------------
			internal static MissingVerseRange CreateFromPrevAndEnd(ScrReference versePrev, ScrReference rangeLast)
			{
				return new MissingVerseRange(versePrev.NextVerse, rangeLast);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Checks to see if the passed range is a subset of this range.
			/// </summary>
			/// <param name="otherRange">The other range.</param>
			/// <returns>true if range is a subset</returns>
			/// ------------------------------------------------------------------------------------
			internal bool IsSubset(MissingVerseRange otherRange)
			{
				return m_rangeStart <= otherRange.m_rangeStart && m_rangeEnd >= otherRange.m_rangeEnd;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Returns a string with this range formatted up all nice and pretty.
			/// </summary>
			/// <param name="scr">The Scripture object</param>
			/// ------------------------------------------------------------------------------------
			public string ToString(IScripture scr)
			{
				return BCVRef.MakeReferenceString(m_rangeStart, m_rangeEnd,
					scr.ChapterVerseSepr, scr.Bridge);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Resets the start reference to the next verse after the given versePrev value. This
			/// can only be used to reduce the range (increase the start verse), not expand it.
			/// </summary>
			/// <param name="versePrev">The reference of the previous verse, upon which the start
			/// reference of this range should be based</param>
			/// <exception cref="T:InvalidOperationException">The Start property can only be used to
			/// reduce the verse range, not expand it.</exception>
			/// <exception cref="T:InvalidOperationException">Illegal verse range.</exception>
			/// ------------------------------------------------------------------------------------
			internal void SetStartBasedOnPrev(ScrReference versePrev)
			{
				ScrReference value = versePrev.NextVerse;
				if (m_rangeStart > value)
					throw new InvalidOperationException("The Start property can only be used to reduce the verse range, not expand it.");
				if (value > m_rangeEnd)
					throw new InvalidOperationException("Illegal verse range.");
				m_rangeStart = value;
			}

			#region Properties
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the start reference.
			/// </summary>
			internal ScrReference Start
			{
				get { return m_rangeStart; }
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the end reference.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			internal ScrReference End
			{
				get { return m_rangeEnd; }
			}
			#endregion
		}
		#endregion

		#region Constants
		/// <summary>Query to find the list of back translations used in this book.</summary>
		private const string kSqlFindBtWSs =
			"select DISTINCT ctt.ws " +
				"from ScrBook book " +
				"join CmObject sec on sec.Owner$ = book.id " +
				"join CmObject txt on txt.Owner$ = sec.id or txt.Owner$ = book.id " +
				"join StTxtPara_ para on para.Owner$ = txt.id " +
				"join CmTranslation_ ct on ct.Owner$ = para.id and ct.type = {0} " +
				"join CmTranslation_Translation ctt on ctt.Obj = ct.id " +
				"where book.id = {1}";
		#endregion

		#region Data Members
		private static readonly uint s_intSize =
			(uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(int));
		ScrBookTokenEnumerable m_tokenizer;
		#endregion

		#region ScrBook Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the SIL book ID 3-letter code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BookId
		{
			get { return ScrReference.NumberToBookCode(CanonicalNum); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the best available abbrev for this ScrBook to use in the UI.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BestUIAbbrev
		{
			get { return BookId;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the best available name for this ScrBook to use in the UI.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BestUIName
		{
			get { return ScriptureServices.GetUiBookName(CanonicalNum); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TODO: This will need to change when the ScrRefSystem goes away
		/// Gets the best available name for this ScrBook - the vernacular name if it's
		/// available, otherwise the analysis language name, otherwise the SIL code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString BestAvailName
		{
			get
			{
				ITsString bookName = Name.VernacularDefaultWritingSystem;
				if (bookName == null || string.IsNullOrEmpty(bookName.Text))
					bookName = Name.AnalysisDefaultWritingSystem;
				if (bookName == null || string.IsNullOrEmpty(bookName.Text))
				{
					bookName = m_cache.TsStrFactory.MakeString(BookId, WritingSystemServices.FallbackUserWs(m_cache));
				}

				Debug.Assert(bookName != null && !string.IsNullOrEmpty(bookName.Text));

				return bookName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of all paragraphs in this book in their natural order (i.e., title
		/// paragraphs first, then intro pragraphs, then Scripture paragraphs). This does not
		/// include footnote paragraphs or picture captions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<IScrTxtPara> Paragraphs
		{
			get
			{
				List<IScrTxtPara> paras = new List<IScrTxtPara>();
				if (TitleOA != null) // Should never be, but some tests cheat and don't set up Title
				{
					foreach (IScrTxtPara para in TitleOA.ParagraphsOS)
						paras.Add(para);
				}
				foreach (IScrSection section in SectionsOS)
					paras.AddRange(section.Paragraphs);
				return paras;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets hvos of the writing systems for the back translations used in this book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual HashSet<int> BackTransWs
		{
			get
			{
				HashSet<int> btWs = new HashSet<int>();
				foreach (IScrTxtPara para in Paragraphs)
				{
					ICmTranslation bt = para.GetBT();
					if (bt != null)
						btWs.UnionWith(bt.Translation.AvailableWritingSystemIds);
				}
				return btWs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first section in the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrSection FirstSection
		{
			get { return this[0]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first Scripture (non-intro) section in the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrSection FirstScriptureSection
		{
			get
			{
				foreach (IScrSection section in SectionsOS)
					if (!section.IsIntro)
						return section;
				// It turns out that it pretty easy to get a book with only introduction. Either
				// by importing something that only has introduction or by inserting an
				// intro section and deleting all scripture sections.
				//throw new Exception("Book has no Scripture section.");
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last section in the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrSection LastSection
		{
			get { return this[SectionsOS.Count - 1]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the specified section in the book. If the section doesn't exist (i.e. index
		/// is out of range), then null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrSection this[int i]
		{
			get	{ return (i < 0 || i >= SectionsOS.Count ? null : (ScrSection)SectionsOS[i]); }
		}

		#endregion

		#region Methods to enforce data integrity
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Need this version, because the ICmObjectInternal.RemoveObjectSideEffects version
		/// can't be virtual, and we want subclasses to be able to override the method.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			base.AddObjectSideEffectsInternal(e);

			if (e.Flid == ScrBookTags.kflidSections)
				((ScrSection)e.ObjectAdded).AdjustReferences();
		}
		#endregion

		#region Footnote-related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnote at the specified position in this book.
		/// </summary>
		/// <param name="iSection">Index of section.</param>
		/// <param name="iParagraph">Index of paragraph.</param>
		/// <param name="ich">Character position.</param>
		/// <param name="tag">Tag</param>
		/// <returns>Footnote at specified position, or <c>null</c> if specified position is
		/// not in front of a footnote marker run.</returns>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote FindCurrentFootnote(int iSection, int iParagraph, int ich, int tag)
		{
			// Don't bother looking if book has no footnotes.
			if (FootnotesOS.Count == 0)
				return null;

			IStTxtPara para = null;
			if (tag == ScrBookTags.kflidTitle)
				para = TitleOA[iParagraph];
			else
			{
				IScrSection section = SectionsOS[iSection];
				if (tag == ScrSectionTags.kflidHeading)
					para = section.HeadingOA[iParagraph];
				else if (tag == ScrSectionTags.kflidContent)
					para = section.ContentOA[iParagraph];
				else
				{
					Debug.Assert(false, "Unexpected tag");
					return null;
				}
			}

			ITsString tss = para.Contents;
			Debug.Assert(tss.Length >= ich);
			return Cache.ServiceLocator.GetInstance<IScrFootnoteRepository>().GetFootnoteFromProps(
				tss.get_PropertiesAt(ich));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the next footnote and returns an object that references the footnote and holds
		/// all the necessary info (indices and tags) to locate the footnote marker in the
		/// vernacular text.
		/// </summary>
		/// <param name="iSection">Index of section to start search</param>
		/// <param name="iParagraph">Index of paragraph to start search</param>
		/// <param name="ich">Character index to start search</param>
		/// <param name="tag">Flid to start search</param>
		/// <returns>Information about the next footnote, or <c>null</c> if there isn't another
		/// footnote in the current book.</returns>
		/// ------------------------------------------------------------------------------------
		public FootnoteLocationInfo FindNextFootnote(int iSection, int iParagraph, int ich,
			int tag)
		{
			return FindNextFootnote(iSection, iParagraph, ich, tag, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the next footnote and returns an object that references the footnote and holds
		/// all the necessary info (indices and tags) to locate the footnote marker in the
		/// vernacular text.
		/// </summary>
		/// <param name="iSection">Index of section to start search</param>
		/// <param name="iParagraph">Index of paragraph to start search</param>
		/// <param name="ich">Character index to start search</param>
		/// <param name="tag">Flid to start search</param>
		/// <param name="fSkipCurrentPos"><c>true</c> to start search with run after ich,
		/// <c>false</c> to start with current run.</param>
		/// <returns>Information about the next footnote, or <c>null</c> if there isn't another
		/// footnote in the current book.</returns>
		/// ------------------------------------------------------------------------------------
		public FootnoteLocationInfo FindNextFootnote(int iSection, int iParagraph, int ich,
			int tag, bool fSkipCurrentPos)
		{
			// Don't bother looking if book has no footnotes.
			if (FootnotesOS.Count == 0)
				return null;

			IScrFootnote footnote = null;

			if (tag == ScrBookTags.kflidTitle)
			{
				footnote = TitleOA.FindNextFootnote(ref iParagraph, ref ich,
					fSkipCurrentPos);
				if (footnote == null)
				{
					iSection = 0;
					iParagraph = 0;
					ich = 0;
					tag = ScrSectionTags.kflidHeading;
					fSkipCurrentPos = false;
				}
			}

			IFdoOwningSequence<IScrSection> sections = SectionsOS;
			IScrSection section = null;
			if (footnote == null)
				section = sections[iSection];

			if (tag == ScrSectionTags.kflidHeading)
			{
				footnote = section.HeadingOA.FindNextFootnote(ref iParagraph, ref ich,
					fSkipCurrentPos);
				if (footnote == null)
				{
					iParagraph = 0;
					ich = 0;
					tag = ScrSectionTags.kflidContent;
					fSkipCurrentPos = false;
				}
			}

			if (tag == ScrSectionTags.kflidContent)
			{
				footnote = section.ContentOA.FindNextFootnote(ref iParagraph, ref ich,
					fSkipCurrentPos);
			}

			while (footnote == null && iSection < sections.Count - 1)
			{
				iSection++;
				section = sections[iSection];
				footnote = section.FindFirstFootnote(out iParagraph, out ich, out tag);
			}

			if (footnote != null)
				return new FootnoteLocationInfo(footnote, IndexInOwner, iSection, iParagraph, ich, tag);
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the previous footnote and returns it. If a footnote is found,
		/// <paramref name="iSection"/>, <paramref name="iParagraph"/>, <paramref name="ich"/>
		/// and <paramref name="tag"/> will be set to indicate the position before the previous
		/// footnote marker. If no footnote can be found <paramref name="iSection"/>,
		/// <paramref name="iParagraph"/>, <paramref name="ich"/> and <paramref name="tag"/>
		/// won't change.
		/// </summary>
		/// <param name="iSection">Index of section to start search</param>
		/// <param name="iParagraph">Index of paragraph to start search, or -1 to start search
		/// in last paragraph.</param>
		/// <param name="ich">Character index to start search, or -1 to start at the end of
		/// the paragraph.</param>
		/// <param name="tag">Flid to start search</param>
		/// <returns>Previous footnote, or <c>null</c> if there isn't a previous footnote in the
		/// current book.</returns>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote FindPrevFootnote(ref int iSection, ref int iParagraph,
			ref int ich, ref int tag)
		{
			return FindPrevFootnote(ref iSection, ref iParagraph, ref ich, ref tag, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the previous footnote and returns it. If a footnote is found,
		/// <paramref name="iSection"/>, <paramref name="iParagraph"/>, <paramref name="ich"/>
		/// and <paramref name="tag"/> will be set to indicate the position before the previous
		/// footnote marker. If no footnote can be found <paramref name="iSection"/>,
		/// <paramref name="iParagraph"/>, <paramref name="ich"/> and <paramref name="tag"/>
		/// won't change.
		/// </summary>
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
		public IScrFootnote FindPrevFootnote(ref int iSection, ref int iParagraph, ref int ich,
			ref int tag, bool fSkipFirstRun)
		{
			// Don't bother looking if book has no footnotes.
			if (FootnotesOS.Count == 0)
				return null;

			IScrFootnote footnote = null;
			IScrSection section = null;
			if (tag != ScrBookTags.kflidTitle)
				section = SectionsOS[iSection];

			int iSectionTmp = iSection;
			int iParagraphTmp = iParagraph;
			int ichTmp = ich;
			int tagTmp = tag;

			if (tagTmp == ScrSectionTags.kflidContent)
			{
				footnote = (IScrFootnote)section.ContentOA.FindPreviousFootnote(ref iParagraphTmp,
					ref ichTmp, fSkipFirstRun);
				if (footnote == null)
				{
					tagTmp = ScrSectionTags.kflidHeading;
					iParagraphTmp = -1;
					ichTmp = -1;
					fSkipFirstRun = false;
				}
			}

			if (tagTmp == ScrSectionTags.kflidHeading)
			{
				footnote = (IScrFootnote)section.HeadingOA.FindPreviousFootnote(ref iParagraphTmp,
					ref ichTmp, fSkipFirstRun);
				while (footnote == null && iSectionTmp > 0)
				{
					iSectionTmp--;
					section = SectionsOS[iSectionTmp];
					footnote = (IScrFootnote)section.FindLastFootnote(out iParagraphTmp,
						out ichTmp, out tagTmp);
				}

				if (footnote == null)
				{
					iSectionTmp = 0;
					iParagraphTmp = -1;
					ichTmp = -1;
					fSkipFirstRun = false;
				}
			}

			if (footnote == null && TitleOA != null)
			{
				tagTmp = ScrBookTags.kflidTitle;
				footnote = (IScrFootnote)TitleOA.FindPreviousFootnote(ref iParagraphTmp,
					ref ichTmp, fSkipFirstRun);
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
		/// Inserts a footnote by inserting the footnote marker into the given string builder
		/// and creating a new <see cref="T:StFootnote"/> with the footnote marker set to the
		/// same marker. This is the real workhorse, used mainly for internal implementation,
		/// but it's public so import can use it to create footnotes more efficiently.
		/// </summary>
		/// <param name="iInsertAt">Zero-based index of the position in the sequence of
		/// footnotes where the new footnote is to be inserted</param>
		/// <param name="tsStrBldr">String builder for the paragraph being built</param>
		/// <param name="ich">Character index in paragraph where footnote is to be inserted</param>
		/// <returns>The newly created Footnote object</returns>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote InsertFootnoteAt(int iInsertAt, ITsStrBldr tsStrBldr, int ich)
		{
			Debug.Assert(tsStrBldr.Length >= ich);

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
				ws = m_cache.DefaultVernWs;
			}

			IScrFootnote footnote = CreateFootnote(iInsertAt, ws);
			TsStringUtils.InsertOrcIntoPara(footnote.Guid, FwObjDataTypes.kodtOwnNameGuidHot,
				tsStrBldr, ich, ich, ws);

			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a ScrFootnote owned by this ScrBook.
		/// The caller must take care of inserting the proper ORC and the footnote's paragraph
		/// and issusing a propchanged for the new footnote inserted into the book's collection.
		/// </summary>
		/// <param name="iFootnotePos">Zero-based index of the position in the book's sequence of
		/// footnotes where the new footnote is to be inserted</param>
		/// <param name="ws">The writing system for the footnote marker.</param>
		/// <returns>The newly created Footnote object</returns>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote CreateFootnote(int iFootnotePos, int ws)
		{
			// Create StFootnote object and establish it in the database
			IScrFootnote footnote = Services.GetInstance<IScrFootnoteFactory>().Create();
			FootnotesOS.Insert(iFootnotePos, footnote);

			// Mark the footnote marker style as being in use.
			Scripture scr = OwnerOfClass<Scripture>();
			IStStyle markerStyle = scr.FindStyle(ScrStyleNames.FootnoteMarker);
			((StStyle)markerStyle).InUse = true;

			// Copy current value of default footnote options to the new footnote.
			//footnote.DisplayFootnoteReference = scr.DisplayFootnoteReference;
			//footnote.DisplayFootnoteMarker = scr.DisplayFootnoteMarker;

			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches section forwards for first footnote reference in a section range.
		/// </summary>
		/// <param name="ihvoSectionStart">The index of the starting section in the section hvo array</param>
		/// <param name="ihvoSectionEnd">The index of the ending section in the section hvo array</param>
		/// <returns>
		/// first footnote in a range of sections or null if not found
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote FindFirstFootnoteInSectionRange(int ihvoSectionStart, int ihvoSectionEnd)
		{
			IScrFootnote footnote;
			int iPara;
			int ich;
			int tag;
			for (int iSection = ihvoSectionStart; iSection <= ihvoSectionEnd; iSection++)
			{
				IScrSection section = SectionsOS[iSection];
				footnote = section.FindFirstFootnote(out iPara, out ich, out tag);
				if (footnote != null)
					return footnote;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches section backwards for last footnote reference in a section range.
		/// </summary>
		/// <param name="ihvoSectionStart">The index of the starting section in the section hvo array</param>
		/// <param name="ihvoSectionEnd">The index of the ending section in the section hvo array</param>
		/// <returns>
		/// last footnote in a range of sections or null if not found
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote FindLastFootnoteInSectionRange(int ihvoSectionStart, int ihvoSectionEnd)
		{
			IScrFootnote footnote;
			int iPara;
			int ich;
			int tag;
			for (int iSection = ihvoSectionEnd; iSection >= ihvoSectionStart; iSection--)
			{
				IScrSection section = SectionsOS[iSection];
				footnote = section.FindLastFootnote(out iPara, out ich, out tag);
				if (footnote != null)
					return footnote;
			}

			return null;
		}
		#endregion

		#region Methods for maintaining footnote references
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the default values after the initialization of a CmObject. At the point that
		/// this method is called, the object should have an HVO, Guid, and a cache set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void SetDefaultValuesAfterInit()
		{
			base.SetDefaultValuesAfterInit();
			RefreshFootnoteRefs();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the location information for every footnote owned by this book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ClearFootnoteInformation()
		{
			foreach (ScrFootnote footnote in FootnotesOS)
				footnote.FootnoteRefInfo = RefRange.EMPTY;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refresh the footnote ref information for all footnotes owned by this book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void RefreshFootnoteRefs()
		{
			// If there are no footnotes at all, we're done.
			if (FootnotesOS.Count == 0)
				return;

			ClearFootnoteInformation();

			// If this book is in the early stages of getting created and does not yet have a
			// properly initialized section, then don't try to add footnote references yet.
			// (Currently, I think this can only happen during tests.)
			if (SectionsOS.Count == 0 || SectionsOS[0].VerseRefStart == 0)
				return;

			BCVRef footnoteRef = new BCVRef(CanonicalNum, 0, 0);
			// add refs for any footnotes in the book title
			AddFootnoteRefsInText(TitleOA, ref footnoteRef);

			// Add refs for footnotes in each section
			foreach (IScrSection section in SectionsOS)
			{
				// Add refs for footnotes in the section header.
				// TODO: what should the reference chapter:verse be if the footnote is found
				// in a section header?
				BCVRef firstRef = new BCVRef(section.VerseRefStart);
				footnoteRef = new BCVRef(firstRef);
				AddFootnoteRefsInText(section.HeadingOA, ref footnoteRef);

				// Add refs for footnotes in the section content
				footnoteRef = new BCVRef(firstRef);
				AddFootnoteRefsInText(section.ContentOA, ref footnoteRef);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compute and cache the references for all footnotes in the given text.
		/// </summary>
		/// <param name="text">The text whose footnote refs are to be computed</param>
		/// <param name="footnoteRef">Caller should pass in the initial reference to use as the
		/// basis for any references found in the course of parsing the text. Returned value
		/// will be the final reference found, which can be used as the basis for the subsequent
		/// text</param>
		/// ------------------------------------------------------------------------------------
		private void AddFootnoteRefsInText(IStText text, ref BCVRef footnoteRef)
		{
			if (text == null)
				return;

			BCVRef startRef = new BCVRef(footnoteRef);
			BCVRef endRef = new BCVRef(footnoteRef);

			IStFootnoteRepository footnoteRepo = Services.GetInstance<IStFootnoteRepository>();

			foreach (IScrTxtPara para in text.ParagraphsOS)
			{
				// search each run for chapter numbers, verse numbers, or footnote markers
				ITsString tssContents = para.Contents;
				if (tssContents.Length == 0)
					continue;

				int nRun = tssContents.RunCount;
				for (int i = 0; i < nRun; i++)
				{
					//ITsTextProps tprops = tssContents.get_Properties(i);
					//string styleName = tprops.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
					string styleName = tssContents.get_StringProperty(i, (int)FwTextPropType.ktptNamedStyle);

					if (styleName == ScrStyleNames.VerseNumber)
					{
						// When a verse number is encountered, save the number into
						// the reference.
						string sVerseNum = tssContents.get_RunText(i);
						int nVerseStart, nVerseEnd;
						ScrReference.VerseToInt(sVerseNum, out nVerseStart, out nVerseEnd);
						startRef.Verse = nVerseStart;
						endRef.Verse = nVerseEnd;
					}
					else if (styleName == ScrStyleNames.ChapterNumber)
					{
						// If a chapter number is encountered then save the number into
						// the reference and start the verse number back at 1.
						try
						{
							string sChapterNum = tssContents.get_RunText(i);
							startRef.Chapter = endRef.Chapter = ScrReference.ChapterToInt(sChapterNum);
							startRef.Verse = endRef.Verse = 1;
						}
						catch (ArgumentException)
						{
							// ignore runs with invalid Chapter numbers
						}
					}
					else if (styleName == null)
					{
						// If the run is a footnote reference then store the Scripture
						// reference with the footnote hvo in the hash table.
						IScrFootnote footnote = (IScrFootnote)footnoteRepo.GetFootnoteFromObjData(tssContents.get_StringProperty(i, (int)FwTextPropType.ktptObjData));
						if (footnote != null)
							((ScrFootnote)footnote).FootnoteRefInfo = new RefRange(startRef, endRef);
					}
				}
			}
			footnoteRef = endRef;
		}

		#endregion

		#region Miscellaneous public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a <see cref = "T:IScrBook"/>'s Title paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitTitlePara()
		{
			IStText titleText = TitleOA;
			Debug.Assert(titleText != null && titleText.ParagraphsOS.Count == 0);
			IStTxtPara para = titleText.AddNewTextPara(ScrStyleNames.MainBookTitle);

			// set the title text to the vernacular writing system.
			ITsStrBldr bldr = para.Contents.GetBldr();
			int ichLim = bldr.Length;
			bldr.SetIntPropValues(0, ichLim, (int)FwTextPropType.ktptWs, 0, m_cache.DefaultVernWs);
			para.Contents = bldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the next exact match starting from the given section starting at the given
		/// paragraph and character offsets. If not found looking forward from that point, the
		/// search wraps around from the beginning of the book.
		/// </summary>
		/// <param name="targetRef">ScrReference to find</param>
		/// <param name="fFindSectionStart">if set to <c>true</c> this method will return
		/// values corresponding to the beginning of the section head if the target reference is
		/// the first one in a section.</param>
		/// <param name="startingSection">The 0-based index of the first section to look in
		/// </param>
		/// <param name="startingParaIndex">The 0-based index of the first paragraph to look in
		/// </param>
		/// <param name="startingCharIndex">The 0-based index of the first character to look at
		/// </param>
		/// <param name="section">The 0-based index of the section where the reference starts.
		/// </param>
		/// <param name="paraIndex">The 0-based index of the paragraph where the reference starts.
		/// </param>
		/// <param name="ichVerseStart">The 0-based index of the character verse start.</param>
		/// <returns>
		/// 	<c>true</c> if found; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool GetRefStartFromSection(ScrReference targetRef, bool fFindSectionStart,
			IScrSection startingSection, int startingParaIndex, int startingCharIndex,
			out IScrSection section, out int paraIndex, out int ichVerseStart)
		{
			section = startingSection;

			// Find exact match starting at current position
			if (GetRefStart(targetRef, fFindSectionStart, section, startingParaIndex,
				startingCharIndex, -1, -1, out paraIndex, out ichVerseStart))
			{
				return true;
			}

			section = section.NextSection ?? (ScrSection)SectionsOS[0];

			while (section != startingSection)
			{
				if (GetRefStart(targetRef, fFindSectionStart, section, 0, 0, -1, -1,
					out paraIndex, out ichVerseStart))
				{
					return true;
				}
				// if the next section is null, wrap around to the beginning
				section = section.NextSection ?? (ScrSection)SectionsOS[0];
			}

			// We've wrapped all the way back around to the first section we tried. Now
			// try any characters/paragraphs that were before our start position in that section.
			return GetRefStart(targetRef, fFindSectionStart, startingSection, 0, 0,
				startingParaIndex, startingCharIndex, out paraIndex, out ichVerseStart);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the (next) exact match in the given section starting at the given paragraph
		/// and character offsets and ending at the given paragraph and character offsets
		/// </summary>
		/// <param name="targetRef">ScrReference to find</param>
		/// <param name="fFindSectionStart">if set to <c>true</c> this method will return
		/// values corresponding to the beginning of the section head if the target reference is
		/// the first one in a section.</param>
		/// <param name="section">section to search</param>
		/// <param name="startingParaIndex">The 0-based index of the first paragraph to look in
		/// </param>
		/// <param name="startingCharIndex">The 0-based index of the first character to look at
		/// </param>
		/// <param name="endingParaIndex">The 0-based index of the last paragraph to look in
		/// (-1 for end)</param>
		/// <param name="endingCharIndex">The 0-based index of the last character index to look
		/// at (-1 for end)</param>
		/// <param name="paraIndex">The 0-based index of the paragraph where the reference
		/// starts. Undefined if this method returns false.</param>
		/// <param name="ichVerseStart">The 0-based index of the character verse start.
		/// Undefined if this method returns false.</param>
		/// <returns>
		/// 	<c>true</c> if found; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		internal bool GetRefStart(ScrReference targetRef, bool fFindSectionStart,
			IScrSection section, int startingParaIndex, int startingCharIndex,
			int endingParaIndex, int endingCharIndex, out int paraIndex, out int ichVerseStart)
		{
			paraIndex = -1;
			ichVerseStart = -1;
			if (!section.ContainsReference(targetRef) ||
				startingParaIndex >= section.ContentOA.ParagraphsOS.Count) // TE-5439
			{
				return false;
			}

			if (endingParaIndex == -1 || endingParaIndex >= section.ContentOA.ParagraphsOS.Count)
				endingParaIndex = section.ContentOA.ParagraphsOS.Count - 1;

			for (paraIndex = startingParaIndex; paraIndex <= endingParaIndex; ++paraIndex)
			{
				IScrTxtPara para = (IScrTxtPara)section.ContentOA[paraIndex];
				using (var verseSet = new ScrVerseSet(para))
				{
					int currentEndingCharIndex = (paraIndex == endingParaIndex && endingCharIndex != -1) ?
						endingCharIndex : para.Contents.Length - 1;

					foreach (ScrVerse verse in verseSet)
					{
						// ignore all but verse number runs
						if (!verse.VerseNumberRun)
							continue;
						if (verse.VerseStartIndex >= currentEndingCharIndex)
							break;
						if (verse.VerseStartIndex < startingCharIndex)
							continue;
						if (verse.StartRef <= targetRef && targetRef <= verse.EndRef)
						{
							// TODO: Interpret fFindSectionStart
							ichVerseStart = verse.TextStartIndex;
							return true;
						}
					}

					// after the first paragraph, start looking at 0
					startingCharIndex = 0;
				}
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents this instance.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return BestUIName;
		}

		#endregion

		#region Merge section into previous section content
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the heading and content of the given section into the previous section content.
		/// Then deletes the given section.
		/// </summary>
		/// <param name="iSection">index of the section to be moved</param>
		/// <param name="newStyle">The new para (content) style for the moved heading paragraphs.</param>
		/// <returns>the index of first moved paragraph</returns>
		/// <exception cref="T:ArgumentOutOfRangeException">Occurs when section index is invalid.
		/// The index must be greater than 0.</exception>
		/// ------------------------------------------------------------------------------------
		public int MergeSectionIntoPreviousSectionContent(int iSection, IStStyle newStyle)
		{
			IScrSection prevSection = SectionsOS[iSection - 1];
			IScrSection origSection = SectionsOS[iSection];
			int insertIndex = prevSection.ContentOA.ParagraphsOS.Count;

			ScrSection.MoveAllParas(origSection, ScrSectionTags.kflidHeading,
				prevSection, ScrSectionTags.kflidContent, true, newStyle);
			ScrSection.MoveAllParas(origSection, ScrSectionTags.kflidContent,
				prevSection, ScrSectionTags.kflidContent, true, null);

			ScrSection.VerifyThatParaStylesHaveCorrectStructure(prevSection, ScrSectionTags.kflidContent);

			SectionsOS.RemoveAt(iSection);

			// return index of first moved paragraph, i.e. number of paragraphs originally in the target section
			return insertIndex;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the content of the given section into the previous section content.
		/// Then deletes the given section.
		/// <param name="iSection">index of the section to be moved</param>
		/// <exception cref="T:ArgumentOutOfRangeException">Occurs when section index is invalid.
		/// The index must be greater than 0.</exception>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void MergeSectionContentIntoPreviousSectionContent(int iSection)
		{
			IScrSection prevSection = SectionsOS[iSection - 1];
			IScrSection section = SectionsOS[iSection];

			// move the Content from the given section into previous section
			ScrSection.MoveAllParas(section, ScrSectionTags.kflidContent,
				prevSection, ScrSectionTags.kflidContent, true, null);

			// verify the styles (though we didn't change the styles)
			ScrSection.VerifyThatParaStylesHaveCorrectStructure(prevSection, ScrSectionTags.kflidContent);

			// delete the section
			SectionsOS.Remove(section);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the requested number of paragraphs from the heading of one section to the end
		/// of the content of the previous section.
		/// </summary>
		/// <param name="iSection">index of the section we move paragraphs from</param>
		/// <param name="iLastPara">index of the last heading paragraph to be moved. Must
		/// be prior to the final paragraph in the heading.</param>
		/// <param name="newStyle">The new style for the moved paragraphs.</param>
		/// <returns>index of first moved paragraph</returns>
		/// <exception cref="T:ArgumentOutOfRangeException">Occurs when iSection is 0,
		/// or out of range. Also when iLastPara is out of range.</exception>
		/// <exception cref="T:InvalidOperationException">Occurs when the iLastPara is the final
		/// paragraph in the heading. Cannot move all paras in the heading with this method.</exception>
		/// ------------------------------------------------------------------------------------
		public int MoveHeadingParasToPreviousSectionContent(int iSection, int iLastPara,
			IStStyle newStyle)
		{
			IScrSection prevSection = SectionsOS[iSection - 1];
			IScrSection origSection = SectionsOS[iSection];
			int insertIndex = prevSection.ContentOA.ParagraphsOS.Count;

			if (iLastPara == origSection.HeadingOA.ParagraphsOS.Count - 1)
				throw new InvalidOperationException("Must not move all paragraphs in the heading.");

			if (newStyle.Structure != StructureValues.Body)
				throw new ArgumentException("Style must be for section content.");

			// Copy the paragraphs from the section heading to the previous section content
			ScrSection.MoveWholeParas(origSection, ScrSectionTags.kflidHeading,
				0, iLastPara, prevSection, ScrSectionTags.kflidContent, insertIndex, newStyle);

			ScrSection.VerifyThatParaStylesHaveCorrectStructure(prevSection, ScrSectionTags.kflidContent);

			return insertIndex;
		}
		#endregion

		#region Merge section into following section heading
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the heading and content of the given section into the next section heading.
		/// Then deletes the given section.
		/// </summary>
		/// <param name="iSection">index of the section to be moved</param>
		/// <param name="newStyle">The new style for the nerged paragraphs.</param>
		/// <returns>the index of first paragraph originally selected, as viewed by the user</returns>
		/// <exception cref="T:IndexOutOfRangeException">Occurs when section index is invalid.
		/// The index must not be for the last section.</exception>
		/// <exception cref="T:InvalidStructureException">Occurs when a content paragraph that we
		/// are attempting to move to a heading paragraph contains chapter/verse numbers.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public int MergeSectionIntoNextSectionHeading(int iSection, IStStyle newStyle)
		{
			IScrSection origSection = SectionsOS[iSection];
			IScrSection nextSection = SectionsOS[iSection + 1];

			int index = origSection.HeadingOA.ParagraphsOS.Count;

			ScrSection.VerifyParasForHeadingHaveNoReferences(origSection.ContentOA,
				0, origSection.ContentOA.ParagraphsOS.Count - 1, newStyle);

			ScrSection.MoveAllParas(origSection, ScrSectionTags.kflidContent,
				nextSection, ScrSectionTags.kflidHeading, false, newStyle);
			ScrSection.MoveAllParas(origSection, ScrSectionTags.kflidHeading,
				nextSection, ScrSectionTags.kflidHeading, false, null);

			ScrSection.VerifyThatParaStylesHaveCorrectStructure(nextSection, ScrSectionTags.kflidHeading);

			SectionsOS.RemoveAt(iSection);

			// return index of the first paragraph originally selected
			return index;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Move one or more paragraphs from the content of one section to the beginning of
		/// the heading of the following section. The paragraphs from the given index to the
		/// last content paragraph are moved.
		/// </summary>
		/// <param name="iSection">index of the section we move paragraphs from</param>
		/// <param name="iFirstPara">index of the first content paragraph to be moved</param>
		/// <param name="newStyle">The new style for the moved paragraphs.</param>
		/// <exception cref="T:ArgumentOutOfRangeException">Occurs when iSection is the last section,
		/// or out of range. Also when iFirstPara is out of range.</exception>
		/// <exception cref="T:InvalidOperationException">Occurs when the iFirstPara is the first
		/// paragraph in the contents. Cannot move all paras in the contents with this method.</exception>
		/// <exception cref="T:InvalidStructureException">Occurs when a content paragraph that we
		/// are attempting to move to a heading paragraph contains chapter/verse numbers.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public void MoveContentParasToNextSectionHeading(int iSection, int iFirstPara, IStStyle newStyle)
		{
			if (iFirstPara == 0)
				throw new InvalidOperationException("iFirstPara cannot be the first paragraph.");

			if (newStyle.Structure != StructureValues.Heading)
				throw new ArgumentException("Style must be for section heading.");

			IScrSection origSection = SectionsOS[iSection];
			IScrSection nextSection = SectionsOS[iSection + 1];

			ScrSection.VerifyParasForHeadingHaveNoReferences(origSection.ContentOA, iFirstPara,
				origSection.ContentOA.ParagraphsOS.Count - 1, newStyle);

			// Copy the paragraphs from the section content to the next section heading
			ScrSection.MoveWholeParas(origSection, ScrSectionTags.kflidContent, iFirstPara,
				origSection.ContentOA.ParagraphsOS.Count - 1,
				nextSection, ScrSectionTags.kflidHeading, 0, newStyle);

			ScrSection.VerifyThatParaStylesHaveCorrectStructure(nextSection, ScrSectionTags.kflidHeading);
		}
		#endregion

		#region Section deletion handling
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the specified range from the middle of one section contents to the middle
		/// of another section contents.
		/// All ORC deletions are handled properly.
		/// </summary>
		/// <param name="iSectionStart">The index of the first section</param>
		/// <param name="iSectionEnd">The index of the last section</param>
		/// <param name="iParaStart">The index of the paragraph in the first section</param>
		/// <param name="iParaEnd">The index of the paragraph in the last section</param>
		/// <param name="ichStart">The character position in the paragraph in the first section</param>
		/// <param name="ichEnd">The character position in the paragraph in the last section</param>
		/// ------------------------------------------------------------------------------------
		public void DeleteMultiSectionContentRange(int iSectionStart, int iSectionEnd,
			int iParaStart, int iParaEnd, int ichStart, int ichEnd)
		{
			Debug.Assert(iSectionEnd > iSectionStart);

			IScrSection sectionStart = SectionsOS[iSectionStart];
			IScrSection sectionEnd = SectionsOS[iSectionEnd];
			IScrTxtPara paraStart = (IScrTxtPara)sectionStart.ContentOA[iParaStart];
			IScrTxtPara paraEnd = (IScrTxtPara)sectionEnd.ContentOA[iParaEnd];

			// Remove any trailing paragraphs of the start section that is selected
			IStText sectionContent = sectionStart.ContentOA;
			for (int i = sectionContent.ParagraphsOS.Count - 1; i > iParaStart; i--)
				sectionContent.DeleteParagraph(sectionContent[i]);

			// Copy any trailing paragraphs (not selected) of the end section to the start section.
			// Move whole ending paragraph of the selection if selection ends at the beginning
			// of the paragraph
			int iMoveStart = iParaEnd + (ichEnd == 0 ? 0 : 1);
			if (iMoveStart < sectionEnd.ContentOA.ParagraphsOS.Count)
			{
				sectionEnd.ContentOA.ParagraphsOS.MoveTo(iMoveStart,
				sectionEnd.ContentOA.ParagraphsOS.Count - 1,
				sectionStart.ContentOA.ParagraphsOS,
				sectionStart.ContentOA.ParagraphsOS.Count);
			}

			if (ichEnd == 0)
			{
				// Delete the end of the content of the start paragraph from ichStart
				paraStart.ReplaceTextRange(ichStart, paraStart.Contents.Length, null, 0, 0);
			}
			else
			{
				// Copy end of last paragraph of selection to end of first paragraph of selection
				paraStart.MoveText(ichStart, paraStart.Contents.Length, paraEnd, ichEnd, paraEnd.Contents.Length);
				paraStart.StyleRules = paraEnd.StyleRules;
			}

			// Remove whole sections between start and end of selection, including the
			// ending section since content we want from it has been moved to the
			// starting section.
			RemoveSections(iSectionStart + 1, iSectionEnd);
		}
		#endregion

		#region Checking stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepare to return tokens for Scripture checking.
		/// ENHANCE: For our initial implementation, we'll see how it performs if we don't
		/// do the parsing ahead of time. We'll just set up the enumerators...Parse into Tokens.
		/// The tokens are accessed via the TextTokens() method.
		/// We split this operation into two parts since we often want to create
		/// the tokens list once and then present them to several different checks.
		/// </summary>
		/// <param name="chapterNum">0=read whole book, else specified chapter number</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool GetTextToCheck(int chapterNum)
		{
			lock (SyncRoot)
				m_tokenizer = new ScrBookTokenEnumerable(this, chapterNum);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enumerate all the ITextToken's from the most recent GetText call.
		/// </summary>
		/// <returns>An IEnumerable implementation that allows the caller to retrieve each
		/// text token in sequence.</returns>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<ITextToken> TextTokens()
		{
			lock (SyncRoot)
			{
				if (m_tokenizer == null)
					throw new InvalidOperationException("GetTextToCheck must be called before TextTokens");
				return m_tokenizer;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the section where the given chapter begins.
		/// </summary>
		/// <param name="chapterNum">The chapter number (assumed to be in same versification as
		/// that of the Scripture object).</param>
		/// <returns>the index of the section or -1 if not found</returns>
		/// ------------------------------------------------------------------------------------
		public int FindSectionForChapter(int chapterNum)
		{
			// Getting the Versification is probably not very efficient, so do it once outside
			// the loop.
			ScrVers versification = m_cache.LangProject.TranslatedScriptureOA.Versification;
			for (int iSection = 0; iSection < SectionsOS.Count; iSection++)
			{
				IScrSection section = SectionsOS[iSection];
				if (!section.IsIntro)
				{
					ScrReference startRef = new ScrReference(section.VerseRefStart, versification);
					if (startRef.Chapter == chapterNum)
						return iSection;
					if (startRef.Chapter < chapterNum)
					{
						ScrReference endRef = new ScrReference(section.VerseRefEnd, versification);
						if (endRef.Chapter >= chapterNum)
							return iSection;
					}
				}
			}
			return -1;
		}

		#endregion

		#region DetermineOverwritability and helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks to see whether/how this book can be overwritten with the given saved version.
		/// Possibilities are:
		/// * Full overwrite is possible without losing Scripture data
		/// * Full overwrite would lose Scripture data
		/// * Partial overwrite is possible
		/// </summary>
		/// <param name="savedVersion">The saved version to be checked (note that this must be
		/// a real ScrBook, not merely an IScBook).</param>
		/// <param name="sDetails">A string with a formatted list of reference ranges included
		/// in current but missing in the saved version (separated by newlines)</param>
		/// <param name="sectionsToRemove">sections that will need to be removed from this book
		/// before a partial overwrite can be executed (will be null unless return type is
		/// Partial).</param>
		/// <param name="missingBtWs">list of back translation writing systems that are used in
		/// this book but not in the savedVersion</param>
		/// ------------------------------------------------------------------------------------
		public OverwriteType DetermineOverwritability(IScrBook savedVersion, out string sDetails,
			out List<IScrSection> sectionsToRemove, out HashSet<int> missingBtWs)
		{
			sDetails = null;
			sectionsToRemove = null;
			missingBtWs = null;
			StringBuilder strBldr = new StringBuilder();

			Debug.Assert(FirstSection != null && savedVersion.FirstSection != null);
			if (FirstSection == null)
				return OverwriteType.FullNoDataLoss; // Current book is empty
			else if (savedVersion.FirstSection == null)
				return OverwriteType.DataLoss; // Current is not empty but Revision is

			// Check for potential loss of back translation(s).
			missingBtWs = FindMissingBts(savedVersion);

			// Check that introduction won't be lost
			bool fIntroMissingInSaved =  FirstSection.IsIntro && !savedVersion.FirstSection.IsIntro;
			if (fIntroMissingInSaved)
			{
				strBldr.AppendFormat(
					ScrFdoResources.ResourceManager.GetString("kstidBookIntroduction"), BookId);
				strBldr.AppendLine();
			}

			List<MissingVerseRange> versesInCurrentMissingInSaved = new List<MissingVerseRange>();
			IScrSection firstScrSec = FirstScriptureSection;
			IScrSection firstRevScrSec = savedVersion.FirstScriptureSection;
			ScrVers versification = m_cache.LangProject.TranslatedScriptureOA.Versification;
			if (firstScrSec != null && firstRevScrSec == null)
			{
				// The current book has scripture sections that are not in the revision
				MissingVerseRange range = new MissingVerseRange(
					new ScrReference(firstScrSec.VerseRefMin, versification),
					new ScrReference(LastSection.VerseRefEnd, versification));
				versesInCurrentMissingInSaved.Add(range);
			}
			else if (firstScrSec == null && firstRevScrSec != null)
			{
				// All revision sections are new to the current. So everything is fine.
			}
			else if (firstScrSec == null && firstRevScrSec == null)
			{
				// There are no scripture sections in either the current or the revision, so
				// lets just give up!
				return OverwriteType.FullNoDataLoss;
			}
			else
			{
				ScrReference firstScrSecRefMin = new ScrReference(firstScrSec.VerseRefMin, versification);
				ScrReference firstRevSecRefMin = new ScrReference(firstRevScrSec.VerseRefMin, versification);
				if (firstScrSecRefMin.Verse <= 0)
					firstScrSecRefMin.Verse = 1;
				if (firstRevSecRefMin.Verse <= 0)
					firstRevSecRefMin.Verse = 1;

				if (firstScrSecRefMin < firstRevSecRefMin)
				{
					// The first section of the Current book contains verses before the first section
					// in the saved version. Replacing the Current with the saved version would cause
					// data loss.
					MissingVerseRange range = MissingVerseRange.CreateFromMinAndLim(firstScrSecRefMin, firstRevSecRefMin);
					versesInCurrentMissingInSaved.Add(range);
				}
			}

			// Check for hole in saved version
			versesInCurrentMissingInSaved.AddRange(DetectMissingHoles((ScrBook)savedVersion));

			// Check references on last scripture section
			if (LastSection.VerseRefMax > savedVersion.LastSection.VerseRefMax &&
				!savedVersion.LastSection.IsIntro)
			{
				MissingVerseRange range = MissingVerseRange.CreateFromPrevAndEnd(
					new ScrReference(savedVersion.LastSection.VerseRefMax, versification),
					new ScrReference(LastSection.VerseRefMax, versification));
				versesInCurrentMissingInSaved.Add(range);
			}

			if (versesInCurrentMissingInSaved.Count > 0 || fIntroMissingInSaved)
			{
				if (IsPartialOverwritePossible(versesInCurrentMissingInSaved, out sectionsToRemove, fIntroMissingInSaved))
					return OverwriteType.Partial;

				// Add list of holes to the strBldr
				foreach (MissingVerseRange missingVerses in versesInCurrentMissingInSaved)
					strBldr.AppendLine("   " + missingVerses.ToString(Cache.LangProject.TranslatedScriptureOA));

				// Remove the final newline
				int cch = Environment.NewLine.Length;
				strBldr.Remove(strBldr.Length - cch, cch);
				sDetails = strBldr.ToString();

				sectionsToRemove = null;

				return OverwriteType.DataLoss;
			}

			return OverwriteType.FullNoDataLoss;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds back translation(s) that are missing in the savedVersion that are used in this
		/// book.
		/// </summary>
		/// <param name="savedVersion">The saved version.</param>
		/// <returns>set of the HVOs of the writing systems for the the missing back
		/// translation(s)</returns>
		/// <remarks>The ideal solution would be to compare the individual paragraphs to
		/// automatically (as much as possible) preserve back translation data. This method
		/// returns which writing systems are used in this ScrBook, but are completely absent in
		/// the savedVersion.</remarks>
		/// ------------------------------------------------------------------------------------
		protected HashSet<int> FindMissingBts(IScrBook savedVersion)
		{
			HashSet<int> currentBTs = BackTransWs;
			HashSet<int> versionBTs = savedVersion.BackTransWs;

			// Remove back translations writing systems from the list for this book if they
			// are used in the savedVersion.
			currentBTs.ExceptWith(versionBTs);

			return (currentBTs.Count == 0) ? null : currentBTs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes specified sections in a book.
		/// </summary>
		/// <param name="sectionsToRemove">The sections to remove.</param>
		/// ------------------------------------------------------------------------------------
		public void RemoveSections(List<IScrSection> sectionsToRemove)
		{
			RemoveSections(sectionsToRemove, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the specified sections from this book.
		/// </summary>
		/// <param name="iStartSection">The index of the first section to remove.</param>
		/// <param name="iEndSection">The index of the last section to remove.</param>
		/// ------------------------------------------------------------------------------------
		public void RemoveSections(int iStartSection, int iEndSection)
		{
			for (int i = iEndSection; i >= iStartSection; i--)
				RemoveSection(SectionsOS[i]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes specified sections in a book.
		/// </summary>
		/// <param name="sectionsToRemove">The sections to remove.</param>
		/// <param name="progressDlg">The progress dialog box (can be null). Position will be
		/// incremented once per section to be removed (caller is responsible for setting the
		/// range and message appropriately.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public void RemoveSections(List<IScrSection> sectionsToRemove, IProgress progressDlg)
		{
			foreach (IScrSection section in sectionsToRemove)
			{
				RemoveSection(section);

				if (progressDlg != null)
					progressDlg.Position++;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the the specified section from this book.
		/// </summary>
		/// <param name="section">The section to remove.</param>
		/// ------------------------------------------------------------------------------------
		private void RemoveSection(IScrSection section)
		{
			// REVIEW (TimS): Should we also remove pictures or other ORCs that are owned by the
			// deleted section?
			// Remove all footnotes referenced in this section.
			List<IScrFootnote> footnotesToRemove = ((ScrSection)section).GetFootnotes();
			foreach (IScrFootnote footnote in footnotesToRemove)
				FootnotesOS.Remove(footnote);

			SectionsOS.Remove(section);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a paragraph with the specified style id, containing the specified verse number,
		/// if specified, and having the correct sequence number. Assumes the reference is
		/// within this book.
		/// </summary>
		/// <param name="targetStyle">style of paragraph to find</param>
		/// <param name="targetRef">Reference to seek</param>
		/// <param name="iPara">0-based index of paragraph</param>
		/// <param name="iVernSection">0-based index of the section the corresponding
		/// vernacular paragraph is in. This will be 0 if no corresponding paragraph can be
		/// found.</param>
		/// <returns>The corresponding StTxtPara, or null if no matching para is found</returns>
		/// ------------------------------------------------------------------------------------
		public IScrTxtPara FindPara(IStStyle targetStyle, BCVRef targetRef, int iPara, ref int iVernSection)
		{
			Debug.Assert(iPara >= 0);
			try
			{
				if (targetStyle != null)
				{
					if (targetStyle.Context == ContextValues.Title)
					{
						if (iPara < TitleOA.ParagraphsOS.Count)
						{
							IScrTxtPara para = (IScrTxtPara)TitleOA.ParagraphsOS[iPara];
							if (para.StyleName == targetStyle.Name)
								return para as IScrTxtPara;
						}
						return null;
					}

					if (iVernSection >= 0 && iVernSection < SectionsOS.Count)
					{
						IScrSection section = SectionsOS[iVernSection];
						if (targetStyle.Structure == StructureValues.Heading)
						{
							if (iPara < section.HeadingOA.ParagraphsOS.Count)
							{
								IScrTxtPara para = (IScrTxtPara)section.HeadingOA.ParagraphsOS[iPara];
								if (para.StyleName == targetStyle.Name)
									return para as IScrTxtPara;
							}
						}
						else
						{
							if (iPara < section.ContentOA.ParagraphsOS.Count &&
								(targetRef.Verse == 0 ||
								(section.VerseRefMin <= targetRef && targetRef <= section.VerseRefMax)))
							{
								IScrTxtPara para = (IScrTxtPara)section.ContentOA.ParagraphsOS[iPara];
								if (para.StyleName == targetStyle.Name)
									return para as IScrTxtPara;
							}
						}
					}
				}

				Debug.Assert(targetRef.Valid);
				iVernSection = -1;
				ScrReference refToTest = new ScrReference(targetRef,
					m_cache.LangProject.TranslatedScriptureOA.Versification);
				refToTest.Verse = Math.Min(refToTest.Verse, refToTest.LastVerse);
				foreach (IScrSection section in SectionsOS)
				{
					iVernSection++;

					// Since section.VerseRefMax can never be greater than what the versification
					// scheme allows, but the section can contain verses greater than that max.,
					// make sure to find the section based on the versification's max.
					if (section.VerseRefMin <= refToTest && refToTest <= section.VerseRefMax)
					{
						if (targetStyle != null && targetStyle.Structure == StructureValues.Heading)
						{
							if (iPara < section.HeadingOA.ParagraphsOS.Count)
							{
								IScrTxtPara para = (IScrTxtPara)section.HeadingOA.ParagraphsOS[iPara];
								if (para.StyleName == targetStyle.Name)
									return para;
							}
							return null;
						}
						else
						{
							foreach (IStTxtPara stPara in section.ContentOA.ParagraphsOS)
							{
								if (stPara == null)
								{
									Debug.WriteLine("Found a null paragraph!");
									return null;
								}

								if (targetStyle == null || stPara.StyleName == targetStyle.Name)
								{
									BCVRef refStart, refEnd;
									IScrTxtPara para = (IScrTxtPara)stPara;
									para.GetRefsAtPosition(para.Contents.Length - 1, out refStart, out refEnd);
									if (refEnd < targetRef)
										continue;
									para.GetRefsAtPosition(0, out refStart, out refEnd);
									if (refStart > targetRef)
										continue;

									return (IScrTxtPara)stPara;
									//ITsString tss = para.Contents.UnderlyingTsString;
									//int iLim = tss.RunCount;
									//for (int iRun = 0; iRun < iLim; )
									//{
									//    Scripture.GetNextRef(iRun, iLim, tss, true, ref refStart,
									//        ref refEnd, out iRun);
									//    if (refStart <= targetRef && refEnd >= targetRef)
									//        return para;
									//}
								}
							}
						}
					}
				}
			}
			catch (NullStyleRulesException)
			{
			}
			iVernSection = 0;
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the current version has any Scripture sections that are are only
		/// partially covered by the Scripture sections of the saved version.
		/// </summary>
		/// <param name="versesInCurrentMissingInSaved">The verses which are found in this
		/// book but missing in the saved version of this book.</param>
		/// <param name="sectionsToRemove">sections that would need to removed from the
		/// current to automatically merge.</param>
		/// <param name="fIntroMissingInSaved">if set to <c>true</c> the current version has an
		/// introduction but the saved version doesn't.</param>
		/// <returns>
		/// 	<c>true</c> if a partial overwrite is possible.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool IsPartialOverwritePossible(
			List<MissingVerseRange> versesInCurrentMissingInSaved,
			out List<IScrSection> sectionsToRemove, bool fIntroMissingInSaved)
		{
			sectionsToRemove = new List<IScrSection>();
			int iMissingHole = 0;

			foreach (IScrSection section in SectionsOS)
			{
				if (section.IsIntro)
				{
					if (!fIntroMissingInSaved)
					{
						// The saved version contains an introduction so, before a partial import, we would
						// need to remove any sections of introduction this book may have.
						sectionsToRemove.Add(section);
					}
					continue;
				}

				if (versesInCurrentMissingInSaved.Count > iMissingHole + 1 &&
					section.VerseRefStart > versesInCurrentMissingInSaved[iMissingHole].End)
				{
					// Make sure that our hole is synchronized with the current section.
					iMissingHole++;
				}

				MissingVerseRange missingVerses = iMissingHole < versesInCurrentMissingInSaved.Count  ?
					versesInCurrentMissingInSaved[iMissingHole] : null;
				if (missingVerses != null)
				{
					if (section.VerseRefStart >= missingVerses.Start &&
						section.VerseRefEnd <= missingVerses.End)
					{
						// We found either an intro section or a section that is contained within
						// the hole, so we can go on to the next hole in the saved version.
						continue;
					}

					if (section.VerseRefStart <= missingVerses.End &&
						section.VerseRefEnd >= missingVerses.Start)
					{
						// Section overlaps the hole but is not wholly contained in it.
						// We cannot do a partial overwrite.
						return false;
					}

					if (section.VerseRefStart > missingVerses.End)
						iMissingHole++;
				}

				sectionsToRemove.Add(section);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines the sections to remove for overwrite.
		/// </summary>
		/// <param name="savedVersion">The saved version.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public List<IScrSection> DetermineSectionsToRemoveForOverwrite(ScrBook savedVersion)
		{
			int iSectionSaved = 0;
#pragma warning disable 219
			IScrSection sectionSaved = savedVersion.SectionsOS[iSectionSaved];
			foreach (IScrSection sectionCur in SectionsOS)
			{
			}
#pragma warning restore 219
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Detects any "holes" (missing verses between sections) in savedVersion that are not
		/// in this book. Essentially this determines if the Scripture material in the saved
		/// version is a superset of the material in this book. If not, then missing holes will
		/// be detected.
		/// </summary>
		/// <param name="savedVersion">saved version of this book.</param>
		/// <returns>List of misisng holes.</returns>
		/// ------------------------------------------------------------------------------------
		private List<MissingVerseRange> DetectMissingHoles(ScrBook savedVersion)
		{
			List<MissingVerseRange> curHoles = FindHoles();
			List<MissingVerseRange> savedHoles = savedVersion.FindHoles();

			List<MissingVerseRange> missingVerses = new List<MissingVerseRange>();
			int iHoleCur = 0;
			foreach (MissingVerseRange savedRange in savedHoles)
			{
				while (true)
				{
					while (iHoleCur < curHoles.Count &&
						(savedRange.Start > curHoles[iHoleCur].End ||
						(savedRange.Start == curHoles[iHoleCur].End &&
						savedRange.End != curHoles[iHoleCur].Start)))
					{
						// Scan forward through the reference version until we find an
						// overlapping verse range with compareRange.
						iHoleCur++;
					}

					if (iHoleCur >= curHoles.Count)
					{
						// We have handled all the holes in the reference but the comparison has more missing verses.
						//strBldr.AppendLine(savedRange.ToString(Cache.LangProject.TranslatedScriptureOA));
						missingVerses.Add(new MissingVerseRange(savedRange.Start, savedRange.End));
						break;
					}

					if (savedRange.Start < curHoles[iHoleCur].Start)
					{
						// For the current holes being compared, the comparison version's hole starts before that
						// of the reference version. In other words, the reference version has data which extends
						// beyond that which is covered by the comparison version.
						if (curHoles[iHoleCur].Start <= savedRange.End)
						{
							// There is partial overlap of the holes in the two versions.
							MissingVerseRange rng = MissingVerseRange.CreateFromMinAndLim(savedRange.Start, curHoles[iHoleCur].Start);
							//strBldr.AppendLine(rng.ToString(Cache.LangProject.TranslatedScriptureOA));
							missingVerses.Add(rng);
						}
						else // No hole in the reference corresponding to the hole in the comparison
						{
							//strBldr.AppendLine(savedRange.ToString(Cache.LangProject.TranslatedScriptureOA));
							missingVerses.Add(new MissingVerseRange(savedRange.Start, savedRange.End));
						}
					}

					if (savedRange.End <= curHoles[iHoleCur].End)
						break; // ready to look at the next compareRange hole
					else
					{
						// We've dealt with the first part of the hole in the comparison
						// version (where it overlaps a hole in the reference version).
						// So now shrink the hole down and continue comparing it.
						savedRange.SetStartBasedOnPrev(curHoles[iHoleCur].End);
					}
				}
			}

			return missingVerses;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds missing verse ranges in the current book - excluding beginning and end.
		/// </summary>
		/// <returns>List of missing ranges</returns>
		/// ------------------------------------------------------------------------------------
		private List<MissingVerseRange> FindHoles()
		{
			List<MissingVerseRange> holes = new List<MissingVerseRange>();

			if (SectionsOS.Count == 0)
				return holes; // No sections in current, so holes don't need to be updated

			int iCur = 0;
			while (iCur + 1 < SectionsOS.Count && SectionsOS[iCur].IsIntro)
				iCur++;

			int prevEnd = SectionsOS[iCur].VerseRefMax;
			ScrVers versification =
				m_cache.LangProject.TranslatedScriptureOA.Versification;
			for (iCur++; iCur < SectionsOS.Count; prevEnd = SectionsOS[iCur].VerseRefMax, iCur++)
			{
				if (SectionsOS[iCur].VerseRefMin > prevEnd && SectionsOS[iCur].VerseRefMin != prevEnd + 1)
				{
					ScrReference startRef = new ScrReference(prevEnd, versification);
					ScrReference endRef = new ScrReference(SectionsOS[iCur].VerseRefMin, versification);
					ScrReference begRange = (startRef <= endRef) ? startRef : endRef;
					ScrReference endRange = (endRef >= startRef) ? endRef : startRef;

					if (begRange.Chapter == endRange.Chapter)
					{
						holes.Add(MissingVerseRange.CreateFromPrevAndLim(begRange, endRange));
					}
					else if (begRange.Chapter + 1 == endRange.Chapter)
					{
						if (begRange.Verse != begRange.LastVerse)
						{
							holes.Add(MissingVerseRange.CreateFromPrevAndLim(begRange, endRange));
						}
						else if (endRange.Verse != 1)
						{
							holes.Add(MissingVerseRange.CreateFromPrevAndLim(begRange, endRange));
						}
					}
					else
					{
						holes.Add(MissingVerseRange.CreateFromPrevAndLim(begRange, endRange));
					}
				}
			}

			return holes;
		}

		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validation done before adding an object to some vector flid. We need to confirm that
		/// the section is not owned by a different book before we add it.
		/// </summary>
		/// <exception cref="T:InvalidOperationException">The addition is not valid</exception>
		/// ------------------------------------------------------------------------------------
		protected override void ValidateAddObjectInternal(AddObjectEventArgs e)
		{
			if (e.ObjectAdded.Owner != null && e.ObjectAdded.OwnerOfClass<IScrBook>() != this)
				throw new InvalidOperationException("Scripture sections cannot be moved from another book.");
			base.ValidateAddObjectInternal(e);
		}
		#endregion
	}

	#region ScrBookTokenEnumerable class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class that provides an enumerator for parsing Scripture text into tokens that can be
	/// checked.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class ScrBookTokenEnumerable : IEnumerable<ITextToken>
	{
		#region Data members
		private ScrBook m_book;
		private int m_chapterNum;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ScrBookTokenEnumerable"/> class.
		/// </summary>
		/// <param name="book">The book being parsed.</param>
		/// <param name="chapterNum">The 1-basede canonical chapter number being parse, or 0 to
		/// parse the whole book</param>
		/// ------------------------------------------------------------------------------------
		public ScrBookTokenEnumerable(ScrBook book, int chapterNum)
		{
			m_book = book;
			m_chapterNum = chapterNum;
		}
		#endregion

		#region IEnumerable<ITextToken> Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an enumerator that iterates through the tokens.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerator<ITextToken> GetEnumerator()
		{
			return new ScrCheckingTokenizer(m_book, m_chapterNum);
		}

		#endregion

		#region IEnumerable Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an enumerator that iterates through the tokens.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}

	#endregion
}
