// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// User-interface behavior for LexEntries.
	/// </summary>
	internal sealed class LexEntryUi : CmObjectUi
	{
		/// <summary>
		/// Make one. The argument should really be a LexEntry.
		/// </summary>
		private LexEntryUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is ILexEntry);
		}

		/// <summary>
		/// Create default valued entry.
		/// </summary>
		internal LexEntryUi()
		{
		}

		/// <summary />
		internal override IVwViewConstructor VernVc => new LexEntryVc(m_cache);

		/// <summary>
		/// Find the list of LexEntry objects which conceivably match the given wordform.
		/// </summary>
		private static List<ILexEntry> FindEntriesForWordformUI(LcmCache cache, ITsString tssWf, IWfiAnalysis wfa)
		{
			var duplicates = false;
			var retval = cache.ServiceLocator.GetInstance<ILexEntryRepository>().FindEntriesForWordform(cache, tssWf, wfa, ref duplicates);
			if (duplicates)
			{
				MessageBox.Show(Form.ActiveForm, string.Format(LcmUiResources.ksDuplicateWordformsMsg, tssWf.Text), LcmUiResources.ksDuplicateWordformsCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			return retval;
		}

		/// <summary>
		/// Find wordform given a cache and the string.
		/// </summary>
		internal static LexEntryUi FindEntryForWordform(LcmCache cache, ITsString tssWf)
		{
			var matchingEntry = cache.ServiceLocator.GetInstance<ILexEntryRepository>().FindEntryForWordform(cache, tssWf);
			return matchingEntry == null ? null : new LexEntryUi(matchingEntry);
		}

		/// <summary />
		internal static void DisplayOrCreateEntry(LcmCache cache, int hvoSrc, int tagSrc, int wsSrc, int ichMin, int ichLim, IWin32Window owner, FlexComponentParameters flexComponentParameters, IHelpTopicProvider helpProvider, string helpFileKey)
		{
			var tssContext = cache.DomainDataByFlid.get_StringProp(hvoSrc, tagSrc);
			if (tssContext == null)
			{
				return;
			}
			var text = tssContext.Text;
			// If the string is empty, it might be because it's multilingual.  Try that alternative.
			// (See TE-6374.)
			if (text == null && wsSrc != 0)
			{
				tssContext = cache.DomainDataByFlid.get_MultiStringAlt(hvoSrc, tagSrc, wsSrc);
				if (tssContext != null)
				{
					text = tssContext.Text;
				}
			}
			ITsString tssWf = null;
			if (text != null)
			{
				tssWf = tssContext.GetSubstring(ichMin, ichLim);
			}
			if (tssWf == null || tssWf.Length == 0)
			{
				return;
			}
			// We want to limit the lookup to the current word's current analysis, if one exists.
			// See FWR-956.
			IWfiAnalysis wfa = null;
			if (tagSrc == StTxtParaTags.kflidContents)
			{
				IAnalysis anal = null;
				var para = cache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(hvoSrc);
				foreach (var seg in para.SegmentsOS)
				{
					if (seg.BeginOffset <= ichMin && seg.EndOffset >= ichLim)
					{
						var occurrence = seg.FindWagform(ichMin - seg.BeginOffset, ichLim - seg.BeginOffset, out _);
						if (occurrence != null)
						{
							anal = occurrence.Analysis;
						}
						break;
					}
				}
				if (anal != null)
				{
					switch (anal)
					{
						case IWfiAnalysis analysis:
							wfa = analysis;
							break;
						case IWfiGloss gloss:
							wfa = gloss.OwnerOfClass<IWfiAnalysis>();
							break;
					}
				}
			}
			DisplayEntries(cache, owner, flexComponentParameters, helpProvider, helpFileKey, tssWf, wfa);
		}

		internal static void DisplayEntries(LcmCache cache, IWin32Window owner, FlexComponentParameters flexComponentParameters, IHelpTopicProvider helpProvider, string helpFileKey, ITsString tssWfIn, IWfiAnalysis wfa)
		{
			var tssWf = tssWfIn;
			var entries = FindEntriesForWordformUI(cache, tssWf, wfa);
			var styleSheet = FwUtils.StyleSheetFromPropertyTable(flexComponentParameters.PropertyTable);
			if (entries == null || entries.Count == 0)
			{
				var entry = ShowFindEntryDialog(cache, flexComponentParameters, tssWf, owner);
				if (entry == null)
				{
					return;
				}

				entries = new List<ILexEntry>(1) { entry };
			}
			DisplayEntriesRecursive(cache, owner, flexComponentParameters, styleSheet, helpProvider, helpFileKey, entries, tssWf);
		}

		private static void DisplayEntriesRecursive(LcmCache cache, IWin32Window owner, FlexComponentParameters flexComponentParameters, IVwStylesheet stylesheet,
			IHelpTopicProvider helpProvider, string helpFileKey, List<ILexEntry> entries, ITsString tssWf)
		{
			// Loop showing the SummaryDialogForm as long as the user clicks the Other button
			// in that dialog.
			bool otherButtonClicked;
			do
			{
				using (var sdform = new SummaryDialogForm(new List<int>(entries.Select(le => le.Hvo)), helpProvider, helpFileKey, stylesheet, cache, flexComponentParameters.PropertyTable))
				{
					SetCurrentModalForm(sdform);
					if (owner == null)
					{
						sdform.StartPosition = FormStartPosition.CenterScreen;
					}
					sdform.ShowDialog(owner);
					if (sdform.ShouldLink)
					{
						sdform.LinkToLexicon();
					}
					otherButtonClicked = sdform.OtherButtonClicked;
				}
				if (otherButtonClicked)
				{
					// Look for another entry to display.  (If the user doesn't select another
					// entry, loop back and redisplay the current entry.)
					var entry = ShowFindEntryDialog(cache, flexComponentParameters, tssWf, owner);
					if (entry != null)
					{
						// We need a list that contains the entry we found to display on the
						// next go around of this loop.
						entries = new List<ILexEntry> { entry };
						tssWf = entry.HeadWord;
					}
				}
			} while (otherButtonClicked);
		}

		/// <summary>
		/// Set a Modal Form to temporarily show on top of all applications
		/// and have an icon that is accessible for the user after it goes behind other users.
		/// See http://support.ubs-icap.org/default.asp?11269
		/// </summary>
		private static void SetCurrentModalForm(Form newActiveModalForm)
		{
			newActiveModalForm.TopMost = true;
			newActiveModalForm.Activated += s_activeModalForm_Activated;
			newActiveModalForm.ShowInTaskbar = true;
		}

		/// <summary>
		/// setting TopMost in SetCurrentModalForm() forces a dialog to show on top of other applications
		/// in another process that want to launch this dialog (e.g. Paratext via WCF).
		/// but we don't want it to stay on top if the User switches to another application,
		/// so reset TopMost to false after it has launched to the top.
		/// </summary>
		private static void s_activeModalForm_Activated(object sender, EventArgs e)
		{
			((Form)sender).TopMost = false;
		}

		/// <summary>
		/// Assuming the selection can be expanded to a word and a corresponding LexEntry can
		/// be found, show the related words dialog with the words related to the selected one.
		/// </summary>
		/// <remarks>
		/// Currently only called from WCF (11/21/2013 - AP)
		/// </remarks>
		internal static void DisplayRelatedEntries(LcmCache cache, IWin32Window owner, IVwStylesheet styleSheet, IHelpTopicProvider helpProvider, string helpFileKey, ITsString tssWf,
			bool hideInsertButton, IVwSelection sel = null)
		{
			if (tssWf == null || tssWf.Length == 0)
			{
				return;
			}
			using (var leui = FindEntryForWordform(cache, tssWf))
			{
				if (leui == null)
				{
					RelatedWords.ShowNotInDictMessage(owner);
					return;
				}
				var hvoEntry = leui.MyCmObject.Hvo;
				if (!RelatedWords.LoadDomainAndRelationInfo(cache, hvoEntry, out var domains, out var lexrels, out var cdaTemp, owner))
				{
					return;
				}
				using (var rw = new RelatedWords(cache, sel, hvoEntry, domains, lexrels, cdaTemp, styleSheet, hideInsertButton))
				{
					rw.ShowDialog(owner);
				}
			}
		}

		/// <summary>
		/// Launch the Find Entry dialog, and if one is created or selected return it.
		/// </summary>
		/// <returns>The HVO of the selected or created entry</returns>
		private static ILexEntry ShowFindEntryDialog(LcmCache cache, FlexComponentParameters flexComponentParameters, ITsString tssForm, IWin32Window owner)
		{
			using (var entryGoDlg = new EntryGoDlg())
			{
				entryGoDlg.InitializeFlexComponent(flexComponentParameters);
				// Temporarily set TopMost to true so it will launch above any calling app (e.g. Paratext)
				// but reset after activated.
				SetCurrentModalForm(entryGoDlg);
				var wp = new WindowParams
				{
					m_btnText = LcmUiResources.ksShow,
					m_title = LcmUiResources.ksFindInDictionary,
					m_label = LcmUiResources.ksFind_
				};
				if (owner == null)
				{
					entryGoDlg.StartPosition = FormStartPosition.CenterScreen;
				}
				entryGoDlg.Owner = owner as Form;
				entryGoDlg.SetDlgInfo(cache, wp, tssForm);
				entryGoDlg.SetHelpTopic("khtpFindInDictionary");
				if (entryGoDlg.ShowDialog() == DialogResult.OK)
				{
					var entry = entryGoDlg.SelectedObject as ILexEntry;
					Debug.Assert(entry != null);
					return entry;
				}
			}
			return null;
		}
	}
}