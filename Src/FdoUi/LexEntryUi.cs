// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: LexEntryUi.cs
// Responsibility: ?
// Last reviewed: Steve Miller (FindEntryForWordform only)
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FdoUi.Dialogs;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.FieldWorks.LexText.Controls;
using XCore;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// User-interface behavior for LexEntries.
	/// </summary>
	public class LexEntryUi : CmObjectUi
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make one. The argument should really be a LexEntry.
		/// </summary>
		/// <param name="obj"></param>
		/// ------------------------------------------------------------------------------------
		public LexEntryUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is ILexEntry);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create default valued entry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal LexEntryUi()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override IVwViewConstructor VernVc
		{
			get
			{
				CheckDisposed();
				return new LexEntryVc(m_cache);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given an object id, a (string-valued) property ID, and a range of characters,
		/// return the LexEntry that is the best guess as to a useful LE to show to
		/// provide information about that wordform.
		///
		/// Note: the interface takes this form because eventually we may want to query to
		/// see whether the text has been analyzed and we have a known morpheme breakdown
		/// for this wordform. Otherwise, at present we could just pass the text.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoSrc"></param>
		/// <param name="tagSrc"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <returns>LexEntry or null.</returns>
		/// ------------------------------------------------------------------------------------
		public static LexEntryUi FindEntryForWordform(FdoCache cache, int hvoSrc, int tagSrc,
			int ichMin, int ichLim)
		{
			ITsString tssContext = cache.DomainDataByFlid.get_StringProp(hvoSrc, tagSrc);
			if (tssContext == null)
				return null;
			ITsString tssWf = tssContext.GetSubstring(ichMin, ichLim);
			return FindEntryForWordform(cache, tssWf);
		}

		/// <summary>
		/// Find the list of LexEntry objects which conceivably match the given wordform.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="tssWf"></param>
		/// <param name="wfa"></param>
		/// <returns></returns>
		public static List<ILexEntry> FindEntriesForWordformUI(FdoCache cache, ITsString tssWf, IWfiAnalysis wfa)
		{
			bool duplicates = false;
			List<ILexEntry> retval = cache.ServiceLocator.GetInstance<ILexEntryRepository>().FindEntriesForWordform(cache, tssWf, wfa, ref duplicates);

			if (duplicates)
			{
				MessageBox.Show(Form.ActiveForm,
					string.Format(FdoUiStrings.ksDuplicateWordformsMsg, tssWf.Text),
					FdoUiStrings.ksDuplicateWordformsCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			return retval;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find wordform given a cache and the string.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="tssWf"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static LexEntryUi FindEntryForWordform(FdoCache cache, ITsString tssWf)
		{
			ILexEntry matchingEntry = cache.ServiceLocator.GetInstance<ILexEntryRepository>().FindEntryForWordform(cache, tssWf);
			return matchingEntry == null ? null : new LexEntryUi(matchingEntry);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the mediator if it doesn't already exist.  Ensure that the string table in
		/// the mediator is loaded from the Flex string table.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="fRestoreStringTable">output flag that we should restore the original string table</param>
		/// <param name="stOrig">output is the original string table</param>
		/// ------------------------------------------------------------------------------------
		protected static Mediator EnsureValidMediator(Mediator mediator,
			out bool fRestoreStringTable, out StringTable stOrig)
		{
			if (mediator == null)
			{
				mediator = new Mediator();
				fRestoreStringTable = false;
				stOrig = null;
			}
			else
			{
				try
				{
					stOrig = mediator.StringTbl;
					// Check whether this is the Flex string table: look for a lexicon type
					// string and compare the value with what is produced when it's not found.
					string s = stOrig.GetString("MoCompoundRule-Plural", "AlternativeTitles");
					fRestoreStringTable = (s == "*MoCompoundRule-Plural*");
				}
				catch
				{
					stOrig = null;
					fRestoreStringTable = true;
				}
			}
			if (fRestoreStringTable || stOrig == null)
			{
				string dir = Path.Combine(FwDirectoryFinder.FlexFolder, "Configuration");
				mediator.StringTbl = new StringTable(dir);
			}
			return mediator;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoSrc"></param>
		/// <param name="tagSrc"></param>
		/// <param name="wsSrc"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="owner"></param>
		/// <param name="mediator"></param>
		/// <param name="helpProvider"></param>
		/// <param name="helpFileKey">string key to get the help file name</param>
		/// ------------------------------------------------------------------------------------
		public static void DisplayOrCreateEntry(FdoCache cache, int hvoSrc, int tagSrc, int wsSrc,
			int ichMin, int ichLim, IWin32Window owner, Mediator mediator,
			IHelpTopicProvider helpProvider, string helpFileKey)
		{
			ITsString tssContext = cache.DomainDataByFlid.get_StringProp(hvoSrc, tagSrc);
			if (tssContext == null)
				return;

			string text = tssContext.Text;
			// If the string is empty, it might be because it's multilingual.  Try that alternative.
			// (See TE-6374.)
			if (text == null && wsSrc != 0)
			{
				tssContext = cache.DomainDataByFlid.get_MultiStringAlt(hvoSrc, tagSrc, wsSrc);
				if (tssContext != null)
					text = tssContext.Text;
			}
			ITsString tssWf = null;
			if (text != null)
				tssWf = tssContext.GetSubstring(ichMin, ichLim);
			if (tssWf == null || tssWf.Length == 0)
				return;
			// We want to limit the lookup to the current word's current analysis, if one exists.
			// See FWR-956.
			IWfiAnalysis wfa = null;
			if (tagSrc == StTxtParaTags.kflidContents)
			{
				IAnalysis anal = null;
				IStTxtPara para = cache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(hvoSrc);
				foreach (ISegment seg in para.SegmentsOS)
				{
					if (seg.BeginOffset <= ichMin && seg.EndOffset >= ichLim)
					{
						bool exact;
						AnalysisOccurrence occurrence = seg.FindWagform(ichMin - seg.BeginOffset, ichLim - seg.BeginOffset, out exact);
						if (occurrence != null)
							anal = occurrence.Analysis;
						break;
					}
				}
				if (anal != null)
				{
					if (anal is IWfiAnalysis)
						wfa = anal as IWfiAnalysis;
					else if (anal is IWfiGloss)
						wfa = (anal as IWfiGloss).OwnerOfClass<IWfiAnalysis>();
				}
			}
			DisplayEntries(cache, owner, mediator, helpProvider, helpFileKey, tssWf, wfa);
		}

		internal static void DisplayEntry(FdoCache cache, IWin32Window owner, Mediator mediatorIn,
			IHelpTopicProvider helpProvider, string helpFileKey, ITsString tssWfIn)
		{
			ITsString tssWf = tssWfIn;
			LexEntryUi leui = null;
			Mediator mediator = null;
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
					int wsWf = TsStringUtils.GetWsAtOffset(tssWf, 0);
					//use that to get the locale for the WS, which is used for
					string wsLocale = cache.ServiceLocator.WritingSystemManager.Get(wsWf).IcuLocale;
					string sLower = Icu.ToLower(tssWf.Text, wsLocale);
					ITsTextProps ttp = tssWf.get_PropertiesAt(0);
					tssWf = cache.TsStrFactory.MakeStringWithPropsRgch(sLower, sLower.Length, ttp);
					leui = FindEntryForWordform(cache, tssWf);
				}

				// Ensure that we have a valid mediator with the proper string table.
				bool fRestore;
				StringTable stOrig;
				mediator = EnsureValidMediator(mediatorIn, out fRestore, out stOrig);
				FdoCache cache2 = (FdoCache)mediator.PropertyTable.GetValue("cache");
				if (cache2 != cache)
					mediator.PropertyTable.SetProperty("cache", cache);
				EnsureWindowConfiguration(mediator);
				IVwStylesheet styleSheet = GetStyleSheet(cache, mediator);
				if (leui == null)
				{
					ILexEntry entry = ShowFindEntryDialog(cache, mediator, tssWf, owner);
					if (entry == null)
					{
						// Restore the original string table in the mediator if needed.
						if (fRestore)
							mediator.StringTbl = stOrig;
						return;
					}
					leui = new LexEntryUi(entry);
				}
				if (mediator != null)
					leui.Mediator = mediator;
				leui.ShowSummaryDialog(owner, tssWf, helpProvider, helpFileKey, styleSheet);
				// Restore the original string table in the mediator if needed.
				if (fRestore)
					mediator.StringTbl = stOrig;
			}
			finally
			{
				if (leui != null)
					leui.Dispose();
				if (mediator != mediatorIn)
					mediator.Dispose();
			}
		}

		// Currently only called from WCF (11/21/2013 - AP)
		public static void DisplayEntry(FdoCache cache, Mediator mediatorIn,
			IHelpTopicProvider helpProvider, string helpFileKey, ITsString tssWfIn, IWfiAnalysis wfa)
		{
			DisplayEntries(cache, null, mediatorIn, helpProvider, helpFileKey, tssWfIn, wfa);
		}

		internal static void DisplayEntries(FdoCache cache, IWin32Window owner, Mediator mediatorIn,
			IHelpTopicProvider helpProvider, string helpFileKey, ITsString tssWfIn, IWfiAnalysis wfa)
		{
			ITsString tssWf = tssWfIn;
			var entries = FindEntriesForWordformUI(cache, tssWf, wfa);

			StringTable stOrig;
			Mediator mediator;
			IVwStylesheet styleSheet;
			bool fRestore = EnsureFlexTypeSetup(cache, mediatorIn, out stOrig, out mediator, out styleSheet);
			try
			{

				if (entries == null || entries.Count == 0)
				{
					ILexEntry entry = ShowFindEntryDialog(cache, mediator, tssWf, owner);
					if (entry == null)
						return;
					entries = new List<ILexEntry>(1);
					entries.Add(entry);
				}
				DisplayEntriesRecursive(cache, owner, mediator, styleSheet, helpProvider, helpFileKey, entries, tssWf);
			}
			finally
			{
				// Restore the original string table in the mediator if needed.
				if (fRestore)
					mediator.StringTbl = stOrig;
			}
		}

		private static void DisplayEntriesRecursive(FdoCache cache, IWin32Window owner,
			Mediator mediator, IVwStylesheet stylesheet,
			IHelpTopicProvider helpProvider, string helpFileKey,
			List<ILexEntry> entries, ITsString tssWf)
		{
			// Loop showing the SummaryDialogForm as long as the user clicks the Other button
			// in that dialog.
			bool otherButtonClicked = false;
			do
			{
				using (var sdform = new SummaryDialogForm(new List<int>(entries.Select(le => le.Hvo)), tssWf,
														helpProvider, helpFileKey,
														stylesheet, cache, mediator))
				{
					SetCurrentModalForm(sdform);
					if (owner == null)
						sdform.StartPosition = FormStartPosition.CenterScreen;
					sdform.ShowDialog(owner);
					if (sdform.ShouldLink)
						sdform.LinkToLexicon();
					otherButtonClicked = sdform.OtherButtonClicked;
				}
				if (otherButtonClicked)
				{
					// Look for another entry to display.  (If the user doesn't select another
					// entry, loop back and redisplay the current entry.)
					var entry = ShowFindEntryDialog(cache, mediator, tssWf, owner);
					if (entry != null)
					{
						// We need a list that contains the entry we found to display on the
						// next go around of this loop.
						entries = new List<ILexEntry>();
						entries.Add(entry);
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
		/// <param name="newActiveModalForm"></param>
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
		/// <param name="sender"></param>
		/// <param name="e"></param>
		static void s_activeModalForm_Activated(object sender, EventArgs e)
		{
			(sender as Form).TopMost = false;
		}

		private static bool EnsureFlexTypeSetup(FdoCache cache, Mediator mediatorIn, out StringTable stOrig, out Mediator mediator, out IVwStylesheet styleSheet)
		{
			// Ensure that we have a valid mediator with the proper string table.
			bool fRestore = false;
			stOrig = null;
			mediator = EnsureValidMediator(mediatorIn, out fRestore, out stOrig);
			FdoCache cache2 = (FdoCache)mediator.PropertyTable.GetValue("cache");
			if (cache2 != cache)
				mediator.PropertyTable.SetProperty("cache", cache);
			EnsureWindowConfiguration(mediator);
			styleSheet = GetStyleSheet(cache, mediator);
			return fRestore;
		}

		/// <summary>
		/// Determine a stylesheet from a mediator, or create a new one. Currently this is done
		/// by looking for the main window and seeing whether it has a StyleSheet property that
		/// returns one. (We use reflection because the relevant classes are in DLLs we can't
		/// reference.)
		/// </summary>
		/// <param name="mediator"></param>
		/// <returns></returns>
		private static IVwStylesheet StyleSheetFromMediator(Mediator mediator)
		{
			if (mediator == null)
				return null;
			Form mainWindow = mediator.PropertyTable.GetValue("window") as Form;
			if (mainWindow == null)
				return null;
			System.Reflection.PropertyInfo pi = mainWindow.GetType().GetProperty("StyleSheet");
			if (pi == null)
				return null;
			return pi.GetValue(mainWindow, null) as IVwStylesheet;
		}

		private static IVwStylesheet GetStyleSheet(FdoCache cache, Mediator mediator)
		{
			IVwStylesheet vss = StyleSheetFromMediator(mediator);
			if (vss != null)
				return vss;
			// Get a style sheet for the Language Explorer, and store it in the
			// (new) mediator.
			FwStyleSheet styleSheet = new FwStyleSheet();
			styleSheet.Init(cache, cache.LanguageProject.Hvo, LangProjectTags.kflidStyles);
			mediator.PropertyTable.SetProperty("FwStyleSheet", styleSheet);
			mediator.PropertyTable.SetPropertyPersistence("FwStyleSheet", false);
			return styleSheet;
		}

		private static void EnsureWindowConfiguration(Mediator mediator)
		{
			XmlNode xnWindow = (XmlNode)mediator.PropertyTable.GetValue("WindowConfiguration");
			if (xnWindow == null)
			{
				string configFile = FwDirectoryFinder.GetCodeFile("Language Explorer/Configuration/Main.xml");
				// This can be called from TE...in that case, we don't complain about missing include
				// files (true argument) but just trust that we put enough in the installer to make it work.
				XmlDocument configuration = XWindow.LoadConfigurationWithIncludes(configFile, true);
				XmlNode windowConfigurationNode = configuration.SelectSingleNode("window");
				mediator.PropertyTable.SetProperty("WindowConfiguration", windowConfigurationNode);
				mediator.PropertyTable.SetPropertyPersistence("WindowConfiguration", false);
			}
		}

		// Currently only called from WCF (11/21/2013 - AP)
		public static void DisplayRelatedEntries(FdoCache cache, Mediator mediatorIn,
			IHelpTopicProvider helpProvider, string helpFileKey, ITsString tss)
		{
			DisplayRelatedEntries(cache, null, mediatorIn, helpProvider, helpFileKey, tss, true);
		}

		/// ------------------------------------------------------------
		/// <summary>
		/// Assuming the selection can be expanded to a word and a corresponding LexEntry can
		/// be found, show the related words dialog with the words related to the selected one.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="owner">The owning window.</param>
		/// <param name="mediatorIn">The mediator.</param>
		/// <param name="helpProvider">The help provider.</param>
		/// <param name="helpFileKey">The help file key.</param>
		/// <param name="tssWf">The ITsString for the word form.</param>
		/// <param name="hideInsertButton"></param>
		/// ------------------------------------------------------------
		// Currently only called from WCF (11/21/2013 - AP)
		public static void DisplayRelatedEntries(FdoCache cache, IWin32Window owner,
			Mediator mediatorIn, IHelpTopicProvider helpProvider, string helpFileKey, ITsString tssWf,
			bool hideInsertButton)
		{
			if (tssWf == null || tssWf.Length == 0)
				return;

			using (LexEntryUi leui = FindEntryForWordform(cache, tssWf))
			{
				// This doesn't work as well (unless we do a commit) because it may not see current typing.
				//LexEntryUi leui = LexEntryUi.FindEntryForWordform(cache, hvo, tag, ichMin, ichLim);
				if (leui == null)
				{
					RelatedWords.ShowNotInDictMessage(owner);
					return;
				}
				int hvoEntry = leui.Object.Hvo;
				int[] domains;
				int[] lexrels;
				IVwCacheDa cdaTemp;
				if (!RelatedWords.LoadDomainAndRelationInfo(cache, hvoEntry, out domains, out lexrels, out cdaTemp, owner))
					return;
				StringTable stOrig;
				Mediator mediator;
				IVwStylesheet styleSheet;
				bool fRestore = EnsureFlexTypeSetup(cache, mediatorIn, out stOrig, out mediator, out styleSheet);
				using (RelatedWords rw = new RelatedWords(cache, null, hvoEntry, domains, lexrels, cdaTemp, styleSheet,
					mediatorIn, hideInsertButton))
				{
					rw.ShowDialog(owner);
				}
				if (fRestore)
					mediator.StringTbl = stOrig;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Assuming the selection can be expanded to a word and a corresponding LexEntry can
		/// be found, show the related words dialog with the words related to the selected one.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="sel">The sel.</param>
		/// <param name="owner">The owner.</param>
		/// <param name="mediatorIn"></param>
		/// <param name="helpProvider"></param>
		/// <param name="helpFileKey"></param>
		/// ------------------------------------------------------------------------------------
		public static void DisplayRelatedEntries(FdoCache cache, IVwSelection sel, IWin32Window owner,
			Mediator mediatorIn, IHelpTopicProvider helpProvider, string helpFileKey)
		{
			if (sel == null)
				return;
			IVwSelection sel2 = sel.EndPoint(false);
			if (sel2 == null)
				return;
			IVwSelection sel3 = sel2.GrowToWord();
			if (sel3 == null)
				return;
			ITsString tss;
			int ichMin, ichLim, hvo, tag, ws;
			bool fAssocPrev;
			sel3.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvo, out tag, out ws);
			sel3.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag, out ws);
			if (tss.Text == null)
				return;
			ITsString tssWf = tss.GetSubstring(ichMin, ichLim);
			using (LexEntryUi leui = FindEntryForWordform(cache, tssWf))
			{
				// This doesn't work as well (unless we do a commit) because it may not see current typing.
				//LexEntryUi leui = LexEntryUi.FindEntryForWordform(cache, hvo, tag, ichMin, ichLim);
				if (leui == null)
				{
					if (tssWf != null && tssWf.Length > 0)
						RelatedWords.ShowNotInDictMessage(owner);
					return;
				}
				int hvoEntry = leui.Object.Hvo;
				int[] domains;
				int[] lexrels;
				IVwCacheDa cdaTemp;
				if (!RelatedWords.LoadDomainAndRelationInfo(cache, hvoEntry, out domains, out lexrels, out cdaTemp, owner))
					return;
				StringTable stOrig;
				Mediator mediator;
				IVwStylesheet styleSheet;
				bool fRestore = EnsureFlexTypeSetup(cache, mediatorIn, out stOrig, out mediator, out styleSheet);
				using (RelatedWords rw = new RelatedWords(cache, sel3, hvoEntry, domains, lexrels, cdaTemp, styleSheet, mediatorIn, false))
				{
					rw.ShowDialog(owner);
				}
				if (fRestore)
					mediator.StringTbl = stOrig;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flex config file.
		/// </summary>
		/// <value>The flex config file.</value>
		/// ------------------------------------------------------------------------------------
		public static string FlexConfigFile
		{
			get { return FwDirectoryFinder.GetCodeFile(@"Language Explorer/Configuration/Main.xml"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Launch the Find Entry dialog, and if one is created or selected return it.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="mediator">The mediator.</param>
		/// <param name="tssForm">The TSS form.</param>
		/// <param name="owner">The owner.</param>
		/// <returns>The HVO of the selected or created entry</returns>
		/// ------------------------------------------------------------------------------------
		internal static ILexEntry ShowFindEntryDialog(FdoCache cache, Mediator mediator,
			ITsString tssForm, IWin32Window owner)
		{
			// Ensure that we have a valid mediator with the proper string table.
			bool fRestore = false;
			StringTable stOrig = null;
			mediator = EnsureValidMediator(mediator, out fRestore, out stOrig);
			try
			{
				using (EntryGoDlg entryGoDlg = new EntryGoDlg())
				{
					// Temporarily set TopMost to true so it will launch above any calling app (e.g. Paratext)
					// but reset after activated.
					SetCurrentModalForm(entryGoDlg);
					var wp = new WindowParams
								 {
									 m_btnText = FdoUiStrings.ksShow,
									 m_title = FdoUiStrings.ksFindInDictionary,
									 m_label = FdoUiStrings.ksFind_
								 };
					if (owner == null)
						entryGoDlg.StartPosition = FormStartPosition.CenterScreen;
					entryGoDlg.Owner = owner as Form;
					entryGoDlg.SetDlgInfo(cache, wp, mediator, tssForm);
					entryGoDlg.SetHelpTopic("khtpFindInDictionary");
					if (entryGoDlg.ShowDialog() == DialogResult.OK)
					{
						var entry = entryGoDlg.SelectedObject as ILexEntry;
						Debug.Assert(entry != null);
						return entry;
					}
				}
			}
			finally
			{
				// Restore the original string table in the mediator if needed.
				if (fRestore)
					mediator.StringTbl = stOrig;
			}
			return null;
		}

		/// <summary/>
		public void ShowSummaryDialog(IWin32Window owner, ITsString tssWf,
			IHelpTopicProvider helpProvider, string helpFileKey, IVwStylesheet styleSheet)
		{
			CheckDisposed();

			bool otherButtonClicked = false;
			using (SummaryDialogForm form =
				new SummaryDialogForm(this, tssWf, helpProvider, helpFileKey, styleSheet))
			{
				form.ShowDialog(owner);
				if (form.ShouldLink)
					form.LinkToLexicon();
				otherButtonClicked = form.OtherButtonClicked;
			}
			if (otherButtonClicked)
			{
				var entry = ShowFindEntryDialog(this.Object.Cache, this.Mediator, tssWf, owner);
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

		protected override bool ShouldDisplayMenuForClass(int specifiedClsid,
			UIItemDisplayProperties display)
		{
			return LexEntryTags.kClassId == specifiedClsid || base.ShouldDisplayMenuForClass(specifiedClsid, display);
		}

	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Override to support kfragHeadword with a properly live display of the headword.
	/// Also, the default of displaying the vernacular writing system can be overridden.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class LexEntryVc : CmVernObjectVc
	{
		const int kfragFormForm = 9543; // arbitrary.
		/// <summary>
		/// use with WfiMorphBundle to display the headword with variant info appended.
		/// </summary>
		public const int kfragEntryAndVariant = 9544;
		/// <summary>
		/// use with EntryRef to display the variant type info
		/// </summary>
		public const int kfragVariantTypes = 9545;

		int m_ws;
		int m_wsActual = 0;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		public LexEntryVc(FdoCache cache)
			: base(cache)
		{
			m_ws = cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
		}

		public int WritingSystemCode
		{
			get { return m_ws; }
			set { m_ws = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display a view of the LexEntry (or fragment thereof).
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case (int)VcFrags.kfragHeadWord:
					// This case should stay in sync with
					// LexEntry.LexemeFormMorphTypeAndHomographStatic
					vwenv.OpenParagraph();
					AddHeadwordWithHomograph(vwenv, hvo);
					vwenv.CloseParagraph();
					break;
				case kfragEntryAndVariant:
					var wfb = m_cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().GetObject(hvo);
					//int hvoMf = wfb.MorphRA.Hvo;
					//int hvoLexEntry = m_cache.GetOwnerOfObject(hvoMf);
					// if morphbundle morph (entry) is in a variant relationship to the morph bundle sense
					// display its entry headword and variant type information (LT-4053)
					ILexEntryRef ler;
					var variant = wfb.MorphRA.Owner as ILexEntry;
					if (variant.IsVariantOfSenseOrOwnerEntry(wfb.SenseRA, out ler))
					{
						// build Headword from sense's entry
						vwenv.OpenParagraph();
						vwenv.OpenInnerPile();
						vwenv.AddObj(wfb.SenseRA.EntryID, this, (int)VcFrags.kfragHeadWord);
						vwenv.CloseInnerPile();
						vwenv.OpenInnerPile();
						// now add variant type info
						vwenv.AddObj(ler.Hvo, this, kfragVariantTypes);
						vwenv.CloseInnerPile();
						vwenv.CloseParagraph();
						break;
					}

					// build Headword even though we aren't in a variant relationship.
					vwenv.AddObj(variant.Hvo, this, (int)VcFrags.kfragHeadWord);
					break;
				case kfragVariantTypes:
					ler = m_cache.ServiceLocator.GetInstance<ILexEntryRefRepository>().GetObject(hvo);
					bool fNeedInitialPlus = true;
					vwenv.OpenParagraph();
					foreach (var let in ler.VariantEntryTypesRS.Where(let => let.ClassID == LexEntryTypeTags.kClassId))
					{
						// just concatenate them together separated by comma.
						ITsString tssVariantTypeRevAbbr = let.ReverseAbbr.BestAnalysisAlternative;
						if (tssVariantTypeRevAbbr != null && tssVariantTypeRevAbbr.Length > 0)
						{
							if (fNeedInitialPlus)
								vwenv.AddString(TsStringUtils.MakeTss("+", m_cache.DefaultUserWs));
							else
								vwenv.AddString(TsStringUtils.MakeTss(",", m_cache.DefaultUserWs));
							vwenv.AddString(tssVariantTypeRevAbbr);
							fNeedInitialPlus = false;
						}
					}
					vwenv.CloseParagraph();
					break;
				case kfragFormForm: // form of MoForm
					vwenv.AddStringAltMember(MoFormTags.kflidForm, m_wsActual, this);
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

		private void AddHeadwordWithHomograph(IVwEnv vwenv, int hvo)
		{
					ISilDataAccess sda = vwenv.DataAccess;
					int hvoLf = sda.get_ObjectProp(hvo,
						LexEntryTags.kflidLexemeForm);
					int hvoType = 0;
					if (hvoLf != 0)
					{
						hvoType = sda.get_ObjectProp(hvoLf,
							MoFormTags.kflidMorphType);
					}

					// If we have a type of morpheme, show the appropriate prefix that indicates it.
					// We want vernacular so it will match the point size of any aligned vernacular text.
					// (The danger is that the vernacular font doesn't have these characters...not sure what
					// we can do about that, but most do, and it looks awful in analysis if that is a
					// much different size from vernacular.)
					string sPrefix = null;
					if (hvoType != 0)
					{
						sPrefix = sda.get_UnicodeProp(hvoType, MoMorphTypeTags.kflidPrefix);
					}

					// LexEntry.ShortName1; basically tries for form of the lexeme form, then the citation form.
					bool fGotLabel = false;
					int wsActual = 0;
					if (hvoLf != 0)
					{
						// if we have a lexeme form and its label is non-empty, use it.
						if (TryMultiStringAlt(sda, hvoLf, MoFormTags.kflidForm, out wsActual))
						{
							m_wsActual = wsActual;
							fGotLabel = true;
							if (sPrefix != null)
								vwenv.AddString(TsStringUtils.MakeTss(sPrefix, wsActual));
							vwenv.AddObjProp(LexEntryTags.kflidLexemeForm, this, kfragFormForm);
						}
					}
					if (!fGotLabel)
					{
						// If we didn't get a useful form from the lexeme form try the citation form.
						if (TryMultiStringAlt(sda, hvo, LexEntryTags.kflidCitationForm, out wsActual))
						{
							m_wsActual = wsActual;
							if (sPrefix != null)
								vwenv.AddString(TsStringUtils.MakeTss(sPrefix, wsActual));
							vwenv.AddStringAltMember(LexEntryTags.kflidCitationForm, wsActual, this);
							fGotLabel = true;
						}
					}
					int defUserWs = m_cache.WritingSystemFactory.UserWs;
					if (!fGotLabel)
					{
						// If that fails just show two questions marks.
						if (sPrefix != null)
							vwenv.AddString(TsStringUtils.MakeTss(sPrefix, wsActual));
						vwenv.AddString(m_cache.TsStrFactory.MakeString(FdoUiStrings.ksQuestions, defUserWs));	// was "??", not "???"
					}

					// If we have a lexeme form type show the appropriate postfix.
					if (hvoType != 0)
					{
						vwenv.AddString(TsStringUtils.MakeTss(
							sda.get_UnicodeProp(hvoType, MoMorphTypeTags.kflidPostfix), wsActual));
					}

					// Show homograph number if non-zero.
					int nHomograph = sda.get_IntProp(hvo,
						LexEntryTags.kflidHomographNumber);
					vwenv.NoteDependency(new[] { hvo }, new[] { LexEntryTags.kflidHomographNumber }, 1);
					if (nHomograph > 0)
					{
						// Use a string builder to embed the properties in with the TsString.
						// this allows our TsStringCollectorEnv to properly encode the superscript.
						// ideally, TsStringCollectorEnv could be made smarter to handle SetIntPropValues
						// since AppendTss treats the given Tss as atomic.
						ITsIncStrBldr tsBldr = TsIncStrBldrClass.Create();
						tsBldr.SetIntPropValues((int)FwTextPropType.ktptSuperscript,
							(int)FwTextPropVar.ktpvEnum,
							(int)FwSuperscriptVal.kssvSub);
						tsBldr.SetIntPropValues((int)FwTextPropType.ktptBold,
							(int)FwTextPropVar.ktpvEnum,
							(int)FwTextToggleVal.kttvForceOn);
						tsBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
							(int)FwTextPropVar.ktpvDefault, defUserWs);
						tsBldr.Append(nHomograph.ToString());
						vwenv.AddString(tsBldr.GetString());
					}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoEntryToDisplay"></param>
		/// <param name="wsVern"></param>
		/// <param name="ler"></param>
		/// <returns></returns>
		static public ITsString GetLexEntryTss(FdoCache cache, int hvoEntryToDisplay, int wsVern, ILexEntryRef ler)
		{
			LexEntryVc vcEntry = new LexEntryVc(cache);
			vcEntry.WritingSystemCode = wsVern;
			TsStringCollectorEnv collector = new TsStringCollectorEnv(null, cache.MainCacheAccessor, hvoEntryToDisplay);
			collector.RequestAppendSpaceForFirstWordInNewParagraph = false;
			vcEntry.Display(collector, hvoEntryToDisplay, (int)VcFrags.kfragHeadWord);
			if (ler != null)
				vcEntry.Display(collector, ler.Hvo, LexEntryVc.kfragVariantTypes);
			return collector.Result;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="morphBundle"></param>
		/// <param name="wsVern"></param>
		/// <returns></returns>
		static public ITsString GetLexEntryTss(IWfiMorphBundle morphBundle, int wsVern)
		{
			FdoCache cache = morphBundle.Cache;
			LexEntryVc vcEntry = new LexEntryVc(cache);
			vcEntry.WritingSystemCode = wsVern;
			TsStringCollectorEnv collector = new TsStringCollectorEnv(null, cache.MainCacheAccessor, morphBundle.Hvo);
			collector.RequestAppendSpaceForFirstWordInNewParagraph = false;
			vcEntry.Display(collector, morphBundle.Hvo, (int)LexEntryVc.kfragEntryAndVariant);
			return collector.Result;
		}

		private bool TryMultiStringAlt(ISilDataAccess sda, int hvo, int flid, out int wsActual)
		{
			ITsString tss = WritingSystemServices.GetMagicStringAlt(m_cache, m_ws, hvo, flid, true, out wsActual);
			return (tss != null);
		}
	}
}
