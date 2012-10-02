using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;

using XCore;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Drawing;
using SIL.FieldWorks.Resources;
using System.IO;
using SIL.FieldWorks.IText;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	public partial class RespellerDlg : Form
	{
		private Mediator m_mediator;
		private XmlNode m_configurationParameters;
		private FdoCache m_cache;
		private IWfiWordform m_srcwfiWordform;
		private int m_vernWs;
		private RecordClerk m_srcClerk;
		private RecordClerk m_dstClerk;
		private const string s_helpTopic = "khtpRespellerDlg";
		private string m_sMoreButtonText; // original text of More button
		private Size m_moreMinSize; // minimum size when 'more' options shown (=original size, minus a bit on height)
		private Size m_lessMinSize; // minimum size when 'less' options shown.

		// Preview related
		private bool m_previewOn;
		private XmlNode m_oldOccurrenceColumn; // original display of occurrences, when preview off
		// special preview display of occurrences, with string truncated after occurrence
		private XmlNode m_previewOccurrenceColumn;
		private string m_previewButtonText; // original text of the button (before changed to e.g. Clear).
		RespellUndoAction m_respellUndoaction; // created when we do a preview, used when check boxes change.

		Set<int> m_enabledItems = new Set<int>(); // items still enabled.
		// Flag set true if there are other known occurrences (real Twfics) so AllChanged should always return false.
		bool m_fOtherOccurrencesExist;

		string m_lblExplainText; // original text of m_lblExplainDisabled

		// Typically the clerk of the calling Words/Analysis view that manages a list of wordforms.
		// May be null when called from TE change spelling dialog.
		RecordClerk m_wfClerk;
		int m_hvoNewWordform; // if we made a new wordform and changed all instances, this gets set.

		public RespellerDlg()
		{
			InitializeComponent();
		}

		const int kflidParagraphs = (int)StText.StTextTags.kflidParagraphs;
		const int kflidContents = (int)StTxtPara.StTxtParaTags.kflidContents;

		/// <summary>
		/// Despite being public where the other SetDlgInfo is internal, this is a "special-case"
		/// initialization method not used in Flex, but in TE, where there is not a pre-existing mediator
		/// for each main window. This initializer obtains the configuration parameters and sets up a
		/// suitable mediator. In doing so it duplicates knowledge from various places:
		/// 1. Along with the code that initializes xWindow and various other places, it 'knows' where
		/// to find the configuration node for the respeller dialog.
		/// 2. It duplicates some of the logic in FwXWindow.InitMediatorValues and RestoreProperties,
		/// knowing the LocalSettingsId and how to use it to restore mediator properties, and what items
		/// must be in the mediator. Also logic in xWindow for restoring global settings.
		/// 3. It knows how to initialize a number of virtual properties normally created as part of
		/// FLEx's startup code.
		/// 4. It knows how to set up the string table that Flex uses.
		/// 5. It knows how to initialize the Flex part inventories.
		/// </summary>
		/// <returns></returns>
		public bool SetDlgInfo(IWfiWordform wf)
		{
			using (ProgressDialogWorkingOn dlg = new ProgressDialogWorkingOn())
			{
				if (FwApp.App != null)
					dlg.Owner = FwApp.App.ActiveMainWindow;
				else
					dlg.Owner = Form.ActiveForm;
				if (dlg.Owner != null) // I think it's only null when debugging? But play safe.
					dlg.Icon = dlg.Owner.Icon;
				dlg.Text = MEStrings.ksFindingOccurrences;
				dlg.WorkingOnText = MEStrings.ksSearchingOccurrences;
				dlg.ProgressLabel = MEStrings.ksProgress;
				dlg.Show(Form.ActiveForm);
				dlg.Update();
				dlg.BringToFront();
				MilestoneProgressState progressState = new MilestoneProgressState(dlg.ProgressDisplayer);
				try
				{
					//progressState.AddMilestone(1.0f);
					//progressState.AddMilestone(1.0f);
					//progressState.AddMilestone(1.0f);
					//progressState.AddMilestone(4.0f);
					//progressState.AddMilestone(2.0f);
					FdoCache cache = wf.Cache;
					m_srcwfiWordform = wf;
					// Get the parameter node.
					string path = Path.Combine(DirectoryFinder.GetFWCodeSubDirectory(@"Language Explorer\Configuration\Words"),
						"areaConfiguration.xml");
					XmlDocument doc = XmlUtils.LoadConfigurationWithIncludes(path, true);
					XmlNode paramNode = doc.DocumentElement.SelectSingleNode("listeners/listener[@class=\"SIL.FieldWorks.XWorks.MorphologyEditor.RespellerDlgListener\"]/parameters");
					Debug.Assert(paramNode != null);
					// Initialize a mediator.
					Mediator mediator = new Mediator();
					// Copied from FwXWindow.InitMediatorValues
					mediator.PropertyTable.LocalSettingsId = cache.DatabaseName;
					mediator.PropertyTable.SetProperty("cache", cache);
					mediator.PropertyTable.SetPropertyPersistence("cache", false);
					//// Enhance JohnT: possibly these three lines (also copied) are not needed.
					//mediator.PropertyTable.SetProperty("DocumentName", GetMainWindowCaption(cache));
					//mediator.PropertyTable.SetPropertyPersistence("DocumentName", false);
					mediator.PathVariables["{DISTFILES}"] = DirectoryFinder.FWCodeDirectory;
					mediator.PropertyTable.RestoreFromFile(mediator.PropertyTable.GlobalSettingsId);
					mediator.PropertyTable.RestoreFromFile(mediator.PropertyTable.LocalSettingsId);
					//progressState.SetMilestone();
					// Set this AFTER the restore! Otherwise it goes away!
					mediator.PropertyTable.SetProperty("window", dlg.Owner);
					mediator.PropertyTable.SetPropertyPersistence("window", false);

					string directoryContainingConfiguration = DirectoryFinder.GetFWCodeSubDirectory(@"Language Explorer\Configuration");
					StringTable table = new SIL.Utils.StringTable(directoryContainingConfiguration);
					mediator.StringTbl = table;
					//progressState.SetMilestone();

					EnsureVirtuals(cache);
					//progressState.SetMilestone();

					SIL.FieldWorks.Common.Controls.LayoutCache.InitializePartInventories(cache.DatabaseName);
					//progressState.SetMilestone();

					// Get all the scripture texts.
					// Review: should we include IText ones too?
					// Note that the ownership check is designed to exclude archived drafts.
					// The second half collects footnotes and the title of the book.
					string sql = "select st.id from StText_ st "
						+ "join ScrSection_ ss on st.Owner$ = ss.id "
						+ "join ScrBook_ sb on ss.Owner$ = sb.id "
						+ "join Scripture s on sb.Owner$ = s.id "
						+ "union "
						+ "select st.id from StText_ st "
						+ "join ScrBook_ sb on st.Owner$ = sb.id "
						+ "join Scripture s on sb.Owner$ = s.id";
					int[] texts = DbOps.ReadIntArrayFromCommand(cache, sql, null);
					//progressState.SetMilestone();

					// Build concordance info, including the occurrence list for our wordform.
					//		Enhance: possibly we could create the Wfics only for this wordform?
					try
					{
						cache.EnableBulkLoadingIfPossible(true);
						ParagraphParser.ConcordTexts(cache, texts, progressState);
					}
					finally
					{
						cache.EnableBulkLoadingIfPossible(false);
					}
					m_cache = cache;

					GetCaptionWfics(texts, wf);

					return SetDlgInfo1(mediator, paramNode);
				}
				finally
				{
					dlg.Close();
				}
			}
		}

		/// <summary>
		/// This version is used inside FLEx when the friendly tool is not active. So, we need to
		/// build the concordance, but on FLEx's list, and we can assume all the parts and layouts
		/// are loaded.
		/// </summary>
		/// <param name="wf"></param>
		/// <returns></returns>
		public bool SetDlgInfo2(IWfiWordform wf, Mediator mediator, XmlNode configurationParams)
		{
			using (ProgressDialogWorkingOn dlg = new ProgressDialogWorkingOn())
			{
				dlg.Owner = Form.ActiveForm;
				if (dlg.Owner != null) // I think it's only null when debugging? But play safe.
					dlg.Icon = dlg.Owner.Icon;
				dlg.Text = MEStrings.ksFindingOccurrences;
				dlg.WorkingOnText = MEStrings.ksSearchingOccurrences;
				dlg.ProgressLabel = MEStrings.ksProgress;
				dlg.Show(Form.ActiveForm);
				dlg.Update();
				dlg.BringToFront();
				MilestoneProgressState progressState = new MilestoneProgressState(dlg.ProgressDisplayer);
				try
				{
					//progressState.AddMilestone(1.0f);
					//progressState.AddMilestone(1.0f);
					//progressState.AddMilestone(1.0f);
					//progressState.AddMilestone(4.0f);
					//progressState.AddMilestone(2.0f);
					FdoCache cache = wf.Cache;
					m_srcwfiWordform = wf;

					ConcordanceWordsVirtualHandler handler = cache.VwCacheDaAccessor.GetVirtualHandlerId(
						WordformInventory.ConcordanceWordformsFlid(cache)) as ConcordanceWordsVirtualHandler;
					handler.Progress = progressState;
					// We don't use this value here, we just want it to get computed with our progress
					// state active.
					cache.GetVectorSize(cache.LangProject.WordformInventoryOA.Hvo, handler.Tag);
					handler.Progress = null;

					m_cache = cache;

					return SetDlgInfo1(mediator, configurationParams);
				}
				finally
				{
					dlg.Close();
				}
			}
		}

		/// <summary>
		/// Scan the texts for pictures with captions.
		/// </summary>
		/// <param name="texts"></param>
		private void GetCaptionWfics(int[] texts, IWfiWordform wf)
		{
			int flidCaptions = DummyVirtualHandler.InstallDummyHandler(m_cache.VwCacheDaAccessor,
				"WfiWordform", "OccurrencesInCaptions",
				(int)CellarModuleDefns.kcptReferenceSequence).Tag;
			List<int> occurrencesInCaptions = new List<int>();
			string wordform = wf.Form.VernacularDefaultWritingSystem;
			if (string.IsNullOrEmpty(wordform))
				return; // paranoia.
			Set<FwObjDataTypes> desiredType = new Set<FwObjDataTypes>(1);
			desiredType.Add(FwObjDataTypes.kodtGuidMoveableObjDisp);
			int hvoAnnType = CmAnnotationDefn.Twfic(m_cache).Hvo;
			IVwCacheDa cda = m_cache.VwCacheDaAccessor;
			foreach (int hvoText in texts)
			{
				int chvoPara = m_cache.GetVectorSize(hvoText, kflidParagraphs);
				for (int ipara = 0; ipara < chvoPara; ipara++)
				{
					int hvoPara = m_cache.GetVectorItem(hvoText, kflidParagraphs, ipara);
					ITsString tssContents = m_cache.GetTsStringProperty(hvoPara, kflidContents);
					int crun = tssContents.RunCount;
					for (int irun = 0; irun < crun; irun++)
					{
						// See if the run is a picture ORC
						TsRunInfo tri;
						FwObjDataTypes odt;
						ITsTextProps props;
						Guid guid = StringUtils.GetGuidFromRun(tssContents, irun, out odt, out tri, out props, desiredType);
						if (guid == Guid.Empty)
							continue;
						// See if its caption contains our wordform
						int hvoPicture = m_cache.GetIdFromGuid(guid);
						int clsid = m_cache.GetClassOfObject(hvoPicture);
						if (clsid != (int)CmPicture.kclsidCmPicture)
							continue; // bizarre, just for defensiveness.
						ITsString tssCaption = m_cache.GetMultiStringAlt(hvoPicture,
							(int)CmPicture.CmPictureTags.kflidCaption, m_cache.DefaultVernWs);
						WordMaker wordMaker = new WordMaker(tssCaption, m_cache.LanguageWritingSystemFactoryAccessor);
						for (; ; )
						{
							int ichMin;
							int ichLim;
							ITsString tssTxtWord = wordMaker.NextWord(out ichMin, out ichLim);
							if (tssTxtWord == null)
								break;
							if (tssTxtWord.Text != wordform)
								continue;
							int hvoAnn = CmBaseAnnotation.CreateDummyAnnotation(m_cache, hvoPicture,
								hvoAnnType, ichMin, ichLim, wf.Hvo);
							cda.CacheIntProp(hvoAnn, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidFlid,
								(int)CmPicture.CmPictureTags.kflidCaption);
							cda.CacheObjProp(hvoAnn, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidWritingSystem,
								m_cache.DefaultVernWs);
							occurrencesInCaptions.Add(hvoAnn);
						}
					}
				}
			}
			// Make the list the value of the occurrencesInCaptions property.
			cda.CacheVecProp(wf.Hvo, flidCaptions, occurrencesInCaptions.ToArray(), occurrencesInCaptions.Count);
		}

		/// <summary>
		/// Save the settings currently set in the mediator. Note that this is needed only
		/// when the dialog is initialized (typically from TE) using the FdoCache SetDlgInfo().
		/// </summary>
		public void SaveSettings()
		{
			m_mediator.PropertyTable.SaveGlobalSettings();
			m_mediator.PropertyTable.SaveLocalSettings();
		}

		/// <summary>
		/// Ensure that virtual properties required for the proper functioning of the dialog exist.
		/// </summary>
		/// <param name="cache"></param>
		internal void EnsureVirtuals(FdoCache cache)
		{
			// Create required virtual properties
			BaseVirtualHandler.InstallVirtuals(@"Language Explorer\Configuration\Main.xml",
				new string[] { "SIL.FieldWorks.FDO.", "SIL.FieldWorks.IText." }, cache, true);

			//XmlDocument doc = new XmlDocument();
			//// Subset of Flex virtuals required for parsing paragraphs etc.
			//doc.LoadXml(
			//"<virtuals>"
			//    + "<virtual modelclass=\"CmObject\" virtualfield=\"OwnerHVO\" destinationClass=\"CmObject\">"
			//    + "<dynamicloaderinfo assemblyPath=\"FDO.dll\" class=\"SIL.FieldWorks.FDO.FDOAtomicPropertyVirtualHandler\"/>"
			//    + "</virtual>"
			//    + "<virtual modelclass=\"StTxtPara\" virtualfield=\"Segments\">"
			//    + "<dynamicloaderinfo assemblyPath=\"ITextDll.dll\" class=\"SIL.FieldWorks.IText.ParagraphSegmentsVirtualHandler\"/>"
			//    + "</virtual>"
			//    + "<virtual modelclass=\"WfiWordform\" virtualfield=\"OccurrencesInTexts\" destinationClass=\"CmBaseAnnotation\">"
			//    + "<dynamicloaderinfo assemblyPath=\"ITextDll.dll\" class=\"SIL.FieldWorks.IText.OccurrencesInTextsVirtualHandler\"/>"
			//    + "</virtual>"
			//    + "<virtual modelclass=\"WfiWordform\" virtualfield=\"HumanApprovedAnalyses\" computeeverytime=\"true\">"
			//    + "<dynamicloaderinfo assemblyPath=\"FDO.dll\" class=\"SIL.FieldWorks.FDO.FDOSequencePropertyVirtualHandler\"/>"
			//    + "</virtual>"
			//    + "<virtual modelclass=\"WfiWordform\" virtualfield=\"HumanNoOpinionParses\" computeeverytime=\"true\" requiresRealParserGeneratedData=\"true\">"
			//    + "<dynamicloaderinfo assemblyPath=\"FDO.dll\" class=\"SIL.FieldWorks.FDO.FDOSequencePropertyVirtualHandler\"/>"
			//    + "</virtual>"
			//    + "<virtual modelclass=\"WfiWordform\" virtualfield=\"HumanDisapprovedParses\" computeeverytime=\"true\">"
			//    + "<dynamicloaderinfo assemblyPath=\"FDO.dll\" class=\"SIL.FieldWorks.FDO.FDOSequencePropertyVirtualHandler\"/>"
			//    + "</virtual>"
			//    + "<virtual modelclass=\"WfiWordform\" virtualfield=\"FullConcordanceCount\" depends=\"OccurrencesInTexts\" computeeverytime=\"true\">"
			//    + "<dynamicloaderinfo assemblyPath=\"FDO.dll\" class=\"SIL.FieldWorks.FDO.IntegerPropertyVirtualHandler\"/>"
			//    + "</virtual>"
			//    + "<virtual modelclass=\"WfiWordform\" virtualfield=\"UserCount\" bulkLoadMethod=\"LoadAllUserCounts\" computeeverytime=\"true\">"
			//    + "<dynamicloaderinfo assemblyPath=\"FDO.dll\" class=\"SIL.FieldWorks.FDO.IntegerPropertyVirtualHandler\"/>"
			//    + "</virtual>"
			//    + "<virtual modelclass=\"WfiWordform\" virtualfield=\"ParserCount\" bulkLoadMethod=\"LoadAllParserCounts\" computeeverytime=\"true\" requiresRealParserGeneratedData=\"true\">"
			//    + "<dynamicloaderinfo assemblyPath=\"FDO.dll\" class=\"SIL.FieldWorks.FDO.IntegerPropertyVirtualHandler\"/>"
			//    + "</virtual>"
			//    + "<virtual modelclass=\"WfiWordform\" virtualfield=\"ConflictCount\" computeeverytime=\"true\" requiresRealParserGeneratedData=\"true\">"
			//    + "<dynamicloaderinfo assemblyPath=\"FDO.dll\" class=\"SIL.FieldWorks.FDO.IntegerPropertyVirtualHandler\"/>"
			//    + "</virtual>"
			//    + "<virtual modelclass=\"CmBaseAnnotation\" virtualfield=\"Reference\">"
			//    + "<dynamicloaderinfo assemblyPath=\"ITextDll.dll\" class=\"SIL.FieldWorks.IText.AnnotationRefHandler\"/>"
			//    + "</virtual>"
			//    + "<virtual modelclass=\"StText\" virtualfield=\"Title\" ws=\"$ws=best vernoranal\" depends=\"OwnerHVO.Name\" computeeverytime=\"true\">"
			//    + "<dynamicloaderinfo assemblyPath=\"ITextDll.dll\" class=\"SIL.FieldWorks.IText.StTextTitleVirtualHandler\"/>"
			//    + "</virtual>"
			//    + "<virtual modelclass=\"LangProject\" virtualfield=\"InterlinearTexts\" depends=\"Texts.Contents,Texts\" destinationClass=\"StText\" propertyTableKey=\"ITexts-ScriptureIds\">"
			//    + "<dynamicloaderinfo assemblyPath=\"ITextDll.dll\" class=\"SIL.FieldWorks.IText.InterlinearTextsVirtualHandler\"/>"
			//    + "</virtual>"
			//    + "<virtual modelclass=\"WordformInventory\" virtualfield=\"ConcordanceWords\" destinationClass=\"WfiWordform\" "
			//    + "depends=\"OwnerHVO.InterlinearTexts,Wordforms,OwnerHVO.InterlinearTexts.Paragraphs,OwnerHVO.InterlinearTexts.Paragraphs.Contents\">"
			//    + "<dynamicloaderinfo assemblyPath=\"ITextDll.dll\" class=\"SIL.FieldWorks.IText.ConcordanceWordsVirtualHandler\"/>"
			//    + "</virtual>"
			//+ "</virtuals>");
			//BaseVirtualHandler.InstallVirtuals(doc.DocumentElement, cache);
		}

		internal bool SetDlgInfo(Mediator mediator, XmlNode configurationParameters)
		{
			m_wfClerk = (RecordClerk)mediator.PropertyTable.GetValue("RecordClerk-concordanceWords");
			m_wfClerk.SuppressSaveOnChangeRecord = true; // various things trigger change record and would prevent Undo
			m_srcwfiWordform = (IWfiWordform)m_wfClerk.CurrentObject;
			return SetDlgInfo1(mediator, configurationParameters);
		}

		private bool SetDlgInfo1(Mediator mediator, XmlNode configurationParameters)
		{
			using (new WaitCursor(this))
			{
				m_mediator = mediator;
				m_configurationParameters = configurationParameters;

				m_btnRefresh.Image = SIL.FieldWorks.Resources.ResourceHelper.RefreshIcon;

				m_rbDiscardAnalyses.Checked = m_mediator.PropertyTable.GetBoolProperty("RemoveAnalyses", true);
				m_rbKeepAnalyses.Checked = !m_rbDiscardAnalyses.Checked;
				m_rbDiscardAnalyses.Click += new EventHandler(m_rbDiscardAnalyses_Click);
				m_rbKeepAnalyses.Click += new EventHandler(m_rbDiscardAnalyses_Click);

				m_cbUpdateLexicon.Checked = m_mediator.PropertyTable.GetBoolProperty("UpdateLexiconIfPossible", true);
				m_cbCopyAnalyses.Checked = m_mediator.PropertyTable.GetBoolProperty("CopyAnalysesToNewSpelling", true);
				m_cbCopyAnalyses.Click += new EventHandler(m_cbCopyAnalyses_Click);
				m_cbMaintainCase.Checked = m_mediator.PropertyTable.GetBoolProperty("MaintainCaseOnChangeSpelling", true);
				m_cbMaintainCase.Click += new EventHandler(m_cbMaintainCase_Click);
				m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
				m_cbNewSpelling.TextBox.MaxLength = m_cache.MaxFieldLength((int) WfiWordform.WfiWordformTags.kflidForm);
				if (m_cache.IsDummyObject(m_srcwfiWordform.Hvo))
				{
					m_srcwfiWordform = (IWfiWordform)(m_cache.LangProject.WordformInventoryOA as WordformInventory).ConvertDummyToReal(WordformInventory.ConcordanceWordformsFlid(m_cache), m_srcwfiWordform.Hvo);
					// wfClerk now has the defunct dummy wf, so it needs to be updated to include the new real wf.
					if (m_wfClerk != null)
						m_wfClerk.UpdateList(true);
				}

				// We need to use the 'best vern' ws,
				// since that is what is showing in the Words-Analyses detail edit control.
				// Access to this respeller dlg is currently (Jan. 2008) only via a context menu in the detail edit pane.
				// The user may be showing multiple wordform WSes in the left hand browse view,
				// but we have no way of knowing if the user thinks one of those alternatives is wrong without asking.
				m_vernWs = m_cache.LangProject.ActualWs(
					LangProject.kwsFirstVern,
					m_srcwfiWordform.Hvo,
					(int)WfiWordform.WfiWordformTags.kflidForm);
				// Bail out if no vernacular writing system was found (see LT-8892).
				Debug.Assert(m_vernWs != 0);
				if (m_vernWs == 0)
					return false;
				// Bail out, rather than run into a null reference exception.
				// (Should fix LT-7666.)
				if (m_srcwfiWordform.Form.GetAlternativeTss(m_vernWs) == null || m_srcwfiWordform.Form.GetAlternativeTss(m_vernWs).Length == 0)
					return false;

				m_cbNewSpelling.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
				m_cbNewSpelling.WritingSystemCode = m_vernWs;
				m_cbNewSpelling.StyleSheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
				Debug.Assert(m_cbNewSpelling.StyleSheet != null); // if it is we get a HUGE default font (and can't get the correct size)
				if (m_cbNewSpelling.WritingSystemFactory.get_EngineOrNull(m_vernWs).RightToLeft)
				{
					m_cbNewSpelling.RightToLeft = RightToLeft.Yes;
				}
				m_cbNewSpelling.Tss = m_srcwfiWordform.Form.GetAlternativeTss(m_vernWs);
				m_cbNewSpelling.AdjustForStyleSheet(this, null, m_cbNewSpelling.StyleSheet);
				if (!Application.RenderWithVisualStyles)
					m_cbNewSpelling.Padding = new Padding(1, 2, 1, 1);

				SetSuggestions();

				m_btnApply.Enabled = false;
				m_cbNewSpelling.TextChanged += new EventHandler(m_dstWordform_TextChanged);

				// Setup source browse view.
				XmlNode toolNode = configurationParameters.SelectSingleNode("controls/control[@id='srcSentences']/parameters");
				m_srcClerk = RecordClerkFactory.CreateClerk(m_mediator, toolNode);
				m_srcClerk.OwningObject = m_srcwfiWordform;
				m_sourceSentences.Init(m_mediator, toolNode);

				m_sourceSentences.CheckBoxChanged += new CheckBoxChangedEventHandler(sentences_CheckBoxChanged);

				m_moreMinSize = Size;
				m_moreMinSize.Height -= m_sourceSentences.Height / 2;
				m_lessMinSize = m_moreMinSize;
				m_lessMinSize.Height -= m_optionsPanel.Height;
				AdjustHeightAndMinSize(Height - m_optionsPanel.Height, m_lessMinSize);
				m_optionsPanel.Visible = false;
				m_btnMore.Image = ResourceHelper.MoreButtonDoubleArrowIcon;
				m_btnMore.Click += new EventHandler(btnMore_Click);
				m_sMoreButtonText = m_btnMore.Text;

				m_optionsPanel.Paint += new PaintEventHandler(m_optionsPanel_Paint);
				m_btnPreviewClear.Click += new EventHandler(m_btnPreviewClear_Click);

				// no good...code in MakeRoot of XmlBrowseView happens later and overrides. Control with
				// selectionType attr in Xml configuration.
				//m_sourceSentences.BrowseViewer.SelectedRowHighlighting = XmlBrowseViewBase.SelectionHighlighting.none;

				m_lblExplainText = m_lblExplainDisabled.Text;
				// We only reload the list when refresh is pressed.
				m_srcClerk.ListLoadingSuppressed = true;
				// We initially check everything.
				m_enabledItems.AddRange(m_sourceSentences.BrowseViewer.AllItems);
				foreach (int hvoCba in m_enabledItems)
				{
					m_cache.VwCacheDaAccessor.CacheIntProp(hvoCba, m_sourceSentences.BrowseViewer.PreviewEnabledTag, 1);
					m_cache.VwCacheDaAccessor.CacheIntProp(hvoCba, XmlBrowseViewBaseVc.ktagItemSelected, 1);
				}
				CheckForOtherOccurrences();
				SetEnabledState();
			}
			return true;
		}

		void CheckForOtherOccurrences()
		{
			string qry = string.Format(
				"SELECT cbawf.Id " +
				"FROM CmBaseAnnotation_ cbawf " +
				"WHERE cbawf.InstanceOf = {0} AND cbawf.BeginObject is not null " +
				"UNION " +
				"SELECT cbaAnal.Id " +
				"FROM CmBaseAnnotation_ cbaAnal " +
				"JOIN WfiAnalysis_ anal ON anal.Owner$ = {0}" +
				"WHERE cbaAnal.InstanceOf = anal.Id AND cbaAnal.BeginObject is not null " +
				"UNION " +
				"SELECT cbaGloss.Id " +
				"FROM CmBaseAnnotation_ cbaGloss " +
				"JOIN WfiAnalysis_ anal ON anal.Owner$ = {0}" +
				"JOIN WfiGloss_ gloss ON gloss.Owner$ = anal.Id " +
				"WHERE cbaGloss.InstanceOf = gloss.Id AND cbaGloss.BeginObject is not null ", m_srcwfiWordform.Hvo.ToString());
			Set<int> realOccurrences = new Set<int>(DbOps.ReadIntArrayFromCommand(m_cache, qry, null));
			// There are 'other' occurrences if some of the real ones aren't in the displayed list.
			m_fOtherOccurrencesExist = realOccurrences.Intersection(m_enabledItems).Count != realOccurrences.Count;
		}

		void m_btnPreviewClear_Click(object sender, EventArgs e)
		{
			if (m_previewOn)
			{
				m_previewOn = false;
				m_sourceSentences.BrowseViewer.PreviewColumn = -1;
				m_sourceSentences.BrowseViewer.ReplaceColumn("Occurrence", m_oldOccurrenceColumn);
				m_btnPreviewClear.Text = m_previewButtonText;
				m_respellUndoaction = null;
			}
			else
			{
				m_previewOn = true;
				int previewTag = m_sourceSentences.BrowseViewer.PreviewValuesTag;
				// Create dummy virtual properties as needed by the SpellingPreview Occurrences column
				int adjustedBeginTag = DummyVirtualHandler.InstallDummyHandler(m_cache.VwCacheDaAccessor,
					"CmBaseAnnotation", "AdjustedBeginOffset", (int)CellarModuleDefns.kcptInteger).Tag;
				int adjustedEndTag = DummyVirtualHandler.InstallDummyHandler(m_cache.VwCacheDaAccessor,
					"CmBaseAnnotation", "AdjustedEndOffset", (int)CellarModuleDefns.kcptInteger).Tag;
				int spellingPreviewTag = DummyVirtualHandler.InstallDummyHandler(m_cache.VwCacheDaAccessor,
					"CmBaseAnnotation", "SpellingPreview", (int)CellarModuleDefns.kcptString).Tag;
				// Initialize PrecedingContext etc for each occurrence (text up to and including old spelling)
				MakeUndoAction();
				m_respellUndoaction.SetupPreviews(spellingPreviewTag, previewTag, adjustedBeginTag, adjustedEndTag,
					m_sourceSentences.BrowseViewer.PreviewEnabledTag,
					m_sourceSentences.BrowseViewer.AllItems);
				// Create m_previewOccurrenceColumn if needed
				EnsurePreviewColumn();
				m_oldOccurrenceColumn = m_sourceSentences.BrowseViewer.ReplaceColumn("Occurrence", m_previewOccurrenceColumn);
				m_previewButtonText = m_btnPreviewClear.Text;
				m_btnPreviewClear.Text = MEStrings.ksClear;
			}
		}

		private void MakeUndoAction()
		{
			m_respellUndoaction = new RespellUndoAction(m_cache, m_vernWs, m_srcwfiWordform.Form.GetAlternativeTss(m_vernWs).Text,
				m_cbNewSpelling.Text);
			m_respellUndoaction.PreserveCase = m_cbMaintainCase.Checked;
			int tagEnabled = m_sourceSentences.BrowseViewer.PreviewEnabledTag;
			foreach (int hvo in m_sourceSentences.BrowseViewer.CheckedItems)
			{
				if (m_cache.GetIntProperty(hvo, tagEnabled) == 1)
					m_respellUndoaction.AddOccurrence(hvo);
			}
		}

		// Create the preview column if we haven't already.
		private void EnsurePreviewColumn()
		{
			if (m_previewOccurrenceColumn != null)
				return;
			XmlDocument doc = new XmlDocument();
			bool fRtl = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(m_cache.DefaultVernWs).RightToLeft;
			string insert = "";
			if (fRtl)
				insert = "<righttoleft value=\"on\"/>";
			doc.LoadXml(
				"<column label=\"Occurrence\" width=\"415000\" multipara=\"true\" doNotPersist=\"true\">"
				+ "<concpara min=\"AdjustedBeginOffset\" lim=\"AdjustedEndOffset\" align=\"144000\">"
					+ "<properties><editable value=\"false\"/>" + insert + "</properties>"
					+ "<string class=\"CmBaseAnnotation\" field=\"SpellingPreview\"/>"
					+ "<preview ws=\"vernacular\"/>"
				+ "</concpara>"
			+ "</column>");
			m_previewOccurrenceColumn = doc.DocumentElement;
		}

		private void SetSuggestions()
		{
			Enchant.Dictionary dict = EnchantHelper.GetDictionary(m_vernWs, m_cache.LanguageWritingSystemFactoryAccessor);
			if (dict == null)
				return;
			ICollection<string> suggestions = dict.Suggest(m_cbNewSpelling.Text);
			foreach (string suggestion in suggestions)
			{
				m_cbNewSpelling.Items.Add(suggestion);
			}
		}

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			m_cbNewSpelling.FocusAndSelectText();
		}

		/// <summary>
		/// Adjust our height (but, since we're changing the height to hide a panel at the bottom,
		/// do NOT move controls docked bottom).
		/// </summary>
		/// <param name="newVal"></param>
		void AdjustHeightAndMinSize(int newHeight, Size newMinSize)
		{
			List<Control> bottomControls = new List<Control>();
			this.SuspendLayout();
			foreach (Control c in Controls)
			{
				if (((int)c.Anchor & (int)AnchorStyles.Bottom) != 0)
				{
					bottomControls.Add(c);
					c.Anchor = (AnchorStyles)((int)c.Anchor & ~((int)AnchorStyles.Bottom));
				}
			}
			if (newHeight < Height)
			{
				// Adjust minsize first, lest new height is less than old minimum.
				this.MinimumSize = newMinSize;
				this.Height = newHeight;
			}
			else // increasing height
			{
				// Adjust height first, lest old height is less than new minimum.
				this.Height = newHeight;
				this.MinimumSize = newMinSize;
			}
			PerformLayout();
			foreach (Control c in bottomControls)
			{
				c.Anchor = (AnchorStyles)((int)c.Anchor | ((int)AnchorStyles.Bottom));
			}
			this.ResumeLayout();
		}

		void sentences_CheckBoxChanged(object sender, CheckBoxChangedEventArgs e)
		{
			if (m_respellUndoaction != null)
			{
				// We're displaying a Preview, and need to update things.
				if (e.HvosChanged.Length == 1)
				{
					// Single check box clicked, update with PropChanged
					int hvo = e.HvosChanged[0];
					// We only consider it 'checked' if both checked AND enabled.
					bool itemChecked = m_sourceSentences.BrowseViewer.IsItemChecked(hvo)
						&& m_cache.GetIntProperty(hvo, m_sourceSentences.BrowseViewer.PreviewEnabledTag) == 1;
					if (m_respellUndoaction != null)
						m_respellUndoaction.UpdatePreview(hvo, itemChecked);
				}
				else
				{
					m_respellUndoaction.UpdatePreviews();
				}
			}
			SetEnabledState();
		}

		private bool WordformHasMonomorphemicAnalyses
		{
			get
			{
				foreach (IWfiAnalysis analysis in m_srcwfiWordform.AnalysesOC)
					if (analysis.MorphBundlesOS.Count == 1)
						return true;
				return false;
			}
		}
		private bool WordformHasMultimorphemicAnalyses
		{
			get
			{
				foreach (IWfiAnalysis analysis in m_srcwfiWordform.AnalysesOC)
					if (analysis.MorphBundlesOS.Count > 1)
						return true;
				return false;
			}
		}

		private void SetEnabledState()
		{
			bool enabledBasic = (m_cbNewSpelling.Text != null && m_cbNewSpelling.Text.Length > 0
				&& m_cbNewSpelling.Text != m_srcwfiWordform.Form.GetAlternative(m_vernWs));

			bool someWillChange;
			bool changeAll = AllWillChange(out someWillChange);
			m_btnApply.Enabled = enabledBasic && someWillChange;
			m_btnPreviewClear.Enabled = enabledBasic && someWillChange; // todo: also if 'clear' needed.
			m_cbUpdateLexicon.Enabled = changeAll && WordformHasMonomorphemicAnalyses;
			m_rbDiscardAnalyses.Enabled = m_rbKeepAnalyses.Enabled = changeAll && WordformHasMultimorphemicAnalyses;
			m_cbCopyAnalyses.Enabled = someWillChange && !changeAll;
			if (changeAll)
				m_lblExplainDisabled.Text = "";
			else if (m_fOtherOccurrencesExist)
				m_lblExplainDisabled.Text = MEStrings.ksExplainDisabledScripture;
			else
				m_lblExplainDisabled.Text = m_lblExplainText; // original message says not all checked.
		}

		private void FirePropChanged(int hvo)
		{
			int vhFlid = BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "WfiWordform", "OccurrencesInTexts");
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			sda.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll,
				hvo,
				vhFlid,
				0, // These three numbers are ignored in PropChanged code, as it just reloads it all.
				0,
				1);
		}

		#region Event handlers

		private void m_cbUpdateLexicon_Click(object sender, EventArgs e)
		{
			m_mediator.PropertyTable.SetProperty("UpdateLexiconIfPossible", m_cbUpdateLexicon.Checked, false);
			m_mediator.PropertyTable.SetPropertyPersistence("UpdateLexiconIfPossible", true);
		}

		void m_rbDiscardAnalyses_Click(object sender, EventArgs e)
		{
			m_mediator.PropertyTable.SetProperty("RemoveAnalyses", m_rbDiscardAnalyses.Checked, false);
			m_mediator.PropertyTable.SetPropertyPersistence("RemoveAnalyses", true);
		}

		void m_cbCopyAnalyses_Click(object sender, EventArgs e)
		{
			m_mediator.PropertyTable.SetProperty("CopyAnalysesToNewSpelling", m_cbCopyAnalyses.Checked, false);
			m_mediator.PropertyTable.SetPropertyPersistence("CopyAnalysesToNewSpelling", true);
		}

		void m_cbMaintainCase_Click(object sender, EventArgs e)
		{
			m_mediator.PropertyTable.SetProperty("MaintainCaseOnChangeSpelling", m_cbMaintainCase.Checked, false);
			m_mediator.PropertyTable.SetPropertyPersistence("MaintainCaseOnChangeSpelling", true);
		}

		protected override void OnClosed(EventArgs e)
		{
			// Any way we get closed we want to be sure this gets restored.
			if (m_wfClerk != null)
			{
				m_wfClerk.SuppressSaveOnChangeRecord = false;
				if (m_hvoNewWordform != 0)
				{
					// Move the clerk to the new word if possible.
					m_wfClerk.JumpToRecord(m_hvoNewWordform);
				}
			}
			base.OnClosed(e);
		}

		private void m_btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, "FLExHelpFile", s_helpTopic);
		}

		private void m_dstWordform_TextChanged(object sender, EventArgs e)
		{
			SetEnabledState();
		}

		private void m_tcSpelling_Selected(object sender, TabControlEventArgs e)
		{
			SetEnabledState();
		}

		private void m_btnApply_Click(object sender, EventArgs e)
		{
			if (m_sourceSentences.CheckedItems.Count > 0)
			{
				using (new WaitCursor(this))
				{
					// NB: twfic may ref. wf, anal, or gloss.
					// NB: Need to support selective spelling change.
					//	(gunaa->gwnaa when it means woman, but not for gu-naa, IMP+naa)
					if (m_respellUndoaction == null)
						MakeUndoAction();
					bool someWillChange;
					if (AllWillChange(out someWillChange))
					{
						// No point in letting the user think they can make more changes.
						m_cbNewSpelling.Enabled = false;

						m_respellUndoaction.AllChanged = true;
						m_respellUndoaction.KeepAnalyses = !m_rbDiscardAnalyses.Checked;
						m_respellUndoaction.UpdateLexicalEntries = m_cbUpdateLexicon.Checked;
					}
					else
					{
						m_respellUndoaction.CopyAnalyses = m_cbCopyAnalyses.Checked;
					}

					m_respellUndoaction.DoIt();

					// Reloads the conc. virtual prop for the old wordform,
					// so the old values are removed.
					// This will allow .CanDelete' to return true.
					// Otherwise, it won't be deletable,
					// as it will still have those twfics pointing at it.
					//FirePropChanged(m_srcwfiWordform.Hvo);
					//UpdateDisplay();
					m_respellUndoaction.RemoveChangedItems(m_enabledItems, m_sourceSentences.BrowseViewer.PreviewEnabledTag);
					// If everything changed remember the new wordform.
					if (m_respellUndoaction.AllChanged)
						m_hvoNewWordform = m_respellUndoaction.NewWordform;
					if (m_previewOn)
						EnsurePreviewOff(); // will reconstruct
					else
					{
						m_respellUndoaction = null; // Make sure we use a new one for any future previews or Apply's.
						m_sourceSentences.BrowseViewer.ReconstructView(); // ensure new offsets used.
					}
					SetEnabledState();
				}
			}
		}

		/// <summary>
		/// Return true if every remaining item will change. True if all enabled items are also checked, and there
		/// are no known items that aren't listed at all (e.g., Scripture not currently included).
		/// </summary>
		/// <param name="someWillChange">Set true if some item will change, that is, some checked item is enabled</param>
		/// <returns></returns>
		private bool AllWillChange(out bool someWillChange)
		{
			Set<int> checkedItems = new Set<int>(m_sourceSentences.CheckedItems);
			int changeCount = checkedItems.Intersection(m_enabledItems).Count;
			someWillChange = changeCount > 0;
			return changeCount == m_enabledItems.Count && !m_fOtherOccurrencesExist;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show more options.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void btnMore_Click(object sender, System.EventArgs e)
		{
			m_btnMore.Text = MEStrings.ksLess;
			m_btnMore.Image = ResourceHelper.LessButtonDoubleArrowIcon;
			m_btnMore.Click -= new EventHandler(btnMore_Click);
			m_btnMore.Click += new EventHandler(btnLess_Click);
			AdjustHeightAndMinSize(Height + m_optionsPanel.Height, m_moreMinSize);
			m_optionsPanel.Visible = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show fewer options.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void btnLess_Click(object sender, System.EventArgs e)
		{
			m_btnMore.Text = m_sMoreButtonText;
			m_btnMore.Image = ResourceHelper.MoreButtonDoubleArrowIcon;
			m_btnMore.Click += new EventHandler(btnMore_Click);
			m_btnMore.Click -= new EventHandler(btnLess_Click);
			AdjustHeightAndMinSize(Height - m_optionsPanel.Height, m_lessMinSize);
			m_optionsPanel.Visible = false;
		}

		///-------------------------------------------------------------------------------
		/// <summary>
		/// Draws an etched line on the dialog to separate the Search Options from the
		/// basic controls.
		/// </summary>
		///-------------------------------------------------------------------------------
		void m_optionsPanel_Paint(object sender, PaintEventArgs e)
		{
			int dxMargin = 10;
			int left = m_optionsLabel.Right;
			LineDrawing.Draw(e.Graphics, left,
				(m_optionsLabel.Top + m_optionsLabel.Bottom) / 2,
				m_optionsPanel.Right - left - dxMargin, LineTypes.Etched);
		}

		#endregion Event handlers

		private void m_refreshButton_Click(object sender, EventArgs e)
		{
			// Make sure preview is off.
			EnsurePreviewOff();
			// Check all items still enabled
			foreach (int hvoCba in m_enabledItems)
				m_cache.VwCacheDaAccessor.CacheIntProp(hvoCba, XmlBrowseViewBaseVc.ktagItemSelected, 1);
			//m_sourceSentences.BrowseViewer.ReconstructView();
			// Reconstruct the main list of objects we are showing.
			m_srcClerk.ListLoadingSuppressed = false;
			m_srcClerk.OnRefresh(null);
			m_srcClerk.ListLoadingSuppressed = true;
		}

		private void EnsurePreviewOff()
		{
			if (m_previewOn)
				m_btnPreviewClear_Click(this, new EventArgs());
		}

		private void m_cbMaintainCase_CheckedChanged(object sender, EventArgs e)
		{
			if (m_respellUndoaction != null)
			{
				m_respellUndoaction.PreserveCase = m_cbMaintainCase.Checked;
				m_respellUndoaction.UpdatePreviews();
				m_sourceSentences.BrowseViewer.ReconstructView();
			}
		}
	}

	/// <summary>
	/// a data record class for an occurrence of a spelling change
	/// </summary>
	internal class RespellOccurrence
	{
		int m_hvoBa; // Hvo of CmBaseAnnotation representing occurrence
		// position in the UNMODIFIED input string. (Note that it may not start at this position
		// in the output string, since there may be prior occurrences.) Initially equal to
		// m_hvoBa.BeginOffset; but that may get changed if we are undone.
		int m_ich;

		public RespellOccurrence(int hvoBa, int ich)
		{
			m_hvoBa = hvoBa;
			m_ich = ich;
		}
	}

	/// <summary>
	/// Information about how a paragraph is being changed.
	/// </summary>
	class ParaChangeInfo
	{
		int m_hvoTarget; // the one being changed.
		ITsString m_newContents; // what it will become if the change goes ahead.
		ITsString m_oldContents; // what it was to start with (and will be again if Undone)
		List<int> m_changes = new List<int>(); // hvos of CBAs that represent occurrences being changed.
		int m_flid; // property being changed (typically paragraph contents, but occasionally picture caption)
		int m_ws; // if m_flid is multilingual, ws of alternative; otherwise, zero.

		public ParaChangeInfo(int hvoTarget, int flid, int ws)
		{
			m_hvoTarget = hvoTarget;
			m_flid = flid;
			m_ws = ws;
		}

		// para contents if change proceeds.
		public ITsString NewContents
		{
			get { return m_newContents; }
			set { m_newContents = value; }
		}

		// para contents if change does not proceed (or is undone).
		public ITsString OldContents
		{
			get { return m_oldContents; }
			set { m_oldContents = value; }
		}

		// Get the list of changes that are to be made to this paragraph, that is, CmBaseAnnotations
		// which point at it and have been selected to be changed.
		public List<int> Changes
		{
			get { return m_changes; }
		}

		const int kflidBeginOffset = (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset;
		/// <summary>
		/// Sort the changes (by beginOffset)
		/// </summary>
		public void SortChanges(FdoCache cache)
		{
			m_changes.Sort(
				delegate(int left, int right)
				{
					return cache.GetIntProperty(left, kflidBeginOffset).CompareTo(
					  cache.GetIntProperty(right, kflidBeginOffset));
				});
		}

		/// <summary>
		/// Set the specified value...typically your own new or old value...on the appropriate property.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="newValue"></param>
		public void SetString(FdoCache cache, ITsString newValue, bool fDoUpdate)
		{
			if (m_ws == 0)
			{
				if (fDoUpdate)
					cache.SetTsStringProperty(m_hvoTarget, m_flid, newValue);
				else
					cache.MainCacheAccessor.SetString(m_hvoTarget, m_flid, newValue);
			}
			else
			{
				if (fDoUpdate)
					cache.SetMultiStringAlt(m_hvoTarget, m_flid, m_ws, newValue);
				else
					cache.MainCacheAccessor.SetMultiStringAlt(m_hvoTarget, m_flid, m_ws, newValue);
			}
		}

		public int Flid
		{
			get { return m_flid; }
		}

		public int Ws
		{
			get { return m_ws; }
		}

		/// <summary>
		/// Figure what the new contents needs to be. (Also sets OldContents.)
		/// </summary>
		/// <param name="cache"></param>
		public void MakeNewContents(FdoCache cache, string oldSpelling, string newSpelling, RespellUndoAction action)
		{
			m_oldContents = RespellUndoAction.AnnotationTargetString(m_hvoTarget, m_flid, m_ws, cache);
			ITsStrBldr bldr = m_oldContents.GetBldr();
			SortChanges(cache);
			for (int i = m_changes.Count - 1; i >= 0; i--)
			{
				int ichMin = cache.GetIntProperty(m_changes[i], (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset);
				int ichLim = cache.GetIntProperty(m_changes[i], (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset);
				string replacement = action.Replacement(action.OldOccurrence(m_changes[i]));
				bldr.Replace(ichMin, ichLim, replacement, null);
			}
			m_newContents = bldr.GetString();
		}
	}

	/// <summary>
	/// This class handles doing and undoing a respelling operation. It also implements some of the functionality
	/// that is common to doing the action and previewing it.
	/// </summary>
	internal class RespellUndoAction : IUndoAction
	{
		// The spelling change
		string m_oldSpelling;
		string m_newSpelling;
		Set<int> m_changes = new Set<int>(); // CBAs that represent occurrences we will change.
		/// <summary>
		/// Key is hvo of StTxtPara, value is list (eventually sorted by BeginOffset) of
		/// CBAs that refer to it AND ARE BEING CHANGED.
		/// </summary>
		Dictionary<int, ParaChangeInfo> m_changedParas = new Dictionary<int, ParaChangeInfo>();
		FdoCache m_cache;
		IEnumerable<int> m_occurrences; // items requiring preview.
		bool m_fPreserveCase;
		bool m_fUpdateLexicalEntries;
		bool m_fAllChanged; // set true if all occurrences changed.
		bool m_fKeepAnalyses; // set true to keep analyses of old wordform, even if all changed.

		int m_tagPrecedingContext;
		int m_tagPreview;
		int m_tagAdjustedBegin;
		int m_tagAdjustedEnd;
		int m_tagEnabled;
		CaseFunctions m_cf; // Case functions instance for WS m_wsCf
		int m_wsCf;
		int m_vernWs; // The WS we want to use throughout.

		int m_hvoNewWf; // HVO of wordform created (or found or made real) during DoIt for new spelling.
		int m_hvoOldWf; // HVO of original wordform (possibly made real during DoIt) for old spelling.

		// These preserve the old spelling-dictionary status for Undo.
		bool m_fWasOldSpellingCorrect;
		bool m_fWasNewSpellingCorrect;

		// Info to support efficient Undo/Redo for large lists of changes.
		List<int> m_hvosToChangeIntProps = new List<int>(); // objects with integer props needing change
		List<int> m_tagsToChangeIntProps = new List<int>(); // tags of the properties
		List<int> m_oldValues = new List<int>(); // initial values (target value for Undo)
		List<int> m_newValues = new List<int>(); // alternate values (target value for Redo).

		int[] m_oldOccurrencesOldWf; // occurrences of original wordform before change
		int[] m_oldOccurrencesNewWf; // occurrences of new spelling wordform before change
		int[] m_newOccurrencesOldWf; // occurrences of original wordform after change
		int[] m_newOccurrencesNewWf; // occurrences of new spelling after change.

		/// <summary>
		/// Used in tests only at present, assumes default vernacular WS.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="oldSpelling"></param>
		/// <param name="newSpelling"></param>
		internal RespellUndoAction(FdoCache cache, string oldSpelling, string newSpelling)
			:this(cache, cache.DefaultVernWs, oldSpelling, newSpelling)
		{
		}

		/// <summary>
		/// Normal constructor
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="vernWs"></param>
		/// <param name="oldSpelling"></param>
		/// <param name="newSpelling"></param>
		internal RespellUndoAction(FdoCache cache, int vernWs, string oldSpelling, string newSpelling)
		{
			m_cache = cache;
			m_vernWs = vernWs;
			m_oldSpelling = oldSpelling;
			m_newSpelling = newSpelling;
		}

		internal bool PreserveCase
		{
			get { return m_fPreserveCase; }
			set { m_fPreserveCase = value; }
		}

		/// <summary>
		/// Note one occurrence that we should change.
		/// </summary>
		/// <param name="cba"></param>
		internal void AddOccurrence(int hvoCba)
		{
			m_changes.Add(hvoCba);
		}

		/// <summary>
		/// These three properties determine what we will do to a run which is corrected,
		/// but is not the primary spelling change in focus
		/// </summary>
		static internal int SecondaryTextProp
		{
			get { return (int)FwTextPropType.ktptForeColor; }
		}

		static internal int SecondaryTextVar
		{
			get { return (int)FwTextPropVar.ktpvDefault; }
		}

		static internal int SecondaryTextVal
		{
			get { return (int)ColorUtil.ConvertColorToBGR(Color.Gray); } //(int)FwTextToggleVal.kttvForceOn; }
		}

		/// <summary>
		/// Set up the appropriate preceding and following context for the given occurrence.
		/// </summary>
		/// <param name="tagPrecedingContext"></param>
		/// <param name="tagPreview"></param>
		/// <param name="tagAdjustedBegin"></param>
		/// <param name="tagAdjustedEnd"></param>
		/// <param name="tagEnabled"></param>
		/// <param name="occurrences"></param>
		internal void SetupPreviews(int tagPrecedingContext, int tagPreview,
			int tagAdjustedBegin, int tagAdjustedEnd, int tagEnabled, IEnumerable<int> occurrences)
		{
			m_tagPrecedingContext = tagPrecedingContext;
			m_tagPreview = tagPreview;
			m_tagAdjustedBegin = tagAdjustedBegin;
			m_tagAdjustedEnd = tagAdjustedEnd;
			m_tagEnabled = tagEnabled;
			m_occurrences = occurrences;
			UpdatePreviews();
		}

		/// <summary>
		/// Update all previews for the previously supplied list of occurrences.
		/// </summary>
		internal void UpdatePreviews()
		{
			// Build the dictionary that indicates what will change in each paragraph
			m_changedParas.Clear();
			BuildChangedParasInfo();
			UpdatePreviews(m_occurrences);
		}

		/// <summary>
		/// Return the original text indicated by the cba
		/// </summary>
		/// <param name="hvoCba"></param>
		/// <returns></returns>
		internal ITsString OldOccurrence(int hvoCba)
		{
			return OldOccurrence(hvoCba, 0);
		}
		/// <summary>
		/// Return the original text indicated by the cba
		/// </summary>
		/// <param name="hvoCba"></param>
		/// <param name="delta">Normally zero, in one case BeginOffset has already been adjusted by
		/// adding delta, need to subtract it here.</param>
		/// <returns></returns>
		internal ITsString OldOccurrence(int hvoCba, int delta)
		{
			int flid = m_cache.GetIntProperty(hvoCba, kflidFlid);
			int hvoTarget = GetTargetObject(hvoCba);
			ITsString tssValue;
			tssValue = AnnotationTargetString(hvoTarget, flid,
				m_cache.GetObjProperty(hvoCba, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidWritingSystem),
				m_cache);
			ITsStrBldr bldr = tssValue.GetBldr();
			int ichBegin = BeginOffset(hvoCba) - delta;
			int ichLim = EndOffset(hvoCba);
			if (ichLim < bldr.Length)
				bldr.Replace(ichLim, bldr.Length, "", null);
			if (ichBegin > 0)
				bldr.Replace(0, ichBegin, "", null);
			return bldr.GetString();
		}

		internal static ITsString AnnotationTargetString(int hvoTarget, int flid, int ws, FdoCache cache)
		{
			ITsString tssValue;
			if (IsMultilingual(flid))
			{
				tssValue = cache.GetMultiStringAlt(hvoTarget, flid, ws);
			}
			else
			{
				tssValue = cache.GetTsStringProperty(hvoTarget, flid);
			}
			return tssValue;
		}

		internal string Replacement(ITsString oldTss)
		{
			string replacement = m_newSpelling;
			if (PreserveCase)
			{
				int var;
				int ws = oldTss.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out var);
				if (m_cf == null || m_wsCf != ws)
				{
					string icuLocale = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(ws).IcuLocale;
					m_cf = new CaseFunctions(icuLocale);
					m_wsCf = ws;
				}
				if (m_cf.StringCase(oldTss.Text) == StringCaseStatus.title)
					replacement = m_cf.ToTitle(replacement);
			}
			return replacement;
		}
		/// <summary>
		/// Update previews for the listed occurrences.
		/// </summary>
		private void UpdatePreviews(IEnumerable<int> occurrences)
		{
			foreach (int hvoCba in occurrences)
			{
				int hvoPara = GetTargetObject(hvoCba);
				ParaChangeInfo info;
				if (m_changedParas.TryGetValue(hvoPara, out info))
				{
					// We have to build a modified string, and we might find hvoCba in the list.
					// We also have to figure out how much our offset changed, if the new spelling differs in length.
					ITsStrBldr bldr = info.NewContents.GetBldr();
					int delta = 0; // amount to add to offsets of later words.
					int beginTarget = BeginOffset(hvoCba);
					int ichange = 0;
					bool fGotOffsets = false;
					for(; ichange < info.Changes.Count; ichange++)
					{
						int hvoChange = info.Changes[ichange];
						int beginChange = BeginOffset(hvoChange);
						if (hvoChange == hvoCba)
						{
							// stop preceding context just before the current one.
							int ich = BeginOffset(hvoCba) + delta;

							bldr.ReplaceTsString(ich, bldr.Length, OldOccurrence(hvoCba));
							m_cache.VwCacheDaAccessor.CacheIntProp(hvoCba, m_tagAdjustedBegin, BeginOffset(hvoCba) + delta);
							m_cache.VwCacheDaAccessor.CacheIntProp(hvoCba, m_tagAdjustedEnd, EndOffset(hvoCba) + delta);
							break;
						}
						else if (beginChange > beginTarget && !fGotOffsets)
						{
							// This and future changes are after this occurrence, so the current delta is the one
							// we want (and this is an occurrence we are not changing, or we would have found it).
							SetOffsets(m_tagAdjustedBegin, m_tagAdjustedEnd, hvoCba, delta, beginTarget);
							fGotOffsets = true;
							// don't stop the loop, we want everything in the preceding context string, with later occurrences marked.
							// enhance JohnT: preceding context is the same for every unchanged occurrence in the paragraph,
							// if it is common to change some but not all in the same paragraph we could save it.
						}
						// It's another changed occurrence, not the primary one, highlight it.
						bldr.SetIntPropValues(beginChange + delta, beginChange + delta + m_newSpelling.Length,
							SecondaryTextProp, SecondaryTextVar, SecondaryTextVal);
						delta += m_newSpelling.Length - m_oldSpelling.Length;
					}
					m_cache.VwCacheDaAccessor.CacheStringProp(hvoCba, m_tagPrecedingContext, bldr.GetString());
					if (ichange < info.Changes.Count)
					{
						// need to set up following context also
						bldr = info.NewContents.GetBldr();
						bldr.Replace(0, beginTarget + delta, "", null); // remove everything before occurrence.
						// Make the primary occurrence bold
						bldr.SetIntPropValues(0, m_newSpelling.Length, (int)FwTextPropType.ktptBold,
							(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
						delta = -beginTarget + m_newSpelling.Length - m_oldSpelling.Length;
						ichange++;
						for (; ichange < info.Changes.Count; ichange++)
						{
							int hvoChange = info.Changes[ichange];
							int beginChange = BeginOffset(hvoChange);
							// It's another changed occurrence, not the primary one, highlight it.
							bldr.SetIntPropValues(beginChange + delta, beginChange + delta + m_newSpelling.Length,
								SecondaryTextProp, SecondaryTextVar, SecondaryTextVal);
							delta += m_newSpelling.Length - m_oldSpelling.Length;
						}
						m_cache.VwCacheDaAccessor.CacheStringProp(hvoCba, m_tagPreview, bldr.GetString());
					}
					else if (!fGotOffsets)
					{
						// an unchanged occurrence after all the changed ones
						SetOffsets(m_tagAdjustedBegin, m_tagAdjustedEnd, hvoCba, delta, beginTarget);
					}
				}
				else
				{
					// Unchanged paragraph, copy the key info over.
					ITsString tssVal;
					int flid = m_cache.GetIntProperty(hvoCba, kflidFlid);
					if (IsMultilingual(flid))
					{
						int ws = m_cache.GetObjProperty(hvoCba, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidWritingSystem);
						tssVal = m_cache.GetMultiStringAlt(hvoPara, flid, ws);
					}
					else
					{
						tssVal = m_cache.GetTsStringProperty(hvoPara, flid);
					}

					m_cache.VwCacheDaAccessor.CacheStringProp(hvoCba, m_tagPrecedingContext, tssVal);
					m_cache.VwCacheDaAccessor.CacheIntProp(hvoCba, m_tagAdjustedBegin, BeginOffset(hvoCba));
					m_cache.VwCacheDaAccessor.CacheIntProp(hvoCba, m_tagAdjustedEnd, EndOffset(hvoCba));
				}
			}
		}

		private void SetOffsets(int tagAdjustedBegin, int tagAdjustedEnd, int hvoCba, int delta, int beginTarget)
		{
			m_cache.VwCacheDaAccessor.CacheIntProp(hvoCba, tagAdjustedBegin, beginTarget + delta);
			m_cache.VwCacheDaAccessor.CacheIntProp(hvoCba, tagAdjustedEnd, beginTarget + delta + m_oldSpelling.Length);
		}

		private int BeginOffset(int hvoCba)
		{
			return m_cache.GetIntProperty(hvoCba, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset);
		}
		private int EndOffset(int hvoCba)
		{
			return m_cache.GetIntProperty(hvoCba, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset);
		}

		/// <summary>
		/// Set up the dictionary which tracks changes for each paragraph.
		/// </summary>
		private void BuildChangedParasInfo()
		{
			foreach (int hvoCba in m_changes)
			{
				ParaChangeInfo info = EnsureParaInfo(hvoCba, 0);
				info.Changes.Add(hvoCba);
			}
			// Build the new strings for each
			foreach (ParaChangeInfo info1 in m_changedParas.Values)
			{
				info1.MakeNewContents(m_cache, m_oldSpelling, m_newSpelling, this);
			}
		}

		const int kflidFlid = (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidFlid;

		private ParaChangeInfo EnsureParaInfo(int hvoCba, int hvoTargetPara)
		{
			int hvoPara = GetTargetObject(hvoCba);
			if (hvoTargetPara != 0 && hvoPara != hvoTargetPara)
				return null;
			ParaChangeInfo info;
			if (!m_changedParas.TryGetValue(hvoPara, out info))
			{
				int flid = m_cache.GetIntProperty(hvoCba, kflidFlid);
				int ws = 0;
				if (IsMultilingual(flid))
					ws = m_cache.GetObjProperty(hvoCba, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidWritingSystem);
				info = new ParaChangeInfo(hvoPara, flid, ws);
				m_changedParas[hvoPara] = info;
			}
			return info;
		}

		// For now it's good enough to treat anything but StTxtPara.Contents as multilingual.
		internal static bool IsMultilingual(int flid)
		{
			return flid != kflidContents;
		}

		private int GetTargetObject(int hvoCba)
		{
			return m_cache.GetObjProperty(hvoCba, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
		}

		/// <summary>
		/// Update the preview when the check status of a single HVO changes.
		/// </summary>
		/// <param name="hvosChanged"></param>
		/// <param name="isChecked"></param>
		internal void UpdatePreview(int hvoChanged, bool isChecked)
		{
			if (m_changes.Contains(hvoChanged) == isChecked)
				return;
			if (isChecked)
			{
				m_changes.Add(hvoChanged);
			}
			else
				m_changes.Remove(hvoChanged);
			int hvoPara = GetTargetObject(hvoChanged);
			List<int> occurrencesAffected = new List<int>();
			foreach (int hvo in m_occurrences)
			{
				if (GetTargetObject(hvo) == hvoPara)
					occurrencesAffected.Add(hvo);
			}
			ParaChangeInfo info = EnsureParaInfo(hvoChanged, hvoPara);
			if (isChecked)
				info.Changes.Add(hvoChanged);
			else
				info.Changes.Remove(hvoChanged);
			info.MakeNewContents(m_cache, m_oldSpelling, m_newSpelling, this);
			UpdatePreviews(occurrencesAffected);
			foreach (int hvo in occurrencesAffected)
			{
				// This is enough PropChanged to redraw the whole containing paragraph
				m_cache.PropChanged(hvo, m_tagEnabled, 0, 0, 0);
			}
		}

		const int kflidBeginOffset = (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset;
		const int kflidEndOffset = (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset;
		const int kflidContents = (int)StTxtPara.StTxtParaTags.kflidContents;
		/// <summary>
		/// Update offsets for the given paragraph
		/// </summary>
		private void UpdateOffsets(int hvoPara, ParaChangeInfo info)
		{
			int ichange = 0; // index into info.Changes;
			int delta = 0; // amount to add to offsets of later words.
			if (m_cache.GetClassOfObject(hvoPara) == StTxtPara.kclsidStTxtPara)
			{
				// Adjust segment-by-segment so as to keep track of how far each segment boundary is
				// moved by the replacements within it.
				IStTxtPara para = StTxtPara.CreateFromDBObject(m_cache, hvoPara);
				List<int> segIds = para.Segments;
				foreach (int currentSegmentId in segIds)
				{
					AdjustOffset(currentSegmentId, delta, kflidBeginOffset);
					List<int> xficIds = para.SegmentForms(currentSegmentId);
					AdjustXficOffsets(info, xficIds, ref delta, ref ichange);
					AdjustOffset(currentSegmentId, delta, kflidEndOffset);
				}
			}
			else
			{
				// Not a paragraph, so there won't be segments, and we can't make an IStTxtPara object.
				// Just adjust all the wfics.
				AdjustXficOffsets(info, info.Changes, ref delta, ref ichange);
			}
		}

		/// <summary>
		/// Adjust the offsets of the xfics (wfics or pfics, wordform-in-context or punct-in-context) given
		/// in xficIds, which are a (sub)range of the ids in info.changes. Both input and output are the
		/// total change in length and the index of the place we are up to in info.Changes.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="xficIds"></param>
		/// <param name="delta"></param>
		/// <param name="ichange"></param>
		private void AdjustXficOffsets(ParaChangeInfo info, List<int> xficIds, ref int delta, ref int ichange)
		{
			foreach (int xfic in xficIds)
			{
				int beginTarget = BeginOffset(xfic);
				for (; ichange < info.Changes.Count; ichange++)
				{
					int hvoChange = info.Changes[ichange];
					int beginChange = BeginOffset(hvoChange);
					if (beginChange >= beginTarget)
						break;
					ITsString tssOld = OldOccurrence(hvoChange);
					string replacement = Replacement(tssOld);
					delta += replacement.Length - tssOld.Length;
				}
				AdjustOffset(xfic, delta, kflidBeginOffset);
				if (ichange < info.Changes.Count && info.Changes[ichange] == xfic)
				{
					// Adjusting the target itself...it's end needs a different adjustment.
					// Pass delta to account for the fact that the begin offset of xfic has
					// already been adjusted.
					ITsString tssOld = OldOccurrence(xfic, delta);
					string replacement = Replacement(tssOld);
					delta += replacement.Length - tssOld.Length;
					ichange++;
				}
				AdjustOffset(xfic, delta, kflidEndOffset);
			}
		}

		const int kflidInstanceOf = (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf;

		private void UpdateInstanceOf(int hvoWf)
		{
			UpdateInstanceOf(hvoWf, null);
		}
		/// <summary>
		/// Update all the changed wordforms to point at the specified Wf.
		/// </summary>
		/// <param name="hvoWf"></param>
		private void UpdateInstanceOf(int hvoWf, ProgressDialogWorkingOn dlg)
		{
			foreach (ParaChangeInfo info in m_changedParas.Values)
			{
				foreach (int hvoChange in info.Changes)
				{
					if (m_cache.IsDummyObject(hvoChange))
						m_cache.VwCacheDaAccessor.CacheObjProp(hvoChange, kflidInstanceOf, hvoWf);
					else
						m_cache.MainCacheAccessor.SetObjProp(hvoChange, kflidInstanceOf, hvoWf);
				}
				UpdateProgress(dlg);
			}
		}

		private void AdjustOffset(int cba, int delta, int flid)
		{
			if (delta == 0)
				return;
			int oldVal = m_cache.GetIntProperty(cba, flid);
			m_hvosToChangeIntProps.Add(cba);
			m_tagsToChangeIntProps.Add(flid);
			m_oldValues.Add(oldVal);
			int newVal = oldVal + delta;
			m_newValues.Add(newVal);
			// If it's a dummy we just update the cache value; if real, we do a proper set, but don't
			// do PropChanged, it's too expensive. We'll reconstruct when done.
			if (m_cache.IsDummyObject(cba))
				m_cache.VwCacheDaAccessor.CacheIntProp(cba, flid, newVal);
			else
				m_cache.MainCacheAccessor.SetInt(cba, flid, newVal);
		}

		/// <summary>
		/// Actually make the change indicated in the action (execute the original command,
		/// from the Apply button in the dialog).
		/// </summary>
		public void DoIt()
		{
			if (m_changes.Count == 0)
				return;
			if (m_changedParas.Count == 0) // Preview not used
				BuildChangedParasInfo();
			if (m_changedParas.Count < 10)
				CoreDoIt(null);
			else
			{
				using (ProgressDialogWorkingOn dlg = new ProgressDialogWorkingOn())
				{
					dlg.Owner = Form.ActiveForm;
					dlg.Icon = dlg.Owner.Icon;
					dlg.Minimum = 0;
					// 2x accounts for two main loops; extra 10 very roughly accounts for final cleanup.
					dlg.Maximum = m_changedParas.Count * 2 + 10;
					dlg.Text = MEStrings.ksChangingSpelling;
					dlg.WorkingOnText = MEStrings.ksChangingSpelling;
					dlg.ProgressLabel = MEStrings.ksProgress;
					dlg.Show();
					dlg.BringToFront();
					CoreDoIt(dlg);
					dlg.Close();
				}
			}
		}

		// The new wordform (if any).
		internal int NewWordform
		{
			get { return m_hvoNewWf; }
		}

		/// <summary>
		/// Core of the DoIt method, may be called with or without progress dialog.
		/// </summary>
		/// <param name="dlg"></param>
		private void CoreDoIt(ProgressDialogWorkingOn dlg)
		{
			// While we do the changes which we can much more efficiently Undo/Redo in this action,
			// we don't want the various changes we make generating masses of SqlUndoActions.
			IActionHandler handler = m_cache.ActionHandlerAccessor;
			m_cache.MainCacheAccessor.SetActionHandler(null);
			m_hvoNewWf = WfiWordform.FindOrCreateWordform(m_cache, m_newSpelling, m_vernWs, true);
			try
			{
				foreach (KeyValuePair<int, ParaChangeInfo> pair in m_changedParas)
				{
					UpdateOffsets(pair.Key, pair.Value);
					// Review JohnT: should we do the PropChanged, or not?? If not, we should force
					// a Refresh when the dialog closes.
					if (!pair.Value.OldContents.Equals(pair.Value.NewContents))
						pair.Value.SetString(m_cache, pair.Value.NewContents, true);
					UpdateProgress(dlg);
				}
				UpdateInstanceOf(m_hvoNewWf, dlg);
			}
			finally
			{
				m_cache.MainCacheAccessor.SetActionHandler(handler);
			}
			if (dlg != null)
				dlg.WorkingOnText = MEStrings.ksDealingAnalyses;
			m_cache.BeginUndoTask(string.Format(MEStrings.ksUndoChangeSpelling, m_oldSpelling, m_newSpelling),
				string.Format(MEStrings.ksRedoChangeSpelling, m_oldSpelling, m_newSpelling));
			m_cache.ActionHandlerAccessor.AddAction(this);
			UpdateProgress(dlg);

			// The destination wordform really exists and should be marked correct.
			IWfiWordform wf = WfiWordform.CreateFromDBObject(m_cache, m_hvoNewWf);
			m_oldOccurrencesNewWf = wf.OccurrencesInTexts.ToArray();
			m_fWasNewSpellingCorrect = wf.SpellingStatus == (int)SpellingStatusStates.correct;
			wf.SpellingStatus = (int)SpellingStatusStates.correct;
			EnchantHelper.SetSpellingStatus(m_newSpelling, m_vernWs,
				m_cache.LanguageWritingSystemFactoryAccessor, true);
			UpdateProgress(dlg);

			m_hvoOldWf = WfiWordform.FindOrCreateWordform(m_cache, m_oldSpelling, m_vernWs, true);
			IWfiWordform wfOld = WfiWordform.CreateFromDBObject(m_cache, m_hvoOldWf);
			m_oldOccurrencesOldWf = wfOld.OccurrencesInTexts.ToArray();
			m_fWasOldSpellingCorrect = wfOld.SpellingStatus == (int)SpellingStatusStates.correct;
			UpdateProgress(dlg);

			// Compute new occurrence lists, save and cache
			Set<int> changes = new Set<int>();
			foreach (ParaChangeInfo info in m_changedParas.Values)
				changes.AddRange(info.Changes);
			if (AllChanged)
			{
				m_newOccurrencesOldWf = new int[0]; // no remaining occurrences
			}
			else
			{
				// Only some changed, need to figure m_newOccurrences
				List<int> newOccurrencesOldWf = new List<int>();
				foreach (int hvo in m_oldOccurrencesOldWf)
					if (!changes.Contains(hvo))
						newOccurrencesOldWf.Add(hvo);
				m_newOccurrencesOldWf = newOccurrencesOldWf.ToArray();
			}
			UpdateProgress(dlg);
			List<int> newOccurrences = new List<int>(m_oldOccurrencesNewWf.Length + changes.Count);
			newOccurrences.AddRange(m_oldOccurrencesNewWf);
			foreach (int hvo in changes)
			{
				if (m_cache.IsDummyObject(hvo))
					// if this is a dummy annotation, make sure we update the owner
					m_cache.VwCacheDaAccessor.CacheObjProp(hvo, (int)CmObjectFields.kflidCmObject_Owner, m_hvoNewWf);
				newOccurrences.Add(hvo);
			}
			m_newOccurrencesNewWf = newOccurrences.ToArray();
			int vhId = BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "WfiWordform", "OccurrencesInTexts");
			m_cache.VwCacheDaAccessor.CacheVecProp(m_hvoOldWf, vhId, m_newOccurrencesOldWf, m_newOccurrencesOldWf.Length);
			m_cache.VwCacheDaAccessor.CacheVecProp(m_hvoNewWf, vhId, m_newOccurrencesNewWf, m_newOccurrencesNewWf.Length);
			SendCountVirtualPropChanged(m_hvoNewWf);
			SendCountVirtualPropChanged(m_hvoOldWf);
			UpdateProgress(dlg);

			// Deal with analyses.
			if (CopyAnalyses)
			{
				foreach (WfiAnalysis analysis in wfOld.AnalysesOC)
				{
					// Only copy approved analyses.
					if (analysis.GetAgentOpinion(m_cache.LangProject.DefaultUserAgent) != Opinions.approves)
						continue;
					IWfiAnalysis newAnalysis = wf.AnalysesOC.Add(new WfiAnalysis());
					foreach (WfiGloss gloss in analysis.MeaningsOC)
					{
						IWfiGloss newGloss = newAnalysis.MeaningsOC.Add(new WfiGloss());
						newGloss.Form.CopyAlternatives(gloss.Form);
					}
					foreach (WfiMorphBundle bundle in analysis.MorphBundlesOS)
					{
						IWfiMorphBundle newBundle = newAnalysis.MorphBundlesOS.Append(new WfiMorphBundle());
						newBundle.Form.CopyAlternatives(bundle.Form);
						newBundle.SenseRA = bundle.SenseRA;
						newBundle.MorphRA = bundle.MorphRA;
						newBundle.MsaRA = bundle.MsaRA;
					}
				}
			}
			UpdateProgress(dlg);
			if (AllChanged)
			{
				wfOld.SpellingStatus = (int)SpellingStatusStates.incorrect;
				EnchantHelper.SetSpellingStatus(m_oldSpelling, m_vernWs,
					m_cache.LanguageWritingSystemFactoryAccessor, false);
				if (UpdateLexicalEntries)
				{
					foreach (WfiAnalysis wa in wfOld.AnalysesOC)
					{
						if (wa.MorphBundlesOS.Count == 1)
						{
						}
					}
				}
				if (!KeepAnalyses)
				{
					// Remove multi-morpheme anals in src wf.
					List<IWfiAnalysis> goners = new List<IWfiAnalysis>();
					foreach (IWfiAnalysis goner in wfOld.AnalysesOC)
					{
						if (goner.MorphBundlesOS.Count > 1)
							goners.Add(goner);
					}
					foreach (IWfiAnalysis goner in goners)
					{
						goner.DeleteUnderlyingObject(); // This will shift twfic pointers up to wordform, if needed.
					}
					goners.Clear();
				}
				if (UpdateLexicalEntries)
				{
					// Change LE allo on single morpheme anals.
					foreach (IWfiAnalysis update in wfOld.AnalysesOC)
					{
						if (update.MorphBundlesOS.Count == 1)
						{
							IWfiMorphBundle mb = update.MorphBundlesOS[0];

							TsStringAccessor tsa = mb.Form.GetAlternative(m_vernWs);
							string srcForm = tsa.Text;
							if (srcForm != null)
							{
								// Change morph bundle form.
								mb.Form.SetAlternative(m_newSpelling, m_vernWs);
							}

							IMoForm mf = mb.MorphRA;
							if (mf != null)
							{
								mf.Form.SetAlternative(m_newSpelling, m_vernWs);
							}
						}
					}
				}

				// Move remaining anals from src wf to new wf.
				// This changes the owners of the remaining ones,
				// since it is an owning property.
				wf.AnalysesOC.Add(wfOld.AnalysesOC.HvoArray);

				SendAnalysisVirtualPropChanged(m_hvoNewWf);
				SendAnalysisVirtualPropChanged(m_hvoOldWf);
				UpdateProgress(dlg);
			}
			m_cache.EndUndoTask();
		}

		private static void UpdateProgress(ProgressDialogWorkingOn dlg)
		{
			if (dlg != null)
			{
				dlg.PerformStep();
				dlg.Update();
			}
		}

		/// <summary>
		/// Send notifications indicating that the virtual properties for the subtypes of analyses have changed.
		/// Since these are collections, it's arbitrary what changed; we simulate one item being added.
		/// </summary>
		/// <param name="wf"></param>
		private void SendAnalysisVirtualPropChanged(int hvoWf)
		{
			if (!m_cache.IsValidObject(hvoWf))
				return; // may be in a state, e.g. Undone, where it doesn't exist.
			if (m_cache.VwCacheDaAccessor.GetVirtualHandlerName("WfiWordform", "HumanApprovedAnalyses") == null)
				return; // testing (grrr...test code in production!)
		}

		private void SendCountVirtualPropChanged(int hvoWf)
		{
			// Notify everyone about the change in the virtual properties
			// for the three types of analyses.
			WordformVirtualPropChanged(hvoWf, "HumanApprovedAnalyses");
			WordformVirtualPropChanged(hvoWf, "HumanNoOpinionParses");
			WordformVirtualPropChanged(hvoWf, "HumanDisapprovedParses");
			WordformVirtualPropChanged(hvoWf, "FullConcordanceCount");
			WordformVirtualPropChanged(hvoWf, "UserCount");
			WordformVirtualPropChanged(hvoWf, "ParserCount");
			WordformVirtualPropChanged(hvoWf, "ConflictCount");
		}
		void WordformVirtualPropChanged(int hvoWf, string name)
		{
			m_cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll,
				hvoWf,
				BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "WfiWordform", name),
				0,
				0,
				1);
		}

		/// <summary>
		/// Switch the values of the integer variables as indicated.
		/// </summary>
		/// <param name="altVals"></param>
		void SwitchValues(List<int> altVals)
		{
			for (int i = 0; i < m_hvosToChangeIntProps.Count; i++)
			{
				int hvo = m_hvosToChangeIntProps[i];
				int tag = m_tagsToChangeIntProps[i];
				int newVal = altVals[i];
				if (m_cache.IsDummyObject(hvo))
					m_cache.VwCacheDaAccessor.CacheIntProp(hvo, tag, newVal);
				else
					m_cache.MainCacheAccessor.SetInt(hvo, tag, newVal);
			}
		}

		/// <summary>
		/// Flag may be set to true so that where a wordform has monomorphemic analysis/es,
		/// the lexical entry(s) will also be updated. That is, if this is true, and there is a
		/// monomorphemic analysis that points at a particular MoForm, the form of the MoForm will
		/// be updated.
		/// </summary>
		public bool UpdateLexicalEntries
		{
			get { return m_fUpdateLexicalEntries; }
			set { m_fUpdateLexicalEntries = value; }
		}

		/// <summary>
		/// Flag set true if all occurrences changed. Enables effects of UpdateLexicalEntries
		/// and KeepAnalyses, and causes old wordform to be marked incorrect.
		/// </summary>
		public bool AllChanged
		{
			get { return m_fAllChanged; }
			set { m_fAllChanged = value; }
		}

		/// <summary>
		/// Flag set true to keep analyses, even if all occurrences changed.
		/// (Monomorphemic analyses are kept anyway, if UpdateLexicalEntries is true.)
		/// </summary>
		public bool KeepAnalyses
		{
			get { return m_fKeepAnalyses; }
			set { m_fKeepAnalyses = value; }
		}

		#region IUndoAction Members

		public void Commit()
		{
		}

		public bool IsDataChange()
		{
			return true;
		}

		public bool IsRedoable()
		{
			return true;
		}

		public bool Redo(bool fRefreshPending)
		{
			SwitchValues(m_newValues);
			foreach (KeyValuePair<int, ParaChangeInfo> pair in m_changedParas)
			{
				// Review JohnT: should we do the PropChanged, or not?? If not, we should force
				// a Refresh when the dialog closes.
				pair.Value.SetString(m_cache, pair.Value.NewContents, !fRefreshPending);
			}
			UpdateInstanceOf(m_hvoNewWf);
			// Make sure that the re-created new wordform is the one we will look up if we ask for one for this string and ws.
			// This MIGHT only be needed during testing (when a test has created a dummy in the process of verifying the Undo),
			// but it makes the shipping code more robust, too.
			m_cache.LangProject.WordformInventoryOA.UpdateConcWordform(m_newSpelling, m_vernWs, m_hvoNewWf);
			EnchantHelper.SetSpellingStatus(m_newSpelling, m_vernWs,
				m_cache.LanguageWritingSystemFactoryAccessor, true);
			if (AllChanged)
			{
				EnchantHelper.SetSpellingStatus(m_oldSpelling, m_vernWs,
					m_cache.LanguageWritingSystemFactoryAccessor, false);
				SendAnalysisVirtualPropChanged(m_hvoNewWf);
				SendAnalysisVirtualPropChanged(m_hvoOldWf);
			}
			int vhId = BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "WfiWordform", "OccurrencesInTexts");
			m_cache.VwCacheDaAccessor.CacheVecProp(m_hvoOldWf, vhId, m_newOccurrencesOldWf, m_newOccurrencesOldWf.Length);
			m_cache.VwCacheDaAccessor.CacheVecProp(m_hvoNewWf, vhId, m_newOccurrencesNewWf, m_newOccurrencesNewWf.Length);
			SendCountVirtualPropChanged(m_hvoNewWf);
			SendCountVirtualPropChanged(m_hvoOldWf);
			return true;
		}

		bool m_fCopyAnalyses;

		public bool CopyAnalyses
		{
			get { return m_fCopyAnalyses; }
			set { m_fCopyAnalyses = value; }
		}

		public bool RequiresRefresh()
		{
			return false;
		}

		public bool SuppressNotification
		{
			set {  }
		}

		public bool Undo(bool fRefreshPending)
		{
			SwitchValues(m_oldValues);
			foreach (KeyValuePair<int, ParaChangeInfo> pair in m_changedParas)
			{
				// Review JohnT: should we do the PropChanged, or not?? If not, we should force
				// a Refresh when the dialog closes.
				pair.Value.SetString(m_cache, pair.Value.OldContents, !fRefreshPending);
			}
			UpdateInstanceOf(m_hvoOldWf);
			EnchantHelper.SetSpellingStatus(m_newSpelling, m_vernWs,
				m_cache.LanguageWritingSystemFactoryAccessor, m_fWasNewSpellingCorrect);
			EnchantHelper.SetSpellingStatus(m_oldSpelling, m_vernWs,
				m_cache.LanguageWritingSystemFactoryAccessor, m_fWasOldSpellingCorrect);
			int vhId = BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "WfiWordform", "OccurrencesInTexts");
			m_cache.VwCacheDaAccessor.CacheVecProp(m_hvoOldWf, vhId, m_oldOccurrencesOldWf, m_oldOccurrencesOldWf.Length);
			m_cache.VwCacheDaAccessor.CacheVecProp(m_hvoNewWf, vhId, m_oldOccurrencesNewWf, m_oldOccurrencesNewWf.Length);
			SendCountVirtualPropChanged(m_hvoNewWf);
			SendCountVirtualPropChanged(m_hvoOldWf);
			if (AllChanged)
			{
				SendAnalysisVirtualPropChanged(m_hvoNewWf);
				SendAnalysisVirtualPropChanged(m_hvoOldWf);
			}
			return true;
		}

		#endregion

		/// <summary>
		/// Remove all changed items from the set of enabled ones.
		/// </summary>
		/// <param name="m_enabledItems"></param>
		internal void RemoveChangedItems(Set<int> m_enabledItems, int tagEnabled)
		{
			foreach (ParaChangeInfo info in m_changedParas.Values)
				foreach (int hvoCba in info.Changes)
				{
					m_cache.VwCacheDaAccessor.CacheIntProp(hvoCba, tagEnabled, 0);
					m_enabledItems.Remove(hvoCba);
				}
		}
	}
}
