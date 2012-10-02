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
// File: ScrSection.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;
using System.Collections.Generic;

namespace SIL.FieldWorks.FDO.Scripture
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ScrSection.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ScrSection
	{
		#region member data
		/// <summary>This is used in the OwningBook property - use OwningBook property instead
		/// </summary>
		private ScrBook m_cachedOwningBook_DONOTUSE = null;
		#endregion

		#region Static methods that create a ScrSection
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new section, to be owned by the given book. Since the StTexts are empty,
		/// this version of the function is generic (i.e. the new section may be made either
		/// an intro section or a scripture text section by the calling code).
		/// </summary>
		/// <param name="book">The book where the new section will be created</param>
		/// <param name="iSection">The zero-based index of the new section</param>
		/// <returns>The newly created <see cref="ScrSection"/></returns>
		/// ------------------------------------------------------------------------------------
		public static IScrSection CreateEmptySection(IScrBook book, int iSection)
		{
			Debug.Assert(book != null);

			FdoCache cache = book.Cache;
			Debug.Assert(iSection >= 0 && iSection <= book.SectionsOS.Count);

			ScrSection prevSection = null;
			if (iSection > 0)
				prevSection = new ScrSection(cache,
					book.SectionsOS.HvoArray[iSection - 1]);

			// Now insert the section in the book at the specified location.
			IScrSection section = (IScrSection)book.SectionsOS.InsertAt(new ScrSection(), iSection);

			// Insert StTexts for section heading and section content.
			section.HeadingOA = new StText();
			section.ContentOA = new StText();

			return section;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new section, to be owned by the given book. The section will be an intro
		/// section if the isIntro flag is set to <code>true</code>.  Empty paragraph is
		/// created in the heading.
		/// </summary>
		/// <param name="book">The book where the new section will be created</param>
		/// <param name="iSection">The zero-based index of the new section</param>
		/// <param name="isIntro">True to create an intro section, false to create a
		/// normal scripture section</param>
		/// <returns>The newly created <see cref="ScrSection"/></returns>
		/// ------------------------------------------------------------------------------------
		public static IScrSection CreateSectionWithHeadingPara(IScrBook book, int iSection,
			bool isIntro)
		{
			return CreateSection(book, iSection, isIntro, true, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new section, to be owned by the given book. The section will be an intro
		/// section if the isIntro flag is set to <code>true</code>.  Empty paragraph is
		/// created in the content.
		/// </summary>
		/// <param name="book">The book where the new section will be created</param>
		/// <param name="iSection">The zero-based index of the new section</param>
		/// <param name="isIntro">True to create an intro section, false to create a
		/// normal scripture section</param>
		/// <returns>The newly created <see cref="ScrSection"/></returns>
		/// ------------------------------------------------------------------------------------
		public static IScrSection CreateSectionWithContentPara(IScrBook book, int iSection,
			bool isIntro)
		{
			return CreateSection(book, iSection, isIntro, false, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new section, to be owned by the given book. The section will be an intro
		/// section if the isIntro flag is set to <code>true</code>. Empty paragraphs are
		/// created in both heading and contents.
		/// </summary>
		/// <param name="book">The book where the new section will be created</param>
		/// <param name="iSection">The zero-based index of the new section</param>
		/// <param name="isIntro">True to create an intro section, false to create a
		/// normal scripture section</param>
		/// <returns>The newly created <see cref="ScrSection"/></returns>
		/// ------------------------------------------------------------------------------------
		public static IScrSection CreateSectionWithEmptyParas(IScrBook book,
			int iSection, bool isIntro)
		{
			return CreateSection(book, iSection, isIntro, true, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a section with optional heading/content paragraphs.
		/// </summary>
		/// <param name="book">The book where the new section will be created</param>
		/// <param name="iSection">The zero-based index of the new section</param>
		/// <param name="isIntro">True to create an intro section, false to create a
		/// normal scripture section</param>
		/// <param name="createHeadingPara">if true, heading paragraph will be created</param>
		/// <param name="createContentPara">if true, content paragraph will be created</param>
		/// <returns>The newly created <see cref="ScrSection"/></returns>
		/// ------------------------------------------------------------------------------------
		private static ScrSection CreateSection(IScrBook book, int iSection, bool isIntro,
			bool createHeadingPara, bool createContentPara)
		{
			Debug.Assert(book != null);

			// Create an empty section. The end reference needs to be set to indicate to
			// AdjustReferences if the section is an intro section
			ScrSection section = (ScrSection)CreateEmptySection(book, iSection);
			section.VerseRefEnd = new ScrReference(book.CanonicalNum, 1, isIntro ? 0 : 1,
				book.Cache.LangProject.TranslatedScriptureOA.Versification);
			section.AdjustReferences();

			// Add an empty paragraph to the section head.
			if (createHeadingPara)
			{
				string paraStyle =
					isIntro ? ScrStyleNames.IntroSectionHead : ScrStyleNames.SectionHead;
				StTxtParaBldr.CreateEmptyPara(book.Cache, section.HeadingOAHvo, paraStyle,
					book.Cache.DefaultVernWs);
			}

			// Add an empty paragraph to the section content.
			if (createContentPara)
			{
				string paraStyle =
					isIntro ? ScrStyleNames.IntroParagraph : ScrStyleNames.NormalParagraph;
				StTxtParaBldr.CreateEmptyPara(book.Cache, section.ContentOAHvo, paraStyle,
					book.Cache.DefaultVernWs);
			}

			return section;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new section, to be owned by the given book. The section will be an intro
		/// section if the isIntro flag is set to <code>true</code>
		/// The contents of the first content paragraph are filled with a single run as
		/// requested. The start and end references for the section are set based on where it's
		/// being inserted in the book.
		/// </summary>
		/// <param name="book">The book where the new section will be created</param>
		/// <param name="iSection">The zero-based index of the new section</param>
		/// <param name="contentText">The text to be used as the first para in the new section
		/// content</param>
		/// <param name="contentTextProps">The character properties to be applied to the first
		/// para in the new section content</param>
		/// <param name="isIntro">True to create an intro section, false to create a
		/// normal scripture section</param>
		/// <returns>Created section</returns>
		/// ------------------------------------------------------------------------------------
		public static IScrSection CreateScrSection(IScrBook book, int iSection, string contentText,
			ITsTextProps contentTextProps, bool isIntro)
		{
			Debug.Assert(book != null);

			IScrSection section = CreateSectionWithHeadingPara(book, iSection, isIntro);

			// Insert the section contents.
			using (StTxtParaBldr bldr = new StTxtParaBldr(book.Cache))
			{
				bldr.ParaProps = StyleUtils.ParaStyleTextProps(
					isIntro ? ScrStyleNames.IntroParagraph : ScrStyleNames.NormalParagraph);
				bldr.AppendRun(contentText, contentTextProps);
				bldr.CreateParagraph(section.ContentOAHvo);
			} // Dispose() frees ICU resources.
			return section;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the lowest reference of the section.
		/// Override to enable debug assertion
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int VerseRefMin
		{
			get { return VerseRefMin_Generated; }
			set
			{
				if (VerseRefMin_Generated != value)
				{
					Debug.Assert(value >= 1000000, "Invalid VerseRefMin value: " + value);
					VerseRefMin_Generated = value;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the highest reference of the section.
		/// Override to enable debug assertion
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int VerseRefMax
		{
			get	{ return VerseRefMax_Generated; }
			set
			{
				if (VerseRefMax_Generated != value)
				{
					Debug.Assert(value >= 1000000, "Invalid VerseRefMax value: " + value);
					VerseRefMax_Generated = value;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the start reference of the section
		/// Override to enable debug assertion
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int VerseRefStart
		{
			get	{ return VerseRefStart_Generated; }
			set
			{
				if (VerseRefStart_Generated != value)
				{
					Debug.Assert(value >= 1000000, "Invalid VerseRefStart value: " + value);
					VerseRefStart_Generated = value;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the end reference of the section
		/// Override to enable debug assertion
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int VerseRefEnd
		{
			get { return VerseRefEnd_Generated; }
			set
			{
				if (VerseRefEnd_Generated != value)
				{
					Debug.Assert(value >= 1000000, "Invalid VerseRefEnd value: " + value);
					VerseRefEnd_Generated = value;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if the first paragraph starts with a verse number or a chapter number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool StartsWithVerseOrChapterNumber
		{
			get
			{
				if (ContentOA.ParagraphsOS.Count == 0)
					return false;

				StTxtPara firstPara = (StTxtPara)ContentOA.ParagraphsOS.FirstItem;
				if (firstPara.Contents.Length == 0)
					return false;
				ITsTextProps ttp = firstPara.Contents.UnderlyingTsString.get_Properties(0);
				return StStyle.IsStyle(ttp, ScrStyleNames.VerseNumber) ||
					StStyle.IsStyle(ttp, ScrStyleNames.ChapterNumber);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if the first paragraph starts with a chapter number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool StartsWithChapterNumber
		{
			get
			{
				if (ContentOA.ParagraphsOS.Count == 0)
					return false;

				StTxtPara firstPara = (StTxtPara)ContentOA.ParagraphsOS.FirstItem;
				ITsTextProps ttp = firstPara.Contents.UnderlyingTsString.get_Properties(0);
				return StStyle.IsStyle(ttp, ScrStyleNames.ChapterNumber);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the context of the current section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ContextValues Context
		{
			get	{ return (IsIntro) ? ContextValues.Intro : ContextValues.Text; }
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Returns true if this section has Content paragraph(s) with a context of Intro. This
		///// is a somewhat expensive call and should only be used in AdjustReferences or other
		///// circumstances when the section references cannot be trusted to provide accurate
		///// information about whether a section is an introductory section.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//private bool HasIntroContents
		//{
		//    get
		//    {
		//        Debug.Assert(HeadingOA.ParagraphsOS.Count > 0 && ContentOA.ParagraphsOS.Count > 0);
		//        foreach (StPara para in HeadingOA.ParagraphsOS)
		//        {
		//            string styleName = para.StyleName;
		//            if (!String.IsNullOrEmpty(styleName))
		//            {
		//                foreach (StStyle style in Cache.LangProject.TranslatedScriptureOA.StylesOC)
		//                    if (style.Name == styleName)
		//                        return (style.Context == ContextValues.Intro);
		//            }
		//        }
		//        return BCVRef.GetVerseFromBcv(VerseRefEnd) == 0;
		//    }
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if this section is an introduction section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsIntro
		{
			get { return BCVRef.GetVerseFromBcv(VerseRefEnd) == 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if the given reference is recognized as that of an introduction section.
		/// (provides a static equivalent of this.IsIntro).
		/// </summary>
		/// <param name="cache">The FDO cache.</param>
		/// <param name="hvoSection">The hvo of the section.</param>
		/// <remarks>
		/// This method exists strictly as a performance enhancement and can be replaced by a
		/// direct call to IsIntro when the cache is ported and it is inexpensive to access the
		/// real ScrSection object.
		/// We come here a lot (gets called from Update handler), so we don't want to construct
		/// a ScrSection object here but get the value we're interested in directly from the
		/// cache. Creating a ScrSection object checks if the HVO is valid. In doing so it does
		/// a query on the database (select Class$ from CmObject...) This happens only in Debug,
		/// but getting the interesting value directly from the cache makes it easier to do SQL
		/// profiling.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public static bool IsIntroSection(FdoCache cache, int hvoSection)
		{
			return BCVRef.GetVerseFromBcv(cache.GetIntProperty(hvoSection,
				(int)ScrSection.ScrSectionTags.kflidVerseRefEnd)) == 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the section in the book's collection of sections.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int IndexInBook
		{
			get { return IndexInOwner; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the owning book object of the section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrBook OwningBook
		{
			get
			{
				// the cached owning book should only be used in this property
				if (m_cachedOwningBook_DONOTUSE == null)
					m_cachedOwningBook_DONOTUSE = new ScrBook(Cache, OwnerHVO);
				return m_cachedOwningBook_DONOTUSE;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the previous section, else null if there is not one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrSection PreviousSection
		{
			get
			{
				int index = IndexInBook;
				if (index == 0)
					return null;
				return (ScrSection)OwningBook.SectionsOS[index - 1];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the next section, else null if there is not one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrSection NextSection
		{
			get
			{
				int index = IndexInBook;
				if (index == OwningBook.SectionsOS.Count - 1)
					return null;
				return (ScrSection)OwningBook.SectionsOS[index + 1];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of content paragraphs in the section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ContentParagraphCount
		{
			get { return ContentOA.ParagraphsOS.Count; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of heading paragraphs in the section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int HeadingParagraphCount
		{
			get { return HeadingOA.ParagraphsOS.Count; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first section content paragraph. If there are no paragraphs,
		/// then null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StTxtPara FirstContentParagraph
		{
			get { return this[0]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last section content paragraph. If there are no paragraphs,
		/// then null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StTxtPara LastContentParagraph
		{
			get { return this[ContentParagraphCount - 1]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the section's content paragraph for the specified index.
		/// If the index is invalid, null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StTxtPara this[int i]
		{
			get
			{
				return (i < 0 || i >= ContentParagraphCount ?
					null : (StTxtPara)ContentOA.ParagraphsOS[i]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first section heading paragraph. If there are no paragraphs,
		/// then null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StTxtPara FirstHeadingParagraph
		{
			get
			{
				return (HeadingParagraphCount == 0 ? null :
					(StTxtPara)HeadingOA.ParagraphsOS[0]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last section heading paragraph. If there are no paragraphs,
		/// then null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StTxtPara LastHeadingParagraph
		{
			get
			{
				int count = HeadingParagraphCount;
				return (count == 0 ? null : (StTxtPara)HeadingOA.ParagraphsOS[count - 1]);
			}
		}

		#endregion

		#region Contains Chapter/Verse
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the section contains a given chapter number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ContainsChapter(int chapter)
		{
			return (chapter >= BCVRef.GetChapterFromBcv(VerseRefMin) &&
				chapter <= BCVRef.GetChapterFromBcv(VerseRefMax));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the section contains a given reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ContainsReference(ScrReference reference)
		{
			return (VerseRefMin <= reference && reference <= VerseRefMax);
		}

		#endregion

		#region Static helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets section that contains the given paragraph.
		/// </summary>
		/// <param name="para"></param>
		/// <returns>the section that owns the paragraph or null if the paragraph is in a
		/// title</returns>
		/// ------------------------------------------------------------------------------------
		public static ScrSection GetSectionFromParagraph(StTxtPara para)
		{
			if (para == null)
				return null;

			// If the paragraph does not belong to a section, then return null
			int sectionHvo = para.Cache.GetOwnerOfObject(para.OwnerHVO);
			return (para.Cache.GetClassOfObject(sectionHvo) != ScrSection.kClassId ?
				null : new ScrSection(para.Cache, sectionHvo));
		}

		#endregion

		#region Other Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnotes in the section heading and contents.
		/// </summary>
		/// <returns>FootnoteInfo list for each footnote in the section</returns>
		/// ------------------------------------------------------------------------------------
		public List<FootnoteInfo> GetFootnotes()
		{
			List<FootnoteInfo> sectionFootnotes = new List<FootnoteInfo>();

			foreach (StTxtPara headingPara in HeadingOA.ParagraphsOS)
				sectionFootnotes.AddRange(headingPara.GetFootnotes());

			foreach (StTxtPara contentPara in ContentOA.ParagraphsOS)
				sectionFootnotes.AddRange(contentPara.GetFootnotes());

			return sectionFootnotes;
		}
		#endregion

		#region Methods for Style changes in a Section Heading
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the heading and content of this section into the previous section content.
		/// </summary>
		/// <param name="cache">the fdocache</param>
		/// <param name="book">the current book</param>
		/// <param name="iSection">index of the section to be moved</param>
		/// <returns>number of paragraphs in previous section before merge (use as index
		/// of first moved paragraph)</returns>
		/// ------------------------------------------------------------------------------------
		public static int MergeIntoPreviousSectionContent(FdoCache cache, ScrBook book, int iSection)
		{
			ScrSection prevSection = (ScrSection) book.SectionsOS[iSection - 1];
			ScrSection origSection = (ScrSection) book.SectionsOS[iSection];
			int cInitParaCount = prevSection.ContentOA.ParagraphsOS.Count;

			StText.MoveTextContents(origSection.HeadingOA, prevSection.ContentOA, true);
			StText.MoveTextContents(origSection.ContentOA, prevSection.ContentOA, true);

			book.SectionsOS.RemoveAt(iSection);

			// Number of paragraphs originally in the target section
			return cInitParaCount;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the requested number of paragraphs from the heading of one section to the end
		/// of the content of the previous section.
		/// </summary>
		/// <param name="cache">the fdocache</param>
		/// <param name="book">the current book</param>
		/// <param name="iSection">index of the section we move paragraphs from</param>
		/// <param name="indexLastPara">index of the last heading paragraph to be moved</param>
		/// <returns>number of paragraphs in previous section before move (use as index
		/// of first moved paragraph)</returns>
		/// ------------------------------------------------------------------------------------
		public static int MoveHeadingToPreviousSectionContent(FdoCache cache, ScrBook book,
			int iSection, int indexLastPara)
		{
			IScrSection prevSection = book.SectionsOS[iSection - 1];
			IScrSection origSection = book.SectionsOS[iSection];  //this*************
			int cInitParaCount = prevSection.ContentOA.ParagraphsOS.Count;

			// Copy the paragraphs from the section heading to the previous section content
			StText.MoveTextParagraphs(origSection.HeadingOA, prevSection.ContentOA,
				indexLastPara, true);

			return cInitParaCount;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves paragraphs from the heading of a section to the beginning of its content.
		/// All paragraphs from the given index to the end of the heading are moved.
		/// </summary>
		/// <param name="indexFirstPara">index of the first heading paragraph to be moved</param>
		/// ------------------------------------------------------------------------------------
		public void MoveHeadingParasToContent(int indexFirstPara)
		{
			Debug.Assert(indexFirstPara >= 0 && indexFirstPara < HeadingOA.ParagraphsOS.Count);

			StText.MoveTextParagraphsAndFixEmpty(HeadingOA, ContentOA, indexFirstPara, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Divides the current section into two sections with section index iSection and
		/// iSection + 1. Moves the selected paragraphs from the heading of
		/// the current section into the content of the current section.
		/// </summary>
		/// <param name="iSection">index of the current section</param>
		/// <param name="iParaStart">index of the first heading paragraph to be moved into
		/// content</param>
		/// <param name="iParaEnd">index of the last heading paragraph to be moved into
		/// content</param>
		/// ------------------------------------------------------------------------------------
		public void SplitSectionHeading(int iSection, int iParaStart, int iParaEnd)
		{
			// Create empty section after the current section
			ScrSection newSection = (ScrSection)ScrSection.CreateEmptySection(OwningBook, iSection + 1);

			// Move Heading and Content to new section:
			// the heading paras after selection,
			StText.MoveTextParagraphs(this.HeadingOA, newSection.HeadingOA,
				iParaEnd + 1, false);
			// all content paras
			StText.MoveTextParagraphs(this.ContentOA, newSection.ContentOA, 0, false);

			// Move current selection in heading to content
			// note: because we already moved the "heading paras after selection"
			// we will now move all paras from start of selection
			MoveHeadingParasToContent(iParaStart);

			// Adjust references for the two sections
			AdjustReferences();
			newSection.AdjustReferences();
			Cache.PropChanged(OwnerHVO, (int)ScrBook.ScrBookTags.kflidSections, iSection + 1, 1, 0);
		}
		#endregion

		#region Methods for Style changes in a Section Content
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new section with the given paragraph as the section head and the remaining
		/// paragraphs after it as the new section content.
		/// </summary>
		/// <param name="para">the given paragraph</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IScrSection ChangeParagraphToSectionHead(StTxtPara para)
		{
			for (int i = 0; i < ContentOA.ParagraphsOS.Count; i++)
				if (ContentOA.ParagraphsOS.HvoArray[i] == para.Hvo)
					return ChangeParagraphToSectionHead(i, 1);

			// Did not find paragraph in content - throw an exception
			throw new ArgumentException("Paragraph is not part of section content");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new section using the contents of the given paragraph as it's heading
		/// and the remaining paragraphs of this section as it's content.
		/// </summary>
		/// <param name="iPara">Index of paragraph to be changed to section head.</param>
		/// <param name="cParagraphs">Number of paragraphs changed.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IScrSection ChangeParagraphToSectionHead(int iPara, int cParagraphs)
		{
			ScrBook book = OwningBook;
			int iSection = 0;
			while (book.SectionsOS.HvoArray[iSection] != Hvo)
			{
				iSection++;
				Debug.Assert(iSection < book.SectionsOS.Count,
					"Couldn't find index of section " + Hvo);
			}
			//Set up parameters depending on the type of change
			iSection++;  //New section follows original one.

			ScrSection newSection = (ScrSection)ScrSection.CreateEmptySection(book, iSection);

			// move paragraphs following the changed paragraphs, if any
			if (iPara + cParagraphs < ContentOA.ParagraphsOS.Count)
			{
				StText.MoveTextParagraphs(ContentOA, newSection.ContentOA, iPara + cParagraphs, false);
			}
			else
			{
				// if the paragraph was at the end of a section then we need to create a
				// blank content paragraph in the new section.
				string styleName =
					IsIntro ? ScrStyleNames.IntroParagraph : ScrStyleNames.NormalParagraph;

				StTxtParaBldr.CreateEmptyPara(book.Cache, newSection.ContentOAHvo, styleName,
					Cache.DefaultVernWs);
			}

			// move paragraphs to section head
			StText.MoveTextParagraphs(ContentOA, newSection.HeadingOA, iPara, false);

			// Make sure the references are correct for both the sections.
			AdjustReferences();
			newSection.AdjustReferences();

			// notify views that new section exists
			m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, book.Hvo,
				(int)ScrBook.ScrBookTags.kflidSections, iSection, 1, 0);

			return newSection as IScrSection;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new section using the contents of the given section heading as it's content
		/// and the remaining paragraphs above the selection will become the heading of the new
		/// section
		/// </summary>
		/// <param name="iPara">index of paragraph to be changed to section content</param>
		/// <param name="cParagraphs">number of paragraphs to move</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IScrSection ChangeParagraphToSectionContent(int iPara, int cParagraphs)
		{
			ScrBook book = OwningBook;
			int iSection = 0;
			while (book.SectionsOS.HvoArray[iSection] != Hvo)
			{
				iSection++;
				Debug.Assert(iSection < book.SectionsOS.Count,
					"Couldn't find index of section " + Hvo);
			}
			ScrSection newSection = (ScrSection)ScrSection.CreateEmptySection(book, iSection);

			// move paragraphs preceding the changed paragraphs, if any
			if (iPara > 0)
				StText.MoveTextParagraphs(HeadingOA, newSection.HeadingOA, iPara-1, true);
			else
			{
				// if the paragraph was at the beginning of a section then we need to create a
				// blank heading paragraph in the new section.
				string styleName =
					IsIntro ? ScrStyleNames.IntroSectionHead : ScrStyleNames.SectionHead;

				StTxtParaBldr.CreateEmptyPara(book.Cache, newSection.HeadingOAHvo, styleName,
					Cache.DefaultVernWs);
			}

			// move paragraphs to section head
			StText.MoveTextParagraphs(HeadingOA, newSection.ContentOA, cParagraphs -1, true);

			// Make sure the sections have correct references
			AdjustReferences();
			newSection.AdjustReferences();

			// notify views that new section exists
			m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, book.Hvo,
				(int)ScrBook.ScrBookTags.kflidSections, iSection, 1, 0);

			return newSection as IScrSection;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves paragraphs from the content of a section to the end of it's heading.
		/// All paragraphs from the beginning of the content to the given index are moved.
		/// </summary>
		/// <param name="indexLastPara">index of the last content paragraph to be moved</param>
		/// ------------------------------------------------------------------------------------
		public void MoveContentParasToHeading(int indexLastPara)
		{
			Debug.Assert(indexLastPara >= 0 && indexLastPara < ContentOA.ParagraphsOS.Count);

			StText.MoveTextParagraphsAndFixEmpty(ContentOA, HeadingOA, indexLastPara, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves one or more paragraphs from the content of one section to the beginning of
		/// the heading of the following section. The paragraphs from the given index to the
		/// last content paragraph are moved.
		/// </summary>
		/// <param name="cache">the fdocache</param>
		/// <param name="book">the current book</param>
		/// <param name="iSection">index of the section we move paragraphs from</param>
		/// <param name="indexFirstPara">index of the first content paragraph to be moved</param>
		/// ------------------------------------------------------------------------------------
		public static void MoveContentParasToNextSectionHeading(FdoCache cache, ScrBook book,
			int iSection, int indexFirstPara)
		{
			Debug.Assert(iSection < book.SectionsOS.Count - 1);
			IScrSection origSection = book.SectionsOS[iSection];
			IScrSection nextSection = book.SectionsOS[iSection + 1];

			// Copy the paragraphs from the section content to the next section heading
			StText.MoveTextParagraphs(origSection.ContentOA, nextSection.HeadingOA,
				indexFirstPara, false);
		}
		#endregion

		#region References
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the starting and ending display references
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void GetDisplayRefs(out BCVRef startRef, out BCVRef endRef)
		{
			startRef = VerseRefMin;
			endRef = VerseRefMax;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the start and end section references to reflect the content of the section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AdjustReferences()
		{
			AdjustReferences(IsIntro);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the start and end section references to reflect the content of the section.
		/// </summary>
		/// <param name="fIsIntro">if set to <c>true</c> this is an intro section.</param>
		/// ------------------------------------------------------------------------------------
		public void AdjustReferences(bool fIsIntro)
		{
			// If this is not the first section then get the previous section's end reference
			// as a starting point for this section
			ScrSection prevSection = PreviousSection;
			ScrReference currentRefStart = new ScrReference(OwningBook.CanonicalNum, 1, 0,
				Cache.LangProject.TranslatedScriptureOA.Versification);
			if (prevSection != null)
				currentRefStart.BBCCCVVV = prevSection.VerseRefEnd;

			// If this is not an intro section then start the verse at 1 so it will not
			// be an intro section.
			if (currentRefStart.Verse == 0 && !fIsIntro)
				currentRefStart.Verse = 1;

			// Default the starting reference for the case that there is no content.
			int newSectionStart = currentRefStart;

			// Scan the paragraphs of the section to get the min and max references
			ScrReference refMin = new ScrReference(currentRefStart);
			ScrReference refMax = new ScrReference(currentRefStart);
			ScrReference currentRefEnd = new ScrReference(currentRefStart);
			bool isFirstTextRun = true;
			if (ContentOA != null)
			{
				foreach (StTxtPara para in ContentOA.ParagraphsOS)
				{
					ITsString paraContents = para.Contents.UnderlyingTsString;
					int iLim = paraContents.RunCount;
					RefRunType runType = RefRunType.None;
					for (int iRun = 0; iRun < iLim; )
					{
						// for very first run in StText we want to set VerseRefStart
						int iLimTmp = (iRun == 0 && isFirstTextRun) ? iRun + 1 : iLim;
						runType = Scripture.GetNextRef(iRun, iLimTmp, paraContents, true,
							ref currentRefStart, ref currentRefEnd, out iRun);

						// If a verse or chapter was found, adjust the max and min if the current
						// verse refs are less than min or greater than max
						if (runType != RefRunType.None)
						{
							// If a chapter or verse is found at the start of the section, then use that
							// reference instead of the one from the previous section as the min and max.
							if (isFirstTextRun || currentRefStart < refMin)
								refMin.BBCCCVVV = currentRefStart.BBCCCVVV;
							if (isFirstTextRun || currentRefEnd > refMax)
								refMax.BBCCCVVV = currentRefEnd.BBCCCVVV;
						}

						// after the first run, store the starting reference
						if (isFirstTextRun)
						{
							newSectionStart = currentRefStart;
							isFirstTextRun = false;
						}
					}
				}
			}
			// Store the min and max as the reference range for the section
			VerseRefStart = newSectionStart;
			VerseRefMin = refMin;
			VerseRefMax = refMax;

			// Store the last reference for the section.
			bool verseRefEndHasChanged = (VerseRefEnd != currentRefEnd.BBCCCVVV);
			bool verseRefEndChapterHasChanged = (BCVRef.GetChapterFromBcv(VerseRefEnd) != currentRefEnd.Chapter);
			VerseRefEnd = currentRefEnd;

			// If the last reference changes then the next section's references have potentially been invalidated
			ScrSection nextSection = NextSection;
			if (nextSection != null)
			{
				if ((verseRefEndChapterHasChanged && !nextSection.StartsWithChapterNumber)||
					(verseRefEndHasChanged && !nextSection.StartsWithVerseOrChapterNumber))
				{
					nextSection.AdjustReferences();
				}
			}
		}
		#endregion
	}
}
