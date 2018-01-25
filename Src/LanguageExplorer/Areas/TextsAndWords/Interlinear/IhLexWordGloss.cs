// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal class IhLexWordGloss : IhMorphEntry
	{
		internal IhLexWordGloss(IHelpTopicProvider helpTopicProvider) : base(helpTopicProvider)
		{
		}

		protected override void SelectEntryIcon(int morphIndex)
		{
			m_sandbox.SelectIcon(SandboxBase.ktagWordGlossIcon);
		}

		protected override void CopyLexEntryInfoToMonomorphemicWordGlossAndPos()
		{
			CopyLexEntryInfoToMonomorphemicWordGlossAndPos(true, true);
		}

		/// <summary>
		/// In the context of a LexWordGloss handler, the user is making a selection in the word combo list
		/// that should fill in the Word Gloss. So, make sure we copy the selected lex information.
		/// </summary>
		protected override void CopyLexEntryInfoToMonomorphemicWordGlossAndPos(bool fCopyToWordGloss, bool fCopyToWordPos)
		{
			base.CopyLexEntryInfoToMonomorphemicWordGlossAndPos(true, true);
		}

		protected override void CopySenseToWordGloss(bool fCopyWordGloss, int hvoSbRootSense)
		{
			if (hvoSbRootSense == 0 && fCopyWordGloss)
			{
				// clear out the WordGloss line(s).
				var sda = m_caches.DataAccess;
				foreach (var wsId in m_sandbox.InterlinLineChoices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss))
				{
					var tssGloss = TsStringUtils.MakeString("", wsId);
					sda.SetMultiStringAlt(m_hvoSbWord, SandboxBase.ktagSbWordGloss, wsId, tssGloss);
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord, SandboxBase.ktagSbWordGloss, wsId, 0, 0);
				}
			}
			else
			{
				base.CopySenseToWordGloss(fCopyWordGloss, hvoSbRootSense);
			}
			// treat as a deliberate user selection, not a guess.
			if (fCopyWordGloss)
			{
				m_caches.DataAccess.SetInt(m_hvoSbWord, SandboxBase.ktagSbWordGlossGuess, 0);
			}
		}

		protected override int CopyLexPosToWordPos(bool fCopyToWordCat, int hvoLexPos)
		{

			int hvoPos;
			if (fCopyToWordCat && hvoLexPos == 0)
			{
				// clear out the existing POS
				hvoPos = m_sandbox.CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidWordPos, hvoLexPos, CmPossibilityTags.kflidAbbreviation);
				var hvoSbWordPos = m_caches.DataAccess.get_ObjectProp(m_hvoSbWord, SandboxBase.ktagSbWordPos);
				m_caches.DataAccess.SetObjProp(m_hvoSbWord, SandboxBase.ktagSbWordPos, hvoPos);
				m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord, SandboxBase.ktagSbWordPos, 0, 1, (hvoSbWordPos == 0 ? 0 : 1));
			}
			else
			{
				hvoPos = base.CopyLexPosToWordPos(fCopyToWordCat, hvoLexPos);
			}
			// treat as a deliberate user selection, not a guess.
			if (fCopyToWordCat)
			{
				m_caches.DataAccess.SetInt(hvoPos, SandboxBase.ktagSbNamedObjGuess, 0);
			}
			return hvoPos;

		}
	}
}