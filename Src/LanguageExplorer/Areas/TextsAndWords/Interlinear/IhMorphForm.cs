// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal class IhMorphForm : InterlinComboHandler
	{
		internal IhMorphForm()
			: base()
		{
		}
		internal IhMorphForm(SandboxBase sandbox)
			: base(sandbox)
		{
		}

		public override int IndexOfCurrentItem => 0;

		public override void SetupCombo()
		{
			base.SetupCombo();
			// Any time we pop this up, the text in the box is the text form of the current
			// analysis, as a starting point.
			var cmorphs = MorphCount;
			Debug.Assert(cmorphs != 0); // we're supposed to be building on one of them!

			var wordform = m_sandbox.GetWordformOfAnalysis();
			var wa = m_sandbox.GetWfiAnalysisInUse();

			// Find the actual original form of the current wordform
			var tssForm = m_sandbox.FindAFullWordForm(wordform);
			var form = StrFromTss(tssForm);
			var fBaseWordIsPhrase = SandboxBase.IsPhrase(form);

			// First, store the current morph breakdown if we have one,
			// Otherwise, if the user has deleted all the morphemes on the morpheme line
			// (per LT-1621) simply use the original wordform.
			// NOTE: Normally we would use Sandbox.IsMorphFormLineEmpty for this condition
			// but since we're already using the variable(s) needed for this check,
			// here we'll use those variables for economy/performance instead.
			var currentBreakdown = m_sandbox.SandboxEditMonitor.BuildCurrentMorphsString();
			if (currentBreakdown != string.Empty)
			{
				ComboList.Text = currentBreakdown;
				// The above and every other distinct morpheme breakdown from owned
				// WfiAnalyses are possible choices.
				var tssText = TsStringUtils.MakeString(currentBreakdown, m_wsVern);
				ComboList.Items.Add(tssText);
			}
			else
			{
				ComboList.Text = form;
				ComboList.Items.Add(tssForm);
			}
			// if we added the fullWordform (or the current breakdown is somehow empty although we may have an analysis), then add the
			// wordform HVO; otherwise, add the analysis HVO.
			if (currentBreakdown == string.Empty || (wa == null && tssForm != null && tssForm.Equals(ComboList.Items[0] as ITsString)))
			{
				m_items.Add(wordform?.Hvo ?? 0);
			}
			else
			{
				m_items.Add(wa?.Hvo ?? 0);  // [wfi] hvoAnalysis may equal '0' (for annotations that are instances of Wordform).
			}
			Debug.Assert(m_items.Count == ComboList.Items.Count, "combo list (m_comboList) should contain the same count as the m_items list (hvos)");
			AddAnalysesOf(wordform, fBaseWordIsPhrase);
			// Add the original wordform, if not already present.
			AddIfNotPresent(tssForm, wordform);
			ComboList.SelectedIndex = IndexOfCurrentItem;

			// Add any relevant 'other case' forms.
			int wsVern = m_sandbox.RawWordformWs;
			string locale = m_caches.MainCache.ServiceLocator.WritingSystemManager.Get(wsVern).IcuLocale;
			CaseFunctions cf = new CaseFunctions(locale);
			switch (m_sandbox.CaseStatus)
			{
				case StringCaseStatus.allLower:
					break; // no more to add
				case StringCaseStatus.title:
					AddOtherCase(cf.SwitchTitleAndLower(form));
					break;
				case StringCaseStatus.mixed:
					switch (cf.StringCase(form))
					{
						case StringCaseStatus.allLower:
							AddOtherCase(cf.ToTitle(form));
							AddOtherCase(m_sandbox.RawWordform.Text);
							break;
						case StringCaseStatus.title:
							AddOtherCase(cf.ToLower(form));
							AddOtherCase(m_sandbox.RawWordform.Text);
							break;
						case StringCaseStatus.mixed:
							AddOtherCase(cf.ToLower(form));
							AddOtherCase(cf.ToTitle(form));
							break;
					}
					break;
			}
			Debug.Assert(m_items.Count == ComboList.Items.Count,
				"combo list (m_comboList) should contain the same count as the m_items list (hvos)");
			ComboList.Items.Add(ITextStrings.ksEditMorphBreaks_);
		}

		/// <summary>
		/// Add to the combo the specified alternate-case form of the word.
		/// </summary>
		private void AddOtherCase(string other)
		{
			// 0 is a reserved value for other case wordform
			AddIfNotPresent(TsStringUtils.MakeString(other, m_sandbox.RawWordformWs), null);
		}

		/// <summary>
		/// Add to the combo the analyses of the specified wordform (that don't already occur).
		/// REFACTOR : possibly could refactor with SandboxEditMonitor.BuildCurrentMorphsString
		/// </summary>
		private void AddAnalysesOf(IWfiWordform wordform, bool fBaseWordIsPhrase)
		{
			if (wordform == null)
			{
				return; // no real wordform, can't have analyses.
			}
			var builder = TsStringUtils.MakeStrBldr();
			var space = TsStringUtils.MakeString(fBaseWordIsPhrase ? "  " : " ", m_wsVern);
			foreach (var wa in wordform.AnalysesOC)
			{
				var opinions = wa.GetAgentOpinion(m_caches.MainCache.LangProject.DefaultUserAgent);
				if (opinions == Opinions.disapproves)
				{
					continue;   // skip any analysis the user has disapproved.
				}
				var cmorphs = wa.MorphBundlesOS.Count;
				if (cmorphs == 0)
				{
					continue;
				}
				builder.Clear();
				for (var imorph = 0; imorph < cmorphs; ++imorph)
				{
					if (imorph != 0)
					{
						builder.ReplaceTsString(builder.Length, builder.Length, space);
					}
					var mb = wa.MorphBundlesOS[imorph];
					var morph = mb.MorphRA;
					if (morph != null)
					{
						var tss = morph.Form.get_String(m_sandbox.RawWordformWs);
						var morphType = morph.MorphTypeRA;
						var sPrefix = morphType.Prefix;
						var sPostfix = morphType.Postfix;
						var ich = builder.Length;
						builder.ReplaceTsString(ich, ich, tss);
						if (!string.IsNullOrEmpty(sPrefix))
						{
							builder.Replace(ich, ich, sPrefix, null);
						}

						if (!string.IsNullOrEmpty(sPostfix))
						{
							builder.Replace(builder.Length, builder.Length, sPostfix, null);
						}
					}
					else
					{
						// No MoMorph object? Must be the Form string.
						var tss = mb.Form.get_String(m_sandbox.RawWordformWs);
						builder.ReplaceTsString(builder.Length, builder.Length, tss);
					}
				}
				var tssAnal = builder.GetString();
				// Add only non-whitespace morpheme breakdowns.
				if (tssAnal.Length > 0 && tssAnal.Text.Trim().Length > 0)
				{
					AddIfNotPresent(tssAnal, wa);
				}
			}
		}

		/// <summary>
		/// Add an item to the combo unless it is already present.
		/// </summary>
		private void AddIfNotPresent(ITsString tssAnal, ICmObject analysisObj)
		{
			// Can't use m_comboList.Items.Contains() because it doesn't use our Equals
			// function and just notes that all the TsStrings are different objects.
			var fFound = false;
			foreach (ITsString tss in ComboList.Items)
			{
				if (tss.Equals(tssAnal))
				{
					fFound = true;
					break;
				}
			}

			if (fFound)
			{
				return;
			}
			ComboList.Items.Add(tssAnal);
			m_items.Add(analysisObj?.Hvo ?? 0);

		}

		public override bool HandleReturnKey()
		{
			var cda = (IVwCacheDa)m_caches.DataAccess;
			var sda = m_caches.DataAccess;
			var cmorphs = MorphCount;
			// JohnT: 0 is fine, that's what we see for a word which has no known analyses and
			// shows up as *** on the morphs line.
			for (var imorph = 0; imorph < cmorphs; ++imorph)
			{
				var hvoMbSec = MorphHvo(imorph);
				// Erase all the information.
				cda.CacheObjProp(hvoMbSec, SandboxBase.ktagSbMorphForm, 0);
				cda.CacheObjProp(hvoMbSec, SandboxBase.ktagSbMorphEntry, 0);
				cda.CacheObjProp(hvoMbSec, SandboxBase.ktagSbMorphGloss, 0);
				cda.CacheObjProp(hvoMbSec, SandboxBase.ktagSbMorphPos, 0);
				cda.CacheStringProp(hvoMbSec, SandboxBase.ktagSbMorphPrefix, null);
				cda.CacheStringProp(hvoMbSec, SandboxBase.ktagSbMorphPostfix, null);
				// Send notifiers for each of these deleted items.
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoMbSec, SandboxBase.ktagSbMorphForm, 0, 1, 1);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoMbSec, SandboxBase.ktagSbMorphEntry, 0, 0, 1);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoMbSec, SandboxBase.ktagSbMorphGloss, 0, 0, 1);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoMbSec, SandboxBase.ktagSbMorphPos, 0, 0, 1);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoMbSec, SandboxBase.ktagSbMorphPrefix, 0, 0, 1);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoMbSec, SandboxBase.ktagSbMorphPostfix, 0, 0, 1);
			}
			// Now erase the morph bundles themselves.
			cda.CacheVecProp(m_hvoSbWord, SandboxBase.ktagSbWordMorphs, new int[0], 0);
			sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord, SandboxBase.ktagSbWordMorphs, 0, 0, 1);

			var mb = new MorphemeBreaker(m_caches, ComboList.Text, m_hvoSbWord, m_wsVern, m_sandbox);
			mb.Run();
			// Everything changed, more or less.
			m_rootb.Reconstruct();
			// Todo: having called reconstruct, selection is invalid, may have to do
			// something special about making a new one.
			return true;
		}

		public override void HandleSelect(int index)
		{
			string sMorphs;
			if (index >= m_items.Count)
			{
				// The user did not choose an existing set of morph breaks, which means that
				// he wants to bring up a dialog to edit the morph breaks manually.
				sMorphs = EditMorphBreaks();
			}
			else
			{
				// user selected an existing set of morph breaks.
				var menuItemForm = (ComboList.Items[index]) as ITsString;
				Debug.Assert(menuItemForm != null, "menu item should be TsString");
				var hvoAnal = m_items[index];
				if (hvoAnal == 0)
				{
					// We're looking at an alternate case form of the whole word.
					// Switch the sandbox to the corresponding form.
					m_sandbox.SetWordform(menuItemForm, true);
					return;
				}
				// use the new morph break down.
				sMorphs = StrFromTss(menuItemForm);
			}
			UpdateMorphBreaks(sMorphs);
			m_sandbox.SelectIconOfMorph(0, SandboxBase.ktagMorphFormIcon);
		}

		internal void UpdateMorphBreaks(string sMorphs)
		{
			if (sMorphs != null && sMorphs.Trim().Length > 0)
			{
				sMorphs = sMorphs.Trim();
			}
			else
			{
				return;
			}

			var sda = m_caches.DataAccess;
			// Compare to the actual original form of the sandbox wordform
			var wf = m_sandbox.GetWordformOfAnalysis();
			var tssWordform = m_sandbox.FindAFullWordForm(wf);
			var wordform = StrFromTss(tssWordform);
			if (wordform == sMorphs)
			{
				// The only wordform choice in the list is the wordform of
				// some current analysis. We want to switch to that original wordform.
				// We do NOT want to look up the default, because that could well have an
				// existing morpheme breakdown, preventing us from getting back to the original
				// whole word.
				m_sandbox.SetWordform(tssWordform, false);
				return;
			}
			// We want to try to break this down into morphemes.
			// nb: use sda.Replace rather than cds.CachVecProp so that this registers as a change
			// in need of saving.
			var coldMorphs = sda.get_VecSize(m_hvoSbWord, SandboxBase.ktagSbWordMorphs);
			sda.Replace(m_hvoSbWord, SandboxBase.ktagSbWordMorphs, 0, coldMorphs, new int[0], 0);
			sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord, SandboxBase.ktagSbWordMorphs, 0, 0, coldMorphs);
			var mb = new MorphemeBreaker(m_caches, sMorphs, m_hvoSbWord, m_wsVern, m_sandbox);
			mb.Run();
			// Everything changed, more or less.
			m_rootb.Reconstruct();
			// We've changed properties that the morph manager cares about, but we don't want it
			// to fire when we fix the selection.
			m_sandbox.EditMonitor.NeedMorphemeUpdate = false;
		}

		/// <summary />
		/// <returns>string of new morph breaks</returns>
		internal string EditMorphBreaks()
		{
			string sMorphs;
			var propTable = ((IPropertyTableProvider)m_sandbox.FindForm()).PropertyTable;
			using (var dlg = new EditMorphBreaksDlg(propTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider)))
			{
				var tssWord = m_sandbox.SbWordForm(m_sandbox.RawWordformWs);
				sMorphs = m_sandbox.SandboxEditMonitor.BuildCurrentMorphsString();
				dlg.Initialize(tssWord, sMorphs, m_caches.MainCache.MainCacheAccessor.WritingSystemFactory, m_caches.MainCache, m_sandbox.StyleSheet);
				var mainWnd = m_sandbox.FindForm();
				// Making the form active fixes problems like LT-2619.
				// I'm (RandyR) not sure what adverse impact might show up by doing this.
				mainWnd.Activate();
				sMorphs = dlg.ShowDialog(mainWnd) == DialogResult.OK ? dlg.GetMorphs() : null;
			}
			return sMorphs;
		}
	}
}