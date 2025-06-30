// Copyright (c) 2002-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Last reviewed: Steve Miller (FindEntryForWordform only)
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.FdoUi.Dialogs;
using SIL.FieldWorks.LexText.Controls;
using SIL.LCModel.DomainImpl;
using XCore;

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
		public static LexEntryUi FindEntryForWordform(LcmCache cache, int hvoSrc, int tagSrc,
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
		public static List<ILexEntry> FindEntriesForWordformUI(LcmCache cache, ITsString tssWf, IWfiAnalysis wfa)
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
		public static LexEntryUi FindEntryForWordform(LcmCache cache, ITsString tssWf)
		{
			ILexEntry matchingEntry = cache.ServiceLocator.GetInstance<ILexEntryRepository>().FindEntryForWordform(cache, tssWf);
			return matchingEntry == null ? null : new LexEntryUi(matchingEntry);
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
		/// <param name="propertyTable"></param>
		/// <param name="helpProvider"></param>
		/// <param name="helpFileKey">string key to get the help file name</param>
		/// ------------------------------------------------------------------------------------
		public static void DisplayOrCreateEntry(LcmCache cache, int hvoSrc, int tagSrc, int wsSrc,
			int ichMin, int ichLim, IWin32Window owner, Mediator mediator, PropertyTable propertyTable,
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
			DisplayEntries(cache, owner, mediator, propertyTable, helpProvider, helpFileKey, tssWf, wfa);
		}

		// Currently only called from WCF (11/21/2013 - AP)
		public static void DisplayEntry(LcmCache cache, Mediator mediatorIn, PropertyTable propertyTable,
			IHelpTopicProvider helpProvider, string helpFileKey, ITsString tssWfIn, IWfiAnalysis wfa)
		{
			DisplayEntries(cache, null, mediatorIn, propertyTable, helpProvider, helpFileKey, tssWfIn, wfa);
		}

		internal static void DisplayEntries(LcmCache cache, IWin32Window owner, Mediator mediator, PropertyTable propertyTable,
			IHelpTopicProvider helpProvider, string helpFileKey, ITsString tssWfIn, IWfiAnalysis wfa)
		{
			ITsString tssWf = tssWfIn;
			var entries = FindEntriesForWordformUI(cache, tssWf, wfa);

			IVwStylesheet styleSheet = GetStyleSheet(cache, propertyTable);
				if (entries == null || entries.Count == 0)
				{
				ILexEntry entry = ShowFindEntryDialog(cache, mediator, propertyTable, tssWf, owner);
					if (entry == null)
						return;
					entries = new List<ILexEntry>(1);
					entries.Add(entry);
				}
			DisplayEntriesRecursive(cache, owner, mediator, propertyTable, styleSheet, helpProvider, helpFileKey, entries, tssWf);
			}

		private static void DisplayEntriesRecursive(LcmCache cache, IWin32Window owner,
			Mediator mediator, PropertyTable propertyTable, IVwStylesheet stylesheet,
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
														stylesheet, cache, mediator, propertyTable))
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
					var entry = ShowFindEntryDialog(cache, mediator, propertyTable, tssWf, owner);
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

		/// <summary>
		/// Determine a stylesheet from a PropertyTable, or create a new one. Currently this is done
		/// by looking for the main window and seeing whether it has a StyleSheet property that
		/// returns one. (We use reflection because the relevant classes are in DLLs we can't
		/// reference.)
		/// </summary>
		/// <returns></returns>
		private static IVwStylesheet StyleSheetFromPropertyTable(PropertyTable propertyTable)
		{
			Form mainWindow = propertyTable.GetValue<Form>("window");
			if (mainWindow == null)
				return null;
			System.Reflection.PropertyInfo pi = mainWindow.GetType().GetProperty("StyleSheet");
			if (pi == null)
				return null;
			return pi.GetValue(mainWindow, null) as IVwStylesheet;
		}

		private static IVwStylesheet GetStyleSheet(LcmCache cache, PropertyTable propertyTable)
		{
			IVwStylesheet vss = StyleSheetFromPropertyTable(propertyTable);
			if (vss != null)
				return vss;
			// Get a style sheet for the Language Explorer, and store it in the
			// (new) mediator.
			LcmStyleSheet styleSheet = new LcmStyleSheet();
			styleSheet.Init(cache, cache.LanguageProject.Hvo, LangProjectTags.kflidStyles);
			propertyTable.SetProperty("LcmStyleSheet", styleSheet, true);
			propertyTable.SetPropertyPersistence("LcmStyleSheet", false);
			return styleSheet;
		}

		private static void EnsureWindowConfiguration(PropertyTable propertyTable)
		{
			XmlNode xnWindow = propertyTable.GetValue<XmlNode>("WindowConfiguration");
			if (xnWindow == null)
			{
				string configFile = FwDirectoryFinder.GetCodeFile("Language Explorer/Configuration/Main.xml");
				// This can be called from TE...in that case, we don't complain about missing include
				// files (true argument) but just trust that we put enough in the installer to make it work.
				XmlDocument configuration = XWindow.LoadConfigurationWithIncludes(configFile, true);
				XmlNode windowConfigurationNode = configuration.SelectSingleNode("window");
				propertyTable.SetProperty("WindowConfiguration", windowConfigurationNode, true);
				propertyTable.SetPropertyPersistence("WindowConfiguration", false);
			}
		}

		// Currently only called from WCF (11/21/2013 - AP)
		public static void DisplayRelatedEntries(LcmCache cache, Mediator mediatorIn, PropertyTable propertyTable,
			IHelpTopicProvider helpProvider, string helpFileKey, ITsString tss)
		{
			DisplayRelatedEntries(cache, null, mediatorIn, propertyTable, helpProvider, helpFileKey, tss, true);
		}

		/// ------------------------------------------------------------
		/// <summary>
		/// Assuming the selection can be expanded to a word and a corresponding LexEntry can
		/// be found, show the related words dialog with the words related to the selected one.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="owner">The owning window.</param>
		/// <param name="mediator">The mediator.</param>
		/// <param name="propertyTable"></param>
		/// <param name="helpProvider">The help provider.</param>
		/// <param name="helpFileKey">The help file key.</param>
		/// <param name="tssWf">The ITsString for the word form.</param>
		/// <param name="hideInsertButton"></param>
		/// ------------------------------------------------------------
		// Currently only called from WCF (11/21/2013 - AP)
		public static void DisplayRelatedEntries(LcmCache cache, IWin32Window owner,
			Mediator mediator, PropertyTable propertyTable, IHelpTopicProvider helpProvider, string helpFileKey, ITsString tssWf,
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
				IVwStylesheet styleSheet = GetStyleSheet(cache, propertyTable);
				using (RelatedWords rw = new RelatedWords(cache, null, hvoEntry, domains, lexrels, cdaTemp, styleSheet,
					mediator, hideInsertButton))
				{
					rw.ShowDialog(owner);
				}
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
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="helpProvider"></param>
		/// <param name="helpFileKey"></param>
		/// ------------------------------------------------------------------------------------
		public static void DisplayRelatedEntries(LcmCache cache, IVwSelection sel, IWin32Window owner,
			Mediator mediator, PropertyTable propertyTable, IHelpTopicProvider helpProvider, string helpFileKey)
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
				IVwStylesheet styleSheet = GetStyleSheet(cache, propertyTable);
				using (RelatedWords rw = new RelatedWords(cache, sel3, hvoEntry, domains, lexrels, cdaTemp, styleSheet, mediator, false))
				{
					rw.ShowDialog(owner);
				}
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
		/// <param name="propertyTable"></param>
		/// <param name="tssForm">The TSS form.</param>
		/// <param name="owner">The owner.</param>
		/// <returns>The HVO of the selected or created entry</returns>
		/// ------------------------------------------------------------------------------------
		internal static ILexEntry ShowFindEntryDialog(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			ITsString tssForm, IWin32Window owner)
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
				entryGoDlg.SetDlgInfo(cache, wp, mediator, propertyTable, tssForm);
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
				var entry = ShowFindEntryDialog(Object.Cache, Mediator, m_propertyTable, tssWf, owner);
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
		public LexEntryVc(LcmCache cache)
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
					var sense = wfb.SenseRA != null ? wfb.SenseRA : wfb.DefaultSense;
					if (variant.IsVariantOfSenseOrOwnerEntry(sense, out ler))
					{
						// build Headword from sense's entry
						vwenv.OpenParagraph();
						vwenv.OpenInnerPile();
						vwenv.AddObj(sense.EntryID, this, (int)VcFrags.kfragHeadWord);
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
								vwenv.AddString(TsStringUtils.MakeString("+", m_cache.DefaultUserWs));
							else
								vwenv.AddString(TsStringUtils.MakeString(",", m_cache.DefaultUserWs));
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

					// Show homograph number if non-zero.
					int defUserWs = m_cache.WritingSystemFactory.UserWs;
					int nHomograph = sda.get_IntProp(hvo, LexEntryTags.kflidHomographNumber);
					var hc = m_cache.ServiceLocator.GetInstance<HomographConfiguration>();
					//Insert HomographNumber when position is Before
					if (hc.HomographNumberBefore)
						InsertHomographNumber(vwenv, hc, nHomograph, defUserWs);

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
								vwenv.AddString(TsStringUtils.MakeString(sPrefix, wsActual));
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
								vwenv.AddString(TsStringUtils.MakeString(sPrefix, wsActual));
							vwenv.AddStringAltMember(LexEntryTags.kflidCitationForm, wsActual, this);
							fGotLabel = true;
						}
					}

					if (!fGotLabel)
					{
						// If that fails just show two questions marks.
						if (sPrefix != null)
							vwenv.AddString(TsStringUtils.MakeString(sPrefix, wsActual));
						vwenv.AddString(TsStringUtils.MakeString(FdoUiStrings.ksQuestions, defUserWs));	// was "??", not "???"
					}

					// If we have a lexeme form type show the appropriate postfix.
					if (hvoType != 0)
					{
						vwenv.AddString(TsStringUtils.MakeString(
							sda.get_UnicodeProp(hvoType, MoMorphTypeTags.kflidPostfix), wsActual));
					}

					vwenv.NoteDependency(new[] {hvo}, new[] {LexEntryTags.kflidHomographNumber}, 1);
					//Insert HomographNumber when position is After
					if (!hc.HomographNumberBefore)
						InsertHomographNumber(vwenv, hc, nHomograph, defUserWs);
		}

		/// <summary>
		/// Method to insert the homograph number with settings into the Text
		/// </summary>
		private void InsertHomographNumber(IVwEnv vwenv, HomographConfiguration hc, int nHomograph, int defUserWs)
		{
			if (nHomograph <= 0)
				return;

			// Use a string builder to embed the properties in with the TsString.
			// this allows our TsStringCollectorEnv to properly encode the superscript.
			// ideally, TsStringCollectorEnv could be made smarter to handle SetIntPropValues
			// since AppendTss treats the given Tss as atomic.
			ITsIncStrBldr tsBldr = TsStringUtils.MakeIncStrBldr();
			tsBldr.SetIntPropValues((int) FwTextPropType.ktptSuperscript,
				(int) FwTextPropVar.ktpvEnum,
				(int) FwSuperscriptVal.kssvSub);
			tsBldr.SetIntPropValues((int) FwTextPropType.ktptBold,
				(int) FwTextPropVar.ktpvEnum,
				(int) FwTextToggleVal.kttvForceOn);
			tsBldr.SetIntPropValues((int) FwTextPropType.ktptWs,
				(int) FwTextPropVar.ktpvDefault, defUserWs);
			StringServices.InsertHomographNumber(tsBldr, nHomograph, hc, HomographConfiguration.HeadwordVariant.Main, m_cache);
			vwenv.AddString(tsBldr.GetString());
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoEntryToDisplay"></param>
		/// <param name="wsVern"></param>
		/// <param name="ler"></param>
		/// <returns></returns>
		static public ITsString GetLexEntryTss(LcmCache cache, int hvoEntryToDisplay, int wsVern, ILexEntryRef ler)
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
			LcmCache cache = morphBundle.Cache;
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
