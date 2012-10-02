// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrTxtPara.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.FwUtils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Flags that show if verse and/or chapter are found
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[Flags]
	public enum ChapterVerseFound
	{
		/// <summary>Nothing found</summary>
		None = 0,
		/// <summary>Verse was found</summary>
		Verse = 1,
		/// <summary>Chapter was found</summary>
		Chapter = 2
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Specialized <see cref="StTxtPara"/> that knows how to apply scripture references.
	/// </summary>
	/// <remarks>This class does not have a corresponding table in the database.</remarks>
	/// ----------------------------------------------------------------------------------------
	public class ScrTxtPara : StTxtPara, System.Collections.IEnumerable
	{
		private uint intSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(int));

		#region Constructors and initialization
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrTxtPara"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ScrTxtPara(): base()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrTxtPara"/> class.
		/// </summary>
		/// <param name="cache">The FDO cache object</param>
		/// -----------------------------------------------------------------------------------
		public ScrTxtPara(FdoCache cache): base()
		{
			m_cache = cache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrTxtPara"/> class.
		/// </summary>
		/// <param name="fcCache">The FDO cache object</param>
		/// <param name="hvo">HVO of the new object</param>
		/// ------------------------------------------------------------------------------------
		public ScrTxtPara(FdoCache fcCache, int hvo)
			: base(fcCache, hvo)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this constructor where you want to have control over loading/validating objects
		/// </summary>
		/// <param name="fcCache"></param>
		/// <param name="hvo"></param>
		/// <param name="fCheckValidity"></param>
		/// <param name="fLoadIntoCache"></param>
		/// ------------------------------------------------------------------------------------
		public ScrTxtPara(FdoCache fcCache, int hvo, bool fCheckValidity, bool fLoadIntoCache)
			: base(fcCache, hvo, fCheckValidity, fLoadIntoCache)
		{
		}
		#endregion

		#region Blank Dummy Footnote creation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a blank dummy footnote. Used when a footnote object is missing.
		/// Note: This does not insert an ORC into the paragraph. The caller is fixing the ORC
		/// with a missing object.
		/// </summary>
		/// <param name="owner">The owner to which we will add a footnote.</param>
		/// <param name="iFootnote">The 0-based index where the footnote will be inserted in the
		/// owner.</param>
		/// <param name="paraContents">The paragraph string where the ORC is being fixed.</param>
		/// <param name="iRun">The 0-based index of the run from which we will get the writing
		/// system used in the footnote.</param>
		/// <returns>a blank general footnote</returns>
		/// ------------------------------------------------------------------------------------
		protected override StFootnote CreateBlankDummyFootnote(ICmObject owner, int iFootnote,
			ITsString paraContents, int iRun)
		{
			return CreateBlankDummyFootnoteNoRecursion(owner, iFootnote, paraContents, iRun);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a blank dummy footnote. Used when a footnote object is missing.
		/// Note: This does not insert an ORC into the paragraph. The caller is fixing the ORC
		/// with a missing object.
		/// </summary>
		/// <param name="owner">The owner to which we will add a footnote.</param>
		/// <param name="iFootnote">The 0-based index where the footnote will be inserted in the
		/// owner.</param>
		/// <param name="paraContents">The paragraph string where the ORC is being fixed.</param>
		/// <param name="iRun">The 0-based index of the run from which we will get the writing
		/// system used in the footnote.</param>
		/// <returns>a blank general footnote</returns>
		/// ------------------------------------------------------------------------------------
		protected override StFootnote CreateBlankDummyFootnoteNoRecursion(ICmObject owner,
			int iFootnote, ITsString paraContents, int iRun)
		{
			if (!(owner is IScrBook))
				return base.CreateBlankDummyFootnoteNoRecursion(owner, iFootnote, paraContents, iRun);

			// get the writing system of the existing ORC run
			int nDummy;
			int ws = paraContents.get_Properties(iRun).GetIntPropValues(
				(int)FwTextPropType.ktptWs, out nDummy);

			//  Make a dummy blank footnote
			IScripture scr = m_cache.LangProject.TranslatedScriptureOA;
			string sMarker = scr.GeneralFootnoteMarker;
			StFootnote newFootnote = ScrFootnote.CreateFootnoteInScrBook((IScrBook)owner, iFootnote, ref sMarker,
				m_cache, ws);

			// Create an empty footnote paragraph with properties with default style and writing system.
			StTxtPara footnotePara = new StTxtPara();
			newFootnote.ParagraphsOS.Append(footnotePara);
			footnotePara.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph);
			// Insert an empty run into the footnote paragraph in order to set the
			// default writing system.
			ITsStrFactory strFactory = TsStrFactoryClass.Create();
			footnotePara.Contents.UnderlyingTsString =
				strFactory.MakeString(string.Empty, m_cache.DefaultVernWs);

			return newFootnote;
		}
		#endregion

		#region Getting/setting StyleName, Context, and Structure
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the style for the current paragraph. Returns the default
		/// (based on context/structure) if the style name is not set in the style rules (which
		/// should never happen but has been known to).
		/// </summary>
		/// <exception cref="InvalidOperationException">Unable to get style name for paragraph.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public override string StyleName
		{
			get
			{
				string sStyleName = null;
				if (StyleRules != null)
					sStyleName = base.StyleName;
				if (!string.IsNullOrEmpty(sStyleName))
					return sStyleName;
				return DefaultStyleName;
			}
			set { base.StyleName = value; }
		}

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
					case (int)ScrSection.ScrSectionTags.kflidHeading:
						{
							ScrSection section = new ScrSection(Cache, Owner.OwnerHVO);
							return (section.IsIntro) ? ScrStyleNames.IntroSectionHead : ScrStyleNames.SectionHead;
						}
					case (int)ScrSection.ScrSectionTags.kflidContent:
						{
							ScrSection section = new ScrSection(Cache, Owner.OwnerHVO);
							return (section.IsIntro) ? ScrStyleNames.IntroParagraph : ScrStyleNames.NormalParagraph;
						}
					case (int)ScrBook.ScrBookTags.kflidTitle:
						return ScrStyleNames.MainBookTitle;
					case (int)ScrBook.ScrBookTags.kflidFootnotes:
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
					case (int)ScrSection.ScrSectionTags.kflidHeading:
					case (int)ScrSection.ScrSectionTags.kflidContent:
						return new ScrSection(Cache, Owner.OwnerHVO).Context;
					case (int)ScrBook.ScrBookTags.kflidTitle:
						return ContextValues.Title;
					case (int)ScrBook.ScrBookTags.kflidFootnotes:
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
		/// Determine the start and end reference for the section this paragraph belongs to.
		/// </summary>
		/// <param name="sectRefStart">Section start reference</param>
		/// <param name="sectRefEnd">Section end reference</param>
		/// ------------------------------------------------------------------------------------
		protected void GetSectionStartAndEndRefs(out BCVRef sectRefStart,
			out BCVRef sectRefEnd)
		{
			// Processing depends on "type" of paragraph (content, heading, title...)
			switch (m_cache.GetOwningFlidOfObject(OwnerHVO))
			{
				case (int)ScrSection.ScrSectionTags.kflidContent:
				case (int)ScrSection.ScrSectionTags.kflidHeading:
				{
					int hvoScrSection = m_cache.GetOwnerOfObject(OwnerHVO);
					int nSectRefMin = m_cache.GetIntProperty(hvoScrSection,
						(int)ScrSection.ScrSectionTags.kflidVerseRefMin);
					int nSectRefMax = m_cache.GetIntProperty(hvoScrSection,
						(int)ScrSection.ScrSectionTags.kflidVerseRefMax);
					sectRefStart = new BCVRef(nSectRefMin);
					sectRefEnd = new BCVRef(nSectRefMax);
					break;
				}
				case (int)ScrBook.ScrBookTags.kflidTitle:
				{
					IScrBook scrBook = new ScrBook(m_cache, m_cache.GetOwnerOfObject(OwnerHVO));
					sectRefStart = new BCVRef(scrBook.CanonicalNum, 1, 0);
					sectRefEnd = new BCVRef(scrBook.CanonicalNum, 1, 0);
					break;
				}
				default:
					Debug.Assert(false); // Unexpected owning field for StText
					sectRefStart = 0;
					sectRefEnd = 0;
					break;
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
		static public ChapterVerseFound GetBCVRefAtPosWithinTss(ITsString tss, int ichPos,
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
					if (!fGotVerse && StStyle.IsStyle(ttpRun, ScrStyleNames.VerseNumber))
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
					else if (StStyle.IsStyle(ttpRun, ScrStyleNames.ChapterNumber))
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
		protected ChapterVerseFound GetBCVRefAtEndOfPara(out BCVRef refStart,
			out BCVRef refEnd)
		{
			refStart = new BCVRef();
			refEnd = new BCVRef();
			if (Contents.Text == null)
				return ChapterVerseFound.None;
			return GetBCVRefAtPosWithinPara(-1, Contents.Length, true, out refStart, out refEnd);
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
		/// ------------------------------------------------------------------------------------
		protected ChapterVerseFound GetBCVRefAtPosWithinPara(int wsBT, int ichPos,
			bool fAssocPrev, out BCVRef refStart, out BCVRef refEnd)
		{
			ITsString tss;
			if (wsBT > 0)
				tss = GetBT().Translation.GetAlternativeTss(wsBT);
			else
				tss = Contents.UnderlyingTsString;

			return GetBCVRefAtPosWithinTss(tss, ichPos, fAssocPrev, out refStart, out refEnd);
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
		public void GetBCVRefAtPosition(int ivPos, out BCVRef refStart, out BCVRef refEnd)
		{
			GetBCVRefAtPosition(-1, ivPos, false, out refStart, out refEnd);
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
		public void GetBCVRefAtPosition(int ivPos, bool fAssocPrev, out BCVRef refStart,
			out BCVRef refEnd)
		{
			GetBCVRefAtPosition(-1, ivPos, fAssocPrev, out refStart, out refEnd);
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
		public void GetBCVRefAtPosition(int wsBT, int ivPos, bool fAssocPrev,
			out BCVRef refStart, out BCVRef refEnd)
		{
			refStart = new BCVRef();
			refEnd = new BCVRef();

			// Might be trying to get the BCVRef in a footnote
			int ownerOwnFlid = m_cache.GetOwningFlidOfObject(OwnerHVO);
			if (ownerOwnFlid == (int)ScrBook.ScrBookTags.kflidFootnotes)
			{
				ScrFootnote footnote = new ScrFootnote(m_cache, OwnerHVO);
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

			BCVRef sectRefStart;
			BCVRef sectRefEnd;
			GetSectionStartAndEndRefs(out sectRefStart, out sectRefEnd);

			while (true)
			{
				if (para == this)
				{
					found = para.GetBCVRefAtPosWithinPara(wsBT, ivPos, fAssocPrev, out refStartT,
						out refEndT);
				}
				else
					found = para.GetBCVRefAtEndOfPara(out refStartT, out refEndT);

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
					// REVIEW (EberhardB): does this work if not all paragraphs are
					// loaded in the cache?
					chvoParas = m_cache.GetVectorSize(OwnerHVO,
						(int)StText.StTextTags.kflidParagraphs);
					// Go forward through vector of paragraphs to find the one being parsed
					for (ihvoPara = 0; ihvoPara < chvoParas; ihvoPara++)
					{
						int hvoPara = m_cache.GetVectorItem(OwnerHVO,
							(int)StText.StTextTags.kflidParagraphs,	ihvoPara);
						if (hvoPara == Hvo)
							break; // found our current para
					}
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
				int hvoNewPara = m_cache.GetVectorItem(OwnerHVO,
					(int)StText.StTextTags.kflidParagraphs, ihvoPara);
				// use a special constructor since we already know the section refs
				para = new ScrTxtPara(m_cache, hvoNewPara, false, false);
			}
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
			ITsString tss = Contents.UnderlyingTsString;
			int cRun = tss.RunCount;
			ITsTextProps ttpRun;
			for (int i = 0; i < cRun; i++)
			{
				ttpRun = tss.get_Properties(i);

				if (StStyle.IsStyle(ttpRun, ScrStyleNames.VerseNumber) ||
					StStyle.IsStyle(ttpRun, ScrStyleNames.ChapterNumber))
					return true;
			}
			return false;
		}
		#endregion

		#region Handle ChapterVerseEdits and UpdatingSectionRefs
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust section refs for the edited paragraph.
		/// Called by a ChangeWatcher if any StTxtPara.Contents is modified.
		/// </summary>
		/// <param name="ivMin">Character index where text was added and/or deleted</param>
		/// <param name="cvIns">Count of characters inserted</param>
		/// <param name="cvDel">Count of characters deleted</param>
		/// ------------------------------------------------------------------------------------
		public void ProcessChapterVerseNums(int ivMin, int cvIns, int cvDel)
		{
			// We expect that our para is part of a ScrSection.Content
			Debug.Assert(m_cache.GetOwningFlidOfObject(OwnerHVO) ==
				(int)ScrSection.ScrSectionTags.kflidContent);

			// If we join a paragraph with an empty paragraph, there's nothing for us to do.
			if (cvIns == 0 && cvDel == 0)
				return;

			// Now adjust our section refs if necessary
			ScrSection section = new ScrSection(m_cache, m_cache.GetOwnerOfObject(OwnerHVO));
			section.AdjustReferences();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust section refs if needed.
		/// Called by a ChangeWatcher if any paras in StText.ParagraphsOS are added, deleted,
		/// or moved.
		/// </summary>
		/// <param name="text">the StText whose Paragraphs collection was changed</param>
		/// <param name="ivMin">index of the first para inserted or deleted in the StText</param>
		/// ------------------------------------------------------------------------------------
		public static void AdjustSectionRefsForStTextParaChg(IStText text, int ivMin)
		{
			// We expect that this StText is part of ScrSection.Content
			Debug.Assert(text.Cache.GetOwningFlidOfObject(text.Hvo) ==
				(int)ScrSection.ScrSectionTags.kflidContent);

			ScrSection section = new ScrSection(text.Cache, text.Cache.GetOwnerOfObject(text.Hvo));
			section.AdjustReferences();
		}
		#endregion

		#region ScrVerseSet enumerator
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method to implement IEnumerable. Enables sequential access to the verses
		/// in this paragraph.
		/// </summary>
		/// <returns>An enumerator for getting the verses from this paragraph</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IEnumerator GetEnumerator()
		{
			return new ScrVerseSet(this);
		}
		#endregion

		/// <summary>
		/// Test whether a particular StTxtPara is part of Scripture.
		/// </summary>
		/// <param name="hvoPara"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static bool IsScripturePara(int hvoPara, FdoCache cache)
		{
			int flidOfOwnerOfSttext = cache.GetOwningFlidOfObject(cache.GetOwnerOfObject(hvoPara));
			return Scripture.Scripture.IsScriptureTextFlid(flidOfOwnerOfSttext);
		}
		//#region Error reporting
		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// This method reports the specified error message as an error annotation. (Eventually)
		///// </summary>
		///// <param name="ErrorMessage">The error message to report</param>
		///// <param name="ipos">The character index with the paragraph of the error.</param>
		///// <param name="iLim">The end of the error occurance in the paragraph.</param>
		///// <param name="ErrorFlags">The bit flags for the type of error message.</param>
		///// <remarks>Currently sets a dummy property in the cache instead.</remarks>
		///// ------------------------------------------------------------------------------------
		//protected void LogError(string ErrorMessage, int ipos, int iLim,
		//    ParaErrorFlags ErrorFlags)
		//{
		//    //TODO: replace with red squiggly
		//    ErrorInPara = ErrorInPara | (int)ErrorFlags;
		//    Console.WriteLine(ErrorMessage);
		//}
		//#endregion
	}
}
