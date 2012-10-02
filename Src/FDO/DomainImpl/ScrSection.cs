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
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;
using System.Collections.Generic;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ScrSection.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class ScrSection
	{
		#region Member variables
		private bool m_cloneInProgress = false;
		#endregion

		#region Properties
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

				IStTxtPara firstPara = (IStTxtPara)ContentOA.ParagraphsOS[0];
				if (firstPara.Contents.Length == 0)
					return false;
				ITsTextProps ttp = firstPara.Contents.get_Properties(0);
				return ttp.Style() == ScrStyleNames.VerseNumber ||
					ttp.Style() == ScrStyleNames.ChapterNumber;
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

				IStTxtPara firstPara = (IStTxtPara)ContentOA.ParagraphsOS[0];
				if (firstPara.Contents.Length == 0)
					return false;
				ITsTextProps ttp = firstPara.Contents.get_Properties(0);
				return ttp.Style() == ScrStyleNames.ChapterNumber;
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
		/// Gets a value indicating whether this instance is first scripture section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsFirstScriptureSection
		{
			get { return !IsIntro && (PreviousSection == null || PreviousSection.IsIntro); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the owning book object of the section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrBook OwningBook
		{
			get { return (IScrBook)Owner; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the previous section, else null if there is not one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrSection PreviousSection
		{
			get
			{
				int index;
				// It's pathologically possible for it to guess another flid, if the object
				// isn't in any property of the owner, as during deletion.
				if (OwningFlidAndIndex(true, out index) != ScrBookTags.kflidSections)
					return null;
				if (index <= 0)
					return null;
				return OwningBook.SectionsOS[index - 1];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the next section, else null if there is not one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrSection NextSection
		{
			get
			{
				int index = IndexInOwner;
				if (index == OwningBook.SectionsOS.Count - 1)
					return null;
				return (IScrSection)OwningBook.SectionsOS[index + 1];
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
		/// Gets a list of all paragraphs in this section in their natural order (i.e., heading
		/// paragraphs first, then content pragraphs). This does not include footnote paragraphs
		/// or picture captions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<IScrTxtPara> Paragraphs
		{
			get
			{
				List<IScrTxtPara> paras = new List<IScrTxtPara>();
				foreach (IScrTxtPara para in HeadingOA.ParagraphsOS)
					paras.Add(para);
				foreach (IScrTxtPara para in ContentOA.ParagraphsOS)
					paras.Add(para);
				return paras;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first section content paragraph. If there are no paragraphs,
		/// then null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IStTxtPara FirstContentParagraph
		{
			get
			{
				return (ContentParagraphCount > 0 ? (IStTxtPara)ContentOA.ParagraphsOS[0] : null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last section content paragraph. If there are no paragraphs,
		/// then null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IStTxtPara LastContentParagraph
		{
			get
			{
				int count = ContentParagraphCount;
				return (count > 0 ? (IStTxtPara)ContentOA.ParagraphsOS[count - 1] : null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first section heading paragraph. If there are no paragraphs,
		/// then null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IStTxtPara FirstHeadingParagraph
		{
			get
			{
				return (HeadingParagraphCount > 0 ? (IStTxtPara)HeadingOA.ParagraphsOS[0] : null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last section heading paragraph. If there are no paragraphs,
		/// then null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IStTxtPara LastHeadingParagraph
		{
			get
			{
				int count = HeadingParagraphCount;
				return (count > 0 ? (IStTxtPara)HeadingOA.ParagraphsOS[count - 1] : null);
			}
		}

		#endregion

		#region Chapter/Verse-related methods
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the duplicate verse numbers in text.
		/// </summary>
		/// <param name="text">The text (either the remainder of the text following the
		/// paragraph where the insertion ocurred or in the next section)</param>
		/// <param name="iParaStart">The index of the para to start searching for dups.</param>
		/// <param name="wsAlt">The writing system, if a back trans multiString alt,
		/// otherwise 0 for the vernacular</param>
		/// <param name="chapterToRemove">The duplicate chapter number to remove.</param>
		/// <param name="removeUpToVerse">The last duplicate verse number to remove.</param>
		/// <returns><c>true</c> if all remaining duplicates have been removed; <c>false</c>
		/// if caller should re-call this method with the next section (or just give up)
		/// </returns>
		/// ------------------------------------------------------------------------------------
		internal bool RemoveDuplicateVerseNumbersInText(IStText text, int iParaStart,
			int wsAlt, int chapterToRemove, int removeUpToVerse)
		{
			for (int i = iParaStart; i < text.ParagraphsOS.Count; i++)
			{
				// Remove any duplicate verse number in this para or translation
				if (((ScrTxtPara)text[i]).RemoveDuplicateVerseNumbersInPara(wsAlt,
					chapterToRemove, removeUpToVerse, 0))
				{
					return true; // removal is complete
				}
			}
			return false; // removal isn't complete
		}
		#endregion

		#region Other Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnotes in the section heading and contents.
		/// </summary>
		/// <returns>FootnoteInfo list for each footnote in the section</returns>
		/// ------------------------------------------------------------------------------------
		public List<IScrFootnote> GetFootnotes()
		{
			List<IScrFootnote> sectionFootnotes = new List<IScrFootnote>();

			foreach (IScrTxtPara headingPara in HeadingOA.ParagraphsOS)
			{
				foreach (FootnoteInfo info in headingPara.GetFootnotes())
					sectionFootnotes.Add(info.footnote);
			}

			foreach (IScrTxtPara contentPara in ContentOA.ParagraphsOS)
			{
				foreach (FootnoteInfo info in contentPara.GetFootnotes())
					sectionFootnotes.Add(info.footnote);
			}

			return sectionFootnotes;
		}

		#endregion

		#region Find-Footnote methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches section forwards for first footnote reference.
		/// </summary>
		/// <param name="iPara">out: The index of the para containing the footnote.</param>
		/// <param name="ich">out: The character index where the footnote was found.</param>
		/// <param name="tag">out: whether the footnote was found in heading or contents</param>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote FindFirstFootnote(out int iPara, out int ich, out int tag)
		{
			tag = ScrSectionTags.kflidHeading;
			IScrFootnote footnote = (IScrFootnote)HeadingOA.FindFirstFootnote(out iPara, out ich);
			if (footnote == null)
			{
				tag = ScrSectionTags.kflidContent;
				footnote = (IScrFootnote)ContentOA.FindFirstFootnote(out iPara, out ich);
			}
			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches section backwards for last footnote reference.
		/// </summary>
		/// <param name="iPara">out: The index of the para containing the footnote.</param>
		/// <param name="ich">out: The character index where the footnote was found.</param>
		/// <param name="tag">out: whether the footnote was found in heading or contents</param>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote FindLastFootnote(out int iPara, out int ich, out int tag)
		{
			tag = ScrSectionTags.kflidContent;
			IScrFootnote footnote = (IScrFootnote)ContentOA.FindLastFootnote(out iPara, out ich);
			if (footnote == null)
			{
				tag = ScrSectionTags.kflidHeading;
				footnote = (IScrFootnote)HeadingOA.FindLastFootnote(out iPara, out ich);
			}
			return footnote;
		}
		#endregion

		#region Methods to transfer contents between StTexts
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Move the entire Heading or Content paragraphs from srcSection to the Heading or Contents
		/// in another ScrSection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static void MoveAllParas(IScrSection srcSection, int srcField,
			IScrSection destSection, int destField, bool fAtEnd, IStStyle newStyle)
		{
			IStText destText = (destField == ScrSectionTags.kflidContent) ? destSection.ContentOA :
				destSection.HeadingOA;
			int iLastParaSrc = (srcField == ScrSectionTags.kflidContent) ?
				srcSection.ContentOA.ParagraphsOS.Count - 1 : srcSection.HeadingOA.ParagraphsOS.Count - 1;
			int insertIndex = (fAtEnd) ? destText.ParagraphsOS.Count : 0;

			MoveWholeParas(srcSection, srcField, 0, iLastParaSrc, destSection, destField, insertIndex, newStyle);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Use this method to transfer whole paragraphs from one StText to another.
		/// </summary>
		/// <param name="srcSection">ScrSection from which the paragraphs are moved</param>
		/// <param name="srcField">The field (contents or heading) from which we will move paras</param>
		/// <param name="iMinSrc">Index of first paragraph to be moved</param>
		/// <param name="iLastSrc">Index of last paragraph to be moved</param>
		/// <param name="destSection">ScrSection to which the paragraphs are moved</param>
		/// <param name="destField">The destination field (contents or heading).</param>
		/// <param name="destIndex">Index of paragraph before which paragraphs are to be
		/// inserted</param>
		/// <param name="destStyle">The paragraph style to apply to the moved paragraphs.</param>
		/// --------------------------------------------------------------------------------
		internal static void MoveWholeParas(IScrSection srcSection, int srcField, int iMinSrc, int iLastSrc,
			IScrSection destSection, int destField, int destIndex, IStStyle destStyle)
		{
			IStText srcText = (srcField == ScrSectionTags.kflidContent) ? srcSection.ContentOA :
				srcSection.HeadingOA;
			IStText destnText = (destField == ScrSectionTags.kflidContent) ? destSection.ContentOA :
				destSection.HeadingOA;

			bool fStructureChange = (srcField != destField);

			Debug.Assert((fStructureChange && destStyle != null) || (!fStructureChange && destStyle == null),
				"Unexpected style specification: " +
				(fStructureChange ? " changing structure but no style specified." :
				" not changing structure but style is specified."));

			for (int i = iMinSrc; i <= iLastSrc; i++)
			{
				IStTxtPara paraToMove = (IStTxtPara)srcText.ParagraphsOS[iMinSrc];

				// Insert method will remove the para from its prior StText
				destnText.ParagraphsOS.Insert(destIndex, paraToMove);
				destIndex++;

				// Make sure the style is set after moving the paragraph to its new owner
				// so that the generated PropChanges happen in the correct order.
				// (Caused by a NoteDependency on the style rules for the paragraph)
				// (TE-9233)
				if (destStyle != null)
					paraToMove.StyleName = destStyle.Name;

				Debug.Assert(!srcText.ParagraphsOS.Contains(paraToMove),
					"Paragraph not removed from current ScrSection.");
				Debug.Assert(destnText.ParagraphsOS.Contains(paraToMove),
					"Paragraph not added to destination ScrSection.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check if the given StText (contents or heading of this section) is empty.
		/// If so, add an empty paragraph to it.
		/// </summary>
		/// <param name="field">The field (heading or contents).</param>
		/// <param name="styleName">The style name for the new empty paragraph (if needed).</param>
		/// ------------------------------------------------------------------------------------
		internal void FixEmptyStText(int field, string styleName)
		{
			IStText text = (field == ScrSectionTags.kflidContent) ? ContentOA : HeadingOA;

			if (text.ParagraphsOS.Count == 0)
				Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(text, styleName);
		}

		#region Split Heading and Contents into new section
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is needed when a section is to be inserted at an insertion point (split
		/// position) in the heading of this section.
		/// This method creates a new following section, copies the given heading if any
		/// (e.g. from the revision book) as the new heading, and moves the paragraphs after the
		/// split position in this section heading to the new section heading.
		/// </summary>
		///
		/// <param name="iParaSplit">Index of heading paragraph containing the split position</param>
		/// <param name="ichSplit">Character index within the paragraph at which to split</param>
		/// <returns>the new following section</returns>
		///
		/// <exception cref="ArgumentOutOfRangeException">Occurs when iParaSplit or ichSplit
		/// is out of range.</exception>
		/// <exception cref="InvalidStructureException">Occurs if the style of the paragraphs
		/// is not the correct structure.</exception>
		/// ------------------------------------------------------------------------------------
		public IScrSection SplitSectionHeading_atIP(int iParaSplit, int ichSplit)
		{
			// Creates a new following section
			// and moves paragraphs after the specified paragraphs to the new section heading
			IScrSection newSection = SplitSection(ScrSectionTags.kflidHeading, iParaSplit, ichSplit);

			// Make sure that, if the new section heading or content paragraphs are empty,
			// a valid paragraph is set up.
			if (newSection.HeadingOA.ParagraphsOS.Count == 0)
				((ScrSection)newSection).FixEmptyStText(ScrSectionTags.kflidHeading, HeadingOA[0].StyleName);

			// The content of the source section might be empty.
			if (ContentOA.ParagraphsOS.Count == 0)
				FixEmptyStText(ScrSectionTags.kflidContent, newSection.ContentOA[0].StyleName);

			VerifyThatParaStylesHaveCorrectStructure(newSection, ScrSectionTags.kflidHeading);
			VerifyThatParaStylesHaveCorrectStructure(newSection, ScrSectionTags.kflidContent);

			return newSection;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is needed when a section is to be inserted at an insertion point (split
		/// position) in the content of this section.
		/// This method creates a new following section, copies the given heading text
		/// as the new heading with new properties applied, and moves the paragraphs after the
		/// split position in this section content to the new section content.
		/// </summary>
		/// <param name="iParaSplit">Index of heading paragraph containing the split position</param>
		/// <param name="headingText">The ITsString that will become the heading.</param>
		/// <param name="headingStyleName">The style name to apply to the heading paragraph.</param>
		/// <returns>the new following section</returns>
		/// ------------------------------------------------------------------------------------
		public IScrSection SplitSectionContent_atIP(int iParaSplit, ITsString headingText,
			string headingStyleName)
		{
			int iSection = IndexInOwner;

			// Create a new section and add the current paragraph to the heading.
			IScrSection newSection =
				m_cache.ServiceLocator.GetInstance<IScrSectionFactory>().CreateEmptySection(
				(IScrBook)Owner, iSection + 1);

			newSection.HeadingOA.InsertNewPara(-1, headingStyleName, headingText);

			IStStyle headingStyle = Cache.LangProject.TranslatedScriptureOA.FindStyle(headingStyleName);
			if (headingStyle == null)
				throw new ArgumentException("Unknown style :" + headingStyleName);

			// Copy paragraphs after the last added paragraph to the new section.
			MoveWholeParas(this, ScrSectionTags.kflidContent, iParaSplit, ContentOA.ParagraphsOS.Count - 1,
				newSection, ScrSectionTags.kflidContent, 0, null);

			VerifyThatParaStylesHaveCorrectStructure(newSection, ScrSectionTags.kflidHeading);
			VerifyThatParaStylesHaveCorrectStructure(newSection, ScrSectionTags.kflidContent);

			return newSection;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Divides the current section into two sections with section index iSection and
		/// iSection + 1. Moves the selected paragraphs from the heading of
		/// the current section into the content of the current section.
		/// </summary>
		/// <param name="iParaStart">index of the first heading paragraph to be moved into
		/// content</param>
		/// <param name="iParaEnd">index of the last heading paragraph to be moved into
		/// content</param>
		/// <param name="newStyle">The new style for the heading paragraphs that will become
		/// content.</param>
		/// ------------------------------------------------------------------------------------
		public void SplitSectionHeading_ExistingParaBecomesContent(int iParaStart, int iParaEnd,
			IStStyle newStyle)
		{
			int iSection = IndexInOwner;

			// Create empty section after the current section
			IScrSection newSection =
				m_cache.ServiceLocator.GetInstance<IScrSectionFactory>().CreateEmptySection((IScrBook)Owner, iSection + 1);

			// Move remainder of Heading and all the Content to new following section:
			MoveWholeParas(this, ScrSectionTags.kflidHeading, iParaEnd + 1, HeadingOA.ParagraphsOS.Count - 1,
				newSection, ScrSectionTags.kflidHeading, 0, null);
			MoveAllParas(this, ScrSectionTags.kflidContent,
				newSection, ScrSectionTags.kflidContent, false, null);

			// Move current selection in heading to content
			// note: because we already moved the "heading paras after selection"
			// we will now move all paras from start of selection
			MoveHeadingParasToContent(iParaStart, newStyle);

			VerifyThatParaStylesHaveCorrectStructure(this, ScrSectionTags.kflidContent);
			VerifyThatParaStylesHaveCorrectStructure(newSection, ScrSectionTags.kflidHeading);
			VerifyThatParaStylesHaveCorrectStructure(newSection, ScrSectionTags.kflidContent);

			// We shouldn't need to do a PropChanged in the new FDO
			//Cache.PropChanged(Owner.Hvo, ScrBookTags.kflidSections, iSection + 1, 1, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is needed when a section is to be inserted at an insertion point (split
		/// position) in the content of this section.
		/// This method creates a new following section and moves the paragraphs after the
		/// split position in this section content to the new section content.
		/// </summary>
		///
		/// <param name="iParaSplit">Index of content paragraph containing the split position</param>
		/// <param name="ichSplit">Character index within the paragraph at which to split</param>
		/// <returns>the new following section</returns>
		///
		/// <exception cref="ArgumentOutOfRangeException">Occurs when iParaSplit or ichSplit
		/// is out of range.</exception>
		/// <exception cref="InvalidStructureException">Occurs if the style of the paragraphs
		/// is not the correct structure.</exception>
		/// ------------------------------------------------------------------------------------
		public IScrSection SplitSectionContent_atIP(int iParaSplit, int ichSplit)
		{
			return SplitSectionContent_atIP(iParaSplit, ichSplit, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is needed when a section is to be inserted at an insertion point (split
		/// position) in the content of this section.
		/// This method creates a new following section, copies the given heading if any
		/// (e.g. from the revision book) as the new heading, and moves the paragraphs after the
		/// split position in this section content to the new section content.
		/// </summary>
		///
		/// <param name="iParaSplit">Index of content paragraph containing the split position</param>
		/// <param name="ichSplit">Character index within the paragraph at which to split</param>
		/// <param name="newHeading">the StText containing the heading to copy, or null if new
		/// heading is to be empty</param>
		/// <returns>the new following section</returns>
		///
		/// <exception cref="ArgumentOutOfRangeException">Occurs when iParaSplit or ichSplit
		/// is out of range.</exception>
		/// <exception cref="InvalidStructureException">Occurs if the style of the paragraphs
		/// is not the correct structure.</exception>
		/// ------------------------------------------------------------------------------------
		public IScrSection SplitSectionContent_atIP(int iParaSplit, int ichSplit, IStText newHeading)
		{
			// Creates a new following section
			// and moves paragraphs after the specified paragraphs to the new section content
			IScrSection newSection = SplitSection(ScrSectionTags.kflidContent, iParaSplit, ichSplit);

			// Copy the new heading, if available.
			if (newHeading != null && newHeading.ParagraphsOS.Count > 0)
			{
				foreach (IScrTxtPara sourcePara in newHeading.ParagraphsOS)
				{
					//append a new section heading paragaph as the copy destination
					IScrTxtPara newPara = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
						newSection.HeadingOA, sourcePara.StyleName);
					newPara.ReplacePara(sourcePara);
				}
			}

			// Make sure that, if the new section heading or content paragraphs are empty,
			// a valid paragraph is set up.
			if (newSection.HeadingOA.ParagraphsOS.Count == 0)
				((ScrSection)newSection).FixEmptyStText(ScrSectionTags.kflidHeading, HeadingOA[0].StyleName);

			if (newSection.ContentOA.ParagraphsOS.Count == 0)
				((ScrSection)newSection).FixEmptyStText(ScrSectionTags.kflidContent, ContentOA[0].StyleName);

			VerifyThatParaStylesHaveCorrectStructure(newSection, ScrSectionTags.kflidHeading);
			VerifyThatParaStylesHaveCorrectStructure(newSection, ScrSectionTags.kflidContent);

			return newSection;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is needed when an inner paragraph in the content of this section
		/// has its style changed to a heading.
		/// This method creates a new following section using the specified content paragraphs as the new heading
		/// and the remaining paragraphs of this section as its content.
		/// The specified paragraphs must not include the first or last paragraphs. Also, the
		/// caller must make the necessary style changes before calling this method, i.e. the
		/// specified paragraphs must have a section heading style applied.</summary>
		///
		/// <param name="iPara">Index of first content paragraph to be changed to section head.
		/// It must not be the first paragraph.</param>
		/// <param name="cParagraphs">Number of paragraphs to be changed to section head.
		/// The last paragraph in the section content must NOT be included.</param>
		/// <returns>the new following section</returns>
		/// <param name="newStyle">The new style for the content paragraphs that will become
		/// heading.</param>
		///
		/// <exception cref="ArgumentOutOfRangeException">Occurs when iPara is the first paragraph,
		/// or out of range. Also when the count includes the last paragraph.</exception>
		/// <exception cref="InvalidStructureException">Occurs when the style of the specified paragraphs
		/// is not already section heading structure.</exception>
		/// ------------------------------------------------------------------------------------
		public IScrSection SplitSectionContent_ExistingParaBecomesHeading(int iPara, int cParagraphs,
			IStStyle newStyle)
		{
			VerifyParasForHeadingHaveNoReferences(ContentOA, iPara, iPara + cParagraphs - 1, newStyle);

			// Creates a new following section
			// and moves paragraphs after the specified paragraphs to the new section content
			IScrSection newSection = SplitSection(ScrSectionTags.kflidContent, iPara + cParagraphs, 0);

			// move specified paragraphs to new section head
			MoveWholeParas(this, ScrSectionTags.kflidContent, iPara,
				ContentOA.ParagraphsOS.Count - 1, newSection, ScrSectionTags.kflidHeading, 0, newStyle);

			VerifyThatParaStylesHaveCorrectStructure(newSection, ScrSectionTags.kflidHeading);
			VerifyThatParaStylesHaveCorrectStructure(newSection, ScrSectionTags.kflidContent);

			return newSection;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new following section, then moves the paragraphs after the split position
		/// in this section heading or content to the new section heading or content.
		/// </summary>
		/// <param name="field">The field where the split will be created (heading or content)</param>
		/// <param name="iParaSplit">Index of content paragraph containing the split position</param>
		/// <param name="ichSplit">Character index within the paragraph at which to split</param>
		/// <returns>The new following section. The section heading is still empty.</returns>
		/// ------------------------------------------------------------------------------------
		private IScrSection SplitSection(int field, int iParaSplit, int ichSplit)
		{
			// Create a new section immediately following the given section
			int iSection = IndexInOwner;
			IScrSection newSection =
				m_cache.ServiceLocator.GetInstance<IScrSectionFactory>().CreateEmptySection(
					(IScrBook)Owner, iSection + 1);

			// move paragraphs following the changed paragraphs, if any
			int paraCount = (field == ScrSectionTags.kflidContent) ? ContentOA.ParagraphsOS.Count :
				HeadingOA.ParagraphsOS.Count;

			if (iParaSplit == paraCount)
			{
				// On the last paragraph of the StText. Create empty section contents with the
				// properties of the previous paragraph.
				IStText myText = (field == ScrSectionTags.kflidContent) ? ContentOA : HeadingOA;
				IStText newText = (field == ScrSectionTags.kflidContent) ? newSection.ContentOA :
					newSection.HeadingOA;
				newText.AddNewTextPara(myText[iParaSplit - 1].StyleName);
				return newSection;
			}
			if (iParaSplit < 0 || iParaSplit > paraCount)
			{
				throw new ArgumentOutOfRangeException("Invalid paragraph split index of " + iParaSplit +
					" in an StText with " + paraCount + "paragraphs.");
			}
			// if we divide at a paragraph break in Current...
			if (ichSplit == 0)
			{
				// copy complete paragraphs after the split to the new section.
				MoveWholeParas(this, field, iParaSplit, paraCount - 1, newSection, field, 0, null);

				// In some cases the book merger should split the contents mid-verse,
				// but it's unable to do that properly, and all content paras are moved.
				// We need to ensure that the original section content is not left with zero
				// paragraphs, which would be invalid (TE-7132).
				int fieldParaCount = (field == ScrSectionTags.kflidContent) ? ContentOA.ParagraphsOS.Count :
					HeadingOA.ParagraphsOS.Count;
				if (fieldParaCount == 0)
				{
					IScrTxtPara firstPara = (field == ScrSectionTags.kflidContent) ?
						(IScrTxtPara)newSection.ContentOA.ParagraphsOS[0] :
						(IScrTxtPara)newSection.HeadingOA.ParagraphsOS[0];
					FixEmptyStText(field, firstPara.StyleName);
				}
			}

			// we are splitting the section in the middle or end of a paragraph...
			else
			{
				// if we are before the end of the paragraph...
				IScrTxtPara splitPara = (field == ScrSectionTags.kflidContent) ?
					(IScrTxtPara)ContentOA.ParagraphsOS[iParaSplit] :
					(IScrTxtPara)HeadingOA.ParagraphsOS[iParaSplit];

				if (ichSplit < splitPara.Contents.Length)
				{
					//move the partial paragraph after the split point
					MovePartialContentsTo(field, newSection, iParaSplit, ichSplit);
				}
				else
				{
					// We are at the end of a paragraph. Move the whole paragraphs following the split.
					MoveWholeParas(this, field, iParaSplit + 1,
						paraCount - 1, newSection, field, 0, null);
				}
			}

			if (field == ScrSectionTags.kflidHeading)
			{
				// We need to move all of the content paragraphs to the new section if the
				// section split was added from a heading.
				MoveAllParas(this, ScrSectionTags.kflidContent, newSection,
					ScrSectionTags.kflidContent, false, null);
			}

			return newSection;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this method to move a portion of one StText to the same field (i.e. heading or
		/// content) in another section.
		/// </summary>
		/// <param name="field">The field (heading or contents) from and to which contents
		/// will be moved.</param>
		/// <param name="destSection">section to which the contents is moved</param>
		/// <param name="iparaDiv">Index of last partial paragraph to be moved or the first
		/// whole paragraph not moved</param>
		/// <param name="ichDiv">character offset of last character to be moved or zero if
		/// none are to be moved</param>
		/// ------------------------------------------------------------------------------------
		private void MovePartialContentsTo(int field, IScrSection destSection, int iparaDiv,
			int ichDiv)
		{
			IStText srcText = (field == ScrSectionTags.kflidContent) ? ContentOA : HeadingOA;
			IStText destText = (field == ScrSectionTags.kflidContent) ? destSection.ContentOA :
				destSection.HeadingOA;

			int iLastSrcPara = srcText.ParagraphsOS.Count - 1;
			Debug.Assert((iparaDiv >= 0) && (iparaDiv <= iLastSrcPara));

			IStTxtPara divPara = (IStTxtPara)srcText.ParagraphsOS[iparaDiv];
			if (ichDiv > 0 && ichDiv < divPara.Contents.Length)
			{
				divPara.SplitParaAt(ichDiv);
				iLastSrcPara++;
			}

			// Set up parameters for whole paragraph movement based on direction of movement
			//From para following IP to the end, pre-pended
			int iStartAt = (ichDiv > 0) ? iparaDiv + 1 : iparaDiv;
			int iInsertAt = 0;

			// Move the whole paragraphs of srcText to empty destText
			if (iparaDiv != iLastSrcPara || ichDiv == 0)
				MoveWholeParas(this, field, iStartAt, iLastSrcPara, destSection, field, iInsertAt, null);

			if (srcText.ParagraphsOS.Count == 0)
			{
				// We moved all of the paragraphs out of the existing section so we need to
				// create a new paragraph so the user can enter text
				IStTxtPara newSectionFirstPara = destText[0];
				StTxtParaBldr bldr = new StTxtParaBldr(m_cache);
				bldr.ParaStyleName = newSectionFirstPara.StyleName;
				bldr.AppendRun(string.Empty, StyleUtils.CharStyleTextProps(null, m_cache.DefaultVernWs));
				bldr.CreateParagraph(srcText);
			}
		}
		#endregion

		#endregion

		#region Methods for Style changes in a Section Heading
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves paragraphs from the heading of a section to the beginning of its content.
		/// All paragraphs from the given index to the end of the heading are moved.
		/// </summary>
		/// <param name="indexFirstPara">index of the first heading paragraph to be moved</param>
		/// <param name="newStyle">The new style for the moved paragraphs.</param>
		/// <exception cref="ArgumentOutOfRangeException">Occurs when paragraph index is invalid.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public void MoveHeadingParasToContent(int indexFirstPara, IStStyle newStyle)
		{
			// Get paragraph props for first heading para in case we need to create a new one.
			ITsTextProps paraPropsSave = HeadingOA.ParagraphsOS[indexFirstPara].StyleRules;

			MoveWholeParas(this, ScrSectionTags.kflidHeading, indexFirstPara, HeadingOA.ParagraphsOS.Count - 1,
				this, ScrSectionTags.kflidContent, 0, newStyle);

			// Verify that paragraph styles are consistent with the structure changes.
			VerifyThatParaStylesHaveCorrectStructure(this, ScrSectionTags.kflidContent);

			// If heading is now empty, create a new empty paragraph.
			if (HeadingOA.ParagraphsOS.Count == 0)
			{
				string styleName = paraPropsSave.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				StTxtParaBldr.CreateEmptyPara(m_cache, HeadingOA, styleName, m_cache.DefaultVernWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves paragraphs from the content of a section to the end of its heading.
		/// All paragraphs from the beginning of the content to the given index are moved.
		/// </summary>
		/// <param name="indexLastPara">index of the last content paragraph to be moved</param>
		/// <param name="newStyle">The new style for the moved paragraphs.</param>
		/// <exception cref="InvalidStructureException">Occurs when a content paragraph that we
		/// are attempting to move to a heading paragraph contains chapter/verse numbers.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public void MoveContentParasToHeading(int indexLastPara, IStStyle newStyle)
		{
			// Get paragraph props for last content para in case we need to create a new one.
			ITsTextProps paraPropsSave = ContentOA.ParagraphsOS[indexLastPara].StyleRules;

			VerifyParasForHeadingHaveNoReferences(ContentOA, 0, indexLastPara, newStyle);

			MoveWholeParas(this, ScrSectionTags.kflidContent, 0, indexLastPara,
				this, ScrSectionTags.kflidHeading, HeadingOA.ParagraphsOS.Count, newStyle);

			// Verify that paragraph styles are consistent with the structure changes.
			VerifyThatParaStylesHaveCorrectStructure(this, ScrSectionTags.kflidHeading);

			// If content is now empty, create a new empty paragraph.
			if (ContentOA.ParagraphsOS.Count == 0)
			{
				string styleName = paraPropsSave.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				StTxtParaBldr.CreateEmptyPara(m_cache, ContentOA, styleName, m_cache.DefaultVernWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the content paragraphs that to move to heading paragraphs have no
		/// Scripture references.
		/// </summary>
		/// <param name="contents">The contents of the ScrSection.</param>
		/// <param name="startPara">The index of the first content para.</param>
		/// <param name="endPara">The index of the last content para.</param>
		/// <param name="newStyle">The new style for the moved paragraphs.</param>
		/// <exception cref="InvalidStructureException">Occurs when a content paragraph that we
		/// are attempting to move to a heading paragraph contains chapter/verse numbers.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		internal static void VerifyParasForHeadingHaveNoReferences(IStText contents,
			int startPara, int endPara, IStStyle newStyle)
		{
			for (int iPara = startPara; iPara <= endPara; iPara++)
			{
				if (((IScrTxtPara)contents.ParagraphsOS[iPara]).HasChapterOrVerseNumbers())
					throw new InvalidStructureException(newStyle.Name, StructureValues.Body);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that para styles in the specified field (content or heading) have correct
		/// structure.
		/// </summary>
		/// <param name="section">Section to check</param>
		/// <param name="field">The field (Heading or Content) in the section to verify.</param>
		/// <exception cref="InvalidStructureException">Occurs when the style is not compatible
		/// with the field (either heading or content)</exception>
		/// ------------------------------------------------------------------------------------
		internal static void VerifyThatParaStylesHaveCorrectStructure(IScrSection section, int field)
		{
			IStText text;
			StructureValues structure;

			if (field == ScrSectionTags.kflidContent)
			{
				text = section.ContentOA;
				structure = StructureValues.Body;
			}
			else if (field == ScrSectionTags.kflidHeading)
			{
				text = section.HeadingOA;
				structure = StructureValues.Heading;
			}
			else
				throw new ArgumentException("Invalid field. Must be for Heading or Content.");

			foreach (IScrTxtPara para in text.ParagraphsOS)
			{
				ITsTextProps ttp = para.StyleRules;
				string styleName = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				IStStyle style = ((IScripture)section.Owner.Owner).FindStyle(styleName);
				if (style.Structure != structure)
					throw new InvalidStructureException(styleName, structure);
				//Review: FWR-1319 Should FDO verify the Context (Intro, Scripture Text) also?
			}
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
		internal void AdjustReferences()
		{
			AdjustReferences(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the start and end section references to reflect the content of the section.
		/// </summary>
		/// <param name="paraToIgnore">The paragraph to ignore (likely because it's being deleted).</param>
		/// ------------------------------------------------------------------------------------
		internal void AdjustReferences(IScrTxtPara paraToIgnore)
		{
			// Don't want to adjust references while cloning - direct copy is fine.
			if (m_cloneInProgress)
				return;

			if (SectionAdjustmentSuppressionHelper.IsSuppressionActive())
			{
				SectionAdjustmentSuppressionHelper.RegisterSection(this);
				return;
			}

			// If this is not the first section then get the previous section's end reference
			// as a starting point for this section
			IScrSection prevSection = PreviousSection;
			ScrReference currentRefStart = new ScrReference(OwningBook.CanonicalNum, 1, 0,
				Cache.LangProject.TranslatedScriptureOA.Versification);
			if (prevSection != null)
				currentRefStart.BBCCCVVV = prevSection.VerseRefEnd;

			// If this is not an intro section then start the verse at 1 so it will not
			// be an intro section.
			if (currentRefStart.Verse == 0 && !IsIntro)
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
				foreach (IStTxtPara para in ContentOA.ParagraphsOS)
				{
					if (para == paraToIgnore)
						continue;

					ITsString paraContents = para.Contents;
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
			IScrSection nextSection = NextSection;
			if (nextSection != null)
			{
				if ((verseRefEndChapterHasChanged && !nextSection.StartsWithChapterNumber)||
					(verseRefEndHasChanged && !nextSection.StartsWithVerseOrChapterNumber))
				{
					((ScrSection)nextSection).AdjustReferences();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensure that the reference being set has at least the correct book number set.
		/// </summary>
		/// <param name="newValue">An integer expected to be in the form BBCCCVVV</param>
		/// ------------------------------------------------------------------------------------
		partial void ValidateVerseRefStart(ref int newValue)
		{
			ValidateScrRefParam(newValue, "VerseRefStart", OwningBook.CanonicalNum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensure that the reference being set has at least the correct book number set.
		/// </summary>
		/// <param name="newValue">An integer expected to be in the form BBCCCVVV</param>
		/// ------------------------------------------------------------------------------------
		partial void ValidateVerseRefEnd(ref int newValue)
		{
			ValidateScrRefParam(newValue, "VerseRefEnd", OwningBook.CanonicalNum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensure that the reference being set has at least the correct book number set.
		/// </summary>
		/// <param name="newValue">An integer expected to be in the form BBCCCVVV</param>
		/// ------------------------------------------------------------------------------------
		partial void ValidateVerseRefMin(ref int newValue)
		{
			ValidateScrRefParam(newValue, "VerseRefMin", OwningBook.CanonicalNum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensure that the reference being set has at least the correct book number set.
		/// </summary>
		/// <param name="newValue">An integer expected to be in the form BBCCCVVV</param>
		/// ------------------------------------------------------------------------------------
		partial void ValidateVerseRefMax(ref int newValue)
		{
			ValidateScrRefParam(newValue, "VerseRefMax", OwningBook.CanonicalNum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensure that the reference being set has at least the correct book number set.
		/// </summary>
		/// <param name="newValue">An integer expected to be in the form BBCCCVVV</param>
		/// <param name="paramName">The name of the parameter being set (for error reporting)
		/// </param>
		/// <param name="bookNum">The canonical (1-based) book number of the section's owner
		/// </param>
		/// ------------------------------------------------------------------------------------
		private static void ValidateScrRefParam(int newValue, string paramName, int bookNum)
		{
			if (BCVRef.GetBookFromBcv(newValue) != bookNum)
			{
				throw new ArgumentOutOfRangeException(paramName, newValue,
					"Reference values for sections in " + BCVRef.NumberToBookCode(bookNum) +
					" must be between " + (bookNum * 1000000) + " and " + (((bookNum + 1) * 1000000) - 1));
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets properties of section that is being cloned.
		/// </summary>
		/// <param name="clone">Destination object for clone operation</param>
		/// ------------------------------------------------------------------------------------
		public void SetCloneProperties(ICmObject clone)
		{
			ScrSection clonedSection = (ScrSection)clone;
			clonedSection.m_cloneInProgress = true;
			clonedSection.VerseRefStart = VerseRefStart;
			clonedSection.VerseRefEnd = VerseRefEnd;
			clonedSection.VerseRefMin = VerseRefMin;
			clonedSection.VerseRefMax = VerseRefMax;
			CopyObject<IStText>.CloneFdoObject(HeadingOA, x => clonedSection.HeadingOA = x);
			CopyObject<IStText>.CloneFdoObject(ContentOA, x => clonedSection.ContentOA = x);
			clonedSection.m_cloneInProgress = false;
		}

		partial void ValidateContentOA(ref IStText newObjValue)
		{
			if (newObjValue == null)
				throw new InvalidOperationException("New value must not be null.");
		}
		partial void ValidateHeadingOA(ref IStText newObjValue)
		{
			if (newObjValue == null)
				throw new InvalidOperationException("New value must not be null.");
		}
	}
}
