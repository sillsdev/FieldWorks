// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2003' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrTxtPara.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using System.Collections.Generic;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Specialized <see cref="StTxtPara"/> that knows how to apply scripture references.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class ScrTxtPara
	{
		#region Data members
		private IScripture m_scr;
		private bool m_fSupressOrcHandling;
		#endregion

		#region ChapterVerseFound enum
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Flags indicating whether verse and/or chapter numbers are found when getting the
		/// reference at some position in a paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Flags]
		private enum ChapterVerseFound
		{
			/// <summary>Nothing found</summary>
			None = 0,
			/// <summary>Verse was found</summary>
			Verse = 1,
			/// <summary>Chapter was found</summary>
			Chapter = 2
		}
		#endregion ChapterVerseFound enum

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IScripture Scripture
		{
			get
			{
				if (m_scr == null)
					m_scr = m_cache.LangProject.TranslatedScriptureOA;
				return m_scr;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets section that contains this paragraph.
		/// </summary>
		/// <returns>the section that owns the paragraph or null if the paragraph is in a
		/// title</returns>
		/// ------------------------------------------------------------------------------------
		public IScrSection OwningSection
		{
			get { return Owner.Owner as IScrSection; }
		}
		#endregion

		#region Blank Dummy Footnote creation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a blank dummy footnote. Used when a footnote object is missing.
		/// Note: This does not insert an ORC into the paragraph. The caller is fixing the ORC
		/// with a missing object.
		/// </summary>
		/// <param name="sequence">The sequence to which we will add a footnote.</param>
		/// <param name="iFootnote">The 0-based index where the footnote will be inserted in the
		/// owner.</param>
		/// <param name="paraContents">The paragraph string where the ORC is being fixed.</param>
		/// <param name="iRun">The 0-based index of the run from which we will get the writing
		/// system used in the footnote.</param>
		/// <returns>a blank general footnote</returns>
		/// ------------------------------------------------------------------------------------
		protected override IStFootnote CreateBlankDummyFootnote(IFdoOwningSequence<IStFootnote> sequence,
			int iFootnote, ITsString paraContents, int iRun)
		{
			return CreateBlankDummyFootnoteNoRecursion(sequence, iFootnote, paraContents, iRun);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a blank dummy footnote. Used when a footnote object is missing.
		/// Note: This does not insert an ORC into the paragraph. The caller is fixing the ORC
		/// with a missing object.
		/// </summary>
		/// <param name="sequence">The sequence to which we will add a footnote.</param>
		/// <param name="iFootnote">The 0-based index where the footnote will be inserted in the
		/// owner.</param>
		/// <param name="paraContents">The paragraph string where the ORC is being fixed.</param>
		/// <param name="iRun">The 0-based index of the run from which we will get the writing
		/// system used in the footnote.</param>
		/// <returns>a blank general footnote</returns>
		/// ------------------------------------------------------------------------------------
		protected override IStFootnote CreateBlankDummyFootnoteNoRecursion(IFdoOwningSequence<IStFootnote> sequence,
			int iFootnote, ITsString paraContents, int iRun)
		{
			// get the writing system of the existing ORC run
			int nDummy;
			int ws = paraContents.get_Properties(iRun).GetIntPropValues(
				(int)FwTextPropType.ktptWs, out nDummy);

			//  Make a dummy blank footnote
			IStFootnote newFootnote = OwnerOfClass<IScrBook>().CreateFootnote(iFootnote, ws);

			// Create an empty footnote paragraph with properties with default style and writing system.
			IStTxtPara footnotePara = newFootnote.AddNewTextPara(ScrStyleNames.NormalFootnoteParagraph);
			// Insert an empty run into the footnote paragraph in order to set the
			// default writing system.
			ITsStrFactory strFactory = TsStrFactoryClass.Create();
			footnotePara.Contents =	strFactory.MakeString(string.Empty, m_cache.DefaultVernWs);

			return newFootnote;
		}
		#endregion

		#region Real footnote handling
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a List of all footnotes "owned" by this paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<FootnoteInfo> GetFootnotes()
		{
			ITsString contents = Contents;
			List<FootnoteInfo> footnotes = new List<FootnoteInfo>();

			for (int iRun = 0; iRun < contents.RunCount; iRun++)
			{
				Guid guidOfObj = TsStringUtils.GetGuidFromRun(contents, iRun,
					FwObjDataTypes.kodtOwnNameGuidHot);
				if (guidOfObj != Guid.Empty)
				{
					try
					{
						IScrFootnote footnote = Services.GetInstance<IScrFootnoteRepository>().GetObject(guidOfObj);
						footnotes.Add(new FootnoteInfo(footnote));
					}
					catch (KeyNotFoundException) { }
				}
			}

			return footnotes;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches the contents of this paragraph forwards for next footnote ORC.
		/// </summary>
		/// <param name="ich">Character index to start search.</param>
		/// <param name="fSkipCurrentPosition">If <c>true</c> we search forward starting with the
		/// run after ich, otherwise we start with the current run.</param>
		/// <returns>Next footnote in string after ich, or <c>null</c> if footnote can't be
		/// found.</returns>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote FindNextFootnoteInContents(ref int ich, bool fSkipCurrentPosition)
		{
			if (ich == Contents.Length)
				return null;
			int irun = Contents.get_RunAt(ich);
			int runCount = Contents.RunCount;

			if (fSkipCurrentPosition)
				irun++;

			while (irun < runCount)
			{
				ITsTextProps ttp = Contents.get_Properties(irun);
				IScrFootnote footnote =
					Cache.ServiceLocator.GetInstance<IScrFootnoteRepository>().GetFootnoteFromProps(ttp);
				if (footnote != null)
				{
					ich = Contents.get_LimOfRun(irun);
					return footnote;
				}
				irun++;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches the contents of this paragraph backwards for the previous footnote ORC.
		/// </summary>
		/// <param name="ich">Character index to start search, or -1 to start at the end of
		/// the string.</param>
		/// <param name="fSkipCurrentPosition">If <c>true</c> we search backwards starting
		/// with the run before ich, otherwise we start with the run ich is in.</param>
		/// <returns>Previous footnote in string, or <c>null</c> if footnote can't be found.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote FindPrevFootnoteInContents(ref int ich, bool fSkipCurrentPosition)
		{
			if (ich == -1)
			{
				fSkipCurrentPosition = false;
				ich = Contents.Length;
			}

			ITsTextProps ttp = (fSkipCurrentPosition ? null : Contents.get_PropertiesAt(ich));
			IScrFootnote footnote = (fSkipCurrentPosition ? null :
				Cache.ServiceLocator.GetInstance<IScrFootnoteRepository>().GetFootnoteFromProps(ttp));

			int irun = Contents.get_RunAt(ich);
			while (footnote == null && irun > 0)
			{
				irun--;
				ttp = Contents.get_Properties(irun);
				footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteRepository>().GetFootnoteFromProps(ttp);
			}

			ich = Contents.get_MinOfRun(irun);
			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the previous footnote and returns it.
		/// </summary>
		/// <param name="ich">Character index to start search, or -1 to start at the end of
		/// the paragraph.</param>
		/// <returns>Previous footnote, or <c>null</c> if there isn't a previous footnote in the
		/// current book.</returns>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote FindPreviousFootnote(int ich)
		{
			IStText text = (IStText)Owner;
			int iPara = IndexInOwner;
			int flid = text.OwningFlid;
			if (text.Owner is IScrSection)
			{
				IScrSection section = OwningSection;
				int iSection = section.IndexInOwner;
				return ((IScrBook)section.Owner).FindPrevFootnote(ref iSection, ref iPara,
					ref ich, ref flid);
			}
			else if (text.Owner is IScrBook) // Title of a book
				return (IScrFootnote)text.FindPreviousFootnote(ref iPara, ref ich, true);
			else
				throw new InvalidOperationException("Footnotes should only be found in Scripture Book titles, section heads, and contents");
		}
		#endregion

		#region Getting/setting StyleName, Context, and Structure
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the default style (based on context/structure) for the current
		/// paragraph.
		/// </summary>
		/// <exception cref="InvalidOperationException">Unable to get style name for paragraph.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public string DefaultStyleName
		{
			get
			{
				switch (Owner.OwningFlid)
				{
					case ScrSectionTags.kflidHeading:
						return (((IScrSection)Owner.Owner).IsIntro) ? ScrStyleNames.IntroSectionHead :
							ScrStyleNames.SectionHead;
					case ScrSectionTags.kflidContent:
						return (((IScrSection)Owner.Owner).IsIntro) ? ScrStyleNames.IntroParagraph :
							ScrStyleNames.NormalParagraph;
					case ScrBookTags.kflidTitle:
						return ScrStyleNames.MainBookTitle;
					case ScrBookTags.kflidFootnotes:
						return ScrStyleNames.NormalFootnoteParagraph;
				}
				throw new InvalidOperationException("Unable to get style name for paragraph.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the context of the current paragraph.
		/// </summary>
		/// <exception cref="InvalidOperationException">Unable to get context for paragraph.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public ContextValues Context
		{
			get
			{
				switch (Owner.OwningFlid)
				{
					case ScrSectionTags.kflidHeading:
					case ScrSectionTags.kflidContent:
						return ((IScrSection)Owner.Owner).Context;
					case ScrBookTags.kflidTitle:
						return ContextValues.Title;
					case ScrBookTags.kflidFootnotes:
						return ContextValues.Note;
					default:
						throw new InvalidOperationException("Unable to get context for paragraph.");
				}
			}
		}
		#endregion

		#region Verse and Chapter Number methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the start reference based on the hierarchical context of this paragraph.
		/// Usually this will be the min or max reference of the owning ScrSection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private BCVRef OwnerStartRef
		{
			get	{ return GetOwnerBCVRef(true); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the end reference based on the hierarchical context of this paragraph.
		/// Usually this will be the min or max reference of the owning ScrSection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private BCVRef OwnerEndRef
		{
			get { return GetOwnerBCVRef(false); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the start or end reference based on the hierarchical context of this paragraph.
		/// Usually this will be the min or max reference of the owning ScrSection
		/// </summary>
		/// <param name="fStart"><c>true</c> to retrive the start reference; <c>false</c> to
		/// retrieve the end reference</param>
		/// ------------------------------------------------------------------------------------
		private BCVRef GetOwnerBCVRef(bool fStart)
		{
			// Processing depends on "type" of paragraph (content, heading, title...)
			switch (Owner.OwningFlid)
			{
				case ScrSectionTags.kflidContent:
				case ScrSectionTags.kflidHeading:
					return (fStart) ? OwningSection.VerseRefMin : OwningSection.VerseRefMax;
				case ScrBookTags.kflidTitle:
					return new BCVRef(((IScrBook)Owner.Owner).CanonicalNum, 1, 0);
				default:
					throw new InvalidOperationException("Unexpected owning field for ScrTxtPara's owner.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the start and end reference, by searching backwards in the given ITsString
		/// from the given position.
		/// </summary>
		/// <param name="tss">the given ITsString</param>
		/// <param name="ichPos">Index of character in paragraph whose reference we want</param>
		/// <param name="fAssocPrev">Consider this position to be associated with any preceding
		/// text in the paragraph (in the case where ichPos is at a chapter boundary).</param>
		/// <param name="refStart">[out] Start reference for the paragraph.</param>
		/// <param name="refEnd">[out] End reference for the paragraph.</param>
		/// <returns>A value of <see cref="ChapterVerseFound"/> that tells if a chapter and/or
		/// verse number was found in this paragraph.</returns>
		/// <remarks>If ichPos LT zero, we will not search this para, and simply return.
		/// Be careful not to use this method to search the contents of paragraph using
		/// character offsets from the BT!
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		private ChapterVerseFound GetRefsAtPosWithinTss(ITsString tss, int ichPos,
			bool fAssocPrev, out BCVRef refStart, out BCVRef refEnd)
		{
			refStart = new BCVRef();
			refEnd = new BCVRef();
			ChapterVerseFound retVal = ChapterVerseFound.None;

			if (tss.Length <= 0)
				return retVal;

			TsRunInfo tsi;
			ITsTextProps ttpRun;
			bool fGotVerse = false;

			int ich = ichPos;
			if (ich > tss.Length)
				ich = tss.Length;
			while (ich >= 0)
			{
				// Get props of current run.
				ttpRun = tss.FetchRunInfoAt(ich, out tsi);

				// If we're at (the front edge of) a C/V number boundary and the
				// caller said to associate the position with the previous material, then
				// ignore this run unless we're at the beginning of the para.
				// The run is actually the *following*  run, which we don't care about.
				if (!fAssocPrev || ichPos <= 0 || ichPos != tsi.ichMin)
				{
					// See if it is our verse number style.
					if (!fGotVerse && ttpRun.Style() == ScrStyleNames.VerseNumber)
					{
						// The whole run is the verse number. Extract it.
						string sVerseNum = tss.get_RunText(tsi.irun);
						//					string sVerseNum = tss.Text.Substring(tsi.ichMin,
						//						tsi.ichLim - tsi.ichMin);
						int startVerse, endVerse;
						ScrReference.VerseToInt(sVerseNum, out startVerse, out endVerse);
						refStart.Verse = startVerse;
						refEnd.Verse = endVerse;
						fGotVerse = true;
						retVal = ChapterVerseFound.Verse;
					}
					// See if it is our chapter number style.
					else if (ttpRun.Style() == ScrStyleNames.ChapterNumber)
					{
						try
						{
							// Assume the whole run is the chapter number. Extract it.
							string sChapterNum = tss.get_RunText(tsi.irun);
							int nChapter = ScrReference.ChapterToInt(sChapterNum);
							refStart.Chapter = refEnd.Chapter = nChapter;

							if (fGotVerse)
							{
								// Found a chapter number to go with the verse number we
								// already found, so build the full reference using this
								// chapter with the previously found verse (already set).
								retVal |= ChapterVerseFound.Chapter;
							}
							else
							{
								// Found a chapter number but no verse number, so assume the
								// edited text is in verse 1 of the chapter.
								refStart.Verse = refEnd.Verse = 1;
								fGotVerse = true;
								retVal = ChapterVerseFound.Chapter | ChapterVerseFound.Verse;
							}
							break;
						}
						catch (ArgumentException)
						{
							// ignore runs with invalid Chapter numbers
						}
					}
				}

				// move index (going backwards) to the character just before the Min of the run
				// we just looked at
				ich = tsi.ichMin - 1;
			}
			return retVal;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the start and end reference at the end of this paragraph.
		/// </summary>
		/// <param name="refStart">[out] Start reference for the paragraph.</param>
		/// <param name="refEnd">[out] End reference for the paragraph.</param>
		/// <returns>A value of <see cref="ChapterVerseFound"/> that tells if a chapter and/or
		/// verse number was found in the paragraph.</returns>
		/// ------------------------------------------------------------------------------------
		private ChapterVerseFound GetRefsAtEndOfPara(out BCVRef refStart,
			out BCVRef refEnd)
		{
			if (Contents.Text == null)
			{
				refStart = new BCVRef();
				refEnd = new BCVRef();
				return ChapterVerseFound.None;
			}
			return GetRefsAtPosWithinPara(-1, Contents.Length, true, out refStart, out refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the start and end reference, by searching backwards in this paragraph from the
		/// given position.
		/// </summary>
		/// <param name="wsBT">HVO of the writing system of the BT to search, or -1 to search
		/// the vernacular.</param>
		/// <param name="ichPos">Index of character in paragraph whose reference we want</param>
		/// <param name="fAssocPrev">If true, we will search strictly backward if ichPos is at a
		/// chapter boundary).</param>
		/// <param name="refStart">[out] Start reference for the paragraph.</param>
		/// <param name="refEnd">[out] End reference for the paragraph.</param>
		/// <returns>A value of <see cref="ChapterVerseFound"/> that tells if a chapter and/or
		/// verse number was found in this paragraph.</returns>
		/// <remarks>If ichPos LT zero, we will not search this para, and simply return.
		/// </remarks>
		/// <exception cref="NullReferenceException">If wsBT is specified for a non-existant
		/// back translation</exception>
		/// ------------------------------------------------------------------------------------
		private ChapterVerseFound GetRefsAtPosWithinPara(int wsBT, int ichPos,
			bool fAssocPrev, out BCVRef refStart, out BCVRef refEnd)
		{
			ITsString tss;
			if (wsBT > 0)
				tss = GetBT().Translation.get_String(wsBT);
			else
				tss = Contents;

			return GetRefsAtPosWithinTss(tss, ichPos, fAssocPrev, out refStart, out refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the start and end reference of the specified position <paramref name="ivPos"/>
		/// in the paragraph. Section reference could be used, if available, to fill in missing
		/// information, but (at least for now) we will not search back into previous sections.
		/// </summary>
		/// <param name="ivPos">Character offset in the paragraph.</param>
		/// <param name="refStart">[out] Start reference</param>
		/// <param name="refEnd">[out] End reference</param>
		/// <remarks><p><paramref name="refStart"/> and <paramref name="refEnd"/> are only
		/// different if we have bridged verse numbers.</p>
		/// <p>May return incomplete or invalid reference if, for example, the section
		/// object does not have a valid start reference.</p>
		/// <p>If ivPos LT zero, we will not search this para, but look only in previous
		/// paragraphs</p></remarks>
		/// ------------------------------------------------------------------------------------
		public void GetRefsAtPosition(int ivPos, out BCVRef refStart, out BCVRef refEnd)
		{
			GetRefsAtPosition(-1, ivPos, false, out refStart, out refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the start and end reference of the specified position <paramref name="ivPos"/>
		/// in the paragraph. Section reference could be used, if available, to fill in missing
		/// information, but (at least for now) we will not search back into previous sections.
		/// </summary>
		/// <param name="ivPos">Character offset in the paragraph.</param>
		/// <param name="fAssocPrev">Consider this position to be associated with any preceding
		/// text in the paragraph (in the case where ichPos is at a chapter boundary).</param>
		/// <param name="refStart">[out] Start reference</param>
		/// <param name="refEnd">[out] End reference</param>
		/// <remarks><p><paramref name="refStart"/> and <paramref name="refEnd"/> are only
		/// different if we have bridged verse numbers.</p>
		/// <p>May return incomplete or invalid reference if, for example, the section
		/// object does not have a valid start reference.</p>
		/// <p>If ivPos LT zero, we will not search this para, but look only in previous
		/// paragraphs</p></remarks>
		/// ------------------------------------------------------------------------------------
		private void GetRefsAtPosition(int ivPos, bool fAssocPrev, out BCVRef refStart,
			out BCVRef refEnd)
		{
			GetRefsAtPosition(-1, ivPos, fAssocPrev, out refStart, out refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the start and end reference of the specified position <paramref name="ivPos"/>
		/// in the paragraph. Section reference could be used, if available, to fill in missing
		/// information, but (at least for now) we will not search back into previous sections.
		/// </summary>
		/// <param name="wsBT">HVO of the writing system of the BT to search, or -1 to search
		/// the vernacular.</param>
		/// <param name="ivPos">Character offset in the paragraph.</param>
		/// <param name="fAssocPrev">Consider this position to be associated with any preceding
		/// text in the paragraph (in the case where ichPos is at a chapter boundary).</param>
		/// <param name="refStart">[out] Start reference</param>
		/// <param name="refEnd">[out] End reference</param>
		/// <remarks><p><paramref name="refStart"/> and <paramref name="refEnd"/> are only
		/// different if we have bridged verse numbers.</p>
		/// <p>May return incomplete or invalid reference if, for example, the section
		/// object does not have a valid start reference.</p>
		/// <p>If ivPos LT zero, we will not search this para, but look only in previous
		/// paragraphs</p></remarks>
		/// ------------------------------------------------------------------------------------
		public void GetRefsAtPosition(int wsBT, int ivPos, bool fAssocPrev,
			out BCVRef refStart, out BCVRef refEnd)
		{
			refStart = new BCVRef();
			refEnd = new BCVRef();

			// Might be trying to get the BCVRef in a footnote
			int ownerOwnFlid = Owner.OwningFlid;
			if (ownerOwnFlid == ScrBookTags.kflidFootnotes)
			{
				IScrFootnote footnote = (IScrFootnote)Owner;
				refStart = footnote.StartRef;
				refEnd = footnote.StartRef;
				return;
			}

			BCVRef refStartT = new BCVRef();
			BCVRef refEndT = new BCVRef();
			ChapterVerseFound found = ChapterVerseFound.None;
			bool fGotVerse = false;
			ScrTxtPara para = this; // curent paragraph being examined for reference
			int chvoParas = 0; // count of paragraphs in the section
			int ihvoPara = 0; // index of the paragraph within the section

			BCVRef sectRefStart = OwnerStartRef;
			BCVRef sectRefEnd = OwnerEndRef;

			while (true)
			{
				if (para == this)
				{
					found = para.GetRefsAtPosWithinPara(wsBT, ivPos, fAssocPrev, out refStartT,
						out refEndT);
				}
				else
					found = para.GetRefsAtEndOfPara(out refStartT, out refEndT);

				// if we found a verse, remember it
				if (!fGotVerse && ((found & ChapterVerseFound.Verse) != 0))
				{
					refStart.Verse = refStartT.Verse;
					refEnd.Verse = refEndT.Verse;
					fGotVerse = true;
				}

				// if we found a chapter, process it
				if ((found & ChapterVerseFound.Chapter) != 0)
				{
					if (sectRefStart != null && !sectRefStart.IsEmpty)
						refStart.Book = refEnd.Book = sectRefStart.Book; //may not exist

					refStart.Chapter = refEnd.Chapter = refStartT.Chapter;

					// GetBCVwithinPara always returns a verse if it finds a chapter number
					// so we have already built the full reference
					Debug.Assert(fGotVerse);
					return;
				}

				// We got to the beginning of the paragraph being edited and still haven't
				// found a decent reference for our edited text, so keep looking back to
				// get it from a previous paragraph.

				// First time thru, figure out which paragraph we are in
				if (chvoParas == 0)
				{
					ihvoPara = IndexInOwner;
					chvoParas = ((IStText)Owner).ParagraphsOS.Count;
				}

				// Move to the previous paragraph
				ihvoPara--;

				if (ihvoPara < 0)
				{
					// We are at the beginning of the section. We can't look back any further.
					// ENHANCE TomB: If we search all the way through to the beginning of the
					// section and never get a valid reference, this section begins in the
					// middle of a verse or chapter (unlikely in the case of a verse, but
					// quite likely in the case of a chapter). OR (most likely) this edit
					// happened at the very beginning of the section, and when we start
					// parsing, the first thing we'll get is a decent reference.
					// REVIEW: we're using the section reference, but since they don't get
					// updated, a change (like removing a chapter) in a previous section
					// could mess up this section.
					if (fGotVerse)
					{
						// Use the verse we got previously (already set), along with the
						// first chapter for the section and the book, if available.
						if (sectRefStart != 0)
						{
							refStart.Chapter = refEnd.Chapter = sectRefStart.Chapter;
							refStart.Book = refEnd.Book = sectRefStart.Book;
						}
					}
					else
					{
						// REVIEW:
						// For now, we're just using the first verse for the section, but this
						// could be wrong if the section begins in the middle of a verse bridge
						// or misleading if the section just doesn't yet have verse numbers
						// marked.
						if (sectRefStart != 0)
						{
							refStart = new BCVRef(sectRefStart);
							refEnd = new BCVRef(sectRefStart);
						}

						// If we are looking for a negative position in the first para of a section
						// the needed result is not precisely defined yet,
						// but this is where you could set it
						//						if (para == this && ivPos < 0)
						//							refStart.Verse = refEnd.Verse = ????;
					}
					return;
				}

				// Set up for the previous paragraph in this section, and we'll try again
				para = (ScrTxtPara)((IStText)Owner)[ihvoPara];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a BT string and ich, find the matching verse number string in the given
		/// vernacular string (or the next one if fGetNext is true).
		/// </summary>
		/// <param name="ich">character offset in BT string</param>
		/// <param name="wsBt">writing system of the BT</param>
		/// <param name="fGetNext">if false, we get the associated verse number from the
		/// vernacular; if true, we get the next verse number after the associated one</param>
		/// <param name="sChapterRunVern">the string of a chapter number run,
		/// if one was found just before the verse number we found; else null</param>
		/// <returns>
		/// The verse number string found in the vernacular, or null if none
		/// </returns>
		/// <remarks>If associated verse number match in vernacular is within bridge,
		/// its usefulness depends on fGetNext and whether we match the beginning or the end.
		/// Details are documented in the code. Unuseable matches will return null.</remarks>
		/// ------------------------------------------------------------------------------------
		private string GetMatchingVerseNumberFromVern(int ich, int wsBt, bool fGetNext,
			out string sChapterRunVern)
		{
			string sVerseRunVern = null;
			sChapterRunVern = null;

			BCVRef curRefStartBt;
			BCVRef curRefEndBt;

			// Get the current verse/chapter in the BT at ich
			ChapterVerseFound cvFoundBt = GetRefsAtPosWithinTss(
				GetOrCreateBT().Translation.get_String(wsBt), ich, true,
				out curRefStartBt, out curRefEndBt);

			// If we found a verse...
			if (cvFoundBt == ChapterVerseFound.Verse
				|| cvFoundBt == (ChapterVerseFound.Chapter | ChapterVerseFound.Verse))
			{
				// Find the associated verse number in the vernacular para
				int ichLimVern;
				// First, find the same verse number in the vernacular para
				//   note: param chapterFoundVern is set true because we assume our parent
				//   paragraph is in the right chapter.
				bool chapterFoundVern = true;
				bool verseFoundVern;
				int verseStartVern, verseEndVern;
				verseFoundVern = FindVerseNumberInTss(curRefStartBt, Contents, false,
					ref chapterFoundVern, out ichLimVern, out verseStartVern, out verseEndVern);


				// Did we find matching verse number run in the vernacular para?
				if (verseFoundVern)
				{
					// ichLimVern is already at the Lim of the associated verse number
					Debug.Assert(ichLimVern > 0);
					if (fGetNext)
					{
						// we want to get the following verse
						// if we found a bridge, we can use it only if we match the end of bridge
						if (verseStartVern != verseEndVern && verseEndVern != curRefEndBt.Verse)
						{
							// the end references do not match-- this is not a useful match.
							sVerseRunVern = null;
						}
						else // most common case - get the following verse number
						{
							sVerseRunVern = GetNextVerseNumberRun (Contents, ichLimVern,
								out sChapterRunVern);
						}
					}
					else
					{
						// get the one we found
						sVerseRunVern = Contents.get_RunText(Contents.get_RunAt(ichLimVern - 1));
						// if we found a bridge, we can use it only if we match the start of bridge
						ScrReference.VerseToInt(sVerseRunVern, out verseStartVern, out verseEndVern);
						if (verseStartVern != curRefStartBt.Verse)
						{
							// our match is deep within a bridged verse number; we can't use it
							sVerseRunVern = null;
						}
					}
				}
				else if (ichLimVern > 0)
				{
					// We did not find curRefStartBt in the vernacular, but we did find a
					//   higher verse number.
					Debug.Assert(false, "FindVerseNumberInTss should never find a greater verse");
				}
				else
				{
					// No match and no larger verse number in the vernacular.
					// In this case, user should begin inserting at start of BT para to synchronize
					//   with the vernacular translation.
					sVerseRunVern = null;
				}
			}
			else if (cvFoundBt == ChapterVerseFound.Chapter)
			{
				MiscUtils.ErrorBeep(); // TODO TE-2278: Need to implement this scenario
				return "400 CHAPTER FOUND, NO VERSE";
			}
			else // No chapter or verse found in the back translation
			{
				// Get first verse reference from vernacular, if available
				sVerseRunVern = GetNextVerseNumberRun(Contents, 0, out sChapterRunVern);
			}

			return sVerseRunVern;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a structured string, this method searches for a given target reference. If
		/// found, it returns the position just after the verse number found in that paragraph.
		/// If fStopIfGreaterVerseFound is true and we encounter a greater verse number,
		/// we return false but with information about the greater verse number encountered.
		/// </summary>
		/// <param name="targetRef">The reference being sought</param>
		/// <param name="tss">structured string to search for reference</param>
		/// <param name="fStopIfGreaterVerseFound">true if we want to return false immediately
		/// if we find a greater verse number</param>
		/// <param name="fChapterFound">at call: true if beginning of this tss is already in the
		/// target chapter; upon return: set to true if we encountered the target chapter in
		/// this tss</param>
		/// <param name="ichLim">The index immediately following the verse number found,
		/// or 0 if desired verse number not found.</param>
		/// <param name="startVerseOut">starting verse of the verse number run found,
		/// or 0 if desired verse number not found.</param>
		/// <param name="endVerseOut">ending verse of the verse number run found,
		/// or 0 if desired verse number not found.</param>
		/// <returns>true if matching verse number found, otherwise false</returns>
		/// <remarks>if fStopIfGreaterVerseFound is true and we in fact encounter a greater one,
		/// we return false immediately and the 3 output params provide info about the greater
		/// verse number found</remarks>
		/// ------------------------------------------------------------------------------------
		private bool FindVerseNumberInTss(BCVRef targetRef, ITsString tss,
			bool fStopIfGreaterVerseFound, ref bool fChapterFound, out int ichLim,
			out int startVerseOut, out int endVerseOut)
		{
			ichLim = 0; // default values if not found
			startVerseOut = 0;
			endVerseOut = 0;

			if (tss.Text == null)
				return false;

			TsRunInfo tsi;
			ITsTextProps ttpRun;
			int ich = 0;
			bool fFoundChapterNumHere = false;
			int iRun = 0;
			while (ich < tss.Length)
			{
				// Get props of current run.
				ttpRun = tss.FetchRunInfoAt(ich, out tsi);
				// If we are already in our target chapter
				if (fChapterFound)
				{
					// See if run is our verse number style.
					if (ttpRun.Style() == ScrStyleNames.VerseNumber)
					{
						// The whole run is the verse number. Extract it.
						string sVerseNum = tss.get_RunText(tsi.irun);
						int startVerse, endVerse;
						ScrReference.VerseToInt(sVerseNum, out startVerse, out endVerse);
						if (startVerse <= targetRef.Verse && endVerse >= targetRef.Verse)
						{
							ichLim = tsi.ichLim; //end of the verse number run
							startVerseOut = startVerse;
							endVerseOut = endVerse;
							return true;
						}
						else if (fStopIfGreaterVerseFound && startVerse > targetRef.Verse)
						{	// we found a greater verse number and we want to stop on it
							ichLim = tsi.ichLim; //end of the verse number run
							startVerseOut = startVerse;
							endVerseOut = endVerse;
							return false;
						}
					}
					else if (targetRef.Verse == 1 && fFoundChapterNumHere)
					{
						ichLim = tsi.ichMin; //end of the verse number run
						startVerseOut = endVerseOut = 1;
						return true;
					}
				}

				// See if run is our chapter number style.
				if (ttpRun.Style() == ScrStyleNames.ChapterNumber)
				{
					try
					{
						// Assume the whole run is the chapter number. Extract it.
						string sChapterNum = tss.get_RunText(tsi.irun);
						int nChapter = ScrReference.ChapterToInt(sChapterNum);
						// Is this our target chapter number?
						fFoundChapterNumHere = fChapterFound =
							(nChapter == targetRef.Chapter || fChapterFound);
					}
					catch (ArgumentException)
					{
						// ignore runs with invalid Chapter numbers
					}
				}
				ich = tsi.ichLim;
				iRun++;
			}

			// Verse was not found in the tss
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given an IP selection in a vernacular paragraph, if the following run is a verse
		/// number, determine if a verse number is missing and if so insert it at the IP.
		/// </summary>
		/// <param name="ichIp">The character position in the paragraph</param>
		/// <param name="ichMinNextVerse">The character offset at the beginning of the following
		/// verse number run</param>
		/// <param name="sVerseNumIns">output: String representation of the new verse number
		/// inserted, or null if none inserted</param>
		/// <param name="ichLimInserted">output: set to the end of the inserted verse number
		/// run, or -1 if none inserted</param>
		/// ------------------------------------------------------------------------------------
		public void InsertMissingVerseNumberInVern(int ichIp, int ichMinNextVerse,
			out string sVerseNumIns, out int ichLimInserted)
		{
			sVerseNumIns = null;
			ichLimInserted = -1;

			// get info on the following verse number run
			if (Contents.StyleAt(ichMinNextVerse) != ScrStyleNames.VerseNumber)
				return;

			string sVerseNumNext = Contents.get_RunText(Contents.get_RunAt(ichMinNextVerse));
			if (sVerseNumNext == null)
				return;
			int startVerseNext = ScrReference.VerseToIntStart(sVerseNumNext);

			// Determine the appropriate verse number string to consider inserting at the IP
			sVerseNumIns = GetVernVerseNumberToInsert(ichIp, false);
			if (sVerseNumIns == null)
				return;
			int startVerseInsert = ScrReference.VerseToIntStart(sVerseNumIns);

			// Is number to insert less than the next existing verse number?
			if (startVerseInsert < startVerseNext)
			{
				// If so, the  number to insert is missing! We need to insert it at the IP.

				// Add space, if needed, before verse is inserted.
				int ichIns = ichIp;
				InsertSpaceIfNeeded(ichIns, 0, ref ichIns);

				// Now insert the new verse number
				int defVernWs = m_cache.DefaultVernWs;
				ITsTextProps ttpVerse = StyleUtils.CharStyleTextProps(
					ScrStyleNames.VerseNumber, defVernWs);

				ReplaceInParaOrBt(0, sVerseNumIns, ttpVerse, ichIns, ichIns, out ichLimInserted);
			}
			else
				sVerseNumIns = null; // nothing inserted
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a space if the character before the ichIp is not white space and not in a
		/// chapter number run.
		/// This method is used when the insert is to be at the original IP, not when the ich
		/// was moved to a "word" boundary.
		/// </summary>
		/// <param name="ichIp">the ich into the tss at the IP</param>
		/// <param name="wsAlt">The writing system, if a back translation multiString alt,
		/// otherwise 0 for the vernacular</param>
		/// <param name="ichLimInserted">ref: the end of the inserted verse number run;
		/// not changed if nothing inserted</param>
		/// ------------------------------------------------------------------------------------
		private void InsertSpaceIfNeeded(int ichIp, int wsAlt, ref int ichLimInserted)
		{
			int ichIns = ichIp;
			if (ichIns > 0)
			{
				//If previous character is neither white space nor a chapter number...
				ITsString tss = (wsAlt == 0) ? Contents : GetOrCreateBT().Translation.get_String(wsAlt);
				ITsTextProps ttp = tss.get_PropertiesAt(ichIns - 1);
				if (!m_cache.ServiceLocator.UnicodeCharProps.get_IsSeparator(tss.Text[ichIns - 1]) &&
					ttp.Style() != ScrStyleNames.ChapterNumber)
				{
					//add a space.
					ReplaceInParaOrBt(wsAlt, " ", tss.get_PropertiesAt(ichIns),
						ichIns, ichIns, out ichLimInserted);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a position and selection in a vernacular paragraph string, use the current
		/// reference and context to build the appropriate verse number string to insert at the
		/// ich position. Ususally the current verse reference + 1.
		/// </summary>
		/// <param name="ich">The character position we would like to insert a verse number at.
		/// This is either on the current selection, or moved to a nearby "word" boundary.</param>
		/// <param name="fInVerse">true if ich is at an existing verse number</param>
		/// <returns>
		/// The verse number string to insert, or null if the verse number would be
		/// out of range
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private string GetVernVerseNumberToInsert(int ich, bool fInVerse)
		{
			//Get the BCV end ref at the current selection
			BCVRef refEnd, dummy;
			GetRefsAtPosition(0, ich, true, out dummy, out refEnd);
			ScrReference refToInsert = new ScrReference(refEnd, Scripture.Versification);

			// Note that our ich may be at a "word" boundary near the selection.
			// If the ich is on a verse number, update refToInsert.Verse to its end value.
			if (fInVerse)
			{
				string sVerseRun = Contents.get_RunText(Contents.get_RunAt(ich));
				refToInsert.Verse = ScrReference.VerseToIntEnd(sVerseRun);
			}

			//  If verse number is already at the end of this chapter (or beyond), quit now!
			if (refToInsert.Verse >= refToInsert.LastVerse)
				return null;

			// Calculate the default next verse number: current verse ref + 1
			string sVerseNum = Scripture.ConvertToString(refToInsert.Verse + 1);

			// If we are already in a verse, we are done; this is the usual case for a verse num update
			if (fInVerse)
				return sVerseNum;

			// we are inserting in text at ich...

			// If we are at the beginning of the first scripture section, we insert verse 1.
			IScrSection section = OwnerOfClass<IScrSection>();
			if (IndexInOwner == 0 && ich == 0 && section != null && section.IsFirstScriptureSection)
				sVerseNum = Scripture.ConvertToString(1);

			// If we directly follow a chapter number, we insert verse 1!
			if (ich > 0 && Contents.StyleAt(ich - 1) == ScrStyleNames.ChapterNumber)
				sVerseNum = Scripture.ConvertToString(1);

			return sVerseNum;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the text of the next verse number run in a given ITsString after the given
		/// character position.
		/// </summary>
		/// <param name="tss">the given ITsString</param>
		/// <param name="ich">the given character position</param>
		/// <param name="sChapterNumberRun">the string of a chapter number run,
		/// if one was found just before the verse number we found; else null</param>
		/// <returns>the string of the next verse number after the given ich, or null if not
		/// found</returns>
		/// ------------------------------------------------------------------------------------
		private string GetNextVerseNumberRun(ITsString tss, int ich, out string sChapterNumberRun)
		{
			sChapterNumberRun = null;

			if (tss.Text == null)
				return null;

			TsRunInfo tsi;
			ITsTextProps ttpRun;
			while (ich < tss.Length)
			{
				// Get props of current run.
				ttpRun = tss.FetchRunInfoAt(ich, out tsi);
				// See if run is our verse number style.
				if (ttpRun.Style() == ScrStyleNames.VerseNumber)
				{
					// The whole run is the verse number. Extract it.
					string sVerseNumberRun = tss.get_RunText(tsi.irun);

					// Also extract a preceeding chapter number run, if present.
					if (tsi.ichMin > 0)
					{
						// Get props of previous run.
						ttpRun = tss.FetchRunInfoAt(tsi.ichMin - 1, out tsi);
						// See if run is chapter number style; get its text.
						if (ttpRun.Style() == ScrStyleNames.ChapterNumber)
							sChapterNumberRun = tss.get_RunText(tsi.irun);
					}
					return sVerseNumberRun;
				}

				// if this is a chapter number, check the next run to see if it is a
				// verse number (perhaps it is implied).
				if (ttpRun.Style() == ScrStyleNames.ChapterNumber &&
					tsi.irun < tss.RunCount - 1)
				{
					ITsTextProps ttpNextRun = tss.get_Properties(tsi.irun + 1);
					if (ttpNextRun.Style() != ScrStyleNames.VerseNumber)
					{
						// verse 1 is implied; get the chapter number
						sChapterNumberRun = tss.get_RunText(tsi.irun);
						return null;
					}
				}

				ich = tsi.ichLim; // get ready for next run
			}

			// no verse number found
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a verse number run in a vernacular paragraph string, create or extend a verse
		/// bridge.
		/// </summary>
		/// <param name="ichMin">The character offset at the beginning of the verse number
		/// run</param>
		/// <param name="ichLim">the end of the verse number run</param>
		/// <param name="sVerseNumIns">output: String representation of the new end number appended
		/// to verse bridge</param>
		/// <param name="ichLimIns">output: the end offset of the updated verse number run</param>
		/// <returns>
		/// 	<c>true</c> if we updated a verse number/bridge; false if not
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool UpdateExistingVerseNumberInVern(int ichMin, int ichLim,
			out string sVerseNumIns, out int ichLimIns)
		{
			Debug.Assert(ichMin < ichLim);
			ichLimIns = -1;

			// Determine the appropriate verse number string to insert here
			sVerseNumIns = GetVernVerseNumberToInsert(ichMin, true);
			if (sVerseNumIns == null)
			{
				MiscUtils.ErrorBeep();
				return false;
			}

			IWritingSystem wsObj = Services.WritingSystems.DefaultVernacularWritingSystem;
			int defVernWs = wsObj.Handle;
			int iRun = Contents.get_RunAt(ichMin);
			string sCurrVerseNumber = Contents.get_RunText(iRun);

			string bridgeJoiner = m_scr.BridgeForWs(m_cache.DefaultVernWs);
			int bridgePos = sCurrVerseNumber.IndexOf(bridgeJoiner, StringComparison.Ordinal);
			int ichInsAt;
			ITsTextProps ttpVerse = StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, defVernWs);

			if (bridgePos != -1)
			{
				// Add to existing verse bridge (add an additional pylon or two)
				ichInsAt = ichMin + bridgePos + bridgeJoiner.Length;
				ReplaceInParaOrBt(0, sVerseNumIns, ttpVerse, ichInsAt, ichLim, out ichLimIns);
			}
			else
			{
				// Create a verse bridge by adding to the existing number
				ichInsAt = ichLim;
				ReplaceInParaOrBt(0, bridgeJoiner + sVerseNumIns, ttpVerse, ichInsAt, ichInsAt, out ichLimIns);
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a position in a vernacular paragraph string, insert the next verse number.
		/// </summary>
		/// <param name="ich">The character position at which to insert verse number</param>
		/// <param name="sVerseNumIns">The s verse num ins.</param>
		/// <param name="ichLim">Gets set to the end of the new verse number run</param>
		/// <returns>
		/// 	<c>true</c> if we inserted a verse number; <c>false</c> if not
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool InsertNextVerseNumberInVern(int ich, out string sVerseNumIns, out int ichLim)
		{
			// Insert next verse in vernacular
			sVerseNumIns = GetVernVerseNumberToInsert(ich, false);

			if (sVerseNumIns == null)
			{
				// nothing to insert
				ichLim = ich;
				return false;
			}

			// Insert the verse number
			ITsTextProps ttp = StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				m_cache.DefaultVernWs);

			ReplaceInParaOrBt(0, sVerseNumIns, ttp, ich, ich, out ichLim);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a verse number run in the back translation string, try to locate the
		/// corresponding verse in the vernacular, and update the verse number in the BT.
		/// </summary>
		/// <param name="wsAlt">The writing system of the back trans multiString alt</param>
		/// <param name="ichMin">The character offset at the beginning of the BT verse number
		/// run</param>
		/// <param name="ichLim">the end of the verse number run</param>
		/// <param name="sVerseNumIns">output: String representation of the new end number appended
		/// to verse bridge</param>
		/// <param name="sChapterNumIns">output: String containing the inserted chapter number,
		/// or null if no chapter number inserted</param>
		/// <param name="ichLimIns">output: the end offset of the updated chapter/verse numbers</param>
		/// ------------------------------------------------------------------------------------
		public void UpdateExistingVerseNumberInBt(int wsAlt, int ichMin, int ichLim,
			out string sVerseNumIns, out string sChapterNumIns, out int ichLimIns)
		{
			if (ichMin == 0)
			{
				// We are at start of BT para: get the first verse number in the vernacular.
				sVerseNumIns = GetBtVerseNumberFromVern(0, wsAlt, false, out sChapterNumIns);
			}
			else
			{
				// We are within the BT para.
				// our preferred algorithm: get previous verse reference from back translation,
				// find its match in the vernacular, then get the next verse num following it
				sVerseNumIns = GetBtVerseNumberFromVern(ichMin - 1, wsAlt, true,
					out sChapterNumIns);
				// an alternate algorithm: get the matching verse num in the vernacular
				//sVerseNumIns = GetBtVerseNumberFromVern(selHelper, tss, ichMin, wsAlt, false);
			}

			ReplaceRangeInBt(wsAlt, ichMin, ichLim, ref sChapterNumIns, ref sVerseNumIns, out ichLimIns);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert/replace the range in the given back translation with the given chapter and verse.
		/// </summary>
		/// <param name="wsAlt">The writing system of the back translation multiString alt</param>
		/// <param name="ichMin">character offset Min at which we will replace in the tss
		///  </param>
		/// <param name="ichLim">end of the range at which we will replace in the tss </param>
		/// <param name="sChapterNumIns">The chapter number string to be inserted, if any</param>
		/// <param name="sVerseNumIns">The verse number string to be inserted, if any</param>
		/// <param name="ichLimIns">output: gets set to the end of what we inserted</param>
		/// ------------------------------------------------------------------------------------
		private void ReplaceRangeInBt(int wsAlt, int ichMin, int ichLim,
			ref string sChapterNumIns, ref string sVerseNumIns, out int ichLimIns)
		{
			ichLimIns = -1;
			ITsString tssBt = GetBT().Translation.get_String(wsAlt);

			// Insert the chapter number, if defined
			if (sChapterNumIns != null)
			{
				int iRun = tssBt.get_RunAt(ichMin);
				string oldText = tssBt.get_RunText(iRun);

				if (oldText == sChapterNumIns &&
					tssBt.Style(iRun) == ScrStyleNames.ChapterNumber)
				{
					ichMin += sChapterNumIns.Length;
					sChapterNumIns = null;
				}
				else
				{
					ITsTextProps ttpChap = StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, wsAlt);
					ReplaceInParaOrBt(wsAlt, sChapterNumIns, ttpChap, ichMin, ichLim, out ichLimIns);
					// adjust range for verse insert
					ichMin += sChapterNumIns.Length;
					ichLim = ichMin;
					tssBt = GetBT().Translation.get_String(wsAlt); // don't use out-of-date copy.
				}
			}

			// Insert the verse number, if defined.
			if (sVerseNumIns != null)
			{
				// If the text to be replaced is already the correct verse number, then do not make
				// any change, so an undo task will not be created.
				if (ichMin < ichLim && tssBt.GetChars(ichMin, ichLim) == sVerseNumIns &&
					tssBt.StyleAt(ichMin) == ScrStyleNames.VerseNumber)
				{
					sVerseNumIns = null;
				}
				else
				{
					ITsTextProps ttpVerse = StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, wsAlt);
					ReplaceInParaOrBt(wsAlt, sVerseNumIns, ttpVerse, ichMin, ichLim, out ichLimIns);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a structured string in a paragraph or back translation, replaces the given
		/// range with a given string and given run props. Updates the cache.
		/// </summary>
		/// <param name="wsAlt">The writing system, if a back translation multiString alt,
		/// otherwise 0 for the vernacular</param>
		/// <param name="str">the string to be inserted; if null, we remove the range</param>
		/// <param name="ttp">The text props for the string being inserted</param>
		/// <param name="ichMin">character offset Min at which we will replace in the tss
		///  </param>
		/// <param name="ichLim">end of the range at which we will replace in the tss </param>
		/// <param name="ichLimNew">gets set to the end of what we inserted</param>
		/// ------------------------------------------------------------------------------------
		public void ReplaceInParaOrBt(int wsAlt, string str, ITsTextProps ttp, int ichMin,
			int ichLim, out int ichLimNew)
		{
			Debug.Assert(wsAlt >= 0);
			// Insert in the given string in place of the existing range
			int cchIns = (str == null ? 0 : str.Length);
			ITsString tss = (wsAlt == 0) ? Contents : GetOrCreateBT().Translation.get_String(wsAlt);
			ITsStrBldr tsb = tss.GetBldr();
			tsb.Replace(ichMin, ichLim, str, ttp);
			tss = tsb.GetString();

			// Update the cache with the new tss...
			if (wsAlt == 0) //vernacular para
				Contents = tss;
			else // translation
			{
				GetOrCreateBT().Translation.set_String(wsAlt, tss);
			}

			// calculate the end of what was inserted
			ichLimNew = ichMin + cchIns;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a string representing the next verse number or verse bridge from the vernacular
		/// for inserting (or updating) the back translation at or following ich.
		/// Also a string for the chapter number, if found in the vernacular.
		/// Result strings are in lower ASCII digits.
		/// </summary>
		/// <param name="ichBt">The character offset in the BT paragraph</param>
		/// <param name="wsBt">The writing system of the BT</param>
		/// <param name="fGetNext">if false, we get the associated verse number from the
		/// vernacular); if true, we get the next verse number after the associated one</param>
		/// <param name="sChapNumberBt">the chapter number string in lower ASCII digits,
		/// if one was found just before the verse number we found</param>
		/// <returns>
		/// The verse number string in lower ASCII digits, or null if there is no
		/// corresponding verse number in the vernacular.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private string GetBtVerseNumberFromVern(int ichBt, int wsBt, bool fGetNext,
			out string sChapNumberBt)
		{
			// Get matching verse and/or chapter strings from the vernacular
			sChapNumberBt = null;
			string sVerseRunVern; // verse number run we'll get from the vernacular
			string sChapterRunVern; // chapter number run we may get from the vernacular
			if (ichBt == 0)
			{
				// We are at the start of the BT para--get the first verse number in the
				//     vernacular para
				sVerseRunVern = GetNextVerseNumberRun(Contents, 0, out sChapterRunVern);
			}
			else
			{
				// Get the desired verse number in the vernacular
				sVerseRunVern = GetMatchingVerseNumberFromVern(ichBt, wsBt, fGetNext, out sChapterRunVern);
			}

			// Adjust the vernacular verse/chapter run selections for special cases.
			int ichLimRef_IfRefAtVernStart = GetIchLimRef_IfIchInRefAtParaStart(Contents, 0);
			int ichLimRef_IfIchInRefAtBtStart = GetIchLimRef_IfIchInRefAtParaStart(
				GetOrCreateBT().Translation.get_String(wsBt), ichBt);

			// -1 means paragraph doesn't begin with a chapter/verse number run, or the ich wasn't in it.
			// if the vernacular begins with a reference (chapter/verse) run...
			if (ichLimRef_IfRefAtVernStart != -1)
			{
				// get that first reference in vern
				string sDummy;
				string sFirstVerseVern = GetNextVerseNumberRun(Contents, 0, out sDummy);
				// Design behavior: if we are in the middle of the BT, we don't want to insert
				// a verse number that's at the start of the vern, even if it matches our
				// current BCV reference in the BT. If it does match, we instead we want to get the
				// following verse number from the vernacular.

				// If the ichBt wasn't in chapter/verse reference at start of BT.
				// And, the vernacular paragraph begins with a reference that is the same as
				// the reference selected as a matching verse.
				// And our ichBt is not at the beginning of the BT para...
				if (ichLimRef_IfIchInRefAtBtStart == -1 && sFirstVerseVern == sVerseRunVern &&
					ichBt != 0)
				{
					// IP is not at the start of the back translation, so find the
					// next verse in the vernacular after the begining reference.
					sVerseRunVern = GetNextVerseNumberRun(Contents, ichLimRef_IfRefAtVernStart,
						out sChapterRunVern);
				}
			}
			// else the vernacular does NOT begin with a reference
			else if (ichBt == 0 || ichLimRef_IfIchInRefAtBtStart != -1)
			{
				// Trying to insert (or update) a verse number at the start of a BT para when the
				// vernacular does not begin with a reference. This is illegal.
				sVerseRunVern = null;
				sChapterRunVern = null;
			}

			// Convert chapter number to lower ASCII for back translation
			if (sChapterRunVern != null)
			{
				try
				{
					int chapNum = ScrReference.ChapterToInt(sChapterRunVern);
					sChapNumberBt = chapNum.ToString();
				}
				catch (ArgumentException)
				{
					// ignore runs with invalid Chapter numbers
					sChapNumberBt = null;
				}
			}

			// Convert verse number to lower ASCII for back translation
			if (sVerseRunVern != null)
			{
				// convert verse number to lower ASCII
				int startVerse, endVerse;
				ScrReference.VerseToInt(sVerseRunVern, out startVerse, out endVerse);
				string nextVerseStringBT = startVerse.ToString();
				// TODO: If we support right to left languages in a back translation, then
				// we need to fix the bridge character to include an RTL mark on either side
				// of it.
				if (endVerse > startVerse)
					nextVerseStringBT += "-" + endVerse.ToString();

				return nextVerseStringBT;
			}

			// no verse number found to insert
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the string starts with chapter/verse run and the ich is within one
		/// of these runs.
		/// </summary>
		/// <param name="tss">paragraph string</param>
		/// <param name="ich">character offset</param>
		/// <returns>ich after the reference or -1 if the paragraph does not begin with a
		/// chapter/verse run</returns>
		/// ------------------------------------------------------------------------------------
		private static int GetIchLimRef_IfIchInRefAtParaStart(ITsString tss, int ich)
		{
			int iRun = tss.get_RunAt(ich);
			ITsTextProps firstTtp = tss.get_Properties(0);
			ITsTextProps ttp = tss.get_PropertiesAt(ich);

			if ((ttp.Style() == ScrStyleNames.VerseNumber ||
				ttp.Style() == ScrStyleNames.ChapterNumber) && iRun == 0)
			{
				return GetLimOfReference(tss, iRun, ref ttp);
			}
			else if (firstTtp.Style() == ScrStyleNames.ChapterNumber &&
				ttp.Style() == ScrStyleNames.VerseNumber && iRun == 1)
			{
				// The first run is a chapter and the next is a verse number run.
				// Return the lim of the verse number (current) run.
				return tss.get_LimOfRun(iRun);
			}
			else if (iRun == 0 && IsBlank(tss, iRun))
			{
				// The first run contains only white space.
				// Ignore this run and check the following runs.
				if (tss.RunCount > 1)
				{
					ttp = tss.get_Properties(iRun + 1);
					if (ttp.Style() == ScrStyleNames.VerseNumber ||
						ttp.Style() == ScrStyleNames.ChapterNumber)
					{
						return GetLimOfReference(tss, iRun + 1, ref ttp);
					}
				}
			}

			// Paragraph doesn't begin with a chapter/verse number run (or the ich
			// wasn't in it).
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove any duplicate verse numbers following the new verse number in the following
		/// text in the current as well as the following section, if any.
		/// </summary>
		/// <param name="wsAlt">The writing system, if a back trans multiString alt,
		/// otherwise 0 for the vernacular</param>
		/// <param name="chapterToRemove">A string representation of the duplicate chapter number
		/// to remove.</param>
		/// <param name="verseRangeToRemove">A string representation of the duplicate verse number to
		/// remove. This may also be a verse bridge, in which case we will remove all verse
		/// numbers up to the end value of the bridge</param>
		/// <param name="ich">The character offset after which we start looking for dups</param>
		/// ------------------------------------------------------------------------------------
		public void RemoveDuplicateVerseNumbers(int wsAlt, string chapterToRemove,
			string verseRangeToRemove, int ich)
		{
			// Determine the verse number we will remove up to
			int removeUpToVerse = ScrReference.VerseToIntEnd(verseRangeToRemove);

			// Determine the last chapter reference to remove.
			int removeChapter = (chapterToRemove != null && chapterToRemove.Length > 0) ?
				ScrReference.ChapterToInt(chapterToRemove) : 0;

			// Look to see if there is a matching number there that is not marked with the verse number style, in which
			// case we should remove it.
			string insertedText = chapterToRemove ?? string.Empty + verseRangeToRemove;
			ITsString tss = (wsAlt == 0) ? Contents : GetBT().Translation.get_String(wsAlt);
			if (tss != null && tss.Length >= ich + insertedText.Length &&
				tss.Text.Substring(ich, insertedText.Length) == insertedText)
			{
				int dummy;
				ReplaceInParaOrBt(wsAlt, null, null, ich, ich + insertedText.Length, out dummy);
			}

			// First look in the paragraph where the verse was inserted.
			if (RemoveDuplicateVerseNumbersInPara(wsAlt, removeChapter, removeUpToVerse, ich))
				return;

			IStText text = (IStText)Owner;

			// Search through current and subsequent section (if any) for duplicate verse numbers.
			// First look through successive paragraphs for duplicate verse numbers, and then
			// try the next section if necessary.
			if (!((ScrSection)text.Owner).RemoveDuplicateVerseNumbersInText(text,
				IndexInOwner + 1, wsAlt, removeChapter, removeUpToVerse))
			{
				IScrSection nextSection = ((IScrSection)text.Owner).NextSection;
				if (nextSection != null)
				{
					((ScrSection)nextSection).RemoveDuplicateVerseNumbersInText(
						nextSection.ContentOA, 0, wsAlt, removeChapter, removeUpToVerse);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove any duplicate chapter/verse number(s) following the new verse number, in the
		/// given paragraph or back translation.
		/// </summary>
		/// <param name="wsAlt">The writing system, if a back translation multiString alt,
		/// otherwise 0 for the vernacular</param>
		/// <param name="removeChapter">The chapter number to remove, or 0 if none</param>
		/// <param name="removeUpToVerse">The maximum verse number to remove</param>
		/// <param name="ich">The character offset at which we start looking for dups</param>
		/// <returns><c>true</c> if we're all done removing, <c>false</c> if caller needs to
		/// check subsequent paragraphs
		/// </returns>
		/// ------------------------------------------------------------------------------------
		internal bool RemoveDuplicateVerseNumbersInPara(int wsAlt, int removeChapter,
			int removeUpToVerse, int ich)
		{
			bool fAllDone = false;
			ITsString tss = (wsAlt == 0) ? Contents : GetOrCreateBT().Translation.get_String(wsAlt);

			while (ich < tss.Length)
			{
				int iRun = tss.get_RunAt(ich);
				ITsTextProps ttp = tss.get_Properties(iRun);
				if (removeChapter > 0 && ttp.Style() == ScrStyleNames.ChapterNumber)
				{
					// Process this chapter number run
					string chapterNumberText = tss.get_RunText(iRun);
					int chapterNum = ScrReference.ChapterToInt(chapterNumberText);

					if (chapterNum == removeChapter)
					{
						// Target chapter found. Remove!
						int cchDel = chapterNumberText.Length;
						int dummy;
						ReplaceInParaOrBt(wsAlt, null, null, ich, ich + cchDel, out dummy);

						// since we removed an entire run, we must adjust our current iRun
						if (iRun > 0)
							--iRun;
					}
					//If we found a chapter beyond our target chapter, we are done.
					else if (chapterNum > removeChapter)
						return true;
				}
				else if (tss.Style(iRun) == ScrStyleNames.VerseNumber)
				{
					// Process this verse number run
					string verseNumberText = tss.get_RunText(iRun);
					string bridgeJoiner = Scripture.BridgeForWs(wsAlt);

					// get binary values of this verse number run
					int startNumber, endNumber; //numbers in this verse number run
					ScrReference.VerseToInt(verseNumberText, out startNumber, out endNumber);
					// and the bridge-joiner index, if any
					int bridgeJoinerIndex = verseNumberText.IndexOf(bridgeJoiner, StringComparison.Ordinal);

					if (startNumber <= removeUpToVerse)
					{
						// Target verse(s) found. Remove!
						int cchDel;
						int dummy;

						if (endNumber <= removeUpToVerse)
						{
							//remove all of verseNumberText
							cchDel = verseNumberText.Length;
							ReplaceInParaOrBt(wsAlt, null, null, ich, ich + cchDel, out dummy);

							// since we removed an entire run, we must adjust our current iRun
							if (iRun > 0)
								--iRun;

							if (endNumber == removeUpToVerse)
								fAllDone = true;
						}
						else if (endNumber == removeUpToVerse + 1)
						{
							// reduce to a single verse (ending verse)
							Debug.Assert(bridgeJoinerIndex > -1);
							cchDel = bridgeJoinerIndex + bridgeJoiner.Length;
							ReplaceInParaOrBt(wsAlt, null, null, ich, ich + cchDel, out dummy);

							fAllDone = true;
						}
						else // endNumber > removeUpToVerse + 1
						{
							// set beginning of bridge to max+1
							Debug.Assert(bridgeJoinerIndex > -1);
							cchDel = bridgeJoinerIndex;
							string maxPlusOne = Scripture.ConvertToString(removeUpToVerse + 1);
							ReplaceInParaOrBt(wsAlt, maxPlusOne, ttp, ich, ich + cchDel, out dummy);

							fAllDone = true;
						}
					}
					else // startNumber > removeUpToVerse
					{
						fAllDone = true; //we are done looking.
						//  we assume verse numbers are in order
					}

					if (fAllDone)
						return true;
				}
				// we are not looking to remove a chapter, and the current run is not a verse
				else if (tss.Style(iRun) == ScrStyleNames.ChapterNumber)
				{
					string runText = tss.get_RunText(iRun);
					try
					{
						if (ScrReference.ChapterToInt(runText) != 1)
							return true; // quit when a chapter number other than 1 is found.
					}
					catch (ArgumentException)
					{
						return true; // See TE-9223
					}
					// chapter 1 found after inserted verse number. Remove!
					int cchDel = runText.Length;
					int dummy;
					ReplaceInParaOrBt(wsAlt, null, null, ich, ich + cchDel, out dummy);

					// since we removed an entire run, we must adjust our current iRun
					if (iRun > 0)
						--iRun;
				}
				// We need to re-get the TsString used for this loop since it might have been
				// changed by one of the method calls.
				tss = (wsAlt == 0) ? Contents : GetOrCreateBT().Translation.get_String(wsAlt);
				ich = tss.get_LimOfRun(iRun);
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove the current chapter run if it is not within the specified reference range.
		/// </summary>
		/// <param name="iRun">run index</param>
		/// <param name="wsAlt">the writing system, if a back translation the multiString alt</param>
		/// <param name="startRef">minimum reference allowed in tss</param>
		/// <param name="endRef">maximum reference allowed in tss</param>
		/// <returns>true if chapter number run removed, otherwise false</returns>
		/// <remarks>If the specified run is not a chapter run, it will not be removed.</remarks>
		/// ------------------------------------------------------------------------------------
		private bool RemoveOutOfRangeChapterRun(int iRun, int wsAlt,
			BCVRef startRef, BCVRef endRef)
		{
			ITsString tss = (wsAlt == 0) ? Contents : GetOrCreateBT().Translation.get_String(wsAlt);
			Debug.Assert(iRun >= 0 && iRun < tss.RunCount, "Out of range run index");

			if (tss.Style(iRun) != ScrStyleNames.ChapterNumber)
				return false;

			string runText = tss.get_RunText(iRun);
			int chapterNum = ScrReference.ChapterToInt(runText);
			if (chapterNum < startRef.Chapter || chapterNum > endRef.Chapter)
			{
				// chapter number is out of range. Remove!
				int dummy;
				int cchDel = runText.Length;
				int ich = tss.get_MinOfRun(iRun);
				ReplaceInParaOrBt(wsAlt, null, null, ich, ich + cchDel, out dummy);

				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove chapters in back translation paragraph (btTss) that do not exist in the
		/// vernacular paragraph.
		/// </summary>
		/// <param name="wsAlt">The writing system, if a back translation multiString alt</param>
		/// <param name="startRef">starting reference of vernacular paragraph</param>
		/// <param name="endRef">ending reference of vernacular paragraph</param>
		/// ------------------------------------------------------------------------------------
		private void CleanChapterInBtPara(int wsAlt, BCVRef startRef, BCVRef endRef)
		{
			int iRun = 0;

			while (iRun < GetOrCreateBT().Translation.get_String(wsAlt).RunCount)
			{
				RemoveOutOfRangeChapterRun(iRun, wsAlt, startRef, endRef);
				iRun++;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a position in a back translation string, try to locate the corresponding
		/// chapter in the vernacular and insert it in the BT
		/// (or update the existing reference in the BT).
		/// </summary>
		/// <param name="wsAlt">The writing system of the back trans multiString alt</param>
		/// <param name="ichMin">The ich min.</param>
		/// <param name="ichLim">The ich lim.</param>
		/// <param name="ichLimIns">output: set to the end of the new BT chapter number run</param>
		/// <returns>
		/// 	<c>true</c> if a chapter number was inserted; <c>false</c> otherwise.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool InsertNextChapterNumberInBt(int wsAlt, int ichMin, int ichLim, out int ichLimIns)
		{
			ichLimIns = -1; //default output, if nothing inserted

			ITsString tssBt = GetOrCreateBT().Translation.get_String(wsAlt);

			// Get the corresponding chapter number from the vernacular
#pragma warning disable 219
			string sVerseNumIns, sChapterNumIns;
#pragma warning restore 219
			if (ichMin == 0)
			{
				// We are at the start of BT para--get the first chapter number in the
				//     vernacular para
				sVerseNumIns = GetBtVerseNumberFromVern(0, wsAlt, false, out sChapterNumIns);
			}
			else
			{
				sVerseNumIns = GetBtVerseNumberFromVern(ichMin - 1, wsAlt, true, out sChapterNumIns);
				// an idea:
				//if no matching chapter number found for our current ich,
				// consider attempting to get a chapter number at the start of the vern, and
				// insert/update at the start of the BT
			}

			if (sChapterNumIns == null)
				return false;

			// Insert the chapter number into the tssBt, and update the cache
			int ichLimReplace = ichLim > 0 ? ichLim : ichMin;
			ITsTextProps ttp = StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, wsAlt);
			ReplaceInParaOrBt(wsAlt, sChapterNumIns, ttp, ichMin, ichLimReplace, out ichLimIns);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a position in a back translation string, try to locate the corresponding
		/// verse in the vernacular, and insert the verse number in the BT.
		/// </summary>
		/// <param name="wsAlt">The writing system of the back trans multiString alt</param>
		/// <param name="ich">The character position at which to insert verse number</param>
		/// <param name="sVerseNumIns">output: String containing the inserted verse number,
		/// or null if no verse number inserted</param>
		/// <param name="sChapterNumIns">output: String containing the inserted chapter number,
		/// or null if no chapter number inserted</param>
		/// <param name="ichLimIns">output: Gets set to the end of the new BT chapter/verse numbers
		/// inserted</param>
		/// <returns>
		/// 	<c>true</c> if we inserted a verse number/bridge; <c>false</c> if not
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool InsertNextVerseNumberInBt(int wsAlt, int ich, out string sVerseNumIns,
			out string sChapterNumIns, out int ichLimIns)
		{
			// Get the corresponding verse number from the vernacular
			sVerseNumIns = GetBtVerseNumberFromVern(ich, wsAlt, true, out sChapterNumIns);
			ReplaceRangeInBt(wsAlt, ich, ich, ref sChapterNumIns, ref sVerseNumIns, out ichLimIns);

			// Remove any chapter numbers not in the chapter range of the vernacular para that owns this BT
			BCVRef startRef;
			BCVRef endRef;
			FindParaRefRange(out startRef, out endRef);
			CleanChapterInBtPara(wsAlt, startRef, endRef);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the beginning and ending reference for a given paragraph.
		/// </summary>
		/// <param name="startRef">out: reference at start of paragraph</param>
		/// <param name="endRef">out: reference at end of paragraph</param>
		/// ------------------------------------------------------------------------------------
		private void FindParaRefRange(out BCVRef startRef, out BCVRef endRef)
		{
			BCVRef notUsed;
			GetRefsAtPosition(0, false, out startRef, out notUsed);
			GetRefsAtPosition(Contents.Length, out notUsed, out endRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ending character index of reference run--or runs if a chapter and verse
		/// number are adjacent.
		/// </summary>
		/// <param name="tss">The ITsString.</param>
		/// <param name="iRun">The index of the run.</param>
		/// <param name="ttp">The text properties of the run.</param>
		/// <returns>character index at the end of the reference runs(s)</returns>
		/// ------------------------------------------------------------------------------------
		private static int GetLimOfReference(ITsString tss, int iRun, ref ITsTextProps ttp)
		{
			// The first run is either a verse or chapter number run.
			if (tss.RunCount > iRun + 1)
			{
				// There are more runs so determine if the next run is a verse number.
				// If so, get the lim of this next run.
				if (tss.Style(iRun + 1) == ScrStyleNames.VerseNumber)
					return tss.get_LimOfRun(iRun + 1);
			}

			return tss.get_LimOfRun(iRun);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified run in the ITsString contains only whitespace.
		/// </summary>
		/// <param name="tss">The ITsString.</param>
		/// <param name="iRun">The index of the run into the tss.</param>
		/// <returns>
		/// 	<c>true</c> if the specified run in the ITsString is blank; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private static bool IsBlank(ITsString tss, int iRun)
		{
			if (tss.RunCount > iRun)
			{
				string runText = tss.get_RunText(iRun);
				return (runText == null || runText.Trim().Length == 0);
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if paragraph has chapter or verse numbers in it.
		/// </summary>
		/// <returns><code>true</code> if either a chapter number or verse number is found
		/// in the paragraph</returns>
		/// ------------------------------------------------------------------------------------
		public bool HasChapterOrVerseNumbers()
		{
			ITsString tss = Contents;
			int cRun = tss.RunCount;
			ITsTextProps ttpRun;
			for (int i = 0; i < cRun; i++)
			{
				ttpRun = tss.get_Properties(i);
				string runText = tss.get_RunText(i);

				if (!string.IsNullOrEmpty(runText) &&
					(ttpRun.Style() == ScrStyleNames.VerseNumber ||
					ttpRun.Style() == ScrStyleNames.ChapterNumber))
				{
					return true;
				}
			}
			return false;
		}
		#endregion

		#region Handle ChapterVerseEdits and UpdatingSectionRefs
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Virtual method to allow subclasses to handle  the side effects of setting the
		/// paragraph contents
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnContentsChanged(ITsString originalValue, ITsString newValue,
			TsStringDiffInfo diffInfo, bool fAdjustAnalyses)
		{
			base.OnContentsChanged(originalValue, newValue, diffInfo, fAdjustAnalyses);

			if (OwningSection != null)
				((ScrSection)OwningSection).AdjustReferences();

			if ((originalValue == null && newValue != null) ||
				(originalValue != null && newValue == null) ||
				(originalValue != null && originalValue.Text != newValue.Text))
			{
				MarkBackTranslationsAsUnfinished();
			}

			if (!m_fSupressOrcHandling)
			{
				HandleRemovedContents(originalValue, newValue, diffInfo);
				HandleNewContents(newValue, diffInfo);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles new contents that were added to this paragraph
		/// </summary>
		/// <param name="newValue">The new value.</param>
		/// <param name="diffInfo">Information about the change.</param>
		/// ------------------------------------------------------------------------------------
		private void HandleNewContents(ITsString newValue, TsStringDiffInfo diffInfo)
		{
			if (newValue == null || diffInfo.CchInsert == 0)
				return; // No new contents to handle

			int iRunStart = newValue.get_RunAt(diffInfo.IchFirstDiff);
			int iRunEnd = newValue.get_RunAt(diffInfo.IchFirstDiff + diffInfo.CchInsert);
			IStFootnoteRepository footnoteRepo = Cache.ServiceLocator.GetInstance<IStFootnoteRepository>();

			bool updateFootnoteReferences = false;
			for (int i = iRunStart; i <= iRunEnd; i++)
			{
				string styleName = newValue.get_StringProperty(i, (int)FwTextPropType.ktptNamedStyle);
				if (styleName == ScrStyleNames.VerseNumber || styleName == ScrStyleNames.ChapterNumber)
				{
					// If a verse or chapter number was inserted or changed, we need to recaluclate
					// the footnote Scripture references for this paragraph and any following it.
					updateFootnoteReferences = true;
				}
				else if (styleName == null)
				{
					// Any footnotes that were inserted need to have their owning paragraph updated
					IScrFootnote footnote = (IScrFootnote)footnoteRepo.GetFootnoteFromObjData(newValue.get_StringProperty(i, (int)FwTextPropType.ktptObjData));
					if (footnote != null)
						((ScrFootnote)footnote).ParaContainingOrcRA = this;
				}
			}

			if (updateFootnoteReferences)
			{
				IScrBook book = OwnerOfClass<IScrBook>();
				if (book != null)
					((ScrBook)book).ClearFootnoteInformation();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles contents that were removed from this paragraph
		/// </summary>
		/// <param name="originalValue">The original value of the paragraph text.</param>
		/// <param name="newValue">The new value of the paragraph text.</param>
		/// <param name="diffInfo">Information about the change.</param>
		/// ------------------------------------------------------------------------------------
		private void HandleRemovedContents(ITsString originalValue, ITsString newValue,
			TsStringDiffInfo diffInfo)
		{
			if (originalValue == null || diffInfo.CchDeleteFromOld == 0)
				return; // No contents that could have been deleted

			int iRunStart = originalValue.get_RunAt(diffInfo.IchFirstDiff);
			int iRunEnd = originalValue.get_RunAt(diffInfo.IchFirstDiff + diffInfo.CchDeleteFromOld);
			int newRunCount = (newValue != null) ? newValue.RunCount : 0;
			IStFootnoteRepository footnoteRepo = Cache.ServiceLocator.GetInstance<IStFootnoteRepository>();

			bool updateFootnoteReferences = false;
			for (int i = iRunStart; i <= iRunEnd; i++)
			{
				string styleName = originalValue.get_StringProperty(i, (int)FwTextPropType.ktptNamedStyle);
				if (styleName == ScrStyleNames.VerseNumber || styleName == ScrStyleNames.ChapterNumber)
				{
					// If a verse or chapter number was removed or changed, we need to recaluclate
					// the footnote Scripture references for this paragraph and any following it.
					updateFootnoteReferences = true;
				}
				else if (styleName == null)
				{
					// Any footnotes that were deleted from this paragraph need to be deleted
					IScrFootnote footnote = (IScrFootnote)footnoteRepo.GetFootnoteFromObjData(
						originalValue.get_StringProperty(i, (int)FwTextPropType.ktptObjData));
					if (footnote != null)
					{
						bool fFound = false;
						for (int iNew = 0; iNew < newRunCount; iNew++)
						{
							Guid guidNew = TsStringUtils.GetGuidFromRun(newValue, iNew);
							if (guidNew == footnote.Guid)
								fFound = true;
						}
						if (!fFound)
							DeleteFootnote(footnote);
					}
				}
			}

			if (updateFootnoteReferences)
			{
				IScrBook book = OwnerOfClass<IScrBook>();
				if (book != null)
					((ScrBook)book).ClearFootnoteInformation();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the specified footnote.
		/// </summary>
		/// <param name="footnote">The footnote to delete.</param>
		/// ------------------------------------------------------------------------------------
		private void DeleteFootnote(IScrFootnote footnote)
		{
			// The CreateOwnedObjects method will be updating GUIDs of footnotes in a archive
			// copy of a book with new copies of those footnotes owned by book this paragraph
			// belongs to - don't want to delete the footnote of the archived copy
			if (footnote.Owner == OwnerOfClass<IScrBook>())
			{
				// We found a footnote and we know it still exists
				IScrBook book = (IScrBook)footnote.Owner;
				IScrTxtPara para = footnote.ParaContainingOrcRA;
				// If there's no (vernacular) paragraph with an ORC that references this
				// footnote, there can't be a translation either.
				if (para != null && para.IsValidObject)
					para.DeleteAnyBtMarkersForFootnote(footnote.Guid);
				book.FootnotesOS.Remove(footnote);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Mark all of the back translations for this paragraph as unfinished.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void MarkBackTranslationsAsUnfinished()
		{
			// ENHANCE: Someday may need to do this for other translations, not just BT (will
			// need to rename this method and possibly move it back into StTxtPara).
			ICmTranslation translation = GetBT();
			if (translation == null)
				return;

			((CmTranslation)translation).MarkAsUnfinished();
		}
		#endregion

		#region ScrVerseSet enumerator
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method to implement IEnumerable. Enables sequential access to the verses
		/// in this paragraph (treats chapter numbers as separate verse tokens).
		/// </summary>
		/// <returns>An enumerator for getting the verses from this paragraph</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IEnumerator GetEnumerator()
		{
			return new ScrVerseSet(this, true);
		}
		#endregion

		#region Overrides of StTxtPara
		public override ITsString Reference(ISegment seg, int ich)
		{
			var stText = Owner as IStText;
			if (stText == null)
				return Cache.MakeUserTss("unknown"); // should never happen, I think?
			if (stText.OwningFlid == ScrSectionTags.kflidContent)
			{
				// Body of Scripture. Figure a book/chapter/verse
				IScrBook book = (IScrBook) stText.Owner.Owner;
				string mainRef = ScriptureServices.FullScrRef(this, ich, book.BestUIAbbrev).Trim();
				return Cache.MakeUserTss(mainRef + ScriptureServices.VerseSegLabel(this, SegmentsOS.IndexOf(seg)));
			}
			if (stText.OwningFlid == ScrSectionTags.kflidHeading)
			{
				// use the section title without qualifiers.
				return stText.Title.BestVernacularAnalysisAlternative;
			}
			if (stText.OwningFlid == ScrBookTags.kflidTitle)
			{
				return stText.Title.BestVernacularAnalysisAlternative;
			}
			return Cache.MakeUserTss("unknown"); // should never happen, I think?
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnote sequence.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override IFdoOwningSequence<IStFootnote> FootnoteSequence
		{
			get
			{
				return new OwningSequenceWrapper<IStFootnote, IScrFootnote>(
					OwnerOfClass<IScrBook>().FootnotesOS);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Splits the paragraph at the specified character index.
		/// </summary>
		/// <param name="ich">The character index where a split will be inserted.</param>
		/// <returns>the new paragraph inserted at the character index</returns>
		/// ------------------------------------------------------------------------------------
		public override IStTxtPara SplitParaAt(int ich)
		{
			m_fSupressOrcHandling = true;
			try
			{
				return base.SplitParaAt(ich);
			}
			finally
			{
				m_fSupressOrcHandling = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Marks the char styles within the specified range as being InUse.
		/// </summary>
		/// <param name="ichFirstDiff">The character index of the first diff.</param>
		/// <param name="cchInsert">The number of characters inserted.</param>
		/// ------------------------------------------------------------------------------------
		protected override void MarkCharStylesInUse(int ichFirstDiff, int cchInsert)
		{
			if (Cache.LangProject == null || Cache.LangProject.TranslatedScriptureOA == null)
				return;

			int firstRun = Contents.get_RunAt(ichFirstDiff);
			int lastRun = Contents.get_RunAt(ichFirstDiff + cchInsert);

			// Mark the character style in each run in the specified range as InUse.
			for (int iRun = firstRun; iRun <= lastRun; iRun++)
			{
				ITsTextProps ttp = Contents.get_Properties(iRun);
				string styleName = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				IStStyle style = Scripture.FindStyle(styleName);
				if (style != null)
					((StStyle)style).InUse = true;
			}
		}
		#endregion

		#region Overrides of StPara
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the style rules change.
		/// </summary>
		/// <param name="originalValue">The original style rules.</param>
		/// <param name="newValue">The new style rules.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnStyleRulesChange(ITsTextProps originalValue, ITsTextProps newValue)
		{
			if (newValue == null)
				throw new InvalidOperationException("Style rules can not be set to null");
			if (newValue.StrPropCount > 1 || newValue.IntPropCount > 0)
				throw new InvalidOperationException("Only allowed property in the style rules for a ScrTxtPara is the style name");

			string styleName = newValue.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			if (styleName == null)
				throw new InvalidOperationException("Can not set a style rules that have no style name (ktptNamedStyle)");

			if (Cache.LangProject.TranslatedScriptureOA != null) // Only for tests, hopefully!
			{
				IStStyle style = Cache.LangProject.TranslatedScriptureOA.FindStyle(styleName);
				if (style != null)
					((StStyle)style).InUse = true;
			}
		}
		#endregion

		#region Copy and Replace, Move or Clear paragraph content
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the text from the specified range in the fromPara to this paragraph. Use this
		/// method only when the paragraphs are in the same book.
		/// </summary>
		/// <param name="ichDest">The character index where the text will be placed.</param>
		/// <param name="sourcePara">The source para for the text.</param>
		/// <param name="ichMinSrc">The starting character index of the text to be moved.</param>
		/// <param name="ichLimSrc">The lim character index of the text to be moved.</param>
		/// <exception cref="ArgumentException">occurs when the source and destination
		/// paragraphs are not owned by the same book</exception>
		/// ------------------------------------------------------------------------------------
		public void MoveText(int ichDest, IScrTxtPara sourcePara, int ichMinSrc, int ichLimSrc)
		{
			MoveText(ichDest, ichDest, sourcePara, ichMinSrc, ichLimSrc);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the text from the specified range in the fromPara to this paragraph. Use this
		/// method only when the paragraphs are in the same book.
		/// </summary>
		/// <param name="ichMinDest">The starting character index where the text will be placed.</param>
		/// <param name="ichLimDest">The ending character index where the text will be placed.</param>
		/// <param name="sourcePara">The source para for the text.</param>
		/// <param name="ichMinSrc">The starting character index of the text to be moved.</param>
		/// <param name="ichLimSrc">The lim character index of the text to be moved.</param>
		/// <exception cref="ArgumentException">occurs when the source and destination
		/// paragraphs are not owned by the same book</exception>
		/// ------------------------------------------------------------------------------------
		public void MoveText(int ichMinDest, int ichLimDest, IScrTxtPara sourcePara,
			int ichMinSrc, int ichLimSrc)
		{
			if (OwnerOfClass<IScrBook>() != sourcePara.OwnerOfClass<IScrBook>())
				throw new ArgumentException("The source and destination paragraphs must be owned by the same book.");

			// Call the base version so we don't create copies of the ORCs. They will be moved
			// to their new location.
			base.ReplaceTextRange(ichMinDest, ichLimDest, sourcePara, ichMinSrc, ichLimSrc);
			// delete the original range of text
			((ScrTxtPara)sourcePara).DeleteMovedRange(ichMinSrc, ichLimSrc);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the range which was moved to another paragraph.
		/// </summary>
		/// <param name="ichMin">The character index beginning the range to delete.</param>
		/// <param name="ichLim">The character index ending the range to delete.</param>
		/// ------------------------------------------------------------------------------------
		private void DeleteMovedRange(int ichMin, int ichLim)
		{
			m_fSupressOrcHandling = true;
			Contents = Contents.Remove(ichMin, ichLim - ichMin);
			m_fSupressOrcHandling = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends the para contents, BT, etc. from the following paragraph to this paragraph
		/// and removes the following paragraph afterwords.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MergeParaWithNext()
		{
			m_fSupressOrcHandling = true;
			try
			{
				base.MergeParaWithNext();
			}
			finally
			{
				m_fSupressOrcHandling = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replaces the specified range of text with the specified range in the fromPara to
		/// this paragraph.
		/// </summary>
		/// <param name="ichMinDest">The starting character index where the text will be replaced.</param>
		/// <param name="ichLimDest">The ending character index where the text will be replaced.</param>
		/// <param name="fromPara">The source para for the text.</param>
		/// <param name="ichMinSrc">The starting character index to copy from.</param>
		/// <param name="ichLimSrc">The ending character index to copy from.</param>
		/// ------------------------------------------------------------------------------------
		public override void ReplaceTextRange(int ichMinDest, int ichLimDest, IStTxtPara fromPara,
			int ichMinSrc, int ichLimSrc)
		{
			base.ReplaceTextRange(ichMinDest, ichLimDest, fromPara, ichMinSrc, ichLimSrc);

			if (fromPara != null && !m_fSupressOrcHandling && !((ScrTxtPara)fromPara).m_fSupressOrcHandling)
			{
				// Create any footnote or picture objects referenced in the copied text.
				int charsAdded = ichLimSrc - ichMinSrc;
				CreateOwnedObjects(ichMinDest, ichMinDest + charsAdded);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replaces this paragraph with the sourcePara. Creates  copies (in this paragraph) of
		/// all objects refered to by ORCs in the sourcePara.
		/// </summary>
		/// <param name="sourcePara">The source para for the text.</param>
		/// ------------------------------------------------------------------------------------
		public void ReplacePara(IScrTxtPara sourcePara)
		{
			sourcePara.SetCloneProperties(this);

			if (OwningSection != null)
				((ScrSection)OwningSection).AdjustReferences();

			// Create any footnote or picture objects referenced in the copied text.
			CreateOwnedObjects(0, Contents.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create copies of any objects whose guids are owned by the given portion of the
		/// paragraph Contents. Use this when part of a string has just been replaced with a
		/// previous revision that may have object references.
		/// </summary>
		/// <param name="ichMin">The 0-based index of the first character to search for ORCs
		/// </param>
		/// <param name="ichLim">The 0-based index of the character following the last character
		/// to be searched</param>
		/// ------------------------------------------------------------------------------------
		public void CreateOwnedObjects(int ichMin, int ichLim)
		{
			m_fSupressOrcHandling = true;
			if (ichLim <= ichMin)
				return; // nothing to do

			ITsTextProps ttp;
			TsRunInfo tri;
			int firstRun = Contents.get_RunAt(ichMin);
			int lastRun = Contents.get_RunAt(ichLim - 1);

			// these variables are initialized if needed as we process the runs
			ITsStrBldr paraBldr = null;
			int iFirstFootnote = -1;
			int footnoteCount = 0; // number of footnotes we have encountered in the given range

			IFdoOwningSequence<IStFootnote> feetnote = FootnoteSequence;
			// Check each run, and create copies of the owned objects
			for (int iRun = firstRun; iRun <= lastRun; iRun++)
			{
				FwObjDataTypes odt;
				Guid guidOfObjToCopy = TsStringUtils.GetOwnedGuidFromRun(Contents, iRun, out odt,
					out tri, out ttp);

				// if this run is an owning ORC...
				if (guidOfObjToCopy != Guid.Empty)
				{
					Guid guidOfNewObj = Guid.Empty;
					if (odt == FwObjDataTypes.kodtOwnNameGuidHot)
					{
						guidOfNewObj = CreateFootnoteCopy(ref iFirstFootnote, footnoteCount, ichMin,
							guidOfObjToCopy, iRun);
						footnoteCount++;
					}
					else if (odt == FwObjDataTypes.kodtGuidMoveableObjDisp)
					{
						guidOfNewObj = CreatePictureCopy(guidOfObjToCopy);
					}

					// If a new object was created, update all the ORCs for it
					if (guidOfNewObj != Guid.Empty)
					{
						// We re-use the same string builder for the paragraph contents.
						//  Just get it if this is the first time thru.
						if (paraBldr == null)
							paraBldr = Contents.GetBldr();

						UpdateORCforNewObjData(paraBldr, ttp, tri, odt, guidOfNewObj);

						// In each translation, update any ORC from the old object, to the new
						UpdateOrcsInTranslations(guidOfObjToCopy, guidOfNewObj);
					}
				}
			}
			// save the updated paragraph string
			if (paraBldr != null)
				Contents = paraBldr.GetString();

			m_fSupressOrcHandling = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a copy of the specified footnote.
		/// </summary>
		/// <param name="iFirstFootnote">The index of the first footnote, or -1 if not known.</param>
		/// <param name="footnoteCount">The number of footnotes added since the first one.</param>
		/// <param name="ich">Offset in this para that is used when we need to scan
		/// backwards for the previous footnote index.</param>
		/// <param name="guidOfObjToCopy">The guid of the footnote to copy.</param>
		/// <param name="iRun">The 0-based index of the run from which we will get the writing
		/// system used in a blank footnote.</param>
		/// <returns>The GUID of the new footnote copy.</returns>
		/// ------------------------------------------------------------------------------------
		protected override Guid CreateFootnoteCopy(ref int iFirstFootnote, int footnoteCount, int ich,
			Guid guidOfObjToCopy, int iRun)
		{
			// If this is the first footnote created, get the correct starting index to use
			// TODO (TimS): if there are two footnotes together, we noticed some problems with getting the
			// correct footnote index
			if (iFirstFootnote == -1)
				iFirstFootnote = NextFootnoteIndex(ich);

			Debug.Assert(iFirstFootnote > -1);
			// Create the new copy of the footnote.
			IScrFootnote oldFootnote;
			IStFootnote newFootnote;
			int iFootnote = iFirstFootnote + footnoteCount;
			if (Services.GetInstance<IScrFootnoteRepository>().TryGetFootnote(guidOfObjToCopy, out oldFootnote))
			{
				newFootnote = CopyObject<IScrFootnote>.CloneFdoObject(oldFootnote,
					x => FootnoteSequence.Insert(iFootnote, x));
			}
			else
			{
				// Unable to find footnote with this guid, so create a blank footnote.
				newFootnote = CreateBlankDummyFootnote(FootnoteSequence, iFootnote,
					Contents, iRun);
			}
			Debug.Assert(newFootnote.Guid != guidOfObjToCopy);
			return newFootnote.Guid;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the next 0-based footnote index to use for the first footnote being created
		/// in this paragraph at or after the given char location.
		/// This should be called only for the first foonote; the caller is expected to properly
		/// increment the returned index if multiple contiguous footnotes are to be inserted.
		/// Typically this is called from IScrTxtPara.CreateOwnedObjects() when footnote material
		/// has been insterted into a paragraph.
		/// <param name="ich">offset in paragraph to start looking before</param>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int NextFootnoteIndex(int ich)
		{
			int iFootnote = 0;
			IScrFootnote prevFootnote = FindPreviousFootnote(ich);

			if (prevFootnote != null)
				iFootnote = prevFootnote.IndexInOwner + 1;

			return iFootnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the contents of a paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Clear()
		{
			RemoveOwnedObjectsForString(0, Contents.Length);
			Contents = Cache.TsStrFactory.MakeString(string.Empty,
				m_cache.DefaultVernWs);
			TranslationsOC.Clear();
		}
		#endregion

		#region OwningSequenceWrapper class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TWrap">The type of the wrapped sequence.</typeparam>
		/// ------------------------------------------------------------------------------------
		private class OwningSequenceWrapper<T, TWrap> : IFdoOwningSequence<T>
			where TWrap : class, T where T : class, ICmObject
		{
			private IFdoOwningSequence<TWrap> m_wrappedSeq;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="OwningSequenceWrapper&lt;T, TWrap&gt;"/> class.
			/// </summary>
			/// <param name="wrappedSeq">The wrapped owning sequence.</param>
			/// --------------------------------------------------------------------------------
			public OwningSequenceWrapper(IFdoOwningSequence<TWrap> wrappedSeq)
			{
				m_wrappedSeq = wrappedSeq;
			}

			#region IFdoOwningSequence<T> Members

			public void MoveTo(int iStart, int iEnd, IFdoOwningSequence<T> seqDest, int iDestStart)
			{
				throw new NotImplementedException();
			}

			#endregion

			#region IFdoList<T> Members

			public T[] ToArray()
			{
				throw new NotImplementedException();
			}

			public void Replace(int start, int numberToDelete, IEnumerable<ICmObject> thingsToAdd)
			{
				throw new NotImplementedException();
			}

			#endregion

			#region IList<T> Members

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
			/// </summary>
			/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
			/// <returns>
			/// The index of <paramref name="item"/> if found in the list; otherwise, -1.
			/// </returns>
			/// --------------------------------------------------------------------------------
			public int IndexOf(T item)
			{
				return m_wrappedSeq.IndexOf((TWrap)item);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Inserts an item to the <see cref="T:System.Collections.Generic.IList`1"/> at the specified index.
			/// </summary>
			/// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
			/// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
			/// <exception cref="T:System.ArgumentOutOfRangeException">
			/// 	<paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.
			/// </exception>
			/// <exception cref="T:System.NotSupportedException">
			/// The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.
			/// </exception>
			/// --------------------------------------------------------------------------------
			public void Insert(int index, T item)
			{
				m_wrappedSeq.Insert(index, (TWrap)item);
			}

			public void RemoveAt(int index)
			{
				throw new NotImplementedException();
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the value at the specified index.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public T this[int index]
			{
				get { return m_wrappedSeq[index]; }
				set { m_wrappedSeq[index] = (TWrap)value; }
			}

			#endregion

			#region ICollection<T> Members

			public void Add(T item)
			{
				throw new NotImplementedException();
			}

			public void Clear()
			{
				throw new NotImplementedException();
			}

			public bool Contains(T item)
			{
				throw new NotImplementedException();
			}

			public void CopyTo(T[] array, int arrayIndex)
			{
				throw new NotImplementedException();
			}

			public int Count
			{
				get { throw new NotImplementedException(); }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
			/// </summary>
			/// <value></value>
			/// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.
			/// </returns>
			/// --------------------------------------------------------------------------------
			public bool IsReadOnly
			{
				get { return false; }
			}

			public bool Remove(T item)
			{
				throw new NotImplementedException();
			}

			#endregion

			#region IEnumerable<T> Members

			public IEnumerator<T> GetEnumerator()
			{
				throw new NotImplementedException();
			}

			#endregion

			#region IEnumerable Members

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Returns an enumerator that iterates through a collection.
			/// </summary>
			/// <returns>
			/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
			/// </returns>
			/// --------------------------------------------------------------------------------
			IEnumerator IEnumerable.GetEnumerator()
			{
				return m_wrappedSeq.GetEnumerator();
			}

			#endregion

			#region IFdoVector Members

			public int[] ToHvoArray()
			{
				throw new NotImplementedException();
			}

			public Guid[] ToGuidArray()
			{
				throw new NotImplementedException();
			}

			public IEnumerable<ICmObject> Objects
			{
				get { throw new NotImplementedException(); }
			}

			#endregion
		}
		#endregion
	}
}
