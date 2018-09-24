// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls.LexText;
using LanguageExplorer.LcmUi.Dialogs;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// User-interface behavior for LexEntries.
	/// </summary>
	public class LexEntryUi : CmObjectUi
	{
		/// <summary>
		/// Make one. The argument should really be a LexEntry.
		/// </summary>
		public LexEntryUi(ICmObject obj)
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
		public override IVwViewConstructor VernVc => new LexEntryVc(m_cache);

		/// <summary>
		/// Given an object id, a (string-valued) property ID, and a range of characters,
		/// return the LexEntry that is the best guess as to a useful LE to show to
		/// provide information about that wordform.
		///
		/// Note: the interface takes this form because eventually we may want to query to
		/// see whether the text has been analyzed and we have a known morpheme breakdown
		/// for this wordform. Otherwise, at present we could just pass the text.
		/// </summary>
		public static LexEntryUi FindEntryForWordform(LcmCache cache, int hvoSrc, int tagSrc, int ichMin, int ichLim)
		{
			var tssContext = cache.DomainDataByFlid.get_StringProp(hvoSrc, tagSrc);
			if (tssContext == null)
			{
				return null;
			}
			var tssWf = tssContext.GetSubstring(ichMin, ichLim);
			return FindEntryForWordform(cache, tssWf);
		}

		/// <summary>
		/// Find the list of LexEntry objects which conceivably match the given wordform.
		/// </summary>
		public static List<ILexEntry> FindEntriesForWordformUI(LcmCache cache, ITsString tssWf, IWfiAnalysis wfa)
		{
			var duplicates = false;
			var retval = cache.ServiceLocator.GetInstance<ILexEntryRepository>().FindEntriesForWordform(cache, tssWf, wfa, ref duplicates);

			if (duplicates)
			{
				MessageBox.Show(Form.ActiveForm, string.Format(LcmUiStrings.ksDuplicateWordformsMsg, tssWf.Text), LcmUiStrings.ksDuplicateWordformsCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			return retval;
		}

		/// <summary>
		/// Find wordform given a cache and the string.
		/// </summary>
		public static LexEntryUi FindEntryForWordform(LcmCache cache, ITsString tssWf)
		{
			var matchingEntry = cache.ServiceLocator.GetInstance<ILexEntryRepository>().FindEntryForWordform(cache, tssWf);
			return matchingEntry == null ? null : new LexEntryUi(matchingEntry);
		}

		/// <summary />
		public static void DisplayOrCreateEntry(LcmCache cache, int hvoSrc, int tagSrc, int wsSrc,
			int ichMin, int ichLim, IWin32Window owner, IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber,
			IHelpTopicProvider helpProvider, string helpFileKey)
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
						bool exact;
						var occurrence = seg.FindWagform(ichMin - seg.BeginOffset, ichLim - seg.BeginOffset, out exact);
						if (occurrence != null)
						{
							anal = occurrence.Analysis;
						}
						break;
					}
				}
				if (anal != null)
				{
					if (anal is IWfiAnalysis)
					{
						wfa = (IWfiAnalysis)anal;
					}
					else if (anal is IWfiGloss)
					{
						wfa = ((IWfiGloss)anal).OwnerOfClass<IWfiAnalysis>();
					}
				}
			}
			DisplayEntries(cache, owner, propertyTable, publisher, subscriber, helpProvider, helpFileKey, tssWf, wfa);
		}

		internal static void DisplayEntry(LcmCache cache, IWin32Window owner, IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber,
			IHelpTopicProvider helpProvider, string helpFileKey, ITsString tssWfIn)
		{
			var tssWf = tssWfIn;
			LexEntryUi leui = null;
			try
			{
				leui = FindEntryForWordform(cache, tssWf);

				// if we do not find a match for the word then try converting it to lowercase and see if there
				// is an entry in the lexicon for the Wordform in lowercase. This is needed for occurences of
				// words which are capitalized at the beginning of sentences.  LT-7444 RickM
				if (leui == null)
				{
					//We need to be careful when converting to lowercase therefore use Icu.ToLower()
					//get the WS of the tsString
					var wsWf = TsStringUtils.GetWsAtOffset(tssWf, 0);
					//use that to get the locale for the WS, which is used for
					var wsLocale = cache.ServiceLocator.WritingSystemManager.Get(wsWf).IcuLocale;
					var sLower = Icu.ToLower(tssWf.Text, wsLocale);
					var ttp = tssWf.get_PropertiesAt(0);
					tssWf = TsStringUtils.MakeString(sLower, ttp);
					leui = FindEntryForWordform(cache, tssWf);
				}

				EnsureWindowConfiguration(propertyTable);
				var styleSheet = FwUtils.StyleSheetFromPropertyTable(propertyTable);
				if (leui == null)
				{
					var entry = ShowFindEntryDialog(cache, propertyTable, publisher, subscriber, tssWf, owner);
					if (entry == null)
					{
						return;
					}
					leui = new LexEntryUi(entry);
				}
				leui.ShowSummaryDialog(owner, tssWf, helpProvider, helpFileKey, styleSheet);
			}
			finally
			{
				leui?.Dispose();
			}
		}

		public static void DisplayEntries(LcmCache cache, IWin32Window owner, IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber,
			IHelpTopicProvider helpProvider, string helpFileKey, ITsString tssWfIn, IWfiAnalysis wfa)
		{
			var tssWf = tssWfIn;
			var entries = FindEntriesForWordformUI(cache, tssWf, wfa);
			var styleSheet = FwUtils.StyleSheetFromPropertyTable(propertyTable);
			if (entries == null || entries.Count == 0)
			{
				var entry = ShowFindEntryDialog(cache, propertyTable, publisher, subscriber, tssWf, owner);
				if (entry == null)
				{
					return;
				}

				entries = new List<ILexEntry>(1) {entry};
			}
			DisplayEntriesRecursive(cache, owner, propertyTable, publisher, subscriber, styleSheet, helpProvider, helpFileKey, entries, tssWf);
		}

		private static void DisplayEntriesRecursive(LcmCache cache, IWin32Window owner,
			IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber, IVwStylesheet stylesheet,
			IHelpTopicProvider helpProvider, string helpFileKey,
			List<ILexEntry> entries, ITsString tssWf)
		{
			// Loop showing the SummaryDialogForm as long as the user clicks the Other button
			// in that dialog.
			bool otherButtonClicked;
			do
			{
				using (var sdform = new SummaryDialogForm(new List<int>(entries.Select(le => le.Hvo)), helpProvider, helpFileKey, stylesheet, cache, propertyTable))
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
					var entry = ShowFindEntryDialog(cache, propertyTable, publisher, subscriber, tssWf, owner);
					if (entry != null)
					{
						// We need a list that contains the entry we found to display on the
						// next go around of this loop.
						entries = new List<ILexEntry> {entry};
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
		static void s_activeModalForm_Activated(object sender, EventArgs e)
		{
			((Form)sender).TopMost = false;
		}

		private static void EnsureWindowConfiguration(IPropertyTable propertyTable)
		{
#if RANDYTODO
			XmlNode xnWindow = propertyTable.GetValue<XmlNode>("WindowConfiguration");
			if (xnWindow == null)
			{
				string configFile = FwDirectoryFinder.GetCodeFile("Language Explorer/Configuration/Main.xml");
				// This can be called from TE...in that case, we don't complain about missing include
				// files (true argument) but just trust that we put enough in the installer to make it work.
				XmlDocument configuration = XWindow.LoadConfigurationWithIncludes(configFile, true);
				XmlNode windowConfigurationNode = configuration.SelectSingleNode(LanguageExplorerConstants.window);
				propertyTable.SetProperty("WindowConfiguration", windowConfigurationNode, false, true);
			}
#endif
		}

		/// <summary>
		/// Assuming the selection can be expanded to a word and a corresponding LexEntry can
		/// be found, show the related words dialog with the words related to the selected one.
		/// </summary>
		/// <remarks>
		/// Currently only called from WCF (11/21/2013 - AP)
		/// </remarks>
		public static void DisplayRelatedEntries(LcmCache cache, IWin32Window owner,
			IVwStylesheet styleSheet, IHelpTopicProvider helpProvider, string helpFileKey, ITsString tssWf,
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
				int[] domains;
				int[] lexrels;
				IVwCacheDa cdaTemp;
				if (!RelatedWords.LoadDomainAndRelationInfo(cache, hvoEntry, out domains, out lexrels, out cdaTemp, owner))
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
		/// Assuming the selection can be expanded to a word and a corresponding LexEntry can
		/// be found, show the related words dialog with the words related to the selected one.
		/// </summary>
		/// <remarks>
		/// Currently only called from WCF (11/21/2013 - AP)
		/// </remarks>
		public static void DisplayRelatedEntries(LcmCache cache, IWin32Window owner,
			IPropertyTable propertyTable, IHelpTopicProvider helpProvider, string helpFileKey, ITsString tssWf,
			bool hideInsertButton)
		{
			DisplayRelatedEntries(cache, owner, FwUtils.StyleSheetFromPropertyTable(propertyTable), helpProvider, helpFileKey, tssWf, hideInsertButton);
		}

		/// <summary>
		/// Assuming the selection can be expanded to a word and a corresponding LexEntry can
		/// be found, show the related words dialog with the words related to the selected one.
		/// </summary>
		public static void DisplayRelatedEntries(LcmCache cache, IVwSelection sel, IWin32Window owner,
			IPropertyTable propertyTable, IHelpTopicProvider helpProvider, string helpFileKey)
		{
			var sel2 = sel?.EndPoint(false);
			var sel3 = sel2?.GrowToWord();
			if (sel3 == null)
			{
				return;
			}
			ITsString tss;
			int ichMin, ichLim, hvo, tag, ws;
			bool fAssocPrev;
			sel3.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvo, out tag, out ws);
			sel3.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag, out ws);
			if (tss.Text == null)
			{
				return;
			}
			DisplayRelatedEntries(cache, owner, FwUtils.StyleSheetFromPropertyTable(propertyTable), helpProvider, helpFileKey, tss.GetSubstring(ichMin, ichLim), false, sel);
		}

		/// <summary>
		/// Launch the Find Entry dialog, and if one is created or selected return it.
		/// </summary>
		/// <returns>The HVO of the selected or created entry</returns>
		internal static ILexEntry ShowFindEntryDialog(LcmCache cache, IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber, ITsString tssForm, IWin32Window owner)
		{
			using (var entryGoDlg = new EntryGoDlg())
			{
				entryGoDlg.InitializeFlexComponent(new FlexComponentParameters(propertyTable, publisher, subscriber));
				// Temporarily set TopMost to true so it will launch above any calling app (e.g. Paratext)
				// but reset after activated.
				SetCurrentModalForm(entryGoDlg);
				var wp = new WindowParams
				{
					m_btnText = LcmUiStrings.ksShow,
					m_title = LcmUiStrings.ksFindInDictionary,
					m_label = LcmUiStrings.ksFind_
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

		/// <summary />
		public void ShowSummaryDialog(IWin32Window owner, ITsString tssWf, IHelpTopicProvider helpProvider, string helpFileKey, IVwStylesheet styleSheet)
		{
			bool otherButtonClicked;
			using (var form = new SummaryDialogForm(this, helpProvider, helpFileKey, styleSheet))
			{
				form.ShowDialog(owner);
				if (form.ShouldLink)
				{
					form.LinkToLexicon();
				}
				otherButtonClicked = form.OtherButtonClicked;
			}
			if (otherButtonClicked)
			{
				var entry = ShowFindEntryDialog(MyCmObject.Cache, PropertyTable, Publisher, Subscriber, tssWf, owner);
				if (entry != null)
				{
					using (var leuiNew = new LexEntryUi(entry))
					{
						leuiNew.ShowSummaryDialog(owner, entry.HeadWord, helpProvider, helpFileKey, styleSheet);
					}
				}
				else
				{
					// redisplay the original entry (recursively)
					ShowSummaryDialog(owner, tssWf, helpProvider, helpFileKey, styleSheet);
				}
			}
		}

#if RANDYTODO
		protected override bool ShouldDisplayMenuForClass(int specifiedClsid,
			UIItemDisplayProperties display)
		{
			return LexEntryTags.kClassId == specifiedClsid || base.ShouldDisplayMenuForClass(specifiedClsid, display);
		}
#endif
	}
}
