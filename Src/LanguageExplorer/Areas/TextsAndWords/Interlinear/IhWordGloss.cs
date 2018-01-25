// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Drawing;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// The WordGloss has no interesting menu for now. Just allow the text to be edited.
	/// </summary>
	internal class IhWordGloss : InterlinComboHandler
	{
		public IhWordGloss()
			: base()
		{
		}

		public override void HandleSelect(int index)
		{
			CheckDisposed();

			var fGuessingOld = m_caches.DataAccess.get_IntProp(m_hvoSbWord, SandboxBase.ktagSbWordGlossGuess);
			var item = ComboList.SelectedItem as HvoTssComboItem;
			if (item == null)
			{
				return;
			}
			m_sandbox.WordGlossHvo = item.Hvo;
			foreach (var ws in m_sandbox.InterlinLineChoices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss))
			{
				var tss = item.Hvo == 0 ? TsStringUtils.EmptyString(ws) : m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(item.Hvo, WfiGlossTags.kflidForm, ws);
				m_caches.DataAccess.SetMultiStringAlt(m_hvoSbWord, SandboxBase.ktagSbWordGloss, ws, tss);
				// Regenerate the string regardless.  (See LT-10456)
				m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord, SandboxBase.ktagSbWordGloss, ws, tss.Length, tss.Length);
				// If it used to be a guess, mark it as no longer a guess.
				if (fGuessingOld == 0)
				{
					continue;
				}
				m_caches.DataAccess.SetInt(m_hvoSbWord, SandboxBase.ktagSbWordGlossGuess, 0);
				m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord, SandboxBase.ktagSbWordGlossGuess, 0, 1, 1);
			}

			m_sandbox.SelectAtEndOfWordGloss(-1);
		}

		// Not much to do except to initialize the edit box embedded in the combobox with
		// the proper writing system factory, writing system, and TsString.
		public override void SetupCombo()
		{
			CheckDisposed();

			base.SetupCombo();
			var hvoEmptyGloss = 0;
			var tsb = TsStringUtils.MakeStrBldr();
			ComboList.WritingSystemFactory = m_caches.MainCache.LanguageWritingSystemFactoryAccessor;
			// Find the WfiAnalysis (from existing analysis or guess) to provide its word glosses as options (cf. LT-1428)
			var wa = m_sandbox.GetWfiAnalysisInUse();
			if (wa != null)
			{
				AddComboItems(ref hvoEmptyGloss, tsb, wa);
			}
			// TODO: Maybe this should merge invisibly with the current top of the undo stack? or start an
			// invisible top?
			using (var helper = new NonUndoableUnitOfWorkHelper(m_caches.MainCache.ActionHandlerAccessor))
			{
				var analMethod = m_sandbox.CreateRealWfiAnalysisMethod();
				var anal = analMethod.Run();

				helper.RollBack = false;
				if (anal is IWfiAnalysis && anal.Guid != wa.Guid)
				{
					AddComboItems(ref hvoEmptyGloss, tsb, anal as IWfiAnalysis);
				}
				Debug.Assert(analMethod.ObsoleteAnalysis == null);
			}
			ComboList.Items.Add(new HvoTssComboItem(hvoEmptyGloss, TsStringUtils.MakeString(ITextStrings.ksNewWordGloss2, m_caches.MainCache.DefaultUserWs)));
			// Set combo selection to current selection.
			ComboList.SelectedIndex = IndexOfCurrentItem;

			// Enhance JohnT: if the analysts decide so, here we add all the other glosses from other analyses.
		}

		private void AddComboItems(ref int hvoEmptyGloss, ITsStrBldr tsb, IWfiAnalysis wa)
		{
			var wsids = m_sandbox.InterlinLineChoices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss);
			foreach (var gloss in wa.MeaningsOC)
			{
				var glossCount = 0;
				foreach (var ws in wsids)
				{
					var nextWsGloss = gloss.Form.get_String(ws);
					if (nextWsGloss.Length == 0)
					{
						continue;
					}
					// Append a comma if there are more glosses.
					if (glossCount > 0)
					{
						tsb.Replace(tsb.Length, tsb.Length, ", ", null);
					}

					// Append a Ws label if there are more than one Ws.
					if (wsids.Count > 1)
					{
						tsb.ReplaceTsString(tsb.Length, tsb.Length, WsListManager.WsLabel(m_caches.MainCache, ws));
						tsb.Replace(tsb.Length, tsb.Length, " ", null);
					}
					var oldLen = tsb.Length;
					tsb.ReplaceTsString(oldLen, oldLen, nextWsGloss);
					var color = (int)CmObjectUi.RGB(Color.Blue);
					tsb.SetIntPropValues(oldLen, tsb.Length, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, color);
					glossCount++;
				}
				// (LT-1428) If we find an empty gloss, use this hvo for "New word gloss" instead of 0.
				if (glossCount == 0 && wsids.Count > 0)
				{
					hvoEmptyGloss = gloss.Hvo;
					var tpbUserWs = TsStringUtils.MakePropsBldr();
					tpbUserWs.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsUser);
					tsb.Replace(tsb.Length, tsb.Length, ITextStrings.ksEmpty, tpbUserWs.GetTextProps());
				}

				ComboList.Items.Add(new HvoTssComboItem(gloss.Hvo, tsb.GetString()));
				tsb.Clear();
			}
		}

		/// <summary>
		/// Return the index corresponding to the current WordGloss state of the Sandbox.
		/// </summary>
		public override int IndexOfCurrentItem
		{
			get
			{
				for (var i = 0; i < ComboList.Items.Count; ++i)
				{
					var item = (HvoTssComboItem)ComboList.Items[i];
					if (item.Hvo == m_sandbox.WordGlossHvo)
					{
						return i;
					}
				}
				return -1;
			}
		}
	}
}