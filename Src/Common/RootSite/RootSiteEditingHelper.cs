// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RootSiteEditingHelper.cs
// Responsibility:
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections.Generic;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Ling;

using Enchant;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Summary description for RootSiteEditingHelper.
	/// </summary>
	public class RootSiteEditingHelper : EditingHelper
	{
		#region Member variables
		/// <summary>
		/// The cache
		/// </summary>
		protected FdoCache m_cache;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RootSiteEditingHelper"/> class.
		/// </summary>
		/// <param name="cache">The DB connection</param>
		/// <param name="callbacks">implementation of <see cref="IEditingCallbacks"/></param>
		/// ------------------------------------------------------------------------------------
		public RootSiteEditingHelper(FdoCache cache, IEditingCallbacks callbacks)
			: base(callbacks)
		{
			m_cache = cache;
		}

		/// <summary>
		/// Setter, normally only used if the client view's cache was not yet set at the time
		/// of creating the editing helper.
		/// </summary>
		public FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system factory
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				if (m_cache == null)
					return base.WritingSystemFactory;
				return m_cache.LanguageWritingSystemFactoryAccessor;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the enter key.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override bool HandleEnterKey()
		{
			return false;
		}

		/// <summary>
		/// Add the word to the spelling dictionary.
		/// Overrides to also add to the wordform inventory.
		/// </summary>
		/// <param name="dict"></param>
		/// <param name="word"></param>
		/// <param name="ws"></param>
		public override void AddToSpellDict(Dictionary dict, string word, int ws)
		{
			base.AddToSpellDict(dict, word, ws);
			if (m_cache == null)
				return; // bizarre, but means we just can't do it.
			// If it's in a current vernacular writing system, we want to update the WFI as well.
			bool fVern = false;
			foreach (LgWritingSystem lws in m_cache.LangProject.CurVernWssRS)
				if (lws.Hvo == ws)
				{
					fVern = true;
					break;
				}
			if (!fVern)
				return;
			// Now add to WFI.
			int hvoWf = SIL.FieldWorks.FDO.Ling.WfiWordform.FindOrCreateWordform(m_cache, word, ws, true);
			IWfiWordform wf = SIL.FieldWorks.FDO.Ling.WfiWordform.CreateFromDBObject(m_cache, hvoWf);
			wf.SpellingStatus = (int)SpellingStatusStates.correct;
		}

		#region Navigation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Go to the next paragraph looking at the selection information.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void GoToNextPara()
		{
			int level = CurrentSelection.GetNumberOfLevels(SelectionHelper.SelLimitType.Top) - 1;
			bool fEnd = CurrentSelection.Selection.EndBeforeAnchor;
			while (level >= 0)
			{
				int iBox = CurrentSelection.Selection.get_BoxIndex(fEnd, level);
				IVwSelection sel = Callbacks.EditedRootBox.MakeSelInBox(CurrentSelection.Selection, fEnd, level,
					iBox + 1, true, false, true);
				if (sel != null)
				{
					CurrentSelection.RootSite.ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
					return;
				}
				// Try the next level up
				level--;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid of the next property to be displayed for the current object
		/// </summary>
		/// <param name="flid">The flid of the current (i.e., selected) property</param>
		/// <returns>the flid of the next property to be displayed</returns>
		/// <remarks>ENHANCE: This approach will not work if the same flids are ever displayed
		/// at multiple levels (or in different frags) with different following flids</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual int GetNextFlid(int flid)
		{
			return -1;
		}
		#endregion

		#region Special deletions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is called by a view when the views code is about to delete a paragraph. We
		/// need to save the back translations by moving them to whatever paragraph the deleted
		/// one is merging with.
		/// </summary>
		/// <param name="selHelper">The selection helper</param>
		/// <param name="hvoObject">Paragraph to be deleted</param>
		/// <param name="hvoOwner">StText that owns the para</param>
		/// <param name="tag">flid in which para is owned</param>
		/// <param name="ihvo">index of paragraph in text</param>
		/// <param name="fMergeNext"><c>true</c> if this paragraph is merging with the
		/// following paragraph.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AboutToDelete(SelectionHelper selHelper, int hvoObject,
			int hvoOwner, int tag, int ihvo, bool fMergeNext)
		{
			CheckDisposed();

			if (tag != (int)StText.StTextTags.kflidParagraphs)
				return;

			StTxtPara paraToDelete = new StTxtPara(m_cache, hvoObject);

			// If the paragraph that is being deleted is empty, then do not attempt to save a back
			// translation for it.
			if (paraToDelete.Contents.Text == null)
				return;

			// ihvoTop is either the paragraph before the IP or the first paragraph in a range selection
			int ihvoTop = ihvo - 1;
			int hvoOwnerSurviving = hvoOwner;

			// Figure out what is being deleted and what is staying.
			// NOTE: it is possible that the selection is no longer valid. This is ok for our purposes here,
			// since all information we access here is already retrieved and stored in member variables of
			// SelectionHelper.
			if (selHelper.IsRange)
			{
				int paraLev = selHelper.GetLevelForTag(tag, SelectionHelper.SelLimitType.Top);
				SelLevInfo[] rgSelLevInfo = selHelper.GetLevelInfo(SelectionHelper.SelLimitType.Top);
				ihvoTop = rgSelLevInfo[paraLev].ihvo;
				if (paraLev + 1 < rgSelLevInfo.Length)
					hvoOwnerSurviving = rgSelLevInfo[paraLev + 1].hvo;
				int ihvoBottom = selHelper.GetLevelInfoForTag(tag, SelectionHelper.SelLimitType.Bottom).ihvo;

				// Pretty sure that if we get here top will NEVER equal bottom.
				Debug.Assert(ihvoTop != ihvoBottom || hvoOwnerSurviving != hvoOwner);
				if (hvoOwnerSurviving == hvoOwner)
				{
					if (ihvoTop == ihvoBottom)
						return;

					int ichEnd = selHelper.GetIch(SelectionHelper.SelLimitType.Bottom);
					// No need to merge because entire paragraph (with its contents) is going away.
					if (ihvo == ihvoBottom && ichEnd == paraToDelete.Contents.Length)
						return;
					// No need to merge because entire paragraph (with its contents) is going away.
					if (ihvo > ihvoTop && ihvo < ihvoBottom)
						return;
				}
			}

			// Determine the surviving paragraph.
			StText text = new StText(m_cache, hvoOwnerSurviving);
			StTxtPara paraSurviving;
			if (fMergeNext)
			{
				// when merging with next and there are no more paragraphs, then the BT can be discarded.
				if (text.ParagraphsOS.Count < ihvo + 1)
					return;
				// The surviving paragraph will be the one following the one that is deleted
				paraSurviving = (StTxtPara)text.ParagraphsOS[ihvo + 1];
			}
			else
			{
				// If we are deleting the first paragraph in the surviving text, the BT should
				// also be deleted, so we're done.
				if (ihvo == 0 && hvoOwnerSurviving == hvoOwner)
					return;
				// The surviving paragraph will be the top one in the selection
				paraSurviving = (StTxtPara)text.ParagraphsOS[ihvoTop];
			}

			ITsStrBldr bldr;
			ILgWritingSystemFactory wsf;
			List<int> writingSystems = GetWsList(out wsf);

			foreach (ICmTranslation transToDelete in paraToDelete.TranslationsOC)
			{
				// Find or create surviving translation of the same type.
				ICmTranslation transSurviving = paraSurviving.GetOrCreateTrans(transToDelete.TypeRA);

				// Merge back translations of the surviving and paragraph to be deleted for each writing system
				foreach (int ws in writingSystems)
				{
					TsStringAccessor tssAccToDelete = transToDelete.Translation.GetAlternative(ws);
					if (tssAccToDelete.Text != null)
					{
						TsStringAccessor tssAccSurviving = transSurviving.Translation.GetAlternative(ws);
						bldr = tssAccSurviving.UnderlyingTsString.GetBldr();

						// If the surviving paragraph ends with white space of the paragraph to delete
						// begins with white space, add white space.
						string textSurviving = bldr.Text;
						ILgCharacterPropertyEngine charPropEng = m_cache.UnicodeCharProps;
						if (textSurviving != null &&
							!charPropEng.get_IsSeparator(textSurviving[textSurviving.Length - 1]) &&
							!charPropEng.get_IsSeparator(tssAccToDelete.Text[0]))
						{
							bldr.ReplaceRgch(textSurviving.Length, textSurviving.Length, " ", 1, null);
						}

						int cch = bldr.Length;
						bldr.ReplaceTsString(cch, cch, tssAccToDelete.UnderlyingTsString);
						tssAccSurviving.UnderlyingTsString = bldr.GetString();
					}
				}
			}
		}
		#endregion
	}
}
