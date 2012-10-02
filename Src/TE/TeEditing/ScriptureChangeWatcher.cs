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
// File: ScriptureChangeWatcher.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.TE;

namespace SIL.FieldWorks.FDO.Scripture
{
	#region ScriptureChangeWatcher
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class is used to set up the ChangeWatchers defined in this file, for scripture.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScriptureChangeWatcher
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the publicly accessible way to creating all the ChangeWatchers needed for
		/// scripture editing. Upon construction, the change watchers automatically register
		/// themselves and assumes a life of their own, so nothing is returned to the caller.
		/// </summary>
		/// <param name="cache">The cache</param>
		/// ------------------------------------------------------------------------------------
		public static void Create(FdoCache cache)
		{
			Debug.Assert(cache != null);
			if (cache.ChangeWatchers != null)
			{
				foreach (ChangeWatcher cw in cache.ChangeWatchers)
				{
					if (cw is ScrParaContentsChangeWatcher)
						return; // Only one of these allowed per DB connection
				}
			}
			// No need to assign these to a member variables because the change watchers register
			// themselves (and thereby take on a life of their own).
			new ScrParaContentsChangeWatcher(cache);
			new StTextParagraphsChangeWatcher(cache);
			new CmTranslationChangeWatcher(cache);
			new StFootnoteVectorChangeWatcher(cache);
		}
	}
	#endregion

	#region ScrParaContentsChangeWatcher
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The ScrParaContentsChangeWatcher class receives notifications of paragraph edits and
	/// implements the desired side effects, which may include:
	/// * updating section references
	/// * re-parsing the paragraph for wordforms?
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrParaContentsChangeWatcher : ChangeWatcher
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ScrParaContentsChangeWatcher(FdoCache cache) :
			base(cache, (int)StTxtPara.StTxtParaTags.kflidContents)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls ParseStTxtPara to carry out the desired side effects: re-parsing the paragraph
		/// for wordforms, ???.
		/// </summary>
		/// <param name="hvoPara">The Paragraph that was changed</param>
		/// <param name="ivMin">the starting character index where the change occurred</param>
		/// <param name="cvIns">the number of characters inserted</param>
		/// <param name="cvDel">the number of characters deleted</param>
		/// ------------------------------------------------------------------------------------
		protected override void DoEffectsOfPropChange(int hvoPara, int ivMin, int cvIns, int cvDel)
		{
			// If nothing really changed, don't do anything.
			if (cvIns == 0 && cvDel == 0)
				return;

			// Check that the paragraph is truly Scripture, and not a footnote or some other kind
			// of non-Scripture paragraph
			int hvoOfStTextThatOwnsPara = m_cache.GetOwnerOfObject(hvoPara);

			switch (m_cache.GetOwningFlidOfObject(hvoOfStTextThatOwnsPara))
			{
				case (int)ScrSection.ScrSectionTags.kflidContent:
				{
					ScrTxtPara para = new ScrTxtPara(m_cache, hvoPara, false, false);
					// get para props to determine para style - Intro?
					para.ProcessChapterVerseNums(ivMin, cvIns, cvDel);

					// Mark any back translations as unfinished
					para.MarkBackTranslationsAsUnfinished();
					break;
				}
				case (int)ScrBook.ScrBookTags.kflidFootnotes:
				case (int)ScrSection.ScrSectionTags.kflidHeading:
				{
					ScrTxtPara para = new ScrTxtPara(m_cache, hvoPara, false, false);
					// Mark any back translations as stale
					para.MarkBackTranslationsAsUnfinished();
					break;
				}
				default:
					// REVIEW TETeam(TomB): Is any checking needed for anything else?
					break;
			}
		}
	}
	#endregion

	#region StTextParagraphsChangeWatcher
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The StTextParagraphsChangeWatcher class receives notifications of changes to the
	/// collection of paragraphs in a StText. When paragraphs are added, deleted, or moved
	/// between sections the change watcher adjusts the scripture references for the section.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StTextParagraphsChangeWatcher : ChangeWatcher
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal StTextParagraphsChangeWatcher(FdoCache cache) :
			base(cache, (int)StText.StTextTags.kflidParagraphs)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts the references of the section and creates any needed BT's
		/// </summary>
		/// <param name="hvoText">The StText that was changed</param>
		/// <param name="ivMin">the starting index where the change occurred</param>
		/// <param name="cvIns">the number of paragraphs inserted</param>
		/// <param name="cvDel">the number of paragraphs deleted</param>
		/// ------------------------------------------------------------------------------------
		protected override void DoEffectsOfPropChange(int hvoText, int ivMin, int cvIns, int cvDel)
		{
			if (cvIns == 0 && cvDel == 0)
				return; // Nothing actually changed

			int flid = m_cache.GetOwningFlidOfObject(hvoText);
			StText text = new StText(m_cache, hvoText, false, false);
			FdoOwningSequence<IStPara> paras = text.ParagraphsOS;

			// Create back translations for any new paragraphs
			if (flid == (int)ScrSection.ScrSectionTags.kflidContent ||
				flid == (int)ScrSection.ScrSectionTags.kflidHeading ||
				flid == (int)ScrBook.ScrBookTags.kflidTitle)
			{
				for (int iPara = ivMin; iPara < ivMin + cvIns; iPara++)
				{
					ScrTxtPara para = new ScrTxtPara(m_cache, paras.HvoArray[iPara]);
					para.GetOrCreateBT();
				}
			}

			// Adjust section references for section contents if we have some paragraphs left
			if (flid == (int)ScrSection.ScrSectionTags.kflidContent && paras.Count > 0)
				ScrTxtPara.AdjustSectionRefsForStTextParaChg(text, ivMin);

			// If we show boundary markers and we insert or delete a paragraph, we have to
			// update the marker of the previous paragraph as well, as that might
			// now be no longer the last paragraph (and thus needs to show the paragraph
			// marker instead of the section marker).
			if (((cvIns > 0 && cvDel == 0) || (cvIns == 0 && cvDel > 0)) && ivMin > 0
				&& Options.ShowFormatMarksSetting)
			{
				text.Cache.PropChanged(null, PropChangeType.kpctNotifyAll, hvoText,
					(int)StText.StTextTags.kflidParagraphs, ivMin - 1, 1, 1);
			}
		}
	}
	#endregion

	#region CmTranslationChangeWatcher
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Watches for changes in the back translation paragraphs
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CmTranslationChangeWatcher : ChangeWatcher
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal CmTranslationChangeWatcher(FdoCache cache) :
			base(cache, (int)CmTranslation.CmTranslationTags.kflidTranslation)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the status of a back translation if it was modified (from finished or checked
		/// to unfinished).
		/// </summary>
		/// <param name="hvoTrans">A translation that was changed (probably a back translation)
		/// </param>
		/// <param name="ivMin">in CmTranslation, this is the hvo of the writing system of the
		/// string that has changed</param>
		/// <param name="cvIns">the number of items inserted?</param>
		/// <param name="cvDel">the number of items deleted?</param>
		/// ------------------------------------------------------------------------------------
		protected override void DoEffectsOfPropChange(int hvoTrans, int ivMin, int cvIns, int cvDel)
		{
			CmTranslation trans = new CmTranslation(m_cache, hvoTrans);
			if (trans.TypeRA.Guid == LangProject.kguidTranBackTranslation)
				MarkCurrentBackTranslationAsUnfinished(trans, ivMin);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Mark specified back translation as unfinished. This should be called when the back
		/// translation is modified.
		/// </summary>
		/// <param name="translation">The translation.</param>
		/// <param name="hvoWs">The hvo of the current back translation writing system</param>
		/// ------------------------------------------------------------------------------------
		private void MarkCurrentBackTranslationAsUnfinished(CmTranslation translation, int hvoWs)
		{
			if (m_cache.IsRealObject(hvoWs, LgWritingSystem.kclsidLgWritingSystem))
			{
				// We have confirmed that this hvo is for a writing system.
				// set the specified alternate writing system to unfinished.
				string status = translation.Status.GetAlternative(hvoWs);
				if (status != null && status != BackTranslationStatus.Unfinished.ToString())
					translation.Status.SetAlternative(BackTranslationStatus.Unfinished.ToString(), hvoWs);
			}
		}
	}
	#endregion

	#region StFootnoteVectorChangeWatcher
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Watches for changes on the vector of footnotes in a book
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StFootnoteVectorChangeWatcher : ChangeWatcher
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal StFootnoteVectorChangeWatcher(FdoCache cache) :
			base(cache, (int)ScrBook.ScrBookTags.kflidFootnotes)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoBook">The hvo of the book containing the footnotes</param>
		/// <param name="ivMin">The index in the vector of the change</param>
		/// <param name="cvIns">the number of footnotes inserted</param>
		/// <param name="cvDel">the number of footnotes deleted</param>
		/// ------------------------------------------------------------------------------------
		protected override void DoEffectsOfPropChange(int hvoBook, int ivMin, int cvIns, int cvDel)
		{
			bool fNeedToRecalculate = true;
			ScrBook book = new ScrBook(m_cache, hvoBook);
			if (cvIns > 0)
			{
				// if we inserted, we only need to recalculate the markers if the inserted footnote
				// offset the auto-lettering.
				Debug.Assert(ivMin < book.FootnotesOS.Count);
				ScrFootnote footnoteIns = new ScrFootnote(m_cache, book.FootnotesOS.HvoArray[ivMin]);
				fNeedToRecalculate = (footnoteIns.FootnoteType == FootnoteMarkerTypes.AutoFootnoteMarker);
			}
			if (fNeedToRecalculate)
				ScrFootnote.RecalculateFootnoteMarkers(book, ivMin);
		}
	}
	#endregion
}
